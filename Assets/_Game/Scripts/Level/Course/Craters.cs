using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // What a crash leaves on the ground (2026-11-12) : a scorched crater with cracks running out of it and a rim of
    // broken chunks. Pooled : the oldest one goes when the pool is full. Purely visual, the track stays solid (holes are
    // TrackHoles' job).
    public static class Craters
    {
        private const int POOL = 48;
        private static readonly GameObject[] _pool = new GameObject[POOL];
        private static int _next;
        private static Mesh _mesh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            for (int i = 0; i < POOL; i++) _pool[i] = null;
            _next = 0;
        }

        // 2026-11-14 : no decal any more (craters are holes, TrackHoles) : only the embers and the dust of the impact.
        public static void Add(Vector3 pPoint, Vector3 pNormal, float pRadius, Color pRim)
        {
            FX.Sparks(pPoint + pNormal * 2f, new Color(1f, 0.45f, 0.1f), 40, pRadius * 1.2f, 1.2f, 1.2f);
        }

        private static void AddDecal(Vector3 pPoint, Vector3 pNormal, float pRadius, Color pRim)
        {
            GameObject lCrater = _pool[_next];
            if (lCrater == null)
            {
                lCrater = new GameObject("Crater");
                lCrater.AddComponent<MeshFilter>().sharedMesh = Mesh;
                lCrater.AddComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _pool[_next] = lCrater;
            }
            Material lMaterial = Mats.Flat(Color.Lerp(pRim, Color.black, 0.75f), false, Color.Lerp(pRim, Color.black, 0.35f));
            // Over a hole only the rim stays : its middle is cut with the floor.
            lMaterial.SetFloat("_Holes", 1f);
            lCrater.GetComponent<MeshRenderer>().sharedMaterial = lMaterial;
            lCrater.transform.SetPositionAndRotation(pPoint + pNormal * 0.4f, Quaternion.FromToRotation(Vector3.up, pNormal) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            lCrater.transform.localScale = Vector3.one * pRadius;
            lCrater.SetActive(true);
            _next = (_next + 1) % POOL;
            // Embers glowing in it for a while.
            FX.Sparks(pPoint + pNormal * 2f, new Color(1f, 0.45f, 0.1f), 40, pRadius * 1.2f, 1.2f, 1.2f);
        }

        // A unit crater : a dark scorched disc, a rim of raised chunks, cracks radiating out.
        private static Mesh Mesh
        {
            get
            {
                if (_mesh != null) return _mesh;
                MeshData lData = new MeshData();
                const int SIDES = 18;
                Color lBurnt = new Color(0.25f, 0.22f, 0.22f);
                Color lCore = new Color(0.08f, 0.06f, 0.06f);
                for (int i = 0; i < SIDES; i++)
                {
                    float lA0 = i * Mathf.PI * 2f / SIDES;
                    float lA1 = (i + 1) * Mathf.PI * 2f / SIDES;
                    Vector3 lD0 = new Vector3(Mathf.Cos(lA0), 0f, Mathf.Sin(lA0));
                    Vector3 lD1 = new Vector3(Mathf.Cos(lA1), 0f, Mathf.Sin(lA1));
                    // Floor of the crater (slightly sunk) and its scorched ring.
                    lData.Quad(Vector3.down * 0.05f, Vector3.down * 0.05f, lD1 * 0.6f, lD0 * 0.6f, Vector3.up, lCore);
                    lData.Quad(lD0 * 0.6f, lD1 * 0.6f, lD1 * 1f + Vector3.up * 0.06f, lD0 * 1f + Vector3.up * 0.06f, Vector3.up, lBurnt);
                    // Rim chunks.
                    if (i % 2 == 0)
                    {
                        Vector3 lC = lD0 * 0.95f;
                        lData.Append(RockSculptor.Boulder(i % 6), Matrix4x4.TRS(lC, Quaternion.Euler(0f, i * 37f, 0f), new Vector3(0.18f, 0.12f + (i % 3) * 0.04f, 0.14f)), new Color(0.35f, 0.3f, 0.3f));
                    }
                    // Cracks.
                    if (i % 3 == 0)
                    {
                        Vector3 lSide = Vector3.Cross(Vector3.up, lD0) * 0.03f;
                        lData.Quad(lD0 * 1f - lSide, lD0 * 1f + lSide, lD0 * 1.7f + Vector3.up * 0.04f, lD0 * 1.7f + Vector3.up * 0.04f, Vector3.up, lCore);
                    }
                }
                _mesh = lData.ToMesh("Crater");
                return _mesh;
            }
        }
    }

    // A red circle on the ground where something is about to hit (2026-11-12) : it pulses and closes in until the
    // impact, so the danger is read well before it lands.
    public class TelegraphRing : MonoBehaviour
    {
        private const bool SHOW = false;
        private float _life;
        private float _time;
        private float _radius;
        private Material _material;
        private Transform _beam;
        private static Mesh _ring;

        public static TelegraphRing Create(Vector3 pPoint, Vector3 pNormal, float pRadius, float pTime, bool pBeam)
        {
            TelegraphRing lRing = new GameObject("Telegraph").AddComponent<TelegraphRing>();
            lRing.transform.SetPositionAndRotation(pPoint + pNormal * 0.6f, Quaternion.FromToRotation(Vector3.up, pNormal));
            lRing._life = Mathf.Max(0.3f, pTime);
            lRing._radius = pRadius;
            // 2026-11-14 : nothing drawn on the ground any more (no circle) : the falling thing itself, seen from far with
            // its smoke, is the warning, and the ticks are heard as it nears.
            if (!SHOW) return lRing;
            lRing._material = new Material(Mats.Glow(new Color(3f, 0.25f, 0.2f, 0.85f), true, 1f));
            GameObject lMesh = new GameObject("Ring");
            lMesh.transform.SetParent(lRing.transform, false);
            lMesh.AddComponent<MeshFilter>().sharedMesh = RingMesh;
            MeshRenderer lRenderer = lMesh.AddComponent<MeshRenderer>();
            lRenderer.sharedMaterial = lRing._material;
            lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (pBeam)
            {
                // A faint red column rising from it : seen from far away.
                GameObject lColumn = Mats.Primitive(PrimitiveType.Cylinder, lRing.transform, Vector3.zero, Vector3.one, lRing._material, "Beam");
                lRing._beam = lColumn.transform;
            }
            lRing.Tick();
            return lRing;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            if (_time >= _life)
            {
                if (_material != null) Destroy(_material);
                Destroy(gameObject);
                return;
            }
            Tick();
        }

        // Warning ticks, closer and closer together as the impact nears, heard when it is near the runner.
        private static readonly float[] TICKS = { 0f, 0.35f, 0.6f, 0.78f, 0.9f, 0.97f };
        private int _tick;

        private void Tick()
        {
            float lT = Mathf.Clamp01(_time / _life);
            while (_tick < TICKS.Length && lT >= TICKS[_tick])
            {
                _tick++;
                PlayerController lPlayer = RaceContext.Player;
                if (lPlayer == null) break;
                float lNear = Mathf.Clamp01(1f - Vector3.Distance(lPlayer.transform.position, transform.position) / 1500f);
                if (lNear > 0.05f) AudioManager.Play("sfx_telegraph_tick", 0.65f * lNear, 1f, 0.02f);
            }
            // Closes from wide to the impact size, pulsing faster and faster.
            if (_material == null) return;
            float lPulse = 0.5f + 0.5f * Mathf.Sin(_time * Mathf.Lerp(8f, 30f, lT));
            transform.localScale = Vector3.one * _radius * Mathf.Lerp(1.8f, 1f, lT * lT);
            _material.SetColor("_Color", new Color(3f, 0.25f, 0.2f, Mathf.Lerp(0.35f, 0.95f, lPulse) * Mathf.Lerp(0.6f, 1f, lT)));
            if (_beam != null)
            {
                // 800 m tall in the world whatever the ring's own scale ; thinner as the impact nears.
                float lScale = Mathf.Max(0.01f, transform.localScale.y);
                _beam.localScale = new Vector3(0.22f * (1f - lT * 0.5f), 400f / lScale, 0.22f * (1f - lT * 0.5f));
                _beam.localPosition = Vector3.up * 400f / lScale;
            }
        }

        // A unit ring with a softer inner disc.
        private static Mesh RingMesh
        {
            get
            {
                if (_ring != null) return _ring;
                MeshData lData = new MeshData();
                const int SIDES = 40;
                for (int i = 0; i < SIDES; i++)
                {
                    float lA0 = i * Mathf.PI * 2f / SIDES;
                    float lA1 = (i + 1) * Mathf.PI * 2f / SIDES;
                    Vector3 lD0 = new Vector3(Mathf.Cos(lA0), 0f, Mathf.Sin(lA0));
                    Vector3 lD1 = new Vector3(Mathf.Cos(lA1), 0f, Mathf.Sin(lA1));
                    lData.Quad(lD0 * 0.86f, lD1 * 0.86f, lD1, lD0, Vector3.up, Color.white);
                    lData.Quad(lD0 * 0.5f, lD1 * 0.5f, lD1 * 0.55f, lD0 * 0.55f, Vector3.up, Color.white);
                    if (i % 4 == 0) lData.Quad(lD0 * 0.6f, lD1 * 0.6f, lD1 * 0.8f, lD0 * 0.8f, Vector3.up, new Color(1f, 1f, 1f, 0.5f));
                }
                _ring = lData.ToMesh("TelegraphRing");
                return _ring;
            }
        }
    }
}
