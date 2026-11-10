using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Stage select : flat graphics standing in perspective, the whole screen one panel running away to a vanishing point on
    // the right. Arrow-shaped cards nested one in the next : the front one is big, the next ones recede in depth so you
    // see what is coming. The wheel, the keys or a drag (with momentum) slide them along ; the front one is the one you
    // launch.
    public class StageSelect : MonoBehaviour
    {
        private const float CARD_WIDTH = 1040f;
        private const float CARD_HEIGHT = 640f;
        // The arrows touch : each card's point sits in the notch of the next.
        private const float CARD_STEP = 1000f;
        private const float FRONT_SCALE = 1.22f;

        private class Card
        {
            public RectTransform rect;
            public CanvasGroup group;
        }

        private RectTransform _root;
        private PerspectivePanel _panel;
        private Camera _uiCamera;
        private Card[] _cards;
        private RectTransform _row;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _tagline;
        private TextMeshProUGUI _description;
        private TextMeshProUGUI _code;
        private TextMeshProUGUI _best;
        private UI3DText _rank;
        private RectTransform _go;
        private UIShape _goBack;
        private RectTransform _info;
        private System.Action<int> _launch;
        private System.Action _back;
        private float _focus;
        private int _target;
        private int _shown = -1;
        private bool _dragging;
        private bool _dragMoved;
        private float _dragStartX;
        private float _dragStartFocus;
        private float _dragLastX;
        // Fling : cards per second, slowed by friction, then the nearest card snaps to the front.
        private float _velocity;
        private bool _coasting;

        public static StageSelect Create(Canvas pCanvas, System.Action<int> pLaunch, System.Action pBack)
        {
            StageSelect lSelect = new GameObject("StageSelect").AddComponent<StageSelect>();
            lSelect.transform.SetParent(pCanvas.transform, false);
            lSelect._launch = pLaunch;
            lSelect._back = pBack;
            lSelect._uiCamera = pCanvas.worldCamera;
            lSelect.Build((RectTransform)pCanvas.transform);
            return lSelect;
        }

        private void Build(RectTransform pCanvas)
        {
            _root = UIFactory.Stretch(UIFactory.CreateRect(pCanvas, "Stages", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            _panel = _root.gameObject.AddComponent<PerspectivePanel>();
            _panel.baseEuler = new Vector3(6f, -27f, 4f);
            _root.localScale = Vector3.one * 0.8f;
            _panel.amountX = 2.5f;
            _panel.amountY = 2f;

            // Backdrop, oversized so the tilted panel always covers the screen.
            RectTransform lBack = UIFactory.CreateRect(_root, "Backdrop", new Vector2(0.5f, 0.5f), new Vector2(200f, 0f), new Vector2(6200f, 3400f));
            UIFactory.CreateImage(lBack, "Red", UIKit.RED, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6200f, 3400f));
            UIKit.Chevrons(lBack, "Chevrons", new Color(0.8f, 0.04f, 0.09f), new Color(0.72f, 0.03f, 0.08f), 40f);
            UIKit.Tile(_root, "SlashA", new Color(1f, 1f, 1f, 0.1f), new Vector2(0.5f, 0.5f), new Vector2(-240f, 520f), new Vector2(3600f, 70f), 0.5f, 8f);

            _row = UIFactory.CreateRect(_root, "Row", new Vector2(0.5f, 0.5f), new Vector2(-260f, 130f), Vector2.zero);
            _cards = new Card[LevelDatabase.Count];
            for (int i = 0; i < _cards.Length; i++) _cards[i] = BuildCard(i);

            // The band under the front card : code, name, tagline, two lines of description ; best time and rank big on the right.
            _info = UIFactory.CreateRect(_root, "Info", new Vector2(0.5f, 0.5f), new Vector2(-120f, -380f), new Vector2(1800f, 280f));
            _info.localRotation = Quaternion.Euler(0f, 0f, 3f);
            UIKit.Tile(_info, "Plate", UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1700f, 250f), 0.4f);
            UIKit.Tile(_info, "Band", UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(-540f, 96f), new Vector2(620f, 66f), 0.4f);
            _code = UIKit.Text(_info, "", 40f, UIKit.GOLD, new Vector2(0.5f, 0.5f), new Vector2(-540f, 96f), new Vector2(600f, 60f), UIKit.Font.Heavy);
            _code.enableAutoSizing = true;
            _code.fontSizeMin = 18f;
            _code.fontSizeMax = 40f;
            _title = UIKit.Text(_info, "", 80f, UIKit.INK, new Vector2(0f, 0.5f), new Vector2(560f, 24f), new Vector2(980f, 90f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            _title.enableAutoSizing = true;
            _title.fontSizeMin = 30f;
            _title.fontSizeMax = 80f;
            _tagline = UIKit.Text(_info, "", 30f, UIKit.RED_DARK, new Vector2(0f, 0.5f), new Vector2(575f, -32f), new Vector2(980f, 36f), UIKit.Font.Bold, TextAlignmentOptions.Left);
            _tagline.enableAutoSizing = true;
            _tagline.fontSizeMin = 14f;
            _tagline.fontSizeMax = 30f;
            _description = UIKit.Text(_info, "", 26f, new Color(0.25f, 0.22f, 0.3f), new Vector2(0f, 0.5f), new Vector2(590f, -90f), new Vector2(980f, 70f), UIKit.Font.Bold, TextAlignmentOptions.TopLeft);
            _description.enableAutoSizing = true;
            _description.fontSizeMin = 14f;
            _description.fontSizeMax = 26f;
            _description.textWrappingMode = TextWrappingModes.Normal;

            UIKit.Text(_info, "BEST", 34f, UIKit.RED_DARK, new Vector2(1f, 0.5f), new Vector2(-420f, 70f), new Vector2(300f, 40f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            _best = UIKit.Text(_info, "", 72f, UIKit.INK, new Vector2(1f, 0.5f), new Vector2(-420f, 10f), new Vector2(460f, 84f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            _rank = UI3DText.Create(_info, " ", 170f, new Vector2(1f, 0.5f), new Vector2(-150f, 10f), new Vector2(230f, 220f), UIKit.RED, UIKit.RED_DARK, 18, 4f);
            _rank.tiltX = 6f;
            _rank.tiltY = 4f;

            _go = UIFactory.CreateRect(_root, "Go", new Vector2(0.5f, 0.5f), new Vector2(760f, -640f), new Vector2(680f, 250f));
            _go.localRotation = Quaternion.Euler(0f, 0f, 3f);
            UIShape lGoShadow = UIFactory.CreateShape(_go, "Shadow", UIShape.ShapeKind.Chevron, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(18f, -14f), new Vector2(700f, 260f));
            lGoShadow.skew = 0.38f;
            _goBack = UIFactory.CreateShape(_go, "Plate", UIShape.ShapeKind.Chevron, UIKit.RED_DEEP, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 250f));
            _goBack.skew = 0.38f;
            UI3DText lGoWord = UI3DText.Create(_go, "GO", 200f, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(480f, 230f), UIKit.WHITE, UIKit.RED_DARK, 14, 4f);
            lGoWord.tiltX = 6f;
            lGoWord.tiltY = 4f;

            // A strip of little arrows rushing toward the vanishing point.
            RectTransform lStrip = UIFactory.CreateRect(_root, "ArrowStrip", new Vector2(0.5f, 0.5f), new Vector2(500f, -830f), new Vector2(4400f, 50f));
            lStrip.localRotation = Quaternion.Euler(0f, 0f, 3f);
            UIImageMask(lStrip);
            UIChevronPattern lArrows = UIKit.Chevrons(lStrip, "Arrows", new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0.62f), 180f);
            lArrows.cellWidth = 120f;
            lArrows.cellHeight = 50f;

            gameObject.SetActive(false);
            _root.gameObject.SetActive(false);
        }

        private static void UIImageMask(RectTransform pRect)
        {
            Image lImage = pRect.gameObject.AddComponent<Image>();
            lImage.color = Color.white;
            pRect.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        }

        private Card BuildCard(int pIndex)
        {
            LevelDefinition lLevel = LevelDatabase.Get(pIndex);
            Theme lTheme = lLevel.theme();
            bool lUnlocked = GameSession.IsUnlocked(pIndex);

            RectTransform lRect = UIFactory.CreateRect(_row, "Card" + pIndex, new Vector2(0.5f, 0.5f), new Vector2(pIndex * CARD_STEP, 0f), new Vector2(CARD_WIDTH, CARD_HEIGHT));
            CanvasGroup lGroup = lRect.gameObject.AddComponent<CanvasGroup>();
            const float SKEW = 0.42f;

            UIShape lOuter = UIFactory.CreateShape(lRect, "Outer", UIShape.ShapeKind.Chevron, UIKit.RED_DEEP, new Vector2(0.5f, 0.5f), new Vector2(-14f, 0f), new Vector2(CARD_WIDTH + 60f, CARD_HEIGHT + 50f));
            lOuter.skew = SKEW;
            UIShape lFrame = UIFactory.CreateShape(lRect, "Frame", UIShape.ShapeKind.Chevron, UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CARD_WIDTH + 22f, CARD_HEIGHT + 22f));
            lFrame.skew = SKEW;
            UIShape lFace = UIFactory.CreateShape(lRect, "Face", UIShape.ShapeKind.Chevron, lTheme.road, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CARD_WIDTH, CARD_HEIGHT));
            lFace.skew = SKEW;
            Texture2D lPicture = Resources.Load<Texture2D>("StageCards/stage_" + pIndex);
            if (lPicture != null)
            {
                RawImage lImage = UIKit.MaskedImage(lFace, lPicture);
                // Cropped to the card, never stretched.
                float lCardAspect = (CARD_WIDTH + CARD_HEIGHT * SKEW * 2f) / CARD_HEIGHT;
                float lPictureAspect = lPicture.width / (float)lPicture.height;
                if (lPictureAspect > lCardAspect)
                {
                    float lW = lCardAspect / lPictureAspect;
                    lImage.uvRect = new Rect((1f - lW) * 0.5f, 0f, lW, 1f);
                }
                else
                {
                    float lH = lPictureAspect / lCardAspect;
                    lImage.uvRect = new Rect(0f, (1f - lH) * 0.5f, 1f, lH);
                }
            }
            else
            {
                // No capture : the colours of the place, with its sky band.
                lFace.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                UIKit.Tile(lFace.transform, "Sky", lTheme.skyTop, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(CARD_WIDTH * 1.4f, 340f), 0f);
                UIKit.Tile(lFace.transform, "Land", lTheme.roadA, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(CARD_WIDTH * 1.4f, 180f), 0.5f);
                UIKit.Tile(lFace.transform, "Slash", lTheme.accent, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(CARD_WIDTH * 1.4f, 60f), 0.5f, 10f);
            }
            // A dark band at the bottom of the picture so the number always reads.
            UIKit.Tile(lRect, "NumberBand", new Color(0.04f, 0.02f, 0.06f, 0.7f), new Vector2(0f, 0f), new Vector2(330f, 92f), new Vector2(420f, 130f), 0.42f);
            TextMeshProUGUI lLabel = UIKit.Text(lRect, LevelDatabase.Code(pIndex), 128f, UIKit.WHITE, new Vector2(0f, 0f), new Vector2(330f, 92f), new Vector2(320f, 150f), UIKit.Font.Heavy);
            UIFactory.Outline(lLabel, UIKit.INK, 0.25f);
            if (!lUnlocked)
            {
                UIFactory.CreateShape(lRect, "Dim", UIShape.ShapeKind.Chevron, new Color(0.04f, 0.02f, 0.06f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CARD_WIDTH, CARD_HEIGHT)).skew = SKEW;
                UIKit.Text(lRect, "LOCKED", 90f, UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 110f), UIKit.Font.Heavy);
            }
            return new Card { rect = lRect, group = lGroup };
        }

        public void Show(int pIndex)
        {
            gameObject.SetActive(true);
            _root.gameObject.SetActive(true);
            _target = Mathf.Clamp(pIndex, 0, _cards.Length - 1);
            _focus = _target - 0.6f;
            _velocity = 0f;
            _coasting = false;
            _shown = -1;
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private void Move(int pDelta)
        {
            int lNext = Mathf.Clamp(_target + pDelta, 0, _cards.Length - 1);
            if (lNext != _target) AudioManager.Play("ui_move", 0.6f);
            _target = lNext;
            _coasting = false;
            _velocity = 0f;
        }

        private void Update()
        {
            float lDt = Time.unscaledDeltaTime;
            ReadInput(lDt);
            if (_coasting)
            {
                _focus += _velocity * lDt;
                _velocity *= Mathf.Exp(-3.2f * lDt);
                if (_focus < 0f || _focus > _cards.Length - 1)
                {
                    _focus = Mathf.Clamp(_focus, 0f, _cards.Length - 1);
                    _velocity = 0f;
                }
                int lNearest = Mathf.Clamp(Mathf.RoundToInt(_focus), 0, _cards.Length - 1);
                if (lNearest != _target) AudioManager.Play("ui_move", 0.35f);
                _target = lNearest;
                if (Mathf.Abs(_velocity) < 1.5f) _coasting = false;
            }
            else if (!_dragging) _focus = Mathf.Lerp(_focus, _target, 1f - Mathf.Exp(-10f * lDt));
            for (int i = 0; i < _cards.Length; i++)
            {
                float lFrom = i - _focus;
                Card lCard = _cards[i];
                bool lVisible = lFrom > -1.6f && lFrom < 7f;
                if (lCard.rect.gameObject.activeSelf != lVisible) lCard.rect.gameObject.SetActive(lVisible);
                if (!lVisible) continue;
                // Those we have passed slide off ; the front one is big ; the next ones recede and turn toward you.
                lCard.group.alpha = lFrom < -0.4f ? Mathf.Clamp01(1.6f + lFrom) : 1f;
                float lFront = Mathf.Clamp01(1f - Mathf.Abs(lFrom));
                float lAhead = Mathf.Max(0f, lFrom);
                lCard.rect.localScale = Vector3.one * Mathf.Lerp(0.86f, FRONT_SCALE, lFront);
                lCard.rect.localPosition = new Vector3(lFrom * CARD_STEP + lAhead * 40f, lFront * 30f, lAhead * 140f - lFront * 80f);
                lCard.rect.localRotation = Quaternion.Euler(0f, lAhead * -3f, 0f);
                // The front card is drawn on top of the ones behind it.
                lCard.rect.SetSiblingIndex(Mathf.Clamp(_cards.Length - 1 - Mathf.RoundToInt(Mathf.Abs(lFrom) * 2f), 0, _cards.Length - 1));
            }
            if (_shown != _target) ShowInfo(_target);
            _go.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.035f);
        }

        private void ShowInfo(int pIndex)
        {
            _shown = pIndex;
            LevelDefinition lLevel = LevelDatabase.Get(pIndex);
            _title.text = lLevel.title;
            _tagline.text = lLevel.tagline;
            _description.text = lLevel.description;
            _code.text = "STAGE " + LevelDatabase.Code(pIndex) + "   |   " + LevelDatabase.WORLDS[lLevel.world];
            float lBest = GameSession.GetBestTime(pIndex);
            _best.text = lBest > 0f ? GameSession.FormatTime(lBest) : "--:--.--";
            int lRank = GameSession.GetRank(pIndex);
            string lLetter = GameSession.RankLetter(lRank);
            _rank.SetText(string.IsNullOrEmpty(lLetter) ? "-" : lLetter);
            _rank.SetFace(lRank >= 4 ? UIKit.GOLD : UIKit.RED);
            _rank.Slam(1.8f, 40f);
            bool lOpen = GameSession.IsUnlocked(pIndex);
            _goBack.color = lOpen ? UIKit.RED_DEEP : UIKit.GREY;
            UITween.Kill(_info);
            _info.localScale = new Vector3(1f, 0.8f, 1f);
            UITween.Scale(_info, Vector3.one, 0.18f, Ease.OutBack);
        }

        private bool Over(RectTransform pRect) => RectTransformUtility.RectangleContainsScreenPoint(pRect, HSInput.MousePosition, _uiCamera);

        private void ReadInput(float pDt)
        {
            if (SceneTransition.IsBusy) return;
            Vector2Int lMove = HSInput.MenuMove;
            if (lMove.x != 0) Move(lMove.x);
            else if (lMove.y != 0) Move(-lMove.y);
            float lWheel = HSInput.MouseWheel;
            if (Mathf.Abs(lWheel) > 0.01f) Move(lWheel > 0f ? -1 : 1);

            float lMouseX = HSInput.MousePosition.x;
            if (HSInput.MouseClicked)
            {
                _dragging = true;
                _dragMoved = false;
                _dragStartX = lMouseX;
                _dragLastX = lMouseX;
                _dragStartFocus = _focus;
                _coasting = false;
                _velocity = 0f;
            }
            if (_dragging && HSInput.MouseHeld)
            {
                float lDelta = (lMouseX - _dragStartX) / Mathf.Max(1f, Screen.width);
                if (Mathf.Abs(lDelta) > 0.015f) _dragMoved = true;
                if (_dragMoved)
                {
                    // The cards follow the hand exactly while you hold them.
                    _focus = Mathf.Clamp(_dragStartFocus - lDelta * 2.6f, -0.3f, _cards.Length - 0.7f);
                    _target = Mathf.Clamp(Mathf.RoundToInt(_focus), 0, _cards.Length - 1);
                    float lInstant = -(lMouseX - _dragLastX) / Mathf.Max(1f, Screen.width) * 2.6f / Mathf.Max(0.0001f, pDt);
                    _velocity = Mathf.Lerp(_velocity, lInstant, 1f - Mathf.Exp(-14f * pDt));
                }
                _dragLastX = lMouseX;
            }
            if (_dragging && !HSInput.MouseHeld)
            {
                _dragging = false;
                if (!_dragMoved) Click();
                else
                {
                    // Let go : it keeps sliding with your throw.
                    _coasting = Mathf.Abs(_velocity) > 1.5f;
                    if (!_coasting) _target = Mathf.Clamp(Mathf.RoundToInt(_focus), 0, _cards.Length - 1);
                }
            }
            if (HSInput.SubmitMenuPressed) _launch?.Invoke(_target);
        }

        private void Click()
        {
            if (Over(_go))
            {
                _launch?.Invoke(_target);
                return;
            }
            // A click on a card brings it to the front ; on the front one, it launches.
            for (int i = 0; i < _cards.Length; i++)
            {
                if (!_cards[i].rect.gameObject.activeSelf || !Over(_cards[i].rect)) continue;
                if (i == _target) _launch?.Invoke(i);
                else
                {
                    AudioManager.Play("ui_move", 0.6f);
                    _target = i;
                }
                return;
            }
        }
    }
}
