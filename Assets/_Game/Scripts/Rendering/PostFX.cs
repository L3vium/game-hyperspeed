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
        private static readonly int REWIND = Shader.PropertyToID("_HS_Rewind");
        private static readonly int DESATURATE = Shader.PropertyToID("_HS_Desaturate");
        private static readonly int FISHEYE = Shader.PropertyToID("_HS_Fisheye");
        private static readonly int FADE = Shader.PropertyToID("_HS_Fade");
        private static readonly int LETTERBOX = Shader.PropertyToID("_HS_Letterbox");
        private static readonly int INTENSITY = Shader.PropertyToID("_HS_Intensity");

        public static PostFX Instance { get; private set; }

        public float speed01;
        public float hyper01;
        public float light01;
        private float _light;
        // Death rewind look (desaturated, scanlines) and pause backdrop (game visible but dimmed).
        public float rewind;
        public float pause;
        // 0..1 black fade (death), letterbox height as a fraction of the screen, colour intensity (climax, victory).
        public float fade;
        public float letterbox;
        public float intensity;
        private float _fade;
        private float _letterbox;
        private float _intensity;

        private VolumeProfile _profile;
        private Bloom _bloom;
        private ChromaticAberration _chromatic;
        private Vignette _vignette;
        private LensDistortion _lens;
        private ColorAdjustments _color;
        private MotionBlur _motionBlur;
        private PaniniProjection _panini;
        private float _rewind;
        private float _pause;

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
            _color.saturation.Override(30f);
            _color.contrast.Override(14f);

            Tonemapping lTonemap = _profile.Add<Tonemapping>(true);
            lTonemap.mode.Override(TonemappingMode.Neutral);

            _motionBlur = _profile.Add<MotionBlur>(true);
            _motionBlur.mode.Override(MotionBlurMode.CameraOnly);
            _motionBlur.quality.Override(MotionBlurQuality.Medium);

            // Keeps the screen edges readable at very wide FOV.
            _panini = _profile.Add<PaniniProjection>(true);
            _panini.cropToFit.Override(1f);

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
            Shader.SetGlobalFloat(REWIND, 0f);
            Shader.SetGlobalFloat(DESATURATE, 0f);
            Shader.SetGlobalFloat(FISHEYE, 0f);
            Shader.SetGlobalFloat(FADE, 0f);
            Shader.SetGlobalFloat(LETTERBOX, 0f);
            Shader.SetGlobalFloat(INTENSITY, 0f);
        }

        public void Pulse(float pAmount) => _pulse = Mathf.Max(_pulse, pAmount);

        public void Flash(Color pColor, float pAmount)
        {
            _flashColor = pColor;
            _flash = Mathf.Max(_flash, pAmount);
        }

        // Ink of the impact frames : the colour of the place you are in.
        public void ImpactColor(Color pColor) => Shader.SetGlobalColor(IMPACT_COLOR, pColor);

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
            _rewind = Mathf.MoveTowards(_rewind, rewind, lDt * 6f);
            _pause = Mathf.MoveTowards(_pause, pause, lDt * 6f);
            // Clarity first : the center of the screen (runner + route) stays sharp at any speed.
            _chromatic.intensity.Override(Mathf.Clamp01((0.18f + lSpeed * 0.7f + _pulse * 0.7f + hyper01 * 0.25f + _light * 0.3f) * GameSession.AberrationAmount + _rewind * 0.6f));
            _vignette.intensity.Override(0.2f + lSpeed * 0.16f + _vignetteBoost * 0.2f + _pause * 0.25f);
            _vignette.color.Override(Color.Lerp(Color.black, _vignetteColor, Mathf.Clamp01(_vignetteBoost * 1.5f)));
            _light = Mathf.MoveTowards(_light, light01, lDt * 2f);
            _lens.intensity.Override(-(lSpeed * 0.3f + _pulse * 0.14f + _light * 0.14f));
            _lens.scale.Override(1f);
            _panini.distance.Override(Mathf.Clamp01((lSpeed - 0.15f) * 0.7f));
            _motionBlur.intensity.Override(0.05f + lSpeed * 0.3f);
            _bloom.intensity.Override(0.8f + lSpeed * 0.5f + hyper01 * 0.3f + _pulse * 0.8f + _light * 0.6f);
            _color.postExposure.Override(_pulse * 0.15f + hyper01 * 0.1f - _pause * 0.35f);
            _color.saturation.Override(30f - _pause * 40f);

            Shader.SetGlobalFloat(ABERRATION, (0.3f + lSpeed * 2f + _pulse * 2.4f + hyper01 * 1.2f + _light * 1.2f) * GameSession.AberrationAmount + _rewind * 2f);
            Shader.SetGlobalFloat(RADIAL_BLUR, Mathf.Min(2.2f, Mathf.Clamp01(lSpeed - 0.06f) * 1.35f + _pulse * 0.8f + hyper01 * 0.35f + _light * 0.5f));
            Shader.SetGlobalFloat(REWIND, _rewind);
            Shader.SetGlobalFloat(DESATURATE, _rewind * 0.75f);
            // Flat colour : no dither, no posterize.
            Shader.SetGlobalFloat(DITHER, 0f);
            _fade = Mathf.MoveTowards(_fade, fade, lDt * 5f);
            _letterbox = Mathf.MoveTowards(_letterbox, letterbox, lDt * 0.6f);
            _intensity = Mathf.MoveTowards(_intensity, intensity, lDt * 1.5f);
            Shader.SetGlobalFloat(FISHEYE, 0f);
            Shader.SetGlobalFloat(FADE, _fade);
            Shader.SetGlobalFloat(LETTERBOX, _letterbox);
            Shader.SetGlobalFloat(INTENSITY, _intensity);
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
