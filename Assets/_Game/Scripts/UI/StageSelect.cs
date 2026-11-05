using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Stage select : flat graphics standing in perspective, the whole screen one panel running away to a vanishing point on
    // the right. Big arrow-shaped cards in a row : the wheel, a drag or the keys slide them along, the front one is the one
    // you launch.
    public class StageSelect : MonoBehaviour
    {
        private const float CARD_WIDTH = 1040f;
        private const float CARD_HEIGHT = 640f;
        private const float CARD_STEP = 1120f;

        private class Card
        {
            public RectTransform rect;
            public CanvasGroup group;
            public float pulse;
        }

        private RectTransform _root;
        private PerspectivePanel _panel;
        private Camera _uiCamera;
        private Card[] _cards;
        private RectTransform _row;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _tagline;
        private TextMeshProUGUI _best;
        private TextMeshProUGUI _league;
        private TextMeshProUGUI _bigWord;
        private TextMeshProUGUI _rank;
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
            _panel.amountX = 5f;
            _panel.amountY = 4f;

            // Backdrop, oversized so the tilted panel always covers the screen.
            RectTransform lBack = UIFactory.CreateRect(_root, "Backdrop", new Vector2(0.5f, 0.5f), new Vector2(200f, 0f), new Vector2(6200f, 3400f));
            UIFactory.CreateImage(lBack, "Red", UIKit.RED, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6200f, 3400f));
            UIKit.Chevrons(lBack, "Chevrons", new Color(0.8f, 0.04f, 0.09f), new Color(0.72f, 0.03f, 0.08f), 40f);

            _bigWord = UIKit.OutlineText(_root, "STAGE", 460f, new Color(1f, 1f, 1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-860f, 60f), new Vector2(1400f, 520f), 0f, 0.05f);
            UIKit.Tile(_root, "SlashA", new Color(1f, 1f, 1f, 0.14f), new Vector2(0.5f, 0.5f), new Vector2(-240f, 420f), new Vector2(3600f, 90f), 0.5f, 8f);
            UIKit.Tile(_root, "SlashB", new Color(0.4f, 0f, 0.03f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(300f, 610f), new Vector2(3600f, 150f), 0.5f, 8f);

            _row = UIFactory.CreateRect(_root, "Row", new Vector2(0.5f, 0.5f), new Vector2(-90f, 90f), Vector2.zero);
            _cards = new Card[LevelDatabase.Count];
            for (int i = 0; i < _cards.Length; i++) _cards[i] = BuildCard(i);

            // The band under the front card.
            _info = UIFactory.CreateRect(_root, "Info", new Vector2(0.5f, 0.5f), new Vector2(-60f, -310f), new Vector2(1500f, 150f));
            _info.localRotation = Quaternion.Euler(0f, 0f, 3f);
            UIKit.Tile(_info, "Plate", UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1320f, 128f), 0.5f);
            _title = UIKit.Text(_info, "", 74f, UIKit.INK, new Vector2(0f, 0.5f), new Vector2(440f, 18f), new Vector2(820f, 84f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            _title.enableAutoSizing = true;
            _title.fontSizeMin = 26f;
            _title.fontSizeMax = 74f;
            _tagline = UIKit.Text(_info, "", 28f, UIKit.RED_DARK, new Vector2(0f, 0.5f), new Vector2(460f, -38f), new Vector2(820f, 36f), UIKit.Font.Bold, TextAlignmentOptions.Left);
            _tagline.enableAutoSizing = true;
            _tagline.fontSizeMin = 14f;
            _tagline.fontSizeMax = 28f;
            _best = UIKit.Text(_info, "", 36f, UIKit.INK, new Vector2(1f, 0.5f), new Vector2(-290f, 24f), new Vector2(300f, 44f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            _rank = UIKit.Text(_info, "", 44f, UIKit.RED, new Vector2(1f, 0.5f), new Vector2(-290f, -28f), new Vector2(300f, 50f), UIKit.Font.Heavy, TextAlignmentOptions.Right);

            _go = UIFactory.CreateRect(_root, "Go", new Vector2(0.5f, 0.5f), new Vector2(800f, -350f), new Vector2(340f, 130f));
            _go.localRotation = Quaternion.Euler(0f, 0f, 3f);
            _goBack = UIFactory.CreateShape(_go, "Plate", UIShape.ShapeKind.Chevron, UIKit.RED_DEEP, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 130f));
            _goBack.skew = 0.38f;
            UIKit.Text(_go, "GO", 96f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(6f, 0f), new Vector2(240f, 110f), UIKit.Font.Heavy);

            _league = UIKit.Text(_root, "", 40f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-860f, 450f), new Vector2(700f, 54f), UIKit.Font.Heavy, TextAlignmentOptions.Left);

            // A strip of little arrows rushing toward the vanishing point.
            RectTransform lStrip = UIFactory.CreateRect(_root, "ArrowStrip", new Vector2(0.5f, 0.5f), new Vector2(500f, -470f), new Vector2(4400f, 50f));
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

            UIShape lOuter = UIFactory.CreateShape(lRect, "Outer", UIShape.ShapeKind.Chevron, UIKit.RED_DEEP, new Vector2(0.5f, 0.5f), new Vector2(-14f, 0f), new Vector2(CARD_WIDTH + 90f, CARD_HEIGHT + 62f));
            lOuter.skew = SKEW;
            UIShape lFrame = UIFactory.CreateShape(lRect, "Frame", UIShape.ShapeKind.Chevron, UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CARD_WIDTH + 30f, CARD_HEIGHT + 26f));
            lFrame.skew = SKEW;
            UIShape lFace = UIFactory.CreateShape(lRect, "Face", UIShape.ShapeKind.Chevron, lTheme.road, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CARD_WIDTH, CARD_HEIGHT));
            lFace.skew = SKEW;
            Texture2D lPicture = Resources.Load<Texture2D>("StageCards/stage_" + pIndex);
            if (lPicture != null) UIKit.MaskedImage(lFace, lPicture);
            else
            {
                // No capture : the colours of the place, with its sky band.
                lFace.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                UIKit.Tile(lFace.transform, "Sky", lTheme.skyTop, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(CARD_WIDTH * 1.4f, 340f), 0f);
                UIKit.Tile(lFace.transform, "Land", lTheme.roadA, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(CARD_WIDTH * 1.4f, 180f), 0.5f);
                UIKit.Tile(lFace.transform, "Slash", lTheme.accent, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(CARD_WIDTH * 1.4f, 60f), 0.5f, 10f);
            }

            string lNumber = (lLevel.world + 1) + "-" + StageInWorld(pIndex);
            TextMeshProUGUI lLabel = UIKit.Text(lRect, lNumber, 128f, UIKit.WHITE, new Vector2(0f, 0f), new Vector2(330f, 90f), new Vector2(320f, 150f), UIKit.Font.Heavy);
            UIFactory.Outline(lLabel, UIKit.INK, 0.25f);
            if (!lUnlocked)
            {
                UIFactory.CreateShape(lRect, "Dim", UIShape.ShapeKind.Chevron, new Color(0.04f, 0.02f, 0.06f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CARD_WIDTH, CARD_HEIGHT)).skew = SKEW;
                UIKit.Text(lRect, "LOCKED", 90f, UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 110f), UIKit.Font.Heavy);
            }
            return new Card { rect = lRect, group = lGroup };
        }

        private static int StageInWorld(int pIndex)
        {
            int lWorld = LevelDatabase.Get(pIndex).world;
            int lCount = 0;
            for (int i = 0; i <= pIndex; i++)
                if (LevelDatabase.Get(i).world == lWorld) lCount++;
            return lCount;
        }

        public void Show(int pIndex)
        {
            gameObject.SetActive(true);
            _root.gameObject.SetActive(true);
            _target = Mathf.Clamp(pIndex, 0, _cards.Length - 1);
            _focus = _target - 0.6f;
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
        }

        private void Update()
        {
            float lDt = Time.unscaledDeltaTime;
            ReadInput();
            _focus = Mathf.Lerp(_focus, _target, 1f - Mathf.Exp(-11f * lDt));
            _row.anchoredPosition = new Vector2(-90f - _focus * CARD_STEP, 90f);
            for (int i = 0; i < _cards.Length; i++)
            {
                float lFrom = i - _focus;
                Card lCard = _cards[i];
                bool lVisible = lFrom > -1.6f && lFrom < 6f;
                if (lCard.rect.gameObject.activeSelf != lVisible) lCard.rect.gameObject.SetActive(lVisible);
                if (!lVisible) continue;
                // Those we have passed slide off, the front one is big, the others wait toward the vanishing point.
                lCard.group.alpha = lFrom < -0.4f ? Mathf.Clamp01(1.6f + lFrom) : 1f;
                float lFront = Mathf.Clamp01(1f - Mathf.Abs(lFrom));
                lCard.rect.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, lFront);
            }
            if (_shown != _target) ShowInfo(_target);
            _go.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.03f);
        }

        private void ShowInfo(int pIndex)
        {
            _shown = pIndex;
            LevelDefinition lLevel = LevelDatabase.Get(pIndex);
            _title.text = lLevel.title;
            _tagline.text = lLevel.tagline;
            _league.text = LevelDatabase.WORLDS[lLevel.world];
            _bigWord.text = "STAGE " + (pIndex + 1);
            float lBest = GameSession.GetBestTime(pIndex);
            _best.text = lBest > 0f ? GameSession.FormatTime(lBest) : "--:--.--";
            _rank.text = GameSession.RankLetter(GameSession.GetRank(pIndex));
            bool lOpen = GameSession.IsUnlocked(pIndex);
            _goBack.color = lOpen ? UIKit.RED_DEEP : UIKit.GREY;
            UITween.Kill(_info);
            _info.localScale = new Vector3(1f, 0.8f, 1f);
            UITween.Scale(_info, Vector3.one, 0.18f, Ease.OutBack);
        }

        private bool Over(RectTransform pRect) => RectTransformUtility.RectangleContainsScreenPoint(pRect, HSInput.MousePosition, _uiCamera);

        private void ReadInput()
        {
            if (SceneTransition.IsBusy) return;
            Vector2Int lMove = HSInput.MenuMove;
            if (lMove.x != 0) Move(lMove.x);
            else if (lMove.y != 0) Move(-lMove.y);
            float lWheel = HSInput.MouseWheel;
            if (Mathf.Abs(lWheel) > 0.01f) Move(lWheel > 0f ? -1 : 1);

            if (HSInput.MouseClicked)
            {
                _dragging = true;
                _dragMoved = false;
                _dragStartX = HSInput.MousePosition.x;
                _dragStartFocus = _focus;
            }
            if (_dragging && HSInput.MouseHeld)
            {
                float lDelta = (HSInput.MousePosition.x - _dragStartX) / Mathf.Max(1f, Screen.width);
                if (Mathf.Abs(lDelta) > 0.015f) _dragMoved = true;
                if (_dragMoved) _target = Mathf.Clamp(Mathf.RoundToInt(_dragStartFocus - lDelta * 2.4f), 0, _cards.Length - 1);
            }
            if (_dragging && !HSInput.MouseHeld)
            {
                _dragging = false;
                if (!_dragMoved) Click();
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
