using System.Collections;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Death, then back in the race fast and readable : a short hold on the fall (the runner bursts into light), then the
    // camera flies over the track from where you died to where you come back, and the runner rematerialises there in a
    // flash. You always see where you are taken. Never a long wait, never a black screen.
    //
    // 2026-11-14 : the whole move is one continuous curve, with no cut and no snap. The camera coasts out of its last
    // motion while it turns to the fall, flies an eased arc (zero speed and acceleration at both ends), looks at a point
    // gliding from the fall to the place you come back to, and lands exactly on the race camera's own pose there.
    public class RespawnDirector : MonoBehaviour
    {
        private const float FALL_VIEW = 0.35f;
        private const float TRAVEL_MIN = 0.9f;
        private const float TRAVEL_MAX = 1.6f;
        // Metres of flight per extra second of travel.
        private const float TRAVEL_PER_SECOND = 5000f;
        private const float SETTLE = 0.2f;

        private PlayerController _player;
        private CameraRig _rig;

        public bool Busy { get; private set; }
        public static RespawnDirector Instance { get; private set; }
        private float _graceUntil = -1f;
        // While the runner is being brought back, and a moment after, nothing behind it may give way.
        public static bool HoldWorld => Instance != null && (Instance.Busy || Time.time < Instance._graceUntil);
        // Largest per-frame camera move of the last flight (Dev log : the flight must never jump).
        public static float LastMaxStep { get; private set; }
        public static float LastMaxTurn { get; private set; }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static RespawnDirector Create(PlayerController pPlayer, CameraRig pRig)
        {
            RespawnDirector lDirector = new GameObject("RespawnDirector").AddComponent<RespawnDirector>();
            lDirector._player = pPlayer;
            lDirector._rig = pRig;
            Instance = lDirector;
            pPlayer.OnDied += lDirector.OnDied;
            return lDirector;
        }

        private void OnDied()
        {
            if (!Busy) StartCoroutine(Sequence());
        }

        private static float Smoother(float pT)
        {
            pT = Mathf.Clamp01(pT);
            return pT * pT * pT * (pT * (pT * 6f - 15f) + 10f);
        }

        private IEnumerator Sequence()
        {
            Busy = true;
            Breadcrumb lSafe = _player.PickRespawn();
            LevelLayout lLayout = RaceContext.Layout;
            Vector3 lVelocity = _player.Velocity;
            Transform lCamera = _rig.transform;
            Vector3 lCamPosition = lCamera.position;
            Quaternion lCamRotation = lCamera.rotation;
            float lFov = _rig.Cam.fieldOfView;
            // The camera was following the runner : it keeps that motion for a moment and eases to a stop.
            Vector3 lCamVelocity = lVelocity;
            LastMaxStep = 0f;
            LastMaxTurn = 0f;

            // 1. The fall, held for a glance : the runner keeps its momentum, the camera turns smoothly to watch it.
            AudioManager.Play("sfx_fall", 0.9f);
            Vector3 lFallUp = lLayout != null ? lLayout.GravityUp(_player.Progress) : Vector3.up;
            for (float lTime = 0f; lTime < FALL_VIEW; lTime += Time.unscaledDeltaTime)
            {
                float lDt = Time.unscaledDeltaTime;
                lVelocity -= lFallUp * Ballistics.G_FALL * lDt;
                _player.transform.position += lVelocity * lDt;
                lCamPosition += lCamVelocity * lDt;
                lCamVelocity *= Mathf.Exp(-7f * lDt);
                Vector3 lToRunner = _player.FocusPosition - lCamPosition;
                if (lToRunner.sqrMagnitude > 1f)
                    lCamRotation = Quaternion.Slerp(lCamRotation, Quaternion.LookRotation(lToRunner, lCamRotation * Vector3.up), 1f - Mathf.Exp(-6f * lDt));
                Place(lCamPosition, lCamRotation, lFov);
                yield return null;
            }
            Vector3 lGone = _player.FocusPosition;
            FX.Burst(lGone, lVelocity.sqrMagnitude > 1f ? lVelocity.normalized : Vector3.up, Color.white, 30f);
            FX.Sparks(lGone, new Color(0.6f, 0.9f, 1f), 90, 60f, 0.6f, 0.6f);
            FX.Shockwave(lGone, (lCamPosition - lGone).normalized, Color.white, 26f, 0.3f);
            SetRunnerVisible(false);

            // The world behind the safe point is put back as it was.
            if (CourseBreak.Instance != null) CourseBreak.Instance.RewindTo(lSafe.s);
            if (ChaosDirector.Instance != null) ChaosDirector.Instance.RewindTo(lSafe.s);

            // 2. The flight, onto the exact pose the race camera will have behind the runner at the safe point.
            Vector3 lFocus, lUp, lLook;
            float lSpeed, lSpeed01;
            _player.PreviewRespawn(lSafe, out lFocus, out lUp, out lLook, out lSpeed, out lSpeed01);
            Vector3 lEnd, lEndLook;
            float lEndFov;
            _rig.GameplayPose(lFocus, lUp, lLook, lSpeed, lSpeed01, out lEnd, out lEndLook, out lEndFov);

            Vector3 lStart = lCamPosition;
            Vector3 lStartUp = lCamRotation * Vector3.up;
            // What the camera looked at when the flight starts : a point along its view, at the fall's distance.
            Vector3 lStartLook = lStart + lCamRotation * Vector3.forward * Mathf.Max(10f, Vector3.Distance(lStart, lGone));
            float lDistance = Vector3.Distance(lStart, lEnd);
            float lTravel = Mathf.Clamp(TRAVEL_MIN + lDistance / TRAVEL_PER_SECOND, TRAVEL_MIN, TRAVEL_MAX);
            float lArc = Mathf.Clamp(lDistance * 0.3f, 25f, 500f);
            Vector3 lWorldUp = Vector3.up;
            Vector3 lA = lStart + lWorldUp * lArc;
            Vector3 lB = lEnd + lWorldUp * lArc - lLook * lArc * 0.35f;

            if (PostFX.Instance != null) PostFX.Instance.rewind = 0.4f;
            AudioManager.Play("sfx_rewind", 0.8f);
            for (float lTime = 0f; lTime < lTravel; lTime += Time.unscaledDeltaTime)
            {
                float lRaw = Mathf.Clamp01(lTime / lTravel);
                float lT = Smoother(lRaw);
                Vector3 lPosition = Bezier(lStart, lA, lB, lEnd, lT);
                // The look glides from where you fell to where you come back : the way back reads at a glance.
                Vector3 lTarget = Vector3.Lerp(lStartLook, lEndLook, Smoother(lRaw * 1.15f - 0.05f));
                Vector3 lFrameUp = Vector3.Slerp(lStartUp, lUp, lT);
                Vector3 lDirection = lTarget - lPosition;
                Quaternion lRotation = lDirection.sqrMagnitude > 0.01f ? Quaternion.LookRotation(lDirection, lFrameUp) : lCamRotation;
                Place(lPosition, lRotation, Mathf.Lerp(lFov, lEndFov, lT));
                if (PostFX.Instance != null) PostFX.Instance.speed01 = Mathf.Sin(lRaw * Mathf.PI) * 0.6f;
                HUD.LinesBurst(Mathf.Sin(lRaw * Mathf.PI) * 0.4f);
                yield return null;
            }
            Place(lEnd, Quaternion.LookRotation(lEndLook - lEnd, lUp), lEndFov);

            // 3. Back : the runner appears in a flash of light where the camera landed, the race camera takes over in place.
            _player.RespawnAt(lSafe);
            if (CourseBreak.Instance != null) CourseBreak.Instance.RewindTo(lSafe.s);
            if (ChaosDirector.Instance != null) ChaosDirector.Instance.RewindTo(lSafe.s);
            _graceUntil = Time.time + 2.5f;
            SetRunnerVisible(true);
            if (PostFX.Instance != null) PostFX.Instance.rewind = 0f;
            _rig.Snap();
            _rig.ClearOverride(SETTLE);
            HSJuice.Play(JuiceId.Respawn, new JuiceContext { position = _player.FocusPosition, direction = lLook, normal = lUp, intensity = 1f, size = 1f });
            if (Autopilot.Enabled || Debug.isDebugBuild)
                Debug.Log("[HS-CAM] respawn flight " + lDistance.ToString("F0") + " m in " + lTravel.ToString("F2") + " s, max step " + LastMaxStep.ToString("F1") + " m, max turn " + LastMaxTurn.ToString("F1") + " deg");
            yield return new WaitForSecondsRealtime(SETTLE);
            Busy = false;
        }

        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private bool _hasLast;

        // One frame of the shot, with the per-frame move logged.
        private void Place(Vector3 pPosition, Quaternion pRotation, float pFov)
        {
            if (_hasLast && Busy)
            {
                LastMaxStep = Mathf.Max(LastMaxStep, Vector3.Distance(pPosition, _lastPosition));
                LastMaxTurn = Mathf.Max(LastMaxTurn, Quaternion.Angle(pRotation, _lastRotation));
            }
            _lastPosition = pPosition;
            _lastRotation = pRotation;
            _hasLast = true;
            _rig.CutTo(pPosition, pRotation, pFov);
        }

        private void SetRunnerVisible(bool pVisible)
        {
            if (_player.Visual != null && _player.Visual.Root != null) _player.Visual.Root.gameObject.SetActive(pVisible);
        }

        private static Vector3 Bezier(Vector3 pA, Vector3 pB, Vector3 pC, Vector3 pD, float pT)
        {
            float lU = 1f - pT;
            return lU * lU * lU * pA + 3f * lU * lU * pT * pB + 3f * lU * pT * pT * pC + pT * pT * pT * pD;
        }
    }
}
