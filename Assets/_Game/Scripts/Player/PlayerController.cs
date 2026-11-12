using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum PlayerState { Frozen, Ground, Air, Grind, Zip, Crash, Dead, Finished }

    // Kinematic runner on a 3D track : the mouse gives the forward direction, A / D strafes across it. It sticks to any
    // surface it runs on, even upside down. Speed comes from instant kicks that fade away, with no ceiling but a safety
    // one. Only a long chain of boosts turns it to light.
    public partial class PlayerController : MonoBehaviour, ICameraTarget
    {
        // ---------------- tuning
        private const float RUN_ACCEL = 90f;
        private const float SLOPE_ACCEL = 46f;
        // Jumps grow with the speed : a fast runner leaps far higher (base + share of the ground speed, capped).
        private const float JUMP_VELOCITY = 62f;
        private const float JUMP_SPEED_SHARE = 0.1f;
        private const float JUMP_VELOCITY_MAX = 140f;
        private const float DASH_TIME = 0.26f;
        // After a dash press, breakables still shatter for this long : the timing is generous.
        private const float BREAK_WINDOW = 0.6f;
        // A dash pressed during the cooldown or with an empty gauge fires as soon as it can, within this time.
        private const float DASH_BUFFER = 0.2f;
        private const float COYOTE_TIME = 0.2f;
        private const float JUMP_BUFFER = 0.18f;
        private const float RADIUS = 0.45f;
        private const float HEIGHT = 1.8f;
        private const float SKIN = 0.04f;
        private const float GROUND_MIN_DOT = 0.35f;
        private const float START_WINDOW_BEFORE = 0.3f;
        private const float START_WINDOW_AFTER = 0.2f;
        private const float FREE_ROAM_SPEED = 34f;
        // Above cruise : half of the extra speed is gone after about 5 s, sooner the further above you are.
        private const float DRAG = 0.13f;
        private const float DRAG_OVER = 0.12f;
        private const float DRAG_LIGHT = 0.1f;
        private const float LIGHT_HOLD = 0.5f;
        // A full light charge lasts this long in light form (light gates refill it).
        private const float LIGHT_CHARGE_TIME = 15f;
        private const float AIM_PITCH_MIN = -80f;
        private const float AIM_PITCH_MAX = 70f;
        // No run asked : the runner slows down to a stop (fraction of cruise lost per second), harder with the brake.
        private const float COAST_DECEL = 0.9f;
        private const float BRAKE_DECEL = 2.6f;
        // Out of the stage : sideways past the walls, or this far under the floor around you.
        private const float OUT_SIDE_MARGIN = 75f;
        private const float OUT_UNDER = 90f;
        private const float FALL_TIME = 0.55f;
        private const float SCENERY_TIME = 0.35f;
        private const float WRONG_WAY_TIME = 0.8f;
        private const float BREAK_KICK = 0.3f;

        // ---------------- state
        public PlayerState State { get; private set; } = PlayerState.Frozen;
        public Vector3 Velocity => _velocity;
        // The real motion, on a surface as well as in the air.
        public Vector3 OnGroundVelocity => OnSurface ? _moveDir * _speed + Vector3.Cross(_up, _moveDir).normalized * _slip : _velocity;
        public Vector3 Up => _up;
        public Vector3 GravityUp => _gravityUp;
        public Vector3 Heading => _heading;
        public float Speed => OnSurface ? _speed : _velocity.magnitude;
        public float TotalSpeed => OnSurface ? Mathf.Sqrt(_speed * _speed + _slip * _slip) : _velocity.magnitude;
        private bool OnSurface => State == PlayerState.Ground || State == PlayerState.Grind || State == PlayerState.Zip || State == PlayerState.Finished;
        public Vector3 FacingDirection => _heading;
        public float BoostAmount => Mathf.Clamp01(_boostTimer / 0.6f);
        public SpeedProfile Profile => _profile;
        public bool CheatInvincible;
        // Menus own the keyboard (title screen) : the runner runs on its own.
        public bool InputLocked;
        public bool Hyper { get; private set; }
        public bool LightForm { get; private set; }
        public bool Drifting { get; private set; }
        public int DriftDirection { get; private set; }
        public float DriftCharge { get; private set; }
        public int DriftTier => DriftCharge >= 1.8f ? 3 : DriftCharge >= 1.1f ? 2 : DriftCharge >= 0.5f ? 1 : 0;
        public bool Sliding { get; private set; }
        public bool Diving { get; private set; }
        public bool Dashing => _dashTimer > 0f || Thrusting;
        public bool Thrusting { get; private set; }
        public bool Punching => _punchTimer > 0f;
        public bool HasDoubleJump { get; private set; } = true;
        public float DashEnergy => _dashEnergy;
        public float Steer { get; private set; }
        // How hard the player asks to run (0 to 1) and to brake : nothing asked, nothing run.
        public float RunInput { get; private set; }
        public float BrakeInput { get; private set; }
        // Running against the track for a moment : the HUD says so.
        public bool WrongWay { get; private set; }
        private float _wrongWayTime;
        // Riding a leaning wall : which side, 0 when on the floor.
        public int WallSide { get; private set; }
        public bool WallRiding => WallSide != 0;
        public float StateTime { get; private set; }
        public int Combo { get; private set; }
        public float ComboTimer01 => Mathf.Clamp01(_comboTimer / 3f);
        public int Respawns { get; private set; }
        public float TopSpeed { get; private set; }
        public bool Invulnerable => _invulnerable > 0f;
        public float Progress { get; private set; }
        // Progress where the runner last had its feet on something : platforms behind give way from there.
        public float GroundProgress { get; private set; }
        public float Lateral { get; private set; }
        public float DistanceFromPath => Mathf.Abs(Lateral);
        public bool FreeRoam => _freeRoam;
        public PlayerVisual Visual => _visual;
        public float AirTime => _airTime;

        // Camera / visual signals.
        public float DiveTime { get; private set; }
        public bool HasLandingPrediction { get; private set; }
        public Vector3 LandingPoint { get; private set; }
        public float LandingTime { get; private set; }

        public event System.Action OnJumped;
        public event System.Action OnDoubleJumped;
        public event System.Action<float> OnLanded;
        public event System.Action OnPunched;
        public event System.Action OnDashed;
        public event System.Action OnCrashed;
        public event System.Action OnRespawned;
        public event System.Action OnDied;
        public event System.Action OnHyper;
        public event System.Action OnLight;
        public event System.Action OnPerfectStart;

        private Vector3 _velocity;
        private Vector3 _up = Vector3.up;
        private Vector3 _gravityUp = Vector3.up;
        private Vector3 _heading = Vector3.forward;
        private Vector3 _moveDir = Vector3.forward;
        private float _speed;
        private float _lastGrounded;
        private float _jumpBufferedAt = -10f;
        private float _dashTimer;
        private float _breakTimer;
        private float _dashBufferedAt = -10f;
        private float _dashCooldown;
        private float _dashEnergy = DASH_ENERGY;
        private float _dashHeldTime;
        private float _punchTimer;
        private float _punchCooldown;
        private float _invulnerable;
        private float _crashTimer;
        private float _comboTimer;
        private float _airTime;
        private Vector3 _smoothCameraUp = Vector3.up;
        private bool _autoRun;
        private bool _punchHit;
        private float _boostTimer;
        private float _goTime = -1f;
        private float _startPressTime = -1f;
        private bool _freeRoam;
        private bool _barrier;
        private float _overLight;
        private int _substeps = 1;

        // Where the mouse looks : a direction lying on the surface you stand on, and a pitch above that surface.
        private Vector3 _aimForward = Vector3.forward;
        private Vector3 _aimUp = Vector3.up;
        public float AimPitch { get; private set; }

        private LevelLayout _layout;
        private SpeedProfile _profile = new SpeedProfile();
        private PlayerVisual _visual;
        private Breadcrumbs _crumbs;

        // ---------------- camera target
        public Vector3 FocusPosition => (_visual != null ? _visual.Root.position : transform.position) + PhysUp * 0.9f;
        public Vector3 CameraUp => _smoothCameraUp;
        // The body's own up : the surface under the feet, or gravity in the air.
        private Vector3 PhysUp => OnSurface ? _up : _gravityUp;
        // The view's up : it only turns where gravity really turns (loops, twists, wall turns). Everywhere else it is
        // the line's frame without its bank, so bumps, tilted blocks and mountain flanks never tip the camera.
        private Vector3 CameraUpRaw
        {
            get
            {
                if (_layout == null || _freeRoam) return PhysUp;
                SpineSample lSample = _layout.spine.samples[_layout.spine.IndexOf(Progress)];
                if (lSample.localGravity) return PhysUp;
                // A short hop keeps the view it had : the frame only goes back to world up on a real flight.
                if (!OnSurface) return _airTime < 0.6f ? _smoothCameraUp : Vector3.up;
                Vector3 lForward = lSample.forward;
                Vector3 lRight = Vector3.Cross(Vector3.up, lForward);
                if (lRight.sqrMagnitude < 0.01f) return lSample.up;
                return Vector3.Cross(lForward, lRight.normalized).normalized;
            }
        }
        public Vector3 CameraForward => _aimForward;
        // The full look direction, pitch included.
        public Vector3 AimDirection => Quaternion.AngleAxis(-AimPitch, Vector3.Cross(_aimUp, _aimForward).normalized) * _aimForward;
        // 0 at low speed, 1 when the runner is about to turn to light.
        public float Speed01 => Mathf.Clamp01((Speed - _profile.cruise * 0.55f) / (_profile.lightEnter - _profile.cruise * 0.55f));
        public float CameraRoll => -Steer * 4f - (Drifting ? DriftDirection * 8f : 0f) + WallSide * 6f;
        public bool HyperMode => Hyper;
        // How hard you dive : 0 not diving, 1 looking straight down. A belly slide counts for half.
        public float Dive01 => Diving ? Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(-18f, AIM_PITCH_MIN, AimPitch)) : BellySliding ? 0.45f : 0f;

        public void Init(LevelLayout pLayout, PlayerVisual pVisual)
        {
            _layout = pLayout;
            _profile = pLayout.profile;
            _visual = pVisual;
            _crumbs = new Breadcrumbs();
            Place(pLayout.spawnS, pLayout.field.CenterLateral(pLayout.spawnS));
            State = PlayerState.Frozen;
        }

        private void Place(float pS, float pLateral)
        {
            _up = _layout.field.Normal(pS);
            transform.position = _layout.GroundPoint(pS, pLateral) + _up * 0.05f;
            Vector3 lForward = _layout.Flow(pS);
            _gravityUp = _up;
            _smoothCameraUp = _up;
            _heading = Vector3.ProjectOnPlane(lForward, _up).normalized;
            _moveDir = _heading;
            _aimForward = _heading;
            _aimUp = _up;
            AimPitch = 0f;
            _velocity = Vector3.zero;
            _speed = 0f;
            _slip = 0f;
            Progress = pS;
            GroundProgress = pS;
            Lateral = pLateral;
            transform.rotation = Quaternion.LookRotation(_heading, _up);
        }

        // ================================================================ race flow

        // Before GO : free movement around the start plaza, held behind the limit line.
        // pDrop > 0 : the runner comes down from the sky and slams on the plaza.
        public void BeginFreeRoam(bool pBarrier = true, float pDrop = 0f)
        {
            _freeRoam = true;
            _barrier = pBarrier;
            _speed = 0f;
            if (pDrop > 0f)
            {
                transform.position += Vector3.up * pDrop;
                _velocity = Vector3.down * 40f;
                _gravityUp = Vector3.up;
                SetState(PlayerState.Air);
            }
            else SetState(PlayerState.Ground);
        }

        // Rocket start : press jump / dash / forward right on GO.
        public void ArmStart(float pGoTime)
        {
            _goTime = pGoTime;
            _startPressTime = -1f;
        }

        public void StartRunning()
        {
            _freeRoam = false;
            _barrier = false;
            if (State == PlayerState.Frozen || State == PlayerState.Dead) SetState(PlayerState.Ground);
            _slip = 0f;
            // Already asking to run on GO : you leave at once.
            if (HSInput.Run > 0.1f || Autopilot.Enabled) _speed = Mathf.Max(_speed, _profile.cruise * 0.8f);
            float lDelta = _startPressTime - _goTime;
            if (_startPressTime > 0f && lDelta >= -START_WINDOW_BEFORE && lDelta <= START_WINDOW_AFTER) PerfectStart();
        }

        private void PerfectStart()
        {
            _speed = Mathf.Max(_speed, _profile.cruise * 1.45f);
            _boostTimer = 2f;
            OnPerfectStart?.Invoke();
            HSJuice.Play(JuiceId.PerfectStart, Jx(transform.position));
        }

        private bool StartInputPressed => HSInput.JumpPressed || HSInput.DashPressed || HSInput.RunPressed;

        // Victory lap (2026-11-12) : after the line the runner runs the whole stage again at this pace, for ever.
        public float LapSpeed;

        // ---------------- light speed (2026-11-12)
        // On a light highway : light form runs at the stage's light speed (12 to 20 km/s) ; without it, the highway's
        // stream still carries you at 40 % of that.
        public bool OnHighway => _layout != null && _profile.LightEnabled && _layout.InHighway(Progress);
        public float Light01 => _profile.LightEnabled ? Mathf.Clamp01((Speed - _profile.lightEnter) / Mathf.Max(1f, _profile.lightSpeed - _profile.lightEnter)) : 0f;
        private const float HIGHWAY_STREAM = 0.4f;

        // Speed the highway asks for right now (0 off a highway).
        private float HighwaySpeed => !OnHighway || State == PlayerState.Finished ? 0f : LightForm ? _profile.lightSpeed : _profile.lightSpeed * HIGHWAY_STREAM;

        private void UpdateHighwaySpeed(float pDt)
        {
            float lTarget = HighwaySpeed;
            if (lTarget <= 0f) return;
            // Eases up to it in about a second and a half, holds it without drag.
            float lRate = _profile.lightSpeed * 0.7f * pDt;
            if (OnSurface) _speed = Mathf.Max(_speed, Mathf.MoveTowards(_speed, lTarget, lRate));
            else
            {
                Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
                float lFlatSpeed = lFlat.magnitude;
                if (lFlatSpeed > 1f && lFlatSpeed < lTarget) _velocity += lFlat / lFlatSpeed * (Mathf.MoveTowards(lFlatSpeed, lTarget, lRate) - lFlatSpeed);
            }
        }

        public void Finish()
        {
            _autoRun = true;
            _freeRoam = false;
            if (State == PlayerState.Ground) SetState(PlayerState.Finished);
        }

        // ================================================================ update

        private void Update()
        {
            float lDt = Time.deltaTime;
            if (lDt <= 0f)
            {
                // Hit-stop : the world holds still but nothing you press is lost, and the view still answers.
                if (!Juice.Paused && State != PlayerState.Frozen && State != PlayerState.Dead)
                {
                    BufferInput();
                    UpdateAim(Time.unscaledDeltaTime);
                }
                return;
            }

            // Only a press inside the window counts (an early one is not used up).
            float lStartDelta = Time.time - _goTime;
            if (_goTime > 0f && _startPressTime < 0f && lStartDelta >= -START_WINDOW_BEFORE && lStartDelta <= START_WINDOW_AFTER && StartInputPressed)
            {
                _startPressTime = Time.time;
                if (State != PlayerState.Frozen && !_freeRoam && Time.time >= _goTime) PerfectStart();
            }
            if (State == PlayerState.Frozen || State == PlayerState.Dead)
            {
                _visual?.Tick(lDt);
                return;
            }

            UpdateAim(lDt);
            ReadInput(lDt);
            Timers(lDt);
            if (State == PlayerState.Air || State == PlayerState.Crash) PredictAir();

            float lDistance = TotalSpeed * lDt;
            // At light speed (up to 20 km/s, over 300 m a frame) the steps stay a few metres long : nothing is skipped.
            int lSubsteps = Mathf.Clamp(Mathf.CeilToInt(lDistance / 1.2f), 1, 28);
            if (lDistance > 28f * 1.2f) lSubsteps = Mathf.Clamp(Mathf.CeilToInt(lDistance / 6f), 28, 72);
            _substeps = lSubsteps;
            float lSubDt = lDt / lSubsteps;
            for (int i = 0; i < lSubsteps; i++)
            {
                switch (State)
                {
                    case PlayerState.Ground:
                    case PlayerState.Finished: GroundStep(lSubDt); break;
                    case PlayerState.Air:
                    case PlayerState.Crash: AirStep(lSubDt); break;
                    case PlayerState.Grind: GrindStep(lSubDt); break;
                }
                if (State == PlayerState.Dead) break;
                TouchStuff();
            }
            if (State == PlayerState.Dead)
            {
                _visual?.Tick(lDt);
                return;
            }

            UpdateHyper(lDt);
            TrackProgress();
            CheckFallOut();
            UpdateWrongWay(lDt);
            if (State == PlayerState.Ground && !_freeRoam) _crumbs.Record(this, _layout, Progress, Lateral, _speed, Time.time);
            transform.rotation = Quaternion.LookRotation(_heading, OnSurface ? _up : _gravityUp);
            AudioManager.SetLoop("sfx_boost_loop", BoostAmount * 0.6f, 0.9f + BoostAmount * 0.3f);
            _smoothCameraUp = Vector3.Slerp(_smoothCameraUp, CameraUpRaw, 1f - Mathf.Exp(-12f * lDt)).normalized;
            TopSpeed = Mathf.Max(TopSpeed, Speed);
            _visual?.Tick(lDt);
        }

        // The aim is carried along by the surface, so a loop never flips the view. The mouse then turns it freely.
        private void UpdateAim(float pDt)
        {
            Vector3 lRefUp = CameraUpRaw;
            _aimForward = Quaternion.FromToRotation(_aimUp, lRefUp) * _aimForward;
            _aimUp = lRefUp;

            if (_layout != null && (InputLocked || _autoRun))
            {
                // Nobody at the controls : look at the track ahead.
                float lAhead = Progress + Mathf.Max(30f, Speed * 0.5f);
                Vector3 lTarget = _layout.GroundPoint(lAhead, _layout.field.CenterLateral(lAhead)) - transform.position;
                AimAt(lTarget, 0f, 6f * pDt);
            }
            else if (HSInput.BotAimActive) AimAt(HSInput.BotAimDirection, HSInput.BotAimPitch, 12f * pDt);
            else
            {
                Vector2 lLook = HSInput.LookDelta;
                // Gamepad : the left stick sideways steers, the right stick looks. Both turn the same aim.
                if (State != PlayerState.Crash && !_freeRoam) lLook.x += HSInput.TurnStick * HSInput.PAD_TURN * Mathf.Lerp(1f, 0.8f, Speed01) * Time.unscaledDeltaTime;
                _aimForward = Quaternion.AngleAxis(lLook.x, _aimUp) * _aimForward;
                // Mouse up looks up (GameSession.InvertY flips it in HSInput).
                AimPitch = Mathf.Clamp(AimPitch + lLook.y, AIM_PITCH_MIN, AIM_PITCH_MAX);
                // Slight help at very high speed : the line ahead pulls the aim, never enough to fight the mouse.
                if (_layout != null && !_freeRoam && OnSurface)
                {
                    float lAssist = Mathf.Lerp(0f, 12f, Speed01) * Mathf.Deg2Rad * pDt;
                    AimAt(_layout.Flow(Progress + Speed * 0.3f), AimPitch, lAssist);
                }
            }
            Vector3 lFlat = Vector3.ProjectOnPlane(_aimForward, _aimUp);
            _aimForward = lFlat.sqrMagnitude > 0.0001f ? lFlat.normalized : Vector3.Cross(_aimUp, Vector3.right).normalized;
        }

        private void AimAt(Vector3 pDirection, float pPitch, float pRadians)
        {
            Vector3 lFlat = Vector3.ProjectOnPlane(pDirection, _aimUp);
            if (lFlat.sqrMagnitude > 0.0001f) _aimForward = Vector3.RotateTowards(_aimForward, lFlat.normalized, pRadians, 0f);
            AimPitch = Mathf.MoveTowards(AimPitch, pPitch, pRadians * Mathf.Rad2Deg);
        }

        private bool _jumpPressed, _dashPressed, _slidePressed, _slideHeld, _punchPressed, _dashHeld;

        // How fast the mouse is turning the view, in degrees per second (smoothed).
        public float MouseTurn { get; private set; }

        // Presses made during a hit-stop, played on the next real frame.
        private bool _bufJump, _bufDash, _bufSlide, _bufPunch;

        private void BufferInput()
        {
            _bufJump |= HSInput.JumpPressed;
            _bufDash |= HSInput.DashPressed;
            _bufSlide |= HSInput.SlidePressed;
            _bufPunch |= HSInput.PunchPressed;
        }

        private void ReadInput(float pDt)
        {
            bool lCanAct = State != PlayerState.Crash && State != PlayerState.Finished && !_autoRun && !InputLocked;
            Steer = lCanAct ? HSInput.Steer : 0f;
            _turnStick = lCanAct ? HSInput.TurnStick : 0f;
            // Nobody at the controls (title runner) : it runs on its own.
            RunInput = InputLocked ? 1f : _autoRun ? 0f : HSInput.Run;
            BrakeInput = lCanAct ? HSInput.Brake : 0f;
            float lMouseTurn = lCanAct && !HSInput.BotAimActive ? HSInput.LookDelta.x / Mathf.Max(0.0001f, pDt) : 0f;
            MouseTurn = Mathf.Lerp(MouseTurn, lMouseTurn, 1f - Mathf.Exp(-12f * pDt));
            _jumpPressed = lCanAct && (HSInput.JumpPressed || _bufJump);
            _dashPressed = lCanAct && (HSInput.DashPressed || _bufDash) && !_freeRoam;
            _dashHeld = lCanAct && HSInput.DashHeld && !_freeRoam;
            _slidePressed = lCanAct && (HSInput.SlidePressed || _bufSlide);
            _slideHeld = lCanAct && HSInput.SlideHeld;
            _punchPressed = lCanAct && (HSInput.PunchPressed || _bufPunch) && !_freeRoam;
            _bufJump = _bufDash = _bufSlide = _bufPunch = false;

            if (_jumpPressed) _jumpBufferedAt = Time.time;
            if (_punchPressed && _punchCooldown <= 0f) Punch();
            if (_dashPressed) _dashBufferedAt = Time.time;
            if (lCanAct && Time.time - _dashBufferedAt < DASH_BUFFER && _dashCooldown <= 0f && _dashEnergy >= 1f)
            {
                _dashBufferedAt = -10f;
                Dash();
            }
            if (_jumpPressed) TryJump();
            UpdateThrust(pDt);
        }

        private void Timers(float pDt)
        {
            StateTime += pDt;
            _dashTimer -= pDt;
            _breakTimer -= pDt;
            _dashCooldown -= pDt;
            _punchTimer -= pDt;
            _punchCooldown -= pDt;
            _invulnerable -= pDt;
            _railCooldown -= pDt;
            _boostTimer -= pDt;
            _comboTimer -= pDt;
            if (_comboTimer <= 0f) Combo = 0;
            _dashEnergy = Mathf.Min(DASH_ENERGY, _dashEnergy + DASH_RECHARGE * pDt * (Thrusting ? 0f : 1f));
            if (Diving) DiveTime += pDt;
            else DiveTime = Mathf.MoveTowards(DiveTime, 0f, pDt * 3f);

            if (State == PlayerState.Crash)
            {
                _crashTimer -= pDt;
                if (_crashTimer <= 0f) SetState(PlayerState.Air);
            }
        }

        private void SetState(PlayerState pState)
        {
            if (State == pState) return;
            if (State == PlayerState.Grind) AudioManager.SetLoop("sfx_grind_loop", 0f);
            State = pState;
            StateTime = 0f;
            if (pState != PlayerState.Ground && pState != PlayerState.Finished)
            {
                EndDrift(false);
                WallSide = 0;
                AudioManager.SetLoop("sfx_wallrun_loop", 0f);
            }
            if (pState == PlayerState.Ground || pState == PlayerState.Finished)
            {
                _airTime = 0f;
                HasLandingPrediction = false;
            }
        }

        // ================================================================ interactions

        private void TouchStuff()
        {
            Vector3 lCenter = transform.position + PhysUp * 0.9f;
            for (int i = Interactable.All.Count - 1; i >= 0; i--)
            {
                if (i >= Interactable.All.Count) continue;
                Interactable lItem = Interactable.All[i];
                if (lItem.Test(lCenter) && lItem.Accepts(this)) lItem.Touch(this);
            }

            // Dashing, or just after a dash : smash. The reach is wider then, so a near miss still breaks it.
            bool lSmash = Dashing || _breakTimer > 0f || LightForm || Punching;
            float lReach = lSmash ? 7f : 1.5f;
            for (int i = Breakable.All.Count - 1; i >= 0; i--)
            {
                if (i >= Breakable.All.Count) continue;
                Breakable lBreakable = Breakable.All[i];
                if (!lBreakable.Overlaps(lCenter, lReach)) continue;
                if (lSmash) Burst(lBreakable);
                else BodyHit(lBreakable);
            }

            if (Punching) PunchHits();
        }

        // Dashing through it : it explodes in an impact frame and you lose nothing at all.
        private void Burst(Breakable pBreakable)
        {
            Vector3 lAt = pBreakable.transform.position;
            pBreakable.Smash(_heading, Speed);
            AddCombo();
            _dashEnergy = Mathf.Min(DASH_ENERGY, _dashEnergy + 0.4f);
            // Every break is a small speed pad.
            if (OnSurface) _speed = CapKick(_speed, _profile.cruise * BREAK_KICK * KickScale(0.3f));
            else _velocity = CapVelocity(_velocity, _velocity.normalized * _profile.cruise * BREAK_KICK * KickScale(0.3f));
            _boostTimer = Mathf.Max(_boostTimer, 0.6f);
            JuiceContext lJuice = Jx(lAt);
            lJuice.size = pBreakable.radius / 16f;
            // The break sounds like what broke : glass, ice, a door, a crate, rock.
            lJuice.label = pBreakable.Material;
            HSJuice.Play(JuiceId.BreakSmall, lJuice);
        }

        // Running into it without a dash : it still breaks, but it takes a good part of your speed.
        private void BodyHit(Breakable pBreakable)
        {
            pBreakable.Smash(_heading, Speed * 0.5f);
            if (Invulnerable || CheatInvincible) return;
            if (OnSurface) _speed = Mathf.Max(RunFloor, _speed * 0.75f);
            else _velocity *= 0.75f;
            _invulnerable = 0.3f;
            HSJuice.Play(JuiceId.BodyHit, Jx(pBreakable.transform.position));
        }

        // One punch : wide reach, it explodes and you pick up speed.
        private void PunchHits()
        {
            Vector3 lFist = transform.position + PhysUp + _heading * (6f + Speed * 0.1f);
            for (int i = Breakable.All.Count - 1; i >= 0; i--)
            {
                if (i >= Breakable.All.Count) continue;
                Breakable lBreakable = Breakable.All[i];
                if (!lBreakable.Overlaps(lFist, 12f)) continue;
                Vector3 lWhere = lBreakable.transform.position;
                Burst(lBreakable);
                if (!_punchHit)
                {
                    _punchHit = true;
                    HSJuice.Play(JuiceId.PunchHit, Jx(lWhere));
                    _visual?.PunchImpact(lWhere);
                }
            }
        }

        // ================================================================ movement

        private int GroundMask => Layers.GROUND;

        private void Move(Vector3 pDisplacement)
        {
            Vector3 lUp = State == PlayerState.Ground || State == PlayerState.Finished ? _up : _gravityUp;
            float lLift = State == PlayerState.Ground || State == PlayerState.Finished ? 0.5f : 0.1f;
            for (int lIteration = 0; lIteration < 3; lIteration++)
            {
                float lDistance = pDisplacement.magnitude;
                if (lDistance < 0.0001f) return;
                Vector3 lDir = pDisplacement / lDistance;
                Vector3 lBottom = transform.position + lUp * (lLift + RADIUS);
                Vector3 lTop = transform.position + lUp * (HEIGHT - RADIUS);
                RaycastHit lHit;
                if (!Physics.CapsuleCast(lBottom, lTop, RADIUS, lDir, out lHit, lDistance + SKIN, GroundMask, QueryTriggerInteraction.Ignore))
                {
                    transform.position += pDisplacement;
                    return;
                }
                float lTravel = Mathf.Max(0f, lHit.distance - SKIN);
                transform.position += lDir * lTravel;
                Vector3 lRemaining = lDir * (lDistance - lTravel);
                if (!OnHit(lHit, lDir)) return;
                lUp = State == PlayerState.Ground || State == PlayerState.Finished ? _up : _gravityUp;
                pDisplacement = Vector3.ProjectOnPlane(lRemaining, lHit.normal);
            }
        }

        // Returns true if movement should continue sliding along the surface.
        private bool OnHit(RaycastHit pHit, Vector3 pDir)
        {
            Vector3 lNormal = pHit.normal;
            switch (State)
            {
                case PlayerState.Ground:
                case PlayerState.Finished:
                    // A surface you can run onto : it simply becomes the new ground.
                    if (Vector3.Dot(lNormal, _up) >= GROUND_MIN_DOT) return true;
                    if (State == PlayerState.Ground && !_freeRoam && HitObstacle(pHit, pDir)) return false;
                    transform.position += lNormal * 0.02f;
                    return true;
                case PlayerState.Air:
                case PlayerState.Crash:
                    if (Vector3.Dot(lNormal, _gravityUp) > 0.12f)
                    {
                        Land(lNormal, pHit.point, pHit.collider);
                        return false;
                    }
                    if (State == PlayerState.Air && HitObstacle(pHit, pDir)) return false;
                    _velocity = Vector3.ProjectOnPlane(_velocity, lNormal);
                    return true;
            }
            return true;
        }

        // Straight into a face : it stops you, unless you are going through it. At an angle you glance off.
        private bool HitObstacle(RaycastHit pHit, Vector3 pDir)
        {
            // The lip of the next block, just above you : you get over it with your speed, a run is never lost to a step.
            if (pHit.collider.gameObject.layer == Layers.DECK && TryClimbLip(pDir)) return true;
            Monolith lBlock = pHit.collider.GetComponent<Monolith>();
            if (lBlock != null && (LightForm || (Dashing && _profile.worldBreaker)))
            {
                lBlock.Smash(_heading, Speed);
                JuiceContext lJuice = Jx(pHit.point);
                lJuice.size = 3f;
                HSJuice.Play(JuiceId.BreakBig, lJuice);
                AddCombo();
                return true;
            }
            float lHeadOn = -Vector3.Dot(pDir, pHit.normal);
            if (Autopilot.Enabled) Debug.Log("[HS-HIT] " + pHit.collider.name + " headOn=" + lHeadOn.ToString("F2") + " s=" + Progress.ToString("F0") + " v=" + Speed.ToString("F0"));
            if (lHeadOn > 0.72f && !Invulnerable)
            {
                Crash(pHit.point);
                return true;
            }
            float lLoss = 1f - lHeadOn * 0.35f;
            if (OnSurface)
            {
                _speed = Mathf.Max(RunFloor, _speed * lLoss);
                _heading = Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(_heading, pHit.normal), _up).normalized;
                _moveDir = _heading;
            }
            else _velocity = Vector3.ProjectOnPlane(_velocity, pHit.normal) * lLoss;
            JuiceContext lGlance = Jx(pHit.point);
            lGlance.normal = pHit.normal;
            HSJuice.Play(JuiceId.GlanceOff, lGlance);
            transform.position += pHit.normal * 0.05f;
            return false;
        }

        private bool TryClimbLip(Vector3 pDir)
        {
            const float REACH = 9f;
            Vector3 lUp = OnSurface ? _up : _gravityUp;
            Vector3 lVelocity = OnSurface ? _moveDir * _speed + Vector3.Cross(_up, _moveDir).normalized * _slip : _velocity;
            RaycastHit lHit;
            Vector3 lOrigin = transform.position + lUp * (REACH + 1f) + pDir * 2.5f;
            if (!Physics.Raycast(lOrigin, -lUp, out lHit, REACH + 1f, 1 << Layers.DECK, QueryTriggerInteraction.Ignore) || Vector3.Dot(lHit.normal, lUp) < 0.5f) return false;
            transform.position = lHit.point;
            Vector3 lTangent = Vector3.ProjectOnPlane(lVelocity, lHit.normal);
            _up = lHit.normal;
            _gravityUp = lHit.normal;
            _heading = lTangent.sqrMagnitude > 1f ? lTangent.normalized : Vector3.ProjectOnPlane(_heading, lHit.normal).normalized;
            _moveDir = _heading;
            _speed = Mathf.Max(lTangent.magnitude, RunFloor);
            _slip = 0f;
            Diving = false;
            SetState(_autoRun ? PlayerState.Finished : PlayerState.Ground);
            HasDoubleJump = true;
            FX.Puff(lHit.point, Color.white, 6, 3f, 6f);
            return true;
        }

        private void UpdateHyper(float pDt)
        {
            UpdateChain(pDt);
            UpdateHighwaySpeed(pDt);
            // Falling is not running : a long drop alone never turns you to light.
            float lSpeed = OnSurface ? _speed : Vector3.ProjectOnPlane(_velocity, _gravityUp).magnitude;
            // Hyper only exists from 2-4 on (2026-11-12).
            if (!Hyper && _profile.hyperEnabled && lSpeed >= _profile.hyperEnter && !_freeRoam)
            {
                Hyper = true;
                HSJuice.Play(JuiceId.HyperEnter, Jx(transform.position + PhysUp));
                Debug.Log("[HS-TIER] hyper entered s=" + Progress.ToString("F0") + " v=" + lSpeed.ToString("F0") + " chain=" + Chain);
                OnHyper?.Invoke();
            }
            else if (Hyper && lSpeed < _profile.hyperExit)
            {
                Hyper = false;
                HSJuice.Play(JuiceId.HyperExit, Jx(transform.position));
            }

            // Light : only where it exists (3-4 on), only once a long chain of pads has charged it, only in hyper.
            bool lCharged = _profile.LightEnabled && LightCharge >= 1f && Hyper;
            if (lCharged && !LightForm && !_freeRoam && !InputLocked)
            {
                // The charge is full : the light takes you, whatever the speed was.
                if (OnSurface) _speed = Mathf.Max(_speed, _profile.lightEnter);
                else _velocity = _velocity.normalized * Mathf.Max(_velocity.magnitude, _profile.lightEnter);
                lSpeed = Mathf.Max(lSpeed, _profile.lightEnter);
            }
            _overLight = lCharged && lSpeed >= _profile.lightEnter && !_freeRoam && !InputLocked ? _overLight + pDt : 0f;
            if (!LightForm && _overLight >= LIGHT_HOLD)
            {
                LightForm = true;
                HSJuice.Play(JuiceId.LightEnter, Jx(transform.position + PhysUp));
                OnLight?.Invoke();
                Debug.Log("[HS-FEEL] light entered at v=" + lSpeed.ToString("F0") + " cruise=" + _profile.cruise.ToString("F0") + " s=" + Progress.ToString("F0"));
            }
            else if (LightForm && (lSpeed < _profile.lightExit || LightCharge <= 0f))
            {
                LightForm = false;
                HSJuice.Play(JuiceId.LightExit, Jx(transform.position));
            }
            // The wind is the speed you hear : a low roar at a jog, a howl near light.
            // Three layers of wind : a low roar from the first steps, the body of the wind, a howl near light.
            float lWind = Mathf.Clamp01(Speed / Mathf.Max(1f, _profile.cruise));
            AudioManager.SetLoop("sfx_wind_low", Mathf.Clamp01(lWind * 0.7f) * 0.9f, 0.8f + Speed01 * 0.4f);
            AudioManager.SetLoop("sfx_wind_mid", Mathf.Clamp01(Speed01 * 1.2f) * 0.85f, 0.85f + Speed01 * 0.5f);
            AudioManager.SetLoop("sfx_wind_high", Mathf.Clamp01((Speed01 - 0.45f) * 1.8f) * 0.8f, 0.9f + Speed01 * 0.4f);
            AudioManager.SetLoop("sfx_light_loop", LightForm ? 0.7f : 0f, 1f + Speed01 * 0.2f);
            AudioManager.SetLoop("sfx_dive_loop", Diving ? 0.3f + Dive01 * 0.6f : 0f, 0.85f + Dive01 * 0.4f);
            if (Diving && !_wasDiving) HSJuice.Play(JuiceId.DiveStart, Jx(transform.position));
            _wasDiving = Diving;
        }

        // ================================================================ progress, death, respawn

        private void TrackProgress()
        {
            if (_layout == null) return;
            float lLateral;
            if (State == PlayerState.Grind && _rail != null)
            {
                Progress = _rail.SpineS(_railDistance);
                _layout.spine.ProjectNear(transform.position, Progress, out lLateral);
            }
            else
            {
                float lPrevious = Progress;
                float lNew = _layout.spine.Project(transform.position, Progress, out lLateral);
                // In the air the projection may hop to another part of the line : never further than the flight allows.
                if (!OnSurface) lNew = Mathf.Min(lNew, lPrevious + TotalSpeed * Time.deltaTime * 1.5f + 2f);
                Progress = lNew;
            }
            Lateral = lLateral;
            if (OnSurface) GroundProgress = Progress;

            if (_barrier)
            {
                // Held in the plaza until GO. Its back is closed too.
                float lLimit = _layout.startLineS - 3f;
                if (Progress > lLimit)
                {
                    Vector3 lForward = _layout.spine.Forward(Progress);
                    transform.position -= lForward * (Progress - lLimit);
                    _velocity -= lForward * Mathf.Max(0f, Vector3.Dot(_velocity, lForward));
                    Progress = lLimit;
                }
                float lBack = _layout.spine.StartS + 30f;
                if (Progress < lBack)
                {
                    transform.position += _layout.spine.Forward(Progress) * (lBack - Progress);
                    Progress = lBack;
                }
            }
        }

        private float _noGroundTime;
        private float _sceneryTime;
        private bool _wasDiving;

        // Speed you keep after a knock while you ask to run. Without a run asked, nothing holds you up.
        private float RunFloor => RunInput > 0.1f ? _profile.cruise * 0.55f : 0f;

        // Out of the stage is death, at once, and you come back at a safe spot behind : off the sides, under the floor,
        // on the scenery, falling with no track under the arc, or lost in the air of a loop. No throw-back, no bounce.
        private void CheckFallOut()
        {
            if (_layout == null || State == PlayerState.Finished || State == PlayerState.Dead || _autoRun || _freeRoam) return;
            float lDt = Time.deltaTime;
            string lWhy = null;

            float lSide = Mathf.Abs(Lateral - _layout.field.CenterLateral(Progress)) - _layout.field.TotalHalfWidth(Progress);
            if (lSide > OUT_SIDE_MARGIN) lWhy = "side";
            else if (UnderFloor() > OUT_UNDER) lWhy = "under";
            else if (_layout.IsLost(transform.position, Progress)) lWhy = "lost";

            _sceneryTime = OnSurface && _groundLayer == Layers.SCENERY ? _sceneryTime + lDt : 0f;
            if (lWhy == null && _sceneryTime > SCENERY_TIME) lWhy = "scenery";

            if (State == PlayerState.Air || State == PlayerState.Crash)
            {
                bool lFalling = Vector3.Dot(_velocity, _gravityUp) < 0f;
                _noGroundTime = !HasLandingPrediction && lFalling ? _noGroundTime + lDt : 0f;
                if (lWhy == null && _noGroundTime > FALL_TIME) lWhy = "fall";
            }
            else _noGroundTime = 0f;

            if (lWhy == null) return;
            if (Autopilot.Enabled) Debug.Log("[HS-LD] out (" + lWhy + ") s=" + Progress.ToString("F0") + " lat=" + Lateral.ToString("F0") + " pos=" + transform.position.ToString("F0") + " vel=" + _velocity.ToString("F0") + " state=" + State);
            Die();
        }

        // How far under the lowest floor around (just behind to well ahead) the runner is, along the floor's normal.
        private float UnderFloor()
        {
            float lAhead = Mathf.Max(200f, Speed * 1.5f);
            float lDeepest = float.MinValue;
            for (float lD = -50f; lD <= lAhead; lD += 50f)
            {
                float lS = Progress + lD;
                Vector3 lFloor = _layout.GroundPoint(lS, _layout.field.CenterLateral(lS));
                float lUnder = Vector3.Dot(lFloor - transform.position, _layout.field.Normal(lS));
                // The shallowest "under" wins : being above any floor around means you are not under the stage.
                if (lD == -50f || lUnder < lDeepest) lDeepest = lUnder;
            }
            return lDeepest;
        }

        // Heading against the track on the ground, for a moment : the HUD warns. Nothing pushes you around.
        private void UpdateWrongWay(float pDt)
        {
            bool lAgainst = _layout != null && OnSurface && !_freeRoam && !_autoRun && _speed > 15f && Vector3.Dot(_heading, _layout.Flow(Progress)) < -0.3f;
            _wrongWayTime = lAgainst ? _wrongWayTime + pDt : Mathf.Max(0f, _wrongWayTime - pDt * 2f);
            WrongWay = _wrongWayTime > WRONG_WAY_TIME;
        }

        public void Die()
        {
            if (State == PlayerState.Dead) return;
            if (CheatInvincible)
            {
                RespawnAt(_crumbs.PickSafe(Progress, Speed, _layout), false);
                return;
            }
            Diving = false;
            EndDrift(false);
            SetState(PlayerState.Dead);
            Combo = 0;
            HSJuice.Play(JuiceId.Death, Jx(transform.position));
            AudioManager.SetLoop("sfx_dive_loop", 0f);
            AudioManager.SetLoop("sfx_light_loop", 0f);
            OnDied?.Invoke();
        }

        public Breadcrumb PickRespawn() => _crumbs.PickSafe(Progress, Speed, _layout);

        // Context of a juice event happening to the runner : where, the way it runs, its up.
        private JuiceContext Jx(Vector3 pAt, float pIntensity = 1f) =>
            new JuiceContext { position = pAt, direction = _heading, normal = PhysUp, intensity = pIntensity, size = 1f };

        public Breadcrumbs Crumbs => _crumbs;

        // Called by the respawn director at the end of the rewind.
        public void RespawnAt(Breadcrumb pCrumb, bool pCount = true)
        {
            float lCenter = _layout.field.CenterLateral(pCrumb.s);
            float lHalf = _layout.field.HalfWidth(pCrumb.s) * 0.7f;
            float lLateral = Mathf.Clamp(pCrumb.lateral, lCenter - lHalf, lCenter + lHalf);
            Place(pCrumb.s, lLateral);
            // Back in the race at once if you are asking to run, standing still otherwise.
            _speed = HSInput.Run > 0.1f || Autopilot.Enabled ? Mathf.Max(pCrumb.speed * 0.8f, _profile.cruise * 0.85f) : 0f;
            _wrongWayTime = 0f;
            WrongWay = false;
            _sceneryTime = 0f;
            _noGroundTime = 0f;
            _velocity = _heading * _speed;
            _invulnerable = 1.5f;
            _airTime = 0f;
            HasDoubleJump = true;
            Combo = 0;
            if (pCount) Respawns++;
            SetState(_autoRun ? PlayerState.Finished : PlayerState.Ground);
            _crumbs.Clear();
            OnRespawned?.Invoke();
        }

        public void SkipToNextCheckpoint()
        {
            float lS = Mathf.Min(Progress + _profile.cruise * 10f, _layout.finishS - 10f);
            for (int i = 0; i < 80 && !_layout.IsSafe(lS, _layout.field.CenterLateral(lS)) && lS < _layout.finishS - 20f; i++) lS += _profile.cruise * 0.25f;
            RespawnAt(new Breadcrumb { s = lS, lateral = _layout.field.CenterLateral(lS), speed = _profile.cruise }, false);
            CameraRig.Instance?.Snap();
        }
    }
}
