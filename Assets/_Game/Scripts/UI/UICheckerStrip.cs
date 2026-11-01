using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Slanted racing checker band (two rows), optionally scrolling.
    [RequireComponent(typeof(CanvasRenderer))]
    public class UICheckerStrip : MaskableGraphic
    {
        public Color secondary = new Color(0.55f, 0f, 0.05f);
        public float cell = 46f;
        public float slant = 0.6f;
        public float speed = 0f;

        private float _offset;

        private void Update()
        {
            if (speed == 0f) return;
            _offset = Mathf.Repeat(_offset + speed * Time.unscaledDeltaTime, cell * 2f);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper pHelper)
        {
            pHelper.Clear();
            Rect lRect = GetPixelAdjustedRect();
            float lRowHeight = lRect.height * 0.5f;
            for (int lRow = 0; lRow < 2; lRow++)
            {
                float lY0 = lRect.yMin + lRow * lRowHeight;
                float lY1 = lY0 + lRowHeight;
                int lIndex = 0;
                for (float lX = lRect.xMin - cell * 2f + _offset; lX < lRect.xMax; lX += cell, lIndex++)
                {
                    bool lLight = (lIndex + lRow) % 2 == 0;
                    Color32 lColor = lLight ? color : secondary;
                    float lSkew = lRowHeight * slant;
                    int lV = pHelper.currentVertCount;
                    pHelper.AddVert(new Vector2(Mathf.Max(lX, lRect.xMin), lY0), lColor, Vector2.zero);
                    pHelper.AddVert(new Vector2(Mathf.Max(lX + lSkew, lRect.xMin), lY1), lColor, Vector2.zero);
                    pHelper.AddVert(new Vector2(Mathf.Min(lX + cell + lSkew, lRect.xMax), lY1), lColor, Vector2.zero);
                    pHelper.AddVert(new Vector2(Mathf.Min(lX + cell, lRect.xMax), lY0), lColor, Vector2.zero);
                    pHelper.AddTriangle(lV, lV + 1, lV + 2);
                    pHelper.AddTriangle(lV + 2, lV + 3, lV);
                }
            }
        }
    }
}
