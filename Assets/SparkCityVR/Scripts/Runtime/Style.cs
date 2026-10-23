// Visual style: generated rounded/glow sprites, fonts, button hover/press effects and panel accents.
// No image files are needed - the sprites are drawn into small textures at startup.
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SparkCity
{
    /// <summary>Procedurally generated UI sprites (9-sliced, so they scale to any size).</summary>
    public static class Sprites
    {
        static Sprite rounded, outline, glow, star;
        const int Size = 64;
        const int Radius = 22;

        public static Sprite Rounded { get { if (rounded == null) rounded = Make(false, false); return rounded; } }
        public static Sprite Outline { get { if (outline == null) outline = Make(true, false); return outline; } }
        /// <summary>Rounded shape whose alpha fades from top (1) to bottom (0): used as a soft header glow.</summary>
        public static Sprite Glow { get { if (glow == null) glow = Make(false, true); return glow; } }
        /// <summary>A filled five-pointed star (anti-aliased), for score stars.</summary>
        public static Sprite Star { get { if (star == null) star = MakeStar(); return star; } }

        static Sprite MakeStar()
        {
            const int S = 128;
            const float R = 60f, r = R * 0.45f;
            float seg = 2f * Mathf.PI / 5f, half = seg / 2f;
            var p1 = new Vector2(0f, R);                                    // outer point
            var p2 = new Vector2(r * Mathf.Sin(half), r * Mathf.Cos(half)); // inner corner
            var e = p2 - p1;
            float len = e.magnitude;
            float originSide = Mathf.Sign(e.x * (0f - p1.y) - e.y * (0f - p1.x));
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    var v = new Vector2(x + 0.5f - S / 2f, y + 0.5f - S / 2f);
                    float ang = Mathf.Atan2(v.x, v.y);            // 0 = straight up
                    if (ang < 0) ang += 2f * Mathf.PI;
                    float a = ang % seg;
                    if (a > half) a = seg - a;                    // mirror inside each point
                    float d = v.magnitude;
                    var p = new Vector2(d * Mathf.Sin(a), d * Mathf.Cos(a));
                    float signed = (e.x * (p.y - p1.y) - e.y * (p.x - p1.x)) / len * originSide; // > 0 inside
                    float alpha = Mathf.Clamp01(signed + 0.5f);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite Make(bool ring, bool gradient)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // signed distance to a rounded square
                    float cx = Mathf.Clamp(x + 0.5f, Radius, Size - Radius);
                    float cy = Mathf.Clamp(y + 0.5f, Radius, Size - Radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - Radius;
                    float a = Mathf.Clamp01(0.5f - d);                 // inside = 1, soft 1px edge
                    if (ring) a *= Mathf.Clamp01(d + 3.5f);            // keep only a 3 px border
                    if (gradient) a *= Mathf.Clamp01((float)y / (Size - 1)); // top bright, bottom clear
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var border = new Vector4(Radius + 2, Radius + 2, Radius + 2, Radius + 2);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>Applies a rounded 9-sliced sprite. cornerScale > 1 makes the corners larger.</summary>
        public static void Apply(Image img, Sprite s, float cornerScale = 1.6f)
        {
            img.sprite = s;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.1f, cornerScale);
        }
    }

    /// <summary>Fonts for headings and body text.
    /// Put Heading.ttf and Body.ttf in Assets/SparkCityVR/Resources/SparkCityFonts/ and run
    /// "Spark City VR > Create Font Assets" (makes "Heading SDF" / "Body SDF"). If the font assets
    /// are missing, they are created from the .ttf files at runtime instead.</summary>
    public static class Fonts
    {
        static bool loaded;
        static TMP_FontAsset heading, body;

        public static TMP_FontAsset Heading { get { Load(); return heading; } }
        public static TMP_FontAsset Body { get { Load(); return body; } }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            heading = Find("Heading");
            body = Find("Body");
            if (heading == null) heading = body;
            if (body == null) body = heading;
            if (heading != null) Debug.Log("[SparkCity] Using custom fonts: " + heading.name + " / " + (body != null ? body.name : "-"));
        }

        static TMP_FontAsset Find(string name)
        {
            var fa = Resources.Load<TMP_FontAsset>("SparkCityFonts/" + name + " SDF");
            if (fa != null) return fa;
            var font = Resources.Load<Font>("SparkCityFonts/" + name);
            if (font == null) return null;
            try
            {
                fa = TMP_FontAsset.CreateFontAsset(font);
                if (fa != null) fa.name = name + " SDF (runtime)";
                return fa;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SparkCity] Could not create a font asset from " + name + ".ttf: " + e.Message);
                return null;
            }
        }

        public static void UseHeading(TMP_Text t) { if (t != null && Heading != null) t.font = Heading; }
        public static void UseBody(TMP_Text t) { if (t != null && Body != null) t.font = Body; }
    }

    /// <summary>Hover / press feedback for buttons: grows slightly and lights its outline.</summary>
    public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image outline;
        public Color glowColor = Color.white;
        float target = 1f, scale = 1f;
        bool hover;

        public void OnPointerEnter(PointerEventData e) { hover = true; target = 1.05f; }
        public void OnPointerExit(PointerEventData e) { hover = false; target = 1f; }
        public void OnPointerDown(PointerEventData e) { target = 0.95f; }
        public void OnPointerUp(PointerEventData e) { target = hover ? 1.05f : 1f; }

        void OnDisable() { hover = false; target = 1f; scale = 1f; transform.localScale = Vector3.one; }

        void Update()
        {
            scale = Mathf.MoveTowards(scale, target, Time.unscaledDeltaTime * 1.2f);
            transform.localScale = new Vector3(scale, scale, 1f);
            if (outline != null)
            {
                var c = glowColor;
                c.a = hover ? 0.95f : 0.22f;
                outline.color = c;
            }
        }
    }

    /// <summary>Keeps a panel's border and header glow in its accent color; pulses only when urgent.</summary>
    public class PanelStyle : MonoBehaviour
    {
        public Image accentBar, border, glow;
        public bool urgent;
        /// <summary>Strength of the colored glow behind the header (0 = plain navy header).</summary>
        public float glowAlpha = 0.2f;
        float flashUntil;

        /// <summary>Short red flash (e.g. after a wrong answer).</summary>
        public void Flash(float seconds = 0.6f) { flashUntil = Time.unscaledTime + seconds; }

        void LateUpdate()
        {
            if (accentBar == null) return;
            Color accent = accentBar.color;
            bool flashing = Time.unscaledTime < flashUntil;
            if (urgent || flashing)
            {
                float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (flashing ? 18f : 5f));
                Color red = Pal.Bad;
                if (border != null) border.color = new Color(red.r, red.g, red.b, 0.45f + 0.55f * p);
                if (glow != null) glow.color = new Color(red.r, red.g, red.b, 0.12f + 0.18f * p);
            }
            else
            {
                if (border != null) border.color = new Color(accent.r, accent.g, accent.b, 0.85f);
                if (glow != null) glow.color = new Color(accent.r, accent.g, accent.b, glowAlpha);
            }
        }
    }
}
