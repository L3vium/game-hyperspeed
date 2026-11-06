using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // What light speed leaves behind : a trail that stays in the world for many seconds, its strands slowly coiling around
    // the path you took and widening as they fade.
    public class LightTrail : MonoBehaviour
    {
        private const float LIFE = 10f;
        private const float EVERY = 0.05f;
        private const int MAX_POINTS = (int)(LIFE / EVERY);
        private const int STRANDS = 3;

        private struct Point
        {
            public Vector3 position;
            public Vector3 right;
            public Vector3 up;
            public float time;
        }

        private readonly List<Point> _points = new List<Point>(MAX_POINTS);
        private LineRenderer[] _lines;
        private Vector3[] _buffer;
        private float _timer;

        public static LightTrail Create()
        {
            LightTrail lTrail = new GameObject("LightTrail").AddComponent<LightTrail>();
            lTrail.Build();
            return lTrail;
        }

        private void Build()
        {
            _lines = new LineRenderer[STRANDS];
            _buffer = new Vector3[MAX_POINTS];
            Color[] lColors = { new Color(3f, 3.4f, 4f), new Color(0.6f, 1.6f, 4f), new Color(2.8f, 0.8f, 3.6f) };
            for (int i = 0; i < STRANDS; i++)
            {
                LineRenderer lLine = new GameObject("Strand" + i).AddComponent<LineRenderer>();
                lLine.transform.SetParent(transform, false);
                lLine.useWorldSpace = true;
                lLine.positionCount = 0;
                lLine.sharedMaterial = Mats.Glow(lColors[i], true);
                lLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lLine.widthCurve = new AnimationCurve(new Keyframe(0f, i == 0 ? 1.6f : 0.9f), new Keyframe(0.15f, i == 0 ? 2.8f : 1.5f), new Keyframe(1f, 0.1f));
                Gradient lFade = new Gradient();
                lFade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                              new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.55f, 0.5f), new GradientAlphaKey(0f, 1f) });
                lLine.colorGradient = lFade;
                _lines[i] = lLine;
            }
        }

        // Called every frame by the visual while the runner is light.
        public void Add(Vector3 pPosition, Vector3 pRight, Vector3 pUp)
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = EVERY;
            if (_points.Count >= MAX_POINTS) _points.RemoveAt(0);
            _points.Add(new Point { position = pPosition, right = pRight, up = pUp, time = Time.time });
        }

        public void Clear()
        {
            _points.Clear();
            foreach (LineRenderer lLine in _lines) lLine.positionCount = 0;
        }

        private void LateUpdate()
        {
            float lNow = Time.time;
            int lDrop = 0;
            while (lDrop < _points.Count && lNow - _points[lDrop].time > LIFE) lDrop++;
            if (lDrop > 0) _points.RemoveRange(0, lDrop);
            int lCount = _points.Count;
            for (int s = 0; s < STRANDS; s++)
            {
                _lines[s].positionCount = lCount;
                if (lCount == 0) continue;
                float lOffset = s * Mathf.PI * 2f / STRANDS;
                // Newest first, so the width and fade curves run from the runner to the far end.
                for (int i = 0; i < lCount; i++)
                {
                    Point lPoint = _points[lCount - 1 - i];
                    float lAge = lNow - lPoint.time;
                    float lRadius = (s == 0 ? 0.4f : 1.4f) + lAge * lAge * 0.45f;
                    float lAngle = lOffset + lPoint.time * 5.5f + lAge * 0.9f;
                    _buffer[i] = lPoint.position + lPoint.right * Mathf.Cos(lAngle) * lRadius + lPoint.up * (Mathf.Sin(lAngle) * lRadius + 1f);
                }
                _lines[s].SetPositions(_buffer);
            }
        }
    }
}
