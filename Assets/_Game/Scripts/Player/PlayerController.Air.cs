using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Air : gravity profile, glide (hold jump), targeted dive (crouch), landing prediction + air edge guard,
    // kicker assist, landings, ledge catch and wall runs.
    public partial class PlayerController
    {
        private const float GLIDE_DURATION = 1.8f;
        private const float GLIDE_SINK = 10f;

        private float _glideTime;
        private float _assistGravity;
        private bool _padFlight;
        private bool _jumpRising;
        private float _lastDiveLanding = -10f;
        private int _diveChain;
        private float _airIntent;
        private Deck _predictedDeck;

        // ================================================================ air step

        private void AirStep(float pDt)
        {
            _airTime += pDt;
            _gravityUp = Vector3.Slerp(_gravityUp, Vector3.up, 1f - Mathf.Exp(-2.4f * pDt)).normalized;
            Vector3 lBefore = transform.position;

            float lUp = Vector3.Dot(_velocity, _gravityUp);
            if (lUp <= 0f) _jumpRising = false;
            UpdateGlide(lUp, pDt);
            float lGravity = Ballistics.Gravity(lUp);
            if (HSInput.JumpHeld && lUp > 0f && _jumpRising && State == PlayerState.Air) lGravity *= 0.75f;
            if (Gliding) lGravity = Ballistics.G_FALL * Mathf.Lerp(0.28f, 1f, Mathf.Clamp01(_glideTime / GLIDE_DURATION));
            if (Diving) lGravity *= 2.2f;
            if (Dashing) lGravity *= 0.15f;
            if (_assistGravity > 0f && !Gliding && !Diving && lUp < 0f) lGravity = _assistGravity;
            _velocity -= _gravityUp * lGravity * pDt;

            Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
            float lFlatSpeed = lFlat.magnitude;
            Vector3 lFlatDir = lFlatSpeed > 0.5f ? lFlat / lFlatSpeed : _heading;
            if (Gliding)
            {
                float lVertical = Vector3.Dot(_velocity, _gravityUp);
                if (lVertical < -GLIDE_SINK) _velocity += _gravityUp * (-GLIDE_SINK - lVertical);
                if (lFlatSpeed < RUN_SPEED + 15f) _velocity += lFlatDir * 6f * pDt;
            }
            if (Diving && lFlatSpeed < MAX_SPEED) _velocity += lFlatDir * 14f * pDt;

            if (State == PlayerState.Air && _layout != null)
            {
                Vector3 lRight = _layout.spine.Right(Progress);
                float lLateralMax = 1.1f * Mathf.Lerp(28f, 40f, Mathf.Clamp01((lFlatSpeed - 55f) / 145f));
                float lAccel = (Gliding ? 40f : 28f) * Steer;
                float lLateral = Vector3.Dot(_velocity, lRight);
                if (Mathf.Abs(lLateral + lAccel * pDt) < lLateralMax || lLateral * lAccel < 0f) _velocity += lRight * lAccel * pDt;
                AirGuard(lRight, pDt);
                if (!_padFlight) WallMagnet(pDt);
            }
            lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
            if (lFlat.sqrMagnitude > 1f) _heading = lFlat.normalized;
            else _heading = Vector3.ProjectOnPlane(_heading, _gravityUp).normalized;

            Move(_velocity * pDt);
            if (State != PlayerState.Air && State != PlayerState.Crash) return;
            UpdatePassThrough(lBefore);

            if (Vector3.Dot(_velocity, _gravityUp) <= 0f)
            {
                RaycastHit lHit;
                float lReach = 0.25f + Mathf.Max(0f, -Vector3.Dot(_velocity, _gravityUp)) * pDt;
                if (Physics.Raycast(transform.position + _gravityUp * 0.6f, -_gravityUp, out lHit, 0.6f + lReach, GroundMask, QueryTriggerInteraction.Ignore)
                    && Vector3.Dot(lHit.normal, _gravityUp) > 0.45f)
                {
                    Land(lHit.normal, lHit.point, DeckChunk.DeckOf(lHit.collider));
                    return;
                }
            }

            if (State == PlayerState.Air)
            {
                TryWallRun();
                if (State == PlayerState.Air) TryGrind();
            }
        }

        // Once per frame : where will this arc land ? Feeds the air guard, the fall-out check and the camera.
        private void PredictAir()
        {
            if (_layout == null) return;
            float lScale = Gliding ? 0.45f : Diving ? 2.2f : 1f;
            DeckHit lHit;
            float lTime, lS;
            HasLandingPrediction = Ballistics.PredictLanding(_layout, transform.position, _velocity, Progress, lScale, 3.2f, _passThrough || (Diving && _slideHeld), out lHit, out lTime, out lS);
            if (HasLandingPrediction)
            {
                LandingPoint = lHit.point;
                LandingTime = lTime;
                _predictedDeck = lHit.deck;
            }
            else _predictedDeck = null;
        }

        // Without a landing ahead, nudge toward the platform you are aiming at. Hard (never released) at high energy.
        private void AirGuard(Vector3 pRight, float pDt)
        {
            if (_padFlight || HasLandingPrediction)
            {
                _airIntent = 0f;
                return;
            }
            bool lHard = Hyper || BoostAmount > 0f || Dashing || State == PlayerState.Crash || _takeoffEdge == EdgeKind.Wall;
            int lOutward = Lateral < 0f ? -1 : 1;
            _airIntent = Steer * lOutward >= 0.6f ? _airIntent + pDt : 0f;
            if (!lHard && (_exitGranted || _airIntent >= 0.25f)) return;

            float lFlat = Mathf.Max(20f, Vector3.ProjectOnPlane(_velocity, Vector3.up).magnitude);
            float lFutureS = Progress + lFlat * 0.6f;
            float lBestCost = float.MaxValue;
            float lBestDelta = 0f;
            float lBestHeight = 0f;
            foreach (Deck lDeck in _layout.DecksAt(lFutureS))
            {
                if (lDeck.surface == DeckSurface.Lane || !lDeck.ContainsS(lFutureS)) continue;
                Vector3 lPoint = lDeck.SurfacePoint(lFutureS, 0f);
                if (lPoint.y > transform.position.y + 1.5f) continue;
                float lHalf = lDeck.Width(lFutureS) * 0.5f - 1.5f;
                float lDelta = lDeck.LateralOffset(lFutureS) - Lateral;
                if (Mathf.Abs(lDelta) < lHalf) lDelta = 0f;
                else lDelta -= Mathf.Sign(lDelta) * lHalf;
                float lCost = Mathf.Abs(lDelta) - 4f * Steer * Mathf.Sign(lDelta) + (transform.position.y - lPoint.y) * 0.05f;
                if (lCost < lBestCost)
                {
                    lBestCost = lCost;
                    lBestDelta = lDelta;
                    lBestHeight = lPoint.y;
                }
            }
            if (lBestCost == float.MaxValue || Mathf.Abs(lBestDelta) < 0.01f) return;
            float lVertical = Vector3.Dot(_velocity, Vector3.up);
            float lDrop = Mathf.Max(0f, transform.position.y - lBestHeight);
            float lTime = Mathf.Max(0.35f, (lVertical + Mathf.Sqrt(lVertical * lVertical + 2f * Ballistics.G_FALL * lDrop)) / Ballistics.G_FALL);
            float lLimit = lHard ? 60f : 25f;
            float lAccel = Mathf.Clamp(2f * lBestDelta / (lTime * lTime), -lLimit, lLimit);
            _velocity += pRight * lAccel * pDt;
        }

        // Ramp lips : the natural arc keeps the slope momentum ; only an arc that would miss the landing zone is fixed.
        private void KickerAssist(KickerDef pKicker)
        {
            DeckHit lHit;
            float lTime, lS;
            bool lLands = Ballistics.PredictLanding(_layout, transform.position, _velocity, Progress, 1f, 4f, false, out lHit, out lTime, out lS);
            float lZone0 = pKicker.zoneS0 + 8f;
            float lZone1 = pKicker.zoneS1 - 15f;
            if (lLands && lS >= lZone0 && lS <= lZone1) return;
            Deck lLanding = _layout.MainDeckAt(pKicker.zoneS0 + 10f);
            if (lLanding == null) return;
            float lFlat = Mathf.Max(20f, Vector3.ProjectOnPlane(_velocity, Vector3.up).magnitude);
            float lVertical = Vector3.Dot(_velocity, Vector3.up);
            if (!lLands || lS < lZone0)
            {
                float lHeight = lLanding.SurfacePoint(lZone0, 0f).y - transform.position.y;
                float lNeeded = Ballistics.SolveVerticalSpeed(lZone0 - Progress, lFlat, lHeight);
                _velocity += Vector3.up * Mathf.Clamp(lNeeded - lVertical, 0f, 15f);
            }
            else
            {
                float lTau = Mathf.Max(0.2f, (lZone1 - Progress) / lFlat);
                float lHeight = lLanding.SurfacePoint(lZone1, 0f).y - transform.position.y;
                _assistGravity = Mathf.Clamp(2f * (lVertical * lTau - lHeight) / (lTau * lTau), Ballistics.G_FALL, Ballistics.G_FALL * 1.8f);
            }
        }

        // ================================================================ glide and dive

        private void UpdateGlide(float pVertical, float pDt)
        {
            bool lWant = State == PlayerState.Air && HSInput.JumpHeld && pVertical < 0f && !Diving && _airTime > 0.15f;
            if (lWant && !Gliding && _glideTime < GLIDE_DURATION) StartGlide();
            else if (!lWant && Gliding) StopGlide();
            if (Gliding) _glideTime += pDt;
        }

        private void StartGlide()
        {
            Gliding = true;
            _assistGravity = 0f;
            AudioManager.Play("sfx_glide_open", 0.6f);
        }

        private void StopGlide()
        {
            if (!Gliding) return;
            Gliding = false;
            AudioManager.SetLoop("sfx_glide_loop", 0f);
        }

        // Crouch in the air : plunge straight to the platform below (refused if there is nothing below).
        private void Dive()
        {
            Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
            float lDown = Mathf.Min(Vector3.Dot(_velocity, _gravityUp), -(45f + 0.2f * lFlat.magnitude));
            Vector3 lVelocity = lFlat + _gravityUp * lDown;
            if (_layout != null)
            {
                DeckHit lHit;
                float lTime, lS;
                if (!Ballistics.PredictLanding(_layout, transform.position, lVelocity, Progress, 2.2f, 3f, _slideHeld, out lHit, out lTime, out lS))
                {
                    FX.Puff(transform.position, Color.white, 4, 1f, 3f);
                    AudioManager.Play("ui_locked", 0.5f);
                    return;
                }
            }
            Diving = true;
            _diveStart = Time.time;
            if (Time.time - _lastDiveLanding > 1.2f) _diveChain = 0;
            StopGlide();
            _assistGravity = 0f;
            _padFlight = false;
            _velocity = lVelocity;
            AudioManager.Play("sfx_dive", 0.8f);
            Juice.FovKick(4f);
        }

        // ================================================================ landing

        private void Land(Vector3 pNormal, Vector3 pPoint, Deck pDeck)
        {
            float lImpact = -Vector3.Dot(_velocity, pNormal);
            Vector3 lTangent = Vector3.ProjectOnPlane(_velocity, pNormal);
            float lAngle = Vector3.Angle(_velocity, lTangent);
            _up = pNormal;
            _gravityUp = pNormal;
            transform.position = pPoint;
            _speed = lTangent.magnitude;
            if (_speed > 1f) _heading = lTangent.normalized;
            else _heading = Vector3.ProjectOnPlane(_heading, pNormal).normalized;
            _moveDir = _heading;
            if (pDeck != null) _deck = pDeck;
            else if (_layout != null)
            {
                DeckHit lHit;
                if (_layout.SurfaceBelow(pPoint + pNormal * 0.5f, Progress, 1f, 2f, false, out lHit)) _deck = lHit.deck;
            }
            if (_layout != null && _deck != null)
            {
                Vector3 lForward = DeckForward(_deck);
                _psi = Mathf.Clamp(Vector3.SignedAngle(lForward, _heading, _up), -30f, 30f);
            }
            _slip = 0f;
            _passThrough = false;
            bool lWasCrash = State == PlayerState.Crash;
            bool lWasDiving = Diving;
            Diving = false;
            SetState(_autoRun ? PlayerState.Finished : PlayerState.Ground);
            HasDoubleJump = true;
            HasAirDash = true;

            if (lWasDiving)
            {
                float lTiming = Time.time - _diveStart;
                bool lPerfect = lTiming < 0.28f;
                string lLabel = lPerfect ? "PERFECT!" : lTiming < 0.6f ? "GREAT!" : "GOOD";
                Color lColor = lPerfect ? UIFactory.YELLOW : lTiming < 0.6f ? UIFactory.CYAN : Color.white;
                float lBonus = Mathf.Min(30f, 0.35f * Mathf.Max(0f, lImpact)) * (lPerfect ? 1.3f : 1f);
                if (Time.time - _lastDiveLanding < 1.2f) _diveChain++;
                lBonus *= 1f + Mathf.Min(0.5f, _diveChain * 0.1f);
                _lastDiveLanding = Time.time;
                _speed = Mathf.Min(MAX_SPEED, _speed + lBonus);
                _boostTimer = Mathf.Max(_boostTimer, lPerfect ? 1f : 0.5f);
                Juice.Popup(_diveChain > 0 ? lLabel + " x" + (_diveChain + 1) : lLabel, lColor, lPerfect ? 1.4f : 1.1f);
                Juice.FovKick(lPerfect ? 8f : 5f);
                Juice.Pulse(lPerfect ? 0.8f : 0.4f);
                Juice.Rollback(lPerfect ? 7f : 4f);
                Juice.Shake(lPerfect ? 0.4f : 0.25f);
                FX.Shockwave(pPoint + pNormal * 0.1f, pNormal, lColor, 14f);
                FX.Sparks(pPoint, lColor, 30, 22f);
                AudioManager.Play(lPerfect ? "sfx_land_perfect" : "sfx_land_good", 0.9f);
                AddCombo();
            }
            else if (lAngle < 10f && lImpact < 15f && !lWasCrash && _airTime > 0.3f)
            {
                _speed = Mathf.Min(MAX_SPEED, _speed + 4f);
                Juice.Popup("SMOOTH", Color.white, 0.8f);
                FX.Puff(pPoint, Color.white, 4, 1.2f, 3f);
                AudioManager.Play("sfx_land", 0.6f);
            }
            else if (lImpact > 35f && !lWasCrash)
            {
                _speed *= 0.95f;
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
            ProbeGround(_deck, 1f);
        }

        // Shins hit the front face of a platform just below its top : climb onto it instead of bouncing off.
        private bool TryLedge(RaycastHit pHit)
        {
            if (Mathf.Abs(Vector3.Dot(pHit.normal, _gravityUp)) >= 0.35f || Vector3.Dot(_velocity, _gravityUp) > 4f) return false;
            Vector3 lOrigin = pHit.point + _gravityUp * 2.2f - pHit.normal * 0.7f;
            RaycastHit lTop;
            if (!Physics.Raycast(lOrigin, -_gravityUp, out lTop, 2.6f, GroundMask, QueryTriggerInteraction.Ignore)) return false;
            if (Vector3.Dot(lTop.normal, _gravityUp) < 0.7f) return false;
            if (Vector3.Dot(lTop.point - transform.position, _gravityUp) > 2f) return false;
            Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
            _velocity = lFlat * 0.92f;
            Land(lTop.normal, lTop.point, DeckChunk.DeckOf(lTop.collider));
            Juice.Popup("CLUTCH!", UIFactory.CYAN, 1f);
            return true;
        }

        // ================================================================ wall run

        private float _wallCooldown;
        private Vector3 _wallNormal;
        private Vector3 _lastWallNormal;
        private Vector3 _wallRunDir;
        private float _wallSpeed;
        private float _wallVertical;

        private void TryWallRun()
        {
            if (_wallCooldown > 0f || Speed < 14f || Vector3.Dot(_velocity, _gravityUp) > 14f) return;
            Vector3 lCenter = transform.position + _gravityUp * 1f;
            Vector3 lRight = Vector3.Cross(_gravityUp, _heading).normalized;
            for (int lSide = -1; lSide <= 1; lSide += 2)
            {
                RaycastHit lHit;
                if (!Physics.Raycast(lCenter, lRight * lSide, out lHit, 2.6f, GroundMask, QueryTriggerInteraction.Ignore)) continue;
                if (Mathf.Abs(Vector3.Dot(lHit.normal, _gravityUp)) > 0.35f) continue;
                if (DeckChunk.DeckOf(lHit.collider) != null) continue;
                if (Vector3.Dot(lHit.normal, _lastWallNormal) > 0.9f && _wallCooldown > -0.6f) continue;
                Vector3 lDir = Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(_velocity, lHit.normal), _gravityUp);
                if (lDir.sqrMagnitude < 25f) continue;

                _wallNormal = lHit.normal;
                _wallRunDir = lDir.normalized;
                _wallSpeed = Mathf.Max(lDir.magnitude, 40f);
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

        // Over a floorless stretch between walls, pull toward the closest wall so wall runs are easy to catch.
        private void WallMagnet(float pDt)
        {
            if (_layout == null || _wallCooldown > 0f) return;
            DeckHit lBelow;
            if (_layout.SurfaceBelow(transform.position, Progress, 0f, 8f, false, out lBelow)) return;
            Vector3 lRight = Vector3.Cross(_gravityUp, _heading).normalized;
            Vector3 lCenter = transform.position + _gravityUp;
            float lBest = 7f;
            int lSide = 0;
            for (int i = -1; i <= 1; i += 2)
            {
                RaycastHit lHit;
                if (Physics.Raycast(lCenter, lRight * i, out lHit, 7f, GroundMask, QueryTriggerInteraction.Ignore)
                    && Mathf.Abs(Vector3.Dot(lHit.normal, _gravityUp)) < 0.35f && DeckChunk.DeckOf(lHit.collider) == null && lHit.distance < lBest)
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
            if (!Physics.Raycast(lCenter, -_wallNormal, out lHit, 1.8f, GroundMask, QueryTriggerInteraction.Ignore)
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

            if (Physics.Raycast(transform.position + _gravityUp * 0.5f, -_gravityUp, out lHit, 0.7f, GroundMask, QueryTriggerInteraction.Ignore)
                && Vector3.Dot(lHit.normal, _gravityUp) > 0.5f)
                Land(lHit.normal, lHit.point, DeckChunk.DeckOf(lHit.collider));
        }

        private void LeaveWall(float pCooldown)
        {
            _lastWallNormal = _wallNormal;
            _wallCooldown = pCooldown;
            _takeoffY = transform.position.y;
            SetState(PlayerState.Air);
        }
    }
}
