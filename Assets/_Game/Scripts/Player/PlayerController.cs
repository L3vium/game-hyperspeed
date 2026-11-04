using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum PlayerState { Frozen, Ground, Air, WallRun, Grind, Zip, Crash, Dead, Finished }

    // Kinematic runner on an enormous terrain : the route gives the forward direction, you strafe across it. Speed is
    // stage-scaled by a SpeedProfile and only ever lost to climbs and crashes. Past the hyper threshold you turn to light.
    public partial class PlayerController : MonoBehaviour, ICameraTarget
    {
        // ---------------- tuning
        private const float RUN_ACCEL = 60f;
        private const float SLOPE_ACCEL = 34f;
        private const float JUMP_VELOCITY = 20f;
        private const float DASH_TIME = 0.22f;
        private const float COYOTE_TIME = 0.14f;
        private const float JUMP_BUFFER = 0.14f;
        private const float RADIUS = 0.45f;
        private const float HEIGHT = 1.8f;
        private const float SKIN = 0.04f;
        private const float GROUND_MIN_DOT = 0.5f;
        private const float GROUND_MIN_WORLD_DOT = 0.42f;
        private const float START_WINDOW_BEFORE = 0.3f;
        private const float START_WINDOW_AFTER = 0.15f;
        private const float FREE_ROAM_SPEED = 28f;

        // ---------------- state
        public PlayerState State { get; private set; } = PlayerState.Frozen;
        public Vector3 Velocity => _velocity;
        public Vector3 Up => _up;
        public Vector3 GravityUp => _gravityUp;
        public Vector3 Heading => _heading;
        public float Speed => OnSurface ? _speed : Vector3.ProjectOnPlane(_velocity, _gravityUp).magnitude;
        public float TotalSpeed => OnSurface ? Mathf.Sqrt(_speed * _speed + _slip * _slip) : _velocity.magnitude;
        private bool OnSurface => State == PlayerState.Ground || State == PlayerState.Grind || State == PlayerState.Zip || State == PlayerState.Finished;
        public Vector3 FacingDirection => _heading;
        public float BoostAmount => Mathf.Clamp01(_boostTimer / 0.6f);
        public SpeedProfile Profile => _profile;
        public bool CheatInvincible;
        // Menus own the keyboard (title screen) : the runner idles in place.
        public bool InputLocked;
        public bool Hyper { get; private set; }
        public bool LightForm { get; private set; }
        public bool Drifting { get; private set; }
        public int DriftDirection { get; private set; }
        public float DriftCharge { get; private set; }
        public int DriftTier => DriftCharge >= 2f ? 3 : DriftCharge >= 1.2f ? 2 : DriftCharge >= 0.5f ? 1 : 0;
        public bool Sliding { get; private set; }
        public bool Diving { get; private set; }
        public bool Gliding { get; private set; }
        public bool Dashing => _dashTimer > 0f;
        public bool Punching => _punchTimer > 0f;
        public bool HasDoubleJump { get; private set; } = true;
        public bool HasAirDash { get; private set; } = true;
        public float Steer { get; private set; }
        public int WallSide { get; private set; }
        public float StateTime { get; private set; }
        public int Combo { get; private set; }
        public float ComboTimer01 => Mathf.Clamp01(_comboTimer / 3f);
        public int Respawns { get; private set; }
        public float TopSpeed { get; private set; }
        public bool Invulnerable => _invulnerable > 0f;
        public float Progress { get; private set; }
        public float Lateral { get; private set; }
        public float DistanceFromPath => Mathf.Abs(Lateral);
        public bool FreeRoam => _freeRoam;
        public PlayerVisual Visual => _visual;
        public float AirTime => _airTime;

        // Camera / visual signals.
        public float DiveTime { get; private set; }
        // Held for a moment, the dive swings the camera above the runner, looking down the fall.
        public float DiveCamera01 => HSMath.Smooth(Mathf.Clamp01((DiveTime - 0.35f) / 0.7f));
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

        private Vector3 _velocity;
        private Vector3 _up = Vector3.up;
        private Vector3 _gravityUp = Vector3.up;
        private Vector3 _heading = Vector3.forward;
        private Vector3 _moveDir = Vector3.forward;
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
        private float _airTime;
        private Vector3 _smoothCameraUp = Vector3.up;
        private bool _autoRun;
        private bool _punchHit;
        private float _boostTimer;
        private float _goTime = -1f;
        private float _startPressTime = -1f;
        private bool _freeRoam;
        private bool _barrier;
        private bool _padFlightLocked;

        private LevelLayout _layout;
        private SpeedProfile _profile = new SpeedProfile();
        private PlayerVisual _visual;
        private Breadcrumbs _crumbs;

        // ---------------- camera target
        public Vector3 FocusPosition => (_visual != null ? _visual.Root.position : transform.position) + CameraUpRaw * 0.9f;
        public Vector3 CameraUp => _smoothCameraUp;
        private Vector3 CameraUpRaw => State == PlayerState.Ground || State == PlayerState.Grind || State == PlayerState.Finished ? _up : _gravityUp;
        public Vector3 CameraForward
        {
            get
            {
                if (_freeRoam) return _heading;
                if (State == PlayerState.WallRun && Wall != WallMode.Climb) return _wallRunDir;
                if (_layout == null) return _heading;
                // Along the route, a third of the way toward where the body points : strafing never swings the view.
                Vector3 lFlow = _layout.Flow(Progress + 6f);
                Vector3 lFlat = Vector3.ProjectOnPlane(_heading, Vector3.up);
                if (lFlat.sqrMagnitude > 0.01f) lFlow = Vector3.Slerp(lFlow, lFlat.normalized, 0.3f);
                return lFlow.normalized;
            }
        }
        public float Speed01 => Mathf.Clamp01((Speed - _profile.cruise * 0.55f) / (_profile.max - _profile.cruise * 0.55f));
        public float CameraRoll => -Steer * 7f - (Drifting ? DriftDirection * 9f : 0f) + (State == PlayerState.WallRun && Wall != WallMode.Climb ? WallSide * 16f : 0f);
        public bool HyperMode => Hyper;

        public void Init(LevelLayout pLayout, PlayerVisual pVisual)
        {
            _layout = pLayout;
            _profile = pLayout.profile;
            Ballistics.Scale = _profile.gravityScale;
            _visual = pVisual;
            _crumbs = new Breadcrumbs();
            Place(pLayout.spawnS, 0f);
            State = PlayerState.Frozen;
        }

        private void Place(float pS, float pLateral)
        {
            transform.position = _layout.GroundPoint(pS, pLateral) + Vector3.up * 0.05f;
            Vector3 lForward = _layout.Flow(pS);
            _up = _layout.field.Normal(pS, pLateral, 6f);
            _gravityUp = _up;
            _smoothCameraUp = _up;
            _heading = Vector3.ProjectOnPlane(lForward, _up).normalized;
            _moveDir = _heading;
            _velocity = Vector3.zero;
            _speed = 0f;
            _slip = 0f;
            Progress = pS;
            Lateral = pLateral;
            _airYaw = _layout.spine.Evaluate(pS).yaw;
            transform.rotation = Quaternion.LookRotation(_heading, _up);
        }

        // ================================================================ race flow

        // Before GO : free movement around the start plaza (camera-relative), held behind the barrier.
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
            if (_layout != null) _heading = Vector3.ProjectOnPlane(_layout.Flow(Progress), _up).normalized;
            _speed = Mathf.Max(_speed, _profile.cruise * 0.8f);
            float lDelta = _startPressTime - _goTime;
            if (_startPressTime > 0f && lDelta >= -START_WINDOW_BEFORE && lDelta <= START_WINDOW_AFTER) PerfectStart();
        }

        private void PerfectStart()
        {
            _speed = Mathf.Max(_speed, _profile.cruise * 1.4f);
            _boostTimer = 3f;
            Juice.Popup("PERFECT START!!", UIFactory.YELLOW, 1.5f);
            Juice.Freeze(0.06f);
            Juice.Rollback(10f);
            Juice.FovKick(10f);
            AudioManager.Play("sfx_perfect_start", 1f);
            AddCombo();
        }

        private bool StartInputPressed => HSInput.JumpPressed || HSInput.DashPressed || HSInput.ForwardPressed;

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
            if (lDt <= 0f) return;

            if (_goTime > 0f && _startPressTime < 0f && Mathf.Abs(Time.time - _goTime) <= START_WINDOW_BEFORE + 0.05f && StartInputPressed)
            {
                _startPressTime = Time.time;
                if (State != PlayerState.Frozen && !_freeRoam && Time.time >= _goTime) PerfectStart();
            }
            if (State == PlayerState.Frozen || State == PlayerState.Dead)
            {
                _visual?.Tick(lDt);
                return;
            }

            ReadInput(lDt);
            Timers(lDt);
            if (State == PlayerState.Air || State == PlayerState.Crash) PredictAir();

            float lDistance = TotalSpeed * lDt;
            int lSubsteps = Mathf.Clamp(Mathf.CeilToInt(lDistance / 0.8f), 1, 16);
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

            UpdateHyper();
            TrackProgress();
            CheckFallOut();
            if (State == PlayerState.Ground && !_freeRoam) _crumbs.Record(this, _layout, Progress, Lateral, _speed, Time.time);
            transform.rotation = Quaternion.LookRotation(_heading, OnSurface ? _up : _gravityUp);
            AudioManager.SetLoop("sfx_boost_loop", BoostAmount * 0.6f, 0.9f + BoostAmount * 0.3f);
            _smoothCameraUp = Vector3.Slerp(_smoothCameraUp, CameraUpRaw, 1f - Mathf.Exp(-10f * lDt)).normalized;
            TopSpeed = Mathf.Max(TopSpeed, Speed);
            _visual?.Tick(lDt);
        }

        private bool _jumpPressed, _dashPressed, _slidePressed, _slideHeld, _punchPressed;

        private void ReadInput(float pDt)
        {
            bool lCanAct = State != PlayerState.Crash && State != PlayerState.Finished && !_autoRun && !InputLocked;
            Steer = lCanAct ? HSInput.Steer : 0f;
            _jumpPressed = lCanAct && HSInput.JumpPressed;
            _dashPressed = lCanAct && HSInput.DashPressed && !_freeRoam;
            _slidePressed = lCanAct && HSInput.SlidePressed;
            _slideHeld = lCanAct && HSInput.SlideHeld;
            _punchPressed = lCanAct && HSInput.PunchPressed && !_freeRoam;

            if (_jumpPressed) _jumpBufferedAt = Time.time;
            if (_punchPressed && _punchCooldown <= 0f) Punch();
            if (_dashPressed && _dashCooldown <= 0f) Dash();
            if (_jumpPressed) TryJump();
            if (_slidePressed && State == PlayerState.Air && !Diving) Dive();
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
            _boostTimer -= pDt;
            _comboTimer -= pDt;
            if (_comboTimer <= 0f) Combo = 0;
            if (Diving) DiveTime += pDt;
            else if (Time.time - _lastDiveLanding > 0.6f) DiveTime = Mathf.MoveTowards(DiveTime, 0f, pDt * 3f);

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
            if (State == PlayerState.WallRun) AudioManager.SetLoop("sfx_wallrun_loop", 0f);
            if (Gliding && pState != PlayerState.Air) StopGlide();
            State = pState;
            StateTime = 0f;
            if (pState != PlayerState.Ground && pState != PlayerState.Finished) EndDrift(false);
            if (pState == PlayerState.Air && _layout != null) _airYaw = _layout.spine.Evaluate(Progress).yaw;
            if (pState == PlayerState.Ground || pState == PlayerState.Finished)
            {
                _airTime = 0f;
                _glideTime = 0f;
                _padFlight = false;
                HasLandingPrediction = false;
            }
        }

        // ================================================================ interactions

        private void TouchStuff()
        {
            Vector3 lCenter = transform.position + CameraUpRaw * 0.9f;
            for (int i = Interactable.All.Count - 1; i >= 0; i--)
            {
                if (i >= Interactable.All.Count) continue;
                Interactable lItem = Interactable.All[i];
                if (lItem.Test(lCenter)) lItem.Touch(this);
            }

            bool lSmash = Speed >= _profile.cruise * 1.25f || Dashing || Hyper || Punching;
            for (int i = Breakable.All.Count - 1; i >= 0; i--)
            {
                if (i >= Breakable.All.Count) continue;
                Breakable lBreakable = Breakable.All[i];
                if (!lBreakable.Overlaps(lCenter, 1.5f)) continue;
                if (lSmash)
                {
                    lBreakable.Smash(_heading, Speed);
                    Juice.Hitstop(0.03f, 0.1f);
                    Juice.Shake(0.4f);
                    AddCombo();
                    if (Combo % 5 == 0) Juice.Popup("SMASH x" + Combo, UIFactory.YELLOW, 1.1f);
                }
                else
                {
                    lBreakable.Smash(_heading, Speed * 0.5f);
                    Crash(lBreakable.transform.position, "OUCH!");
                }
            }

            if (Punching) PunchHits();
        }

        // One punch : wide reach, the rock explodes, time stops for a beat and you pick up speed.
        private void PunchHits()
        {
            Vector3 lFist = transform.position + CameraUpRaw + _heading * (8f + Speed * 0.14f);
            float lRadius = 14f * Mathf.Sqrt(_profile.k);
            for (int i = Breakable.All.Count - 1; i >= 0; i--)
            {
                if (i >= Breakable.All.Count) continue;
                Breakable lBreakable = Breakable.All[i];
                if (!lBreakable.Overlaps(lFist, lRadius)) continue;
                lBreakable.Smash(_heading, Mathf.Max(Speed, 40f));
                float lBoost = _profile.cruise * 0.16f;
                if (OnSurface) _speed = Mathf.Min(_profile.max, _speed + lBoost);
                else _velocity += _heading * lBoost;
                _boostTimer = Mathf.Max(_boostTimer, 0.8f);
                AddCombo();
                if (!_punchHit)
                {
                    _punchHit = true;
                    Juice.Hitstop(0.11f);
                    Juice.Shake(0.8f);
                    Juice.ImpactFrame(1f);
                    Juice.Rollback(12f);
                    Juice.FovKick(10f);
                    AudioManager.Play("sfx_punch_hit", 1f);
                    Juice.Popup(Combo > 2 ? "SMASH x" + Combo : "SMASH!", UIFactory.RED, 1.5f);
                    _visual?.PunchImpact(lBreakable.transform.position);
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
                    if (Vector3.Dot(lNormal, _up) >= GROUND_MIN_DOT && lNormal.y >= GROUND_MIN_WORLD_DOT) return true;
                    if (State == PlayerState.Ground && !_freeRoam && EnterWall(pHit)) return State != PlayerState.WallRun;
                    transform.position += lNormal * 0.02f;
                    return true;
                case PlayerState.Air:
                case PlayerState.Crash:
                    if (Vector3.Dot(lNormal, _gravityUp) > 0.45f)
                    {
                        Land(lNormal, pHit.point, pHit.collider);
                        return false;
                    }
                    if (Vector3.Dot(lNormal, _gravityUp) < -0.5f)
                    {
                        _velocity = Vector3.ProjectOnPlane(_velocity, lNormal);
                        return true;
                    }
                    if (State == PlayerState.Air && EnterWall(pHit)) return State != PlayerState.WallRun;
                    // Sliding along a slanted face must never turn forward speed into a launch.
                    float lRise = Mathf.Max(0f, Vector3.Dot(_velocity, _gravityUp));
                    _velocity = Vector3.ProjectOnPlane(_velocity, lNormal);
                    float lExtra = Vector3.Dot(_velocity, _gravityUp) - lRise;
                    if (lExtra > 0f) _velocity -= _gravityUp * lExtra;
                    return true;
                case PlayerState.WallRun:
                    if (Vector3.Dot(lNormal, _gravityUp) > 0.5f)
                    {
                        _velocity = _wallRunDir * _wallSpeed;
                        Land(lNormal, pHit.point, pHit.collider);
                        return false;
                    }
                    return true;
            }
            return true;
        }

        private void UpdateHyper()
        {
            float lSpeed = Speed;
            if (!Hyper && lSpeed >= _profile.hyperEnter && !_freeRoam)
            {
                Hyper = true;
                AudioManager.Play("sfx_hyper", 1f);
                Juice.Popup("HYPERSPEED!!", Color.white, 1.7f);
                Juice.Shake(0.5f);
                Juice.Rollback(10f);
                Juice.ImpactFrame(1f);
                Juice.SlowMo(0.3f, 0.3f);
                OnHyper?.Invoke();
            }
            else if (Hyper && lSpeed < _profile.hyperExit)
            {
                Hyper = false;
                AudioManager.Play("sfx_hyper_out", 0.7f);
            }

            // Past this, there is no runner left : only light.
            if (!LightForm && lSpeed >= _profile.lightEnter && !_freeRoam)
            {
                LightForm = true;
                Juice.Popup("LIGHT SPEED!!", Color.white, 2f);
                Juice.Flash(Color.white, 0.8f);
                Juice.Shake(0.7f);
                Juice.Rollback(12f);
                Juice.ImpactFrame(1f);
                AudioManager.Play("sfx_hyper", 1f, 1.3f);
                OnLight?.Invoke();
            }
            else if (LightForm && lSpeed < _profile.lightExit)
            {
                LightForm = false;
                AudioManager.Play("sfx_hyper_out", 0.7f, 1.2f);
            }
            AudioManager.SetLoop("sfx_wind_loop", Mathf.Clamp01(Speed01 + 0.25f) * 0.7f, 0.8f + Speed01 * 0.6f);
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
            else Progress = _layout.spine.Project(transform.position, Progress, out lLateral);
            Lateral = lLateral;

            if (_barrier)
            {
                // Energy barrier at the start line : held in the plaza until GO. The plaza's back is closed too.
                float lLimit = _layout.startLineS - 3f;
                if (Progress > lLimit)
                {
                    Vector3 lForward = _layout.spine.Forward(Progress);
                    transform.position -= lForward * (Progress - lLimit);
                    _velocity -= lForward * Mathf.Max(0f, Vector3.Dot(_velocity, lForward));
                    Progress = lLimit;
                }
                float lBack = _layout.spine.StartS + 60f;
                if (Progress < lBack)
                {
                    transform.position += _layout.spine.Forward(Progress) * (lBack - Progress);
                    Progress = lBack;
                }
            }
        }

        private float _noGroundTime;

        // Far below the ground, or 2 s falling with no ground under you : gone. Nothing else ever kills you.
        private void CheckFallOut()
        {
            if (_layout == null || State == PlayerState.Finished || State == PlayerState.Dead || _autoRun) return;
            if (State != PlayerState.Air && State != PlayerState.Crash)
            {
                _noGroundTime = 0f;
                return;
            }
            bool lOver = _layout.field.OverGround(Progress, Lateral);
            _noGroundTime = !lOver && _velocity.y < 0f ? _noGroundTime + Time.deltaTime : 0f;
            if (_noGroundTime > 2f || transform.position.y < _layout.KillY(Progress, Speed))
            {
                if (Autopilot.Enabled) Debug.Log("[HS-LD] fall-out s=" + Progress.ToString("F0") + " lat=" + Lateral.ToString("F0") + " y=" + transform.position.y.ToString("F0") + " vel=" + _velocity.ToString("F0") + " state=" + State);
                Die();
            }
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
            StopGlide();
            EndDrift(false);
            SetState(PlayerState.Dead);
            Combo = 0;
            OnDied?.Invoke();
        }

        public Breadcrumb PickRespawn() => _crumbs.PickSafe(Progress, Speed, _layout);

        public Breadcrumbs Crumbs => _crumbs;

        // Called by the respawn director at the end of the rewind.
        public void RespawnAt(Breadcrumb pCrumb, bool pCount = true)
        {
            float lHalf = _layout.field.HalfWidth(pCrumb.s) * 0.8f;
            float lLateral = Mathf.Clamp(pCrumb.lateral, -lHalf, lHalf);
            Place(pCrumb.s, lLateral);
            _speed = Mathf.Max(pCrumb.speed * 0.9f, _profile.cruise);
            _velocity = _heading * _speed;
            _invulnerable = 1.5f;
            _airTime = 0f;
            HasDoubleJump = true;
            HasAirDash = true;
            Combo = 0;
            if (pCount) Respawns++;
            SetState(_autoRun ? PlayerState.Finished : PlayerState.Ground);
            _crumbs.Clear();
            OnRespawned?.Invoke();
        }

        public void SkipToNextCheckpoint()
        {
            float lS = Mathf.Min(Progress + _profile.cruise * 10f, _layout.finishS - 10f);
            RespawnAt(new Breadcrumb { s = lS, lateral = 0f, speed = _profile.cruise }, false);
            CameraRig.Instance?.Snap();
        }
    }
}
