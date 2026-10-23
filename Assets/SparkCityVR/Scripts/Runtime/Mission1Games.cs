// Mission 1 (batch processing): five short, hands-on mini-games.
// Every interaction is "point + trigger" (in the Editor: mouse aim + left click).
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparkCity
{
    // ===================================================================== 1. DATA SORTER
    /// <summary>Sources slide in on a conveyor; the player decides KEEP or TOSS using the 4 V's.</summary>
    public class DataSorterGame : MiniGame
    {
        public SorterDef def;

        public override Vector3 HudOffset { get { return new Vector3(0f, 2.17f, 1.4f); } }

        protected override IEnumerator Play()
        {
            G.Hud.Step("1/5   " + def.title.ToUpper(), def.instruction);
            G.Hud.SetAccent(Pal.Cyan);

            // props
            Box("Belt", At(0f, 0.93f, 1.1f), new Vector3(3.2f, 0.06f, 0.42f), Mat.Lit(new Color(0.12f, 0.14f, 0.2f)));
            Box("BeltEdge", At(0f, 0.97f, 0.89f), new Vector3(3.2f, 0.015f, 0.015f), Mat.Glow(Pal.Cyan, 1.2f));
            Vector3 bayPos = At(1.25f, 0.98f, 1.0f);
            Vector3 binPos = At(-1.25f, 0.72f, 1.0f);
            // KEEP bay: dark pad with a glowing green rim, an inner light and arrows flowing into Spark
            Box("KeepBay", bayPos, new Vector3(0.55f, 0.06f, 0.5f), Mat.Lit(new Color(0.05f, 0.09f, 0.15f), 0.6f));
            Box("KeepBayInner", bayPos + Vector3.up * 0.032f, new Vector3(0.43f, 0.004f, 0.38f), Mat.Glow(Pal.Good, 0.5f));
            Box("RimFront", bayPos + Vector3.up * 0.035f - A.Fwd * 0.25f, new Vector3(0.56f, 0.02f, 0.02f), Mat.Glow(Pal.Good, 1.8f));
            Box("RimBack", bayPos + Vector3.up * 0.035f + A.Fwd * 0.25f, new Vector3(0.56f, 0.02f, 0.02f), Mat.Glow(Pal.Good, 1.8f));
            Box("RimLeft", bayPos + Vector3.up * 0.035f - A.Right * 0.275f, new Vector3(0.02f, 0.02f, 0.5f), Mat.Glow(Pal.Good, 1.8f));
            Box("RimRight", bayPos + Vector3.up * 0.035f + A.Right * 0.275f, new Vector3(0.02f, 0.02f, 0.5f), Mat.Glow(Pal.Good, 1.8f));
            var sign = Label(bayPos + Vector3.up * 0.62f, new Vector2(0.9f, 0.15f),
                             Pal.Col("INGEST", Pal.Good) + "  " + Pal.Col(">>", Pal.TextLight) + "  " + Pal.Col("SPARK", Pal.EnterNormal), 58, Pal.TextLight, true);
            sign.textWrappingMode = TextWrappingModes.NoWrap; // always one line
            sign.fontStyle = FontStyles.Bold;
            sign.characterSpacing = 6f;
            Fonts.UseHeading(sign);
            StartCoroutine(FlowArrows(bayPos));
            Box("TossBin", binPos, new Vector3(0.45f, 0.45f, 0.45f), Mat.Lit(new Color(0.35f, 0.1f, 0.1f)));
            var tossSign = Label(binPos + Vector3.up * 0.45f, new Vector2(0.4f, 0.15f), "TOSS", 58, Pal.Bad, true);
            tossSign.fontStyle = FontStyles.Bold;
            tossSign.characterSpacing = 6f;
            Fonts.UseHeading(tossSign);

            string choice = null;
            // KEEP / TOSS: about 30% smaller than before, directly under the information card (both fully visible)
            Vector3 tossPos = At(-0.3f, 1.27f, 1.17f);
            Vector3 keepPos = At(0.3f, 1.27f, 1.17f);
            Btn(tossPos, new Vector2(0.32f, 0.17f), "<  TOSS", new Color(0.55f, 0.12f, 0.12f), () => choice = "toss", 58);
            Btn(keepPos, new Vector2(0.32f, 0.17f), "KEEP  >", new Color(0.1f, 0.5f, 0.22f), () => choice = "keep", 58);
            var card = new SourceCard(root, At(0f, 1.63f, 1.18f), Face(At(0f, 1.63f, 1.18f)));

            maxPoints = def.sources.Length * 100;
            int keptCount = 0;
            var ingested = new List<string>();
            for (int i = 0; i < def.sources.Length; i++)
            {
                var s = def.sources[i];
                G.Hud.Say("", Pal.Dim); // clear the previous source's feedback, so it is never read as being about this one
                card.Waiting("Incoming source " + (i + 1) + " of " + def.sources.Length + " ...");
                Color sc = Fx.Hex(s.color, Pal.Cyan);
                var crate = BuildCartridge(s.name, sc);
                crate.SetPositionAndRotation(At(-1.8f, 1.12f, 1.1f), A.Rot);

                Sfx.Whoosh();
                yield return Fx.MoveTo(crate, At(0f, 1.12f, 1.1f), 0.6f);
                yield return Scan(crate, sc);   // short scanning sweep, then the card shows the result

                card.Show(s, i + 1, def.sources.Length);

                SetHint((s.keep ? "KEEP it. " : "TOSS it. ") + s.reason, s.keep ? keepPos : tossPos);
                if (i == 0) Cue.Show(crate.position + Vector3.up * 0.22f, 0.02f);
                G.Hud.Instruct(def.instruction.Replace("{name}", s.name));
                choice = null;
                yield return new WaitUntil(() => choice != null);
                Cue.Hide();
                Sfx.Click();

                bool kept = choice == "keep";
                bool ok = kept == s.keep;
                Vector3 pop = crate.position + Vector3.up * 0.35f;
                if (ok) Good(100, (s.keep ? "Kept!  " : "Tossed!  ") + s.reason, pop);
                else if (kept)
                {
                    Bad("Kept - but the city does NOT need it. " + s.reason + " It is now in YOUR pipeline...", pop);
                    if (s.veracity < 50) G.Progress.keptUnreliable.Add(s.name); else G.Progress.keptUnneeded.Add(s.name);
                }
                else
                {
                    Bad("Tossed - but the city NEEDS it! " + s.reason + " Your pipeline will miss this data...", pop);
                    G.Progress.tossedNeeded.Add(s.name);
                }

                // the cartridge goes where the PLAYER sent it - choices have consequences later
                Vector3 dest = kept ? bayPos + Vector3.up * (0.12f + 0.06f * keptCount) + A.Right * (0.05f * (keptCount % 2)) : binPos + Vector3.up * 0.25f;
                yield return Fx.MoveTo(crate, dest, 0.6f, 0.35f);
                if (kept)
                {
                    keptCount++;
                    crate.localScale = Vector3.one * 0.55f;
                    ingested.Add(s.name);
                }
                else crate.gameObject.SetActive(false);
                yield return GameClock.Wait(ok ? 1.3f : 3.0f);
            }
            card.Summary("<b>Ingested into Spark</b>\n" + (ingested.Count > 0 ? string.Join("\n", ingested) : "nothing!"));
            yield return GameClock.Wait(1f);
        }

        // ------------------------------------------------------------ data cartridge
        /// <summary>A glowing data cartridge: dark casing, colored glowing face and edges, and a simple icon for the source.</summary>
        Transform BuildCartridge(string name, Color c)
        {
            var t = Fx.Group("Cartridge_" + name, root).transform;
            Material casing = Mat.Lit(new Color(0.06f, 0.09f, 0.15f), 0.7f);
            Material glow = Mat.Glow(c, 1.6f);
            Material face = Mat.Glow(c, 0.55f);
            Material icon = Mat.Glow(Color.Lerp(c, Color.white, 0.75f), 2.2f);
            Fx.Prim(PrimitiveType.Cube, "Casing", t, Vector3.zero, new Vector3(0.30f, 0.20f, 0.12f), casing);
            Fx.Prim(PrimitiveType.Cube, "Face", t, new Vector3(0f, 0f, -0.062f), new Vector3(0.25f, 0.15f, 0.004f), face);
            // glowing edges (top, bottom) and a connector notch, like a cartridge
            Fx.Prim(PrimitiveType.Cube, "EdgeTop", t, new Vector3(0f, 0.101f, 0f), new Vector3(0.302f, 0.008f, 0.122f), glow);
            Fx.Prim(PrimitiveType.Cube, "EdgeBottom", t, new Vector3(0f, -0.101f, 0f), new Vector3(0.302f, 0.008f, 0.122f), glow);
            Fx.Prim(PrimitiveType.Cube, "Connector", t, new Vector3(0f, -0.115f, 0f), new Vector3(0.14f, 0.025f, 0.08f), casing);
            for (int k = 0; k < 4; k++)
                Fx.Prim(PrimitiveType.Cube, "Pin", t, new Vector3(-0.045f + 0.03f * k, -0.124f, -0.041f), new Vector3(0.012f, 0.012f, 0.004f), glow);
            var ic = Fx.Group("Icon", t).transform;
            ic.localPosition = new Vector3(0f, 0f, -0.066f);
            BuildIcon(ic, name.ToLowerInvariant(), icon);
            return t;
        }

        /// <summary>Simple icons from primitives, picked from the source name.</summary>
        static void BuildIcon(Transform p, string n, Material m)
        {
            const float d = 0.004f; // icon depth
            if (n.Contains("river") || n.Contains("gauge") || n.Contains("water"))
            {
                // water drop: a circle with a small diamond on top
                Fx.Prim(PrimitiveType.Sphere, "Drop", p, new Vector3(0f, -0.012f, 0f), new Vector3(0.06f, 0.06f, d), m);
                var tip = Fx.Prim(PrimitiveType.Cube, "Tip", p, new Vector3(0f, 0.022f, 0f), new Vector3(0.03f, 0.03f, d), m);
                tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            else if (n.Contains("social") || n.Contains("feed"))
            {
                // speech bubble
                Fx.Prim(PrimitiveType.Cube, "Bubble", p, new Vector3(0f, 0.008f, 0f), new Vector3(0.10f, 0.06f, d), m);
                var tail = Fx.Prim(PrimitiveType.Cube, "Tail", p, new Vector3(-0.025f, -0.026f, 0f), new Vector3(0.022f, 0.022f, d), m);
                tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            else if (n.Contains("weather"))
            {
                // cloud: three circles on a flat base
                Fx.Prim(PrimitiveType.Sphere, "C1", p, new Vector3(-0.03f, -0.005f, 0f), new Vector3(0.045f, 0.045f, d), m);
                Fx.Prim(PrimitiveType.Sphere, "C2", p, new Vector3(0.0f, 0.012f, 0f), new Vector3(0.06f, 0.06f, d), m);
                Fx.Prim(PrimitiveType.Sphere, "C3", p, new Vector3(0.032f, -0.005f, 0f), new Vector3(0.045f, 0.045f, d), m);
                Fx.Prim(PrimitiveType.Cube, "Base", p, new Vector3(0f, -0.018f, 0f), new Vector3(0.1f, 0.022f, d), m);
            }
            else if (n.Contains("power") || n.Contains("grid") || n.Contains("log"))
            {
                // lightning bolt: two slanted bars
                var a = Fx.Prim(PrimitiveType.Cube, "B1", p, new Vector3(0.008f, 0.02f, 0f), new Vector3(0.018f, 0.06f, d), m);
                a.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
                var b = Fx.Prim(PrimitiveType.Cube, "B2", p, new Vector3(-0.008f, -0.02f, 0f), new Vector3(0.018f, 0.06f, d), m);
                b.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
                Fx.Prim(PrimitiveType.Cube, "Mid", p, Vector3.zero, new Vector3(0.04f, 0.012f, d), m);
            }
            else if (n.Contains("emergency") || n.Contains("call"))
            {
                // medical cross
                Fx.Prim(PrimitiveType.Cube, "V", p, Vector3.zero, new Vector3(0.025f, 0.08f, d), m);
                Fx.Prim(PrimitiveType.Cube, "H", p, Vector3.zero, new Vector3(0.08f, 0.025f, d), m);
            }
            else if (n.Contains("table") || n.Contains("location"))
            {
                // table grid: 3 x 2 cells
                for (int x = 0; x < 3; x++)
                    for (int y = 0; y < 2; y++)
                        Fx.Prim(PrimitiveType.Cube, "Cell", p, new Vector3(-0.034f + 0.034f * x, 0.016f - 0.032f * y, 0f), new Vector3(0.028f, 0.026f, d), m);
            }
            else
            {
                Fx.Prim(PrimitiveType.Sphere, "Dot", p, Vector3.zero, new Vector3(0.06f, 0.06f, d), m);
            }
        }

        /// <summary>A thin glowing beam sweeps down and up over the cartridge (about 0.8 s).</summary>
        IEnumerator Scan(Transform crate, Color c)
        {
            var beam = Fx.Prim(PrimitiveType.Cube, "ScanBeam", root, Vector3.zero, new Vector3(0.36f, 0.006f, 0.17f), Mat.Glow(Color.Lerp(c, Color.white, 0.5f), 2.5f));
            const float dur = 0.8f;
            float t = 0f;
            while (t < dur)
            {
                t += GameClock.Dt;
                float k = Mathf.PingPong(t / dur * 2f, 1f);  // top -> bottom -> top
                beam.transform.SetPositionAndRotation(crate.position + Vector3.up * Mathf.Lerp(0.12f, -0.12f, k), A.Rot);
                yield return null;
            }
            Destroy(beam);
            Sfx.Click();
        }

        // ------------------------------------------------------------ information card
        /// <summary>The source's 4 V's in aligned rows with dividers and a real reliability bar.</summary>
        class SourceCard
        {
            readonly TextMeshProUGUI title, waiting, summary;
            readonly GameObject rows;
            readonly TextMeshProUGUI[] values = new TextMeshProUGUI[3];
            readonly RectTransform barFill;
            readonly Image barFillImg;
            readonly TextMeshProUGUI pct;
            const float W = 1000f, H = 520f, Pad = 56f, HeadH = 96f, RowH = 92f, ValueX = 400f;

            public SourceCard(Transform parent, Vector3 pos, Quaternion rot)
            {
                var c = UIKit.Canvas("SourceCard", parent, new Vector2(W, H), 0.001f, false);
                c.transform.SetPositionAndRotation(pos, rot);
                var r = (RectTransform)c.transform;
                var bg = UIKit.Image(r, Pal.Rgb("#091322", 0.95f), "Bg");
                UIKit.Stretch(bg.rectTransform);
                Sprites.Apply(bg, Sprites.Rounded, 1.4f);
                var ol = UIKit.Image(r, new Color(Pal.WelcomeBorder.r, Pal.WelcomeBorder.g, Pal.WelcomeBorder.b, 0.8f), "Border");
                UIKit.Stretch(ol.rectTransform);
                Sprites.Apply(ol, Sprites.Outline, 1.4f);

                title = UIKit.Text(r, "", 46, Pal.TextLight, TextAlignmentOptions.Center, "Title");
                Place(title.rectTransform, Pad, 14, W - 2 * Pad, HeadH - 14);
                title.fontStyle = FontStyles.Bold;
                title.enableAutoSizing = true; title.fontSizeMin = 26; title.fontSizeMax = 46;
                var line = UIKit.Image(r, new Color(Pal.WelcomeBorder.r, Pal.WelcomeBorder.g, Pal.WelcomeBorder.b, 0.6f), "HeadLine");
                Place(line.rectTransform, Pad, HeadH, W - 2 * Pad, 3);

                rows = UIKit.Rect("Rows", r).gameObject;
                UIKit.Stretch((RectTransform)rows.transform);
                string[] labels = { "Volume", "Velocity", "Variety", "Veracity" };
                for (int i = 0; i < 4; i++)
                {
                    float y = HeadH + 14 + i * RowH;
                    var lab = UIKit.Text(rows.transform, labels[i], 38, Pal.Dim, TextAlignmentOptions.MidlineLeft, "Label");
                    Place(lab.rectTransform, Pad + 20, y, ValueX - Pad - 20, RowH);
                    if (i < 3)
                    {
                        values[i] = UIKit.Text(rows.transform, "", 40, Pal.TextLight, TextAlignmentOptions.MidlineLeft, "Value");
                        Place(values[i].rectTransform, ValueX, y, W - ValueX - Pad, RowH);
                        values[i].enableAutoSizing = true; values[i].fontSizeMin = 24; values[i].fontSizeMax = 40;
                        var div = UIKit.Image(rows.transform, new Color(1f, 1f, 1f, 0.08f), "Divider");
                        Place(div.rectTransform, Pad, y + RowH - 1, W - 2 * Pad, 2);
                    }
                    else
                    {
                        // reliability bar: dark track + colored fill + percentage
                        float barW = 360f, barH = 30f;
                        var track = UIKit.Image(rows.transform, new Color(1f, 1f, 1f, 0.1f), "BarTrack");
                        Place(track.rectTransform, ValueX, y + (RowH - barH) / 2f, barW, barH);
                        Sprites.Apply(track, Sprites.Rounded, 0.5f);
                        barFillImg = UIKit.Image(track.rectTransform, Pal.Bad, "BarFill");
                        barFill = barFillImg.rectTransform;
                        barFill.anchorMin = new Vector2(0f, 0f); barFill.anchorMax = new Vector2(0.3f, 1f);
                        barFill.offsetMin = Vector2.zero; barFill.offsetMax = Vector2.zero;
                        Sprites.Apply(barFillImg, Sprites.Rounded, 0.5f);
                        pct = UIKit.Text(rows.transform, "", 42, Pal.Bad, TextAlignmentOptions.MidlineLeft, "Pct");
                        Place(pct.rectTransform, ValueX + barW + 24, y, W - ValueX - barW - 24 - Pad, RowH);
                        pct.fontStyle = FontStyles.Bold;
                    }
                }

                waiting = UIKit.Text(r, "", 38, Pal.Dim, TextAlignmentOptions.Center, "Waiting");
                Place(waiting.rectTransform, Pad, HeadH + 14, W - 2 * Pad, H - HeadH - 40);
                summary = UIKit.Text(r, "", 40, Pal.TextLight, TextAlignmentOptions.Center, "Summary");
                Place(summary.rectTransform, Pad, 30, W - 2 * Pad, H - 60);
                summary.enableAutoSizing = true; summary.fontSizeMin = 24; summary.fontSizeMax = 40;
            }

            /// <summary>Positions a rect from the top-left corner of the card (pixels).</summary>
            static void Place(RectTransform rt, float x, float y, float w, float h)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(x, -y);
                rt.sizeDelta = new Vector2(w, h);
            }

            public void Waiting(string text)
            {
                title.gameObject.SetActive(true);
                title.text = "SCANNING";
                rows.SetActive(false);
                summary.text = "";
                waiting.text = text;
            }

            public void Show(SourceItem s, int index, int count)
            {
                waiting.text = "";
                summary.text = "";
                rows.SetActive(true);
                title.gameObject.SetActive(true);
                title.text = s.name + "   " + Pal.Col("<size=70%>" + index + " of " + count + "</size>", Pal.Dim);
                values[0].text = s.volume;
                values[1].text = s.velocity;
                values[2].text = s.variety;
                Color vc = s.veracity >= 80 ? Pal.Good : s.veracity >= 60 ? Pal.Warn : Pal.Bad;
                barFill.anchorMax = new Vector2(Mathf.Clamp01(s.veracity / 100f), 1f);
                barFillImg.color = vc;
                pct.text = s.veracity + "%";
                pct.color = vc;
            }

            public void Summary(string text)
            {
                rows.SetActive(false);
                waiting.text = "";
                title.gameObject.SetActive(false);
                summary.text = text;
            }
        }

        /// <summary>Small glowing arrows that keep sliding up from the KEEP bay toward the INGEST sign.</summary>
        IEnumerator FlowArrows(Vector3 bayPos)
        {
            const int n = 3;
            var arrows = new Transform[n];
            for (int k = 0; k < n; k++)
            {
                var g = Fx.Group("FlowArrow", root).transform;
                // a "^" chevron made of two thin bars
                var l = Fx.Prim(PrimitiveType.Cube, "L", g, new Vector3(-0.035f, 0f, 0f), new Vector3(0.09f, 0.014f, 0.014f), Mat.Glow(Pal.Good, 2f));
                l.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
                var r = Fx.Prim(PrimitiveType.Cube, "R", g, new Vector3(0.035f, 0f, 0f), new Vector3(0.09f, 0.014f, 0.014f), Mat.Glow(Pal.Good, 2f));
                r.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
                arrows[k] = g;
            }
            float t = 0f;
            while (root != null)
            {
                t += GameClock.Dt;
                for (int k = 0; k < n; k++)
                {
                    if (arrows[k] == null) yield break;
                    float p = Mathf.Repeat(t * 0.45f + k / (float)n, 1f);       // 0 -> 1, then starts again
                    var pos = bayPos + A.Right * 0.36f + Vector3.up * (0.12f + 0.38f * p); // just right of the bay
                    arrows[k].SetPositionAndRotation(pos, A.Rot);
                    arrows[k].localScale = Vector3.one * Mathf.Sin(p * Mathf.PI); // fades in and out by size
                }
                yield return null;
            }
        }
    }

    // ===================================================================== 2. CLEAN-UP BLASTER
    /// <summary>Dirty records fly toward the player; zap each with the matching cleaning tool.</summary>
    public class CleanupGame : MiniGame
    {
        public CleanupDef def;

        public override Vector3 HudOffset { get { return new Vector3(0f, 2.1f, 1.55f); } }

        protected override IEnumerator Play()
        {
            G.Hud.Step("2/5   " + def.title.ToUpper(), def.instruction);
            G.Hud.SetAccent(Pal.Warn);

            // records fly in a little higher, so the tool buttons fit underneath at a comfortable height
            Vector3 start = At(0f, 1.5f, 3.2f);
            Vector3 end = At(0f, 1.5f, 1.0f);
            var ring = Fx.Prim(PrimitiveType.Cylinder, "Portal", root, Vector3.zero, new Vector3(1.1f, 0.01f, 1.1f), Mat.Glow(new Color(0.6f, 0.3f, 1f), 1.6f));
            ring.transform.SetPositionAndRotation(start + A.Fwd * 0.05f, A.Rot * Quaternion.Euler(90f, 0f, 0f));
            Label(start + Vector3.up * 0.65f, new Vector2(1.4f, 0.14f), "INCOMING DIRTY DATA", 70, new Color(0.75f, 0.55f, 1f));

            string choice = null;
            var toolPos = new Dictionary<string, Vector3>();
            int n = def.tools.Length;
            for (int i = 0; i < n; i++)
            {
                var t = def.tools[i];
                // tool buttons: raised to waist/chest height, a bit farther away and closer together
                float x = n == 1 ? 0f : -0.66f + 1.32f * i / (n - 1);
                Vector3 p = At(x, 1.12f, 1.05f);
                toolPos[t.id] = p;
                Color c = Fx.Hex(t.color, Pal.Btn) * 0.55f;
                c.a = 1f;
                string id = t.id;
                string sub = string.IsNullOrEmpty(t.hint) ? Pal.Code(t.code) : "(" + t.hint + ")";
                // tilted back 20 degrees so the face points up toward the player's eyes
                UIKit.WorldButton(root, p, Face(p) * Quaternion.Euler(20f, 0f, 0f), new Vector2(0.42f, 0.26f),
                    "<b>" + t.label + "</b>\n<size=62%>" + sub + "</size>", c, () => choice = id, 40);
            }

            maxPoints = def.records.Length * 100;
            bonusMax = def.records.Length * 50;   // 100 per correct record (counts toward 800) + up to 50 EXTRA for speed
            G.Progress.recordsTotal = def.records.Length;

            // ---- practice record: it waits in front of the player, no timer, no score
            if (def.practice != null && !string.IsNullOrEmpty(def.practice.row))
            {
                var pr = def.practice;
                var ptool = def.tools.FirstOrDefault(t => t.id == pr.tool);
                var pcard = Fx.Group("PracticeRecord", root).transform;
                Vector3 pPos = At(0f, 1.5f, 1.6f);
                pcard.SetPositionAndRotation(pPos, A.Rot);
                UIKit.WorldLabel(pcard, pPos, A.Rot, new Vector2(1.25f, 0.4f),
                    "<size=75%><b>PRACTICE  -  " + pr.source.ToUpper() + "</b></size>\n" + pr.row, 48, Pal.Text, true);
                G.Hud.Instruct("PRACTICE (no timer): read the RED part, then press the tool that fixes it.");
                G.FloodHold = true; // the flood countdown waits during practice
                SetHint(pr.clue + "  Use: " + (ptool != null ? ptool.label : pr.tool), toolPos.ContainsKey(pr.tool) ? toolPos[pr.tool] : (Vector3?)null);
                Cue.Show(pPos + Vector3.up * 0.25f, 0.02f);
                float waited = 0f;
                while (true)
                {
                    choice = null;
                    while (choice == null)
                    {
                        if (!G.Paused) waited += GameClock.Dt;
                        // after 10 s without an answer, point at the right tool
                        if (waited > 10f && toolPos.ContainsKey(pr.tool)) Cue.Show(toolPos[pr.tool], 0.18f);
                        yield return null;
                    }
                    if (choice == pr.tool) break;
                    Sfx.Bad();
                    G.Hud.Say("Not that one. " + pr.clue + " Look at each tool's description.", Pal.Warn);
                }
                Cue.Hide();
                Sfx.Zap();
                G.Hud.Say("Yes! " + (ptool != null ? ptool.label + "  " + Pal.Code(ptool.code) : "") + "   Now for real - records will MOVE toward you!", Pal.Good);
                StartCoroutine(Anim.Burst(pcard));
                yield return GameClock.Wait(2.6f);
                if (pcard != null) Destroy(pcard.gameObject);
                G.FloodHold = false;
                G.Hud.Instruct(def.instruction);
            }

            for (int i = 0; i < def.records.Length; i++)
            {
                var r = def.records[i];
                var tool = def.tools.FirstOrDefault(t => t.id == r.tool);
                string toolLabel = tool != null ? tool.label : r.tool;

                var card = Fx.Group("Record", root).transform;
                card.SetPositionAndRotation(start, A.Rot);
                UIKit.WorldLabel(card, start, A.Rot, new Vector2(1.25f, 0.4f),
                    "<size=75%><b>" + r.source.ToUpper() + "</b></size>\n" + r.row, 48, Pal.Text, true);
                Sfx.Whoosh();

                SetHint(r.clue + "  Use: " + toolLabel, toolPos.ContainsKey(r.tool) ? toolPos[r.tool] : (Vector3?)null);
                if (i == 0) G.Hud.Say("Here they come! The RED part of each record is the problem.", Pal.Cyan);
                float duration = Mathf.Max(8f, 12f - i * 0.5f);
                float time = 0f;
                bool done = false, hadWrong = false;
                choice = null;
                while (!done)
                {
                    if (!G.Paused) time += GameClock.Dt; // the record waits while help is open
                    float k = Mathf.Clamp01(time / duration);
                    card.position = Vector3.Lerp(start, end, k);
                    if (choice != null)
                    {
                        if (choice == r.tool)
                        {
                            int speed = hadWrong ? 0 : Mathf.RoundToInt(50f * (1f - k));
                            int pts = hadWrong ? 20 : 100 + speed; // all correct = full 800, speed is a bonus on top
                            bonus += speed;
                            Sfx.Zap();
                            Good(pts, "Zapped!  " + toolLabel + "  " + Pal.Code(tool != null ? tool.code : ""), card.position + Vector3.up * 0.3f);
                            StartCoroutine(Anim.Burst(card));
                            done = true;
                        }
                        else
                        {
                            hadWrong = true;
                            Bad("Wrong tool. " + r.clue, card.position + Vector3.up * 0.3f);
                            choice = null;
                        }
                    }
                    if (!done && k >= 1f)
                    {
                        Bad("Missed! This record needed: " + toolLabel + " - it slipped into your data.", card.position + Vector3.up * 0.3f);
                        G.Progress.recordsMissed++;
                        G.Progress.missedTools.Add(r.tool);
                        StartCoroutine(Anim.Shake(card, 0.05f, 0.4f));
                        done = true;
                    }
                    yield return null;
                }
                Cue.Hide();
                yield return GameClock.Wait(1.0f);
                if (card != null) Destroy(card.gameObject);
            }
        }
    }

    // ===================================================================== 3. LAZY PIPELINE
    /// <summary>Click blocks in the right order; nothing runs until the ACTION block is placed.</summary>
    public class LazyPipelineGame : MiniGame
    {
        public PipelineDef def;

        public override Vector3 HudOffset { get { return new Vector3(0f, 2.22f, 1.55f); } }

        protected override IEnumerator Play()
        {
            G.Hud.Step("3/5   " + def.title.ToUpper(), def.instruction);
            G.Hud.SetAccent(new Color(0.35f, 0.55f, 0.95f));

            int slots = def.order.Length;
            var slotPos = new Vector3[slots];
            var slotPlates = new Renderer[slots];
            float span = 1.36f;
            for (int i = 0; i < slots; i++)
            {
                float x = slots == 1 ? 0f : -span / 2f + span * i / (slots - 1);
                slotPos[i] = At(x, 1.3f, 0.95f);
                slotPlates[i] = Box("Slot" + i, slotPos[i] + A.Fwd * 0.05f, new Vector3(0.28f, 0.14f, 0.01f), Mat.Glow(Pal.Idle, 1f)).GetComponent<Renderer>();
                Label(slotPos[i] + Vector3.up * 0.13f, new Vector2(0.2f, 0.07f), (i + 1).ToString(), 50, Pal.Dim);
                if (i < slots - 1) Label(slotPos[i] + A.Right * (span / (slots - 1) * 0.5f), new Vector2(0.08f, 0.08f), ">", 70, Pal.Spark);
            }

            var dash = Label(At(0f, 1.7f, 1.22f), new Vector2(1.3f, 0.36f), "", 36, Pal.Text, true);
            dash.alignment = TextAlignmentOptions.Center;

            // tray: blocks in a fixed shuffled order (so the answer is not given away)
            var defs = def.blocks.ToList();
            var rnd = new System.Random(42);
            defs = defs.OrderBy(_ => rnd.Next()).ToList();
            var blockT = new Dictionary<string, Transform>();
            var blockHit = new Dictionary<string, GameObject>();
            string clicked = null;
            int perRow = Mathf.CeilToInt(defs.Count / 2f);
            for (int i = 0; i < defs.Count; i++)
            {
                var b = defs[i];
                // two compact rows (about 1 m wide) so all choices are in view together with the slots
                int row = i < perRow ? 0 : 1;
                int idxInRow = row == 0 ? i : i - perRow;
                int inRow = row == 0 ? perRow : defs.Count - perRow;
                // the name is printed ON the block's face (no label hovering over other blocks);
                // rows are spread apart and tilted toward the player's eyes so every block is easy to read
                float rowSpan = 0.37f * (inRow - 1);
                float x = inRow <= 1 ? 0f : -rowSpan / 2f + rowSpan * idxInRow / (inRow - 1);
                Vector3 p = row == 0 ? At(x, 1.0f, 0.74f) : At(x, 1.16f, 0.86f);
                var g = Fx.Group("Block_" + b.id, root).transform;
                g.SetPositionAndRotation(p, A.Rot * Quaternion.Euler(30f, 0f, 0f));
                Color c = b.kind == "action" ? Pal.Spark : new Color(0.3f, 0.5f, 0.95f);
                Fx.Prim(PrimitiveType.Cube, "Body", g, Vector3.zero, new Vector3(0.32f, 0.13f, 0.06f), Mat.Lit(Color.Lerp(c, Color.black, 0.45f), 0.6f));
                Fx.Prim(PrimitiveType.Cube, "Face", g, new Vector3(0f, 0f, -0.031f), new Vector3(0.3f, 0.11f, 0.004f), Mat.Glow(c, 0.7f));
                Fx.Prim(PrimitiveType.Cube, "Top", g, new Vector3(0f, 0.066f, 0f), new Vector3(0.322f, 0.006f, 0.062f), Mat.Glow(c, 1.8f));
                var lab = UIKit.WorldLabel(g, g.TransformPoint(new Vector3(0f, 0f, -0.036f)), g.rotation, new Vector2(0.29f, 0.1f), b.label, 46, Color.white, false);
                lab.fontStyle = FontStyles.Bold;
                lab.enableAutoSizing = true; lab.fontSizeMin = 24; lab.fontSizeMax = 46;
                lab.textWrappingMode = TextWrappingModes.NoWrap;
                Fonts.UseHeading(lab);
                blockT[b.id] = g;
                string id = b.id;
                var hit = Hit(p - A.Fwd * 0.07f, new Vector2(0.34f, 0.17f), () => clicked = id);
                blockHit[b.id] = hit.transform.parent.gameObject;
            }
            Label(At(0f, 0.87f, 0.66f), new Vector2(1.6f, 0.07f),
                Pal.Col("blue = transformation (lazy)", new Color(0.5f, 0.7f, 1f)) + "      " + Pal.Col("orange = action (runs the job)", Pal.Spark), 36, Pal.Text);

            maxPoints = slots * 60 + 60;
            var placed = new List<string>();
            UpdateDash(dash, placed, false);

            for (int step = 0; step < slots; step++)
            {
                string expected = def.order[step];
                var eb = def.Block(expected);
                SetHint("Next comes " + (eb != null ? eb.label : expected) + ". " + (eb != null ? eb.role : ""), blockT.ContainsKey(expected) ? blockT[expected].position : (Vector3?)null);
                if (step == 0) { Cue.Show(blockT[expected].position, 0.22f); G.Hud.Say("Start with the block that LOADS the data.", Pal.Cyan); }
                nextPrompt = def.prompts != null && step < def.prompts.Length ? def.prompts[step] : "";
                if (nextPrompt.Length > 0) G.Hud.Instruct(nextPrompt);
                UpdateDash(dash, placed, false);
                slotPlates[step].sharedMaterial = Mat.Glow(Pal.Warn, 1.6f); // the slot being filled
                bool hadWrong = false;
                while (true)
                {
                    clicked = null;
                    yield return new WaitUntil(() => clicked != null);
                    if (clicked == expected) break;
                    hadWrong = true;
                    G.Progress.pipelineWrongTries++;
                    var wrongB = def.Block(clicked);
                    int wrongIndex = System.Array.IndexOf(def.order, clicked);
                    string why;
                    if (wrongB == null) why = "Not this one.";
                    else if (wrongIndex > step) why = "Not yet! " + (eb != null ? eb.role : "");
                    else why = wrongB.role;
                    Bad(why, blockT[clicked].position + Vector3.up * 0.25f);
                    StartCoroutine(Anim.Shake(blockT[clicked]));
                }
                Cue.Hide();
                Sfx.Click();
                blockHit[expected].SetActive(false);
                yield return Fx.MoveTo(blockT[expected], slotPos[step], 0.4f, 0.15f);
                blockT[expected].rotation = A.Rot; // stand upright in the slot
                slotPlates[step].sharedMaterial = Mat.Glow(Pal.Good, 1.6f);
                placed.Add(expected);

                bool isAction = eb != null && eb.kind == "action";
                if (isAction)
                {
                    Good(hadWrong ? 20 : 60, "ACTION!  " + eb.label + "() made Spark run the whole plan.", slotPos[step] + Vector3.up * 0.3f);
                    Sfx.Win();
                    yield return CountRows(dash, placed);
                }
                else
                {
                    Good(hadWrong ? 20 : 60, "Added to the plan - but NOTHING ran yet. Transformations are lazy.", slotPos[step] + Vector3.up * 0.3f);
                    UpdateDash(dash, placed, false);
                }
            }

            // quick memory check
            yield return GameClock.Wait(1.2f);
            G.Hud.Instruct(def.checkQuestion);
            G.Hud.Say("", Pal.Dim);
            foreach (var go in blockHit.Values) if (go != null) go.SetActive(false);
            foreach (var kv in blockT) if (kv.Value != null && !placed.Contains(kv.Key)) kv.Value.gameObject.SetActive(false); // clear the tray for the question buttons
            int answer = -1;
            // the question appears ON the dashboard (eye level), with its three answers right under it -
            // so it is obvious that the question and the buttons belong together
            dash.alignment = TextAlignmentOptions.Top;
            dash.text = Pal.Col("<b>QUICK CHECK</b>", Pal.EnterNormal) + "\n" + def.checkQuestion;
            var qButtons = new List<Button>();
            for (int i = 0; i < def.checkOptions.Length; i++)
            {
                int idx = i;
                float x = -0.42f + 0.42f * i;
                var qb = UIKit.WorldButton(root, At(x, 1.6f, 1.19f), Face(At(x, 1.6f, 1.19f)), new Vector2(0.36f, 0.12f),
                                           def.checkOptions[i], Pal.NavyBtn, () => answer = idx, 50);
                UIKit.SetButtonColors(qb, Pal.NavyBtn, Pal.TutorialHover, Pal.TextLight);
                // draw the buttons AFTER the see-through dashboard, so none of them look faded behind it
                var qc = qb.GetComponentInParent<Canvas>();
                if (qc != null) qc.sortingOrder = 5;
                qButtons.Add(qb);
            }
            // (no arrow here: pointing at one answer would look like a hint)
            SetHint("The orange block - an ACTION.", null);
            yield return new WaitUntil(() => answer >= 0);
            Cue.Hide();
            // show the result on the buttons: right one green, a wrong pick red
            UIKit.SetButtonColors(qButtons[def.checkAnswer], Pal.Good, Pal.Good, Pal.TextOnAmber);
            if (answer != def.checkAnswer) UIKit.SetButtonColors(qButtons[answer], Pal.Bad, Pal.Bad, Pal.TextLight);
            if (answer == def.checkAnswer) Good(60, "Right! Only an ACTION (" + def.checkOptions[def.checkAnswer] + ") starts the job.", At(0f, 1.95f, 1.19f));
            else Bad("It was " + def.checkOptions[def.checkAnswer] + " - transformations only plan, the ACTION runs.", At(0f, 1.95f, 1.19f));
            yield return GameClock.Wait(2.2f);
        }

        string nextPrompt = "";

        void UpdateDash(TextMeshProUGUI dash, List<string> placed, bool running)
        {
            var sb = new StringBuilder("<b>SPARK PLAN</b>   ");
            sb.Append(placed.Count == 0 ? Pal.Col("(empty)", Pal.Dim) : string.Join(" -> ", placed.Select(id => def.Block(id).label)));
            sb.Append("\nRows processed: <b>0</b>   Status: ");
            sb.Append(running ? Pal.Col("RUNNING", Pal.Good) : Pal.Col("WAITING (lazy)", Pal.Warn));
            if (!string.IsNullOrEmpty(nextPrompt)) sb.Append("\n").Append(Pal.Col("NEXT >  " + nextPrompt, Pal.Cyan));
            dash.text = sb.ToString();
        }

        IEnumerator CountRows(TextMeshProUGUI dash, List<string> placed)
        {
            nextPrompt = "";
            string plan = string.Join(" -> ", placed.Select(id => def.Block(id).label));
            float t = 0f;
            const float dur = 2.2f;
            while (t < dur)
            {
                t += GameClock.Dt;
                long rows = (long)(2400000L * Mathf.SmoothStep(0f, 1f, t / dur));
                dash.text = "<b>SPARK PLAN</b>   " + plan + "\nRows processed: <b>" + rows.ToString("N0") + "</b>\nStatus: " +
                            Pal.Col("JOB 1 RUNNING - 3 stages, 16 tasks", Pal.Good);
                yield return null;
            }
            dash.text = "<b>SPARK PLAN</b>   " + plan + "\nRows processed: <b>2,400,000</b>\nStatus: " + Pal.Col("JOB 1 DONE - triggered by the action", Pal.Good);
        }
    }

    // ===================================================================== 4. BALANCE THE CLUSTER
    /// <summary>Meet driver / executor / partition, watch data skew stall the job, then repartition by hand.</summary>
    public class BalanceClusterGame : MiniGame
    {
        public ClusterDef def;

        public override Vector3 HudOffset { get { return new Vector3(0f, 2.3f, 1.9f); } }

        class Tower
        {
            public int index;
            public Vector3 basePos;
            public readonly List<Transform> parts = new List<Transform>();
            public TextMeshProUGUI label;
            public Renderer beacon;
        }

        readonly List<Tower> towers = new List<Tower>();
        /// <summary>One color per executor (purple, pink, lime, blue) - partitions stay cyan, the driver is the pink-white globe.</summary>
        static readonly Color[] ExecColors = { Pal.Rgb("#8B5CF6"), Pal.Rgb("#F472B6"), Pal.Rgb("#A3E635"), Pal.Rgb("#3B82F6") };
        Transform driverCore;

        protected override IEnumerator Play()
        {
            G.Hud.Step("4/5   " + def.title.ToUpper(), "Meet the cluster.");
            G.Hud.SetAccent(Pal.Good);

            // driver
            // (narrow podium, so it does not hide the partitions on Executors 2 and 3 behind it)
            Fx.Prim(PrimitiveType.Cylinder, "Podium", root, Vector3.zero, new Vector3(0.5f, 0.45f, 0.5f), Mat.Lit(new Color(0.15f, 0.12f, 0.2f))).transform.position = At(0f, 0.45f, 3.0f);
            Fx.Prim(PrimitiveType.Cylinder, "PodiumRing", root, Vector3.zero, new Vector3(0.52f, 0.01f, 0.52f), Mat.Glow(new Color(0.95f, 0.45f, 0.75f), 1.8f)).transform.position = At(0f, 0.9f, 3.0f);
            driverCore = Fx.Prim(PrimitiveType.Sphere, "DriverCore", root, Vector3.zero, Vector3.one * 0.42f, Mat.Glow(new Color(0.95f, 0.45f, 0.75f), 1.7f)).transform;
            driverCore.position = At(0f, 1.2f, 3.0f);
            // the word DRIVER written on the globe itself (dark text on the bright core)
            var dl = Label(driverCore.position - A.Fwd * 0.22f, new Vector2(0.4f, 0.1f), "DRIVER", 72, Pal.Rgb("#091322"));
            dl.fontStyle = FontStyles.Bold;
            dl.characterSpacing = 6f;
            dl.textWrappingMode = TextWrappingModes.NoWrap;
            Fonts.UseHeading(dl);

            // executors with partitions
            int[] counts = def.partitions;
            int n = counts.Length;
            for (int i = 0; i < n; i++)
            {
                float x = n == 1 ? 0f : -2.4f + 4.8f * i / (n - 1);
                var t = new Tower { index = i, basePos = At(x, 0f, 4.6f) };
                // each executor has its own color: glowing rim + a number plate on the front
                Color ec = ExecColors[i % ExecColors.Length];
                Box("ExecBase" + i, t.basePos + Vector3.up * 0.1f, new Vector3(0.9f, 0.2f, 0.9f), Mat.Lit(new Color(0.08f, 0.1f, 0.16f), 0.6f));
                Box("RimF" + i, t.basePos + Vector3.up * 0.2f - A.Fwd * 0.45f, new Vector3(0.92f, 0.025f, 0.025f), Mat.Glow(ec, 1.8f));
                Box("RimB" + i, t.basePos + Vector3.up * 0.2f + A.Fwd * 0.45f, new Vector3(0.92f, 0.025f, 0.025f), Mat.Glow(ec, 1.8f));
                Box("RimL" + i, t.basePos + Vector3.up * 0.2f - A.Right * 0.45f, new Vector3(0.025f, 0.025f, 0.92f), Mat.Glow(ec, 1.8f));
                Box("RimR" + i, t.basePos + Vector3.up * 0.2f + A.Right * 0.45f, new Vector3(0.025f, 0.025f, 0.92f), Mat.Glow(ec, 1.8f));
                Box("Plate" + i, t.basePos + Vector3.up * 0.1f - A.Fwd * 0.452f, new Vector3(0.5f, 0.13f, 0.01f), Mat.Glow(ec, 0.8f));
                var num = Label(t.basePos + Vector3.up * 0.1f - A.Fwd * 0.46f, new Vector2(0.5f, 0.13f), "EXEC " + (i + 1), 70, Pal.Rgb("#091322"));
                num.fontStyle = FontStyles.Bold;
                num.textWrappingMode = TextWrappingModes.NoWrap;
                Fonts.UseHeading(num);
                t.beacon = Fx.Prim(PrimitiveType.Sphere, "Beacon", root, Vector3.zero, Vector3.one * 0.18f, Mat.Glow(Pal.Good, 2f)).GetComponent<Renderer>();
                t.beacon.transform.position = t.basePos + Vector3.up * 1.5f;
                t.label = Label(t.basePos + Vector3.up * 1.95f, new Vector2(1.3f, 0.36f), "", 64, Pal.Text, true);
                for (int k = 0; k < counts[i]; k++)
                {
                    var p = Fx.Prim(PrimitiveType.Cube, "Partition", root, Vector3.zero, Vector3.one * 0.22f, Mat.Glow(Pal.Cyan, 0.9f)).transform;
                    p.position = SlotPos(t, k);
                    t.parts.Add(p);
                }
                towers.Add(t);
            }
            var meter = Label(At(0f, 2.6f, 6.4f), new Vector2(3.4f, 0.3f), "", 110, Pal.Text, true);
            RefreshLabels(meter, -1);

            // hit areas
            string clicked = null;
            var bigHits = new List<GameObject>(); // driver + tower areas (switched off while picking a partition)
            bigHits.Add(Hit(driverCore.position - A.Fwd * 0.35f, new Vector2(0.8f, 0.8f), () => clicked = "driver").transform.parent.gameObject);
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                bigHits.Add(Hit(towers[i].basePos + Vector3.up * 1.0f - A.Fwd * 0.55f, new Vector2(1.0f, 2.0f), () => clicked = "tower" + idx).transform.parent.gameObject);
            }
            // partition target: covers ALL cubes on Executor 1, right in front of them (so the ray hits where the cubes are)
            Vector3 partCenter = Vector3.zero;
            foreach (var p in towers[0].parts) partCenter += p.position;
            partCenter /= Mathf.Max(1, towers[0].parts.Count);
            var partHitGo = Hit(partCenter - A.Fwd * 0.2f, new Vector2(0.7f, 0.5f), () => clicked = "partition").transform.parent.gameObject;
            partHitGo.SetActive(false);

            maxPoints = 150 + 6 * 30 + 100;

            // ---- A: identify the parts
            yield return Identify(() => clicked, v => clicked = v, "driver", driverCore.position,
                "Click the DRIVER - the pink core that plans the job.",
                "Driver: the brain. It turns your plan into stages and tasks.",
                "That is an executor. The DRIVER is the pink core in the middle.");
            yield return Identify(() => clicked, v => clicked = v, "tower", towers[0].basePos + Vector3.up * 1.4f,
                "Click any EXECUTOR - a worker tower.",
                "Executor: a worker process. It runs tasks on its partitions.",
                "That is the driver. Executors are the four towers.");
            // only the partition cubes are clickable now, and they light up amber so they are easy to find
            foreach (var go in bigHits) go.SetActive(false);
            foreach (var p in towers[0].parts) Mat.SetGlow(p.GetComponent<Renderer>(), Pal.EnterNormal, 2f);
            partHitGo.SetActive(true);
            yield return Identify(() => clicked, v => clicked = v, "partition", partCenter,
                "Click a PARTITION - one of the glowing cubes on Executor 1 (arrow).",
                "Partition: a chunk of data. One task processes one partition.",
                "Click one of the small glowing cubes under the arrow.");
            partHitGo.SetActive(false);
            foreach (var p in towers[0].parts) Mat.SetGlow(p.GetComponent<Renderer>(), Pal.Cyan, 0.9f);
            foreach (var go in bigHits) go.SetActive(true);

            // ---- B: run the job - data skew
            G.Hud.Instruct("Watch the job run...");
            G.Hud.Say("", Pal.Dim);
            yield return GameClock.Wait(0.6f);
            yield return RunStage(stopAt: 3, meter: meter);
            Sfx.Bad();
            towers[1].beacon.sharedMaterial = Mat.Glow(Pal.Bad, 2.4f);
            G.Hud.Say("Executors 1, 3 and 4 are IDLE - the whole stage waits for Executor 2. This is DATA SKEW.", Pal.Bad);
            RefreshLabels(meter, 1);
            yield return GameClock.Wait(2.8f);
            foreach (var t in towers) foreach (var p in t.parts) Mat.SetGlow(p.GetComponent<Renderer>(), Pal.Cyan, 0.9f);

            // ---- C: repartition by hand
            int target = Mathf.CeilToInt(counts.Sum() / (float)n);
            G.Hud.Instruct(def.instruction);
            G.Hud.Say("Move partitions until every executor has " + target + ".", Pal.Cyan);
            SetHint("Click Executor 2 (red) first, then a tower with fewer than " + target + " partitions.", towers[1].basePos + Vector3.up * 1.6f);
            Cue.Show(towers[1].basePos + Vector3.up * 1.6f, 0.1f);
            int picked = -1;
            Transform lifted = null;
            int wrongSinceMove = 0, wrongMoves = 0;
            while (towers.Max(t => t.parts.Count) - towers.Min(t => t.parts.Count) > 1 || picked >= 0)
            {
                clicked = null;
                yield return new WaitUntil(() => clicked != null);
                if (!clicked.StartsWith("tower")) continue;
                int ti = int.Parse(clicked.Substring(5));
                var tw = towers[ti];
                if (picked < 0)
                {
                    if (tw.parts.Count > target)
                    {
                        picked = ti;
                        lifted = tw.parts[tw.parts.Count - 1];
                        Mat.SetGlow(lifted.GetComponent<Renderer>(), Pal.Warn, 2f);
                        StartCoroutine(Fx.MoveTo(lifted, lifted.position + Vector3.up * 0.4f, 0.2f));
                        Sfx.Click();
                        Cue.Hide();
                        G.Hud.Say("Picked a partition. Now click an IDLE executor.", Pal.Cyan);
                    }
                    else { Bad("Take partitions FROM the overloaded executor (red, Executor 2).", tw.basePos + Vector3.up * 2f); G.Progress.clusterWrongMoves++; wrongSinceMove++; wrongMoves++; }
                }
                else if (ti == picked)
                {
                    // put it back
                    StartCoroutine(Fx.MoveTo(lifted, SlotPos(tw, tw.parts.Count - 1), 0.2f));
                    Mat.SetGlow(lifted.GetComponent<Renderer>(), Pal.Cyan, 0.9f);
                    picked = -1; lifted = null;
                    G.Hud.Say("Put back. Pick again.", Pal.Dim);
                }
                else if (tw.parts.Count < target)
                {
                    var from = towers[picked];
                    from.parts.Remove(lifted);
                    tw.parts.Add(lifted);
                    Mat.SetGlow(lifted.GetComponent<Renderer>(), Pal.Cyan, 0.9f);
                    Sfx.Whoosh();
                    yield return Fx.MoveTo(lifted, SlotPos(tw, tw.parts.Count - 1), 0.5f, 0.8f);
                    picked = -1; lifted = null;
                    RefreshLabels(meter, towers[1].parts.Count > target ? 1 : -1);
                    Good(wrongSinceMove == 0 ? 30 : 10, "Moved!  Stage time is now " + StageSeconds() + " s.", tw.basePos + Vector3.up * 2f);
                    wrongSinceMove = 0;
                }
                else { Bad("That executor already has enough. Choose one with fewer partitions.", tw.basePos + Vector3.up * 2f); G.Progress.clusterWrongMoves++; wrongSinceMove++; wrongMoves++; }
            }

            Cue.Hide();
            towers[1].beacon.sharedMaterial = Mat.Glow(Pal.Good, 2f);
            // balancing bonus: 100, minus 20 for each wrong click while balancing (at least 20)
            Good(Mathf.Max(20, 100 - 20 * wrongMoves), "Balanced! In real Spark, df.repartition() shuffles ROWS into new partitions to spread the work more evenly.", At(0f, 2.2f, 3.2f));
            G.Hud.Instruct("Run the stage again...");
            yield return GameClock.Wait(0.8f);
            yield return RunStage(stopAt: -1, meter: meter);
            Sfx.Win();
            G.Hud.Say("Job finished in " + StageSeconds() + " s instead of " + (counts.Max() * 5) + " s - all executors worked together!", Pal.Good);
            yield return GameClock.Wait(2.5f);
        }

        IEnumerator Identify(System.Func<string> get, System.Action<string> set, string want, Vector3 cuePos, string ask, string good, string wrongMsg)
        {
            G.Hud.Instruct(ask);
            SetHint(ask, cuePos);
            Cue.Show(cuePos, 0.35f);
            int misses = 0;
            while (true)
            {
                set(null);
                yield return new WaitUntil(() => get() != null);
                string c = get();
                bool ok = want == "tower" ? c.StartsWith("tower") : c == want;
                if (ok) break;
                misses++;
                Bad(wrongMsg, cuePos + Vector3.up * 0.3f);
            }
            Cue.Hide();
            // full points only on the first try
            Good(misses == 0 ? 50 : misses == 1 ? 25 : 10, good, cuePos + Vector3.up * 0.3f);
            yield return GameClock.Wait(1.6f);
        }

        /// <summary>Animates tasks finishing (one partition per 0.45 s on every executor in parallel).</summary>
        IEnumerator RunStage(int stopAt, TextMeshProUGUI meter)
        {
            int maxCount = towers.Max(t => t.parts.Count);
            int steps = stopAt >= 0 ? stopAt : maxCount;
            for (int k = 0; k < steps; k++)
            {
                foreach (var t in towers)
                {
                    if (k < t.parts.Count) Mat.SetGlow(t.parts[k].GetComponent<Renderer>(), Pal.Good, 0.7f);
                    if (k + 1 < t.parts.Count && k + 1 < steps) Mat.SetGlow(t.parts[k + 1].GetComponent<Renderer>(), Pal.Warn, 1.4f);
                }
                Sfx.Click();
                yield return GameClock.Wait(0.45f);
            }
            if (stopAt >= 0)
            {
                // the skewed executor is still busy
                var busy = towers.OrderByDescending(t => t.parts.Count).First();
                for (int k = stopAt; k < busy.parts.Count; k++) Mat.SetGlow(busy.parts[k].GetComponent<Renderer>(), k == stopAt ? Pal.Warn : Pal.Bad, 1.3f);
            }
        }

        int StageSeconds() { return towers.Max(t => t.parts.Count) * 5; }

        void RefreshLabels(TextMeshProUGUI meter, int overloaded)
        {
            foreach (var t in towers)
            {
                bool over = t.index == overloaded;
                t.label.text = Pal.Col("<b>EXECUTOR " + (t.index + 1) + "</b>", ExecColors[t.index % ExecColors.Length]) + "\n" + (over ? Pal.Col(t.parts.Count + " partitions  OVERLOADED", Pal.Bad) : t.parts.Count + " partitions");
            }
            int s = StageSeconds();
            Color c = s > 25 ? Pal.Bad : s > 20 ? Pal.Warn : Pal.Good;
            meter.text = "Stage time  " + Pal.Col(Fx.Bar(s / 50f, 20) + "  " + s + " s", c) + "   <size=70%>(the slowest executor decides)</size>";
        }

        Vector3 SlotPos(Tower t, int k)
        {
            int layer = k / 4;
            int idx = k % 4;
            float dx = (idx % 2 - 0.5f) * 0.27f;
            float dz = (idx / 2 - 0.5f) * 0.27f;
            return t.basePos + A.Right * dx + A.Fwd * dz + Vector3.up * (0.33f + layer * 0.27f);
        }
    }

    // ===================================================================== 5. TRUST CHECK
    /// <summary>Two practice reports, then the player's OWN report (built from their choices).
    /// For their own report: approve -> outcome; reject/rerun -> repair problems -> rerun -> judge again.</summary>
    public class TrustCheckGame : MiniGame
    {
        public TrustDef def;
        public Mission1Def mission;

        // layout: instructions (HUD, left) and the report (right) side by side at eye level, centred as a pair;
        // the decision buttons sit in one row underneath, in the middle
        public override Vector3 HudOffset { get { return new Vector3(-0.66f, 1.75f, 1.6f); } }
        public override float FaceAmount { get { return 0f; } } // keep looking straight ahead at the pair
        const float RowY = 1.37f, RowZ = 1.5f;

        TextMeshProUGUI report;
        readonly List<GameObject> decisionButtons = new List<GameObject>();
        string choice;

        class OwnReport
        {
            public ReportItem item;
            public bool approvable;
            public bool critical;
        }

        OwnReport BuildOwnReport()
        {
            var pr = G.Progress;
            var lines = new List<string>();
            int trust = 96;
            bool critical = false;
            if (pr.keptUnreliable.Count + pr.keptUnneeded.Count + pr.tossedNeeded.Count == 0) lines.Add("[OK] right sources");
            foreach (var n in pr.keptUnreliable) { lines.Add("[X] kept " + n + " (unreliable, not needed)"); trust -= 35; critical = true; }
            foreach (var n in pr.keptUnneeded) { lines.Add("[!] kept " + n + " (not needed - slower)"); trust -= 10; }
            foreach (var n in pr.tossedNeeded) { lines.Add("[X] missing " + n + " (the city needs it)"); trust -= 25; critical = true; }
            int critMissed = pr.missedTools.Count(t => t == "corrupt" || t == "type");
            int minorMissed = pr.missedTools.Count - critMissed;
            if (pr.missedTools.Count == 0) lines.Add("[OK] all dirty records cleaned");
            if (critMissed > 0) { lines.Add("[X] " + critMissed + " corrupted / wrong-type record(s) in the data"); trust -= 15 * critMissed; critical = true; }
            if (minorMissed > 0) { lines.Add("[!] " + minorMissed + " duplicate / missing-value record(s) in the data"); trust -= 7 * minorMissed; }
            lines.Add("[OK] plan in order, job finished after repartition");
            trust = Mathf.Clamp(trust, 10, 98);

            // critical problems block approval, whatever the score
            bool approvable = !critical && trust >= 80;
            var item = new ReportItem
            {
                title = "YOUR team (built from your choices)",
                trust = trust,
                lines = lines.ToArray(),
                correct = approvable ? "approve" : "reject",
                result = approvable ? "Says: evacuate " + mission.DistrictName(mission.hazardDistrict) + ", rescue via " + mission.DistrictName(mission.safeFrom) + "."
                                    : "Says: evacuate " + mission.DistrictName(mission.wrongDistrict) + "  (bad data changed the answer!)",
                why = approvable ? "Your data is trustworthy - approve it!"
                                 : (critical ? "A critical problem (red X) means it must NOT be approved. Reject it, repair it, rerun."
                                             : "Trust is below 80. Reject it, repair the problems, then rerun.")
            };
            return new OwnReport { item = item, approvable = approvable, critical = critical };
        }

        void ShowReport(ReportItem r, int number, int total)
        {
            Color tc = r.trust >= 80 ? Pal.Good : r.trust >= 50 ? Pal.Warn : Pal.Bad;
            var sb = new StringBuilder();
            sb.Append("<b>REPORT ").Append(number).Append(" of ").Append(total).Append(":  ").Append(r.title.ToUpper()).Append("</b>\n");
            foreach (var line in r.lines)
            {
                Color c = line.StartsWith("[OK]") ? Pal.Good : line.StartsWith("[!]") ? Pal.Warn : Pal.Bad;
                sb.Append(Pal.Col(line, c)).Append("\n");
            }
            sb.Append("Trust  ").Append(Pal.Col(Fx.Bar(r.trust / 100f, 16) + "  " + r.trust, tc)).Append("\n");
            sb.Append("<i>").Append(r.result).Append("</i>");
            report.text = sb.ToString();
            Sfx.Whoosh();
        }

        void SetDecisionButtons(bool visible)
        {
            foreach (var b in decisionButtons) if (b != null) b.SetActive(visible);
        }

        protected override IEnumerator Play()
        {
            G.Hud.Step("5/5   " + def.title.ToUpper(), def.instruction);
            G.Hud.SetAccent(Pal.Cyan);
            var map = G.Map;
            string hz = mission.hazardDistrict, wr = mission.wrongDistrict;

            report = Label(At(0.6f, 1.75f, 1.6f), new Vector2(1.1f, 0.53f), "", 34, Pal.Text, true);
            report.alignment = TextAlignmentOptions.MidlineLeft;
            report.enableAutoSizing = true;      // long reports shrink to fit the panel
            report.fontSizeMin = 20;
            report.fontSizeMax = 34;
            Vector3 pA = At(-0.42f, RowY, RowZ), pR = At(0f, RowY, RowZ), pU = At(0.42f, RowY, RowZ);
            decisionButtons.Add(Btn(pA, new Vector2(0.36f, 0.15f), "APPROVE", new Color(0.1f, 0.45f, 0.22f), () => choice = "approve", 50).transform.parent.gameObject);
            decisionButtons.Add(Btn(pR, new Vector2(0.36f, 0.15f), "REJECT", new Color(0.5f, 0.13f, 0.13f), () => choice = "reject", 50).transform.parent.gameObject);
            decisionButtons.Add(Btn(pU, new Vector2(0.36f, 0.15f), "RERUN", new Color(0.5f, 0.38f, 0.05f), () => choice = "rerun", 50).transform.parent.gameObject);

            var practice = def.reports.Take(Mathf.Max(0, def.reports.Length - 1)).ToList();
            int total = practice.Count + 1;
            maxPoints = total * 100;

            // ---- practice reports from other teams
            for (int i = 0; i < practice.Count; i++)
            {
                var r = practice[i];
                ShowReport(r, i + 1, total);
                Vector3 answerPos = r.correct == "approve" ? pA : r.correct == "reject" ? pR : pU;
                SetHint(r.correct.ToUpper() + ": " + r.why, answerPos);
                if (i == 0) Cue.Show(report.transform.position, 0.33f);
                choice = null;
                yield return new WaitUntil(() => choice != null);
                Cue.Hide();
                bool ok = choice == r.correct;
                if (ok) Good(100, "Correct - " + r.why, report.transform.position + Vector3.up * 0.38f);
                else Bad("The right call was " + r.correct.ToUpper() + ". " + r.why, report.transform.position + Vector3.up * 0.38f);
                G.MapOverride = true;
                ShowConsequence(map, choice, r.correct == "approve", hz, wr);
                yield return GameClock.Wait(ok ? 3.0f : 4.2f);
                map.ResetVisuals();
                G.MapOverride = false;
            }

            // ---- the player's own report: decide, repair, rerun, decide again
            G.Hud.Instruct("Last one is YOUR pipeline. Judge it honestly: APPROVE, REJECT or RERUN?");
            bool firstDecision = true;
            string outcome = null;
            while (outcome == null)
            {
                var own = BuildOwnReport();
                ShowReport(own.item, total, total);
                SetDecisionButtons(true);
                Vector3 answerPos = own.approvable ? pA : pR;
                SetHint(own.item.why, answerPos);
                choice = null;
                yield return new WaitUntil(() => choice != null);
                SessionLog.Event("own_report", choice + " trust=" + own.item.trust + " approvable=" + own.approvable);

                bool ok = own.approvable ? choice == "approve" : choice != "approve";
                Vector3 pop = report.transform.position + Vector3.up * 0.38f;
                if (firstDecision)
                {
                    if (ok) Good(100, "Good call. " + own.item.why, pop);
                    else Bad(own.approvable ? "Your data was fine - blocking it leaves the city without a warning." : "That was risky! " + own.item.why, pop);
                }
                else if (ok && own.approvable) Good(50, "Recovered! The repaired output is trustworthy.", pop);
                firstDecision = false;

                if (choice == "approve")
                {
                    outcome = own.approvable ? (G.FloodActive && G.FloodLeft <= 0f ? "late" : "safe") : "wrong";
                    break;
                }
                if (own.approvable)
                {
                    if (choice == "reject") { outcome = "blocked"; break; }
                    // rerun on good data: wastes time (Rerun costs 25 s), same result comes back
                    yield return Rerun("Nothing needed fixing - the same good result comes back.");
                    continue;
                }

                // bad data: reject stops it; then repair and rerun (or give up)
                if (choice == "reject")
                {
                    G.Hud.Say("Rejected - bad output stopped. Now REPAIR the problems and RERUN, or give up.", Pal.Warn);
                    bool giveUp = false;
                    yield return AskRepairOrGiveUp(v => giveUp = v);
                    if (giveUp) { outcome = "gaveup"; break; }
                }
                yield return RepairStage();
                yield return Rerun("Spark reprocessed the repaired data.");
                if (G.FloodActive && G.FloodLeft <= 0f)
                    G.Hud.Say("The flood has already arrived - decide quickly!", Pal.Bad);
            }

            // ---- the city outcome
            SetDecisionButtons(false);
            G.MapOverride = true;
            map.ResetVisuals();
            string text;
            switch (outcome)
            {
                case "safe":
                    text = "SAFE - " + mission.DistrictName(hz) + " was evacuated in time.";
                    map.SetHazard(hz, 0.6f); map.SetBeacon(hz, true, Pal.Good); map.SetLabelColor(hz, Pal.Good);
                    map.ShowRoute(mission.safeFrom, hz, Pal.Good);
                    map.SetStatus(hz, "EVACUATED  -  EVERYONE SAFE", Pal.Good);
                    Sfx.Win();
                    break;
                case "late":
                    text = "PARTLY FLOODED - the plan was right, but it came too late.";
                    map.SetHazard(hz, 1f); map.SetBeacon(hz, true, Pal.Warn); map.SetLabelColor(hz, Pal.Warn);
                    map.ShowRoute(mission.safeFrom, hz, Pal.Warn);
                    map.SetStatus(hz, "WARNING TOO LATE", Pal.EnterNormal);
                    Sfx.Bad();
                    break;
                case "wrong":
                    text = "FLOODED - bad data sent the evacuation to " + mission.DistrictName(wr) + ".";
                    map.SetHazard(hz, 1f); map.SetTint(hz, Pal.Bad); map.SetLabelColor(hz, Pal.Bad);
                    map.SetBeacon(wr, true, Pal.Warn);
                    map.ShowRoute(mission.unsafeFrom, hz, Pal.Bad);
                    map.SetStatus(hz, "FLOODED  -  NOT EVACUATED", Pal.Bad);
                    map.SetStatus(wr, "WRONG DISTRICT EVACUATED", Pal.EnterNormal);
                    Sfx.Bad();
                    break;
                case "gaveup":
                    text = "PARTLY FLOODED - you stopped bad data, but no corrected warning went out.";
                    map.SetHazard(hz, 0.9f); map.SetBeacon(hz, true, Pal.Warn); map.SetLabelColor(hz, Pal.Warn);
                    map.SetStatus(hz, "NO WARNING SENT", Pal.EnterNormal);
                    Sfx.Bad();
                    break;
                default: // blocked good data
                    text = "FLOODED - good data was blocked and no warning went out.";
                    map.SetHazard(hz, 1f); map.SetTint(hz, Pal.Bad); map.SetLabelColor(hz, Pal.Bad);
                    map.SetStatus(hz, "FLOODED  -  NO WARNING", Pal.Bad);
                    Sfx.Bad();
                    break;
            }
            G.Progress.cityOutcome = text;
            SessionLog.Event("city_outcome", text);
            G.Hud.Instruct("Look at the city map.");
            G.Hud.Say(text, text.StartsWith("SAFE") ? Pal.Good : Pal.Bad);
            yield return GameClock.Wait(4.5f);
        }

        IEnumerator AskRepairOrGiveUp(System.Action<bool> result)
        {
            SetDecisionButtons(false);
            string pick = null;
            var repair = Btn(At(-0.24f, RowY, RowZ), new Vector2(0.42f, 0.15f), "Repair + rerun", new Color(0.1f, 0.4f, 0.55f), () => pick = "repair", 44);
            var stop = Btn(At(0.24f, RowY, RowZ), new Vector2(0.36f, 0.15f), "Give up", new Color(0.3f, 0.12f, 0.12f), () => pick = "stop", 44);
            yield return new WaitUntil(() => pick != null);
            repair.transform.parent.gameObject.SetActive(false);
            stop.transform.parent.gameObject.SetActive(false);
            result(pick == "stop");
        }

        /// <summary>One button per problem; each repair costs flood time. Then RERUN.</summary>
        IEnumerator RepairStage()
        {
            SetDecisionButtons(false);
            var pr = G.Progress;
            var buttons = new List<GameObject>();
            string clicked = null;
            var fixes = new List<string>();
            if (pr.keptUnreliable.Count + pr.keptUnneeded.Count > 0) fixes.Add("remove");
            if (pr.tossedNeeded.Count > 0) fixes.Add("addback");
            if (pr.missedTools.Count > 0) fixes.Add("reclean");
            G.Hud.Instruct("REPAIR: click each problem to fix it (each costs 20 s), then press RERUN.");
            for (int i = 0; i < fixes.Count; i++)
            {
                string f = fixes[i];
                string label = f == "remove" ? "Remove bad sources" : f == "addback" ? "Re-ingest missing data" : "Re-clean dirty records";
                float x = fixes.Count == 1 ? 0f : -0.42f + 0.84f * i / (fixes.Count - 1);
                buttons.Add(Btn(At(x, RowY, RowZ), new Vector2(0.38f, 0.15f), label, new Color(0.1f, 0.4f, 0.55f), () => clicked = f, 34).transform.parent.gameObject);
            }
            var rerunGo = Btn(At(0f, RowY - 0.19f, RowZ), new Vector2(0.5f, 0.14f), "RERUN pipeline  >", new Color(0.5f, 0.38f, 0.05f), () => clicked = "rerun", 44).transform.parent.gameObject;
            SetHint("Fix every red X and yellow ! problem, then RERUN.", buttons.Count > 0 ? buttons[0].transform.position : rerunGo.transform.position);
            while (true)
            {
                clicked = null;
                yield return new WaitUntil(() => clicked != null);
                if (clicked == "rerun") break;
                Sfx.Click();
                G.FloodPenalty(20f);
                if (clicked == "remove") { pr.keptUnreliable.Clear(); pr.keptUnneeded.Clear(); G.Hud.Say("Removed the unneeded / unreliable sources.", Pal.Cyan); }
                if (clicked == "addback") { pr.tossedNeeded.Clear(); G.Hud.Say("Re-ingested the missing data.", Pal.Cyan); }
                if (clicked == "reclean") { pr.missedTools.Clear(); pr.recordsMissed = 0; G.Hud.Say("Re-cleaned the dirty records.", Pal.Cyan); }
                int idx = fixes.IndexOf(clicked);
                if (idx >= 0 && buttons[idx] != null) buttons[idx].SetActive(false);
                SessionLog.Event("repair", clicked);
            }
            foreach (var b in buttons) if (b != null) Destroy(b);
            Destroy(rerunGo);
        }

        IEnumerator Rerun(string doneMessage)
        {
            SetDecisionButtons(false);
            G.FloodPenalty(25f);
            G.Hud.Instruct("RERUN: Spark is processing the data again...");
            for (int i = 0; i < 3; i++)
            {
                report.text = "<b>RERUNNING SPARK JOB</b>\n\n" + Pal.Col(Fx.Bar((i + 1) / 3f, 20), Pal.Cyan) + "\n\nread -> filter -> join -> groupBy -> write";
                Sfx.Click();
                yield return GameClock.Wait(0.8f);
            }
            G.Hud.Say(doneMessage + " Judge the new report.", Pal.Cyan);
            G.Hud.Instruct("Judge YOUR new report: APPROVE, REJECT or RERUN?");
        }

        static void ShowConsequence(CityMap map, string choice, bool dataGood, string hz, string wr)
        {
            if (choice == "approve" && dataGood)
            {
                map.SetHazard(hz, 0.5f); map.SetBeacon(hz, true, Pal.Good); map.SetLabelColor(hz, Pal.Good);
                map.SetStatus(hz, "EVACUATED  -  SAFE", Pal.Good);
            }
            else if (choice == "approve")
            {
                map.SetHazard(hz, 1f); map.SetTint(hz, Pal.Bad); map.SetBeacon(wr, true, Pal.Warn);
                map.SetStatus(hz, "FLOODED  -  NOT EVACUATED", Pal.Bad);
            }
            else if (dataGood)
            {
                map.SetHazard(hz, 1f); map.SetTint(hz, Pal.Bad);
                map.SetStatus(hz, "FLOODED  -  NO WARNING", Pal.Bad);
            }
            else
            {
                map.SetHazard(hz, 0.6f); map.SetBeacon(hz, true, Pal.Warn);
                map.SetStatus(hz, "BAD DATA STOPPED", Pal.EnterNormal);
            }
        }
    }
}