using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Player actions and external impulses : jumps, dash, punch, pads, rings, boosts, crashes, combo.
    public partial class PlayerController
    {
        private float JumpSpeed => JUMP_VELOCITY * Mathf.Sqrt(Ballistics.Scale);

        private void TryJump()
        {
            bool lCoyote = State == PlayerState.Air && Time.time - _lastGrounded < COYOTE_TIME && Vector3.Dot(_velocity, _gravityUp) <= 1f;
            if (State == PlayerState.Ground || lCoyote)
            {
                Vector3 lRight = Vector3.Cross(_up, _moveDir).normalized;
                _velocity = _moveDir * _speed + lRight * _slip + _up * JumpSpeed;
                _gravityUp = Vector3.Dot(_up, Vector3.up) > 0.85f ? Vector3.up : _up;
                _jumpBufferedAt = -10f;
                _lastGrounded = -10f;
                _jumpRising = true;
                if (Drifting) EndDrift(true);
                SetState(PlayerState.Air);
                _airYaw = _layout != null ? _layout.spine.Evaluate(Progress).yaw : 0f;
                AudioManager.Play("sfx_jump", 0.8f);
                FX.Puff(transform.position, Color.white, 5, 1.8f, 5f);
                OnJumped?.Invoke();
            }
            else if (State == PlayerState.WallRun)
            {
                bool lGood = StateTime > 0.1f && StateTime < 0.9f;
                float lBonus = (lGood ? 0.12f : 0.04f) * _profile.cruise;
                Vector3 lFlow = _layout != null ? _layout.Flow(Progress) : _heading;
                Vector3 lAlong = Wall == WallMode.Climb ? lFlow : _wallRunDir;
                float lSpeed = Wall == WallMode.Climb ? Mathf.Max(_wallTopSpeed, _profile.cruise) : _wallSpeed;
                _velocity = lAlong * (lSpeed + lBonus) + _wallNormal * (18f + 10f * _profile.k) + _gravityUp * JumpSpeed;
                _jumpBufferedAt = -10f;
                _jumpRising = true;
                LeaveWall(0.25f);
                _airYaw = _layout != null ? _layout.spine.Evaluate(Progress).yaw : 0f;
                HasDoubleJump = true;
                HasAirDash = true;
                AudioManager.Play("sfx_walljump", 0.9f);
                FX.Sparks(transform.position + _gravityUp, Color.white, 16, 14f);
                Juice.Popup(lGood ? "WALL JUMP!" : "wall jump", lGood ? UIFactory.CYAN : Color.white, lGood ? 1.1f : 0.8f);
                if (lGood) { Juice.FovKick(5f); AddCombo(); }
                OnJumped?.Invoke();
            }
            else if (State == PlayerState.Grind)
            {
                _velocity = _heading * (_speed + 4f) + _up * JumpSpeed;
                _railCooldown = 0.5f;
                _jumpBufferedAt = -10f;
                _jumpRising = true;
                SetState(PlayerState.Air);
                _airYaw = _layout != null ? _layout.spine.Evaluate(Progress).yaw : 0f;
                AudioManager.Play("sfx_jump", 0.8f, 1.1f);
                OnJumped?.Invoke();
            }
            else if (State == PlayerState.Air && HasDoubleJump)
            {
                HasDoubleJump = false;
                Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
                _velocity = lFlat + _gravityUp * JumpSpeed * 0.9f;
                Diving = false;
                StopGlide();
                _jumpRising = true;
                _jumpBufferedAt = -10f;
                AudioManager.Play("sfx_doublejump", 0.85f);
                FX.Shockwave(transform.position, _gravityUp, Color.white, 8f, 0.25f);
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

            float lBoost = _profile.cruise * 0.16f;
            if (State == PlayerState.Ground || State == PlayerState.Grind) _speed = Mathf.Min(_profile.max, _speed + lBoost);
            else if (State == PlayerState.WallRun) _wallSpeed = Mathf.Min(_profile.max, _wallSpeed + lBoost);
            else
            {
                Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
                Vector3 lDir = lFlat.sqrMagnitude > 1f ? lFlat.normalized : _heading;
                float lNew = Mathf.Min(_profile.max, lFlat.magnitude + lBoost);
                _velocity = lDir * lNew + _gravityUp * Mathf.Max(2f, Vector3.Dot(_velocity, _gravityUp) * 0.2f);
                Diving = false;
                StopGlide();
            }
            AudioManager.Play("sfx_dash", 0.85f);
            Juice.FovKick(7f);
            Juice.Rollback(5f);
            Juice.Pulse(0.5f);
            Juice.Shake(0.15f);
            FX.Sparks(transform.position + CameraUpRaw, Color.white, 18, 22f);
            OnDashed?.Invoke();
        }

        private void Punch()
        {
            _punchTimer = 0.3f;
            _punchCooldown = 0.34f;
            _punchHit = false;
            AudioManager.Play("sfx_punch_whoosh", 0.7f);
            OnPunched?.Invoke();
        }

        // Launch strip : keeps your forward speed and your side, the height comes from the strip.
        public void Launch(float pAlong, float pSide, float pVertical)
        {
            SpineSample lSample = _layout.spine.Evaluate(Progress);
            _velocity = lSample.Forward * pAlong + lSample.Right * pSide + Vector3.up * pVertical;
            _gravityUp = Vector3.up;
            _airYaw = lSample.yaw;
            _padFlight = true;
            Diving = false;
            StopGlide();
            EndDrift(false);
            _heading = lSample.Forward;
            SetState(PlayerState.Air);
            _padFlight = true;
            _jumpRising = false;
            HasDoubleJump = true;
            HasAirDash = true;
            _glideTime = 0f;
            _boostTimer = Mathf.Max(_boostTimer, 0.5f);
            AudioManager.Play("sfx_launch", 0.9f);
            Juice.Popup("LAUNCH!", UIFactory.YELLOW, 1.1f);
            Juice.FovKick(8f);
            Juice.Rollback(5f);
            Juice.Shake(0.25f);
            AddCombo();
        }

        public void ApplyBoost(float pBoost, Vector3 pDirection, float pAlign, string pLabel)
        {
            if (OnSurface) _speed = Mathf.Min(_profile.max, _speed + pBoost);
            else if (State == PlayerState.WallRun) _wallSpeed = Mathf.Min(_profile.max, _wallSpeed + pBoost);
            else _velocity += pDirection.normalized * pBoost;

            _boostTimer = Mathf.Max(_boostTimer, 1.2f);
            AudioManager.Play("sfx_boost", 0.95f);
            Juice.FovKick(8f);
            Juice.Rollback(7f);
            Juice.Pulse(0.6f);
            Juice.Shake(0.22f);
            Juice.Vignette(UIFactory.CYAN, 0.6f);
            if (!string.IsNullOrEmpty(pLabel)) Juice.Popup(pLabel, UIFactory.CYAN, 1.1f);
            FX.Sparks(transform.position + CameraUpRaw * 0.5f, UIFactory.CYAN, 50, 40f, 0.7f, 0.5f);
            FX.Burst(transform.position + CameraUpRaw, _heading, UIFactory.CYAN, 22f);
            AddCombo();
        }

        public void Crash(Vector3 pPoint, string pLabel = "OUCH!")
        {
            if (Invulnerable || CheatInvincible || State == PlayerState.Crash || State == PlayerState.Frozen || State == PlayerState.Finished || State == PlayerState.Dead) return;
            Vector3 lAway = transform.position - pPoint;
            lAway = Vector3.ProjectOnPlane(lAway, _gravityUp).normalized;
            float lSpeed = Mathf.Max(Speed * 0.6f, _profile.cruise * 0.55f);
            if (OnSurface) _gravityUp = _up;
            _velocity = _heading * lSpeed + lAway * 8f + _gravityUp * 16f * Mathf.Sqrt(Ballistics.Scale);
            _airYaw = _layout != null ? _layout.spine.Evaluate(Progress).yaw : 0f;
            _crashTimer = 0.55f;
            _invulnerable = 1.4f;
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
