// Editor menu: "Spark City VR"
//   > Build Game Scene            creates materials + a ready-to-play scene with the XR rig
//   > Configure Quest Build       Android / IL2CPP / ARM64 / Linear + app id
//   > Open Log Folder (Editor)    where play-mode logs are written on this PC
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SparkCity.EditorTools
{
    public static class SparkCitySetup
    {
        const string Root = "Assets/SparkCityVR";
        const string MatFolder = Root + "/Resources/SparkCityMaterials";
        const string ScenePath = Root + "/Scenes/SparkCity.unity";

        [MenuItem("Spark City VR/Build Game Scene", priority = 1)]
        public static void BuildScene()
        {
            if (!CheckTmp()) return;
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                if (!EditorUtility.DisplayDialog("Spark City VR",
                    "No render pipeline asset is set (Project Settings > Graphics). The game is designed for URP " +
                    "(use the Unity 'VR' or 'Universal 3D' template). Continue anyway with built-in shaders?", "Continue", "Cancel"))
                    return;
            }

            var rigPrefab = FindXrOriginPrefab();
            if (rigPrefab == null)
            {
                EditorUtility.DisplayDialog("Spark City VR - XR rig not found",
                    "Could not find the 'XR Origin (XR Rig)' prefab.\n\n" +
                    "Open Window > Package Manager > XR Interaction Toolkit > Samples and import 'Starter Assets', " +
                    "then run Spark City VR > Build Game Scene again.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CreateMaterials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
            rig.transform.SetPositionAndRotation(new Vector3(0f, 0f, -60f), Quaternion.identity); // welcome space

            if (Object.FindFirstObjectByType<XRInteractionManager>() == null)
                new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();

            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<XRUIInputModule>();
            }
            else if (es.GetComponent<XRUIInputModule>() == null)
            {
                foreach (var m in es.GetComponents<BaseInputModule>()) Object.DestroyImmediate(m);
                es.gameObject.AddComponent<XRUIInputModule>();
            }

            var game = new GameObject("SparkCityGame");
            game.AddComponent<SparkCityGame>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            Selection.activeGameObject = game;

            EditorUtility.DisplayDialog("Spark City VR",
                "Scene created: " + ScenePath + "\n\n" +
                "Press Play to test (desktop test mode: mouse + keyboard), " +
                "or use File > Build And Run with your Quest 3S connected.\n\n" +
                "Everything (rooms, stations, UI) is built when the game starts, so the scene looks empty in Edit mode.", "OK");
        }

        [MenuItem("Spark City VR/Configure Quest Build Settings", priority = 2)]
        public static void ConfigureBuild()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (EditorUtility.DisplayDialog("Spark City VR", "Switch the build platform to Android now? (This can take a few minutes.)", "Switch", "Skip"))
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }
            try
            {
                PlayerSettings.companyName = string.IsNullOrEmpty(PlayerSettings.companyName) || PlayerSettings.companyName == "DefaultCompany" ? "SparkCityEdu" : PlayerSettings.companyName;
                PlayerSettings.productName = "Spark City VR";
                var android = NamedBuildTarget.Android;
                if (PlayerSettings.GetApplicationIdentifier(android).Contains("DefaultCompany") || string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(android)))
                    PlayerSettings.SetApplicationIdentifier(android, "com.sparkcityedu.sparkcityvr");
                PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.colorSpace = ColorSpace.Linear;
                AddSceneToBuild(ScenePath);
                EditorUtility.DisplayDialog("Spark City VR",
                    "Done: Android, IL2CPP, ARM64, Linear color, app id " + PlayerSettings.GetApplicationIdentifier(android) + ".\n\n" +
                    "Still check by hand (see SETUP_GUIDE):\n" +
                    "- Project Settings > XR Plug-in Management > Android tab: OpenXR ON\n" +
                    "- OpenXR > Android: 'Meta Quest Support' feature ON, Oculus Touch controller profile added\n" +
                    "- Project Validation: fix all issues", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[SparkCity] Could not apply some build settings: " + e.Message);
            }
        }

        /// <summary>Converts Resources/SparkCityFonts/Heading.ttf and Body.ttf into TextMeshPro font assets.</summary>
        [MenuItem("Spark City VR/Create Font Assets", priority = 3)]
        public static void CreateFontAssets()
        {
            string folder = Root + "/Resources/SparkCityFonts";
            int made = 0;
            foreach (var name in new[] { "Heading", "Body" })
            {
                string ttf = FindFontFile(folder, name);
                if (ttf == null) continue;
                var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
                if (font == null) continue;
                string outPath = folder + "/" + name + " SDF.asset";
                if (AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(outPath) != null) AssetDatabase.DeleteAsset(outPath);
                var fa = TMPro.TMP_FontAsset.CreateFontAsset(font);
                if (fa == null) { Debug.LogError("[SparkCity] Could not create a font asset from " + ttf); continue; }
                fa.name = name + " SDF";
                AssetDatabase.CreateAsset(fa, outPath);
                if (fa.atlasTextures != null)
                    foreach (var t in fa.atlasTextures)
                        if (t != null) { t.name = fa.name + " Atlas"; AssetDatabase.AddObjectToAsset(t, fa); }
                if (fa.material != null) { fa.material.name = fa.name + " Material"; AssetDatabase.AddObjectToAsset(fa.material, fa); }
                EditorUtility.SetDirty(fa);
                made++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Spark City VR - fonts",
                made > 0 ? "Created " + made + " font asset(s) in " + folder + "."
                         : "No fonts found. Put Heading.ttf and/or Body.ttf (or .otf) into " + folder + " and run this again.", "OK");
        }

        static string FindFontFile(string folder, string name)
        {
            foreach (var ext in new[] { ".ttf", ".otf", ".TTF", ".OTF" })
            {
                string p = folder + "/" + name + ext;
                if (File.Exists(p)) return p;
            }
            return null;
        }

        [MenuItem("Spark City VR/Open Log Folder (Editor play mode)", priority = 20)]
        public static void OpenLogs()
        {
            string dir = Path.Combine(Application.persistentDataPath, "SparkCityLogs");
            Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }

        // -------------------------------------------------------------------------------
        static bool CheckTmp()
        {
            var settings = Resources.Load<TMPro.TMP_Settings>("TMP Settings");
            if (settings != null) return true;
            EditorUtility.DisplayDialog("Spark City VR - TextMeshPro",
                "TextMeshPro essential resources are not imported yet.\n\n" +
                "Open Window > TextMeshPro > Import TMP Essential Resources, click Import, then run Spark City VR > Build Game Scene again.", "OK");
            return false;
        }

        static GameObject FindXrOriginPrefab()
        {
            var guids = AssetDatabase.FindAssets("XR Origin t:Prefab");
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToList();
            string best = paths.FirstOrDefault(p => p.Contains("Starter Assets") && Path.GetFileNameWithoutExtension(p) == "XR Origin (XR Rig)")
                       ?? paths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == "XR Origin (XR Rig)")
                       ?? paths.FirstOrDefault(p => p.Contains("Starter Assets") && Path.GetFileNameWithoutExtension(p).StartsWith("XR Origin"));
            if (best != null) return AssetDatabase.LoadAssetAtPath<GameObject>(best);

            // Fallback: any prefab that contains an XROrigin component (e.g. a renamed or template rig).
            GameObject found = null;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (!p.StartsWith("Assets/")) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go == null || go.GetComponentInChildren<Unity.XR.CoreUtils.XROrigin>(true) == null) continue;
                if (go.GetComponentsInChildren<Camera>(true).Length == 0) continue;
                if (found == null || p.Contains("Starter Assets")) found = go;
                if (p.Contains("Starter Assets")) break;
            }
            if (found != null) Debug.Log("[SparkCity] Using XR rig prefab: " + AssetDatabase.GetAssetPath(found));
            return found;
        }

        static void CreateMaterials()
        {
            Directory.CreateDirectory(MatFolder);
            bool urp = GraphicsSettings.defaultRenderPipeline != null;
            var lit = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
            var unlit = Shader.Find(urp ? "Universal Render Pipeline/Unlit" : "Unlit/Color");
            if (lit == null) lit = Shader.Find("Standard");
            if (unlit == null) unlit = Shader.Find("Unlit/Color");

            MakeMat("SC_Lit", lit, m => { SetColor(m, new Color(0.5f, 0.5f, 0.5f)); });
            MakeMat("SC_Glow", lit, m =>
            {
                SetColor(m, new Color(0.3f, 0.3f, 0.3f));
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.white);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            });
            MakeMat("SC_Unlit", unlit, m => { SetColor(m, Color.white); });
            // skybox material for 360 welcome artwork (keeps the panoramic shader in the Quest build)
            var pano = Shader.Find("Skybox/Panoramic");
            if (pano != null)
                MakeMat("SC_Sky", pano, m =>
                {
                    if (m.HasProperty("_Mapping")) m.SetFloat("_Mapping", 1f);      // latitude-longitude layout
                    if (m.HasProperty("_ImageType")) m.SetFloat("_ImageType", 0f);  // 360 degrees
                    m.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
                    m.DisableKeyword("_MAPPING_6_FRAMES_LAYOUT");
                });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        static void MakeMat(string name, Shader shader, System.Action<Material> setup)
        {
            string path = MatFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                setup(mat);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
                setup(mat);
                EditorUtility.SetDirty(mat);
            }
        }

        static void AddSceneToBuild(string path)
        {
            if (!File.Exists(path)) return;
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
