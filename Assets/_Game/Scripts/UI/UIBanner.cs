using TMPro;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Anime callout : a slanted slab with a word in real 3D that slams in, holds, then shoots off to the side.
    // Used for the big notifications at the top (PERFECT START, HYPERSPEED) and for the hints at the bottom.
    public class UIBanner : MonoBehaviour
    {
        private RectTransform _rect;
        private RectTransform _slab;
        private UIShape _slabShape;
        private UIShape _stripe;
        private UI3DText _word;
        private Vector2 _home;
        private float _timer;
        private float _hold;
        private float _age;

        public static UIBanner Create(Transform pParent, string pName, Vector2 pAnchor, Vector2 pPosition, Vector2 pSize, float pFont)
        {
            RectTransform lRect = UIFactory.CreateRect(pParent, pName, pAnchor, pPosition, pSize);
            UIBanner lBanner = lRect.gameObject.AddComponent<UIBanner>();
            lBanner._rect = lRect;
            lBanner._home = pPosition;
            lBanner._slab = UIFactory.CreateRect(lRect, "Slab", new Vector2(0.5f, 0.5f), Vector2.zero, pSize);
            lBanner._slabShape = UIKit.Tile(lBanner._slab, "Back", UIKit.INK, new Vector2(0.5f, 0.5f), Vector2.zero, pSize, 0.35f);
            lBanner._stripe = UIKit.Tile(lBanner._slab, "Stripe", UIKit.GOLD, new Vector2(0.5f, 0.5f), new Vector2(0f, -pSize.y * 0.5f - 8f), new Vector2(pSize.x * 0.92f, 12f), 0.35f);
            lBanner._word = UI3DText.Create(lRect, " ", pFont, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(pSize.x * 0.95f, pSize.y * 1.3f), UIKit.WHITE, UIKit.RED_DARK, 16, 4f);
            lBanner._word.tiltX = 8f;
            lBanner._word.tiltY = 6f;
            lRect.gameObject.SetActive(false);
            return lBanner;
        }

        public void Show(string pText, Color pColor, float pHold = 1.4f)
        {
            gameObject.SetActive(true);
            _word.SetText(pText);
            _word.SetFace(Color.white);
            _word.SetSide(Color.Lerp(pColor, Color.black, 0.35f));
            _word.SetAlpha(1f);
            _word.Slam(2.4f, 55f);
            _slabShape.color = new Color(0.05f, 0.03f, 0.08f, 0.82f);
            _stripe.color = pColor;
            _hold = pHold;
            _timer = pHold + 0.35f;
            _age = 0f;
            _rect.anchoredPosition = _home;
            _slab.localScale = new Vector3(0.2f, 1f, 1f);
        }

        private void Update()
        {
            float lDt = Time.unscaledDeltaTime;
            _age += lDt;
            _timer -= lDt;
            // The slab unrolls under the word, then everything leaves fast to the right.
            float lOpen = 1f - Mathf.Pow(1f - Mathf.Clamp01(_age / 0.18f), 3f);
            _slab.localScale = new Vector3(Mathf.Lerp(0.2f, 1f, lOpen), 1f, 1f);
            if (_timer < 0.35f)
            {
                float lOut = 1f - Mathf.Clamp01(_timer / 0.35f);
                _rect.anchoredPosition = _home + new Vector2(lOut * lOut * 2600f, 0f);
            }
            if (_timer <= 0f) gameObject.SetActive(false);
        }
    }
}
