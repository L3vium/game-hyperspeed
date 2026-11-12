using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Player actions and external impulses : jumps, dash and held thrust, punch, launches, boosts, crashes.
    public partial class PlayerController
    {
        // Dash energy : a press costs one, holding burns it. Rings and bursts give some back.
        private const float DASH_ENERGY = 3f;
        private const float DASH_RECHARGE = 0.5f;
        // 2026-11-11 : the run is calmer, the kicks far stronger (they still fade, see DRAG).
        private const float DASH_KICK = 0.8f;
        private const float AIR_DASH_KICK = 1f;
        // Past this many times cruise, kicks add nothing more.
        private const float SOFT_CAP = 4.6f;
        private const float THRUST_DELAY = 0.2f;
        private const float THRUST_BURN = 1.3f;
        private const float THRUST_POWER = 0.75f;
        private const float LIFT_PITCH = 13f;

        private float JumpSpeed
        {
            get
            {
                float lGround = OnSurface ? _speed : Vector3.ProjectOnPlane(_velocity, _gravityUp).magnitude;
                return Mathf.Min(JUMP_VELOCITY_MAX, JUMP_VELOCITY + lGround * JUMP_SPEED_SHARE);
            }
        }

        private void TryJump()
        {
            bool lCoyote = State == PlayerState.Air && Time.time - _lastGrounded < COYOTE_TIME && Vector3.Dot(_velocity, _gravityUp) <= 1f;
            if (State == PlayerState.Ground || lCoyote)
            {
                Vector3 lRight = Vector3.Cross(_up, _moveDir).normalized;
                _velocity = _moveDir * _speed + lRight * _slip + _up * JumpSpeed;
                _gravityUp = _up;
                _jumpCut = true;
                _jumpBufferedAt = -10f;
                _lastGrounded = -10f;
                if (Drifting) EndDrift(true);
                SetState(PlayerState.Air);
                HSJuice.Play(JuiceId.Jump, Jx(transform.position, Speed01));
                OnJumped?.Invoke();
            }
            else if (State == PlayerState.Grind)
            {
                _velocity = _heading * (_speed + 4f) + _up * JumpSpeed;
                _railCooldown = 0.5f;
                _jumpCut = true;
                _jumpBufferedAt = -10f;
                SetState(PlayerState.Air);
                HSJuice.Play(JuiceId.RopeJump, Jx(transform.position));
                OnJumped?.Invoke();
            }
            // Pressed just before touching down : that is a ground jump on landing (buffered), not the double jump.
            else if (State == PlayerState.Air && HasLandingPrediction && LandingTime < 0.15f && Vector3.Dot(_velocity, _gravityUp) < 0f) return;
            else if (State == PlayerState.Air && HasDoubleJump)
            {
                HasDoubleJump = false;
                Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, _gravityUp);
                _velocity = lFlat + _gravityUp * JumpSpeed * 0.9f;
                _jumpBufferedAt = -10f;
                _jumpCut = true;
                HSJuice.Play(JuiceId.DoubleJump, Jx(transform.position));
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
            _breakTimer = BREAK_WINDOW;
            _dashCooldown = 0.3f;

            float lCruise = _profile.cruise;
            float lScale = KickScale(0.25f);
            if (State == PlayerState.Ground || State == PlayerState.Grind)
            {
                _speed = CapKick(_speed, lCruise * DASH_KICK * lScale);
                if (State == PlayerState.Ground && AimPitch > LIFT_PITCH) LiftOff();
            }
            else
            {
                float lNew = CapKick(_velocity.magnitude, lCruise * AIR_DASH_KICK * lScale);
                _velocity = AimDirection * lNew;
            }
            JuiceContext lDashJuice = Jx(transform.position + PhysUp);
            lDashJuice.direction = OnSurface ? _heading : AimDirection;
            HSJuice.Play(OnSurface ? JuiceId.Dash : JuiceId.DashAir, lDashJuice);
            OnDashed?.Invoke();
        }

        // Looking up while dashing : the ground lets go and you fly along your aim. Never inside a loop or a twist,
        // where "up" is the middle of the loop : there a dash stays glued to the surface.
        private void LiftOff()
        {
            if (_layout != null && _layout.spine.samples[_layout.spine.IndexOf(Progress)].localGravity) return;
            float lSpeed = _speed;
            TakeOff(TakeoffReason.Aim);
            _velocity = AimDirection * lSpeed;
            HSJuice.Play(JuiceId.LiftOff, Jx(transform.position));
        }

        // Hold : a continuous push along the look direction, far stronger than a press, for as long as the energy lasts.
        private void UpdateThrust(float pDt)
        {
            _dashHeldTime = _dashHeld ? _dashHeldTime + pDt : 0f;
            bool lWant = _dashHeld && _dashHeldTime > THRUST_DELAY && _dashEnergy > 0f && State != PlayerState.Crash;
            if (lWant && !Thrusting) HSJuice.Play(JuiceId.ThrustStart, Jx(transform.position));
            Thrusting = lWant;
            if (!Thrusting) return;
            _dashEnergy = Mathf.Max(0f, _dashEnergy - THRUST_BURN * pDt);
            float lPush = _profile.cruise * THRUST_POWER * KickScale(0.15f) * pDt;
            if (State == PlayerState.Ground || State == PlayerState.Grind)
            {
                _speed = CapKick(_speed, lPush);
                if (State == PlayerState.Ground && AimPitch > LIFT_PITCH) LiftOff();
            }
            else _velocity = CapVelocity(_velocity, AimDirection * lPush * (Diving ? 2f : 1.3f));
            _boostTimer = Mathf.Max(_boostTimer, 0.3f);
        }

        private void Punch()
        {
            _punchTimer = 0.3f;
            _punchCooldown = 0.34f;
            _punchHit = false;
            HSJuice.Play(JuiceId.Punch, Jx(transform.position));
            OnPunched?.Invoke();
        }

        // Launch strip : a high jump. Your speed and your line stay yours.
        public void Launch(float pUp)
        {
            Vector3 lUp = OnSurface ? _up : _gravityUp;
            Vector3 lVelocity = OnSurface ? _moveDir * _speed + Vector3.Cross(_up, _moveDir).normalized * _slip : _velocity;
            _velocity = Vector3.ProjectOnPlane(lVelocity, lUp) + lUp * pUp;
            _gravityUp = lUp;
            _jumpCut = false;
            EndDrift(false);
            SetState(PlayerState.Air);
            HasDoubleJump = true;
            _boostTimer = Mathf.Max(_boostTimer, 0.5f);
            HSJuice.Play(JuiceId.Launch, Jx(transform.position));
        }

        // One instant kick. It fades on its own : see the drag in SpeedStep. pRefill also gives dash energy back.
        public void ApplyBoost(float pBoost, Vector3 pDirection, float pAlign, string pLabel, bool pRefill = false, bool pPad = false, JuiceId pJuice = JuiceId.BoostOther)
        {
            // Kicks are huge from a calm run and worth less and less up high : one pad throws you toward hyper, only a
            // chain of them and your dashes reaches light, and nothing stacks forever.
            pBoost *= KickScale(pPad ? 0.35f : 0.2f);
            // Pads and rings link the chain that lifts the ceiling (and charges light where it exists).
            if (pPad || pJuice == JuiceId.Ring) AddChain();
            if (OnSurface) _speed = CapKick(_speed, pBoost);
            else _velocity = CapVelocity(_velocity, pDirection.normalized * pBoost);
            if (pRefill) _dashEnergy = Mathf.Min(DASH_ENERGY, _dashEnergy + 0.7f);

            float lAmount = Mathf.Clamp01(pBoost / (_profile.cruise * 0.8f));
            _boostTimer = Mathf.Max(_boostTimer, 0.5f + lAmount * 0.6f);
            // Everything you see, hear and feel : the event in the HS_Juice prefab (pads, rings, drift, others).
            JuiceContext lJuice = Jx(transform.position + PhysUp * 0.5f, pPad ? 1f : lAmount);
            lJuice.direction = pDirection.sqrMagnitude > 0.01f ? pDirection.normalized : _heading;
            lJuice.label = pLabel;
            HSJuice.Play(pPad ? JuiceId.BoostPad : pJuice, lJuice);
            AddCombo();
        }

        // How much of a kick lands at the current speed : all of it up to cruise, less and less above (never under the
        // floor), and nothing past SOFT_CAP times cruise.
        private float KickScale(float pFloor)
        {
            float lRatio = Speed / Mathf.Max(1f, _profile.cruise);
            float lScale = Mathf.Clamp(1.6f - 0.45f * lRatio, pFloor, 1f);
            return lScale * Mathf.Clamp01(SOFT_CAP - lRatio);
        }

        // Touching a hazard : most of your speed is gone. That is the whole punishment, and it is enough.
        public void Crash(Vector3 pPoint, string pLabel = null)
        {
            if (Invulnerable || CheatInvincible || State == PlayerState.Crash || State == PlayerState.Frozen || State == PlayerState.Finished || State == PlayerState.Dead) return;
            if (Autopilot.Enabled) Debug.Log("[HS-CRASH] s=" + Progress.ToString("F0") + " v=" + Speed.ToString("F0") + " state=" + State + " at=" + pPoint.ToString("F0") + " lat=" + Lateral.ToString("F0"));
            Vector3 lUp = OnSurface ? _up : _gravityUp;
            Vector3 lAway = Vector3.ProjectOnPlane(transform.position - pPoint, lUp).normalized;
            float lSpeed = Mathf.Max(Speed * 0.4f, RunFloor);
            _gravityUp = lUp;
            _velocity = _heading * lSpeed + lAway * 10f + lUp * 18f;
            _crashTimer = 0.45f;
            _invulnerable = 1.2f;
            Diving = false;
            SetState(PlayerState.Crash);
            Combo = 0;
            HSJuice.Play(JuiceId.Crash, Jx(pPoint));
            OnCrashed?.Invoke();
        }

        private void AddCombo()
        {
            Combo++;
            _comboTimer = 3f;
        }

        // ---------------------------------------------------------------- chain and ceiling (2026-11-12)

        // Pads, rings and rope exits within CHAIN_WINDOW of each other : the chain. Only a chain lifts the ceiling of
        // the kicks, so one pad can never cross a speed tier.
        public int Chain { get; private set; }
        // 0..1 : light speed charged by a chain of pads in hyper (stages where light exists).
        public float LightCharge { get; private set; }
        private float _chainTimer;

        public float KickCeiling => _profile.KickCeiling(Chain);

        private void AddChain()
        {
            Chain = _chainTimer > 0f ? Chain + 1 : 1;
            _chainTimer = SpeedProfile.CHAIN_WINDOW;
            if (_profile.LightEnabled && Hyper && !LightForm) LightCharge = Mathf.Min(1f, LightCharge + 1f / _profile.lightPads);
        }

        private void UpdateChain(float pDt)
        {
            _chainTimer -= pDt;
            if (_chainTimer <= 0f) Chain = 0;
            // A broken chain lets the charge leak slowly ; in light form the charge is what keeps you there.
            // On a light highway the light holds for the whole of it ; off it, the charge burns.
            if (LightForm)
            {
                if (!OnHighway) LightCharge = Mathf.Max(0f, LightCharge - pDt / LIGHT_CHARGE_TIME);
            }
            else if (Chain == 0) LightCharge = Mathf.Max(0f, LightCharge - pDt * 0.25f);
        }

        // A kick adds speed up to the ceiling, never past it, and never takes away speed you already have.
        private float CapKick(float pCurrent, float pAdd)
        {
            float lCeiling = Mathf.Min(KickCeiling, _profile.SafetyMax);
            if (pCurrent >= lCeiling) return Mathf.Min(pCurrent, _profile.SafetyMax);
            return Mathf.Min(pCurrent + pAdd, lCeiling);
        }

        private Vector3 CapVelocity(Vector3 pVelocity, Vector3 pAdd)
        {
            Vector3 lNew = pVelocity + pAdd;
            float lMagnitude = lNew.magnitude;
            float lAllowed = CapKick(pVelocity.magnitude, Mathf.Max(0f, lMagnitude - pVelocity.magnitude));
            return lMagnitude > lAllowed && lMagnitude > 0.01f ? lNew * (lAllowed / lMagnitude) : lNew;
        }

        // Dev cheat : straight to the top of the stage (light where it exists, hyper otherwise).
        public void CheatMaxSpeed()
        {
            Chain = 12;
            _chainTimer = SpeedProfile.CHAIN_WINDOW;
            if (_profile.LightEnabled) LightCharge = 1f;
            float lTarget = _profile.LightEnabled ? _profile.lightEnter : _profile.hyperEnabled ? _profile.hyperEnter * 1.1f : KickCeiling;
            if (OnSurface) _speed = Mathf.Max(_speed, lTarget);
            else _velocity = _velocity.normalized * Mathf.Max(_velocity.magnitude, lTarget);
        }
    }
}
