using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Flat "road" drawn toward a vanishing point : converging rails and dashes rushing at the viewer.
    // Gives the 2D stage select its depth and its sense of speed.
    public class UIPerspectiveRoad : MaskableGraphic
    {
        public Vector2 vanishingPoint = new Vector2(0f, 240f);
        public float baseY = -560f;
        public float halfWidth = 1500f;
        public int lines = 9;
        public float speed = 1.6f;
        public Color dashColor = new Color(1f, 1f, 1f, 0.5f);

        private float _scroll;

        private void Update()
        {
            _scroll = Mathf.Repeat(_scroll + Time.unscaledDeltaTime * speed, 1f);
            SetVerticesDirty();
        }

        // Point of the road at lateral u (-1..1) and depth t (0 = viewer, 1 = horizon).
        private Vector2 RoadPoint(float pU, float pT)
        {
            Vector2 lNear = new Vector2(pU * halfWidth, baseY);
            return Vector2.Lerp(lNear, vanishingPoint, pT);
        }

        private static void AddQuad(VertexHelper pHelper, Vector2 pA, Vector2 pB, Vector2 pC, Vector2 pD, Color pColorNear, Color pColorFar)
        {
            int lIndex = pHelper.currentVertCount;
            pHelper.AddVert(pA, pColorNear, Vector2.zero);
            pHelper.AddVert(pB, pColorFar, Vector2.zero);
            pHelper.AddVert(pC, pColorFar, Vector2.zero);
            pHelper.AddVert(pD, pColorNear, Vector2.zero);
            pHelper.AddTriangle(lIndex, lIndex + 1, lIndex + 2);
            pHelper.AddTriangle(lIndex, lIndex + 2, lIndex + 3);
        }

        protected override void OnPopulateMesh(VertexHelper pHelper)
        {
            pHelper.Clear();
            Color lFar = new Color(color.r, color.g, color.b, 0f);
            for (int i = 0; i < lines; i++)
            {
                float lU = Mathf.Lerp(-1f, 1f, i / (lines - 1f));
                float lWidth = Mathf.Abs(lU) > 0.95f ? 10f : 3f;
                Vector2 lNear = RoadPoint(lU, 0f);
                Vector2 lHorizon = RoadPoint(lU, 1f);
                AddQuad(pHelper, lNear + Vector2.left * lWidth, lHorizon, lHorizon, lNear + Vector2.right * lWidth, color, lFar);
            }
            // Dashes coming at you : spacing grows with perspective (t^2 mapping).
            for (int k = 0; k < 9; k++)
            {
                float lT0 = Mathf.Repeat(k / 9f - _scroll, 1f);
                float lT1 = Mathf.Min(1f, lT0 + 0.045f);
                float lD0 = 1f - (1f - lT0) * (1f - lT0);
                float lD1 = 1f - (1f - lT1) * (1f - lT1);
                for (int lSide = -1; lSide <= 1; lSide += 2)
                {
                    float lU = lSide * 0.33f;
                    Vector2 lA = RoadPoint(lU, lD0);
                    Vector2 lB = RoadPoint(lU, lD1);
                    float lW0 = Mathf.Lerp(16f, 1f, lD0);
                    float lW1 = Mathf.Lerp(16f, 1f, lD1);
                    Color lColor = new Color(dashColor.r, dashColor.g, dashColor.b, dashColor.a * (1f - lD0));
                    AddQuad(pHelper, lA + Vector2.left * lW0, lB + Vector2.left * lW1, lB + Vector2.right * lW1, lA + Vector2.right * lW0, lColor, lColor);
                }
            }
        }
    }
}
