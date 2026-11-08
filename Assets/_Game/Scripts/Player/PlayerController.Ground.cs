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

        public float LateralSpeed => OnSurface ? _slip : (_layout != null ? Vector3.Dot(_velocity, _layout.spine.Right(Progress)) : 0f);

        private void GroundStep(float pDt)
        {
            _lastGrounded = Time.time;
            Vector3 lForward = Vector3.ProjectOnPlane(_heading, _up);
            lForward = lForward.sqrMagnitude > 0.0001f ? lForward.normalized : _aimForward;

            UpdateDrift(pDt);
            Sliding = _slideHeld && !Drifting && !_freeRoam;
            BellySliding = !_freeRoam && !Drifting && AimPitch < DIVE_PITCH && Vector3.Dot(lForward, GravityDown()) > 0.08f;

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
                    float lTurn = Mathf.Lerp(11f, 5f, Speed01) * (Drifting ? 1.5f : 1f);
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
            if (!_freeRoam && CheckOffTrack(pDt)) return;

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
            float lMax = _profile.StrafeMax(_speed);
            float lInput = Steer;
            if (Drifting)
            {
                lInput = Mathf.Clamp(DriftDirection * 0.55f + Steer * 0.6f, -1f, 1f);
                lMax *= 1.35f;
            }
            float lTarget = lInput * lMax;
            _slip = Mathf.MoveTowards(_slip, lTarget, lMax / (Mathf.Abs(lTarget) > Mathf.Abs(_slip) ? 0.12f : 0.2f) * pDt);
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
                // Past the line : a real stop.
                _speed = Mathf.MoveTowards(_speed, 0f, Mathf.Max(lCruise * 1.6f, _speed * 1.4f) * pDt);
                return;
            }
            float lSlope = Vector3.Dot(pForward, GravityDown());
            _speed += lSlope * SLOPE_ACCEL * (lSlope < 0f ? 0.4f : 1f) * (Sliding || BellySliding ? 1.8f : 1f) * pDt;
            // A drift bites : it bleeds speed down to 70 % of what you came in with. Releasing it gives it all back.
            if (Drifting)
            {
                float lFloor = Mathf.Max(_driftEntrySpeed * 0.7f, lCruise * 0.6f);
                if (_speed > lFloor) _speed = Mathf.MoveTowards(_speed, lFloor, Mathf.Max(lCruise * 0.45f, _driftEntrySpeed * 0.35f) * pDt);
                _speed = Mathf.Clamp(_speed, lCruise * 0.55f, SpeedProfile.SAFETY_MAX);
                return;
            }
            float lTarget = lCruise * (1f + 0.1f * Mathf.Max(0f, HSInput.Forward));
            if (_speed < lTarget) _speed = Mathf.MoveTowards(_speed, lTarget, RUN_ACCEL * (Sliding ? 0.3f : 1f) * pDt);
            else
            {
                float lOver = _speed / lCruise - 1f;
                float lDrag = LightForm ? DRAG_LIGHT : DRAG + DRAG_OVER * lOver;
                _speed -= (_speed - lTarget) * (1f - Mathf.Exp(-lDrag * pDt));
            }
            _speed = Mathf.Clamp(_speed, lCruise * 0.55f, SpeedProfile.SAFETY_MAX);
        }

        // Before GO (and on the title) : jog anywhere on the plaza, relative to the view.
        private void FreeRoamSteer(float pDt)
        {
            Vector3 lInput = InputLocked ? Vector3.zero : _aimForward * HSInput.Forward + Vector3.Cross(_up, _aimForward).normalized * HSInput.Steer;
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
                    AudioManager.Play("sfx_wall_attach", 0.7f);
                    FX.Sparks(transform.position, Color.white, 16, 16f);
                }
                WallSide = lSide;
            }
            AudioManager.SetLoop("sfx_wallrun_loop", WallRiding ? 0.5f : 0f, 1f + _speed * 0.002f);
            if (WallRiding && Random.value < 0.5f) FX.Sparks(transform.position, Color.white, 1, 10f, 0.5f, 0.25f);
        }

        // ================================================================ recovery

        private int _groundLayer;
        private float _offTrackTime;
        private float _recoverTimer;

        // Running on scenery, or far out of the track : a moment later a bounce throws you back onto it.
        private bool CheckOffTrack(float pDt)
        {
            if (_layout == null || _autoRun) return false;
            bool lOff = _groundLayer == Layers.SCENERY || Mathf.Abs(Lateral - _layout.field.CenterLateral(Progress)) > _layout.field.TotalHalfWidth(Progress) + 20f;
            _offTrackTime = lOff ? _offTrackTime + pDt : 0f;
            if (_offTrackTime < 0.5f) return false;
            Recover();
            return true;
        }

        // A trampoline throw : a ballistic arc that lands on solid track ahead, the view turned toward it.
        public void Recover()
        {
            if (_layout == null) return;
            float lS = Progress + Mathf.Max(220f, Speed * 1.4f);
            for (int i = 0; i < 60 && lS < _layout.finishS; i++)
            {
                if (_layout.field.Solid(lS) && !_layout.spine.samples[_layout.spine.IndexOf(lS)].localGravity) break;
                lS += 60f;
            }
            Vector3 lTarget = _layout.GroundPoint(lS, _layout.RouteLateral(lS)) + _layout.field.Normal(lS) * 6f;
            Vector3 lDelta = lTarget - transform.position;
            Vector3 lFlat = Vector3.ProjectOnPlane(lDelta, Vector3.up);
            float lTime = Mathf.Clamp(lFlat.magnitude / Mathf.Max(Speed, _profile.cruise), 1.1f, 3f);
            const float GRAVITY = 135f;
            _velocity = lFlat / lTime + Vector3.up * (lDelta.y / lTime + 0.5f * GRAVITY * lTime);
            _recoverTimer = lTime * 0.85f;
            _offTrackTime = 0f;
            _gravityUp = Vector3.up;
            if (lFlat.sqrMagnitude > 1f)
            {
                _heading = lFlat.normalized;
                _aimForward = _heading;
            }
            AimPitch = 0f;
            Diving = false;
            _invulnerable = Mathf.Max(_invulnerable, 1f);
            HasDoubleJump = true;
            SetState(PlayerState.Air);
            AudioManager.Play("sfx_launch", 1f, 0.8f);
            FX.Shockwave(transform.position + Vector3.up, Vector3.up, UIFactory.CYAN, 60f, 0.5f);
            FX.Sparks(transform.position, UIFactory.CYAN, 40, 60f, 0.8f, 0.5f);
            Juice.FovKick(10f);
            Juice.Rollback(12f);
            Juice.Shake(0.4f);
            if (Autopilot.Enabled) Debug.Log("[HS-REC] recover s=" + Progress.ToString("F0") + " to=" + lS.ToString("F0") + " t=" + lTime.ToString("F1"));
        }

        // Leaves the ground with the exact tangent velocity of the surface.
        private void TakeOff(TakeoffReason pReason)
        {
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
            if (!Drifting && _slideHeld && Mathf.Abs(Steer) > 0.35f && _speed > 30f)
            {
                Drifting = true;
                DriftDirection = Steer > 0f ? 1 : -1;
                DriftCharge = 0f;
                _driftEntrySpeed = _speed;
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
            // Released while running where you look : the speed the drift ate comes back at once, and the charge on top.
            Vector3 lAim = Vector3.ProjectOnPlane(_aimForward, _up);
            float lAlign = lAim.sqrMagnitude > 0.0001f ? Mathf.InverseLerp(0.5f, 0.97f, Vector3.Dot(lAim.normalized, _heading)) : 0f;
            _speed = Mathf.Max(_speed, _driftEntrySpeed * Mathf.Lerp(0.85f, 1f, lAlign));
            if (lTier == 0) return;
            float[] lBoosts = { 0f, 0.3f, 0.55f, 0.8f };
            ApplyBoost(lBoosts[lTier] * _profile.cruise * Mathf.Lerp(0.35f, 1f, lAlign), _heading, 0f, lAlign > 0.9f && lTier >= 2 ? "DRIFT BOOST" : null);
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
