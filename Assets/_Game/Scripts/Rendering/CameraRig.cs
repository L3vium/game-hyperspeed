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
    }

    // Free third person camera : it always orbits where the mouse looks, behind the runner, in the frame of the surface
    // it runs on. It falls far back as speed builds, gets thrown back by every kick and catches up on a spring, and rolls
    // hard. Every change is smoothed. Shake never feeds back into the position, and a paused game freezes it completely.
    public class CameraRig : MonoBehaviour
    {
        private const float FOV_MIN = 90f;
        private const float FOV_MAX = 135f;
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
        private float _lag;
        private float _lagVelocity;
        private float _kick;
        private float _kickTarget;
        private float _fov = 90f;
        private float _fovVelocity;
        private float _trauma;
        private float _roll;
        private float _hyperBlend;
        private float _lightBlend;
        private bool _finishMode;
        private Vector3 _finishPosition;
        private float _blockDistance = 1f;

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
            _base = DesiredPosition(target.Speed01);
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
            _finishPosition = transform.position;
        }

        public void AddTrauma(float pAmount) => _trauma = Mathf.Clamp01(_trauma + pAmount);

        // Additive FOV kick with an envelope : never a sudden in / out pump.
        public void FovKick(float pAmount) => _kickTarget = Mathf.Min(KICK_MAX, Mathf.Max(_kickTarget, pAmount) + pAmount * 0.2f);

        // Thrown back : the camera falls behind, then the spring brings it home.
        public void Rollback(float pAmount) => _lag = Mathf.Min(LAG_MAX, Mathf.Max(_lag, pAmount) + pAmount * 0.3f);

        private Vector3 DesiredPosition(float pSpeed)
        {
            float lDistance = (freeLook ? 9f : Mathf.Lerp(8f, 30f, Mathf.Sqrt(pSpeed)) + _lightBlend * 12f + _lag) * distanceScale;
            float lHeight = freeLook ? 2f : Mathf.Lerp(2.4f, 5f, pSpeed);
            return target.FocusPosition + _up * lHeight - _look * lDistance;
        }

        private Vector3 LookPoint(float pSpeed)
        {
            return target.FocusPosition + _up * Mathf.Lerp(1.6f, 2.8f, pSpeed) + _look * Mathf.Lerp(8f, 26f, pSpeed);
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
            _up = Vector3.Slerp(_up, target.CameraUp, 1f - Mathf.Exp(-9f * lDt)).normalized;
            // The mouse answers almost at once.
            _look = Vector3.Slerp(_look, target.AimDirection, 1f - Mathf.Exp(-34f * lDt)).normalized;

            _hyperBlend = Mathf.MoveTowards(_hyperBlend, target.HyperMode ? 1f : 0f, lDt * 1.5f);
            _lightBlend = Mathf.MoveTowards(_lightBlend, _player != null && _player.LightForm ? 1f : 0f, lDt * 1.2f);
            _lag = Mathf.SmoothDamp(_lag, 0f, ref _lagVelocity, 0.55f, Mathf.Infinity, lDt);
            _kick = Mathf.MoveTowards(_kick, _kickTarget, lUnscaled * KICK_MAX / 0.08f);
            _kickTarget = Mathf.MoveTowards(_kickTarget, 0f, lUnscaled * KICK_MAX / 0.7f);
            _trauma = Mathf.MoveTowards(_trauma, 0f, lUnscaled * 1.4f);
            _roll = Mathf.Lerp(_roll, target.CameraRoll, 1f - Mathf.Exp(-6f * lDt));

            Vector3 lDesired = DesiredPosition(lSpeed);
            Vector3 lLook = LookPoint(lSpeed);
            if (_finishMode)
            {
                lDesired = _finishPosition;
                lLook = target.FocusPosition;
            }
            lDesired = AvoidWorld(lDesired, lDt);

            _base = Vector3.SmoothDamp(_base, lDesired, ref _velocity, _finishMode ? 0.6f : freeLook ? 0.08f : 0.028f, Mathf.Infinity, lDt);
            Quaternion lRotation = Quaternion.LookRotation(lLook - _base, _up) * Quaternion.Euler(0f, 0f, freeLook ? 0f : _roll);

            // Rumble grows with speed : at full speed the frame never rests.
            float lShake = _trauma * _trauma + lSpeed * lSpeed * 0.06f + _hyperBlend * 0.02f + _lightBlend * 0.04f;
            float lTime = Time.unscaledTime * 24f;
            Vector3 lShakeOffset = new Vector3(Mathf.PerlinNoise(lTime, 0.1f) - 0.5f, Mathf.PerlinNoise(0.3f, lTime) - 0.5f, 0f) * lShake * 1.4f;
            Quaternion lShakeRot = Quaternion.Euler((Mathf.PerlinNoise(lTime, 5.2f) - 0.5f) * lShake * 5f, (Mathf.PerlinNoise(7.1f, lTime) - 0.5f) * lShake * 5f, (Mathf.PerlinNoise(lTime, 9.4f) - 0.5f) * lShake * 8f);
            Vector3 lPosition = _base + lRotation * lShakeOffset;
            lRotation *= lShakeRot;

            float lFovTarget = Mathf.Lerp(FOV_MIN, FOV_MAX, Mathf.Sqrt(lSpeed)) + _hyperBlend * FOV_HYPER + _kick;
            if (freeLook) lFovTarget = 74f;
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
            Cam.fieldOfView = Mathf.Clamp(_fov, 40f, 150f);
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
            Vector3 lFocus = target.FocusPosition;
            Vector3 lDelta = pDesired - lFocus;
            float lLength = lDelta.magnitude;
            if (lLength < 0.01f) return pDesired;
            float lWanted = 1f;
            RaycastHit lHit;
            if (Physics.SphereCast(lFocus, 0.6f, lDelta / lLength, out lHit, lLength, (1 << Layers.PROP) | (1 << Layers.DECK), QueryTriggerInteraction.Ignore))
                lWanted = Mathf.Clamp01((lHit.distance - 0.2f) / lLength);
            _blockDistance = lWanted < _blockDistance ? Mathf.Lerp(_blockDistance, lWanted, 1f - Mathf.Exp(-40f * pDt)) : Mathf.Lerp(_blockDistance, lWanted, 1f - Mathf.Exp(-3f * pDt));
            return lFocus + lDelta * _blockDistance;
        }

        private void ApplyShot(float pDt)
        {
            _shotPosition = Vector3.Lerp(_shotPosition, _overridePosition, 1f - Mathf.Exp(-14f * pDt));
            Quaternion lLook = Quaternion.LookRotation(_overrideLook - _shotPosition, Vector3.up);
            _shotRotation = Quaternion.Slerp(_shotRotation, lLook, 1f - Mathf.Exp(-14f * pDt));
        }
    }
}
