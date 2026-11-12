using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Holes punched through the track by what crashes on it (2026-11-12), on the hardest stages (world 3) : a sphere per
    // hole. The track shader (HS_Flat with _Holes on) discards its floor inside them, the runner's ground casts ignore
    // hits inside them (you fall through), the autopilot steers round them and no respawn lands next to one. A ring of
    // the last MAX holes : the oldest closes when a new one opens, by then it is far behind.
    public static class TrackHoles
    {
        public const int MAX = 24;
        private static readonly Vector4[] _holes = new Vector4[MAX];
        private static readonly int HOLES = Shader.PropertyToID("_HS_Holes");
        private static readonly int COUNT = Shader.PropertyToID("_HS_HoleCount");
        private static int _count;
        private static int _next;
        // A hot glowing lip round each hole : seen from far at any speed.
        private static readonly GameObject[] _rims = new GameObject[MAX];
        private static Mesh _ring;

        // On for the stage being raced (world 3 only).
        public static bool Enabled;

        public static int Count => _count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            for (int i = 0; i < MAX; i++)
            {
                _holes[i] = Vector4.zero;
                if (_rims[i] != null) _rims[i].SetActive(false);
            }
            _count = 0;
            _next = 0;
            Push();
        }

        public static void Punch(Vector3 pCenter, float pRadius) => Punch(pCenter, Vector3.up, pRadius);

        public static void Punch(Vector3 pCenter, Vector3 pNormal, float pRadius)
        {
            if (!Enabled || pRadius < 1f || OnLightRun(pCenter)) return;
            _holes[_next] = new Vector4(pCenter.x, pCenter.y, pCenter.z, pRadius);
            GameObject lRim = _rims[_next];
            if (lRim == null)
            {
                lRim = new GameObject("HoleRim");
                lRim.AddComponent<MeshFilter>().sharedMesh = Ring;
                MeshRenderer lRenderer = lRim.AddComponent<MeshRenderer>();
                lRenderer.sharedMaterial = Mats.Glow(new Color(1f, 0.42f, 0.08f) * 2.4f);
                lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _rims[_next] = lRim;
            }
            lRim.transform.SetPositionAndRotation(pCenter + pNormal * 0.8f, Quaternion.FromToRotation(Vector3.up, pNormal));
            lRim.transform.localScale = Vector3.one * pRadius;
            lRim.SetActive(true);
            _next = (_next + 1) % MAX;
            _count = Mathf.Min(_count + 1, MAX);
            Push();
        }

        // The pad line that charges the light (the 9 km before a light highway) and the highway itself stay whole : a
        // hole there breaks the chain and the light is lost for the whole stage.
        private static bool OnLightRun(Vector3 pPoint)
        {
            LevelLayout lLayout = RaceContext.Layout;
            PlayerController lPlayer = RaceContext.Player;
            if (lLayout == null || lLayout.profile == null || !lLayout.profile.LightEnabled || lLayout.highways.Count == 0) return false;
            float lLateral;
            float lS = lLayout.spine.Project(pPoint, lPlayer != null ? lPlayer.Progress : 0f, out lLateral);
            foreach (Vector2 lHighway in lLayout.highways)
                if (lS >= lHighway.x - 9000f && lS <= lHighway.y) return true;
            return false;
        }

        public static bool Inside(Vector3 pPoint, float pMargin = 0f)
        {
            Vector4 lHole;
            return Find(pPoint, pMargin, out lHole);
        }

        // The hole whose sphere (grown by pMargin) contains the point.
        public static bool Find(Vector3 pPoint, float pMargin, out Vector4 pHole)
        {
            for (int i = 0; i < _count; i++)
            {
                Vector4 lHole = _holes[i];
                float lRadius = lHole.w + pMargin;
                if (lHole.w > 0f && (pPoint - (Vector3)lHole).sqrMagnitude < lRadius * lRadius)
                {
                    pHole = lHole;
                    return true;
                }
            }
            pHole = Vector4.zero;
            return false;
        }

        // Flat ring from 0.97 to 1.22 of the radius.
        private static Mesh Ring
        {
            get
            {
                if (_ring != null) return _ring;
                const int SEGMENTS = 48;
                Vector3[] lVertices = new Vector3[SEGMENTS * 2];
                int[] lTriangles = new int[SEGMENTS * 6];
                for (int i = 0; i < SEGMENTS; i++)
                {
                    float lA = i * Mathf.PI * 2f / SEGMENTS;
                    Vector3 lDir = new Vector3(Mathf.Cos(lA), 0f, Mathf.Sin(lA));
                    lVertices[i * 2] = lDir * 0.97f;
                    lVertices[i * 2 + 1] = lDir * 1.22f;
                    int lNext = (i + 1) % SEGMENTS;
                    lTriangles[i * 6] = i * 2; lTriangles[i * 6 + 1] = lNext * 2; lTriangles[i * 6 + 2] = i * 2 + 1;
                    lTriangles[i * 6 + 3] = i * 2 + 1; lTriangles[i * 6 + 4] = lNext * 2; lTriangles[i * 6 + 5] = lNext * 2 + 1;
                }
                _ring = new Mesh { name = "HoleRim", vertices = lVertices, triangles = lTriangles };
                _ring.RecalculateNormals();
                _ring.RecalculateBounds();
                return _ring;
            }
        }

        private static void Push()
        {
            Shader.SetGlobalVectorArray(HOLES, _holes);
            Shader.SetGlobalFloat(COUNT, _count);
        }
    }
}
