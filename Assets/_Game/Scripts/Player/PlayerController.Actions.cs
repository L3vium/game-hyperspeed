using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Player actions and external impulses : jumps, dash and held thrust, punch, launches, boosts, crashes.
    public partial class PlayerController
    {
        // Dash energy : a press costs one, holding burns it. Rings and bursts give some back.
        private const float DASH_ENERGY = 3f;
        private const float DASH_RECHARGE = 0.5f;
        private const float DASH_KICK = 0.36f;
        private const float AIR_DASH_KICK = 0.55f;
        private const float THRUST_DELAY = 0.2f;
        private const float THRUST_BURN = 1.3f;
        private const float THRUST_POWER = 0.75f;
        private const float LIFT_PITCH = 13f;

        private float JumpSpeed => JUMP_VELOCITY;

        private void TryJump()
        {
            bool lCoyote = State == PlayerState.Air && Time.time - _lastGrounded < COYOTE_TIME && Vector3.Dot(_velocity, _gravityUp) <= 1f;
            if (State == PlayerState.Ground || lCoyote)
            {
                Vector3 lRight = Vector3.Cross(_up, _moveDir).normalized;
                _velocity = _moveDir * _speed + lRight * _slip + _up * JumpSpeed;
                _gravityUp = _up;
                _jumpBufferedAt = -10f;
                _lastGrounded = -10f;
                if (Drifting) EndDrift(true);
                SetState(PlayerState.Air);
                AudioManager.Play("sfx_jump", 0.8f);
                FX.Puff(transform.position, Color.white, 5, 1.8f, 5f);
                OnJumped?.Invoke();
            }
            else if (State == PlayerState.Grind)
            {
                _velocity = _heading * (_speed + 4f) + _up * JumpSpeed;
                _railCooldown = 0.5f;
                _jumpBufferedAt = -10f;
                SetState(PlayerState.Air);
                AudioManager.Play("sfx_jump", 0.8f, 1.1f);
                OnJumped?.Invoke();
            }
            else if (State == PlayerState.Air && HasDoubleJump)
            {
                HasDoubleJump = false;
                Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
                _velocity = lFlat + _gravityUp * JumpSpeed * 0.9f;
                _jumpBufferedAt = -10f;
                AudioManager.Play("sfx_doublejump", 0.85f);
                FX.Shockwave(transform.position, _gravityUp, Color.white, 10f, 0.25f);
                OnDoubleJumped?.Invoke();
            }
        }

        // Press : a big kick along the look direction. On the ground it stays on the ground unless you look up.
        private void Dash()
        {
            if (State == PlayerState.Crash || State == PlayerState.Frozen || State == PlayerState.Dead || State == PlayerState.Finished) return;
            if (_dashEnergy < 1f) return;
            _dashEnergy -= 1f;
            _dashTimer = DASH_TIME;
            _dashCooldown = 0.3f;

            float lCruise = _profile.cruise;
            if (State == PlayerState.Ground || State == PlayerState.Grind)
            {
                _speed = Mathf.Min(SpeedProfile.SAFETY_MAX, _speed + lCruise * DASH_KICK);
                if (State == PlayerState.Ground && AimPitch > LIFT_PITCH) LiftOff();
            }
            else
            {
                float lNew = _velocity.magnitude + lCruise * AIR_DASH_KICK;
                _velocity = AimDirection * Mathf.Min(SpeedProfile.SAFETY_MAX, lNew);
            }
            AudioManager.Play("sfx_dash", 0.85f);
            Juice.FovKick(9f);
            Juice.Rollback(9f);
            Juice.Pulse(0.5f);
            Juice.Shake(0.18f);
            FX.Sparks(transform.position + PhysUp, Color.white, 22, 30f);
            OnDashed?.Invoke();
        }

        // Looking up while dashing : the ground lets go and you fly along your aim.
        private void LiftOff()
        {
            float lSpeed = _speed;
            TakeOff(TakeoffReason.Aim);
            _velocity = AimDirection * lSpeed;
            AudioManager.Play("sfx_launch", 0.7f);
        }

        // Hold : a continuous push along the look direction, far stronger than a press, for as long as the energy lasts.
        private void UpdateThrust(float pDt)
        {
            _dashHeldTime = _dashHeld ? _dashHeldTime + pDt : 0f;
            bool lWant = _dashHeld && _dashHeldTime > THRUST_DELAY && _dashEnergy > 0f && State != PlayerState.Crash;
            if (lWant && !Thrusting) AudioManager.Play("sfx_boost_small", 0.6f);
            Thrusting = lWant;
            if (!Thrusting) return;
            _dashEnergy = Mathf.Max(0f, _dashEnergy - THRUST_BURN * pDt);
            float lPush = _profile.cruise * THRUST_POWER * pDt;
            if (State == PlayerState.Ground || State == PlayerState.Grind)
            {
                _speed = Mathf.Min(SpeedProfile.SAFETY_MAX, _speed + lPush);
                if (State == PlayerState.Ground && AimPitch > LIFT_PITCH) LiftOff();
            }
            else _velocity += AimDirection * lPush * (Diving ? 2f : 1.3f);
            _boostTimer = Mathf.Max(_boostTimer, 0.3f);
        }

        private void Punch()
        {
            _punchTimer = 0.3f;
            _punchCooldown = 0.34f;
            _punchHit = false;
            AudioManager.Play("sfx_punch_whoosh", 0.7f);
            OnPunched?.Invoke();
        }

        // Launch strip : a high jump. Your speed and your line stay yours.
        public void Launch(float pUp)
        {
            Vector3 lUp = OnSurface ? _up : _gravityUp;
            Vector3 lVelocity = OnSurface ? _moveDir * _speed + Vector3.Cross(_up, _moveDir).normalized * _slip : _velocity;
            _velocity = Vector3.ProjectOnPlane(lVelocity, lUp) + lUp * pUp;
            _gravityUp = lUp;
            EndDrift(false);
            SetState(PlayerState.Air);
            HasDoubleJump = true;
            _boostTimer = Mathf.Max(_boostTimer, 0.5f);
            AudioManager.Play("sfx_launch", 0.9f);
            Juice.FovKick(8f);
            Juice.Rollback(7f);
            Juice.Shake(0.25f);
        }

        // One instant kick. It fades on its own : see the drag in SpeedStep. pRefill also gives dash energy back.
        public void ApplyBoost(float pBoost, Vector3 pDirection, float pAlign, string pLabel, bool pRefill = false)
        {
            // Pads and rings are worth less and less up high : what takes you to light is your own dashes on top of them.
            float lRatio = Speed / _profile.cruise;
            pBoost *= Mathf.Clamp(1.45f - 0.36f * lRatio, 0.3f, 1f);
            if (OnSurface) _speed = Mathf.Min(SpeedProfile.SAFETY_MAX, _speed + pBoost);
            else _velocity += pDirection.normalized * pBoost;
            if (pRefill) _dashEnergy = Mathf.Min(DASH_ENERGY, _dashEnergy + 0.7f);

            float lAmount = Mathf.Clamp01(pBoost / (_profile.cruise * 0.3f));
            _boostTimer = Mathf.Max(_boostTimer, 0.5f + lAmount * 0.6f);
            AudioManager.Play("sfx_boost", 0.95f);
            Juice.FovKick(5f + lAmount * 6f);
            Juice.Rollback(6f + lAmount * 8f);
            Juice.Pulse(0.35f + lAmount * 0.4f);
            Juice.Shake(0.1f + lAmount * 0.16f);
            Juice.Vignette(UIFactory.CYAN, 0.5f);
            if (!string.IsNullOrEmpty(pLabel)) Juice.Popup(pLabel, UIFactory.CYAN, 1f);
            FX.Sparks(transform.position + PhysUp * 0.5f, UIFactory.CYAN, 50, 50f, 0.9f, 0.5f);
            FX.Burst(transform.position + PhysUp, _heading, UIFactory.CYAN, 22f);
            AddCombo();
        }

        // Touching a hazard : most of your speed is gone. That is the whole punishment, and it is enough.
        public void Crash(Vector3 pPoint, string pLabel = null)
        {
            if (Invulnerable || CheatInvincible || State == PlayerState.Crash || State == PlayerState.Frozen || State == PlayerState.Finished || State == PlayerState.Dead) return;
            Vector3 lUp = OnSurface ? _up : _gravityUp;
            Vector3 lAway = Vector3.ProjectOnPlane(transform.position - pPoint, lUp).normalized;
            float lSpeed = Mathf.Max(Speed * 0.4f, _profile.cruise * 0.55f);
            _gravityUp = lUp;
            _velocity = _heading * lSpeed + lAway * 10f + lUp * 18f;
            _crashTimer = 0.45f;
            _invulnerable = 1.2f;
            Diving = false;
            SetState(PlayerState.Crash);
            Combo = 0;
            AudioManager.Play("sfx_crash", 1f);
            Juice.Shake(0.8f);
            Juice.Flash(UIFactory.RED, 0.3f);
            Juice.Hitstop(0.06f);
            FX.Sparks(pPoint, Color.white, 30, 30f);
            OnCrashed?.Invoke();
        }

        private void AddCombo()
        {
            Combo++;
            _comboTimer = 3f;
        }
    }
}
