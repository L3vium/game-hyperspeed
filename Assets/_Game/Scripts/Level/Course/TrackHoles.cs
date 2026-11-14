using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Craters are real holes in the level (2026-11-14, on every stage) : what crashes on the track, and the craters the
    // level design places, punch a hole through it. A sphere per hole :
    // - the track shader (HS_Flat with _Holes on) discards everything inside it : you see straight through ;
    // - a short wall band inside the hole gives the cut its thickness. Nothing is drawn over the hole : no rim, no disc ;
    // - the track's collider loses its triangles inside it, so anything physical falls through, and the runner's own
    //   ground casts ignore hits inside it ;
    // - the autopilot steers round holes and no respawn lands next to one.
    // The shader takes the 24 holes nearest the camera ; the game keeps up to 128.
    public static class TrackHoles
    {
        public const int MAX = 128;
        public const int SHADER_MAX = 24;
        private static readonly Vector4[] _holes = new Vector4[MAX];
        private static readonly Vector4[] _shader = new Vector4[SHADER_MAX];
        private static readonly int HOLES = Shader.PropertyToID("_HS_Holes");
        private static readonly int COUNT = Shader.PropertyToID("_HS_HoleCount");
        private static int _count;
        private static int _next;
        private static float _lastPunch = -10f;
        private static readonly GameObject[] _walls = new GameObject[MAX];
        private static Mesh _band;
        private static Material _wallMaterial;
        private static HoleUpdater _updater;

        // On for the stage being raced.
        public static bool Enabled;

        public static int Count => _count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            for (int i = 0; i < MAX; i++)
            {
                _holes[i] = Vector4.zero;
                if (_walls[i] != null) _walls[i].SetActive(false);
            }
            _count = 0;
            _next = 0;
            _lastPunch = -10f;
            _wallMaterial = null;
            for (int i = 0; i < SHADER_MAX; i++) _shader[i] = Vector4.zero;
            Shader.SetGlobalVectorArray(HOLES, _shader);
            Shader.SetGlobalFloat(COUNT, 0f);
        }

        public static void Punch(Vector3 pCenter, float pRadius) => Punch(pCenter, Vector3.up, pRadius);

        // pDesigned : a crater placed by the level design (no fairness check, the stage was built with it).
        public static void Punch(Vector3 pCenter, Vector3 pNormal, float pRadius, bool pDesigned = false)
        {
            if (!Enabled || pRadius < 1f) return;
            // Crashes punch at most one hole every few seconds on the first stages, one a second on the last ones : the
            // level is dug, not shredded.
            float lDifficulty = RaceContext.Layout != null && RaceContext.Layout.profile != null ? RaceContext.Layout.profile.difficulty : 1f;
            if (!pDesigned && (Time.time - _lastPunch < Mathf.Lerp(4f, 1f, lDifficulty) || !Fair(pCenter, pRadius))) return;
            if (!pDesigned) _lastPunch = Time.time;
            _holes[_next] = new Vector4(pCenter.x, pCenter.y, pCenter.z, pRadius);
            Wall(_next, pCenter, pNormal, pRadius);
            _next = (_next + 1) % MAX;
            _count = Mathf.Min(_count + 1, MAX);
            CutColliders(pCenter, pRadius);
            EnsureUpdater();
        }

        // A hole never opens right under the runner : it always has the time to see it and steer round or jump.
        private static bool Fair(Vector3 pCenter, float pRadius)
        {
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null) return true;
            float lReach = pRadius + Mathf.Max(30f, lPlayer.Speed * 0.4f);
            return (lPlayer.transform.position - pCenter).sqrMagnitude > lReach * lReach;
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

        // ---------------------------------------------------------------- the wall inside the cut

        private static void Wall(int pIndex, Vector3 pCenter, Vector3 pNormal, float pRadius)
        {
            GameObject lWall = _walls[pIndex];
            if (lWall == null)
            {
                lWall = new GameObject("HoleWall");
                lWall.AddComponent<MeshFilter>().sharedMesh = Band;
                MeshRenderer lRenderer = lWall.AddComponent<MeshRenderer>();
                lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _walls[pIndex] = lWall;
            }
            lWall.GetComponent<MeshRenderer>().sharedMaterial = WallMaterial;
            float lDepth = Mathf.Max(30f, pRadius * 0.9f);
            lWall.transform.SetPositionAndRotation(pCenter + pNormal * 0.2f, Quaternion.FromToRotation(Vector3.up, pNormal));
            // Just inside the cut, so its top edge never shows over the floor.
            lWall.transform.localScale = new Vector3(pRadius * 0.995f, lDepth, pRadius * 0.995f);
            lWall.SetActive(true);
        }

        private static Material WallMaterial
        {
            get
            {
                if (_wallMaterial != null) return _wallMaterial;
                Theme lTheme = RaceContext.Theme;
                Color lSide = lTheme != null ? Color.Lerp(lTheme.roadSide, Color.black, 0.45f) : new Color(0.25f, 0.2f, 0.2f);
                Material lMaterial = Mats.Flat(lTheme != null ? lTheme.shade : Color.gray, false, lSide);
                if (lTheme != null) _wallMaterial = lMaterial;
                return lMaterial;
            }
        }

        // A unit band from y = 0 down to y = -1, its faces turned inward (seen only from inside the hole), layered.
        private static Mesh Band
        {
            get
            {
                if (_band != null) return _band;
                MeshData lData = new MeshData();
                const int SIDES = 40;
                Color lTop = new Color(0.55f, 0.5f, 0.5f);
                Color lDeep = new Color(0.12f, 0.1f, 0.12f);
                for (int i = 0; i < SIDES; i++)
                {
                    float lA0 = i * Mathf.PI * 2f / SIDES;
                    float lA1 = (i + 1) * Mathf.PI * 2f / SIDES;
                    Vector3 lD0 = new Vector3(Mathf.Cos(lA0), 0f, Mathf.Sin(lA0));
                    Vector3 lD1 = new Vector3(Mathf.Cos(lA1), 0f, Mathf.Sin(lA1));
                    // Inward faces : wound so they face the hole's axis.
                    lData.Quad(lD1, lD0, lD0 + Vector3.down * 0.35f, lD1 + Vector3.down * 0.35f, -(lD0 + lD1).normalized, lTop);
                    lData.Quad(lD1 + Vector3.down * 0.35f, lD0 + Vector3.down * 0.35f, lD0 + Vector3.down, lD1 + Vector3.down, -(lD0 + lD1).normalized, lDeep);
                }
                _band = lData.ToMesh("HoleWall");
                return _band;
            }
        }

        // ---------------------------------------------------------------- collision : the triangles inside go

        private static void CutColliders(Vector3 pCenter, float pRadius)
        {
            Bounds lHole = new Bounds(pCenter, Vector3.one * pRadius * 2f);
            foreach (TrackChunk lChunk in TrackBaker.Chunks)
            {
                if (lChunk.collider == null || lChunk.collider.sharedMesh == null) continue;
                if (!lChunk.collider.bounds.Intersects(lHole)) continue;
                Cut(lChunk.collider, pCenter, pRadius);
            }
        }

        private static readonly List<Vector3> _vertices = new List<Vector3>();
        private static readonly List<int> _indices = new List<int>();

        private static void Cut(MeshCollider pCollider, Vector3 pCenter, float pRadius)
        {
            Mesh lMesh = pCollider.sharedMesh;
            Transform lTransform = pCollider.transform;
            Vector3 lLocalCenter = lTransform.InverseTransformPoint(pCenter);
            _vertices.Clear();
            _indices.Clear();
            lMesh.GetVertices(_vertices);
            lMesh.GetTriangles(_indices, 0);
            float lRadiusSqr = pRadius * pRadius;
            // Triangles touching the hole are split until they are small next to it, then the ones inside are dropped.
            float lFine = Mathf.Max(2f, pRadius * 0.22f);
            List<int> lKept = new List<int>(_indices.Count);
            Stack<int> lWork = new Stack<int>();
            bool lChanged = false;
            for (int i = 0; i < _indices.Count; i += 3)
            {
                lWork.Push(_indices[i]); lWork.Push(_indices[i + 1]); lWork.Push(_indices[i + 2]);
                while (lWork.Count > 0)
                {
                    int lC = lWork.Pop(), lB = lWork.Pop(), lA = lWork.Pop();
                    Vector3 lPA = _vertices[lA], lPB = _vertices[lB], lPC = _vertices[lC];
                    if (!TouchesSphere(lPA, lPB, lPC, lLocalCenter, pRadius))
                    {
                        lKept.Add(lA); lKept.Add(lB); lKept.Add(lC);
                        continue;
                    }
                    lChanged = true;
                    float lAB = (lPB - lPA).sqrMagnitude, lBC = (lPC - lPB).sqrMagnitude, lCA = (lPA - lPC).sqrMagnitude;
                    float lLongest = Mathf.Max(lAB, Mathf.Max(lBC, lCA));
                    if (lLongest > lFine * lFine && _vertices.Count < 60000)
                    {
                        // Split the longest edge in two.
                        if (lLongest == lAB) { int lM = Mid(lPA, lPB); lWork.Push(lA); lWork.Push(lM); lWork.Push(lC); lWork.Push(lM); lWork.Push(lB); lWork.Push(lC); }
                        else if (lLongest == lBC) { int lM = Mid(lPB, lPC); lWork.Push(lA); lWork.Push(lB); lWork.Push(lM); lWork.Push(lA); lWork.Push(lM); lWork.Push(lC); }
                        else { int lM = Mid(lPC, lPA); lWork.Push(lA); lWork.Push(lB); lWork.Push(lM); lWork.Push(lM); lWork.Push(lB); lWork.Push(lC); }
                        continue;
                    }
                    Vector3 lCentroid = (lPA + lPB + lPC) / 3f;
                    if ((lCentroid - lLocalCenter).sqrMagnitude < lRadiusSqr) continue;
                    lKept.Add(lA); lKept.Add(lB); lKept.Add(lC);
                }
            }
            if (!lChanged) return;
            Mesh lCut = new Mesh { name = lMesh.name };
            lCut.indexFormat = _vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            lCut.SetVertices(_vertices);
            lCut.SetTriangles(lKept, 0);
            pCollider.sharedMesh = lCut;
        }

        private static int Mid(Vector3 pA, Vector3 pB)
        {
            _vertices.Add((pA + pB) * 0.5f);
            return _vertices.Count - 1;
        }

        private static bool TouchesSphere(Vector3 pA, Vector3 pB, Vector3 pC, Vector3 pCenter, float pRadius)
        {
            Vector3 lMin = Vector3.Min(pA, Vector3.Min(pB, pC)) - Vector3.one * pRadius;
            Vector3 lMax = Vector3.Max(pA, Vector3.Max(pB, pC)) + Vector3.one * pRadius;
            return pCenter.x >= lMin.x && pCenter.x <= lMax.x && pCenter.y >= lMin.y && pCenter.y <= lMax.y && pCenter.z >= lMin.z && pCenter.z <= lMax.z;
        }

        // ---------------------------------------------------------------- the shader gets the nearest holes

        private static void EnsureUpdater()
        {
            if (_updater != null) return;
            _updater = new GameObject("TrackHoles").AddComponent<HoleUpdater>();
        }

        private static readonly float[] _distances = new float[MAX];
        private static readonly int[] _order = new int[MAX];

        public static void PushNearest(Vector3 pEye)
        {
            int lUsed = 0;
            for (int i = 0; i < _count; i++)
            {
                if (_holes[i].w <= 0f) continue;
                _order[lUsed] = i;
                _distances[lUsed] = ((Vector3)_holes[i] - pEye).sqrMagnitude;
                lUsed++;
            }
            int lTake = Mathf.Min(lUsed, SHADER_MAX);
            // Partial selection : the lTake nearest first.
            for (int k = 0; k < lTake; k++)
            {
                int lBest = k;
                for (int j = k + 1; j < lUsed; j++) if (_distances[j] < _distances[lBest]) lBest = j;
                float lD = _distances[k]; _distances[k] = _distances[lBest]; _distances[lBest] = lD;
                int lO = _order[k]; _order[k] = _order[lBest]; _order[lBest] = lO;
                _shader[k] = _holes[_order[k]];
            }
            for (int k = lTake; k < SHADER_MAX; k++) _shader[k] = Vector4.zero;
            Shader.SetGlobalVectorArray(HOLES, _shader);
            Shader.SetGlobalFloat(COUNT, lTake);
        }

        private class HoleUpdater : MonoBehaviour
        {
            private void LateUpdate()
            {
                Camera lCamera = CameraRig.Instance != null ? CameraRig.Instance.Cam : Camera.main;
                if (lCamera != null) PushNearest(lCamera.transform.position);
            }

            private void OnDestroy()
            {
                if (_updater == this) _updater = null;
            }
        }
    }
}
