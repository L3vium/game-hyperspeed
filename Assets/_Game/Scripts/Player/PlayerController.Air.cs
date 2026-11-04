using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Air : gravity profile, glide (hold jump), dive (crouch) straight down, the flight follows the route's turns,
    // landings keep the momentum. Walls : run along them, climb them head-on, slide down them.
    public partial class PlayerController
    {
        public enum WallMode { Run, Climb, Slide }

        private const float GLIDE_DURATION = 2.2f;
        private const float GLIDE_SINK = 12f;
        private const float WALL_RUN_TIME = 3f;

        private float _glideTime;
        private bool _padFlight;
        private bool _jumpRising;
        private float _lastDiveLanding = -10f;
        private int _diveChain;
        private float _airYaw;

        public WallMode Wall { get; private set; }

        // ================================================================ air step

        private void AirStep(float pDt)
        {
            _airTime += pDt;
            _gravityUp = Vector3.Slerp(_gravityUp, Vector3.up, 1f - Mathf.Exp(-2.4f * pDt)).normalized;

            float lUp = Vector3.Dot(_velocity, _gravityUp);
            if (lUp <= 0f) _jumpRising = false;
            UpdateGlide(lUp, pDt);
            float lGravity = Ballistics.Gravity(lUp);
            if (HSInput.JumpHeld && lUp > 0f && _jumpRising && State == PlayerState.Air) lGravity *= 0.75f;
            if (Gliding) lGravity = Ballistics.G_FALL * Ballistics.Scale * Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(_glideTime / GLIDE_DURATION));
            if (Diving) lGravity *= 2.6f;
            if (Dashing) lGravity *= 0.15f;
            _velocity -= _gravityUp * lGravity * pDt;

            if (_layout != null) FlowFlight(pDt);

            Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
            float lFlatSpeed = lFlat.magnitude;
            Vector3 lFlatDir = lFlatSpeed > 0.5f ? lFlat / lFlatSpeed : _heading;
            if (Gliding)
            {
                float lVertical = Vector3.Dot(_velocity, _gravityUp);
                if (lVertical < -GLIDE_SINK) _velocity += _gravityUp * (-GLIDE_SINK - lVertical);
            }
            if (Diving && lFlatSpeed < _profile.max) _velocity += lFlatDir * _profile.cruise * 0.35f * pDt;

            if (lFlat.sqrMagnitude > 1f) _heading = lFlat.normalized;
            else _heading = Vector3.ProjectOnPlane(_heading, _gravityUp).normalized;

            Move(_velocity * pDt);
            if (State != PlayerState.Air && State != PlayerState.Crash) return;
            ReprojectProgress();
            GroundFloorGuard();

            if (Vector3.Dot(_velocity, _gravityUp) <= 0f)
            {
                RaycastHit lHit;
                float lReach = 0.25f + Mathf.Max(0f, -Vector3.Dot(_velocity, _gravityUp)) * pDt;
                if (Physics.Raycast(transform.position + _gravityUp * 0.6f, -_gravityUp, out lHit, 0.6f + lReach, GroundMask, QueryTriggerInteraction.Ignore)
                    && Vector3.Dot(lHit.normal, _gravityUp) > 0.45f)
                {
                    Land(lHit.normal, lHit.point, lHit.collider);
                    return;
                }
            }

            if (State == PlayerState.Air)
            {
                TryWallRun();
                if (State == PlayerState.Air) TryGrind();
            }
        }

        // In flight the velocity turns with the route, A / D gives the same strong lateral as on the ground.
        private void FlowFlight(float pDt)
        {
            SpineSample lSample = _layout.spine.Evaluate(Progress);
            float lTurn = Mathf.DeltaAngle(_airYaw, lSample.yaw);
            _airYaw = lSample.yaw;
            if (Mathf.Abs(lTurn) > 0.0001f && !_padFlightLocked)
            {
                Vector3 lVertical = Vector3.up * _velocity.y;
                Vector3 lPlanar = _velocity - lVertical;
                _velocity = Quaternion.AngleAxis(lTurn, Vector3.up) * lPlanar + lVertical;
            }
            if (State != PlayerState.Air) return;

            Vector3 lRight = lSample.Right;
            float lLateral = Vector3.Dot(_velocity, lRight);
            float lAlong = Vector3.Dot(_velocity, lSample.Forward);
            float lMax = _profile.StrafeMax(Mathf.Abs(lAlong)) * (Gliding ? 1.25f : 1f) * (Diving ? 1.3f : 1f);
            float lNew = lLateral;
            if (Mathf.Abs(Steer) > 0.05f) lNew = Mathf.MoveTowards(lLateral, Steer * lMax, lMax / 0.22f * pDt);
            else if (!_padFlight) lNew = Mathf.MoveTowards(lLateral, 0f, lMax * 0.6f * pDt);

            // Over the void past the banks : a strong pull back unless you are really pushing out at speed.
            TerrainField lField = _layout.field;
            float lFromCenter = Lateral - lField.Offset(Progress);
            float lOver = Mathf.Abs(lFromCenter) - lField.HalfWidth(Progress);
            if (lOver > 0f)
            {
                int lSide = lFromCenter < 0f ? -1 : 1;
                float lDepth = Mathf.Clamp01(lOver / (lField.bermWidth + lField.skirtWidth));
                bool lPushing = Steer * lSide > 0.5f && (Hyper || LightForm);
                if (!lPushing) lNew -= lSide * lDepth * lMax * 2.4f * pDt;
            }
            _velocity += lRight * (lNew - lLateral);
        }

        // Fast arcs can get under the single-sided terrain : never stay below the ground.
        private void GroundFloorGuard()
        {
            if (_layout == null) return;
            if (!_layout.field.OverGround(Progress, Lateral)) return;
            float lGround = _layout.Ground(Progress, Lateral);
            if (transform.position.y < lGround - 1f && Vector3.Dot(_velocity, Vector3.up) <= 0f)
            {
                Vector3 lNormal = _layout.field.Normal(Progress, Lateral, 6f);
                Land(lNormal, new Vector3(transform.position.x, lGround, transform.position.z), null);
            }
        }

        // Once per frame : where will this arc land ? Feeds the fall-out check and the camera.
        private void PredictAir()
        {
            if (_layout == null) return;
            float lScale = Gliding ? 0.45f : Diving ? 2.6f : 1f;
            SpineSample lSample = _layout.spine.Evaluate(Progress);
            float lAlong = Vector3.Dot(_velocity, lSample.Forward);
            float lLateral = Vector3.Dot(_velocity, lSample.Right);
            Vector3 lPoint;
            float lTime;
            HasLandingPrediction = Ballistics.PredictLanding(_layout, Progress, Lateral, transform.position.y, lAlong, lLateral, _velocity.y, lScale, 7f, out lPoint, out lTime);
            if (HasLandingPrediction)
            {
                LandingPoint = lPoint;
                LandingTime = lTime;
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
            AudioManager.Play("sfx_glide_open", 0.6f);
        }

        private void StopGlide()
        {
            if (!Gliding) return;
            Gliding = false;
            AudioManager.SetLoop("sfx_glide_loop", 0f);
        }

        // Crouch in the air : plunge. Always allowed, always fast.
        private void Dive()
        {
            Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
            float lPlunge = Mathf.Max(120f, 0.8f * lFlat.magnitude);
            float lDown = Mathf.Min(Vector3.Dot(_velocity, _gravityUp), -lPlunge);
            Diving = true;
            _diveStart = Time.time;
            if (Time.time - _lastDiveLanding > 1.2f) _diveChain = 0;
            StopGlide();
            _padFlight = false;
            _velocity = lFlat + _gravityUp * lDown;
            AudioManager.Play("sfx_dive", 0.8f);
            Juice.FovKick(6f);
            Juice.Rollback(4f);
        }

        // ================================================================ landing

        private void Land(Vector3 pNormal, Vector3 pPoint, Collider pCollider)
        {
            float lImpact = -Vector3.Dot(_velocity, pNormal);
            Vector3 lTangent = Vector3.ProjectOnPlane(_velocity, pNormal);
            float lAngle = Vector3.Angle(_velocity, lTangent);
            _up = pNormal;
            _gravityUp = pNormal;
            transform.position = pPoint;
            _onTerrain = pCollider == null || pCollider.gameObject.layer == Layers.DECK;

            Vector3 lFlow = _layout != null ? _layout.Flow(Progress) : _heading;
            Vector3 lForward = Vector3.ProjectOnPlane(lFlow, pNormal).normalized;
            Vector3 lRight = Vector3.Cross(pNormal, lForward).normalized;
            _speed = _freeRoam ? 0f : Mathf.Max(Vector3.Dot(lTangent, lForward), _profile.cruise * 0.55f);
            _slip = _freeRoam ? 0f : Vector3.Dot(lTangent, lRight);
            if (!_freeRoam) _heading = lForward;
            _moveDir = lForward;

            bool lWasCrash = State == PlayerState.Crash;
            bool lWasDiving = Diving;
            Diving = false;
            _padFlightLocked = false;
            SetState(_autoRun ? PlayerState.Finished : PlayerState.Ground);
            HasDoubleJump = true;
            HasAirDash = true;

            if (lWasDiving)
            {
                float lTiming = Time.time - _diveStart;
                bool lPerfect = lTiming < 0.45f;
                string lLabel = lPerfect ? "PERFECT!" : lTiming < 1.1f ? "GREAT!" : "DIVE!";
                Color lColor = lPerfect ? UIFactory.YELLOW : lTiming < 1.1f ? UIFactory.CYAN : Color.white;
                float lBonus = Mathf.Min(_profile.cruise * 0.45f, 0.3f * Mathf.Max(0f, lImpact)) * (lPerfect ? 1.3f : 1f);
                if (Time.time - _lastDiveLanding < 1.2f) _diveChain++;
                lBonus *= 1f + Mathf.Min(0.5f, _diveChain * 0.1f);
                _lastDiveLanding = Time.time;
                _speed = Mathf.Min(_profile.max, _speed + lBonus);
                _boostTimer = Mathf.Max(_boostTimer, lPerfect ? 1.2f : 0.7f);
                Juice.Popup(_diveChain > 0 ? lLabel + " x" + (_diveChain + 1) : lLabel, lColor, lPerfect ? 1.4f : 1.1f);
                Juice.FovKick(lPerfect ? 10f : 7f);
                Juice.Pulse(lPerfect ? 0.8f : 0.5f);
                Juice.Rollback(lPerfect ? 9f : 6f);
                Juice.Shake(lPerfect ? 0.55f : 0.35f);
                FX.Shockwave(pPoint + pNormal * 0.3f, pNormal, lColor, 34f, 0.45f);
                FX.Sparks(pPoint, lColor, 40, 45f);
                FX.Shards(pPoint, RaceContext.Theme != null ? RaceContext.Theme.roadSide : Color.gray, 16, 34f, 1.8f, Vector3.up * 30f);
                AudioManager.Play(lPerfect ? "sfx_land_perfect" : "sfx_land_good", 0.9f);
                AddCombo();
            }
            else if (lAngle < 12f && lImpact < 18f && !lWasCrash && _airTime > 0.3f)
            {
                _speed = Mathf.Min(_profile.max, _speed + _profile.cruise * 0.05f);
                Juice.Popup("SMOOTH", Color.white, 0.8f);
                FX.Puff(pPoint, Color.white, 4, 1.8f, 4f);
                AudioManager.Play("sfx_land", 0.6f);
            }
            else if (lImpact > 35f && !lWasCrash)
            {
                Juice.Shake(0.3f);
                FX.Puff(pPoint, Color.white, 8, 3f, 8f);
                FX.Shockwave(pPoint + pNormal * 0.3f, pNormal, Color.white, 16f, 0.3f);
                AudioManager.Play("sfx_land_heavy", 0.8f);
            }
            else
            {
                FX.Puff(pPoint, Color.white, 4, 1.8f, 4f);
                AudioManager.Play("sfx_land", 0.6f);
            }
            OnLanded?.Invoke(lImpact);
            ReprojectProgress();
        }

        // ================================================================ walls

        private float _wallCooldown;
        private Vector3 _wallNormal;
        private Vector3 _lastWallNormal;
        private Vector3 _wallRunDir;
        private float _wallSpeed;
        private float _wallVertical;

        private static bool IsWallSurface(Collider pCollider, Vector3 pNormal)
        {
            return pCollider.gameObject.layer == Layers.PROP && Mathf.Abs(pNormal.y) < 0.4f;
        }

        // Contact with a block face : head-on you climb it, at an angle you run along it. At light speed it breaks.
        private bool EnterWall(RaycastHit pHit)
        {
            if (_wallCooldown > 0f || !IsWallSurface(pHit.collider, pHit.normal)) return false;
            if (_profile.worldBreaker && (Hyper || LightForm))
            {
                Monolith lBlock = pHit.collider.GetComponent<Monolith>();
                if (lBlock != null)
                {
                    lBlock.Smash(_heading, Speed);
                    Juice.Hitstop(0.05f);
                    Juice.Shake(0.5f);
                    Juice.ImpactFrame(0.7f);
                    AddCombo();
                    return true;
                }
            }
            Vector3 lVelocity = OnSurface ? _moveDir * _speed + Vector3.Cross(_up, _moveDir).normalized * _slip : _velocity;
            Vector3 lFlat = Vector3.ProjectOnPlane(lVelocity, Vector3.up);
            float lSpeed = lFlat.magnitude;
            if (lSpeed < 6f) return false;
            float lHeadOn = -Vector3.Dot(lFlat / lSpeed, pHit.normal);
            _wallNormal = Vector3.ProjectOnPlane(pHit.normal, Vector3.up).normalized;
            Vector3 lRight = Vector3.Cross(Vector3.up, _heading).normalized;
            WallSide = Vector3.Dot(-_wallNormal, lRight) >= 0f ? 1 : -1;
            _gravityUp = Vector3.up;
            Diving = false;
            StopGlide();
            HasDoubleJump = true;
            HasAirDash = true;
            if (lHeadOn > 0.55f)
            {
                Wall = WallMode.Climb;
                _wallSpeed = Mathf.Max(lSpeed * 0.8f, 60f * _profile.k);
                _wallRunDir = _heading;
                _wallTopSpeed = lSpeed;
                Juice.Popup("CLIMB!", UIFactory.CYAN, 0.9f);
                Juice.Rollback(4f);
            }
            else
            {
                Wall = WallMode.Run;
                Vector3 lAlong = Vector3.ProjectOnPlane(lFlat, _wallNormal);
                _wallRunDir = lAlong.normalized;
                _wallSpeed = Mathf.Max(lAlong.magnitude, _profile.cruise * 0.7f);
                _wallVertical = Mathf.Max(lVelocity.y, 0f) * 0.35f + 4f + _profile.k * 3f;
            }
            SetState(PlayerState.WallRun);
            AudioManager.Play("sfx_wall_attach", 0.7f);
            FX.Sparks(pHit.point, Color.white, 16, 16f);
            return true;
        }

        private float _wallTopSpeed;

        private void TryWallRun()
        {
            if (_wallCooldown > 0f || Speed < 14f || Vector3.Dot(_velocity, _gravityUp) > 30f * Ballistics.Scale) return;
            Vector3 lCenter = transform.position + _gravityUp;
            Vector3 lRight = Vector3.Cross(_gravityUp, _heading).normalized;
            float lReach = 2.6f + Mathf.Abs(Vector3.Dot(_velocity, lRight)) * 0.03f;
            for (int lSide = -1; lSide <= 1; lSide += 2)
            {
                RaycastHit lHit;
                if (!Physics.Raycast(lCenter, lRight * lSide, out lHit, lReach, 1 << Layers.PROP, QueryTriggerInteraction.Ignore)) continue;
                if (!IsWallSurface(lHit.collider, lHit.normal)) continue;
                if (Vector3.Dot(lHit.normal, _lastWallNormal) > 0.9f && _wallCooldown > -0.6f) continue;
                if (EnterWall(lHit)) return;
            }
        }

        private void WallStep(float pDt)
        {
            switch (Wall)
            {
                case WallMode.Climb: ClimbStep(pDt); break;
                case WallMode.Run: RunStep(pDt); break;
                default: SlideStep(pDt); break;
            }
        }

        private void ClimbStep(float pDt)
        {
            float lBoost = _boostTimer > 0f ? 1.2f : 1f;
            _velocity = Vector3.up * _wallSpeed * lBoost;
            _heading = -_wallNormal;
            transform.position += _velocity * pDt;
            // Slide along the face with A / D.
            Vector3 lAlong = Vector3.Cross(Vector3.up, -_wallNormal).normalized;
            transform.position += lAlong * Steer * _profile.StrafeMax(_wallSpeed) * 0.5f * pDt;

            RaycastHit lHit;
            if (Physics.Raycast(transform.position + Vector3.up * 1.2f + _wallNormal * 1f, -_wallNormal, out lHit, 3.4f, 1 << Layers.PROP, QueryTriggerInteraction.Ignore))
            {
                transform.position = lHit.point + _wallNormal * 0.55f - Vector3.up * 1.2f;
                if (Random.value < 0.3f) FX.Sparks(lHit.point, Color.white, 1, 8f, 0.3f, 0.2f);
                AudioManager.SetLoop("sfx_wallrun_loop", 0.5f, 1.2f);
                return;
            }
            // Over the top : vault with all your speed.
            Vector3 lFlow = _layout != null ? _layout.Flow(Progress) : _heading;
            _velocity = lFlow * Mathf.Max(_wallTopSpeed, _profile.cruise) + Vector3.up * 16f * Ballistics.Scale;
            _heading = lFlow;
            transform.position += lFlow * 1.5f + Vector3.up * 1.5f;
            Juice.Popup("VAULT!", UIFactory.YELLOW, 1.1f);
            Juice.FovKick(6f);
            AddCombo();
            LeaveWall(0.3f);
            _airYaw = _layout != null ? _layout.spine.Evaluate(Progress).yaw : 0f;
        }

        private void RunStep(float pDt)
        {
            if (StateTime > WALL_RUN_TIME)
            {
                Wall = WallMode.Slide;
                return;
            }
            float lGravity = StateTime > WALL_RUN_TIME * 0.7f ? 34f : 7f;
            _wallVertical -= lGravity * pDt;
            _wallSpeed = Mathf.Min(_profile.max, _wallSpeed + _profile.cruise * 0.06f * pDt);
            _velocity = _wallRunDir * _wallSpeed + Vector3.up * _wallVertical;
            _heading = _wallRunDir;
            Move(_velocity * pDt);
            if (State != PlayerState.WallRun) return;
            ReprojectProgress();
            WallContact(pDt, 2.2f);
        }

        private void SlideStep(float pDt)
        {
            _wallVertical = Mathf.Max(_wallVertical - 40f * pDt, -22f);
            _wallSpeed = Mathf.Max(_profile.cruise * 0.55f, _wallSpeed - _profile.cruise * 0.1f * pDt);
            _velocity = _wallRunDir * _wallSpeed + Vector3.up * _wallVertical;
            _heading = _wallRunDir;
            Move(_velocity * pDt);
            if (State != PlayerState.WallRun) return;
            ReprojectProgress();
            WallContact(pDt, 2.2f);
        }

        private void WallContact(float pDt, float pReach)
        {
            RaycastHit lHit;
            Vector3 lCenter = transform.position + Vector3.up;
            if (!Physics.Raycast(lCenter, -_wallNormal, out lHit, pReach, 1 << Layers.PROP, QueryTriggerInteraction.Ignore) || Mathf.Abs(lHit.normal.y) > 0.45f)
            {
                LeaveWall(0.15f);
                _airYaw = _layout != null ? _layout.spine.Evaluate(Progress).yaw : 0f;
                return;
            }
            _wallNormal = Vector3.ProjectOnPlane(lHit.normal, Vector3.up).normalized;
            _wallRunDir = Vector3.ProjectOnPlane(_wallRunDir, _wallNormal).normalized;
            transform.position = lHit.point + _wallNormal * 0.55f - Vector3.up;
            AudioManager.SetLoop("sfx_wallrun_loop", 0.5f, 1f + _wallSpeed * 0.002f);
            if (Random.value < 0.4f) FX.Sparks(lHit.point, Color.white, 1, 8f, 0.3f, 0.2f);

            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out lHit, 0.8f, GroundMask, QueryTriggerInteraction.Ignore) && lHit.normal.y > 0.5f)
            {
                _velocity = _wallRunDir * _wallSpeed;
                Land(lHit.normal, lHit.point, lHit.collider);
            }
        }

        private void LeaveWall(float pCooldown)
        {
            _lastWallNormal = _wallNormal;
            _wallCooldown = pCooldown;
            SetState(PlayerState.Air);
        }
    }
}
