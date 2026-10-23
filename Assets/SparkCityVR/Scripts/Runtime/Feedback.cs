// Arcade feedback: generated sound effects, a bouncing "look here" arrow, and floating "+100" popups.
using System.Collections;
using TMPro;
using UnityEngine;

namespace SparkCity
{
    /// <summary>Sound effects synthesized at startup (no audio files needed).</summary>
    public static class Sfx
    {
        static AudioSource source;
        static AudioClip good, bad, click, whoosh, win, zap;
        static AudioClip[] stars;

        static void Init()
        {
            if (source != null) return;
            var go = new GameObject("SparkCity Sfx");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = Volume;
            // soft sine tones only (no harsh square waves), short and quiet so neighbours are not disturbed
            good = Tone("good", new[] { 587f, 880f }, 0.16f, 0.22f, false);
            bad = Tone("bad", new[] { 247f, 196f }, 0.22f, 0.18f, false);
            click = Tone("click", new[] { 880f }, 0.03f, 0.1f, false);
            whoosh = Noise("whoosh", 0.22f, 0.06f);
            win = Tone("win", new[] { 523f, 659f, 784f }, 0.42f, 0.2f, false);
            zap = Tone("zap", new[] { 1046f, 784f, 523f }, 0.16f, 0.16f, false);
            // three rising "ding" notes for the star reveal
            stars = new[] { Tone("star1", new[] { 659f }, 0.14f, 0.18f, false), Tone("star2", new[] { 784f }, 0.14f, 0.18f, false), Tone("star3", new[] { 988f, 1319f }, 0.22f, 0.2f, false) };
        }

        /// <summary>Plays the frequencies one after another (a short arpeggio).</summary>
        static AudioClip Tone(string name, float[] freqs, float duration, float volume, bool square)
        {
            const int rate = 22050;
            int n = Mathf.Max(1, Mathf.RoundToInt(rate * duration));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                int seg = Mathf.Min(freqs.Length - 1, i * freqs.Length / n);
                float s = Mathf.Sin(2f * Mathf.PI * freqs[seg] * t);
                if (square) s = s >= 0 ? 0.6f : -0.6f;
                float env = Mathf.Clamp01(i / (rate * 0.005f)) * Mathf.Clamp01((n - i) / (rate * 0.05f));
                data[i] = s * env * volume;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Noise(string name, float duration, float volume)
        {
            const int rate = 22050;
            int n = Mathf.RoundToInt(rate * duration);
            var data = new float[n];
            var rnd = new System.Random(7);
            float last = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                float env = Mathf.Sin(k * Mathf.PI);
                last = Mathf.Lerp(last, (float)(rnd.NextDouble() * 2 - 1), 0.15f + 0.5f * k);
                data[i] = last * env * volume;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Master volume for all game sounds (0..1). Lower it if the classroom is crowded.</summary>
        public const float Volume = 0.45f;
        static float lastPlay;

        static void Play(AudioClip c)
        {
            Init();
            if (c == null) return;
            if (Time.unscaledTime - lastPlay < 0.06f) return; // never stack many sounds at once
            lastPlay = Time.unscaledTime;
            source.PlayOneShot(c);
        }
        public static void Good() { Init(); Play(good); }
        public static void Bad() { Init(); Play(bad); }
        public static void Click() { Init(); Play(click); }
        public static void Whoosh() { Init(); Play(whoosh); }
        public static void Win() { Init(); Play(win); }
        public static void Zap() { Init(); Play(zap); }
        /// <summary>Star reveal note: 0, 1, 2 = rising pitch.</summary>
        public static void Star(int i) { Init(); Play(stars[Mathf.Clamp(i, 0, stars.Length - 1)]); }
    }

    /// <summary>A bouncing yellow arrow that points at what the player should look at next.</summary>
    public class Cue : MonoBehaviour
    {
        static Cue instance;
        Vector3 target;
        bool showing;

        static Cue Get()
        {
            if (instance != null) return instance;
            var go = new GameObject("SparkCity Cue");
            instance = go.AddComponent<Cue>();
            var mat = Mat.Glow(new Color(1f, 0.85f, 0.1f), 2.2f);
            // a small downward arrow: a "V" head and a thin stem (it always faces the player)
            var left = Fx.Prim(PrimitiveType.Cube, "HeadL", go.transform, new Vector3(-0.018f, 0.022f, 0f), new Vector3(0.012f, 0.055f, 0.012f), mat);
            left.transform.localRotation = Quaternion.Euler(0f, 0f, 40f);
            var right = Fx.Prim(PrimitiveType.Cube, "HeadR", go.transform, new Vector3(0.018f, 0.022f, 0f), new Vector3(0.012f, 0.055f, 0.012f), mat);
            right.transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
            Fx.Prim(PrimitiveType.Cube, "Stem", go.transform, new Vector3(0f, 0.065f, 0f), new Vector3(0.012f, 0.07f, 0.012f), mat);
            go.SetActive(false);
            return instance;
        }

        /// <summary>Show the arrow just above a world position.</summary>
        public static void Show(Vector3 worldPos, float above = 0.12f)
        {
            var c = Get();
            c.target = worldPos + Vector3.up * above;
            c.showing = true;
            c.gameObject.SetActive(true);
            c.transform.position = c.target;
        }

        public static void Hide()
        {
            if (instance == null) return;
            instance.showing = false;
            instance.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!showing) return;
            transform.position = target + Vector3.up * (0.04f * Mathf.Abs(Mathf.Sin(Time.time * 4f)));
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 d = transform.position - cam.transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
                // stay readable when the target is far away (e.g. executor towers)
                float dist = Vector3.Distance(transform.position, cam.transform.position);
                transform.localScale = Vector3.one * Mathf.Clamp(dist / 1.3f, 1f, 3.5f);
            }
        }
    }

