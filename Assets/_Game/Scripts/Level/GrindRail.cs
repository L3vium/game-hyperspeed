using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Polyline rail the player magnetizes to and surfs along.
    public class GrindRail : MonoBehaviour
    {
        public static readonly List<GrindRail> All = new List<GrindRail>();

        private const int TUBE_SIDES = 6;

        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<Vector3> _ups = new List<Vector3>();
        private readonly List<float> _distances = new List<float>();
        private readonly List<float> _spineS = new List<float>();
        private Bounds _bounds;
        public Bounds Area => _bounds;

        public float Length { get; private set; }
        public float catchRadius = 20f;
        // A side loop rope (2026-11-12) : the view turns with the runner around it.
        public bool loop;

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        public void AddPoint(Vector3 pPoint, Vector3 pUp, float pSpineS = 0f)
        {
            if (_points.Count > 0) Length += Vector3.Distance(_points[_points.Count - 1], pPoint);
            _points.Add(pPoint);
            _ups.Add(pUp);
            _distances.Add(Length);
            _spineS.Add(pSpineS);
        }

        public float StartS => _spineS.Count > 0 ? _spineS[0] : 0f;

        public float EndS => _spineS.Count > 0 ? _spineS[_spineS.Count - 1] : 0f;

        // Spine distance under a rail distance (progress while grinding).
        public float SpineS(float pDistance)
        {
            pDistance = Mathf.Clamp(pDistance, 0f, Length);
            int lIndex = 1;
            while (lIndex < _distances.Count - 1 && _distances[lIndex] < pDistance) lIndex++;
            float lT = Mathf.InverseLerp(_distances[lIndex - 1], _distances[lIndex], pDistance);
            return Mathf.Lerp(_spineS[lIndex - 1], _spineS[lIndex], lT);
        }

        private void ComputeBounds()
        {
            _bounds = new Bounds(_points[0], Vector3.zero);
            foreach (Vector3 lPoint in _points) _bounds.Encapsulate(lPoint);
            _bounds.Expand(catchRadius * 2f + 8f);
        }

        // Electric rope : thick glowing tube, big enough to read from far away.
        public void Finish(Color pColor, float pRadius)
        {
            ComputeBounds();
            if (_points.Count < 2) return;
            Material lMaterial = Mats.GlowStripes(pColor * 2.6f, 0.08f, 9f);
            lMaterial.SetFloat("_Cull", 2f);
            BuildTube(pRadius, lMaterial);
            ElectricArcs.Attach(this, pColor);
        }

        private void BuildTube(float pRadius, Material pMaterial)
        {
            List<Vector3> lVertices = new List<Vector3>();
            List<Vector3> lNormals = new List<Vector3>();
            List<Vector2> lUvs = new List<Vector2>();
            List<int> lTriangles = new List<int>();
            for (int i = 0; i < _points.Count; i++)
            {
                Vector3 lTangent = (i < _points.Count - 1 ? _points[i + 1] - _points[i] : _points[i] - _points[i - 1]).normalized;
                Vector3 lUp = _ups[i];
                Vector3 lRight = Vector3.Cross(lUp, lTangent).normalized;
                for (int j = 0; j <= TUBE_SIDES; j++)
                {
                    float lAngle = j * Mathf.PI * 2f / TUBE_SIDES;
                    Vector3 lNormal = lRight * Mathf.Cos(lAngle) + lUp * Mathf.Sin(lAngle);
                    lVertices.Add(transform.InverseTransformPoint(_points[i] + lNormal * pRadius));
                    lNormals.Add(transform.InverseTransformDirection(lNormal));
                    lUvs.Add(new Vector2(_distances[i] * 0.25f, j / (float)TUBE_SIDES));
                }
                if (i == 0) continue;
                int lRow = (i - 1) * (TUBE_SIDES + 1);
                int lNext = i * (TUBE_SIDES + 1);
                for (int j = 0; j < TUBE_SIDES; j++)
                {
                    lTriangles.Add(lRow + j); lTriangles.Add(lNext + j); lTriangles.Add(lNext + j + 1);
                    lTriangles.Add(lRow + j); lTriangles.Add(lNext + j + 1); lTriangles.Add(lRow + j + 1);
                }
            }
            Mesh lMesh = new Mesh();
            lMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            lMesh.SetVertices(lVertices);
            lMesh.SetNormals(lNormals);
            lMesh.SetUVs(0, lUvs);
            lMesh.SetTriangles(lTriangles, 0);
            lMesh.RecalculateBounds();
            GameObject lTube = new GameObject("Tube");
            lTube.transform.SetParent(transform, false);
            lTube.AddComponent<MeshFilter>().sharedMesh = lMesh;
            lTube.AddComponent<MeshRenderer>().sharedMaterial = pMaterial;
        }

        public void Evaluate(float pDistance, out Vector3 pPosition, out Vector3 pTangent, out Vector3 pUp)
        {
            pDistance = Mathf.Clamp(pDistance, 0f, Length);
            int lIndex = 1;
            while (lIndex < _distances.Count - 1 && _distances[lIndex] < pDistance) lIndex++;
            float lT = Mathf.InverseLerp(_distances[lIndex - 1], _distances[lIndex], pDistance);
            pPosition = Vector3.Lerp(_points[lIndex - 1], _points[lIndex], lT);
            pTangent = (_points[lIndex] - _points[lIndex - 1]).normalized;
            pUp = Vector3.Slerp(_ups[lIndex - 1], _ups[lIndex], lT).normalized;
        }

        public bool FindNearest(Vector3 pPosition, float pRadius, out float pDistance, out float pRailDistance)
        {
            pDistance = float.MaxValue;
            pRailDistance = 0f;
            if (!_bounds.Contains(pPosition)) return false;
            for (int i = 1; i < _points.Count; i++)
            {
                Vector3 lA = _points[i - 1];
                Vector3 lSegment = _points[i] - lA;
                float lLength = lSegment.magnitude;
                float lT = Mathf.Clamp(Vector3.Dot(pPosition - lA, lSegment) / (lLength * lLength), 0f, 1f);
                float lDist = Vector3.Distance(pPosition, lA + lSegment * lT);
                if (lDist < pDistance)
                {
                    pDistance = lDist;
                    pRailDistance = _distances[i - 1] + lLength * lT;
                }
            }
            return pDistance <= pRadius;
        }
    }
}
