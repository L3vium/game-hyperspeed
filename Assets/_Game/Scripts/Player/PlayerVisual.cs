using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Runner visual : the Mixamo humanoid (GameAssets.runnerPrefab) driven by the generated Runner.controller, then a
    // procedural layer applied after the Animator. Flips, swirls and tumbles turn around the centre of mass, ground
    // leans around the feet (never into the floor) ; turns run from the head down to the hips while the root catches up ;
    // bank in curves, arrow posture and swaying hands at speed, squash & stretch, teeter at the edges, head that reads
    // the trajectory, two verlet ribbons from the neck. Effects : aura streaming backward, shadow afterimages, hand trails.
    public class PlayerVisual : MonoBehaviour
    {
        private const float MODEL_HEIGHT = 2.7f;
        private const float AFTERIMAGE_INTERVAL = 0.045f;
        private const float AFTERIMAGE_LIFE = 0.22f;
        private const float AFTERIMAGE_SPEED = 85f;
        private const int AFTERIMAGE_POOL = 48;
        private const float SWIRL_TIME = 0.42f;
        private const float FLIP_TIME = 0.55f;
        private const float TUMBLE_TIME = 0.7f;
        private const float PUNCH_TIME = 0.28f;
        private const int RIBBON_POINTS = 16;

        private static readonly int FLASH = Shader.PropertyToID("_Flash");
        private static readonly int AURA_INTENSITY = Shader.PropertyToID("_Intensity");
        private static readonly int AURA_DRAG = Shader.PropertyToID("_Drag");
        private static readonly int AURA_MOTION = Shader.PropertyToID("_MotionDir");
        private static readonly int P_SPEED = Animator.StringToHash("Speed");
        private static readonly int P_VERTICAL = Animator.StringToHash("Vertical");
        private static readonly int P_GROUNDED = Animator.StringToHash("Grounded");
        private static readonly int P_GLIDE = Animator.StringToHash("Glide");
        private static readonly int P_DIVE = Animator.StringToHash("Dive");
        private static readonly int P_SLIDE = Animator.StringToHash("Slide");
        private static readonly int P_GRIND = Animator.StringToHash("Grind");
        private static readonly int P_HANG = Animator.StringToHash("Hang");
        private static readonly int P_WALLRUN = Animator.StringToHash("WallRun");
        private static readonly int P_WALLCLIMB = Animator.StringToHash("WallClimb");
        private static readonly int P_WALLSLIDE = Animator.StringToHash("WallSlide");
        private static readonly int P_WIN = Animator.StringToHash("Win");
        private static readonly int P_LOSE = Animator.StringToHash("Lose");
        private static readonly int P_PRERACE = Animator.StringToHash("PreRace");
        private static readonly int P_JUMP = Animator.StringToHash("Jump");
        private static readonly int P_FLIP = Animator.StringToHash("Flip");
        private static readonly int P_LAND = Animator.StringToHash("Land");
        private static readonly int P_LAND_HARD = Animator.StringToHash("LandHard");
        private static readonly int P_LAND_ROLL = Animator.StringToHash("LandRoll");
        private static readonly int P_STUMBLE = Animator.StringToHash("Stumble");
        private static readonly int P_TAUNT = Animator.StringToHash("Taunt");

        public Transform Root { get; private set; }

        private PlayerController _player;
        private Transform _body;
        private Transform _model;
        private Animator _animator;
        private bool _animated;
        private bool _clipFlip;
        private float _comHeight = 1f;
        private Renderer[] _bodyRenderers;
        private Renderer[] _auraRenderers;
        private Material _auraMaterial;
        private MaterialPropertyBlock _block;
        private Transform _shadow;
        private ParticleSystem _dust;
        private ParticleSystem _sparks;
        private TrailRenderer _trailL, _trailR;
        private Transform _wingL, _wingR;
        private TrailRenderer _helixA, _helixB;
        private Transform _helixAnchorA, _helixAnchorB;
        private TrailRenderer _skidL, _skidR;
        private LightTrail _lightWake;
        // Boost cone : a sheath of wind around the runner, tip ahead, open behind.
        private Transform _cone;
        private Material _coneMaterial;
        private Material _coneInnerMaterial;
        private float _coneAmount;
        private float _turnSwirl;
        private float _windPhase;
        private float _smokeTimer;

        // Humanoid bones (null on the placeholder).
        private Transform _hips, _spine, _chest, _neck, _head;
        private Transform _upperLegL, _lowerLegL, _footL, _upperLegR, _lowerLegR, _footR;

        // Air tricks : flips, back flips, corkscrews, spins and side flips sized to the time in the air, body tucked.
        private enum Trick { FrontFlip, BackFlip, Cork, Spin, SideFlip }
        private float _trickT = 1f;
        private float _trickDuration = 1f;
        private int _trickTurns = 1;
        private Trick _trick;
        private int _trickIndex;
        private bool _trickedThisAir;
        private float _tuck;
        // Secondary motion : the upper body lags behind accelerations on springs (a light active ragdoll).
        private Vector3 _lastVelocity;
        private Vector2 _lean;
        private Vector2 _leanVelocity;
        private bool _pendingWin;
        private PlayerLightBall _ball;
        private Transform _upperArmL, _lowerArmL, _handL, _upperArmR, _lowerArmR, _handR;
        private Vector3 _headForward = Vector3.forward;

        // Procedural state.
        private Quaternion _facing = Quaternion.identity;
        private Vector3 _lastHeading = Vector3.forward;
        private float _yawRate;
        private float _turnLead;
        private float _bank;
        private float _pitch;
        private float _pivotH;
        private float _arrow;
        private float _swayPhase;
        private float _openArms;
        private float _teeter;
        private float _swirlT = 1f;
        private float _swirlDir = 1f;
        private bool _swirlAir;
        private float _flipT = 1f;
        private float _tumbleT = 1f;
        private float _punchT = 1f;
        private float _stretch = 1f;
        private float _stretchVelocity;
        private float _zStretch = 1f;
        private float _zStretchVelocity;
        private float _flash;
        private float _aura;
        private float _auraDeploy;
        private float _stepPhase;
        private Vector3 _lookTarget;

        private class Ghost
        {
            public Transform transform;
            public MeshRenderer renderer;
            public Mesh mesh;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public float age = 1f;
        }

        private Ghost[] _ghosts;
        private int _ghostCursor;
        private float _ghostTimer;
        private SkinnedMeshRenderer[] _skins;
        private MeshFilter[] _staticMeshes;
        private Color _ghostColor = new Color(0.05f, 0.02f, 0.14f, 0.6f);

        private class Ribbon
        {
            public LineRenderer line;
            public Vector3 offset;
            public Vector3[] points = new Vector3[RIBBON_POINTS];
            public Vector3[] previous = new Vector3[RIBBON_POINTS];
            public float segment;
        }

        private Ribbon[] _ribbons;
        private Vector3 _lastRootPosition;
        private bool _ribbonsReset = true;

        public static PlayerVisual Create(PlayerController pPlayer)
        {
            PlayerVisual lVisual = new GameObject("PlayerVisual").AddComponent<PlayerVisual>();
            lVisual.Build(pPlayer);
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
            _body = Pivot("Body", transform, Vector3.zero);

            if (!BuildRunner()) BuildPlaceholder();
            _bodyRenderers = _model.GetComponentsInChildren<Renderer>();
            _skins = _model.GetComponentsInChildren<SkinnedMeshRenderer>();
            _staticMeshes = _model.GetComponentsInChildren<MeshFilter>();

            BuildAura();
            BuildGhosts();
            BuildTrails();
            BuildRibbons();
            BuildCone();

            _shadow = Mats.Primitive(PrimitiveType.Cylinder, null, Vector3.zero, new Vector3(1.4f, 0.01f, 1.4f), Mats.Glow(new Color(0.05f, 0f, 0.15f, 0.45f), false, 1f)).transform;
            _shadow.name = "BlobShadow";

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

            _player.OnJumped += () =>
            {
                _stretchVelocity += 2.8f;
                // A landing left pending must never play in the air.
                if (_animated)
                {
                    _animator.ResetTrigger(P_LAND);
                    _animator.ResetTrigger(P_LAND_HARD);
                    _animator.ResetTrigger(P_LAND_ROLL);
                }
                Trigger(P_JUMP);
            };
            _player.OnDoubleJumped += () =>
            {
                _stretchVelocity += 1.9f;
                if (_clipFlip) Trigger(P_FLIP);
                else _flipT = 0f;
            };
            _player.OnLanded += OnLanded;
            _player.OnPunched += () => { _punchT = 0f; };
            _player.OnDashed += OnDashed;
            _player.OnCrashed += () => { _tumbleT = 0f; _flash = 1f; Trigger(P_STUMBLE); };
            _player.OnHyper += () =>
            {
                _auraDeploy = 0f;
                FX.Shockwave(transform.position + transform.up * _comHeight, transform.forward, new Color(0.6f, 1f, 1f), 14f, 0.3f);
            };
            _player.OnRespawned += Snap;
            Snap();
        }

        // ---------------------------------------------------------------- model

        private bool BuildRunner()
        {
            GameObject lPrefab = GameAssets.Instance.runnerPrefab;
            if (lPrefab == null) return false;
            GameObject lInstance = Instantiate(lPrefab, _body);
            lInstance.name = "Runner";
            _model = lInstance.transform;
            _model.localPosition = Vector3.zero;
            _model.localRotation = Quaternion.identity;

            // Normalize to the gameplay height, feet on the root (measured in the bind pose).
            Renderer[] lRenderers = lInstance.GetComponentsInChildren<Renderer>();
            if (lRenderers.Length > 0)
            {
                Bounds lBounds = lRenderers[0].bounds;
                foreach (Renderer lRenderer in lRenderers) lBounds.Encapsulate(lRenderer.bounds);
                float lScale = lBounds.size.y > 0.3f && lBounds.size.y < 20f ? MODEL_HEIGHT / lBounds.size.y : 1f;
                _model.localScale *= lScale;
                _model.position -= transform.up * ((lBounds.min.y - transform.position.y) * lScale);
            }
            Toonify(lRenderers);

            _animator = lInstance.GetComponentInChildren<Animator>();
            if (_animator == null) _animator = lInstance.AddComponent<Animator>();
            _animator.runtimeAnimatorController = GameAssets.Instance.runnerController;
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animated = _animator.runtimeAnimatorController != null;
            if (_animated)
                foreach (AnimationClip lClip in _animator.runtimeAnimatorController.animationClips)
                    if (lClip != null && lClip.name == "jump_flip") _clipFlip = true;

            if (_animator.avatar != null && _animator.avatar.isHuman)
            {
                _hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
                _spine = _animator.GetBoneTransform(HumanBodyBones.Spine);
                _chest = _animator.GetBoneTransform(HumanBodyBones.UpperChest);
                if (_chest == null) _chest = _animator.GetBoneTransform(HumanBodyBones.Chest);
                _neck = _animator.GetBoneTransform(HumanBodyBones.Neck);
                _head = _animator.GetBoneTransform(HumanBodyBones.Head);
                _upperArmL = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                _lowerArmL = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                _handL = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
                _upperArmR = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                _lowerArmR = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                _handR = _animator.GetBoneTransform(HumanBodyBones.RightHand);
                _upperLegL = _animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                _lowerLegL = _animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                _footL = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                _upperLegR = _animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                _lowerLegR = _animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                _footR = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if (_hips != null) _comHeight = Mathf.Clamp(Vector3.Dot(_hips.position - transform.position, transform.up), 0.6f, 1.3f);
                if (_head != null) _headForward = Quaternion.Inverse(_head.rotation) * transform.forward;
                PlayerOutfit.Dress(transform, _animator);
            }
            return true;
        }

        // Dev stand-in until the Mixamo runner is imported (never shipped in a build).
        private void BuildPlaceholder()
        {
            _model = Pivot("Runner", _body, Vector3.zero);
            Mats.Primitive(PrimitiveType.Capsule, _model, new Vector3(0f, 0.9f, 0f), new Vector3(0.8f, 0.9f, 0.8f), Mats.Toon(new Color(0.75f, 0.78f, 0.85f)), "PlaceholderBody");
            Mats.Primitive(PrimitiveType.Sphere, _model, new Vector3(0f, 1.45f, 0.3f), new Vector3(0.5f, 0.14f, 0.2f), Mats.Glow(new Color(0.5f, 2.5f, 3f)), "Visor");
        }

        // Mixamo materials -> cel-shaded materials keeping their textures.
        private void Toonify(Renderer[] pRenderers)
        {
            foreach (Renderer lRenderer in pRenderers)
            {
                Material[] lMaterials = lRenderer.sharedMaterials;
                for (int i = 0; i < lMaterials.Length; i++)
                {
                    Material lSource = lMaterials[i];
                    Material lToon = new Material(Mats.Toon(Color.white, new Color(0.62f, 0.58f, 0.82f), 0.012f, 0.55f, true));
                    if (lSource != null)
                    {
                        Texture lTexture = lSource.HasProperty("_BaseMap") ? lSource.GetTexture("_BaseMap") : lSource.mainTexture;
                        if (lTexture != null) lToon.SetTexture("_BaseMap", lTexture);
                        Color lColor = lSource.HasProperty("_BaseColor") ? lSource.GetColor("_BaseColor") : lSource.HasProperty("_Color") ? lSource.color : Color.white;
                        lToon.SetColor(Mats.BASE_COLOR, lColor);
                        lToon.name = lSource.name + " (Toon)";
                    }
                    lMaterials[i] = lToon;
                }
                lRenderer.sharedMaterials = lMaterials;
                lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        // ---------------------------------------------------------------- effects setup

        // Aura : the same skinned meshes again, pushed out along their normals and dragged backward by the shader.
        private void BuildAura()
        {
            Shader lShader = Shader.Find("Hyperspeed/Aura");
            if (lShader == null) return;
            _auraMaterial = new Material(lShader);
            System.Collections.Generic.List<Renderer> lAura = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer lRenderer in _bodyRenderers)
            {
                SkinnedMeshRenderer lSkin = lRenderer as SkinnedMeshRenderer;
                MeshFilter lFilter = lRenderer.GetComponent<MeshFilter>();
                Mesh lMesh = lSkin != null ? lSkin.sharedMesh : lFilter != null ? lFilter.sharedMesh : null;
                if (lMesh == null) continue;
                GameObject lCopy = new GameObject(lRenderer.name + "_Aura");
                lCopy.transform.SetParent(lRenderer.transform, false);
                Renderer lCopyRenderer;
                if (lSkin != null)
                {
                    SkinnedMeshRenderer lCopySkin = lCopy.AddComponent<SkinnedMeshRenderer>();
                    lCopySkin.sharedMesh = lSkin.sharedMesh;
                    lCopySkin.bones = lSkin.bones;
                    lCopySkin.rootBone = lSkin.rootBone;
                    lCopySkin.localBounds = lSkin.localBounds;
                    lCopySkin.updateWhenOffscreen = true;
                    lCopyRenderer = lCopySkin;
                }
                else
                {
                    lCopy.AddComponent<MeshFilter>().sharedMesh = lMesh;
                    lCopyRenderer = lCopy.AddComponent<MeshRenderer>();
                }
                Material[] lMaterials = new Material[Mathf.Max(1, lMesh.subMeshCount)];
                for (int i = 0; i < lMaterials.Length; i++) lMaterials[i] = _auraMaterial;
                lCopyRenderer.sharedMaterials = lMaterials;
                lCopyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lCopyRenderer.enabled = false;
                lAura.Add(lCopyRenderer);
            }
            _auraRenderers = lAura.ToArray();
        }

        private void BuildGhosts()
        {
            _ghosts = new Ghost[AFTERIMAGE_POOL];
            Material lMaterial = Mats.Glow(_ghostColor, false, 0f, 0.8f);
            for (int i = 0; i < _ghosts.Length; i++)
            {
                GameObject lObject = new GameObject("Afterimage");
                lObject.SetActive(false);
                Ghost lGhost = new Ghost { transform = lObject.transform, mesh = new Mesh() };
                lGhost.mesh.MarkDynamic();
                lObject.AddComponent<MeshFilter>().sharedMesh = lGhost.mesh;
                lGhost.renderer = lObject.AddComponent<MeshRenderer>();
                lGhost.renderer.sharedMaterial = lMaterial;
                lGhost.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _ghosts[i] = lGhost;
            }
        }

        private void BuildTrails()
        {
            // Two long streaks born from the hands : they follow every swing of the arms. Without a rig, from the sides.
            _wingL = Pivot("WingL", _body, new Vector3(-1.6f, 1.2f, -0.2f));
            _wingR = Pivot("WingR", _body, new Vector3(1.6f, 1.2f, -0.2f));
            _trailL = CreateTrail(_handL != null ? _handL : _wingL, new Color(0.4f, 1.6f, 3f, 0.95f), 0.75f, 0.42f);
            _trailR = CreateTrail(_handR != null ? _handR : _wingR, new Color(2.8f, 0.5f, 2.2f, 0.95f), 0.75f, 0.42f);
            // Thin at the hand, swelling just behind it, then thinning out : the streak grows out of the fist.
            AnimationCurve lShape = new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(0.12f, 1f), new Keyframe(0.5f, 0.6f), new Keyframe(1f, 0f));
            _trailL.widthCurve = lShape;
            _trailR.widthCurve = lShape;
            _trailL.minVertexDistance = _trailR.minVertexDistance = 0.08f;

            _helixAnchorA = Pivot("HelixA", _body, Vector3.zero);
            _helixAnchorB = Pivot("HelixB", _body, Vector3.zero);
            _helixA = CreateTrail(_helixAnchorA, new Color(2.8f, 2.8f, 3f, 1f), 0.5f, 0.4f);
            _helixB = CreateTrail(_helixAnchorB, new Color(3f, 0.8f, 0.5f, 1f), 0.5f, 0.4f);

            // Marks the drift leaves on the ground, wide enough to read from far back.
            Color lMark = RaceContext.Theme != null ? Color.Lerp(RaceContext.Theme.roadSide, Color.black, 0.4f) : new Color(0.1f, 0.1f, 0.12f);
            lMark.a = 0.8f;
            Transform lSkidL = Pivot("SkidL", transform, new Vector3(-0.4f, 0.14f, 0f));
            Transform lSkidR = Pivot("SkidR", transform, new Vector3(0.4f, 0.14f, 0f));
            _skidL = CreateTrail(lSkidL, lMark, 2.2f, 0.7f, false);
            _skidR = CreateTrail(lSkidR, lMark, 2.2f, 0.7f, false);
        }

        private void UpdateWings(float pSpeed)
        {
            float lLevel = _player.Speed01;
            _windPhase += Time.deltaTime * Mathf.Lerp(3f, 7f, lLevel);
            float lSpread = 1.5f + lLevel * 2.2f;
            float lSwing = _player.Steer * 1.4f + _player.LateralSpeed * 0.02f;
            _wingL.localPosition = new Vector3(-lSpread + lSwing + Mathf.Sin(_windPhase) * 0.8f, 1.1f + Mathf.Sin(_windPhase * 1.3f) * 0.5f, -0.3f);
            _wingR.localPosition = new Vector3(lSpread + lSwing + Mathf.Sin(_windPhase + 2.2f) * 0.8f, 1.1f + Mathf.Sin(_windPhase * 1.3f + 1.4f) * 0.5f, -0.3f);
            // Alive : length breathes with speed and the arm swing, width grows in from nothing when they start.
            _trailGrow = _trailL.emitting ? Mathf.MoveTowards(_trailGrow, 1f, Time.deltaTime / 0.25f) : 0f;
            float lBreath = 0.85f + Mathf.Sin(_windPhase * 2.3f) * 0.15f;
            _trailL.time = Mathf.Lerp(0.3f, 0.9f, lLevel) * lBreath * (0.6f + 0.4f * _trailGrow);
            _trailR.time = Mathf.Lerp(0.3f, 0.9f, lLevel) * (1.7f - lBreath) * (0.6f + 0.4f * _trailGrow);
            float lWidth = Mathf.Lerp(0.22f, 0.5f, lLevel) + _player.BoostAmount * 0.25f;
            _trailL.widthMultiplier = lWidth * _trailGrow * (0.9f + Mathf.Sin(_windPhase * 3.1f) * 0.1f);
            _trailR.widthMultiplier = lWidth * _trailGrow * (0.9f + Mathf.Sin(_windPhase * 2.7f + 1f) * 0.1f);
        }

        private float _trailGrow;

        private void UpdateHelix()
        {
            bool lDash = _swirlT < 1f;
            bool lOn = (lDash || _turnSwirl > 0.15f) && !_lightOn && _player.State != PlayerState.Dead;
            _helixA.emitting = lOn;
            _helixB.emitting = lOn;
            if (!lOn) return;
            float lAngle = lDash ? _swirlT * Mathf.PI * 5f : Time.time * 14f * Mathf.Sign(_yawRate);
            float lRadius = lDash ? Mathf.Lerp(0.6f, 2f, Mathf.Sin(_swirlT * Mathf.PI)) : Mathf.Lerp(0.5f, 1.5f, _turnSwirl);
            _helixAnchorA.localPosition = new Vector3(Mathf.Cos(lAngle) * lRadius, 1f + Mathf.Sin(lAngle) * lRadius, 0f);
            _helixAnchorB.localPosition = new Vector3(-Mathf.Cos(lAngle) * lRadius, 1f - Mathf.Sin(lAngle) * lRadius, 0f);
        }

        private TrailRenderer CreateTrail(Transform pParent, Color pColor, float pTime, float pWidth, bool pAdditive = true)
        {
            Transform lAnchor = Pivot("Trail", pParent, Vector3.zero);
            TrailRenderer lTrail = lAnchor.gameObject.AddComponent<TrailRenderer>();
            lTrail.time = pTime;
            lTrail.startWidth = pWidth;
            lTrail.endWidth = 0f;
            lTrail.minVertexDistance = 0.2f;
            lTrail.sharedMaterial = Mats.Glow(pColor, pAdditive);
            lTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lTrail.numCapVertices = 2;
            lTrail.emitting = false;
            return lTrail;
        }

        // Two ribbons from the back of the neck (red and white) : follow-through on every move.
        private void BuildRibbons()
        {
            Color[] lColors = { UIFactory.RED, new Color(0.95f, 0.95f, 1f) };
            _ribbons = new Ribbon[2];
            for (int i = 0; i < 2; i++)
            {
                Ribbon lRibbon = new Ribbon { offset = new Vector3(i == 0 ? -0.12f : 0.12f, 0.02f, -0.2f), segment = i == 0 ? 0.3f : 0.24f };
                GameObject lObject = new GameObject("Ribbon");
                lObject.transform.SetParent(transform, false);
                lRibbon.line = lObject.AddComponent<LineRenderer>();
                lRibbon.line.positionCount = RIBBON_POINTS;
                lRibbon.line.useWorldSpace = true;
                lRibbon.line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.34f), new Keyframe(0.75f, 0.22f), new Keyframe(1f, 0.02f));
                lRibbon.line.sharedMaterial = Mats.Glow(lColors[i], false, 0f, 0f, true);
                lRibbon.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lRibbon.line.numCornerVertices = 2;
                _ribbons[i] = lRibbon;
            }
        }

        // ---------------------------------------------------------------- events

        private void Trigger(int pId)
        {
            if (_animated) _animator.SetTrigger(pId);
        }

        private void OnLanded(float pImpact)
        {
            // A jump asked in the air (coyote) must not replay once on the ground.
            if (_animated) _animator.ResetTrigger(P_JUMP);
            // A trick not finished in time snaps to its end : never a half flip on the ground.
            _trickT = 1f;
            _trickedThisAir = false;
            _stretchVelocity -= Mathf.Clamp(pImpact * 0.07f, 0.6f, 2.8f);
            float lSpeed = _player.Speed;
            if (_player.LastLandingWasDive && pImpact > 30f) Trigger(lSpeed < 25f ? P_LAND_HARD : P_LAND_ROLL);
            else if (pImpact > 90f && lSpeed < 25f) Trigger(P_LAND_HARD);
            else if (pImpact > 8f && lSpeed < 25f) Trigger(P_LAND);
            EmitLandingDust(pImpact);
        }

        private void OnDashed()
        {
            // A corkscrew along the way you are going, on the ground as in the air, with two ribbons coiling around it.
            _swirlT = 0f;
            _swirlAir = true;
            _helixA.Clear();
            _helixB.Clear();
            _swirlDir = _player.Steer < -0.1f ? -1f : 1f;
            _zStretchVelocity += 2.6f;
            _flash = 0.4f;
            for (int i = 0; i < 3; i++) SpawnGhost(i * 0.4f);
        }

        private void Snap()
        {
            if (_player == null) return;
            transform.position = _player.transform.position;
            _facing = _player.transform.rotation;
            transform.rotation = _facing;
            _lastHeading = _facing * Vector3.forward;
            _turnLead = 0f;
            _yawRate = 0f;
            _ribbonsReset = true;
            _trailL?.Clear();
            _trailR?.Clear();
            _trickT = 1f;
            _trickedThisAir = false;
            if (_ball != null) _ball.Clear();
            if (_lightWake != null) _lightWake.Clear();
        }

        public void SetVictory() => SetResult(true);

        // Juice : kicks the body springs (up stretches tall, negative squashes ; forward stretches along the run).
        public void Punch(float pUp, float pForward, float pFlash)
        {
            _stretchVelocity += pUp;
            _zStretchVelocity += pForward;
            _flash = Mathf.Max(_flash, pFlash);
        }

        // A win : the runner keeps running past the line first, and only celebrates once it has stopped.
        public void SetResult(bool pWin)
        {
            if (!_animated) return;
            if (pWin) _pendingWin = true;
            else _animator.SetBool(P_LOSE, true);
        }

        public void Taunt() => Trigger(P_TAUNT);

        // One punch connected : the fist flashes, the whole body lunges at the impact.
        public void PunchImpact(Vector3 pPoint)
        {
            _punchT = 0.25f;
            _flash = 1f;
            _zStretchVelocity += 5f;
            FX.Burst(transform.position + transform.up, (pPoint - transform.position).normalized, new Color(1f, 0.7f, 0.2f), 40f);
        }

        private void OnDestroy()
        {
            if (_shadow != null) Destroy(_shadow.gameObject);
            if (_ghosts == null) return;
            foreach (Ghost lGhost in _ghosts)
            {
                if (lGhost.transform != null) Destroy(lGhost.transform.gameObject);
                Destroy(lGhost.mesh);
            }
        }

        // ---------------------------------------------------------------- per frame (from the controller's Update)

        public void Tick(float pDt)
        {
            if (_player == null || pDt <= 0f) return;
            PlayerState lState = _player.State;
            bool lGrounded = lState == PlayerState.Ground || lState == PlayerState.Finished || lState == PlayerState.Frozen;
            bool lOnSurface = lGrounded || lState == PlayerState.Grind;
            Vector3 lUp = lOnSurface ? _player.Up : _player.GravityUp;
            Vector3 lHeading = Vector3.ProjectOnPlane(_player.FacingDirection, lUp);
            if (lHeading.sqrMagnitude < 1e-4f) lHeading = _facing * Vector3.forward;
            lHeading.Normalize();
            float lSpeed = _player.Speed;

            // Root : catches up with the heading in ~0.15 s (faster on big turns) ; the upper body leads (see LateUpdate).
            Quaternion lTarget = Quaternion.LookRotation(lHeading, lUp);
            float lError = Quaternion.Angle(_facing, lTarget);
            _facing = Quaternion.Slerp(_facing, lTarget, 1f - Mathf.Exp(-(1f / 0.15f) * (1f + lError / 45f) * pDt));
            transform.SetPositionAndRotation(_player.transform.position, _facing);
            _turnLead = Mathf.Lerp(_turnLead, Vector3.SignedAngle(_facing * Vector3.forward, lHeading, lUp), 1f - Mathf.Exp(-20f * pDt));

            float lYaw = Vector3.SignedAngle(_lastHeading, lHeading, lUp) / pDt;
            _lastHeading = lHeading;
            _yawRate = Mathf.Lerp(_yawRate, Mathf.Clamp(lYaw, -400f, 400f), 1f - Mathf.Exp(-10f * pDt));

            _swirlT = Mathf.MoveTowards(_swirlT, 1f, pDt / SWIRL_TIME);
            // Hard turns coil the streaks behind you too, not only dashes.
            float lTurn = Mathf.InverseLerp(70f, 220f, Mathf.Abs(_yawRate));
            _turnSwirl = Mathf.MoveTowards(_turnSwirl, lTurn, pDt * (lTurn > _turnSwirl ? 6f : 2f));
            UpdateWings(lSpeed);
            UpdateHelix();
            UpdateCone(pDt);
            _flipT = Mathf.MoveTowards(_flipT, 1f, pDt / FLIP_TIME);
            _tumbleT = Mathf.MoveTowards(_tumbleT, 1f, pDt / TUMBLE_TIME);
            UpdateTrick(lState, pDt);
            UpdateLean(pDt);
            if (_pendingWin && _animated && _player.Speed < 4f)
            {
                _pendingWin = false;
                _animator.SetBool(P_WIN, true);
            }
            _punchT = Mathf.MoveTowards(_punchT, 1f, pDt / PUNCH_TIME);
            _flash = Mathf.MoveTowards(_flash, 0f, pDt * 4f);

            // Bank (right positive) and pitch (nose down positive), around the feet on the ground, the centre in the air.
            float lBank = 0f;
            float lPitch = 0f;
            float lVertical = Vector3.Dot(_player.Velocity, _player.GravityUp);
            _teeter = 0f;
            switch (lState)
            {
                case PlayerState.Ground:
                case PlayerState.Finished:
                    float lCentripetal = lSpeed * _yawRate * Mathf.Deg2Rad;
                    lBank = Mathf.Clamp(Mathf.Atan2(lCentripetal, 9.81f) * Mathf.Rad2Deg * 0.5f, -26f, 26f) + _player.Steer * 4f;
                    
                    if (_player.Drifting) lBank += _player.DriftDirection * 10f;
                    // Sliding on a leaning wall : the body lies back against it.
                    if (_player.WallRiding)
                    {
                        lBank -= _player.WallSide * 26f;
                        lPitch = -10f;
                    }
                    // Looking down a slope : thrown forward over the feet, belly toward the ground.
                    if (_player.BellySliding) lPitch = 50f;
                    break;
                case PlayerState.Air:
                    lBank = _player.Steer * 10f;
                    if (_player.Diving)
                    {
                        // Head first like a missile : at least as steep as you look, steeper as the fall bends down.
                        float lHorizontal = Vector3.ProjectOnPlane(_player.Velocity, _player.GravityUp).magnitude;
                        float lPath = Mathf.Atan2(-lVertical, Mathf.Max(1f, lHorizontal)) * Mathf.Rad2Deg;
                        lPitch = Mathf.Clamp(Mathf.Max(lPath, 40f + _player.Dive01 * 45f), 0f, 88f);
                        lBank = _player.Steer * 18f + Mathf.Sin(Time.time * 31f) * 2f * _player.Dive01;
                    }
                    else lPitch = lVertical > 0f ? -5f : 7f;
                    break;
                case PlayerState.Grind:
                    // Surfing the current : crouched, swaying, shaken by the voltage.
                    lBank = Mathf.Sin(Time.time * 7f) * 9f + Mathf.Sin(Time.time * 53f) * 2f;
                    lPitch = 14f + Mathf.Sin(Time.time * 41f) * 2f;
                    break;
                case PlayerState.Zip:
                    lBank = Mathf.Sin(Time.time * 3f) * 8f;
                    lPitch = -8f;
                    break;
            }
            // Thrown forward : the faster, the more the whole body leans over its feet.
            if (lGrounded) lPitch += _player.Speed01 * 20f + _player.BoostAmount * 12f + (_player.Dashing ? 16f : 0f);
            // Footfalls : a light squash on each step, never goofy.
            if (lGrounded && lState != PlayerState.Frozen && lSpeed > 5f && !_player.BellySliding)
                _stretchVelocity += Mathf.Sin(_swayPhase * 2f) * 3.2f * pDt * 10f * Mathf.Lerp(1f, 0.4f, _player.Speed01);
            if (_swirlT < 1f) lPitch = Mathf.Max(lPitch, Mathf.Sin(_swirlT * Mathf.PI) * 52f);
            _bank = Mathf.Lerp(_bank, lBank, 1f - Mathf.Exp(-9f * pDt));
            _pitch = Mathf.Lerp(_pitch, lPitch, 1f - Mathf.Exp(-9f * pDt));

            float lSwirl = _swirlT < 1f ? EaseInOut(_swirlT) * 360f * _swirlDir : 0f;
            float lFlip = _flipT < 1f ? EaseInOut(_flipT) * 360f : 0f;
            float lTumble = _tumbleT < 1f ? (1f - (1f - _tumbleT) * (1f - _tumbleT)) * 360f : 0f;
            bool lAround = !lGrounded || (_swirlT < 1f && _swirlAir) || _flipT < 1f || _tumbleT < 1f || _trickT < 1f;
            _pivotH = Mathf.MoveTowards(_pivotH, lAround ? _comHeight : 0f, pDt * 8f);

            // The trick in progress : whole turns, eased in and out, around the centre of mass.
            float lTrickPitch = 0f, lTrickYaw = 0f, lTrickRoll = 0f;
            if (_trickT < 1f)
            {
                float lTurns = EaseInOut(_trickT) * 360f * _trickTurns;
                switch (_trick)
                {
                    case Trick.FrontFlip: lTrickPitch = lTurns; break;
                    case Trick.BackFlip: lTrickPitch = -lTurns; break;
                    case Trick.Cork: lTrickPitch = lTurns; lTrickYaw = EaseInOut(_trickT) * 360f; break;
                    case Trick.Spin: lTrickYaw = lTurns; break;
                    case Trick.SideFlip: lTrickRoll = lTurns * (_trickIndex % 2 == 0 ? 1f : -1f); break;
                }
            }

            float lYawOffset = (_swirlAir ? 0f : lSwirl) + lTrickYaw;
            float lRoll = -_bank - (_swirlAir ? lSwirl : 0f) + lTumble * 0.35f + lTrickRoll;
            float lPitchTotal = _pitch + lFlip + lTumble + lTrickPitch;
            Quaternion lRotation = Quaternion.AngleAxis(lYawOffset, Vector3.up) * Quaternion.AngleAxis(lRoll, Vector3.forward) * Quaternion.AngleAxis(lPitchTotal, Vector3.right);

            // Squash & stretch around the same pivot, volume preserved.
            _stretchVelocity += ((1f - _stretch) * 320f - _stretchVelocity * 16f) * pDt;
            _stretch = Mathf.Clamp(_stretch + _stretchVelocity * pDt, 0.8f, 1.2f);
            _zStretchVelocity += ((1f - _zStretch) * 260f - _zStretchVelocity * 15f) * pDt;
            _zStretch = Mathf.Clamp(_zStretch + _zStretchVelocity * pDt, 0.86f, 1.24f);
            Vector3 lScale = new Vector3(1f / Mathf.Sqrt(_stretch * _zStretch), _stretch / Mathf.Sqrt(_zStretch), _zStretch / Mathf.Sqrt(_stretch));
            Vector3 lPivot = new Vector3(0f, _pivotH, 0f);
            _body.localRotation = lRotation;
            _body.localScale = lScale;
            _body.localPosition = lPivot - lRotation * Vector3.Scale(lScale, lPivot);

            // Posture targets for the rig.
            float lArrow = lState == PlayerState.Ground || lState == PlayerState.Finished ? Mathf.Clamp01((lSpeed - _player.Profile.cruise * 0.9f) / (_player.Profile.cruise * 0.4f)) : 0f;
            if (_player.Hyper && lGrounded) lArrow = 1f;
            // Diving : arms swept back along the body, a dart.
            if (_player.Diving || _player.BellySliding) lArrow = 1f;
            _arrow = Mathf.Lerp(_arrow, lArrow, 1f - Mathf.Exp(-5f * pDt));
            _swayPhase += pDt * Mathf.Lerp(5f, 9f, _player.Speed01);
            float lWideArms = lState == PlayerState.Grind ? 0.85f : _player.WallRiding ? 0.6f : 0f;
            _openArms = _swirlT < 1f ? Mathf.Sin(_swirlT * Mathf.PI) : Mathf.MoveTowards(_openArms, lWideArms, pDt * 6f);
            _lookTarget = _player.HasLandingPrediction && lState == PlayerState.Air ? _player.LandingPoint : transform.position + lHeading * 30f + lUp * 1.2f;

            UpdateAnimator(lState, lGrounded, lSpeed, lVertical, pDt);
            UpdateAura(pDt, lSpeed);
            UpdateShadow();
            UpdateSteps(lState, lGrounded, lSpeed, pDt);

            bool lBlink = _player.Invulnerable && Mathf.Repeat(Time.time * 12f, 1f) > 0.5f;
            _block.SetFloat(FLASH, _flash > 0.3f || lBlink ? 0.85f : 0f);
            foreach (Renderer lRenderer in _bodyRenderers) if (lRenderer != null) lRenderer.SetPropertyBlock(_block);

            bool lTrails = lState != PlayerState.Dead && !_lightOn && (lSpeed > _player.Profile.cruise * 0.9f || _player.Dashing);
            _trailL.emitting = lTrails;
            _trailR.emitting = lTrails;
            bool lSkid = _player.Drifting && lState == PlayerState.Ground;
            _skidL.emitting = lSkid;
            _skidR.emitting = lSkid;
            if (lState == PlayerState.Grind) EmitFootSparks(UIFactory.YELLOW, 2);
            if (_player.Drifting) EmitDriftSparks();
        }

        private void UpdateAnimator(PlayerState pState, bool pGrounded, float pSpeed, float pVertical, float pDt)
        {
            if (!_animated) return;
            // Jog, run, sprint across the real speed range : the stride follows how fast you go.
            float lSpeedParam = pState == PlayerState.Frozen ? 0f : Mathf.Clamp01(pSpeed / Mathf.Max(20f, _player.Profile.cruise * 0.85f));
            _animator.SetFloat(P_SPEED, lSpeedParam, 0.08f, pDt);
            _animator.SetFloat(P_VERTICAL, pVertical);
            _animator.SetBool(P_GROUNDED, pGrounded);
            _animator.SetBool(P_GLIDE, false);
            _animator.SetBool(P_DIVE, _player.Diving);
            _animator.SetBool(P_SLIDE, pGrounded && (_player.Sliding || _player.Drifting || _player.WallRiding));
            _animator.SetBool(P_GRIND, pState == PlayerState.Grind);
            _animator.SetBool(P_HANG, pState == PlayerState.Zip);
            _animator.SetBool(P_WALLRUN, false);
            _animator.SetBool(P_WALLCLIMB, false);
            _animator.SetBool(P_WALLSLIDE, false);
            _animator.SetBool(P_PRERACE, pState == PlayerState.Frozen || _player.FreeRoam);
            // Stylized cadence : the legs keep up with speeds no human reaches.
            _animator.speed = pGrounded && pState != PlayerState.Frozen ? Mathf.Lerp(0.9f, 2.3f, Mathf.Clamp01(pSpeed / Mathf.Max(1f, _player.Profile.cruise * 2.2f))) : 1f;
        }

        // ---------------------------------------------------------------- procedural layer (after the Animator)

        private void LateUpdate()
        {
            if (_player == null) return;
            float lDt = Time.deltaTime;
            UpdateLightForm();
            if (_hips != null && !_lightOn) ApplyRig();
            if (_ribbons != null)
                foreach (Ribbon lRibbon in _ribbons) lRibbon.line.enabled = !_lightOn;
            UpdateRibbons(lDt);
            UpdateGhosts(lDt);
        }

        private void ApplyRig()
        {
            Vector3 lUp = transform.up;
            Vector3 lRight = transform.right;
            Vector3 lForward = transform.forward;

            // Turn distributed from the hips to the head (40 / 70 / 100 %) : the body turns like an animation, never as a block.
            float lLead = Mathf.Clamp(_turnLead, -60f, 60f);
            Rotate(_hips, lUp, lLead * 0.4f);
            Rotate(_chest != null ? _chest : _spine, lUp, lLead * 0.3f);
            Rotate(_head, lUp, lLead * 0.3f);

            // Arrow posture : the spine folds forward, the neck brings the eyes back to the horizon.
            float lBend = _arrow * 38f;
            Rotate(_spine, lRight, lBend * 0.5f);
            Rotate(_chest, lRight, lBend * 0.5f);
            Rotate(_neck, lRight, -lBend * 0.65f);

            // Arms : swept back at speed with the hands swaying left / right, open during swirls, windmilling at an edge.
            float lSway = Mathf.Sin(_swayPhase) * 0.62f * _arrow;
            ArmTo(_upperArmL, _lowerArmL, _handL, Dir(lRight, lUp, lForward, -0.32f + lSway, -0.5f, -0.8f), _arrow * 0.9f);
            ArmTo(_upperArmR, _lowerArmR, _handR, Dir(lRight, lUp, lForward, 0.32f + lSway, -0.5f, -0.8f), _arrow * 0.9f);
            if (_openArms > 0.01f)
            {
                ArmTo(_upperArmL, _lowerArmL, _handL, Dir(lRight, lUp, lForward, -1f, 0.2f, 0.1f), _openArms);
                ArmTo(_upperArmR, _lowerArmR, _handR, Dir(lRight, lUp, lForward, 1f, 0.2f, 0.1f), _openArms);
            }
            if (_teeter > 0.02f)
            {
                float lAngle = Time.time * 14f;
                ArmTo(_upperArmL, _lowerArmL, _handL, Dir(lRight, lUp, lForward, -0.7f, Mathf.Sin(lAngle), Mathf.Cos(lAngle)), _teeter);
                ArmTo(_upperArmR, _lowerArmR, _handR, Dir(lRight, lUp, lForward, 0.7f, Mathf.Sin(lAngle + Mathf.PI), Mathf.Cos(lAngle + Mathf.PI)), _teeter);
            }
            if (_punchT < 1f)
            {
                float lPunch = Mathf.Sin(_punchT * Mathf.PI);
                ArmTo(_upperArmR, _lowerArmR, _handR, Dir(lRight, lUp, lForward, 0.1f, 0.05f, 1f), lPunch);
                Rotate(_chest, lUp, -25f * lPunch);
            }

            // Springs : the chest and the head lag behind the accelerations (lean back on a kick, sway in the turns).
            Rotate(_chest != null ? _chest : _spine, lRight, _lean.x);
            Rotate(_chest != null ? _chest : _spine, lForward, _lean.y);
            Rotate(_head, lRight, _lean.x * 0.6f);
            Rotate(_head, lForward, _lean.y * 0.5f);

            // Tucked in a flip : knees to the chest, hands on the shins.
            if (_tuck > 0.01f)
            {
                Vector3 lBR = _body.right, lBU = _body.up, lBF = _body.forward;
                Vector3 lThigh = Dir(lBR, lBU, lBF, 0f, 0.35f, 1f);
                Vector3 lShin = Dir(lBR, lBU, lBF, 0f, -1f, -0.15f);
                Aim(_upperLegL, _lowerLegL, lThigh + lBR * -0.15f, _tuck);
                Aim(_lowerLegL, _footL, lShin, _tuck);
                Aim(_upperLegR, _lowerLegR, lThigh + lBR * 0.15f, _tuck);
                Aim(_lowerLegR, _footR, lShin, _tuck);
                ArmTo(_upperArmL, _lowerArmL, _handL, Dir(lBR, lBU, lBF, -0.25f, -0.35f, 1f), _tuck);
                ArmTo(_upperArmR, _lowerArmR, _handR, Dir(lBR, lBU, lBF, 0.25f, -0.35f, 1f), _tuck);
                Rotate(_spine, lBR, 25f * _tuck);
                Rotate(_neck, lBR, 20f * _tuck);
            }
            // Just before touching down : legs reach for the ground, arms open for balance.
            else if (_player.State == PlayerState.Air && _player.HasLandingPrediction && _player.LandingTime < 0.25f && !_player.Diving)
            {
                float lReach = 1f - _player.LandingTime / 0.25f;
                ArmTo(_upperArmL, _lowerArmL, _handL, Dir(lRight, lUp, lForward, -1f, 0.35f, 0.2f), lReach * 0.7f);
                ArmTo(_upperArmR, _lowerArmR, _handR, Dir(lRight, lUp, lForward, 1f, 0.35f, 0.2f), lReach * 0.7f);
            }

            // Head reads the trajectory : the landing point in the air, the route ahead on the ground.
            if (_head != null)
            {
                Vector3 lCurrent = _head.rotation * _headForward;
                Vector3 lWanted = _lookTarget - _head.position;
                if (lWanted.sqrMagnitude > 0.01f)
                {
                    float lAngle = Vector3.Angle(lCurrent, lWanted);
                    Quaternion lDelta = Quaternion.FromToRotation(lCurrent, Vector3.RotateTowards(lCurrent, lWanted, Mathf.Min(lAngle, 50f) * Mathf.Deg2Rad, 0f));
                    _head.rotation = Quaternion.Slerp(Quaternion.identity, lDelta, 0.55f) * _head.rotation;
                }
            }
        }

        private static Vector3 Dir(Vector3 pRight, Vector3 pUp, Vector3 pForward, float pX, float pY, float pZ) => (pRight * pX + pUp * pY + pForward * pZ).normalized;

        private static void Rotate(Transform pBone, Vector3 pAxis, float pDegrees)
        {
            if (pBone == null || Mathf.Abs(pDegrees) < 0.01f) return;
            pBone.rotation = Quaternion.AngleAxis(pDegrees, pAxis) * pBone.rotation;
        }

        // Points the upper arm then the forearm along pDirection (straight arm), blended by pWeight.
        private static void ArmTo(Transform pUpper, Transform pLower, Transform pHand, Vector3 pDirection, float pWeight)
        {
            if (pWeight <= 0.001f) return;
            Aim(pUpper, pLower, pDirection, pWeight);
            Aim(pLower, pHand, pDirection, pWeight);
        }

        private static void Aim(Transform pBone, Transform pChild, Vector3 pDirection, float pWeight)
        {
            if (pBone == null || pChild == null) return;
            Vector3 lCurrent = pChild.position - pBone.position;
            if (lCurrent.sqrMagnitude < 1e-6f) return;
            Quaternion lDelta = Quaternion.FromToRotation(lCurrent, pDirection);
            pBone.rotation = Quaternion.Slerp(Quaternion.identity, lDelta, Mathf.Clamp01(pWeight)) * pBone.rotation;
        }

        private static float EaseInOut(float pT) => pT < 0.5f ? 4f * pT * pT * pT : 1f - Mathf.Pow(-2f * pT + 2f, 3f) * 0.5f;

        // ---------------------------------------------------------------- ribbons

        private void UpdateRibbons(float pDt)
        {
            if (_ribbons == null || pDt <= 0f) return;
            Vector3 lAnchorPosition = _neck != null ? _neck.position : transform.position + transform.up * 1.5f;
            Vector3 lShift = transform.position - _lastRootPosition;
            _lastRootPosition = transform.position;
            float lSpeed = _player.Speed;
            Vector3 lWind = -transform.forward * Mathf.Min(lSpeed, 160f) * 1.2f - transform.up * 7f;
            float lDt2 = pDt * pDt;
            foreach (Ribbon lRibbon in _ribbons)
            {
                Vector3 lRoot = lAnchorPosition + transform.rotation * lRibbon.offset;
                if (_ribbonsReset || lShift.sqrMagnitude > 900f)
                {
                    for (int i = 0; i < RIBBON_POINTS; i++)
                    {
                        lRibbon.points[i] = lRoot - transform.forward * lRibbon.segment * i;
                        lRibbon.previous[i] = lRibbon.points[i];
                    }
                }
                lRibbon.points[0] = lRoot;
                lRibbon.previous[0] = lRoot;
                for (int i = 1; i < RIBBON_POINTS; i++)
                {
                    Vector3 lPoint = lRibbon.points[i] + lShift;
                    Vector3 lPrevious = lRibbon.previous[i] + lShift;
                    Vector3 lFlutter = transform.right * Mathf.Sin(Time.time * 23f + i * 0.9f + lRibbon.offset.x * 40f) * Mathf.Min(lSpeed, 120f) * 0.25f;
                    lRibbon.previous[i] = lPoint;
                    lRibbon.points[i] = lPoint + (lPoint - lPrevious) * 0.92f + (lWind + lFlutter) * lDt2;
                }
                for (int lPass = 0; lPass < 3; lPass++)
                {
                    for (int i = 1; i < RIBBON_POINTS; i++)
                    {
                        Vector3 lDelta = lRibbon.points[i] - lRibbon.points[i - 1];
                        float lLength = lDelta.magnitude;
                        if (lLength > 1e-5f) lRibbon.points[i] = lRibbon.points[i - 1] + lDelta * (lRibbon.segment / lLength);
                    }
                }
                lRibbon.line.SetPositions(lRibbon.points);
            }
            _ribbonsReset = false;
        }

        // ---------------------------------------------------------------- light form

        private bool _lightOn;

        // Dash shield (2026-11-11 : "rounder toward the front") : a rounded shock wave wrapped around the front of the
        // runner, its nose just ahead of the chest, opening backward like the air bending around it. Only while dashing or
        // boosted : it snaps on and fades right after.
        private void BuildCone()
        {
            _cone = new GameObject("DashShield").transform;
            _cone.SetParent(transform, false);
            _coneMaterial = new Material(Mats.Glow(new Color(0.75f, 1.5f, 2.4f, 0.75f), true, 0f, 2.5f));
            _coneMaterial.SetFloat("_StripeFreq", 6f);
            _coneMaterial.SetFloat("_Scroll", -4f);
            MeshRenderer lRenderer = _cone.gameObject.AddComponent<MeshRenderer>();
            _cone.gameObject.AddComponent<MeshFilter>().sharedMesh = MeshUtil.Shield(12, 32);
            lRenderer.sharedMaterial = _coneMaterial;
            lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // A white skin just inside, tighter : the front of the shock.
            GameObject lInner = new GameObject("Edge");
            lInner.transform.SetParent(_cone, false);
            lInner.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            lInner.transform.localScale = new Vector3(0.82f, 0.82f, 0.8f);
            lInner.AddComponent<MeshFilter>().sharedMesh = MeshUtil.Shield(10, 28);
            _coneInnerMaterial = new Material(Mats.Glow(new Color(2.6f, 2.6f, 3f, 0.9f), true));
            _coneInnerMaterial.SetFloat("_StripeFreq", 11f);
            _coneInnerMaterial.SetFloat("_Scroll", -8f);
            MeshRenderer lInnerRenderer = lInner.AddComponent<MeshRenderer>();
            lInnerRenderer.sharedMaterial = _coneInnerMaterial;
            lInnerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _cone.gameObject.SetActive(false);
        }

        private float _wakeOpen;

        private void UpdateCone(float pDt)
        {
            if (_cone == null) return;
            bool lDash = (_player.Dashing || _player.BoostAmount > 0.45f) && _player.State != PlayerState.Dead && _player.State != PlayerState.Frozen && !_lightOn;
            // Opens in a blink, folds away in a quarter of a second.
            _coneAmount = Mathf.MoveTowards(_coneAmount, lDash ? 1f : 0f, pDt * (lDash ? 14f : 4f));
            _wakeOpen = lDash ? Mathf.MoveTowards(_wakeOpen, 1f, pDt * 10f) : _coneAmount > 0.02f ? _wakeOpen : 0f;
            bool lShow = _coneAmount > 0.02f;
            if (_cone.gameObject.activeSelf != lShow) _cone.gameObject.SetActive(lShow);
            if (!lShow) return;
            Vector3 lMotion = _player.OnGroundVelocity.sqrMagnitude > 1f ? _player.OnGroundVelocity.normalized : transform.forward;
            Vector3 lUp = Vector3.ProjectOnPlane(transform.up, lMotion).normalized;
            if (lUp.sqrMagnitude < 0.01f) lUp = transform.up;
            // Nose just ahead of the chest, wrapping the body and opening backward ; longer with speed.
            Vector3 lNose = transform.position + transform.up * (_comHeight * 1.05f) + lMotion * 1.6f;
            _cone.SetPositionAndRotation(lNose, Quaternion.LookRotation(lMotion, lUp));
            float lRadius = 2.6f * Mathf.Lerp(0.6f, 1f, _wakeOpen) * (1f + Mathf.Sin(Time.time * 30f) * 0.04f);
            float lLength = 7f * (0.8f + 0.7f * _player.Speed01) * Mathf.Lerp(0.5f, 1f, _wakeOpen);
            _cone.localScale = new Vector3(lRadius, lRadius, lLength);
            _coneMaterial.SetColor("_Color", new Color(0.75f, 1.5f, 2.4f, 0.75f * _coneAmount));
            _coneInnerMaterial.SetColor("_Color", new Color(2.6f, 2.6f, 3f, 0.9f * _coneAmount));
        }

        // On a rope, in hyper and at light speed the runner is a ball of light (bigger each time).
        private BallMode WantedBall()
        {
            PlayerState lState = _player.State;
            if (lState == PlayerState.Dead || lState == PlayerState.Frozen || lState == PlayerState.Finished) return BallMode.None;
            if (_player.LightForm) return BallMode.Light;
            if (_player.Hyper) return BallMode.Hyper;
            if (lState == PlayerState.Grind) return BallMode.Rope;
            return BallMode.None;
        }

        private void UpdateLightForm()
        {
            if (_ball == null)
            {
                _ball = PlayerLightBall.Create(transform);
                _lightWake = LightTrail.Create();
            }
            BallMode lMode = WantedBall();
            bool lWant = lMode != BallMode.None;
            if (lWant != _lightOn)
            {
                _lightOn = lWant;
                if (_model != null) _model.gameObject.SetActive(!lWant);
                FX.Burst(transform.position + transform.up, transform.forward, Color.white, 40f);
                FX.Shockwave(transform.position + transform.up, transform.forward, Color.white, 44f, 0.4f);
                _trailL.Clear();
                _trailR.Clear();
            }
            _ball.SetMode(lMode);
            if (!lWant) return;
            _ball.Tick(Time.deltaTime, _player.Speed01, _player.OnGroundVelocity);
            if (lMode == BallMode.Rope) return;
            _lightWake.SetWidth(lMode == BallMode.Light ? 4f : 2.4f);
            _lightWake.Add(transform.position, transform.right, transform.up);
        }


        // ---------------------------------------------------------------- tricks and springs

        private void UpdateTrick(PlayerState pState, float pDt)
        {
            if (_trickT < 1f)
            {
                _trickT = Mathf.MoveTowards(_trickT, 1f, pDt / Mathf.Max(0.3f, _trickDuration));
                if (pState != PlayerState.Air) _trickT = 1f;
            }
            // A real flight (not a dive, not a hop) : a trick sized to the time left before the landing.
            else if (pState == PlayerState.Air && !_trickedThisAir && !_player.Diving && !_lightOn && _player.AirTime > 0.1f
                     && _player.HasLandingPrediction && _player.LandingTime > 0.55f)
            {
                _trickedThisAir = true;
                _trickDuration = _player.LandingTime - 0.12f;
                _trickTurns = Mathf.Clamp(Mathf.RoundToInt(_trickDuration / 0.75f), 1, 3);
                _trickIndex++;
                Trick[] lCycle = { Trick.FrontFlip, Trick.Cork, Trick.BackFlip, Trick.Spin, Trick.SideFlip };
                _trick = lCycle[_trickIndex % lCycle.Length];
                if (_trick == Trick.Spin) _trickTurns = Mathf.Max(1, _trickTurns);
                _trickT = 0f;
                HSJuice.Play(JuiceId.Flip, new JuiceContext { position = transform.position, direction = transform.forward, normal = transform.up, intensity = _trickTurns / 3f, size = 1f });
            }
            // Tucked during flips (not spins), opening up near the end so the landing reads.
            float lTuck = _trickT < 1f && _trick != Trick.Spin ? Mathf.Clamp01(Mathf.Sin(_trickT * Mathf.PI) * 1.6f) : 0f;
            _tuck = Mathf.MoveTowards(_tuck, lTuck, pDt * 8f);
        }

        // The chest and the head hang back on springs when the body is pushed (kicks, landings, turns).
        private void UpdateLean(float pDt)
        {
            Vector3 lVelocity = _player.OnGroundVelocity;
            Vector3 lAccel = (lVelocity - _lastVelocity) / Mathf.Max(0.0001f, pDt);
            _lastVelocity = lVelocity;
            Vector3 lLocal = transform.InverseTransformDirection(lAccel);
            Vector2 lTarget = new Vector2(Mathf.Clamp(-lLocal.z * 0.05f, -16f, 16f), Mathf.Clamp(lLocal.x * 0.035f, -14f, 14f));
            if (_player.State == PlayerState.Dead || lAccel.sqrMagnitude > 1e8f) lTarget = Vector2.zero;
            _leanVelocity += ((lTarget - _lean) * 90f - _leanVelocity * 11f) * pDt;
            _lean += _leanVelocity * pDt;
            _lean = Vector2.ClampMagnitude(_lean, 22f);
        }

        // ---------------------------------------------------------------- aura, afterimages

        private void UpdateAura(float pDt, float pSpeed)
        {
            if (_auraMaterial == null) return;
            float lTarget = _player.Hyper ? 1f : Mathf.Max(_player.BoostAmount * 0.55f, _player.Dashing ? 0.7f : 0f);
            if (_player.State == PlayerState.Dead || _player.State == PlayerState.Frozen) lTarget = 0f;
            _aura = Mathf.MoveTowards(_aura, lTarget, pDt * (lTarget > _aura ? 4f : 1.5f));
            _auraDeploy = Mathf.MoveTowards(_auraDeploy, 1f, pDt / 0.35f);
            bool lShow = _aura > 0.01f;
            foreach (Renderer lRenderer in _auraRenderers) if (lRenderer.enabled != lShow) lRenderer.enabled = lShow;
            if (!lShow) return;
            // Deploys backward with a small overshoot, longer with speed.
            float lDeploy = 1f + 1.7f * Mathf.Pow(_auraDeploy - 1f, 3f) + 0.7f * Mathf.Pow(_auraDeploy - 1f, 2f);
            Vector3 lMotion = _player.Velocity.sqrMagnitude > 1f ? _player.Velocity.normalized : transform.forward;
            _auraMaterial.SetFloat(AURA_INTENSITY, _aura);
            _auraMaterial.SetFloat(AURA_DRAG, lDeploy * Mathf.Lerp(0.8f, 2.2f, Mathf.Clamp01((pSpeed - 60f) / 120f)));
            _auraMaterial.SetVector(AURA_MOTION, lMotion);
        }

        private void UpdateGhosts(float pDt)
        {
            bool lEmit = _player.State != PlayerState.Dead && _player.State != PlayerState.Frozen && (_player.Speed > _player.Profile.hyperExit || _player.Dashing);
            _ghostTimer -= pDt;
            if (lEmit && _ghostTimer <= 0f)
            {
                _ghostTimer = AFTERIMAGE_INTERVAL;
                SpawnGhost(0f);
            }
            foreach (Ghost lGhost in _ghosts)
            {
                if (lGhost.age >= 1f) continue;
                lGhost.age += pDt / AFTERIMAGE_LIFE;
                if (lGhost.age >= 1f)
                {
                    lGhost.transform.gameObject.SetActive(false);
                    continue;
                }
                // Shadow clones stay with the runner and slide back a little while they fade.
                Vector3 lLocal = lGhost.localPosition - Vector3.forward * (lGhost.age * 2.2f);
                lGhost.transform.SetPositionAndRotation(transform.TransformPoint(lLocal), transform.rotation * lGhost.localRotation);
                Color lColor = _ghostColor;
                lColor.a *= (1f - lGhost.age) * (1f - lGhost.age);
                _block.Clear();
                _block.SetColor(Mats.COLOR, lColor);
                lGhost.renderer.SetPropertyBlock(_block);
            }
            _block.Clear();
        }

        private void SpawnGhost(float pStartAge)
        {
            if (_skins.Length > 0)
            {
                foreach (SkinnedMeshRenderer lSkin in _skins) if (lSkin.enabled) Bake(lSkin.transform, null, lSkin, pStartAge);
            }
            else foreach (MeshFilter lFilter in _staticMeshes) if (lFilter.sharedMesh != null) Bake(lFilter.transform, lFilter.sharedMesh, null, pStartAge);
        }

        private void Bake(Transform pSource, Mesh pMesh, SkinnedMeshRenderer pSkin, float pStartAge)
        {
            Ghost lGhost = _ghosts[_ghostCursor];
            _ghostCursor = (_ghostCursor + 1) % _ghosts.Length;
            if (pSkin != null) pSkin.BakeMesh(lGhost.mesh);
            else
            {
                lGhost.mesh.Clear();
                lGhost.mesh.vertices = pMesh.vertices;
                lGhost.mesh.normals = pMesh.normals;
                lGhost.mesh.triangles = pMesh.triangles;
            }
            lGhost.localPosition = transform.InverseTransformPoint(pSource.position);
            lGhost.localRotation = Quaternion.Inverse(transform.rotation) * pSource.rotation;
            lGhost.transform.localScale = pSource.lossyScale;
            lGhost.age = pStartAge;
            lGhost.transform.gameObject.SetActive(true);
        }

        // ---------------------------------------------------------------- ground fx

        private void UpdateSteps(PlayerState pState, bool pGrounded, float pSpeed, float pDt)
        {
            if (!pGrounded || pState == PlayerState.Frozen || pSpeed < 3f) return;
            float lPrevious = _stepPhase;
            _stepPhase += pDt * 3.2f * (_animated ? _animator.speed : 1f) * Mathf.Lerp(0.8f, 1.4f, Mathf.Clamp01(pSpeed / 60f));
            if (Mathf.Floor(lPrevious) == Mathf.Floor(_stepPhase)) return;
            EmitDust(2);
            AudioManager.PlayVaried("sfx_step", 4, 0.45f, 1f + Mathf.Clamp01(pSpeed / 120f) * 0.3f, 0.1f);
        }

        private void EmitDust(int pCount)
        {
            ParticleSystem.EmitParams lParams = new ParticleSystem.EmitParams();
            lParams.position = transform.position + transform.up * 0.1f;
            lParams.applyShapeToPosition = true;
            _dust.Emit(lParams, pCount);
        }

        private void EmitLandingDust(float pImpact)
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

        // The camera sits far back at speed : everything scales with the view so it still reads.
        private float ViewScale => CameraRig.Instance != null ? Mathf.Clamp(Vector3.Distance(CameraRig.Instance.transform.position, transform.position) / 8f, 1f, 4.5f) : 1f;

        private void EmitDriftSparks()
        {
            Color[] lColors = { Color.white, new Color(0.3f, 0.9f, 3.4f), new Color(3.4f, 1.6f, 0.2f), new Color(3.4f, 0.4f, 2.8f) };
            float lScale = ViewScale;
            ParticleSystem.EmitParams lParams = new ParticleSystem.EmitParams();
            lParams.startColor = lColors[_player.DriftTier];
            int lCount = 6 + _player.DriftTier * 4;
            float lFlow = Mathf.Min(_player.Speed, 300f);
            for (int i = 0; i < lCount; i++)
            {
                float lSide = (i % 2 == 0 ? -1f : 1f) * 0.4f;
                lParams.position = transform.position + transform.right * lSide - transform.forward * 0.3f + transform.up * 0.1f;
                lParams.velocity = transform.forward * lFlow * 0.5f + transform.right * Random.Range(-1f, 1f) * lFlow * 0.22f + transform.up * Random.Range(3f, 26f);
                lParams.startSize = Random.Range(0.18f, 0.42f) * lScale;
                lParams.startLifetime = Random.Range(0.25f, 0.6f);
                _sparks.Emit(lParams, 1);
            }
            // Ground thrown up in clouds, the colour of the ground.
            _smokeTimer -= Time.deltaTime;
            if (_smokeTimer > 0f) return;
            _smokeTimer = 0.05f;
            Color lDust = RaceContext.Theme != null ? Color.Lerp(RaceContext.Theme.roadA, Color.white, 0.2f) : Color.white;
            FX.Puff(transform.position + transform.up * 0.4f, lDust, 3, 1.4f * lScale, 6f);
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

        private void UpdateShadow()
        {
            if (_shadow == null) return;
            RaycastHit lHit;
            Vector3 lDown = -_player.GravityUp;
            if (Physics.Raycast(transform.position + _player.GravityUp * 0.5f, lDown, out lHit, 60f, Layers.GROUND, QueryTriggerInteraction.Ignore))
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
}
