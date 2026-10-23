// Mission 2 (Structured Streaming): two short mini-games in the cluster room.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparkCity
{
    // ===================================================================== 1. WINDOW CATCHER
    /// <summary>Choose a watermark lateness threshold so late events are caught without making the dashboard too slow.
    /// Every run replays the SAME 100 events, and what the player sees (caught / dropped packets) is exactly
    /// what is scored. The watermark is shown as "max event time seen minus the threshold".</summary>
    public class WindowCatcherGame : MiniGame
    {
        public WindowDef def;

        public override Vector3 HudOffset { get { return new Vector3(0f, 2.3f, 1.9f); } }

        Transform inlet, gate, dashboard, bin;
        TextMeshProUGUI readout;
        int caughtCount, droppedCount, active;
        readonly List<Renderer> gateParts = new List<Renderer>();

        // ---- the fixed event list (built once from the mission presets, so the visible run and the score always agree)
        struct Ev { public float eventMin, late; }
        readonly List<Ev> events = new List<Ev>();
        const float WindowEnd = 5f;      // window 17:00 - 17:05 (minutes after 17:00)
        const float TimelineEnd = 21f;   // the stream runs until the max event time reaches 17:21

        // ---- stream monitor (timeline): window block, "max event time" marker, watermark marker
        RectTransform maxMarker, wmMarker;
        TextMeshProUGUI windowLabel;
        string markerText = "";
        Image windowBlock;
        const float TlX = 90f, TlW = 1320f;  // timeline position inside the 1500 px monitor

        void BuildEvents()
        {
            events.Clear();
            var opts = def.options.OrderBy(o => o.minutes).ToList();
            var rnd = new System.Random(7);           // fixed seed: identical events for every run
            int prevCaught = 0;
            float prevMin = 0f;
            for (int i = 0; i < opts.Count; i++)
            {
                int count = Mathf.Max(0, opts[i].caught - prevCaught);
                // on-time events for the smallest threshold, otherwise late by an amount between the two thresholds
                float late = i == 0 ? opts[0].minutes : (prevMin + opts[i].minutes) / 2f + 0.25f;
                for (int k = 0; k < count; k++) events.Add(new Ev { eventMin = (float)rnd.NextDouble() * WindowEnd * 0.98f, late = late });
                prevCaught = Mathf.Max(prevCaught, opts[i].caught);
                prevMin = opts[i].minutes;
            }
            // events later than every threshold (never caught)
            for (int k = prevCaught; k < 100; k++) events.Add(new Ev { eventMin = (float)rnd.NextDouble() * WindowEnd * 0.98f, late = prevMin + 2f });
        }

        int CaughtPercent(float threshold)
        {
            int c = events.Count(e => e.late <= threshold);
            return Mathf.RoundToInt(100f * c / Mathf.Max(1, events.Count));
        }

        protected override IEnumerator Play()
        {
            BuildEvents();
            G.Hud.Step("1/2   " + def.title.ToUpper(), "Watch: GPS events stream into a 5-minute window. ORANGE = late.");
            G.Hud.SetAccent(new Color(0.65f, 0.55f, 1f));

            // ---- the scene, read left to right:  KAFKA  ->  5-MIN WINDOW (gate)  ->  DASHBOARD
            const float Z = 3.6f;
            inlet = Fx.Prim(PrimitiveType.Cylinder, "KafkaInlet", root, Vector3.zero, new Vector3(0.36f, 0.28f, 0.36f), Mat.Glow(new Color(0.55f, 0.6f, 0.75f), 1f)).transform;
            inlet.position = At(-1.6f, 1.42f, Z);
            inlet.rotation = A.Rot * Quaternion.Euler(0f, 0f, 90f);
            var kl = Label(At(-1.6f, 1.8f, Z), new Vector2(1.1f, 0.24f), "<b>1  KAFKA STREAM</b>\n<size=70%>ambulance GPS events</size>", 56, Pal.TextLight, true);
            Fonts.UseHeading(kl);

            gate = Fx.Group("WindowGate", root).transform;
            gate.SetPositionAndRotation(At(0f, 0f, Z), A.Rot);
            var gm = Mat.Glow(Pal.Warn, 1.5f);
            gateParts.Add(Fx.Prim(PrimitiveType.Cube, "Post", gate, new Vector3(-0.45f, 1.42f, 0f), new Vector3(0.06f, 1.05f, 0.06f), gm).GetComponent<Renderer>());
            gateParts.Add(Fx.Prim(PrimitiveType.Cube, "Post", gate, new Vector3(0.45f, 1.42f, 0f), new Vector3(0.06f, 1.05f, 0.06f), gm).GetComponent<Renderer>());
            gateParts.Add(Fx.Prim(PrimitiveType.Cube, "Top", gate, new Vector3(0f, 1.95f, 0f), new Vector3(0.96f, 0.06f, 0.06f), gm).GetComponent<Renderer>());
            gateParts.Add(Fx.Prim(PrimitiveType.Cube, "Bottom", gate, new Vector3(0f, 0.9f, 0f), new Vector3(0.96f, 0.06f, 0.06f), gm).GetComponent<Renderer>());
            var wl = Label(At(0f, 1.78f, Z - 0.05f), new Vector2(0.84f, 0.24f), "<b>2  5-MIN WINDOW</b>\n<size=70%>event time 17:00 - 17:05</size>", 56, Pal.Warn, true);
            Fonts.UseHeading(wl);

            dashboard = Box("Dashboard", At(1.6f, 1.42f, Z), new Vector3(0.6f, 0.42f, 0.08f), Mat.Glow(new Color(0.1f, 0.35f, 0.55f), 1f)).transform;
            var dl = Label(At(1.6f, 1.8f, Z), new Vector2(1.1f, 0.24f), "<b>3  TRAFFIC DASHBOARD</b>\n<size=70%>shows the counted events</size>", 56, Pal.Cyan, true);
            Fonts.UseHeading(dl);
            Label(At(-0.82f, 1.42f, Z), new Vector2(0.3f, 0.2f), ">>", 110, Pal.Dim);
            Label(At(0.82f, 1.42f, Z), new Vector2(0.3f, 0.2f), ">>", 110, Pal.Dim);
            // dropped events fall onto a red grate on the floor under the window
            bin = Box("DroppedGrate", At(0f, 0.02f, Z - 0.1f), new Vector3(0.9f, 0.02f, 0.5f), Mat.Glow(Pal.Bad, 0.8f)).transform;

            // ---- stream monitor close to the player: event-time timeline + result line
            BuildMonitor(At(0f, 1.27f, 1.32f));

            // ---- controls: lateness thresholds + RUN
            int selected = -1;
            bool runPressed = false;
            var optButtons = new List<Button>();
            int n = def.options.Length;
            Vector3 tenPos = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                float x = n == 1 ? 0f : -0.68f + 1.36f * i / (n - 1);
                Vector3 p = At(x, 1.02f, 1.25f);
                if (def.options[i].minutes == 10) tenPos = p;
                var b = UIKit.WorldButton(root, p, Face(p) * Quaternion.Euler(15f, 0f, 0f), new Vector2(0.3f, 0.13f), def.options[i].minutes + " min", Pal.NavyBtn, () =>
                {
                    selected = idx;
                    Sfx.Click();
                    for (int j = 0; j < optButtons.Count; j++)
                        UIKit.SetButtonColors(optButtons[j], j == idx ? Pal.EnterNormal : Pal.NavyBtn, j == idx ? Pal.EnterHover : Pal.NavyBtnHover, j == idx ? Pal.TextOnAmber : Pal.TextLight);
                    readout.text = "Lateness threshold: <b>" + def.options[idx].minutes + " min</b>   -   now press RUN";
                    SetWatermark(0f, def.options[idx].minutes);
                }, 46);
                UIKit.SetButtonColors(b, Pal.NavyBtn, Pal.NavyBtnHover, Pal.TextLight);
                optButtons.Add(b);
            }
            Vector3 runPos = At(0f, 0.86f, 1.22f);
            var run = UIKit.WorldButton(root, runPos, Face(runPos) * Quaternion.Euler(15f, 0f, 0f), new Vector2(0.44f, 0.13f), "RUN  >", new Color(0.1f, 0.45f, 0.22f), () => runPressed = true, 52);
            System.Action<bool> lockControls = on => { foreach (var b in optButtons) b.interactable = !on; run.interactable = !on; };
            lockControls(true);

            maxPoints = 300;

            // demo run: 0-minute lateness allowance
            yield return GameClock.Wait(0.8f);
            var zero = def.options.OrderBy(o => o.minutes).First();
            yield return RunStream(zero);
            Sfx.Bad();
            G.Hud.Say("0-minute lateness allowance: only " + CaughtPercent(zero.minutes) + "% caught - every LATE event was dropped, so jammed roads look empty!", Pal.Bad);
            yield return GameClock.Wait(2.5f);

            G.Hud.Instruct("Pick a threshold, then RUN.   Goal: " + def.goalCaught + "%+ caught, under " + def.goalDelay + " s", true);
            G.Hud.Say("", Pal.Dim);
            lockControls(false);
            SetHint("Try 10 minutes: it catches almost every late event and the delay stays small.", tenPos);

            int attempts = 0;
            while (true)
            {
                runPressed = false;
                yield return new WaitUntil(() => runPressed);
                Cue.Hide();
                if (selected < 0) { G.Hud.Say("Pick a threshold first.", Pal.Warn); continue; }
                attempts++;
                var opt = def.options[selected];
                lockControls(true);                 // no changes while this run is being evaluated
                yield return RunStream(opt);
                int pct = CaughtPercent(opt.minutes); // the same number the animation just showed
                bool enough = pct >= def.goalCaught;
                bool fast = opt.delay < def.goalDelay;
                SessionLog.Event("watermark", opt.minutes + "min");
                if (enough && fast)
                {
                    int pts = attempts == 1 ? 300 : attempts == 2 ? 200 : 100;
                    Sfx.Win();
                    Good(pts, pct + "% caught with only " + opt.delay + " s delay. Perfect trade-off!", readout.transform.position + Vector3.up * 0.25f);
                    break;
                }
                if (!enough) Bad(pct + "% caught - too many late events dropped. Allow MORE lateness.", readout.transform.position + Vector3.up * 0.25f);
                else Bad(pct + "% caught but " + opt.delay + " s delay - too SLOW for ambulances. Allow less lateness.", readout.transform.position + Vector3.up * 0.25f);
                lockControls(false);
            }
            yield return GameClock.Wait(2.5f);
        }

        // ------------------------------------------------------------ stream monitor
        void BuildMonitor(Vector3 pos)
        {
            const float W = 1500f, H = 230f;
            var c = UIKit.Canvas("StreamMonitor", root, new Vector2(W, H), 0.001f, false);
            c.transform.SetPositionAndRotation(pos, Face(pos));
            var r = (RectTransform)c.transform;
            var bg = UIKit.Image(r, Pal.Rgb("#091322", 0.92f), "Bg");
            UIKit.Stretch(bg.rectTransform);
            Sprites.Apply(bg, Sprites.Rounded, 1.2f);
            var ol = UIKit.Image(r, new Color(0.65f, 0.55f, 1f, 0.8f), "Border");
            UIKit.Stretch(ol.rectTransform);
            Sprites.Apply(ol, Sprites.Outline, 1.2f);

            // timeline track: event time 17:00 -> 17:21
            var track = UIKit.Image(r, new Color(1f, 1f, 1f, 0.15f), "Track");
            Place(track.rectTransform, TlX, 70, TlW, 8);
            windowBlock = UIKit.Image(r, new Color(Pal.Warn.r, Pal.Warn.g, Pal.Warn.b, 0.55f), "WindowBlock");
            Place(windowBlock.rectTransform, TlX, 58, TlW * WindowEnd / TimelineEnd, 32);
            windowLabel = UIKit.Text(r, "WINDOW 17:00-17:05", 22, Pal.Warn, TextAlignmentOptions.Left, "WinLabel");
            Place(windowLabel.rectTransform, TlX, 26, 760, 30);
            for (int m = 0; m <= 20; m += 5)
            {
                var tick = UIKit.Text(r, "17:" + m.ToString("00"), 20, Pal.Dim, TextAlignmentOptions.Center, "Tick");
                Place(tick.rectTransform, TlX + TlW * m / TimelineEnd - 40, 92, 80, 24);
            }
            maxMarker = Marker(r, Pal.Cyan);
            wmMarker = Marker(r, Pal.Bad);
            var legend = UIKit.Text(r, Pal.Col("|", Pal.Cyan) + " max event time seen      " + Pal.Col("|", Pal.Bad) + " watermark", 24, Pal.TextLight, TextAlignmentOptions.Right, "Legend");
            Place(legend.rectTransform, W - 620, 26, 560, 30);

            readout = UIKit.Text(r, Pal.Col("WATERMARK = max event time seen  -  the lateness threshold", Pal.TextLight), 34, Pal.TextLight, TextAlignmentOptions.Center, "Readout");
            Place(readout.rectTransform, 30, 150, W - 60, 70);
            readout.enableAutoSizing = true; readout.fontSizeMin = 20; readout.fontSizeMax = 34;
            SetWatermark(0f, 0f);
        }

        RectTransform Marker(RectTransform parent, Color c)
        {
            var m = UIKit.Image(parent, c, "Marker");
            m.rectTransform.anchorMin = m.rectTransform.anchorMax = new Vector2(0f, 1f);
            m.rectTransform.pivot = new Vector2(0.5f, 1f);
            m.rectTransform.sizeDelta = new Vector2(8, 44);
            return m.rectTransform;
        }

        static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static string Clock(float minAfter5pm) { int m = Mathf.FloorToInt(Mathf.Max(0f, minAfter5pm)); return "17:" + m.ToString("00"); }

        /// <summary>Moves the two markers: max event time seen, and watermark = max - threshold.</summary>
        void SetWatermark(float maxSeen, float threshold)
        {
            float wm = Mathf.Max(0f, maxSeen - threshold);
            maxMarker.anchoredPosition = new Vector2(TlX + TlW * Mathf.Clamp01(maxSeen / TimelineEnd), -52f);
            wmMarker.anchoredPosition = new Vector2(TlX + TlW * Mathf.Clamp01(wm / TimelineEnd), -52f);
            markerText = Pal.Col("max " + Clock(maxSeen), Pal.Cyan) + "  -  " + threshold.ToString("0") + " min  =  " + Pal.Col("watermark " + Clock(wm), Pal.Bad);
            bool finalized = maxSeen - threshold >= WindowEnd;
            windowBlock.color = finalized ? new Color(0.5f, 0.5f, 0.55f, 0.55f) : new Color(Pal.Warn.r, Pal.Warn.g, Pal.Warn.b, 0.55f);
            windowLabel.text = finalized ? "WINDOW 17:00-17:05  -  FINALIZED (later events are dropped)" : "WINDOW 17:00-17:05  -  open";
        }

        // ------------------------------------------------------------ the run
        IEnumerator RunStream(WatermarkOption opt)
        {
            caughtCount = droppedCount = 0;
            active = 0;
            var gateColor = Mat.Glow(Pal.Warn, 1.5f);
            foreach (var r in gateParts) r.sharedMaterial = gateColor;
            // each event ARRIVES when the stream's max event time reaches (its event time + its lateness)
            var order = events.OrderBy(e => e.eventMin + e.late).ToList();
            int next = 0;
            float t = 0f;
            const float dur = 5f;                       // real seconds for event time 17:00 -> 17:21
            while (t < dur || next < order.Count)
            {
                t += GameClock.Dt;
                float maxSeen = Mathf.Min(TimelineEnd, TimelineEnd * t / dur);
                SetWatermark(maxSeen, opt.minutes);
                while (next < order.Count && order[next].eventMin + order[next].late <= maxSeen)
                {
                    var e = order[next++];
                    bool caught = e.late <= opt.minutes; // older than the watermark when it arrives -> dropped
                    StartCoroutine(Packet(e.late > 0f, caught));
                }
                readout.text = markerText + "      " + Pal.Col("caught " + caughtCount, Pal.Good) + "   " + Pal.Col("dropped " + droppedCount, Pal.Bad);
                yield return null;
            }
            // wait until every packet has finished its trip before showing the result
            while (active > 0)
            {
                readout.text = markerText + "      " + Pal.Col("caught " + caughtCount, Pal.Good) + "   " + Pal.Col("dropped " + droppedCount, Pal.Bad);
                yield return null;
            }
            int pct = Mathf.RoundToInt(100f * caughtCount / Mathf.Max(1, caughtCount + droppedCount));
            Color c = pct >= def.goalCaught ? Pal.Good : Pal.Bad;
            Color d = opt.delay < def.goalDelay ? Pal.Good : Pal.Bad;
            foreach (var r in gateParts) r.sharedMaterial = Mat.Glow(pct >= def.goalCaught ? Pal.Good : Pal.Warn, 1.5f);
            readout.text = "Threshold " + opt.minutes + " min:   caught " + Pal.Col(pct + "%", c) + "   (" + caughtCount + " / " + (caughtCount + droppedCount) + ")" +
                           "     dashboard delay " + Pal.Col(opt.delay + " s", d) + "  <size=70%>(simulated)</size>";
        }

        IEnumerator Packet(bool late, bool caught)
        {
            active++;
            var p = Fx.Prim(PrimitiveType.Sphere, "Event", root, Vector3.zero, Vector3.one * 0.07f, Mat.Glow(late ? Pal.Warn : Pal.Cyan, 2f)).transform;
            Vector3 a = inlet.position;
            Vector3 b = gate.position + Vector3.up * 1.42f + A.Right * (float)(Random.value - 0.5f) * 0.5f;
            Vector3 c = caught ? dashboard.position : bin.position + Vector3.up * 0.03f;
            float t = 0f;
            const float d1 = 0.6f, d2 = 0.5f;
            while (t < d1) { if (p == null) { active--; yield break; } t += GameClock.Dt; p.position = Vector3.Lerp(a, b, t / d1); yield return null; }
            t = 0f;
            while (t < d2)
            {
                if (p == null) { active--; yield break; }
                t += GameClock.Dt;
                var pos = Vector3.Lerp(b, c, t / d2);
                pos.y += Mathf.Sin(t / d2 * Mathf.PI) * (caught ? 0.2f : 0.05f);
                p.position = pos;
                yield return null;
            }
            if (caught) caughtCount++; else droppedCount++;
            active--;
            if (p != null) Destroy(p.gameObject);
        }
    }

    // ===================================================================== 2. CRASH RECOVERY
    /// <summary>The streaming application crashes and restarts; without a durable checkpoint all progress is lost.
    /// (A single executor failure would just be retried by Spark - so here the WHOLE app goes down.)
    /// With a checkpoint, students SEE progress being saved to the disk, and the restart reading it back.
    /// Drama comes from events (room dims, lights go out, amber warning), not from constant glow.</summary>
    public class CrashRecoveryGame : MiniGame
    {
        public CrashDef def;

        public override Vector3 HudOffset { get { return new Vector3(0f, 2.3f, 1.9f); } }

        Transform disk;
        TextMeshProUGUI diskLabel;
        readonly List<Renderer> strips = new List<Renderer>();   // executor light strips
        readonly List<Vector3> execPos = new List<Vector3>();
        Renderer driverCore, driverRing;
        Material coreOn, ringOn, stripOn, off;
        Vector3 driverPos;
        float progress;

        // status panel (one place for headline, detail and the progress bar)
        TextMeshProUGUI statusHead, statusSub, statusPct;
        RectTransform barFill;
        Image barFillImg, statusBorder;

        // room lighting (dimmed briefly during the crash)
        Light sun;
        float sunIntensity;
        Color ambient;

        protected override IEnumerator Play()
        {
            G.Hud.Step("2/2   " + def.title.ToUpper(), "Watch the streaming job run...");

            // ---- driver (left): small dark core + a controlled luminous ring, nameplate above
            driverPos = At(-1.9f, 1.15f, 3.0f);
            Fx.Prim(PrimitiveType.Cylinder, "Podium", root, Vector3.zero, new Vector3(0.44f, 0.45f, 0.44f), Mat.Lit(new Color(0.1f, 0.1f, 0.15f))).transform.position = At(-1.9f, 0.45f, 3.0f);
            coreOn = Mat.Glow(new Color(0.55f, 0.22f, 0.42f), 0.9f);
            ringOn = Mat.Glow(new Color(0.95f, 0.45f, 0.75f), 1.8f);
            off = Mat.Lit(new Color(0.08f, 0.08f, 0.1f));
            var core = Fx.Prim(PrimitiveType.Sphere, "DriverCore", root, Vector3.zero, Vector3.one * 0.26f, coreOn);
            core.transform.position = driverPos;
            driverCore = core.GetComponent<Renderer>();
            var ring = Fx.Prim(PrimitiveType.Cylinder, "DriverRing", root, Vector3.zero, new Vector3(0.44f, 0.008f, 0.44f), ringOn);
            ring.transform.SetPositionAndRotation(driverPos, A.Rot * Quaternion.Euler(70f, 0f, 0f)); // ring tilted toward the player
            driverRing = ring.GetComponent<Renderer>();
            var dl = Label(driverPos + Vector3.up * 0.42f, new Vector2(0.5f, 0.12f), "DRIVER", 64, new Color(0.95f, 0.55f, 0.8f), true);
            dl.fontStyle = FontStyles.Bold;
            dl.characterSpacing = 6f;
            Fonts.UseHeading(dl);

            // ---- executors (right): dark blocks with a restrained light strip, nameplates above
            stripOn = Mat.Glow(Pal.WelcomeBorder, 1.4f);
            for (int i = 0; i < 3; i++)
            {
                float x = -0.5f + 1.25f * i;
                Vector3 bp = At(x, 0f, 4.2f);
                execPos.Add(bp + Vector3.up * 0.6f);
                Box("ExecBase" + i, bp + Vector3.up * 0.45f, new Vector3(0.6f, 0.9f, 0.6f), Mat.Lit(new Color(0.1f, 0.12f, 0.18f)));
                strips.Add(Box("ExecStrip" + i, bp + Vector3.up * 0.8f - A.Fwd * 0.305f, new Vector3(0.5f, 0.03f, 0.01f), stripOn).GetComponent<Renderer>());
                var el = Label(bp + Vector3.up * 1.1f, new Vector2(0.7f, 0.12f), "EXECUTOR " + (i + 1), 52, Pal.TextLight, true);
                Fonts.UseHeading(el);
                // subtle cyan pathway on the floor from the driver to this executor
                Vector3 from = At(-1.9f, 0.01f, 3.0f), to = bp + Vector3.up * 0.01f;
                var path = Fx.Prim(PrimitiveType.Cube, "Path" + i, root, Vector3.zero, new Vector3(0.03f, 0.004f, Vector3.Distance(from, to)), Mat.Glow(Pal.WelcomeBorder, 0.5f));
                path.transform.SetPositionAndRotation((from + to) / 2f, Quaternion.LookRotation(to - from, Vector3.up));
            }

            // ---- durable storage (checkpoint disk) - appears when the checkpoint is switched on
            disk = Fx.Group("CheckpointDisk", root).transform;
            disk.position = At(-1.0f, 0.5f, 2.6f);
            Fx.Prim(PrimitiveType.Cylinder, "Disk", disk, Vector3.zero, new Vector3(0.4f, 0.05f, 0.4f), Mat.Glow(Pal.Good, 1.2f));
            Fx.Prim(PrimitiveType.Cylinder, "Disk2", disk, new Vector3(0f, 0.13f, 0f), new Vector3(0.4f, 0.05f, 0.4f), Mat.Glow(Pal.Good, 1.2f));
            diskLabel = Label(At(-1.0f, 0.9f, 2.6f), new Vector2(0.9f, 0.22f), "<b>CHECKPOINT</b>\n<size=70%>durable storage</size>", 52, Pal.Good, true);
            disk.gameObject.SetActive(false);
            diskLabel.transform.parent.gameObject.SetActive(false);

            // ---- one clear status panel
            BuildStatusPanel(At(0.35f, 1.88f, 3.3f));
            SetProgress(0f, "STREAMING JOB RUNNING", "Reading the GPS stream", Pal.Cyan);

            // room light (for the crash dim)
            sun = RenderSettings.sun != null ? RenderSettings.sun : FindFirstObjectByType<Light>();
            sunIntensity = sun != null ? sun.intensity : 1f;
            ambient = RenderSettings.ambientLight;

            // ---- choices: equal size, on a shallow arc (all the same distance), neutral navy
            string choice = null;
            var optButtons = new List<Button>();
            Vector3 correctPos = Vector3.zero;
            int n = def.options.Length;
            for (int i = 0; i < n; i++)
            {
                var o = def.options[i];
                float ang = n == 1 ? 0f : -24f + 48f * i / (n - 1);      // degrees around the player
                Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
                Vector3 p = A.pos + A.Rot * (dir * 1.25f) + Vector3.up * 1.06f;
                if (o.correct) correctPos = p;
                string id = o.id;
                var b = UIKit.WorldButton(root, p, Face(p) * Quaternion.Euler(15f, 0f, 0f), new Vector2(0.46f, 0.14f), o.label, Pal.NavyBtn, () => choice = id, 40);
                UIKit.SetButtonColors(b, Pal.NavyBtn, Pal.NavyBtnHover, Pal.TextLight);
                optButtons.Add(b);
            }
            foreach (var b in optButtons) b.interactable = false;

            maxPoints = 300;

            // first run: crash without protection
            yield return GameClock.Wait(0.6f);
            yield return RunWithCrash(false);
            G.Hud.Say("The app restarted from the beginning - 40 minutes of data must be read again and the running totals are gone!", Pal.Bad);
            yield return GameClock.Wait(2.6f);

            G.Hud.Instruct(def.instruction);
            G.Hud.Say("", Pal.Dim);
            foreach (var b in optButtons) b.interactable = true;
            SetHint("You need something saved OUTSIDE the application's memory, on durable storage.", correctPos);

            int attempts = 0;
            while (true)
            {
                choice = null;
                yield return new WaitUntil(() => choice != null);
                Cue.Hide();
                attempts++;
                var o = def.options.First(x => x.id == choice);
                SessionLog.Event("crash_choice", o.id);
                foreach (var b in optButtons) b.interactable = false;
                G.Hud.Say("Running again with: " + o.label + "...", Pal.Cyan);
                if (o.correct)
                {
                    disk.gameObject.SetActive(true);
                    diskLabel.transform.parent.gameObject.SetActive(true);
                    Sfx.Click();
                    yield return RunWithCrash(true);
                    int pts = attempts == 1 ? 300 : attempts == 2 ? 200 : 100;
                    Sfx.Win();
                    Good(pts, o.why, statusHead.transform.position + Vector3.down * 0.4f);
                    break;
                }
                yield return RunWithCrash(false);
                Bad(o.why + " Try another.", statusHead.transform.position + Vector3.down * 0.4f);
                foreach (var b in optButtons) b.interactable = true;
            }
            yield return GameClock.Wait(2.5f);
            RestoreRoom();
        }

        void OnDisable() { RestoreRoom(); } // never leave the room dark

        IEnumerator RunWithCrash(bool checkpoint)
        {
            SetAppAlive(true);
            if (checkpoint)
            {
                // progress is saved to the disk while the job runs: students SEE the checkpoint being written
                yield return Animate(0f, 0.3f, 1.2f, "STREAMING JOB RUNNING", "Processing - checkpoint ON", Pal.Cyan);
                yield return Pulse(driverPos, disk.position, "Saved: offsets + state at 30%");
                yield return Animate(0.3f, 0.6f, 1.2f, "STREAMING JOB RUNNING", "Processing - checkpoint ON", Pal.Cyan);
                yield return Pulse(driverPos, disk.position, "Saved: offsets + state at 60%");
            }
            else yield return Animate(0f, 0.6f, 2.4f, "STREAMING JOB RUNNING", "Processing - no checkpoint", Pal.Cyan);

            // ---- the crash: room dims, every light goes out, amber warning
            Sfx.Bad();
            SetAppAlive(false);
            DimRoom(true);
            SetProgress(progress, "APPLICATION CRASHED", "Driver and executors are down", Pal.Bad);
            yield return GameClock.Wait(0.9f);
            SetProgress(progress, "APPLICATION RESTARTING", "Starting a new driver and executors...", Pal.EnterNormal);
            Popup.Show(driverPos + Vector3.up * 0.7f, "RESTARTING...", Pal.EnterNormal, 80f);
            yield return GameClock.Wait(0.9f);
            DimRoom(false);
            SetAppAlive(true);
            if (checkpoint)
            {
                // the restarted app reads its progress BACK from the checkpoint, then the cluster continues
                SetProgress(0f, "APPLICATION RESTARTED", "Reading the checkpoint...", Pal.EnterNormal);
                yield return Pulse(disk.position, driverPos, "Restored: offsets + state");
                foreach (var e in execPos) StartCoroutine(Pulse(driverPos, e, "", false));
                yield return GameClock.Wait(0.7f);
                SetProgress(0.6f, "RECOVERED FROM CHECKPOINT", "Continuing exactly where it stopped", Pal.Good);
                yield return GameClock.Wait(0.8f);
                yield return Animate(0.6f, 1f, 1.6f, "RECOVERED FROM CHECKPOINT", "Continuing exactly where it stopped", Pal.Good);
                SetProgress(1f, "BATCH COMPLETE", "The stream keeps running", Pal.Good);
            }
            else
            {
                yield return Animate(0.6f, 0f, 0.7f, "APPLICATION RESTARTED", "Checkpoint missing - batch progress lost", Pal.Bad);
                SetProgress(0f, "APPLICATION RESTARTED", "Checkpoint missing - batch progress lost", Pal.Bad);
            }
        }

        /// <summary>A glowing pulse that flies from a to b (checkpoint save / restore), with an optional label.</summary>
        IEnumerator Pulse(Vector3 a, Vector3 b, string text, bool wait = true)
        {
            var p = Fx.Prim(PrimitiveType.Sphere, "Pulse", root, Vector3.zero, Vector3.one * 0.1f, Mat.Glow(Pal.Good, 2.6f)).transform;
            float t = 0f;
            const float dur = 0.6f;
            while (t < dur && p != null)
            {
                t += GameClock.Dt;
                var pos = Vector3.Lerp(a, b, t / dur);
                pos.y += Mathf.Sin(t / dur * Mathf.PI) * 0.25f;
                p.position = pos;
                yield return null;
            }
            if (p != null) Destroy(p.gameObject);
            if (!string.IsNullOrEmpty(text))
            {
                Sfx.Click();
                Popup.Show(b + Vector3.up * 0.35f, text, Pal.Good, 60f);
            }
            if (wait) yield return GameClock.Wait(0.4f);
        }

        /// <summary>Lights (alive) or switches off (crashed) the driver ring and the executor strips.</summary>
        void SetAppAlive(bool alive)
        {
            if (driverCore != null) driverCore.sharedMaterial = alive ? coreOn : off;
            if (driverRing != null) driverRing.sharedMaterial = alive ? ringOn : off;
            foreach (var s in strips) if (s != null) s.sharedMaterial = alive ? stripOn : off;
        }

        void DimRoom(bool dim)
        {
            if (sun != null) sun.intensity = dim ? sunIntensity * 0.25f : sunIntensity;
            RenderSettings.ambientLight = dim ? ambient * 0.35f : ambient;
        }

        void RestoreRoom()
        {
            if (sun != null && sunIntensity > 0f) sun.intensity = sunIntensity;
            if (ambient != default(Color)) RenderSettings.ambientLight = ambient;
        }

        // ------------------------------------------------------------ status panel
        void BuildStatusPanel(Vector3 pos)
        {
            const float W = 2000f, H = 380f;
            var c = UIKit.Canvas("CrashStatus", root, new Vector2(W, H), 0.001f, false);
            c.transform.SetPositionAndRotation(pos, Face(pos));
            var r = (RectTransform)c.transform;
            var bg = UIKit.Image(r, Pal.Rgb("#091322", 0.95f), "Bg");
            UIKit.Stretch(bg.rectTransform);
            Sprites.Apply(bg, Sprites.Rounded, 1.6f);
            statusBorder = UIKit.Image(r, Pal.WelcomeBorder, "Border");
            UIKit.Stretch(statusBorder.rectTransform);
            Sprites.Apply(statusBorder, Sprites.Outline, 1.6f);

            statusHead = UIKit.Text(r, "", 86, Pal.TextLight, TextAlignmentOptions.Center, "Head");
            Place(statusHead.rectTransform, 40, 24, W - 80, 110);
            statusHead.fontStyle = FontStyles.Bold;
            statusHead.characterSpacing = 6f;
            statusHead.enableAutoSizing = true; statusHead.fontSizeMin = 40; statusHead.fontSizeMax = 86;
            Fonts.UseHeading(statusHead);
            statusSub = UIKit.Text(r, "", 50, Pal.Dim, TextAlignmentOptions.Center, "Sub");
            Place(statusSub.rectTransform, 40, 136, W - 80, 70);

            // progress bar: track + fill + percentage
            var track = UIKit.Image(r, new Color(1f, 1f, 1f, 0.12f), "Track");
            Place(track.rectTransform, 120, 240, W - 460, 60);
            Sprites.Apply(track, Sprites.Rounded, 1f);
            barFillImg = UIKit.Image(track.rectTransform, Pal.Cyan, "Fill");
            barFill = barFillImg.rectTransform;
            barFill.anchorMin = Vector2.zero; barFill.anchorMax = new Vector2(0f, 1f);
            barFill.offsetMin = Vector2.zero; barFill.offsetMax = Vector2.zero;
            Sprites.Apply(barFillImg, Sprites.Rounded, 1f);
            statusPct = UIKit.Text(r, "0%", 64, Pal.TextLight, TextAlignmentOptions.MidlineLeft, "Pct");
            Place(statusPct.rectTransform, W - 310, 230, 260, 80);
            statusPct.fontStyle = FontStyles.Bold;
            var cap = UIKit.Text(r, "current batch progress", 30, Pal.Dim, TextAlignmentOptions.Left, "Caption");
            Place(cap.rectTransform, 120, 305, 800, 40);
        }

        static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        IEnumerator Animate(float from, float to, float duration, string head, string sub, Color color)
        {
            float t = 0f;
            while (t < duration)
            {
                t += GameClock.Dt;
                SetProgress(Mathf.Lerp(from, to, t / duration), head, sub, color);
                yield return null;
            }
            SetProgress(to, head, sub, color);
        }

        /// <summary>The ONLY place the percentage is written; all colors come from the state passed in.</summary>
        void SetProgress(float value, string head, string sub, Color color)
        {
            progress = Mathf.Clamp01(value);
            barFill.anchorMax = new Vector2(progress, 1f);
            barFillImg.color = color;
            statusPct.text = Mathf.RoundToInt(progress * 100f) + "%";
            statusHead.text = head;
            statusHead.color = color == Pal.Cyan ? Pal.TextLight : color;
            statusSub.text = sub;
            statusBorder.color = color;
        }
    }
}