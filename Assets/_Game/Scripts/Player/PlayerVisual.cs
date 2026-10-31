using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Procedural jelly humanoid : spring squash & stretch, wobbly shader, procedural run cycle,
    // pose blending per state and the "light form" at hyper speed.
    public class PlayerVisual : MonoBehaviour
    {
        public static readonly Color SLIME_COLOR = new Color(0.25f, 0.92f, 0.85f);
        private static readonly Color SLIME_SHADE = new Color(0.45f, 0.55f, 0.95f);
        private static readonly Color OUTLINE = new Color(0.05f, 0.08f, 0.2f);
        private static readonly int WOBBLE = Shader.PropertyToID("_Wobble");
        private static readonly int WOBBLE_SPEED = Shader.PropertyToID("_WobbleSpeed");

        public Transform Root { get; private set; }

        private PlayerController _player;
        private Transform _pivot;
        private Transform _body;
        private Transform _head;
        private Transform _drop;
        private Transform _armL, _armR, _legL, _legR, _handR, _handL;
        private Transform _light;
        private Transform _shadow;
        private Material _slime;
        private Renderer[] _bodyRenderers;
        private TrailRenderer _scarf;
        private TrailRenderer _handTrailL, _handTrailR;
        private TrailRenderer _lightTrail;
        private ParticleSystem _dust;
        private ParticleSystem _sparks;

        private Quaternion _facing = Quaternion.identity;
        private float _squash = 1f;
        private float _squashVelocity;
        private Vector3 _headLag;
        private Vector3 _headLagVelocity;
        private Vector3 _dropLag;
        private Vector3 _dropLagVelocity;
        private Vector3 _lastVelocity;
        private float _phase;
        private float _flip;
        private float _punch;
        private float _dash;
        private float _lean;
        private float _crashSpin;
        private float _flash;
        private bool _lightForm;
        private Vector3 _poseEuler;
        private float _crouch;
        private float _victory;

        public static PlayerVisual Create(PlayerController pPlayer)
        {
            PlayerVisual lVisual = new GameObject("PlayerVisual").AddComponent<PlayerVisual>();
            lVisual.Build(pPlayer);
            return lVisual;
        }

        // Used by the title screen : same model without a controller.
        public static PlayerVisual CreateDisplay(Transform pParent)
        {
            PlayerVisual lVisual = new GameObject("PlayerDisplay").AddComponent<PlayerVisual>();
            lVisual.transform.SetParent(pParent, false);
            lVisual.Build(null);
            return lVisual;
        }

        private GameObject Part(PrimitiveType pType, Transform pParent, Vector3 pPos, Vector3 pScale, Material pMaterial)
        {
            return Mats.Primitive(pType, pParent, pPos, pScale, pMaterial);
        }

        private Transform Pivot(string pName, Transform pParent, Vector3 pPos)
        {
            Transform lPivot = new GameObject(pName).transform;
            lPivot.SetParent(pParent, false);
            lPivot.localPosition = pPos;
            return lPivot;
        }

        private void Build(PlayerController pPlayer)
        {
            _player = pPlayer;
            Root = transform;
            _slime = Mats.ToonUnique(SLIME_COLOR);
            _slime.SetColor("_ShadeColor", SLIME_SHADE);
            _slime.SetColor("_OutlineColor", OUTLINE);
            _slime.SetFloat("_OutlineWidth", 0.028f);
            _slime.SetFloat("_RimAmount", 0.9f);
            _slime.SetColor("_RimColor", new Color(0.8f, 1f, 1f));
            _slime.SetFloat("_SpecSize", 0.7f);
            _slime.SetFloat("_HalftoneAmount", 0.8f);
            _slime.SetFloat(WOBBLE, 0.025f);
            _slime.SetFloat(WOBBLE_SPEED, 9f);
            Material lWhite = Mats.Toon(Color.white, null, 0.02f);
            Material lBlack = Mats.Toon(new Color(0.05f, 0.05f, 0.08f), null, 0f);
            Material lRed = Mats.Toon(UIFactory.RED, new Color(0.6f, 0.4f, 0.6f), 0.025f);

            _pivot = Pivot("Pivot", transform, Vector3.zero);
            _body = Pivot("Body", _pivot, new Vector3(0f, 0.95f, 0f));
            Part(PrimitiveType.Sphere, _body, new Vector3(0f, 0.3f, 0f), new Vector3(0.95f, 1.0f, 0.8f), _slime);
            Part(PrimitiveType.Sphere, _body, new Vector3(0f, 0.02f, 0f), new Vector3(0.8f, 0.55f, 0.7f), _slime);
            // Scarf around the neck
            Part(PrimitiveType.Cylinder, _body, new Vector3(0f, 0.72f, 0f), new Vector3(0.78f, 0.09f, 0.7f), lRed);

            _head = Pivot("Head", _body, new Vector3(0f, 1.15f, 0f));
            Part(PrimitiveType.Sphere, _head, Vector3.zero, new Vector3(0.95f, 0.88f, 0.9f), _slime);
            for (int lSide = -1; lSide <= 1; lSide += 2)
            {
                Part(PrimitiveType.Sphere, _head, new Vector3(lSide * 0.19f, 0.05f, 0.36f), new Vector3(0.24f, 0.34f, 0.12f), lWhite);
                Part(PrimitiveType.Sphere, _head, new Vector3(lSide * 0.17f, 0.04f, 0.42f), new Vector3(0.12f, 0.2f, 0.06f), lBlack);
            }
            // Goggles band
            Part(PrimitiveType.Cylinder, _head, new Vector3(0f, 0.22f, 0f), new Vector3(0.98f, 0.06f, 0.93f), lRed);
            _drop = Pivot("Drop", _head, new Vector3(0f, 0.42f, -0.05f));
            Part(PrimitiveType.Sphere, _drop, new Vector3(0f, 0.16f, 0f), new Vector3(0.22f, 0.36f, 0.22f), _slime);

            _armL = Pivot("ArmL", _body, new Vector3(-0.46f, 0.55f, 0f));
            _armR = Pivot("ArmR", _body, new Vector3(0.46f, 0.55f, 0f));
            Part(PrimitiveType.Capsule, _armL, new Vector3(0f, -0.3f, 0f), new Vector3(0.24f, 0.3f, 0.24f), _slime);
            Part(PrimitiveType.Capsule, _armR, new Vector3(0f, -0.3f, 0f), new Vector3(0.24f, 0.3f, 0.24f), _slime);
            _handL = Part(PrimitiveType.Sphere, _armL, new Vector3(0f, -0.62f, 0f), Vector3.one * 0.3f, lWhite).transform;
            _handR = Part(PrimitiveType.Sphere, _armR, new Vector3(0f, -0.62f, 0f), Vector3.one * 0.3f, lWhite).transform;

            _legL = Pivot("LegL", _pivot, new Vector3(-0.22f, 0.85f, 0f));
            _legR = Pivot("LegR", _pivot, new Vector3(0.22f, 0.85f, 0f));
            Part(PrimitiveType.Capsule, _legL, new Vector3(0f, -0.38f, 0f), new Vector3(0.28f, 0.36f, 0.28f), _slime);
            Part(PrimitiveType.Capsule, _legR, new Vector3(0f, -0.38f, 0f), new Vector3(0.28f, 0.36f, 0.28f), _slime);
            Part(PrimitiveType.Sphere, _legL, new Vector3(0f, -0.8f, 0.1f), new Vector3(0.34f, 0.2f, 0.48f), lRed);
            Part(PrimitiveType.Sphere, _legR, new Vector3(0f, -0.8f, 0.1f), new Vector3(0.34f, 0.2f, 0.48f), lRed);

            _bodyRenderers = _pivot.GetComponentsInChildren<Renderer>();

            _scarf = CreateTrail(_body, new Vector3(0f, 0.72f, -0.35f), 0.1f, 0.26f, UIFactory.RED, false);
            _handTrailL = CreateTrail(_handL, Vector3.zero, 0.12f, 0.12f, new Color(1f, 1f, 1f, 0.7f), true);
            _handTrailR = CreateTrail(_handR, Vector3.zero, 0.12f, 0.12f, new Color(1f, 1f, 1f, 0.7f), true);

            // Light form
            _light = Pivot("LightForm", transform, new Vector3(0f, 1f, 0f));
            Mats.Primitive(PrimitiveType.Sphere, _light, Vector3.zero, Vector3.one * 1.4f, Mats.Glow(new Color(5f, 6f, 6f), true, 0f, 1.5f));
            Mats.Primitive(PrimitiveType.Sphere, _light, new Vector3(0f, 0f, -1.2f), new Vector3(0.9f, 0.9f, 3.5f), Mats.Glow(new Color(1f, 3f, 3.2f, 0.7f), true));
            _lightTrail = CreateTrail(_light, Vector3.zero, 0.5f, 1.6f, new Color(2f, 4f, 4f), true);
            _light.gameObject.SetActive(false);

            // Blob shadow
            _shadow = Mats.Primitive(PrimitiveType.Cylinder, null, Vector3.zero, new Vector3(1.4f, 0.01f, 1.4f), Mats.Glow(new Color(0.05f, 0f, 0.15f, 0.45f), false, 1f)).transform;
            _shadow.name = "BlobShadow";
            if (_player == null) _shadow.SetParent(transform, false);

            _dust = FX.CreateSystem("Dust", transform.position, Mats.Glow(new Color(1f, 1f, 1f, 0.5f), false, 1f), false);
            _dust.transform.SetParent(transform, false);
            ParticleSystem.MainModule lMain = _dust.main;
            lMain.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            lMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            lMain.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            lMain.maxParticles = 300;
            ParticleSystem.SizeOverLifetimeModule lSize = _dust.sizeOverLifetime;
            lSize.enabled = true;
            lSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            _sparks = FX.CreateSystem("DriftSparks", transform.position, Mats.Glow(Color.white * 3f, true, 1f), false);
            _sparks.transform.SetParent(transform, false);
            ParticleSystem.MainModule lSparkMain = _sparks.main;
            lSparkMain.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            lSparkMain.startSpeed = new ParticleSystem.MinMaxCurve(4f, 12f);
            lSparkMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            lSparkMain.gravityModifier = 1.5f;
            lSparkMain.maxParticles = 400;
            ParticleSystemRenderer lSparkRenderer = _sparks.GetComponent<ParticleSystemRenderer>();
            lSparkRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            lSparkRenderer.velocityScale = 0.05f;

            if (_player != null)
            {
                _player.OnJumped += () => { _squashVelocity += 6f; };
                _player.OnDoubleJumped += () => { _flip = 1f; _squashVelocity += 5f; };
                _player.OnLanded += pImpact => { _squashVelocity -= Mathf.Clamp(pImpact * 0.3f, 2f, 9f); };
                _player.OnPunched += () => { _punch = 1f; };
                _player.OnDashed += () => { _dash = 1f; _flash = 0.6f; };
                _player.OnCrashed += () => { _crashSpin = 1f; _flash = 1f; };
                _player.OnRespawned += () => { _facing = _player.transform.rotation; transform.position = _player.transform.position; };
                transform.position = _player.transform.position;
                _facing = _player.transform.rotation;
            }
        }

        private TrailRenderer CreateTrail(Transform pParent, Vector3 pLocal, float pTime, float pWidth, Color pColor, bool pAdditive)
        {
            Transform lAnchor = Pivot("Trail", pParent, pLocal);
            TrailRenderer lTrail = lAnchor.gameObject.AddComponent<TrailRenderer>();
            lTrail.time = pTime;
            lTrail.startWidth = pWidth;
            lTrail.endWidth = pWidth * 0.2f;
            lTrail.minVertexDistance = 0.1f;
            lTrail.sharedMaterial = Mats.Glow(pColor, pAdditive);
            lTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lTrail.numCornerVertices = 2;
            return lTrail;
        }

        public void SetVictory() => _victory = 1f;

        // Title screen / display mode : run in place.
        public void TickDisplay(float pDt, float pSpeed)
        {
            AnimateRun(pDt, pSpeed, true, 0);
            UpdateSprings(pDt, Vector3.zero);
            _pivot.localRotation = Quaternion.Euler(Mathf.Lerp(8f, 20f, pSpeed / 80f), 0f, Mathf.Sin(Time.time * 1.3f) * 6f);
            _slime.SetFloat(WOBBLE, 0.03f);
        }

        public void Tick(float pDt)
        {
            if (_player == null) return;
            PlayerState lState = _player.State;
            Vector3 lUp = lState == PlayerState.Ground || lState == PlayerState.Grind || lState == PlayerState.Finished ? _player.Up : _player.GravityUp;
            Vector3 lForward = _player.Heading;
            if (lState == PlayerState.WallRun) lUp = Vector3.Slerp(_player.GravityUp, -Vector3.Cross(_player.Heading, _player.GravityUp) * _player.WallSide, 0.28f);

            Quaternion lTarget = Quaternion.LookRotation(Vector3.ProjectOnPlane(lForward, lUp).normalized, lUp);
            _facing = Quaternion.Slerp(_facing, lTarget, 1f - Mathf.Exp(-18f * pDt));
            transform.SetPositionAndRotation(_player.transform.position, _facing);

            float lSpeed = _player.Speed;
            Vector3 lAcceleration = (_player.Velocity - _lastVelocity) / Mathf.Max(pDt, 0.0001f);
            _lastVelocity = _player.Velocity;

            UpdateLightForm(pDt);
            if (_lightForm)
            {
                UpdateShadow();
                return;
            }

            bool lGrounded = lState == PlayerState.Ground || lState == PlayerState.Finished;
            _lean = Mathf.Lerp(_lean, -_player.Steer * 18f - (_player.Drifting ? _player.DriftDirection * 22f : 0f), 1f - Mathf.Exp(-8f * pDt));
            _flip = Mathf.MoveTowards(_flip, 0f, pDt * 2.6f);
            _punch = Mathf.MoveTowards(_punch, 0f, pDt * 4f);
            _dash = Mathf.MoveTowards(_dash, 0f, pDt * 4f);
            _crashSpin = Mathf.MoveTowards(_crashSpin, 0f, pDt * 1.8f);
            _flash = Mathf.MoveTowards(_flash, 0f, pDt * 4f);
            _victory = Mathf.MoveTowards(_victory, 0f, pDt * 0.5f);

            Vector3 lPose;
            float lCrouchTarget = 0f;
            float lYaw = 0f;
            if (lState == PlayerState.Frozen)
            {
                // Sprinter "ready" stance.
                AnimateStatic(new Vector3(-50f, 20f, 0f), new Vector3(40f, -30f, 0f), new Vector3(-60f, 0f, 0f), new Vector3(40f, 0f, 0f), pDt);
                lPose = new Vector3(35f, 0f, 0f);
                lCrouchTarget = 0.35f;
            }
            else if (lState == PlayerState.Grind)
            {
                AnimateStatic(new Vector3(0f, 0f, 70f), new Vector3(0f, 0f, -70f), new Vector3(-10f, 0f, 20f), new Vector3(-10f, 0f, -20f), pDt);
                lPose = new Vector3(10f, 0f, Mathf.Sin(Time.time * 30f) * 2f);
                lYaw = 75f;
                lCrouchTarget = 0.25f;
            }
            else if (lState == PlayerState.Crash)
            {
                AnimateStatic(new Vector3(-150f, 0f, 30f), new Vector3(-150f, 0f, -30f), new Vector3(-40f, 0f, 20f), new Vector3(30f, 0f, -20f), pDt);
                lPose = new Vector3(_crashSpin * 720f, 0f, 0f);
            }
            else if (_player.Diving)
            {
                AnimateStatic(new Vector3(-170f, 0f, 10f), new Vector3(-170f, 0f, -10f), new Vector3(15f, 0f, 5f), new Vector3(15f, 0f, -5f), pDt);
                lPose = new Vector3(70f, 0f, 0f);
            }
            else if (lState == PlayerState.Air)
            {
                float lUpSpeed = Vector3.Dot(_player.Velocity, _player.GravityUp);
                float lRise = Mathf.Clamp01(lUpSpeed / 15f);
                AnimateStatic(new Vector3(-30f - lRise * 120f, 0f, 35f), new Vector3(-30f - lRise * 120f, 0f, -35f), new Vector3(-50f, 0f, 0f), new Vector3(25f, 0f, 0f), pDt);
                lPose = new Vector3(15f - lRise * 10f, 0f, 0f);
            }
            else
            {
                float lRunSpeed = lState == PlayerState.WallRun ? lSpeed * 1.2f : lSpeed;
                AnimateRun(pDt, lRunSpeed, lGrounded || lState == PlayerState.WallRun, _player.Drifting ? _player.DriftDirection : 0);
                lPose = new Vector3(Mathf.Lerp(8f, 26f, lSpeed / 90f), 0f, 0f);
                if (_player.Drifting)
                {
                    lYaw = _player.DriftDirection * -35f;
                    lCrouchTarget = 0.35f;
                    EmitDriftSparks();
                }
                if (_player.Sliding) lCrouchTarget = 0.45f;
            }

            if (_victory > 0f && lState == PlayerState.Finished)
            {
                _armL.localRotation = Quaternion.Euler(0f, 0f, -160f);
                _armR.localRotation = Quaternion.Euler(0f, 0f, 160f);
            }

            if (_punch > 0f)
            {
                float lPunch = Mathf.Sin(_punch * Mathf.PI);
                _armR.localRotation = Quaternion.Euler(-90f * lPunch - 10f, 0f, 0f);
                _handR.localScale = Vector3.one * (0.3f + lPunch * 0.45f);
                lYaw += 25f * lPunch;
            }
            else _handR.localScale = Vector3.one * 0.3f;

            _crouch = Mathf.Lerp(_crouch, lCrouchTarget, 1f - Mathf.Exp(-12f * pDt));
            float lFlipAngle = _flip > 0f ? (1f - _flip) * 360f : 0f;
            _poseEuler = Vector3.Lerp(_poseEuler, lPose, 1f - Mathf.Exp(-12f * pDt));
            _pivot.localRotation = Quaternion.Euler(0f, lYaw, 0f) * Quaternion.Euler(_poseEuler.x + lFlipAngle, 0f, _lean + _poseEuler.z);
            _pivot.localPosition = new Vector3(0f, -_crouch * 0.4f + (lFlipAngle > 0f ? 0.6f : 0f), 0f);

            Vector3 lLocalAccel = Quaternion.Inverse(_facing) * lAcceleration;
            UpdateSprings(pDt, lLocalAccel);

            float lDashStretch = _dash * 0.6f;
            Vector3 lScale = new Vector3(1f / Mathf.Sqrt(_squash), _squash, 1f / Mathf.Sqrt(_squash));
            lScale = Vector3.Scale(lScale, new Vector3(1f - lDashStretch * 0.3f, 1f - lDashStretch * 0.3f, 1f + lDashStretch));
            lScale.y *= 1f - _crouch * 0.25f;
            _pivot.localScale = lScale;

            _slime.SetFloat(WOBBLE, 0.02f + Mathf.Clamp01(lSpeed / 120f) * 0.03f + Mathf.Abs(_squash - 1f) * 0.12f);
            _slime.SetFloat(Mats.FLASH, _flash > 0.3f || (_player.Invulnerable && Mathf.Repeat(Time.time * 12f, 1f) > 0.5f) ? 0.8f : 0f);

            bool lFast = lSpeed > 55f;
            _handTrailL.emitting = lFast;
            _handTrailR.emitting = lFast;
            _scarf.time = Mathf.Clamp(1.6f / Mathf.Max(lSpeed, 1f), 0.02f, 0.15f);
            UpdateShadow();
        }

        private void UpdateLightForm(float pDt)
        {
            bool lWant = _player.Hyper && _player.State != PlayerState.Crash;
            if (lWant != _lightForm)
            {
                _lightForm = lWant;
                foreach (Renderer lRenderer in _bodyRenderers) lRenderer.enabled = !lWant;
                _light.gameObject.SetActive(lWant);
                _scarf.emitting = !lWant;
                _handTrailL.emitting = false;
                _handTrailR.emitting = false;
                _lightTrail.Clear();
                FX.Sparks(transform.position + transform.up, Color.white, 40, 25f, 0.6f, 0.5f);
                FX.Shockwave(transform.position + transform.up, transform.forward, new Color(0.6f, 1f, 1f), 12f, 0.3f);
            }
            if (_lightForm)
            {
                float lPulse = 1f + Mathf.Sin(Time.time * 40f) * 0.1f;
                _light.localScale = new Vector3(lPulse, lPulse, lPulse * (1f + _player.Speed / 120f));
                _lightTrail.time = Mathf.Lerp(0.3f, 0.8f, (_player.Speed - 90f) / 80f);
            }
        }

        private void AnimateRun(float pDt, float pSpeed, bool pGrounded, int pDrift)
        {
            float lSpeed01 = Mathf.Clamp01(pSpeed / 80f);
            float lCadence = Mathf.Lerp(9f, 21f, lSpeed01);
            float lPrevPhase = _phase;
            _phase += pDt * lCadence;
            float lSwing = Mathf.Sin(_phase);
            float lAmplitude = Mathf.Lerp(35f, 75f, lSpeed01);

            _legL.localRotation = Quaternion.Euler(lSwing * lAmplitude, 0f, 0f);
            _legR.localRotation = Quaternion.Euler(-lSwing * lAmplitude, 0f, 0f);

            if (pSpeed > 62f)
            {
                // Arms swept back : anime sprint.
                _armL.localRotation = Quaternion.Euler(65f + lSwing * 6f, 0f, 18f);
                _armR.localRotation = Quaternion.Euler(65f - lSwing * 6f, 0f, -18f);
            }
            else
            {
                _armL.localRotation = Quaternion.Euler(-lSwing * lAmplitude * 0.9f, 0f, 12f);
                _armR.localRotation = Quaternion.Euler(lSwing * lAmplitude * 0.9f, 0f, -12f);
            }
            if (pDrift != 0)
            {
                Transform lInner = pDrift > 0 ? _armR : _armL;
                lInner.localRotation = Quaternion.Euler(-20f, 0f, pDrift * -55f);
            }

            _body.localPosition = new Vector3(0f, 0.95f + Mathf.Abs(Mathf.Sin(_phase)) * 0.09f, 0f);

            // Footsteps on each half cycle.
            if (pGrounded && Mathf.Floor(lPrevPhase / Mathf.PI) != Mathf.Floor(_phase / Mathf.PI))
            {
                _squashVelocity -= 1.2f;
                if (_player != null)
                {
                    EmitDust(2);
                    AudioManager.Play("sfx_step", 0.25f, 1f + lSpeed01 * 0.3f, 0.12f);
                }
            }
        }

        private void AnimateStatic(Vector3 pArmL, Vector3 pArmR, Vector3 pLegL, Vector3 pLegR, float pDt)
        {
            float lT = 1f - Mathf.Exp(-14f * pDt);
            _armL.localRotation = Quaternion.Slerp(_armL.localRotation, Quaternion.Euler(pArmL), lT);
            _armR.localRotation = Quaternion.Slerp(_armR.localRotation, Quaternion.Euler(pArmR), lT);
            _legL.localRotation = Quaternion.Slerp(_legL.localRotation, Quaternion.Euler(pLegL), lT);
            _legR.localRotation = Quaternion.Slerp(_legR.localRotation, Quaternion.Euler(pLegR), lT);
            _body.localPosition = Vector3.Lerp(_body.localPosition, new Vector3(0f, 0.95f, 0f), lT);
        }

        private void UpdateSprings(float pDt, Vector3 pLocalAccel)
        {
            // Volume-preserving squash spring.
            float lForce = (1f - _squash) * 220f - _squashVelocity * 11f;
            _squashVelocity += lForce * pDt;
            _squash = Mathf.Clamp(_squash + _squashVelocity * pDt, 0.55f, 1.6f);

            // Head and droplet lag behind accelerations (jelly secondary motion).
            Vector3 lTarget = Vector3.ClampMagnitude(-pLocalAccel * 0.0025f, 0.25f);
            _headLag = Vector3.SmoothDamp(_headLag, lTarget, ref _headLagVelocity, 0.08f);
            _head.localPosition = new Vector3(0f, 1.15f, 0f) + _headLag;
            _dropLag = Vector3.SmoothDamp(_dropLag, lTarget * 3f + new Vector3(0f, 0f, -0.2f), ref _dropLagVelocity, 0.12f);
            _drop.localRotation = Quaternion.Euler(Mathf.Clamp(_dropLag.z * -120f, -70f, 70f), 0f, Mathf.Clamp(_dropLag.x * 120f, -70f, 70f));
        }

        private void EmitDust(int pCount)
        {
            ParticleSystem.EmitParams lParams = new ParticleSystem.EmitParams();
            lParams.position = transform.position + transform.up * 0.1f;
            lParams.applyShapeToPosition = true;
            _dust.Emit(lParams, pCount);
        }

        private void EmitDriftSparks()
        {
            Color[] lColors = { Color.white, new Color(0.3f, 0.8f, 3f), new Color(3f, 1.5f, 0.2f), new Color(3f, 0.4f, 2.5f) };
            ParticleSystem.EmitParams lParams = new ParticleSystem.EmitParams();
            lParams.startColor = lColors[_player.DriftTier];
            for (int lSide = -1; lSide <= 1; lSide += 2)
            {
                lParams.position = transform.position + transform.right * lSide * 0.3f - transform.forward * 0.3f + transform.up * 0.1f;
                lParams.velocity = (-transform.forward * 6f + transform.up * 3f + transform.right * lSide * 2f) + Random.insideUnitSphere * 2f;
                _sparks.Emit(lParams, 1);
            }
        }

        private void UpdateShadow()
        {
            if (_shadow == null || _player == null) return;
            RaycastHit lHit;
            Vector3 lDown = -_player.GravityUp;
            if (Physics.Raycast(transform.position + _player.GravityUp * 0.5f, lDown, out lHit, 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                _shadow.gameObject.SetActive(true);
                _shadow.position = lHit.point + lHit.normal * 0.06f;
                _shadow.up = lHit.normal;
                float lSize = Mathf.Lerp(1.6f, 0.6f, lHit.distance / 30f);
                _shadow.localScale = new Vector3(lSize, 0.01f, lSize);
            }
            else _shadow.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_shadow != null) Destroy(_shadow.gameObject);
        }
    }
}
