using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Rigged runner (Ellen, humanoid) animated procedurally in muscle space :
    // run / sprint cycles, lean, anticipation before landing, follow-through on head and spine,
    // dash swirl with afterimages, energy cone while boosting, spiky drift sparks and the light form.
    public class PlayerVisual : MonoBehaviour
    {
        private static readonly int FLASH = Shader.PropertyToID("_Flash");

        public Transform Root { get; private set; }

        private PlayerController _player;
        private Transform _pivot;
        private Transform _model;
        private Animator _animator;
        private HumanoidAnimator _anim;
        private SkinnedMeshRenderer _skin;
        private Renderer[] _bodyRenderers;
        private MaterialPropertyBlock _block;
        private Transform _light;
        private Transform _shadow;
        private Transform _cone;
        private Material _coneMaterial;
        private TrailRenderer _scarf;
        private TrailRenderer _handTrailL, _handTrailR;
        private TrailRenderer _footTrailL, _footTrailR;
        private TrailRenderer _lightTrail;
        private ParticleSystem _dust;
        private ParticleSystem _sparks;
        private ParticleSystem _wind;

        private Quaternion _facing = Quaternion.identity;
        private float _phase;
        private float _lean;
        private float _pitch;
        private float _yawOffset;
        private float _squash = 1f;
        private float _squashVelocity;
        private float _flip;
        private float _dashSpin;
        private float _punch;
        private float _crashSpin;
        private float _flash;
        private float _victory;
        private float _turnRate;
        private float _spineLag;
        private bool _lightForm;
        private Vector3 _lastForward = Vector3.forward;

        public static PlayerVisual Create(PlayerController pPlayer)
        {
            PlayerVisual lVisual = new GameObject("PlayerVisual").AddComponent<PlayerVisual>();
            lVisual.Build(pPlayer);
            return lVisual;
        }

        public static PlayerVisual CreateDisplay(Transform pParent)
        {
            PlayerVisual lVisual = new GameObject("PlayerDisplay").AddComponent<PlayerVisual>();
            lVisual.transform.SetParent(pParent, false);
            lVisual.Build(null);
            return lVisual;
        }

        private Transform Pivot(string pName, Transform pParent, Vector3 pPosition)
        {
            Transform lPivot = new GameObject(pName).transform;
            lPivot.SetParent(pParent, false);
            lPivot.localPosition = pPosition;
            return lPivot;
        }

        private void Build(PlayerController pPlayer)
        {
            _player = pPlayer;
            Root = transform;
            _block = new MaterialPropertyBlock();
            _pivot = Pivot("Pivot", transform, Vector3.zero);

            GameObject lPrefab = GameAssets.Instance.characterPrefab;
            GameObject lModel = Instantiate(lPrefab, _pivot, false);
            lModel.name = "Runner";
            _model = lModel.transform;
            _model.localPosition = Vector3.zero;
            _model.localRotation = Quaternion.identity;
            _animator = lModel.GetComponent<Animator>();
            _animator.runtimeAnimatorController = null;
            _animator.applyRootMotion = false;
            _animator.enabled = false;
            _anim = new HumanoidAnimator(_animator);

            _skin = lModel.GetComponentInChildren<SkinnedMeshRenderer>();
            _skin.updateWhenOffscreen = true;
            Material[] lMaterials = GameAssets.Instance.characterMaterials;
            if (lMaterials != null && lMaterials.Length > 0)
            {
                Material[] lSlots = new Material[_skin.sharedMaterials.Length];
                for (int i = 0; i < lSlots.Length; i++) lSlots[i] = lMaterials[Mathf.Min(i, lMaterials.Length - 1)];
                _skin.sharedMaterials = lSlots;
            }
            _bodyRenderers = _model.GetComponentsInChildren<Renderer>();

            Transform lNeck = _animator.GetBoneTransform(HumanBodyBones.Neck) ?? _animator.GetBoneTransform(HumanBodyBones.Head);
            _scarf = CreateTrail(lNeck, new Vector3(0f, 0f, -0.08f), 0.1f, 0.22f, UIFactory.RED, false);
            _handTrailL = CreateTrail(_animator.GetBoneTransform(HumanBodyBones.LeftHand), Vector3.zero, 0.12f, 0.1f, new Color(0.6f, 1f, 1f, 0.8f), true);
            _handTrailR = CreateTrail(_animator.GetBoneTransform(HumanBodyBones.RightHand), Vector3.zero, 0.12f, 0.1f, new Color(1f, 0.5f, 0.9f, 0.8f), true);
            _footTrailL = CreateTrail(_animator.GetBoneTransform(HumanBodyBones.LeftFoot), Vector3.zero, 0.1f, 0.12f, new Color(1f, 0.6f, 0.2f, 0.8f), true);
            _footTrailR = CreateTrail(_animator.GetBoneTransform(HumanBodyBones.RightFoot), Vector3.zero, 0.1f, 0.12f, new Color(1f, 0.6f, 0.2f, 0.8f), true);

            // Light form
            _light = Pivot("LightForm", transform, new Vector3(0f, 1f, 0f));
            Mats.Primitive(PrimitiveType.Sphere, _light, Vector3.zero, Vector3.one * 1.3f, Mats.Glow(new Color(5f, 6f, 6f), true, 0f, 1.5f));
            Mats.Primitive(PrimitiveType.Sphere, _light, new Vector3(0f, 0f, -1.6f), new Vector3(0.8f, 0.8f, 4.5f), Mats.Glow(new Color(1.5f, 2.5f, 4f, 0.7f), true));
            _lightTrail = CreateTrail(_light, Vector3.zero, 0.6f, 1.8f, new Color(2f, 4f, 5f), true);
            _light.gameObject.SetActive(false);

            // Energy cone while boosting (points backward around the runner)
            _cone = Pivot("EnergyCone", transform, new Vector3(0f, 1f, 0f));
            _coneMaterial = new Material(Shader.Find("Hyperspeed/EnergyCone"));
            GameObject lCone = new GameObject("Cone");
            lCone.transform.SetParent(_cone, false);
            lCone.AddComponent<MeshFilter>().sharedMesh = MeshUtil.Cone(1.7f, 0.2f, 6.5f, 24);
            lCone.AddComponent<MeshRenderer>().sharedMaterial = _coneMaterial;
            _cone.gameObject.SetActive(false);

            // Blob shadow
            _shadow = Mats.Primitive(PrimitiveType.Cylinder, null, Vector3.zero, new Vector3(1.4f, 0.01f, 1.4f), Mats.Glow(new Color(0.05f, 0f, 0.15f, 0.45f), false, 1f)).transform;
            _shadow.name = "BlobShadow";
            if (_player == null) _shadow.SetParent(transform, false);

            _dust = FX.CreateSystem("Dust", transform.position, Mats.Glow(new Color(1f, 1f, 1f, 0.5f), false, 1f), false);
            _dust.transform.SetParent(transform, false);
            ParticleSystem.MainModule lMain = _dust.main;
            lMain.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            lMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            lMain.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            lMain.maxParticles = 300;
            ParticleSystem.SizeOverLifetimeModule lSize = _dust.sizeOverLifetime;
            lSize.enabled = true;
            lSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            _sparks = FX.CreateSpikeSystem("Sparks", 600);
            _sparks.transform.SetParent(transform, false);
            _wind = FX.CreateSpikeSystem("WindSpikes", 300);
            _wind.transform.SetParent(transform, false);

            if (_player != null)
            {
                _player.OnJumped += () => { _squashVelocity += 1.2f; };
                _player.OnDoubleJumped += () => { _flip = 1f; };
                _player.OnLanded += pImpact => { _squashVelocity -= Mathf.Clamp(pImpact * 0.05f, 0.4f, 1.6f); EmitLandingSparks(pImpact); };
                _player.OnPunched += () => { _punch = 1f; };
                _player.OnDashed += () => { _dashSpin = 1f; _flash = 0.5f; SpawnAfterimages(); };
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
            lTrail.endWidth = pWidth * 0.1f;
            lTrail.minVertexDistance = 0.08f;
            lTrail.sharedMaterial = Mats.Glow(pColor, pAdditive);
            lTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lTrail.numCornerVertices = 2;
            return lTrail;
        }

        public void SetVictory() => _victory = 1f;

        private void OnDestroy()
        {
            if (_shadow != null) Destroy(_shadow.gameObject);
            _anim?.Dispose();
        }

        // ================================================================ display (title screen)

        public void TickDisplay(float pDt, float pSpeed)
        {
            _anim.ResetTargets();
            RunCycle(pDt, pSpeed, 0f);
            _anim.Apply(pDt, 16f);
            _pivot.localRotation = Quaternion.Euler(Mathf.Lerp(6f, 16f, pSpeed / 90f), 0f, Mathf.Sin(Time.time * 1.1f) * 5f);
            _scarf.time = 0.06f;
        }

        // ================================================================ gameplay

        public void Tick(float pDt)
        {
            if (_player == null) return;
            PlayerState lState = _player.State;
            bool lGrounded = lState == PlayerState.Ground || lState == PlayerState.Finished;
            Vector3 lUp = lGrounded || lState == PlayerState.Grind || lState == PlayerState.Zip ? _player.Up : _player.GravityUp;
            if (lState == PlayerState.Zip) lUp = _player.GravityUp;
            Vector3 lForward = _player.FacingDirection;

            Quaternion lTarget = Quaternion.LookRotation(Vector3.ProjectOnPlane(lForward, lUp).normalized, lUp);
            _facing = Quaternion.Slerp(_facing, lTarget, 1f - Mathf.Exp(-16f * pDt));
            transform.SetPositionAndRotation(_player.transform.position, _facing);

            // Angular velocity drives lean and spine lag (follow-through).
            Vector3 lFlatForward = Vector3.ProjectOnPlane(lForward, lUp).normalized;
            float lYawSpeed = pDt > 0f ? Vector3.SignedAngle(_lastForward, lFlatForward, lUp) / pDt : 0f;
            _lastForward = lFlatForward;
            _turnRate = Mathf.Lerp(_turnRate, Mathf.Clamp(lYawSpeed, -240f, 240f), 1f - Mathf.Exp(-8f * pDt));

            float lSpeed = _player.Speed;
            UpdateLightForm();
            UpdateCone(pDt);
            UpdateShadow();
            if (_lightForm) return;

            _flip = Mathf.MoveTowards(_flip, 0f, pDt * 2.8f);
            _dashSpin = Mathf.MoveTowards(_dashSpin, 0f, pDt * 3.2f);
            _punch = Mathf.MoveTowards(_punch, 0f, pDt * 4f);
            _crashSpin = Mathf.MoveTowards(_crashSpin, 0f, pDt * 1.8f);
            _flash = Mathf.MoveTowards(_flash, 0f, pDt * 4f);
            _victory = Mathf.MoveTowards(_victory, 0f, pDt * 0.4f);

            _anim.ResetTargets();
            float lTargetLean = -_turnRate * 0.12f - _player.Steer * 8f;
            float lTargetPitch = 0f;
            float lTargetYaw = 0f;
            float lSharpness = 14f;

            switch (lState)
            {
                case PlayerState.Frozen:
                    PoseReady();
                    lTargetPitch = 22f;
                    break;
                case PlayerState.Grind:
                    PoseSurf();
                    lTargetYaw = 75f;
                    lTargetLean = Mathf.Sin(Time.time * 7f) * 4f;
                    EmitFootSparks(UIFactory.YELLOW, 2);
                    break;
                case PlayerState.Zip:
                    PoseHang();
                    lTargetLean = Mathf.Sin(Time.time * 3f) * 8f;
                    lTargetPitch = -10f;
                    break;
                case PlayerState.Crash:
                    PoseCrash();
                    break;
                case PlayerState.WallRun:
                    RunCycle(pDt, lSpeed * 1.15f, 0f);
                    lTargetLean = _player.WallSide * -28f;
                    lTargetPitch = 12f;
                    EmitFootSparks(Color.white, 1);
                    break;
                case PlayerState.Air:
                    if (_player.Diving) { PoseDive(); lTargetPitch = 65f; }
                    else PoseAir();
                    break;
                default:
                    if (_player.Drifting)
                    {
                        PoseDrift();
                        lTargetYaw = -_player.DriftDirection * 50f;
                        lTargetLean = _player.DriftDirection * 18f - _turnRate * 0.05f;
                        lTargetPitch = 8f;
                        EmitDriftSparks();
                    }
                    else
                    {
                        RunCycle(pDt, lSpeed, _player.Sliding ? 1f : 0f);
                        lTargetPitch = Mathf.Lerp(4f, 20f, Mathf.Clamp01(lSpeed / 90f));
                    }
                    break;
            }

            if (_punch > 0f) PosePunch(Mathf.Sin(_punch * Mathf.PI));
            if (_victory > 0f && lState == PlayerState.Finished) PoseVictory();
            if (_dashSpin > 0f) { PoseDash(); lSharpness = 24f; }

            // Follow-through : spine and head lag behind turns, head stays level-ish.
            _spineLag = Mathf.Lerp(_spineLag, Mathf.Clamp(_turnRate / 240f, -1f, 1f), 1f - Mathf.Exp(-6f * pDt));
            _anim.Add(HumanoidAnimator.M.SpineLR, -_spineLag * 0.35f);
            _anim.Add(HumanoidAnimator.M.ChestTwist, _spineLag * 0.3f);
            _anim.Add(HumanoidAnimator.M.NeckTilt, (_lean / 30f) * 0.5f);
            _anim.Add(HumanoidAnimator.M.HeadTurn, _spineLag * 0.4f);
            _anim.Apply(pDt, lSharpness);

            _lean = Mathf.Lerp(_lean, Mathf.Clamp(lTargetLean, -40f, 40f), 1f - Mathf.Exp(-9f * pDt));
            _pitch = Mathf.Lerp(_pitch, lTargetPitch, 1f - Mathf.Exp(-9f * pDt));
            _yawOffset = Mathf.LerpAngle(_yawOffset, lTargetYaw, 1f - Mathf.Exp(-7f * pDt));
            float lFlip = _flip > 0f ? Mathf.SmoothStep(0f, 1f, 1f - _flip) * 360f : 0f;
            float lSpin = _dashSpin > 0f ? Mathf.SmoothStep(0f, 1f, 1f - _dashSpin) * 360f : 0f;
            float lTumble = lState == PlayerState.Crash ? _crashSpin * 540f : 0f;
            _pivot.localRotation = Quaternion.Euler(0f, _yawOffset, 0f) * Quaternion.Euler(_pitch + lFlip + lTumble, 0f, _lean + lSpin);
            _pivot.localPosition = new Vector3(0f, (lFlip > 0f || lSpin > 0f) ? 0.5f : 0f, 0f);

            // Subtle squash only (max 6 %).
            float lForce = (1f - _squash) * 260f - _squashVelocity * 14f;
            _squashVelocity += lForce * pDt;
            _squash = Mathf.Clamp(_squash + _squashVelocity * pDt, 0.94f, 1.06f);
            _pivot.localScale = new Vector3(1f / Mathf.Sqrt(_squash), _squash, 1f / Mathf.Sqrt(_squash));

            bool lInvulnerableBlink = _player.Invulnerable && Mathf.Repeat(Time.time * 12f, 1f) > 0.5f;
            _block.SetFloat(FLASH, _flash > 0.3f || lInvulnerableBlink ? 0.85f : 0f);
            foreach (Renderer lRenderer in _bodyRenderers) lRenderer.SetPropertyBlock(_block);

            bool lFast = lSpeed > 55f;
            _handTrailL.emitting = lFast;
            _handTrailR.emitting = lFast;
            _footTrailL.emitting = lSpeed > 75f && lGrounded;
            _footTrailR.emitting = lSpeed > 75f && lGrounded;
            _scarf.time = Mathf.Clamp(1.4f / Mathf.Max(lSpeed, 1f), 0.02f, 0.12f);
            EmitWindSpikes(lSpeed);
        }

        // ================================================================ poses

        private void RunCycle(float pDt, float pSpeed, float pSlide)
        {
            float lS = Mathf.Clamp01(pSpeed / 80f);
            float lPrev = _phase;
            _phase += pDt * Mathf.Lerp(8.5f, 19f, lS);
            float lSin = Mathf.Sin(_phase);
            float lCos = Mathf.Cos(_phase);
            float lStride = Mathf.Lerp(32f, 62f, lS);

            // Legs : the forward-swinging leg folds its knee (arc), the planted one stays extended.
            float lLeftAngle = 10f + lSin * lStride;
            float lRightAngle = 10f - lSin * lStride;
            float lLeftKnee = 18f + Mathf.Max(0f, lCos) * Mathf.Lerp(45f, 110f, lS);
            float lRightKnee = 18f + Mathf.Max(0f, -lCos) * Mathf.Lerp(45f, 110f, lS);
            _anim.Set(HumanoidAnimator.M.LUpLegFB, HumanoidAnimator.Leg(lLeftAngle));
            _anim.Set(HumanoidAnimator.M.RUpLegFB, HumanoidAnimator.Leg(lRightAngle));
            _anim.Set(HumanoidAnimator.M.LLowLeg, HumanoidAnimator.Knee(lLeftKnee));
            _anim.Set(HumanoidAnimator.M.RLowLeg, HumanoidAnimator.Knee(lRightKnee));
            _anim.Set(HumanoidAnimator.M.LFoot, -0.2f * lCos);
            _anim.Set(HumanoidAnimator.M.RFoot, 0.2f * lCos);

            if (pSpeed > 62f)
            {
                // Anime sprint : arms swept back, chest forward.
                _anim.Set(HumanoidAnimator.M.LArmFB, 0.85f + lSin * 0.05f);
                _anim.Set(HumanoidAnimator.M.RArmFB, 0.85f - lSin * 0.05f);
                _anim.Set(HumanoidAnimator.M.LArmDU, -0.35f);
                _anim.Set(HumanoidAnimator.M.RArmDU, -0.35f);
                _anim.Set(HumanoidAnimator.M.LForearm, 0.85f);
                _anim.Set(HumanoidAnimator.M.RForearm, 0.85f);
                _anim.Set(HumanoidAnimator.M.SpineFB, -0.6f);
                _anim.Set(HumanoidAnimator.M.ChestFB, -0.5f);
                _anim.SetFist(0.2f);
            }
            else
            {
                float lArm = Mathf.Lerp(0.35f, 0.75f, lS);
                _anim.Set(HumanoidAnimator.M.LArmFB, lSin * lArm);
                _anim.Set(HumanoidAnimator.M.RArmFB, -lSin * lArm);
                _anim.Set(HumanoidAnimator.M.LArmDU, -0.6f + Mathf.Abs(lSin) * 0.1f);
                _anim.Set(HumanoidAnimator.M.RArmDU, -0.6f + Mathf.Abs(lSin) * 0.1f);
                _anim.Set(HumanoidAnimator.M.LForearm, HumanoidAnimator.Elbow(70f + lSin * 20f));
                _anim.Set(HumanoidAnimator.M.RForearm, HumanoidAnimator.Elbow(70f - lSin * 20f));
                _anim.Set(HumanoidAnimator.M.SpineFB, -0.3f * lS);
                _anim.SetFist(0.7f);
            }
            _anim.Set(HumanoidAnimator.M.SpineTwist, -lSin * 0.18f);
            _anim.Set(HumanoidAnimator.M.ChestTwist, -lSin * 0.15f);
            _anim.Set(HumanoidAnimator.M.NeckNod, 0.1f);
            _anim.BodyOffsetY = -0.04f - Mathf.Abs(lCos) * 0.03f * (1f + lS) - pSlide * 0.25f;

            if (Mathf.Floor(lPrev / Mathf.PI) != Mathf.Floor(_phase / Mathf.PI) && _player != null && _player.State == PlayerState.Ground)
            {
                EmitDust(2);
                AudioManager.Play("sfx_step", 0.22f, 1f + lS * 0.3f, 0.12f);
            }
        }

        private void PoseReady()
        {
            _anim.Set(HumanoidAnimator.M.LUpLegFB, HumanoidAnimator.Leg(70f));
            _anim.Set(HumanoidAnimator.M.LLowLeg, HumanoidAnimator.Knee(100f));
            _anim.Set(HumanoidAnimator.M.RUpLegFB, HumanoidAnimator.Leg(-15f));
            _anim.Set(HumanoidAnimator.M.RLowLeg, HumanoidAnimator.Knee(60f));
            _anim.Set(HumanoidAnimator.M.LArmFB, 0.6f);
            _anim.Set(HumanoidAnimator.M.RArmFB, -0.6f);
            _anim.Set(HumanoidAnimator.M.LForearm, HumanoidAnimator.Elbow(80f));
            _anim.Set(HumanoidAnimator.M.RForearm, HumanoidAnimator.Elbow(80f));
            _anim.Set(HumanoidAnimator.M.SpineFB, -0.7f);
            _anim.Set(HumanoidAnimator.M.NeckNod, 0.4f);
            _anim.SetFist(0.8f);
            _anim.BodyOffsetY = -0.3f;
        }

        private void PoseAir()
        {
            float lUp = Vector3.Dot(_player.Velocity, _player.GravityUp);
            float lRise = Mathf.Clamp01(lUp / 14f);
            // Anticipation : legs come forward when the ground gets close.
            float lLand = LandingAnticipation();
            float lTuck = Mathf.Lerp(30f, 85f, lRise);
            _anim.Set(HumanoidAnimator.M.LUpLegFB, HumanoidAnimator.Leg(Mathf.Lerp(lTuck, 45f, lLand)));
            _anim.Set(HumanoidAnimator.M.LLowLeg, HumanoidAnimator.Knee(Mathf.Lerp(100f, 55f, lLand)));
            _anim.Set(HumanoidAnimator.M.RUpLegFB, HumanoidAnimator.Leg(Mathf.Lerp(-10f, 30f, lLand)));
            _anim.Set(HumanoidAnimator.M.RLowLeg, HumanoidAnimator.Knee(Mathf.Lerp(70f, 50f, lLand)));
            _anim.Set(HumanoidAnimator.M.LArmDU, Mathf.Lerp(0.2f, 0.55f, lRise));
            _anim.Set(HumanoidAnimator.M.RArmDU, Mathf.Lerp(0.2f, 0.55f, lRise));
            _anim.Set(HumanoidAnimator.M.LArmFB, -0.2f);
            _anim.Set(HumanoidAnimator.M.RArmFB, 0.3f);
            _anim.Set(HumanoidAnimator.M.LForearm, HumanoidAnimator.Elbow(35f));
            _anim.Set(HumanoidAnimator.M.RForearm, HumanoidAnimator.Elbow(35f));
            _anim.Set(HumanoidAnimator.M.SpineFB, Mathf.Lerp(-0.2f, 0.2f, lRise));
            _anim.SetFist(0.3f);
            _anim.BodyOffsetY = -lLand * 0.12f;
        }

        private float LandingAnticipation()
        {
            if (Vector3.Dot(_player.Velocity, _player.GravityUp) > 0f) return 0f;
            RaycastHit lHit;
            if (Physics.Raycast(transform.position + _player.GravityUp * 0.5f, -_player.GravityUp, out lHit, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return 1f - Mathf.Clamp01((lHit.distance - 0.5f) / 3.5f);
            return 0f;
        }

        private void PoseDive()
        {
            _anim.Set(HumanoidAnimator.M.LArmDU, 1f);
            _anim.Set(HumanoidAnimator.M.RArmDU, 1f);
            _anim.Set(HumanoidAnimator.M.LArmFB, -0.7f);
            _anim.Set(HumanoidAnimator.M.RArmFB, -0.7f);
            _anim.Set(HumanoidAnimator.M.LForearm, 1f);
            _anim.Set(HumanoidAnimator.M.RForearm, 1f);
            _anim.Set(HumanoidAnimator.M.LUpLegFB, HumanoidAnimator.Leg(-12f));
            _anim.Set(HumanoidAnimator.M.RUpLegFB, HumanoidAnimator.Leg(-8f));
            _anim.Set(HumanoidAnimator.M.LLowLeg, HumanoidAnimator.Knee(10f));
            _anim.Set(HumanoidAnimator.M.RLowLeg, HumanoidAnimator.Knee(25f));
            _anim.Set(HumanoidAnimator.M.NeckNod, 0.6f);
            _anim.SetFist(0f);
        }

        private void PoseDrift()
        {
            int lDir = _player.DriftDirection;
            _anim.Set(HumanoidAnimator.M.LUpLegFB, HumanoidAnimator.Leg(55f));
            _anim.Set(HumanoidAnimator.M.RUpLegFB, HumanoidAnimator.Leg(20f));
            _anim.Set(HumanoidAnimator.M.LLowLeg, HumanoidAnimator.Knee(95f));
            _anim.Set(HumanoidAnimator.M.RLowLeg, HumanoidAnimator.Knee(60f));
            _anim.Set(HumanoidAnimator.M.LUpLegIO, 0.4f);
            _anim.Set(HumanoidAnimator.M.RUpLegIO, 0.4f);
            // Inner hand reaches the ground, outer arm balances.
            HumanoidAnimator.M lInnerDU = lDir > 0 ? HumanoidAnimator.M.RArmDU : HumanoidAnimator.M.LArmDU;
            HumanoidAnimator.M lOuterDU = lDir > 0 ? HumanoidAnimator.M.LArmDU : HumanoidAnimator.M.RArmDU;
            HumanoidAnimator.M lOuterFB = lDir > 0 ? HumanoidAnimator.M.LArmFB : HumanoidAnimator.M.RArmFB;
            _anim.Set(lInnerDU, -0.2f);
            _anim.Set(lOuterDU, 0.6f);
            _anim.Set(lOuterFB, 0.5f);
            _anim.Set(lDir > 0 ? HumanoidAnimator.M.RForearm : HumanoidAnimator.M.LForearm, 1f);
            _anim.Set(HumanoidAnimator.M.SpineLR, lDir * 0.6f);
            _anim.Set(HumanoidAnimator.M.SpineFB, -0.5f);
            _anim.Set(HumanoidAnimator.M.HeadTurn, -lDir * 0.6f);
            _anim.SetFist(0.2f);
            _anim.BodyOffsetY = -0.35f;
        }

        private void PoseSurf()
        {
            _anim.Set(HumanoidAnimator.M.LUpLegFB, HumanoidAnimator.Leg(35f));
            _anim.Set(HumanoidAnimator.M.RUpLegFB, HumanoidAnimator.Leg(35f));
            _anim.Set(HumanoidAnimator.M.LLowLeg, HumanoidAnimator.Knee(70f));
            _anim.Set(HumanoidAnimator.M.RLowLeg, HumanoidAnimator.Knee(70f));
            _anim.Set(HumanoidAnimator.M.LUpLegIO, 0.7f);
            _anim.Set(HumanoidAnimator.M.RUpLegIO, 0.7f);
            _anim.Set(HumanoidAnimator.M.LArmDU, 0.35f + Mathf.Sin(Time.time * 6f) * 0.08f);
            _anim.Set(HumanoidAnimator.M.RArmDU, 0.35f - Mathf.Sin(Time.time * 6f) * 0.08f);
            _anim.Set(HumanoidAnimator.M.LForearm, 0.9f);
            _anim.Set(HumanoidAnimator.M.RForearm, 0.9f);
            _anim.Set(HumanoidAnimator.M.HeadTurn, -0.8f);
            _anim.Set(HumanoidAnimator.M.SpineFB, -0.3f);
            _anim.SetFist(0f);
            _anim.BodyOffsetY = -0.2f;
        }

        private void PoseHang()
        {
            float lSwing = Mathf.Sin(Time.time * 4f);
            _anim.Set(HumanoidAnimator.M.LArmDU, 1.05f);
            _anim.Set(HumanoidAnimator.M.RArmDU, 1.05f);
            _anim.Set(HumanoidAnimator.M.LArmFB, -0.25f);
            _anim.Set(HumanoidAnimator.M.RArmFB, -0.25f);
            _anim.Set(HumanoidAnimator.M.LForearm, 0.9f);
            _anim.Set(HumanoidAnimator.M.RForearm, 0.9f);
            _anim.Set(HumanoidAnimator.M.LUpLegFB, HumanoidAnimator.Leg(35f + lSwing * 15f));
            _anim.Set(HumanoidAnimator.M.RUpLegFB, HumanoidAnimator.Leg(25f + lSwing * 15f));
            _anim.Set(HumanoidAnimator.M.LLowLeg, HumanoidAnimator.Knee(40f));
            _anim.Set(HumanoidAnimator.M.RLowLeg, HumanoidAnimator.Knee(30f));
            _anim.SetFist(1f);
        }

        private void PoseCrash()
        {
            float lT = Time.time * 14f;
            _anim.Set(HumanoidAnimator.M.LArmDU, 0.8f + Mathf.Sin(lT) * 0.3f);
            _anim.Set(HumanoidAnimator.M.RArmDU, 0.6f + Mathf.Cos(lT) * 0.3f);
            _anim.Set(HumanoidAnimator.M.LUpLegFB, HumanoidAnimator.Leg(50f + Mathf.Sin(lT * 1.3f) * 30f));
            _anim.Set(HumanoidAnimator.M.RUpLegFB, HumanoidAnimator.Leg(10f + Mathf.Cos(lT * 1.1f) * 30f));
            _anim.Set(HumanoidAnimator.M.LLowLeg, HumanoidAnimator.Knee(80f));
            _anim.Set(HumanoidAnimator.M.RLowLeg, HumanoidAnimator.Knee(40f));
            _anim.SetFist(0f);
        }

        private void PoseDash()
        {
            _anim.Set(HumanoidAnimator.M.LArmFB, 0.95f);
            _anim.Set(HumanoidAnimator.M.RArmFB, 0.95f);
            _anim.Set(HumanoidAnimator.M.LArmDU, -0.2f);
            _anim.Set(HumanoidAnimator.M.RArmDU, -0.2f);
            _anim.Set(HumanoidAnimator.M.LForearm, 1f);
            _anim.Set(HumanoidAnimator.M.RForearm, 1f);
            _anim.Set(HumanoidAnimator.M.LUpLegFB, HumanoidAnimator.Leg(60f));
            _anim.Set(HumanoidAnimator.M.RUpLegFB, HumanoidAnimator.Leg(-20f));
            _anim.Set(HumanoidAnimator.M.LLowLeg, HumanoidAnimator.Knee(90f));
            _anim.Set(HumanoidAnimator.M.RLowLeg, HumanoidAnimator.Knee(30f));
            _anim.Set(HumanoidAnimator.M.SpineFB, -0.8f);
        }

        private void PosePunch(float pAmount)
        {
            _anim.Set(HumanoidAnimator.M.RArmDU, Mathf.Lerp(-0.4f, 0.35f, pAmount));
            _anim.Set(HumanoidAnimator.M.RArmFB, Mathf.Lerp(0.3f, -1f, pAmount));
            _anim.Set(HumanoidAnimator.M.RForearm, Mathf.Lerp(0f, 1f, pAmount));
            _anim.Set(HumanoidAnimator.M.LArmFB, Mathf.Lerp(0f, 0.7f, pAmount));
            _anim.Set(HumanoidAnimator.M.SpineTwist, -0.6f * pAmount);
            _anim.Set(HumanoidAnimator.M.ChestTwist, -0.6f * pAmount);
            _anim.SetFist(1f);
        }

        private void PoseVictory()
        {
            _anim.Set(HumanoidAnimator.M.LArmDU, 1f);
            _anim.Set(HumanoidAnimator.M.RArmDU, 0.9f);
            _anim.Set(HumanoidAnimator.M.LArmFB, -0.3f);
            _anim.Set(HumanoidAnimator.M.RArmFB, -0.2f);
            _anim.Set(HumanoidAnimator.M.LForearm, 1f);
            _anim.Set(HumanoidAnimator.M.RForearm, 0.6f);
            _anim.SetFist(1f);
        }

        // ================================================================ effects

        private void UpdateLightForm()
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
                FX.Sparks(transform.position + transform.up, Color.white, 60, 35f, 0.7f, 0.5f);
                FX.Shockwave(transform.position + transform.up, transform.forward, new Color(0.6f, 1f, 1f), 18f, 0.35f);
            }
            if (_lightForm)
            {
                float lPulse = 1f + Mathf.Sin(Time.time * 40f) * 0.1f;
                _light.localScale = new Vector3(lPulse, lPulse, lPulse * (1f + _player.Speed / 120f));
                _lightTrail.time = Mathf.Lerp(0.4f, 1f, (_player.Speed - 90f) / 80f);
            }
        }

        private void UpdateCone(float pDt)
        {
            float lBoost = _player.BoostAmount;
            bool lShow = lBoost > 0.02f;
            if (_cone.gameObject.activeSelf != lShow) _cone.gameObject.SetActive(lShow);
            if (!lShow) return;
            _cone.localRotation = Quaternion.identity;
            _cone.localScale = new Vector3(1f, 1f, 0.6f + lBoost * 0.8f) * (0.9f + Mathf.Sin(Time.time * 30f) * 0.05f);
            _coneMaterial.SetFloat("_Intensity", lBoost);
        }

        private void SpawnAfterimages()
        {
            if (_skin == null) return;
            for (int i = 0; i < 3; i++)
            {
                Mesh lMesh = new Mesh();
                _skin.BakeMesh(lMesh);
                GameObject lGhost = new GameObject("Afterimage");
                lGhost.transform.SetPositionAndRotation(_skin.transform.position - _facing * Vector3.forward * (i * 0.9f), _skin.transform.rotation);
                lGhost.transform.localScale = _skin.transform.lossyScale;
                lGhost.AddComponent<MeshFilter>().sharedMesh = lMesh;
                Color lColor = i % 2 == 0 ? new Color(0.3f, 1f, 1f, 0.5f) : new Color(1f, 0.3f, 0.9f, 0.5f);
                Material[] lMaterials = new Material[lMesh.subMeshCount];
                for (int j = 0; j < lMaterials.Length; j++) lMaterials[j] = Mats.Glow(lColor * 1.5f, true);
                lGhost.AddComponent<MeshRenderer>().sharedMaterials = lMaterials;
                lGhost.AddComponent<Fader>().Init(0.25f + i * 0.08f, lMesh);
            }
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
            int lCount = 3 + _player.DriftTier * 2;
            for (int i = 0; i < lCount; i++)
            {
                float lSide = (i % 2 == 0 ? -1f : 1f) * 0.25f;
                lParams.position = transform.position + transform.right * lSide - transform.forward * 0.2f + transform.up * 0.05f;
                lParams.velocity = -transform.forward * Random.Range(6f, 16f) + transform.up * Random.Range(1f, 6f) + transform.right * Random.Range(-5f, 5f);
                lParams.startSize = Random.Range(0.08f, 0.2f);
                lParams.startLifetime = Random.Range(0.15f, 0.35f);
                _sparks.Emit(lParams, 1);
            }
        }

        private void EmitFootSparks(Color pColor, int pCount)
        {
            ParticleSystem.EmitParams lParams = new ParticleSystem.EmitParams();
            lParams.startColor = pColor * 2.5f;
            for (int i = 0; i < pCount; i++)
            {
                lParams.position = transform.position + Random.insideUnitSphere * 0.2f;
                lParams.velocity = -transform.forward * Random.Range(5f, 14f) + Random.insideUnitSphere * 4f + transform.up * 2f;
                lParams.startSize = Random.Range(0.08f, 0.16f);
                lParams.startLifetime = Random.Range(0.12f, 0.3f);
                _sparks.Emit(lParams, 1);
            }
        }

        private void EmitLandingSparks(float pImpact)
        {
            ParticleSystem.EmitParams lParams = new ParticleSystem.EmitParams();
            int lCount = Mathf.Clamp((int)(pImpact * 0.8f), 6, 30);
            for (int i = 0; i < lCount; i++)
            {
                Vector2 lCircle = Random.insideUnitCircle.normalized;
                Vector3 lDir = transform.right * lCircle.x + transform.forward * lCircle.y;
                lParams.position = transform.position + lDir * 0.3f;
                lParams.velocity = lDir * Random.Range(6f, 16f) + transform.up * Random.Range(1f, 4f);
                lParams.startColor = new Color(2f, 2f, 2.2f);
                lParams.startSize = Random.Range(0.08f, 0.18f);
                lParams.startLifetime = Random.Range(0.15f, 0.3f);
                _sparks.Emit(lParams, 1);
            }
        }

        // Spiky wind streaks around the runner at speed (they shoot backward past the camera).
        private void EmitWindSpikes(float pSpeed)
        {
            float lRate = Mathf.Clamp01((pSpeed - 45f) / 60f);
            if (lRate <= 0f) return;
            int lCount = Random.value < lRate * 2.5f ? Mathf.CeilToInt(lRate * 3f) : 0;
            ParticleSystem.EmitParams lParams = new ParticleSystem.EmitParams();
            for (int i = 0; i < lCount; i++)
            {
                Vector2 lCircle = Random.insideUnitCircle.normalized * Random.Range(1.5f, 4.5f);
                lParams.position = transform.position + transform.up * (1f + lCircle.y) + transform.right * lCircle.x + transform.forward * Random.Range(2f, 8f);
                lParams.velocity = -transform.forward * Random.Range(40f, 90f);
                float lHue = Random.value;
                lParams.startColor = lHue < 0.33f ? new Color(0.4f, 1.6f, 2f) : lHue < 0.66f ? new Color(2f, 0.5f, 1.8f) : new Color(2f, 2f, 2f);
                lParams.startSize = Random.Range(0.04f, 0.1f);
                lParams.startLifetime = Random.Range(0.12f, 0.25f);
                _wind.Emit(lParams, 1);
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
                float lSize = Mathf.Lerp(1.4f, 0.5f, lHit.distance / 30f);
                _shadow.localScale = new Vector3(lSize, 0.01f, lSize);
            }
            else _shadow.gameObject.SetActive(false);
        }
    }

    // Fades out and destroys a temporary renderer (afterimages).
    public class Fader : MonoBehaviour
    {
        private float _duration;
        private float _time;
        private Mesh _mesh;
        private Renderer _renderer;
        private MaterialPropertyBlock _block;

        public void Init(float pDuration, Mesh pMesh)
        {
            _duration = pDuration;
            _mesh = pMesh;
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            _time += Time.deltaTime;
            float lAlpha = 1f - _time / _duration;
            _block.SetColor(Mats.COLOR, new Color(0.6f, 1.6f, 2f, Mathf.Max(0f, lAlpha) * 0.6f));
            _renderer.SetPropertyBlock(_block);
            transform.localScale *= 1f + Time.deltaTime * 0.6f;
            if (_time >= _duration)
            {
                Destroy(_mesh);
                Destroy(gameObject);
            }
        }
    }
}
