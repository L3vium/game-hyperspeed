using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Static facade for game feel : hitstop, freeze frames, slow motion, shake, flashes, impact frames, popups.
    public class Juice : MonoBehaviour
    {
        private static Juice _instance;
        private Coroutine _timeRoutine;
        private float _timeLockUntil;

        private Canvas _impactCanvas;
        private RawImage _impactLines;
        private Material _impactMaterial;
        private UIShape _impactBurst;
        private float _impactTimer;
        private float _impactStrength;

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
            if (!Paused) Time.timeScale = 1f;
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

        // ---------------------------------------------------------------- time

        public static void Hitstop(float pDuration, float pScale = 0.03f) => Instance.TimeEffect(pDuration, pScale, false);

        public static void Freeze(float pDuration) => Instance.TimeEffect(pDuration, 0f, false);

        public static void SlowMo(float pScale, float pDuration) => Instance.TimeEffect(pDuration, pScale, true);

        private void TimeEffect(float pDuration, float pScale, bool pEase)
        {
            if (Paused) return;
            // A stronger, longer effect already running wins.
            if (Time.unscaledTime < _timeLockUntil && Time.timeScale <= pScale) return;
            if (_timeRoutine != null) StopCoroutine(_timeRoutine);
            _timeLockUntil = Time.unscaledTime + pDuration;
            _timeRoutine = StartCoroutine(TimeRoutine(pDuration, pScale, pEase));
        }

        private IEnumerator TimeRoutine(float pDuration, float pScale, bool pEase)
        {
            AudioManager.Duck(pDuration + 0.2f);
            for (float lTime = 0f; lTime < pDuration; lTime += Time.unscaledDeltaTime)
            {
                if (Paused) yield break;
                float lT = lTime / pDuration;
                Time.timeScale = pEase ? Mathf.Lerp(pScale, 1f, lT * lT) : pScale;
                yield return null;
            }
            if (!Paused) Time.timeScale = 1f;
            _timeRoutine = null;
        }

        // ---------------------------------------------------------------- impact frames

        // Anime impact frame : ink-inverted frames + manga radial lines + Vefects impact sprite.
        public static void ImpactFrame(float pStrength = 1f)
        {
            Juice lJuice = Instance;
            if (PostFX.Instance != null) PostFX.Instance.Impact(pStrength > 0.8f ? 3 : 2);
            lJuice.ShowImpactOverlay(pStrength);
            Shake(0.25f + 0.3f * pStrength);
            if (CameraRig.Instance != null) CameraRig.Instance.FovPunch(-10f * pStrength);
            GameObject lPrefab = GameAssets.Instance.impactFramePrefab;
            if (lPrefab != null && CameraRig.Instance != null && pStrength > 0.55f)
            {
                Transform lCam = CameraRig.Instance.transform;
                GameObject lFx = Instantiate(lPrefab, lCam);
                lFx.transform.localPosition = new Vector3(0f, 0f, 3f);
                lFx.transform.localRotation = Quaternion.identity;
                Destroy(lFx, 1.2f);
            }
        }

        // Impact frame anchored on a world hit (punch, smash).
        public static void Impact(Vector3 pPoint, float pStrength = 1f)
        {
            Hitstop(0.05f + 0.05f * pStrength);
            ImpactFrame(pStrength);
        }

        private void BuildImpactOverlay()
        {
            _impactCanvas = UIFactory.CreateCanvas("ImpactCanvas", 60, transform);
            _impactLines = UIFactory.CreateRect(_impactCanvas.transform, "ImpactLines", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            UIFactory.Stretch(_impactLines.rectTransform);
            _impactMaterial = new Material(Shader.Find("Hyperspeed/ImpactLines"));
            _impactLines.material = _impactMaterial;
            _impactLines.raycastTarget = false;
            _impactBurst = UIFactory.CreateShape(_impactCanvas.transform, "ImpactBurst", UIShape.ShapeKind.Burst, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 700f));
            _impactBurst.SetShape(UIShape.ShapeKind.Burst, 0f, 26, 0.45f, 3);
            _impactCanvas.gameObject.SetActive(false);
        }

        private void ShowImpactOverlay(float pStrength)
        {
            if (_impactCanvas == null) BuildImpactOverlay();
            _impactTimer = 0.07f + 0.05f * pStrength;
            _impactStrength = pStrength;
            _impactCanvas.gameObject.SetActive(true);
            _impactMaterial.SetFloat("_Seed", Random.value * 100f);
            _impactBurst.SetShape(UIShape.ShapeKind.Burst, 0f, 26, 0.45f, Random.Range(0, 999));
            _impactBurst.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }

        private void Update()
        {
            if (_impactCanvas == null || !_impactCanvas.gameObject.activeSelf) return;
            _impactTimer -= Time.unscaledDeltaTime;
            float lT = Mathf.Clamp01(_impactTimer / 0.12f);
            _impactMaterial.SetFloat("_Intensity", lT * _impactStrength);
            _impactBurst.color = new Color(1f, 1f, 1f, lT * 0.9f);
            _impactBurst.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 0.6f, lT);
            if (_impactTimer <= 0f) _impactCanvas.gameObject.SetActive(false);
        }
    }
}
