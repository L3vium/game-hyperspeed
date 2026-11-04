using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Ground : the world flows along the route, A / D strafes without ever costing forward speed, speed is never lost
    // without a cause, crests launch you with their momentum, drift charges a boost.
    public partial class PlayerController
    {
        private const float SNAP_REACH = 0.35f;

        public enum TakeoffReason { Edge, Crest, Jump, Pad }

        private float _slip;
        private bool _onTerrain = true;

        public float LateralSpeed => OnSurface ? _slip : (_layout != null ? Vector3.Dot(_velocity, _layout.spine.Right(Progress)) : 0f);

        private void GroundStep(float pDt)
        {
            _lastGrounded = Time.time;
            Vector3 lFlow = _layout != null ? _layout.Flow(Progress) : _heading;
            Vector3 lForward = Vector3.ProjectOnPlane(lFlow, _up);
            lForward = lForward.sqrMagnitude > 0.0001f ? lForward.normalized : _heading;
            Vector3 lRight = Vector3.Cross(_up, lForward).normalized;

            UpdateDrift(pDt);
            Sliding = _slideHeld && !Drifting && !_freeRoam;

            if (_freeRoam)
            {
                FreeRoamSteer(pDt);
                lForward = _heading;
                _slip = 0f;
            }
            else
            {
                StrafeStep(lRight, pDt);
                SpeedStep(lForward, pDt);
            }

            _moveDir = lForward;
            _velocity = lForward * _speed + lRight * _slip;
            if (!_freeRoam && _velocity.sqrMagnitude > 1f)
                _heading = Vector3.ProjectOnPlane(lForward * _speed + lRight * _slip * 0.45f, _up).normalized;

            Move(_velocity * pDt);
            if (State != PlayerState.Ground && State != PlayerState.Finished) return;
            ReprojectProgress();

            if (!ProbeGround(pDt))
            {
                TakeOff(TakeoffReason.Edge);
                return;
            }

            // Crest : when the ground falls away faster than gravity can follow, you fly off with your momentum.
            if (_onTerrain && !_freeRoam && _layout != null)
            {
                float lStep = Mathf.Max(10f, _speed * 0.14f);
                float lCurve = _layout.field.Convexity(Progress, Lateral, lStep);
                if (lCurve > 0f && _speed * _speed * lCurve > Ballistics.G_FALL * Ballistics.Scale * 1.15f)
                {
                    TakeOff(TakeoffReason.Crest);
                    return;
                }
            }

            if (!_freeRoam && TryGrind()) return;
            if (Time.time - _jumpBufferedAt < JUMP_BUFFER && State == PlayerState.Ground) TryJump();
        }

        // Lateral speed that does not touch the forward one. Past the edge the banks push back : only a big overshoot leaves.
        private void StrafeStep(Vector3 pRight, float pDt)
        {
            float lMax = _profile.StrafeMax(_speed);
            float lInput = Steer;
            if (Drifting)
            {
                lInput = Mathf.Clamp(DriftDirection * 0.55f + Steer * 0.6f, -1f, 1f);
                lMax *= 1.25f;
            }
            if (State == PlayerState.Finished) lInput = Mathf.Clamp(-Lateral / 30f, -0.3f, 0.3f);

            if (_layout != null)
            {
                TerrainField lField = _layout.field;
                float lFromCenter = Lateral - lField.Offset(Progress);
                int lSide = lFromCenter < 0f ? -1 : 1;
                float lOver = Mathf.Abs(lFromCenter) - lField.HalfWidth(Progress);
                if (lOver > 0f)
                {
                    float lDepth = Mathf.Clamp01(lOver / lField.bermWidth);
                    bool lReallyFast = Hyper || LightForm;
                    if (lInput * lSide > 0f) lInput *= lReallyFast ? 1f - lDepth * 0.5f : 1f - lDepth * 0.9f;
                    lInput -= lSide * lDepth * (lReallyFast ? 0.5f : 1.2f);
                }
            }
            float lTarget = Mathf.Clamp(lInput, -1.2f, 1.2f) * lMax;
            _slip = Mathf.MoveTowards(_slip, lTarget, lMax / (Mathf.Abs(lTarget) > Mathf.Abs(_slip) ? 0.14f : 0.2f) * pDt);
            // The slope pushes you sideways : rolling ground shapes your line.
            _slip += Vector3.Dot(Vector3.down, pRight) * SLOPE_ACCEL * 0.55f * pDt;
        }

        // Nothing slows you but a climb or a crash. Boosts stack up, a slope feeds you.
        private void SpeedStep(Vector3 pForward, float pDt)
        {
            float lSlope = Vector3.Dot(pForward, Vector3.down);
            float lGain = lSlope * SLOPE_ACCEL * Ballistics.Scale * (lSlope < 0f ? 0.4f : 1f) * (Sliding ? 1.8f : 1f);
            float lCruise = _profile.cruise;
            float lTarget = lCruise * (1f + 0.1f * Mathf.Max(0f, HSInput.Forward));
            if (State == PlayerState.Finished)
            {
                // Past the line : hard brake inside the run-out.
                float lStop = Progress > _layout.finishS + lCruise * 2f ? 0f : lCruise * 0.35f;
                _speed = Mathf.MoveTowards(_speed, lStop, lCruise * 1.3f * pDt);
                return;
            }
            _speed += lGain * pDt;
            if (_speed < lTarget) _speed = Mathf.MoveTowards(_speed, lTarget, RUN_ACCEL * _profile.k * (Sliding ? 0.3f : 1f) * pDt);
            _speed = Mathf.Clamp(_speed, lCruise * 0.55f, _profile.max);
        }

        // Before GO (and on the title) : camera-relative jog anywhere on the plaza.
        private void FreeRoamSteer(float pDt)
        {
            Vector3 lCameraForward = CameraRig.Instance != null ? CameraRig.Instance.transform.forward : _heading;
            Vector3 lFlatForward = Vector3.ProjectOnPlane(lCameraForward, _up).normalized;
            Vector3 lFlatRight = Vector3.Cross(_up, lFlatForward).normalized;
            Vector3 lInput = InputLocked ? Vector3.zero : lFlatForward * HSInput.Forward + lFlatRight * HSInput.Steer;
            float lAmount = Mathf.Clamp01(lInput.magnitude);
            if (lAmount > 0.1f) _heading = Vector3.RotateTowards(_heading, lInput.normalized, 10f * pDt, 0f);
            _heading = Vector3.ProjectOnPlane(_heading, _up).normalized;
            float lTarget = lAmount * FREE_ROAM_SPEED * (HSInput.DashHeld && !InputLocked ? 1.6f : 1f);
            _speed = Mathf.MoveTowards(_speed, lTarget, (lTarget > _speed ? 60f : 70f) * pDt);
        }

        // Ray probe. Snap reach follows the step so fast feet stay on rolling ground.
        private bool ProbeGround(float pDt)
        {
            RaycastHit lHit;
            Vector3 lOrigin = transform.position + _up * 1.1f;
            float lReach = Mathf.Max(SNAP_REACH, _speed * pDt * 0.26f);
            if (Physics.Raycast(lOrigin, -_up, out lHit, 1.1f + lReach, GroundMask, QueryTriggerInteraction.Ignore)
                && Vector3.Dot(lHit.normal, _up) >= GROUND_MIN_DOT && lHit.normal.y >= GROUND_MIN_WORLD_DOT)
            {
                Stick(lHit, pDt);
                return true;
            }
            return false;
        }

        private void Stick(RaycastHit pHit, float pDt)
        {
            transform.position = pHit.point;
            _onTerrain = pHit.collider.gameObject.layer == Layers.DECK;
            // Facets stay visual : on the terrain the body follows the smooth height function.
            Vector3 lNormal = pHit.normal;
            if (_onTerrain && _layout != null) lNormal = _layout.field.Normal(Progress, Lateral, Mathf.Max(6f, 7f * _profile.k));
            _up = Vector3.Slerp(_up, lNormal, 1f - Mathf.Exp(-22f * pDt)).normalized;
            _gravityUp = _up;
        }

        // Leaves the ground with the exact tangent velocity of the surface : slope momentum is kept.
        private void TakeOff(TakeoffReason pReason)
        {
            Vector3 lRight = Vector3.Cross(_up, _moveDir).normalized;
            _velocity = _moveDir * _speed + lRight * _slip;
            float lClimb = _velocity.y - 0.5f * _velocity.magnitude;
            if (lClimb > 0f) _velocity.y -= lClimb;
            _gravityUp = Vector3.Dot(_up, Vector3.up) > 0.85f ? Vector3.up : _up;
            SetState(PlayerState.Air);
            float lFlight = Vector3.Dot(_velocity, Vector3.up);
            if (pReason == TakeoffReason.Crest && lFlight > 8f) Juice.Popup("AIR!", Color.white, 0.8f);
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
            float[] lBoosts = { 0f, 0.16f, 0.26f, 0.38f };
            string[] lNames = { "", "DRIFT!", "SUPER DRIFT!", "ULTRA DRIFT!!" };
            ApplyBoost(lBoosts[lTier] * _profile.cruise, _heading, 0f, lNames[lTier]);
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
