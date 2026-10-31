using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Com.IsartDigital.Hyperspeed
{
    // Runtime volume + global values for the Stylize fullscreen pass, driven by speed and juice events.
    public class PostFX : MonoBehaviour
    {
        private static readonly int ABERRATION = Shader.PropertyToID("_HS_Aberration");
        private static readonly int RADIAL_BLUR = Shader.PropertyToID("_HS_RadialBlur");
        private static readonly int DITHER = Shader.PropertyToID("_HS_Dither");
        private static readonly int IMPACT = Shader.PropertyToID("_HS_Impact");
        private static readonly int FLASH = Shader.PropertyToID("_HS_Flash");
        private static readonly int FLASH_COLOR = Shader.PropertyToID("_HS_FlashColor");
        private static readonly int IMPACT_COLOR = Shader.PropertyToID("_HS_ImpactColor");

        public static PostFX Instance { get; private set; }

        public float speed01;
        public float hyper01;

        private VolumeProfile _profile;
        private Bloom _bloom;
        private ChromaticAberration _chromatic;
        private Vignette _vignette;
        private LensDistortion _lens;
        private ColorAdjustments _color;
        private MotionBlur _motionBlur;

        private float _pulse;
        private float _flash;
        private Color _flashColor = Color.white;
        private int _impactFrames;
        private float _impactTimer;
        private Color _vignetteColor = Color.black;
        private float _vignetteBoost;

        private void Awake()
        {
            Instance = this;
            Volume lVolume = gameObject.AddComponent<Volume>();
            lVolume.isGlobal = true;
            lVolume.priority = 10f;
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            lVolume.profile = _profile;

            _bloom = _profile.Add<Bloom>(true);
            _bloom.intensity.Override(1.1f);
            _bloom.threshold.Override(0.95f);
            _bloom.scatter.Override(0.65f);

            _chromatic = _profile.Add<ChromaticAberration>(true);
            _vignette = _profile.Add<Vignette>(true);
            _vignette.smoothness.Override(0.45f);
            _lens = _profile.Add<LensDistortion>(true);

            _color = _profile.Add<ColorAdjustments>(true);
            _color.saturation.Override(22f);
            _color.contrast.Override(14f);

            Tonemapping lTonemap = _profile.Add<Tonemapping>(true);
            lTonemap.mode.Override(TonemappingMode.Neutral);

            _motionBlur = _profile.Add<MotionBlur>(true);
            _motionBlur.mode.Override(MotionBlurMode.CameraOnly);
            _motionBlur.quality.Override(MotionBlurQuality.Medium);

            Shader.SetGlobalColor(IMPACT_COLOR, UIFactory.RED);
            Shader.SetGlobalFloat(IMPACT, 0f);
            Shader.SetGlobalFloat(FLASH, 0f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Shader.SetGlobalFloat(IMPACT, 0f);
            Shader.SetGlobalFloat(FLASH, 0f);
            Shader.SetGlobalFloat(ABERRATION, 0f);
            Shader.SetGlobalFloat(RADIAL_BLUR, 0f);
        }

        public void Pulse(float pAmount) => _pulse = Mathf.Max(_pulse, pAmount);

        public void Flash(Color pColor, float pAmount)
        {
            _flashColor = pColor;
            _flash = Mathf.Max(_flash, pAmount);
        }

        public void Impact(int pFrames)
        {
            _impactFrames = Mathf.Max(_impactFrames, pFrames);
            _impactTimer = 0f;
        }

        public void VignetteColor(Color pColor, float pAmount)
        {
            _vignetteColor = pColor;
            _vignetteBoost = Mathf.Max(_vignetteBoost, pAmount);
        }

        private void LateUpdate()
        {
            float lDt = Time.unscaledDeltaTime;
            _pulse = Mathf.MoveTowards(_pulse, 0f, lDt * 2.5f);
            _flash = Mathf.MoveTowards(_flash, 0f, lDt * 5f);
            _vignetteBoost = Mathf.MoveTowards(_vignetteBoost, 0f, lDt * 1.5f);

            float lSpeed = speed01;
            _chromatic.intensity.Override(Mathf.Clamp01(0.08f + lSpeed * 0.45f + _pulse * 0.6f + hyper01 * 0.3f));
            _vignette.intensity.Override(0.22f + lSpeed * 0.18f + _vignetteBoost * 0.25f);
            _vignette.color.Override(Color.Lerp(Color.black, _vignetteColor, Mathf.Clamp01(_vignetteBoost * 1.5f)));
            _lens.intensity.Override(-(lSpeed * 0.28f + _pulse * 0.2f + hyper01 * 0.15f));
            _lens.scale.Override(1f + lSpeed * 0.04f);
            _motionBlur.intensity.Override(lSpeed * 0.18f);
            _bloom.intensity.Override(1.0f + hyper01 * 1.2f + _pulse * 0.8f);
            _color.postExposure.Override(_pulse * 0.25f + hyper01 * 0.15f);

            Shader.SetGlobalFloat(ABERRATION, lSpeed * 0.8f + _pulse * 1.6f + hyper01 * 1.2f);
            Shader.SetGlobalFloat(RADIAL_BLUR, Mathf.Clamp01(lSpeed - 0.35f) * 0.9f + _pulse * 0.6f + hyper01 * 0.8f);
            Shader.SetGlobalFloat(DITHER, 0.55f);
            Shader.SetGlobalFloat(FLASH, _flash);
            Shader.SetGlobalColor(FLASH_COLOR, _flashColor);

            if (_impactFrames > 0)
            {
                _impactTimer += lDt;
                Shader.SetGlobalFloat(IMPACT, _impactTimer < 0.035f ? 1f : 0.4f);
                if (_impactTimer >= 0.035f * _impactFrames)
                {
                    _impactFrames = 0;
                    Shader.SetGlobalFloat(IMPACT, 0f);
                }
            }
        }
    }
}
