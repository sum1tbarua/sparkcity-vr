// Desktop test mode: play Spark City VR in the Unity Editor with a normal mouse,
// without a headset and without the XR Interaction Simulator.
//   Move mouse        the RIGHT CONTROLLER follows the cursor (its ray points where you point)
//   Left click        trigger: press buttons and choose objects
//   Right drag        look around
//   W A S D           walk (Q / E turn)
// It switches on automatically in the Editor when no headset is connected and the
// XR Interaction Simulator is not running.
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SparkCity
{
    public class DesktopTestMode : MonoBehaviour
    {
        public static bool Active { get; private set; }
        static DesktopTestMode instance;
        const float NormalFov = 60f;   // natural first-person view on a monitor
        const float WideFov = 90f;     // close to the Quest 3S field of view (press F)

        /// <summary>Turns the desktop view toward a point (called when the game moves the player).</summary>
        public static void Face(Vector3 world, float amount = 0.6f)
        {
            if (!Active || instance == null) return;
            var cam = Camera.main;
            var root = PlayerRig.Root;
            if (cam == null || root == null) return;
            Vector3 d = world - cam.transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            float yaw = Vector3.SignedAngle(Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up), d, Vector3.up);
            root.RotateAround(cam.transform.position, Vector3.up, yaw * amount);
            instance.pitch = 0f; // eyes level, like a person standing at the station
            var e = cam.transform.localEulerAngles;
            cam.transform.localEulerAngles = new Vector3(instance.pitch, e.y, 0f);
        }

        float pitch;
        Transform rightCtrl, leftCtrl;
        Vector3 aimPoint;
        bool hasAim;

        IEnumerator Start()
        {
            instance = this;
            Active = false;
            if (!Application.isEditor) yield break;
            yield return new WaitForSeconds(0.5f);
            bool headset = UnityEngine.XR.XRSettings.isDeviceActive;
            bool simulator = FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t => t.name.Contains("Simulator") && t.gameObject.activeInHierarchy);
            if (headset || simulator)
            {
                Debug.Log("[SparkCity] Desktop test mode OFF (" + (headset ? "headset" : "XR Interaction Simulator") + " detected).");
                yield break;
            }
            Active = true;
            Debug.Log("[SparkCity] Desktop test mode ON: mouse aims, left-click = trigger, arrows/Q/E turn.");

            // stop the XR UI module from also handling the mouse (avoids double clicks)
            var es = FindFirstObjectByType<EventSystem>();
            if (es != null)
            {
                var xrui = es.GetComponent<XRUIInputModule>();
                if (xrui != null) xrui.enableMouseInput = false;
            }

            FindControllers();
            if (Camera.main != null) Camera.main.fieldOfView = NormalFov;
            Application.onBeforeRender += PoseControllers;

            // put the camera at eye height (no headset tracking is moving it)
            var cam = PlayerRig.Cam;
            if (cam != null && PlayerRig.Root != null)
            {
                float above = cam.position.y - PlayerRig.Root.position.y;
                cam.localPosition += Vector3.up * (PlayerRig.EyeHeight - above);
            }
        }

        void OnDestroy()
        {
            Application.onBeforeRender -= PoseControllers;
        }

        void FindControllers()
        {
            var root = PlayerRig.Root;
            if (root == null) return;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (!n.Contains("controller") || n.Contains("stabilized") || n.Contains("visual")) continue;
                if (rightCtrl == null && n.Contains("right")) rightCtrl = t;
                if (leftCtrl == null && n.Contains("left")) leftCtrl = t;
            }
            if (rightCtrl == null) Debug.LogWarning("[SparkCity] Desktop mode: right controller not found in the XR rig.");

            // The starter rig's XR Input Modality Manager hides controllers that are not tracked.
            // With no headset nothing is tracked, so switch it off and show the controllers.
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb != null && mb.GetType().Name == "XRInputModalityManager") mb.enabled = false;
            if (rightCtrl != null) rightCtrl.gameObject.SetActive(true);
            if (leftCtrl != null) leftCtrl.gameObject.SetActive(true);
        }

        /// <summary>Holds the controllers in front of the camera; the right one aims at the mouse.</summary>
        void PoseControllers()
        {
            if (!Active) return;
            var cam = Camera.main;
            if (cam == null) return;
            var ct = cam.transform;
            Vector3 flatFwd = Vector3.ProjectOnPlane(ct.forward, Vector3.up).normalized;
            if (flatFwd.sqrMagnitude < 0.01f) flatFwd = ct.forward;
            Vector3 right = Vector3.Cross(Vector3.up, flatFwd);
            if (rightCtrl != null)
            {
                Vector3 p = ct.position + right * 0.2f - Vector3.up * 0.19f + flatFwd * 0.45f;
                Vector3 target = hasAim ? aimPoint : ct.position + ct.forward * 3f;
                Vector3 dir = target - p;
                if (dir.sqrMagnitude > 0.0001f) rightCtrl.SetPositionAndRotation(p, Quaternion.LookRotation(dir.normalized, Vector3.up));
            }
            if (leftCtrl != null)
            {
                Vector3 p = ct.position - right * 0.22f - Vector3.up * 0.21f + flatFwd * 0.42f;
                leftCtrl.SetPositionAndRotation(p, Quaternion.LookRotation(flatFwd - Vector3.up * 0.6f, Vector3.up));
            }
        }

        void LateUpdate()
        {
            PoseControllers();
        }

        void Update()
        {
            if (!Active) return;
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return;

            Vector2 screen = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(screen);
            UpdateAimPoint(cam, ray, screen);

            // ---- look around (right drag)
            if (mouse.rightButton.isPressed)
            {
                Vector2 d = mouse.delta.ReadValue() * 0.15f;
                var root = PlayerRig.Root;
                if (root != null) root.RotateAround(cam.transform.position, Vector3.up, d.x);
                pitch = Mathf.Clamp(pitch - d.y, -70f, 70f);
                var e = cam.transform.localEulerAngles;
                cam.transform.localEulerAngles = new Vector3(pitch, e.y, 0f);
            }

            // ---- walk with W A S D, turn with Q / E
            var kb = Keyboard.current;
            var rootT = PlayerRig.Root;
            if (kb != null && rootT != null)
            {
                Vector3 f = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                Vector3 r = Vector3.Cross(Vector3.up, f);
                Vector3 move = Vector3.zero;
                if (kb.wKey.isPressed) move += f;
                if (kb.sKey.isPressed) move -= f;
                if (kb.dKey.isPressed) move += r;
                if (kb.aKey.isPressed) move -= r;
                if (move.sqrMagnitude > 0f)
                {
                    Vector3 delta = move.normalized * 1.8f * Time.deltaTime;
                    var cc = rootT.GetComponent<CharacterController>();
                    if (cc != null && cc.enabled) cc.Move(delta); else rootT.position += delta;
                }
                if (kb.fKey.wasPressedThisFrame)
                    cam.fieldOfView = cam.fieldOfView < 75f ? WideFov : NormalFov;
                // turn: Q/E or Left/Right arrows (the mouse only aims the controller)
                float turn = 0f;
                if (kb.eKey.isPressed || kb.rightArrowKey.isPressed) turn += 1f;
                if (kb.qKey.isPressed || kb.leftArrowKey.isPressed) turn -= 1f;
                if (turn != 0f) rootT.RotateAround(cam.transform.position, Vector3.up, Mathf.Clamp(turn, -1f, 1f) * 90f * Time.deltaTime);
                // look up / down: Up/Down arrows
                float look = (kb.downArrowKey.isPressed ? 1f : 0f) - (kb.upArrowKey.isPressed ? 1f : 0f);
                if (look != 0f)
                {
                    pitch = Mathf.Clamp(pitch + look * 60f * Time.deltaTime, -70f, 70f);
                    var ce = cam.transform.localEulerAngles;
                    cam.transform.localEulerAngles = new Vector3(pitch, ce.y, 0f);
                }
            }

            if (!mouse.leftButton.wasPressedThisFrame) return;

            // ---- click: the nearest clickable panel/object under the mouse
            TryClickButton(cam, ray, screen);
        }

        void UpdateAimPoint(Camera cam, Ray ray, Vector2 screen)
        {
            float best = 8f;
            bool hit = false;
            if (Physics.Raycast(ray, out RaycastHit h, 30f, ~0, QueryTriggerInteraction.Ignore)) { best = h.distance; hit = true; }
            foreach (var r in FindObjectsByType<TrackedDeviceGraphicRaycaster>(FindObjectsSortMode.None))
            {
                if (!r.isActiveAndEnabled) continue;
                var rt = (RectTransform)r.transform;
                var plane = new Plane(rt.forward, rt.position);
                if (!plane.Raycast(ray, out float d) || d <= 0f || d >= best) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(rt, screen, cam)) continue;
                best = d; hit = true;
            }
            aimPoint = ray.GetPoint(hit ? best : 8f);
            hasAim = true;
        }

        static bool TryClickButton(Camera cam, Ray ray, Vector2 screen)
        {
            Canvas best = null;
            float bestDist = float.MaxValue;
            foreach (var r in FindObjectsByType<TrackedDeviceGraphicRaycaster>(FindObjectsSortMode.None))
            {
                if (!r.isActiveAndEnabled) continue;
                var canvas = r.GetComponent<Canvas>();
                if (canvas == null) continue;
                var rt = (RectTransform)canvas.transform;
                var plane = new Plane(rt.forward, rt.position);
                if (!plane.Raycast(ray, out float d) || d <= 0f) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(rt, screen, cam)) continue;
                if (d < bestDist) { bestDist = d; best = canvas; }
            }
            if (best == null) return false;
            foreach (var b in best.GetComponentsInChildren<Button>())
            {
                if (!b.isActiveAndEnabled || !b.interactable) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)b.transform, screen, cam))
                {
                    b.onClick.Invoke();
                    return true;
                }
            }
            return true; // clicked on a panel background: swallow the click
        }

        void OnGUI()
        {
            if (!Active) return;
            var style = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            GUI.Box(new Rect(10, 10, 960, 28), "  DESKTOP TEST MODE   mouse: aim   left-click: trigger   right-drag or arrows/Q/E: look   WASD: walk   F: wide view", style);
        }
    }
}
