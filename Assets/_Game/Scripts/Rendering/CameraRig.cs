using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Com.IsartDigital.Hyperspeed
{
    public interface ICameraTarget
    {
        Vector3 FocusPosition { get; }
        Vector3 CameraUp { get; }
        Vector3 CameraForward { get; }
        Vector3 AimDirection { get; }
        float Speed01 { get; }
        float CameraRoll { get; }
        bool HyperMode { get; }
        float Dive01 { get; }
    }

    // Free third person camera : it always orbits where the mouse looks, behind the runner, in the frame of the surface
    // it runs on. It falls far back as speed builds, gets thrown back by every kick and catches up on a spring, and rolls
    // hard. Every change is smoothed. Shake never feeds back into the position, and a paused game freezes it completely.
    public class CameraRig : MonoBehaviour
    {
        private const float FOV_MIN = 105f;
        private const float FOV_MAX = 145f;
        private const float FOV_CLAMP = 155f;
        // How far the focus may trail behind the runner along the view's up (bumps are filtered, long falls are not).
        private const float FOCUS_VERTICAL_LAG = 5f;
        private const float FOV_HYPER = 4f;
        private const float FOV_RATE = 140f;
        private const float KICK_MAX = 16f;
        private const float LAG_MAX = 26f;

        public static CameraRig Instance { get; private set; }

        public Camera Cam { get; private set; }
        public ICameraTarget target;
        public bool freeLook;
        // Title : frames the runner off centre (projection shift), and closer.
        public float lensShiftX;
        public float distanceScale = 1f;

        private PlayerController _player;
        private LevelLayout _layout;
        private Vector3 _up = Vector3.up;
        private Vector3 _look = Vector3.forward;
        private Vector3 _velocity;
        private Vector3 _base;
        // Camera offset from the focus, smoothed in the runner's frame : no lag that grows with speed.
        private Vector3 _offset;
        private Vector3 _focus;
        private float _lag;
        private float _lagVelocity;
        private float _kick;
        private float _kickTarget;
        private float _fov = 90f;
        private float _fovVelocity;
        private float _trauma;
        private float _roll;
        private float _dive;
        private float _hyperBlend;
        private float _lightBlend;
        private bool _finishMode;
        private Vector3 _finishPosition;
        private float _blockDistance = 1f;
        // 0 on open ground, 1 inside a loop / twist : the view tucks in and rises so it stays inside the curve.
        private float _curl;
        private float _finishOrbit;
        private float _windTime;

        private bool _override;
        private float _overrideBlend;
        private float _overrideRate = 6f;
        private Vector3 _overridePosition;
        private Vector3 _overrideLook;
        private float _overrideFov;
        private Vector3 _shotPosition;
        private Quaternion _shotRotation = Quaternion.identity;

        public static Camera CreateCamera(string pName = "Main Camera")
        {
            GameObject lObject = new GameObject(pName);
            lObject.tag = "MainCamera";
            Camera lCamera = lObject.AddComponent<Camera>();
            lCamera.nearClipPlane = 0.3f;
            lCamera.farClipPlane = 16000f;
            lCamera.fieldOfView = 80f;
            lCamera.cullingMask &= ~(1 << UIPerspective.LAYER);
            lObject.AddComponent<AudioListener>();
            UniversalAdditionalCameraData lData = lObject.AddComponent<UniversalAdditionalCameraData>();
            lData.renderPostProcessing = true;
            lData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            lData.requiresColorTexture = true;
            lData.requiresDepthTexture = true;
            return lCamera;
        }

        private void Awake()
        {
            Instance = this;
            Cam = GetComponent<Camera>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetTarget(PlayerController pPlayer, LevelLayout pLayout)
        {
            target = pPlayer;
            _player = pPlayer;
            _layout = pLayout;
        }

        public void Snap()
        {
            if (target == null) return;
            _up = target.CameraUp;
            _look = target.AimDirection;
            _focus = target.FocusPosition;
            _base = DesiredPosition(target.Speed01);
            _offset = _base - _focus;
            transform.position = _base;
            transform.rotation = Quaternion.LookRotation(LookPoint(target.Speed01) - _base, _up);
            _velocity = Vector3.zero;
        }

        // Cinematic shot (start swoop, death hold, rewind) : the rig blends toward it and back when cleared.
        public void SetOverride(Vector3 pPosition, Vector3 pLook, float pFov = -1f, float pBlendTime = 0.12f)
        {
            if (!_override) _overrideRate = 1f / Mathf.Max(0.01f, pBlendTime);
            if (!_override && _overrideBlend <= 0.001f)
            {
                _shotPosition = transform.position;
                _shotRotation = transform.rotation;
            }
            _override = true;
            _overridePosition = pPosition;
            _overrideLook = pLook;
            _overrideFov = pFov > 0f ? pFov : _fov;
        }

        public void CutTo(Vector3 pPosition, Vector3 pLook, float pFov)
        {
            SetOverride(pPosition, pLook, pFov);
            _overrideBlend = 1f;
            _shotPosition = pPosition;
            _shotRotation = Quaternion.LookRotation(pLook - pPosition, Vector3.up);
        }

        public void ClearOverride(float pBlendTime = 0.3f)
        {
            _override = false;
            _overrideRate = 1f / Mathf.Max(0.01f, pBlendTime);
        }

        public void SetFinishMode(bool pValue)
        {
            _finishMode = pValue;
            _finishOrbit = 0f;
            _finishPosition = transform.position;
        }

        public void AddTrauma(float pAmount) => _trauma = Mathf.Clamp01(_trauma + pAmount);

        // Additive FOV kick with an envelope : never a sudden in / out pump.
        public void FovKick(float pAmount) => _kickTarget = Mathf.Min(KICK_MAX, Mathf.Max(_kickTarget, pAmount) + pAmount * 0.2f);

        // Thrown back : the camera falls behind, then the spring brings it home.
        public void Rollback(float pAmount) => _lag = Mathf.Min(LAG_MAX, Mathf.Max(_lag, pAmount) + pAmount * 0.3f);

        // Far back and well above the runner, looking down onto it : the runner is small in a huge world, and the
        // ground's bumps barely move the view. Even the slowest run sits twice as far as before.
        private Vector3 DesiredPosition(float pSpeed)
        {
            float lDistance = (freeLook ? 14f : Mathf.Lerp(18f, 60f, Mathf.Sqrt(pSpeed)) + _lightBlend * 20f + _lag) * distanceScale;
            float lHeight = (freeLook ? 4f : Mathf.Lerp(7f, 16f, pSpeed)) * distanceScale;
            // In a loop the line behind you curves away under the track : come closer and higher instead.
            lDistance *= Mathf.Lerp(1f, 0.42f, _curl);
            lHeight *= Mathf.Lerp(1f, 1.6f, _curl);
            return _focus + _up * lHeight - _look * lDistance;
        }

        private Vector3 LookPoint(float pSpeed)
        {
            float lAhead = Mathf.Lerp(18f, 60f, Mathf.Sqrt(pSpeed)) * 0.25f * distanceScale;
            return _focus + _up * Mathf.Lerp(1.2f, 2.5f, pSpeed) * distanceScale + _look * lAhead;
        }

        // Follows the runner exactly along the ground, but filters its height : bumps and steps are smoothed out.
        private void UpdateFocus(float pDt)
        {
            Vector3 lRaw = target.FocusPosition;
            Vector3 lDelta = lRaw - _focus;
            float lVertical = Vector3.Dot(lDelta, _up);
            Vector3 lFlat = lDelta - _up * lVertical;
            float lKeep = lVertical * Mathf.Exp(-9f * pDt);
            lKeep = Mathf.Clamp(lKeep, -FOCUS_VERTICAL_LAG, FOCUS_VERTICAL_LAG);
            _focus = lRaw - _up * lKeep;
            if (lFlat.sqrMagnitude > 250000f) _focus = lRaw;
        }

        private void LateUpdate()
        {
            if (target == null && !_override) return;
            float lDt = Time.deltaTime;
            float lUnscaled = Time.unscaledDeltaTime;
            bool lFrozen = Time.timeScale <= 0f;
            float lTargetBlend = _override ? 1f : 0f;
            if (!lFrozen) _overrideBlend = Mathf.MoveTowards(_overrideBlend, lTargetBlend, lUnscaled * _overrideRate);
            if (target == null)
            {
                ApplyShot(lUnscaled);
                transform.SetPositionAndRotation(_shotPosition, _shotRotation);
                Cam.fieldOfView = _overrideFov;
                return;
            }
            // Paused : the image stays exactly where it is.
            if (lFrozen) return;

            float lSpeed = target.Speed01;
            _curl = Mathf.MoveTowards(_curl, CurlTarget(), lDt * 2.5f);
            // The up only turns where gravity turns (the target gives a stable frame) : slowly on open ground, tightly
            // inside loops so the view never lags outside the curve.
            _up = Vector3.Slerp(_up, target.CameraUp, 1f - Mathf.Exp(-Mathf.Lerp(4f, 11f, _curl) * lDt)).normalized;
            // The mouse is followed with some weight : the view swings, it never snaps.
            _look = Vector3.Slerp(_look, target.AimDirection, 1f - Mathf.Exp(-12f * lDt)).normalized;
            UpdateFocus(lDt);

            _hyperBlend = Mathf.MoveTowards(_hyperBlend, target.HyperMode ? 1f : 0f, lDt * 1.5f);
            _lightBlend = Mathf.MoveTowards(_lightBlend, _player != null && _player.LightForm ? 1f : 0f, lDt * 1.2f);
            _lag = Mathf.SmoothDamp(_lag, 0f, ref _lagVelocity, 0.55f, Mathf.Infinity, lDt);
            _kick = Mathf.MoveTowards(_kick, _kickTarget, lUnscaled * KICK_MAX / 0.08f);
            _kickTarget = Mathf.MoveTowards(_kickTarget, 0f, lUnscaled * KICK_MAX / 0.7f);
            _trauma = Mathf.MoveTowards(_trauma, 0f, lUnscaled * 1.4f);
            _roll = Mathf.Lerp(_roll, target.CameraRoll, 1f - Mathf.Exp(-6f * lDt));
            _dive = Mathf.Lerp(_dive, target.Dive01, 1f - Mathf.Exp(-5f * lDt));

            Vector3 lDesired = DesiredPosition(lSpeed);
            Vector3 lLook = LookPoint(lSpeed);
            if (_finishMode)
            {
                // Victory : a slow orbit around the runner, who keeps on running.
                _finishOrbit += lDt * 22f;
                Vector3 lBack = Quaternion.AngleAxis(_finishOrbit, _up) * -_look;
                lDesired = _focus + lBack * 16f + _up * 5f;
                lLook = target.FocusPosition + _up * 1f;
                _base = Vector3.SmoothDamp(_base, lDesired, ref _velocity, 0.35f, Mathf.Infinity, lDt);
                _offset = _base - _focus;
            }
            else
            {
                lDesired = AvoidWorld(lDesired, lDt);
                _offset = Vector3.SmoothDamp(_offset, lDesired - _focus, ref _velocity, freeLook ? 0.12f : 0.1f, Mathf.Infinity, lDt);
                _base = _focus + _offset;
            }
            Quaternion lRotation = Quaternion.LookRotation(lLook - _base, _up) * Quaternion.Euler(0f, 0f, freeLook ? 0f : _roll);

            // Rumble grows with speed : at full speed the frame never rests.
            float lShake = _trauma * _trauma + lSpeed * lSpeed * 0.06f + _hyperBlend * 0.02f + _lightBlend * 0.04f + _dive * 0.05f;
            float lTime = Time.unscaledTime * 24f;
            Vector3 lShakeOffset = new Vector3(Mathf.PerlinNoise(lTime, 0.1f) - 0.5f, Mathf.PerlinNoise(0.3f, lTime) - 0.5f, 0f) * lShake * 1.4f;
            Quaternion lShakeRot = Quaternion.Euler((Mathf.PerlinNoise(lTime, 5.2f) - 0.5f) * lShake * 5f, (Mathf.PerlinNoise(7.1f, lTime) - 0.5f) * lShake * 5f, (Mathf.PerlinNoise(lTime, 9.4f) - 0.5f) * lShake * 8f);
            // Diving : a slow sway, like the wind pushing the camera around.
            _windTime += lDt * 0.6f;
            float lWind = _dive;
            Vector3 lWindOffset = new Vector3(Mathf.PerlinNoise(_windTime, 3.3f) - 0.5f, Mathf.PerlinNoise(8.1f, _windTime) - 0.5f, 0f) * lWind * 3.2f;
            Quaternion lWindRot = Quaternion.Euler((Mathf.PerlinNoise(_windTime, 11f) - 0.5f) * lWind * 5f, 0f, (Mathf.PerlinNoise(13f, _windTime) - 0.5f) * lWind * 9f);
            Vector3 lPosition = _base + lRotation * (lShakeOffset + lWindOffset);
            lRotation *= lShakeRot * lWindRot;
            if (!_finishMode) lPosition = KeepAboveSurface(lPosition);

            float lCalm = _player != null ? Mathf.InverseLerp(260f, 450f, _player.Profile.cruise) : 1f;
            float lFovTarget = Mathf.Lerp(FOV_MIN - 8f + lCalm * 8f, FOV_MAX, Mathf.Sqrt(lSpeed)) + _hyperBlend * FOV_HYPER + _lightBlend * 10f + _kick + _dive * 10f;
            if (freeLook) lFovTarget = 84f;
            _fov = Mathf.SmoothDamp(_fov, lFovTarget, ref _fovVelocity, 0.22f, FOV_RATE, lDt);

            if (_overrideBlend > 0f)
            {
                ApplyShot(lUnscaled);
                float lBlend = HSMath.Smooth(_overrideBlend);
                lPosition = Vector3.Lerp(lPosition, _shotPosition, lBlend);
                lRotation = Quaternion.Slerp(lRotation, _shotRotation, lBlend);
                transform.SetPositionAndRotation(lPosition, lRotation);
                Cam.fieldOfView = Mathf.Lerp(_fov, _overrideFov, lBlend);
                if (_overrideBlend >= 1f) _velocity = Vector3.zero;
                return;
            }
            transform.SetPositionAndRotation(lPosition, lRotation);
            Cam.fieldOfView = Mathf.Clamp(_fov, 40f, FOV_CLAMP);
            if (lensShiftX != 0f)
            {
                Cam.ResetProjectionMatrix();
                Matrix4x4 lProjection = Cam.projectionMatrix;
                lProjection[0, 2] = lensShiftX;
                Cam.projectionMatrix = lProjection;
            }
        }

        // Anything solid between the runner and the camera pulls it in fast and lets it out slowly.
        private Vector3 AvoidWorld(Vector3 pDesired, float pDt)
        {
            // Only a real block of the line of sight counts : the ray starts a little above the runner so the
            // ground it stands on, or the flank it slides on, never pulls the camera in.
            Vector3 lFocus = _focus + _up * 1.5f;
            Vector3 lDelta = pDesired - lFocus;
            float lLength = lDelta.magnitude;
            if (lLength < 0.01f) return pDesired;
            float lWanted = 1f;
            RaycastHit lHit;
            if (Physics.SphereCast(lFocus, 0.4f, lDelta / lLength, out lHit, lLength, (1 << Layers.PROP) | (1 << Layers.DECK), QueryTriggerInteraction.Ignore) && lHit.distance > 2f)
                lWanted = Mathf.Clamp01((lHit.distance - 0.5f) / lLength);
            _blockDistance = lWanted < _blockDistance ? Mathf.Lerp(_blockDistance, lWanted, 1f - Mathf.Exp(-40f * pDt)) : Mathf.Lerp(_blockDistance, lWanted, 1f - Mathf.Exp(-3f * pDt));
            return lFocus + lDelta * _blockDistance;
        }

        private float CurlTarget()
        {
            if (_player == null || _layout == null || freeLook) return 0f;
            Spine lSpine = _layout.spine;
            float lAhead = Mathf.Max(30f, _player.Speed * 0.35f);
            if (lSpine.samples[lSpine.IndexOf(_player.Progress)].localGravity) return 1f;
            if (lSpine.samples[lSpine.IndexOf(_player.Progress + lAhead)].localGravity) return 0.6f;
            return 0f;
        }

        // Last guard : the camera never ends up under or inside the surface the runner is on.
        private Vector3 KeepAboveSurface(Vector3 pPosition)
        {
            Vector3 lFrom = _focus + _up * 3f;
            Vector3 lDelta = pPosition - lFrom;
            float lLength = lDelta.magnitude;
            RaycastHit lHit;
            if (lLength > 0.5f && Physics.SphereCast(lFrom, 0.6f, lDelta / lLength, out lHit, lLength + 1f, 1 << Layers.DECK, QueryTriggerInteraction.Ignore))
                pPosition = lFrom + lDelta / lLength * Mathf.Max(0.5f, lHit.distance - 1.5f) + lHit.normal * 1.5f;
            return pPosition;
        }

        private void ApplyShot(float pDt)
        {
            _shotPosition = Vector3.Lerp(_shotPosition, _overridePosition, 1f - Mathf.Exp(-14f * pDt));
            Quaternion lLook = Quaternion.LookRotation(_overrideLook - _shotPosition, Vector3.up);
            _shotRotation = Quaternion.Slerp(_shotRotation, lLook, 1f - Mathf.Exp(-14f * pDt));
        }
    }
}
