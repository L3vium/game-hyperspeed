using System.Collections.Generic;
using Com.IsartDigital.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Title scene : 3D runner backdrop + Boost-Vector-like menus (tiles, chevron cards, outlined giant words).
    public class TitleScreen : MonoBehaviour
    {
        private enum Screen { Press, Main, Stages, Settings, Controls }

        public static bool OpenLevelSelect;

        private Screen _screen = Screen.Press;
        private RectTransform _root;
        private Camera _camera;
        private PlayerVisual _display;
        private Transform _stage;
        private Material _speedLines;
        private float _orbit;
        private int _enteredFrame;

        private RectTransform _pressGroup;
        private RectTransform _mainGroup;
        private RectTransform _stagesGroup;
        private RectTransform _settingsGroup;
        private RectTransform _controlsGroup;
        private UINav _mainNav;
        private UINav _settingsNav;
        private TextMeshProUGUI _press;

        // Stage carousel
        private readonly List<RectTransform> _cards = new List<RectTransform>();
        private readonly List<CanvasGroup> _cardGroups = new List<CanvasGroup>();
        private int _selectedStage;
        private float _carousel;
        private TextMeshProUGUI _stageTitle;
        private TextMeshProUGUI _stageTag;
        private TextMeshProUGUI _stageBest;
        private TextMeshProUGUI _leagueText;
        private RectTransform _goButton;
        private RectTransform _backButton;
        private RectTransform _arrowLeft;
        private RectTransform _arrowRight;
        private TextMeshProUGUI _musicValue;
        private TextMeshProUGUI _sfxValue;

        private void Awake()
        {
            Time.timeScale = 1f;
            RaceContext.Clear();
            AudioManager.LoadVolumes();
            BuildBackdrop();
            BuildUI();
            if (OpenLevelSelect)
            {
                OpenLevelSelect = false;
                ShowStages();
            }
            else SetScreen(Screen.Press);
        }

        private void Start()
        {
            AudioManager.PlayMusic("music_title", 0.5f);
        }

        // ================================================================ 3D backdrop

        private void BuildBackdrop()
        {
            Theme lTheme = Theme.Islands();
            Transform lRoot = new GameObject("Backdrop").transform;
            EnvironmentBuilder.Build(lTheme, lRoot);
            _camera = CameraRig.CreateCamera();
            _camera.fieldOfView = 62f;
            new GameObject("PostFX").AddComponent<PostFX>().speed01 = 0.45f;

            _stage = new GameObject("Stage").transform;
            _stage.SetParent(lRoot, false);
            for (int i = 0; i < 24; i++)
            {
                Color lColor = i % 2 == 0 ? lTheme.roadA : lTheme.roadB;
                PropFactory.Part(PrimitiveType.Cube, _stage, new Vector3(0f, -1.25f, i * 8f - 40f), new Vector3(16f, 2.5f, 8f), lColor, default(Vector3), 0f).name = "Tile";
            }
            PropFactory.Part(PrimitiveType.Cube, _stage, new Vector3(-7.6f, -0.05f, 50f), new Vector3(0.8f, 0.1f, 200f), lTheme.roadEdge, default(Vector3), 0f);
            PropFactory.Part(PrimitiveType.Cube, _stage, new Vector3(7.6f, -0.05f, 50f), new Vector3(0.8f, 0.1f, 200f), lTheme.roadEdge, default(Vector3), 0f);
            Random.InitState(5);
            for (int i = 0; i < 60; i++)
            {
                float lSide = i % 2 == 0 ? -1f : 1f;
                Vector3 lPosition = new Vector3(lSide * Random.Range(30f, 160f), Random.Range(-40f, 5f), Random.Range(-80f, 600f));
                PropFactory.Decor(DecorKind.Islands, lTheme, _stage, lPosition, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Random.Range(1f, 2.5f));
            }
            _display = PlayerVisual.CreateDisplay(lRoot);
            _display.transform.position = Vector3.zero;
        }

        private void Update()
        {
            float lDt = Time.deltaTime;
            _display.TickDisplay(lDt, 72f);
            foreach (Transform lChild in _stage)
            {
                bool lTile = lChild.name == "Tile";
                if (!lTile && lChild.name != "Decor") continue;
                Vector3 lPosition = lChild.localPosition;
                lPosition.z -= lDt * (lTile ? 60f : 25f);
                if (lTile && lPosition.z < -48f) lPosition.z += 24 * 8f;
                else if (!lTile && lPosition.z < -100f) lPosition.z += 700f;
                lChild.localPosition = lPosition;
            }

            _orbit += lDt * 9f;
            bool lSide = _screen == Screen.Main || _screen == Screen.Settings || _screen == Screen.Controls;
            float lAngle = Mathf.Sin(_orbit * Mathf.Deg2Rad * 2f) * 25f + (lSide ? -28f : -10f);
            Vector3 lOffset = Quaternion.Euler(0f, lAngle, 0f) * new Vector3(0f, 1.9f, -5.2f);
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, lOffset, 1f - Mathf.Exp(-3f * lDt));
            Vector3 lLook = lSide ? new Vector3(-6f, 1.1f, 1.5f) : new Vector3(0f, 1.4f, 2f);
            _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation, Quaternion.LookRotation(lLook - _camera.transform.position), 1f - Mathf.Exp(-4f * lDt));
            _speedLines.SetFloat("_Intensity", 0.5f + Mathf.Sin(Time.time * 2f) * 0.06f);

            AnimateGroups(Time.unscaledDeltaTime);
            if (Time.frameCount <= _enteredFrame + 1 || SceneTransition.IsBusy) return;
            switch (_screen)
            {
                case Screen.Press:
                    _press.alpha = 0.55f + Mathf.Sin(Time.unscaledTime * 5f) * 0.45f;
                    if (HSInput.AnyPressed)
                    {
                        AudioManager.Play("ui_select", 0.9f);
                        Juice.Flash(Color.white, 0.4f);
                        ShowMain();
                    }
                    break;
                case Screen.Stages:
                    UpdateStages();
                    break;
                case Screen.Controls:
                    if (HSInput.BackPressed || HSInput.SubmitPressed || HSInput.MouseClicked)
                    {
                        AudioManager.Play("ui_back", 0.7f);
                        ShowMain();
                    }
                    break;
            }
        }

        // ================================================================ UI construction

        private RectTransform Group(string pName)
        {
            RectTransform lGroup = UIFactory.Stretch(UIFactory.CreateRect(_root, pName, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            lGroup.gameObject.AddComponent<CanvasGroup>();
            return lGroup;
        }

        private void BuildUI()
        {
            Canvas lCanvas = UIFactory.CreateCanvas("TitleCanvas", 10, transform);
            _root = (RectTransform)lCanvas.transform;
            RawImage lLines = UIFactory.CreateRect(_root, "SpeedLines", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            UIFactory.Stretch(lLines.rectTransform);
            _speedLines = new Material(Shader.Find("Hyperspeed/SpeedLines"));
            lLines.material = _speedLines;
            lLines.raycastTarget = false;

            BuildPress();
            BuildMain();
            BuildStages();
            BuildSettings();
            BuildControls();

            if (GameSession.CheatsEnabled)
            {
                UIShape lBadge = UIKit.Tile(_root, "DevBadge", UIKit.GOLD, new Vector2(1f, 1f), new Vector2(-170f, -40f), new Vector2(300f, 46f), 0.4f);
                UIKit.Text(lBadge.transform, "DEV BUILD  -  CHEATS ON", 24f, UIKit.INK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 46f), UIKit.Font.Heavy);
            }
        }

        private void BuildPress()
        {
            _pressGroup = Group("Press");
            UIKit.OutlineText(_pressGroup, "HYPER SPEED", 330f, new Color(1f, 1f, 1f, 0.35f), new Vector2(0.5f, 0.5f), new Vector2(-20f, 230f), new Vector2(2400f, 380f), -6f, 0.08f);
            UIShape lSlash = UIKit.Tile(_pressGroup, "Slash", UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2(-60f, 200f), new Vector2(1500f, 250f), 0.55f, -6f);
            UIKit.Chevrons(lSlash.transform, "Chevrons", UIKit.RED, UIKit.RED_DARK, 80f);
            lSlash.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            TextMeshProUGUI lShadow = UIKit.Text(_pressGroup, "HYPER SPEED", 210f, UIKit.RED_DEEP, new Vector2(0.5f, 0.5f), new Vector2(-50f, 186f), new Vector2(2000f, 260f), UIKit.Font.Heavy, TextAlignmentOptions.Center, true, -6f);
            TextMeshProUGUI lLogo = UIKit.Text(_pressGroup, "HYPER SPEED", 210f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-62f, 198f), new Vector2(2000f, 260f), UIKit.Font.Heavy, TextAlignmentOptions.Center, true, -6f);
            lLogo.characterSpacing = -6f;
            lShadow.characterSpacing = -6f;
            lLogo.gameObject.AddComponent<UIJitter>().amount = 1.5f;
            UIShape lTag = UIKit.Tile(_pressGroup, "Tag", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(160f, 40f), new Vector2(760f, 64f), 0.5f, -6f);
            UIKit.Text(lTag.transform, "RACE AGAINST ANYTHING", 40f, UIKit.INK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 64f), UIKit.Font.Heavy);
            _press = UIKit.Text(_pressGroup, "PRESS ANY BUTTON", 48f, UIKit.WHITE, new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(1200f, 80f), UIKit.Font.Heavy);
            UIFactory.Outline(_press, UIKit.INK, 0.25f);
            UIKit.Checker(_pressGroup, "Checker", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(2200f, 56f), UIKit.WHITE, UIKit.RED_DARK, 60f);
        }

        private NavTile MakeTile(Transform pParent, string pName, string pLabel, Vector2 pPosition, Vector2 pSize, Color pNormal, Color pSelected, Color pText, Color pSelectedText, float pFontSize)
        {
            RectTransform lHolder = UIFactory.CreateRect(pParent, pName, new Vector2(0.5f, 0.5f), pPosition, pSize);
            UIShape lFrame = UIKit.Tile(lHolder, "Frame", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(10f, -8f), pSize + new Vector2(14f, 14f), 0.3f);
            UIShape lBack = UIKit.Tile(lHolder, "Back", pNormal, new Vector2(0.5f, 0.5f), Vector2.zero, pSize, 0.3f);
            TextMeshProUGUI lLabel = UIKit.Text(lHolder, pLabel, pFontSize, pText, new Vector2(0.5f, 0.5f), Vector2.zero, pSize, UIKit.Font.Heavy);
            NavTile lTile = lHolder.gameObject.AddComponent<NavTile>();
            lTile.back = lBack;
            lTile.frame = lFrame;
            lTile.label = lLabel;
            lTile.normalBack = pNormal;
            lTile.selectedBack = pSelected;
            lTile.normalText = pText;
            lTile.selectedText = pSelectedText;
            lTile.SetSelected(false);
            return lTile;
        }

        private void BuildMain()
        {
            _mainGroup = Group("Main");
            UIShape lPanel = UIKit.Tile(_mainGroup, "RedPanel", UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2(-480f, 0f), new Vector2(1500f, 1300f), 0.16f);
            UIKit.Chevrons(lPanel.transform, "Chevrons", UIKit.RED, new Color(0.8f, 0.04f, 0.09f), 40f);
            lPanel.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            UIKit.OutlineText(_mainGroup, "HYPER", 300f, new Color(1f, 1f, 1f, 0.45f), new Vector2(0.5f, 0.5f), new Vector2(-560f, 360f), new Vector2(1400f, 320f), 0f, 0.07f);
            UIKit.OutlineText(_mainGroup, "SPEED", 300f, new Color(1f, 1f, 1f, 0.25f), new Vector2(0.5f, 0.5f), new Vector2(-300f, -380f), new Vector2(1400f, 320f), 0f, 0.07f);

            int lNext = Mathf.Clamp(GameSession.UnlockedCount - 1, 0, LevelDatabase.Count - 1);
            LevelDefinition lNextLevel = LevelDatabase.Get(lNext);
            NavTile lPlay = MakeTile(_mainGroup, "Play", "PLAY", new Vector2(-470f, 80f), new Vector2(880f, 250f), UIKit.RED_DARK, UIKit.RED_DEEP, UIKit.WHITE, UIKit.WHITE, 190f);
            lPlay.label.alignment = TextAlignmentOptions.Left;
            lPlay.label.margin = new Vector4(80f, 0f, 0f, 0f);
            UIShape lStrip = UIKit.Tile(lPlay.transform, "Next", UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(-120f, 150f), new Vector2(620f, 52f), 0.5f);
            UIKit.Text(lStrip.transform, "STAGE " + (lNext + 1).ToString("00") + "  |  " + lNextLevel.title, 26f, UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 52f), UIKit.Font.Bold);
            RawImage lPlayIcon = UIKit.Icon(lPlay.transform, "play", new Color(1f, 1f, 1f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(300f, 0f), 140f);

            NavTile lStages = MakeTile(_mainGroup, "Stages", "STAGE SELECT", new Vector2(-640f, -160f), new Vector2(520f, 110f), UIKit.WHITE, UIKit.INK, UIKit.INK, UIKit.WHITE, 54f);
            NavTile lControls = MakeTile(_mainGroup, "Controls", "CONTROLS", new Vector2(-120f, -160f), new Vector2(420f, 110f), UIKit.WHITE, UIKit.INK, UIKit.INK, UIKit.WHITE, 54f);
            NavTile lSettings = MakeTile(_mainGroup, "Settings", "", new Vector2(250f, 110f), new Vector2(330f, 300f), UIKit.ORANGE, new Color(1f, 0.75f, 0.3f), UIKit.INK, UIKit.INK, 40f);
            UIKit.Icon(lSettings.transform, "gear", new Color(1f, 1f, 1f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(20f, 40f), 170f).gameObject.AddComponent<UISpin>().speed = -30f;
            UIShape lSettingsBand = UIKit.Tile(lSettings.transform, "Band", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-10f, -105f), new Vector2(300f, 56f), 0.3f);
            UIKit.Text(lSettingsBand.transform, "SETTINGS", 36f, UIKit.INK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 56f), UIKit.Font.Heavy);
            NavTile lQuit = MakeTile(_mainGroup, "Quit", "EXIT GAME", new Vector2(-700f, -320f), new Vector2(400f, 80f), UIKit.RED_DEEP, UIKit.INK, new Color(1f, 1f, 1f, 0.55f), UIKit.WHITE, 40f);

            RectTransform lNow = UIFactory.CreateRect(_mainGroup, "NowPlaying", new Vector2(0f, 0f), new Vector2(330f, 90f), new Vector2(560f, 60f));
            UIKit.Icon(lNow, "note", UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(30f, 0f), 46f);
            UIKit.Text(lNow, "NOW PLAYING\n<size=22>OST - Race Against Anything</size>", 26f, UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(300f, 0f), new Vector2(480f, 60f), UIKit.Font.Bold, TextAlignmentOptions.Left, false);
            UIKit.Checker(_mainGroup, "Checker", new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(2200f, 44f), UIKit.WHITE, UIKit.RED_DARK, 40f);

            _mainNav = UINav.Create(_mainGroup.gameObject);
            _mainNav.Add((RectTransform)lPlay.transform, new Vector2(-470f, 80f), () => LaunchStage(lNext), lPlay.SetSelected);
            _mainNav.Add((RectTransform)lSettings.transform, new Vector2(250f, 110f), ShowSettings, lSettings.SetSelected);
            _mainNav.Add((RectTransform)lStages.transform, new Vector2(-640f, -160f), ShowStages, lStages.SetSelected);
            _mainNav.Add((RectTransform)lControls.transform, new Vector2(-120f, -160f), ShowControls, lControls.SetSelected);
            _mainNav.Add((RectTransform)lQuit.transform, new Vector2(-700f, -320f), Quit, lQuit.SetSelected);
            _mainNav.onBack = () => SetScreen(Screen.Press);
        }

        private void BuildStages()
        {
            _stagesGroup = Group("Stages");
            UIKit.Chevrons(_stagesGroup, "Chevrons", UIKit.RED, new Color(0.8f, 0.04f, 0.09f), 60f);
            UIKit.OutlineText(_stagesGroup, "START", 420f, new Color(1f, 1f, 1f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(-560f, 120f), new Vector2(1400f, 450f), 0f, 0.05f);
            _leagueText = UIKit.OutlineText(_stagesGroup, "", 130f, new Color(1f, 1f, 1f, 0.8f), new Vector2(1f, 0f), new Vector2(-560f, 110f), new Vector2(1100f, 160f), 0f, 0.1f, TextAlignmentOptions.Right);

            for (int i = 0; i < LevelDatabase.Count; i++)
            {
                LevelDefinition lLevel = LevelDatabase.Levels[i];
                RectTransform lCard = UIFactory.CreateRect(_stagesGroup, "Card" + i, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 440f));
                _cardGroups.Add(lCard.gameObject.AddComponent<CanvasGroup>());
                UIShape lBorder = UIFactory.CreateShape(lCard, "Border", UIShape.ShapeKind.Chevron, UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 480f));
                lBorder.skew = 0.42f;
                UIShape lInner = UIFactory.CreateShape(lCard, "Inner", UIShape.ShapeKind.Chevron, UIKit.RED_DEEP, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 440f));
                lInner.skew = 0.42f;
                bool lOpen = GameSession.IsUnlocked(i);
                RawImage lThumb = UIKit.MaskedImage(lInner, RivalThumbnails.Get(i));
                if (!lOpen) lThumb.color = new Color(0.25f, 0.2f, 0.25f);
                string lNumber = (lLevel.world + 1) + "-" + (IndexInWorld(i) + 1);
                TextMeshProUGUI lNumberText = UIKit.Text(lCard, lNumber, 120f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-170f, -140f), new Vector2(400f, 150f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
                UIFactory.Outline(lNumberText, UIKit.INK, 0.2f);
                int lRank = GameSession.GetRank(i);
                RawImage lTrophy = UIKit.Icon(lCard, lOpen ? "trophy" : "lock", lRank > 0 ? UIKit.GOLD : new Color(1f, 1f, 1f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(-250f, 150f), 90f);
                if (lRank > 0)
                {
                    TextMeshProUGUI lRankText = UIKit.Text(lCard, GameSession.RankLetter(lRank), 80f, UIKit.GOLD, new Vector2(0.5f, 0.5f), new Vector2(-160f, 150f), new Vector2(120f, 110f), UIKit.Font.Heavy);
                    UIFactory.Outline(lRankText, UIKit.INK, 0.25f);
                }
                if (!lOpen) UIKit.Text(lCard, "LOCKED", 64f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(40f, 20f), new Vector2(500f, 90f), UIKit.Font.Heavy);
                _cards.Add(lCard);
            }

            UIShape lBanner = UIKit.Tile(_stagesGroup, "Banner", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-40f, -330f), new Vector2(1300f, 150f), 0.3f, 4f);
            _stageTitle = UIKit.Text(lBanner.transform, "", 74f, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(-40f, 18f), new Vector2(1150f, 90f), UIKit.Font.Thin, TextAlignmentOptions.Left);
            _stageTag = UIKit.Text(lBanner.transform, "", 28f, UIKit.RED_DARK, new Vector2(0.5f, 0.5f), new Vector2(-40f, -44f), new Vector2(1150f, 40f), UIKit.Font.Bold, TextAlignmentOptions.Left);
            _stageBest = UIKit.Text(lBanner.transform, "", 30f, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(430f, 30f), new Vector2(400f, 50f), UIKit.Font.Heavy, TextAlignmentOptions.Right);

            UIShape lGo = UIFactory.CreateShape(_stagesGroup, "Go", UIShape.ShapeKind.Chevron, UIKit.RED_DEEP, new Vector2(0.5f, 0.5f), new Vector2(690f, -380f), new Vector2(330f, 140f));
            lGo.skew = 0.45f;
            UIKit.Text(lGo.transform, "GO", 110f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-10f, 0f), new Vector2(300f, 140f), UIKit.Font.Heavy);
            _goButton = lGo.rectTransform;
            _goButton.gameObject.AddComponent<UIPulse>();

            UIShape lBack = UIKit.Tile(_stagesGroup, "Back", new Color(0.3f, 0.3f, 0.32f), new Vector2(0f, 1f), new Vector2(230f, -80f), new Vector2(330f, 100f), 0.45f);
            UIKit.Checker(lBack.transform, "Checker", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 100f), new Color(0.45f, 0.45f, 0.47f), new Color(0.35f, 0.35f, 0.37f));
            UIKit.Icon(lBack.transform, "menu", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-90f, 0f), 60f);
            UIKit.Text(lBack.transform, "BACK", 52f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(220f, 100f), UIKit.Font.Thin);
            _backButton = lBack.rectTransform;

            _arrowLeft = UIKit.Icon(_stagesGroup, "arrow_left", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-860f, 40f), 120f).rectTransform;
            _arrowRight = UIKit.Icon(_stagesGroup, "arrow_right", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(860f, 40f), 120f).rectTransform;
            UIKit.Checker(_stagesGroup, "Checker", new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(2200f, 44f), UIKit.WHITE, UIKit.RED_DARK, -40f);
        }

        private static int IndexInWorld(int pIndex)
        {
            int lCount = 0;
            for (int i = 0; i < pIndex; i++)
                if (LevelDatabase.Levels[i].world == LevelDatabase.Levels[pIndex].world) lCount++;
            return lCount;
        }

        private void BuildSettings()
        {
            _settingsGroup = Group("Settings");
            UIShape lPanel = UIKit.Tile(_settingsGroup, "Panel", UIKit.ORANGE, new Vector2(0.5f, 0.5f), new Vector2(-380f, 0f), new Vector2(1100f, 900f), 0.12f);
            UIKit.OutlineText(_settingsGroup, "SETTINGS", 200f, new Color(1f, 1f, 1f, 0.7f), new Vector2(0.5f, 0.5f), new Vector2(-420f, 330f), new Vector2(1300f, 220f), 0f, 0.08f);
            NavTile lMusic = MakeTile(_settingsGroup, "Music", "", new Vector2(-380f, 120f), new Vector2(860f, 110f), UIKit.WHITE, UIKit.INK, UIKit.INK, UIKit.WHITE, 46f);
            _musicValue = lMusic.label;
            NavTile lSfx = MakeTile(_settingsGroup, "Sfx", "", new Vector2(-380f, -20f), new Vector2(860f, 110f), UIKit.WHITE, UIKit.INK, UIKit.INK, UIKit.WHITE, 46f);
            _sfxValue = lSfx.label;
            NavTile lBack = MakeTile(_settingsGroup, "Back", "BACK", new Vector2(-520f, -200f), new Vector2(420f, 100f), new Color(0.3f, 0.3f, 0.32f), UIKit.INK, UIKit.WHITE, UIKit.WHITE, 48f);
            if (GameSession.CheatsEnabled)
                UIKit.Text(_settingsGroup, "CHEATS :  F1 invincible   F2 hyper   F3 next checkpoint   F4 win   F5 slow-mo   F9 autopilot", 26f, UIKit.INK,
                    new Vector2(0.5f, 0.5f), new Vector2(-380f, -330f), new Vector2(1000f, 60f), UIKit.Font.Bold);
            _settingsNav = UINav.Create(_settingsGroup.gameObject);
            _settingsNav.Add((RectTransform)lMusic.transform, new Vector2(0f, 1f), null, lMusic.SetSelected, pDir => ChangeVolume(true, pDir));
            _settingsNav.Add((RectTransform)lSfx.transform, new Vector2(0f, 0f), null, lSfx.SetSelected, pDir => ChangeVolume(false, pDir));
            _settingsNav.Add((RectTransform)lBack.transform, new Vector2(0f, -1f), ShowMain, lBack.SetSelected);
            _settingsNav.onBack = ShowMain;
            RefreshVolumes();
        }

        private void ChangeVolume(bool pMusic, int pDir)
        {
            if (pMusic) AudioManager.MusicMaster = Mathf.Clamp01(AudioManager.MusicMaster + pDir * 0.1f);
            else AudioManager.MasterSfx = Mathf.Clamp01(AudioManager.MasterSfx + pDir * 0.1f);
            AudioManager.SaveVolumes();
            AudioManager.Play("ui_move", 0.8f);
            RefreshVolumes();
        }

        private void RefreshVolumes()
        {
            _musicValue.text = "MUSIC     <  " + Mathf.RoundToInt(AudioManager.MusicMaster * 100f) + "%  >";
            _sfxValue.text = "EFFECTS     <  " + Mathf.RoundToInt(AudioManager.MasterSfx * 100f) + "%  >";
        }

        private void BuildControls()
        {
            _controlsGroup = Group("Controls");
            UIShape lPanel = UIKit.Tile(_controlsGroup, "Panel", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-300f, -20f), new Vector2(1300f, 860f), 0.1f);
            UIKit.OutlineText(_controlsGroup, "CONTROLS", 200f, new Color(1f, 1f, 1f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(-380f, 400f), new Vector2(1300f, 220f), 0f, 0.08f);
            string[] lLines =
            {
                "STEER", "A / D   -   Left stick",
                "JUMP / DOUBLE / WALL JUMP", "Space   -   A",
                "DASH (swirl)", "Shift / Right click   -   RB / B",
                "DRIFT (ground)  /  DIVE (air)", "Ctrl / C   -   LT / LB",
                "PUNCH", "E / Left click   -   X / RT",
                "PERFECT START", "Space / W right on GO",
                "RESTART  /  PAUSE", "R  /  Esc   -   Select / Start"
            };
            for (int i = 0; i < lLines.Length; i += 2)
            {
                float lY = 260f - i * 42f;
                UIKit.Text(_controlsGroup, lLines[i], 34f, UIKit.RED_DARK, new Vector2(0.5f, 0.5f), new Vector2(-560f, lY), new Vector2(560f, 60f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
                UIKit.Text(_controlsGroup, lLines[i + 1], 34f, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(40f, lY), new Vector2(620f, 60f), UIKit.Font.Bold, TextAlignmentOptions.Left, false);
            }
            UIKit.Text(_controlsGroup, "Boost pads, rings and PERFECT landings take you to HYPER SPEED.  Over 60 m/s you smash everything.", 26f, UIKit.INK,
                new Vector2(0.5f, 0.5f), new Vector2(-300f, -330f), new Vector2(1150f, 60f), UIKit.Font.Bold);
        }

        // ================================================================ screens

        private void SetScreen(Screen pScreen)
        {
            _screen = pScreen;
            _enteredFrame = Time.frameCount;
            _mainNav.active = pScreen == Screen.Main;
            _settingsNav.active = pScreen == Screen.Settings;
            foreach (RectTransform lGroup in new[] { _pressGroup, _mainGroup, _stagesGroup, _settingsGroup, _controlsGroup })
            {
                bool lOn = (lGroup == _pressGroup && pScreen == Screen.Press) || (lGroup == _mainGroup && pScreen == Screen.Main) ||
                           (lGroup == _stagesGroup && pScreen == Screen.Stages) || (lGroup == _settingsGroup && pScreen == Screen.Settings) ||
                           (lGroup == _controlsGroup && pScreen == Screen.Controls);
                if (lOn && !lGroup.gameObject.activeSelf)
                {
                    lGroup.gameObject.SetActive(true);
                    lGroup.anchoredPosition = new Vector2(-500f, 0f);
                    lGroup.GetComponent<CanvasGroup>().alpha = 0f;
                }
                else if (!lOn) lGroup.gameObject.SetActive(false);
            }
            AudioManager.Play("ui_whoosh", 0.5f, 1.2f);
        }

        private void AnimateGroups(float pDt)
        {
            foreach (RectTransform lGroup in new[] { _pressGroup, _mainGroup, _stagesGroup, _settingsGroup, _controlsGroup })
            {
                if (!lGroup.gameObject.activeSelf) continue;
                lGroup.anchoredPosition = Vector2.Lerp(lGroup.anchoredPosition, Vector2.zero, 1f - Mathf.Exp(-12f * pDt));
                CanvasGroup lCanvas = lGroup.GetComponent<CanvasGroup>();
                lCanvas.alpha = Mathf.MoveTowards(lCanvas.alpha, 1f, pDt * 5f);
            }
        }

        private void ShowMain()
        {
            SetScreen(Screen.Main);
            _mainNav.Select(0);
        }

        private void ShowSettings()
        {
            SetScreen(Screen.Settings);
            _settingsNav.Select(0);
        }

        private void ShowControls() => SetScreen(Screen.Controls);

        private void ShowStages()
        {
            SetScreen(Screen.Stages);
            _selectedStage = Mathf.Clamp(Mathf.Min(GameSession.SelectedLevel, GameSession.UnlockedCount - 1), 0, LevelDatabase.Count - 1);
            _carousel = _selectedStage;
        }

        private void UpdateStages()
        {
            int lPrevious = _selectedStage;
            Vector2Int lMove = HSInput.MenuMove;
            if (lMove.x != 0) _selectedStage = Mathf.Clamp(_selectedStage + lMove.x, 0, LevelDatabase.Count - 1);

            Vector2 lMouse = HSInput.MousePosition;
            if (HSInput.MouseClicked)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_backButton, lMouse, null)) { AudioManager.Play("ui_back", 0.7f); ShowMain(); return; }
                if (RectTransformUtility.RectangleContainsScreenPoint(_goButton, lMouse, null)) { LaunchStage(_selectedStage); return; }
                if (RectTransformUtility.RectangleContainsScreenPoint(_arrowLeft, lMouse, null)) _selectedStage = Mathf.Max(0, _selectedStage - 1);
                else if (RectTransformUtility.RectangleContainsScreenPoint(_arrowRight, lMouse, null)) _selectedStage = Mathf.Min(LevelDatabase.Count - 1, _selectedStage + 1);
                else
                {
                    for (int i = 0; i < _cards.Count; i++)
                    {
                        if (!RectTransformUtility.RectangleContainsScreenPoint(_cards[i], lMouse, null)) continue;
                        if (i == _selectedStage) { LaunchStage(i); return; }
                        _selectedStage = i;
                        break;
                    }
                }
            }
            if (_selectedStage != lPrevious) AudioManager.Play("ui_move", 0.7f);

            _carousel = Mathf.Lerp(_carousel, _selectedStage, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            for (int i = 0; i < _cards.Count; i++)
            {
                float lOffset = i - _carousel;
                float lAbs = Mathf.Abs(lOffset);
                RectTransform lCard = _cards[i];
                lCard.anchoredPosition = new Vector2(lOffset * 640f - lAbs * 40f, 60f - lAbs * 30f);
                lCard.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(lAbs));
                lCard.localRotation = Quaternion.Euler(0f, 0f, i == _selectedStage ? Mathf.Sin(Time.unscaledTime * 3f) * 1.2f : 0f);
                _cardGroups[i].alpha = Mathf.Clamp01(1.6f - lAbs * 0.7f);
                lCard.gameObject.SetActive(lAbs < 2.5f);
                if (lAbs < 0.5f) lCard.SetAsLastSibling();
            }
            _goButton.SetAsLastSibling();

            LevelDefinition lLevel = LevelDatabase.Levels[_selectedStage];
            bool lOpen = GameSession.IsUnlocked(_selectedStage);
            _stageTitle.text = lOpen ? lLevel.title : "???";
            _stageTag.text = lOpen ? lLevel.tagline : "Beat the previous stage to unlock.";
            float lBest = GameSession.GetBestTime(_selectedStage);
            _stageBest.text = lOpen ? "BEST  " + GameSession.FormatTime(lBest) : "";
            _leagueText.text = LevelDatabase.WORLDS[lLevel.world];
            float lPulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.08f;
            _arrowLeft.localScale = Vector3.one * (_selectedStage > 0 ? lPulse : 0.5f);
            _arrowRight.localScale = Vector3.one * (_selectedStage < LevelDatabase.Count - 1 ? lPulse : 0.5f);

            if (HSInput.SubmitPressed) LaunchStage(_selectedStage);
            else if (HSInput.BackPressed)
            {
                AudioManager.Play("ui_back", 0.7f);
                ShowMain();
            }
        }

        private void LaunchStage(int pIndex)
        {
            if (!GameSession.IsUnlocked(pIndex))
            {
                AudioManager.Play("ui_locked", 0.8f);
                Juice.Shake(0.2f);
                return;
            }
            if (SceneTransition.IsBusy) return;
            AudioManager.Play("ui_select", 1f);
            GameSession.SelectedLevel = pIndex;
            SceneTransition.LoadStage(pIndex);
        }

        private void Quit()
        {
            AudioManager.Play("ui_back", 0.8f);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    public class UIPulse : MonoBehaviour
    {
        private void Update()
        {
            transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.05f);
        }
    }
}
