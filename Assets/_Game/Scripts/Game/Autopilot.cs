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
        // Last ~3 s of samples, dumped on every death so LD failures can be read from the log.
        private readonly string[] _trace = new string[12];
        private int _traceCursor;
        private float _traceTimer;
        private float _slideHold;
        private float _punchCooldown;

        public void Init(PlayerController pPlayer, LevelLayout pLayout)
        {
            _player = pPlayer;
            _layout = pLayout;
            _player.OnDied += () =>
            {
                if (!Enabled) return;
                System.Text.StringBuilder lLog = new System.Text.StringBuilder("[HS-LD] death s=" + _player.Progress.ToString("F0") + " deck=" + (_player.CurrentDeck != null ? _player.CurrentDeck.id : -1) + " v=" + _player.Speed.ToString("F0"));
                for (int i = 0; i < _trace.Length; i++)
                {
                    string lLine = _trace[(_traceCursor + i) % _trace.Length];
                    if (lLine != null) lLog.Append("\n   ").Append(lLine);
                }
                Debug.Log(lLog.ToString());
            };
            // After a respawn only the acts ahead of the restart point count (never replay the jump just behind).
            _player.OnRespawned += () =>
            {
                _actIndex = 0;
                while (_actIndex < _layout.acts.Count && _layout.acts[_actIndex].s < _player.Progress + 2f) _actIndex++;
            };
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
            _traceTimer -= Time.deltaTime;
            if (_traceTimer <= 0f)
            {
                _traceTimer = 0.25f;
                Deck lDeck = _player.CurrentDeck;
                float lFloor = lDeck != null ? lDeck.SurfacePoint(lS, _player.Lateral - lDeck.LateralOffset(lS)).y : float.NaN;
                _trace[_traceCursor] = "t" + Time.time.ToString("F2") + " s" + lS.ToString("F0") + " lat" + _player.Lateral.ToString("F1") + " y" + _player.transform.position.y.ToString("F1")
                    + " floor" + lFloor.ToString("F1") + " " + _player.State + " deck" + (lDeck != null ? lDeck.id : -1) + " v" + _player.Speed.ToString("F0")
                    + " vy" + Vector3.Dot(_player.Velocity, Vector3.up).ToString("F0") + (_player.HasLandingPrediction ? " land@" + _player.LandingPoint.y.ToString("F0") : " noLand")
                    + (_player.Gliding ? " glide" : "") + (_player.Diving ? " dive" : "");
                _traceCursor = (_traceCursor + 1) % _trace.Length;
            }
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

            // Dash on the ground like a decent player (keeps the rival benchmark honest and outruns collapse waves).
            if (_player.State == PlayerState.Ground && _player.Speed < 100f && !ActAhead(lS, 12f)) HSInput.BotDashFrame = lNext;

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

        // A jump only pays when the next route deck sits above the no-jump arc (the ledge assist covers ~1 m) :
        // at speed a short gap is crossed by running off the lip, while a jump would overshoot the next platform.
        private bool JumpNeeded(RouteAct pAct, float pSpeed)
        {
            Deck lFrom = pAct.deck;
            float lLip = lFrom != null ? Mathf.Min(pAct.s + 3f, lFrom.EndS) : pAct.s + 3f;
            float lFromY = lFrom != null ? lFrom.SurfacePoint(lLip, 0f).y : _player.transform.position.y;
            for (float lS = lLip + 1f; lS < lLip + 90f; lS += 1f)
            {
                RouteNode lNode = _layout.RouteAt(lS, null);
                if (lNode.deck == null || lNode.deck == lFrom || !lNode.deck.ContainsS(lS)) continue;
                float lTime = (lS - lLip) / Mathf.Max(pSpeed, 1f);
                float lArc = lFromY - 0.5f * 58f * lTime * lTime;
                return lNode.deck.SurfacePoint(lS, lNode.lateral).y > lArc + 1f;
            }
            return true;
        }

        private bool ActAhead(float pS, float pDistance)
        {
            for (int i = _actIndex; i < _layout.acts.Count; i++)
            {
                float lS = _layout.acts[i].s;
                if (lS > pS + pDistance) return false;
                if (lS >= pS - 2f) return true;
            }
            return false;
        }

        private bool Fire(RouteAct pAct, int pFrame)
        {
            bool lFired = true;
            switch (pAct.action)
            {
                case RouteAction.Jump:
                    lFired = _player.State == PlayerState.Ground;
                    if (lFired && JumpNeeded(pAct, _player.Speed)) HSInput.BotJumpFrame = pFrame;
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
