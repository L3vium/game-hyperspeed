using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Route-following bot (cheat key 7) : it looks where a player would look, through virtual inputs, so stages are tested
    // through the real controller. Aggressive mode chains every boost it can reach : the proof that light speed exists.
    public class Autopilot : MonoBehaviour
    {
        public static bool Enabled;
        public static bool Aggressive;
        // Sky mode (-hsRoute sky) : takes every kicker and follows the sky lanes (decks, ropes, rings) instead of the
        // ground lane.
        public static bool Sky;

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
            HSInput.BotRun = 0f;
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
                Debug.Log("[HS-FEEL] s=" + lS.ToString("F0") + " v=" + _player.Speed.ToString("F0") + " ratio=" + (_player.Speed / _player.Profile.cruise).ToString("F2") + " hyper=" + _player.Hyper + " light=" + _player.LightForm + " state=" + _player.State);
            }
            int lNext = Time.frameCount + 1;

            // Look at the point of the route ahead : a closer one when off the line.
            float lLook = Mathf.Max(30f, lSpeed * 0.45f);
            float lTargetS = lS + lLook;
            float lLateral = _layout.RouteLateral(lTargetS);
            Vector3 lPoint = _layout.GroundPoint(lTargetS, lLateral);
            bool lInSky = false;
            // Before a light highway the sky route waits : the pad line that charges the light is on the ground.
            bool lLightAhead = false;
            if (_player.Profile.LightEnabled)
                foreach (Vector2 lHighway in _layout.highways)
                    if (lHighway.x > lS && lHighway.x - lS < Mathf.Max(9000f, lSpeed * 14f)) lLightAhead = true;
            if (Sky && !lLightAhead)
            {
                float lGroundY = _layout.GroundPoint(lS, _player.Lateral).y;
                SkyRoute.Piece lPiece;
                bool lAbove = _player.transform.position.y > lGroundY + 25f;
                // Up there means near the piece's own altitude : over a gap between low platforms the runner is above
                // the ground too, but the lane is hundreds of metres higher and steering to it only drops it off an edge.
                if (lAbove && SkyRoute.Find(lS, lSpeed * 3f, out lPiece)
                    && Mathf.Abs(_player.transform.position.y - lPiece.HeightAt(Mathf.Clamp(lTargetS, lPiece.s0, lPiece.s1))) < 90f)
                {
                    // Up there : follow the decks, ropes and rings.
                    float lAt = Mathf.Clamp(lTargetS, lPiece.s0, lPiece.s1);
                    lPoint = SkyKit.Point(_layout, lAt, lPiece.LateralAt(lAt), lPiece.HeightAt(lAt));
                    lInSky = true;
                }
                else if (SkyRoute.NextKicker(lS - 5f, lSpeed * 4f, out lPiece))
                {
                    // A kicker ahead : line up with it, without leaving the floor under the look point while it is
                    // still further than that.
                    if (lPiece.s0 <= lTargetS) lPoint = _layout.GroundPoint(lTargetS, lPiece.lateral0);
                    else
                    {
                        float lCenter = _layout.field.CenterLateral(lTargetS);
                        float lRoom = _layout.field.HalfWidth(lTargetS) * 0.8f;
                        lPoint = _layout.GroundPoint(lTargetS, Mathf.Clamp(lPiece.lateral0, lCenter - lRoom, lCenter + lRoom));
                    }
                    lInSky = lS > lPiece.s0 - lSpeed * 2f;
                }
            }
            // A hole on the way : round it on the side with more room, or jump it when it is too close to turn.
            if (!lInSky && TrackHoles.Count > 0)
            {
                for (float lAhead = lS + 15f; lAhead < lS + lSpeed * 1.4f; lAhead += 12f)
                {
                    Vector4 lHole;
                    if (!TrackHoles.Find(_layout.GroundPoint(lAhead, lLateral), 14f, out lHole)) continue;
                    float lHoleLateral;
                    _layout.spine.Project(lHole, lAhead, out lHoleLateral);
                    float lAway = lHoleLateral > _layout.field.CenterLateral(lAhead) ? -1f : 1f;
                    lLateral = lHoleLateral + lAway * (lHole.w + 28f);
                    lPoint = _layout.GroundPoint(lTargetS, lLateral);
                    if (lAhead - lS < lSpeed * 0.3f && _player.State == PlayerState.Ground) HSInput.BotJump();
                    break;
                }
            }
            HSInput.BotAimActive = true;
            HSInput.BotAimDirection = lPoint - _player.transform.position;
            HSInput.BotAimPitch = 0f;
            HSInput.BotSteer = 0f;
            // A player runs by holding the run key : so does the bot.
            HSInput.BotRun = 1f;

            // Past a lip with nothing below : look down to dive for a moment.
            if (_diveTimer > 0f)
            {
                _diveTimer -= Time.deltaTime;
                HSInput.BotAimPitch = -40f;
            }

            while (_actIndex < _layout.acts.Count && _layout.acts[_actIndex].s < lS - lSpeed * 0.5f) _actIndex++;
            // In the sky the ground lane's jumps and dives do not apply.
            if (lInSky)
            {
                while (_actIndex < _layout.acts.Count && _layout.acts[_actIndex].s <= lS) _actIndex++;
            }
            else if (_actIndex < _layout.acts.Count && lS >= _layout.acts[_actIndex].s)
            {
                RouteAct lAct = _layout.acts[_actIndex];
                if (lAct.action == RouteAction.Dive)
                {
                    if (_player.State == PlayerState.Air) { _diveTimer = 0.5f; _actIndex++; }
                }
                else if (lAct.action == RouteAction.Jump)
                {
                    HSInput.BotJump();
                    _actIndex++;
                }
                else if (lAct.action == RouteAction.Dash)
                {
                    HSInput.BotDashFrame = lNext;
                    _actIndex++;
                }
            }

            // Over a hole with the speed dropping : a jump and a dash buy distance, as a player would.
            if (!lInSky && _player.State == PlayerState.Air && !_layout.field.Solid(lS) && Vector3.Dot(_player.Velocity, _player.GravityUp) < -30f && _diveTimer <= 0f)
            {
                if (_player.HasDoubleJump) HSInput.BotJump();
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
