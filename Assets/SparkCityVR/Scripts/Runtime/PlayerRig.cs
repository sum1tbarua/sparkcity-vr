// Moves the XR Origin so the player's head ends up at a station Anchor, facing the station.
using UnityEngine;

namespace SparkCity
{
    public static class PlayerRig
    {
        /// <summary>When true, the rig is raised so a seated player's eyes are at standing height.</summary>
        public static bool SeatedFriendly
        {
            get { return PlayerPrefs.GetInt("sc_seated", 1) == 1; }
            set { PlayerPrefs.SetInt("sc_seated", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public const float EyeHeight = 1.6f;
        static Anchor last;
        static bool hasLast;

        public static Transform Cam
        {
            get { return Camera.main != null ? Camera.main.transform : null; }
        }

        public static Transform Root
        {
            get { var c = Cam; return c != null ? c.root : null; }
        }

        public static void MoveTo(Anchor a)
        {
            last = a; hasLast = true;
            var cam = Cam;
            var root = Root;
            if (cam == null || root == null || root == cam) return;

            var cc = root.GetComponentInChildren<CharacterController>();
            bool ccWasEnabled = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;

            // 1) rotate the rig around the head so the head looks along the anchor's yaw
            float delta = Mathf.DeltaAngle(cam.eulerAngles.y, a.yaw);
            root.RotateAround(cam.position, Vector3.up, delta);

            // 2) slide horizontally so the head is above the anchor point
            Vector3 headOffset = cam.position - root.position;
            Vector3 p = new Vector3(a.pos.x - headOffset.x, a.pos.y, a.pos.z - headOffset.z);

            // 3) optional seated height boost
            if (SeatedFriendly)
            {
                float headLocal = headOffset.y;
                float boost = Mathf.Clamp(EyeHeight - headLocal, 0f, 0.8f);
                p.y += boost;
            }
            root.position = p;

            if (cc != null) cc.enabled = ccWasEnabled;
            Physics.SyncTransforms();
        }

        /// <summary>Re-applies the last anchor (use as a "recenter" button).</summary>
        public static void Recenter()
        {
            if (hasLast) MoveTo(last);
        }
    }
}
