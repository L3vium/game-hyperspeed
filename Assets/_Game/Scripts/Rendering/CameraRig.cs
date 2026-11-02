using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Com.IsartDigital.Hyperspeed
{
    public interface ICameraTarget
    {
        Vector3 FocusPosition { get; }
        Vector3 CameraUp { get; }
        Vector3 CameraForward { get; }
        float Speed01 { get; }
        float CameraRoll { get; }
        bool HyperMode { get; }
    }

    // Third person chase camera. Every change is smoothed (no zoom ping-pong) : FOV follows speed through a
    // critically damped spring with a rate limit, kicks have an attack / release envelope, the rollback eases out
    // over a second. It anticipates the route one second ahead, rises over long dives, and supports free look,
    // blended cinematic shots and a hold (death).
    public class CameraRig : MonoBehaviour
    {
        private const float FOV_MIN = 86f;
        private const float FOV_MAX = 118f;
        private const float FOV_HYPER = 8f;
        private const float FOV_RATE = 90f;
        private const float KICK_MAX = 10f;
        private const float LAG_MAX = 8f;

        public static CameraRig Instance { get; private set; }

        public Camera Cam { get; private set; }
        public ICameraTarget target;
        public bool freeLook;

        private PlayerController _player;
        private LevelLayout _layout;
        private Vector3 _up = Vector3.up;
        private Vector3 _forward = Vector3.forward;
        private Vector3 _velocity;
        private float _lag;
        private float _kick;
        private float _kickTarget;
        private float _fov = 88f;
        private float _fovVelocity;
        private float _trauma;
        private float _roll;
        private float _hyperBlend;
        private float _dive;
        private float _freeYaw;
        private float _freePitch = 12f;
        private float _lookIdle;
        private bool _finishMode;
        private Vector3 _finishPosition;

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
            lCamera.nearClipPlane = 0.1f;
            lCamera.farClipPlane = 4000f;
            lCamera.fieldOfView = 80f;
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
            _forward = target.CameraForward;
            _freeYaw = Mathf.Atan2(_forward.x, _forward.z) * Mathf.Rad2Deg;
            transform.position = DesiredPosition(target.Speed01);
            transform.rotation = Quaternion.LookRotation(LookPoint(target.Speed01) - transform.position, _up);
            _velocity = Vector3.zero;
        }

        // Cinematic shot (intro, death hold, rewind) : the rig blends toward it and back when cleared.
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
            _finishPosition = transform.position;
        }

        public void AddTrauma(float pAmount) => _trauma = Mathf.Clamp01(_trauma + pAmount);

        // Additive FOV kick with an envelope : never a sudden in / out pump.
        public void FovKick(float pAmount) => _kickTarget = Mathf.Min(KICK_MAX, Mathf.Max(_kickTarget, pAmount) + pAmount * 0.2f);

        public void Rollback(float pAmount) => _lag = Mathf.Min(LAG_MAX, Mathf.Max(_lag, pAmount) + pAmount * 0.2f);

        private Vector3 DesiredPosition(float pSpeed)
        {
            if (freeLook)
            {
                Quaternion lOrbit = Quaternion.Euler(_freePitch, _freeYaw, 0f);
                return target.FocusPosition + lOrbit * new Vector3(0f, 0.6f, -7.5f);
            }
            float lDistance = Mathf.Lerp(6f, 14f, pSpeed) + _hyperBlend * 5f + _lag + _dive * 4f;
            float lHeight = Mathf.Lerp(2.2f, 3.8f, pSpeed) + _hyperBlend + _dive * 8f;
            return target.FocusPosition + _up * lHeight - _forward * lDistance;
        }

        private Vector3 LookPoint(float pSpeed)
        {
            if (freeLook) return target.FocusPosition + Vector3.up * 0.8f;
            Vector3 lLook = target.FocusPosition + _up * 1.1f + _forward * (Mathf.Lerp(4f, 16f, pSpeed) + _hyperBlend * 6f);
            if (_player != null && _player.HasLandingPrediction && _dive > 0f) lLook = Vector3.Lerp(lLook, _player.LandingPoint, 0.5f * _dive);
            return lLook;
        }

        private void LateUpdate()
        {
            if (target == null && !_override) return;
            float lDt = Time.deltaTime;
            float lUnscaled = Time.unscaledDeltaTime;
            float lTargetBlend = _override ? 1f : 0f;
            _overrideBlend = Mathf.MoveTowards(_overrideBlend, lTargetBlend, lUnscaled * _overrideRate);
            if (target == null)
            {
                ApplyShot(lUnscaled);
                transform.SetPositionAndRotation(_shotPosition, _shotRotation);
                Cam.fieldOfView = _overrideFov;
                return;
            }

            float lSpeed = target.Speed01;
            _up = Vector3.Slerp(_up, target.CameraUp, 1f - Mathf.Exp(-7f * lDt)).normalized;
            Vector3 lTargetForward = target.CameraForward;
            // Anticipation : lean the view toward where the route goes one second from now.
            if (_player != null && _layout != null && !freeLook && _player.State != PlayerState.Dead)
            {
                Vector3 lAhead = _layout.spine.Forward(_player.Progress + Mathf.Max(20f, _player.Speed) * 1f);
                lTargetForward = Vector3.Slerp(lTargetForward, lAhead, 0.35f);
            }
            lTargetForward = Vector3.ProjectOnPlane(lTargetForward, _up);
            if (lTargetForward.sqrMagnitude > 0.001f) _forward = Vector3.Slerp(_forward, lTargetForward.normalized, 1f - Mathf.Exp(-5f * lDt));
            _forward = Vector3.ProjectOnPlane(_forward, _up).normalized;

            UpdateFreeLook(lUnscaled);
            _hyperBlend = Mathf.MoveTowards(_hyperBlend, target.HyperMode ? 1f : 0f, lDt * 1.5f);
            _lag = Mathf.Lerp(_lag, 0f, 1f - Mathf.Exp(-0.7f * lDt));
            _kick = Mathf.MoveTowards(_kick, _kickTarget, lUnscaled * KICK_MAX / 0.1f);
            _kickTarget = Mathf.MoveTowards(_kickTarget, 0f, lUnscaled * KICK_MAX / 0.8f);
            _trauma = Mathf.MoveTowards(_trauma, 0f, lUnscaled * 1.4f);
            _roll = Mathf.Lerp(_roll, target.CameraRoll, 1f - Mathf.Exp(-5f * lDt));
            float lDiveTarget = _player != null ? _player.DiveCamera01 : 0f;
            _dive = Mathf.MoveTowards(_dive, lDiveTarget, lDt * (lDiveTarget > _dive ? 0.9f : 2f));

            Vector3 lDesired = DesiredPosition(lSpeed);
            Vector3 lLook = LookPoint(lSpeed);
            if (_finishMode)
            {
                lDesired = _finishPosition;
                lLook = target.FocusPosition;
            }

            Vector3 lPosition = Vector3.SmoothDamp(transform.position, lDesired, ref _velocity, _finishMode ? 0.6f : freeLook ? 0.08f : 0.05f, Mathf.Infinity, lDt);
            Quaternion lRotation = Quaternion.LookRotation(lLook - lPosition, freeLook ? Vector3.up : _up) * Quaternion.Euler(0f, 0f, freeLook ? 0f : _roll);

            float lShake = _trauma * _trauma + lSpeed * lSpeed * 0.015f + _hyperBlend * 0.015f;
            float lTime = Time.unscaledTime * 22f;
            Vector3 lShakeOffset = new Vector3(Mathf.PerlinNoise(lTime, 0.1f) - 0.5f, Mathf.PerlinNoise(0.3f, lTime) - 0.5f, 0f) * lShake * 1.2f;
            Quaternion lShakeRot = Quaternion.Euler((Mathf.PerlinNoise(lTime, 5.2f) - 0.5f) * lShake * 5f, (Mathf.PerlinNoise(7.1f, lTime) - 0.5f) * lShake * 5f, (Mathf.PerlinNoise(lTime, 9.4f) - 0.5f) * lShake * 8f);
            lPosition += lRotation * lShakeOffset;
            lRotation *= lShakeRot;

            float lFovTarget = Mathf.Lerp(FOV_MIN, FOV_MAX, lSpeed) + _hyperBlend * FOV_HYPER + _kick;
            if (freeLook) lFovTarget = 70f;
            _fov = Mathf.SmoothDamp(_fov, lFovTarget, ref _fovVelocity, 0.3f, FOV_RATE, lDt);

            if (_overrideBlend > 0f)
            {
                ApplyShot(lUnscaled);
                float lBlend = LevelBuilder.Smooth(_overrideBlend);
                lPosition = Vector3.Lerp(lPosition, _shotPosition, lBlend);
                lRotation = Quaternion.Slerp(lRotation, _shotRotation, lBlend);
                transform.SetPositionAndRotation(lPosition, lRotation);
                Cam.fieldOfView = Mathf.Lerp(_fov, _overrideFov, lBlend);
                if (_overrideBlend >= 1f) _velocity = Vector3.zero;
                return;
            }
            transform.SetPositionAndRotation(lPosition, lRotation);
            Cam.fieldOfView = Mathf.Clamp(_fov, 40f, 130f);
        }

        private void ApplyShot(float pDt)
        {
            _shotPosition = Vector3.Lerp(_shotPosition, _overridePosition, 1f - Mathf.Exp(-14f * pDt));
            Quaternion lLook = Quaternion.LookRotation(_overrideLook - _shotPosition, Vector3.up);
            _shotRotation = Quaternion.Slerp(_shotRotation, lLook, 1f - Mathf.Exp(-14f * pDt));
        }

        // Mouse (locked cursor) or right stick orbit while free roaming ; recenters behind the runner when idle.
        private void UpdateFreeLook(float pDt)
        {
            if (!freeLook)
            {
                _freeYaw = Mathf.Atan2(_forward.x, _forward.z) * Mathf.Rad2Deg;
                _freePitch = 12f;
                return;
            }
            Vector2 lLook = HSInput.LookDelta;
            _freeYaw += lLook.x;
            _freePitch = Mathf.Clamp(_freePitch - lLook.y, -15f, 50f);
            if (lLook.sqrMagnitude > 0.0001f) _lookIdle = 0f;
            else _lookIdle += pDt;
            if (_lookIdle > 1.5f && _player != null && _player.Speed > 3f)
            {
                float lHeadingYaw = Mathf.Atan2(_player.Heading.x, _player.Heading.z) * Mathf.Rad2Deg;
                _freeYaw = Mathf.MoveTowardsAngle(_freeYaw, lHeadingYaw, pDt * 60f);
            }
        }
    }
}
