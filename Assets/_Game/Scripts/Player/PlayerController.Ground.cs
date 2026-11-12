using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Ground : you run where the mouse looks, A / D strafes on top of that. You stay glued to the surface whatever its
    // angle : only a jump, an edge, a ramp or aiming up while dashing takes you off it. Drift charges a boost.
    public partial class PlayerController
    {
        private const float SNAP_REACH = 1.4f;

        public enum TakeoffReason { Edge, Jump, Aim, Stall }

        private float _slip;
        private float _driftEntrySpeed;

        // Looking down a slope on the ground : belly first, it feeds you harder.
        public bool BellySliding { get; private set; }
        public bool LastLandingWasDive { get; private set; }
        private float _diveSlideTimer;

        public float LateralSpeed => OnSurface ? _slip : (_layout != null ? Vector3.Dot(_velocity, _layout.spine.Right(Progress)) : 0f);

        // The convoy block under the feet : it carries you along.
        private MovingPlatform _carrier;

        private void GroundStep(float pDt)
        {
            _lastGrounded = Time.time;
            if (_carrier != null && _carrier.Delta.sqrMagnitude > 0f) transform.position += _carrier.Delta / Mathf.Max(1, _substeps);
            Vector3 lForward = Vector3.ProjectOnPlane(_heading, _up);
            lForward = lForward.sqrMagnitude > 0.0001f ? lForward.normalized : _aimForward;

            UpdateDrift(pDt);
            Sliding = _slideHeld && !Drifting && !_freeRoam;
            _diveSlideTimer -= pDt;
            BellySliding = !_freeRoam && !Drifting && (_diveSlideTimer > 0f || AimPitch < DIVE_PITCH && Vector3.Dot(lForward, GravityDown()) > 0.08f);

            if (_freeRoam)
            {
                FreeRoamSteer(pDt);
                lForward = _heading;
                _slip = 0f;
            }
            else
            {
                // Quick to answer the mouse, a bit heavier the faster you go. A drift swings harder.
                Vector3 lAim = Vector3.ProjectOnPlane(_aimForward, _up);
                if (lAim.sqrMagnitude > 0.0001f)
                {
                    float lTurn = Mathf.Lerp(14f, 8f, Speed01) * (Drifting ? 1.5f : 1f);
                    lForward = Vector3.RotateTowards(lForward, lAim.normalized, lTurn * pDt, 0f);
                }
                // On the sections that carry you (loops, twists) the track itself keeps you in : it is a ride, not a tightrope.
                bool lCarried = _layout != null && _layout.spine.samples[_layout.spine.IndexOf(Progress)].localGravity;
                if (lCarried)
                {
                    Vector3 lFlow = Vector3.ProjectOnPlane(_layout.Flow(Progress), _up);
                    if (lFlow.sqrMagnitude > 0.0001f) lForward = Vector3.RotateTowards(lForward, lFlow.normalized, 2.6f * pDt, 0f);
                }
                Vector3 lSide = Vector3.Cross(_up, lForward).normalized;
                StrafeStep(lSide, pDt);
                if (lCarried)
                {
                    float lHalf = _layout.field.HalfWidth(Progress);
                    float lOut = (Lateral - _layout.field.CenterLateral(Progress)) / Mathf.Max(1f, lHalf);
                    if (Mathf.Abs(lOut) > 0.45f) _slip -= Mathf.Sign(lOut) * (Mathf.Abs(lOut) - 0.45f) * _speed * 1.6f * pDt;
                }
                SpeedStep(lForward, pDt);
                _heading = lForward;
            }

            Vector3 lRight = Vector3.Cross(_up, lForward).normalized;
            _moveDir = lForward;
            _velocity = lForward * _speed + lRight * _slip;

            Move(_velocity * pDt);
            if (State != PlayerState.Ground && State != PlayerState.Finished) return;
            ReprojectProgress();

            if (!ProbeGround(pDt))
            {
                TakeOff(TakeoffReason.Edge);
                return;
            }
            UpdateWallRide();

            // A wall or a ceiling only holds you if you keep your speed, unless the track is built to carry you.
            bool lLocal = _layout != null && _layout.spine.samples[_layout.spine.IndexOf(Progress)].localGravity;
            if (!lLocal && !_freeRoam && Vector3.Dot(_up, Vector3.up) < 0.25f && _speed < _profile.cruise * 0.7f)
            {
                TakeOff(TakeoffReason.Stall);
                return;
            }

            if (!_freeRoam && TryGrind()) return;
            if (Time.time - _jumpBufferedAt < JUMP_BUFFER && State == PlayerState.Ground) TryJump();
        }

        private void StrafeStep(Vector3 pRight, float pDt)
        {
            float lMax = _profile.StrafeMax(_speed) * 1.25f;
            float lInput = Steer;
            if (Drifting)
            {
                lInput = Mathf.Clamp(DriftDirection * 0.55f + DriftSteer * 0.6f, -1f, 1f);
                lMax *= 1.35f;
            }
            float lTarget = lInput * lMax;
            // Snappy both ways : the sidestep answers at once and stops at once.
            _slip = Mathf.MoveTowards(_slip, lTarget, lMax / (Mathf.Abs(lTarget) > Mathf.Abs(_slip) ? 0.06f : 0.1f) * pDt);
            // The slope pushes you sideways : leaning blocks and walls shape your line.
            _slip += Vector3.Dot(GravityDown(), pRight) * SLOPE_ACCEL * 0.8f * pDt;
            // Up a mountain side : it slides you back down to the floor, harder the higher you went.
            if (_layout != null && !_freeRoam)
            {
                TrackBlock lBlock = _layout.field.BlockAt(Progress);
                if (lBlock != null && lBlock.mountain)
                {
                    float lOut = Lateral - lBlock.offset;
                    float lOver = Mathf.Abs(lOut) - lBlock.halfWidth;
                    if (lOver > 0f)
                    {
                        Vector3 lHome = -Mathf.Sign(lOut) * _layout.spine.Right(Progress);
                        _slip += Vector3.Dot(lHome, pRight) * (70f + lOver * 0.9f) * pDt;
                    }
                }
            }
        }

        private Vector3 GravityDown() => _layout != null ? -_layout.GravityUp(Progress) : Vector3.down;

        // Kicks are instant and fade back toward cruise. Slopes feed you. Nothing clamps the top but the safety limit.
        private void SpeedStep(Vector3 pForward, float pDt)
        {
            float lCruise = _profile.cruise;
            if (State == PlayerState.Finished)
            {
                // Past the line : the runner eases down to a proud run and keeps going (the results show it running),
                // and only stops before the end of the track.
                float lEnd = _layout != null ? _layout.Length - Mathf.Max(150f, lCruise * 1.5f) : float.MaxValue;
                float lWanted = Progress < lEnd ? lCruise * 0.45f : 0f;
                float lRate = _speed > lWanted ? Mathf.Max(lCruise * 0.8f, (_speed - lWanted) * 2.2f) : RUN_ACCEL;
                _speed = Mathf.MoveTowards(_speed, lWanted, lRate * pDt);
                return;
            }
            float lSlope = Vector3.Dot(pForward, GravityDown());
            _speed += lSlope * SLOPE_ACCEL * (lSlope < 0f ? 0.4f : 1f) * (Sliding || BellySliding ? 1.8f : 1f) * pDt;
            // A drift bites : it bleeds speed down to 70 % of what you came in with. Releasing it gives it all back.
            if (Drifting)
            {
                float lFloor = Mathf.Max(_driftEntrySpeed * 0.7f, lCruise * 0.6f);
                if (_speed > lFloor) _speed = Mathf.MoveTowards(_speed, lFloor, Mathf.Max(lCruise * 0.45f, _driftEntrySpeed * 0.35f) * pDt);
                _speed = Mathf.Clamp(_speed, lCruise * 0.55f, _profile.SafetyMax);
                return;
            }
            // You run only while you ask for it. Let go and you coast down to a stop ; brake and you stop fast.
            float lRun = RunInput;
            if (lRun < 0.05f || BrakeInput > 0.05f)
            {
                float lDecel = Mathf.Max(lCruise * COAST_DECEL, _speed * 0.9f) * (1f + BrakeInput * (BRAKE_DECEL / COAST_DECEL - 1f));
                if (Sliding || BellySliding) lDecel *= 0.35f;
                _speed = Mathf.MoveTowards(_speed, 0f, lDecel * pDt);
                _speed = Mathf.Clamp(_speed, 0f, _profile.SafetyMax);
                return;
            }
            float lTarget = lCruise * lRun;
            if (_speed < lTarget) _speed = Mathf.MoveTowards(_speed, lTarget, RUN_ACCEL * (Sliding ? 0.3f : 1f) * pDt);
            else if (_speed <= lCruise) _speed = Mathf.MoveTowards(_speed, lTarget, RUN_ACCEL * 0.8f * pDt);
            else
            {
                float lOver = _speed / lCruise - 1f;
                float lDrag = LightForm ? DRAG_LIGHT : DRAG + DRAG_OVER * lOver;
                _speed -= (_speed - lTarget) * (1f - Mathf.Exp(-lDrag * pDt));
            }
            _speed = Mathf.Clamp(_speed, 0f, _profile.SafetyMax);
        }

        // Before GO (and on the title) : jog anywhere on the plaza, relative to the view.
        private void FreeRoamSteer(float pDt)
        {
            Vector3 lInput = InputLocked ? Vector3.zero : _aimForward * HSInput.Forward + Vector3.Cross(_up, _aimForward).normalized * HSInput.Sideways;
            float lAmount = Mathf.Clamp01(lInput.magnitude);
            if (lAmount > 0.1f) _heading = Vector3.RotateTowards(_heading, lInput.normalized, 10f * pDt, 0f);
            _heading = Vector3.ProjectOnPlane(_heading, _up).normalized;
            float lTarget = lAmount * FREE_ROAM_SPEED * (HSInput.DashHeld && !InputLocked ? 1.6f : 1f);
            _speed = Mathf.MoveTowards(_speed, lTarget, (lTarget > _speed ? 60f : 70f) * pDt);
        }

        // Ray probe with a generous reach : fast feet stay on rolling ground and over crests.
        private bool ProbeGround(float pDt)
        {
            RaycastHit lHit;
            Vector3 lOrigin = transform.position + _up * 1.1f;
            float lReach = Mathf.Max(SNAP_REACH, _speed * pDt * 1.4f);
            if (Physics.Raycast(lOrigin, -_up, out lHit, 1.1f + lReach, GroundMask, QueryTriggerInteraction.Ignore) && Vector3.Dot(lHit.normal, _up) >= GROUND_MIN_DOT)
            {
                Stick(lHit, pDt);
                _groundLayer = lHit.collider.gameObject.layer;
                _carrier = lHit.collider.GetComponent<MovingPlatform>();
                return true;
            }
            return false;
        }

        private void Stick(RaycastHit pHit, float pDt)
        {
            transform.position = pHit.point;
            float lRate = 26f + _speed * 0.06f;
            _up = Vector3.Slerp(_up, pHit.normal, 1f - Mathf.Exp(-lRate * pDt)).normalized;
            _gravityUp = _up;
        }

        // Higher up the side than the floor : a wall ride, with its pose and its sparks.
        private void UpdateWallRide()
        {
            int lSide = 0;
            if (_layout != null && !_freeRoam)
            {
                float lAngle = Vector3.Angle(_up, _layout.field.Normal(Progress));
                if (lAngle > 18f) lSide = Vector3.Dot(_up, Vector3.Cross(_layout.field.Normal(Progress), _layout.Flow(Progress))) > 0f ? -1 : 1;
            }
            if (lSide != WallSide)
            {
                if (lSide != 0 && WallSide == 0)
                {
                    HSJuice.Play(JuiceId.WallAttach, Jx(transform.position));
                }
                WallSide = lSide;
            }
            AudioManager.SetLoop("sfx_wallrun_loop", WallRiding ? 0.5f : 0f, 1f + _speed * 0.002f);
            if (WallRiding && Random.value < 0.5f) FX.Sparks(transform.position, Color.white, 1, 10f, 0.5f, 0.25f);
        }

        // ================================================================ surface bookkeeping

        private int _groundLayer;
        // Time spent in the air on a section that carries gravity (loop, corkscrew, twist).
        private float _localAirTime;

        // Leaves the ground with the exact tangent velocity of the surface.
        private void TakeOff(TakeoffReason pReason)
        {
            _carrier = null;
            Vector3 lRight = Vector3.Cross(_up, _moveDir).normalized;
            _velocity = _moveDir * _speed + lRight * _slip;
            _gravityUp = _up;
            SetState(PlayerState.Air);
        }

        // ================================================================ drift

        private void UpdateDrift(float pDt)
        {
            if (State != PlayerState.Ground || _freeRoam)
            {
                EndDrift(false);
                return;
            }
            // Drift key + a real turn (keys, left stick, or a hard mouse swing) at a real speed : the drift starts.
            float lTurn = DriftSteer;
            if (Mathf.Abs(lTurn) < 0.35f && Mathf.Abs(MouseTurn) > 70f) lTurn = Mathf.Clamp(MouseTurn / 140f, -1f, 1f);
            if (!Drifting && _slideHeld && Mathf.Abs(lTurn) > 0.35f && _speed > _profile.cruise * 0.5f)
            {
                Drifting = true;
                DriftDirection = lTurn > 0f ? 1 : -1;
                DriftCharge = 0f;
                _driftEntrySpeed = _speed;
                HSJuice.Play(JuiceId.DriftStart, Jx(transform.position));
            }
            if (!Drifting) return;
            if (!_slideHeld)
            {
                EndDrift(true);
                return;
            }
            int lTierBefore = DriftTier;
            DriftCharge += pDt * (0.55f + Mathf.Max(Mathf.Abs(DriftSteer), Mathf.Clamp01(Mathf.Abs(MouseTurn) / 120f)) * 0.65f);
            if (DriftTier > lTierBefore)
            {
                HSJuice.Play(JuiceId.DriftTier, Jx(transform.position, DriftTier / 3f));
            }
            AudioManager.SetLoop("sfx_drift_loop", 0.45f, 0.9f + DriftTier * 0.12f);
        }

        // The turn asked for a drift : sidestep keys, or the gamepad's steering stick.
        private float DriftSteer => Mathf.Abs(Steer) >= Mathf.Abs(_turnStick) ? Steer : _turnStick;
        private float _turnStick;

        private void EndDrift(bool pRelease)
        {
            if (!Drifting) return;
            Drifting = false;
            AudioManager.SetLoop("sfx_drift_loop", 0f);
            if (!pRelease) { DriftCharge = 0f; return; }
            int lTier = DriftTier;
            DriftCharge = 0f;
            // Released while running where you look : the speed the drift ate comes back at once, and the charge on top.
            Vector3 lAim = Vector3.ProjectOnPlane(_aimForward, _up);
            float lAlign = lAim.sqrMagnitude > 0.0001f ? Mathf.InverseLerp(0.5f, 0.97f, Vector3.Dot(lAim.normalized, _heading)) : 0f;
            _speed = Mathf.Max(_speed, _driftEntrySpeed * Mathf.Lerp(0.85f, 1f, lAlign));
            if (lTier == 0) return;
            float[] lBoosts = { 0f, 0.6f, 1.1f, 1.6f };
            ApplyBoost(lBoosts[lTier] * _profile.cruise * Mathf.Lerp(0.35f, 1f, lAlign), _heading, 0f, lAlign > 0.9f && lTier >= 2 ? "DRIFT BOOST" : null, false, false, JuiceId.DriftBoost);
        }

        private void ReprojectProgress()
        {
            if (_layout == null) return;
            float lLateral;
            Progress = _layout.spine.ProjectNear(transform.position, Progress, out lLateral);
            Lateral = lLateral;
        }
    }
}
