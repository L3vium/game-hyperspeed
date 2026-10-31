using System.Collections;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Static facade for game feel : hitstop, shake, flashes, impact frames, popups.
    public class Juice : MonoBehaviour
    {
        private static Juice _instance;
        private Coroutine _hitstop;

        public static bool Paused;

        public static event System.Action<string, Color, float> OnPopup;

        private static Juice Instance
        {
            get
            {
                if (_instance == null) _instance = new GameObject("Juice").AddComponent<Juice>();
                return _instance;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public static void Shake(float pTrauma)
        {
            if (CameraRig.Instance != null) CameraRig.Instance.AddTrauma(pTrauma);
        }

        public static void FovKick(float pAmount)
        {
            if (CameraRig.Instance != null) CameraRig.Instance.FovKick(pAmount);
        }

        public static void Rollback(float pAmount)
        {
            if (CameraRig.Instance != null) CameraRig.Instance.Rollback(pAmount);
        }

        public static void Flash(Color pColor, float pAmount)
        {
            if (PostFX.Instance != null) PostFX.Instance.Flash(pColor, pAmount);
        }

        public static void Pulse(float pAmount)
        {
            if (PostFX.Instance != null) PostFX.Instance.Pulse(pAmount);
        }

        public static void Vignette(Color pColor, float pAmount)
        {
            if (PostFX.Instance != null) PostFX.Instance.VignetteColor(pColor, pAmount);
        }

        public static void Popup(string pText, Color pColor, float pScale = 1f)
        {
            if (OnPopup != null) OnPopup(pText, pColor, pScale);
        }

        public static void Hitstop(float pDuration, float pScale = 0.03f)
        {
            Juice lJuice = Instance;
            if (lJuice._hitstop != null) lJuice.StopCoroutine(lJuice._hitstop);
            lJuice._hitstop = lJuice.StartCoroutine(lJuice.HitstopRoutine(pDuration, pScale));
        }

        private IEnumerator HitstopRoutine(float pDuration, float pScale)
        {
            if (!Paused) Time.timeScale = pScale;
            yield return new WaitForSecondsRealtime(pDuration);
            if (!Paused) Time.timeScale = 1f;
            _hitstop = null;
        }

        // Anime impact frame : stylize ink frames + Vefects impact sprite + hitstop + shake.
        public static void Impact(Vector3 pPoint, float pStrength = 1f)
        {
            if (PostFX.Instance != null) PostFX.Instance.Impact(pStrength > 0.8f ? 3 : 2);
            Hitstop(0.05f + 0.05f * pStrength);
            Shake(0.35f * pStrength + 0.2f);
            if (CameraRig.Instance != null) CameraRig.Instance.FovPunch(-8f * pStrength);
            GameObject lPrefab = GameAssets.Instance.impactFramePrefab;
            if (lPrefab != null && CameraRig.Instance != null)
            {
                Transform lCam = CameraRig.Instance.transform;
                GameObject lFx = Instantiate(lPrefab, Vector3.Lerp(lCam.position, pPoint, 0.5f), Quaternion.LookRotation(lCam.forward, lCam.up));
                Destroy(lFx, 1.2f);
            }
        }
    }
}
