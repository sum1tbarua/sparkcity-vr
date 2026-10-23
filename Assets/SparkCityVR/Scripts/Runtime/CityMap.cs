// The holographic city map on the command-center table: 6 districts, hazard overlays,
// alert beacons and moving vehicle routes for the consequence scene.
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SparkCity
{
    public class CityMap : MonoBehaviour
    {
        class District
        {
            public string id;
            public Transform block;
            public Renderer blockRend;
            public Transform hazard;
            public Renderer hazardRend;
            public Transform beacon;
            public Renderer beaconRend;
            public TextMeshProUGUI label;
            public GameObject status;          // small tag above the district: "FLOOD IN 08:11", "EVACUATED - SAFE"...
            public TextMeshProUGUI statusText;
            public UnityEngine.UI.Image statusBorder;
            public Vector3 center;      // world position of the top of the block
            public float hazardLevel;
            public float hazardTarget;
            public bool beaconOn;
            public Color beaconColor;
        }

        class Route
        {
            public Vector3 from, to;
            public List<Transform> cars = new List<Transform>();
        }

        readonly Dictionary<string, District> districts = new Dictionary<string, District>();
        readonly List<Route> routes = new List<Route>();
        Color hazardColor = Color.blue;
        static readonly string[] Ids = { "D1", "D2", "D3", "D4", "D5", "D6" };
        const float BlockSize = 0.62f;
        const float Spacing = 0.72f;

        public static CityMap Build(Transform parent, Vector3 tableCenter, float yaw)
        {
            var go = Fx.Group("CityMap", parent);
            go.transform.SetPositionAndRotation(tableCenter, Quaternion.Euler(0, yaw, 0));
            var map = go.AddComponent<CityMap>();
            map.BuildGeometry();
            return map;
        }

        void BuildGeometry()
        {
            // table
            Fx.Prim(PrimitiveType.Cube, "TableTop", transform, new Vector3(0, -0.04f, 0), new Vector3(2.35f, 0.06f, 1.65f), Mat.Lit(new Color(0.08f, 0.1f, 0.15f)));
            Fx.Prim(PrimitiveType.Cube, "TableRim", transform, new Vector3(0, -0.05f, 0), new Vector3(2.42f, 0.02f, 1.72f), Mat.Glow(Pal.Cyan, 0.8f)); // just below the top: only the edge glows
            Fx.Prim(PrimitiveType.Cylinder, "TableLeg", transform, new Vector3(0, -0.45f, 0), new Vector3(0.5f, 0.4f, 0.5f), Mat.Lit(new Color(0.12f, 0.14f, 0.2f)));
            // river line across the map (decorative)
            Fx.Prim(PrimitiveType.Cube, "River", transform, new Vector3(0, 0.0f, 0.0f), new Vector3(2.3f, 0.012f, 0.06f), Mat.Glow(new Color(0.2f, 0.5f, 1f), 1.0f));

            for (int i = 0; i < Ids.Length; i++)
            {
                int col = i % 3;
                int row = i / 3;
                var local = new Vector3((col - 1) * Spacing, 0.0f, (row == 0 ? 1 : -1) * Spacing * 0.55f);
                var d = new District { id = Ids[i] };
                var blockGo = Fx.Prim(PrimitiveType.Cube, "District_" + d.id, transform, local + new Vector3(0, 0.03f, 0), new Vector3(BlockSize, 0.05f, BlockSize * 0.62f), Mat.Lit(Pal.Idle));
                d.block = blockGo.transform;
                d.blockRend = blockGo.GetComponent<Renderer>();

                // small buildings for flavor
                var rnd = new System.Random(i * 37 + 11);
                for (int b = 0; b < 5; b++)
                {
                    float bx = (float)(rnd.NextDouble() - 0.5) * BlockSize * 0.75f;
                    float bz = (float)(rnd.NextDouble() - 0.5) * BlockSize * 0.4f;
                    float bh = 0.04f + (float)rnd.NextDouble() * 0.12f;
                    Fx.Prim(PrimitiveType.Cube, "Bldg", transform, local + new Vector3(bx, 0.055f + bh * 0.5f, bz), new Vector3(0.07f, bh, 0.07f), Mat.Lit(new Color(0.35f, 0.42f, 0.55f)));
                }

                var hz = Fx.Prim(PrimitiveType.Cube, "Hazard_" + d.id, transform, local + new Vector3(0, 0.06f, 0), new Vector3(BlockSize * 0.98f, 0.001f, BlockSize * 0.6f), Mat.Glow(Color.blue, 1f));
                d.hazard = hz.transform;
                d.hazardRend = hz.GetComponent<Renderer>();
                hz.SetActive(false);

                var bc = Fx.Prim(PrimitiveType.Cylinder, "Beacon_" + d.id, transform, local + new Vector3(0, 0.35f, 0), new Vector3(0.05f, 0.3f, 0.05f), Mat.Glow(Pal.Good, 2f));
                d.beacon = bc.transform;
                d.beaconRend = bc.GetComponent<Renderer>();
                bc.SetActive(false);

                // label floating above, tilted toward the viewer
                var c = UIKit.Canvas("Label_" + d.id, transform, new Vector2(520, 130), 0.001f, false);
                c.transform.localPosition = local + new Vector3(0, 0.26f, -0.05f);
                c.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
                d.label = UIKit.Text(c.transform, d.id, 52, Pal.Text, TextAlignmentOptions.Center, "Name");
                UIKit.Stretch(d.label.rectTransform);
                d.label.fontStyle = FontStyles.Bold;

                // status tag (hidden until used)
                var sc = UIKit.Canvas("Status_" + d.id, transform, new Vector2(620, 96), 0.001f, false);
                sc.transform.localPosition = local + new Vector3(0, 0.4f, -0.05f);
                sc.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
                var sbg = UIKit.Image(sc.transform, Pal.Rgb("#091322", 0.92f), "Bg");
                UIKit.Stretch(sbg.rectTransform);
                Sprites.Apply(sbg, Sprites.Rounded, 1.2f);
                d.statusBorder = UIKit.Image(sc.transform, Pal.WelcomeBorder, "Border");
                UIKit.Stretch(d.statusBorder.rectTransform);
                Sprites.Apply(d.statusBorder, Sprites.Outline, 1.2f);
                d.statusText = UIKit.Text(sc.transform, "", 50, Pal.TextLight, TextAlignmentOptions.Center, "Text");
                UIKit.Stretch(d.statusText.rectTransform, 14, 14, 4, 4);
                d.statusText.fontStyle = FontStyles.Bold;
                d.statusText.enableAutoSizing = true; d.statusText.fontSizeMin = 24; d.statusText.fontSizeMax = 50;
                d.status = sc.gameObject;
                d.status.SetActive(false);

                d.center = transform.TransformPoint(local + new Vector3(0, 0.09f, 0));
                districts[d.id] = d;
            }
        }

        /// <summary>Names the 6 districts (D1..D6) and sets the hazard color (flood blue etc.).</summary>
        public void Setup(string[] names, Color hazard)
        {
            hazardColor = hazard;
            foreach (var d in districts.Values)
            {
                int i = int.Parse(d.id.Substring(1)) - 1;
                string name = names != null && i >= 0 && i < names.Length ? names[i] : "";
                d.label.text = d.id + "  " + name;
            }
            ResetVisuals();
        }

        public void ResetVisuals()
        {
            ClearRoutes();
            foreach (var d in districts.Values)
            {
                d.blockRend.sharedMaterial = Mat.Lit(Pal.Idle);
                d.hazardLevel = d.hazardTarget = 0f;
                d.hazard.gameObject.SetActive(false);
                d.beaconOn = false;
                d.beacon.gameObject.SetActive(false);
                d.label.color = Pal.Text;
                d.status.SetActive(false);
            }
        }

        /// <summary>Shows a small status tag above a district (null or "" hides it).</summary>
        public void SetStatus(string id, string text, Color c)
        {
            if (!districts.TryGetValue(id, out var d)) return;
            bool on = !string.IsNullOrEmpty(text);
            if (d.status.activeSelf != on) d.status.SetActive(on);
            if (!on) return;
            d.statusText.text = text;
            d.statusText.color = c;
            d.statusBorder.color = c;
        }

        public void SetTint(string id, Color c)
        {
            if (districts.TryGetValue(id, out var d)) d.blockRend.sharedMaterial = Mat.Lit(c);
        }

        /// <summary>Animate a hazard overlay (water, jam, blackout) to a level 0..1.</summary>
        public void SetHazard(string id, float level01)
        {
            if (!districts.TryGetValue(id, out var d)) return;
            d.hazardTarget = Mathf.Clamp01(level01);
            d.hazard.gameObject.SetActive(level01 > 0f);
            d.hazardRend.sharedMaterial = Mat.Glow(hazardColor, 1.2f);
        }

        public void SetBeacon(string id, bool on, Color c)
        {
            if (!districts.TryGetValue(id, out var d)) return;
            d.beaconOn = on;
            d.beaconColor = c;
            d.beacon.gameObject.SetActive(on);
            d.beaconRend.sharedMaterial = Mat.Glow(c, 2f);
        }

        public void SetLabelColor(string id, Color c)
        {
            if (districts.TryGetValue(id, out var d)) d.label.color = c;
        }

        public void ShowRoute(string fromId, string toId, Color c)
        {
            if (!districts.TryGetValue(fromId, out var a) || !districts.TryGetValue(toId, out var b)) return;
            var r = new Route { from = a.center + Vector3.up * 0.03f, to = b.center + Vector3.up * 0.03f };
            for (int i = 0; i < 6; i++)
            {
                var car = Fx.Prim(PrimitiveType.Sphere, "Vehicle", transform, Vector3.zero, Vector3.one * 0.045f, Mat.Glow(c, 2.2f));
                r.cars.Add(car.transform);
            }
            routes.Add(r);
        }

        public void ClearRoutes()
        {
            foreach (var r in routes)
                foreach (var car in r.cars)
                    if (car != null) Destroy(car.gameObject);
            routes.Clear();
        }

        void Update()
        {
            float t = Time.time;
            foreach (var d in districts.Values)
            {
                if (d.hazard.gameObject.activeSelf)
                {
                    d.hazardLevel = Mathf.MoveTowards(d.hazardLevel, d.hazardTarget, Time.deltaTime * 0.25f);
                    float h = Mathf.Max(0.002f, d.hazardLevel * 0.12f);
                    var s = d.hazard.localScale;
                    d.hazard.localScale = new Vector3(s.x, h, s.z);
                    var p = d.hazard.localPosition;
                    d.hazard.localPosition = new Vector3(p.x, 0.055f + h * 0.5f, p.z);
                }
                if (d.beaconOn)
                {
                    float pulse = 1f + 0.25f * Mathf.Sin(t * 6f);
                    d.beacon.localScale = new Vector3(0.05f * pulse, 0.3f, 0.05f * pulse);
                }
            }
            foreach (var r in routes)
            {
                for (int i = 0; i < r.cars.Count; i++)
                {
                    if (r.cars[i] == null) continue;
                    float k = Mathf.Repeat(t * 0.25f + i / (float)r.cars.Count, 1f);
                    var p = Vector3.Lerp(r.from, r.to, k);
                    p.y += Mathf.Sin(k * Mathf.PI) * 0.12f;
                    r.cars[i].position = p;
                }
            }
        }
    }
}