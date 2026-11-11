using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Settings, the same screen from the title and from the pause : sound, display, camera, look sensitivity, comfort,
    // and the controls page (rebind every button, keyboard and gamepad). Left / right changes a value, confirm steps it.
    public class SettingsMenu : MonoBehaviour
    {
        private const float ROW_W = 760f;
        private const float ROW_H = 76f;
        private const float ROW_STEP = 88f;
        private const float COLUMN_X = 410f;
        private const float TOP_Y = 330f;

        private class Row
        {
            public TextMeshProUGUI value;
            public System.Func<string> read;
            public bool opens;
        }

        private RectTransform _root;
        private RectTransform _main;
        private RectTransform _controls;
        private RectTransform _credits;
        private UINav _mainNav;
        private UINav _controlsNav;
        private UINav _creditsNav;
        private readonly List<Row> _rows = new List<Row>();
        private readonly List<Row> _bindRows = new List<Row>();
        private System.Action _close;
        private List<Vector2Int> _resolutions;

        public bool IsOpen => _root != null && _root.gameObject.activeSelf;
        // The one on screen, if any : the pause key leaves it alone (Esc / B go back inside it).
        public static SettingsMenu Current { get; private set; }

        public static SettingsMenu Create(Transform pCanvas, bool pCredits, System.Action pClose)
        {
            RectTransform lRoot = UIFactory.Stretch(UIFactory.CreateRect(pCanvas, "Settings", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            SettingsMenu lMenu = lRoot.gameObject.AddComponent<SettingsMenu>();
            lMenu._root = lRoot;
            lMenu._close = pClose;
            lMenu.Build(pCredits);
            lRoot.gameObject.SetActive(false);
            return lMenu;
        }

        public void Open()
        {
            _root.gameObject.SetActive(true);
            Current = this;
            ShowPage(_main);
            _mainNav.Select(0);
            Refresh();
        }

        public void Close()
        {
            HSBindings.CancelRebind();
            _root.gameObject.SetActive(false);
            if (Current == this) Current = null;
            _close?.Invoke();
        }

        // ================================================================ build

        private RectTransform Page(string pName, string pTitle, float pDim)
        {
            RectTransform lPage = UIFactory.Stretch(UIFactory.CreateRect(_root, pName, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            lPage.gameObject.AddComponent<PerspectivePanel>();
            UIKit.FullScreen(lPage, "Dim", new Color(0.04f, 0.02f, 0.06f, pDim)).rectTransform.localScale = Vector3.one * 1.3f;
            TextMeshProUGUI lTitle = UIKit.Text(lPage, pTitle, 110f, UIKit.WHITE, new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(1200f, 140f), UIKit.Font.Heavy);
            lTitle.enableAutoSizing = true;
            lTitle.fontSizeMin = 50f;
            lTitle.fontSizeMax = 110f;
            return lPage;
        }

        private void Build(bool pCredits)
        {
            _main = Page("Main", "SETTINGS", 0.8f);
            _mainNav = UINav.Create(_main.gameObject);
            _mainNav.onBack = Close;

            int lLeft = 0;
            AddRow(_main, -1, lLeft++, "MUSIC", () => Percent(AudioManager.MusicMaster), d => { AudioManager.MusicMaster = Step01(AudioManager.MusicMaster, d); AudioManager.SaveVolumes(); });
            AddRow(_main, -1, lLeft++, "EFFECTS", () => Percent(AudioManager.MasterSfx), d => { AudioManager.MasterSfx = Step01(AudioManager.MasterSfx, d); AudioManager.SaveVolumes(); AudioManager.Play("sfx_jump", 0.8f); });
            AddRow(_main, -1, lLeft++, "RESOLUTION", ResolutionText, ChangeResolution);
            AddRow(_main, -1, lLeft++, "SCREEN", ScreenModeText, ChangeScreenMode);
            AddRow(_main, -1, lLeft++, "VSYNC", () => OnOff(GameSession.VSync), d => GameSession.VSync = !GameSession.VSync);
            AddRow(_main, -1, lLeft++, "QUALITY", () => QualitySettings.names[GameSession.Quality].ToUpperInvariant(), d => GameSession.Quality = (GameSession.Quality + d + QualitySettings.names.Length) % QualitySettings.names.Length);
            AddRow(_main, -1, lLeft++, "FIELD OF VIEW", () => Mathf.RoundToInt(GameSession.FovScale * 100f) + "%", d => GameSession.FovScale += d * 0.05f);
            AddRow(_main, -1, lLeft++, "WIDE ANGLE", () => OnOff(GameSession.WideAngle), d =>
            {
                GameSession.WideAngle = !GameSession.WideAngle;
                if (PanoramaRig.Active != null) PanoramaRig.Active.SetEnabled(GameSession.WideAngle);
            });
            AddRow(_main, -1, lLeft++, "CAMERA DISTANCE", () => Mathf.RoundToInt(GameSession.CameraDistance * 100f) + "%", d => GameSession.CameraDistance += d * 0.1f);
            if (pCredits) AddRow(_main, -1, lLeft++, "CREDITS", () => ">>", d => ShowPage(_credits), true);

            int lRight = 0;
            AddRow(_main, 1, lRight++, "CONTROLS", () => ">>", d => ShowPage(_controls), true);
            AddRow(_main, 1, lRight++, "MOUSE", () => GameSession.MouseSensitivity.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), d => GameSession.MouseSensitivity += d * 0.1f);
            AddRow(_main, 1, lRight++, "STICK HORIZONTAL", () => GameSession.PadSensitivityX.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), d => GameSession.PadSensitivityX += d * 0.1f);
            AddRow(_main, 1, lRight++, "STICK VERTICAL", () => GameSession.PadSensitivityY.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), d => GameSession.PadSensitivityY += d * 0.1f);
            AddRow(_main, 1, lRight++, "INVERT Y", () => OnOff(GameSession.InvertY), d => GameSession.InvertY = !GameSession.InvertY);
            AddRow(_main, 1, lRight++, "VIBRATION", () => OnOff(GameSession.Vibration), d => { GameSession.Vibration = !GameSession.Vibration; Juice.Rumble(0.6f); });
            AddRow(_main, 1, lRight++, "SCREEN SHAKE", () => Percent(GameSession.ShakeAmount), d => GameSession.ShakeAmount = Step01(GameSession.ShakeAmount, d));
            AddRow(_main, 1, lRight++, "ABERRATION", () => Percent(GameSession.AberrationAmount), d => GameSession.AberrationAmount = Step01(GameSession.AberrationAmount, d));
            AddRow(_main, 1, lRight++, "FLASHES", () => Percent(GameSession.FlashAmount), d => GameSession.FlashAmount = Step01(GameSession.FlashAmount, d));
            AddRow(_main, 1, lRight++, "WIDE QUALITY", () => GameSession.PanoramaQuality == 0 ? "LOW" : GameSession.PanoramaQuality == 1 ? "MEDIUM" : "HIGH", d => GameSession.PanoramaQuality = (GameSession.PanoramaQuality + d + 3) % 3);

            BackArrow(_main, _mainNav, Close);
            BuildControls();
            if (pCredits) BuildCredits();
        }

        private static string Percent(float pValue) => Mathf.RoundToInt(pValue * 100f) + "%";

        private static string OnOff(bool pValue) => pValue ? "ON" : "OFF";

        // Steps of 10 %, wrapping past the ends so one button can go round.
        private static float Step01(float pValue, int pDirection)
        {
            float lValue = Mathf.Round(pValue * 10f) / 10f + pDirection * 0.1f;
            if (lValue > 1.04f) lValue = 0f;
            if (lValue < -0.04f) lValue = 1f;
            return Mathf.Clamp01(lValue);
        }

        private void AddRow(RectTransform pPage, int pColumn, int pIndex, string pLabel, System.Func<string> pRead, System.Action<int> pChange, bool pOpens = false)
        {
            float lY = TOP_Y - pIndex * ROW_STEP;
            NavTile lTile = Tile(pPage, pLabel, new Vector2(pColumn * COLUMN_X, lY), new Vector2(ROW_W, ROW_H), out TextMeshProUGUI lValue);
            Row lRow = new Row { value = lValue, read = pRead, opens = pOpens };
            _rows.Add(lRow);
            System.Action<int> lChange = pDirection =>
            {
                pChange(pDirection);
                if (!pOpens) AudioManager.Play("ui_move", 0.5f, 1f + pDirection * 0.08f);
                Refresh();
            };
            _mainNav.Add((RectTransform)lTile.transform, new Vector2(pColumn, -pIndex), () => lChange(1), lTile.SetSelected, pOpens ? (System.Action<int>)null : lChange);
        }

        private NavTile Tile(RectTransform pParent, string pLabel, Vector2 pPosition, Vector2 pSize, out TextMeshProUGUI pValue, float pLabelShare = 0.55f)
        {
            RectTransform lHolder = UIFactory.CreateRect(pParent, pLabel, new Vector2(0.5f, 0.5f), pPosition, pSize);
            UIShape lFrame = UIKit.Tile(lHolder, "Frame", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(10f, -8f), pSize + new Vector2(14f, 14f), 0.3f);
            UIShape lBack = UIKit.Tile(lHolder, "Back", UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, pSize, 0.3f);
            float lLabelWidth = pSize.x * pLabelShare;
            TextMeshProUGUI lLabel = UIKit.Text(lHolder, pLabel, 40f, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(-pSize.x * 0.5f + 40f + lLabelWidth * 0.5f, 0f), new Vector2(lLabelWidth, pSize.y * 0.85f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            lLabel.enableAutoSizing = true;
            lLabel.fontSizeMin = 18f;
            lLabel.fontSizeMax = 40f;
            float lValueWidth = pSize.x - lLabelWidth - 80f;
            pValue = UIKit.Text(lHolder, "", 40f, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(pSize.x * 0.5f - 40f - lValueWidth * 0.5f, 0f), new Vector2(lValueWidth, pSize.y * 0.85f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            pValue.enableAutoSizing = true;
            pValue.fontSizeMin = 16f;
            pValue.fontSizeMax = 40f;
            NavTile lTile = lHolder.gameObject.AddComponent<NavTile>();
            lTile.back = lBack;
            lTile.frame = lFrame;
            lTile.label = lLabel;
            lTile.normalBack = UIKit.WHITE;
            lTile.selectedBack = UIKit.RED;
            lTile.normalText = UIKit.INK;
            lTile.selectedText = UIKit.WHITE;
            lTile.selectedScale = 1.04f;
            lHolder.gameObject.AddComponent<UIFollowColor>().Init(lLabel, pValue);
            return lTile;
        }

        private void BackArrow(RectTransform pParent, UINav pNav, System.Action pAction)
        {
            RectTransform lHolder = UIFactory.CreateRect(pParent, "Back", new Vector2(0f, 1f), new Vector2(66f, -58f), new Vector2(84f, 64f));
            UIShape lBack = UIKit.Tile(lHolder, "Plate", UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 56f), 0.3f);
            RectTransform lArrow = UIFactory.CreateRect(lHolder, "Arrow", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 30f));
            UIFactory.CreateShape(lArrow, "Head", UIShape.ShapeKind.Triangle, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(-8f, 0f), new Vector2(24f, 22f), 90f);
            UIFactory.CreateImage(lArrow, "Tail", UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(22f, 7f));
            pNav.Add(lHolder, new Vector2(-2f, 5f), pAction, pOn => lBack.color = pOn ? UIKit.GOLD : UIKit.WHITE);
        }

        // ---------------------------------------------------------------- controls

        private void BuildControls()
        {
            _controls = Page("Controls", "CONTROLS", 0.88f);
            _controlsNav = UINav.Create(_controls.gameObject);
            _controlsNav.onBack = () => ShowPage(_main);
            UIKit.Text(_controls, "KEYBOARD / MOUSE", 30f, UIKit.GOLD, new Vector2(0.5f, 0.5f), new Vector2(-40f, TOP_Y + 22f), new Vector2(520f, 44f), UIKit.Font.Heavy);
            UIKit.Text(_controls, "GAMEPAD", 30f, UIKit.GOLD, new Vector2(0.5f, 0.5f), new Vector2(500f, TOP_Y + 22f), new Vector2(420f, 44f), UIKit.Font.Heavy);
            for (int i = 0; i < HSBindings.REBINDABLE.Length; i++)
            {
                HSBindings.Entry lEntry = HSBindings.REBINDABLE[i];
                float lY = TOP_Y - 34f - i * (ROW_STEP - 10f);
                UIKit.Text(_controls, lEntry.label, 34f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-620f, lY), new Vector2(360f, 60f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
                AddBindCell(lEntry.action, false, new Vector2(-40f, lY), new Vector2(560f, ROW_H - 10f), i);
                AddBindCell(lEntry.action, true, new Vector2(500f, lY), new Vector2(420f, ROW_H - 10f), i);
            }
            float lBottom = TOP_Y - 34f - HSBindings.REBINDABLE.Length * (ROW_STEP - 10f) - 6f;
            NavTile lReset = Tile(_controls, "RESET ALL", new Vector2(230f, lBottom), new Vector2(420f, ROW_H - 6f), out TextMeshProUGUI lIgnored, 0.9f);
            _controlsNav.Add((RectTransform)lReset.transform, new Vector2(0.5f, -HSBindings.REBINDABLE.Length), () =>
            {
                HSBindings.ResetAll();
                RefreshBindings();
            }, lReset.SetSelected);
            UIKit.Text(_controls, "STEER : MOUSE   /   LEFT STICK        LOOK : MOUSE   /   RIGHT STICK        PAUSE : ESC   /   START", 26f, new Color(1f, 1f, 1f, 0.7f),
                new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(1800f, 40f), UIKit.Font.Bold);
            BackArrow(_controls, _controlsNav, () => ShowPage(_main));
            _controls.gameObject.SetActive(false);
        }

        private void AddBindCell(string pAction, bool pGamepad, Vector2 pPosition, Vector2 pSize, int pIndex)
        {
            NavTile lTile = Tile(_controls, "", pPosition, pSize, out TextMeshProUGUI lValue, 0.02f);
            lValue.alignment = TextAlignmentOptions.Center;
            lValue.rectTransform.anchoredPosition = Vector2.zero;
            lValue.rectTransform.sizeDelta = new Vector2(pSize.x - 60f, pSize.y * 0.8f);
            Row lRow = new Row { value = lValue, read = () => HSBindings.Display(pAction, pGamepad) };
            _bindRows.Add(lRow);
            _controlsNav.Add((RectTransform)lTile.transform, new Vector2(pGamepad ? 1f : 0f, -pIndex), () =>
            {
                lValue.text = pGamepad ? "PRESS A BUTTON..." : "PRESS A KEY...";
                HSBindings.Rebind(pAction, pGamepad, () =>
                {
                    AudioManager.Play("ui_select", 0.7f);
                    RefreshBindings();
                });
            }, lTile.SetSelected);
        }

        private void RefreshBindings()
        {
            foreach (Row lRow in _bindRows) lRow.value.text = lRow.read();
        }

        // ---------------------------------------------------------------- credits

        private void BuildCredits()
        {
            _credits = Page("Credits", "", 0.92f);
            TextAsset lCredits = Resources.Load<TextAsset>("Credits");
            RectTransform lTextRect = UIFactory.Stretch(UIFactory.CreateRect(_credits, "Text", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            lTextRect.offsetMin = new Vector2(120f, 50f);
            lTextRect.offsetMax = new Vector2(-120f, -110f);
            TextMeshProUGUI lText = lTextRect.gameObject.AddComponent<TextMeshProUGUI>();
            lText.font = GameAssets.BodyFont;
            lText.text = lCredits != null ? lCredits.text : "";
            lText.enableAutoSizing = true;
            lText.fontSizeMin = 8f;
            lText.fontSizeMax = 24f;
            lText.alignment = TextAlignmentOptions.Center;
            lText.color = UIKit.WHITE;
            lText.raycastTarget = false;
            _creditsNav = UINav.Create(_credits.gameObject);
            _creditsNav.onBack = () => ShowPage(_main);
            BackArrow(_credits, _creditsNav, () => ShowPage(_main));
            _credits.gameObject.SetActive(false);
        }

        // ================================================================ pages

        private void ShowPage(RectTransform pPage)
        {
            HSBindings.CancelRebind();
            _main.gameObject.SetActive(pPage == _main);
            _controls.gameObject.SetActive(pPage == _controls);
            if (_credits != null) _credits.gameObject.SetActive(pPage == _credits);
            if (pPage == _controls)
            {
                RefreshBindings();
                _controlsNav.Select(0);
            }
            else if (pPage == _main) _mainNav.Select(0);
            AudioManager.Play("ui_whoosh", 0.5f);
        }

        private void Refresh()
        {
            foreach (Row lRow in _rows) lRow.value.text = lRow.opens ? lRow.read() : "< " + lRow.read() + " >";
        }

        // ---------------------------------------------------------------- display

        private List<Vector2Int> Resolutions()
        {
            if (_resolutions != null) return _resolutions;
            _resolutions = new List<Vector2Int>();
            foreach (Resolution lResolution in Screen.resolutions)
            {
                Vector2Int lSize = new Vector2Int(lResolution.width, lResolution.height);
                if (lSize.x >= 800 && !_resolutions.Contains(lSize)) _resolutions.Add(lSize);
            }
            if (_resolutions.Count == 0) _resolutions.Add(new Vector2Int(Screen.width, Screen.height));
            _resolutions.Sort((pA, pB) => (pA.x * pA.y).CompareTo(pB.x * pB.y));
            return _resolutions;
        }

        private int CurrentResolution()
        {
            List<Vector2Int> lList = Resolutions();
            int lBest = lList.Count - 1;
            for (int i = 0; i < lList.Count; i++)
                if (lList[i].x == Screen.width && lList[i].y == Screen.height) lBest = i;
            return lBest;
        }

        private string ResolutionText() => Screen.width + " x " + Screen.height;

        private void ChangeResolution(int pDirection)
        {
            List<Vector2Int> lList = Resolutions();
            int lIndex = (CurrentResolution() + pDirection + lList.Count) % lList.Count;
            Screen.SetResolution(lList[lIndex].x, lList[lIndex].y, Screen.fullScreenMode);
        }

        private static readonly FullScreenMode[] MODES = { FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen, FullScreenMode.Windowed };

        private string ScreenModeText()
        {
            switch (Screen.fullScreenMode)
            {
                case FullScreenMode.ExclusiveFullScreen: return "FULLSCREEN";
                case FullScreenMode.Windowed: return "WINDOWED";
                default: return "BORDERLESS";
            }
        }

        private void ChangeScreenMode(int pDirection)
        {
            int lIndex = System.Array.IndexOf(MODES, Screen.fullScreenMode);
            if (lIndex < 0) lIndex = 0;
            lIndex = (lIndex + pDirection + MODES.Length) % MODES.Length;
            Screen.fullScreenMode = MODES[lIndex];
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private float _refreshTimer;

        private void Update()
        {
            // Resolution and window changes land a frame later : keep the shown values true.
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f || !IsOpen) return;
            _refreshTimer = 0.3f;
            if (_main.gameObject.activeSelf) Refresh();
            else if (_controls.gameObject.activeSelf && !HSBindings.Rebinding) RefreshBindings();
        }
    }
}
