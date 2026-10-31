using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Simple track-following bot (F9). Used to playtest levels end to end and as an attract mode.
    public class Autopilot : MonoBehaviour
    {
        public static bool Enabled;

        private PlayerController _player;
        private float _jumpCooldown;
        private float _airDecisionCooldown;

        public void Init(PlayerController pPlayer)
        {
            _player = pPlayer;
        }

        private void OnDisable() => Release();

        private void Release()
        {
            HSInput.BotSteer = 0f;
            HSInput.BotSlideHeld = false;
        }

        private void Update()
        {
            if (HSInput.AutopilotTogglePressed)
            {
                Enabled = !Enabled;
                Juice.Popup(Enabled ? "AUTOPILOT ON" : "AUTOPILOT OFF", Color.white, 1f);
                if (!Enabled) Release();
            }
            if (!Enabled || _player == null || RaceContext.Path == null || !RaceContext.Running) return;

            TrackPath lPath = RaceContext.Path;
            float lSpeed = Mathf.Max(_player.Speed, 20f);
            float lProgress = _player.Progress;
            _jumpCooldown -= Time.deltaTime;
            _airDecisionCooldown -= Time.deltaTime;
            int lNext = Time.frameCount + 1;

            // Steer toward a point ahead on the center line.
            TrackSample lTarget = lPath.Evaluate(lProgress + 10f + lSpeed * 0.3f);
            Vector3 lUp = _player.State == PlayerState.Ground ? _player.Up : _player.GravityUp;
            Vector3 lToTarget = Vector3.ProjectOnPlane(lTarget.position - _player.transform.position, lUp);
            float lAngle = Vector3.SignedAngle(Vector3.ProjectOnPlane(_player.Heading, lUp), lToTarget, lUp);
            HSInput.BotSteer = Mathf.Clamp(lAngle / 25f, -1f, 1f);

            TrackSample lHere = lPath.Evaluate(lProgress);
            TrackSample lAhead = lPath.Evaluate(lProgress + lSpeed * 0.05f + 2f);
            float lHeight = Vector3.Dot(_player.transform.position - lHere.position, lHere.Up);

            switch (_player.State)
            {
                case PlayerState.Ground:
                    if (lHere.solid && !lAhead.solid && _jumpCooldown <= 0f && !HasRailAhead(lProgress))
                    {
                        HSInput.BotJumpFrame = lNext;
                        _jumpCooldown = 0.4f;
                    }
                    break;
                case PlayerState.Air:
                    float lVertical = Vector3.Dot(_player.Velocity, _player.GravityUp);
                    if (_airDecisionCooldown <= 0f && lVertical < 0f && !lHere.solid && lHeight < 1.5f)
                    {
                        _airDecisionCooldown = 0.3f;
                        if (_player.HasDoubleJump) HSInput.BotJumpFrame = lNext;
                        else if (_player.HasAirDash) HSInput.BotDashFrame = lNext;
                    }
                    else if (_airDecisionCooldown <= 0f && lVertical < -4f && lHere.solid && lHeight < 5f && lHeight > 1.2f && !_player.Diving)
                    {
                        _airDecisionCooldown = 0.5f;
                        HSInput.BotSlideFrame = lNext;
                    }
                    break;
                case PlayerState.WallRun:
                    if (_player.StateTime > 0.45f && _jumpCooldown <= 0f)
                    {
                        HSInput.BotJumpFrame = lNext;
                        _jumpCooldown = 0.5f;
                    }
                    break;
            }

            foreach (Breakable lBreakable in Breakable.All)
            {
                Vector3 lDelta = lBreakable.transform.position - _player.transform.position;
                float lAlong = Vector3.Dot(lDelta, _player.Heading);
                if (lAlong > 0f && lAlong < 6f + lSpeed * 0.08f && Vector3.ProjectOnPlane(lDelta, _player.Heading).magnitude < 3.5f && _player.Speed < PlayerController.SMASH_SPEED)
                {
                    HSInput.BotPunchFrame = lNext;
                    break;
                }
            }
        }

        private static bool HasRailAhead(float pProgress)
        {
            TrackSample lSample = RaceContext.Path.Evaluate(pProgress + 8f);
            foreach (GrindRail lRail in GrindRail.All)
            {
                float lDistance, lRailDistance;
                if (lRail.FindNearest(lSample.position, 4f, out lDistance, out lRailDistance)) return true;
            }
            return false;
        }
    }
}
