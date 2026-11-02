using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Title scene : a live slice of the broken world (you control the runner on the PRESS START screen), then
    // Boost-Vector-like menus. PLAY opens the stage select : a perspective carousel you drag with inertia.
    public class TitleScreen : MonoBehaviour
    {
        private enum Screen { Press, Main, Stages, Settings, Controls, Credits }

        private const float CARD_PIXELS = 560f;

        public static bool OpenLevelSelect;

        private Screen _screen = Screen.Press;
        private RectTransform _root;
        private Canvas _canvas;
        private CameraRig _rig;
        private PlayerController _player;
        private LevelLayout _layout;
        private Material _speedLines;
        private int _enteredFrame;
        private float _orbit;

        private RectTransform _pressGroup;
        private RectTransform _mainGroup;
        private RectTransform _stagesGroup;
        private RectTransform _settingsGroup;
        private RectTransform _controlsGroup;
        private RectTransform _creditsGroup;
        private UINav _mainNav;
        private UINav _settingsNav;
        private TextMeshProUGUI _press;
        private readonly List<RectTransform> _mainItems = new List<RectTransform>();

        // Stage carousel
        private readonly List<RectTransform> _cards = new List<RectTransform>();
        private readonly List<CanvasGroup> _cardGroups = new List<CanvasGroup>();
        private readonly List<UIShape> _cardBorders = new List<UIShape>();
        private float _carousel;
        private float _carouselVelocity;
        private float _carouselTarget;
        private bool _dragging;
        private bool _dragMoved;
        private float _dragStartX;
        private float _dragStartCarousel;
        private float _lastDragX;
        private float _lastClickTime;
        private int _lastClickCard = -1;
        private int _selectedStage;
        private int _shownStage = -1;
        private RectTransform _stageInfo;
        private TextMeshProUGUI _stageNumber;
        private TextMeshProUGUI _stageTitle;
        private TextMeshProUGUI _stageTag;
        private TextMeshProUGUI _stageBest;
        private TextMeshProUGUI _stageSpeed;
        private TextMeshProUGUI _leagueText;
        private RawImage _keyArt;
        private RectTransform _goButton;
        private TextMeshProUGUI _goLabel;
        private RectTransform _backButton;
        private RectTransform _carouselArea;
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

        private void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RaceContext.Clear();
            Interactable.All.Clear();
            Breakable.All.Clear();
            GrindRail.All.Clear();
            DeckChunk.ClearAll();
        }

        // ================================================================ live backdrop

        private void BuildBackdrop()
        {
            int lLeague = Mathf.Clamp(LevelDatabase.Get(Mathf.Max(0, GameSession.UnlockedCount - 1)).world, 0, 2);
            Theme lTheme = lLeague == 0 ? Theme.Islands() : lLeague == 1 ? Theme.Desert() : Theme.Space();
            RaceContext.Theme = lTheme;
            Transform lRoot = new GameObject("Backdrop").transform;
            EnvironmentBuilder.Build(lTheme, lRoot);

            Random.State lState = Random.state;
            Random.InitState(5);
            Transform lTrack = new GameObject("Track").transform;
            lTrack.SetParent(lRoot, false);
            LevelBuilder lBuilder = new LevelBuilder(lTrack, lTheme);
            lBuilder.Start(60f).NoLane().Edges(EdgeKind.Lip).Run(80f).BoostStrip(80f, 2).Islands(300f, 3, 24f, 5f).Run(60f)
                .Kicker(16f, 45f, 6f).Run(60f).Cascade(3, 14f, 60f).Run(80f).Upper(220f, 16f).Run(120f).Finish();
            _layout = lBuilder.Build();
            Random.state = lState;
            RaceContext.Layout = _layout;
            EnvironmentBuilder.Decorate(lTheme, _layout, lRoot, 17);
            StartGate.Instance?.Open();

            Camera lCamera = CameraRig.CreateCamera();
            _rig = lCamera.gameObject.AddComponent<CameraRig>();
            new GameObject("PostFX").AddComponent<PostFX>();

            _player = new GameObject("Player").AddComponent<PlayerController>();
            PlayerVisual lVisual = PlayerVisual.Create(_player);
            _player.Init(_layout, lVisual);
            _player.BeginFreeRoam(false);
            RaceContext.Player = _player;
            _rig.SetTarget(_player, _layout);
            _rig.freeLook = true;
            _rig.Snap();
            RespawnDirector.Create(_player, _rig);
        }

        private void UpdateBackdrop(float pDt)
        {
            bool lPlayable = _screen == Screen.Press;
            _player.InputLocked = !lPlayable;
            Cursor.lockState = lPlayable ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lPlayable;
            // Fell off the showcase or reached its end : back to the plaza.
            if (_player.Progress > _layout.finishS + 100f) _player.RespawnAt(new Breadcrumb { s = -10f, deck = _layout.MainDeckAt(-10f), speed = 0f }, false);

            if (lPlayable)
            {
                _rig.freeLook = true;
                _rig.ClearOverride(0.8f);
                return;
            }
            // Menus : slow cinematic orbit, the runner framed on the right third (menus live on the left).
            _orbit += pDt * 6f;
            Vector3 lFocus = _player.FocusPosition;
            Vector3 lForward = Vector3.ProjectOnPlane(_player.Heading, Vector3.up).normalized;
            Quaternion lYaw = Quaternion.AngleAxis(150f + Mathf.Sin(_orbit * Mathf.Deg2Rad) * 25f, Vector3.up);
            Vector3 lOffset = lYaw * lForward * (_screen == Screen.Stages ? 9f : 6.5f) + Vector3.up * (_screen == Screen.Stages ? 2.5f : 1.4f);
            Vector3 lRight = Vector3.Cross(Vector3.up, (lFocus - (lFocus + lOffset)).normalized);
            _rig.SetOverride(lFocus + lOffset, lFocus + Vector3.up * 0.3f - lRight * 2.3f, 50f, 0.8f);
        }

        // ================================================================ update

        private void Update()
        {
            float lDt = Time.deltaTime;
            UpdateBackdrop(lDt);
            _speedLines.SetFloat("_Intensity", _screen == Screen.Press ? 0f : 0.35f + Mathf.Sin(Time.time * 2f) * 0.05f);
            if (Time.frameCount <= _enteredFrame + 1 || SceneTransition.IsBusy) return;
            switch (_screen)
            {
                case Screen.Press:
                    _press.alpha = 0.6f + Mathf.Sin(Time.unscaledTime * 5f) * 0.4f;
                    if (HSInput.SubmitMenuPressed || HSInput.MouseClicked)
                    {
                        AudioManager.Play("ui_select", 0.9f);
                        ShowMain();
                    }
                    break;
                case Screen.Main:
                    Parallax(_mainGroup, 14f);
                    break;
                case Screen.Stages:
                    UpdateStages(Time.unscaledDeltaTime);
                    break;
                case Screen.Controls:
                case Screen.Credits:
                    if (HSInput.BackPressed || HSInput.SubmitPressed || HSInput.MouseClicked)
                    {
                        AudioManager.Play("ui_back", 0.7f);
                        ShowMain();
                    }
                    break;
            }
        }

        // Menu layers drift with the mouse : depth and life in a flat UI.
        private void Parallax(RectTransform pGroup, float pAmount)
        {
            Vector2 lMouse = HSInput.MousePosition;
            Vector2 lCenter = new Vector2(UnityEngine.Screen.width, UnityEngine.Screen.height) * 0.5f;
            Vector2 lOffset = new Vector2((lMouse.x - lCenter.x) / lCenter.x, (lMouse.y - lCenter.y) / lCenter.y);
            for (int i = 0; i < pGroup.childCount; i++)
            {
                RectTransform lChild = (RectTransform)pGroup.GetChild(i);
                UIParallax lLayer = lChild.GetComponent<UIParallax>();
                if (lLayer == null) continue;
                lLayer.Apply(-lOffset * pAmount * lLayer.depth);
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
            _canvas = UIFactory.CreateCanvas("TitleCanvas", 10, transform);
            _root = (RectTransform)_canvas.transform;
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
            BuildCredits();

            if (GameSession.CheatsEnabled)
            {
                UIShape lBadge = UIKit.Tile(_root, "DevBadge", UIKit.GOLD, new Vector2(1f, 1f), new Vector2(-170f, -40f), new Vector2(300f, 46f), 0.4f);
                UIKit.Text(lBadge.transform, "DEV BUILD  -  CHEATS ON", 24f, UIKit.INK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 46f), UIKit.Font.Heavy);
            }
        }

        private void BuildPress()
        {
            _pressGroup = Group("Press");
            UIKit.OutlineText(_pressGroup, "HYPER SPEED", 330f, new Color(1f, 1f, 1f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(-20f, 300f), new Vector2(2400f, 380f), -6f, 0.08f);
            UIShape lSlash = UIKit.Tile(_pressGroup, "Slash", UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2(-60f, 270f), new Vector2(1500f, 250f), 0.55f, -6f);
            UIKit.Chevrons(lSlash.transform, "Chevrons", UIKit.RED, UIKit.RED_DARK, 80f);
            lSlash.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            TextMeshProUGUI lShadow = UIKit.Text(_pressGroup, "HYPER SPEED", 210f, UIKit.RED_DEEP, new Vector2(0.5f, 0.5f), new Vector2(-50f, 256f), new Vector2(2000f, 260f), UIKit.Font.Heavy, TextAlignmentOptions.Center, true, -6f);
            TextMeshProUGUI lLogo = UIKit.Text(_pressGroup, "HYPER SPEED", 210f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-62f, 268f), new Vector2(2000f, 260f), UIKit.Font.Heavy, TextAlignmentOptions.Center, true, -6f);
            lLogo.characterSpacing = -6f;
            lShadow.characterSpacing = -6f;
            lLogo.gameObject.AddComponent<UISpeedGhost>();
            UIShape lTag = UIKit.Tile(_pressGroup, "Tag", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(160f, 110f), new Vector2(760f, 64f), 0.5f, -6f);
            UIKit.Text(lTag.transform, "RACE AGAINST ANYTHING", 40f, UIKit.INK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 64f), UIKit.Font.Heavy);
            _press = UIKit.Text(_pressGroup, "PRESS ENTER  /  START", 48f, UIKit.WHITE, new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(1200f, 80f), UIKit.Font.Heavy);
            UIFactory.Outline(_press, UIKit.INK, 0.25f);
            TextMeshProUGUI lHint = UIKit.Text(_pressGroup, "WASD  MOVE     SPACE  JUMP / GLIDE     S  DIVE     MOUSE  LOOK", 26f, new Color(1f, 1f, 1f, 0.85f), new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(1600f, 40f), UIKit.Font.Bold);
            UIFactory.Outline(lHint, UIKit.INK, 0.25f);
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
            lHolder.gameObject.AddComponent<UIParallax>().depth = 0.6f;
            return lTile;
        }

        private void BuildMain()
        {
            _mainGroup = Group("Main");
            UIShape lPanel = UIKit.Tile(_mainGroup, "RedPanel", new Color(UIKit.RED.r, UIKit.RED.g, UIKit.RED.b, 0.92f), new Vector2(0.5f, 0.5f), new Vector2(-560f, 0f), new Vector2(1300f, 1300f), 0.16f);
            UIKit.Chevrons(lPanel.transform, "Chevrons", new Color(0f, 0f, 0f, 0f), new Color(0.8f, 0.04f, 0.09f, 0.6f), 40f);
            lPanel.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            lPanel.gameObject.AddComponent<UIParallax>().depth = 0.25f;
            UIKit.OutlineText(_mainGroup, "HYPER", 300f, new Color(1f, 1f, 1f, 0.45f), new Vector2(0.5f, 0.5f), new Vector2(-600f, 360f), new Vector2(1400f, 320f), 0f, 0.07f).gameObject.AddComponent<UIParallax>().depth = 1.4f;
            UIKit.OutlineText(_mainGroup, "SPEED", 300f, new Color(1f, 1f, 1f, 0.25f), new Vector2(0.5f, 0.5f), new Vector2(-340f, -380f), new Vector2(1400f, 320f), 0f, 0.07f).gameObject.AddComponent<UIParallax>().depth = 1.8f;

            int lNext = Mathf.Clamp(GameSession.UnlockedCount - 1, 0, LevelDatabase.Count - 1);
            LevelDefinition lNextLevel = LevelDatabase.Get(lNext);
            NavTile lPlay = MakeTile(_mainGroup, "Play", "PLAY", new Vector2(-560f, 90f), new Vector2(820f, 240f), UIKit.RED_DARK, UIKit.RED_DEEP, UIKit.WHITE, UIKit.WHITE, 180f);
            lPlay.label.alignment = TextAlignmentOptions.Left;
            lPlay.label.margin = new Vector4(80f, 0f, 0f, 0f);
            lPlay.label.gameObject.AddComponent<UISpeedGhost>();
            UIShape lStrip = UIKit.Tile(lPlay.transform, "Next", UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(-110f, 146f), new Vector2(600f, 50f), 0.5f);
            UIKit.Text(lStrip.transform, "NEXT : STAGE " + (lNext + 1).ToString("00") + "  |  " + lNextLevel.title, 24f, UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(580f, 50f), UIKit.Font.Bold);
            UIKit.Icon(lPlay.transform, "play", new Color(1f, 1f, 1f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(280f, 0f), 130f);

            NavTile lControls = MakeTile(_mainGroup, "Controls", "CONTROLS", new Vector2(-740f, -150f), new Vector2(440f, 104f), UIKit.WHITE, UIKit.INK, UIKit.INK, UIKit.WHITE, 50f);
            NavTile lCredits = MakeTile(_mainGroup, "Credits", "CREDITS", new Vector2(-290f, -150f), new Vector2(400f, 104f), UIKit.WHITE, UIKit.INK, UIKit.INK, UIKit.WHITE, 50f);
            NavTile lSettings = MakeTile(_mainGroup, "Settings", "", new Vector2(110f, 120f), new Vector2(300f, 280f), UIKit.ORANGE, new Color(1f, 0.75f, 0.3f), UIKit.INK, UIKit.INK, 40f);
            UIKit.Icon(lSettings.transform, "gear", new Color(1f, 1f, 1f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(20f, 36f), 160f).gameObject.AddComponent<UISpin>().speed = -30f;
            UIShape lSettingsBand = UIKit.Tile(lSettings.transform, "Band", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-10f, -98f), new Vector2(280f, 54f), 0.3f);
            UIKit.Text(lSettingsBand.transform, "SETTINGS", 34f, UIKit.INK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 54f), UIKit.Font.Heavy);
            NavTile lQuit = MakeTile(_mainGroup, "Quit", "EXIT GAME", new Vector2(-780f, -300f), new Vector2(380f, 76f), UIKit.RED_DEEP, UIKit.INK, new Color(1f, 1f, 1f, 0.6f), UIKit.WHITE, 38f);

            RectTransform lNow = UIFactory.CreateRect(_mainGroup, "NowPlaying", new Vector2(0f, 0f), new Vector2(330f, 90f), new Vector2(560f, 60f));
            UIKit.Icon(lNow, "note", UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(30f, 0f), 46f);
            UIKit.Text(lNow, "NOW PLAYING\n<size=22>OST - Race Against Anything</size>", 26f, UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(300f, 0f), new Vector2(480f, 60f), UIKit.Font.Bold, TextAlignmentOptions.Left, false);
            UIKit.Checker(_mainGroup, "Checker", new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(2200f, 44f), UIKit.WHITE, UIKit.RED_DARK, 40f);

            _mainItems.AddRange(new[] { (RectTransform)lPlay.transform, (RectTransform)lSettings.transform, (RectTransform)lControls.transform, (RectTransform)lCredits.transform, (RectTransform)lQuit.transform });
            _mainNav = UINav.Create(_mainGroup.gameObject);
            _mainNav.Add((RectTransform)lPlay.transform, new Vector2(-560f, 90f), ShowStages, lPlay.SetSelected);
            _mainNav.Add((RectTransform)lSettings.transform, new Vector2(110f, 120f), ShowSettings, lSettings.SetSelected);
            _mainNav.Add((RectTransform)lControls.transform, new Vector2(-740f, -150f), ShowControls, lControls.SetSelected);
            _mainNav.Add((RectTransform)lCredits.transform, new Vector2(-290f, -150f), ShowCredits, lCredits.SetSelected);
            _mainNav.Add((RectTransform)lQuit.transform, new Vector2(-780f, -300f), Quit, lQuit.SetSelected);
            _mainNav.onBack = () => SetScreen(Screen.Press);
        }

        // ================================================================ stage select

        private void BuildStages()
        {
            _stagesGroup = Group("Stages");
            // Translucent : the live world stays visible behind the carousel.
            UIKit.FullScreen(_stagesGroup, "Shade", new Color(0.04f, 0.02f, 0.06f, 0.45f));
            _keyArt = UIFactory.CreateRect(_stagesGroup, "KeyArt", new Vector2(1f, 0.5f), new Vector2(-520f, 140f), new Vector2(1280f, 720f)).gameObject.AddComponent<RawImage>();
            _keyArt.color = new Color(1f, 1f, 1f, 0.22f);
            _keyArt.raycastTarget = false;
            _leagueText = UIKit.OutlineText(_stagesGroup, "", 170f, new Color(1f, 1f, 1f, 0.35f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(2200f, 220f), 0f, 0.07f);
            UIPerspectiveRoad lRoad = UIFactory.CreateRect(_stagesGroup, "Road", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f)).gameObject.AddComponent<UIPerspectiveRoad>();
            lRoad.color = new Color(1f, 1f, 1f, 0.35f);
            lRoad.raycastTarget = false;

            _carouselArea = UIFactory.CreateRect(_stagesGroup, "CarouselArea", new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1920f, 600f));
            for (int i = 0; i < LevelDatabase.Count; i++)
            {
                LevelDefinition lLevel = LevelDatabase.Levels[i];
                RectTransform lCard = UIFactory.CreateRect(_carouselArea, "Card" + i, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 360f));
                _cardGroups.Add(lCard.gameObject.AddComponent<CanvasGroup>());
                UIShape lBorder = UIKit.Tile(lCard, "Border", UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 390f), 0.3f);
                _cardBorders.Add(lBorder);
                UIShape lInner = UIKit.Tile(lCard, "Inner", UIKit.RED_DEEP, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 360f), 0.3f);
                bool lOpen = GameSession.IsUnlocked(i);
                RawImage lThumb = UIKit.MaskedImage(lInner, RivalThumbnails.Get(i));
                if (!lOpen) lThumb.color = new Color(0.2f, 0.16f, 0.22f);
                string lNumber = (lLevel.world + 1) + "-" + (IndexInWorld(i) + 1);
                TextMeshProUGUI lNumberText = UIKit.Text(lCard, lNumber, 96f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-150f, -112f), new Vector2(360f, 120f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
                UIFactory.Outline(lNumberText, UIKit.INK, 0.2f);
                int lRank = GameSession.GetRank(i);
                UIKit.Icon(lCard, lOpen ? "trophy" : "lock", lRank > 0 ? UIKit.GOLD : new Color(1f, 1f, 1f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(-220f, 120f), 72f);
                if (lRank > 0)
                {
                    TextMeshProUGUI lRankText = UIKit.Text(lCard, GameSession.RankLetter(lRank), 64f, UIKit.GOLD, new Vector2(0.5f, 0.5f), new Vector2(-145f, 120f), new Vector2(100f, 90f), UIKit.Font.Heavy);
                    UIFactory.Outline(lRankText, UIKit.INK, 0.25f);
                }
                if (!lOpen) UIKit.Text(lCard, "LOCKED", 56f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(40f, 10f), new Vector2(420f, 80f), UIKit.Font.Heavy);
                _cards.Add(lCard);
            }

            _stageInfo = UIFactory.CreateRect(_stagesGroup, "Info", new Vector2(0f, 0f), new Vector2(620f, 190f), new Vector2(1100f, 300f));
            UIKit.Tile(_stageInfo, "Back", new Color(1f, 1f, 1f, 0.94f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080f, 230f), 0.25f);
            _stageNumber = UIKit.Text(_stageInfo, "", 34f, UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2(-180f, 84f), new Vector2(700f, 44f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            _stageTitle = UIKit.Text(_stageInfo, "", 70f, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(-20f, 22f), new Vector2(1000f, 90f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            _stageTitle.enableAutoSizing = true;
            _stageTitle.fontSizeMin = 40f;
            _stageTitle.fontSizeMax = 70f;
            _stageTag = UIKit.Text(_stageInfo, "", 28f, UIKit.RED_DARK, new Vector2(0.5f, 0.5f), new Vector2(-20f, -40f), new Vector2(1000f, 40f), UIKit.Font.Bold, TextAlignmentOptions.Left);
            _stageBest = UIKit.Text(_stageInfo, "", 28f, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(-220f, -88f), new Vector2(580f, 40f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            _stageSpeed = UIKit.Text(_stageInfo, "", 28f, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(280f, -88f), new Vector2(420f, 40f), UIKit.Font.Heavy, TextAlignmentOptions.Right);

            UIShape lGo = UIFactory.CreateShape(_stagesGroup, "Go", UIShape.ShapeKind.Chevron, UIKit.RED, new Vector2(1f, 0f), new Vector2(-260f, 180f), new Vector2(380f, 170f));
            lGo.skew = 0.45f;
            _goLabel = UIKit.Text(lGo.transform, "GO", 120f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-14f, 0f), new Vector2(340f, 170f), UIKit.Font.Heavy);
            _goLabel.gameObject.AddComponent<UISpeedGhost>();
            _goButton = lGo.rectTransform;
            _goButton.gameObject.AddComponent<UIPulse>();
            UIKit.Text(_stagesGroup, "DRAG  /  WHEEL  /  ARROWS     CLICK : SELECT     ENTER  /  DOUBLE CLICK : GO", 22f, new Color(1f, 1f, 1f, 0.7f), new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(1600f, 34f), UIKit.Font.Bold);

            UIShape lBack = UIKit.Tile(_stagesGroup, "Back", new Color(0.12f, 0.1f, 0.14f, 0.9f), new Vector2(0f, 1f), new Vector2(230f, -80f), new Vector2(330f, 100f), 0.45f);
            UIKit.Icon(lBack.transform, "menu", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-90f, 0f), 60f);
            UIKit.Text(lBack.transform, "BACK", 52f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(220f, 100f), UIKit.Font.Thin);
            _backButton = lBack.rectTransform;
            UIKit.Checker(_stagesGroup, "Checker", new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(2200f, 44f), UIKit.WHITE, UIKit.RED_DARK, -40f);
        }

        private static int IndexInWorld(int pIndex)
        {
            int lCount = 0;
            for (int i = 0; i < pIndex; i++)
                if (LevelDatabase.Levels[i].world == LevelDatabase.Levels[pIndex].world) lCount++;
            return lCount;
        }

        private float ToCanvas(float pPixels) => pPixels / Mathf.Max(0.01f, _canvas.scaleFactor);

        private void UpdateStages(float pDt)
        {
            int lCount = LevelDatabase.Count;
            Vector2 lMouse = HSInput.MousePosition;

            // Keyboard / gamepad / wheel move the target by whole cards.
            Vector2Int lMove = HSInput.MenuMove;
            if (lMove.x != 0) _carouselTarget = Mathf.Clamp(Mathf.Round(_carouselTarget) + lMove.x, 0, lCount - 1);
            float lWheel = HSInput.MouseWheel;
            if (Mathf.Abs(lWheel) > 0.01f) _carouselTarget = Mathf.Clamp(Mathf.Round(_carouselTarget) - Mathf.Sign(lWheel), 0, lCount - 1);

            // Click-hold and drag : the carousel follows the cursor, keeps its momentum on release.
            if (HSInput.MouseClicked)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_backButton, lMouse, null)) { AudioManager.Play("ui_back", 0.7f); ShowMain(); return; }
                if (RectTransformUtility.RectangleContainsScreenPoint(_goButton, lMouse, null)) { LaunchStage(_selectedStage); return; }
                if (RectTransformUtility.RectangleContainsScreenPoint(_carouselArea, lMouse, null))
                {
                    _dragging = true;
                    _dragMoved = false;
                    _dragStartX = lMouse.x;
                    _lastDragX = lMouse.x;
                    _dragStartCarousel = _carousel;
                    _carouselVelocity = 0f;
                }
            }
            if (_dragging)
            {
                if (HSInput.MouseHeld)
                {
                    float lDelta = ToCanvas(lMouse.x - _dragStartX);
                    if (Mathf.Abs(lDelta) > 12f) _dragMoved = true;
                    float lNew = Mathf.Clamp(_dragStartCarousel - lDelta / CARD_PIXELS, -0.4f, lCount - 0.6f);
                    float lInstant = pDt > 0f ? (lNew - _carousel) / pDt : 0f;
                    _carouselVelocity = Mathf.Lerp(_carouselVelocity, lInstant, 1f - Mathf.Exp(-18f * pDt));
                    _carousel = lNew;
                    _lastDragX = lMouse.x;
                }
                else
                {
                    _dragging = false;
                    if (!_dragMoved) ClickCard(lMouse);
                    else _carouselTarget = Mathf.Clamp(Mathf.Round(_carousel + _carouselVelocity * 0.35f), 0, lCount - 1);
                }
            }
            if (!_dragging)
            {
                // Inertia then an underdamped spring onto the target card (small overshoot).
                float lAccel = 160f * (_carouselTarget - _carousel) - 2f * Mathf.Sqrt(160f) * 0.7f * _carouselVelocity;
                _carouselVelocity += lAccel * pDt;
                _carousel += _carouselVelocity * pDt;
            }
            int lSelected = Mathf.Clamp(Mathf.RoundToInt(_carousel), 0, lCount - 1);
            if (lSelected != _selectedStage)
            {
                _selectedStage = lSelected;
                AudioManager.Play("ui_move", 0.6f, 1f + (lSelected % 3) * 0.05f);
            }
            LayoutCards();
            RefreshStageInfo();

            if (HSInput.SubmitPressed) LaunchStage(_selectedStage);
            else if (HSInput.BackPressed)
            {
                AudioManager.Play("ui_back", 0.7f);
                ShowMain();
            }
        }

        private void ClickCard(Vector2 pMouse)
        {
            int lHit = -1;
            float lBest = float.MaxValue;
            for (int i = 0; i < _cards.Count; i++)
            {
                if (!_cards[i].gameObject.activeSelf || !RectTransformUtility.RectangleContainsScreenPoint(_cards[i], pMouse, null)) continue;
                float lDepth = Mathf.Abs(i - _carousel);
                if (lDepth < lBest) { lBest = lDepth; lHit = i; }
            }
            if (lHit < 0) return;
            bool lDouble = lHit == _lastClickCard && Time.unscaledTime - _lastClickTime < 0.35f;
            _lastClickCard = lHit;
            _lastClickTime = Time.unscaledTime;
            if (lDouble && lHit == _selectedStage) { LaunchStage(lHit); return; }
            _carouselTarget = lHit;
            UITween.Punch(_cards[lHit], 0.08f);
            AudioManager.Play("ui_select", 0.5f, 1.2f);
        }

        // Cards recede toward the vanishing point on both sides : scale, height and skew follow the depth.
        private void LayoutCards()
        {
            float lTime = Time.unscaledTime;
            for (int i = 0; i < _cards.Count; i++)
            {
                float lOffset = i - _carousel;
                float lDepth = Mathf.Abs(lOffset);
                float lScale = 1f / (1f + 0.55f * lDepth);
                RectTransform lCard = _cards[i];
                lCard.anchoredPosition = new Vector2(Mathf.Sign(lOffset) * (1f - lScale) * 1350f, lDepth * 70f * lScale + 10f);
                lCard.localScale = Vector3.one * lScale;
                lCard.localRotation = Quaternion.Euler(0f, 0f, -lOffset * 3f * lScale + (i == _selectedStage ? Mathf.Sin(lTime * 3f) * 0.8f : 0f));
                _cardGroups[i].alpha = Mathf.Clamp01(1.3f - lDepth * 0.28f);
                _cardBorders[i].color = i == _selectedStage ? UIKit.GOLD : UIKit.WHITE;
                lCard.gameObject.SetActive(lDepth < 4.5f);
            }
            // Draw order : farthest first.
            List<int> lOrder = new List<int>();
            for (int i = 0; i < _cards.Count; i++) lOrder.Add(i);
            lOrder.Sort((pA, pB) => Mathf.Abs(pB - _carousel).CompareTo(Mathf.Abs(pA - _carousel)));
            foreach (int lIndex in lOrder) _cards[lIndex].SetAsLastSibling();
        }

        private void RefreshStageInfo()
        {
            if (_shownStage == _selectedStage) return;
            _shownStage = _selectedStage;
            LevelDefinition lLevel = LevelDatabase.Levels[_selectedStage];
            bool lOpen = GameSession.IsUnlocked(_selectedStage);
            _stageNumber.text = LevelDatabase.WORLDS[lLevel.world] + "  //  STAGE " + (_selectedStage + 1).ToString("00");
            _stageTitle.text = lOpen ? lLevel.title : "LOCKED";
            _stageTag.text = lOpen ? lLevel.tagline : "Beat the previous stage to unlock.";
            float lBest = GameSession.GetBestTime(_selectedStage);
            _stageBest.text = lOpen ? "BEST  " + GameSession.FormatTime(lBest) + "   RANK  " + GameSession.RankLetter(GameSession.GetRank(_selectedStage)) : "";
            _stageSpeed.text = lOpen ? "TOP SPEED  " + RivalFactory.TopSpeedKmh(lLevel.rivalSpeed) + " KM/H" : "";
            _leagueText.text = LevelDatabase.WORLDS[lLevel.world];
            _keyArt.texture = RivalThumbnails.Get(_selectedStage);
            _goLabel.text = lOpen ? "GO" : "LOCKED";
            _goLabel.fontSize = lOpen ? 120f : 56f;
            // Kinetic refresh : the info plate snaps in from the right.
            UITween.Kill(_stageInfo);
            _stageInfo.anchoredPosition = new Vector2(700f, 190f);
            UITween.Move(_stageInfo, new Vector2(620f, 190f), 0.25f, Ease.OutBack);
            UITween.Kill(_keyArt.rectTransform);
            _keyArt.rectTransform.anchoredPosition = new Vector2(-380f, 140f);
            UITween.Move(_keyArt.rectTransform, new Vector2(-520f, 140f), 0.4f, Ease.OutExpo);
        }

        // ================================================================ other screens

        private void BuildSettings()
        {
            _settingsGroup = Group("Settings");
            UIKit.Tile(_settingsGroup, "Panel", new Color(UIKit.ORANGE.r, UIKit.ORANGE.g, UIKit.ORANGE.b, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(-420f, 0f), new Vector2(1100f, 900f), 0.12f);
            UIKit.OutlineText(_settingsGroup, "SETTINGS", 200f, new Color(1f, 1f, 1f, 0.7f), new Vector2(0.5f, 0.5f), new Vector2(-460f, 330f), new Vector2(1300f, 220f), 0f, 0.08f);
            NavTile lMusic = MakeTile(_settingsGroup, "Music", "", new Vector2(-420f, 120f), new Vector2(860f, 110f), UIKit.WHITE, UIKit.INK, UIKit.INK, UIKit.WHITE, 46f);
            _musicValue = lMusic.label;
            NavTile lSfx = MakeTile(_settingsGroup, "Sfx", "", new Vector2(-420f, -20f), new Vector2(860f, 110f), UIKit.WHITE, UIKit.INK, UIKit.INK, UIKit.WHITE, 46f);
            _sfxValue = lSfx.label;
            NavTile lBack = MakeTile(_settingsGroup, "Back", "BACK", new Vector2(-560f, -200f), new Vector2(420f, 100f), new Color(0.3f, 0.3f, 0.32f), UIKit.INK, UIKit.WHITE, UIKit.WHITE, 48f);
            if (GameSession.CheatsEnabled)
                UIKit.Text(_settingsGroup, "CHEATS :  F1 invincible   F2 boost   F3 next checkpoint   F4 win   F5 slow-mo   F6 layout   F9 autopilot", 24f, UIKit.INK,
                    new Vector2(0.5f, 0.5f), new Vector2(-420f, -330f), new Vector2(1040f, 60f), UIKit.Font.Bold);
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
            UIKit.Tile(_controlsGroup, "Panel", new Color(1f, 1f, 1f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(-300f, -20f), new Vector2(1300f, 900f), 0.1f);
            UIKit.OutlineText(_controlsGroup, "CONTROLS", 200f, new Color(1f, 1f, 1f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(-380f, 420f), new Vector2(1300f, 220f), 0f, 0.08f);
            string[] lLines =
            {
                "STEER", "A / D   -   Left stick",
                "JUMP  /  DOUBLE JUMP", "Space   -   A",
                "GLIDE", "Hold Space while falling",
                "DIVE  /  BREAK THROUGH", "S / Ctrl in the air or on cracked floors   -   LT",
                "DRIFT", "S / Ctrl + steer on the ground",
                "DASH", "Shift / Right click   -   RB / B",
                "PUNCH", "E / Left click   -   X",
                "LEAVE A PLATFORM", "Keep pushing toward its edge",
                "PERFECT START", "Space / W right on GO",
                "RESTART  /  PAUSE", "R  /  Esc   -   Select / Start"
            };
            for (int i = 0; i < lLines.Length; i += 2)
            {
                float lY = 290f - i * 32f;
                UIKit.Text(_controlsGroup, lLines[i], 32f, UIKit.RED_DARK, new Vector2(0.5f, 0.5f), new Vector2(-580f, lY), new Vector2(560f, 56f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
                UIKit.Text(_controlsGroup, lLines[i + 1], 30f, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(40f, lY), new Vector2(680f, 56f), UIKit.Font.Bold, TextAlignmentOptions.Left, false);
            }
            UIKit.Text(_controlsGroup, "Boost pads, rings, rails and PERFECT dive landings take you to HYPER SPEED.", 26f, UIKit.INK,
                new Vector2(0.5f, 0.5f), new Vector2(-300f, -370f), new Vector2(1150f, 60f), UIKit.Font.Bold);
        }

        private void BuildCredits()
        {
            _creditsGroup = Group("Credits");
            UIKit.Tile(_creditsGroup, "Panel", new Color(0.08f, 0.06f, 0.1f, 0.92f), new Vector2(0.5f, 0.5f), new Vector2(-300f, -20f), new Vector2(1300f, 900f), 0.1f);
            UIKit.OutlineText(_creditsGroup, "CREDITS", 200f, new Color(1f, 1f, 1f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(-380f, 420f), new Vector2(1300f, 220f), 0f, 0.08f);
            TextAsset lCredits = Resources.Load<TextAsset>("Credits");
            string lText = lCredits != null ? lCredits.text : "HYPER SPEED  -  ISART DIGITAL";
            TextMeshProUGUI lBody = UIKit.Text(_creditsGroup, lText, 26f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-300f, -40f), new Vector2(1150f, 720f), UIKit.Font.Bold, TextAlignmentOptions.TopLeft, false);
            lBody.textWrappingMode = TextWrappingModes.Normal;
        }

        // ================================================================ screens

        private void SetScreen(Screen pScreen)
        {
            _screen = pScreen;
            _enteredFrame = Time.frameCount;
            _mainNav.active = pScreen == Screen.Main;
            _settingsNav.active = pScreen == Screen.Settings;
            RectTransform[] lGroups = { _pressGroup, _mainGroup, _stagesGroup, _settingsGroup, _controlsGroup, _creditsGroup };
            Screen[] lScreens = { Screen.Press, Screen.Main, Screen.Stages, Screen.Settings, Screen.Controls, Screen.Credits };
            for (int i = 0; i < lGroups.Length; i++)
            {
                RectTransform lGroup = lGroups[i];
                bool lOn = lScreens[i] == pScreen;
                if (lOn && !lGroup.gameObject.activeSelf)
                {
                    lGroup.gameObject.SetActive(true);
                    CanvasGroup lCanvas = lGroup.GetComponent<CanvasGroup>();
                    lCanvas.alpha = 0f;
                    UITween.Kill(lCanvas);
                    UITween.Fade(lCanvas, 1f, 0.25f);
                    UITween.Kill(lGroup);
                    UITween.MoveFrom(lGroup, new Vector2(-260f, 0f), 0.4f, Ease.OutExpo);
                }
                else if (!lOn) lGroup.gameObject.SetActive(false);
            }
            if (pScreen == Screen.Main)
            {
                // Staggered entrance : tiles slam in one after another.
                for (int i = 0; i < _mainItems.Count; i++)
                {
                    RectTransform lItem = _mainItems[i];
                    UITween.Kill(lItem);
                    lItem.localScale = Vector3.one * 0.6f;
                    UITween.Scale(lItem, Vector3.one, 0.35f, Ease.OutBack, i * 0.05f);
                }
            }
            AudioManager.Play("ui_whoosh", 0.5f, 1.2f);
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

        private void ShowCredits() => SetScreen(Screen.Credits);

        private void ShowStages()
        {
            SetScreen(Screen.Stages);
            _selectedStage = Mathf.Clamp(Mathf.Min(GameSession.SelectedLevel, GameSession.UnlockedCount - 1), 0, LevelDatabase.Count - 1);
            if (GameSession.SelectedLevel == 0) _selectedStage = Mathf.Clamp(GameSession.UnlockedCount - 1, 0, LevelDatabase.Count - 1);
            _carouselTarget = _selectedStage;
            _carousel = _selectedStage + 3f;
            _carouselVelocity = 0f;
            _shownStage = -1;
        }

        private void LaunchStage(int pIndex)
        {
            if (!GameSession.IsUnlocked(pIndex))
            {
                AudioManager.Play("ui_locked", 0.8f);
                UITween.Punch(_goButton, 0.15f);
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

    // Layer that drifts with the mouse (depth > 1 moves more).
    public class UIParallax : MonoBehaviour
    {
        public float depth = 1f;
        private Vector2 _base;
        private bool _init;

        public void Apply(Vector2 pOffset)
        {
            RectTransform lRect = (RectTransform)transform;
            if (!_init) { _base = lRect.anchoredPosition; _init = true; }
            lRect.anchoredPosition = Vector2.Lerp(lRect.anchoredPosition, _base + pOffset, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        }
    }

    // Speed ghosts : two faded copies of a text trail behind it horizontally, breathing with time.
    public class UISpeedGhost : MonoBehaviour
    {
        private TextMeshProUGUI[] _ghosts;
        private TextMeshProUGUI _source;

        private void Start()
        {
            _source = GetComponent<TextMeshProUGUI>();
            if (_source == null) { enabled = false; return; }
            _ghosts = new TextMeshProUGUI[2];
            for (int i = 0; i < 2; i++)
            {
                TextMeshProUGUI lGhost = Instantiate(_source, transform.parent);
                Destroy(lGhost.GetComponent<UISpeedGhost>());
                lGhost.name = "Ghost" + i;
                lGhost.transform.SetSiblingIndex(_source.transform.GetSiblingIndex());
                lGhost.color = i == 0 ? new Color(UIKit.CYAN.r, UIKit.CYAN.g, UIKit.CYAN.b, 0.35f) : new Color(UIKit.RED.r, UIKit.RED.g, UIKit.RED.b, 0.3f);
                _ghosts[i] = lGhost;
            }
        }

        private void Update()
        {
            if (_ghosts == null) return;
            float lWave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.1f);
            for (int i = 0; i < _ghosts.Length; i++)
            {
                if (_ghosts[i] == null) continue;
                _ghosts[i].text = _source.text;
                _ghosts[i].rectTransform.anchoredPosition = _source.rectTransform.anchoredPosition + new Vector2(-(10f + i * 12f) * (0.6f + lWave), i == 0 ? 2f : -2f);
                _ghosts[i].alpha = _source.alpha * (i == 0 ? 0.35f : 0.25f);
            }
        }
    }
}
