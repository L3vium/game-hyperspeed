using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Race HUD, kept out of the way : speed on a plane tilted in perspective bottom left with the dash pips under it, the
    // time at the top, the race on a vertical rail on the right edge (flag at the top, you and the rival by name, the gap),
    // a pointer to the rival. Nothing in the middle of the screen.
    public class HUD : MonoBehaviour
    {
        private const float RAIL_HALF = 270f;
        // Left margin of the result column, in canvas units from the left edge of the screen.
        private const float SAFE_LEFT = 150f;

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
        // The chain (pads, rings, ropes in a row) that lifts the speed ceiling, and the light charge where light exists.
        private TextMeshProUGUI _chainText;
        private UIShape _lightBack;
        private UIShape _lightFill;
        private int _shownChain;
        private float _chainPop;
        private float _tagTimer;
        private UI3DText _countWord;
        private float _countTimer;
        private TextMeshProUGUI _speedText;
        private RectTransform _speedo;

        private RectTransform _rail;
        private RectTransform _playerMarker;
        private RectTransform _rivalMarker;
        private RectTransform _rivalPointer;
        private TextMeshProUGUI _rivalName;
        private TextMeshProUGUI _rivalGap;
        private UIShape _rivalMark;
        private RectTransform _flag;
        private bool _rivalDone;
        private float _rivalDoneTime;
        private UIBanner _notify;
        private UIBanner _hint;
        private UIBanner _wrongWay;
        private float _wrongWayTimer;
        // Big chevrons under the WRONG WAY banner pointing the way to turn.
        private RectTransform _turnArrows;
        private UIShape[] _turnChevrons;
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

            // Timer (top centre).
            _timerText = UIKit.Text(_root, "00:00.00", 38f, UIKit.WHITE, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(300f, 50f), UIKit.Font.Heavy);
            UIFactory.Outline(_timerText, UIKit.INK, 0.25f);

            // The race, on the right edge : start at the bottom, the flag at the top, you and the rival by name.
            _rail = UIFactory.CreateRect(_root, "Rail", new Vector2(1f, 0.5f), new Vector2(-70f, 10f), new Vector2(10f, RAIL_HALF * 2f));
            UIKit.Tile(_rail, "Back", new Color(0f, 0f, 0f, 0.35f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, RAIL_HALF * 2f + 8f), 0f);
            UIFactory.CreateImage(_rail, "Line", new Color(1f, 1f, 1f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4f, RAIL_HALF * 2f));
            _flag = UIFactory.CreateRect(_rail, "Flag", new Vector2(0.5f, 0.5f), new Vector2(0f, RAIL_HALF + 34f), new Vector2(48f, 48f));
            UIKit.Icon(_flag, "flag", UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, 46f);
            _rivalMarker = Marker("Rival", UIKit.RED, out _rivalMark);
            _rivalName = UIKit.Text(_rivalMarker, RivalShortName(), 26f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-140f, 9f), new Vector2(220f, 34f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            UIFactory.Outline(_rivalName, UIKit.INK, 0.3f);
            _rivalGap = UIKit.Text(_rivalMarker, "", 22f, UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2(-140f, -17f), new Vector2(220f, 28f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            UIFactory.Outline(_rivalGap, UIKit.INK, 0.3f);
            UIShape lYouMark;
            _playerMarker = Marker("Player", UIKit.CYAN, out lYouMark);
            TextMeshProUGUI lYou = UIKit.Text(_playerMarker, "YOU", 26f, UIKit.CYAN, new Vector2(0.5f, 0.5f), new Vector2(-80f, 0f), new Vector2(100f, 34f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            UIFactory.Outline(lYou, UIKit.INK, 0.3f);

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
            _chainText = UIKit.Text(_speedo, "", 44f, UIKit.GOLD, new Vector2(0f, 0.5f), new Vector2(200f, 118f), new Vector2(420f, 56f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            UIFactory.Outline(_chainText, UIKit.INK, 0.25f);
            _lightBack = UIKit.Tile(_speedo, "LightBack", new Color(0f, 0f, 0f, 0.42f), new Vector2(0f, 0.5f), new Vector2(290f, -128f), new Vector2(500f, 12f), 0.6f);
            _lightFill = UIKit.Tile(_speedo, "LightFill", UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(45f, -128f), new Vector2(10f, 12f), 0.6f);
            _lightBack.gameObject.SetActive(false);
            _lightFill.gameObject.SetActive(false);

            _countWord = UI3DText.Create(lRoot3D, " ", 300f, new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(900f, 320f), UIKit.WHITE, UIKit.RED_DARK, 22, 5f);
            _countWord.gameObject.SetActive(false);

            // Big callouts at the top centre, instructions at the bottom : anime style, in 3D.
            _notify = UIBanner.Create(lRoot3D, "Notify", new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(900f, 120f), 96f);
            _hint = UIBanner.Create(lRoot3D, "Hint", new Vector2(0.5f, 0f), new Vector2(260f, 330f), new Vector2(900f, 80f), 50f);
            _wrongWay = UIBanner.Create(lRoot3D, "WrongWay", new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 120f), 90f);
            _turnArrows = UIFactory.CreateRect(lRoot3D, "TurnAround", new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(520f, 140f));
            _turnChevrons = new UIShape[3];
            for (int i = 0; i < 3; i++)
            {
                _turnChevrons[i] = UIFactory.CreateShape(_turnArrows, "Chevron" + i, UIShape.ShapeKind.Chevron, UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 150f, 0f), new Vector2(120f, 130f));
                _turnChevrons[i].skew = 0.45f;
            }
            _turnArrows.gameObject.SetActive(false);
        }

        private string RivalShortName()
        {
            if (_rival == null) return "";
            return RivalFactory.DisplayName(_rival.kind).Replace("THE ", "").Replace("A ", "");
        }

        private RectTransform Marker(string pName, Color pColor, out UIShape pMark)
        {
            RectTransform lMarker = UIFactory.CreateRect(_rail, pName, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            // A chevron pointing at the rail from the left, and a tick across it.
            pMark = UIFactory.CreateShape(lMarker, "Mark", UIShape.ShapeKind.Triangle, pColor, new Vector2(0.5f, 0.5f), new Vector2(-22f, 0f), new Vector2(26f, 22f), -90f);
            UIFactory.CreateImage(lMarker, "Tick", pColor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 5f));
            return lMarker;
        }

        private float RailY(float pDistance)
        {
            float lFinish = Mathf.Max(1f, _path.finishS);
            return Mathf.Lerp(-RAIL_HALF, RAIL_HALF, Mathf.Clamp01(pDistance / lFinish));
        }

        // The rival crossed the line before you : said by name, loud, and its marker burns on the flag.
        public void RivalFinished()
        {
            _rivalDone = true;
            _rivalDoneTime = Time.unscaledTime;
            string lName = _rival != null ? RivalFactory.DisplayName(_rival.kind) : "THE RIVAL";
            _notifyHold = 0f;
            _pendingText = null;
            Notify(lName + " CROSSED THE LINE", UIKit.RED, 2.6f);
            if (_rivalGap != null)
            {
                _rivalGap.text = "FINISHED";
                _rivalGap.color = UIKit.RED;
            }
        }

        public void SetRaceTime(float pTime)
        {
            _timerText.text = GameSession.FormatTime(pTime);
        }

        private float _lineBurst;
        private float _lastBoost;
        private static float _externalBurst;

        // Juice events snap the screen speed lines toward the centre (they ease back out by themselves).
        public static void LinesBurst(float pAmount) => _externalBurst = Mathf.Max(_externalBurst, Mathf.Clamp01(pAmount));

        private float _lineLight;

        private void Update()
        {
            float lDt = Time.unscaledDeltaTime;
            if (_player != null)
            {
                // A calm base that grows with speed (the wide view must stay readable) ; the kicks snap them in hard.
                float lTarget = 0.05f + Mathf.Pow(_player.Speed01, 1.3f) * 0.45f + (_player.LightForm ? 0.35f : 0f) + (_player.Dashing ? 0.1f : 0f) + _player.BoostAmount * 0.1f + _player.Dive01 * 0.2f
                                + (_player.Hyper ? HSSpeedStateLook.Get(SpeedState.Hyper).speedLines : 0f) + (_player.LightForm ? HSSpeedStateLook.Get(SpeedState.Light).speedLines : 0f);
                _lineIntensity = Mathf.Lerp(_lineIntensity, Mathf.Clamp01(lTarget), 1f - Mathf.Exp(-7f * lDt));
                // Every new kick : the streaks snap toward the centre, then ease back out over 0.4 s.
                float lBoost = _player.BoostAmount;
                if (lBoost > _lastBoost + 0.05f) _lineBurst = 1f;
                _lastBoost = lBoost;
                _lineBurst = Mathf.Max(_lineBurst, _externalBurst);
                _externalBurst = 0f;
                _lineBurst = Mathf.MoveTowards(_lineBurst, 0f, lDt / 0.4f);
                _speedLinesMaterial.SetFloat("_Intensity", _player.State == PlayerState.Frozen ? 0f : _lineIntensity);
                _speedLinesMaterial.SetFloat("_Boost", lBoost);
                _speedLinesMaterial.SetFloat("_Burst", _lineBurst * _lineBurst);
                _speedLinesMaterial.SetColor("_LineColor", new Color(0.9f, 0.95f, 1f, 0.95f));
                _lineLight = Mathf.MoveTowards(_lineLight, _player.LightForm ? 1f : 0f, lDt * 1.5f);
                _speedLinesMaterial.SetFloat("_Light", _lineLight);
                if (RaceContext.Theme != null) _speedLinesMaterial.SetColor("_Accent", RaceContext.Theme.accent);
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

            UpdateChain(pDt);

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

        // "x5 HYPER" : the chain is what opens the speed tiers, so it is shown big. The light charge fills under it.
        private void UpdateChain(float pDt)
        {
            int lChain = _player.Chain;
            if (lChain != _shownChain)
            {
                if (lChain > _shownChain) _chainPop = 1f;
                _shownChain = lChain;
            }
            _chainPop = Mathf.MoveTowards(_chainPop, 0f, pDt * 4f);
            SpeedProfile lProfile = _player.Profile;
            if (lChain >= 2)
            {
                bool lOpens = lProfile.hyperEnabled && _player.KickCeiling >= lProfile.hyperEnter;
                _chainText.text = "x" + lChain + (_player.LightForm ? "  LIGHT" : lOpens ? "  HYPER" : "  CHAIN");
                _chainText.color = _player.LightForm ? UIKit.WHITE : lOpens ? UIKit.CYAN : UIKit.GOLD;
            }
            else _chainText.text = "";
            _chainText.rectTransform.localScale = Vector3.one * (1f + _chainPop * 0.35f);
            bool lLight = lProfile.LightEnabled;
            if (_lightBack.gameObject.activeSelf != lLight)
            {
                _lightBack.gameObject.SetActive(lLight);
                _lightFill.gameObject.SetActive(lLight);
            }
            if (!lLight) return;
            float lWidth = Mathf.Lerp(10f, 500f, _player.LightCharge);
            _lightFill.rectTransform.sizeDelta = new Vector2(lWidth, 12f);
            _lightFill.rectTransform.anchoredPosition = new Vector2(40f + lWidth * 0.5f, -128f);
            _lightFill.color = _player.LightCharge >= 1f ? UIKit.WHITE : Color.Lerp(UIKit.CYAN, UIKit.WHITE, _player.LightCharge);
        }

        private void UpdateRail()
        {
            if (_path == null || _path.finishS <= 0f) return;
            _playerMarker.anchoredPosition = new Vector2(0f, RailY(_player.Progress));
            if (_rival == null)
            {
                _rivalMarker.gameObject.SetActive(false);
                return;
            }
            _rivalMarker.anchoredPosition = new Vector2(0f, RailY(Mathf.Max(0f, _rival.distance)));
            // Close to you on the rail : its name moves out of the way of YOU.
            float lApart = _rivalMarker.anchoredPosition.y - _playerMarker.anchoredPosition.y;
            float lShift = Mathf.Abs(lApart) < 46f ? (lApart >= 0f ? 1f : -1f) * (46f - Mathf.Abs(lApart)) : 0f;
            _rivalName.rectTransform.anchoredPosition = new Vector2(-140f, 9f + lShift);
            _rivalGap.rectTransform.anchoredPosition = new Vector2(-140f, -17f + lShift);
            if (_rivalDone)
            {
                // Burning on the flag.
                float lBlink = Mathf.Repeat((Time.unscaledTime - _rivalDoneTime) * 3f, 1f) < 0.5f ? 1f : 0.35f;
                _rivalMark.color = new Color(UIKit.RED.r, UIKit.RED.g, UIKit.RED.b, lBlink);
                _rivalMarker.localScale = Vector3.one * (1.15f + Mathf.Sin(Time.unscaledTime * 12f) * 0.1f);
            }
            else
            {
                // Ahead of you : + in red ; behind : - in cyan.
                float lGap = _rival.distance - _player.Progress;
                _rivalGap.text = (lGap >= 0f ? "+" : "-") + Mathf.RoundToInt(Mathf.Abs(lGap)) + " m";
                _rivalGap.color = lGap >= 0f ? UIKit.RED : UIKit.CYAN;
            }
            // A chaser right behind you : the edges of the screen say it, no words needed.
            if (_rival.chaser && RaceContext.Running && _player.Progress - _rival.distance < _player.Profile.cruise * 1.2f) Juice.Vignette(UIKit.RED, 0.6f);
        }

        // Points at the rival : marker above it when visible, clamped arrow on the screen edge otherwise.
        private void UpdateRivalPointer()
        {
            Camera lCamera = CameraRig.Instance != null ? CameraRig.Instance.Cam : null;
            if (_rival == null || lCamera == null || (!_rival.chaser && _rival.distance < _player.Progress)) { _rivalPointer.gameObject.SetActive(false); return; }
            _rivalPointer.gameObject.SetActive(true);
            Vector3 lScreen;
            if (PanoramaRig.Active != null && PanoramaRig.Active.On) PanoramaRig.Active.WorldToScreen(_rival.transform.position + Vector3.up * 6f, out lScreen);
            else lScreen = lCamera.WorldToScreenPoint(_rival.transform.position + Vector3.up * 6f);
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
                HSJuice.Play(JuiceId.Hint, _player.transform.position);
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
            // The red vignette closes in softly while you run the wrong way.
            if (PostFX.Instance != null) PostFX.Instance.wrongWay = _player.WrongWay ? 1f : 0f;
            bool lArrows = _player.WrongWay;
            if (_turnArrows.gameObject.activeSelf != lArrows) _turnArrows.gameObject.SetActive(lArrows);
            if (lArrows)
            {
                // Which way to turn : toward the track's direction. The chevrons light one after the other that way.
                Vector3 lFlow = _path.Flow(_player.Progress);
                float lSide = Vector3.Dot(Vector3.Cross(_player.Heading, lFlow), _player.Up) >= 0f ? 1f : -1f;
                float lPhase = Time.unscaledTime * 4f;
                for (int i = 0; i < _turnChevrons.Length; i++)
                {
                    int lOrder = lSide > 0f ? i : _turnChevrons.Length - 1 - i;
                    float lLit = Mathf.Clamp01(1f - Mathf.Repeat(lPhase - lOrder * 0.33f, 1f) * 1.6f);
                    _turnChevrons[i].color = new Color(1f, Mathf.Lerp(0.05f, 0.85f, lLit), Mathf.Lerp(0.1f, 0.8f, lLit), Mathf.Lerp(0.35f, 1f, lLit));
                    _turnChevrons[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, lSide > 0f ? 0f : 180f);
                    _turnChevrons[i].rectTransform.localScale = Vector3.one * (1f + lLit * 0.25f);
                }
            }
            _wrongWayTimer -= pDt;
            if (!_player.WrongWay || _wrongWayTimer > 0f) return;
            _wrongWayTimer = 1.2f;
            _wrongWay.Show("WRONG WAY - TURN AROUND", UIKit.RED, 1.1f);
            HSJuice.Play(JuiceId.WrongWay, _player.transform.position);
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

        // Results (2026-11-12) : the show is the runner still running the stage behind them, so the results stay small, in
        // a corner : a compact card (time, best, margin, buttons) at the bottom left, and the rank letter big and centred
        // at the top, under the cinema bars. No dim, no slab across the screen.
        public void ShowResult(bool pWin, string pHeadline, float pTime, int pRank, bool pRecord, float pBest, bool pHasNext,
                               System.Action pNext, System.Action pRetry, System.Action pMenu, string pMargin = null, string pNextLabel = "NEXT STAGE")
        {
            // The race HUD leaves : only the results and the runner remain.
            SetPaused(true);
            RectTransform lTilt = TiltRoot("End3D", 30);
            PerspectivePanel lSwing = lTilt.GetComponent<PerspectivePanel>();
            lSwing.amountX = 1.2f;
            lSwing.amountY = 0.9f;
            lSwing.slide = new Vector2(4f, 3f);
            RectTransform lRoot = UIFactory.Stretch(UIFactory.CreateRect(lTilt, "Result", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));

            // ---- the card, bottom left, about a quarter of the width
            const float CARD_W = 520f;
            const float CARD_H = 470f;
            RectTransform lCard = UIFactory.CreateRect(lRoot, "Card", new Vector2(0f, 0f), new Vector2(SAFE_LEFT + CARD_W * 0.5f - 40f, 200f + CARD_H * 0.5f), new Vector2(CARD_W, CARD_H));
            UIShape lBack = UIKit.Tile(lCard, "Back", new Color(UIKit.INK.r, UIKit.INK.g, UIKit.INK.b, 0.82f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CARD_W, CARD_H), 0.08f);
            lBack.raycastTarget = false;
            UIKit.Tile(lCard, "Edge", pWin ? UIKit.GOLD : UIKit.RED, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(CARD_W, 12f), 0.08f);
            TextMeshProUGUI lWord = UIKit.Text(lCard, pWin ? "VICTORY" : pHeadline, 64f, pWin ? UIKit.WHITE : UIKit.GOLD, new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(CARD_W - 50f, 74f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            lWord.enableAutoSizing = true;
            lWord.fontSizeMin = 28f;
            lWord.fontSizeMax = 64f;
            UIKit.Text(lCard, "TIME  " + GameSession.FormatTime(pTime), 40f, UIKit.WHITE, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(CARD_W - 50f, 48f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            string lBestLine = pRecord ? "NEW RECORD!" : "BEST  " + GameSession.FormatTime(pBest);
            UIKit.Text(lCard, lBestLine, 28f, pRecord ? UIKit.GOLD : new Color(1f, 1f, 1f, 0.75f), new Vector2(0.5f, 1f), new Vector2(0f, -162f), new Vector2(CARD_W - 50f, 36f), UIKit.Font.Bold, TextAlignmentOptions.Left);
            if (!string.IsNullOrEmpty(pMargin))
                UIKit.Text(lCard, pMargin, 28f, pWin ? UIKit.CYAN : UIKit.RED, new Vector2(0.5f, 1f), new Vector2(0f, -198f), new Vector2(CARD_W - 50f, 36f), UIKit.Font.Heavy, TextAlignmentOptions.Left);

            UINav lNav = UINav.Create(lCard.gameObject);
            float lY = -60f;
            Vector2 lCenter = new Vector2(0.5f, 0.5f);
            if (pWin && pHasNext)
            {
                NavTile lNext = Button(lCard, pNextLabel, lCenter, new Vector2(0f, lY), new Vector2(CARD_W - 70f, 62f), UIKit.WHITE, UIKit.INK);
                lNext.label.fontSize = 36f;
                lNav.Add((RectTransform)lNext.transform, new Vector2(0f, lY), pNext, lNext.SetSelected);
                lY -= 76f;
            }
            NavTile lRetry = Button(lCard, "RETRY", lCenter, new Vector2(0f, lY), new Vector2(CARD_W - 70f, 62f), UIKit.WHITE, UIKit.INK);
            lRetry.label.fontSize = 36f;
            lNav.Add((RectTransform)lRetry.transform, new Vector2(0f, lY), pRetry, lRetry.SetSelected);
            lY -= 76f;
            NavTile lMenu = Button(lCard, "STAGE SELECT", lCenter, new Vector2(0f, lY), new Vector2(CARD_W - 70f, 62f), new Color(0.3f, 0.3f, 0.32f), UIKit.WHITE);
            lMenu.label.fontSize = 36f;
            lNav.Add((RectTransform)lMenu.transform, new Vector2(0f, lY), pMenu, lMenu.SetSelected);
            lNav.Select(0);
            lCard.gameObject.AddComponent<UISlideIn>();

            if (!pWin) return;
            // ---- the rank, big and centred at the top, under the bar
            // High in the frame : the runner, in the middle of the frame, stays in the clear under it.
            RectTransform lRank = UIFactory.CreateRect(lRoot, "Rank", new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(520f, 400f));
            UI3DText lLetter = UI3DText.Create(lRank, GameSession.RankLetter(pRank), 400f, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(420f, 420f), pRank >= 4 ? UIKit.GOLD : UIKit.RED, UIKit.RED_DARK, 26, 7f);
            lLetter.tiltX = 10f;
            lLetter.tiltY = 8f;
            UIRankDrop lDrop = lLetter.gameObject.AddComponent<UIRankDrop>();
            lDrop.letter = lLetter;
            lDrop.rank = pRank;
            TextMeshProUGUI lRankWord = UIKit.Text(lRank, "RANK", 40f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(330f, -60f), new Vector2(200f, 50f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            UIFactory.Outline(lRankWord, UIKit.INK, 0.25f);
        }

        // The ending (2026-11-12) : after the last stage, the runner keeps running the supernova behind a card that says
        // it is over, every stage's rank and time, and the credits rolling up. Any confirm goes back to the stages.
        public void ShowEnding(System.Action pDone)
        {
            if (_endCanvas != null) Destroy(_endCanvas.gameObject);
            RectTransform lTilt = TiltRoot("Ending3D", 40);
            RectTransform lRoot = UIFactory.Stretch(UIFactory.CreateRect(lTilt, "Ending", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            TextMeshProUGUI lEnd = UIKit.Text(lRoot, "THE END", 150f, UIKit.GOLD, new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(1400f, 170f), UIKit.Font.Heavy);
            UIFactory.Outline(lEnd, UIKit.INK, 0.25f);
            UIKit.Text(lRoot, "You outran everything. Even the end of the world.", 40f, UIKit.WHITE, new Vector2(0.5f, 1f), new Vector2(0f, -390f), new Vector2(1400f, 50f), UIKit.Font.Bold);

            // Every stage : code, name, rank, best time ; the total at the bottom.
            RectTransform lTable = UIFactory.CreateRect(lRoot, "Ranks", new Vector2(0f, 0.5f), new Vector2(SAFE_LEFT + 330f, -40f), new Vector2(640f, 760f));
            UIKit.Tile(lTable, "Back", new Color(UIKit.INK.r, UIKit.INK.g, UIKit.INK.b, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 760f), 0.06f).raycastTarget = false;
            float lTotal = 0f;
            bool lAll = true;
            for (int i = 0; i < LevelDatabase.Count; i++)
            {
                float lY = 350f - i * 38f;
                float lBest = GameSession.GetBestTime(i);
                if (lBest > 0f) lTotal += lBest; else lAll = false;
                int lRank = GameSession.GetRank(i);
                UIKit.Text(lTable, LevelDatabase.Code(i), 24f, UIKit.GOLD, new Vector2(0.5f, 0.5f), new Vector2(-260f, lY), new Vector2(80f, 34f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
                UIKit.Text(lTable, LevelDatabase.Get(i).title, 24f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-40f, lY), new Vector2(360f, 34f), UIKit.Font.Bold, TextAlignmentOptions.Left);
                UIKit.Text(lTable, GameSession.RankLetter(lRank), 28f, lRank >= 4 ? UIKit.GOLD : UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(170f, lY), new Vector2(50f, 34f), UIKit.Font.Heavy);
                UIKit.Text(lTable, GameSession.FormatTime(lBest), 22f, new Color(1f, 1f, 1f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(255f, lY), new Vector2(130f, 34f), UIKit.Font.Bold, TextAlignmentOptions.Right);
            }
            UIKit.Text(lTable, (lAll ? "TOTAL  " : "TOTAL (so far)  ") + GameSession.FormatTime(lTotal), 32f, UIKit.CYAN, new Vector2(0.5f, 0.5f), new Vector2(0f, -345f), new Vector2(600f, 44f), UIKit.Font.Heavy);

            // The credits roll on the right.
            TextAsset lCredits = Resources.Load<TextAsset>("Credits");
            RectTransform lRollMask = UIFactory.CreateRect(lRoot, "CreditsMask", new Vector2(1f, 0.5f), new Vector2(-SAFE_LEFT - 330f, -40f), new Vector2(660f, 640f));
            lRollMask.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI lRoll = UIKit.Text(lRollMask, lCredits != null ? lCredits.text : "Thanks for playing.", 26f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(0f, -900f), new Vector2(640f, 1400f), UIKit.Font.Bold, TextAlignmentOptions.Top, false);
            lRoll.richText = true;
            lRoll.gameObject.AddComponent<UICreditsRoll>();

            UINav lNav = UINav.Create(lRoot.gameObject);
            NavTile lBack = Button(lRoot, "BACK TO THE STAGES", new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(620f, 80f), UIKit.WHITE, UIKit.INK);
            lBack.label.fontSize = 40f;
            lNav.Add((RectTransform)lBack.transform, Vector2.zero, pDone, lBack.SetSelected);
            lNav.Select(0);
            AudioManager.PlayMusic(AudioManager.HasClip("music_ending") ? "music_ending" : "music_win", 0.55f);
        }

        public GameObject ShowPause(System.Action pResume, System.Action pRetry, System.Action pMenu)
        {
            RectTransform lTilt = TiltRoot("Pause3D", 500);
            RectTransform lPanel = UIFactory.Stretch(UIFactory.CreateRect(lTilt, "Pause", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            UIKit.FullScreen(lPanel, "Dim", new Color(0f, 0f, 0f, 0.25f)).rectTransform.localScale = new Vector3(2.4f, 2.4f, 1f);
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
        public int rank = 4;
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
                HSJuice.Play(JuiceId.RankReveal, new JuiceContext { intensity = rank / 4f, label = GameSession.RankLetter(rank).ToLowerInvariant(), size = 1f });
            }
            if (_landed) lRect.anchoredPosition = _home + new Vector2(0f, Mathf.Sin(_time * 2.2f) * 6f);
        }
    }

    // Credits rolling up, slowly, over and over.
    public class UICreditsRoll : MonoBehaviour
    {
        private void Update()
        {
            RectTransform lRect = (RectTransform)transform;
            Vector2 lPosition = lRect.anchoredPosition + new Vector2(0f, Time.unscaledDeltaTime * 42f);
            if (lPosition.y > 1200f) lPosition.y = -900f;
            lRect.anchoredPosition = lPosition;
        }
    }

    public class UISlideIn : MonoBehaviour
    {
        private float _time;
        private Vector2 _home;
        private bool _init;

        private void Update()
        {
            RectTransform lRect = (RectTransform)transform;
            if (!_init)
            {
                // Slides in from the left onto wherever it was placed.
                _home = lRect.anchoredPosition;
                _init = true;
            }
            _time += Time.unscaledDeltaTime;
            float lT = 1f - Mathf.Pow(1f - Mathf.Clamp01(_time / 0.35f), 3f);
            lRect.anchoredPosition = _home + new Vector2(Mathf.Lerp(-1920f, 0f, lT), 0f);
            if (lT >= 1f) enabled = false;
        }
    }
}
