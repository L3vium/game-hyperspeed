using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum BallMode { None, Rope, Hyper, Light }

    // The runner turned to light (2026-11-11 : "on a rope and in hyper we must be a big ball of light") : a blinding core
    // in a huge soft halo, lightning bursting out of it, a cloud of sparks orbiting, a jagged bolt of lightning trailing
    // behind along the path it took, and a wide streak of light. Bigger on a rope < hyper < light. The runner's body is
    // hidden while it is on.
    public class PlayerLightBall : MonoBehaviour
    {
        private const int ARCS = 16;
        private const int BOLT_POINTS = 26;
        private const float BOLT_TIME = 0.55f;
        private const int HISTORY = 64;

        // Core diameter, halo diameter, streak width per mode (None, Rope, Hyper, Light). 2026-11-12 : smaller, it sat in the
        // middle of the screen and hid the track ahead.
        private static readonly float[] CORE = { 0f, 4f, 7f, 13f };
        private static readonly float[] HALO = { 0f, 10f, 20f, 38f };
        private static readonly float[] STREAK = { 0f, 5f, 11f, 22f };
        private static readonly float[] SPARKS = { 0f, 90f, 160f, 320f };
        private static readonly Color[] TINT = { Color.white, new Color(0.7f, 1.6f, 3.2f), new Color(0.9f, 1.8f, 3.4f), new Color(2.4f, 2.6f, 3.4f) };

        public BallMode Mode { get; private set; }
        public float Size => _size;

        private Transform _root;
        private Transform _core;
        private Transform _halo;
        private Material _haloMaterial;
        private Material _coreMaterial;
        private LineRenderer[] _arcs;
        private LineRenderer _bolt;
        private TrailRenderer _streak;
        private ParticleSystem _orbit;
        private float _size;
        private float _arcTimer;
        private float _boltTimer;
        private readonly Vector3[] _history = new Vector3[HISTORY];
        private readonly float[] _historyTime = new float[HISTORY];
        private int _historyHead;
        private int _historyCount;
        private readonly Vector3[] _boltPoints = new Vector3[BOLT_POINTS];

        public static PlayerLightBall Create(Transform pParent)
        {
            PlayerLightBall lBall = new GameObject("LightBall").AddComponent<PlayerLightBall>();
            lBall.transform.SetParent(pParent, false);
            lBall.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            lBall.Build();
            return lBall;
        }

        private void Build()
        {
            _root = transform;
            _coreMaterial = new Material(Mats.Glow(new Color(8f, 8f, 9f), false, 0f, 0f, true));
            _core = Mats.Primitive(PrimitiveType.Sphere, _root, Vector3.zero, Vector3.one, _coreMaterial, "Core").transform;
            _haloMaterial = new Material(Mats.Glow(new Color(0.7f, 1.4f, 3f, 0.45f), true, 1f, 1.5f));
            _halo = Mats.Primitive(PrimitiveType.Sphere, _root, Vector3.zero, Vector3.one, _haloMaterial, "Halo").transform;
            foreach (Renderer lRenderer in _root.GetComponentsInChildren<Renderer>()) lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Lightning bursting out of the core.
            _arcs = new LineRenderer[ARCS];
            Material lArc = Mats.Glow(new Color(2.4f, 3.4f, 6f), true);
            for (int i = 0; i < ARCS; i++)
            {
                LineRenderer lLine = new GameObject("Arc").AddComponent<LineRenderer>();
                lLine.transform.SetParent(_root, false);
                lLine.useWorldSpace = false;
                lLine.positionCount = 9;
                lLine.sharedMaterial = lArc;
                lLine.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.08f));
                lLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _arcs[i] = lLine;
            }

            // The bolt behind : a jagged line along the path of the last half second, redrawn every few frames.
            _bolt = new GameObject("TrailingBolt").AddComponent<LineRenderer>();
            _bolt.transform.SetParent(_root, false);
            _bolt.useWorldSpace = true;
            _bolt.positionCount = BOLT_POINTS;
            _bolt.sharedMaterial = Mats.Glow(new Color(2.2f, 3.2f, 6f), true);
            _bolt.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.35f, 0.7f), new Keyframe(1f, 0.05f));
            _bolt.numCornerVertices = 1;
            _bolt.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // A wide streak of light.
            _streak = gameObject.AddComponent<TrailRenderer>();
            _streak.time = 0.75f;
            _streak.minVertexDistance = 0.5f;
            _streak.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.2f, 0.85f), new Keyframe(1f, 0f));
            _streak.sharedMaterial = Mats.Glow(new Color(1.2f, 2.2f, 3.2f, 0.85f), true);
            _streak.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Gradient lFade = new Gradient();
            lFade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f, 0.8f, 1f), 1f) },
                          new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            _streak.colorGradient = lFade;

            // Sparks orbiting the ball.
            _orbit = FX.CreateSystem("OrbitSparks", _root.position, Mats.Glow(new Color(2f, 2.6f, 4f, 1f), true, 1f), false);
            _orbit.transform.SetParent(_root, false);
            _orbit.transform.localPosition = Vector3.zero;
            ParticleSystem.MainModule lMain = _orbit.main;
            lMain.loop = true;
            lMain.playOnAwake = false;
            lMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            lMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            lMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 8f);
            lMain.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
            lMain.maxParticles = 800;
            ParticleSystem.ShapeModule lShape = _orbit.shape;
            lShape.enabled = true;
            lShape.shapeType = ParticleSystemShapeType.Sphere;
            lShape.radius = 1f;
            ParticleSystem.VelocityOverLifetimeModule lVelocity = _orbit.velocityOverLifetime;
            lVelocity.enabled = true;
            lVelocity.space = ParticleSystemSimulationSpace.Local;
            lVelocity.orbitalX = new ParticleSystem.MinMaxCurve(-3f, 3f);
            lVelocity.orbitalY = new ParticleSystem.MinMaxCurve(4f, 8f);
            lVelocity.orbitalZ = new ParticleSystem.MinMaxCurve(-3f, 3f);
            ParticleSystem.SizeOverLifetimeModule lSize = _orbit.sizeOverLifetime;
            lSize.enabled = true;
            lSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            ParticleSystem.EmissionModule lEmission = _orbit.emission;
            lEmission.rateOverTime = 0f;

            gameObject.SetActive(false);
        }

        public void SetMode(BallMode pMode)
        {
            if (pMode == Mode) return;
            bool lWasOn = Mode != BallMode.None;
            Mode = pMode;
            bool lOn = pMode != BallMode.None;
            if (lOn && !lWasOn)
            {
                gameObject.SetActive(true);
                _streak.Clear();
                _historyCount = 0;
                _size = CORE[(int)pMode] * 0.3f;
                _orbit.Play();
            }
            else if (!lOn && lWasOn)
            {
                _orbit.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                gameObject.SetActive(false);
            }
        }

        public void Clear()
        {
            _streak.Clear();
            _historyCount = 0;
        }

        public void Tick(float pDt, float pSpeed01, Vector3 pVelocity)
        {
            if (Mode == BallMode.None || pDt <= 0f) return;
            int lMode = (int)Mode;
            // Grows into its size with an overshoot, breathes fast.
            _size = Mathf.Lerp(_size, CORE[lMode], 1f - Mathf.Exp(-7f * pDt));
            float lPulse = 1f + Mathf.Sin(Time.time * 37f) * 0.08f + Mathf.Sin(Time.time * 11f) * 0.05f;
            float lStretch = 1f + Mathf.Clamp01(pSpeed01) * 0.6f;
            Vector3 lForward = pVelocity.sqrMagnitude > 1f ? pVelocity.normalized : _root.forward;
            _root.rotation = Quaternion.LookRotation(lForward, Vector3.up);
            _core.localScale = new Vector3(_size * lPulse, _size * lPulse, _size * lPulse * lStretch);
            float lHalo = _size / CORE[lMode] * HALO[lMode];
            _halo.localScale = new Vector3(lHalo, lHalo, lHalo * (1f + Mathf.Clamp01(pSpeed01) * 0.8f)) * (1f + Mathf.Sin(Time.time * 6f) * 0.06f);
            Color lTint = TINT[lMode];
            _haloMaterial.SetColor(Mats.COLOR, new Color(lTint.r, lTint.g, lTint.b, 0.42f));
            _streak.widthMultiplier = STREAK[lMode] * (_size / CORE[lMode]);
            _streak.time = Mode == BallMode.Light ? 1.1f : 0.75f;

            ParticleSystem.EmissionModule lEmission = _orbit.emission;
            lEmission.rateOverTime = SPARKS[lMode];
            ParticleSystem.ShapeModule lShape = _orbit.shape;
            lShape.radius = lHalo * 0.4f;

            Remember(_root.position);
            UpdateArcs(pDt, lHalo);
            UpdateBolt(pDt);
        }

        private void Remember(Vector3 pPosition)
        {
            _history[_historyHead] = pPosition;
            _historyTime[_historyHead] = Time.time;
            _historyHead = (_historyHead + 1) % HISTORY;
            _historyCount = Mathf.Min(_historyCount + 1, HISTORY);
        }

        private void UpdateArcs(float pDt, float pHalo)
        {
            _arcTimer -= pDt;
            if (_arcTimer > 0f) return;
            _arcTimer = 0.035f;
            float lWidth = Mathf.Max(0.4f, _size * 0.09f);
            for (int i = 0; i < _arcs.Length; i++)
            {
                LineRenderer lLine = _arcs[i];
                Vector3 lDir = Random.onUnitSphere;
                Vector3 lSide = Vector3.Cross(lDir, Random.onUnitSphere).normalized;
                float lReach = Random.Range(0.45f, 0.95f) * pHalo;
                lLine.widthMultiplier = lWidth;
                for (int p = 0; p < lLine.positionCount; p++)
                {
                    float lT = p / (lLine.positionCount - 1f);
                    lLine.SetPosition(p, lDir * lReach * lT + lSide * Mathf.Sin(lT * Mathf.PI) * Random.Range(-0.15f, 0.15f) * pHalo);
                }
            }
        }

        // The bolt follows where the ball has been over BOLT_TIME, kinked at random, thick at the ball.
        private void UpdateBolt(float pDt)
        {
            _boltTimer -= pDt;
            if (_boltTimer > 0f) return;
            _boltTimer = 0.04f;
            if (_historyCount < 3)
            {
                _bolt.positionCount = 0;
                return;
            }
            float lNow = Time.time;
            int lValid = 0;
            for (int i = 0; i < _historyCount; i++)
            {
                int lIndex = (_historyHead - 1 - i + HISTORY) % HISTORY;
                if (lNow - _historyTime[lIndex] > BOLT_TIME) break;
                lValid++;
            }
            if (lValid < 3)
            {
                _bolt.positionCount = 0;
                return;
            }
            float lJag = _size * 0.35f;
            _bolt.positionCount = BOLT_POINTS;
            for (int p = 0; p < BOLT_POINTS; p++)
            {
                float lT = p / (BOLT_POINTS - 1f);
                float lF = lT * (lValid - 1);
                int lA = Mathf.FloorToInt(lF);
                int lB = Mathf.Min(lA + 1, lValid - 1);
                Vector3 lPa = _history[(_historyHead - 1 - lA + HISTORY) % HISTORY];
                Vector3 lPb = _history[(_historyHead - 1 - lB + HISTORY) % HISTORY];
                Vector3 lPoint = Vector3.Lerp(lPa, lPb, lF - lA);
                if (p > 0) lPoint += Random.insideUnitSphere * lJag * Mathf.Sin(lT * Mathf.PI * 0.5f + 0.4f);
                _boltPoints[p] = lPoint;
            }
            _bolt.SetPositions(_boltPoints);
            _bolt.widthMultiplier = Mathf.Max(0.6f, _size * 0.22f);
        }
    }
}
