using System.Collections.Generic;
using Com.IsartDigital.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Title scene : animated 3D backdrop, "press any button", main menu, controls and stage select.
    public class TitleScreen : MonoBehaviour
    {
        private enum Screen { Press, Main, Controls, Levels }

        public static bool OpenLevelSelect;

        private Screen _screen = Screen.Press;
        private RectTransform _root;
        private RectTransform _titleGroup;
        private TextMeshProUGUI _press;
        private MenuList _mainMenu;
        private RectTransform _controls;
        private RectTransform _levels;
        private Material _speedLines;
        private Camera _camera;
        private PlayerVisual _display;
        private Transform _stage;
        private float _orbit;
        private int _enteredFrame;

        private readonly List<RectTransform> _cards = new List<RectTransform>();
        private readonly List<UIShape> _cardBacks = new List<UIShape>();
        private int _selectedCard;
        private TextMeshProUGUI _detailTitle;
        private TextMeshProUGUI _detailTag;
        private TextMeshProUGUI _detailBest;
        private TextMeshProUGUI _leagueLabel;

        private void Awake()
        {
            Time.timeScale = 1f;
            RaceContext.Clear();
            BuildBackdrop();
            BuildUI();
            if (OpenLevelSelect)
            {
                OpenLevelSelect = false;
                ShowLevels();
            }
            else ShowPress();
        }

        private void Start()
        {
            AudioManager.PlayMusic("music_title", 0.5f);
        }

        // ---------------------------------------------------------------- backdrop

        private void BuildBackdrop()
        {
            Theme lTheme = Theme.Islands();
            Transform lRoot = new GameObject("Backdrop").transform;
            EnvironmentBuilder.Build(lTheme, lRoot);

            _camera = CameraRig.CreateCamera();
            _camera.fieldOfView = 70f;
            new GameObject("PostFX").AddComponent<PostFX>().speed01 = 0.35f;

            _stage = new GameObject("Stage").transform;
            _stage.SetParent(lRoot, false);
            // Endless treadmill road under the runner.
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
            _display.TickDisplay(lDt, 70f);

            // Scroll the stage toward the camera to fake running.
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
            float lAngle = Mathf.Sin(_orbit * Mathf.Deg2Rad * 2f) * 35f + (_screen == Screen.Levels ? 25f : -20f);
            Vector3 lOffset = Quaternion.Euler(0f, lAngle, 0f) * new Vector3(0f, 2.2f, -6.5f);
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, lOffset, 1f - Mathf.Exp(-3f * lDt));
            _camera.transform.LookAt(new Vector3(0f, 1.4f, 2f));
            _camera.transform.Rotate(0f, 0f, Mathf.Sin(Time.time * 0.8f) * 4f, Space.Self);

            _speedLines.SetFloat("_Intensity", 0.45f + Mathf.Sin(Time.time * 2f) * 0.05f);
            if (_titleGroup.gameObject.activeSelf)
            {
                float lPulse = 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.02f;
                _titleGroup.localScale = Vector3.one * lPulse;
            }

            if (Time.frameCount <= _enteredFrame + 1) return;
            switch (_screen)
            {
                case Screen.Press:
                    _press.alpha = Mathf.Repeat(Time.unscaledTime * 1.6f, 1f) > 0.35f ? 1f : 0.15f;
                    if (HSInput.AnyPressed && !SceneTransition.IsBusy)
                    {
                        AudioManager.Play("ui_select", 0.9f);
                        Juice.Flash(Color.white, 0.4f);
                        ShowMain();
                    }
                    break;
                case Screen.Controls:
                    if (HSInput.BackPressed || HSInput.SubmitPressed || HSInput.MouseClicked)
                    {
                        AudioManager.Play("ui_back", 0.7f);
                        ShowMain();
                    }
                    break;
                case Screen.Levels:
                    UpdateLevels();
                    break;
            }
        }

        // ---------------------------------------------------------------- UI

        private void BuildUI()
        {
            Canvas lCanvas = UIFactory.CreateCanvas("TitleCanvas", 10, transform);
            _root = (RectTransform)lCanvas.transform;

            RawImage lLines = UIFactory.CreateRect(_root, "SpeedLines", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            UIFactory.Stretch(lLines.rectTransform);
            _speedLines = new Material(Shader.Find("Hyperspeed/SpeedLines"));
            lLines.material = _speedLines;
            lLines.raycastTarget = false;

            // Title logo
            _titleGroup = UIFactory.CreateRect(_root, "Title", new Vector2(0.5f, 0.5f), new Vector2(-330f, 230f), new Vector2(1100f, 500f));
            UIFactory.CreateShape(_titleGroup, "Slash", UIShape.ShapeKind.Parallelogram, UIFactory.RED, new Vector2(0.5f, 0.5f), new Vector2(40f, 10f), new Vector2(1250f, 300f), -8f).skew = 0.5f;
            UIFactory.CreateShape(_titleGroup, "Slash2", UIShape.ShapeKind.Parallelogram, UIFactory.BLACK, new Vector2(0.5f, 0.5f), new Vector2(20f, -150f), new Vector2(900f, 70f), -8f).skew = 0.5f;
            Title("HYPER", new Vector2(-215f, 112f), 175f);
            Title("SPEED", new Vector2(150f, -40f), 195f);
            TextMeshProUGUI lSub = UIFactory.CreateText(_titleGroup, "RACE AGAINST ANYTHING", 46f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(20f, -150f), new Vector2(900f, 70f), true);
            lSub.fontStyle = FontStyles.Italic;
            lSub.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f);

            _press = UIFactory.CreateText(_root, "- PRESS ANY BUTTON -", 56f, UIFactory.WHITE, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1200f, 90f), true);
            _press.fontStyle = FontStyles.Italic;
            UIFactory.Outline(_press, UIFactory.BLACK, 0.3f);

            _mainMenu = MenuList.Create(_root, new Vector2(0.5f, 0.5f), new Vector2(460f, -40f));
            _mainMenu.Add("PLAY", ShowLevels);
            _mainMenu.Add("CONTROLS", ShowControls);
            _mainMenu.Add("QUIT", Quit);
            _mainMenu.OnBack = ShowPress;

            BuildControls();
            BuildLevels();

            TextMeshProUGUI lCredit = UIFactory.CreateText(_root, "ISART DIGITAL  //  HYPERSPEED", 24f, new Color(1f, 1f, 1f, 0.7f), new Vector2(1f, 0f), new Vector2(-250f, 30f), new Vector2(500f, 40f), false);
            lCredit.fontStyle = FontStyles.Italic | FontStyles.Bold;
        }

        private void Title(string pText, Vector2 pPosition, float pSize)
        {
            TextMeshProUGUI lShadow = UIFactory.CreateText(_titleGroup, pText, pSize, UIFactory.BLACK, new Vector2(0.5f, 0.5f), pPosition + new Vector2(14f, -12f), new Vector2(1000f, 240f), true);
            TextMeshProUGUI lText = UIFactory.CreateText(_titleGroup, pText, pSize, UIFactory.WHITE, new Vector2(0.5f, 0.5f), pPosition, new Vector2(1000f, 240f), true);
            foreach (TextMeshProUGUI lLabel in new[] { lShadow, lText })
            {
                lLabel.fontStyle = FontStyles.Italic;
                lLabel.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f);
                lLabel.characterSpacing = -4f;
            }
            UIFactory.Outline(lText, UIFactory.BLACK, 0.12f);
            lText.gameObject.AddComponent<UIJitter>().amount = 2f;
        }

        private void BuildControls()
        {
            _controls = UIFactory.CreateRect(_root, "Controls", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            UIFactory.CreateShape(_controls, "Back", UIShape.ShapeKind.Parallelogram, new Color(0f, 0f, 0f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(200f, -60f), new Vector2(1300f, 820f), -3f).skew = 0.08f;
            TextMeshProUGUI lHeader = UIFactory.CreateText(_controls, "CONTROLS", 110f, UIFactory.RED, new Vector2(0.5f, 0.5f), new Vector2(-150f, 330f), new Vector2(900f, 140f), true);
            lHeader.fontStyle = FontStyles.Italic;
            lHeader.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -6f);
            string[] lLines =
            {
                "<color=#EB0819>STEER</color>   A / D   -   Left stick",
                "<color=#EB0819>JUMP / DOUBLE / WALL JUMP</color>   Space   -   A",
                "<color=#EB0819>DASH</color>   Shift / Right click   -   RB / B",
                "<color=#EB0819>DRIFT (ground) / DIVE (air)</color>   Ctrl / C   -   LT / LB",
                "<color=#EB0819>PUNCH</color>   E / Left click   -   X / RT",
                "<color=#EB0819>RUN FASTER / BRAKE</color>   W / S",
                "<color=#EB0819>RESTART</color>   R   -   Select       <color=#EB0819>PAUSE</color>   Esc   -   Start",
                "",
                "<size=30><i>Dive right before landing for a PERFECT boost. Release a charged drift for speed.",
                "Over 60 m/s you smash through everything. Over 95 m/s you become LIGHT.</i></size>"
            };
            for (int i = 0; i < lLines.Length; i++)
            {
                TextMeshProUGUI lLine = UIFactory.CreateText(_controls, lLines[i], 38f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(220f, 200f - i * 62f), new Vector2(1200f, 60f), false, TextAlignmentOptions.Left);
                lLine.fontStyle = FontStyles.Bold;
            }
        }

        private void BuildLevels()
        {
            _levels = UIFactory.CreateRect(_root, "Levels", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            UIFactory.CreateShape(_levels, "Band", UIShape.ShapeKind.Parallelogram, new Color(0f, 0f, 0f, 0.75f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(2300f, 640f), -4f).skew = 0.05f;
            TextMeshProUGUI lHeader = UIFactory.CreateText(_levels, "STAGE SELECT", 100f, UIFactory.WHITE, new Vector2(0.5f, 1f), new Vector2(-470f, -90f), new Vector2(1000f, 130f), true);
            lHeader.fontStyle = FontStyles.Italic;
            lHeader.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -5f);
            UIFactory.Outline(lHeader, UIFactory.BLACK, 0.2f);
            _leagueLabel = UIFactory.CreateText(_levels, "", 44f, UIFactory.RED, new Vector2(0.5f, 1f), new Vector2(420f, -100f), new Vector2(800f, 70f), true);
            _leagueLabel.fontStyle = FontStyles.Italic;

            int lColumns = 5;
            for (int i = 0; i < LevelDatabase.Count; i++)
            {
                int lRow = i / lColumns;
                int lColumn = i % lColumns;
                Vector2 lPosition = new Vector2(-640f + lColumn * 320f - lRow * 30f, 220f - lRow * 190f);
                RectTransform lCard = UIFactory.CreateRect(_levels, "Card" + i, new Vector2(0.5f, 0.5f), lPosition, new Vector2(280f, 160f));
                UIShape lBack = UIFactory.CreateShape(lCard, "Back", UIShape.ShapeKind.Parallelogram, UIFactory.BLACK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 160f));
                lBack.skew = 0.12f;
                LevelDefinition lLevel = LevelDatabase.Levels[i];
                bool lUnlocked = GameSession.IsUnlocked(i);
                TextMeshProUGUI lNumber = UIFactory.CreateText(lCard, (i + 1).ToString("00"), 70f, lUnlocked ? UIFactory.WHITE : new Color(1f, 1f, 1f, 0.25f), new Vector2(0.5f, 0.5f), new Vector2(-70f, 25f), new Vector2(140f, 90f), true);
                lNumber.fontStyle = FontStyles.Italic;
                string lName = lUnlocked ? RivalFactory.DisplayName(lLevel.rival).Replace("THE ", "") : "LOCKED";
                TextMeshProUGUI lLabel = UIFactory.CreateText(lCard, lName, 26f, lUnlocked ? UIFactory.WHITE : new Color(1f, 1f, 1f, 0.35f), new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(260f, 40f), true);
                lLabel.fontStyle = FontStyles.Italic;
                int lRank = GameSession.GetRank(i);
                if (lRank > 0)
                {
                    TextMeshProUGUI lRankText = UIFactory.CreateText(lCard, GameSession.RankLetter(lRank), 64f, UIFactory.YELLOW, new Vector2(0.5f, 0.5f), new Vector2(80f, 25f), new Vector2(100f, 90f), true);
                    lRankText.fontStyle = FontStyles.Italic;
                    UIFactory.Outline(lRankText, UIFactory.BLACK, 0.25f);
                }
                _cards.Add(lCard);
                _cardBacks.Add(lBack);
            }

            RectTransform lDetail = UIFactory.CreateRect(_levels, "Detail", new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1800f, 200f));
            UIFactory.CreateShape(lDetail, "Back", UIShape.ShapeKind.Parallelogram, UIFactory.RED, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1700f, 150f), -2f).skew = 0.3f;
            _detailTitle = UIFactory.CreateText(lDetail, "", 70f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-260f, 20f), new Vector2(1100f, 90f), true, TextAlignmentOptions.Left);
            _detailTitle.fontStyle = FontStyles.Italic;
            UIFactory.Outline(_detailTitle, UIFactory.BLACK, 0.2f);
            _detailTag = UIFactory.CreateText(lDetail, "", 32f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-260f, -40f), new Vector2(1100f, 50f), false, TextAlignmentOptions.Left);
            _detailTag.fontStyle = FontStyles.Italic | FontStyles.Bold;
            _detailBest = UIFactory.CreateText(lDetail, "", 40f, UIFactory.BLACK, new Vector2(0.5f, 0.5f), new Vector2(560f, 0f), new Vector2(500f, 70f), true);
            _detailBest.fontStyle = FontStyles.Italic;

            TextMeshProUGUI lBack2 = UIFactory.CreateText(_levels, "ENTER : GO      ESC : BACK", 30f, UIFactory.WHITE, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(900f, 50f), false);
            lBack2.fontStyle = FontStyles.Bold | FontStyles.Italic;
            UIFactory.Outline(lBack2, UIFactory.BLACK, 0.25f);
        }

        private void UpdateLevels()
        {
            Vector2Int lMove = HSInput.MenuMove;
            int lPrevious = _selectedCard;
            if (lMove.x != 0) _selectedCard += lMove.x;
            if (lMove.y != 0) _selectedCard -= lMove.y * 5;
            _selectedCard = Mathf.Clamp(_selectedCard, 0, LevelDatabase.Count - 1);

            Vector2 lMouse = HSInput.MousePosition;
            for (int i = 0; i < _cards.Count; i++)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(_cards[i], lMouse, null)) continue;
                if (HSInput.MouseDelta.sqrMagnitude > 1f) _selectedCard = i;
                if (HSInput.MouseClicked)
                {
                    _selectedCard = i;
                    LaunchSelected();
                    return;
                }
            }
            if (_selectedCard != lPrevious) AudioManager.Play("ui_move", 0.6f);

            float lT = 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime);
            for (int i = 0; i < _cards.Count; i++)
            {
                bool lSelected = i == _selectedCard;
                bool lUnlocked = GameSession.IsUnlocked(i);
                _cards[i].localScale = Vector3.Lerp(_cards[i].localScale, Vector3.one * (lSelected ? 1.15f : 0.95f), lT);
                _cards[i].localRotation = Quaternion.Euler(0f, 0f, lSelected ? 4f + Mathf.Sin(Time.unscaledTime * 6f) * 1.5f : 0f);
                _cardBacks[i].color = lSelected ? (lUnlocked ? UIFactory.RED : UIFactory.DARK_RED) : (lUnlocked ? UIFactory.BLACK : new Color(0.15f, 0.12f, 0.15f));
            }

            LevelDefinition lLevel = LevelDatabase.Levels[_selectedCard];
            bool lOpen = GameSession.IsUnlocked(_selectedCard);
            _leagueLabel.text = LevelDatabase.WORLDS[lLevel.world];
            _detailTitle.text = lOpen ? lLevel.title : "???";
            _detailTag.text = lOpen ? lLevel.tagline : "Beat the previous stage to unlock.";
            float lBest = GameSession.GetBestTime(_selectedCard);
            _detailBest.text = lOpen ? "BEST " + GameSession.FormatTime(lBest) : "LOCKED";

            if (HSInput.SubmitPressed) LaunchSelected();
            else if (HSInput.BackPressed)
            {
                AudioManager.Play("ui_back", 0.7f);
                ShowMain();
            }
        }

        private void LaunchSelected()
        {
            if (!GameSession.IsUnlocked(_selectedCard))
            {
                AudioManager.Play("ui_locked", 0.8f);
                Juice.Shake(0.2f);
                return;
            }
            if (SceneTransition.IsBusy) return;
            AudioManager.Play("ui_select", 1f);
            GameSession.SelectedLevel = _selectedCard;
            SceneTransition.LoadScene(SceneName.CORE, "STAGE " + (_selectedCard + 1).ToString("00"));
        }

        // ---------------------------------------------------------------- screens

        private void SetScreen(Screen pScreen)
        {
            _screen = pScreen;
            _enteredFrame = Time.frameCount;
            _titleGroup.gameObject.SetActive(pScreen == Screen.Press || pScreen == Screen.Main);
            _press.gameObject.SetActive(pScreen == Screen.Press);
            _mainMenu.gameObject.SetActive(pScreen == Screen.Main);
            _controls.gameObject.SetActive(pScreen == Screen.Controls);
            _levels.gameObject.SetActive(pScreen == Screen.Levels);
        }

        private void ShowPress() => SetScreen(Screen.Press);

        private void ShowMain()
        {
            SetScreen(Screen.Main);
            _mainMenu.SetSelected(0);
        }

        private void ShowControls() => SetScreen(Screen.Controls);

        private void ShowLevels()
        {
            SetScreen(Screen.Levels);
            _selectedCard = Mathf.Clamp(Mathf.Min(GameSession.SelectedLevel, GameSession.UnlockedCount - 1), 0, LevelDatabase.Count - 1);
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
}
