using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // In-race overlay : speed lines, speedometer, race bar, popups, hints, countdown and end panels.
    public class HUD : MonoBehaviour
    {
        private class Popup
        {
            public TextMeshProUGUI text;
            public UIShape burst;
            public float time;
            public float duration;
            public Vector2 start;
            public float rotation;
            public float scale;
        }

        private Canvas _canvas;
        private RectTransform _root;
        private Material _speedLinesMaterial;
        private float _lineIntensity;

        private TextMeshProUGUI _speedText;
        private UIShape _speedBar;
        private TextMeshProUGUI _timerText;
        private RectTransform _raceBar;
        private RectTransform _playerMarker;
        private RectTransform _rivalMarker;
        private TextMeshProUGUI _gapText;
        private TextMeshProUGUI _comboText;
        private UIShape _driftBar;
        private UIShape _driftBack;
        private Image _jumpPip;
        private Image _dashPip;
        private TextMeshProUGUI _hintText;
        private RectTransform _hintPanel;
        private TextMeshProUGUI _warningText;

        private readonly List<Popup> _popups = new List<Popup>();
        private readonly Queue<Popup> _popupPool = new Queue<Popup>();
        private int _hintIndex;
        private float _hintTimer;
        private float _displayedSpeed;

        private PlayerController _player;
        private Rival _rival;
        private TrackPath _path;

        public Transform Root => _root;

        public static HUD Create(PlayerController pPlayer, Rival pRival, TrackPath pPath, LevelDefinition pLevel)
        {
            HUD lHud = new GameObject("HUD").AddComponent<HUD>();
            lHud._player = pPlayer;
            lHud._rival = pRival;
            lHud._path = pPath;
            lHud.Build(pLevel);
            Juice.OnPopup += lHud.ShowPopup;
            return lHud;
        }

        private void OnDestroy()
        {
            Juice.OnPopup -= ShowPopup;
        }

        private void Build(LevelDefinition pLevel)
        {
            _canvas = UIFactory.CreateCanvas("HUDCanvas", 10, transform);
            _root = (RectTransform)_canvas.transform;

            // Speed lines overlay
            RawImage lLines = UIFactory.CreateRect(_root, "SpeedLines", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            UIFactory.Stretch(lLines.rectTransform);
            _speedLinesMaterial = new Material(Shader.Find("Hyperspeed/SpeedLines"));
            lLines.material = _speedLinesMaterial;
            lLines.raycastTarget = false;

            // Speedometer (top left)
            UIFactory.CreateShape(_root, "SpeedBackRed", UIShape.ShapeKind.Parallelogram, UIFactory.RED, new Vector2(0f, 1f), new Vector2(225f, -88f), new Vector2(360f, 120f), 6f).skew = 0.35f;
            UIFactory.CreateShape(_root, "SpeedBack", UIShape.ShapeKind.Parallelogram, UIFactory.BLACK, new Vector2(0f, 1f), new Vector2(210f, -80f), new Vector2(360f, 110f), 6f).skew = 0.35f;
            _speedText = UIFactory.CreateText(_root, "0", 96f, UIFactory.WHITE, new Vector2(0f, 1f), new Vector2(190f, -76f), new Vector2(300f, 110f), true, TextAlignmentOptions.Right);
            _speedText.fontStyle = FontStyles.Italic;
            _speedText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 6f);
            TextMeshProUGUI lUnit = UIFactory.CreateText(_root, "m/s", 34f, UIFactory.WHITE, new Vector2(0f, 1f), new Vector2(352f, -104f), new Vector2(100f, 50f), false);
            lUnit.fontStyle = FontStyles.Italic | FontStyles.Bold;
            _speedBar = UIFactory.CreateShape(_root, "SpeedBar", UIShape.ShapeKind.Parallelogram, UIFactory.CYAN, new Vector2(0f, 1f), new Vector2(60f, -150f), new Vector2(10f, 16f), 6f);
            _speedBar.rectTransform.pivot = new Vector2(0f, 0.5f);

            // Timer (top right)
            UIFactory.CreateShape(_root, "TimerBack", UIShape.ShapeKind.Parallelogram, UIFactory.BLACK, new Vector2(1f, 1f), new Vector2(-200f, -70f), new Vector2(330f, 80f), -5f);
            _timerText = UIFactory.CreateText(_root, "00:00.00", 54f, UIFactory.WHITE, new Vector2(1f, 1f), new Vector2(-200f, -68f), new Vector2(320f, 80f), true);
            _timerText.fontStyle = FontStyles.Italic;
            _timerText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -5f);

            // Race bar (top center)
            UIShape lVs = UIFactory.CreateShape(_root, "VsBack", UIShape.ShapeKind.Parallelogram, UIFactory.BLACK, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(620f, 64f), -2f);
            lVs.skew = 0.4f;
            TextMeshProUGUI lVsText = UIFactory.CreateText(_root, "<color=#EB0819>VS.</color> " + RivalFactory.DisplayName(pLevel.rival), 40f, UIFactory.WHITE, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(620f, 64f), true);
            lVsText.fontStyle = FontStyles.Italic;
            lVsText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -2f);
            _raceBar = UIFactory.CreateImage(_root, "RaceBar", new Color(0f, 0f, 0f, 0.6f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(700f, 10f)).rectTransform;
            UIFactory.CreateImage(_raceBar, "Finish", UIFactory.WHITE, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(8f, 34f));
            _rivalMarker = UIFactory.CreateShape(_raceBar, "Rival", UIShape.ShapeKind.Triangle, UIFactory.RED, new Vector2(0f, 0.5f), new Vector2(0f, -22f), new Vector2(30f, 26f)).rectTransform;
            _playerMarker = UIFactory.CreateShape(_raceBar, "Player", UIShape.ShapeKind.Triangle, UIFactory.CYAN, new Vector2(0f, 0.5f), new Vector2(0f, 22f), new Vector2(34f, 30f), 180f).rectTransform;
            _gapText = UIFactory.CreateText(_root, "", 30f, UIFactory.WHITE, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(600f, 40f), false);
            _gapText.fontStyle = FontStyles.Italic | FontStyles.Bold;
            UIFactory.Outline(_gapText, UIFactory.BLACK, 0.25f);

            // Combo (right)
            _comboText = UIFactory.CreateText(_root, "", 64f, UIFactory.YELLOW, new Vector2(1f, 0.5f), new Vector2(-190f, 120f), new Vector2(360f, 90f), true);
            _comboText.fontStyle = FontStyles.Italic;
            _comboText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            UIFactory.Outline(_comboText, UIFactory.BLACK, 0.3f);

            // Drift charge + ability pips (bottom center)
            _driftBack = UIFactory.CreateShape(_root, "DriftBack", UIShape.ShapeKind.Parallelogram, new Color(0f, 0f, 0f, 0.6f), new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(320f, 22f));
            _driftBar = UIFactory.CreateShape(_root, "DriftBar", UIShape.ShapeKind.Parallelogram, UIFactory.CYAN, new Vector2(0.5f, 0f), new Vector2(-160f, 170f), new Vector2(0f, 18f));
            _driftBar.rectTransform.pivot = new Vector2(0f, 0.5f);
            _jumpPip = UIFactory.CreateImage(_root, "JumpPip", UIFactory.WHITE, new Vector2(0.5f, 0f), new Vector2(-24f, 136f), new Vector2(26f, 26f));
            _jumpPip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _dashPip = UIFactory.CreateImage(_root, "DashPip", UIFactory.CYAN, new Vector2(0.5f, 0f), new Vector2(24f, 136f), new Vector2(26f, 26f));
            _dashPip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Hints (bottom)
            _hintPanel = UIFactory.CreateRect(_root, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1100f, 80f));
            UIFactory.CreateShape(_hintPanel, "HintBack", UIShape.ShapeKind.Parallelogram, UIFactory.BLACK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 70f)).skew = 0.3f;
            UIFactory.CreateShape(_hintPanel, "HintTag", UIShape.ShapeKind.Parallelogram, UIFactory.RED, new Vector2(0f, 0.5f), new Vector2(100f, 0f), new Vector2(110f, 70f)).skew = 0.3f;
            UIFactory.CreateText(_hintPanel, "TIP", 38f, UIFactory.WHITE, new Vector2(0f, 0.5f), new Vector2(100f, 2f), new Vector2(110f, 70f), true).fontStyle = FontStyles.Italic;
            _hintText = UIFactory.CreateText(_hintPanel, "", 40f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(60f, 2f), new Vector2(850f, 70f), false);
            _hintText.fontStyle = FontStyles.Italic | FontStyles.Bold;
            _hintPanel.gameObject.SetActive(false);

            _warningText = UIFactory.CreateText(_root, "", 52f, UIFactory.RED, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1400f, 80f), true);
            _warningText.fontStyle = FontStyles.Italic;
            UIFactory.Outline(_warningText, UIFactory.BLACK, 0.3f);
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
                float lSpeed = _player.Speed;
                _displayedSpeed = Mathf.Lerp(_displayedSpeed, lSpeed, 1f - Mathf.Exp(-12f * lDt));
                _speedText.text = Mathf.RoundToInt(_displayedSpeed).ToString();
                _speedText.color = _player.Hyper ? Color.Lerp(UIFactory.CYAN, Color.white, Mathf.Repeat(Time.unscaledTime * 8f, 1f)) : (lSpeed > PlayerController.SMASH_SPEED ? UIFactory.YELLOW : UIFactory.WHITE);
                float lShake = Mathf.Clamp01((lSpeed - 60f) / 80f) * 4f;
                _speedText.rectTransform.anchoredPosition = new Vector2(190f, -76f) + Random.insideUnitCircle * lShake;
                _speedBar.rectTransform.sizeDelta = new Vector2(Mathf.Clamp01(lSpeed / PlayerController.HYPER_ENTER) * 320f, 16f);
                _speedBar.color = _player.Hyper ? Color.white : (lSpeed > PlayerController.SMASH_SPEED ? UIFactory.YELLOW : UIFactory.CYAN);

                float lTarget = _player.Speed01 + (_player.Hyper ? 0.25f : 0f) + (_player.Dashing ? 0.3f : 0f);
                _lineIntensity = Mathf.Lerp(_lineIntensity, Mathf.Clamp01(lTarget), 1f - Mathf.Exp(-6f * lDt));
                _speedLinesMaterial.SetFloat("_Intensity", _player.State == PlayerState.Frozen ? 0f : _lineIntensity);
                _speedLinesMaterial.SetColor("_LineColor", _player.Hyper ? new Color(0.7f, 1f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.75f));

                bool lDrift = _player.Drifting;
                _driftBack.gameObject.SetActive(lDrift);
                _driftBar.gameObject.SetActive(lDrift);
                if (lDrift)
                {
                    Color[] lColors = { Color.white, new Color(0.3f, 0.7f, 1f), new Color(1f, 0.6f, 0.1f), new Color(1f, 0.3f, 0.9f) };
                    _driftBar.color = lColors[_player.DriftTier];
                    _driftBar.rectTransform.sizeDelta = new Vector2(Mathf.Clamp01(_player.DriftCharge / 2f) * 320f, 18f);
                }
                _jumpPip.color = _player.HasDoubleJump ? UIFactory.WHITE : new Color(1f, 1f, 1f, 0.15f);
                _dashPip.color = _player.HasAirDash ? UIFactory.CYAN : new Color(0.2f, 0.9f, 1f, 0.15f);

                _comboText.text = _player.Combo >= 3 ? "x" + _player.Combo + " COMBO" : "";
                _comboText.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 12f) * 0.04f);

                UpdateRaceBar();
                UpdateHints(lDt);
            }
            UpdatePopups(lDt);
        }

        private void UpdateRaceBar()
        {
            if (_path == null || _path.finishDistance <= 0f) return;
            float lPlayer = Mathf.Clamp01(_player.Progress / _path.finishDistance);
            float lRival = Mathf.Clamp01(_rival != null ? _rival.distance / _path.finishDistance : 0f);
            _playerMarker.anchoredPosition = new Vector2(lPlayer * 700f, 22f);
            _rivalMarker.anchoredPosition = new Vector2(lRival * 700f, -22f);
            if (_rival == null) return;

            float lGap = _player.Progress - _rival.distance;
            if (_rival.chaser)
            {
                _gapText.text = Mathf.RoundToInt(Mathf.Max(0f, lGap)) + " m AHEAD OF " + RivalFactory.DisplayName(_rival.kind);
                _gapText.color = lGap < 90f ? UIFactory.RED : UIFactory.WHITE;
                bool lDanger = lGap < 90f && RaceContext.Running;
                _warningText.text = lDanger ? "!! " + RivalFactory.DisplayName(_rival.kind) + " IS RIGHT BEHIND YOU !!" : "";
                _warningText.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 16f) * 0.06f);
                if (lDanger) Juice.Vignette(UIFactory.RED, 0.5f);
            }
            else
            {
                _gapText.text = lGap >= 0f ? "+" + Mathf.RoundToInt(lGap) + " m" : Mathf.RoundToInt(lGap) + " m";
                _gapText.color = lGap >= 0f ? UIFactory.CYAN : UIFactory.RED;
            }
        }

        private void UpdateHints(float pDt)
        {
            if (_hintIndex < _path.hints.Count && _player.Progress >= _path.hints[_hintIndex].Key - 10f)
            {
                _hintText.text = _path.hints[_hintIndex].Value;
                _hintIndex++;
                _hintTimer = 4f;
                _hintPanel.gameObject.SetActive(true);
                _hintPanel.anchoredPosition = new Vector2(-1400f, 70f);
                AudioManager.Play("ui_hint", 0.5f);
            }
            if (_hintTimer > 0f)
            {
                _hintTimer -= pDt;
                float lTargetX = _hintTimer > 0.4f ? 0f : 1600f;
                _hintPanel.anchoredPosition = Vector2.Lerp(_hintPanel.anchoredPosition, new Vector2(lTargetX, 70f), 1f - Mathf.Exp(-14f * pDt));
                if (_hintTimer <= 0f) _hintPanel.gameObject.SetActive(false);
            }
        }

        // ---------------------------------------------------------------- popups

        private void ShowPopup(string pText, Color pColor, float pScale)
        {
            Popup lPopup = _popupPool.Count > 0 ? _popupPool.Dequeue() : CreatePopup();
            lPopup.text.gameObject.SetActive(true);
            lPopup.burst.gameObject.SetActive(true);
            lPopup.text.text = pText;
            lPopup.text.color = pColor;
            lPopup.burst.color = pColor == UIFactory.RED ? UIFactory.BLACK : (pColor == Color.white ? UIFactory.RED : UIFactory.BLACK);
            lPopup.burst.seed = Random.Range(0, 9999);
            lPopup.burst.SetVerticesDirty();
            lPopup.time = 0f;
            lPopup.duration = 0.9f;
            lPopup.scale = pScale;
            lPopup.rotation = Random.Range(-12f, 12f);
            lPopup.start = new Vector2(Random.Range(-260f, 260f), Random.Range(40f, 200f));
            _popups.Add(lPopup);
            if (_popups.Count > 5)
            {
                Recycle(_popups[0]);
                _popups.RemoveAt(0);
            }
        }

        private Popup CreatePopup()
        {
            Popup lPopup = new Popup();
            lPopup.burst = UIFactory.CreateShape(_root, "PopupBurst", UIShape.ShapeKind.Burst, UIFactory.BLACK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 160f));
            lPopup.burst.spikes = 18;
            lPopup.burst.innerRatio = 0.75f;
            lPopup.text = UIFactory.CreateText(_root, "", 64f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 100f), true);
            lPopup.text.fontStyle = FontStyles.Italic;
            UIFactory.Outline(lPopup.text, UIFactory.BLACK, 0.25f);
            return lPopup;
        }

        private void Recycle(Popup pPopup)
        {
            pPopup.text.gameObject.SetActive(false);
            pPopup.burst.gameObject.SetActive(false);
            _popupPool.Enqueue(pPopup);
        }

        private void UpdatePopups(float pDt)
        {
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                Popup lPopup = _popups[i];
                lPopup.time += pDt;
                float lT = lPopup.time / lPopup.duration;
                if (lT >= 1f)
                {
                    Recycle(lPopup);
                    _popups.RemoveAt(i);
                    continue;
                }
                float lIn = Mathf.Clamp01(lPopup.time / 0.08f);
                float lScale = lPopup.scale * (lIn < 1f ? Mathf.Lerp(2.2f, 1f, lIn) : 1f + Mathf.Sin(lPopup.time * 30f) * 0.03f);
                Vector2 lPosition = lPopup.start + new Vector2(0f, lT * 60f) + (lT < 0.15f ? Random.insideUnitCircle * 8f : Vector2.zero);
                float lAlpha = lT > 0.75f ? 1f - (lT - 0.75f) / 0.25f : 1f;
                lPopup.text.rectTransform.anchoredPosition = lPosition;
                lPopup.text.rectTransform.localScale = Vector3.one * lScale;
                lPopup.text.rectTransform.localRotation = Quaternion.Euler(0f, 0f, lPopup.rotation);
                lPopup.text.alpha = lAlpha;
                lPopup.burst.rectTransform.anchoredPosition = lPosition;
                lPopup.burst.rectTransform.localScale = Vector3.one * lScale * (0.4f + 0.18f * lPopup.text.text.Length * 0.25f);
                lPopup.burst.rectTransform.localRotation = Quaternion.Euler(0f, 0f, lPopup.rotation * -1.5f + lT * 40f);
                Color lBurst = lPopup.burst.color;
                lBurst.a = lAlpha * 0.9f;
                lPopup.burst.color = lBurst;
            }
        }

        // ---------------------------------------------------------------- countdown & cards

        public IEnumerator TitleCard(LevelDefinition pLevel, int pIndex)
        {
            RectTransform lCard = UIFactory.CreateRect(_root, "TitleCard", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 600f));
            UIShape lRed = UIFactory.CreateShape(lCard, "Red", UIShape.ShapeKind.Parallelogram, UIFactory.RED, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(2400f, 210f), -6f);
            UIShape lBlack = UIFactory.CreateShape(lCard, "Black", UIShape.ShapeKind.Parallelogram, UIFactory.BLACK, new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(2400f, 90f), -6f);
            TextMeshProUGUI lLevel = UIFactory.CreateText(lCard, LevelDatabase.WORLDS[pLevel.world] + "  -  STAGE " + (pIndex + 1).ToString("00"), 36f, UIFactory.BLACK, new Vector2(0.5f, 0.5f), new Vector2(-300f, 150f), new Vector2(1200f, 60f), true);
            lLevel.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -6f);
            TextMeshProUGUI lTitle = UIFactory.CreateText(lCard, pLevel.title, 130f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(0f, 46f), new Vector2(2000f, 200f), true);
            lTitle.fontStyle = FontStyles.Italic;
            lTitle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -6f);
            UIFactory.Outline(lTitle, UIFactory.BLACK, 0.2f);
            TextMeshProUGUI lTag = UIFactory.CreateText(lCard, pLevel.tagline, 40f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(0f, -106f), new Vector2(1800f, 80f), false);
            lTag.fontStyle = FontStyles.Italic | FontStyles.Bold;
            lTag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -6f);

            AudioManager.Play("ui_whoosh", 0.8f);
            float lDuration = 2.2f;
            for (float lTime = 0f; lTime < lDuration; lTime += Time.unscaledDeltaTime)
            {
                float lIn = 1f - Mathf.Pow(1f - Mathf.Clamp01(lTime / 0.3f), 3f);
                float lOut = Mathf.Clamp01((lTime - (lDuration - 0.3f)) / 0.3f);
                float lX = Mathf.Lerp(-2400f, 0f, lIn) + lOut * 2400f;
                lRed.rectTransform.anchoredPosition = new Vector2(lX + lTime * 40f, 40f);
                lBlack.rectTransform.anchoredPosition = new Vector2(-lX - lTime * 40f, -110f);
                lTitle.rectTransform.anchoredPosition = new Vector2(lX * 1.2f + lTime * 25f, 46f);
                lTag.rectTransform.anchoredPosition = new Vector2(-lX * 1.2f - lTime * 25f, -106f);
                lLevel.rectTransform.anchoredPosition = new Vector2(-300f + lX * 1.4f, 150f);
                yield return null;
            }
            Destroy(lCard.gameObject);
        }

        public IEnumerator Countdown(System.Action pOnGo)
        {
            string[] lSteps = { "3", "2", "1", "GO!!" };
            for (int i = 0; i < lSteps.Length; i++)
            {
                bool lGo = i == lSteps.Length - 1;
                RectTransform lGroup = UIFactory.CreateRect(_root, "Count", new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(600f, 600f));
                UIShape lBurst = UIFactory.CreateShape(lGroup, "Burst", UIShape.ShapeKind.Burst, lGo ? UIFactory.RED : UIFactory.BLACK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(lGo ? 900f : 520f, lGo ? 520f : 520f));
                lBurst.SetShape(UIShape.ShapeKind.Burst, 0f, lGo ? 22 : 14, 0.68f, i * 31 + 7);
                UIShape lInner = UIFactory.CreateShape(lGroup, "Inner", UIShape.ShapeKind.Burst, lGo ? UIFactory.WHITE : UIFactory.RED, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(lGo ? 760f : 420f, lGo ? 420f : 420f));
                lInner.SetShape(UIShape.ShapeKind.Burst, 0f, lGo ? 22 : 14, 0.75f, i * 17 + 3);
                TextMeshProUGUI lText = UIFactory.CreateText(lGroup, lSteps[i], lGo ? 230f : 300f, lGo ? UIFactory.BLACK : UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(900f, 400f), true);
                lText.fontStyle = FontStyles.Italic;
                if (!lGo) UIFactory.Outline(lText, UIFactory.BLACK, 0.2f);
                AudioManager.Play(lGo ? "sfx_go" : "sfx_count", 1f);
                if (lGo)
                {
                    Juice.Flash(Color.white, 0.5f);
                    pOnGo?.Invoke();
                }

                float lDuration = lGo ? 0.9f : 0.75f;
                for (float lTime = 0f; lTime < lDuration; lTime += Time.unscaledDeltaTime)
                {
                    float lIn = Mathf.Clamp01(lTime / 0.12f);
                    float lScale = lIn < 1f ? Mathf.Lerp(2.6f, 1f, lIn * lIn) : 1f + (lTime - 0.12f) * 0.15f;
                    float lAlpha = lTime > lDuration - 0.2f ? (lDuration - lTime) / 0.2f : 1f;
                    lGroup.localScale = Vector3.one * lScale;
                    lGroup.localRotation = Quaternion.Euler(0f, 0f, (lGo ? -8f : 6f) + lTime * 10f);
                    lBurst.rectTransform.localRotation = Quaternion.Euler(0f, 0f, lTime * 60f);
                    lInner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -lTime * 40f);
                    lText.alpha = lAlpha;
                    lBurst.color = new Color(lBurst.color.r, lBurst.color.g, lBurst.color.b, lAlpha);
                    lInner.color = new Color(lInner.color.r, lInner.color.g, lInner.color.b, lAlpha);
                    if (lGo && lTime > 0.05f && lTime < 0.1f) Juice.Shake(0.5f);
                    yield return null;
                }
                Destroy(lGroup.gameObject);
                if (lGo) yield break;
            }
        }

        // ---------------------------------------------------------------- end screens

        public MenuList ShowResult(bool pWin, string pHeadline, float pTime, int pRank, bool pRecord, float pBest, bool pHasNext,
                                   System.Action pNext, System.Action pRetry, System.Action pMenu)
        {
            RectTransform lPanel = UIFactory.CreateRect(_root, "Result", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            Image lDim = UIFactory.CreateImage(lPanel, "Dim", new Color(0f, 0f, 0f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UIFactory.Stretch(lDim.rectTransform);

            Color lMain = pWin ? UIFactory.RED : UIFactory.BLACK;
            UIFactory.CreateShape(lPanel, "BannerShadow", UIShape.ShapeKind.Parallelogram, pWin ? UIFactory.BLACK : UIFactory.RED, new Vector2(0.5f, 0.5f), new Vector2(30f, 290f), new Vector2(2300f, 220f), -7f);
            UIFactory.CreateShape(lPanel, "Banner", UIShape.ShapeKind.Parallelogram, lMain, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(2300f, 200f), -7f);
            TextMeshProUGUI lHead = UIFactory.CreateText(lPanel, pHeadline, 170f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-120f, 300f), new Vector2(1800f, 220f), true);
            lHead.fontStyle = FontStyles.Italic;
            lHead.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -7f);
            UIFactory.Outline(lHead, UIFactory.BLACK, 0.15f);
            lHead.gameObject.AddComponent<UIJitter>().amount = 4f;

            // Rank
            if (pWin)
            {
                UIShape lBurst = UIFactory.CreateShape(lPanel, "RankBurst", UIShape.ShapeKind.Burst, UIFactory.BLACK, new Vector2(0.5f, 0.5f), new Vector2(560f, -40f), new Vector2(460f, 460f));
                lBurst.SetShape(UIShape.ShapeKind.Burst, 0f, 16, 0.7f, 42);
                lBurst.gameObject.AddComponent<UISpin>().speed = 20f;
                UIShape lBurst2 = UIFactory.CreateShape(lPanel, "RankBurst2", UIShape.ShapeKind.Burst, UIFactory.YELLOW, new Vector2(0.5f, 0.5f), new Vector2(560f, -40f), new Vector2(380f, 380f));
                lBurst2.SetShape(UIShape.ShapeKind.Burst, 0f, 16, 0.75f, 7);
                lBurst2.gameObject.AddComponent<UISpin>().speed = -30f;
                TextMeshProUGUI lRank = UIFactory.CreateText(lPanel, GameSession.RankLetter(pRank), 300f, UIFactory.RED, new Vector2(0.5f, 0.5f), new Vector2(560f, -30f), new Vector2(400f, 400f), true);
                lRank.fontStyle = FontStyles.Italic;
                UIFactory.Outline(lRank, UIFactory.BLACK, 0.2f);
                lRank.gameObject.AddComponent<UIJitter>().amount = 3f;
            }

            UIFactory.CreateShape(lPanel, "TimeBack", UIShape.ShapeKind.Parallelogram, UIFactory.BLACK, new Vector2(0.5f, 0.5f), new Vector2(-330f, 110f), new Vector2(720f, 90f), -4f);
            TextMeshProUGUI lTime = UIFactory.CreateText(lPanel, "TIME  " + GameSession.FormatTime(pTime), 56f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-330f, 112f), new Vector2(700f, 90f), true);
            lTime.fontStyle = FontStyles.Italic;
            lTime.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            string lBestLine = pRecord ? "<color=#FFE01A>NEW RECORD!</color>" : "BEST  " + GameSession.FormatTime(pBest);
            TextMeshProUGUI lBest = UIFactory.CreateText(lPanel, lBestLine, 40f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-300f, 40f), new Vector2(700f, 60f), false);
            lBest.fontStyle = FontStyles.Italic | FontStyles.Bold;
            UIFactory.Outline(lBest, UIFactory.BLACK, 0.25f);

            MenuList lMenu = MenuList.Create(lPanel, new Vector2(0.5f, 0.5f), new Vector2(-420f, -370f));
            if (pWin && pHasNext) lMenu.Add("NEXT STAGE", pNext);
            lMenu.Add("RETRY", pRetry);
            lMenu.Add("STAGE SELECT", pMenu);
            lMenu.SetSelected(0);
            lPanel.gameObject.AddComponent<UISlideIn>();
            return lMenu;
        }

        public GameObject ShowPause(System.Action pResume, System.Action pRetry, System.Action pMenu)
        {
            RectTransform lPanel = UIFactory.CreateRect(_root, "Pause", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            Image lDim = UIFactory.CreateImage(lPanel, "Dim", new Color(0f, 0f, 0f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UIFactory.Stretch(lDim.rectTransform);
            UIFactory.CreateShape(lPanel, "Slash", UIShape.ShapeKind.Parallelogram, UIFactory.RED, new Vector2(0.5f, 0.5f), new Vector2(-300f, 0f), new Vector2(500f, 1600f), 18f);
            TextMeshProUGUI lTitle = UIFactory.CreateText(lPanel, "PAUSE", 200f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-420f, 300f), new Vector2(900f, 260f), true);
            lTitle.fontStyle = FontStyles.Italic;
            lTitle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            UIFactory.Outline(lTitle, UIFactory.BLACK, 0.2f);
            MenuList lMenu = MenuList.Create(lPanel, new Vector2(0.5f, 0.5f), new Vector2(260f, -170f));
            lMenu.Add("RESUME", pResume);
            lMenu.Add("RESTART", pRetry);
            lMenu.Add("STAGE SELECT", pMenu);
            lMenu.OnBack = pResume;
            lMenu.SetSelected(0);
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
            ((RectTransform)transform).anchoredPosition = new Vector2(Mathf.Lerp(1920f, 0f, lT), 0f);
            if (lT >= 1f) enabled = false;
        }
    }
}
