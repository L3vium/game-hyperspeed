using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Com.IsartDigital.Hyperspeed
{
    // A burst of post process that fades over its duration (juice events : dash, pads, breaks...).
    public struct PostKick
    {
        public float aberration;
        public float radial;
        public float bloom;
        public float exposure;
        public float lens;
        public float saturation;
        public float duration;
        public AnimationCurve fade;
        public float start;
    }

    // Runtime volume + global values for the Stylize fullscreen pass, driven by speed and juice events.
    public class PostFX : MonoBehaviour
    {
        private const int MAX_KICKS = 16;
        private readonly System.Collections.Generic.List<PostKick> _kicks = new System.Collections.Generic.List<PostKick>();
        // Sum of the running kicks, this frame.
        private float _kAberration, _kRadial, _kBloom, _kExposure, _kLens, _kSaturation;

        public void Kick(PostKick pKick)
        {
            pKick.start = Time.unscaledTime;
            if (_kicks.Count >= MAX_KICKS) _kicks.RemoveAt(0);
            _kicks.Add(pKick);
        }

        private void SumKicks()
        {
            _kAberration = _kRadial = _kBloom = _kExposure = _kLens = _kSaturation = 0f;
            float lNow = Time.unscaledTime;
            for (int i = _kicks.Count - 1; i >= 0; i--)
            {
                PostKick lKick = _kicks[i];
                float lT = (lNow - lKick.start) / lKick.duration;
                if (lT >= 1f)
                {
                    _kicks.RemoveAt(i);
                    continue;
                }
                float lW = lKick.fade != null ? lKick.fade.Evaluate(lT) : 1f - lT;
                _kAberration += lKick.aberration * lW;
                _kRadial += lKick.radial * lW;
                _kBloom += lKick.bloom * lW;
                _kExposure += lKick.exposure * lW;
                _kLens += lKick.lens * lW;
                _kSaturation += lKick.saturation * lW;
            }
        }
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
        private static readonly int WRONG_WAY = Shader.PropertyToID("_HS_WrongWay");

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
        // 0..1 : the red wrong-way vignette (fades in and out softly).
        public float wrongWay;
        private float _wrongWay;
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
            Shader.SetGlobalFloat(WRONG_WAY, 0f);
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
            SumKicks();
            // The wide angle view already bends the picture : no Panini, no camera motion blur (wrong on a recomposed
            // image), a lighter lens. The speed state looks add their own screen.
            const bool lWide = false;
            HSSpeedStateLook lHyper = HSSpeedStateLook.Get(SpeedState.Hyper);
            HSSpeedStateLook lLight = HSSpeedStateLook.Get(SpeedState.Light);
            float lStateAberration = hyper01 * lHyper.aberration + _light * lLight.aberration;
            float lStateRadial = hyper01 * lHyper.radialBlur + _light * lLight.radialBlur;
            float lStateBloom = hyper01 * lHyper.bloom + _light * lLight.bloom;
            float lStateSaturation = hyper01 * lHyper.saturation + _light * lLight.saturation;
            _rewind = Mathf.MoveTowards(_rewind, rewind, lDt * 6f);
            _pause = Mathf.MoveTowards(_pause, pause, lDt * 6f);
            // Clarity first : the center of the screen (runner + route) stays sharp at any speed.
            // 2026-11-12 : a constant chromatic aberration in the whole race (stronger at the edges, the centre stays sharp).
            _chromatic.intensity.Override(Mathf.Clamp01((0.38f + lSpeed * 0.6f + _pulse * 0.7f + hyper01 * 0.25f + _light * 0.3f + lStateAberration * 0.3f) * GameSession.AberrationAmount + _kAberration * 0.5f + _rewind * 0.6f));
            _vignette.intensity.Override(0.2f + lSpeed * 0.16f + _vignetteBoost * 0.2f + _pause * 0.25f);
            _vignette.color.Override(Color.Lerp(Color.black, _vignetteColor, Mathf.Clamp01(_vignetteBoost * 1.5f)));
            _light = Mathf.MoveTowards(_light, light01, lDt * 2f);
            _lens.intensity.Override(Mathf.Clamp(-(lSpeed * 0.3f + _pulse * 0.14f + _light * 0.14f + _kLens) * (lWide ? 0.35f : 1f), -0.85f, 0f));
            _lens.scale.Override(1f);
            _panini.distance.Override(lWide ? 0f : Mathf.Clamp01((lSpeed - 0.15f) * 0.7f));
            // Readability first (2026-11-12) : speed is shown by lines and colour, not by smearing the track ahead.
            _motionBlur.intensity.Override(lWide ? 0f : 0.03f + lSpeed * 0.12f);
            // The sun and the glows must never blind the track ahead (2026-11-12).
            _bloom.intensity.Override(Mathf.Min(1.6f, 0.55f + lSpeed * 0.25f + hyper01 * 0.15f + _pulse * 0.4f + _light * 0.4f + _kBloom * 0.5f + lStateBloom * 0.5f));
            _color.postExposure.Override(_pulse * 0.15f + hyper01 * 0.1f - _pause * 0.35f + _kExposure);
            _color.saturation.Override(Mathf.Clamp(30f - _pause * 40f + _kSaturation + lStateSaturation, -100f, 100f));

            Shader.SetGlobalFloat(ABERRATION, (0.9f + lSpeed * 2f + _pulse * 2.4f + hyper01 * 1.2f + _light * 1.2f + lStateAberration * 2f) * GameSession.AberrationAmount + _kAberration * 2.4f + _rewind * 2f);
            // 2026-11-14 : half the blur it had, and capped : the track ahead must stay readable at any speed (the
            // aberration above carries the speed instead).
            Shader.SetGlobalFloat(RADIAL_BLUR, Mathf.Min(0.55f, (Mathf.Clamp01(lSpeed - 0.2f) * 0.3f + _pulse * 0.4f + hyper01 * 0.08f + _light * 0.25f + _kRadial * 0.5f + lStateRadial * 0.5f) * 0.5f));
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
            _wrongWay = Mathf.MoveTowards(_wrongWay, wrongWay, lDt * (wrongWay > _wrongWay ? 2.5f : 1.2f));
            Shader.SetGlobalFloat(WRONG_WAY, _wrongWay);
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
