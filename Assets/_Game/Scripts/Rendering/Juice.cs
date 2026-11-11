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
            if (UnityEngine.InputSystem.Gamepad.current != null) UnityEngine.InputSystem.Gamepad.current.SetMotorSpeeds(0f, 0f);
        }

        public static void Shake(float pTrauma)
        {
            if (CameraRig.Instance != null) CameraRig.Instance.AddTrauma(pTrauma * GameSession.ShakeAmount);
            Rumble(pTrauma);
        }

        // ---------------------------------------------------------------- gamepad rumble

        private float _rumble;

        // A short buzz in the pad, as strong as the shake that asked for it (setting "VIBRATION").
        public static void Rumble(float pAmount)
        {
            if (!GameSession.Vibration || UnityEngine.InputSystem.Gamepad.current == null) return;
            Instance._rumble = Mathf.Clamp01(Mathf.Max(Instance._rumble, pAmount));
        }

        private void UpdateRumble()
        {
            UnityEngine.InputSystem.Gamepad lPad = UnityEngine.InputSystem.Gamepad.current;
            if (lPad == null) return;
            if (Paused || _rumble <= 0.01f)
            {
                if (_rumble > 0f) lPad.SetMotorSpeeds(0f, 0f);
                _rumble = 0f;
                return;
            }
            lPad.SetMotorSpeeds(_rumble * 0.6f, _rumble);
            _rumble = Mathf.MoveTowards(_rumble, 0f, Time.unscaledDeltaTime * 4f);
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
            if (PostFX.Instance != null) PostFX.Instance.Flash(pColor, pAmount * GameSession.FlashAmount);
        }

        public static void Pulse(float pAmount)
        {
            if (PostFX.Instance != null) PostFX.Instance.Pulse(pAmount * GameSession.AberrationAmount);
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

        // Anime impact frame, only ever called by Impacts : the real scene is inked
        // for a few frames (inverted first), thin manga lines around the edges, Vefects scene-sampled sprite.
        public static void ImpactFrame(float pStrength = 1f, Vector3? pWorldPoint = null, int pInkFrames = 0)
        {
            Juice lJuice = Instance;
            Theme lTheme = RaceContext.Theme;
            if (PostFX.Instance != null)
            {
                if (lTheme != null) PostFX.Instance.ImpactColor(lTheme.accent);
                // Short ink on a block : the 3D burst in the world does the talking.
                PostFX.Instance.Impact(pInkFrames > 0 ? pInkFrames : pWorldPoint.HasValue ? 2 : pStrength > 0.9f ? 4 : 3);
            }
            lJuice.ShowImpactOverlay(Mathf.Clamp01(pStrength));
            Shake(0.2f + 0.2f * Mathf.Clamp01(pStrength));
            GameObject lPrefab = GameAssets.Instance.impactFramePrefab;
            if (lPrefab != null && CameraRig.Instance != null && pStrength > 0.55f)
            {
                Transform lCam = CameraRig.Instance.transform;
                GameObject lFx;
                if (pWorldPoint.HasValue)
                {
                    // Set in the world on the block : it bends and inks the terrain and the pieces around it in 3D.
                    Vector3 lAt = pWorldPoint.Value;
                    float lDistance = Mathf.Max(4f, Vector3.Distance(lCam.position, lAt));
                    lFx = Instantiate(lPrefab, lAt, Quaternion.LookRotation(lAt - lCam.position, lCam.up));
                    lFx.transform.localScale = Vector3.one * (lDistance / 3f) * 0.75f;
                    foreach (ParticleSystem lSystem in lFx.GetComponentsInChildren<ParticleSystem>())
                    {
                        ParticleSystem.MainModule lMain = lSystem.main;
                        lMain.scalingMode = ParticleSystemScalingMode.Hierarchy;
                    }
                    FX.Shockwave(lAt, (lCam.position - lAt).normalized, Color.white, lDistance * 0.35f, 0.25f);
                    if (lTheme != null) FX.Shockwave(lAt, Vector3.up, lTheme.accent, lDistance * 0.5f, 0.35f);
                }
                else
                {
                    lFx = Instantiate(lPrefab, lCam);
                    lFx.transform.localPosition = new Vector3(0f, 0f, 3f);
                    lFx.transform.localRotation = Quaternion.identity;
                    // Wide angle : drawn by the main camera over the recomposed picture, never inside one face.
                    if (PanoramaRig.Active != null && PanoramaRig.Active.On)
                        foreach (Transform lPart in lFx.GetComponentsInChildren<Transform>(true)) lPart.gameObject.layer = PanoramaRig.LAYER;
                }
                // The plugin distorts what is on screen : give it the colours of the place too.
                if (lTheme != null)
                {
                    MaterialPropertyBlock lBlock = new MaterialPropertyBlock();
                    lBlock.SetColor("_LUTCoreColor", Color.Lerp(lTheme.accent, Color.black, 0.75f));
                    lBlock.SetColor("_LUTCoreColorInverted", Color.Lerp(lTheme.fog, Color.white, 0.6f));
                    foreach (ParticleSystemRenderer lRenderer in lFx.GetComponentsInChildren<ParticleSystemRenderer>()) lRenderer.SetPropertyBlock(lBlock);
                }
                Destroy(lFx, 1.2f);
            }
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
            UpdateRumble();
            if (_impactCanvas == null || !_impactCanvas.gameObject.activeSelf) return;
            if (Paused)
            {
                _impactCanvas.gameObject.SetActive(false);
                return;
            }
            _impactTimer -= Time.unscaledDeltaTime;
            float lT = Mathf.Clamp01(_impactTimer / 0.12f);
            _impactMaterial.SetFloat("_Intensity", lT * _impactStrength * 0.6f);
            _impactBurst.color = new Color(1f, 1f, 1f, 0f);
            _impactBurst.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 0.6f, lT);
            if (_impactTimer <= 0f) _impactCanvas.gameObject.SetActive(false);
        }
    }
}
