using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Procedural UI polygons for the Persona-like look : slanted panels and jagged bursts.
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIShape : MaskableGraphic
    {
        public enum ShapeKind { Parallelogram, Burst, Triangle, Chevron }

        public ShapeKind kind = ShapeKind.Parallelogram;
        public float skew = 0.18f;
        public int spikes = 16;
        public float innerRatio = 0.62f;
        public int seed = 1;

        public void SetShape(ShapeKind pKind, float pSkew = 0.18f, int pSpikes = 16, float pInner = 0.62f, int pSeed = 1)
        {
            kind = pKind;
            skew = pSkew;
            spikes = pSpikes;
            innerRatio = pInner;
            seed = pSeed;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper pHelper)
        {
            pHelper.Clear();
            Rect lRect = GetPixelAdjustedRect();
            Color32 lColor = color;

            if (kind == ShapeKind.Parallelogram)
            {
                float lOffset = lRect.height * skew;
                pHelper.AddVert(new Vector3(lRect.xMin - lOffset, lRect.yMin), lColor, Vector2.zero);
                pHelper.AddVert(new Vector3(lRect.xMin + lOffset, lRect.yMax), lColor, Vector2.up);
                pHelper.AddVert(new Vector3(lRect.xMax + lOffset, lRect.yMax), lColor, Vector2.one);
                pHelper.AddVert(new Vector3(lRect.xMax - lOffset, lRect.yMin), lColor, Vector2.right);
                pHelper.AddTriangle(0, 1, 2);
                pHelper.AddTriangle(2, 3, 0);
            }
            else if (kind == ShapeKind.Chevron)
            {
                // Arrow-shaped card pointing right : notch on the left, point on the right.
                float lDepth = lRect.height * skew;
                pHelper.AddVert(new Vector3(lRect.xMin, lRect.yMin), lColor, Vector2.zero);
                pHelper.AddVert(new Vector3(lRect.xMin + lDepth, lRect.center.y), lColor, Vector2.zero);
                pHelper.AddVert(new Vector3(lRect.xMin, lRect.yMax), lColor, Vector2.zero);
                pHelper.AddVert(new Vector3(lRect.xMax - lDepth, lRect.yMax), lColor, Vector2.zero);
                pHelper.AddVert(new Vector3(lRect.xMax, lRect.center.y), lColor, Vector2.zero);
                pHelper.AddVert(new Vector3(lRect.xMax - lDepth, lRect.yMin), lColor, Vector2.zero);
                pHelper.AddTriangle(0, 1, 5);
                pHelper.AddTriangle(1, 4, 5);
                pHelper.AddTriangle(1, 2, 3);
                pHelper.AddTriangle(1, 3, 4);
            }
            else if (kind == ShapeKind.Triangle)
            {
                pHelper.AddVert(new Vector3(lRect.xMin, lRect.yMin), lColor, Vector2.zero);
                pHelper.AddVert(new Vector3(lRect.center.x, lRect.yMax), lColor, Vector2.up);
                pHelper.AddVert(new Vector3(lRect.xMax, lRect.yMin), lColor, Vector2.right);
                pHelper.AddTriangle(0, 1, 2);
            }
            else
            {
                System.Random lRandom = new System.Random(seed);
                Vector2 lCenter = lRect.center;
                pHelper.AddVert(lCenter, lColor, new Vector2(0.5f, 0.5f));
                int lCount = spikes * 2;
                for (int i = 0; i < lCount; i++)
                {
                    float lAngle = (i / (float)lCount) * Mathf.PI * 2f;
                    float lRadius = (i % 2 == 0) ? 1f - (float)lRandom.NextDouble() * 0.25f : innerRatio * (0.85f + (float)lRandom.NextDouble() * 0.3f);
                    Vector2 lPoint = lCenter + new Vector2(Mathf.Cos(lAngle) * lRect.width, Mathf.Sin(lAngle) * lRect.height) * 0.5f * lRadius;
                    pHelper.AddVert(lPoint, lColor, Vector2.zero);
                }
                for (int i = 0; i < lCount; i++)
                    pHelper.AddTriangle(0, 1 + i, 1 + (i + 1) % lCount);
            }
        }
    }
}
