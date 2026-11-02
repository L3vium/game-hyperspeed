using System.Collections;
using Com.IsartDigital.Utils;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Core scene : builds the level from its definition, then intro shots, free roam on the start plaza,
    // slow countdown on the start arch, race, results. Every wait runs on scaled time so pausing freezes the flow.
    public class GameManager : Singleton<GameManager>
    {
        private enum Phase { Intro, PreStart, Countdown, Racing, Ended }

        private static readonly int ROAD_SCROLL = Shader.PropertyToID("_HS_RoadScroll");
        private static readonly int ROAD_SPEED = Shader.PropertyToID("_HS_RoadSpeed");

        private const float FREE_ROAM_MAX = 5f;
        private const float COUNT_STEP = 1f;

        [Tooltip("Dev cheats : every stage unlocked, F1 invincible, F2 boost, F3 next checkpoint, F4 win, F5 slow-mo, F6 layout debug, F9 autopilot. Forced on by the HS_CHEATS define (Dev build).")]
        [SerializeField] private bool _cheatCodes;

        private LevelDefinition _level;
        private int _levelIndex;
        private LevelLayout _layout;
        private PlayerController _player;
        private PlayerVisual _visual;
        private Rival _rival;
        private HUD _hud;
        private CameraRig _rig;
        private RespawnDirector _respawn;
        private Phase _phase = Phase.Intro;
        private float _raceTime;
        private bool _paused;
        private GameObject _pausePanel;
        private float _roadScroll;
        private float _hyperBlend;
        private bool _slowCheat;
        private bool _readyPressed;

        public bool Paused => _paused;

        protected override void OnAwake()
        {
            if (_cheatCodes) GameSession.CheatsEnabled = true;
            Time.timeScale = 1f;
            Juice.Paused = false;
            RaceContext.Clear();
            _levelIndex = GameSession.SelectedLevel;
            _level = LevelDatabase.Get(_levelIndex);
            BuildLevel();
        }

        private void BuildLevel()
        {
            Theme lTheme = _level.theme();
            RaceContext.Theme = lTheme;
            Transform lRoot = new GameObject("Level").transform;
            EnvironmentBuilder.Build(lTheme, lRoot);

            Random.State lState = Random.state;
            Random.InitState(_level.seed);
            Transform lTrackRoot = new GameObject("Track").transform;
            lTrackRoot.SetParent(lRoot, false);
            LevelBuilder lBuilder = new LevelBuilder(lTrackRoot, lTheme);
            _level.build(lBuilder);
            _layout = lBuilder.Build();
            Random.state = lState;
            RaceContext.Layout = _layout;
            EnvironmentBuilder.Decorate(lTheme, _layout, lRoot, _level.seed * 7 + 3);

            Camera lCamera = CameraRig.CreateCamera();
            _rig = lCamera.gameObject.AddComponent<CameraRig>();
            new GameObject("PostFX").AddComponent<PostFX>();

            _player = new GameObject("Player").AddComponent<PlayerController>();
            _visual = PlayerVisual.Create(_player);
            _player.Init(_layout, _visual);
            RaceContext.Player = _player;
            _rig.SetTarget(_player, _layout);
            _rig.Snap();

            float lStart = _level.chaser ? _level.chaserStart : 0f;
            _rival = Rival.Create(_level.rival, _level.rivalSpeed, _level.rivalDelay, _level.chaser, lStart, lRoot);
            _respawn = RespawnDirector.Create(_player, _rig);

            if (_level.chaser) _player.OnRespawned += () => StartCoroutine(PushChaserBack());
            _hud = HUD.Create(_player, _rival, _layout, _level);
            if (GameSession.CheatsEnabled)
            {
                gameObject.AddComponent<Autopilot>().Init(_player, _layout);
                gameObject.AddComponent<LayoutDebugDraw>().Init(_layout, _player);
            }
        }

        private void Start()
        {
            AudioManager.PlayMusic("music_intro", 0.45f);
            StartCoroutine(Flow());
        }

        private IEnumerator Flow()
        {
            SetCursorLocked(true);
            _phase = Phase.Intro;
            if (!GameSession.SkipTitleIntro)
            {
                IntroDirector lIntro = new IntroDirector(_layout, _level, _levelIndex, _player, _rival, _rig, _hud);
                yield return lIntro.Play();
            }
            GameSession.SkipTitleIntro = false;
            _hud.IntroClear();
            _rig.ClearOverride(0.6f);

            // Free roam on the start plaza : move, jump, look around before the countdown.
            _phase = Phase.PreStart;
            _player.BeginFreeRoam();
            _rig.freeLook = true;
            _hud.ShowReady(IntroDirector.Objective(_level, _layout));
            _readyPressed = false;
            for (float lTime = 0f; lTime < FREE_ROAM_MAX && !_readyPressed; lTime += Time.deltaTime)
            {
                if (lTime > 0.4f && !_paused && HSInput.SubmitPressed) _readyPressed = true;
                yield return null;
            }
            _hud.HideReady();

            // Slower countdown, shown on the start arch and in a compact top-center counter. You can still move.
            _phase = Phase.Countdown;
            string[] lSteps = { "3", "2", "1" };
            _player.ArmStart(Time.time + COUNT_STEP * lSteps.Length);
            for (int i = 0; i < lSteps.Length; i++)
            {
                StartGate.Instance?.ShowCount(lSteps[i], Color.white);
                _hud.ShowCount(lSteps[i], false);
                AudioManager.Play("sfx_count", 1f);
                yield return new WaitForSeconds(COUNT_STEP);
            }
            BeginRace();
        }

        // Respawning sends you back : give the chaser the same handicap once the new progress is known.
        private IEnumerator PushChaserBack()
        {
            yield return null;
            _rival.distance = Mathf.Min(_rival.distance, _player.Progress - 90f);
        }

        private void BeginRace()
        {
            _phase = Phase.Racing;
            RaceContext.Running = true;
            StartGate.Instance?.Open();
            _hud.ShowCount("GO!", true);
            AudioManager.Play("sfx_go", 1f);
            _rig.freeLook = false;
            _player.StartRunning();
            AudioManager.PlayMusic(_level.music, 0.6f);
            Juice.Shake(0.25f);
        }

        private void Update()
        {
            if (HSInput.PausePressed && _phase != Phase.Ended && !SceneTransition.IsBusy)
            {
                if (_paused) Resume();
                else Pause();
            }
            if (_paused) return;
            if (HSInput.RestartPressed && !SceneTransition.IsBusy && _phase != Phase.Intro) Retry();

            DriveGlobals();
            if (GameSession.CheatsEnabled) Cheats();

            RaceContext.PlayerProgress = _player.Progress;
            if (_phase != Phase.Racing) return;

            _raceTime += Time.deltaTime;
            _hud.SetRaceTime(_raceTime);
            AudioManager.SetMusicPitch(1f + Mathf.Clamp01((_player.Speed - 70f) / 130f) * 0.06f);

            if (_player.Progress >= _layout.finishS && _player.State != PlayerState.Dead && !_respawn.Busy)
                StartCoroutine(EndRoutine(true, "FINISH!!"));
            else if (!_level.chaser && _rival.Finished)
                StartCoroutine(EndRoutine(false, "TOO SLOW"));
            else if (_level.chaser && !_player.Invulnerable && !_respawn.Busy && _rival.distance >= _player.Progress - 2f && _rival.distance > 0f)
                StartCoroutine(EndRoutine(false, _level.rival == RivalKind.Death ? "REAPED" : "VAPORIZED"));
        }

        // Feeds speed to the post process, the road speed lines and the music.
        private void DriveGlobals()
        {
            float lSpeed = _player.Speed;
            _hyperBlend = Mathf.MoveTowards(_hyperBlend, _player.Hyper ? 1f : 0f, Time.deltaTime * 2f);
            if (PostFX.Instance != null)
            {
                PostFX.Instance.speed01 = _player.Speed01;
                PostFX.Instance.hyper01 = _hyperBlend;
            }
            _roadScroll += lSpeed * Time.deltaTime * 0.6f;
            Shader.SetGlobalFloat(ROAD_SCROLL, _roadScroll);
            Shader.SetGlobalFloat(ROAD_SPEED, Mathf.Clamp01((lSpeed - 25f) / 100f));
        }

        private void Cheats()
        {
            UnityEngine.InputSystem.Keyboard lKeyboard = UnityEngine.InputSystem.Keyboard.current;
            if (lKeyboard == null) return;
            if (lKeyboard.f1Key.wasPressedThisFrame)
            {
                _player.CheatInvincible = !_player.CheatInvincible;
                Juice.Popup(_player.CheatInvincible ? "INVINCIBLE ON" : "INVINCIBLE OFF", UIKit.GOLD, 1f);
            }
            if (lKeyboard.f2Key.wasPressedThisFrame && _phase == Phase.Racing) _player.ApplyBoost(70f, _player.Heading, 0f, "CHEAT BOOST");
            if (lKeyboard.f3Key.wasPressedThisFrame && _phase == Phase.Racing) _player.SkipToNextCheckpoint();
            if (lKeyboard.f4Key.wasPressedThisFrame && _phase == Phase.Racing) StartCoroutine(EndRoutine(true, "FINISH!!"));
            if (lKeyboard.f5Key.wasPressedThisFrame)
            {
                _slowCheat = !_slowCheat;
                Time.timeScale = _slowCheat ? 0.35f : 1f;
            }
        }

        private IEnumerator EndRoutine(bool pWin, string pHeadline)
        {
            _phase = Phase.Ended;
            RaceContext.Running = false;
            _player.Finish();
            AudioManager.SetMusicPitch(1f);

            if (pWin)
            {
                _visual.SetVictory();
                Juice.ImpactFrame(1f);
                Juice.Popup(pHeadline, UIKit.GOLD, 2f);
                AudioManager.Play("sfx_finish", 1f);
                Juice.SlowMo(0.25f, 1f);
                yield return new WaitForSecondsRealtime(1.1f);
                _rig.SetFinishMode(true);
                AudioManager.PlayMusic("music_win", 0.55f);
            }
            else
            {
                _visual.SetResult(false);
                Juice.Flash(_level.rival == RivalKind.Supernova ? Color.white : UIFactory.RED, 0.6f);
                Juice.Shake(0.6f);
                AudioManager.Play("sfx_lose", 1f);
                Juice.SlowMo(0.3f, 0.8f);
                yield return new WaitForSecondsRealtime(0.9f);
                AudioManager.PlayMusic("music_lose", 0.5f);
            }
            yield return new WaitForSecondsRealtime(0.3f);
            SetCursorLocked(false);

            int lRank = 0;
            bool lRecord = false;
            if (pWin)
            {
                float lRivalTime = _layout.finishS / _level.rivalSpeed + _level.rivalDelay;
                float lRatio = _raceTime / lRivalTime;
                lRank = lRatio < 0.8f ? 4 : lRatio < 0.88f ? 3 : lRatio < 0.96f ? 2 : 1;
                if (_player.Respawns >= 3 && lRank > 2) lRank--;
                lRecord = GameSession.RegisterWin(_levelIndex, _raceTime, lRank);
            }
            bool lHasNext = _levelIndex + 1 < LevelDatabase.Count;
            _hud.ShowResult(pWin, pHeadline, _raceTime, lRank, lRecord, GameSession.GetBestTime(_levelIndex), lHasNext,
                () => { GameSession.SelectedLevel = _levelIndex + 1; SceneTransition.LoadStage(_levelIndex + 1); },
                Retry,
                ToStageSelect);
            AudioManager.Play(pWin ? "sfx_rank" : "ui_back", 1f);
        }

        private void Retry()
        {
            if (_paused) Resume();
            GameSession.SkipTitleIntro = true;
            SceneTransition.LoadScene(SceneName.CORE, "RETRY", 1.6f);
        }

        private void ToStageSelect()
        {
            if (_paused) Resume();
            Time.timeScale = 1f;
            TitleScreen.OpenLevelSelect = true;
            SceneTransition.LoadScene(SceneName.TITLE_SCREEN, "STAGES");
        }

        private void Pause()
        {
            _paused = true;
            Juice.Paused = true;
            Time.timeScale = 0f;
            AudioManager.Play("ui_select", 0.7f);
            AudioManager.Duck(9999f);
            if (PostFX.Instance != null) PostFX.Instance.pause = 1f;
            SetCursorLocked(false);
            _pausePanel = _hud.ShowPause(Resume, Retry, ToStageSelect);
        }

        private void Resume()
        {
            _paused = false;
            Juice.Paused = false;
            Time.timeScale = _slowCheat ? 0.35f : 1f;
            AudioManager.Unduck();
            if (PostFX.Instance != null) PostFX.Instance.pause = 0f;
            SetCursorLocked(_phase != Phase.Ended);
            if (_pausePanel != null) Destroy(_pausePanel);
        }

        private static void SetCursorLocked(bool pLocked)
        {
            Cursor.lockState = pLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !pLocked;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SetCursorLocked(false);
            RaceContext.Clear();
            AudioManager.StopAllLoops();
            Interactable.All.Clear();
            Breakable.All.Clear();
            GrindRail.All.Clear();
            DeckChunk.ClearAll();
            Shader.SetGlobalFloat(ROAD_SPEED, 0f);
        }
    }
}
