// Materials, colors, small tween helpers and the Anchor (a place where the player stands).
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SparkCity
{
    /// <summary>Shared color palette.</summary>
    public static class Pal
    {
        public static readonly Color Bg = new Color(0.04f, 0.06f, 0.11f, 0.97f);
        public static readonly Color Bg2 = new Color(0.09f, 0.12f, 0.19f, 1f);
        public static readonly Color Btn = new Color(0.16f, 0.21f, 0.31f, 1f);
        public static readonly Color Spark = new Color(0.886f, 0.353f, 0.110f, 1f); // Apache Spark orange
        public static readonly Color Cyan = new Color(0.22f, 0.74f, 0.97f, 1f);
        public static readonly Color Good = new Color(0.13f, 0.77f, 0.37f, 1f);
        public static readonly Color Warn = new Color(0.92f, 0.70f, 0.03f, 1f);
        public static readonly Color Bad = new Color(0.94f, 0.27f, 0.27f, 1f);
        public static readonly Color Text = new Color(0.90f, 0.92f, 0.95f, 1f);
        public static readonly Color Dim = new Color(0.58f, 0.64f, 0.72f, 1f);
        public static readonly Color Idle = new Color(0.25f, 0.32f, 0.42f, 1f);
        // neon palette matching the welcome artwork (pink / cyan / purple city at dusk)
        public static readonly Color NeonPink = new Color(1f, 0.25f, 0.64f, 1f);
        public static readonly Color NeonCyan = new Color(0.18f, 0.9f, 1f, 1f);
        public static readonly Color NeonPurple = new Color(0.45f, 0.2f, 0.75f, 1f);
        public static readonly Color NeonBg = new Color(0.07f, 0.03f, 0.12f, 0.86f);

        // ---- WELCOME SCREEN THEME (dark navy / cyan / amber) - edit these to restyle the welcome + tutorial panel
        public static readonly Color WelcomeBg = Rgb("#091322", 0.92f);     // panel background, 92% opacity
        public static readonly Color WelcomeBorder = Rgb("#32D9ED");        // cyan border + header line
        public static readonly Color WelcomeTitle = Rgb("#FFB347");         // SPARK CITY title (amber)
        public static readonly Color EnterNormal = Rgb("#F5A623");          // Enter the Game
        public static readonly Color EnterHover = Rgb("#FFD166");
        public static readonly Color TutorialNormal = Rgb("#176B87");       // Tutorial
        public static readonly Color TutorialHover = Rgb("#249AB5");
        public static readonly Color ExitNormal = Rgb("#613443");           // Exit
        public static readonly Color ExitHover = Rgb("#945064");
        public static readonly Color TextOnAmber = Rgb("#091322");          // dark navy text on the amber button
        public static readonly Color TextLight = Rgb("#F2F6FF");            // near-white text everywhere else

        // ---- MISSION BOARD THEME (uses the colors above, plus these)
        public static readonly Color BoardBg = Rgb("#091322", 0.95f);       // 95% so the map labels don't show through
        public static readonly Color NavyBtn = Rgb("#1B2A40");              // small buttons (Player ID, Seated, Recenter)
        public static readonly Color NavyBtnHover = Rgb("#2B4566");

        public static Color Rgb(string hex, float alpha = 1f)
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString(hex, out c)) c = Color.magenta;
            c.a = alpha;
            return c;
        }

        public static string Hex(Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }
        public static string Col(string s, Color c) { return "<color=" + Hex(c) + ">" + s + "</color>"; }
        /// <summary>Wraps code so TextMeshPro does not treat "&lt;" as a rich-text tag.</summary>
        public static string Code(string s) { return "<noparse>" + s + "</noparse>"; }
    }

    /// <summary>Material factory. Uses material assets created by the setup tool (so the
    /// shaders are included in the Android build), with a Shader.Find fallback.</summary>
    public static class Mat
    {
        static Material litBase, glowBase, unlitBase;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        static void Init()
        {
            if (litBase != null) return;
            litBase = Resources.Load<Material>("SparkCityMaterials/SC_Lit");
            glowBase = Resources.Load<Material>("SparkCityMaterials/SC_Glow");
            unlitBase = Resources.Load<Material>("SparkCityMaterials/SC_Unlit");
            if (litBase == null)
            {
                Debug.LogWarning("[SparkCity] Material assets missing - run 'Spark City VR > Build Game Scene'. Using Shader.Find fallback.");
                litBase = new Material(FindShader("Universal Render Pipeline/Lit", "Standard"));
            }
            if (glowBase == null)
            {
                glowBase = new Material(litBase);
                glowBase.EnableKeyword("_EMISSION");
            }
            if (unlitBase == null) unlitBase = new Material(FindShader("Universal Render Pipeline/Unlit", "Unlit/Color"));
        }

        static Shader FindShader(params string[] names)
        {
            foreach (var n in names)
            {
                var s = Shader.Find(n);
                if (s != null) return s;
            }
            return Shader.Find("Sprites/Default");
        }

        static string Key(string kind, Color c, float f)
        {
            return kind + ColorUtility.ToHtmlStringRGBA(c) + f.ToString("0.00");
        }

        static void SetBase(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        public static Material Lit(Color c, float smoothness = 0.25f)
        {
            Init();
            var k = Key("L", c, smoothness);
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(litBase);
            SetBase(m, c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            cache[k] = m;
            return m;
        }

        public static Material Glow(Color c, float intensity = 1.5f)
        {
            Init();
            var k = Key("G", c, intensity);
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(glowBase);
            SetBase(m, c * 0.6f);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * intensity);
            cache[k] = m;
            return m;
        }

        public static Material Unlit(Color c)
        {
            Init();
            var k = Key("U", c, 0);
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(unlitBase);
            SetBase(m, c);
            cache[k] = m;
            return m;
        }

        /// <summary>Recolor a glowing renderer (uses the shared cache, so no per-frame allocations).</summary>
        public static void SetGlow(Renderer r, Color c, float intensity = 1.5f)
        {
            if (r != null) r.sharedMaterial = Glow(c, Mathf.Round(intensity * 10f) / 10f);
        }
    }

    /// <summary>Game time that stops while a help card is open, so no demo or timer runs on unseen.</summary>
    public static class GameClock
    {
        public static bool Paused;
        public static float Dt { get { return Paused ? 0f : Time.deltaTime; } }

        /// <summary>Like WaitForSeconds, but does not count paused time.</summary>
        public static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Dt;
                yield return null;
            }
        }
    }

    /// <summary>Small helpers for building and animating primitive objects.</summary>
    public static class Fx
    {
        public static Color Hex(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            if (!hex.StartsWith("#")) hex = "#" + hex;
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            if (mat != null) r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            return go;
        }

        public static GameObject Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public static IEnumerator MoveTo(Transform t, Vector3 target, float duration, float arcHeight = 0f)
        {
            if (t == null) yield break;
            Vector3 start = t.position;
            float time = 0f;
            while (time < duration)
            {
                if (t == null) yield break;
                time += GameClock.Dt;
                float k = Mathf.SmoothStep(0f, 1f, time / duration);
                Vector3 p = Vector3.Lerp(start, target, k);
                p.y += Mathf.Sin(k * Mathf.PI) * arcHeight;
                t.position = p;
                yield return null;
            }
            if (t != null) t.position = target;
        }

        public static IEnumerator ScaleTo(Transform t, Vector3 target, float duration)
        {
            if (t == null) yield break;
            Vector3 start = t.localScale;
            float time = 0f;
            while (time < duration)
            {
                if (t == null) yield break;
                time += GameClock.Dt;
                t.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, time / duration));
                yield return null;
            }
            if (t != null) t.localScale = target;
        }

        public static string Clock(float seconds)
        {
            bool neg = seconds < 0;
            seconds = Mathf.Abs(seconds);
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return (neg ? "-" : "") + m.ToString("00") + ":" + s.ToString("00");
        }

        public static string Bar(float value01, int width = 20)
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(value01 * width), 0, width);
            return new string('|', n) + new string('.', width - n);
        }
    }

    /// <summary>A standing spot for the player: position on the floor + facing (yaw).</summary>
    public struct Anchor
    {
        public Vector3 pos;
        public float yaw;

        public Anchor(Vector3 p, float yawDegrees) { pos = p; yaw = yawDegrees; }

        public Quaternion Rot { get { return Quaternion.Euler(0f, yaw, 0f); } }
        public Vector3 Fwd { get { return Rot * Vector3.forward; } }
        public Vector3 Right { get { return Rot * Vector3.right; } }

        /// <summary>World point relative to the player: right, up (from floor), forward.</summary>
        public Vector3 At(float right, float up, float forward)
        {
            return pos + Right * right + Vector3.up * up + Fwd * forward;
        }

        /// <summary>Rotation for a world-space canvas at 'world' so it faces the player.</summary>
        public Quaternion Facing(Vector3 world)
        {
            Vector3 d = world - pos;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) d = Fwd;
            return Quaternion.LookRotation(d.normalized, Vector3.up);
        }
    }
}
