using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Route-following bot (cheat key 7) : holds the route lateral, punches what is ahead, dives where the stage says so.
    // Used to playtest every stage end to end.
    public class Autopilot : MonoBehaviour
    {
        public static bool Enabled;

        private PlayerController _player;
        private LevelLayout _layout;
        private int _actIndex;
        // Last ~3 s of samples, dumped on every death so failures can be read from the log.
        private readonly string[] _trace = new string[12];
        private int _traceCursor;
        private float _traceTimer;
        private float _punchCooldown;

        public void Init(PlayerController pPlayer, LevelLayout pLayout)
        {
            _player = pPlayer;
            _layout = pLayout;
            _player.OnDied += () =>
            {
                if (!Enabled) return;
                System.Text.StringBuilder lLog = new System.Text.StringBuilder("[HS-LD] death s=" + _player.Progress.ToString("F0") + " v=" + _player.Speed.ToString("F0"));
                for (int i = 0; i < _trace.Length; i++)
                {
                    string lLine = _trace[(_traceCursor + i) % _trace.Length];
                    if (lLine != null) lLog.Append("\n   ").Append(lLine);
                }
                Debug.Log(lLog.ToString());
            };
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

        public void Toggle()
        {
            Enabled = !Enabled;
            Juice.Popup(Enabled ? "AUTOPILOT ON" : "AUTOPILOT OFF", Color.white, 1f);
            if (!Enabled) Release();
        }

        private void Update()
        {
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
                _trace[_traceCursor] = "t" + Time.time.ToString("F2") + " s" + lS.ToString("F0") + " lat" + _player.Lateral.ToString("F1") + " y" + _player.transform.position.y.ToString("F1")
                    + " ground" + _layout.Ground(lS, _player.Lateral).ToString("F1") + " " + _player.State + " v" + _player.Speed.ToString("F0")
                    + " vy" + Vector3.Dot(_player.Velocity, Vector3.up).ToString("F0") + (_player.HasLandingPrediction ? " land@" + _player.LandingPoint.y.ToString("F0") : " noLand")
                    + (_player.Diving ? " dive" : "");
                _traceCursor = (_traceCursor + 1) % _trace.Length;
            }
            int lNext = Time.frameCount + 1;
            _punchCooldown -= Time.deltaTime;

            // Steer toward the route lateral half a second ahead : Steer is a fraction of the max strafe speed.
            float lTarget = _layout.RouteLateral(lS + lSpeed * 0.5f);
            float lMax = _player.Profile.StrafeMax(lSpeed);
            HSInput.BotSteer = Mathf.Clamp((lTarget - _player.Lateral) * 2f / lMax, -1f, 1f);

            while (_actIndex < _layout.acts.Count && _layout.acts[_actIndex].s < lS - lSpeed * 0.5f) _actIndex++;
            if (_actIndex < _layout.acts.Count && lS >= _layout.acts[_actIndex].s)
            {
                RouteAct lAct = _layout.acts[_actIndex];
                if (lAct.action == RouteAction.Dive && _player.State == PlayerState.Air && !_player.Diving)
                {
                    HSInput.BotSlideFrame = lNext;
                    _actIndex++;
                }
                else if (lAct.action == RouteAction.Jump && _player.State == PlayerState.Ground)
                {
                    HSInput.BotJumpFrame = lNext;
                    _actIndex++;
                }
                else if (lAct.action == RouteAction.Punch)
                {
                    HSInput.BotPunchFrame = lNext;
                    _actIndex++;
                }
            }

            if (_player.State == PlayerState.Ground && !_player.Hyper && _player.Speed < _player.Profile.hyperEnter) HSInput.BotDashFrame = lNext;

            if (_punchCooldown <= 0f)
            {
                foreach (Breakable lBreakable in Breakable.All)
                {
                    Vector3 lDelta = lBreakable.transform.position - _player.transform.position;
                    float lAlong = Vector3.Dot(lDelta, _player.Heading);
                    if (lAlong > 0f && lAlong < lBreakable.radius + 20f + lSpeed * 0.2f && Vector3.ProjectOnPlane(lDelta, _player.Heading).magnitude < lBreakable.radius + 20f)
                    {
                        HSInput.BotPunchFrame = lNext;
                        _punchCooldown = 0.35f;
                        break;
                    }
                }
            }
        }
    }
}