    /// <summary>Floating text that rises and fades (e.g. "+100").</summary>
    public class Popup : MonoBehaviour
    {
        TextMeshProUGUI text;
        float life;
        Color color;

        public static void Show(Vector3 worldPos, string message, Color color, float size = 90f)
        {
            var cam = Camera.main;
            var canvas = UIKit.Canvas("Popup", null, new Vector2(900, 200), 0.001f, false);
            canvas.transform.position = worldPos;
            if (cam != null)
            {
                Vector3 d = worldPos - cam.transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.0001f) canvas.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            }
            var p = canvas.gameObject.AddComponent<Popup>();
            p.text = UIKit.Text(canvas.transform, message, size, color, TextAlignmentOptions.Center);
            UIKit.Stretch(p.text.rectTransform);
            p.text.fontStyle = FontStyles.Bold;
            p.color = color;
        }

        void Update()
        {
            life += Time.deltaTime;
            transform.position += Vector3.up * 0.35f * Time.deltaTime;
            float a = Mathf.Clamp01(1.6f - life);
            text.color = new Color(color.r, color.g, color.b, a);
            if (life > 1.6f) Destroy(gameObject);
        }
    }

    public static class Anim
    {
        /// <summary>Quick side-to-side shake ("no!").</summary>
        public static IEnumerator Shake(Transform t, float amount = 0.03f, float duration = 0.35f)
        {
            if (t == null) yield break;
            Vector3 start = t.position;
            float time = 0f;
            Vector3 side = t.right;
            while (time < duration)
            {
                if (t == null) yield break;
                time += GameClock.Dt;
                t.position = start + side * Mathf.Sin(time * 60f) * amount * (1f - time / duration);
                yield return null;
            }
            if (t != null) t.position = start;
        }

        /// <summary>Grow and vanish ("zapped!").</summary>
        public static IEnumerator Burst(Transform t, float duration = 0.3f)
        {
            if (t == null) yield break;
            Vector3 s0 = t.localScale;
            float time = 0f;
            while (time < duration)
            {
                if (t == null) yield break;
                time += GameClock.Dt;
                float k = time / duration;
                t.localScale = s0 * (1f + k * 0.8f) * (1f - k);
                yield return null;
            }
            if (t != null) t.gameObject.SetActive(false);
        }
    }
}

