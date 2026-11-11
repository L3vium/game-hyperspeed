using TMPro;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Title screen : one panel swinging with the mouse. Red slash and the name on the left, big diagonal slashes on the
    // right around the runner, who keeps flying through the world behind it all. Play goes to the stage select.
    public class TitleScreen : MonoBehaviour
    {
        private enum ScreenId { Main, Settings, Stages }

        public static bool OpenLevelSelect;

        private Transform _world;
        private Camera _camera;
        private CameraRig _rig;
        private PlayerController _player;
        private LevelLayout _layout;
        private StageSelect _select;

        private Canvas _canvas;
        private Canvas _canvas3D;
        private RectTransform _main;
        private SettingsMenu _settingsMenu;
        private RectTransform _stages;
        private UINav _mainNav;
        private DevCheatBar _cheats;
        private ScreenId _screen;

        private void Awake()
        {
            Time.timeScale = 1f;
            CursorGuard.Set(CursorMode.Menu);
            RaceContext.Clear();
            AudioManager.LoadVolumes();
            GameSession.ApplyDisplay();
            // The test hook only : a normal visit never starts on autopilot.
            Autopilot.Enabled = false;
            BuildWorld();
            BuildUI();
            if (OpenLevelSelect)
            {
                OpenLevelSelect = false;
                SetScreen(ScreenId.Stages);
            }
            else SetScreen(ScreenId.Main);
        }

        private void Start()
        {
            AudioManager.PlayMusic("music_title", 0.5f);
            // Dev build test hook : -hsStage N goes straight into a stage on autopilot.
            if (GameSession.CheatsEnabled)
            {
                string[] lArgs = System.Environment.GetCommandLineArgs();
                for (int i = 0; i < lArgs.Length - 1; i++)
                {
                    int lStage;
                    if (lArgs[i] == "-hsStage" && int.TryParse(lArgs[i + 1], out lStage))
                    {
                        Autopilot.Enabled = true;
                        GameSession.SelectedLevel = lStage;
                        SceneTransition.LoadStage(lStage);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            RaceContext.Clear();
            Interactable.All.Clear();
            Breakable.All.Clear();
            GrindRail.All.Clear();
            TrackBaker.Chunks.Clear();
        }

        // ================================================================ backdrop

        private void BuildWorld()
        {
            Theme lTheme = Theme.Islands();
            RaceContext.Theme = lTheme;
            SpeedProfile lProfile = SpeedProfile.For(150f * LevelDatabase.SPEED_SCALE);
            SpeedProfile.Current = lProfile;
            _world = new GameObject("TitleWorld").transform;
            EnvironmentBuilder.Build(lTheme, _world, lProfile);
            Transform lCourseRoot = new GameObject("Course").transform;
            lCourseRoot.SetParent(_world, false);
            CourseBuilder lBuilder = new CourseBuilder(lCourseRoot, lTheme, lProfile, 17);
            lBuilder.Start(90f, 1600f).NoItems().Hills(8f, 12f).Sweeps(4).Road(5f).Hills(8f, 12f).Sweeps(4).Road(5f).Hills(8f, 12f).Sweeps(4).Road(5f).Hills(8f, 12f).Sweeps(4).Road(5f).Finish();
            _layout = lBuilder.Build();
            RaceContext.Layout = _layout;

            _camera = CameraRig.CreateCamera();
            _rig = _camera.gameObject.AddComponent<CameraRig>();
            new GameObject("PostFX").AddComponent<PostFX>();
            new GameObject("Wind").AddComponent<WindField>();
            _player = new GameObject("Player").AddComponent<PlayerController>();
            PlayerVisual lVisual = PlayerVisual.Create(_player);
            _player.Init(_layout, lVisual);
            _player.InputLocked = true;
            RaceContext.Player = _player;
            _rig.SetTarget(_player, _layout);
            _rig.Snap();
            _player.StartRunning();

            _rig.lensShiftX = -0.42f;
            _rig.distanceScale = 0.27f;
        }

        private void Update()
        {
            // The attract run loops for ever, seen or not.
            if (_player != null && _layout != null && (_player.Progress > _layout.finishS - 200f || _player.State == PlayerState.Dead))
            {
                _player.RespawnAt(new Breadcrumb { s = 0f, lateral = 0f, speed = _layout.profile.cruise }, false);
                _rig.Snap();
            }
            if (_screen == ScreenId.Stages && HSInput.BackPressed && !SceneTransition.IsBusy)
            {
                AudioManager.Play("ui_back", 0.7f);
                SetScreen(ScreenId.Main);
            }
        }

        // ================================================================ UI

        private void BuildUI()
        {
            _canvas3D = UIPerspective.Create("Title3D", 19, transform, _camera);
            Canvas lStages = UIPerspective.Create("Stages3D", 21, transform, _camera);
            // Settings and credits float with the mouse too : created last, drawn on top.
            _canvas = UIPerspective.Create("TitleFlat3D", 22, transform, _camera);
            BuildMain();
            _settingsMenu = SettingsMenu.Create(_canvas.transform, true, () => SetScreen(ScreenId.Main));
            _select = StageSelect.Create(lStages, Launch, () => SetScreen(ScreenId.Main));
            BuildStages();
            if (GameSession.CheatsEnabled) _cheats = DevCheatBar.Create();
        }

        private void BuildMain()
        {
            RectTransform lRoot3D = (RectTransform)_canvas3D.transform;
            _main = UIFactory.Stretch(UIFactory.CreateRect(lRoot3D, "Main", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            PerspectivePanel lPanel = _main.gameObject.AddComponent<PerspectivePanel>();
            lPanel.baseEuler = new Vector3(0f, 7f, 0f);

            // Right side : long slashes cutting across the corners, leaving the runner in the open between them.
            Slash("Slash1", UIKit.RED, new Vector2(640f, 450f), new Vector2(1700f, 190f), -20f);
            Slash("Slash2", UIKit.WHITE, new Vector2(640f, 330f), new Vector2(1700f, 18f), -20f);
            Slash("Slash3", UIKit.RED_DEEP, new Vector2(860f, 240f), new Vector2(1000f, 56f), -20f);
            Slash("Slash4", UIKit.RED, new Vector2(1000f, -560f), new Vector2(1500f, 150f), -20f);
            Slash("Slash5", UIKit.INK, new Vector2(1000f, -450f), new Vector2(1500f, 26f), -20f);
            Slash("Slash6", UIKit.GOLD, new Vector2(1160f, -385f), new Vector2(760f, 12f), -20f);
            UIKit.OutlineText(_main, "HYPER", 300f, new Color(1f, 1f, 1f, 0.75f), new Vector2(0.5f, 0.5f), new Vector2(640f, 430f), new Vector2(1200f, 340f), -20f, 0.05f).raycastTarget = false;

            // Left side : the red slash carrying the name, chevrons running in it.
            UIShape lRed = UIKit.Tile(_main, "RedPanel", UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2(-640f, 40f), new Vector2(1260f, 1500f), 0.26f, 0f);
            UIKit.Chevrons(lRed.transform, "Chevrons", UIKit.RED, UIKit.RED_DARK, 60f);
            lRed.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
            UIKit.Tile(_main, "RedEdge", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(10f, 1500f), 0.26f, 0f);
            UIKit.Tile(_main, "RedEdge2", UIKit.RED_DEEP, new Vector2(0.5f, 0.5f), new Vector2(28f, 40f), new Vector2(26f, 1500f), 0.26f, 0f);

            // The name of the game : extruded, but it only ever moves with the panel.
            UI3DText lHyper = UI3DText.Create(_main, "HYPER", 250f, new Vector2(0f, 1f), new Vector2(480f, -170f), new Vector2(820f, 220f), UIKit.WHITE, UIKit.RED_DEEP, 12, 3f, TextAlignmentOptions.Left, 2f);
            UI3DText lSpeed = UI3DText.Create(_main, "SPEED", 250f, new Vector2(0f, 1f), new Vector2(540f, -360f), new Vector2(820f, 220f), UIKit.WHITE, UIKit.RED_DEEP, 12, 3f, TextAlignmentOptions.Left, 2f);
            lHyper.tiltX = 0f;
            lHyper.tiltY = 0f;
            lSpeed.tiltX = 0f;
            lSpeed.tiltY = 0f;
            UIKit.Tile(_main, "TitleBar", UIKit.WHITE, new Vector2(0f, 1f), new Vector2(520f, -490f), new Vector2(760f, 12f), 0.5f, 2f);

            _mainNav = UINav.Create(_main.gameObject);
            _mainNav.eventCamera = _canvas3D.worldCamera;
            MenuButton("PLAY", 96f, 330f, 560f, 130f, new Vector2(0f, 2f), () => SetScreen(ScreenId.Stages));
            MenuButton("SETTINGS", 54f, 200f, 420f, 90f, new Vector2(0f, 1f), () => SetScreen(ScreenId.Settings));
            MenuButton("EXIT", 54f, 102f, 340f, 60f, new Vector2(0f, 0f), Quit);
            _mainNav.Select(0);

            UICheckerStrip lChecker = UIKit.Checker(_main, "Checker", new Vector2(0.5f, 0f), new Vector2(-300f, 26f), new Vector2(2600f, 34f), UIKit.WHITE, UIKit.RED_DARK);
            lChecker.speed = 60f;
        }

        private void Slash(string pName, Color pColor, Vector2 pPosition, Vector2 pSize, float pRotation)
        {
            UIKit.Tile(_main, pName, pColor, new Vector2(0.5f, 0.5f), pPosition, pSize, 0.6f, pRotation);
        }

        // Slanted plate with a word, lying on the red slash.
        private void MenuButton(string pLabel, float pSize, float pY, float pWidth, float pX, Vector2 pNav, System.Action pAction)
        {
            RectTransform lHolder = UIFactory.CreateRect(_main, pLabel, new Vector2(0f, 0f), new Vector2(pX + pWidth * 0.5f, pY), new Vector2(pWidth, pSize * 1.45f));
            UIShape lFrame = UIKit.Tile(lHolder, "Frame", UIKit.RED_DEEP, new Vector2(0.5f, 0.5f), new Vector2(10f, -8f), new Vector2(pWidth + 14f, pSize * 1.45f + 14f), 0.3f);
            UIShape lBack = UIKit.Tile(lHolder, "Back", UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(pWidth, pSize * 1.45f), 0.3f);
            TextMeshProUGUI lLabel = UIKit.Text(lHolder, pLabel, pSize, UIKit.INK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(pWidth - 50f, pSize * 1.3f), UIKit.Font.Heavy);
            lLabel.enableAutoSizing = true;
            lLabel.fontSizeMin = 20f;
            lLabel.fontSizeMax = pSize;
            NavTile lTile = lHolder.gameObject.AddComponent<NavTile>();
            lTile.back = lBack;
            lTile.frame = lFrame;
            lTile.label = lLabel;
            lTile.normalBack = UIKit.WHITE;
            lTile.selectedBack = UIKit.INK;
            lTile.normalText = UIKit.INK;
            lTile.selectedText = UIKit.WHITE;
            lTile.selectedScale = 1.08f;
            _mainNav.Add(lHolder, pNav, pAction, lTile.SetSelected);
        }

        private void BuildStages()
        {
            _stages = UIFactory.Stretch(UIFactory.CreateRect(_canvas.transform, "Stages", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            UINav lNav = UINav.Create(_stages.gameObject);
            // The arrow is for the mouse : A / Enter launch the stage, B / Esc go back.
            lNav.autoSelect = false;
            lNav.keysSubmit = false;
            BackArrow(_stages, lNav, () => SetScreen(ScreenId.Main));
        }

        // Small arrow, top left of the screen.
        private void BackArrow(RectTransform pParent, UINav pNav, System.Action pAction)
        {
            RectTransform lHolder = UIFactory.CreateRect(pParent, "Back", new Vector2(0f, 1f), new Vector2(66f, -58f), new Vector2(84f, 64f));
            UIShape lBack = UIKit.Tile(lHolder, "Plate", UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 56f), 0.3f);
            RectTransform lArrow = UIFactory.CreateRect(lHolder, "Arrow", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 30f));
            UIFactory.CreateShape(lArrow, "Head", UIShape.ShapeKind.Triangle, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(-8f, 0f), new Vector2(24f, 22f), 90f);
            UIFactory.CreateImage(lArrow, "Tail", UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(22f, 7f));
            pNav.Add(lHolder, new Vector2(-1f, 5f), pAction, pOn => lBack.color = pOn ? UIKit.GOLD : UIKit.WHITE);
        }

        // ================================================================ screens

        private void SetScreen(ScreenId pScreen)
        {
            _screen = pScreen;
            _main.gameObject.SetActive(pScreen == ScreenId.Main);
            if (pScreen == ScreenId.Settings && !_settingsMenu.IsOpen) _settingsMenu.Open();
            _stages.gameObject.SetActive(pScreen == ScreenId.Stages);
            if (pScreen == ScreenId.Stages)
            {
                _select.Show(Mathf.Clamp(GameSession.UnlockedCount - 1, 0, LevelDatabase.Count - 1));
                AudioManager.Play("ui_whoosh", 0.8f);
            }
            else _select.Hide();
            if (pScreen == ScreenId.Main) _mainNav.Select(0);
        }

        // One click, one launch.
        private void Launch(int pIndex)
        {
            if (!GameSession.IsUnlocked(pIndex))
            {
                AudioManager.Play("ui_locked", 0.8f);
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

    // A second text copying the colour of the first (the value next to a setting's label).
    public class UIFollowColor : MonoBehaviour
    {
        private TextMeshProUGUI _source;
        private TextMeshProUGUI _target;

        public void Init(TextMeshProUGUI pSource, TextMeshProUGUI pTarget)
        {
            _source = pSource;
            _target = pTarget;
        }

        private void LateUpdate()
        {
            if (_source != null && _target != null) _target.color = _source.color;
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
