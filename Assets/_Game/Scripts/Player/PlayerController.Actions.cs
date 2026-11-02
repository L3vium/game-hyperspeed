using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Player actions and external impulses : jumps, dash, punch, pads, rings, boosts, crashes, combo.
    public partial class PlayerController
    {
        private void TryJump()
        {
            bool lCoyote = State == PlayerState.Air && Time.time - _lastGrounded < COYOTE_TIME && Vector3.Dot(_velocity, _gravityUp) <= 1f;
            if (State == PlayerState.Ground || lCoyote)
            {
                Vector3 lRight = Vector3.Cross(_up, _moveDir).normalized;
                _velocity = _moveDir * _speed + lRight * _slip + _up * JUMP_VELOCITY;
                _gravityUp = Vector3.Dot(_up, Vector3.up) > 0.85f ? Vector3.up : _up;
                _jumpBufferedAt = -10f;
                _lastGrounded = -10f;
                _jumpRising = true;
                _takeoffY = transform.position.y;
                if (Drifting) EndDrift(true);
                // Jumping toward a lip on purpose counts as intent for the air guard.
                if (_deck != null)
                {
                    int lSide = Lateral < 0f ? -1 : 1;
                    _takeoffEdge = _deck.Edge(Progress, lSide);
                    _exitGranted = _takeoffEdge == EdgeKind.Lip && Steer * lSide >= 0.6f;
                }
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
                _jumpRising = true;
                LeaveWall(0.25f);
                HasDoubleJump = true;
                HasAirDash = true;
                AudioManager.Play("sfx_walljump", 0.9f);
                FX.Sparks(transform.position + _gravityUp, Color.white, 16, 14f);
                Juice.Popup(lGood ? "WALL JUMP!" : "wall jump", lGood ? UIFactory.CYAN : Color.white, lGood ? 1.1f : 0.8f);
                if (lGood) { Juice.FovKick(5f); AddCombo(); }
                OnJumped?.Invoke();
            }
            else if (State == PlayerState.Grind || State == PlayerState.Zip)
            {
                _velocity = _heading * (_speed + 4f) + _up * JUMP_VELOCITY;
                _railCooldown = 0.35f;
                _jumpBufferedAt = -10f;
                _jumpRising = true;
                _takeoffY = transform.position.y;
                _takeoffEdge = EdgeKind.Lip;
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
                StopGlide();
                _assistGravity = 0f;
                _jumpRising = true;
                _jumpBufferedAt = -10f;
                AudioManager.Play("sfx_doublejump", 0.85f);
                FX.Shockwave(transform.position, _gravityUp, Color.white, 5f, 0.25f);
                OnDoubleJumped?.Invoke();
            }
        }

        private void Dash()
        {
            if (State == PlayerState.Crash || State == PlayerState.Frozen || State == PlayerState.Dead) return;
            bool lAir = State == PlayerState.Air;
            if (lAir && !HasAirDash) return;
            if (lAir) HasAirDash = false;
            _dashTimer = DASH_TIME;
            _dashCooldown = 0.55f;

            // Dashing helps but never pushes you into hyper speed on its own.
            float lCap = HYPER_ENTER - 10f;
            if (State == PlayerState.Ground || State == PlayerState.Grind) _speed = Mathf.Max(_speed, Mathf.Min(lCap, _speed + DASH_BOOST));
            else if (State == PlayerState.WallRun) _wallSpeed = Mathf.Max(_wallSpeed, Mathf.Min(lCap, _wallSpeed + DASH_BOOST));
            else
            {
                Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
                Vector3 lDir = lFlat.sqrMagnitude > 1f ? lFlat.normalized : _heading;
                float lNew = Mathf.Max(lFlat.magnitude, Mathf.Min(lCap, lFlat.magnitude + DASH_BOOST));
                _velocity = lDir * lNew + _gravityUp * Mathf.Max(2f, Vector3.Dot(_velocity, _gravityUp) * 0.2f);
                Diving = false;
                StopGlide();
            }
            AudioManager.Play("sfx_dash", 0.85f);
            Juice.FovKick(6f);
            Juice.Rollback(4f);
            Juice.Pulse(0.4f);
            Juice.Shake(0.12f);
            FX.Sparks(transform.position + CameraUpRaw, Color.white, 14, 16f);
            OnDashed?.Invoke();
        }

        private void Punch()
        {
            _punchTimer = 0.24f;
            _punchCooldown = 0.32f;
            _punchHit = false;
            AudioManager.Play("sfx_punch_whoosh", 0.7f);
            OnPunched?.Invoke();
        }

        // Jump pad flight : exact solved arc to the target, the air guard and the kicker assist stay out of it.
        public void LaunchTo(Vector3 pVelocity, Vector3 pTarget)
        {
            _velocity = pVelocity;
            _gravityUp = Vector3.up;
            _takeoffY = transform.position.y;
            _takeoffEdge = EdgeKind.Lip;
            _padFlight = true;
            _assistGravity = 0f;
            Diving = false;
            StopGlide();
            SetState(PlayerState.Air);
            _padFlight = true;
            _jumpRising = false;
            HasDoubleJump = true;
            HasAirDash = true;
            _glideTime = 0f;
            _boostTimer = Mathf.Max(_boostTimer, 0.4f);
            AudioManager.Play("sfx_launch", 0.9f);
            Juice.Popup("LAUNCH!", UIFactory.YELLOW, 1.1f);
            Juice.FovKick(6f);
            Juice.Shake(0.2f);
            AddCombo();
        }

        public void ApplyBoost(float pBoost, Vector3 pDirection, float pAlign, string pLabel)
        {
            if (State == PlayerState.Ground || State == PlayerState.Finished)
            {
                Vector3 lDir = Vector3.ProjectOnPlane(pDirection, _up).normalized;
                if (pAlign > 0f && lDir.sqrMagnitude > 0.1f) _psi = Mathf.Lerp(_psi, 0f, pAlign);
                _speed = Mathf.Min(MAX_SPEED, _speed + pBoost);
            }
            else if (State == PlayerState.Grind || State == PlayerState.Zip) _speed = Mathf.Min(MAX_SPEED, _speed + pBoost);
            else if (State == PlayerState.WallRun) _wallSpeed += pBoost;
            else _velocity += pDirection.normalized * pBoost;

            _boostTimer = Mathf.Max(_boostTimer, 1.2f);
            AudioManager.Play("sfx_boost", 0.95f);
            Juice.FovKick(7f);
            Juice.Rollback(6f);
            Juice.Pulse(0.6f);
            Juice.Shake(0.2f);
            Juice.Vignette(UIFactory.CYAN, 0.6f);
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
            _glideTime = 0f;
            _ringCombo++;
            _ringComboTimer = 2.5f;
            _boostTimer = Mathf.Max(_boostTimer, 0.7f);
            float[] lScale = { 1f, 1.122f, 1.26f, 1.335f, 1.498f, 1.682f, 1.888f, 2f };
            AudioManager.Play("sfx_ring", 0.95f, lScale[Mathf.Min(_ringCombo - 1, lScale.Length - 1)], 0f);
            Juice.Popup(_ringCombo > 1 ? "RING x" + _ringCombo : "RING!", UIFactory.YELLOW, 1f + Mathf.Min(_ringCombo, 5) * 0.08f);
            Juice.FovKick(5f);
            Juice.Rollback(4f);
            Juice.Pulse(0.5f);
            FX.Burst(transform.position + _gravityUp, pDirection, UIFactory.YELLOW, 16f);
            AddCombo();
        }

        public void Crash(Vector3 pPoint, string pLabel = "OUCH!")
        {
            if (Invulnerable || CheatInvincible || State == PlayerState.Crash || State == PlayerState.Frozen || State == PlayerState.Finished || State == PlayerState.Dead) return;
            Vector3 lAway = transform.position - pPoint;
            lAway = Vector3.ProjectOnPlane(lAway, _gravityUp).normalized;
            float lSpeed = Mathf.Max(Speed * 0.5f, 22f);
            if (State == PlayerState.Ground || State == PlayerState.Grind) _gravityUp = _up;
            _velocity = _heading * lSpeed + lAway * 5f + _gravityUp * 11f;
            _crashTimer = 0.55f;
            _invulnerable = 1.4f;
            _takeoffY = transform.position.y;
            Diving = false;
            StopGlide();
            SetState(PlayerState.Crash);
            Combo = 0;
            AudioManager.Play("sfx_crash", 1f);
            Juice.Shake(0.7f);
            Juice.Flash(UIFactory.RED, 0.3f);
            Juice.Hitstop(0.08f);
            Juice.Popup(pLabel, UIFactory.RED, 1.3f);
            FX.Sparks(pPoint, Color.white, 20, 15f);
            OnCrashed?.Invoke();
        }

        private void AddCombo()
        {
            Combo++;
            _comboTimer = 3f;
        }
    }
}
