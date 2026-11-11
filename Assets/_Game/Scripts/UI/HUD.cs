using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Race HUD, kept out of the way : speed on a plane tilted in perspective bottom left with the dash pips under it, the
    // time and a thin progress line at the top, a pointer to the rival. Nothing in the middle of the screen.
    public class HUD : MonoBehaviour
    {
        private const float RAIL_HALF = 230f;

        private Canvas _canvas;
        private RectTransform _root;
        private Material _speedLinesMaterial;
        private float _lineIntensity;

        private TextMeshProUGUI _timerText;
        private Canvas _canvas3D;
        private UIShape _speedFill;
        private TextMeshProUGUI _speedUnit;
        private UIShape[] _dashPips;
        private TextMeshProUGUI _tag;
        private float _tagTimer;
        private UI3DText _countWord;
        private float _countTimer;
        private TextMeshProUGUI _speedText;
        private RectTransform _speedo;

        private RectTransform _rail;
        private RectTransform _playerMarker;
        private RectTransform _rivalMarker;
        private RectTransform _rivalPointer;
        private UIBanner _notify;
        private UIBanner _hint;
        private UIBanner _wrongWay;
        private float _wrongWayTimer;
        // A callout that must be read (PERFECT START) holds the top slot ; the next one waits its turn.
        private float _notifyHold;
        private string _pendingText;
        private Color _pendingColor;
        private float _lastHyperNotice = -100f;
        private Canvas _endCanvas;
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
            pPlayer.OnPerfectStart += () => lHud.Notify("PERFECT START", UIKit.GOLD, 1.6f);
            pPlayer.OnHyper += () =>
            {
                if (Time.unscaledTime - lHud._lastHyperNotice < 8f) return;
                lHud._lastHyperNotice = Time.unscaledTime;
                lHud.Notify("HYPERSPEED", UIKit.CYAN);
            };
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

            // Timer and a thin progress line (top center).
            _timerText = UIKit.Text(_root, "00:00.00", 38f, UIKit.WHITE, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(300f, 50f), UIKit.Font.Heavy);
            UIFactory.Outline(_timerText, UIKit.INK, 0.25f);
            _rail = UIFactory.CreateRect(_root, "Rail", new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(RAIL_HALF * 2f, 6f));
            UIFactory.CreateImage(_rail, "Line", new Color(1f, 1f, 1f, 0.3f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(RAIL_HALF * 2f, 3f));
            _rivalMarker = Marker("Rival", UIKit.RED);
            _playerMarker = Marker("Player", UIKit.WHITE);

            BuildGauges();

            // Rival pointer in the world
            _rivalPointer = UIFactory.CreateRect(_root, "RivalPointer", new Vector2(0f, 0f), Vector2.zero, new Vector2(40f, 40f));
            UIFactory.CreateShape(_rivalPointer, "Arrow", UIShape.ShapeKind.Triangle, UIKit.RED, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 22f), 180f);


        }

        // Speed on a plane tilted in real perspective, bottom left, leaning with the action.
        private void BuildGauges()
        {
            Camera lGame = CameraRig.Instance != null ? CameraRig.Instance.Cam : Camera.main;
            _canvas3D = UIPerspective.Create("HUD3D", 9, transform, lGame);
            RectTransform lRoot3D = (RectTransform)_canvas3D.transform;

            _speedo = UIFactory.CreateRect(lRoot3D, "Speed", new Vector2(0f, 0f), new Vector2(390f, 160f), new Vector2(560f, 170f));
            _speedo.localRotation = Quaternion.Euler(38f, -26f, 9f);
            UIKit.Tile(_speedo, "Plate", new Color(0f, 0f, 0f, 0.42f), new Vector2(0.5f, 0.5f), new Vector2(40f, -46f), new Vector2(520f, 62f), 0.6f);
            _speedFill = UIKit.Tile(_speedo, "Fill", UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(40f, -46f), new Vector2(10f, 54f), 0.6f);
            _speedText = UIKit.Text(_speedo, "0", 118f, UIKit.RED, new Vector2(0f, 0.5f), new Vector2(150f, 30f), new Vector2(300f, 130f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            UIFactory.Outline(_speedText, UIKit.INK, 0.2f);
            _speedUnit = UIKit.Text(_speedo, "km/h", 34f, UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(420f, 8f), new Vector2(120f, 40f), UIKit.Font.Bold, TextAlignmentOptions.Left);

            // Dash energy : three thin pips under the speed.
            _dashPips = new UIShape[3];
            for (int i = 0; i < 3; i++)
                _dashPips[i] = UIKit.Tile(_speedo, "Pip" + i, UIKit.CYAN, new Vector2(0f, 0.5f), new Vector2(66f + i * 98f, -94f), new Vector2(90f, 14f), 0.6f);
            _tag = UIKit.Text(_speedo, "", 30f, UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(330f, -94f), new Vector2(300f, 36f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            UIFactory.Outline(_tag, UIKit.INK, 0.25f);

            _countWord = UI3DText.Create(lRoot3D, " ", 300f, new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(900f, 320f), UIKit.WHITE, UIKit.RED_DARK, 22, 5f);
            _countWord.gameObject.SetActive(false);

            // Big callouts at the top centre, instructions at the bottom : anime style, in 3D.
            _notify = UIBanner.Create(lRoot3D, "Notify", new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(900f, 120f), 96f);
            _hint = UIBanner.Create(lRoot3D, "Hint", new Vector2(0.5f, 0f), new Vector2(260f, 330f), new Vector2(900f, 80f), 50f);
            _wrongWay = UIBanner.Create(lRoot3D, "WrongWay", new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 120f), 90f);
        }

        private string RivalShortName()
        {
            if (_rival == null) return "";
            return RivalFactory.DisplayName(_rival.kind).Replace("THE ", "").Replace("A ", "");
        }

        private RectTransform Marker(string pName, Color pColor)
        {
            RectTransform lMarker = UIFactory.CreateRect(_rail, pName, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 16f));
            UIFactory.CreateShape(lMarker, "Mark", UIShape.ShapeKind.Parallelogram, pColor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 16f)).skew = 0.4f;
            return lMarker;
        }

        private float RailX(float pDistance)
        {
            float lFinish = Mathf.Max(1f, _path.finishS);
            return Mathf.Lerp(-RAIL_HALF, RAIL_HALF, Mathf.Clamp01(pDistance / lFinish));
        }

        public void SetRaceTime(float pTime)
        {
            _timerText.text = GameSession.FormatTime(pTime);
        }

        private float _lineBurst;
        private float _lastBoost;

        private void Update()
        {
            float lDt = Time.unscaledDeltaTime;
            if (_player != null)
            {
                float lTarget = 0.12f + Mathf.Pow(_player.Speed01, 0.8f) * 0.88f + (_player.LightForm ? 0.2f : 0f) + (_player.Dashing ? 0.16f : 0f) + _player.BoostAmount * 0.14f + _player.Dive01 * 0.25f;
                _lineIntensity = Mathf.Lerp(_lineIntensity, Mathf.Clamp01(lTarget), 1f - Mathf.Exp(-7f * lDt));
                // Every new kick : the streaks snap toward the centre, then ease back out over 0.4 s.
                float lBoost = _player.BoostAmount;
                if (lBoost > _lastBoost + 0.05f) _lineBurst = 1f;
                _lastBoost = lBoost;
                _lineBurst = Mathf.MoveTowards(_lineBurst, 0f, lDt / 0.4f);
                _speedLinesMaterial.SetFloat("_Intensity", _player.State == PlayerState.Frozen ? 0f : _lineIntensity);
                _speedLinesMaterial.SetFloat("_Boost", lBoost);
                _speedLinesMaterial.SetFloat("_Burst", _lineBurst * _lineBurst);
                _speedLinesMaterial.SetColor("_LineColor", new Color(0.9f, 0.95f, 1f, 0.95f));
                UpdateRail();
                UpdateRivalPointer();
                UpdateHints(lDt);
                UpdateSpeedo(lDt);
                UpdateWrongWay(lDt);
            }
            UpdateNotify(lDt);
            UpdateCount(lDt);
            if (_tagTimer > 0f)
            {
                _tagTimer -= lDt;
                _tag.alpha = Mathf.Clamp01(_tagTimer / 0.25f);
                if (_tagTimer <= 0f) _tag.text = "";
            }
        }

        private float _shownSpeed;

        private void UpdateSpeedo(float pDt)
        {
            float lKmh = _player.Speed * 3.6f;
            _shownSpeed = Mathf.Lerp(_shownSpeed, lKmh, 1f - Mathf.Exp(-10f * pDt));
            _speedText.text = Mathf.RoundToInt(_shownSpeed).ToString();
            _speedText.color = _player.LightForm ? UIKit.WHITE : _player.Hyper ? UIKit.CYAN : _player.BoostAmount > 0f ? UIKit.GOLD : UIKit.RED;
            float lFill = Mathf.Clamp01(_player.Speed / _player.Profile.lightEnter);
            _speedFill.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(10f, 500f, lFill), 54f);
            _speedFill.rectTransform.anchoredPosition = new Vector2(40f + _speedFill.rectTransform.sizeDelta.x * 0.5f + 25f, -46f);
            _speedFill.color = _player.LightForm ? UIKit.WHITE : _player.Hyper ? UIKit.CYAN : UIKit.GOLD;

            for (int i = 0; i < _dashPips.Length; i++)
            {
                float lCharge = Mathf.Clamp01(_player.DashEnergy - i);
                _dashPips[i].color = lCharge >= 1f ? UIKit.CYAN : new Color(1f, 1f, 1f, 0.15f + lCharge * 0.25f);
            }

            // The gauges shiver and rock with the speed.
            float lShake = _player.Speed01 * _player.Speed01;
            float lWave = Mathf.Sin(Time.unscaledTime * 3f) * 1.5f * lShake;
            Vector2 lTilt = UIMouse.Tilt;
            _speedo.localRotation = Quaternion.Euler(38f + lTilt.y * 3f, -26f - lTilt.x * 4f + lWave, 9f + _player.Steer * 3f);
            _speedo.anchoredPosition = new Vector2(390f, 160f) + (_player.Hyper ? Random.insideUnitCircle * 3f * lShake : Vector2.zero);
        }

        private void UpdateRail()
        {
            if (_path == null || _path.finishS <= 0f) return;
            _playerMarker.anchoredPosition = new Vector2(RailX(_player.Progress), 0f);
            if (_rival == null) return;
            _rivalMarker.anchoredPosition = new Vector2(RailX(Mathf.Max(0f, _rival.distance)), 0f);
            // A chaser right behind you : the edges of the screen say it, no words needed.
            if (_rival.chaser && RaceContext.Running && _player.Progress - _rival.distance < _player.Profile.cruise * 1.2f) Juice.Vignette(UIKit.RED, 0.6f);
        }

        // Points at the rival : marker above it when visible, clamped arrow on the screen edge otherwise.
        private void UpdateRivalPointer()
        {
            Camera lCamera = CameraRig.Instance != null ? CameraRig.Instance.Cam : null;
            if (_rival == null || lCamera == null || (!_rival.chaser && _rival.distance < _player.Progress)) { _rivalPointer.gameObject.SetActive(false); return; }
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
                _hint.Show(lHint, UIKit.CYAN, 3.4f);
                AudioManager.Play("ui_hint", 0.5f);
            }
        }

        // Top centre, big, in 3D : the moments that matter. pHold keeps it on screen : later callouts queue behind it.
        public void Notify(string pText, Color pColor, float pHold = 0f)
        {
            if (_notify == null) return;
            if (_notifyHold > 0f && pHold <= 0f)
            {
                _pendingText = pText;
                _pendingColor = pColor;
                return;
            }
            _notify.Show(pText, pColor, Mathf.Max(1.3f, pHold));
            _notifyHold = pHold;
            AudioManager.Play("ui_hint", 0.7f, 1.2f);
        }

        private void UpdateNotify(float pDt)
        {
            if (_notifyHold <= 0f) return;
            _notifyHold -= pDt;
            if (_notifyHold > 0f || _pendingText == null) return;
            string lText = _pendingText;
            _pendingText = null;
            Notify(lText, _pendingColor);
        }

        // Running against the track : a red callout in the middle, repeated while it lasts, and a beep.
        private void UpdateWrongWay(float pDt)
        {
            _wrongWayTimer -= pDt;
            if (!_player.WrongWay || _wrongWayTimer > 0f) return;
            _wrongWayTimer = 1.2f;
            _wrongWay.Show("WRONG WAY", UIKit.RED, 1.1f);
            AudioManager.Play("sfx_wrong_way", 0.8f);
        }

        // Paused : the race HUD steps aside so nothing covers the pause menu.
        public void SetPaused(bool pPaused)
        {
            if (_canvas != null) _canvas.gameObject.SetActive(!pPaused);
            if (_canvas3D != null) _canvas3D.gameObject.SetActive(!pPaused);
        }

        // A rare word, small, next to the speed.
        private void ShowPopup(string pText, Color pColor, float pScale)
        {
            _tag.text = pText;
            _tag.color = pColor;
            _tag.alpha = 1f;
            _tagTimer = 1.1f;
        }

        // ---------------------------------------------------------------- countdown

        // Big word in real 3D, leaning with the mouse.
        public void ShowCount(string pText, bool pGo)
        {
            _countWord.gameObject.SetActive(true);
            _countWord.SetText(pText);
            _countWord.SetFace(pGo ? UIKit.GOLD : UIKit.WHITE);
            _countWord.SetAlpha(1f);
            _countWord.punch = pGo ? 0.6f : 0.2f;
            _countWord.Slam(pGo ? 3.2f : 2.4f, pGo ? 90f : (pText == "2" ? -60f : 60f));
            _countWord.Kick(pGo ? -3f : -1.5f);
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

        // Results and pause stand on their own perspective canvas : the whole screen floats with the mouse.
        private RectTransform TiltRoot(string pName, int pOrder)
        {
            Camera lGame = CameraRig.Instance != null ? CameraRig.Instance.Cam : Camera.main;
            Canvas lCanvas = UIPerspective.Create(pName, pOrder, transform, lGame);
            RectTransform lHolder = UIFactory.Stretch(UIFactory.CreateRect(lCanvas.transform, "Tilt", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            lHolder.gameObject.AddComponent<PerspectivePanel>();
            if (pOrder < 400) _endCanvas = lCanvas;
            return lHolder;
        }

        private RectTransform EndPanel(string pName, string pWord, Color pWordColor)
        {
            RectTransform lPanel = UIFactory.Stretch(UIFactory.CreateRect(TiltRoot("End3D", 30), pName, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
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
                UIKit.Icon(lRank, "trophy", UIKit.GOLD, new Vector2(0.5f, 0.5f), new Vector2(-150f, 30f), 160f);
                // The letter in real 3D : it drops from the sky, crushes on the card, the screen shakes.
                UI3DText lLetter = UI3DText.Create(lRank, GameSession.RankLetter(pRank), 380f, new Vector2(0.5f, 0.5f), new Vector2(90f, 20f), new Vector2(360f, 420f), pRank >= 4 ? UIKit.GOLD : UIKit.RED, UIKit.RED_DARK, 24, 5f);
                lLetter.tiltX = 14f;
                lLetter.tiltY = 10f;
                lLetter.gameObject.AddComponent<UIRankDrop>().letter = lLetter;
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
            RectTransform lTilt = TiltRoot("Pause3D", 500);
            RectTransform lPanel = UIFactory.Stretch(UIFactory.CreateRect(lTilt, "Pause", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            UIKit.FullScreen(lPanel, "Dim", new Color(0f, 0f, 0f, 0.25f)).rectTransform.localScale = Vector3.one * 1.3f;
            UIKit.Tile(lPanel, "Strip", new Color(UIKit.RED.r, UIKit.RED.g, UIKit.RED.b, 0.88f), new Vector2(0f, 0.5f), new Vector2(330f, 0f), new Vector2(620f, 1400f), 0.12f);
            UIKit.Text(lPanel, "PAUSE", 170f, UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(340f, 330f), new Vector2(620f, 200f), UIKit.Font.Heavy);
            // Where you are : stage code and name.
            int lIndex = GameSession.SelectedLevel;
            TextMeshProUGUI lCode = UIKit.Text(lPanel, "STAGE " + LevelDatabase.Code(lIndex), 64f, UIKit.GOLD, new Vector2(0f, 0.5f), new Vector2(330f, 220f), new Vector2(600f, 74f), UIKit.Font.Heavy);
            UIFactory.Outline(lCode, UIKit.INK, 0.2f);
            TextMeshProUGUI lName = UIKit.Text(lPanel, _level != null ? _level.title : "", 40f, UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(330f, 165f), new Vector2(600f, 50f), UIKit.Font.Heavy);
            lName.enableAutoSizing = true;
            lName.fontSizeMin = 18f;
            lName.fontSizeMax = 40f;
            
            NavTile lResume = Button(lPanel, "RESUME", new Vector2(0f, 0.5f), new Vector2(320f, 80f), new Vector2(480f, 96f), UIKit.WHITE, UIKit.INK);
            NavTile lRetry = Button(lPanel, "RESTART", new Vector2(0f, 0.5f), new Vector2(305f, -40f), new Vector2(480f, 96f), UIKit.WHITE, UIKit.INK);
            NavTile lSettings = Button(lPanel, "SETTINGS", new Vector2(0f, 0.5f), new Vector2(290f, -160f), new Vector2(480f, 96f), UIKit.WHITE, UIKit.INK);
            NavTile lMenu = Button(lPanel, "STAGE SELECT", new Vector2(0f, 0.5f), new Vector2(275f, -280f), new Vector2(480f, 96f), new Color(0.3f, 0.3f, 0.32f), UIKit.WHITE);
            UINav lNav = UINav.Create(lPanel.gameObject);
            // Start resumes : it never also presses the highlighted button.
            lNav.startSubmits = false;
            Canvas lCanvas = lTilt.GetComponentInParent<Canvas>();
            SettingsMenu lMenuSettings = SettingsMenu.Create(lCanvas.transform, false, () =>
            {
                lPanel.gameObject.SetActive(true);
                lNav.active = true;
                lNav.Select(2);
            });
            lNav.Add((RectTransform)lResume.transform, new Vector2(0f, 1f), pResume, lResume.SetSelected);
            lNav.Add((RectTransform)lRetry.transform, new Vector2(0f, 0f), pRetry, lRetry.SetSelected);
            lNav.Add((RectTransform)lSettings.transform, new Vector2(0f, -1f), () =>
            {
                lNav.active = false;
                lPanel.gameObject.SetActive(false);
                lMenuSettings.Open();
            }, lSettings.SetSelected);
            lNav.Add((RectTransform)lMenu.transform, new Vector2(0f, -2f), pMenu, lMenu.SetSelected);
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

    // The rank letter : waits for the panel, falls hard, crushes, the screen shakes, then it settles and breathes.
    public class UIRankDrop : MonoBehaviour
    {
        public UI3DText letter;
        private float _time;
        private bool _landed;
        private Vector2 _home;
        private bool _init;

        private void Update()
        {
            RectTransform lRect = (RectTransform)transform;
            if (!_init)
            {
                _home = lRect.anchoredPosition;
                _init = true;
            }
            _time += Time.unscaledDeltaTime;
            const float DELAY = 0.45f;
            const float FALL = 0.2f;
            if (_time < DELAY)
            {
                letter.SetAlpha(0f);
                return;
            }
            letter.SetAlpha(1f);
            float lT = Mathf.Clamp01((_time - DELAY) / FALL);
            lRect.anchoredPosition = _home + new Vector2(0f, (1f - lT * lT) * 900f);
            if (!_landed && lT >= 1f)
            {
                _landed = true;
                letter.Kick(-9f);
                letter.punch = 0.5f;
                Juice.Shake(0.8f);
                Juice.Pulse(1.4f);
                Juice.Flash(Color.white, 0.25f);
                AudioManager.Play("sfx_rank", 1f, 0.8f);
            }
            if (_landed) lRect.anchoredPosition = _home + new Vector2(0f, Mathf.Sin(_time * 2.2f) * 6f);
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
