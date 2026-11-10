using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Route-following bot (cheat key 7) : it looks where a player would look, through virtual inputs, so stages are tested
    // through the real controller. Aggressive mode chains every boost it can reach : the proof that light speed exists.
    public class Autopilot : MonoBehaviour
    {
        public static bool Enabled;
        public static bool Aggressive;

        private PlayerController _player;
        private LevelLayout _layout;
        private int _actIndex;
        // Last ~3 s of samples, dumped on every death so failures can be read from the log.
        private readonly string[] _trace = new string[12];
        private int _traceCursor;
        private float _traceTimer;
        private float _diveTimer;
        private float _feelTimer;

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

        public void DumpTrace(string pHead)
        {
            System.Text.StringBuilder lLog = new System.Text.StringBuilder(pHead);
            for (int i = 0; i < _trace.Length; i++)
            {
                string lLine = _trace[(_traceCursor + i) % _trace.Length];
                if (lLine != null) lLog.Append("\n   ").Append(lLine);
            }
            Debug.Log(lLog.ToString());
        }

        private void OnDisable() => Release();

        private void Release()
        {
            HSInput.BotSteer = 0f;
            HSInput.BotSlideHeld = false;
            HSInput.BotDashHeld = false;
            HSInput.BotAimActive = false;
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
                _trace[_traceCursor] = "t" + Time.time.ToString("F2") + " s" + lS.ToString("F0") + " lat" + _player.Lateral.ToString("F1") + " pos" + _player.transform.position.ToString("F0")
                    + " " + _player.State + " v" + _player.Speed.ToString("F0") + " pitch" + _player.AimPitch.ToString("F0") + (_player.HasLandingPrediction ? " land" : " noLand") + (_player.Diving ? " dive" : "");
                _traceCursor = (_traceCursor + 1) % _trace.Length;
            }
            _feelTimer -= Time.deltaTime;
            if (_feelTimer <= 0f)
            {
                _feelTimer = 5f;
                Debug.Log("[HS-FEEL] s=" + lS.ToString("F0") + " v=" + _player.Speed.ToString("F0") + " ratio=" + (_player.Speed / _player.Profile.cruise).ToString("F2") + " light=" + _player.LightForm + " state=" + _player.State);
            }
            int lNext = Time.frameCount + 1;

            // Look at the point of the route ahead : a closer one when off the line.
            float lLook = Mathf.Max(30f, lSpeed * 0.45f);
            float lTargetS = lS + lLook;
            float lLateral = _layout.RouteLateral(lTargetS);
            Vector3 lPoint = _layout.GroundPoint(lTargetS, lLateral);
            HSInput.BotAimActive = true;
            HSInput.BotAimDirection = lPoint - _player.transform.position;
            HSInput.BotAimPitch = 0f;
            HSInput.BotSteer = 0f;

            // Past a lip with nothing below : look down to dive for a moment.
            if (_diveTimer > 0f)
            {
                _diveTimer -= Time.deltaTime;
                HSInput.BotAimPitch = -40f;
            }

            while (_actIndex < _layout.acts.Count && _layout.acts[_actIndex].s < lS - lSpeed * 0.5f) _actIndex++;
            if (_actIndex < _layout.acts.Count && lS >= _layout.acts[_actIndex].s)
            {
                RouteAct lAct = _layout.acts[_actIndex];
                if (lAct.action == RouteAction.Dive)
                {
                    if (_player.State == PlayerState.Air) { _diveTimer = 0.5f; _actIndex++; }
                }
                else if (lAct.action == RouteAction.Jump)
                {
                    HSInput.BotJumpFrame = lNext;
                    _actIndex++;
                }
                else if (lAct.action == RouteAction.Dash)
                {
                    HSInput.BotDashFrame = lNext;
                    _actIndex++;
                }
            }

            // Over a hole with the speed dropping : a jump and a dash buy distance, as a player would.
            if (_player.State == PlayerState.Air && !_layout.field.Solid(lS) && Vector3.Dot(_player.Velocity, _player.GravityUp) < -30f && _diveTimer <= 0f)
            {
                if (_player.HasDoubleJump) HSInput.BotJumpFrame = lNext;
                else if (_player.DashEnergy >= 1f) HSInput.BotDashFrame = lNext;
            }

            HSInput.BotDashHeld = false;
            if (Aggressive)
            {
                if (_player.DashEnergy >= 1f) HSInput.BotDashFrame = lNext;
            }
            else if (_player.State == PlayerState.Ground && _player.DashEnergy >= 2.9f && _player.Speed < _player.Profile.cruise * 1.4f) HSInput.BotDashFrame = lNext;
        }
    }
}
