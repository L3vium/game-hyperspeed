using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum PlayerState { Frozen, Ground, Air, WallRun, Grind, Zip, Crash, Finished }

    // Kinematic runner : surface-aligned gravity (loops, walls, ceilings), parkour moves, speed economy.
    public class PlayerController : MonoBehaviour, ICameraTarget
    {
        // ---------------- tuning
        public const float RUN_SPEED = 40f;
        private const float RUN_ACCEL = 28f;
        private const float MAX_SPEED = 175f;
        public const float HYPER_ENTER = 95f;
        private const float HYPER_EXIT = 84f;
        public const float SMASH_SPEED = 60f;
        private const float GRAVITY = 50f;
        private const float SLOPE_ACCEL = 34f;
        private const float JUMP_VELOCITY = 17f;
        private const float DOUBLE_JUMP_VELOCITY = 16f;
        private const float DASH_BOOST = 22f;
        private const float DASH_TIME = 0.22f;
        private const float DIVE_VELOCITY = 42f;
        private const float COYOTE_TIME = 0.13f;
        private const float JUMP_BUFFER = 0.14f;
        private const float RADIUS = 0.45f;
        private const float HEIGHT = 1.8f;
        private const float SKIN = 0.04f;
        private const float GROUND_MIN_DOT = 0.55f;
        private const float STICK_MIN_SPEED = 16f;
        private const float RAIL_RADIUS = 4.5f;
        private const float KILL_DISTANCE = 180f;
        private const float DRIFT_SLIP = 35f;
        private const float START_WINDOW_BEFORE = 0.25f;
        private const float START_WINDOW_AFTER = 0.12f;

        // ---------------- state
        public PlayerState State { get; private set; } = PlayerState.Frozen;
        public Vector3 Velocity => _velocity;
        public Vector3 Up => _up;
        public Vector3 GravityUp => _gravityUp;
        public Vector3 Heading => _heading;
        public float Speed => OnSurface ? _speed : Vector3.ProjectOnPlane(_velocity, _gravityUp).magnitude;
        public float TotalSpeed => OnSurface ? _speed : _velocity.magnitude;
        private bool OnSurface => State == PlayerState.Ground || State == PlayerState.Grind || State == PlayerState.Zip;
        public Vector3 FacingDirection => _heading;
        public float BoostAmount => Mathf.Clamp01(_boostTimer / 0.6f);
        public bool CheatInvincible;
        public bool Hyper { get; private set; }
        public bool Drifting { get; private set; }
        public int DriftDirection { get; private set; }
        public float DriftCharge { get; private set; }
        public int DriftTier => DriftCharge >= 2f ? 3 : DriftCharge >= 1.2f ? 2 : DriftCharge >= 0.5f ? 1 : 0;
        public bool Sliding { get; private set; }
        public bool Diving { get; private set; }
        public bool Dashing => _dashTimer > 0f;
        public bool Punching => _punchTimer > 0f;
        public bool HasDoubleJump { get; private set; } = true;
        public bool HasAirDash { get; private set; } = true;
        public float Steer { get; private set; }
        public int WallSide { get; private set; }
        public float StateTime { get; private set; }
        public int Combo { get; private set; }
        public int Respawns { get; private set; }
        public float TopSpeed { get; private set; }
        public bool Invulnerable => _invulnerable > 0f;
        public float Progress { get; private set; }
        public float DistanceFromPath { get; private set; }

        public event System.Action OnJumped;
        public event System.Action OnDoubleJumped;
        public event System.Action<float> OnLanded;
        public event System.Action OnPunched;
        public event System.Action OnDashed;
        public event System.Action OnCrashed;
        public event System.Action OnRespawned;

        private Vector3 _velocity;
        private Vector3 _up = Vector3.up;
        private Vector3 _gravityUp = Vector3.up;
        private Vector3 _heading = Vector3.forward;
        private float _speed;
        private float _lastGrounded;
        private float _jumpBufferedAt = -10f;
        private float _dashTimer;
        private float _dashCooldown;
        private float _punchTimer;
        private float _punchCooldown;
        private float _diveStart;
        private float _invulnerable;
        private float _crashTimer;
        private float _comboTimer;
        private float _wallCooldown;
        private Vector3 _wallNormal;
        private Vector3 _lastWallNormal;
        private Vector3 _wallRunDir;
        private float _wallSpeed;
        private float _wallVertical;
        private GrindRail _rail;
        private float _railDistance;
        private int _railDirection;
        private float _railCooldown;
        private int _pathIndex;
        private float _airTime;
        private int _ringCombo;
        private float _ringComboTimer;
        private Vector3 _smoothCameraUp = Vector3.up;
        private bool _autoRun;
        private bool _punchHit;
        private Vector3 _moveDir = Vector3.forward;
        private float _hangTimer;
        private float _boostTimer;
        private float _minTrackY;
        private float _goTime = -1f;
        private float _startPressTime = -1f;
        private float _stallTimer;

        private TrackPath _path;
        private PlayerVisual _visual;

        // ---------------- camera target
        public Vector3 FocusPosition => (_visual != null ? _visual.Root.position : transform.position) + CameraUpRaw * 0.9f;
        public Vector3 CameraUp => _smoothCameraUp;
        private Vector3 CameraUpRaw => State == PlayerState.Ground || State == PlayerState.Grind || State == PlayerState.Finished ? _up : _gravityUp;
        public Vector3 CameraForward
        {
            get
            {
                Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, CameraUpRaw);
                if (State == PlayerState.Ground || State == PlayerState.Finished) return _moveDir;
                if (OnSurface || lFlat.magnitude < 6f) return _heading;
                return lFlat.normalized;
            }
        }
        public float Speed01 => Mathf.Clamp01((Speed - 28f) / 95f);
        public float CameraRoll => -Steer * 5f - (Drifting ? DriftDirection * 9f : 0f) + (State == PlayerState.WallRun ? WallSide * 12f : 0f);
        public bool HyperMode => Hyper;

        public void Init(TrackPath pPath, PlayerVisual pVisual)
        {
            _path = pPath;
            _visual = pVisual;
            TrackSample lStart = pPath.Evaluate(8f);
            _minTrackY = float.MaxValue;
            foreach (TrackSample lSample in pPath.samples) _minTrackY = Mathf.Min(_minTrackY, lSample.position.y);
            Place(lStart);
            State = PlayerState.Frozen;
        }

        private void Place(TrackSample pSample)
        {
            transform.position = pSample.position + pSample.Up * 0.05f;
            _up = pSample.Up;
            _gravityUp = pSample.Up;
            _smoothCameraUp = _up;
            _heading = pSample.Forward;
            _moveDir = _heading;
            _velocity = Vector3.zero;
            _speed = 0f;
            _pathIndex = _path.FindNearestGlobal(transform.position);
            transform.rotation = Quaternion.LookRotation(_heading, _up);
        }

        // Rocket start : press jump / dash / forward right on GO.
        public void ArmStart(float pGoTime)
        {
            _goTime = pGoTime;
            _startPressTime = -1f;
        }

        public void StartRunning()
        {
            State = PlayerState.Ground;
            _speed = RUN_SPEED * 0.6f;
            float lDelta = _startPressTime - _goTime;
            if (_startPressTime > 0f && lDelta >= -START_WINDOW_BEFORE && lDelta <= START_WINDOW_AFTER) PerfectStart();
            else if (_startPressTime > 0f && lDelta < -START_WINDOW_BEFORE)
            {
                _stallTimer = 0.6f;
                _speed = 0f;
                Juice.Popup("TOO EARLY!", UIFactory.RED, 1.1f);
                AudioManager.Play("sfx_stall", 0.9f);
            }
        }

        private void PerfectStart()
        {
            _speed = RUN_SPEED + 35f;
            _boostTimer = 1.5f;
            Juice.Popup("PERFECT START!!", UIFactory.YELLOW, 1.5f);
            Juice.ImpactFrame(1f);
            Juice.Freeze(0.08f);
            Juice.Flash(Color.white, 0.6f);
            Juice.Rollback(10f);
            Juice.FovKick(20f);
            AudioManager.Play("sfx_perfect_start", 1f);
            AddCombo();
        }

        private bool StartInputPressed => HSInput.JumpPressed || HSInput.DashPressed || HSInput.ForwardPressed;

        public void Finish()
        {
            _autoRun = true;
            if (State == PlayerState.Ground) State = PlayerState.Finished;
        }

        // ================================================================ update

        private void Update()
        {
            float lDt = Time.deltaTime;
            if (lDt <= 0f) return;

            if (State == PlayerState.Frozen)
            {
                if (_goTime > 0f && _startPressTime < 0f && Time.time > _goTime - 1.6f && StartInputPressed) _startPressTime = Time.time;
                _visual?.Tick(lDt);
                return;
            }
            if (_startPressTime < 0f && _goTime > 0f && Time.time - _goTime <= START_WINDOW_AFTER && StartInputPressed)
            {
                _startPressTime = Time.time;
                PerfectStart();
            }
            if (_stallTimer > 0f)
            {
                _stallTimer -= lDt;
                _speed = 0f;
            }

            ReadInput(lDt);
            Timers(lDt);

            float lDistance = TotalSpeed * lDt;
            int lSubsteps = Mathf.Clamp(Mathf.CeilToInt(lDistance / 0.8f), 1, 12);
            float lSubDt = lDt / lSubsteps;
            for (int i = 0; i < lSubsteps; i++)
            {
                switch (State)
                {
                    case PlayerState.Ground:
                    case PlayerState.Finished: GroundStep(lSubDt); break;
                    case PlayerState.Air:
                    case PlayerState.Crash: AirStep(lSubDt); break;
                    case PlayerState.WallRun: WallStep(lSubDt); break;
                    case PlayerState.Grind:
                    case PlayerState.Zip: GrindStep(lSubDt); break;
                }
                TouchStuff();
            }

            UpdateHyper();
            TrackProgress();
            transform.rotation = Quaternion.LookRotation(_heading, OnSurface || State == PlayerState.Finished ? _up : _gravityUp);
            AudioManager.SetLoop("sfx_boost_loop", BoostAmount * 0.6f, 0.9f + BoostAmount * 0.3f);
            _smoothCameraUp = Vector3.Slerp(_smoothCameraUp, CameraUpRaw, 1f - Mathf.Exp(-10f * lDt)).normalized;
            TopSpeed = Mathf.Max(TopSpeed, Speed);
            _visual?.Tick(lDt);
        }

        private bool _jumpPressed, _dashPressed, _slidePressed, _slideHeld, _punchPressed;

        private void ReadInput(float pDt)
        {
            bool lCanAct = State != PlayerState.Crash && State != PlayerState.Finished && !_autoRun;
            Steer = lCanAct ? HSInput.Steer : 0f;
            _jumpPressed = lCanAct && HSInput.JumpPressed;
            _dashPressed = lCanAct && HSInput.DashPressed;
            _slidePressed = lCanAct && HSInput.SlidePressed;
            _slideHeld = lCanAct && HSInput.SlideHeld;
            _punchPressed = lCanAct && HSInput.PunchPressed;

            if (_jumpPressed) _jumpBufferedAt = Time.time;
            if (_punchPressed && _punchCooldown <= 0f) Punch();
            if (_dashPressed && _dashCooldown <= 0f) Dash();
            if (_jumpPressed) TryJump();
            if (_slidePressed && (State == PlayerState.Air) && !Diving) Dive();
        }

        private void Timers(float pDt)
        {
            StateTime += pDt;
            _dashTimer -= pDt;
            _dashCooldown -= pDt;
            _punchTimer -= pDt;
            _punchCooldown -= pDt;
            _invulnerable -= pDt;
            _wallCooldown -= pDt;
            _railCooldown -= pDt;
            _ringComboTimer -= pDt;
            _boostTimer -= pDt;
            _hangTimer -= pDt;
            if (_ringComboTimer <= 0f) _ringCombo = 0;
            _comboTimer -= pDt;
            if (_comboTimer <= 0f) Combo = 0;

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
            if (State == PlayerState.Zip) AudioManager.SetLoop("sfx_zip_loop", 0f);
            if (State == PlayerState.WallRun) AudioManager.SetLoop("sfx_wallrun_loop", 0f);
            State = pState;
            StateTime = 0f;
            if (pState != PlayerState.Ground && pState != PlayerState.Finished) EndDrift(false);
            if (pState == PlayerState.Ground || pState == PlayerState.Finished) _airTime = 0f;
        }

        // ================================================================ ground

        private void GroundStep(float pDt)
        {
            _lastGrounded = Time.time;
            _heading = Vector3.ProjectOnPlane(_heading, _up).normalized;

            // Steering & drift
            float lSpeed01 = Mathf.Clamp01(_speed / 120f);
            float lTurnRate = Mathf.Lerp(150f, 100f, lSpeed01);
            float lSteer = Steer;
            UpdateDrift(pDt);
            if (Drifting)
            {
                lSteer = DriftDirection * 0.55f + Steer * 0.65f;
                lTurnRate *= 1.55f;
            }
            _heading = Quaternion.AngleAxis(lSteer * lTurnRate * pDt, _up) * _heading;

            // Gentle steering assist toward the track direction when not steering.
            if (Mathf.Abs(Steer) < 0.1f && !Drifting && _path != null && State != PlayerState.Finished)
            {
                TrackSample lSample = _path.samples[Mathf.Clamp(_pathIndex + 3, 0, _path.samples.Count - 1)];
                Vector3 lTangent = Vector3.ProjectOnPlane(lSample.Forward, _up);
                if (lTangent.sqrMagnitude > 0.1f && Vector3.Dot(lTangent.normalized, _heading) > 0.5f)
                    _heading = Vector3.RotateTowards(_heading, lTangent.normalized, 0.9f * pDt, 0f);
            }
            if (State == PlayerState.Finished && _path != null)
            {
                TrackSample lSample = _path.samples[Mathf.Clamp(_pathIndex + 3, 0, _path.samples.Count - 1)];
                _heading = Vector3.RotateTowards(_heading, Vector3.ProjectOnPlane(lSample.Forward, _up).normalized, 2f * pDt, 0f);
            }

            // Speed economy
            Sliding = _slideHeld && !Drifting;
            float lSlope = Vector3.Dot(_heading, Vector3.down);
            // Loop assist : on steep or inverted surfaces climbing costs less and speed never drops too low.
            bool lSteep = Vector3.Dot(_up, Vector3.up) < 0.5f;
            float lSlopeScale = lSteep && lSlope < 0f ? 0.3f : 1f;
            _speed += lSlope * SLOPE_ACCEL * lSlopeScale * (Sliding ? 1.8f : 1f) * pDt;
            if (lSteep) _speed = Mathf.Max(_speed, 34f);
            float lForward = HSInput.Forward;
            float lTargetRun = RUN_SPEED * (1f + 0.12f * Mathf.Max(0f, lForward));
            if (State == PlayerState.Finished) lTargetRun = Progress > _path.Length - 90f ? 0f : 26f;
            if (_speed < lTargetRun) _speed = Mathf.MoveTowards(_speed, lTargetRun, RUN_ACCEL * (Sliding ? 0.3f : 1f) * pDt);
            else
            {
                float lDecay = ((_speed - lTargetRun) * 0.2f + 2.5f) * (Sliding ? 0.4f : 1f) * (lSlope > 0.1f ? 0.3f : 1f);
                _speed = Mathf.Max(lTargetRun, _speed - lDecay * pDt);
            }
            if (lForward < -0.5f && State != PlayerState.Finished) _speed = Mathf.Max(15f, _speed - 30f * pDt);
            if (State == PlayerState.Finished) _speed = Mathf.MoveTowards(_speed, lTargetRun, 30f * pDt);
            _speed = Mathf.Min(_speed, MAX_SPEED);

            // Drift slip : the velocity direction lags behind the heading.
            _moveDir = Vector3.ProjectOnPlane(_moveDir, _up).normalized;
            if (Drifting)
            {
                _moveDir = Vector3.RotateTowards(_moveDir, _heading, 1.6f * pDt, 0f);
                if (Vector3.Angle(_moveDir, _heading) > DRIFT_SLIP) _moveDir = Vector3.RotateTowards(_heading, _moveDir, DRIFT_SLIP * Mathf.Deg2Rad, 0f);
            }
            else _moveDir = Vector3.RotateTowards(_moveDir, _heading, 9f * pDt, 0f);
            _velocity = _moveDir * _speed;
            Move(_velocity * pDt);
            if (State != PlayerState.Ground && State != PlayerState.Finished) return;

            // Crests of climbing roads launch you instead of gluing you down.
            float lRise = Vector3.Dot(_moveDir, Vector3.up);
            float lSnap = lRise > 0.12f ? 0.15f : 0.6f + _speed * 0.006f;
            if (!Snap(lSnap))
            {
                // Ran off an edge : keep the upward momentum of the road (ramp pop + hang time).
                _gravityUp = _up;
                if (lRise > 0.08f)
                {
                    _velocity += Vector3.up * Mathf.Min(_speed * lRise * 0.35f, 14f);
                    _hangTimer = 0.4f;
                    if (_speed > 50f) Juice.Popup("AIR!", Color.white, 0.8f);
                }
                SetState(PlayerState.Air);
                return;
            }

            // Too slow to stick on a ceiling / steep wall : peel off.
            if (Vector3.Dot(_up, Vector3.up) < 0.25f && _speed < STICK_MIN_SPEED)
            {
                _gravityUp = _up;
                _velocity += _up * 4f;
                SetState(PlayerState.Air);
                return;
            }

            if (Time.time - _jumpBufferedAt < JUMP_BUFFER && State == PlayerState.Ground) TryJump();
        }

        private void UpdateDrift(float pDt)
        {
            if (State != PlayerState.Ground)
            {
                EndDrift(false);
                return;
            }
            if (!Drifting && _slideHeld && Mathf.Abs(Steer) > 0.35f && _speed > 20f)
            {
                Drifting = true;
                DriftDirection = Steer > 0f ? 1 : -1;
                DriftCharge = 0f;
                AudioManager.Play("sfx_drift_start", 0.7f);
            }
            if (!Drifting) return;
            if (!_slideHeld)
            {
                EndDrift(true);
                return;
            }
            int lTierBefore = DriftTier;
            DriftCharge += pDt * (0.55f + Mathf.Abs(Steer) * 0.65f);
            if (DriftTier > lTierBefore)
            {
                AudioManager.Play("sfx_drift_tier", 0.6f, 0.9f + DriftTier * 0.15f, 0f);
                Juice.Pulse(0.25f);
            }
            AudioManager.SetLoop("sfx_drift_loop", 0.45f, 0.9f + DriftTier * 0.12f);
        }

        private void EndDrift(bool pRelease)
        {
            if (!Drifting) return;
            Drifting = false;
            AudioManager.SetLoop("sfx_drift_loop", 0f);
            if (!pRelease) { DriftCharge = 0f; return; }
            int lTier = DriftTier;
            DriftCharge = 0f;
            if (lTier == 0) return;
            float[] lBoosts = { 0f, 11f, 19f, 30f };
            string[] lNames = { "", "DRIFT!", "SUPER DRIFT!", "ULTRA DRIFT!!" };
            ApplyBoost(lBoosts[lTier], _heading, 0f, lNames[lTier]);
        }

        private bool Snap(float pDistance)
        {
            RaycastHit lHit;
            Vector3 lOrigin = transform.position + _up * 1.1f;
            if (!Physics.Raycast(lOrigin, -_up, out lHit, 1.1f + pDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!Physics.SphereCast(lOrigin, 0.3f, -_up, out lHit, 0.8f + pDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    return false;
            }
            if (Vector3.Dot(lHit.normal, _up) < GROUND_MIN_DOT) return false;
            transform.position = lHit.point;
            _up = lHit.normal;
            _gravityUp = _up;
            return true;
        }

        // ================================================================ air

        private void AirStep(float pDt)
        {
            _airTime += pDt;
            _gravityUp = Vector3.Slerp(_gravityUp, Vector3.up, 1f - Mathf.Exp(-2.4f * pDt)).normalized;

            float lUpVelocity = Vector3.Dot(_velocity, _gravityUp);
            float lGravity = GRAVITY;
            if (lUpVelocity < 0f) lGravity *= 1.15f;
            if (_hangTimer > 0f) lGravity *= 0.55f;
            else if (HSInput.JumpHeld && State == PlayerState.Air) lGravity *= 0.72f;
            if (Diving) lGravity *= 2.2f;
            if (Dashing) lGravity *= 0.15f;
            _velocity -= _gravityUp * lGravity * pDt;

            // Air control
            Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
            Vector3 lVertical = _velocity - lFlat;
            if (State == PlayerState.Air)
                lFlat = Quaternion.AngleAxis(Steer * 95f * pDt, _gravityUp) * lFlat;
            _velocity = lFlat + lVertical;
            if (State == PlayerState.Air) WallMagnet(pDt);
            if (lFlat.sqrMagnitude > 1f) _heading = lFlat.normalized;
            else _heading = Vector3.ProjectOnPlane(_heading, _gravityUp).normalized;

            Move(_velocity * pDt);
            if (State != PlayerState.Air && State != PlayerState.Crash) return;

            // Ground probe while falling.
            if (Vector3.Dot(_velocity, _gravityUp) <= 0f)
            {
                RaycastHit lHit;
                float lReach = 0.25f + Mathf.Max(0f, -Vector3.Dot(_velocity, _gravityUp)) * pDt;
                if (Physics.Raycast(transform.position + _gravityUp * 0.6f, -_gravityUp, out lHit, 0.6f + lReach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    && Vector3.Dot(lHit.normal, _gravityUp) > 0.45f)
                {
                    Land(lHit.normal, lHit.point);
                    return;
                }
            }

            if (State == PlayerState.Air)
            {
                TryWallRun();
                if (State == PlayerState.Air) TryGrind();
            }
        }

        private void Land(Vector3 pNormal, Vector3 pPoint)
        {
            float lImpact = -Vector3.Dot(_velocity, pNormal);
            Vector3 lTangent = Vector3.ProjectOnPlane(_velocity, pNormal);
            _up = pNormal;
            _gravityUp = pNormal;
            transform.position = pPoint;
            _speed = lTangent.magnitude;
            if (_speed > 1f) _heading = lTangent.normalized;
            else _heading = Vector3.ProjectOnPlane(_heading, pNormal).normalized;

            bool lWasCrash = State == PlayerState.Crash;
            SetState(_autoRun ? PlayerState.Finished : PlayerState.Ground);
            HasDoubleJump = true;
            HasAirDash = true;

            if (Diving)
            {
                float lTiming = Time.time - _diveStart;
                float lSlope = Vector3.Dot(_heading, Vector3.down);
                float lBonus;
                string lLabel;
                Color lColor;
                if (lTiming < 0.28f) { lBonus = 20f; lLabel = "PERFECT!"; lColor = UIFactory.YELLOW; }
                else if (lTiming < 0.6f) { lBonus = 12f; lLabel = "GREAT!"; lColor = UIFactory.CYAN; }
                else { lBonus = 6f; lLabel = "GOOD"; lColor = Color.white; }
                lBonus += Mathf.Min(lImpact, 60f) * 0.3f;
                if (lSlope > 0.12f) lBonus *= 1.4f;
                _speed = Mathf.Min(MAX_SPEED, _speed + lBonus);
                Diving = false;
                bool lPerfect = lTiming < 0.28f;
                _boostTimer = Mathf.Max(_boostTimer, lPerfect ? 1f : 0.5f);
                Juice.Popup(lLabel, lColor, lPerfect ? 1.4f : 1.1f);
                Juice.FovKick(lPerfect ? 22f : 12f);
                Juice.Pulse(lPerfect ? 1f : 0.5f);
                Juice.Rollback(lPerfect ? 12f : 6f);
                Juice.Shake(lPerfect ? 0.45f : 0.3f);
                if (lPerfect)
                {
                    Juice.Flash(Color.white, 0.4f);
                    Juice.ImpactFrame(1f);
                    Juice.Freeze(0.06f);
                }
                FX.Shockwave(pPoint + pNormal * 0.1f, pNormal, lColor, 14f);
                FX.Sparks(pPoint, lColor, 30, 22f);
                AudioManager.Play(lTiming < 0.28f ? "sfx_land_perfect" : "sfx_land_good", 0.9f);
                AddCombo();
            }
            else if (lImpact > 30f && !lWasCrash)
            {
                _speed *= 0.93f;
                Juice.Shake(0.25f);
                FX.Puff(pPoint, Color.white, 8, 2f, 6f);
                AudioManager.Play("sfx_land_heavy", 0.8f);
            }
            else
            {
                FX.Puff(pPoint, Color.white, 4, 1.2f, 3f);
                AudioManager.Play("sfx_land", 0.6f);
            }
            OnLanded?.Invoke(lImpact);
            Snap(0.5f);
        }

        // ================================================================ wall run

        private void TryWallRun()
        {
            if (_wallCooldown > 0f || Speed < 14f || Vector3.Dot(_velocity, _gravityUp) > 14f) return;
            Vector3 lCenter = transform.position + _gravityUp * 1f;
            Vector3 lRight = Vector3.Cross(_gravityUp, _heading).normalized;
            for (int lSide = -1; lSide <= 1; lSide += 2)
            {
                RaycastHit lHit;
                if (!Physics.Raycast(lCenter, lRight * lSide, out lHit, 2.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                if (Mathf.Abs(Vector3.Dot(lHit.normal, _gravityUp)) > 0.35f) continue;
                if (Vector3.Dot(lHit.normal, _lastWallNormal) > 0.9f && _wallCooldown > -0.6f) continue;
                Vector3 lDir = Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(_velocity, lHit.normal), _gravityUp);
                if (lDir.sqrMagnitude < 25f) continue;

                _wallNormal = lHit.normal;
                _wallRunDir = lDir.normalized;
                _wallSpeed = Mathf.Max(lDir.magnitude, 28f);
                _wallVertical = Mathf.Max(Vector3.Dot(_velocity, _gravityUp), 0f) * 0.35f + 3f;
                WallSide = lSide;
                SetState(PlayerState.WallRun);
                Diving = false;
                HasDoubleJump = true;
                HasAirDash = true;
                AudioManager.Play("sfx_wall_attach", 0.7f);
                FX.Sparks(lHit.point, Color.white, 12, 10f);
                return;
            }
        }

        // Over a floorless stretch, gently pull toward the closest wall so wall runs are easy to catch.
        private void WallMagnet(float pDt)
        {
            if (_path == null || _path.samples[_pathIndex].solid || _wallCooldown > 0f) return;
            Vector3 lRight = Vector3.Cross(_gravityUp, _heading).normalized;
            Vector3 lCenter = transform.position + _gravityUp;
            float lBest = 7f;
            int lSide = 0;
            for (int i = -1; i <= 1; i += 2)
            {
                RaycastHit lHit;
                if (Physics.Raycast(lCenter, lRight * i, out lHit, 7f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    && Mathf.Abs(Vector3.Dot(lHit.normal, _gravityUp)) < 0.35f && lHit.distance < lBest)
                {
                    lBest = lHit.distance;
                    lSide = i;
                }
            }
            if (lSide == 0 || lBest < 0.9f) return;
            if (Steer * lSide < -0.3f) return;
            _velocity += lRight * lSide * 26f * pDt;
        }

        private void WallStep(float pDt)
        {
            float lGravity = StateTime > 1.6f ? 34f : 7f;
            _wallVertical -= lGravity * pDt;
            _wallSpeed = Mathf.Min(MAX_SPEED, _wallSpeed + 3f * pDt);
            _velocity = _wallRunDir * _wallSpeed + _gravityUp * _wallVertical;
            _heading = _wallRunDir;
            Move(_velocity * pDt);
            if (State != PlayerState.WallRun) return;

            RaycastHit lHit;
            Vector3 lCenter = transform.position + _gravityUp * 1f;
            if (!Physics.Raycast(lCenter, -_wallNormal, out lHit, 1.8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                || Mathf.Abs(Vector3.Dot(lHit.normal, _gravityUp)) > 0.4f || StateTime > 2.4f)
            {
                LeaveWall(0.15f);
                return;
            }
            _wallNormal = lHit.normal;
            _wallRunDir = Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(_wallRunDir, _wallNormal), _gravityUp).normalized;
            transform.position = lHit.point + _wallNormal * 0.55f - _gravityUp * 1f;
            AudioManager.SetLoop("sfx_wallrun_loop", 0.5f, 1f + _wallSpeed * 0.004f);
            if (Random.value < 0.4f) FX.Sparks(lHit.point, Color.white, 1, 6f, 0.25f, 0.2f);

            if (Physics.Raycast(transform.position + _gravityUp * 0.5f, -_gravityUp, out lHit, 0.7f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && Vector3.Dot(lHit.normal, _gravityUp) > 0.5f)
                Land(lHit.normal, lHit.point);
        }

        private void LeaveWall(float pCooldown)
        {
            _lastWallNormal = _wallNormal;
            _wallCooldown = pCooldown;
            SetState(PlayerState.Air);
        }

        // ================================================================ grind

        private void TryGrind()
        {
            if (_railCooldown > 0f) return;
            foreach (GrindRail lRail in GrindRail.All)
            {
                // Rails are caught from above, zip lines from below (hands reach up to the rope).
                Vector3 lProbe = transform.position + _gravityUp * (lRail.zip ? 2.2f : 0.3f);
                float lDistance, lRailDistance;
                if (!lRail.FindNearest(lProbe, RAIL_RADIUS, out lDistance, out lRailDistance)) continue;
                Vector3 lPos, lTangent, lUp;
                lRail.Evaluate(lRailDistance, out lPos, out lTangent, out lUp);
                float lAlong = Vector3.Dot(_velocity, lTangent);
                if (Mathf.Abs(lAlong) < 8f) continue;
                if (!lRail.zip && Vector3.Dot(transform.position - lPos, lUp) < -0.8f) continue;
                if (lRail.zip)
                {
                    AttachZip(lRail, lRailDistance, lAlong > 0f ? 1 : -1);
                    return;
                }

                _rail = lRail;
                _railDistance = lRailDistance;
                _railDirection = lAlong > 0f ? 1 : -1;
                _speed = Mathf.Max(Speed, 32f);
                SetState(PlayerState.Grind);
                Diving = false;
                HasDoubleJump = true;
                HasAirDash = true;
                AudioManager.Play("sfx_grind_start", 0.8f);
                FX.Sparks(lPos, UIFactory.YELLOW, 25, 18f);
                Juice.Shake(0.15f);
                Juice.Popup("GRIND!", UIFactory.CYAN, 1f);
                AddCombo();
                return;
            }
        }

        private void AttachZip(GrindRail pRail, float pDistance, int pDirection)
        {
            _rail = pRail;
            _railDistance = pDistance;
            _railDirection = pDirection;
            _speed = Mathf.Max(Speed, 30f);
            SetState(PlayerState.Zip);
            Diving = false;
            HasDoubleJump = true;
            HasAirDash = true;
            AudioManager.Play("sfx_zip_start", 0.9f);
            Juice.Shake(0.2f);
            Juice.Popup("ZIP LINE!", UIFactory.CYAN, 1f);
            AddCombo();
        }

        private void GrindStep(float pDt)
        {
            bool lZip = State == PlayerState.Zip;
            Vector3 lPos, lTangent, lUp;
            _rail.Evaluate(_railDistance, out lPos, out lTangent, out lUp);
            float lDownhill = Vector3.Dot(lTangent * _railDirection, Vector3.down);
            _speed = Mathf.Min(MAX_SPEED, _speed + (lZip ? 5f + lDownhill * 45f : 6f) * pDt);
            if (lZip) _speed = Mathf.Max(_speed, 28f);
            _railDistance += _railDirection * _speed * pDt;
            _rail.Evaluate(_railDistance, out lPos, out lTangent, out lUp);
            _up = lZip ? Vector3.up : lUp;
            _gravityUp = _up;
            _heading = lTangent * _railDirection;
            _velocity = _heading * _speed;
            transform.position = lZip ? lPos - Vector3.up * 2.15f : lPos + lUp * 0.25f;
            if (lZip) AudioManager.SetLoop("sfx_zip_loop", 0.6f, 0.8f + _speed * 0.005f);
            else AudioManager.SetLoop("sfx_grind_loop", 0.55f, 0.9f + _speed * 0.004f);
            if (Random.value < 0.6f) FX.Sparks(lPos, Color.Lerp(UIFactory.YELLOW, Color.white, Random.value), 2, 12f, 0.3f, 0.25f);

            if (_railDistance <= 0f || _railDistance >= _rail.Length)
            {
                _velocity = _heading * (_speed + 5f) + _up * 7f;
                _railCooldown = 0.4f;
                SetState(PlayerState.Air);
                Juice.Popup("RAIL BOOST", UIFactory.CYAN, 1f);
                AudioManager.Play("sfx_boost_small", 0.7f);
            }
        }

        // ================================================================ actions

        private void TryJump()
        {
            bool lCoyote = State == PlayerState.Air && Time.time - _lastGrounded < COYOTE_TIME && Vector3.Dot(_velocity, _gravityUp) <= 1f;
            if (State == PlayerState.Ground || lCoyote)
            {
                _velocity = _heading * _speed + _up * JUMP_VELOCITY;
                _gravityUp = _up;
                _jumpBufferedAt = -10f;
                _lastGrounded = -10f;
                if (Drifting) EndDrift(true);
                SetState(PlayerState.Air);
                AudioManager.Play("sfx_jump", 0.8f);
                FX.Puff(transform.position, Color.white, 5, 1.4f, 4f);
                OnJumped?.Invoke();
            }
            else if (State == PlayerState.WallRun)
            {
                bool lGood = StateTime > 0.1f && StateTime < 0.75f;
                float lBonus = lGood ? 8f : 3f;
                _velocity = _wallRunDir * (_wallSpeed + lBonus) + _wallNormal * 14f + _gravityUp * 16f;
                _jumpBufferedAt = -10f;
                LeaveWall(0.25f);
                HasDoubleJump = true;
                HasAirDash = true;
                AudioManager.Play("sfx_walljump", 0.9f);
                FX.Sparks(transform.position + _gravityUp, Color.white, 16, 14f);
                Juice.Popup(lGood ? "WALL JUMP!" : "wall jump", lGood ? UIFactory.CYAN : Color.white, lGood ? 1.1f : 0.8f);
                if (lGood) { Juice.FovKick(7f); AddCombo(); }
                OnJumped?.Invoke();
            }
            else if (State == PlayerState.Grind || State == PlayerState.Zip)
            {
                _velocity = _heading * (_speed + 4f) + _up * JUMP_VELOCITY;
                _railCooldown = 0.35f;
                _jumpBufferedAt = -10f;
                SetState(PlayerState.Air);
                AudioManager.Play("sfx_jump", 0.8f, 1.1f);
                OnJumped?.Invoke();
            }
            else if (State == PlayerState.Air && HasDoubleJump)
            {
                HasDoubleJump = false;
                Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
                _velocity = lFlat + lFlat.normalized * 3f + _gravityUp * DOUBLE_JUMP_VELOCITY;
                Diving = false;
                _jumpBufferedAt = -10f;
                AudioManager.Play("sfx_doublejump", 0.85f);
                FX.Shockwave(transform.position, _gravityUp, Color.white, 5f, 0.25f);
                OnDoubleJumped?.Invoke();
            }
        }

        private void Dash()
        {
            if (State == PlayerState.Crash || State == PlayerState.Frozen) return;
            bool lAir = State == PlayerState.Air;
            if (lAir && !HasAirDash) return;
            if (lAir) HasAirDash = false;
            _dashTimer = DASH_TIME;
            _dashCooldown = 0.55f;

            // Dashing helps but never pushes you into hyper speed on its own.
            float lCap = HYPER_ENTER - 8f;
            if (State == PlayerState.Ground || State == PlayerState.Grind) _speed = Mathf.Max(_speed, Mathf.Min(lCap, _speed + DASH_BOOST));
            else if (State == PlayerState.WallRun) _wallSpeed = Mathf.Max(_wallSpeed, Mathf.Min(lCap, _wallSpeed + DASH_BOOST));
            else
            {
                Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
                Vector3 lDir = lFlat.sqrMagnitude > 1f ? lFlat.normalized : _heading;
                float lNew = Mathf.Max(lFlat.magnitude, Mathf.Min(lCap, lFlat.magnitude + DASH_BOOST));
                _velocity = lDir * lNew + _gravityUp * Mathf.Max(2f, Vector3.Dot(_velocity, _gravityUp) * 0.2f);
                Diving = false;
            }
            AudioManager.Play("sfx_dash", 0.85f);
            Juice.FovKick(14f);
            Juice.Rollback(7f);
            Juice.Pulse(0.6f);
            Juice.Shake(0.15f);
            FX.Sparks(transform.position + CameraUpRaw, Color.white, 14, 16f);
            OnDashed?.Invoke();
        }

        private void Dive()
        {
            Diving = true;
            _diveStart = Time.time;
            Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
            float lUp = Mathf.Min(Vector3.Dot(_velocity, _gravityUp), -DIVE_VELOCITY);
            _velocity = lFlat + _gravityUp * lUp;
            AudioManager.Play("sfx_dive", 0.8f);
            Juice.FovKick(5f);
        }

        private void Punch()
        {
            _punchTimer = 0.24f;
            _punchCooldown = 0.32f;
            _punchHit = false;
            AudioManager.Play("sfx_punch_whoosh", 0.7f);
            OnPunched?.Invoke();
        }

        public void Launch(Vector3 pVelocity, float pForwardBoost)
        {
            Vector3 lFlat = State == PlayerState.Ground ? _heading * _speed : Vector3.ProjectOnPlane(_velocity, pVelocity.normalized);
            _velocity = lFlat + lFlat.normalized * pForwardBoost + pVelocity;
            _gravityUp = Vector3.up;
            SetState(PlayerState.Air);
            HasDoubleJump = true;
            HasAirDash = true;
            AudioManager.Play("sfx_launch", 0.9f);
            Juice.Popup("LAUNCH!", UIFactory.YELLOW, 1.1f);
            Juice.FovKick(10f);
            Juice.Shake(0.2f);
        }

        public void ApplyBoost(float pBoost, Vector3 pDirection, float pAlign, string pLabel)
        {
            if (State == PlayerState.Ground || State == PlayerState.Finished)
            {
                Vector3 lDir = Vector3.ProjectOnPlane(pDirection, _up).normalized;
                if (pAlign > 0f && lDir.sqrMagnitude > 0.1f) _heading = Vector3.Slerp(_heading, lDir, pAlign).normalized;
                _speed = Mathf.Min(MAX_SPEED, _speed + pBoost);
            }
            else if (State == PlayerState.Grind || State == PlayerState.Zip) _speed = Mathf.Min(MAX_SPEED, _speed + pBoost);
            else if (State == PlayerState.WallRun) _wallSpeed += pBoost;
            else _velocity += pDirection.normalized * pBoost;

            _boostTimer = Mathf.Max(_boostTimer, 1.2f);
            AudioManager.Play("sfx_boost", 0.95f);
            Juice.FovKick(20f);
            Juice.Rollback(11f);
            Juice.Pulse(1f);
            Juice.Shake(0.3f);
            Juice.Flash(new Color(0.6f, 1f, 1f), 0.25f);
            Juice.Vignette(UIFactory.CYAN, 0.8f);
            Juice.Freeze(0.035f);
            Juice.ImpactFrame(0.6f);
            if (!string.IsNullOrEmpty(pLabel)) Juice.Popup(pLabel, UIFactory.CYAN, 1.1f);
            FX.Sparks(transform.position + CameraUpRaw * 0.5f, UIFactory.CYAN, 40, 28f, 0.5f, 0.4f);
            FX.Burst(transform.position + CameraUpRaw, _heading, UIFactory.CYAN, 18f);
            AddCombo();
        }

        public void ApplyRing(float pBoost, Vector3 pDirection)
        {
            float lSpeed = Mathf.Max(Speed, RUN_SPEED);
            if (State == PlayerState.Ground) SetState(PlayerState.Air);
            _velocity = pDirection.normalized * Mathf.Min(MAX_SPEED, lSpeed + pBoost) + _gravityUp * 3f;
            HasDoubleJump = true;
            HasAirDash = true;
            Diving = false;
            _ringCombo++;
            _ringComboTimer = 2.5f;
            _boostTimer = Mathf.Max(_boostTimer, 0.7f);
            float[] lScale = { 1f, 1.122f, 1.26f, 1.335f, 1.498f, 1.682f, 1.888f, 2f };
            AudioManager.Play("sfx_ring", 0.95f, lScale[Mathf.Min(_ringCombo - 1, lScale.Length - 1)], 0f);
            Juice.Popup(_ringCombo > 1 ? "RING x" + _ringCombo : "RING!", UIFactory.YELLOW, 1f + Mathf.Min(_ringCombo, 5) * 0.08f);
            Juice.FovKick(12f);
            Juice.Rollback(6f);
            Juice.Pulse(0.7f);
            Juice.Flash(new Color(1f, 0.95f, 0.6f), 0.2f);
            Juice.Freeze(0.03f);
            if (_ringCombo % 3 == 0) Juice.ImpactFrame(0.8f);
            FX.Burst(transform.position + _gravityUp, pDirection, UIFactory.YELLOW, 16f);
            AddCombo();
        }

        public void Crash(Vector3 pPoint, string pLabel = "OUCH!")
        {
            if (Invulnerable || CheatInvincible || State == PlayerState.Crash || State == PlayerState.Frozen || State == PlayerState.Finished) return;
            Vector3 lAway = transform.position - pPoint;
            lAway = Vector3.ProjectOnPlane(lAway, _gravityUp).normalized;
            float lSpeed = Mathf.Max(Speed * 0.5f, 18f);
            if (State == PlayerState.Ground || State == PlayerState.Grind) _gravityUp = _up;
            _velocity = _heading * lSpeed + lAway * 5f + _gravityUp * 11f;
            _crashTimer = 0.55f;
            _invulnerable = 1.4f;
            Diving = false;
            SetState(PlayerState.Crash);
            Combo = 0;
            AudioManager.Play("sfx_crash", 1f);
            Juice.Shake(0.8f);
            Juice.Flash(UIFactory.RED, 0.45f);
            Juice.Hitstop(0.1f);
            Juice.ImpactFrame(0.7f);
            Juice.Popup(pLabel, UIFactory.RED, 1.3f);
            FX.Sparks(pPoint, Color.white, 20, 15f);
            OnCrashed?.Invoke();
        }

        private void AddCombo()
        {
            Combo++;
            _comboTimer = 3f;
        }

        // ================================================================ interactions

        private void TouchStuff()
        {
            Vector3 lCenter = transform.position + CameraUpRaw * 0.9f;
            for (int i = Interactable.All.Count - 1; i >= 0; i--)
            {
                Interactable lItem = Interactable.All[i];
                if (lItem.Test(lCenter)) lItem.Touch(this);
            }

            bool lSmash = Speed >= SMASH_SPEED || Dashing || Hyper || Punching;
            for (int i = Breakable.All.Count - 1; i >= 0; i--)
            {
                Breakable lBreakable = Breakable.All[i];
                if (i >= Breakable.All.Count) continue;
                if (!lBreakable.Overlaps(lCenter, 0.7f)) continue;
                if (lSmash)
                {
                    lBreakable.Smash(_heading, Speed);
                    Juice.Hitstop(0.018f, 0.1f);
                    if (Combo % 8 == 7) Juice.ImpactFrame(0.5f);
                    Juice.Shake(0.18f);
                    AddCombo();
                    if (State == PlayerState.Ground) _speed = Mathf.Min(MAX_SPEED, _speed + 1.5f);
                    if (Combo % 5 == 0) Juice.Popup("SMASH x" + Combo, UIFactory.YELLOW, 1.1f);
                }
                else
                {
                    lBreakable.Flash();
                    Crash(lBreakable.transform.position, lBreakable.kind == BreakableKind.Car ? "HONK!" : "OUCH!");
                }
            }

            if (Punching && _punchTimer > 0.08f) PunchHits();
        }

        private void PunchHits()
        {
            Vector3 lFist = transform.position + CameraUpRaw * 1f + _heading * (2.4f + Speed * 0.03f);
            float lRadius = 2.8f;
            for (int i = Breakable.All.Count - 1; i >= 0; i--)
            {
                if (i >= Breakable.All.Count) continue;
                Breakable lBreakable = Breakable.All[i];
                if (!lBreakable.Overlaps(lFist, lRadius)) continue;
                Vector3 lPoint = lBreakable.transform.position + lBreakable.transform.up * lBreakable.height * 0.5f;
                lBreakable.Smash(_heading, Mathf.Max(Speed, 40f));
                if (State == PlayerState.Ground) _speed = Mathf.Min(MAX_SPEED, _speed + 7f);
                else _velocity += _heading * 5f;
                AddCombo();
                if (!_punchHit)
                {
                    _punchHit = true;
                    Juice.Impact(lPoint, 1f);
                    AudioManager.Play("sfx_punch_hit", 1f);
                    Juice.Popup(Combo > 2 ? "SMASH x" + Combo : "SMASH!", UIFactory.RED, 1.3f);
                }
            }
        }

        // ================================================================ movement

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
                if (!Physics.CapsuleCast(lBottom, lTop, RADIUS, lDir, out lHit, lDistance + SKIN, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
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
                    if (Vector3.Dot(lNormal, _up) >= GROUND_MIN_DOT) return true;
                    {
                        float lImpact = -Vector3.Dot(_heading, lNormal);
                        Vector3 lFlatNormal = Vector3.ProjectOnPlane(lNormal, _up).normalized;
                        Vector3 lSlide = Vector3.ProjectOnPlane(_heading, lFlatNormal);
                        if (lSlide.sqrMagnitude > 0.01f) _heading = lSlide.normalized;
                        if (lImpact > 0.8f && _speed > 30f && !Invulnerable) { Crash(pHit.point, "BONK!"); return false; }
                        _speed *= 1f - Mathf.Clamp01(lImpact) * 0.5f;
                        transform.position += lNormal * 0.02f;
                        return true;
                    }
                case PlayerState.Air:
                case PlayerState.Crash:
                    if (Vector3.Dot(lNormal, _gravityUp) > 0.45f)
                    {
                        Land(lNormal, pHit.point);
                        return false;
                    }
                    if (Vector3.Dot(lNormal, _gravityUp) < -0.5f)
                    {
                        _velocity = Vector3.ProjectOnPlane(_velocity, lNormal);
                        return true;
                    }
                    if (State == PlayerState.Air)
                    {
                        TryWallRun();
                        if (State == PlayerState.WallRun) return false;
                    }
                    _velocity = Vector3.ProjectOnPlane(_velocity, lNormal);
                    return true;
                case PlayerState.WallRun:
                    if (Vector3.Dot(lNormal, _gravityUp) > 0.5f) { Land(lNormal, pHit.point); return false; }
                    if (Vector3.Dot(lNormal, _wallRunDir) < -0.6f) { LeaveWall(0.2f); _velocity = Vector3.ProjectOnPlane(_velocity, lNormal); return false; }
                    return true;
            }
            return true;
        }

        private void UpdateHyper()
        {
            float lSpeed = Speed;
            if (!Hyper && lSpeed >= HYPER_ENTER)
            {
                Hyper = true;
                AudioManager.Play("sfx_hyper", 1f);
                Juice.Flash(Color.white, 0.9f);
                Juice.Popup("HYPERSPEED!!", Color.white, 1.7f);
                Juice.Shake(0.6f);
                Juice.Rollback(22f);
                Juice.ImpactFrame(1f);
                Juice.SlowMo(0.25f, 0.25f);
            }
            else if (Hyper && lSpeed < HYPER_EXIT)
            {
                Hyper = false;
                AudioManager.Play("sfx_hyper_out", 0.7f);
            }
            AudioManager.SetLoop("sfx_wind_loop", Mathf.Clamp01((lSpeed - 25f) / 100f) * 0.7f, 0.8f + Mathf.Clamp01(lSpeed / 150f) * 0.6f);
        }

        private void TrackProgress()
        {
            if (_path == null) return;
            _pathIndex = _path.FindNearest(transform.position, _pathIndex);
            TrackSample lSample = _path.samples[_pathIndex];
            Progress = _path.ProjectDistance(transform.position, _pathIndex);
            DistanceFromPath = Vector3.Distance(transform.position, lSample.position);
            if (State == PlayerState.Finished) return;
            bool lFreeFall = _airTime > 3.5f && Vector3.Dot(_velocity, Vector3.up) < -25f;
            if (DistanceFromPath > KILL_DISTANCE || lFreeFall || transform.position.y < _minTrackY - 80f || _airTime > 9f) Respawn();
        }

        public void SkipToNextCheckpoint()
        {
            foreach (float lCheckpoint in _path.checkpoints)
            {
                if (lCheckpoint <= Progress + 20f) continue;
                Place(_path.Evaluate(lCheckpoint + 4f));
                _speed = RUN_SPEED;
                _velocity = _heading * _speed;
                SetState(PlayerState.Ground);
                CameraRig.Instance?.Snap();
                return;
            }
        }

        public void Respawn()
        {
            float lCheckpoint = _path.LastCheckpointBefore(Progress + 5f);
            TrackSample lSample = _path.Evaluate(Mathf.Max(8f, lCheckpoint + 4f));
            Diving = false;
            EndDrift(false);
            Place(lSample);
            _speed = RUN_SPEED;
            _velocity = _heading * _speed;
            _invulnerable = 1.2f;
            _airTime = 0f;
            HasDoubleJump = true;
            HasAirDash = true;
            Combo = 0;
            Respawns++;
            _moveDir = _heading;
            SetState(_autoRun ? PlayerState.Finished : PlayerState.Ground);
            Juice.Flash(Color.white, 0.8f);
            Juice.Popup("RESPAWN", Color.white, 1f);
            AudioManager.Play("sfx_respawn", 0.8f);
            CameraRig.Instance?.Snap();
            OnRespawned?.Invoke();
        }
    }
}
