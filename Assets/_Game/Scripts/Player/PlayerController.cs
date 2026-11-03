using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum PlayerState { Frozen, Ground, Air, WallRun, Grind, Zip, Crash, Dead, Finished }

    // Kinematic runner on generated platforms : deck-relative steering, physical takeoffs that keep the slope momentum,
    // intent-based edge guard, glide / dive / drop-through, rails caught from the ground, rewind respawn.
    public partial class PlayerController : MonoBehaviour, ICameraTarget
    {
        // ---------------- tuning
        public const float RUN_SPEED = 58f;
        private const float RUN_ACCEL = 40f;
        public const float MAX_SPEED = 200f;
        public const float HYPER_ENTER = 120f;
        private const float HYPER_EXIT = 105f;
        public const float SMASH_SPEED = 80f;
        private const float SLOPE_ACCEL = 34f;
        private const float JUMP_VELOCITY = 18f;
        private const float DOUBLE_JUMP_VELOCITY = 16f;
        private const float DASH_BOOST = 25f;
        private const float DASH_TIME = 0.22f;
        private const float COYOTE_TIME = 0.12f;
        private const float JUMP_BUFFER = 0.14f;
        private const float RADIUS = 0.45f;
        private const float HEIGHT = 1.8f;
        private const float SKIN = 0.04f;
        private const float GROUND_MIN_DOT = 0.55f;
        // Steepest walkable surface against world up (wall-ride banks reach 65 deg) : rolling over a rounded lip
        // onto a vertical cap used to turn the run into a vertical launch.
        private const float GROUND_MIN_WORLD_DOT = 0.3f;
        private const float RAIL_RADIUS = 4.5f;
        private const float DRIFT_SLIP = 35f;
        private const float START_WINDOW_BEFORE = 0.25f;
        private const float START_WINDOW_AFTER = 0.12f;
        private const float FREE_ROAM_SPEED = 17f;

        // ---------------- state
        public PlayerState State { get; private set; } = PlayerState.Frozen;
        public Vector3 Velocity => _velocity;
        public Vector3 Up => _up;
        public Vector3 GravityUp => _gravityUp;
        public Vector3 Heading => _heading;
        public float Speed => OnSurface ? _speed : Vector3.ProjectOnPlane(_velocity, _gravityUp).magnitude;
        public float TotalSpeed => OnSurface ? _speed : _velocity.magnitude;
        private bool OnSurface => State == PlayerState.Ground || State == PlayerState.Grind || State == PlayerState.Zip || State == PlayerState.Finished;
        public Vector3 FacingDirection => _heading;
        public float BoostAmount => Mathf.Clamp01(_boostTimer / 0.6f);
        public bool CheatInvincible;
        // Menus own the keyboard (title screen) : the runner idles in place.
        public bool InputLocked;
        public bool Hyper { get; private set; }
        public bool Drifting { get; private set; }
        public int DriftDirection { get; private set; }
        public float DriftCharge { get; private set; }
        public int DriftTier => DriftCharge >= 2f ? 3 : DriftCharge >= 1.2f ? 2 : DriftCharge >= 0.5f ? 1 : 0;
        public bool Sliding { get; private set; }
        public bool Diving { get; private set; }
        public bool Gliding { get; private set; }
        public bool PassingThrough => _passThrough;
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
        public Deck CurrentDeck => _deck;
        public PlayerVisual Visual => _visual;
        public float AirTime => _airTime;

        // Camera / visual signals.
        public float DiveTime { get; private set; }
        public float DiveCamera01 => LevelBuilder.Smooth(Mathf.Clamp01((DiveTime - 0.25f) / 1.15f));
        public bool HasLandingPrediction { get; private set; }
        public Vector3 LandingPoint { get; private set; }
        public float LandingTime { get; private set; }
        public float EdgeTeeter01 { get; private set; }
        public int EdgeTeeterSide { get; private set; }

        public event System.Action OnJumped;
        public event System.Action OnDoubleJumped;
        public event System.Action<float> OnLanded;
        public event System.Action OnPunched;
        public event System.Action OnDashed;
        public event System.Action OnCrashed;
        public event System.Action OnRespawned;
        public event System.Action OnDied;
        public event System.Action OnHyper;

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
        private int _ringCombo;
        private float _ringComboTimer;
        private Vector3 _smoothCameraUp = Vector3.up;
        private bool _autoRun;
        private bool _punchHit;
        private float _boostTimer;
        private float _goTime = -1f;
        private float _startPressTime = -1f;
        private bool _freeRoam;
        private bool _barrier;

        private LevelLayout _layout;
        private PlayerVisual _visual;
        private Breadcrumbs _crumbs;
        private Deck _deck;

        // ---------------- camera target
        public Vector3 FocusPosition => (_visual != null ? _visual.Root.position : transform.position) + CameraUpRaw * 0.9f;
        public Vector3 CameraUp => _smoothCameraUp;
        private Vector3 CameraUpRaw => State == PlayerState.Ground || State == PlayerState.Grind || State == PlayerState.Finished ? _up : _gravityUp;
        public Vector3 CameraForward
        {
            get
            {
                if (_freeRoam) return _heading;
                if (State == PlayerState.Ground || State == PlayerState.Finished)
                {
                    // Look along the route, not the instant heading : steering does not swing the view.
                    if (_layout != null) return Vector3.ProjectOnPlane(_layout.spine.Forward(Progress + 6f), _up).normalized;
                    return _moveDir;
                }
                Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, CameraUpRaw);
                if (OnSurface || lFlat.magnitude < 6f) return _heading;
                return lFlat.normalized;
            }
        }
        public float Speed01 => Mathf.Clamp01((Speed - 45f) / 130f);
        public float CameraRoll => -Steer * 4f - (Drifting ? DriftDirection * 7f : 0f) + (State == PlayerState.WallRun ? WallSide * 12f : 0f);
        public bool HyperMode => Hyper;

        public void Init(LevelLayout pLayout, PlayerVisual pVisual)
        {
            _layout = pLayout;
            _visual = pVisual;
            _crumbs = new Breadcrumbs();
            Vector3 lStart = pLayout.startPosition;
            float lLateral;
            float lS = pLayout.spine.ProjectGlobal(lStart, out lLateral);
            Deck lDeck = pLayout.MainDeckAt(lS);
            Place(lDeck != null ? lDeck.SurfacePoint(lS, 0f) : lStart, pLayout.spine.Forward(lS), lDeck, lS);
            State = PlayerState.Frozen;
        }

        private void Place(Vector3 pPosition, Vector3 pForward, Deck pDeck, float pS)
        {
            transform.position = pPosition + Vector3.up * 0.05f;
            _deck = pDeck;
            Vector3 lUp = Vector3.up;
            if (pDeck != null)
            {
                Vector3 lC, lF, lR;
                float lW;
                pDeck.Frame(pS, out lC, out lF, out lR, out lUp, out lW);
                pForward = lF;
            }
            _up = lUp;
            _gravityUp = lUp;
            _smoothCameraUp = lUp;
            _heading = Vector3.ProjectOnPlane(pForward, lUp).normalized;
            _moveDir = _heading;
            _velocity = Vector3.zero;
            _speed = 0f;
            _psi = 0f;
            _slip = 0f;
            Progress = pS;
            transform.rotation = Quaternion.LookRotation(_heading, _up);
        }

        // ================================================================ race flow

        // Before GO : free movement around the start plaza (camera-relative), held behind the barrier.
        public void BeginFreeRoam(bool pBarrier = true)
        {
            _freeRoam = true;
            _barrier = pBarrier;
            SetState(PlayerState.Ground);
            _speed = 0f;
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
            if (State == PlayerState.Ground && _layout != null)
            {
                Vector3 lForward = Vector3.ProjectOnPlane(_layout.spine.Forward(Progress), _up).normalized;
                _psi = Mathf.Clamp(Vector3.SignedAngle(lForward, _heading, _up), -25f, 25f);
            }
            _speed = Mathf.Max(_speed, RUN_SPEED * 0.6f);
            float lDelta = _startPressTime - _goTime;
            if (_startPressTime > 0f && lDelta >= -START_WINDOW_BEFORE && lDelta <= START_WINDOW_AFTER) PerfectStart();
        }

        private void PerfectStart()
        {
            _speed = Mathf.Max(_speed, RUN_SPEED + 40f);
            _boostTimer = 1.5f;
            Juice.Popup("PERFECT START!!", UIFactory.YELLOW, 1.5f);
            Juice.Freeze(0.06f);
            Juice.Rollback(8f);
            Juice.FovKick(8f);
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
                    case PlayerState.Grind:
                    case PlayerState.Zip: GrindStep(lSubDt); break;
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
            if (State == PlayerState.Ground && _deck != null && !_freeRoam) _crumbs.Record(this, _deck, Progress, Lateral, _speed, Time.time);
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
            _ringComboTimer -= pDt;
            _boostTimer -= pDt;
            if (_ringComboTimer <= 0f) _ringCombo = 0;
            _comboTimer -= pDt;
            if (_comboTimer <= 0f) Combo = 0;
            if (Diving) DiveTime += pDt;
            else if (Time.time - _lastDiveLanding > 1f) DiveTime = Mathf.MoveTowards(DiveTime, 0f, pDt * 3f);

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
            if (Gliding && pState != PlayerState.Air) StopGlide();
            State = pState;
            StateTime = 0f;
            if (pState != PlayerState.Ground && pState != PlayerState.Finished) EndDrift(false);
            if (pState == PlayerState.Ground || pState == PlayerState.Finished)
            {
                _airTime = 0f;
                _glideTime = 0f;
                _assistGravity = 0f;
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

            bool lSmash = Speed >= SMASH_SPEED || Dashing || Hyper || Punching;
            for (int i = Breakable.All.Count - 1; i >= 0; i--)
            {
                if (i >= Breakable.All.Count) continue;
                Breakable lBreakable = Breakable.All[i];
                if (!lBreakable.Overlaps(lCenter, 0.7f)) continue;
                if (lSmash)
                {
                    lBreakable.Smash(_heading, Speed);
                    Juice.Hitstop(0.018f, 0.1f);
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
                lBreakable.Smash(_heading, Mathf.Max(Speed, 40f));
                if (State == PlayerState.Ground) _speed = Mathf.Min(MAX_SPEED, _speed + 7f);
                else _velocity += _heading * 5f;
                AddCombo();
                if (!_punchHit)
                {
                    _punchHit = true;
                    Juice.Hitstop(0.06f);
                    Juice.Shake(0.3f);
                    AudioManager.Play("sfx_punch_hit", 1f);
                    Juice.Popup(Combo > 2 ? "SMASH x" + Combo : "SMASH!", UIFactory.RED, 1.3f);
                }
            }
        }

        // ================================================================ movement

        private int GroundMask => _passThrough || (Diving && _slideHeld) ? Layers.GROUND_NO_MEMBRANE : Layers.GROUND;

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
                    {
                        float lImpact = -Vector3.Dot(_heading, lNormal);
                        if (lImpact > 0.8f && _speed > 40f && !Invulnerable) { Crash(pHit.point, "BONK!"); return false; }
                        _speed *= 1f - Mathf.Clamp01(lImpact) * 0.4f;
                        transform.position += lNormal * 0.02f;
                        return true;
                    }
                case PlayerState.Air:
                case PlayerState.Crash:
                    if (Vector3.Dot(lNormal, _gravityUp) > 0.45f)
                    {
                        Land(lNormal, pHit.point, DeckChunk.DeckOf(pHit.collider));
                        return false;
                    }
                    if (Vector3.Dot(lNormal, _gravityUp) < -0.5f)
                    {
                        _velocity = Vector3.ProjectOnPlane(_velocity, lNormal);
                        return true;
                    }
                    if (State == PlayerState.Air && TryLedge(pHit)) return false;
                    if (State == PlayerState.Air)
                    {
                        TryWallRun();
                        if (State == PlayerState.WallRun) return false;
                    }
                    // Sliding along a slanted rock face must never turn forward speed into a launch
                    // (a jagged deck cap at 100 m/s used to throw the runner 80 m up).
                    float lRise = Mathf.Max(0f, Vector3.Dot(_velocity, _gravityUp));
                    _velocity = Vector3.ProjectOnPlane(_velocity, lNormal);
                    float lExtra = Vector3.Dot(_velocity, _gravityUp) - lRise;
                    if (lExtra > 0f) _velocity -= _gravityUp * lExtra;
                    return true;
                case PlayerState.WallRun:
                    if (Vector3.Dot(lNormal, _gravityUp) > 0.5f) { Land(lNormal, pHit.point, DeckChunk.DeckOf(pHit.collider)); return false; }
                    if (Vector3.Dot(lNormal, _wallRunDir) < -0.6f) { LeaveWall(0.2f); _velocity = Vector3.ProjectOnPlane(_velocity, lNormal); return false; }
                    return true;
            }
            return true;
        }

        private void UpdateHyper()
        {
            float lSpeed = Speed;
            if (!Hyper && lSpeed >= HYPER_ENTER && !_freeRoam)
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
            else if (Hyper && lSpeed < HYPER_EXIT)
            {
                Hyper = false;
                AudioManager.Play("sfx_hyper_out", 0.7f);
            }
            AudioManager.SetLoop("sfx_wind_loop", Mathf.Clamp01((lSpeed - 30f) / 140f) * 0.7f, 0.8f + Mathf.Clamp01(lSpeed / 200f) * 0.6f);
        }

        // ================================================================ progress, death, respawn

        private void TrackProgress()
        {
            if (_layout == null) return;
            float lLateral;
            if ((State == PlayerState.Grind || State == PlayerState.Zip) && _rail != null)
            {
                Progress = _rail.SpineS(_railDistance);
                _layout.spine.Project(transform.position, Progress, out lLateral);
            }
            else Progress = _layout.spine.Project(transform.position, Progress, out lLateral);
            Lateral = lLateral;
            if (_barrier && _layout != null)
            {
                // Energy barrier at the start line : held in the plaza until GO.
                float lLimit = _layout.startLineS - 1.5f;
                if (Progress > lLimit)
                {
                    Vector3 lForward = _layout.spine.Forward(Progress);
                    transform.position -= lForward * (Progress - lLimit);
                    _velocity -= lForward * Mathf.Max(0f, Vector3.Dot(_velocity, lForward));
                    if (Vector3.Dot(_heading, lForward) > 0f) _speed *= 0.5f;
                    Progress = lLimit;
                }
            }
        }

        // Falling with nothing below : the layout knows within ~0.65 s.
        private float _lostTime;

        private void CheckFallOut()
        {
            if (_layout == null || State == PlayerState.Finished || State == PlayerState.Dead || _autoRun) return;
            if (State != PlayerState.Air && State != PlayerState.Crash)
            {
                _lostTime = 0f;
                return;
            }
            // A predicted landing always wins : long plunges and dives fall far below the route line on purpose.
            // Without one, the verdict waits 0.15 s so a single frame of doubt never kills.
            _lostTime = HasLandingPrediction ? 0f : _lostTime + Time.deltaTime;
            bool lLost = _lostTime > 0.15f;
            bool lNoLanding = lLost && transform.position.y < _takeoffY - 12f;
            bool lBelow = lLost && transform.position.y < _layout.FloorY(Progress) - 14f;
            if (lNoLanding || lBelow || _airTime > (HasLandingPrediction ? 14f : 7f))
            {
                if (Autopilot.Enabled)
                    Debug.Log("[HS-LD] fall-out " + (lNoLanding ? "noLanding" : lBelow ? "belowFloor" : "airTime") + " y=" + transform.position.y.ToString("F1") + " takeoffY=" + _takeoffY.ToString("F1")
                        + " floor=" + _layout.FloorY(Progress).ToString("F1") + " vel=" + _velocity.ToString("F0") + " state=" + State);
                Die();
            }
        }

        public void Die()
        {
            if (State == PlayerState.Dead) return;
            if (CheatInvincible && _crumbs.HasSafe)
            {
                RespawnAt(_crumbs.PickSafe(Progress, _layout), false);
                return;
            }
            Diving = false;
            StopGlide();
            EndDrift(false);
            _passThrough = false;
            SetState(PlayerState.Dead);
            Combo = 0;
            OnDied?.Invoke();
        }

        public Breadcrumb PickRespawn() => _crumbs.PickSafe(Progress, _layout);

        public Breadcrumbs Crumbs => _crumbs;

        // Called by the respawn director at the end of the rewind.
        public void RespawnAt(Breadcrumb pCrumb, bool pCount = true)
        {
            Deck lDeck = pCrumb.deck;
            float lLateral = lDeck != null ? Mathf.Clamp(pCrumb.lateral, -(lDeck.Width(pCrumb.s) * 0.5f - 3f), lDeck.Width(pCrumb.s) * 0.5f - 3f) : 0f;
            Vector3 lPosition = lDeck != null ? lDeck.SurfacePoint(pCrumb.s, lLateral) : pCrumb.position;
            Place(lPosition, _layout.spine.Forward(pCrumb.s), lDeck, pCrumb.s);
            // Back in the race at a controllable pace : never relaunched at hyper speed right before the danger.
            _speed = Mathf.Clamp(pCrumb.speed * 0.9f, RUN_SPEED, RUN_SPEED + 20f);
            _velocity = _heading * _speed;
            _invulnerable = 1.5f;
            _airTime = 0f;
            HasDoubleJump = true;
            HasAirDash = true;
            Combo = 0;
            if (pCount) Respawns++;
            _moveDir = _heading;
            SetState(_autoRun ? PlayerState.Finished : PlayerState.Ground);
            _crumbs.Clear();
            OnRespawned?.Invoke();
        }

        public void SkipToNextCheckpoint()
        {
            foreach (SafePoint lPoint in _layout.safePoints)
            {
                if (lPoint.s <= Progress + 60f) continue;
                RespawnAt(new Breadcrumb { s = lPoint.s, deck = lPoint.deck, lateral = 0f, speed = RUN_SPEED, position = lPoint.deck.SurfacePoint(lPoint.s, 0f) }, false);
                CameraRig.Instance?.Snap();
                return;
            }
        }
    }
}
