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

    // Third person chase camera : surface-aligned up, speed dezoom with boost rollback, huge FOV, trauma shake.
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        public Camera Cam { get; private set; }
        public ICameraTarget target;

        private Vector3 _up = Vector3.up;
        private Vector3 _forward = Vector3.forward;
        private Vector3 _velocity;
        private float _lag;
        private float _fovKick;
        private float _trauma;
        private float _roll;
        private float _hyperBlend;
        private float _introTimer;
        private float _introDuration;
        private bool _finishMode;
        private Vector3 _finishPosition;

        public static Camera CreateCamera(string pName = "Main Camera")
        {
            GameObject lObject = new GameObject(pName);
            lObject.tag = "MainCamera";
            Camera lCamera = lObject.AddComponent<Camera>();
            lCamera.nearClipPlane = 0.1f;
            lCamera.farClipPlane = 3000f;
            lCamera.fieldOfView = 75f;
            lObject.AddComponent<AudioListener>();
            UniversalAdditionalCameraData lData = lObject.AddComponent<UniversalAdditionalCameraData>();
            lData.renderPostProcessing = true;
            lData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
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

        public void Snap()
        {
            if (target == null) return;
            _up = target.CameraUp;
            _forward = target.CameraForward;
            transform.position = DesiredPosition(0f);
            transform.rotation = Quaternion.LookRotation(LookPoint(0f) - transform.position, _up);
            _velocity = Vector3.zero;
        }

        public void PlayIntro(float pDuration)
        {
            _introDuration = pDuration;
            _introTimer = pDuration;
        }

        public void SetFinishMode(bool pValue)
        {
            _finishMode = pValue;
            _finishPosition = transform.position;
        }

        public void AddTrauma(float pAmount) => _trauma = Mathf.Clamp01(_trauma + pAmount);

        public void FovKick(float pAmount) => _fovKick = Mathf.Max(_fovKick, pAmount);

        public void FovPunch(float pAmount) => _fovKick = pAmount;

        public void Rollback(float pAmount) => _lag = Mathf.Max(_lag, pAmount);

        private Vector3 DesiredPosition(float pSpeed)
        {
            float lDistance = Mathf.Lerp(5.2f, 8.5f, pSpeed) + _lag + _hyperBlend * 6f;
            float lHeight = Mathf.Lerp(2.0f, 2.6f, pSpeed) + _hyperBlend * 1.2f;
            return target.FocusPosition + _up * lHeight - _forward * lDistance;
        }

        private Vector3 LookPoint(float pSpeed)
        {
            return target.FocusPosition + _up * 1.1f + _forward * (Mathf.Lerp(3f, 10f, pSpeed) + _hyperBlend * 8f);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float lDt = Time.deltaTime;
            float lUnscaled = Time.unscaledDeltaTime;
            float lSpeed = target.Speed01;

            _up = Vector3.Slerp(_up, target.CameraUp, 1f - Mathf.Exp(-7f * lDt)).normalized;
            Vector3 lTargetForward = Vector3.ProjectOnPlane(target.CameraForward, _up);
            if (lTargetForward.sqrMagnitude > 0.001f)
                _forward = Vector3.Slerp(_forward, lTargetForward.normalized, 1f - Mathf.Exp(-6f * lDt));
            _forward = Vector3.ProjectOnPlane(_forward, _up).normalized;

            _hyperBlend = Mathf.MoveTowards(_hyperBlend, target.HyperMode ? 1f : 0f, lDt * 2.5f);
            _lag = Mathf.Lerp(_lag, 0f, 1f - Mathf.Exp(-2.2f * lDt));
            _fovKick = Mathf.Lerp(_fovKick, 0f, 1f - Mathf.Exp(-5f * lUnscaled));
            _trauma = Mathf.MoveTowards(_trauma, 0f, lUnscaled * 1.4f);
            _roll = Mathf.Lerp(_roll, target.CameraRoll, 1f - Mathf.Exp(-6f * lDt));

            Vector3 lDesired = DesiredPosition(lSpeed);
            Vector3 lLook = LookPoint(lSpeed);

            if (_finishMode)
            {
                lDesired = _finishPosition;
                lLook = target.FocusPosition;
            }

            Vector3 lPosition = Vector3.SmoothDamp(transform.position, lDesired, ref _velocity, _finishMode ? 0.6f : 0.045f, Mathf.Infinity, lDt);
            if (_introTimer > 0f)
            {
                _introTimer -= lDt;
                float lT = 1f - Mathf.Clamp01(_introTimer / _introDuration);
                float lEase = lT * lT * (3f - 2f * lT);
                float lAngle = Mathf.Lerp(200f, 0f, lEase);
                Vector3 lOrbit = Quaternion.AngleAxis(lAngle, _up) * (-_forward * Mathf.Lerp(9f, 5.2f, lEase)) + _up * Mathf.Lerp(1.2f, 2.0f, lEase);
                lPosition = target.FocusPosition + lOrbit;
                lLook = Vector3.Lerp(target.FocusPosition + _up * 1.2f, lLook, lEase);
                _velocity = Vector3.zero;
            }

            Quaternion lRotation = Quaternion.LookRotation(lLook - lPosition, _up) * Quaternion.Euler(0f, 0f, _roll);

            float lShake = _trauma * _trauma + lSpeed * lSpeed * 0.04f + _hyperBlend * 0.03f;
            float lTime = Time.unscaledTime * 22f;
            Vector3 lShakeOffset = new Vector3(Mathf.PerlinNoise(lTime, 0.1f) - 0.5f, Mathf.PerlinNoise(0.3f, lTime) - 0.5f, 0f) * lShake * 1.4f;
            Quaternion lShakeRot = Quaternion.Euler((Mathf.PerlinNoise(lTime, 5.2f) - 0.5f) * lShake * 6f, (Mathf.PerlinNoise(7.1f, lTime) - 0.5f) * lShake * 6f, (Mathf.PerlinNoise(lTime, 9.4f) - 0.5f) * lShake * 10f);

            transform.position = lPosition + lRotation * lShakeOffset;
            transform.rotation = lRotation * lShakeRot;

            float lFov = Mathf.Lerp(74f, 112f, lSpeed) + _fovKick + _hyperBlend * 14f;
            Cam.fieldOfView = Mathf.Clamp(lFov, 40f, 150f);
        }
    }
}
