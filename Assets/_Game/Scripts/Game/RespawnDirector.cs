using System.Collections;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Death, then back in the race fast and readable : a short hold on the fall (the runner bursts into light), then the
    // camera flies over the track from where you died to where you come back, and the runner rematerialises there in a
    // flash. You always see where you are taken. Never a long wait, never a black screen.
    public class RespawnDirector : MonoBehaviour
    {
        private const float FALL_VIEW = 0.3f;
        private const float TRAVEL_MIN = 0.75f;
        private const float TRAVEL_MAX = 1.35f;
        private const float SETTLE = 0.25f;

        private PlayerController _player;
        private CameraRig _rig;

        public bool Busy { get; private set; }
        public static RespawnDirector Instance { get; private set; }
        private float _graceUntil = -1f;
        // While the runner is being brought back, and a moment after, nothing behind it may give way.
        public static bool HoldWorld => Instance != null && (Instance.Busy || Time.time < Instance._graceUntil);

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

        private IEnumerator Sequence()
        {
            Busy = true;
            Breadcrumb lSafe = _player.PickRespawn();
            LevelLayout lLayout = RaceContext.Layout;
            Vector3 lVelocity = _player.Velocity;
            Vector3 lHold = _rig.transform.position;
            Quaternion lHoldRotation = _rig.transform.rotation;

            // 1. The fall, held for a glance : the runner keeps its momentum, then bursts into light.
            AudioManager.Play("sfx_fall", 0.9f);
            Vector3 lFallUp = lLayout != null ? lLayout.GravityUp(_player.Progress) : Vector3.up;
            for (float lTime = 0f; lTime < FALL_VIEW; lTime += Time.unscaledDeltaTime)
            {
                lVelocity -= lFallUp * Ballistics.G_FALL * Time.unscaledDeltaTime;
                _player.transform.position += lVelocity * Time.unscaledDeltaTime;
                _rig.SetOverride(lHold, _player.FocusPosition);
                yield return null;
            }
            Vector3 lGone = _player.FocusPosition;
            FX.Burst(lGone, lVelocity.sqrMagnitude > 1f ? lVelocity.normalized : Vector3.up, Color.white, 30f);
            FX.Sparks(lGone, new Color(0.6f, 0.9f, 1f), 90, 60f, 0.6f, 0.6f);
            FX.Shockwave(lGone, (lHold - lGone).normalized, Color.white, 26f, 0.3f);
            SetRunnerVisible(false);

            // The world behind the safe point is put back as it was.
            if (CourseBreak.Instance != null) CourseBreak.Instance.RewindTo(lSafe.s);
            if (ChaosDirector.Instance != null) ChaosDirector.Instance.RewindTo(lSafe.s);

            // 2. The flight : from the death shot to the shot behind the safe point, arcing high over the track so the
            // way back reads at a glance. A soft rewind tint, wide lens, wind.
            Vector3 lForward = lSafe.heading.sqrMagnitude > 0.1f ? lSafe.heading.normalized : (lLayout != null ? lLayout.Flow(lSafe.s) : Vector3.forward);
            Vector3 lFlat = Vector3.ProjectOnPlane(lForward, Vector3.up).normalized;
            if (lFlat.sqrMagnitude < 0.01f) lFlat = Vector3.forward;
            Vector3 lSafePoint = lSafe.position;
            Vector3 lEnd = lSafePoint + Vector3.up * 22f - lFlat * 46f;
            Vector3 lEndLook = lSafePoint + Vector3.up * 2f + lFlat * 30f;
            float lDistance = Vector3.Distance(lHold, lEnd);
            float lTravel = Mathf.Clamp(0.55f + lDistance / 2400f, TRAVEL_MIN, TRAVEL_MAX);
            float lArc = Mathf.Clamp(lDistance * 0.35f, 60f, 700f);
            Vector3 lStartLook = lHold + lHoldRotation * Vector3.forward * 50f;
            if (PostFX.Instance != null) PostFX.Instance.rewind = 0.45f;
            AudioManager.Play("sfx_rewind", 0.8f);
            for (float lTime = 0f; lTime < lTravel; lTime += Time.unscaledDeltaTime)
            {
                float lT = HSMath.Smooth(Mathf.Clamp01(lTime / lTravel));
                // A bezier rising above both ends.
                Vector3 lA = lHold + Vector3.up * lArc;
                Vector3 lB = lEnd + Vector3.up * lArc - lFlat * lArc * 0.4f;
                Vector3 lPosition = Bezier(lHold, lA, lB, lEnd, lT);
                // Looks along the path first, then settles on the place you come back to.
                Vector3 lAhead = Bezier(lHold, lA, lB, lEnd, Mathf.Min(1f, lT + 0.08f));
                Vector3 lLook = Vector3.Lerp(Vector3.Lerp(lStartLook, lAhead + (lAhead - lPosition) * 4f, Mathf.Clamp01(lT * 4f)), lEndLook, HSMath.Smooth(Mathf.InverseLerp(0.45f, 1f, lT)));
                _rig.CutTo(lPosition, lLook, Mathf.Lerp(95f, 80f, lT));
                if (PostFX.Instance != null) PostFX.Instance.speed01 = Mathf.Sin(lT * Mathf.PI) * 0.8f;
                HUD.LinesBurst(Mathf.Sin(lT * Mathf.PI) * 0.6f);
                yield return null;
            }

            // 3. Back : the runner appears in a flash of light where the camera landed.
            _player.RespawnAt(lSafe);
            if (CourseBreak.Instance != null) CourseBreak.Instance.RewindTo(lSafe.s);
            if (ChaosDirector.Instance != null) ChaosDirector.Instance.RewindTo(lSafe.s);
            _graceUntil = Time.time + 2.5f;
            SetRunnerVisible(true);
            if (PostFX.Instance != null) PostFX.Instance.rewind = 0f;
            _rig.Snap();
            _rig.ClearOverride(SETTLE);
            HSJuice.Play(JuiceId.Respawn, new JuiceContext { position = _player.FocusPosition, direction = lFlat, normal = Vector3.up, intensity = 1f, size = 1f });
            yield return new WaitForSecondsRealtime(SETTLE);
            Busy = false;
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
