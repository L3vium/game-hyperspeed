using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Boost-Vector-like menu kit : flat reds, white slanted tiles, thin outlined giant words, chevrons, checkers.
    public static class UIKit
    {
        public static readonly Color RED = new Color(0.86f, 0.05f, 0.1f);
        public static readonly Color RED_DARK = new Color(0.62f, 0.02f, 0.07f);
        public static readonly Color RED_DEEP = new Color(0.36f, 0.01f, 0.05f);
        public static readonly Color INK = new Color(0.07f, 0.05f, 0.08f);
        public static readonly Color WHITE = Color.white;
        public static readonly Color ORANGE = new Color(1f, 0.6f, 0.09f);
        public static readonly Color GREY = new Color(0.62f, 0.6f, 0.62f);
        public static readonly Color CYAN = new Color(0.25f, 0.95f, 1f);
        public static readonly Color GOLD = new Color(1f, 0.82f, 0.2f);

        public enum Font { Heavy, Bold, Thin }

        public static TMP_FontAsset GetFont(Font pFont)
        {
            switch (pFont)
            {
                case Font.Heavy: return GameAssets.TitleFont;
                case Font.Thin: return GameAssets.ThinFont;
                default: return GameAssets.BodyFont;
            }
        }

        public static TextMeshProUGUI Text(Transform pParent, string pText, float pSize, Color pColor, Vector2 pAnchor, Vector2 pPosition, Vector2 pRect,
                                           Font pFont = Font.Bold, TextAlignmentOptions pAlign = TextAlignmentOptions.Center, bool pItalic = true, float pRotation = 0f)
        {
            TextMeshProUGUI lText = UIFactory.CreateText(pParent, pText, pSize, pColor, pAnchor, pPosition, pRect, false, pAlign);
            lText.font = GetFont(pFont);
            lText.fontStyle = pItalic ? FontStyles.Italic : FontStyles.Normal;
            lText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, pRotation);
            return lText;
        }

        // Hollow word : transparent face, thin colored outline (giant background titles).
        public static TextMeshProUGUI OutlineText(Transform pParent, string pText, float pSize, Color pColor, Vector2 pAnchor, Vector2 pPosition, Vector2 pRect,
                                                  float pRotation = 0f, float pWidth = 0.12f, TextAlignmentOptions pAlign = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI lText = Text(pParent, pText, pSize, pColor, pAnchor, pPosition, pRect, Font.Heavy, pAlign, true, pRotation);
            lText.fontMaterial.EnableKeyword("OUTLINE_ON");
            lText.faceColor = new Color32(255, 255, 255, 0);
            lText.outlineColor = pColor;
            lText.outlineWidth = pWidth;
            lText.color = Color.white;
            return lText;
        }

        public static UIShape Tile(Transform pParent, string pName, Color pColor, Vector2 pAnchor, Vector2 pPosition, Vector2 pSize, float pSkew = 0.35f, float pRotation = 0f)
        {
            UIShape lShape = UIFactory.CreateShape(pParent, pName, UIShape.ShapeKind.Parallelogram, pColor, pAnchor, pPosition, pSize, pRotation);
            lShape.skew = pSkew;
            return lShape;
        }

        public static RawImage Icon(Transform pParent, string pName, Color pColor, Vector2 pAnchor, Vector2 pPosition, float pSize)
        {
            RectTransform lRect = UIFactory.CreateRect(pParent, "Icon_" + pName, pAnchor, pPosition, new Vector2(pSize, pSize));
            lRect.gameObject.AddComponent<CanvasRenderer>();
            RawImage lImage = lRect.gameObject.AddComponent<RawImage>();
            lImage.texture = FindIcon(pName);
            lImage.color = pColor;
            lImage.raycastTarget = false;
            return lImage;
        }

        public static Texture2D FindIcon(string pName)
        {
            Texture2D[] lIcons = GameAssets.Instance.icons;
            if (lIcons == null) return null;
            foreach (Texture2D lIcon in lIcons)
                if (lIcon != null && lIcon.name == "icon_" + pName) return lIcon;
            return null;
        }

        public static UIChevronPattern Chevrons(Transform pParent, string pName, Color pBack, Color pFront, float pSpeed = 50f)
        {
            RectTransform lRect = UIFactory.Stretch(UIFactory.CreateRect(pParent, pName, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            UIChevronPattern lPattern = lRect.gameObject.AddComponent<UIChevronPattern>();
            lPattern.color = pBack;
            lPattern.secondary = pFront;
            lPattern.speed = pSpeed;
            lPattern.raycastTarget = false;
            return lPattern;
        }

        public static UICheckerStrip Checker(Transform pParent, string pName, Vector2 pAnchor, Vector2 pPosition, Vector2 pSize, Color pA, Color pB, float pSpeed = 0f)
        {
            RectTransform lRect = UIFactory.CreateRect(pParent, pName, pAnchor, pPosition, pSize);
            UICheckerStrip lStrip = lRect.gameObject.AddComponent<UICheckerStrip>();
            lStrip.color = pA;
            lStrip.secondary = pB;
            lStrip.speed = pSpeed;
            lStrip.raycastTarget = false;
            return lStrip;
        }

        // Shape used as a stencil mask with a texture inside (card thumbnails).
        public static RawImage MaskedImage(UIShape pShape, Texture pTexture)
        {
            Mask lMask = pShape.gameObject.AddComponent<Mask>();
            lMask.showMaskGraphic = true;
            RectTransform lRect = UIFactory.Stretch(UIFactory.CreateRect(pShape.transform, "Image", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            lRect.offsetMin = new Vector2(-pShape.rectTransform.rect.height * pShape.skew, 0f);
            lRect.offsetMax = new Vector2(pShape.rectTransform.rect.height * pShape.skew, 0f);
            lRect.gameObject.AddComponent<CanvasRenderer>();
            RawImage lImage = lRect.gameObject.AddComponent<RawImage>();
            lImage.texture = pTexture;
            lImage.raycastTarget = false;
            return lImage;
        }

        public static Image FullScreen(Transform pParent, string pName, Color pColor)
        {
            Image lImage = UIFactory.CreateImage(pParent, pName, pColor, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UIFactory.Stretch(lImage.rectTransform);
            return lImage;
        }
    }
}
