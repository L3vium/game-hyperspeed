using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Route-following bot (F9) : steers to the route deck lateral with a PD controller and fires the layout actions
    // (jumps before lips, dives, drop-throughs, glides). Used to playtest every stage end to end.
    public class Autopilot : MonoBehaviour
    {
        public static bool Enabled;

        private PlayerController _player;
        private LevelLayout _layout;
        private int _actIndex;
        private float _slideHold;
        private float _punchCooldown;

        public void Init(PlayerController pPlayer, LevelLayout pLayout)
        {
            _player = pPlayer;
            _layout = pLayout;
            _player.OnDied += () => { if (Enabled) Debug.Log("[HS-LD] death s=" + _player.Progress.ToString("F0") + " deck=" + (_player.CurrentDeck != null ? _player.CurrentDeck.id : -1) + " v=" + _player.Speed.ToString("F0")); };
            // After a respawn the route acts behind the restart point are replayed.
            _player.OnRespawned += () => _actIndex = 0;
        }

        private void OnDisable() => Release();

        private void Release()
        {
            HSInput.BotSteer = 0f;
            HSInput.BotSlideHeld = false;
            HSInput.BotGlideHeld = false;
        }

        private void Update()
        {
            if (HSInput.AutopilotTogglePressed)
            {
                Enabled = !Enabled;
                Juice.Popup(Enabled ? "AUTOPILOT ON" : "AUTOPILOT OFF", Color.white, 1f);
                if (!Enabled) Release();
            }
            if (!Enabled || _player == null || _layout == null || !RaceContext.Running || _player.State == PlayerState.Dead)
            {
                if (Enabled) Release();
                return;
            }

            float lSpeed = Mathf.Max(_player.Speed, 20f);
            float lS = _player.Progress;
            int lNext = Time.frameCount + 1;
            _punchCooldown -= Time.deltaTime;

            // PD steering toward the route lateral a quarter second ahead.
            float lAheadS = lS + lSpeed * 0.25f;
            RouteNode lNode = _layout.RouteAt(lAheadS, _player.CurrentDeck);
            float lTarget = lNode.deck != null ? lNode.deck.LateralOffset(lAheadS) + lNode.lateral : 0f;
            float lLateralSpeed = Vector3.Dot(_player.Velocity, _layout.spine.Right(lS));
            HSInput.BotSteer = Mathf.Clamp(0.22f * (lTarget - _player.Lateral) - 0.035f * lLateralSpeed, -1f, 1f);

            // One-shot actions.
            while (_actIndex < _layout.acts.Count && _layout.acts[_actIndex].s < lS - 30f) _actIndex++;
            for (int i = _actIndex; i < _layout.acts.Count; i++)
            {
                RouteAct lAct = _layout.acts[i];
                if (lAct.s > lS + lSpeed * 0.1f) break;
                if (lS < lAct.s - lSpeed * 0.05f) continue;
                // An act that could not fire yet (wrong state) stays pending until the runner is well past it.
                if (!Fire(lAct, lNext) && lS < lAct.s + Mathf.Max(6f, lSpeed * 0.12f)) break;
                _actIndex = i + 1;
            }

            // Keep crouching through stacked membranes, glide when nothing is below.
            _slideHold -= Time.deltaTime;
            HSInput.BotSlideHeld = _slideHold > 0f;
            HSInput.BotGlideHeld = _player.State == PlayerState.Air && !_player.HasLandingPrediction && !_player.Diving && _player.AirTime > 0.3f;

            if (_punchCooldown <= 0f && _player.Speed < PlayerController.SMASH_SPEED)
            {
                foreach (Breakable lBreakable in Breakable.All)
                {
                    Vector3 lDelta = lBreakable.transform.position - _player.transform.position;
                    float lAlong = Vector3.Dot(lDelta, _player.Heading);
                    if (lAlong > 0f && lAlong < 8f + lSpeed * 0.08f && Vector3.ProjectOnPlane(lDelta, _player.Heading).magnitude < 3.5f)
                    {
                        HSInput.BotPunchFrame = lNext;
                        _punchCooldown = 0.35f;
                        break;
                    }
                }
            }
        }

        private bool Fire(RouteAct pAct, int pFrame)
        {
            bool lFired = true;
            switch (pAct.action)
            {
                case RouteAction.Jump:
                    lFired = _player.State == PlayerState.Ground;
                    if (lFired) HSInput.BotJumpFrame = pFrame;
                    break;
                case RouteAction.Dive:
                    lFired = _player.State == PlayerState.Air && !_player.Diving;
                    if (lFired) HSInput.BotSlideFrame = pFrame;
                    break;
                case RouteAction.DropThrough:
                    HSInput.BotSlideFrame = pFrame;
                    _slideHold = 0.8f;
                    break;
                case RouteAction.Punch:
                    HSInput.BotPunchFrame = pFrame;
                    break;
            }
            if (lFired) Debug.Log("[HS-BOT] " + pAct.action + "@" + pAct.s.ToString("F0") + " fired at s=" + _player.Progress.ToString("F0") + " " + _player.State + " frame " + pFrame);
            return lFired;
        }
    }
}
