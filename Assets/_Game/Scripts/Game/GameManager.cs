using System.Collections;
using Com.IsartDigital.Utils;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Core scene : builds the level from its definition, runs intro / countdown / race / results.
    public class GameManager : Singleton<GameManager>
    {
        private enum Phase { Intro, Racing, Ended }

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

        protected override void OnAwake()
        {
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

            _player = new GameObject("Player").AddComponent<PlayerController>();
            _visual = PlayerVisual.Create(_player);
            _player.Init(_path, _visual);
            RaceContext.Player = _player;
            _rig.target = _player;
            _rig.Snap();

            float lStart = _level.chaser ? _level.chaserStart : 4f;
            _rival = Rival.Create(_level.rival, _level.rivalSpeed, _level.rivalDelay, _level.chaser, lStart, lRoot);

            _hud = HUD.Create(_player, _rival, _path, _level);
            gameObject.AddComponent<Autopilot>().Init(_player);
        }

        private void Start()
        {
            AudioManager.PlayMusic("music_intro", 0.45f);
            StartCoroutine(IntroRoutine());
        }

        private IEnumerator IntroRoutine()
        {
            _rig.PlayIntro(GameSession.SkipTitleIntro ? 1.2f : 3.6f);
            if (!GameSession.SkipTitleIntro) yield return _hud.TitleCard(_level, _levelIndex);
            else yield return new WaitForSeconds(0.4f);
            GameSession.SkipTitleIntro = false;
            yield return _hud.Countdown(BeginRace);
        }

        // Called by the countdown on "GO".
        private void BeginRace()
        {
            _phase = Phase.Racing;
            RaceContext.Running = true;
            _player.StartRunning();
            _player.ApplyBoost(10f, _player.Heading, 0f, null);
            AudioManager.PlayMusic(_level.music, 0.55f);
            Juice.Shake(0.4f);
            Juice.FovKick(14f);
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

            RaceContext.PlayerProgress = _player.Progress;
            if (_phase != Phase.Racing) return;

            _raceTime += Time.deltaTime;
            _hud.SetRaceTime(_raceTime);
            AudioManager.SetMusicPitch(1f + Mathf.Clamp01((_player.Speed - 60f) / 100f) * 0.06f);

            if (_player.Progress >= _path.finishDistance && _player.DistanceFromPath < 35f)
            {
                StartCoroutine(EndRoutine(true, "FINISH!!"));
            }
            else if (!_level.chaser && _rival.Finished)
            {
                StartCoroutine(EndRoutine(false, "TOO SLOW..."));
            }
            else if (_level.chaser && _rival.distance >= _player.Progress - 2f && _rival.distance > 0f)
            {
                StartCoroutine(EndRoutine(false, _level.rival == RivalKind.Death ? "REAPED." : "VAPORIZED."));
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
                Juice.Impact(_player.transform.position + _player.Up * 2f, 1.2f);
                Juice.Popup(pHeadline, UIFactory.YELLOW, 2f);
                AudioManager.Play("sfx_finish", 1f);
                Time.timeScale = 0.25f;
                yield return new WaitForSecondsRealtime(1.1f);
                Time.timeScale = 1f;
                _rig.SetFinishMode(true);
                AudioManager.PlayMusic("music_win", 0.55f);
            }
            else
            {
                Juice.Flash(_level.rival == RivalKind.Supernova ? Color.white : UIFactory.RED, 0.9f);
                Juice.Shake(0.8f);
                AudioManager.Play("sfx_lose", 1f);
                Time.timeScale = 0.3f;
                yield return new WaitForSecondsRealtime(1f);
                Time.timeScale = 1f;
                AudioManager.PlayMusic("music_lose", 0.5f);
            }
            yield return new WaitForSecondsRealtime(0.4f);

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
            MenuList lMenu = _hud.ShowResult(pWin, pWin ? "YOU WIN!" : pHeadline, _raceTime, lRank, lRecord, GameSession.GetBestTime(_levelIndex), lHasNext,
                () => { GameSession.SelectedLevel = _levelIndex + 1; SceneTransition.LoadScene(SceneName.CORE, "STAGE " + (_levelIndex + 2).ToString("00")); },
                Retry,
                ToStageSelect);
            AudioManager.Play(pWin ? "sfx_rank" : "ui_back", 1f);
        }

        private void Retry()
        {
            GameSession.SkipTitleIntro = true;
            SceneTransition.LoadScene(SceneName.CORE, "", 1.6f);
        }

        private void ToStageSelect()
        {
            Time.timeScale = 1f;
            TitleScreen.OpenLevelSelect = true;
            SceneTransition.LoadScene(SceneName.TITLE_SCREEN, "");
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
            Time.timeScale = 1f;
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
        }
    }
}
