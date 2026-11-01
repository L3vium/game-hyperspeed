using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Builds uGUI hierarchies from code with the game palette.
    public static class UIFactory
    {
        public static readonly Color RED = new Color(0.92f, 0.03f, 0.1f);
        public static readonly Color DARK_RED = new Color(0.45f, 0f, 0.05f);
        public static readonly Color BLACK = new Color(0.04f, 0.02f, 0.05f);
        public static readonly Color WHITE = Color.white;
        public static readonly Color CYAN = new Color(0.2f, 0.95f, 1f);
        public static readonly Color YELLOW = new Color(1f, 0.88f, 0.1f);

        public static Canvas CreateCanvas(string pName, int pSortingOrder, Transform pParent = null)
        {
            GameObject lObject = new GameObject(pName, typeof(RectTransform));
            if (pParent != null) lObject.transform.SetParent(pParent, false);
            Canvas lCanvas = lObject.AddComponent<Canvas>();
            lCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            lCanvas.sortingOrder = pSortingOrder;
            CanvasScaler lScaler = lObject.AddComponent<CanvasScaler>();
            lScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            lScaler.referenceResolution = new Vector2(1920f, 1080f);
            lScaler.matchWidthOrHeight = 0.5f;
            return lCanvas;
        }

        public static RectTransform CreateRect(Transform pParent, string pName, Vector2 pAnchor, Vector2 pPosition, Vector2 pSize)
        {
            GameObject lObject = new GameObject(pName, typeof(RectTransform));
            RectTransform lRect = (RectTransform)lObject.transform;
            lRect.SetParent(pParent, false);
            lRect.anchorMin = pAnchor;
            lRect.anchorMax = pAnchor;
            lRect.pivot = new Vector2(0.5f, 0.5f);
            lRect.anchoredPosition = pPosition;
            lRect.sizeDelta = pSize;
            return lRect;
        }

        public static RectTransform Stretch(RectTransform pRect)
        {
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;
            return pRect;
        }

        public static TextMeshProUGUI CreateText(Transform pParent, string pText, float pSize, Color pColor, Vector2 pAnchor, Vector2 pPosition, Vector2 pRectSize, bool pTitleFont = false, TextAlignmentOptions pAlign = TextAlignmentOptions.Center)
        {
            RectTransform lRect = CreateRect(pParent, "Text_" + pText, pAnchor, pPosition, pRectSize);
            TextMeshProUGUI lText = lRect.gameObject.AddComponent<TextMeshProUGUI>();
            lText.font = pTitleFont ? GameAssets.TitleFont : GameAssets.BodyFont;
            lText.text = pText;
            lText.fontSize = pSize;
            lText.color = pColor;
            lText.alignment = pAlign;
            lText.textWrappingMode = TextWrappingModes.NoWrap;
            lText.overflowMode = TextOverflowModes.Overflow;
            lText.raycastTarget = false;
            return lText;
        }

        public static UIShape CreateShape(Transform pParent, string pName, UIShape.ShapeKind pKind, Color pColor, Vector2 pAnchor, Vector2 pPosition, Vector2 pSize, float pRotation = 0f)
        {
            RectTransform lRect = CreateRect(pParent, pName, pAnchor, pPosition, pSize);
            lRect.localRotation = Quaternion.Euler(0f, 0f, pRotation);
            UIShape lShape = lRect.gameObject.AddComponent<UIShape>();
            lShape.kind = pKind;
            lShape.color = pColor;
            lShape.raycastTarget = false;
            return lShape;
        }

        public static Image CreateImage(Transform pParent, string pName, Color pColor, Vector2 pAnchor, Vector2 pPosition, Vector2 pSize)
        {
            RectTransform lRect = CreateRect(pParent, pName, pAnchor, pPosition, pSize);
            lRect.gameObject.AddComponent<CanvasRenderer>();
            Image lImage = lRect.gameObject.AddComponent<Image>();
            lImage.color = pColor;
            lImage.raycastTarget = false;
            return lImage;
        }

        public static void Outline(TextMeshProUGUI pText, Color pColor, float pWidth = 0.25f)
        {
            pText.fontMaterial.EnableKeyword("OUTLINE_ON");
            pText.outlineColor = pColor;
            pText.outlineWidth = pWidth;
        }
    }
}
