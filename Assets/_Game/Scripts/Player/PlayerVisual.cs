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
        private const float SWIRL_TIME = 0.45f;
        private const float FLIP_TIME = 0.55f;
        private const float TUMBLE_TIME = 0.7f;
        private const float PUNCH_TIME = 0.28f;
        private const int RIBBON_POINTS = 10;

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

        // Humanoid bones (null on the placeholder).
        private Transform _hips, _spine, _chest, _neck, _head;
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

            _player.OnJumped += () => { _stretchVelocity += 2.4f; Trigger(P_JUMP); };
            _player.OnDoubleJumped += () =>
            {
                _stretchVelocity += 1.5f;
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
                if (_hips != null) _comHeight = Mathf.Clamp(Vector3.Dot(_hips.position - transform.position, transform.up), 0.6f, 1.3f);
                if (_head != null) _headForward = Quaternion.Inverse(_head.rotation) * transform.forward;
                AddSuitDetails();
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

        // Futuristic details on the rig : emissive visor, light strips on forearms and shins.
        private void AddSuitDetails()
        {
            Color lCyan = new Color(0.4f, 2.4f, 3f);
            if (_head != null && _neck != null)
            {
                float lHead = Mathf.Clamp(Vector3.Distance(_head.position, _neck.position), 0.06f, 0.2f);
                GameObject lVisor = Mats.Primitive(PrimitiveType.Sphere, null, Vector3.zero, new Vector3(lHead * 1.9f, lHead * 0.55f, lHead * 0.9f), Mats.Glow(lCyan), "Visor");
                lVisor.transform.SetPositionAndRotation(_head.position + transform.up * lHead * 0.65f + transform.forward * lHead * 0.75f, transform.rotation);
                lVisor.transform.SetParent(_head, true);
            }
            Strip(_lowerArmL, _handL, lCyan);
            Strip(_lowerArmR, _handR, lCyan);
            Strip(_animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg), _animator.GetBoneTransform(HumanBodyBones.LeftFoot), lCyan);
            Strip(_animator.GetBoneTransform(HumanBodyBones.RightLowerLeg), _animator.GetBoneTransform(HumanBodyBones.RightFoot), lCyan);
        }

        private void Strip(Transform pFrom, Transform pTo, Color pColor)
        {
            if (pFrom == null || pTo == null) return;
            Vector3 lAxis = pTo.position - pFrom.position;
            float lLength = lAxis.magnitude;
            if (lLength < 0.05f) return;
            GameObject lStrip = Mats.Primitive(PrimitiveType.Cube, null, Vector3.zero, new Vector3(0.025f, lLength * 0.7f, 0.025f), Mats.Glow(pColor), "LightStrip");
            Vector3 lOut = Vector3.ProjectOnPlane(transform.forward, lAxis).normalized;
            lStrip.transform.SetPositionAndRotation(pFrom.position + lAxis * 0.5f + lOut * 0.055f, Quaternion.LookRotation(lOut, lAxis));
            lStrip.transform.SetParent(pFrom, true);
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
            Transform lLeft = _handL != null ? _handL : Pivot("HandL", _model, new Vector3(-0.42f, 1f, 0f));
            Transform lRight = _handR != null ? _handR : Pivot("HandR", _model, new Vector3(0.42f, 1f, 0f));
            _trailL = CreateTrail(lLeft, new Color(0.4f, 1.8f, 2.4f, 0.9f));
            _trailR = CreateTrail(lRight, new Color(2.4f, 0.5f, 1.9f, 0.9f));
        }

        private TrailRenderer CreateTrail(Transform pParent, Color pColor)
        {
            Transform lAnchor = Pivot("HandTrail", pParent, Vector3.zero);
            TrailRenderer lTrail = lAnchor.gameObject.AddComponent<TrailRenderer>();
            lTrail.time = 0.16f;
            lTrail.startWidth = 0.05f;
            lTrail.endWidth = 0f;
            lTrail.minVertexDistance = 0.05f;
            lTrail.sharedMaterial = Mats.Glow(pColor, true);
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
                Ribbon lRibbon = new Ribbon { offset = new Vector3(i == 0 ? -0.06f : 0.06f, -0.02f, -0.11f), segment = i == 0 ? 0.13f : 0.11f };
                GameObject lObject = new GameObject("Ribbon");
                lObject.transform.SetParent(transform, false);
                lRibbon.line = lObject.AddComponent<LineRenderer>();
                lRibbon.line.positionCount = RIBBON_POINTS;
                lRibbon.line.useWorldSpace = true;
                lRibbon.line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.07f), new Keyframe(0.7f, 0.05f), new Keyframe(1f, 0.01f));
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
            _stretchVelocity -= Mathf.Clamp(pImpact * 0.06f, 0.5f, 2.5f);
            float lSpeed = _player.Speed;
            if (pImpact > 30f) Trigger(lSpeed < 25f ? P_LAND_HARD : P_LAND_ROLL);
            else if (pImpact > 8f && lSpeed < 25f) Trigger(P_LAND);
            EmitLandingDust(pImpact);
        }

        private void OnDashed()
        {
            bool lAir = _player.State == PlayerState.Air;
            _swirlT = 0f;
            _swirlAir = lAir;
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
        }

        public void SetVictory() => SetResult(true);

        public void SetResult(bool pWin)
        {
            if (!_animated) return;
            _animator.SetBool(pWin ? P_WIN : P_LOSE, true);
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
            bool lOnSurface = lGrounded || lState == PlayerState.Grind || lState == PlayerState.WallRun;
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
            _flipT = Mathf.MoveTowards(_flipT, 1f, pDt / FLIP_TIME);
            _tumbleT = Mathf.MoveTowards(_tumbleT, 1f, pDt / TUMBLE_TIME);
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
                    break;
                case PlayerState.Air:
                    lBank = _player.Steer * (_player.Gliding ? 18f : 10f);
                    if (_player.Diving)
                    {
                        float lHorizontal = Vector3.ProjectOnPlane(_player.Velocity, _player.GravityUp).magnitude;
                        lPitch = Mathf.Clamp(Mathf.Atan2(-lVertical, Mathf.Max(1f, lHorizontal)) * Mathf.Rad2Deg * 0.85f, 0f, 80f);
                    }
                    else if (_player.Gliding) lPitch = 12f;
                    else lPitch = lVertical > 0f ? -5f : 7f;
                    break;
                case PlayerState.Grind:
                    lBank = Mathf.Sin(Time.time * 7f) * 4f;
                    break;
                case PlayerState.Zip:
                    lBank = Mathf.Sin(Time.time * 3f) * 8f;
                    lPitch = -8f;
                    break;
                case PlayerState.WallRun:
                    lBank = -_player.WallSide * 10f;
                    break;
            }
            _bank = Mathf.Lerp(_bank, lBank, 1f - Mathf.Exp(-9f * pDt));
            _pitch = Mathf.Lerp(_pitch, lPitch, 1f - Mathf.Exp(-9f * pDt));

            float lSwirl = _swirlT < 1f ? EaseInOut(_swirlT) * 360f * _swirlDir : 0f;
            float lFlip = _flipT < 1f ? EaseInOut(_flipT) * 360f : 0f;
            float lTumble = _tumbleT < 1f ? (1f - (1f - _tumbleT) * (1f - _tumbleT)) * 360f : 0f;
            bool lAround = !lGrounded || (_swirlT < 1f && _swirlAir) || _flipT < 1f || _tumbleT < 1f;
            _pivotH = Mathf.MoveTowards(_pivotH, lAround ? _comHeight : 0f, pDt * 8f);

            float lYawOffset = _swirlAir ? 0f : lSwirl;
            float lRoll = -_bank - (_swirlAir ? lSwirl : 0f) + lTumble * 0.35f;
            float lPitchTotal = _pitch + lFlip + lTumble;
            Quaternion lRotation = Quaternion.AngleAxis(lYawOffset, Vector3.up) * Quaternion.AngleAxis(lRoll, Vector3.forward) * Quaternion.AngleAxis(lPitchTotal, Vector3.right);

            // Squash & stretch around the same pivot, volume preserved.
            _stretchVelocity += ((1f - _stretch) * 320f - _stretchVelocity * 16f) * pDt;
            _stretch = Mathf.Clamp(_stretch + _stretchVelocity * pDt, 0.82f, 1.18f);
            _zStretchVelocity += ((1f - _zStretch) * 260f - _zStretchVelocity * 15f) * pDt;
            _zStretch = Mathf.Clamp(_zStretch + _zStretchVelocity * pDt, 0.88f, 1.22f);
            Vector3 lScale = new Vector3(1f / Mathf.Sqrt(_stretch * _zStretch), _stretch / Mathf.Sqrt(_zStretch), _zStretch / Mathf.Sqrt(_stretch));
            Vector3 lPivot = new Vector3(0f, _pivotH, 0f);
            _body.localRotation = lRotation;
            _body.localScale = lScale;
            _body.localPosition = lPivot - lRotation * Vector3.Scale(lScale, lPivot);

            // Posture targets for the rig.
            float lArrow = lState == PlayerState.Ground || lState == PlayerState.Finished ? Mathf.Clamp01((lSpeed - 75f) / 50f) : 0f;
            if (_player.Hyper && lGrounded) lArrow = 1f;
            _arrow = Mathf.Lerp(_arrow, lArrow, 1f - Mathf.Exp(-5f * pDt));
            _swayPhase += pDt * Mathf.Lerp(5f, 9f, Mathf.Clamp01((lSpeed - 90f) / 90f));
            _openArms = _swirlT < 1f ? Mathf.Sin(_swirlT * Mathf.PI) : Mathf.MoveTowards(_openArms, 0f, pDt * 6f);
            _lookTarget = _player.HasLandingPrediction && lState == PlayerState.Air ? _player.LandingPoint : transform.position + lHeading * 30f + lUp * 1.2f;

            UpdateAnimator(lState, lGrounded, lSpeed, lVertical, pDt);
            UpdateAura(pDt, lSpeed);
            UpdateShadow();
            UpdateSteps(lState, lGrounded, lSpeed, pDt);

            bool lBlink = _player.Invulnerable && Mathf.Repeat(Time.time * 12f, 1f) > 0.5f;
            _block.SetFloat(FLASH, _flash > 0.3f || lBlink ? 0.85f : 0f);
            foreach (Renderer lRenderer in _bodyRenderers) if (lRenderer != null) lRenderer.SetPropertyBlock(_block);

            bool lTrails = lState != PlayerState.Dead && (lSpeed > _player.Profile.cruise * 1.05f || _player.Dashing || _player.Hyper);
            _trailL.emitting = lTrails;
            _trailR.emitting = lTrails;
            if (lState == PlayerState.Grind) EmitFootSparks(UIFactory.YELLOW, 2);
            if (_player.Drifting) EmitDriftSparks();
        }

        private void UpdateAnimator(PlayerState pState, bool pGrounded, float pSpeed, float pVertical, float pDt)
        {
            if (!_animated) return;
            float lSpeedParam = pState == PlayerState.Frozen ? 0f : Mathf.Clamp01(pSpeed / 55f);
            _animator.SetFloat(P_SPEED, lSpeedParam, 0.08f, pDt);
            _animator.SetFloat(P_VERTICAL, pVertical);
            _animator.SetBool(P_GROUNDED, pGrounded);
            _animator.SetBool(P_GLIDE, _player.Gliding);
            _animator.SetBool(P_DIVE, _player.Diving);
            _animator.SetBool(P_SLIDE, pGrounded && (_player.Sliding || _player.Drifting));
            _animator.SetBool(P_GRIND, pState == PlayerState.Grind);
            _animator.SetBool(P_HANG, pState == PlayerState.Zip);
            _animator.SetBool(P_WALLRUN, pState == PlayerState.WallRun && _player.Wall == PlayerController.WallMode.Run);
            _animator.SetBool(P_WALLCLIMB, pState == PlayerState.WallRun && _player.Wall == PlayerController.WallMode.Climb);
            _animator.SetBool(P_WALLSLIDE, pState == PlayerState.WallRun && _player.Wall == PlayerController.WallMode.Slide);
            _animator.SetBool(P_PRERACE, pState == PlayerState.Frozen || _player.FreeRoam);
            // Stylized cadence : the legs keep up with speeds no human reaches.
            _animator.speed = pGrounded && pState != PlayerState.Frozen ? 1f + Mathf.Clamp01((pSpeed - 20f) / 140f) * 0.8f : 1f;
        }

        // ---------------------------------------------------------------- procedural layer (after the Animator)

        private void LateUpdate()
        {
            if (_player == null) return;
            float lDt = Time.deltaTime;
            UpdateLightForm();
            if (_hips != null && !_lightOn) ApplyRig();
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
            float lBend = _arrow * 26f;
            Rotate(_spine, lRight, lBend * 0.5f);
            Rotate(_chest, lRight, lBend * 0.5f);
            Rotate(_neck, lRight, -lBend * 0.65f);

            // Arms : swept back at speed with the hands swaying left / right, open during swirls, windmilling at an edge.
            float lSway = Mathf.Sin(_swayPhase) * 0.45f * _arrow;
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
            Vector3 lWind = -transform.forward * Mathf.Min(lSpeed, 120f) * 0.6f + Vector3.down * 9f;
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

        private Transform _lightForm;
        private TrailRenderer _lightTrail;
        private bool _lightOn;

        private void BuildLightForm()
        {
            _lightForm = new GameObject("LightForm").transform;
            _lightForm.SetParent(transform, false);
            _lightForm.localPosition = new Vector3(0f, 1f, 0f);
            Mats.Primitive(PrimitiveType.Sphere, _lightForm, Vector3.zero, Vector3.one * 1.8f, Mats.Glow(new Color(6f, 6.5f, 6.5f), false, 0f, 0f, true));
            Mats.Primitive(PrimitiveType.Sphere, _lightForm, new Vector3(0f, 0f, -5f), new Vector3(1.3f, 1.3f, 12f), Mats.Glow(new Color(2f, 3.4f, 5f, 0.8f), true));
            _lightTrail = _lightForm.gameObject.AddComponent<TrailRenderer>();
            _lightTrail.time = 0.7f;
            _lightTrail.startWidth = 2.6f;
            _lightTrail.endWidth = 0f;
            _lightTrail.sharedMaterial = Mats.Glow(new Color(3f, 5f, 6f), true);
            _lightTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lightForm.gameObject.SetActive(false);
        }

        private void UpdateLightForm()
        {
            if (_lightForm == null) BuildLightForm();
            bool lWant = _player.LightForm && _player.State != PlayerState.Dead;
            if (lWant != _lightOn)
            {
                _lightOn = lWant;
                _lightForm.gameObject.SetActive(lWant);
                _lightTrail.Clear();
                if (_model != null) _model.gameObject.SetActive(!lWant);
                FX.Burst(transform.position + transform.up, transform.forward, Color.white, 30f);
                FX.Shockwave(transform.position + transform.up, transform.forward, Color.white, 30f, 0.35f);
            }
            if (lWant)
            {
                float lPulse = 1f + Mathf.Sin(Time.time * 37f) * 0.15f;
                _lightForm.localScale = new Vector3(lPulse, lPulse, lPulse * (1f + _player.Speed01 * 2f));
            }
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
            AudioManager.Play("sfx_step", 0.22f, 1f + Mathf.Clamp01(pSpeed / 120f) * 0.3f, 0.12f);
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
