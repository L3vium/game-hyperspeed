using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum SpeedState { Normal, Hyper, Light }

    // How the game looks in each speed state : wide angle FOV and projection, camera distance and height, screen
    // effects. One per state under "Speed States" in the HS_Juice prefab. Normal gives values at rest (x) and at top
    // speed (y) ; Hyper and Light ADD on top (Light comes on top of Hyper).
    public class HSSpeedStateLook : MonoBehaviour
    {
        public SpeedState state;

        [Header("Wide angle view")]
        [Tooltip("Horizontal FOV in degrees. Normal : at rest (x) and at top speed (y). Hyper / Light : added (x).")]
        public Vector2 fov = new Vector2(175f, 215f);
        [Tooltip("Projection shape : 0.5 stereographic (natural), 0 equidistant (more periphery, smaller centre). Blended between states.")]
        public float shape = 0.5f;
        [Tooltip("0 round fisheye, 1 Panini (straight verticals). Blended between states.")]
        [Range(0f, 1f)] public float cylinder = 0.55f;
        [Tooltip("Classic camera only (wide angle off) : vertical FOV. Normal : at rest / top. Hyper / Light : added.")]
        public Vector2 classicFov = new Vector2(105f, 145f);

        [Header("Camera")]
        [Tooltip("Metres behind the runner. Normal : at rest (x) / top speed (y). Hyper / Light : added (x).")]
        public Vector2 distance = new Vector2(30f, 85f);
        [Tooltip("Metres above the runner. Normal : at rest (x) / top speed (y). Hyper / Light : added (x).")]
        public Vector2 height = new Vector2(10f, 24f);
        [Tooltip("Seconds to blend into this state.")]
        public float blendTime = 0.6f;

        [Header("Screen (added while in this state)")]
        public float aberration;
        public float radialBlur;
        public float bloom;
        public float saturation;
        [Range(0f, 1f)] public float speedLines;

        public static void Defaults(HSSpeedStateLook pLook, SpeedState pState)
        {
            pLook.state = pState;
            switch (pState)
            {
                case SpeedState.Normal:
                    pLook.fov = new Vector2(175f, 215f);
                    pLook.shape = 0.5f;
                    pLook.cylinder = 0.55f;
                    pLook.classicFov = new Vector2(105f, 145f);
                    pLook.distance = new Vector2(30f, 85f);
                    pLook.height = new Vector2(10f, 24f);
                    pLook.blendTime = 0.6f;
                    break;
                // 2026-11-12 : normal and hyper are the camera of build 600da29 again (close, classic perspective) ;
                // only light speed goes far, and only light speed opens the wide view.
                case SpeedState.Hyper:
                    pLook.fov = new Vector2(50f, 0f);
                    pLook.shape = 0.3f;
                    pLook.cylinder = 0.45f;
                    pLook.classicFov = new Vector2(4f, 0f);
                    pLook.distance = new Vector2(35f, 0f);
                    pLook.height = new Vector2(8f, 0f);
                    pLook.blendTime = 0.45f;
                    pLook.aberration = 0.6f;
                    pLook.radialBlur = 0.2f;
                    pLook.bloom = 0.4f;
                    pLook.saturation = 15f;
                    pLook.speedLines = 0.1f;
                    break;
                case SpeedState.Light:
                    pLook.fov = new Vector2(55f, 0f);
                    pLook.shape = 0.12f;
                    pLook.cylinder = 0.35f;
                    pLook.classicFov = new Vector2(10f, 0f);
                    // Hyper + light together keep the far light framing (about 315 m back, 86 m up at full speed).
                    pLook.distance = new Vector2(195f, 0f);
                    pLook.height = new Vector2(54f, 0f);
                    pLook.blendTime = 0.7f;
                    pLook.aberration = 0.8f;
                    pLook.radialBlur = 0.25f;
                    pLook.bloom = 0.8f;
                    pLook.saturation = 10f;
                    pLook.speedLines = 0.08f;
                    break;
            }
        }

        private static HSSpeedStateLook[] _fallback;

        // The look of a state : from the prefab, or the defaults if the prefab has none.
        public static HSSpeedStateLook Get(SpeedState pState)
        {
            HSJuiceBank lBank = HSJuice.Bank;
            HSSpeedStateLook lLook = lBank != null ? lBank.GetLook(pState) : null;
            if (lLook != null) return lLook;
            if (_fallback == null)
            {
                GameObject lObject = new GameObject("SpeedStateDefaults");
                lObject.hideFlags = HideFlags.HideAndDontSave;
                _fallback = new HSSpeedStateLook[3];
                for (int i = 0; i < 3; i++)
                {
                    _fallback[i] = lObject.AddComponent<HSSpeedStateLook>();
                    Defaults(_fallback[i], (SpeedState)i);
                }
            }
            return _fallback[(int)pState];
        }
    }
}
