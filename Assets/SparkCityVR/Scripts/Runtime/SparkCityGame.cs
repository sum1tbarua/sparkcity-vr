// Spark City VR - main controller.
// Welcome space (artwork, Enter / Tutorial / Exit) -> optional tutorial -> Command Center mission board
// -> Start Mission -> mini-games -> summary. Gameplay is logged per Player ID.
// Every mini-game ends with a "REMEMBER" card - the one line students should memorize.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SparkCity
{
    /// <summary>What the player actually did in Mission 1 - used to build their own final report.</summary>
    public class MissionProgress
    {
        public readonly List<string> keptUnreliable = new List<string>(); // kept, not needed AND veracity < 50 (critical)
        public readonly List<string> keptUnneeded = new List<string>();   // kept, not needed but reliable (minor)
        public readonly List<string> tossedNeeded = new List<string>();   // needed data thrown away (critical)
        public readonly List<string> missedTools = new List<string>();    // tool ids of dirty records that slipped through
        public int recordsMissed;      // dirty records that reached the player uncleaned
        public int recordsTotal;
        public int pipelineWrongTries;
        public int clusterWrongMoves;
        public string cityOutcome = "";
    }

    public enum WelcomeArtMode { CurvedScreen, FlatScreen, Panorama360 }

    public class SparkCityGame : MonoBehaviour
    {
        [Tooltip("Editor testing: 1 or 2 jumps straight into that mission; 0 starts at the Welcome screen.")]
        public int debugStartMission = 0;

        [Header("Welcome artwork  (Resources/SparkCityArt/welcome)")]
        [Tooltip("CurvedScreen: any normal picture, bent around the player like a cinema dome (no stretching).\n" +
                 "FlatScreen: a giant flat screen in front of the player (any picture).\n" +
                 "Panorama360: full sky around the player - ONLY for equirectangular 360 images (2:1).")]
        public WelcomeArtMode welcomeArtMode = WelcomeArtMode.CurvedScreen;
        [Tooltip("Width of the flat welcome screen in metres.")]
        public float flatScreenWidth = 9f;
        [Tooltip("Curved screen: height in metres (the width follows the picture's shape).")]
        public float curvedHeight = 5.2f;
        [Tooltip("Curved screen: distance from the player in metres. Smaller = wraps further around you.")]
        public float curvedRadius = 4.5f;

        public Hud Hud { get; private set; }
        public CityMap Map { get; private set; }
        public MissionProgress Progress { get; private set; }

        /// <summary>True while a card (how to play, learn more, remember) is open: games and timers wait.</summary>
        public bool Paused { get { return card != null && card.Visible; } }

        // flood countdown (Mission 1)
        bool floodActive;
        float floodTotal, floodLeft;
        bool floodWarned;
        public float FloodLeft { get { return floodLeft; } }
        public bool FloodActive { get { return floodActive; } }

        Transform world;
        Panel menu;   // big panel for menus, quiz and summaries
        Panel card;   // overlay card: briefing, remember, learn more
        GameObject welcomeArt; // optional artwork from Resources/SparkCityArt/welcome
        MiniGame current;
        readonly List<MiniGame> finished = new List<MiniGame>();
        Coroutine missionRoutine;
        int missionNumber;
        float missionStart;
        bool cardClicked;

        Panel welcome; // the panel in the welcome space

        static ContentDef C { get { return GameData.Content; } }

        // ================================================================== setup
        IEnumerator Start()
        {
            // black screen while loading, and stand in the welcome space from the very first frame,
            // so the command center is never seen before the welcome screen
            ScreenFader.Hold();
            if (debugStartMission != 1 && debugStartMission != 2) PlayerRig.MoveTo(Layout.Welcome);
            EnsureEventSystem();
            if (GetComponent<DesktopTestMode>() == null) gameObject.AddComponent<DesktopTestMode>();
            WorldBuilder.SetupLighting();
            world = Fx.Group("SparkCityWorld", null).transform;
            WorldBuilder.BuildCommandCenter(world);
            WorldBuilder.BuildClusterRoom(world);
            Map = CityMap.Build(world, Layout.MapCenter, 0f);

            Hud = Hud.Create();
            Hud.onHint = () => { if (current != null) current.UseHint(); };
            Hud.onMore = ShowMore;
            Hud.onMenu = ShowBoard;
            menu = Panel.Create("Menu", null, new Vector2(1300, 980), true);
            menu.bg.color = Pal.BoardBg;
            menu.canvas.sortingOrder = 10; // drawn after the city-map labels, so they never show through the panel
            welcome = Panel.Create("Welcome", null, new Vector2(1150, 700), true, 96); // width x height in pixels (1 px = 1 mm)
            welcome.bg.color = Pal.WelcomeBg; // dark navy, slightly see-through
            welcome.SetVisible(false);
            WorldBuilder.BuildWelcomeSpace(world);
            card = Panel.Create("Card", null, new Vector2(1250, 720), true, 78, 0.00075f); // 0.94 m x 0.54 m
            card.canvas.sortingOrder = 20; // help cards stay in front of the board panel
            menu.SetVisible(false);
            card.SetVisible(false);
            BuildWelcomeArt();

            yield return new WaitForSeconds(0.75f); // let head tracking start
            if (C == null || C.mission1 == null || C.mission2 == null)
            {
                OpenMenu("Setup problem");
                menu.AddText("content.json could not be loaded (see the Console).", 32, Pal.Bad);
                yield break;
            }
            Map.Setup(C.mission1.districts, Fx.Hex(C.mission1.hazardColor, Pal.Cyan));
            if (debugStartMission == 1 || debugStartMission == 2) StartMission(debugStartMission);
            else ShowWelcome();
            ScreenFader.Blink("", 0.4f); // gentle fade-in at startup
        }

        /// <summary>Set by Clean-Up practice: the flood waits while the player learns the controls.</summary>
        public bool FloodHold;
        /// <summary>Set by the Trust Check while it shows a consequence on the map.</summary>
        public bool MapOverride;

        void Update()
        {
            GameClock.Paused = Paused;
            if (Hud != null) Hud.SetUrgent(floodActive && floodLeft < 60f);
            if (Hud != null) Hud.SetTimer(floodActive, floodLeft); // keeps the badge correct (and pulsing) even while paused
            if (!floodActive || Paused || FloodHold) return;
            floodLeft -= Time.deltaTime;
            if (floodLeft <= 0f && !floodWarned)
            {
                floodWarned = true;
                Sfx.Bad();
                Hud.Say("The river has reached " + C.mission1.DistrictName(C.mission1.hazardDistrict) + "! Finish quickly - every second counts.", Pal.Bad);
            }
            RefreshPoints();
            // the water visibly rises in the danger district while you work
            float level = floodTotal > 0f ? Mathf.Clamp01(1f - floodLeft / floodTotal) : 0f;
            if (Map != null && !MapOverride)
            {
                Map.SetHazard(C.mission1.hazardDistrict, 0.15f + 0.85f * level);
                // tie the rising water to the flood clock, so players see WHY it fills up
                Map.SetStatus(C.mission1.hazardDistrict, "FLOOD IN  " + Fx.Clock(Mathf.Max(0f, floodLeft)), floodLeft < 60f ? Pal.Bad : Pal.EnterNormal);
            }
        }

        /// <summary>Mistakes and hints cost flood time in Mission 1.</summary>
        public void FloodPenalty(float seconds)
        {
            if (!floodActive) return;
            floodLeft -= seconds;
            Popup.Show(Hud.Position + Vector3.up * 0.3f, "-" + Mathf.RoundToInt(seconds) + " s", Pal.Bad, 70f);
        }

        /// <summary>Loads Resources/SparkCityArt/welcome (png/jpg, up to 8K).
        /// FlatScreen: giant glowing screen in the welcome space. Panorama360: skybox around the player.</summary>
        void BuildWelcomeArt()
        {
            var tex = Resources.Load<Texture2D>("SparkCityArt/welcome");
            if (tex == null) { Debug.Log("[SparkCity] No welcome artwork found (Resources/SparkCityArt/welcome) - using the default backdrop."); return; }
            Debug.Log("[SparkCity] Welcome artwork: " + tex.width + " x " + tex.height + ", mode " + welcomeArtMode);
            if (tex.width > 4096) Debug.Log("[SparkCity] Large artwork: check frame rate and memory on the Quest 3S.");

            if (welcomeArtMode == WelcomeArtMode.Panorama360)
            {
                var baseMat = Resources.Load<Material>("SparkCityMaterials/SC_Sky");
                Material sky = baseMat != null ? new Material(baseMat) : null;
                if (sky == null)
                {
                    var sh = Shader.Find("Skybox/Panoramic");
                    if (sh != null) sky = new Material(sh);
                }
                if (sky == null) { Debug.LogWarning("[SparkCity] Panoramic skybox shader missing - run 'Spark City VR > Build Game Scene' again."); return; }
                if (sky.HasProperty("_MainTex")) sky.SetTexture("_MainTex", tex);
                RenderSettings.skybox = sky;
                if (Camera.main != null) Camera.main.clearFlags = CameraClearFlags.Skybox;
                return;
            }

            if (welcomeArtMode == WelcomeArtMode.CurvedScreen)
            {
                welcomeArt = BuildCurvedScreen(tex);
                var t = GameObject.Find("WelcomeTitle");
                if (t != null) t.SetActive(false);
                return;
            }

            float width = Mathf.Max(1f, flatScreenWidth);
            float height = width * tex.height / Mathf.Max(1f, tex.width);
            var a = Layout.Welcome;
            var pos = a.At(0f, Mathf.Max(2.2f, height * 0.5f + 0.3f), 6.5f);
            var c = UIKit.Canvas("WelcomeArt", world, new Vector2(width * 100f, height * 100f), 0.01f, false);
            c.transform.SetPositionAndRotation(pos, a.Facing(pos));
            var frame = UIKit.Image(c.transform, Pal.NeonPink, "Frame");
            UIKit.Stretch(frame.rectTransform, -6, -6, -6, -6);
            Sprites.Apply(frame, Sprites.Outline, 0.4f);
            var rt = UIKit.Rect("Image", c.transform);
            UIKit.Stretch(rt);
            var img = rt.gameObject.AddComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;
            welcomeArt = c.gameObject;
            var title = GameObject.Find("WelcomeTitle");
            if (title != null) title.SetActive(false); // the artwork replaces the default title sign
        }

        /// <summary>Bends a normal picture into a cylinder section around the player, keeping its proportions.</summary>
        GameObject BuildCurvedScreen(Texture2D tex)
        {
            var a = Layout.Welcome;
            float h = Mathf.Max(1f, curvedHeight);
            float r = Mathf.Max(2f, curvedRadius);
            float w = h * tex.width / Mathf.Max(1f, tex.height);       // real width of the picture
            float arc = Mathf.Min(w / r, Mathf.PI * 1.4f);              // radians, at most ~250 degrees
            const int seg = 96;
            const float bottom = 0.15f;

            var verts = new Vector3[(seg + 1) * 2];
            var uvs = new Vector2[verts.Length];
            var tris = new int[seg * 6];
            for (int i = 0; i <= seg; i++)
            {
                float k = (float)i / seg;
                float ang = -arc / 2f + arc * k;
                var dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                verts[i * 2] = dir * r + Vector3.up * bottom;
                verts[i * 2 + 1] = dir * r + Vector3.up * (bottom + h);
                uvs[i * 2] = new Vector2(k, 0f);
                uvs[i * 2 + 1] = new Vector2(k, 1f);
            }
            for (int i = 0; i < seg; i++)
            {
                int b0 = i * 2, t0 = i * 2 + 1, b1 = i * 2 + 2, t1 = i * 2 + 3;
                int o = i * 6;
                // clockwise as seen from the player in the middle
                tris[o] = b0; tris[o + 1] = t0; tris[o + 2] = t1;
                tris[o + 3] = b0; tris[o + 4] = t1; tris[o + 5] = b1;
            }
            var mesh = new Mesh { name = "CurvedWelcomeScreen" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("WelcomeArtCurved");
            go.transform.SetParent(world, false);
            go.transform.SetPositionAndRotation(a.pos, a.Rot);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var baseMat = Resources.Load<Material>("SparkCityMaterials/SC_Unlit");
            var mat = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture"));
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f); // visible from both sides, just in case
            mr.sharedMaterial = mat;

            // thin glowing neon lines along the top and bottom edges
            for (int e = 0; e < 2; e++)
            {
                float y = e == 0 ? bottom - 0.03f : bottom + h + 0.03f;
                for (int i = 0; i < 24; i++)
                {
                    float ang = -arc / 2f + arc * (i + 0.5f) / 24f;
                    var pos = a.pos + a.Rot * (new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * (r - 0.02f)) + Vector3.up * y;
                    var seg2 = Fx.Prim(PrimitiveType.Cube, "Edge", go.transform, Vector3.zero, new Vector3(arc * r / 24f + 0.01f, 0.03f, 0.03f), Mat.Glow(Pal.NeonPink, 1.6f));
                    seg2.transform.SetPositionAndRotation(pos, a.Rot * Quaternion.Euler(0f, ang * Mathf.Rad2Deg, 0f));
                }
            }
            Debug.Log("[SparkCity] Curved welcome screen: " + w.ToString("0.0") + " m wide, wraps " + (arc * Mathf.Rad2Deg).ToString("0") + " degrees.");
            return go;
        }

        void EnsureEventSystem()
        {
            var es = FindFirstObjectByType<EventSystem>();
            if (es == null) es = new GameObject("EventSystem").AddComponent<EventSystem>();
            if (es.GetComponent<XRUIInputModule>() == null)
            {
                foreach (var mod in es.GetComponents<BaseInputModule>()) mod.enabled = false;
                es.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        // ================================================================== panels
        Vector3 lastAnchorPos = new Vector3(9999f, 0f, 0f);

        /// <summary>Moves the player to a station. Long moves use a comfort blink (black, move, fade in).</summary>
        void MovePlayer(Anchor a, string label = "")
        {
            bool far = (a.pos - lastAnchorPos).magnitude > 0.5f || Mathf.Abs(Mathf.DeltaAngle(a.yaw, lastAnchorYaw)) > 20f;
            if (far && lastAnchorPos.x < 9000f) ScreenFader.Blink(label);
            lastAnchorPos = a.pos;
            lastAnchorYaw = a.yaw;
            PlayerRig.MoveTo(a);
        }
        float lastAnchorYaw;

        void GoTo(Anchor a, Vector3 offset, Panel p, float face = 1f, string label = "")
        {
            MovePlayer(a, label);
            var pos = a.At(offset.x, offset.y, offset.z);
            if (p != null) p.Place(pos, a.Facing(pos));
            DesktopTestMode.Face(pos, face);
        }

        void OpenMenu(string title)
        {
            welcome.SetVisible(false);
            Hud.Show(false);
            card.SetVisible(false);
            GoTo(Layout.Brief, new Vector3(0f, 1.55f, 1.15f), menu);
            menu.SetVisible(true);
            menu.Clear();
            menu.Resize(new Vector2(1300, 980)); // full size for briefings and summaries
            menu.style.glowAlpha = 0.2f;
            menu.title.text = title;
            menu.corner.text = "";
            menu.accentBar.color = Pal.Spark;
        }

        Button BoardButton(RectTransform row, string label, Action onClick, Color normal, Color hover, Color text, float size = 24)
        {
            var b = menu.AddRowButton(row, label, onClick, normal, size);
            UIKit.SetButtonColors(b, normal, hover, text);
            return b;
        }

        static void AddFlex(Panel p)
        {
            var flex = UIKit.Rect("Flex", p.body);
            flex.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        }

        /// <summary>Shows a card and waits for its button.</summary>
        IEnumerator ShowCard(string title, string body, string button, Color accent, Vector3 worldPos, Anchor facing)
        {
            // show the panel BEFORE filling it, so the layout (text wrapping) is computed correctly
            card.Place(worldPos, facing.Facing(worldPos));
            card.SetVisible(true);
            card.Clear();
            card.title.text = title;
            card.corner.text = "";
            card.accentBar.color = accent;
            card.AddText(body, 38);
            AddFlex(card);
            cardClicked = false;
            var row = card.AddRow(80, 14);
            card.AddRowButton(row, "Mission board", () => { Sfx.Click(); ShowBoard(); }, Pal.Btn, 28);
            var go = card.AddRowButton(row, button, () => { cardClicked = true; Sfx.Click(); }, Pal.Spark, 34);
            var le = go.GetComponent<LayoutElement>();
            if (le != null) le.flexibleWidth = 2.5f; // the main button is wider
            card.Rebuild();
            yield return null;
            card.Rebuild();
            yield return new WaitUntil(() => cardClicked);
            card.SetVisible(false);
        }

        Vector3 CardPosFor(MiniGame g)
        {
            // close to the player (0.9 m), in FRONT of every mini-game object, at eye height
            return g.A.At(0f, 1.6f, 0.9f); // always straight ahead (centred), whatever side the HUD is on
        }

        void ShowMore()
        {
            if (current == null || current.text == null) return;
            Sfx.Click();
            var p = CardPosFor(current);
            card.Place(p, current.A.Facing(p));
            card.SetVisible(true);
            card.Clear();
            card.title.text = "Learn more: " + current.text.title;
            card.corner.text = "";
            card.accentBar.color = Pal.Cyan;
            card.AddText(current.text.more, 34);
            AddFlex(card);
            card.AddButton("Close", () => card.SetVisible(false), Pal.Btn, 72, 32);
            card.Rebuild();
            SessionLog.Event("more", current.id);
        }

        /// <summary>Called by MiniGame.Run when a mini-game starts.</summary>
        public void EnterGame(MiniGame g)
        {
            current = g;
            card.SetVisible(false);
            menu.SetVisible(false);
            GoTo(g.A, g.HudOffset, Hud.panel, g.FaceAmount, g.text != null ? "Next:  " + g.text.title : "");
            Hud.Show(true);
            RefreshPoints();
        }

        public void RefreshPoints()
        {
            int total = finished.Sum(f => f.points) + (current != null && !finished.Contains(current) ? current.points : 0);
            Hud.SetPoints(total);               // points stay in the corner
            Hud.SetTimer(floodActive, floodLeft); // the flood timer has its own big badge on top of the HUD
        }

        /// <summary>Short "how to play" card before each mini-game.</summary>
        public IEnumerator ShowHowTo(MiniGame g)
        {
            if (g.text == null || string.IsNullOrEmpty(g.text.howTo)) yield break;
            Hud.Show(false);
            string howTo = g.text.howTo;
            if (g.bonusMax > 0 || g is CleanupGame) howTo += "\n" + Pal.Col("Faster zaps = bonus points!", Pal.EnterNormal);
            yield return ShowCard("HOW TO PLAY:  " + g.text.title.ToUpper(), howTo, "Got it - let's play!", Pal.Cyan, CardPosFor(g), g.A);
            Hud.Show(true);
            SessionLog.Event("howto_seen", g.id);
        }

        public IEnumerator ShowRemember(MiniGame g)
        {
            // clear the stage so nothing shows through the card
            Hud.Show(false);
            Cue.Hide();
            g.gameObject.SetActive(false);

            // ---- dramatic result card: verdict banner, stars that pop in one by one, points counting up
            var pos = CardPosFor(g);
            card.Place(pos, g.A.Facing(pos));
            card.SetVisible(true);
            card.Clear();
            card.title.text = "REMEMBER";
            card.corner.text = "";
            card.accentBar.color = Pal.Good;
            card.AddText("<b>" + g.text.remember + "</b>", 38, Pal.TextLight, TextAlignmentOptions.Center);
            card.AddSpace(8);

            // verdict: correctness first, so "all right but slow" never looks like a mistake
            bool flawless = g.wrong == 0 && g.hints == 0;
            string verdict = flawless ? "FLAWLESS!" : g.wrong == 0 ? "NO MISTAKES" : g.wrong + (g.wrong == 1 ? " MISTAKE" : " MISTAKES");
            Color vcol = g.wrong == 0 ? Pal.Good : Pal.EnterNormal;
            var banner = card.AddText(verdict, 56, vcol, TextAlignmentOptions.Center);
            banner.fontStyle = FontStyles.Bold;
            banner.characterSpacing = 10f;
            Fonts.UseHeading(banner);
            banner.transform.localScale = Vector3.zero;

            // three star slots (dim), filled one by one
            var starRow = card.AddRow(96, 22);
            starRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var starImgs = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var sr = UIKit.Rect("Star" + i, starRow);
                var le = sr.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 96; le.preferredHeight = 96; le.flexibleWidth = 0;
                var back = sr.gameObject.AddComponent<Image>();
                back.sprite = Sprites.Star; back.preserveAspect = true; back.raycastTarget = false;
                back.color = new Color(1f, 1f, 1f, 0.12f);
                var fillImg = UIKit.Image(sr, Pal.EnterNormal, "Fill");
                UIKit.Stretch(fillImg.rectTransform);
                fillImg.sprite = Sprites.Star; fillImg.preserveAspect = true;
                fillImg.transform.localScale = Vector3.zero;
                starImgs[i] = fillImg;
            }

            var pts = card.AddText("0 / " + g.maxPoints + " POINTS", 44, Pal.TextLight, TextAlignmentOptions.Center);
            pts.fontStyle = FontStyles.Bold;
            Fonts.UseHeading(pts);
            // speed bonus: a separate amber line that pops in after the points (EXTRA, on top of the score)
            TextMeshProUGUI bonusText = null;
            if (g.bonusMax > 0)
            {
                bonusText = card.AddText("+" + g.bonus + " SPEED BONUS   " + Pal.Col("<size=70%>total " + g.points + "</size>", Pal.TextLight), 40, Pal.EnterNormal, TextAlignmentOptions.Center);
                bonusText.fontStyle = FontStyles.Bold;
                Fonts.UseHeading(bonusText);
                bonusText.transform.localScale = Vector3.zero;
            }
            if (g.hints > 0) card.AddText(g.hints + (g.hints == 1 ? " hint used" : " hints used"), 28, Pal.Dim, TextAlignmentOptions.Center);

            AddFlex(card);
            cardClicked = false;
            var row = card.AddRow(80, 14);
            card.AddRowButton(row, "Mission board", () => { Sfx.Click(); ShowBoard(); }, Pal.Btn, 28);
            var cont = card.AddRowButton(row, "Continue  >", () => { cardClicked = true; Sfx.Click(); }, Pal.Spark, 34);
            var cle = cont.GetComponent<LayoutElement>();
            if (cle != null) cle.flexibleWidth = 2.5f;
            card.Rebuild();
            yield return null;
            card.Rebuild();

            // animation (any time the player can already press Continue)
            StartCoroutine(RevealResult(banner, starImgs, g.Stars, pts, g.BasePoints, g.maxPoints, bonusText));
            yield return new WaitUntil(() => cardClicked);
            card.SetVisible(false);
        }

        IEnumerator RevealResult(TextMeshProUGUI banner, Image[] stars, int count, TextMeshProUGUI pts, int points, int max, TextMeshProUGUI bonusText)
        {
            // banner slams in
            yield return PopIn(banner.transform, 0.25f, 1.25f);
            Sfx.Win();
            yield return new WaitForSecondsRealtime(0.25f);
            // stars pop in one by one with rising notes
            for (int i = 0; i < count && i < stars.Length; i++)
            {
                if (stars[i] == null) yield break;
                Sfx.Star(i);
                yield return PopIn(stars[i].transform, 0.22f, 1.35f);
                yield return new WaitForSecondsRealtime(0.12f);
            }
            // points count up
            float t = 0f, dur = 0.9f;
            while (t < dur && pts != null)
            {
                t += Time.unscaledDeltaTime;
                int v = Mathf.RoundToInt(Mathf.Lerp(0f, points, Mathf.SmoothStep(0f, 1f, t / dur)));
                pts.text = v + " / " + max + " POINTS";
                yield return null;
            }
            if (pts != null) pts.text = points + " / " + max + " POINTS";
            if (bonusText != null)
            {
                yield return new WaitForSecondsRealtime(0.2f);
                Sfx.Zap();
                yield return PopIn(bonusText.transform, 0.25f, 1.3f);
            }
        }

        /// <summary>Scales from 0 past 'overshoot' and settles at 1 (a punchy pop).</summary>
        static IEnumerator PopIn(Transform tr, float dur, float overshoot)
        {
            float t = 0f;
            while (t < dur && tr != null)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float s = k < 0.6f ? Mathf.Lerp(0f, overshoot, k / 0.6f) : Mathf.Lerp(overshoot, 1f, (k - 0.6f) / 0.4f);
                tr.localScale = Vector3.one * s;
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
        }

        // ================================================================== welcome space
        void OpenWelcome(string title)
        {
            AbortMission();
            menu.SetVisible(false);
            card.SetVisible(false);
            Hud.Show(false);
            // panel straight ahead, centred in the view
            GoTo(Layout.Welcome, new Vector3(0f, 1.45f, 1.5f), welcome, 1f, "");
            welcome.SetVisible(true);
            welcome.Clear();
            welcome.title.text = title;
            welcome.title.color = Pal.TextLight;
            welcome.corner.text = "";
            welcome.corner.color = Pal.WelcomeTitle;
            welcome.accentBar.color = Pal.WelcomeBorder;
        }

        /// <summary>Adds a welcome-panel button with exact normal/hover colors.</summary>
        Button WelcomeButton(string label, System.Action onClick, Color normal, Color hover, Color text, float height, float size)
        {
            var b = welcome.AddButton(label, onClick, normal, height, size);
            UIKit.SetButtonColors(b, normal, hover, text);
            return b;
        }

        Button WelcomeRowButton(RectTransform row, string label, System.Action onClick, Color normal, Color hover, Color text)
        {
            var b = welcome.AddRowButton(row, label, onClick, normal, 30);
            UIKit.SetButtonColors(b, normal, hover, text);
            return b;
        }

        /// <summary>Welcome: the player can only Enter, take the Tutorial, or Exit.</summary>
        void ShowWelcome()
        {
            OpenWelcome("WELCOME TO");
            // the big game title now lives INSIDE the panel
            var big = welcome.AddText("SPARK CITY", 100, Pal.WelcomeTitle, TextAlignmentOptions.Center);
            big.fontStyle = FontStyles.Bold;
            big.characterSpacing = 12f;
            Fonts.UseHeading(big);
            welcome.AddText("Save the city with Apache Spark.", 36, Pal.WelcomeBorder, TextAlignmentOptions.Center);
            welcome.AddSpace(14);
            WelcomeButton("ENTER THE GAME", EnterCommandCenter, Pal.EnterNormal, Pal.EnterHover, Pal.TextOnAmber, 110, 46);
            WelcomeButton("TUTORIAL", () => ShowTutorial(0), Pal.TutorialNormal, Pal.TutorialHover, Pal.TextLight, 96, 40);
            WelcomeButton("EXIT", QuitGame, Pal.ExitNormal, Pal.ExitHover, Pal.TextLight, 84, 34);
            AddFlex(welcome);
            welcome.AddText("Point + TRIGGER to choose", 24, new Color(Pal.TextLight.r, Pal.TextLight.g, Pal.TextLight.b, 0.6f), TextAlignmentOptions.Center);
            welcome.Rebuild();
        }

        /// <summary>Tutorial cards, shown in the welcome space. Players cannot reach the command center from here
        /// except by finishing or skipping.</summary>
        void ShowTutorial(int i)
        {
            var cards = GameData.Tutorial.cards;
            if (cards == null || cards.Length == 0) { EnterCommandCenter(); return; }
            i = Mathf.Clamp(i, 0, cards.Length - 1);
            var c = cards[i];
            OpenWelcome(c.title.ToUpper());
            welcome.accentBar.color = i % 2 == 0 ? Pal.WelcomeBorder : Pal.WelcomeTitle; // cyan / amber
            welcome.corner.text = (i + 1) + " / " + cards.Length;
            welcome.AddSpace(10);
            welcome.AddText(c.body, 40, Pal.TextLight, TextAlignmentOptions.Center);
            welcome.AddSpace(10);
            if (!string.IsNullOrEmpty(c.picture)) welcome.AddText(Pal.Code(c.picture), 34, Pal.WelcomeBorder, TextAlignmentOptions.Center);
            AddFlex(welcome);
            var row = welcome.AddRow(84, 14);
            WelcomeRowButton(row, "<  BACK", () => { if (i == 0) ShowWelcome(); else ShowTutorial(i - 1); }, Pal.ExitNormal, Pal.ExitHover, Pal.TextLight);
            if (i < cards.Length - 1) WelcomeRowButton(row, "NEXT  >", () => ShowTutorial(i + 1), Pal.EnterNormal, Pal.EnterHover, Pal.TextOnAmber);
            else WelcomeRowButton(row, "ENTER THE GAME  >", EnterCommandCenter, Pal.EnterNormal, Pal.EnterHover, Pal.TextOnAmber);
            WelcomeButton("Skip tutorial - enter the game", EnterCommandCenter, Pal.TutorialNormal, Pal.TutorialHover, Pal.TextLight, 64, 26);
            welcome.Rebuild();
            SessionLog.Event("tutorial_card", c.id);
        }

        void EnterCommandCenter()
        {
            welcome.SetVisible(false);
            ShowBoard("Command Center");
        }

        // ================================================================== command center: mission board
        /// <summary>The mission board. The flood countdown only starts when a mission's Start button is pressed.</summary>
        void ShowBoard() { ShowBoard(""); }

        void ShowBoard(string fadeLabel)
        {
            AbortMission();
            welcome.SetVisible(false);
            Hud.Show(false);
            card.SetVisible(false);
            GoTo(Layout.Brief, new Vector3(0f, 1.55f, 1.15f), menu, 1f, fadeLabel);
            menu.SetVisible(true);
            menu.Clear();
            menu.Resize(new Vector2(1300, 620)); // shorter than the briefing panel
            menu.style.glowAlpha = 0f;           // plain navy header
            menu.title.text = "MISSION BOARD";
            menu.title.color = Pal.TextLight;
            menu.corner.text = "";               // Player ID is shown once, in the footer
            menu.accentBar.color = Pal.WelcomeBorder; // thin cyan border + header line
            menu.AddText("Choose a mission.", 32, Pal.WelcomeBorder, TextAlignmentOptions.Center);
            menu.AddSpace(6);
            int b1 = PlayerPrefs.GetInt("sc_best_m1", -1), b2 = PlayerPrefs.GetInt("sc_best_m2", -1);
            // second line = the mission's purpose (larger), with the best score if there is one
            var m1 = menu.AddButton("MISSION 1  -  FLOOD WARNING\n<size=72%>Clean flood data. Send a reliable warning." + (b1 >= 0 ? "   |   best " + b1 + "%" : "") + "</size>",
                () => StartMission(1), Pal.EnterNormal, 140, 40);
            UIKit.SetButtonColors(m1, Pal.EnterNormal, Pal.EnterHover, Pal.TextOnAmber);
            var m2 = menu.AddButton("MISSION 2  -  RUSH HOUR STREAM\n<size=72%>Handle late events. Recover the stream." + (b2 >= 0 ? "   |   best " + b2 + "%" : "") + "</size>",
                () => StartMission(2), Pal.TutorialNormal, 140, 40);
            UIKit.SetButtonColors(m2, Pal.TutorialNormal, Pal.TutorialHover, Pal.TextLight);
            AddFlex(menu);
            // (Player ID row removed - per-student logs are not needed)
            var r2 = menu.AddRow(58, 10);
            BoardButton(r2, "Seated mode: " + (PlayerRig.SeatedFriendly ? "ON" : "OFF"), () =>
            {
                PlayerRig.SeatedFriendly = !PlayerRig.SeatedFriendly;
                ShowBoard();
            }, Pal.NavyBtn, Pal.NavyBtnHover, Pal.TextLight);
            BoardButton(r2, "Recenter view", () => ShowBoard(), Pal.NavyBtn, Pal.NavyBtnHover, Pal.TextLight);
            BoardButton(r2, "Back to Welcome", ShowWelcome, Pal.ExitNormal, Pal.ExitHover, Pal.TextLight);
            menu.Rebuild();
        }

        void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ================================================================== missions
        void StartMission(int n)
        {
            AbortMission();
            missionNumber = n;
            missionRoutine = StartCoroutine(n == 2 ? Mission2() : Mission1());
        }

        void AbortMission()
        {
            if (missionRoutine != null) StopCoroutine(missionRoutine);
            missionRoutine = null;
            if (current != null) current.Cleanup();
            current = null;
            foreach (var f in finished) if (f != null) f.Cleanup();
            finished.Clear();
            Cue.Hide();
            if (Hud != null) Hud.Show(false);
            if (card != null) card.SetVisible(false);
            if (Map != null) Map.ResetVisuals();
            floodActive = false;
            FloodHold = false;
            MapOverride = false;
            GameClock.Paused = false;
        }

        IEnumerator Mission1()
        {
            var m = C.mission1;
            BeginMissionLog("mission1");
            Progress = new MissionProgress();
            Map.Setup(m.districts, Fx.Hex(m.hazardColor, Pal.Cyan));
            Map.SetHazard(m.hazardDistrict, 0.25f);

            // briefing at the city map
            welcome.SetVisible(false);
            menu.SetVisible(false);
            Hud.Show(false);
            MovePlayer(Layout.Brief, "Mission 1");
            var bp = Layout.Brief.At(0f, 1.55f, 1.15f); // straight ahead, centred (same spot as the Mission Board)
            DesktopTestMode.Face(bp, 1f);
            yield return ShowCard(m.title, m.story + "\n\n" + Pal.Col("5 quick mini-games. A yellow arrow shows where to look. Press HINT if you are stuck.", Pal.Cyan),
                "Start  >", Pal.Spark, bp, Layout.Brief);
            Map.ResetVisuals();
            floodTotal = floodLeft = Mathf.Max(1, m.floodMinutes) * 60f;
            floodWarned = false;
            floodActive = true;

            var sorter = MiniGame.Create<DataSorterGame>(this, "sorter", m.sorter, Layout.Sources);
            sorter.def = m.sorter;
            yield return Play(sorter);

            var clean = MiniGame.Create<CleanupGame>(this, "cleanup", m.cleanup, Layout.Clean);
            clean.def = m.cleanup;
            yield return Play(clean);

            var pipe = MiniGame.Create<LazyPipelineGame>(this, "pipeline", m.pipeline, Layout.Pipeline);
            pipe.def = m.pipeline;
            yield return Play(pipe);

            var cluster = MiniGame.Create<BalanceClusterGame>(this, "cluster", m.cluster, Layout.Cluster);
            cluster.def = m.cluster;
            yield return Play(cluster);

            var trust = MiniGame.Create<TrustCheckGame>(this, "trust", m.trust, Layout.Brief);
            trust.def = m.trust;
            trust.mission = m;
            yield return Play(trust);
            floodActive = false;

            ShowSummary(m.title);
        }

        IEnumerator Mission2()
        {
            var m = C.mission2;
            BeginMissionLog("mission2");
            Progress = new MissionProgress();
            floodActive = false;
            welcome.SetVisible(false);
            menu.SetVisible(false);
            Hud.Show(false);
            MovePlayer(Layout.Cluster, "Mission 2");
            var bp = Layout.Cluster.At(0f, 1.6f, 1.3f);
            DesktopTestMode.Face(bp, 1f);
            yield return ShowCard(m.title, m.story + "\n\n" + Pal.Col("2 quick mini-games about streaming.", Pal.Cyan), "Start  >", Pal.Spark, bp, Layout.Cluster);

            var win = MiniGame.Create<WindowCatcherGame>(this, "window", m.window, Layout.Cluster);
            win.def = m.window;
            yield return Play(win);

            var crash = MiniGame.Create<CrashRecoveryGame>(this, "crash", m.crash, Layout.Cluster);
            crash.def = m.crash;
            yield return Play(crash);

            ShowSummary(m.title);
        }

        IEnumerator Play(MiniGame g)
        {
            yield return g.Run();
            finished.Add(g);
            current = null;
            g.gameObject.SetActive(false); // keep the scores, hide the props
            Cue.Hide();
        }

        void BeginMissionLog(string id)
        {
            SessionLog.BeginEvents();
            SessionLog.Event("mission_start", id);
            missionStart = Time.time;
            foreach (var f in finished) if (f != null) f.Cleanup();
            finished.Clear();
        }

        void ShowSummary(string title)
        {
            int points = finished.Sum(f => f.BasePoints); // percentages ignore the speed bonus (max 100%)
            int max = finished.Sum(f => f.maxPoints);
            float seconds = Time.time - missionStart;
            int pct = max > 0 ? Mathf.RoundToInt(100f * points / max) : 0;
            WriteMissionRecord(points, max, seconds);
            int best = PlayerPrefs.GetInt("sc_best_m" + missionNumber, -1);
            if (pct > best) { PlayerPrefs.SetInt("sc_best_m" + missionNumber, pct); PlayerPrefs.Save(); }

            // copy what we need, then clean up the finished games
            var lines = finished.Select(f => Pal.Col(MiniGame.StarText(f.Stars), Pal.Warn) + "  " + f.text.title + "<pos=60%>" + f.BasePoints + " / " + f.maxPoints + (f.bonus > 0 ? Pal.Col("  +" + f.bonus, Pal.EnterNormal) : "")).ToList();
            var remembers = finished.Select(f => f.text.remember).ToList();
            foreach (var f in finished) if (f != null) f.Cleanup();
            finished.Clear();
            missionRoutine = null;

            string outcome = Progress != null ? Progress.cityOutcome : "";
            bool safe = string.IsNullOrEmpty(outcome) || outcome.StartsWith("SAFE");
            bool flooded = outcome.StartsWith("FLOODED");
            string head = safe ? "COMPLETE" : flooded ? "CITY FLOODED" : "PARTLY FLOODED";
            Color accent = safe ? Pal.Good : flooded ? Pal.Bad : Pal.Warn;
            OpenMenu(title + "  -  " + head);
            menu.accentBar.color = accent;
            menu.corner.text = points + " pts";
            if (!string.IsNullOrEmpty(outcome))
                menu.AddText("<b>City:</b> " + outcome + (safe ? "" : "   Play again to save the city!"), 30, accent);
            menu.AddText(string.Join("\n", lines), 30);
            menu.AddText("<b>REMEMBER</b>", 30, Pal.Good);
            foreach (var r in remembers) menu.AddText("- " + r, 24, Pal.Text);
            AddFlex(menu);
            var row = menu.AddRow(78);
            int n = missionNumber;
            menu.AddRowButton(row, "Play again", () => StartMission(n), Pal.Btn, 30);
            if (n == 1) menu.AddRowButton(row, "Mission 2  >", () => StartMission(2), Pal.Spark, 30);
            menu.AddRowButton(row, "Mission board", ShowBoard, Pal.Btn, 30);
            Map.ResetVisuals();
            if (safe) Sfx.Win(); else Sfx.Bad();
        }

        void WriteMissionRecord(int points, int max, float seconds)
        {
            var rec = new MissionRecord
            {
                playerId = SessionLog.PlayerId,
                sessionId = SessionLog.SessionId,
                device = SessionLog.Device(),
                startedAt = SessionLog.Now(),
                missionId = "mission" + missionNumber,
                cityOutcome = Progress != null ? Progress.cityOutcome : "",
                floodSecondsLeft = missionNumber == 1 ? Mathf.Round(floodLeft) : 0f,
                totalSeconds = Mathf.Round(seconds),
                points = points,
                maxPoints = max,
                percent = max > 0 ? Mathf.RoundToInt(100f * points / max) : 0,
                games = finished.Select(f => new GameResult
                {
                    id = f.id,
                    points = f.points,
                    maxPoints = f.maxPoints,
                    correct = f.correct,
                    wrong = f.wrong,
                    hints = f.hints,
                    seconds = Mathf.Round(f.seconds),
                    stars = f.Stars
                }).ToList(),
                events = SessionLog.TakeEvents()
            };
            SessionLog.Write(rec);
        }


    }
}