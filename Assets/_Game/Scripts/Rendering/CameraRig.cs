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
        private const float FOV_CLAMP = 160f;
        // Wide angle : FOV kicks (given in classic degrees) count this much more on the panorama, max horizontal FOV.
        private const float PANO_KICK_SCALE = 2.5f;
        private const float PANO_FOV_MAX = 330f;
        // How far the focus may trail behind the runner along the view's up (bumps are filtered, long falls are not).
        private const float FOCUS_VERTICAL_LAG = 5f;
        private const float FOV_RATE = 140f;
        // Room for the juice : a pad or a dash may kick the FOV and throw the camera back a long way.
        private const float KICK_MAX = 45f;
        private const float LAG_MAX = 70f;

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
        // Wide angle view (PanoramaRig) : horizontal FOV, shape and cylinder, smoothed.
        private float _hfov = 190f;
        private float _hfovVelocity;
        private float _shape = 0.5f;
        private float _cylinder = 0.55f;
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
        // Rotation punch (degrees pitch / yaw / roll) on a stiff spring : hits you feel in the view.
        private Vector3 _punch;
        private Vector3 _punchVelocity;

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

        // Knocks the view (pitch, yaw, roll in degrees) ; a stiff spring brings it back in a fraction of a second.
        public void Punch(Vector3 pDegrees) => _punch = Vector3.ClampMagnitude(_punch + pDegrees, 25f);

        // Far back and well above the runner, looking down onto it : the runner is small in a huge world, and the
        // ground's bumps barely move the view. Much further at hyper speed, very far at light : a drastic change.
        // Every value comes from the speed state looks (HS_Juice prefab, "Speed States").
        private static HSSpeedStateLook Normal => HSSpeedStateLook.Get(SpeedState.Normal);
        private static HSSpeedStateLook HyperLook => HSSpeedStateLook.Get(SpeedState.Hyper);
        private static HSSpeedStateLook LightLook => HSSpeedStateLook.Get(SpeedState.Light);

        private float BackDistance(float pSpeed) => Mathf.Lerp(Normal.distance.x, Normal.distance.y, Mathf.Sqrt(pSpeed)) + _hyperBlend * HyperLook.distance.x + _lightBlend * LightLook.distance.x;

        private float UpHeight(float pSpeed) => Mathf.Lerp(Normal.height.x, Normal.height.y, pSpeed) + _hyperBlend * HyperLook.height.x + _lightBlend * LightLook.height.x;

        public bool WideAngle => PanoramaRig.Active != null && PanoramaRig.Active.On && PanoramaRig.Active.Main == Cam;

        private float UserScale => freeLook ? 1f : GameSession.CameraDistance;

        private Vector3 DesiredPosition(float pSpeed)
        {
            float lDistance = (freeLook ? 14f : BackDistance(pSpeed) + _lag) * distanceScale * UserScale;
            float lHeight = (freeLook ? 4f : UpHeight(pSpeed)) * distanceScale * UserScale;
            // In a loop the view keeps its distance and rises a little : it stays inside the curve without coming close.
            lHeight *= Mathf.Lerp(1f, 1.25f, _curl);
            return _focus + _up * lHeight - _look * lDistance;
        }

        private Vector3 LookPoint(float pSpeed)
        {
            float lAhead = (freeLook ? 14f : BackDistance(pSpeed)) * 0.25f * distanceScale;
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
            float lUnscaled = Time.unscaledDeltaTime;
            // Paused : frozen. A hit-stop only holds the world : the view keeps answering the aim.
            bool lFrozen = Juice.Paused;
            float lDt = Time.timeScale > 0f ? Time.deltaTime : lUnscaled;
            float lTargetBlend = _override ? 1f : 0f;
            if (!lFrozen) _overrideBlend = Mathf.MoveTowards(_overrideBlend, lTargetBlend, lUnscaled * _overrideRate);
            if (target == null)
            {
                ApplyShot(lUnscaled);
                transform.SetPositionAndRotation(_shotPosition, _shotRotation);
                Cam.fieldOfView = _overrideFov;
                ApplyWide(ProjectionMath.EquivalentHFov(_overrideFov, Cam.aspect, _shape) * 1.15f);
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

            _hyperBlend = Mathf.MoveTowards(_hyperBlend, target.HyperMode ? 1f : 0f, lDt / Mathf.Max(0.05f, HyperLook.blendTime));
            _lightBlend = Mathf.MoveTowards(_lightBlend, _player != null && _player.LightForm ? 1f : 0f, lDt / Mathf.Max(0.05f, LightLook.blendTime));
            _lag = Mathf.SmoothDamp(_lag, 0f, ref _lagVelocity, 0.55f, Mathf.Infinity, lDt);
            _kick = Mathf.MoveTowards(_kick, _kickTarget, lUnscaled * KICK_MAX / 0.08f);
            _kickTarget = Mathf.MoveTowards(_kickTarget, 0f, lUnscaled * KICK_MAX / 0.7f);
            _trauma = Mathf.MoveTowards(_trauma, 0f, lUnscaled * 1.4f);
            _punchVelocity += (-_punch * 260f - _punchVelocity * 22f) * lUnscaled;
            _punch += _punchVelocity * lUnscaled;
            _roll = Mathf.Lerp(_roll, target.CameraRoll, 1f - Mathf.Exp(-6f * lDt));
            _dive = Mathf.Lerp(_dive, target.Dive01, 1f - Mathf.Exp(-5f * lDt));

            Vector3 lDesired = DesiredPosition(lSpeed);
            Vector3 lLook = LookPoint(lSpeed);
            if (_finishMode)
            {
                // Victory : a tracking shot from the runner's left, close, sliding slowly from behind to beside it. The
                // runner keeps running and sits on the right of the frame (the results are on the left), running into it.
                _finishOrbit += lDt;
                Vector3 lRunForward = _player != null ? Vector3.ProjectOnPlane(_player.FacingDirection, _up).normalized : _look;
                if (lRunForward.sqrMagnitude < 0.01f) lRunForward = _look;
                Vector3 lRunRight = Vector3.Cross(_up, lRunForward).normalized;
                float lSlide = HSMath.Smooth(Mathf.Clamp01(_finishOrbit / 5f));
                lDesired = _focus - lRunRight * Mathf.Lerp(7f, 9f, lSlide) - lRunForward * Mathf.Lerp(8f, -1f, lSlide) + _up * Mathf.Lerp(3.5f, 2.4f, lSlide);
                lLook = _focus + _up * 0.6f + lRunForward * Mathf.Lerp(3f, 6f, lSlide) - _up * 1.2f;
                lDesired = AvoidWorld(lDesired, lDt);
                _base = Vector3.SmoothDamp(_base, lDesired, ref _velocity, 0.25f, Mathf.Infinity, lDt);
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
            lRotation *= lShakeRot * lWindRot * Quaternion.Euler(_punch);
            lPosition = KeepAboveSurface(lPosition, lDt);

            float lHyperB = HSMath.Smooth(_hyperBlend);
            float lLightB = HSMath.Smooth(_lightBlend);
            float lFovTarget = Mathf.Lerp(Normal.classicFov.x, Normal.classicFov.y, Mathf.Sqrt(lSpeed)) + lHyperB * HyperLook.classicFov.x + lLightB * LightLook.classicFov.x + _kick + _dive * 10f + GameSession.FovOffset;
            if (freeLook) lFovTarget = 84f;
            _fov = Mathf.SmoothDamp(_fov, lFovTarget, ref _fovVelocity, 0.22f, FOV_RATE, lDt);
            // The wide angle view : the same state machine, in horizontal degrees past 180, with its own shape.
            float lHFovTarget = (Mathf.Lerp(Normal.fov.x, Normal.fov.y, Mathf.Sqrt(lSpeed)) + lHyperB * HyperLook.fov.x + lLightB * LightLook.fov.x) * GameSession.FovScale
                                + (_kick + _dive * 10f) * PANO_KICK_SCALE;
            if (freeLook) lHFovTarget = 150f * GameSession.FovScale;
            _hfov = Mathf.SmoothDamp(_hfov, Mathf.Min(PANO_FOV_MAX, lHFovTarget), ref _hfovVelocity, 0.2f, FOV_RATE * PANO_KICK_SCALE, lDt);
            _shape = Mathf.Lerp(Mathf.Lerp(Normal.shape, HyperLook.shape, lHyperB), LightLook.shape, lLightB);
            _cylinder = Mathf.Lerp(Mathf.Lerp(Normal.cylinder, HyperLook.cylinder, lHyperB), LightLook.cylinder, lLightB);

            if (_overrideBlend > 0f)
            {
                ApplyShot(lUnscaled);
                float lBlend = HSMath.Smooth(_overrideBlend);
                lPosition = Vector3.Lerp(lPosition, _shotPosition, lBlend);
                lRotation = Quaternion.Slerp(lRotation, _shotRotation, lBlend);
                transform.SetPositionAndRotation(lPosition, lRotation);
                Cam.fieldOfView = Mathf.Lerp(_fov, _overrideFov, lBlend);
                ApplyWide(Mathf.Lerp(_hfov, ProjectionMath.EquivalentHFov(_overrideFov, Cam.aspect, _shape) * 1.15f, lBlend));
                if (_overrideBlend >= 1f) _velocity = Vector3.zero;
                return;
            }
            transform.SetPositionAndRotation(lPosition, lRotation);
            Cam.fieldOfView = Mathf.Clamp(_finishMode ? 62f : _fov, 40f, FOV_CLAMP);
            ApplyWide(_finishMode ? 125f : _hfov);
            if (lensShiftX != 0f)
            {
                Cam.ResetProjectionMatrix();
                Matrix4x4 lProjection = Cam.projectionMatrix;
                lProjection[0, 2] = lensShiftX;
                Cam.projectionMatrix = lProjection;
            }
        }

        private void ApplyWide(float pHFov)
        {
            if (!WideAngle) return;
            PanoramaRig lRig = PanoramaRig.Active;
            lRig.hfov = pHFov;
            lRig.shape = _shape;
            lRig.cylinder = _cylinder;
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
            if (Physics.SphereCast(lFocus, 0.4f, lDelta / lLength, out lHit, lLength, VIEW_BLOCKERS, QueryTriggerInteraction.Ignore) && lHit.distance > 2f)
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

        // Last guard : the camera never ends up under or inside anything solid (track, scenery, props). The correction is
        // smoothed : it pushes in fast and lets go slowly, never a pop.
        private Vector3 KeepAboveSurface(Vector3 pPosition, float pDt)
        {
            Vector3 lFrom = _focus + _up * 3f;
            Vector3 lDelta = pPosition - lFrom;
            float lLength = lDelta.magnitude;
            Vector3 lWanted = Vector3.zero;
            RaycastHit lHit;
            if (lLength > 0.5f && Physics.SphereCast(lFrom, 1.2f, lDelta / lLength, out lHit, lLength + 2f, VIEW_BLOCKERS, QueryTriggerInteraction.Ignore))
                lWanted = (lFrom + lDelta / lLength * Mathf.Max(0.5f, lHit.distance - 2f) + lHit.normal * 3f) - pPosition;
            // And always a few metres of air under the lens.
            if (Physics.Raycast(pPosition + lWanted + _up * 6f, -_up, out lHit, 9f, VIEW_BLOCKERS, QueryTriggerInteraction.Ignore))
                lWanted += _up * Mathf.Max(0f, 3f - (lHit.distance - 6f));
            float lRate = lWanted.sqrMagnitude > _surfacePush.sqrMagnitude ? 30f : 4f;
            _surfacePush = Vector3.Lerp(_surfacePush, lWanted, 1f - Mathf.Exp(-lRate * pDt));
            return pPosition + _surfacePush;
        }

        private Vector3 _surfacePush;
        private const int VIEW_BLOCKERS = (1 << Layers.DECK) | (1 << Layers.SCENERY) | (1 << Layers.PROP);

        private void ApplyShot(float pDt)
        {
            _shotPosition = Vector3.Lerp(_shotPosition, _overridePosition, 1f - Mathf.Exp(-14f * pDt));
            Quaternion lLook = Quaternion.LookRotation(_overrideLook - _shotPosition, Vector3.up);
            _shotRotation = Quaternion.Slerp(_shotRotation, lLook, 1f - Mathf.Exp(-14f * pDt));
        }
    }
}
