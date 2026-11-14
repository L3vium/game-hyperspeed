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
    //
    // 2026-11-14 : this is the camera of build 2398ecf5 (the one the user wants), with what was asked since :
    // - it sits a little further back, and keeps moving back as the real speed climbs (late stages run ten times faster) ;
    // - it never goes through the track (analytic guard) nor through scenery or props (smoothed guard) ;
    // - it tilts down onto drops so the track ahead is always seen, and the far clip covers the whole stage ;
    // - a punch spring for the juice, the victory lap shots, and a pose the respawn flight can land on without a jump.
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
        // The speed the base camera was tuned for (2398ecf5, stage 1-1) and how much further it sits at that speed.
        private const float REF_SPEED = 216f;
        private const float BASE_FARTHER = 1.15f;
        private const float SPEED_EXPONENT = 0.55f;
        private const float SPEED_SCALE_MAX = 4f;
        // The camera always stays this far on the runner's side of the track surface.
        private const float FLOOR_CLEARANCE = 2.5f;
        // Whatever the stage, at least this much of the world is drawn.
        private const float FAR_MIN = 30000f;
        private const float FAR_MAX = 260000f;
        private const float SHOT_TIME = 4f;
        private const int VIEW_BLOCKERS = (1 << Layers.DECK) | (1 << Layers.SCENERY) | (1 << Layers.PROP);

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
        private float _blockDistance = 1f;
        // 0 on open ground, 1 inside a loop / twist : the view tucks in and rises so it stays inside the curve.
        private float _curl;
        private float _finishOrbit;
        private int _finishShot = -1;
        private float _windTime;
        // Distance and height multiplier from the real speed (smoothed : the kicks have their own pull back).
        private float _speedScale = BASE_FARTHER;
        // Drops ahead : how far down the look point is pulled so the track below is in the frame.
        private float _aheadDrop;
        private Vector3 _surfacePush;
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
            lCamera.farClipPlane = FAR_MIN;
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
            UpdateDepth(true);
        }

        public void Snap()
        {
            if (target == null) return;
            _up = target.CameraUp;
            _look = target.AimDirection;
            _focus = target.FocusPosition;
            _speedScale = SpeedScaleFor(_player != null ? _player.Speed : 0f);
            _lag = 0f;
            _lagVelocity = 0f;
            _aheadDrop = 0f;
            _surfacePush = Vector3.zero;
            _base = DesiredPosition(target.Speed01);
            _offset = _base - _focus;
            transform.position = _base;
            transform.rotation = Quaternion.LookRotation(LookPoint(target.Speed01) - _base, _up);
            _velocity = Vector3.zero;
            _fov = FovFor(target.Speed01, 0f, 0f);
            _fovVelocity = 0f;
        }

        // The pose the race camera will take behind a runner standing there (respawn flight lands on it exactly).
        public void GameplayPose(Vector3 pFocus, Vector3 pUp, Vector3 pLook, float pSpeed, float pSpeed01, out Vector3 pPosition, out Vector3 pLookPoint, out float pFov)
        {
            float lScale = SpeedScaleFor(pSpeed) * distanceScale * UserScale;
            float lDistance = BackDistance(pSpeed01) * lScale;
            float lHeight = UpHeight(pSpeed01) * lScale;
            pPosition = pFocus + pUp * lHeight - pLook * lDistance;
            pLookPoint = pFocus + pUp * Mathf.Lerp(1.2f, 2.5f, pSpeed01) * distanceScale + pLook * lDistance * 0.25f;
            pFov = FovFor(pSpeed01, 0f, 0f);
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
            _shotLocked = false;
            _overridePosition = pPosition;
            _overrideLook = pLook;
            _overrideFov = pFov > 0f ? pFov : Cam.fieldOfView;
        }

        public void CutTo(Vector3 pPosition, Vector3 pLook, float pFov)
        {
            SetOverride(pPosition, pLook, pFov);
            _overrideBlend = 1f;
            _shotPosition = pPosition;
            Vector3 lDirection = pLook - pPosition;
            if (lDirection.sqrMagnitude > 0.0001f) _shotRotation = Quaternion.LookRotation(lDirection, Vector3.up);
        }

        // Exact shot, roll included (the respawn flight) : held as given until cleared, never re-aimed.
        public void CutTo(Vector3 pPosition, Quaternion pRotation, float pFov)
        {
            SetOverride(pPosition, pPosition + pRotation * Vector3.forward * 50f, pFov);
            _overrideBlend = 1f;
            _shotPosition = pPosition;
            _shotRotation = pRotation;
            _shotLocked = true;
        }

        private bool _shotLocked;

        public void ClearOverride(float pBlendTime = 0.3f)
        {
            _override = false;
            _overrideRate = 1f / Mathf.Max(0.01f, pBlendTime);
        }

        public void SetFinishMode(bool pValue)
        {
            _finishMode = pValue;
            _finishOrbit = 0f;
            _finishShot = -1;
        }

        // The runner was put back on the track : the next frame cuts to the shot instead of flying there.
        public void CutFinish() => _finishShot = -1;

        public void AddTrauma(float pAmount) => _trauma = Mathf.Clamp01(_trauma + pAmount);

        // Additive FOV kick with an envelope : never a sudden in / out pump.
        public void FovKick(float pAmount) => _kickTarget = Mathf.Min(KICK_MAX, Mathf.Max(_kickTarget, pAmount) + pAmount * 0.2f);

        // Thrown back : the camera falls behind, then the spring brings it home.
        public void Rollback(float pAmount) => _lag = Mathf.Min(LAG_MAX, Mathf.Max(_lag, pAmount) + pAmount * 0.3f);

        // Knocks the view (pitch, yaw, roll in degrees) ; a stiff spring brings it back in a fraction of a second.
        public void Punch(Vector3 pDegrees) => _punch = Vector3.ClampMagnitude(_punch + pDegrees, 25f);

        private float UserScale => freeLook ? 1f : GameSession.CameraDistance;

        // Ten times the speed must read : a camera that keeps its distance at 1 km/s sees the runner leave the frame.
        // At the base speed it sits a little further than the base camera, and further again the faster you run.
        private static float SpeedScaleFor(float pSpeed)
        {
            float lRatio = Mathf.Max(1f, pSpeed / REF_SPEED);
            return Mathf.Min(SPEED_SCALE_MAX, BASE_FARTHER * Mathf.Pow(lRatio, SPEED_EXPONENT));
        }

        private float BackDistance(float pSpeed01) => freeLook ? 14f : Mathf.Lerp(18f, 60f, Mathf.Sqrt(pSpeed01)) + _lightBlend * 20f;

        private float UpHeight(float pSpeed01) => freeLook ? 4f : Mathf.Lerp(7f, 16f, pSpeed01);

        // Far back and well above the runner, looking down onto it : the runner is small in a huge world, and the
        // ground's bumps barely move the view.
        private Vector3 DesiredPosition(float pSpeed01)
        {
            float lScale = (freeLook ? 1f : _speedScale) * distanceScale * UserScale;
            float lDistance = (BackDistance(pSpeed01) + (freeLook ? 0f : _lag)) * lScale;
            float lHeight = UpHeight(pSpeed01) * lScale;
            // In a loop the line behind you curves away under the track : come closer and higher instead.
            lDistance *= Mathf.Lerp(1f, 0.42f, _curl);
            lHeight *= Mathf.Lerp(1f, 1.6f, _curl);
            return _focus + _up * lHeight - _look * lDistance;
        }

        private Vector3 LookPoint(float pSpeed01)
        {
            float lAhead = BackDistance(pSpeed01) * (freeLook ? 1f : _speedScale) * 0.25f * distanceScale;
            return _focus + _up * Mathf.Lerp(1.2f, 2.5f, pSpeed01) * distanceScale + _look * lAhead + _up * _aheadDrop;
        }

        private float FovFor(float pSpeed01, float pHyper, float pLight)
        {
            if (freeLook) return 84f;
            float lCalm = _player != null ? Mathf.InverseLerp(260f, 450f, _player.Profile.cruise) : 1f;
            return Mathf.Lerp(FOV_MIN - 8f + lCalm * 8f, FOV_MAX, Mathf.Sqrt(pSpeed01)) + pHyper * FOV_HYPER + pLight * 10f + GameSession.FovOffset;
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

        // What is ahead must be seen : when the track drops away in front (a crest, a dive, a step down) the view tilts
        // down onto it, a little up on a climb. Smoothed, never during free look or inside loops.
        private void UpdateAhead(float pDt)
        {
            float lWanted = 0f;
            if (_player != null && _layout != null && _layout.field != null && !freeLook && _curl < 0.05f && !_finishMode)
            {
                float lAhead = Mathf.Max(60f, _player.Speed * 0.9f);
                Vector3 lGround = _layout.GroundPoint(_player.Progress + lAhead, _player.Lateral);
                float lDelta = Vector3.Dot(lGround - _focus, _up);
                lWanted = lDelta < 0f ? Mathf.Max(lDelta * 0.4f, -lAhead * 0.35f) : Mathf.Min(lDelta * 0.15f, 20f * _speedScale);
            }
            _aheadDrop = Mathf.Lerp(_aheadDrop, lWanted, 1f - Mathf.Exp(-3f * pDt));
        }

        // The whole stage is always drawn : the far clip covers its length (at least 30 km), whatever the speed.
        private void UpdateDepth(bool pSnap = false)
        {
            if (Cam == null) return;
            float lFar = FAR_MIN;
            if (_layout != null) lFar = Mathf.Max(lFar, _layout.Length * 1.05f + 4000f);
            if (_player != null) lFar = Mathf.Max(lFar, _player.Speed * 40f);
            lFar = Mathf.Min(lFar, FAR_MAX);
            Cam.farClipPlane = pSnap ? lFar : Mathf.Max(Cam.farClipPlane, lFar);
            Cam.nearClipPlane = Cam.farClipPlane > 120000f ? 0.6f : 0.3f;
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
            _speedScale = Mathf.Lerp(_speedScale, SpeedScaleFor(_player != null ? _player.Speed : 0f), 1f - Mathf.Exp(-1.1f * lDt));
            _lag = Mathf.SmoothDamp(_lag, 0f, ref _lagVelocity, 0.55f, Mathf.Infinity, lDt);
            _kick = Mathf.MoveTowards(_kick, _kickTarget, lUnscaled * KICK_MAX / 0.08f);
            _kickTarget = Mathf.MoveTowards(_kickTarget, 0f, lUnscaled * KICK_MAX / 0.7f);
            _trauma = Mathf.MoveTowards(_trauma, 0f, lUnscaled * 1.4f);
            _punchVelocity += (-_punch * 260f - _punchVelocity * 22f) * lUnscaled;
            _punch += _punchVelocity * lUnscaled;
            _roll = Mathf.Lerp(_roll, target.CameraRoll, 1f - Mathf.Exp(-6f * lDt));
            _dive = Mathf.Lerp(_dive, target.Dive01, 1f - Mathf.Exp(-5f * lDt));
            UpdateAhead(lDt);
            UpdateDepth();

            Vector3 lDesired = DesiredPosition(lSpeed);
            Vector3 lLook = LookPoint(lSpeed);
            if (_finishMode)
            {
                // Victory lap : a director cutting every few seconds between moving shots, the runner in the middle of
                // the frame, never a still camera.
                _finishOrbit += lDt;
                Vector3 lRunForward = _player != null ? Vector3.ProjectOnPlane(_player.FacingDirection, _up).normalized : _look;
                if (lRunForward.sqrMagnitude < 0.01f) lRunForward = _look;
                Vector3 lRunRight = Vector3.Cross(_up, lRunForward).normalized;
                int lShot = Mathf.FloorToInt(_finishOrbit / SHOT_TIME) % 5;
                float lT = (_finishOrbit % SHOT_TIME) / SHOT_TIME;
                bool lCut = lShot != _finishShot;
                _finishShot = lShot;
                FinishShot(lShot, lT, lRunForward, lRunRight, out lDesired, out lLook);
                lDesired = AvoidWorld(lDesired, lDt);
                if (lCut)
                {
                    _base = lDesired;
                    _velocity = Vector3.zero;
                    _surfacePush = Vector3.zero;
                }
                else _base = Vector3.SmoothDamp(_base, lDesired, ref _velocity, 0.12f, Mathf.Infinity, lDt);
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
            if (!_finishMode) lPosition = KeepOnRunnerSide(lPosition);

            float lFovTarget = FovFor(lSpeed, _hyperBlend, _lightBlend) + _kick + _dive * 10f;
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
            Cam.fieldOfView = Mathf.Clamp(_finishMode ? 60f : _fov, 40f, FOV_CLAMP);
            if (lensShiftX != 0f)
            {
                Cam.ResetProjectionMatrix();
                Matrix4x4 lProjection = Cam.projectionMatrix;
                lProjection[0, 2] = lensShiftX;
                Cam.projectionMatrix = lProjection;
            }
        }

        // The five shots of the victory lap, each moving along with the runner.
        private void FinishShot(int pShot, float pT, Vector3 pForward, Vector3 pRight, out Vector3 pPosition, out Vector3 pLook)
        {
            Vector3 lBody = _focus + _up * 0.8f;
            // Looking a little above the runner : it sits just under the middle of the frame, clear of the rank.
            pLook = lBody + _up * 2.2f;
            switch (pShot)
            {
                case 0:
                    pPosition = lBody - pRight * Mathf.Lerp(9f, 7f, pT) - pForward * Mathf.Lerp(6f, -4f, pT) + _up * Mathf.Lerp(2.5f, 1.5f, pT);
                    break;
                case 1:
                    pPosition = lBody + pForward * Mathf.Lerp(9f, 14f, pT) + pRight * Mathf.Sin(pT * 2f) * 3f + _up * 1.6f;
                    break;
                case 2:
                    pPosition = lBody - pForward * Mathf.Lerp(10f, 6f, pT) + pRight * 2.5f - _up * 0.4f;
                    pLook = lBody + pForward * 6f + _up * 2f;
                    break;
                case 3:
                    pPosition = lBody - pForward * Mathf.Lerp(26f, 14f, pT) + _up * Mathf.Lerp(34f, 18f, pT) + pRight * 6f;
                    break;
                default:
                    float lAngle = pT * Mathf.PI * 1.2f;
                    pPosition = lBody + (pRight * Mathf.Cos(lAngle) + pForward * Mathf.Sin(lAngle)) * 12f + _up * 3.5f;
                    break;
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
            if (Physics.SphereCast(lFocus, 0.4f, lDelta / lLength, out lHit, lLength, VIEW_BLOCKERS, QueryTriggerInteraction.Ignore) && lHit.distance > 2f)
                lWanted = Mathf.Clamp01((lHit.distance - 0.5f) / lLength);
            _blockDistance = lWanted < _blockDistance ? Mathf.Lerp(_blockDistance, lWanted, 1f - Mathf.Exp(-40f * pDt)) : Mathf.Lerp(_blockDistance, lWanted, 1f - Mathf.Exp(-3f * pDt));
            return lFocus + lDelta * _blockDistance;
        }

        private float CurlTarget()
        {
            if (_player == null || _layout == null || freeLook) return 0f;
            if (_player.OnLoopRope) return 1f;
            Spine lSpine = _layout.spine;
            float lAhead = Mathf.Max(30f, _player.Speed * 0.35f);
            if (lSpine.samples[lSpine.IndexOf(_player.Progress)].localGravity) return 1f;
            if (lSpine.samples[lSpine.IndexOf(_player.Progress + lAhead)].localGravity) return 0.6f;
            return 0f;
        }

        // Guard : the camera never ends up under or inside anything solid (track, scenery, props). The correction is
        // smoothed : it pushes in fast and lets go slowly, never a pop. And always a few metres of air under the lens.
        private Vector3 KeepAboveSurface(Vector3 pPosition, float pDt)
        {
            Vector3 lFrom = _focus + _up * 3f;
            Vector3 lDelta = pPosition - lFrom;
            float lLength = lDelta.magnitude;
            Vector3 lWanted = Vector3.zero;
            RaycastHit lHit;
            if (lLength > 0.5f && Physics.SphereCast(lFrom, 1.2f, lDelta / lLength, out lHit, lLength + 2f, VIEW_BLOCKERS, QueryTriggerInteraction.Ignore))
                lWanted = (lFrom + lDelta / lLength * Mathf.Max(0.5f, lHit.distance - 2f) + lHit.normal * 3f) - pPosition;
            if (Physics.Raycast(pPosition + lWanted + _up * 6f, -_up, out lHit, 9f, VIEW_BLOCKERS, QueryTriggerInteraction.Ignore))
                lWanted += _up * Mathf.Max(0f, 3f - (lHit.distance - 6f));
            float lRate = lWanted.sqrMagnitude > _surfacePush.sqrMagnitude ? 30f : 4f;
            _surfacePush = Vector3.Lerp(_surfacePush, lWanted, 1f - Mathf.Exp(-lRate * pDt));
            return pPosition + _surfacePush;
        }

        // Analytic guard : wherever the camera is above the track's footprint, it stays at least FLOOR_CLEARANCE on the
        // runner's side of the surface there, at once. Inside a loop "the runner's side" is the inside of the loop.
        private Vector3 KeepOnRunnerSide(Vector3 pPosition)
        {
            if (_player == null || _layout == null || _layout.field == null || freeLook) return pPosition;
            float lS = NearestS(pPosition, _player.Progress);
            TrackBlock lBlock = _layout.field.BlockAt(lS);
            if (lBlock == null) return pPosition;
            Vector3 lNormal = _layout.field.Normal(lS);
            SpineSample lSample = _layout.spine.Evaluate(lS);
            float lLateral = Vector3.Dot(pPosition - lSample.position, lSample.Right);
            if (Mathf.Abs(lLateral - lBlock.offset) > lBlock.halfWidth + 4f) return pPosition;
            Vector3 lFloor = _layout.GroundPoint(lS, lLateral);
            float lHeight = Vector3.Dot(pPosition - lFloor, lNormal);
            // Only a camera that crossed the surface (or skims it) is pushed : a camera far below a deck it never
            // crossed (the runner is under it too) is left alone.
            float lRunnerHeight = Vector3.Dot(_focus - _layout.GroundPoint(_player.Progress, _player.Lateral), _layout.field.Normal(_player.Progress));
            if (lRunnerHeight < -1f) return pPosition;
            if (lHeight < FLOOR_CLEARANCE && lHeight > -60f) pPosition += lNormal * (FLOOR_CLEARANCE - lHeight);
            return pPosition;
        }

        // Spine distance closest to a point, searched from the runner backward and a little forward.
        private float NearestS(Vector3 pPosition, float pHintS)
        {
            float lBack = Mathf.Max(400f, (pPosition - _focus).magnitude * 2.5f);
            float lStep = Mathf.Max(8f, lBack / 60f);
            float lBest = pHintS;
            float lBestDistance = float.MaxValue;
            for (float lS = pHintS - lBack; lS <= pHintS + 60f; lS += lStep)
            {
                float lDistance = (_layout.spine.Evaluate(lS).position - pPosition).sqrMagnitude;
                if (lDistance < lBestDistance) { lBestDistance = lDistance; lBest = lS; }
            }
            for (float lS = lBest - lStep; lS <= lBest + lStep; lS += lStep / 8f)
            {
                float lDistance = (_layout.spine.Evaluate(lS).position - pPosition).sqrMagnitude;
                if (lDistance < lBestDistance) { lBestDistance = lDistance; lBest = lS; }
            }
            return lBest;
        }

        private void ApplyShot(float pDt)
        {
            if (_shotLocked) return;
            _shotPosition = Vector3.Lerp(_shotPosition, _overridePosition, 1f - Mathf.Exp(-14f * pDt));
            Vector3 lDirection = _overrideLook - _shotPosition;
            if (lDirection.sqrMagnitude < 0.0001f) return;
            Quaternion lLook = Quaternion.LookRotation(lDirection, Vector3.up);
            _shotRotation = Quaternion.Slerp(_shotRotation, lLook, 1f - Mathf.Exp(-14f * pDt));
        }
    }
}
