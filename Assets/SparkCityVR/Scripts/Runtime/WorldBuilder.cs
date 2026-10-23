// Builds the static environments from primitives at runtime:
//  - City Command Center (map table + 3 stations) around the origin
//  - Spark Cluster room far away at z = 60
// Kept deliberately low-poly for Quest 3S performance (unlit/emissive look, no shadows).
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace SparkCity
{
    public static class Layout
    {
        // Where the player stands for each step (position on the floor, yaw in degrees).
        public static readonly Anchor Brief = new Anchor(new Vector3(0f, 0f, -2.4f), 0f);
        public static readonly Anchor Sources = new Anchor(new Vector3(-5f, 0f, 0.5f), -90f);
        public static readonly Anchor Clean = new Anchor(new Vector3(5f, 0f, 0.5f), 90f);
        public static readonly Anchor Pipeline = new Anchor(new Vector3(0f, 0f, 3.6f), 0f);
        public static readonly Anchor Cluster = new Anchor(new Vector3(0f, 0f, 60f), 0f);
        public static readonly Anchor Welcome = new Anchor(new Vector3(0f, 0f, -60f), 0f);

        public static Vector3 MapCenter { get { return Brief.At(0.3f, 0.92f, 1.75f); } }
    }

    public static class WorldBuilder
    {
        public static Transform BuildCommandCenter(Transform parent)
        {
            var root = Fx.Group("CommandCenter", parent).transform;
            var floorMat = Mat.Lit(new Color(0.07f, 0.09f, 0.13f), 0.5f);
            var wallMat = Mat.Lit(new Color(0.10f, 0.13f, 0.19f));
            var trim = Mat.Glow(Pal.Cyan, 1.0f);
            var spark = Mat.Glow(Pal.Spark, 1.2f);

            // floor (keeps its collider: teleport target)
            var floor = Fx.Prim(PrimitiveType.Cube, "Floor", root, new Vector3(0, -0.05f, 1f), new Vector3(16f, 0.1f, 14f), floorMat, true);
            MakeTeleportable(floor);
            // floor grid strips
            for (int i = -7; i <= 7; i++)
            {
                Fx.Prim(PrimitiveType.Cube, "GridX", root, new Vector3(i, 0.002f, 1f), new Vector3(0.012f, 0.002f, 14f), Mat.Glow(new Color(0.1f, 0.25f, 0.4f), 0.6f));
            }
            for (int i = -6; i <= 8; i++)
            {
                Fx.Prim(PrimitiveType.Cube, "GridZ", root, new Vector3(0, 0.002f, i), new Vector3(16f, 0.002f, 0.012f), Mat.Glow(new Color(0.1f, 0.25f, 0.4f), 0.6f));
            }
            // walls
            // walls are 5.2 m tall, so the big title sits ABOVE the station signs instead of behind them
            Fx.Prim(PrimitiveType.Cube, "WallN", root, new Vector3(0, 2.6f, 8f), new Vector3(16f, 5.2f, 0.2f), wallMat);
            Fx.Prim(PrimitiveType.Cube, "WallS", root, new Vector3(0, 2.6f, -6f), new Vector3(16f, 5.2f, 0.2f), wallMat);
            Fx.Prim(PrimitiveType.Cube, "WallE", root, new Vector3(8f, 2.6f, 1f), new Vector3(0.2f, 5.2f, 14f), wallMat);
            Fx.Prim(PrimitiveType.Cube, "WallW", root, new Vector3(-8f, 2.6f, 1f), new Vector3(0.2f, 5.2f, 14f), wallMat);
            // glowing trims
            Fx.Prim(PrimitiveType.Cube, "TrimN", root, new Vector3(0, 3.2f, 7.88f), new Vector3(16f, 0.05f, 0.02f), trim);
            Fx.Prim(PrimitiveType.Cube, "TrimS", root, new Vector3(0, 3.2f, -5.88f), new Vector3(16f, 0.05f, 0.02f), trim);
            Fx.Prim(PrimitiveType.Cube, "TrimE", root, new Vector3(7.88f, 3.2f, 1f), new Vector3(0.02f, 0.05f, 14f), trim);
            Fx.Prim(PrimitiveType.Cube, "TrimW", root, new Vector3(-7.88f, 3.2f, 1f), new Vector3(0.02f, 0.05f, 14f), trim);
            // decorative wall screens
            for (int i = 0; i < 5; i++)
            {
                float x = -5f + i * 2.5f;
                Fx.Prim(PrimitiveType.Cube, "Screen", root, new Vector3(x, 2.4f, 7.85f), new Vector3(2f, 1.1f, 0.03f), Mat.Glow(new Color(0.05f, 0.18f, 0.3f), 0.8f));
            }
            // ceiling light bars
            // ceiling closes the room (no bright sky showing above the walls)
            Fx.Prim(PrimitiveType.Cube, "Ceiling", root, new Vector3(0, 5.25f, 1f), new Vector3(16.4f, 0.1f, 14.4f), Mat.Lit(new Color(0.05f, 0.06f, 0.09f)));
            // (3 bars only - a 4th one near the north wall would cut across the title)
            for (int i = 0; i < 3; i++)
                Fx.Prim(PrimitiveType.Cube, "CeilingLight", root, new Vector3(0, 4.6f, -3f + i * 3f), new Vector3(10f, 0.05f, 0.25f), Mat.Glow(new Color(0.8f, 0.9f, 1f), 1.4f));

            BuildTitlePlate(root, new Vector3(0f, 4.4f, 7.86f), Quaternion.identity, "SPARK CITY", "COMMAND CENTER");

            // station platforms + signs
            StationDeck(root, Layout.Sources, "1  DATA SORTER", Pal.Cyan, spark);
            StationDeck(root, Layout.Clean, "2  CLEAN-UP", Pal.Warn, spark);
            // station 3 stands right in front of the title wall: its sign hangs lower and closer, so it never covers the title
            StationDeck(root, Layout.Pipeline, "3  LAZY PIPELINE", Pal.Spark, spark, 2.75f, 2.4f);
            StationDeck(root, Layout.Brief, "MISSION CONTROL", Pal.Good, spark);

            // simple tables under the mini-game props
            var c = Layout.Clean;
            Furniture(root, c.At(0, 0.84f, 0.88f), c.Rot, new Vector3(1.7f, 0.04f, 0.3f), "ToolTable");
            var p = Layout.Pipeline;
            Furniture(root, p.At(0, 1.3f, 1.05f), p.Rot, new Vector3(1.75f, 0.26f, 0.04f), "RackBack");
            Furniture(root, p.At(0, 0.88f, 0.66f), p.Rot, new Vector3(1.35f, 0.04f, 0.36f), "Tray");
            return root;
        }

        public static Transform BuildClusterRoom(Transform parent)
        {
            var a = Layout.Cluster;
            var root = Fx.Group("SparkCluster", parent).transform;
            var floor = Fx.Prim(PrimitiveType.Cube, "ClusterFloor", root, a.At(0, -0.05f, 4f), new Vector3(18f, 0.1f, 16f), Mat.Lit(new Color(0.04f, 0.05f, 0.08f)), true);
            MakeTeleportable(floor);
            var grid = Mat.Glow(new Color(0.12f, 0.07f, 0.04f), 0.35f); // subtle - the game objects should stand out, not the floor
            for (int i = -8; i <= 8; i++)
                Fx.Prim(PrimitiveType.Cube, "GridX", root, a.At(i, 0.002f, 4f), new Vector3(0.012f, 0.002f, 16f), grid);
            for (int i = -4; i <= 12; i++)
                Fx.Prim(PrimitiveType.Cube, "GridZ", root, a.At(0, 0.002f, i), new Vector3(18f, 0.002f, 0.012f), grid);
            var wall = Mat.Lit(new Color(0.06f, 0.07f, 0.11f));
            Fx.Prim(PrimitiveType.Cube, "WallN", root, a.At(0, 2.5f, 12f), new Vector3(18f, 5f, 0.2f), wall);
            Fx.Prim(PrimitiveType.Cube, "WallS", root, a.At(0, 2.5f, -4f), new Vector3(18f, 5f, 0.2f), wall);
            Fx.Prim(PrimitiveType.Cube, "WallE", root, a.At(9f, 2.5f, 4f), new Vector3(0.2f, 5f, 16f), wall);
            Fx.Prim(PrimitiveType.Cube, "WallW", root, a.At(-9f, 2.5f, 4f), new Vector3(0.2f, 5f, 16f), wall);
            for (int i = 0; i < 3; i++)
                Fx.Prim(PrimitiveType.Cube, "CeilingLight", root, a.At(0, 4.9f, i * 4f), new Vector3(12f, 0.05f, 0.25f), Mat.Glow(new Color(1f, 0.8f, 0.6f), 1.2f));

            // ceiling closes the room; no big wall sign here (it competed with the HUD during play)
            Fx.Prim(PrimitiveType.Cube, "ClusterCeiling", root, a.At(0, 5.05f, 4f), new Vector3(18.4f, 0.1f, 16.4f), Mat.Lit(new Color(0.04f, 0.05f, 0.08f)));

            StationDeck(root, a, "CLUSTER CONSOLE", Pal.Spark, Mat.Glow(Pal.Spark, 1.2f));
            Furniture(root, a.At(0, 0.9f, 0.9f), a.Rot, new Vector3(2.3f, 0.04f, 0.3f), "ControlTable");
            return root;
        }

        /// <summary>The welcome space: an open platform under a night sky with a glowing city skyline.
        /// The welcome artwork (if any) is added on top by SparkCityGame.</summary>
        public static Transform BuildWelcomeSpace(Transform parent)
        {
            var a = Layout.Welcome;
            var root = Fx.Group("WelcomeSpace", parent).transform;
            var floor = Fx.Prim(PrimitiveType.Cylinder, "WelcomeFloor", root, a.At(0, -0.05f, 0), new Vector3(9f, 0.05f, 9f), Mat.Lit(new Color(0.05f, 0.06f, 0.1f), 0.6f), true);
            MakeTeleportable(floor);
            Fx.Prim(PrimitiveType.Cylinder, "Ring1", root, a.At(0, 0.004f, 0), new Vector3(3.2f, 0.004f, 3.2f), Mat.Glow(Pal.Spark, 1.4f));
            Fx.Prim(PrimitiveType.Cylinder, "Ring1Inner", root, a.At(0, 0.008f, 0), new Vector3(3.05f, 0.004f, 3.05f), Mat.Lit(new Color(0.05f, 0.06f, 0.1f), 0.6f));
            Fx.Prim(PrimitiveType.Cylinder, "Ring2", root, a.At(0, 0.004f, 0), new Vector3(8.9f, 0.004f, 8.9f), Mat.Glow(Pal.Cyan, 0.9f));
            Fx.Prim(PrimitiveType.Cylinder, "Ring2Inner", root, a.At(0, 0.008f, 0), new Vector3(8.75f, 0.004f, 8.75f), Mat.Lit(new Color(0.05f, 0.06f, 0.1f), 0.6f));
            // (no skyline blocks and no floating title sign: the artwork is the backdrop,
            //  and the SPARK CITY title is inside the welcome panel)
            return root;
        }

        static void StationDeck(Transform root, Anchor a, string label, Color color, Material ring, float signHeight = 3.5f, float signForward = 3.2f)
        {
            Fx.Prim(PrimitiveType.Cylinder, "Deck", root, a.At(0, 0.012f, 0), new Vector3(1.4f, 0.006f, 1.4f), Mat.Lit(new Color(0.12f, 0.15f, 0.22f)));
            var ringGo = Fx.Prim(PrimitiveType.Cylinder, "DeckRing", root, a.At(0, 0.006f, 0), new Vector3(1.5f, 0.004f, 1.5f), Mat.Glow(color, 1.2f));
            ringGo.name = "DeckRing";
            var c = UIKit.Canvas("Sign_" + label, root, new Vector2(1300, 170), 0.001f, false);
            var pos = a.At(0, signHeight, signForward);
            c.transform.SetPositionAndRotation(pos, a.Facing(pos));
            // navy plate with a border in the station color
            var bg = UIKit.Image(c.transform, Pal.Rgb("#091322", 0.9f), "Plate");
            UIKit.Stretch(bg.rectTransform);
            Sprites.Apply(bg, Sprites.Rounded, 1.4f);
            var ol = UIKit.Image(c.transform, color, "Border");
            UIKit.Stretch(ol.rectTransform);
            Sprites.Apply(ol, Sprites.Outline, 1.4f);
            // "1  DATA SORTER" -> number in the station color, name in white
            string text = label;
            int sp = label.IndexOf(' ');
            if (sp > 0 && char.IsDigit(label[0])) text = Pal.Col(label.Substring(0, sp), color) + "   " + label.Substring(sp).Trim();
            var t = UIKit.Text(c.transform, text, 96, Pal.TextLight, TextAlignmentOptions.Center);
            UIKit.Stretch(t.rectTransform, 30, 30, 8, 8);
            t.fontStyle = FontStyles.Bold;
            t.characterSpacing = 8f;
            t.enableAutoSizing = true; t.fontSizeMin = 40; t.fontSizeMax = 96;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            Fonts.UseHeading(t);
        }

        /// <summary>A big wall title: navy plate, cyan frame, amber main title, thin divider and a spaced subtitle.</summary>
        static void BuildTitlePlate(Transform root, Vector3 pos, Quaternion rot, string main, string sub)
        {
            const float W = 4600f, H = 900f;
            var c = UIKit.Canvas("TitlePlate_" + main, root, new Vector2(W, H), 0.001f, false);
            c.transform.SetPositionAndRotation(pos, rot);
            var r = (RectTransform)c.transform;
            var bg = UIKit.Image(r, Pal.Rgb("#091322", 0.94f), "Plate");
            UIKit.Stretch(bg.rectTransform);
            Sprites.Apply(bg, Sprites.Rounded, 3f);
            var ol = UIKit.Image(r, Pal.WelcomeBorder, "Frame");
            UIKit.Stretch(ol.rectTransform);
            Sprites.Apply(ol, Sprites.Outline, 2.2f);

            var title = UIKit.Text(r, main, 360, Pal.WelcomeTitle, TextAlignmentOptions.Center, "Main");
            PlaceTop(title.rectTransform, 40, 60, W - 80, 470);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 22f;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true; title.fontSizeMin = 120; title.fontSizeMax = 360;
            Fonts.UseHeading(title);

            // divider: amber - cyan - amber
            var line = UIKit.Image(r, Pal.WelcomeBorder, "Divider");
            PlaceTop(line.rectTransform, W / 2f - 1300f, 560, 2600, 8);
            var l = UIKit.Image(r, Pal.WelcomeTitle, "AccentL");
            PlaceTop(l.rectTransform, W / 2f - 1300f - 160f, 556, 120, 16);
            var rr = UIKit.Image(r, Pal.WelcomeTitle, "AccentR");
            PlaceTop(rr.rectTransform, W / 2f + 1300f + 40f, 556, 120, 16);

            var subT = UIKit.Text(r, sub, 140, Pal.TextLight, TextAlignmentOptions.Center, "Sub");
            PlaceTop(subT.rectTransform, 40, 600, W - 80, 240);
            subT.characterSpacing = 40f;
            subT.textWrappingMode = TextWrappingModes.NoWrap;
            subT.enableAutoSizing = true; subT.fontSizeMin = 60; subT.fontSizeMax = 140;
            Fonts.UseHeading(subT);

            // glowing strip under the plate
            var strip = Fx.Prim(PrimitiveType.Cube, "TitleGlow", root, Vector3.zero, new Vector3(W * 0.001f + 0.2f, 0.04f, 0.02f), Mat.Glow(Pal.WelcomeBorder, 1.8f));
            strip.transform.SetPositionAndRotation(pos + rot * new Vector3(0f, -H * 0.0005f - 0.08f, 0.01f), rot);
        }

        static void PlaceTop(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static void Furniture(Transform root, Vector3 pos, Quaternion rot, Vector3 size, string name)
        {
            var go = Fx.Prim(PrimitiveType.Cube, name, root, Vector3.zero, size, Mat.Lit(new Color(0.15f, 0.18f, 0.25f)));
            go.transform.SetPositionAndRotation(pos, rot);
            var edge = Fx.Prim(PrimitiveType.Cube, name + "Edge", go.transform, new Vector3(0, 0.45f, -0.53f), new Vector3(1.01f, 0.15f, 0.04f), Mat.Glow(Pal.Cyan, 0.9f));
            edge.name = name + "Edge";
        }

        static void MakeTeleportable(GameObject floor)
        {
            // Teleport anywhere on the floor with the thumbstick (Starter Assets rig).
            // Only the rig's teleport ray (interaction layer "Teleport") may select the floor;
            // if that layer does not exist the mask is 0 and teleporting is simply disabled.
            var area = floor.AddComponent<TeleportationArea>();
            area.interactionLayers = InteractionLayerMask.GetMask("Teleport");
        }

        public static void SetupLighting()
        {
            var lightGo = new GameObject("SparkCity Sun");
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 0.9f;
            l.color = new Color(0.85f, 0.9f, 1f);
            l.shadows = LightShadows.None;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.35f, 0.42f);
            RenderSettings.fog = false;
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0.02f, 0.03f, 0.06f);
            }
        }
    }
}