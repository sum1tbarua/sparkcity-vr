// Builds world-space VR UI from code: canvases, panels, text, buttons.
// Canvases get a TrackedDeviceGraphicRaycaster so XR controller rays can click them
// (in the Editor, the XR Interaction Simulator drives the same simulated controller rays).
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SparkCity
{
    public static class UIKit
    {
        public static Canvas Canvas(string name, Transform parent, Vector2 sizePx, float metersPerPixel = 0.001f, bool interactive = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            if (Camera.main != null) canvas.worldCamera = Camera.main;
            var rt = (RectTransform)go.transform;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizePx;
            rt.localScale = Vector3.one * metersPerPixel;
            if (interactive) go.AddComponent<TrackedDeviceGraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static Image Image(Transform parent, Color color, string name = "Image")
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.TopLeft, string name = "Text")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.richText = true;
            t.raycastTarget = false;
            if (size >= 56f) Fonts.UseHeading(t); else Fonts.UseBody(t);
            return t;
        }

        public static Button Button(Transform parent, string label, Action onClick, Color bg, float height = 72, float fontSize = 30)
        {
            var rt = Rect("Btn_" + label, parent);
            var go = rt.gameObject;
            var img = go.AddComponent<Image>();
            img.color = bg;
            img.raycastTarget = true;
            Sprites.Apply(img, Sprites.Rounded, 1.1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            // normal is a little dim so hover can clearly brighten it; pressed darkens
            colors.normalColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.pressedColor = new Color(0.62f, 0.62f, 0.62f, 1f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            // light outline that glows on hover + scale feedback
            var ol = Image(rt, new Color(1f, 1f, 1f, 0.22f), "Outline");
            Stretch(ol.rectTransform);
            Sprites.Apply(ol, Sprites.Outline, 1.1f);
            var fx = go.AddComponent<ButtonFx>();
            fx.outline = ol;
            fx.glowColor = Color.Lerp(bg, Color.white, 0.6f);
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleWidth = 1f;
            le.flexibleHeight = 0f;

            var t = Text(rt, label, fontSize, Pal.Text, TextAlignmentOptions.Center, "Label");
            Stretch(t.rectTransform, 10, 10, 4, 4);
            Fonts.UseHeading(t);
            t.enableAutoSizing = true;
            t.fontSizeMin = 14;
            t.fontSizeMax = fontSize;
            return btn;
        }

        /// <summary>Gives a button exact normal / hover colors (instead of tinting one color) and a label color.</summary>
        public static void SetButtonColors(Button b, Color normal, Color hover, Color text)
        {
            if (b == null) return;
            if (b.targetGraphic != null) b.targetGraphic.color = Color.white; // the color block below is the real color
            var cb = b.colors;
            cb.normalColor = normal;
            cb.selectedColor = normal;
            cb.highlightedColor = hover;
            cb.pressedColor = Color.Lerp(normal, Color.black, 0.25f);
            cb.disabledColor = new Color(normal.r, normal.g, normal.b, 0.4f);
            cb.colorMultiplier = 1f;
            b.colors = cb;
            var fx = b.GetComponent<ButtonFx>();
            if (fx != null) fx.glowColor = Color.Lerp(hover, Color.white, 0.5f);
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.color = text;
        }

        public static void SetButtonColor(Button b, Color c)
        {
            if (b != null && b.targetGraphic != null) b.targetGraphic.color = c;
        }

        public static void SetButtonLabel(Button b, string label)
        {
            if (b == null) return;
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = label;
        }

        /// <summary>A free-standing button floating in the world (point + trigger to press).</summary>
        public static Button WorldButton(Transform parent, Vector3 worldPos, Quaternion worldRot, Vector2 sizeMeters,
                                         string label, Color color, Action onClick, float fontSize = 60f)
        {
            var c = Canvas("WorldButton_" + label, parent, sizeMeters * 1000f, 0.001f, true);
            c.transform.SetPositionAndRotation(worldPos, worldRot);
            var b = Button(c.transform, label, onClick, color, sizeMeters.y * 1000f, fontSize);
            UIKit.Stretch((RectTransform)b.transform);
            var le = b.GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = true;
            return b;
        }

        /// <summary>An invisible clickable area in front of a 3D object. It glows faintly when pointed at.</summary>
        public static Button HitBox(Transform parent, Vector3 worldPos, Quaternion worldRot, Vector2 sizeMeters, Action onClick, string name = "HitBox")
        {
            var c = Canvas(name, parent, sizeMeters * 1000f, 0.001f, true);
            c.transform.SetPositionAndRotation(worldPos, worldRot);
            var rt = Rect("Area", c.transform);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 0.6f, 0.22f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            colors.pressedColor = new Color(1f, 1f, 0.6f, 0.4f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            btn.colors = colors;
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        /// <summary>World-space text label (not clickable).</summary>
        public static TextMeshProUGUI WorldLabel(Transform parent, Vector3 worldPos, Quaternion worldRot, Vector2 sizeMeters, string text, float fontSize, Color color, bool background = false)
        {
            var c = Canvas("Label", parent, sizeMeters * 1000f, 0.001f, false);
            c.transform.SetPositionAndRotation(worldPos, worldRot);
            if (background)
            {
                var bg = Image(c.transform, new Color(0.02f, 0.04f, 0.09f, 0.88f), "Bg");
                Stretch(bg.rectTransform);
                Sprites.Apply(bg, Sprites.Rounded, 1.4f);
                var ol = Image(c.transform, new Color(Pal.Cyan.r, Pal.Cyan.g, Pal.Cyan.b, 0.45f), "Outline");
                Stretch(ol.rectTransform);
                Sprites.Apply(ol, Sprites.Outline, 1.4f);
            }
            var t = Text(c.transform, text, fontSize, color, TextAlignmentOptions.Center);
            Stretch(t.rectTransform, 14, 14, 8, 8);
            return t;
        }
    }

    /// <summary>A floating world-space panel with a title, a corner text and a vertical body.</summary>
    public class Panel
    {
        public Canvas canvas;
        public RectTransform root;
        public Image bg;
        public Image accentBar;
        public TextMeshProUGUI title;
        public TextMeshProUGUI corner;
        public RectTransform body;
        public Vector2 size;
        public PanelStyle style;

        public static Panel Create(string name, Transform parent, Vector2 sizePx, bool interactive = true, float headerHeight = 78, float metersPerPixel = 0.001f)
        {
            var p = new Panel();
            p.size = sizePx;
            p.canvas = UIKit.Canvas(name, parent, sizePx, metersPerPixel, interactive);
            p.root = (RectTransform)p.canvas.transform;

            // layers: dark rounded body, soft header glow in the accent color, accent line, glowing border
            p.bg = UIKit.Image(p.root, Pal.Bg, "Background");
            UIKit.Stretch(p.bg.rectTransform);
            Sprites.Apply(p.bg.GetComponent<Image>(), Sprites.Rounded, 1.6f);
            p.bg.raycastTarget = interactive; // blocks rays from passing through the panel

            var glow = UIKit.Image(p.root, new Color(1f, 1f, 1f, 0.2f), "HeaderGlow");
            var gr = glow.rectTransform;
            gr.anchorMin = new Vector2(0, 1); gr.anchorMax = new Vector2(1, 1); gr.pivot = new Vector2(0.5f, 1);
            gr.sizeDelta = new Vector2(0, headerHeight * 2.2f); gr.anchoredPosition = Vector2.zero;
            Sprites.Apply(glow, Sprites.Glow, 1.6f);

            p.accentBar = UIKit.Image(p.root, Pal.Spark, "AccentBar");
            var br = p.accentBar.rectTransform;
            br.anchorMin = new Vector2(0, 1); br.anchorMax = new Vector2(1, 1); br.pivot = new Vector2(0.5f, 1);
            br.offsetMin = new Vector2(40, -headerHeight - 2); br.offsetMax = new Vector2(-40, -headerHeight + 2);

            var border = UIKit.Image(p.root, Pal.Spark, "Border");
            UIKit.Stretch(border.rectTransform);
            Sprites.Apply(border, Sprites.Outline, 1.6f);

            p.style = p.canvas.gameObject.AddComponent<PanelStyle>();
            p.style.accentBar = p.accentBar;
            p.style.border = border;
            p.style.glow = glow;

            p.title = UIKit.Text(p.root, "", 44, Pal.Text, TextAlignmentOptions.Midline, "Title");
            var tr = p.title.rectTransform;
            tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(1, 1); tr.pivot = new Vector2(0.5f, 1);
            tr.offsetMin = new Vector2(230, -headerHeight); tr.offsetMax = new Vector2(-230, -6);
            p.title.fontStyle = FontStyles.Bold;
            p.title.characterSpacing = 4f;
            Fonts.UseHeading(p.title);
            p.title.enableAutoSizing = true; p.title.fontSizeMin = 22; p.title.fontSizeMax = 44;

            p.corner = UIKit.Text(p.root, "", 30, Pal.Spark, TextAlignmentOptions.MidlineRight, "Corner");
            var cr = p.corner.rectTransform;
            cr.anchorMin = new Vector2(1, 1); cr.anchorMax = new Vector2(1, 1); cr.pivot = new Vector2(1, 1);
            cr.offsetMin = new Vector2(-230, -headerHeight); cr.offsetMax = new Vector2(-28, -6);
            p.corner.enableAutoSizing = true; p.corner.fontSizeMin = 16; p.corner.fontSizeMax = 30;

            p.body = UIKit.Rect("Body", p.root);
            UIKit.Stretch(p.body, 40, 40, headerHeight + 16, 30);
            var v = p.body.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 12;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return p;
        }

        public void Clear()
        {
            for (int i = body.childCount - 1; i >= 0; i--)
            {
                var go = body.GetChild(i).gameObject;
                go.SetActive(false);
                UnityEngine.Object.Destroy(go);
            }
        }

        public TextMeshProUGUI AddText(string text, float size = 30, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Top)
        {
            return UIKit.Text(body, text, size, color ?? Pal.Text, align);
        }

        public Button AddButton(string label, Action onClick, Color? bg = null, float height = 72, float fontSize = 30)
        {
            return UIKit.Button(body, label, onClick, bg ?? Pal.Btn, height, fontSize);
        }

        public RectTransform AddRow(float height = 72, float spacing = 12, Transform parent = null)
        {
            var row = UIKit.Rect("Row", parent != null ? parent : body);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleHeight = 0f; // otherwise the row grows to fill the panel
            return row;
        }

        public Button AddRowButton(RectTransform row, string label, Action onClick, Color? bg = null, float fontSize = 28)
        {
            var b = UIKit.Button(row, label, onClick, bg ?? Pal.Btn, 60, fontSize);
            return b;
        }

        public TextMeshProUGUI AddRowText(RectTransform row, string text, float size, float preferredWidth, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var t = UIKit.Text(row, text, size, color ?? Pal.Text, align);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = preferredWidth;
            le.flexibleWidth = 0;
            return t;
        }

        public void AddSpace(float height)
        {
            var r = UIKit.Rect("Space", body);
            var le = r.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
        }

        public void Place(Vector3 worldPos, Quaternion worldRot)
        {
            canvas.transform.SetPositionAndRotation(worldPos, worldRot);
        }

        public void PlaceAt(Anchor a, float right, float up, float forward)
        {
            var p = a.At(right, up, forward);
            Place(p, a.Facing(p));
        }

        public void SetVisible(bool visible) { canvas.gameObject.SetActive(visible); }

        /// <summary>Changes the panel size in pixels (1 px = 1 mm for normal panels). Everything inside follows.</summary>
        public void Resize(Vector2 sizePx)
        {
            size = sizePx;
            root.sizeDelta = sizePx;
        }

        /// <summary>Forces the layout (text wrapping, row sizes) to update right now.</summary>
        public void Rebuild()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(body);
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }
        public bool Visible { get { return canvas != null && canvas.gameObject.activeSelf; } }
    }
}