namespace SparkCity
{
    /// <summary>Comfort "blink" for VR: the view goes black, the player is moved, then the view fades back in.
    /// Optionally shows a short label ("Next: Clean-Up Blaster") while black.</summary>
    public class ScreenFader : MonoBehaviour
    {
        static ScreenFader instance;
        UnityEngine.UI.Image black;
        TextMeshProUGUI label;
        Coroutine running;

        static ScreenFader Get()
        {
            var cam = Camera.main;
            if (cam == null) return null;
            if (instance != null && instance.transform.parent == cam.transform) return instance;
            if (instance != null) Destroy(instance.gameObject);
            var canvas = UIKit.Canvas("ScreenFader", cam.transform, new Vector2(4000, 4000), 0.001f, false);
            canvas.transform.localPosition = new Vector3(0f, 0f, 0.3f); // just in front of the eyes
            canvas.transform.localRotation = Quaternion.identity;
            canvas.sortingOrder = 1000;
            instance = canvas.gameObject.AddComponent<ScreenFader>();
            instance.black = UIKit.Image(canvas.transform, Color.black, "Black");
            UIKit.Stretch(instance.black.rectTransform);
            // the "Next: ..." label sits 1.5 m in front of the eyes (not on the black layer 0.3 m away, which is
            // too close to focus on); its own canvas draws on top of the black layer
            var lc = UIKit.Canvas("FaderLabel", canvas.transform, new Vector2(900, 160), 1.5f, false);
            lc.transform.localPosition = new Vector3(0f, 0f, 1200f); // (1.5 m - 0.3 m) / 0.001 per unit
            lc.transform.localRotation = Quaternion.identity;
            lc.overrideSorting = true;
            lc.sortingOrder = 1001;
            instance.label = UIKit.Text(lc.transform, "", 46, Pal.Text, TextAlignmentOptions.Center, "Label");
            var rt = instance.label.rectTransform;
            UIKit.Stretch(rt);
            instance.label.fontStyle = FontStyles.Bold;
            instance.SetAlpha(0f);
            return instance;
        }

        void SetAlpha(float a)
        {
            black.color = new Color(0f, 0f, 0f, a);
            label.color = new Color(Pal.Text.r, Pal.Text.g, Pal.Text.b, a);
            gameObject.SetActive(a > 0.001f);
        }

        /// <summary>Instantly black (so the jump is never seen), hold, then fade back in.</summary>
        public static void Blink(string text = "", float hold = 0.25f)
        {
            var f = Get();
            if (f == null) return;
            f.label.text = text ?? "";
            f.SetAlpha(1f);
            if (f.running != null) f.StopCoroutine(f.running);
            f.running = f.StartCoroutine(f.FadeIn(string.IsNullOrEmpty(text) ? hold : Mathf.Max(hold, 0.9f)));
        }

        /// <summary>Instantly black and stays black until the next Blink (used while the game loads).</summary>
        public static void Hold()
        {
            var f = Get();
            if (f == null) return;
            if (f.running != null) { f.StopCoroutine(f.running); f.running = null; }
            f.label.text = "";
            f.SetAlpha(1f);
        }

        IEnumerator FadeIn(float hold)
        {
            float t = 0f;
            while (t < hold) { t += Time.unscaledDeltaTime; yield return null; }
            const float dur = 0.45f;
            t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(1f - Mathf.SmoothStep(0f, 1f, t / dur));
                yield return null;
            }
            SetAlpha(0f);
            running = null;
        }
    }
}