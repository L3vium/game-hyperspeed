using System.Collections;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Quick death : a glance at the fall, then the runner is rewound along the path it ran back to safe ground,
    // the world is restored, and control comes back. Never a long wait.
    public class RespawnDirector : MonoBehaviour
    {
        private const float FALL_VIEW = 0.25f;
        private const float REWIND = 0.5f;
        private const float SETTLE = 0.2f;

        private PlayerController _player;
        private CameraRig _rig;

        public bool Busy { get; private set; }

        public static RespawnDirector Create(PlayerController pPlayer, CameraRig pRig)
        {
            RespawnDirector lDirector = new GameObject("RespawnDirector").AddComponent<RespawnDirector>();
            lDirector._player = pPlayer;
            lDirector._rig = pRig;
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
            Breadcrumbs lCrumbs = _player.Crumbs;
            Vector3 lVelocity = _player.Velocity;
            Vector3 lHold = _rig.transform.position;

                        Juice.Flash(UIFactory.RED, 0.25f);
            AudioManager.Play("sfx_fall", 0.9f);
            for (float lTime = 0f; lTime < FALL_VIEW; lTime += Time.unscaledDeltaTime)
            {
                lVelocity.y -= Ballistics.G_FALL * Time.unscaledDeltaTime;
                _player.transform.position += lVelocity * Time.unscaledDeltaTime;
                _rig.SetOverride(lHold, _player.FocusPosition);
                yield return null;
            }

            // Rewind along the crumbs (newest first) down to the safe one.
            int lSafeIndex = 0;
            for (int i = 0; i < lCrumbs.Count; i++)
            {
                if (lCrumbs.Get(i).s <= lSafe.s + 0.5f) { lSafeIndex = i; break; }
                lSafeIndex = i;
            }
            if (CourseBreak.Instance != null) CourseBreak.Instance.RewindTo(lSafe.s);
            if (PostFX.Instance != null) PostFX.Instance.rewind = 1f;
            AudioManager.Play("sfx_rewind", 0.8f);
            Vector3 lStart = _player.transform.position;
            for (float lTime = 0f; lTime < REWIND; lTime += Time.unscaledDeltaTime)
            {
                float lT = HSMath.Smooth(Mathf.Clamp01(lTime / REWIND));
                Vector3 lPosition;
                Vector3 lForward;
                if (lCrumbs.Count == 0 || lSafeIndex == 0)
                {
                    lPosition = Vector3.Lerp(lStart, lSafe.position, lT);
                    lForward = lSafe.heading.sqrMagnitude > 0.1f ? lSafe.heading : Vector3.forward;
                }
                else
                {
                    float lF = lT * lSafeIndex;
                    int lA = Mathf.Min(Mathf.FloorToInt(lF), lSafeIndex);
                    int lB = Mathf.Min(lA + 1, lSafeIndex);
                    Breadcrumb lCa = lCrumbs.Get(lA);
                    Breadcrumb lCb = lCrumbs.Get(lB);
                    lPosition = Vector3.Lerp(lCa.position, lCb.position, lF - lA);
                    if (lT < 0.15f) lPosition = Vector3.Lerp(lStart, lPosition, lT / 0.15f);
                    lForward = lCa.heading.sqrMagnitude > 0.1f ? lCa.heading : Vector3.forward;
                }
                _player.transform.position = lPosition;
                Vector3 lFlat = Vector3.ProjectOnPlane(lForward, Vector3.up).normalized;
                _rig.SetOverride(lPosition + Vector3.up * 6f - lFlat * 14f, lPosition + Vector3.up * 1.2f + lFlat * 6f);
                yield return null;
            }

            _player.RespawnAt(lSafe);
            if (PostFX.Instance != null) PostFX.Instance.rewind = 0f;
            _rig.ClearOverride(SETTLE);
                        AudioManager.Play("sfx_respawn", 0.8f);
            yield return new WaitForSecondsRealtime(SETTLE);
            Busy = false;
        }
    }
}
