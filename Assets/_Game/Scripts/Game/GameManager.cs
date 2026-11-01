using System.Collections;
using Com.IsartDigital.Utils;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Core scene : builds the level from its definition, runs the rival intro / countdown / race / results.
    public class GameManager : Singleton<GameManager>
    {
        private enum Phase { Intro, Racing, Ended }

        private static readonly int ROAD_SCROLL = Shader.PropertyToID("_HS_RoadScroll");
        private static readonly int ROAD_SPEED = Shader.PropertyToID("_HS_RoadSpeed");

        [Tooltip("Dev cheats : every stage unlocked, F1 invincible, F2 hyper, F3 next checkpoint, F4 win, F5 slow-mo, F9 autopilot. Forced on by the HS_CHEATS define (Dev build).")]
        [SerializeField] private bool _cheatCodes;

        private LevelDefinition _level;
        private int _levelIndex;
        private TrackPath _path;
        private PlayerController _player;
        private PlayerVisual _visual;
        private Rival _rival;
        private HUD _hud;
        private CameraRig _rig;
        private Phase _phase = Phase.Intro;
        private float _raceTime;
        private bool _paused;
        private GameObject _pausePanel;
        private float _roadScroll;
        private float _hyperBlend;
        private bool _slowCheat;

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
            TrackBuilder lBuilder = new TrackBuilder(lTrackRoot, lTheme);
            _level.build(lBuilder);
            Random.state = lState;
            _path = lBuilder.path;
            RaceContext.Path = _path;
            EnvironmentBuilder.Decorate(lTheme, _path, lRoot, _level.seed * 7 + 3);

            Camera lCamera = CameraRig.CreateCamera();
            _rig = lCamera.gameObject.AddComponent<CameraRig>();
            new GameObject("PostFX").AddComponent<PostFX>();
            EnvironmentBuilder.AttachAmbient(lTheme, lCamera.transform);

            _player = new GameObject("Player").AddComponent<PlayerController>();
            _visual = PlayerVisual.Create(_player);
            _player.Init(_path, _visual);
            RaceContext.Player = _player;
            _rig.target = _player;
            _rig.Snap();

            float lStart = _level.chaser ? _level.chaserStart : 4f;
            _rival = Rival.Create(_level.rival, _level.rivalSpeed, _level.rivalDelay, _level.chaser, lStart, lRoot);

            if (_level.chaser) _player.OnRespawned += () => _rival.distance = Mathf.Min(_rival.distance, _player.Progress - 90f);
            _hud = HUD.Create(_player, _rival, _path, _level);
            if (GameSession.CheatsEnabled) gameObject.AddComponent<Autopilot>().Init(_player);
        }

        private void Start()
        {
            AudioManager.PlayMusic("music_intro", 0.45f);
            StartCoroutine(IntroRoutine());
        }

        // Rival fly-by under a VS card, then camera orbit and countdown (with a rocket-start window on GO).
        private IEnumerator IntroRoutine()
        {
            if (!GameSession.SkipTitleIntro)
            {
                float lStart = _rival.distance;
                TrackSample lSample = _path.Evaluate(20f);
                Vector3 lCamPos = lSample.position + lSample.Right * 14f + lSample.Up * 5f + lSample.Forward * 25f;
                StartCoroutine(_hud.VsCard(_level, _levelIndex, 2.2f));
                Juice.SlowMo(0.35f, 1.2f);
                for (float lTime = 0f; lTime < 2.1f; lTime += Time.unscaledDeltaTime)
                {
                    float lT = lTime / 2.1f;
                    _rival.IntroOffset(Mathf.Lerp(-110f, 160f, lT));
                    _rig.SetOverride(lCamPos, _rival.transform.position);
                    yield return null;
                }
                _rival.IntroOffset(lStart);
                _rig.ClearOverride();
            }
            _rig.PlayIntro(GameSession.SkipTitleIntro ? 1f : 1.6f);
            yield return new WaitForSeconds(GameSession.SkipTitleIntro ? 0.5f : 1.2f);
            GameSession.SkipTitleIntro = false;
            _player.ArmStart(Time.time + 2.25f);
            yield return _hud.Countdown(BeginRace);
        }

        private void BeginRace()
        {
            _phase = Phase.Racing;
            RaceContext.Running = true;
            _player.StartRunning();
            AudioManager.PlayMusic(_level.music, 0.6f);
            Juice.Shake(0.4f);
            Juice.FovKick(16f);
            Juice.ImpactFrame(0.6f);
        }

        private void Update()
        {
            if (HSInput.PausePressed && _phase != Phase.Ended && !SceneTransition.IsBusy)
            {
                if (_paused) Resume();
                else Pause();
            }
            if (_paused) return;
            if (HSInput.RestartPressed && !SceneTransition.IsBusy) Retry();

            DriveGlobals();
            if (GameSession.CheatsEnabled) Cheats();

            RaceContext.PlayerProgress = _player.Progress;
            if (_phase != Phase.Racing) return;

            _raceTime += Time.deltaTime;
            _hud.SetRaceTime(_raceTime);
            AudioManager.SetMusicPitch(1f + Mathf.Clamp01((_player.Speed - 60f) / 100f) * 0.06f);

            if (_player.Progress >= _path.finishDistance && _player.DistanceFromPath < 60f)
                StartCoroutine(EndRoutine(true, "FINISH!!"));
            else if (!_level.chaser && _rival.Finished)
                StartCoroutine(EndRoutine(false, "TOO SLOW"));
            else if (_level.chaser && _rival.distance >= _player.Progress - 2f && _rival.distance > 0f)
                StartCoroutine(EndRoutine(false, _level.rival == RivalKind.Death ? "REAPED" : "VAPORIZED"));
        }

        // Feeds speed to the post process, the road speed lines and the music.
        private void DriveGlobals()
        {
            float lSpeed = _player.Speed;
            _hyperBlend = Mathf.MoveTowards(_hyperBlend, _player.Hyper ? 1f : 0f, Time.deltaTime * 3f);
            if (PostFX.Instance != null)
            {
                PostFX.Instance.speed01 = _player.Speed01;
                PostFX.Instance.hyper01 = _hyperBlend;
            }
            _roadScroll += lSpeed * Time.deltaTime * 0.6f;
            Shader.SetGlobalFloat(ROAD_SCROLL, _roadScroll);
            Shader.SetGlobalFloat(ROAD_SPEED, Mathf.Clamp01((lSpeed - 20f) / 70f));
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
                Juice.ImpactFrame(1.2f);
                Juice.Popup(pHeadline, UIKit.GOLD, 2f);
                AudioManager.Play("sfx_finish", 1f);
                Juice.SlowMo(0.2f, 1.2f);
                yield return new WaitForSecondsRealtime(1.2f);
                _rig.SetFinishMode(true);
                AudioManager.PlayMusic("music_win", 0.55f);
            }
            else
            {
                Juice.Flash(_level.rival == RivalKind.Supernova ? Color.white : UIFactory.RED, 0.9f);
                Juice.Shake(0.8f);
                Juice.ImpactFrame(1f);
                AudioManager.Play("sfx_lose", 1f);
                Juice.SlowMo(0.25f, 1f);
                yield return new WaitForSecondsRealtime(1f);
                AudioManager.PlayMusic("music_lose", 0.5f);
            }
            yield return new WaitForSecondsRealtime(0.3f);

            int lRank = 0;
            bool lRecord = false;
            if (pWin)
            {
                float lRivalTime = _path.finishDistance / _level.rivalSpeed + _level.rivalDelay;
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
            GameSession.SkipTitleIntro = true;
            SceneTransition.LoadScene(SceneName.CORE, "RETRY", 1.6f);
        }

        private void ToStageSelect()
        {
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
            _pausePanel = _hud.ShowPause(Resume, () => { Resume(); Retry(); }, () => { Resume(); ToStageSelect(); });
        }

        private void Resume()
        {
            _paused = false;
            Juice.Paused = false;
            Time.timeScale = _slowCheat ? 0.35f : 1f;
            if (_pausePanel != null) Destroy(_pausePanel);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            RaceContext.Clear();
            AudioManager.StopAllLoops();
            Interactable.All.Clear();
            Breakable.All.Clear();
            GrindRail.All.Clear();
            Shader.SetGlobalFloat(ROAD_SPEED, 0f);
        }
    }
}
