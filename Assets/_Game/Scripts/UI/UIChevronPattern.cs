using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Rows of big ">" chevrons in two reds, scrolling sideways (Boost Vector style backdrop).
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIChevronPattern : MaskableGraphic
    {
        public Color secondary = new Color(0.78f, 0.03f, 0.08f);
        public float cellWidth = 360f;
        public float cellHeight = 180f;
        public float speed = 60f;

        private float _offset;

        private void Update()
        {
            _offset = Mathf.Repeat(_offset + speed * Time.unscaledDeltaTime, cellWidth);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper pHelper)
        {
            pHelper.Clear();
            Rect lRect = GetPixelAdjustedRect();
            Color32 lBack = color;
            Color32 lFront = secondary;
            AddQuad(pHelper, new Vector2(lRect.xMin, lRect.yMin), new Vector2(lRect.xMin, lRect.yMax), new Vector2(lRect.xMax, lRect.yMax), new Vector2(lRect.xMax, lRect.yMin), lBack);

            float lHalf = cellHeight * 0.5f;
            float lThick = cellWidth * 0.32f;
            int lRow = 0;
            for (float lY = lRect.yMin - cellHeight; lY < lRect.yMax + cellHeight; lY += cellHeight, lRow++)
            {
                float lShift = (lRow % 2) * cellWidth * 0.5f;
                for (float lX = lRect.xMin - cellWidth * 2f + _offset + lShift; lX < lRect.xMax + cellWidth; lX += cellWidth)
                {
                    // ">" made of two parallelograms.
                    Vector2 lTip = new Vector2(lX + cellWidth * 0.55f, lY + lHalf);
                    AddQuad(pHelper, new Vector2(lX, lY + cellHeight), new Vector2(lX + lThick, lY + cellHeight), lTip + new Vector2(lThick, 0f), lTip, lFront);
                    AddQuad(pHelper, lTip, lTip + new Vector2(lThick, 0f), new Vector2(lX + lThick, lY), new Vector2(lX, lY), lFront);
                }
            }
        }

        private static void AddQuad(VertexHelper pHelper, Vector2 pA, Vector2 pB, Vector2 pC, Vector2 pD, Color32 pColor)
        {
            int lIndex = pHelper.currentVertCount;
            pHelper.AddVert(pA, pColor, Vector2.zero);
            pHelper.AddVert(pB, pColor, Vector2.zero);
            pHelper.AddVert(pC, pColor, Vector2.zero);
            pHelper.AddVert(pD, pColor, Vector2.zero);
            pHelper.AddTriangle(lIndex, lIndex + 1, lIndex + 2);
            pHelper.AddTriangle(lIndex + 2, lIndex + 3, lIndex);
        }
    }
}
