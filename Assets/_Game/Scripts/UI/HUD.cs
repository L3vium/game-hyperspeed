using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Race HUD : timer at the top, combo and event feed under it, progress on the right edge, speed and boost on planes
    // tilted in perspective at the bottom corners, rival pointer in the world.
    public class HUD : MonoBehaviour
    {
        private class FeedEntry
        {
            public RectTransform rect;
            public CanvasGroup group;
            public float time;
            public float targetY;
        }

        private const int FEED_MAX = 3;
        private const float RAIL_HALF = 330f;

        private Canvas _canvas;
        private RectTransform _root;
        private Material _speedLinesMaterial;
        private float _lineIntensity;

        private TextMeshProUGUI _timerText;
        private Canvas _canvas3D;
        private UIShape _speedFill;
        private TextMeshProUGUI _speedUnit;
        private RectTransform _boost;
        private UIShape _boostFill;
        private TextMeshProUGUI _boostWord;
        private UI3DText _countWord;
        private float _countTimer;
        private TextMeshProUGUI _rivalLabel;
        private RectTransform _combo;
        private CanvasGroup _comboGroup;
        private TextMeshProUGUI _comboText;
        private UIShape _comboBar;
        private int _shownCombo;
        private TextMeshProUGUI _speedText;
        private RectTransform _speedo;
        private RectTransform _feedRoot;
        private readonly List<FeedEntry> _feed = new List<FeedEntry>();
        private TextMeshProUGUI _bigText;
        private float _bigTimer;

        private RectTransform _rail;
        private RectTransform _playerMarker;
        private RectTransform _rivalMarker;
        private RectTransform _rivalPointer;
        private TextMeshProUGUI _warningText;
        private RectTransform _hintPanel;
        private TextMeshProUGUI _hintText;
        private int _hintIndex;
        private float _hintTimer;

        private PlayerController _player;
        private Rival _rival;
        private LevelLayout _path;
        private LevelDefinition _level;

        public Transform Root => _root;

        public static HUD Create(PlayerController pPlayer, Rival pRival, LevelLayout pPath, LevelDefinition pLevel)
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

            // Timer (top center).
            UIKit.Tile(_root, "TimerBack", new Color(0f, 0f, 0f, 0.55f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(400f, 76f), 0.45f);
            UIKit.Tile(_root, "TimerAccentL", UIKit.RED, new Vector2(0.5f, 1f), new Vector2(-208f, -56f), new Vector2(16f, 76f), 0.45f);
            UIKit.Tile(_root, "TimerAccentR", UIKit.CYAN, new Vector2(0.5f, 1f), new Vector2(208f, -56f), new Vector2(16f, 76f), 0.45f);
            _timerText = UIKit.Text(_root, "00:00.00", 56f, UIKit.WHITE, new Vector2(0.5f, 1f), new Vector2(4f, -54f), new Vector2(380f, 76f), UIKit.Font.Heavy);

            // Combo counter + event feed (center top).
            _combo = UIFactory.CreateRect(_root, "Combo", new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(420f, 120f));
            _comboGroup = _combo.gameObject.AddComponent<CanvasGroup>();
            _comboText = UIKit.Text(_combo, "", 76f, UIKit.GOLD, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(420f, 100f), UIKit.Font.Heavy);
            UIFactory.Outline(_comboText, UIKit.INK, 0.25f);
            _comboText.gameObject.AddComponent<UISpeedGhost>();
            UIKit.Tile(_combo, "BarBack", new Color(0f, 0f, 0f, 0.45f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(240f, 10f), 0.6f);
            _comboBar = UIKit.Tile(_combo, "Bar", UIKit.GOLD, new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(240f, 10f), 0.6f);
            _comboGroup.alpha = 0f;
            _feedRoot = UIFactory.CreateRect(_root, "Feed", new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(420f, 300f));

            _bigText = UIKit.OutlineText(_root, "", 150f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1700f, 200f), -4f, 0.12f);
            _bigText.enableAutoSizing = true;
            _bigText.fontSizeMin = 40f;
            _bigText.fontSizeMax = 150f;
            _bigText.gameObject.SetActive(false);

            // Vertical progress rail (right edge) : start at the bottom, flag at the top.
            _rail = UIFactory.CreateRect(_root, "Rail", new Vector2(1f, 0.5f), new Vector2(-58f, 30f), new Vector2(8f, RAIL_HALF * 2f));
            UIFactory.CreateImage(_rail, "Line", new Color(1f, 1f, 1f, 0.35f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, RAIL_HALF * 2f));
            UIKit.Icon(_rail, "flag", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(0f, RAIL_HALF + 26f), 40f);
            _rivalMarker = Marker("Rival", UIKit.RED, RivalShortName());
            _playerMarker = Marker("Player", UIKit.CYAN, "");
            _rivalLabel = _rivalMarker.GetComponentInChildren<TextMeshProUGUI>();

            BuildGauges();

            // Rival pointer in the world
            _rivalPointer = UIFactory.CreateRect(_root, "RivalPointer", new Vector2(0f, 0f), Vector2.zero, new Vector2(60f, 60f));
            UIFactory.CreateShape(_rivalPointer, "Arrow", UIShape.ShapeKind.Triangle, UIKit.RED, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 34f), 180f);

            _warningText = UIKit.Text(_root, "", 40f, UIKit.RED, new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(900f, 50f), UIKit.Font.Heavy);
            UIFactory.Outline(_warningText, UIKit.INK, 0.3f);

            // Hints (bottom center, small white tile)
            _hintPanel = UIFactory.CreateRect(_root, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(920f, 56f));
            UIKit.Tile(_hintPanel, "Back", UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 52f), 0.45f);
            _hintText = UIKit.Text(_hintPanel, "", 28f, UIKit.INK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850f, 52f), UIKit.Font.Heavy);
            _hintText.enableAutoSizing = true;
            _hintText.fontSizeMin = 14f;
            _hintText.fontSizeMax = 28f;
            _hintPanel.gameObject.SetActive(false);
        }

        // Speed and boost on planes tilted in real perspective, bottom left and bottom right, leaning with the action.
        private void BuildGauges()
        {
            Camera lGame = CameraRig.Instance != null ? CameraRig.Instance.Cam : Camera.main;
            _canvas3D = UIPerspective.Create("HUD3D", 9, transform, lGame);
            RectTransform lRoot3D = (RectTransform)_canvas3D.transform;

            _speedo = UIFactory.CreateRect(lRoot3D, "Speed", new Vector2(0f, 0f), new Vector2(340f, 118f), new Vector2(560f, 170f));
            _speedo.localRotation = Quaternion.Euler(38f, -26f, 9f);
            UIKit.Tile(_speedo, "Plate", new Color(0f, 0f, 0f, 0.42f), new Vector2(0.5f, 0.5f), new Vector2(40f, -46f), new Vector2(520f, 62f), 0.6f);
            _speedFill = UIKit.Tile(_speedo, "Fill", UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(40f, -46f), new Vector2(10f, 54f), 0.6f);
            _speedText = UIKit.Text(_speedo, "0", 118f, UIKit.RED, new Vector2(0f, 0.5f), new Vector2(150f, 30f), new Vector2(300f, 130f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            UIFactory.Outline(_speedText, UIKit.INK, 0.2f);
            _speedUnit = UIKit.Text(_speedo, "km/h", 34f, UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(420f, 8f), new Vector2(120f, 40f), UIKit.Font.Bold, TextAlignmentOptions.Left);

            _boost = UIFactory.CreateRect(lRoot3D, "Boost", new Vector2(1f, 0f), new Vector2(-410f, 118f), new Vector2(560f, 170f));
            _boost.localRotation = Quaternion.Euler(38f, 26f, -9f);
            UIKit.Tile(_boost, "Plate", new Color(0f, 0f, 0f, 0.42f), new Vector2(0.5f, 0.5f), new Vector2(-40f, -46f), new Vector2(520f, 62f), -0.6f);
            _boostFill = UIKit.Tile(_boost, "Fill", UIKit.CYAN, new Vector2(1f, 0.5f), new Vector2(-40f, -46f), new Vector2(10f, 54f), -0.6f);
            _boostWord = UIKit.Text(_boost, "", 76f, UIKit.CYAN, new Vector2(1f, 0.5f), new Vector2(-180f, 26f), new Vector2(340f, 90f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            UIFactory.Outline(_boostWord, UIKit.INK, 0.2f);

            _countWord = UI3DText.Create(lRoot3D, " ", 300f, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(900f, 320f), UIKit.WHITE, UIKit.RED_DARK, 14, 3.4f);
            _countWord.gameObject.SetActive(false);
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
            TextMeshProUGUI lText = UIKit.Text(lMarker, pLabel, 22f, pColor, new Vector2(0.5f, 0.5f), new Vector2(-150f, 0f), new Vector2(200f, 30f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            UIFactory.Outline(lText, UIKit.INK, 0.3f);
            return lMarker;
        }

        private float RailY(float pDistance)
        {
            float lFinish = Mathf.Max(1f, _path.finishS);
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
                float lTarget = 0.08f + _player.Speed01 * 0.8f + (_player.Hyper ? 0.1f : 0f) + (_player.LightForm ? 0.15f : 0f) + (_player.Dashing ? 0.2f : 0f) + _player.BoostAmount * 0.12f;
                _lineIntensity = Mathf.Lerp(_lineIntensity, Mathf.Clamp01(lTarget), 1f - Mathf.Exp(-7f * lDt));
                _speedLinesMaterial.SetFloat("_Intensity", _player.State == PlayerState.Frozen ? 0f : _lineIntensity);
                _speedLinesMaterial.SetFloat("_Boost", _player.BoostAmount);
                _speedLinesMaterial.SetColor("_LineColor", _player.Hyper ? new Color(0.8f, 1f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.8f));
                UpdateRail();
                UpdateRivalPointer();
                UpdateHints(lDt);
                UpdateCombo(lDt);
                UpdateSpeedo(lDt);
            }
            UpdateFeed(lDt);
            UpdateCount(lDt);
            if (_bigTimer > 0f)
            {
                _bigTimer -= lDt;
                float lT = 1f - _bigTimer / 1.1f;
                _bigText.rectTransform.localScale = Vector3.one * (lT < 0.12f ? Mathf.Lerp(1.8f, 1f, lT / 0.12f) : 1f + (lT - 0.12f) * 0.08f);
                _bigText.alpha = _bigTimer < 0.3f ? _bigTimer / 0.3f : 1f;
                if (_bigTimer <= 0f) _bigText.gameObject.SetActive(false);
            }
        }

        private void UpdateCombo(float pDt)
        {
            int lCombo = _player.Combo;
            bool lShow = lCombo >= 2 && RaceContext.Running;
            _comboGroup.alpha = Mathf.MoveTowards(_comboGroup.alpha, lShow ? 1f : 0f, pDt * 6f);
            if (lCombo != _shownCombo)
            {
                if (lCombo > _shownCombo && lCombo >= 2)
                {
                    _comboText.text = "x" + lCombo + "<size=40%> COMBO</size>";
                    UITween.Kill(_combo);
                    UITween.Punch(_combo, 0.3f, 0.3f);
                    _comboText.color = lCombo >= 10 ? UIKit.CYAN : lCombo >= 5 ? UIKit.GOLD : UIKit.WHITE;
                }
                _shownCombo = lCombo;
            }
            _comboBar.rectTransform.sizeDelta = new Vector2(240f * _player.ComboTimer01, 10f);
        }

        private float _shownSpeed;
        private float _shownBoost;

        private void UpdateSpeedo(float pDt)
        {
            float lKmh = _player.Speed * 3.6f;
            _shownSpeed = Mathf.Lerp(_shownSpeed, lKmh, 1f - Mathf.Exp(-10f * pDt));
            _speedText.text = Mathf.RoundToInt(_shownSpeed).ToString();
            _speedText.color = _player.LightForm ? UIKit.WHITE : _player.Hyper ? UIKit.CYAN : _player.BoostAmount > 0f ? UIKit.GOLD : UIKit.RED;
            float lFill = Mathf.Clamp01(_player.Speed / _player.Profile.max);
            _speedFill.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(10f, 500f, lFill), 54f);
            _speedFill.rectTransform.anchoredPosition = new Vector2(40f + _speedFill.rectTransform.sizeDelta.x * 0.5f + 25f, -46f);
            _speedFill.color = _player.LightForm ? UIKit.WHITE : _player.Hyper ? UIKit.CYAN : UIKit.GOLD;

            _shownBoost = Mathf.MoveTowards(_shownBoost, _player.BoostAmount, pDt * 4f);
            _boostFill.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(0f, 500f, _shownBoost), 54f);
            _boostFill.rectTransform.anchoredPosition = new Vector2(-40f - _boostFill.rectTransform.sizeDelta.x * 0.5f - 25f, -46f);
            _boostWord.text = _player.LightForm ? "LIGHT" : _player.Hyper ? "HYPER" : _player.BoostAmount > 0f ? "BOOST" : "";
            _boostWord.color = _player.LightForm ? UIKit.WHITE : UIKit.CYAN;

            // The gauges shiver and rock with the speed.
            float lShake = _player.Speed01 * _player.Speed01;
            float lWave = Mathf.Sin(Time.unscaledTime * 3f) * 1.5f * lShake;
            Vector2 lTilt = UIMouse.Tilt;
            _speedo.localRotation = Quaternion.Euler(38f + lTilt.y * 3f, -26f - lTilt.x * 4f + lWave, 9f + _player.Steer * 3f);
            _boost.localRotation = Quaternion.Euler(38f + lTilt.y * 3f, 26f - lTilt.x * 4f - lWave, -9f + _player.Steer * 3f);
            _speedo.anchoredPosition = new Vector2(340f, 118f) + (_player.Hyper ? Random.insideUnitCircle * 3f * lShake : Vector2.zero);
            _boost.anchoredPosition = new Vector2(-410f, 118f) + (_player.Hyper ? Random.insideUnitCircle * 3f * lShake : Vector2.zero);
        }

        private void UpdateRail()
        {
            if (_path == null || _path.finishS <= 0f) return;
            _playerMarker.anchoredPosition = new Vector2(0f, RailY(_player.Progress));
            if (_rival == null) return;
            _rivalMarker.anchoredPosition = new Vector2(0f, RailY(Mathf.Max(0f, _rival.distance)));
            // Markers side by side on the rail : the rival label steps aside so both names stay readable.
            float lDy = _rivalMarker.anchoredPosition.y - _playerMarker.anchoredPosition.y;
            float lPush = Mathf.Abs(lDy) < 32f ? (lDy >= 0f ? 1f : -1f) * (32f - Mathf.Abs(lDy)) : 0f;
            _rivalLabel.rectTransform.anchoredPosition = new Vector2(-150f, lPush);
            float lGap = _player.Progress - _rival.distance;
            if (_rival.chaser)
            {
                bool lDanger = lGap < 160f * _player.Profile.k && RaceContext.Running;
                _warningText.text = lDanger ? RivalShortName() + " IS RIGHT BEHIND YOU" : "";
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
        }

        private void UpdateHints(float pDt)
        {
            if (_hintIndex < _path.hints.Count && _player.Progress >= _path.hints[_hintIndex].Key - 10f && _player.Progress >= -40f)
            {
                string lHint = _path.hints[_hintIndex].Value;
                _hintIndex++;
                if (string.IsNullOrEmpty(lHint)) return;
                _hintText.text = lHint;
                _hintTimer = 4f;
                _hintPanel.gameObject.SetActive(true);
                _hintPanel.anchoredPosition = new Vector2(-1500f, 62f);
                AudioManager.Play("ui_hint", 0.5f);
            }
            if (_hintTimer > 0f)
            {
                _hintTimer -= pDt;
                float lTargetX = _hintTimer > 0.4f ? 0f : 1700f;
                _hintPanel.anchoredPosition = Vector2.Lerp(_hintPanel.anchoredPosition, new Vector2(lTargetX, 62f), 1f - Mathf.Exp(-14f * pDt));
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

            RectTransform lEntry = UIFactory.CreateRect(_feedRoot, "Entry", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 54f));
            CanvasGroup lGroup = lEntry.gameObject.AddComponent<CanvasGroup>();
            bool lDark = pColor == UIKit.WHITE || pColor == Color.white;
            UIKit.Tile(lEntry, "Back", lDark ? new Color(0f, 0f, 0f, 0.6f) : UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 48f), 0.45f);
            UIKit.Tile(lEntry, "Accent", pColor, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(14f, 48f), 0.45f);
            TextMeshProUGUI lText = UIKit.Text(lEntry, pText, 32f, lDark ? UIKit.WHITE : UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(320f, 48f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            lText.enableAutoSizing = true;
            lText.fontSizeMin = 16f;
            lText.fontSizeMax = 32f;
            lEntry.anchoredPosition = new Vector2(0f, 40f);
            lEntry.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-4f, 4f));
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
                Vector2 lTarget = new Vector2(0f, -i * 48f);
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

        // ---------------------------------------------------------------- countdown

        // Big word in real 3D, leaning with the mouse.
        public void ShowCount(string pText, bool pGo)
        {
            _countWord.gameObject.SetActive(true);
            _countWord.SetText(pText);
            _countWord.SetFace(pGo ? UIKit.GOLD : UIKit.WHITE);
            _countWord.SetAlpha(1f);
            _countWord.punch = pGo ? 1.6f : 1.1f;
            _countTimer = pGo ? 0.9f : 1.4f;
        }

        private void UpdateCount(float pDt)
        {
            if (_countTimer <= 0f) return;
            _countTimer -= pDt;
            _countWord.SetAlpha(Mathf.Clamp01(_countTimer / 0.25f));
            if (_countTimer <= 0f) _countWord.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- end screens

        private RectTransform EndPanel(string pName, string pWord, Color pWordColor)
        {
            RectTransform lPanel = UIFactory.Stretch(UIFactory.CreateRect(_root, pName, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            UIKit.FullScreen(lPanel, "Dim", new Color(0f, 0f, 0f, 0.35f));
            UIShape lRed = UIKit.Tile(lPanel, "Red", UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2(-470f, 0f), new Vector2(1300f, 1300f), 0.15f);
            UIKit.Chevrons(lRed.transform, "Chevrons", UIKit.RED, new Color(0.8f, 0.04f, 0.09f), 60f);
            lRed.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            TextMeshProUGUI lWord = UIKit.Text(lPanel, pWord, 200f, pWordColor, new Vector2(0.5f, 1f), new Vector2(-340f, -150f), new Vector2(1100f, 220f), UIKit.Font.Heavy);
            lWord.enableAutoSizing = true;
            lWord.fontSizeMin = 60f;
            lWord.fontSizeMax = 200f;
            UIKit.Checker(lPanel, "Checker", new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(2200f, 44f), UIKit.WHITE, UIKit.RED_DARK, 40f);
            lPanel.gameObject.AddComponent<UISlideIn>();
            return lPanel;
        }

        private NavTile Button(Transform pParent, string pLabel, Vector2 pPosition, Vector2 pSize, Color pNormal, Color pText) => Button(pParent, pLabel, new Vector2(0.5f, 0.5f), pPosition, pSize, pNormal, pText);

        private NavTile Button(Transform pParent, string pLabel, Vector2 pAnchor, Vector2 pPosition, Vector2 pSize, Color pNormal, Color pText)
        {
            RectTransform lHolder = UIFactory.CreateRect(pParent, pLabel, pAnchor, pPosition, pSize);
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
                lNav.Add((RectTransform)lNext.transform, new Vector2(0f, lY), pNext, lNext.SetSelected);
                lY -= 125f;
            }
            NavTile lRetry = Button(lPanel, "RETRY", new Vector2(-620f, lY), new Vector2(520f, 100f), UIKit.WHITE, UIKit.INK);
            lNav.Add((RectTransform)lRetry.transform, new Vector2(0f, lY), pRetry, lRetry.SetSelected);
            lY -= 125f;
            NavTile lMenu = Button(lPanel, "STAGE SELECT", new Vector2(-640f, lY), new Vector2(520f, 100f), new Color(0.3f, 0.3f, 0.32f), UIKit.WHITE);
            lNav.Add((RectTransform)lMenu.transform, new Vector2(0f, lY), pMenu, lMenu.SetSelected);
            lNav.Select(0);
        }

        public GameObject ShowPause(System.Action pResume, System.Action pRetry, System.Action pMenu)
        {
            Canvas lCanvas = UIFactory.CreateCanvas("PauseCanvas", 500, transform);
            RectTransform lPanel = UIFactory.Stretch(UIFactory.CreateRect(lCanvas.transform, "Pause", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            UIKit.FullScreen(lPanel, "Dim", new Color(0f, 0f, 0f, 0.25f));
            UIKit.Tile(lPanel, "Strip", new Color(UIKit.RED.r, UIKit.RED.g, UIKit.RED.b, 0.88f), new Vector2(0f, 0.5f), new Vector2(330f, 0f), new Vector2(620f, 1400f), 0.12f);
            UIKit.Text(lPanel, "PAUSE", 170f, UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(340f, 280f), new Vector2(620f, 200f), UIKit.Font.Heavy);
            
            NavTile lResume = Button(lPanel, "RESUME", new Vector2(0f, 0.5f), new Vector2(320f, 80f), new Vector2(480f, 96f), UIKit.WHITE, UIKit.INK);
            NavTile lRetry = Button(lPanel, "RESTART", new Vector2(0f, 0.5f), new Vector2(305f, -40f), new Vector2(480f, 96f), UIKit.WHITE, UIKit.INK);
            NavTile lMenu = Button(lPanel, "STAGE SELECT", new Vector2(0f, 0.5f), new Vector2(290f, -160f), new Vector2(480f, 96f), new Color(0.3f, 0.3f, 0.32f), UIKit.WHITE);
            UINav lNav = UINav.Create(lPanel.gameObject);
            lNav.Add((RectTransform)lResume.transform, new Vector2(0f, 1f), pResume, lResume.SetSelected);
            lNav.Add((RectTransform)lRetry.transform, new Vector2(0f, 0f), pRetry, lRetry.SetSelected);
            lNav.Add((RectTransform)lMenu.transform, new Vector2(0f, -1f), pMenu, lMenu.SetSelected);
            lNav.onBack = pResume;
            lNav.Select(0);
            return lCanvas.gameObject;
        }
    }

    // Slides a rect in from an offset (unscaled time, ease out cubic).
    public class UISlideFrom : MonoBehaviour
    {
        public Vector2 from;
        private Vector2 _to;
        private float _time;
        private bool _init;

        private void Update()
        {
            RectTransform lRect = (RectTransform)transform;
            if (!_init) { _to = lRect.anchoredPosition; _init = true; }
            _time += Time.unscaledDeltaTime;
            float lT = 1f - Mathf.Pow(1f - Mathf.Clamp01(_time / 0.3f), 3f);
            lRect.anchoredPosition = Vector2.LerpUnclamped(from, _to, lT);
            if (lT >= 1f) enabled = false;
        }
    }

    public class UIBlink : MonoBehaviour
    {
        private TextMeshProUGUI _text;

        private void Update()
        {
            if (_text == null) _text = GetComponent<TextMeshProUGUI>();
            _text.alpha = 0.55f + Mathf.Sin(Time.unscaledTime * 5f) * 0.45f;
        }
    }

    // One countdown step : punch in, shrink the bar, fade out. Scaled time, so it freezes with the pause.
    public class UICountStep : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public UIShape bar;
        public float duration = 1f;
        private float _time;
        private float _barWidth;

        private void Start() => _barWidth = bar.rectTransform.sizeDelta.x;

        private void Update()
        {
            _time += Time.deltaTime;
            float lIn = Mathf.Clamp01(_time / 0.12f);
            transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, 1f - Mathf.Pow(1f - lIn, 3f));
            float lAlpha = _time > duration - 0.2f ? Mathf.Clamp01((duration - _time) / 0.2f) : 1f;
            text.alpha = lAlpha;
            bar.color = new Color(bar.color.r, bar.color.g, bar.color.b, lAlpha);
            bar.rectTransform.sizeDelta = new Vector2(_barWidth * (1f - Mathf.Clamp01(_time / duration) * 0.6f), bar.rectTransform.sizeDelta.y);
            if (_time >= duration) Destroy(gameObject);
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
