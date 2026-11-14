using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Air : heavy gravity, the mouse bends the flight, looking down dives. Gravity follows the track on the sections built
    // for it, so you always fall back on the surface of a loop.
    public partial class PlayerController
    {
        private const float DIVE_PITCH = -18f;

        // Only the track and its props count as somewhere to land : scenery and the plane under the world do not.
        private const int TRACK_MASK = (1 << 0) | (1 << Layers.DECK) | (1 << Layers.PROP);
        // Set by a button jump : releasing the button early cuts the rise.
        private bool _jumpCut;

        private int _diveChain;
        private float _lastDiveLanding = -10f;

        // ================================================================ air step

        private void AirStep(float pDt)
        {
            _airTime += pDt;
            Vector3 lTargetUp = _layout != null ? _layout.GravityUp(Progress) : Vector3.up;
            // In the air inside a loop or a corkscrew : the surface pulls you back down onto it, harder the longer you
            // float, so a jump or a dash in a loop always lands. Only a really lost flight (several seconds) is a fall.
            _localAirTime = lTargetUp != Vector3.up ? _localAirTime + pDt : 0f;
            if (_localAirTime > 4.5f && !_freeRoam && !_autoRun)
            {
                _localAirTime = 0f;
                if (Autopilot.Enabled || Debug.isDebugBuild) Debug.Log("[HS-LD] out (loop air) s=" + Progress.ToString("F0"));
                Die();
                return;
            }
            // Thrown far off a loop or a twist : its pull no longer holds, the world's gravity takes over, and the view
            // turns with it toward the ground you are heading for.
            if (_layout != null && lTargetUp != Vector3.up)
            {
                float lFar = _layout.field.TotalHalfWidth(Progress) * 1.3f + 60f;
                if ((transform.position - _layout.spine.Evaluate(Progress).position).sqrMagnitude > lFar * lFar) lTargetUp = Vector3.up;
            }
            float lUpRate = lTargetUp == Vector3.up ? 3.5f : 9f;
            _gravityUp = Vector3.Slerp(_gravityUp, lTargetUp, 1f - Mathf.Exp(-lUpRate * pDt)).normalized;

            float lVertical = Vector3.Dot(_velocity, _gravityUp);
            Diving = State == PlayerState.Air && AimPitch < DIVE_PITCH;

            float lGravity = Ballistics.Gravity(lVertical);
            // A jump is as high as the button is held : let go early and the rise is cut short.
            if (_jumpCut && lVertical > 0f && !HSInput.JumpHeld && !InputLocked && !Autopilot.Enabled) lGravity *= 2.4f;
            if (lVertical <= 0f) _jumpCut = false;
            // A short float at the top of the arc : the jump reads soft, then it falls for real.
            if (Mathf.Abs(lVertical) < 14f && !Diving) lGravity *= 0.6f;
            if (LightForm) lGravity *= 0.25f;
            if (Dashing) lGravity *= 0.2f;
            // Loop magnet : after a short hop the surface draws you back, and moving away from it is cancelled.
            if (_localAirTime > 0.35f)
            {
                lGravity = Mathf.Max(lGravity, Ballistics.G_FALL) * (1f + (_localAirTime - 0.35f) * 2.5f);
                if (lVertical > 0f) _velocity -= _gravityUp * lVertical * (1f - Mathf.Exp(-4f * pDt));
            }
            _velocity -= _gravityUp * lGravity * pDt;

            if (State == PlayerState.Air) AirControl(pDt);
            AirDrag(pDt);

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
                    && (TrackHoles.Count == 0 || !TrackHoles.Inside(lHit.point))
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
                // The flight answers the aim almost like the ground does : a jump is never a fixed direction.
                float lTurn = (LightForm ? 3f : Mathf.Lerp(6.5f, 4.5f, Speed01)) * pDt;
                lFlat = Vector3.RotateTowards(lFlat, lAim.normalized * lFlatSpeed, lTurn, 0f);
            }
            if (Mathf.Abs(Steer) > 0.05f)
            {
                Vector3 lSide = Vector3.Cross(_gravityUp, lAim.sqrMagnitude > 0.0001f ? lAim.normalized : _heading).normalized;
                float lMax = _profile.StrafeMax(lFlatSpeed);
                float lNow = Vector3.Dot(lFlat, lSide);
                lFlat += lSide * (Mathf.MoveTowards(lNow, Steer * lMax, lMax / 0.12f * pDt) - lNow);
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
            HasRouteAhead = false;
            for (float lT = 0f; lT < 3.2f; lT += STEP)
            {
                // Inside a loop the pull follows the surface under each point of the arc, not the gravity of now.
                Vector3 lUpHere = _gravityUp;
                if (_localAirTime > 0f)
                {
                    float lLat;
                    float lS = _layout.spine.ProjectNear(lPosition, Progress, out lLat);
                    lUpHere = _layout.GravityUp(lS);
                }
                Vector3 lNext = lVelocity + -lUpHere * Ballistics.G_FALL * STEP;
                Vector3 lDelta = (lVelocity + lNext) * 0.5f * STEP;
                RaycastHit lHit;
                bool lFound = Physics.Raycast(lPosition, lDelta.normalized, out lHit, lDelta.magnitude + 1f, TRACK_MASK, QueryTriggerInteraction.Ignore);
                if (lFound && (TrackHoles.Count == 0 || !TrackHoles.Inside(lHit.point)))
                {
                    HasLandingPrediction = true;
                    LandingPoint = lHit.point;
                    LandingTime = lT;
                    return;
                }
                // A rope to catch or a ring to fly through on the way : a route in the air, not a fall.
                if (!HasRouteAhead && lT < 2.4f && RouteNear(lPosition + lDelta * 0.5f)) HasRouteAhead = true;
                lPosition += lDelta;
                lVelocity = lNext;
            }
            // Anything solid ahead on the line and within reach of a dash still counts as hope, but only for a runner
            // still above the track : one that fell through a hole or off a side is not saved by the floor it left.
            float lAbove = Vector3.Dot(transform.position - _layout.GroundPoint(Progress, Lateral), _layout.field.Normal(Progress));
            float lHope = 260f * Mathf.Sqrt(Mathf.Max(1f, _profile.scale));
            if (lAbove > -4f && _layout.field.Solid(Progress + Mathf.Max(60f, Speed * 0.8f)))
                HasLandingPrediction = (_layout.spine.Evaluate(Progress).position - transform.position).sqrMagnitude < lHope * lHope;
        }

        private bool RouteNear(Vector3 pPoint)
        {
            foreach (GrindRail lRail in GrindRail.All)
            {
                float lDistance, lRailDistance;
                if (lRail.FindNearest(pPoint, lRail.catchRadius * 1.4f, out lDistance, out lRailDistance)) return true;
            }
            for (int i = Interactable.All.Count - 1; i >= 0; i--)
            {
                Interactable lItem = Interactable.All[i];
                if (lItem == null || !(lItem is Ring || lItem is Launcher || lItem is BoostPad || lItem is JumpStrip)) continue;
                float lReach = 70f * Mathf.Sqrt(Mathf.Max(1f, _profile.scale));
                if ((lItem.transform.position - pPoint).sqrMagnitude < lReach * lReach) return true;
            }
            return false;
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
            // A dive flows into the ground : most of the fall's speed carries on along it, never a dead stop.
            if (Diving) lLanding = Mathf.Min(Mathf.Max(lTangent.magnitude, _velocity.magnitude * 0.85f), Mathf.Max(_profile.cruise * 2.4f, lFlatNow * 1.15f));
            _up = pNormal;
            _gravityUp = pNormal;
            transform.position = pPoint;
            _speed = _freeRoam ? 0f : Mathf.Max(lLanding, _profile.cruise * 0.55f);
            _slip = 0f;
            if (!_freeRoam) _heading = lForward;
            _moveDir = lForward;

            bool lWasCrash = State == PlayerState.Crash;
            bool lWasDiving = Diving;
            LastLandingWasDive = lWasDiving;
            if (lWasDiving) _diveSlideTimer = 0.45f;
            Diving = false;
            _jumpCut = false;
            SetState(_autoRun ? PlayerState.Finished : PlayerState.Ground);
            HasDoubleJump = true;
            _groundLayer = pCollider != null ? pCollider.gameObject.layer : 0;
            // The plane far under the world : touching it means you left the stage long ago.
            if (pCollider != null && pCollider.GetComponent<SafetyNet>() != null && !_freeRoam && !_autoRun)
            {
                if (Autopilot.Enabled || Debug.isDebugBuild) Debug.Log("[HS-LD] out (kill plane) s=" + Progress.ToString("F0"));
                Die();
                return;
            }

            float lPower = Mathf.Clamp01(lImpact / 220f);
            if (lWasDiving)
            {
                float lBonus = Mathf.Min(_profile.cruise * 0.8f, 0.5f * lImpact);
                if (Time.time - _lastDiveLanding < 1.2f) _diveChain++;
                else _diveChain = 0;
                lBonus *= 1f + Mathf.Min(0.5f, _diveChain * 0.1f);
                _lastDiveLanding = Time.time;
                _speed = Mathf.Min(_profile.SafetyMax, _speed + lBonus);
                _boostTimer = Mathf.Max(_boostTimer, 1f);
            }
            JuiceContext lJuice = new JuiceContext { position = pPoint, direction = _heading, normal = pNormal, intensity = lPower, size = 1f };
            // A dive lands as a kick ; a long drop hits hard ; a plain jump lands softly and keeps the run flowing.
            if (lWasDiving && !lWasCrash) HSJuice.Play(JuiceId.DiveLanding, lJuice);
            else if (lImpact > 90f && !lWasCrash) HSJuice.Play(JuiceId.LandHard, lJuice);
            else HSJuice.Play(JuiceId.Land, lJuice);
            OnLanded?.Invoke(lImpact);
            ReprojectProgress();
        }
    }
}
