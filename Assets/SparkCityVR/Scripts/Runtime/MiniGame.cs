// Base class for all mini-games + the small heads-up panel (HUD) that floats above each game.
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparkCity
{
    /// <summary>The compact instruction panel above the current mini-game:
    /// title + points, ONE instruction line, a feedback line, and Hint / ? / Menu buttons.</summary>
    public class Hud
    {
        public Panel panel;
        TextMeshProUGUI situation, instruction, feedback, points;
        Button hintButton, moreButton, menuButton;
        public Action onHint, onMore, onMenu;

        public static Hud Create()
        {
            var h = new Hud();
            h.panel = Panel.Create("HUD", null, new Vector2(1300, 450), true, 104);
            h.panel.title.fontSizeMax = 40;
            // calm look: opaque navy, thin cyan frame, no colored header glow
            h.panel.bg.color = Pal.Rgb("#091322", 1f);
            h.panel.style.glowAlpha = 0f;
            h.panel.accentBar.color = Pal.WelcomeBorder;
            h.panel.title.color = Pal.TextLight;
            // header layout: points (left)  |  title (middle)  |  flood timer (right) - all INSIDE the panel,
            // so nothing sticks out above it and covers the room behind
            h.titleRect = h.panel.title.rectTransform;
            h.panel.title.textWrappingMode = TextWrappingModes.NoWrap; // title always on one line
            h.CenterTitle(false);
            h.panel.corner.gameObject.SetActive(false); // points live in the bottom row now
            // 1) what to do (big, bold)   2) feedback   3) compact row: points + utility buttons
            h.situation = Line(h.panel, 28, Pal.Cyan, 40);
            h.situation.gameObject.SetActive(false);    // the short "what is happening" line is not shown (less clutter)
            h.instruction = Line(h.panel, 54, Pal.TextLight, 122);
            h.instruction.fontStyle = FontStyles.Bold;
            h.feedback = Line(h.panel, 34, Pal.Dim, 78);
            var row = h.panel.AddRow(48, 12);
            h.points = h.panel.AddRowText(row, "0 pts", 28, 220, Pal.EnterNormal, TextAlignmentOptions.MidlineLeft);
            h.points.fontStyle = FontStyles.Bold;
            h.hintButton = h.panel.AddRowButton(row, "HINT  -20", () => h.onHint?.Invoke(), Pal.NavyBtn, 22);
            h.moreButton = h.panel.AddRowButton(row, "?  LEARN MORE", () => h.onMore?.Invoke(), Pal.NavyBtn, 22);
            h.menuButton = h.panel.AddRowButton(row, "MISSION BOARD", () => h.onMenu?.Invoke(), Pal.NavyBtn, 22);
            UIKit.SetButtonColors(h.hintButton, Pal.NavyBtn, Pal.NavyBtnHover, Pal.EnterNormal);
            UIKit.SetButtonColors(h.moreButton, Pal.NavyBtn, Pal.NavyBtnHover, Pal.TextLight);
            UIKit.SetButtonColors(h.menuButton, Pal.NavyBtn, Pal.NavyBtnHover, Pal.TextLight);
            h.panel.corner.fontSizeMax = 34;

            // big flood-timer badge in the top-right corner of the HUD header
            var badge = UIKit.Rect("TimerBadge", h.panel.root);
            badge.anchorMin = badge.anchorMax = new Vector2(1f, 1f);
            badge.pivot = new Vector2(1f, 1f);
            badge.sizeDelta = new Vector2(470, 86);
            badge.anchoredPosition = new Vector2(-18f, -10f);
            var bg = UIKit.Image(badge, Pal.Rgb("#091322", 0.95f), "Bg");
            UIKit.Stretch(bg.rectTransform);
            Sprites.Apply(bg, Sprites.Rounded, 1.4f);
            h.timerBorder = UIKit.Image(badge, Pal.WelcomeBorder, "Border");
            UIKit.Stretch(h.timerBorder.rectTransform);
            Sprites.Apply(h.timerBorder, Sprites.Outline, 1.4f);
            h.timerText = UIKit.Text(badge, "", 60, Pal.TextLight, TextAlignmentOptions.Center, "Timer");
            UIKit.Stretch(h.timerText.rectTransform, 12, 12, 4, 4);
            h.timerText.fontStyle = FontStyles.Bold;
            h.timerText.characterSpacing = 4f;
            h.timerText.enableAutoSizing = true;
            h.timerText.fontSizeMin = 24;
            h.timerText.fontSizeMax = 60;
            Fonts.UseHeading(h.timerText);
            h.timerBadge = badge.gameObject;
            h.timerBadge.SetActive(false);

            h.panel.SetVisible(false);
            return h;
        }

        GameObject timerBadge;
        RectTransform titleRect;

        /// <summary>Title truly centred; when the flood timer is showing, it keeps clear of the timer on the right.</summary>
        void CenterTitle(bool timerVisible)
        {
            if (titleRect == null) return;
            float right = timerVisible ? 500f : 60f;
            float left = timerVisible ? 60f : 60f;
            titleRect.offsetMin = new Vector2(left, -104); titleRect.offsetMax = new Vector2(-right, -6);
        }
        Image timerBorder;
        TextMeshProUGUI timerText;

        /// <summary>Shows the flood countdown in the big badge (hidden when there is no flood).</summary>
        public void SetTimer(bool visible, float secondsLeft = 0f)
        {
            if (timerBadge == null) return;
            if (timerBadge.activeSelf != visible) { timerBadge.SetActive(visible); CenterTitle(visible); }
            if (!visible) return;
            Color c = secondsLeft < 60f ? Pal.Bad : Pal.EnterNormal; // amber normally, red near the deadline
            timerText.text = Pal.Col("FLOOD  \u2014  " + Fx.Clock(Mathf.Max(0f, secondsLeft)), c);
            float pulse = secondsLeft < 60f ? 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f) : 1f; // pulse only when urgent
            timerBorder.color = new Color(c.r, c.g, c.b, pulse);
        }

        /// <summary>A centered text line with a fixed height; long text shrinks instead of overflowing.</summary>
        static TextMeshProUGUI Line(Panel p, float size, Color color, float height)
        {
            var t = p.AddText("", size, color, TextAlignmentOptions.Center);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            t.enableAutoSizing = true;
            t.fontSizeMin = 18;
            t.fontSizeMax = size;
            return t;
        }

        public void Place(Anchor a, float right, float up, float forward)
        {
            panel.PlaceAt(a, right, up, forward);
        }

        public void Step(string title, string text)
        {
            panel.SetVisible(true);
            panel.title.text = title;
            instruction.textWrappingMode = TextWrappingModes.Normal;
            instruction.text = text;
            feedback.text = "";
            panel.Rebuild();
        }

        public void SetSituation(string text) { situation.text = text ?? ""; }
        public void Instruct(string text) { Instruct(text, false); }
        /// <summary>oneLine = never wrap (the text shrinks a little instead).</summary>
        public void Instruct(string text, bool oneLine)
        {
            instruction.textWrappingMode = oneLine ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            instruction.text = text;
        }

        public void Say(string text, Color color)
        {
            feedback.text = text;
            feedback.color = color;
            if (color == Pal.Bad && panel.style != null) panel.style.Flash(); // short red flash on mistakes
        }

        public void SetUrgent(bool urgent) { if (panel.style != null) panel.style.urgent = urgent; }
        public void SetPoints(int total) { if (points != null) points.text = total + " pts"; }
        /// <summary>(kept for the mini-games; the HUD frame now always stays cyan for a calm look)</summary>
        public void SetAccent(Color c) { }
        public void Show(bool visible) { panel.SetVisible(visible); }
        public Vector3 Position { get { return panel.canvas.transform.position; } }
    }

    /// <summary>One interactive mini-game. Subclasses implement Play() as a coroutine.</summary>
    public abstract class MiniGame : MonoBehaviour
    {
        public SparkCityGame G;
        public Anchor A;
        public StepText text;
        public string id;
        public int points, maxPoints, correct, wrong, hints;
        /// <summary>Speed bonus earned / possible (only games with a speed bonus set bonusMax).</summary>
        public int bonus, bonusMax;
        public float seconds;
        protected string hint = "";
        protected Vector3? hintTarget;
        protected Transform root;

        public static T Create<T>(SparkCityGame game, string gameId, StepText t, Anchor a) where T : MiniGame
        {
            var go = new GameObject("MiniGame_" + gameId);
            var g = go.AddComponent<T>();
            g.G = game;
            g.id = gameId;
            g.text = t;
            g.A = a;
            g.root = go.transform;
            return g;
        }

        /// <summary>Where the HUD floats relative to the player's anchor (right, up, forward).</summary>
        public virtual Vector3 HudOffset { get { return new Vector3(0f, 2.05f, 1.35f); } }
        /// <summary>How much the desktop test view turns toward the HUD (0..1).</summary>
        public virtual float FaceAmount { get { return 1f; } }

        public IEnumerator Run()
        {
            float t0 = Time.time;
            SessionLog.Event("game_start", id);
            G.EnterGame(this);
            yield return G.ShowHowTo(this);
            G.Hud.SetSituation(text != null ? text.situation : "");
            yield return Play();
            seconds = Time.time - t0;
            Cue.Hide();
            SessionLog.Event("game_end", id + " points=" + points + "/" + maxPoints);
            yield return G.ShowRemember(this);
        }

        protected abstract IEnumerator Play();

        // ------------------------------------------------------------ feedback helpers
        protected void Good(int pts, string message, Vector3 at)
        {
            points += pts;
            correct++;
            Sfx.Good();
            G.Hud.Say(message, Pal.Good);
            if (pts > 0) Popup.Show(at, "+" + pts, Pal.Good);
            G.RefreshPoints();
            SessionLog.Event(id + "_good", message);
        }

        protected void Bad(string message, Vector3 at)
        {
            wrong++;
            Sfx.Bad();
            G.Hud.Say(message, Pal.Bad);
            Popup.Show(at, "X", Pal.Bad, 110f);
            G.FloodPenalty(10f);
            SessionLog.Event(id + "_bad", message);
        }

        protected void SetHint(string h, Vector3? target)
        {
            hint = h;
            hintTarget = target;
        }

        public void UseHint()
        {
            if (string.IsNullOrEmpty(hint)) return;
            hints++;
            points = Mathf.Max(0, points - 20);
            G.FloodPenalty(5f);
            G.RefreshPoints();
            G.Hud.Say("Hint: " + hint, Pal.Warn);
            if (hintTarget.HasValue) Cue.Show(hintTarget.Value);
            SessionLog.Event(id + "_hint", hint);
        }

        /// <summary>Points without the speed bonus (what the stars and "x / max" are based on).</summary>
        public int BasePoints { get { return points - bonus; } }

        public int Stars
        {
            get
            {
                float r = maxPoints > 0 ? (float)BasePoints / maxPoints : 1f;
                int byScore = r >= 0.85f ? 3 : r >= 0.55f ? 2 : 1;
                // many mistakes cap the stars, even if the points are high: 3+ mistakes = max 2 stars, 6+ = 1 star
                int byMistakes = wrong >= 6 ? 1 : wrong >= 3 ? 2 : 3;
                return Mathf.Min(byScore, byMistakes);
            }
        }

        public static string StarText(int stars)
        {
            return stars + (stars == 1 ? " STAR " : " STARS");
        }

        // ------------------------------------------------------------ building helpers
        protected Vector3 At(float right, float up, float forward) { return A.At(right, up, forward); }
        protected Quaternion Face(Vector3 p) { return A.Facing(p); }

        protected GameObject Box(string name, Vector3 worldPos, Vector3 size, Material mat)
        {
            var go = Fx.Prim(PrimitiveType.Cube, name, root, Vector3.zero, size, mat);
            go.transform.SetPositionAndRotation(worldPos, A.Rot);
            return go;
        }

        protected Button Btn(Vector3 pos, Vector2 size, string label, Color color, Action onClick, float font = 56f)
        {
            return UIKit.WorldButton(root, pos, Face(pos), size, label, color, onClick, font);
        }

        protected TextMeshProUGUI Label(Vector3 pos, Vector2 size, string text, float font, Color color, bool bg = false)
        {
            return UIKit.WorldLabel(root, pos, Face(pos), size, text, font, color, bg);
        }

        protected Button Hit(Vector3 pos, Vector2 size, Action onClick)
        {
            return UIKit.HitBox(root, pos, Face(pos), size, onClick);
        }

        public void Cleanup()
        {
            Cue.Hide();
            if (this != null) Destroy(gameObject);
        }
    }
}