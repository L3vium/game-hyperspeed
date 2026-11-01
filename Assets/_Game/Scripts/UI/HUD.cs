using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Minimal race HUD : timer, event feed under it, vertical progress rail on the right, rival pointer,
    // discreet countdown, plus VS intro card, results and pause in the Boost-Vector-like style.
    public class HUD : MonoBehaviour
    {
        private class FeedEntry
        {
            public RectTransform rect;
            public CanvasGroup group;
            public float time;
            public float targetY;
        }

        private const int FEED_MAX = 5;
        private const float RAIL_HALF = 330f;

        private Canvas _canvas;
        private RectTransform _root;
        private Material _speedLinesMaterial;
        private float _lineIntensity;

        private TextMeshProUGUI _timerText;
        private RectTransform _feedRoot;
        private readonly List<FeedEntry> _feed = new List<FeedEntry>();
        private TextMeshProUGUI _bigText;
        private float _bigTimer;

        private RectTransform _rail;
        private RectTransform _playerMarker;
        private RectTransform _rivalMarker;
        private RectTransform _rivalPointer;
        private TextMeshProUGUI _rivalPointerText;
        private TextMeshProUGUI _warningText;
        private RectTransform _hintPanel;
        private TextMeshProUGUI _hintText;
        private int _hintIndex;
        private float _hintTimer;

        private PlayerController _player;
        private Rival _rival;
        private TrackPath _path;
        private LevelDefinition _level;

        public Transform Root => _root;

        public static HUD Create(PlayerController pPlayer, Rival pRival, TrackPath pPath, LevelDefinition pLevel)
        {
            HUD lHud = new GameObject("HUD").AddComponent<HUD>();
            lHud._player = pPlayer;
            lHud._rival = pRival;
            lHud._path = pPath;
            lHud._level = pLevel;
            lHud.Build();
            Juice.OnPopup += lHud.ShowPopup;
            return lHud;
        }

        private void OnDestroy()
        {
            Juice.OnPopup -= ShowPopup;
        }

        private void Build()
        {
            _canvas = UIFactory.CreateCanvas("HUDCanvas", 10, transform);
            _root = (RectTransform)_canvas.transform;

            RawImage lLines = UIFactory.CreateRect(_root, "SpeedLines", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            UIFactory.Stretch(lLines.rectTransform);
            _speedLinesMaterial = new Material(Shader.Find("Hyperspeed/SpeedLines"));
            lLines.material = _speedLinesMaterial;
            lLines.raycastTarget = false;

            // Timer (top right)
            UIKit.Tile(_root, "TimerBack", new Color(0f, 0f, 0f, 0.55f), new Vector2(1f, 1f), new Vector2(-230f, -62f), new Vector2(380f, 74f), 0.45f);
            UIKit.Tile(_root, "TimerAccent", UIKit.RED, new Vector2(1f, 1f), new Vector2(-418f, -62f), new Vector2(18f, 74f), 0.45f);
            _timerText = UIKit.Text(_root, "00:00.00", 58f, UIKit.WHITE, new Vector2(1f, 1f), new Vector2(-226f, -60f), new Vector2(360f, 74f), UIKit.Font.Heavy);
            _feedRoot = UIFactory.CreateRect(_root, "Feed", new Vector2(1f, 1f), new Vector2(-230f, -130f), new Vector2(380f, 400f));

            _bigText = UIKit.OutlineText(_root, "", 150f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1900f, 200f), -4f, 0.12f);
            _bigText.gameObject.SetActive(false);

            // Vertical progress rail (right edge)
            _rail = UIFactory.CreateRect(_root, "Rail", new Vector2(1f, 0.5f), new Vector2(-70f, -40f), new Vector2(8f, RAIL_HALF * 2f));
            UIFactory.CreateImage(_rail, "Line", new Color(1f, 1f, 1f, 0.35f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, RAIL_HALF * 2f));
            if (_path != null && _path.finishDistance > 0f)
                foreach (float lCheckpoint in _path.checkpoints)
                    UIFactory.CreateImage(_rail, "Tick", new Color(1f, 1f, 1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, RailY(lCheckpoint)), new Vector2(18f, 3f));
            UIKit.Icon(_rail, "flag", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(0f, RAIL_HALF + 30f), 46f);
            _rivalMarker = Marker("Rival", UIKit.RED, RivalShortName());
            _playerMarker = Marker("Player", UIKit.CYAN, "YOU");

            // Rival pointer in the world
            _rivalPointer = UIFactory.CreateRect(_root, "RivalPointer", new Vector2(0f, 0f), Vector2.zero, new Vector2(60f, 60f));
            UIFactory.CreateShape(_rivalPointer, "Arrow", UIShape.ShapeKind.Triangle, UIKit.RED, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 34f), 180f);
            _rivalPointerText = UIKit.Text(_rivalPointer, "", 24f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(0f, 36f), new Vector2(220f, 34f), UIKit.Font.Heavy);
            UIFactory.Outline(_rivalPointerText, UIKit.INK, 0.3f);

            _warningText = UIKit.Text(_root, "", 40f, UIKit.RED, new Vector2(1f, 0.5f), new Vector2(-330f, -RAIL_HALF - 110f), new Vector2(560f, 50f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            UIFactory.Outline(_warningText, UIKit.INK, 0.3f);

            // Hints (bottom center, small white tile)
            _hintPanel = UIFactory.CreateRect(_root, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1200f, 64f));
            UIKit.Tile(_hintPanel, "Back", UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 60f), 0.45f);
            UIKit.Tile(_hintPanel, "Tag", UIKit.RED, new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(110f, 60f), 0.45f);
            UIKit.Text(_hintPanel, "TIP", 32f, UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(110f, 60f), UIKit.Font.Heavy);
            _hintText = UIKit.Text(_hintPanel, "", 30f, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(60f, 0f), new Vector2(950f, 60f), UIKit.Font.Heavy);
            _hintPanel.gameObject.SetActive(false);
        }

        private string RivalShortName()
        {
            if (_rival == null) return "";
            return RivalFactory.DisplayName(_rival.kind).Replace("THE ", "").Replace("A ", "");
        }

        private RectTransform Marker(string pName, Color pColor, string pLabel)
        {
            RectTransform lMarker = UIFactory.CreateRect(_rail, pName, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            UIFactory.CreateShape(lMarker, "Arrow", UIShape.ShapeKind.Chevron, pColor, new Vector2(0.5f, 0.5f), new Vector2(-14f, 0f), new Vector2(46f, 30f), 180f).skew = 0.45f;
            TextMeshProUGUI lText = UIKit.Text(lMarker, pLabel, 22f, pColor, new Vector2(0.5f, 0.5f), new Vector2(-130f, 0f), new Vector2(200f, 30f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            UIFactory.Outline(lText, UIKit.INK, 0.3f);
            return lMarker;
        }

        private float RailY(float pDistance)
        {
            float lFinish = Mathf.Max(1f, _path.finishDistance);
            return Mathf.Lerp(-RAIL_HALF, RAIL_HALF, Mathf.Clamp01(pDistance / lFinish));
        }

        public void SetRaceTime(float pTime)
        {
            _timerText.text = GameSession.FormatTime(pTime);
        }

        private void Update()
        {
            float lDt = Time.unscaledDeltaTime;
            if (_player != null)
            {
                float lTarget = _player.Speed01 + (_player.Hyper ? 0.3f : 0f) + (_player.Dashing ? 0.35f : 0f) + _player.BoostAmount * 0.3f;
                _lineIntensity = Mathf.Lerp(_lineIntensity, Mathf.Clamp01(lTarget), 1f - Mathf.Exp(-7f * lDt));
                _speedLinesMaterial.SetFloat("_Intensity", _player.State == PlayerState.Frozen ? 0f : _lineIntensity);
                _speedLinesMaterial.SetFloat("_Boost", _player.BoostAmount);
                _speedLinesMaterial.SetColor("_LineColor", _player.Hyper ? new Color(0.8f, 1f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.8f));
                UpdateRail();
                UpdateRivalPointer();
                UpdateHints(lDt);
            }
            UpdateFeed(lDt);
            if (_bigTimer > 0f)
            {
                _bigTimer -= lDt;
                float lT = 1f - _bigTimer / 1.1f;
                _bigText.rectTransform.localScale = Vector3.one * (lT < 0.12f ? Mathf.Lerp(1.8f, 1f, lT / 0.12f) : 1f + (lT - 0.12f) * 0.08f);
                _bigText.alpha = _bigTimer < 0.3f ? _bigTimer / 0.3f : 1f;
                if (_bigTimer <= 0f) _bigText.gameObject.SetActive(false);
            }
        }

        private void UpdateRail()
        {
            if (_path == null || _path.finishDistance <= 0f) return;
            _playerMarker.anchoredPosition = new Vector2(0f, RailY(_player.Progress));
            if (_rival == null) return;
            _rivalMarker.anchoredPosition = new Vector2(0f, RailY(Mathf.Max(0f, _rival.distance)));
            float lGap = _player.Progress - _rival.distance;
            if (_rival.chaser)
            {
                bool lDanger = lGap < 90f && RaceContext.Running;
                _warningText.text = lDanger ? RivalShortName() + "  " + Mathf.Max(0, Mathf.RoundToInt(lGap)) + " m BEHIND" : "";
                if (lDanger) Juice.Vignette(UIKit.RED, 0.6f);
            }
            else _warningText.text = "";
        }

        // Points at the rival : marker above it when visible, clamped arrow on the screen edge otherwise.
        private void UpdateRivalPointer()
        {
            Camera lCamera = CameraRig.Instance != null ? CameraRig.Instance.Cam : null;
            if (_rival == null || lCamera == null) { _rivalPointer.gameObject.SetActive(false); return; }
            _rivalPointer.gameObject.SetActive(true);
            Vector3 lScreen = lCamera.WorldToScreenPoint(_rival.transform.position + Vector3.up * 6f);
            float lScale = _canvas.scaleFactor;
            Vector2 lSize = new Vector2(Screen.width, Screen.height);
            bool lBehind = lScreen.z < 0f;
            if (lBehind) lScreen = -lScreen;
            Vector2 lPoint = new Vector2(lScreen.x, lScreen.y);
            bool lOnScreen = !lBehind && lPoint.x > 40f && lPoint.x < lSize.x - 40f && lPoint.y > 40f && lPoint.y < lSize.y - 40f;
            Vector2 lCenter = lSize * 0.5f;
            if (!lOnScreen)
            {
                Vector2 lDir = (lPoint - lCenter).normalized;
                if (lBehind && lDir.y > -0.2f) lDir = new Vector2(lDir.x, -0.6f).normalized;
                float lK = Mathf.Min((lSize.x * 0.5f - 70f) / Mathf.Max(0.001f, Mathf.Abs(lDir.x)), (lSize.y * 0.5f - 70f) / Mathf.Max(0.001f, Mathf.Abs(lDir.y)));
                lPoint = lCenter + lDir * lK;
                _rivalPointer.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(lDir.y, lDir.x) * Mathf.Rad2Deg + 90f);
            }
            else _rivalPointer.localRotation = Quaternion.identity;
            _rivalPointer.anchoredPosition = lPoint / lScale;
            float lGap = _rival.distance - _player.Progress;
            _rivalPointerText.text = (lGap >= 0f ? "+" : "") + Mathf.RoundToInt(lGap) + " m";
            _rivalPointerText.rectTransform.localRotation = Quaternion.Inverse(_rivalPointer.localRotation);
        }

        private void UpdateHints(float pDt)
        {
            if (_hintIndex < _path.hints.Count && _player.Progress >= _path.hints[_hintIndex].Key - 10f)
            {
                _hintText.text = _path.hints[_hintIndex].Value;
                _hintIndex++;
                _hintTimer = 4f;
                _hintPanel.gameObject.SetActive(true);
                _hintPanel.anchoredPosition = new Vector2(-1500f, 70f);
                AudioManager.Play("ui_hint", 0.5f);
            }
            if (_hintTimer > 0f)
            {
                _hintTimer -= pDt;
                float lTargetX = _hintTimer > 0.4f ? 0f : 1700f;
                _hintPanel.anchoredPosition = Vector2.Lerp(_hintPanel.anchoredPosition, new Vector2(lTargetX, 70f), 1f - Mathf.Exp(-14f * pDt));
                if (_hintTimer <= 0f) _hintPanel.gameObject.SetActive(false);
            }
        }

        // ---------------------------------------------------------------- event feed

        private void ShowPopup(string pText, Color pColor, float pScale)
        {
            // Rare huge events get a big hollow word ; everything lands in the feed under the timer.
            if (pScale >= 1.45f)
            {
                _bigText.text = pText;
                _bigText.outlineColor = pColor == UIKit.WHITE ? UIKit.CYAN : (Color32)pColor;
                _bigText.gameObject.SetActive(true);
                _bigTimer = 1.1f;
            }

            RectTransform lEntry = UIFactory.CreateRect(_feedRoot, "Entry", new Vector2(1f, 1f), new Vector2(-150f, 0f), new Vector2(380f, 54f));
            CanvasGroup lGroup = lEntry.gameObject.AddComponent<CanvasGroup>();
            bool lDark = pColor == UIKit.WHITE || pColor == Color.white;
            UIKit.Tile(lEntry, "Back", lDark ? new Color(0f, 0f, 0f, 0.6f) : UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 48f), 0.45f);
            UIKit.Tile(lEntry, "Accent", pColor, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(14f, 48f), 0.45f);
            TextMeshProUGUI lText = UIKit.Text(lEntry, pText, 32f, lDark ? UIKit.WHITE : UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(320f, 48f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            lText.enableAutoSizing = true;
            lText.fontSizeMin = 16f;
            lText.fontSizeMax = 32f;
            lEntry.anchoredPosition = new Vector2(260f, 0f);
            _feed.Insert(0, new FeedEntry { rect = lEntry, group = lGroup, time = 0f });
            while (_feed.Count > FEED_MAX)
            {
                Destroy(_feed[_feed.Count - 1].rect.gameObject);
                _feed.RemoveAt(_feed.Count - 1);
            }
        }

        private void UpdateFeed(float pDt)
        {
            for (int i = _feed.Count - 1; i >= 0; i--)
            {
                FeedEntry lEntry = _feed[i];
                lEntry.time += pDt;
                Vector2 lTarget = new Vector2(-170f + i * 14f, -i * 58f);
                lEntry.rect.anchoredPosition = Vector2.Lerp(lEntry.rect.anchoredPosition, lTarget, 1f - Mathf.Exp(-18f * pDt));
                float lPop = Mathf.Max(0f, 1f - lEntry.time * 6f);
                lEntry.rect.localScale = Vector3.one * (1f + lPop * 0.25f);
                lEntry.group.alpha = lEntry.time > 2.4f ? Mathf.Max(0f, 1f - (lEntry.time - 2.4f) / 0.4f) : 1f;
                if (lEntry.time > 2.8f)
                {
                    Destroy(lEntry.rect.gameObject);
                    _feed.RemoveAt(i);
                }
            }
        }

        // ---------------------------------------------------------------- VS intro & countdown

        public IEnumerator VsCard(LevelDefinition pLevel, int pIndex, float pDuration)
        {
            RectTransform lCard = UIFactory.Stretch(UIFactory.CreateRect(_root, "VsCard", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            UIShape lBand = UIKit.Tile(lCard, "Band", UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2(0f, -250f), new Vector2(2600f, 260f), 0.4f, -4f);
            UIKit.Chevrons(lBand.transform, "Chevrons", UIKit.RED, UIKit.RED_DARK, 300f);
            lBand.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            TextMeshProUGUI lVs = UIKit.OutlineText(lCard, "VS", 420f, new Color(1f, 1f, 1f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(-520f, 120f), new Vector2(700f, 460f), -4f, 0.07f);
            TextMeshProUGUI lName = UIKit.Text(lCard, RivalFactory.DisplayName(pLevel.rival), 130f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(60f, -230f), new Vector2(2200f, 160f), UIKit.Font.Heavy, TextAlignmentOptions.Center, true, -4f);
            UIFactory.Outline(lName, UIKit.INK, 0.15f);
            TextMeshProUGUI lTag = UIKit.Text(lCard, pLevel.tagline, 36f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(60f, -330f), new Vector2(1800f, 50f), UIKit.Font.Bold, TextAlignmentOptions.Center, true, -4f);
            UIKit.Checker(lCard, "Checker", new Vector2(0.5f, 0.5f), new Vector2(0f, -400f), new Vector2(2600f, 46f), UIKit.WHITE, UIKit.RED_DARK, 300f).rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            TextMeshProUGUI lStage = UIKit.Text(lCard, LevelDatabase.WORLDS[pLevel.world] + "  //  STAGE " + (pIndex + 1).ToString("00"), 34f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-300f, -120f), new Vector2(1000f, 50f), UIKit.Font.Heavy, TextAlignmentOptions.Left, true, -4f);
            AudioManager.Play("sfx_vs", 1f);
            for (float lTime = 0f; lTime < pDuration; lTime += Time.unscaledDeltaTime)
            {
                float lIn = 1f - Mathf.Pow(1f - Mathf.Clamp01(lTime / 0.25f), 3f);
                float lOut = Mathf.Clamp01((lTime - (pDuration - 0.3f)) / 0.3f);
                float lX = Mathf.Lerp(-2600f, 0f, lIn) + lOut * 2600f;
                lBand.rectTransform.anchoredPosition = new Vector2(lX, -250f);
                lName.rectTransform.anchoredPosition = new Vector2(60f - lX * 1.2f + lTime * 30f, -230f);
                lTag.rectTransform.anchoredPosition = new Vector2(60f + lX * 1.1f - lTime * 20f, -330f);
                lStage.rectTransform.anchoredPosition = new Vector2(-300f - lX * 0.8f, -120f);
                lVs.rectTransform.localScale = Vector3.one * Mathf.Lerp(2f, 1f, lIn);
                lVs.alpha = 1f - lOut;
                yield return null;
            }
            Destroy(lCard.gameObject);
        }

        public IEnumerator Countdown(System.Action pOnGo)
        {
            string[] lSteps = { "3", "2", "1", "GO!" };
            for (int i = 0; i < lSteps.Length; i++)
            {
                bool lGo = i == lSteps.Length - 1;
                RectTransform lGroup = UIFactory.CreateRect(_root, "Count", new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(400f, 200f));
                UIShape lBar = UIKit.Tile(lGroup, "Bar", lGo ? UIKit.WHITE : UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2(0f, -62f), new Vector2(lGo ? 300f : 170f, 16f), 0.6f);
                TextMeshProUGUI lText = UIKit.Text(lGroup, lSteps[i], lGo ? 130f : 120f, lGo ? UIKit.GOLD : UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(400f, 160f), UIKit.Font.Heavy);
                UIFactory.Outline(lText, UIKit.INK, 0.2f);
                AudioManager.Play(lGo ? "sfx_go" : "sfx_count", 1f);
                if (lGo)
                {
                    Juice.Flash(Color.white, 0.4f);
                    pOnGo?.Invoke();
                }
                float lDuration = lGo ? 0.8f : 0.75f;
                for (float lTime = 0f; lTime < lDuration; lTime += Time.unscaledDeltaTime)
                {
                    float lIn = Mathf.Clamp01(lTime / 0.1f);
                    lGroup.localScale = Vector3.one * (lIn < 1f ? Mathf.Lerp(1.8f, 1f, lIn) : 1f);
                    float lAlpha = lTime > lDuration - 0.2f ? (lDuration - lTime) / 0.2f : 1f;
                    lText.alpha = lAlpha;
                    lBar.color = new Color(lBar.color.r, lBar.color.g, lBar.color.b, lAlpha);
                    lBar.rectTransform.sizeDelta = new Vector2((lGo ? 300f : 170f) * (1f - lTime / lDuration * 0.5f), 16f);
                    yield return null;
                }
                Destroy(lGroup.gameObject);
            }
        }

        // ---------------------------------------------------------------- end screens

        private RectTransform EndPanel(string pName, string pWord, Color pWordColor)
        {
            RectTransform lPanel = UIFactory.Stretch(UIFactory.CreateRect(_root, pName, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            UIKit.FullScreen(lPanel, "Dim", new Color(0f, 0f, 0f, 0.35f));
            UIShape lRed = UIKit.Tile(lPanel, "Red", UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2(-470f, 0f), new Vector2(1300f, 1300f), 0.15f);
            UIKit.Chevrons(lRed.transform, "Chevrons", UIKit.RED, new Color(0.8f, 0.04f, 0.09f), 60f);
            lRed.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            UIKit.OutlineText(lPanel, pWord, 260f, pWordColor, new Vector2(0.5f, 0.5f), new Vector2(-420f, 330f), new Vector2(1800f, 300f), -4f, 0.07f);
            UIKit.Checker(lPanel, "Checker", new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(2200f, 44f), UIKit.WHITE, UIKit.RED_DARK, 40f);
            lPanel.gameObject.AddComponent<UISlideIn>();
            return lPanel;
        }

        private NavTile Button(Transform pParent, string pLabel, Vector2 pPosition, Vector2 pSize, Color pNormal, Color pText)
        {
            RectTransform lHolder = UIFactory.CreateRect(pParent, pLabel, new Vector2(0.5f, 0.5f), pPosition, pSize);
            UIShape lFrame = UIKit.Tile(lHolder, "Frame", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(10f, -8f), pSize + new Vector2(14f, 14f), 0.3f);
            UIShape lBack = UIKit.Tile(lHolder, "Back", pNormal, new Vector2(0.5f, 0.5f), Vector2.zero, pSize, 0.3f);
            TextMeshProUGUI lLabel = UIKit.Text(lHolder, pLabel, 48f, pText, new Vector2(0.5f, 0.5f), Vector2.zero, pSize, UIKit.Font.Heavy);
            NavTile lTile = lHolder.gameObject.AddComponent<NavTile>();
            lTile.back = lBack;
            lTile.frame = lFrame;
            lTile.label = lLabel;
            lTile.normalBack = pNormal;
            lTile.selectedBack = UIKit.INK;
            lTile.normalText = pText;
            lTile.selectedText = UIKit.WHITE;
            lTile.SetSelected(false);
            return lTile;
        }

        public void ShowResult(bool pWin, string pHeadline, float pTime, int pRank, bool pRecord, float pBest, bool pHasNext,
                               System.Action pNext, System.Action pRetry, System.Action pMenu)
        {
            RectTransform lPanel = EndPanel("Result", pWin ? "VICTORY" : pHeadline, pWin ? UIKit.WHITE : UIKit.GOLD);
            UIShape lTimeTile = UIKit.Tile(lPanel, "Time", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-560f, 120f), new Vector2(720f, 110f), 0.3f);
            UIKit.Text(lTimeTile.transform, "TIME   " + GameSession.FormatTime(pTime), 56f, UIKit.INK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 110f), UIKit.Font.Thin);
            string lBestLine = pRecord ? "NEW RECORD!" : "BEST  " + GameSession.FormatTime(pBest);
            UIKit.Text(lPanel, lBestLine, 36f, pRecord ? UIKit.GOLD : UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-560f, 40f), new Vector2(700f, 50f), UIKit.Font.Heavy, TextAlignmentOptions.Left);

            if (pWin)
            {
                RectTransform lRank = UIFactory.CreateRect(lPanel, "Rank", new Vector2(0.5f, 0.5f), new Vector2(520f, 40f), new Vector2(500f, 500f));
                UIShape lCard = UIFactory.CreateShape(lRank, "Card", UIShape.ShapeKind.Chevron, UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 380f));
                lCard.skew = 0.4f;
                UIKit.Icon(lRank, "trophy", UIKit.GOLD, new Vector2(0.5f, 0.5f), new Vector2(-110f, 30f), 180f);
                TextMeshProUGUI lLetter = UIKit.Text(lRank, GameSession.RankLetter(pRank), 260f, UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2(90f, 10f), new Vector2(300f, 320f), UIKit.Font.Heavy);
                lLetter.gameObject.AddComponent<UIJitter>().amount = 2f;
                UIKit.Text(lRank, "RANK", 40f, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(300f, 50f), UIKit.Font.Heavy);
            }

            UINav lNav = UINav.Create(lPanel.gameObject);
            float lY = -110f;
            if (pWin && pHasNext)
            {
                NavTile lNext = Button(lPanel, "NEXT STAGE", new Vector2(-600f, lY), new Vector2(560f, 100f), UIKit.WHITE, UIKit.INK);
                lNav.Add((RectTransform)lNext.transform, new Vector2(0f, -lY), pNext, lNext.SetSelected);
                lY -= 125f;
            }
            NavTile lRetry = Button(lPanel, "RETRY", new Vector2(-620f, lY), new Vector2(520f, 100f), UIKit.WHITE, UIKit.INK);
            lNav.Add((RectTransform)lRetry.transform, new Vector2(0f, -lY), pRetry, lRetry.SetSelected);
            lY -= 125f;
            NavTile lMenu = Button(lPanel, "STAGE SELECT", new Vector2(-640f, lY), new Vector2(520f, 100f), new Color(0.3f, 0.3f, 0.32f), UIKit.WHITE);
            lNav.Add((RectTransform)lMenu.transform, new Vector2(0f, -lY), pMenu, lMenu.SetSelected);
            lNav.Select(0);
        }

        public GameObject ShowPause(System.Action pResume, System.Action pRetry, System.Action pMenu)
        {
            RectTransform lPanel = EndPanel("Pause", "PAUSE", UIKit.WHITE);
            NavTile lResume = Button(lPanel, "RESUME", new Vector2(-600f, 80f), new Vector2(520f, 100f), UIKit.WHITE, UIKit.INK);
            NavTile lRetry = Button(lPanel, "RESTART", new Vector2(-620f, -45f), new Vector2(520f, 100f), UIKit.WHITE, UIKit.INK);
            NavTile lMenu = Button(lPanel, "STAGE SELECT", new Vector2(-640f, -170f), new Vector2(520f, 100f), new Color(0.3f, 0.3f, 0.32f), UIKit.WHITE);
            UINav lNav = UINav.Create(lPanel.gameObject);
            lNav.Add((RectTransform)lResume.transform, new Vector2(0f, 1f), pResume, lResume.SetSelected);
            lNav.Add((RectTransform)lRetry.transform, new Vector2(0f, 0f), pRetry, lRetry.SetSelected);
            lNav.Add((RectTransform)lMenu.transform, new Vector2(0f, -1f), pMenu, lMenu.SetSelected);
            lNav.onBack = pResume;
            lNav.Select(0);
            return lPanel.gameObject;
        }
    }

    public class UISpin : MonoBehaviour
    {
        public float speed = 20f;
        private void Update() => transform.Rotate(0f, 0f, speed * Time.unscaledDeltaTime);
    }

    public class UIJitter : MonoBehaviour
    {
        public float amount = 3f;
        private Vector2 _base;
        private bool _init;

        private void Update()
        {
            RectTransform lRect = (RectTransform)transform;
            if (!_init) { _base = lRect.anchoredPosition; _init = true; }
            if (Time.frameCount % 3 == 0) lRect.anchoredPosition = _base + Random.insideUnitCircle * amount;
        }
    }

    public class UISlideIn : MonoBehaviour
    {
        private float _time;

        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            float lT = 1f - Mathf.Pow(1f - Mathf.Clamp01(_time / 0.35f), 3f);
            ((RectTransform)transform).anchoredPosition = new Vector2(Mathf.Lerp(-1920f, 0f, lT), 0f);
            if (lT >= 1f) enabled = false;
        }
    }
}
