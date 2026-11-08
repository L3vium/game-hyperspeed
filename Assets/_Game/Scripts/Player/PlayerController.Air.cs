using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Air : heavy gravity, the mouse bends the flight, looking down dives. Gravity follows the track on the sections built
    // for it, so you always fall back on the surface of a loop.
    public partial class PlayerController
    {
        private const float DIVE_PITCH = -18f;

        private int _diveChain;
        private float _lastDiveLanding = -10f;

        // ================================================================ air step

        private void AirStep(float pDt)
        {
            _airTime += pDt;
            Vector3 lTargetUp = _layout != null ? _layout.GravityUp(Progress) : Vector3.up;
            float lUpRate = lTargetUp == Vector3.up ? 3.5f : 9f;
            _gravityUp = Vector3.Slerp(_gravityUp, lTargetUp, 1f - Mathf.Exp(-lUpRate * pDt)).normalized;

            float lVertical = Vector3.Dot(_velocity, _gravityUp);
            bool lRecovering = _recoverTimer > 0f;
            Diving = State == PlayerState.Air && AimPitch < DIVE_PITCH && !lRecovering;

            float lGravity = Ballistics.Gravity(lVertical);
            if (LightForm) lGravity *= 0.25f;
            if (Dashing) lGravity *= 0.2f;
            // The bounce back to the track is a clean arc : it lands where it was aimed.
            if (lRecovering)
            {
                lGravity = 135f;
                _gravityUp = Vector3.up;
            }
            _velocity -= _gravityUp * lGravity * pDt;

            if (State == PlayerState.Air && !lRecovering) AirControl(pDt);
            if (!lRecovering) AirDrag(pDt);

            Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
            if (lFlat.sqrMagnitude > 1f) _heading = lFlat.normalized;
            else _heading = Vector3.ProjectOnPlane(_heading, _gravityUp).normalized;

            Move(_velocity * pDt);
            if (State != PlayerState.Air && State != PlayerState.Crash) return;
            ReprojectProgress();

            if (Vector3.Dot(_velocity, _gravityUp) <= 0f)
            {
                RaycastHit lHit;
                float lReach = 0.25f + Mathf.Max(0f, -Vector3.Dot(_velocity, _gravityUp)) * pDt;
                if (Physics.Raycast(transform.position + _gravityUp * 0.6f, -_gravityUp, out lHit, 0.6f + lReach, GroundMask, QueryTriggerInteraction.Ignore)
                    && Vector3.Dot(lHit.normal, _gravityUp) > 0.12f)
                {
                    Land(lHit.normal, lHit.point, lHit.collider);
                    return;
                }
            }

            if (State == PlayerState.Air) TryGrind();
        }

        // The mouse bends the flight toward where you look, A / D adds a sidestep. Diving pulls the whole velocity down
        // the look direction, Shift pushes along it (see the thrust in Actions).
        private void AirControl(float pDt)
        {
            Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
            Vector3 lVerticalPart = _velocity - lFlat;
            float lFlatSpeed = lFlat.magnitude;
            Vector3 lAim = Vector3.ProjectOnPlane(_aimForward, _gravityUp);
            if (lAim.sqrMagnitude > 0.0001f && lFlatSpeed > 1f)
            {
                float lTurn = (LightForm ? 2.2f : 3.4f) * pDt;
                lFlat = Vector3.RotateTowards(lFlat, lAim.normalized * lFlatSpeed, lTurn, 0f);
            }
            if (Mathf.Abs(Steer) > 0.05f)
            {
                Vector3 lSide = Vector3.Cross(_gravityUp, lAim.sqrMagnitude > 0.0001f ? lAim.normalized : _heading).normalized;
                float lMax = _profile.StrafeMax(lFlatSpeed);
                float lNow = Vector3.Dot(lFlat, lSide);
                lFlat += lSide * (Mathf.MoveTowards(lNow, Steer * lMax, lMax / 0.25f * pDt) - lNow);
            }
            _velocity = lFlat + lVerticalPart;

            if (Diving)
            {
                // The steeper you look, the harder you fall : speed turns toward the look direction and grows.
                float lDepth = Mathf.InverseLerp(DIVE_PITCH, AIM_PITCH_MIN, AimPitch);
                float lSpeed = _velocity.magnitude;
                Vector3 lDirection = AimDirection;
                _velocity = Vector3.RotateTowards(_velocity, lDirection * lSpeed, (1.2f + lDepth * 2.4f) * pDt, 0f);
                _velocity += lDirection * (60f + lDepth * 150f) * pDt;
            }
        }

        // The air slows you as the ground does : extra speed fades the same way, only what you add keeps you up there.
        private void AirDrag(float pDt)
        {
            Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
            float lSpeed = lFlat.magnitude;
            float lCruise = _profile.cruise;
            if (lSpeed <= lCruise) return;
            float lDrag = LightForm ? DRAG_LIGHT : DRAG + DRAG_OVER * (lSpeed / lCruise - 1f);
            float lNew = lSpeed - (lSpeed - lCruise) * (1f - Mathf.Exp(-lDrag * pDt));
            _velocity += lFlat / lSpeed * (lNew - lSpeed);
        }

        // Once per frame : does this arc meet anything ? Feeds the fall-out check.
        private void PredictAir()
        {
            if (_layout == null) return;
            Vector3 lPosition = transform.position;
            Vector3 lVelocity = _velocity;
            const float STEP = 0.2f;
            HasLandingPrediction = false;
            for (float lT = 0f; lT < 3.2f; lT += STEP)
            {
                Vector3 lNext = lVelocity + -_gravityUp * Ballistics.G_FALL * STEP;
                Vector3 lDelta = (lVelocity + lNext) * 0.5f * STEP;
                RaycastHit lHit;
                if (Physics.Raycast(lPosition, lDelta.normalized, out lHit, lDelta.magnitude + 1f, GroundMask, QueryTriggerInteraction.Ignore))
                {
                    HasLandingPrediction = true;
                    LandingPoint = lHit.point;
                    LandingTime = lT;
                    return;
                }
                lPosition += lDelta;
                lVelocity = lNext;
            }
            // Anything solid ahead on the line and within reach of a dash still counts as hope.
            if (_layout.field.Solid(Progress + Mathf.Max(60f, Speed * 0.8f))) HasLandingPrediction = (_layout.spine.Evaluate(Progress).position - transform.position).sqrMagnitude < 260f * 260f;
        }

        // ================================================================ landing

        private void Land(Vector3 pNormal, Vector3 pPoint, Collider pCollider)
        {
            float lImpact = Mathf.Max(0f, -Vector3.Dot(_velocity, pNormal));
            Vector3 lTangent = Vector3.ProjectOnPlane(_velocity, pNormal);
            Vector3 lForward = lTangent.sqrMagnitude > 1f ? lTangent.normalized : Vector3.ProjectOnPlane(_heading, pNormal).normalized;
            // A dive feeds the run, but falling alone can never carry you to light speed.
            float lFlatNow = Vector3.ProjectOnPlane(_velocity, _gravityUp).magnitude;
            float lLanding = Mathf.Min(lTangent.magnitude, Mathf.Max(_profile.cruise * 2.2f, Diving ? 0f : lFlatNow));
            _up = pNormal;
            _gravityUp = pNormal;
            transform.position = pPoint;
            _speed = _freeRoam ? 0f : Mathf.Max(lLanding, _profile.cruise * 0.55f);
            _slip = 0f;
            if (!_freeRoam) _heading = lForward;
            _moveDir = lForward;

            bool lWasCrash = State == PlayerState.Crash;
            bool lWasDiving = Diving;
            Diving = false;
            _recoverTimer = 0f;
            SetState(_autoRun ? PlayerState.Finished : PlayerState.Ground);
            HasDoubleJump = true;
            _groundLayer = pCollider != null ? pCollider.gameObject.layer : 0;
            // The net under the world : it never holds you, it throws you straight back to the track.
            if (pCollider != null && pCollider.GetComponent<SafetyNet>() != null && !_freeRoam && !_autoRun)
            {
                Recover();
                return;
            }

            float lPower = Mathf.Clamp01(lImpact / 220f);
            Color lDust = RaceContext.Theme != null ? Color.Lerp(RaceContext.Theme.roadA, Color.white, 0.25f) : Color.white;
            if (lWasDiving)
            {
                float lBonus = Mathf.Min(_profile.cruise * 0.8f, 0.5f * lImpact);
                if (Time.time - _lastDiveLanding < 1.2f) _diveChain++;
                else _diveChain = 0;
                lBonus *= 1f + Mathf.Min(0.5f, _diveChain * 0.1f);
                _lastDiveLanding = Time.time;
                _speed = Mathf.Min(SpeedProfile.SAFETY_MAX, _speed + lBonus);
                _boostTimer = Mathf.Max(_boostTimer, 1f);
                Juice.FovKick(10f);
                Juice.Pulse(0.8f);
                Juice.Rollback(10f);
                AudioManager.Play("sfx_land_perfect", 0.9f);
            }
            if (lImpact > 22f && !lWasCrash)
            {
                // A real hit on the ground : ring, column of debris, dust.
                float lSize = Mathf.Lerp(14f, 80f, lPower);
                Juice.Shake(Mathf.Lerp(0.2f, 0.75f, lPower));
                FX.Shockwave(pPoint + pNormal * 0.3f, pNormal, Color.white, lSize, 0.4f);
                FX.Shockwave(pPoint + pNormal * 0.5f, pNormal, lDust, lSize * 0.6f, 0.3f);
                FX.Shards(pPoint, RaceContext.Theme != null ? RaceContext.Theme.roadSide : Color.gray, Mathf.RoundToInt(Mathf.Lerp(10f, 44f, lPower)), Mathf.Lerp(26f, 80f, lPower), Mathf.Lerp(1.6f, 3.6f, lPower), pNormal * Mathf.Lerp(20f, 70f, lPower));
                FX.Puff(pPoint, lDust, Mathf.RoundToInt(Mathf.Lerp(8f, 26f, lPower)), Mathf.Lerp(4f, 16f, lPower), Mathf.Lerp(8f, 30f, lPower));
                FX.Sparks(pPoint, lDust, Mathf.RoundToInt(Mathf.Lerp(16f, 70f, lPower)), Mathf.Lerp(30f, 90f, lPower), 0.8f, 0.5f);
                if (!lWasDiving) AudioManager.Play("sfx_land_heavy", 0.8f);
            }
            else
            {
                FX.Puff(pPoint, lDust, 6, 3f, 5f);
                AudioManager.Play("sfx_land", 0.6f);
            }
            OnLanded?.Invoke(lImpact);
            ReprojectProgress();
        }
    }
}
