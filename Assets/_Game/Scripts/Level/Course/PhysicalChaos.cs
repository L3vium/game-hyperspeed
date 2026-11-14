using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // The world is a mess (2026-11-14, asked : a Clustertruck-like chaos, physical, toon, bursts of colour) : real
    // rigid bodies everywhere around the runner. Crates, barrels, rocks and wrecked trucks fall from the sky ahead, get
    // thrown in from the sides, bounce, tumble, roll down the hills, knock into each other, fall through the holes,
    // and every blast pushes them away. Dash through one : it bursts. Run into one : it costs speed. They never block the
    // camera and never stop the runner dead (debris layer, touched through Breakable).
    // Fewer and smaller on the first stages, a storm of them on the last ones.
    public class PhysicalChaos : MonoBehaviour
    {
        private LevelLayout _layout;
        private Theme _theme;
        private float _dropTimer = 2f;
        private float _sideTimer = 3f;
        private float _difficulty;
        private float _root = 1f;

        public static PhysicalChaos Instance { get; private set; }

        public static PhysicalChaos Create(Transform pParent, LevelLayout pLayout, Theme pTheme)
        {
            PhysicalChaos lChaos = new GameObject("PhysicalChaos").AddComponent<PhysicalChaos>();
            lChaos.transform.SetParent(pParent, false);
            lChaos._layout = pLayout;
            lChaos._theme = pTheme;
            lChaos._difficulty = pLayout.profile != null ? pLayout.profile.difficulty : 0.5f;
            lChaos._root = Mathf.Sqrt(Mathf.Max(1f, pLayout.profile != null ? pLayout.profile.scale : 1f));
            Instance = lChaos;
            SkyStreaks.Create(lChaos.transform, pLayout, pTheme);
            return lChaos;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            ChaosBody.ClearAll();
        }

        private int Budget => Mathf.RoundToInt(Mathf.Lerp(45f, 150f, _difficulty));

        private void Update()
        {
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null || _layout == null || !RaceContext.Running) return;
            float lDt = Time.deltaTime;
            _dropTimer -= lDt;
            _sideTimer -= lDt;
            if (_dropTimer <= 0f)
            {
                _dropTimer = Random.Range(0.6f, 1.4f) * Mathf.Lerp(1.6f, 0.35f, _difficulty);
                if (ChaosBody.Live < Budget) Drop(lPlayer);
            }
            if (_sideTimer <= 0f)
            {
                _sideTimer = Random.Range(0.6f, 1.4f) * Mathf.Lerp(2.4f, 0.55f, _difficulty);
                if (ChaosBody.Live < Budget) SideShow(lPlayer);
            }
        }

        private float Ahead(PlayerController pPlayer, float pMinSeconds, float pMaxSeconds)
        {
            float lSpeed = Mathf.Max(pPlayer.Speed, _layout.profile.cruise);
            return Mathf.Min(pPlayer.Progress + lSpeed * Random.Range(pMinSeconds, pMaxSeconds), _layout.finishS - 50f);
        }

        // Things raining from high up ahead : seen falling from far, they hit the ground, bounce and tumble.
        private void Drop(PlayerController pPlayer)
        {
            float lS = Ahead(pPlayer, 1.4f, 3.2f);
            if (lS <= pPlayer.Progress) return;
            float lHalf = _layout.field.HalfWidth(lS);
            int lCount = Random.Range(1, 3 + Mathf.RoundToInt(_difficulty * 3f));
            for (int i = 0; i < lCount; i++)
            {
                float lLateral = _layout.field.CenterLateral(lS) + Random.Range(-1.35f, 1.35f) * lHalf;
                Vector3 lGround = _layout.GroundPoint(lS + Random.Range(-40f, 40f) * _root, lLateral);
                Vector3 lAt = lGround + Vector3.up * Random.Range(160f, 420f) * _root;
                Vector3 lVelocity = Vector3.down * Random.Range(20f, 80f) + Random.insideUnitSphere * 25f;
                ChaosBody.Spawn(transform, lAt, lVelocity, RandomSize(), _theme, _layout);
            }
        }

        // From the sides : a heap of things thrown up over the edge of the track, arcing in and bouncing across it.
        private void SideShow(PlayerController pPlayer)
        {
            float lS = Ahead(pPlayer, 1.1f, 2.6f);
            if (lS <= pPlayer.Progress) return;
            SpineSample lSample = _layout.spine.Evaluate(lS);
            int lSide = Random.value < 0.5f ? -1 : 1;
            float lOut = _layout.field.TotalHalfWidth(lS) + Random.Range(20f, 160f) * _root;
            Vector3 lBase = lSample.position + lSample.Right * (_layout.field.CenterLateral(lS) + lSide * lOut);
            int lCount = Random.Range(2, 4 + Mathf.RoundToInt(_difficulty * 4f));
            for (int i = 0; i < lCount; i++)
            {
                Vector3 lAt = lBase + lSample.forward * Random.Range(-80f, 80f) * _root + Vector3.up * Random.Range(-20f, 60f) * _root;
                Vector3 lVelocity = -lSample.Right * lSide * Random.Range(40f, 140f) * _root + Vector3.up * Random.Range(70f, 160f)
                                    + lSample.forward * Random.Range(-30f, 60f) * _root;
                ChaosBody.Spawn(transform, lAt, lVelocity, RandomSize(), _theme, _layout);
            }
        }

        private float RandomSize() => Random.Range(7f, 22f + 10f * _difficulty) * _root;

        // A blast pushes every body near it away (meteors, crashes, lightning).
        public static void Blast(Vector3 pAt, float pRadius, float pForce)
        {
            ChaosBody.Push(pAt, pRadius, pForce);
        }
    }

    public class ChaosBody : Breakable
    {
        public enum Shape { Crate, Barrel, Rock, Wreck }

        private static readonly List<ChaosBody> _live = new List<ChaosBody>();
        private static readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();
        private static float _lastThud = -10f;

        public static int Live => _live.Count;

        private Shape _shape;
        private Rigidbody _body;
        private LevelLayout _layout;
        private float _gravity = Ballistics.G_FALL;
        private bool _landed;

        public override string Material => _shape == Shape.Rock ? "rock" : _shape == Shape.Wreck ? "door" : "crate";

        public static void ClearAll()
        {
            for (int i = _live.Count - 1; i >= 0; i--) if (_live[i] != null) Destroy(_live[i].gameObject);
            _live.Clear();
            _materials.Clear();
        }

        public static ChaosBody Spawn(Transform pParent, Vector3 pAt, Vector3 pVelocity, float pSize, Theme pTheme, LevelLayout pLayout)
        {
            Shape lShape = (Shape)Random.Range(0, 4);
            GameObject lObject;
            Color lColor = Pick(pTheme);
            switch (lShape)
            {
                case Shape.Barrel:
                    lObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    lObject.transform.localScale = new Vector3(pSize * 0.6f, pSize * 0.45f, pSize * 0.6f);
                    break;
                case Shape.Rock:
                    lObject = new GameObject("Rock");
                    Mesh lMesh = RockSculptor.Boulder(Random.Range(0, 6));
                    lObject.AddComponent<MeshFilter>().sharedMesh = lMesh;
                    lObject.AddComponent<MeshRenderer>();
                    MeshCollider lHull = lObject.AddComponent<MeshCollider>();
                    lHull.sharedMesh = lMesh;
                    lHull.convex = true;
                    lObject.transform.localScale = new Vector3(pSize, pSize * 0.8f, pSize * 1.1f);
                    lColor = Color.Lerp(pTheme.decorC, pTheme.roadSide, Random.value * 0.5f);
                    break;
                case Shape.Wreck:
                    // A truck : a long box with its cab, tumbling end over end.
                    lObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    lObject.transform.localScale = new Vector3(pSize * 0.7f, pSize * 0.6f, pSize * 1.8f);
                    GameObject lCab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(lCab.GetComponent<Collider>());
                    lCab.transform.SetParent(lObject.transform, false);
                    lCab.transform.localPosition = new Vector3(0f, 0.15f, 0.62f);
                    lCab.transform.localScale = new Vector3(0.96f, 1.15f, 0.3f);
                    lCab.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(Color.Lerp(lColor, Color.white, 0.35f), pTheme);
                    lCab.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    break;
                default:
                    lObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    lObject.transform.localScale = Vector3.one * pSize * 0.8f;
                    break;
            }
            lObject.name = "Chaos" + lShape;
            lObject.layer = Layers.DEBRIS;
            lObject.transform.SetParent(pParent, true);
            lObject.transform.SetPositionAndRotation(pAt, Random.rotation);
            MeshRenderer lRenderer = lObject.GetComponent<MeshRenderer>();
            lRenderer.sharedMaterial = MaterialFor(lColor, pTheme);
            lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            ChaosBody lChaos = lObject.AddComponent<ChaosBody>();
            lChaos._shape = lShape;
            lChaos._layout = pLayout;
            lChaos.radius = pSize * 0.6f;
            lChaos.color = lColor;
            Rigidbody lBody = lObject.AddComponent<Rigidbody>();
            lBody.mass = pSize * pSize * pSize * 0.05f;
            lBody.useGravity = false;
            lBody.interpolation = RigidbodyInterpolation.Interpolate;
            lBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            lBody.linearDamping = 0.05f;
            lBody.angularDamping = 0.2f;
            lBody.linearVelocity = pVelocity;
            lBody.angularVelocity = Random.insideUnitSphere * Random.Range(1f, 4f);
            lChaos._body = lBody;
            _live.Add(lChaos);
            return lChaos;
        }

        private static Color Pick(Theme pTheme)
        {
            switch (Random.Range(0, 5))
            {
                case 0: return pTheme.accent;
                case 1: return pTheme.decorA;
                case 2: return pTheme.decorB;
                case 3: return Color.Lerp(pTheme.accent, Color.white, 0.3f);
                default: return pTheme.decorC;
            }
        }

        private static Material MaterialFor(Color pColor, Theme pTheme)
        {
            Material lMaterial;
            if (_materials.TryGetValue(pColor, out lMaterial) && lMaterial != null) return lMaterial;
            lMaterial = Mats.Toon(pColor, pTheme.shade, 0.035f, 0.5f);
            _materials[pColor] = lMaterial;
            return lMaterial;
        }

        public static void Push(Vector3 pAt, float pRadius, float pForce)
        {
            float lRadiusSqr = pRadius * pRadius;
            foreach (ChaosBody lChaos in _live)
            {
                if (lChaos == null || lChaos._body == null) continue;
                if ((lChaos.transform.position - pAt).sqrMagnitude > lRadiusSqr) continue;
                lChaos._body.AddExplosionForce(pForce, pAt, pRadius, 0.6f, ForceMode.VelocityChange);
            }
        }

        private void OnDestroy()
        {
            _live.Remove(this);
        }

        private void FixedUpdate()
        {
            if (_body != null) _body.AddForce(Vector3.down * _gravity, ForceMode.Acceleration);
        }

        protected override void Update()
        {
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null || _layout == null) return;
            // Gone once far behind the runner, or far under the stage.
            Vector3 lAt = transform.position;
            float lBehind = Mathf.Max(400f, lPlayer.Speed * 3f);
            if (Vector3.Dot(lAt - lPlayer.transform.position, lPlayer.Heading) < -lBehind
                || lAt.y < lPlayer.transform.position.y - 3000f * Mathf.Sqrt(Mathf.Max(1f, _layout.profile.scale)))
                Destroy(gameObject);
        }

        // Every big landing : a toon puff and a burst of coloured chips, a thud when it is near.
        private void OnCollisionEnter(Collision pCollision)
        {
            float lImpact = pCollision.relativeVelocity.magnitude;
            if (lImpact < 35f || (_landed && lImpact < 70f)) return;
            _landed = true;
            Vector3 lAt = pCollision.contactCount > 0 ? pCollision.GetContact(0).point : transform.position;
            float lSize = radius;
            Theme lTheme = RaceContext.Theme;
            Color lDust = lTheme != null ? Color.Lerp(lTheme.roadA, Color.white, 0.35f) : Color.white;
            FX.Puff(lAt, lDust, 6, lSize * 0.9f, lSize * 0.6f);
            FX.Shards(lAt, color, 10, lSize * 1.4f, lSize * 0.12f, Vector3.up * lSize);
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer != null && Time.time - _lastThud > 0.12f)
            {
                float lDistance = Vector3.Distance(lPlayer.transform.position, lAt);
                if (lDistance < 900f)
                {
                    _lastThud = Time.time;
                    AudioManager.Play("sfx_land_heavy", Mathf.Clamp01(1f - lDistance / 900f) * 0.55f, Random.Range(0.6f, 0.9f));
                }
            }
        }
    }

    // The sky is never calm (2026-11-14) : meteors streak across it all the time, far away, and burst into colour.
    public class SkyStreaks : MonoBehaviour
    {
        private const int POOL = 14;
        private LevelLayout _layout;
        private Theme _theme;
        private float _timer = 0.5f;
        private float _root = 1f;
        private float _difficulty;
        private readonly List<Streak> _streaks = new List<Streak>();

        private class Streak
        {
            public Transform head;
            public LineRenderer line;
            public Vector3 velocity;
            public float life;
            public float age;
            public Color color;
            public bool active;
        }

        public static SkyStreaks Create(Transform pParent, LevelLayout pLayout, Theme pTheme)
        {
            SkyStreaks lSky = new GameObject("SkyStreaks").AddComponent<SkyStreaks>();
            lSky.transform.SetParent(pParent, false);
            lSky._layout = pLayout;
            lSky._theme = pTheme;
            lSky._root = Mathf.Sqrt(Mathf.Max(1f, pLayout.profile != null ? pLayout.profile.scale : 1f));
            lSky._difficulty = pLayout.profile != null ? pLayout.profile.difficulty : 0.5f;
            return lSky;
        }

        private Streak Free()
        {
            foreach (Streak lStreak in _streaks) if (!lStreak.active) return lStreak;
            if (_streaks.Count >= POOL) return null;
            Streak lNew = new Streak();
            GameObject lHead = Mats.Primitive(PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one, Mats.Glow(Color.white * 3f), "StreakHead");
            Collider lCollider = lHead.GetComponent<Collider>();
            if (lCollider != null) Destroy(lCollider);
            lNew.head = lHead.transform;
            GameObject lLineObject = new GameObject("StreakTrail");
            lLineObject.transform.SetParent(transform, false);
            LineRenderer lLine = lLineObject.AddComponent<LineRenderer>();
            lLine.positionCount = 2;
            lLine.useWorldSpace = true;
            lLine.sharedMaterial = Mats.Glow(Color.white * 2f);
            lLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lNew.line = lLine;
            _streaks.Add(lNew);
            return lNew;
        }

        private void Update()
        {
            PlayerController lPlayer = RaceContext.Player;
            Camera lCamera = CameraRig.Instance != null ? CameraRig.Instance.Cam : null;
            if (lPlayer == null || lCamera == null) return;
            float lDt = Time.deltaTime;
            _timer -= lDt;
            if (_timer <= 0f)
            {
                _timer = Random.Range(0.25f, 0.9f) * Mathf.Lerp(1.5f, 0.5f, _difficulty);
                Launch(lPlayer, lCamera);
            }
            foreach (Streak lStreak in _streaks)
            {
                if (!lStreak.active) continue;
                lStreak.age += lDt;
                lStreak.head.position += lStreak.velocity * lDt;
                float lT = lStreak.age / lStreak.life;
                Vector3 lTail = lStreak.head.position - lStreak.velocity.normalized * Mathf.Min(lStreak.velocity.magnitude * 0.35f, 2600f * _root);
                lStreak.line.SetPosition(0, lStreak.head.position);
                lStreak.line.SetPosition(1, lTail);
                float lWidth = Mathf.Lerp(18f, 6f, lT) * _root;
                lStreak.line.startWidth = lWidth;
                lStreak.line.endWidth = 0f;
                Color lColor = lStreak.color;
                lStreak.line.startColor = lColor;
                lStreak.line.endColor = new Color(lColor.r, lColor.g, lColor.b, 0f);
                if (lStreak.age >= lStreak.life)
                {
                    // It bursts : a flash of colour high in the sky.
                    FX.Burst(lStreak.head.position, lStreak.velocity.normalized, lStreak.color, 260f * _root);
                    FX.Sparks(lStreak.head.position, lStreak.color, 40, 400f * _root, 8f * _root, 1.2f);
                    lStreak.active = false;
                    lStreak.head.gameObject.SetActive(false);
                    lStreak.line.enabled = false;
                }
            }
        }

        private void Launch(PlayerController pPlayer, Camera pCamera)
        {
            Streak lStreak = Free();
            if (lStreak == null) return;
            Vector3 lForward = Vector3.ProjectOnPlane(pCamera.transform.forward, Vector3.up).normalized;
            if (lForward.sqrMagnitude < 0.01f) lForward = Vector3.forward;
            Vector3 lRight = Vector3.Cross(Vector3.up, lForward);
            float lFar = Random.Range(4000f, 12000f) * _root;
            Vector3 lStart = pPlayer.transform.position + lForward * lFar * Random.Range(0.6f, 1.1f) + lRight * Random.Range(-1f, 1f) * lFar
                             + Vector3.up * Random.Range(1500f, 4500f) * _root;
            Vector3 lDirection = (lRight * (Random.value < 0.5f ? -1f : 1f) + Vector3.down * Random.Range(0.25f, 0.7f) + lForward * Random.Range(-0.3f, 0.3f)).normalized;
            Color[] lColors = { _theme.accent, new Color(1f, 0.45f, 0.1f), new Color(1f, 0.3f, 0.8f), new Color(0.4f, 0.9f, 1f), new Color(1f, 0.9f, 0.3f) };
            lStreak.color = lColors[Random.Range(0, lColors.Length)] * 2.2f;
            lStreak.color.a = 1f;
            lStreak.velocity = lDirection * Random.Range(1800f, 5200f) * _root;
            lStreak.life = Random.Range(1.4f, 3.2f);
            lStreak.age = 0f;
            lStreak.active = true;
            lStreak.head.position = lStart;
            lStreak.head.localScale = Vector3.one * Random.Range(40f, 120f) * _root;
            lStreak.head.gameObject.SetActive(true);
            lStreak.line.enabled = true;
        }
    }
}
