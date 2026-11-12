using System.Collections;
using Com.IsartDigital.Utils;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Core scene : builds the stage, then straight into the countdown (you can already move on the plaza), race, results.
    // Every wait runs on scaled time so pausing freezes the flow.
    public class GameManager : Singleton<GameManager>
    {
        private enum Phase { Countdown, Racing, Ended }

        private static readonly int ROAD_SCROLL = Shader.PropertyToID("_HS_RoadScroll");
        private static readonly int ROAD_SPEED = Shader.PropertyToID("_HS_RoadSpeed");

        private const float COUNT_STEP = 1.5f;
        // Rank thresholds : the lead on the line as a share of the race time. A run at cruise with no boost is around a B.
        private const float RANK_S = 0.4f;
        private const float RANK_A = 0.25f;
        private const float RANK_B = 0.1f;

        private float _endLead;

        [Tooltip("Dev cheats, forced on by the HS_CHEATS define (Dev build).")]
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
        private Autopilot _autopilot;
        private DevCheatBar _cheatBar;
        private Phase _phase = Phase.Countdown;
        private float _raceTime;
        private bool _paused;
        private GameObject _pausePanel;
        private float _roadScroll;
        private float _hyperBlend;
        private bool _slowCheat;
        // The rival crossed first : you still run to the line, then it is over.
        private bool _beaten;
        private float _beatenTime;
        private bool _climax;

        public bool Paused => _paused;

        protected override void OnAwake()
        {
            if (_cheatCodes) GameSession.CheatsEnabled = true;
            GameSession.ApplyDisplay();
            AudioManager.LoadVolumes();
            Time.timeScale = 1f;
            Juice.Paused = false;
            RaceContext.Clear();
            _levelIndex = GameSession.SelectedLevel;
            // The carousel comes back to the stage you were in (test runs on autopilot do not move it).
            if (!Autopilot.Enabled) GameSession.RememberStage(_levelIndex);
            _level = LevelDatabase.Get(_levelIndex);
            BuildLevel();
        }

        private void BuildLevel()
        {
            Theme lTheme = _level.theme();
            RaceContext.Theme = lTheme;
            SpeedProfile lProfile = LevelDatabase.Profile(_level);
            SpeedProfile.Current = lProfile;
            Transform lRoot = new GameObject("Level").transform;
            EnvironmentBuilder.Build(lTheme, lRoot, lProfile);

            Random.State lState = Random.state;
            Random.InitState(_level.seed);
            Transform lTrackRoot = new GameObject("Course").transform;
            lTrackRoot.SetParent(lRoot, false);
            // A stage shaped in the Level Design window wins over its recipe.
            StageAsset lAsset = StageAsset.Load(_levelIndex);
            if (lAsset != null) _layout = StageBuilder.Build(lAsset.data, lTheme, lProfile, lTrackRoot);
            else _layout = LevelDatabase.Recipe(_levelIndex, lTrackRoot).Build();
            Random.state = lState;
            RaceContext.Layout = _layout;
            if (_level.collapse) CourseBreak.Create(_layout);

            Camera lCamera = CameraRig.CreateCamera();
            _rig = lCamera.gameObject.AddComponent<CameraRig>();
            // Field of view past 180 degrees : six cameras recomposed (setting WIDE ANGLE).
            PanoramaRig.Attach(lCamera);
            new GameObject("PostFX").AddComponent<PostFX>();
            new GameObject("Wind").AddComponent<WindField>();

            _player = new GameObject("Player").AddComponent<PlayerController>();
            _visual = PlayerVisual.Create(_player);
            _player.Init(_layout, _visual);
            RaceContext.Player = _player;
            _rig.SetTarget(_player, _layout);
            _rig.Snap();

            float lStart = _level.chaser ? _level.chaserStart * lProfile.k : _layout.spawnS + 15f;
            _rival = Rival.Create(_level.rival, LevelDatabase.RivalSpeed(_level), _level.rivalDelay, _level.chaser, lStart, lRoot);
            _respawn = RespawnDirector.Create(_player, _rig);

            if (_level.chaser) _player.OnRespawned += () => StartCoroutine(PushChaserBack());
            _hud = HUD.Create(_player, _rival, _layout, _level);
            if (GameSession.CheatsEnabled)
            {
                _autopilot = gameObject.AddComponent<Autopilot>();
                _autopilot.Init(_player, _layout);
                _cheatBar = DevCheatBar.Create();
            }
        }

        private void Start()
        {
            CursorGuardBehaviour.OnFocusLost += FocusLost;
            HSJuice.Warmup();
            // Everything heavy the race will need is prepared while the transition still covers the screen.
            SceneTransition.Hold();
            StartCoroutine(WarmupRoutine());
            AudioManager.PlayMusic("music_intro", 0.38f);
            StartCoroutine(Flow());
        }

        private IEnumerator Flow()
        {
            SetCursorLocked(true);
            _phase = Phase.Countdown;

            // The runner falls from the sky onto the plaza while the camera swoops down behind it, and the count is
            // already running : you can move, jump and look around from the first instant.
            _player.BeginFreeRoam(true, 55f);
            _rig.freeLook = true;
            _rig.Snap();
            Vector3 lForward = _layout.Flow(_layout.spawnS);
            Vector3 lSpot = _layout.GroundPoint(_layout.spawnS, 0f);
            _rig.CutTo(lSpot + Vector3.up * 40f - lForward * 60f, lSpot + lForward * 120f + Vector3.up * 16f, 72f);
            _rig.ClearOverride(1.3f);

            while (SceneTransition.IsBusy) yield return null;

            string[] lSteps = { "3", "2", "1" };
            _player.ArmStart(Time.time + COUNT_STEP * lSteps.Length);
            for (int i = 0; i < lSteps.Length; i++)
            {
                StartGate.Instance?.ShowCount(lSteps[i], Color.white);
                _hud.ShowCount(lSteps[i], false);
                HSJuice.Play(JuiceId.CountdownTick, _player.transform.position);
                yield return new WaitForSeconds(COUNT_STEP);
            }
            BeginRace();
        }

        // Runs unseen under the transition : the world's stems decoded off the main thread, one of each burst drawn
        // once in front of the camera so their shaders are compiled before the first real one.
        private IEnumerator WarmupRoutine()
        {
            string lMusic = "music_w" + (_level.world + 1);
            AudioManager.PrepareMusicLayers(lMusic);
            float lStart = Time.realtimeSinceStartup;
            Vector3 lAhead = _rig.Cam.transform.position + _rig.Cam.transform.forward * 60f;
            FX.Shards(lAhead, Color.white, 4, 1f, 1f, Vector3.zero);
            FX.Sparks(lAhead, Color.white, 4, 1f);
            FX.Puff(lAhead, Color.white, 2, 2f);
            FX.Shockwave(lAhead, Vector3.up, Color.white, 4f, 0.2f);
            yield return null;
            yield return null;
            while (!AudioManager.MusicLayersReady(lMusic) && Time.realtimeSinceStartup - lStart < 4f) yield return null;
            Debug.Log("[HS-LOAD] warm-up " + ((Time.realtimeSinceStartup - lStart) * 1000f).ToString("F0") + " ms");
            SceneTransition.Release();
        }

        // Respawning sends you back : give the chaser the same handicap once the new progress is known.
        private IEnumerator PushChaserBack()
        {
            yield return null;
            _rival.distance = Mathf.Min(_rival.distance, _player.Progress - 200f * _layout.profile.k);
        }

        private void BeginRace()
        {
            _phase = Phase.Racing;
            RaceContext.Running = true;
            StartGate.Instance?.Open();
            _hud.ShowCount("GO!", true);
            _rig.freeLook = false;
            _player.StartRunning();
            // Each world its own drum & bass, in three stems mixed by the speed state (see DriveGlobals).
            AudioManager.PlayMusicLayers("music_w" + (_level.world + 1), 0.5f);
            HSJuice.Play(JuiceId.Go, _player.transform.position);
        }

        private void Update()
        {
            // Keys typed in another window must never reach a game running in the background.
            bool lFocused = Application.isFocused;
            if (lFocused && HSInput.PausePressed && _phase != Phase.Ended && !SceneTransition.IsBusy && SettingsMenu.Current == null)
            {
                if (_paused) Resume();
                else Pause();
            }
            if (_paused) return;
            if (lFocused && HSInput.RestartPressed && !SceneTransition.IsBusy) Retry();

            DriveGlobals();
            if (lFocused && GameSession.CheatsEnabled) Cheats();
            if (GameSession.CheatsEnabled) PerfLog();

            RaceContext.PlayerProgress = _player.Progress;
            if (_phase != Phase.Racing) return;

            _raceTime += Time.deltaTime;
            _hud.SetRaceTime(_raceTime);
            AudioManager.SetMusicPitch(1f + _player.Speed01 * 0.08f);

            UpdateClimax();
            if (!_level.chaser && _rival.Finished && !_beaten)
            {
                _beaten = true;
                _beatenTime = _raceTime;
                Juice.Popup("TOO SLOW", UIFactory.RED, 1f);
                _hud.RivalFinished();
                HSJuice.Play(JuiceId.RivalFinished, _rival.transform.position);
            }
            if (_player.Progress >= _layout.finishS && _player.State != PlayerState.Dead && !_respawn.Busy)
                StartCoroutine(EndRoutine(!_beaten, _beaten ? "TOO SLOW" : "FINISH!!"));
            else if (_beaten && _raceTime - _beatenTime > 20f)
                StartCoroutine(EndRoutine(false, "TOO SLOW"));
            else if (_level.chaser && !_player.Invulnerable && !_player.CheatInvincible && !_respawn.Busy && !_rival.Passing && _rival.distance >= _player.Progress - 2f && _rival.distance > 0f)
            {
                if (_autopilot != null) _autopilot.DumpTrace("[HS-TRACE]");
                Debug.Log("[HS-CAUGHT] state=" + _player.State + " v=" + _player.Speed.ToString("F0") + " s=" + _player.Progress.ToString("F0") + " lat=" + _player.Lateral.ToString("F0") + " rival=" + _rival.distance.ToString("F0") + " air=" + _player.AirTime.ToString("F1"));
                StartCoroutine(EndRoutine(false, CaughtWord()));
            }
        }

        // The last straight, neck and neck : bars close in from top and bottom, the colours burn.
        private void UpdateClimax()
        {
            if (PostFX.Instance == null || _layout.climaxS <= 0f) return;
            bool lIn = _player.Progress > _layout.climaxS && _player.Progress < _layout.finishS && !_beaten;
            if (lIn && !_climax)
            {
                _climax = true;
                _hud.Notify("FINAL SPRINT", UIFactory.YELLOW);
                HSJuice.Play(JuiceId.FinalSprint, _player.transform.position);
            }
            float lNeck = Mathf.Clamp01(1f - Mathf.Abs(_rival.Gap) / (_layout.profile.cruise * 1.2f));
            PostFX.Instance.letterbox = lIn ? Mathf.Lerp(0.1f, 0.16f, lNeck) : 0f;
            PostFX.Instance.intensity = lIn ? Mathf.Lerp(0.35f, 0.8f, lNeck) : 0f;
        }

        private string CaughtWord()
        {
            switch (_level.rival)
            {
                case RivalKind.TRex: return "EATEN";
                case RivalKind.Death: return "REAPED";
                default: return "VAPORIZED";
            }
        }

        // Feeds speed to the post process, the ground speed lines and the music.
        private void DriveGlobals()
        {
            float lSpeed = _player.Speed;
            _hyperBlend = Mathf.MoveTowards(_hyperBlend, _player.Hyper ? 1f : 0f, Time.deltaTime * 2f);
            if (PostFX.Instance != null)
            {
                PostFX.Instance.speed01 = _player.Speed01;
                PostFX.Instance.hyper01 = _hyperBlend;
                PostFX.Instance.light01 = _player.LightForm ? 1f : 0f;
            }
            AudioManager.SetMusicIntensity(Mathf.Max(_hyperBlend, _player.BoostAmount * 0.6f), _player.LightForm ? 1f : 0f);
            _roadScroll += lSpeed * Time.deltaTime;
            Shader.SetGlobalFloat(ROAD_SCROLL, _roadScroll);
            Shader.SetGlobalFloat(ROAD_SPEED, Mathf.Clamp01(_player.Speed01 + 0.25f));
        }

        // Dev build : the average frame time every 10 s in the player log (wide angle cost check).
        private float _perfTime;
        private int _perfFrames;
        private float _perfWorst;
        private int _perfWindow;

        private void PerfLog()
        {
            // The transition's own frames are not the race's.
            if (SceneTransition.IsBusy) return;
            _perfTime += Time.unscaledDeltaTime;
            _perfFrames++;
            _perfWorst = Mathf.Max(_perfWorst, Time.unscaledDeltaTime);
            if (_perfTime < 10f) return;
            Debug.Log("[HS-PERF] stage=" + (_levelIndex + 1) + " window=" + _perfWindow + " avg=" + (_perfTime / _perfFrames * 1000f).ToString("F1") + "ms worst=" + (_perfWorst * 1000f).ToString("F0") + "ms fps=" + (_perfFrames / _perfTime).ToString("F0") + " wide=" + GameSession.WideAngle + " res=" + Screen.width + "x" + Screen.height);
            _perfTime = 0f;
            _perfFrames = 0;
            _perfWorst = 0f;
            _perfWindow++;
        }

        // Digit keys, shown bottom-right in the Dev build only.
        private void Cheats()
        {
            UnityEngine.InputSystem.Keyboard lKeyboard = UnityEngine.InputSystem.Keyboard.current;
            if (lKeyboard == null) return;
            bool lRacing = _phase == Phase.Racing;
            if (lKeyboard.digit1Key.wasPressedThisFrame)
            {
                _player.CheatInvincible = !_player.CheatInvincible;
                _cheatBar.SetActive(0, _player.CheatInvincible);
            }
            if (lKeyboard.digit2Key.wasPressedThisFrame && lRacing) { _player.ApplyBoost(_layout.profile.cruise * 0.6f, _player.Heading, 0f, null); _cheatBar.Flash(1); }
            if (lKeyboard.digit3Key.wasPressedThisFrame && lRacing) { _player.SkipToNextCheckpoint(); _cheatBar.Flash(2); }
            if (lKeyboard.digit4Key.wasPressedThisFrame && lRacing) { StartCoroutine(EndRoutine(true, "FINISH!!")); _cheatBar.Flash(3); }
            if (lKeyboard.digit5Key.wasPressedThisFrame)
            {
                _slowCheat = !_slowCheat;
                Time.timeScale = _slowCheat ? 0.35f : 1f;
                _cheatBar.SetActive(4, _slowCheat);
            }
            if (lKeyboard.digit6Key.wasPressedThisFrame && _levelIndex + 1 < LevelDatabase.Count)
            {
                GameSession.SelectedLevel = _levelIndex + 1;
                SceneTransition.LoadStage(_levelIndex + 1);
                _cheatBar.Flash(5);
            }
            if (lKeyboard.digit7Key.wasPressedThisFrame)
            {
                _autopilot.Toggle();
                _cheatBar.SetActive(6, Autopilot.Enabled);
            }
            if (lKeyboard.digit8Key.wasPressedThisFrame && lRacing) { _player.ApplyBoost(_layout.profile.lightEnter, _player.Heading, 0f, null); _cheatBar.Flash(7); }
            if (lKeyboard.digit9Key.wasPressedThisFrame && lRacing)
            {
                MeteorDirector lDirector = FindAnyObjectByType<MeteorDirector>();
                if (lDirector != null) Meteor.CreateIncoming(lDirector.transform, _layout, _player, _player.Progress + _player.Speed * 2.5f, 18f, RaceContext.Theme);
                _cheatBar.Flash(8);
            }
            if (lKeyboard.digit0Key.wasPressedThisFrame) { Retry(); _cheatBar.Flash(9); }
            _cheatBar.SetActive(6, Autopilot.Enabled);
        }

        private IEnumerator EndRoutine(bool pWin, string pHeadline)
        {
            _phase = Phase.Ended;
            // Lead on the line, in seconds of the rival's own running : measured before anything else moves.
            float lRivalRate = Mathf.Max(1f, _rival.CurrentRate);
            _endLead = _level.chaser ? Mathf.Max(0f, _player.Progress - _rival.distance) / lRivalRate : Mathf.Max(0f, _layout.finishS - _rival.distance) / lRivalRate;
            RaceContext.Running = false;
            Debug.Log("[HS-END] " + pHeadline + " stage=" + (_levelIndex + 1) + " time=" + _raceTime.ToString("F1") + " s=" + _player.Progress.ToString("F0") + "/" + _layout.finishS.ToString("F0") + " rival=" + _rival.distance.ToString("F0") + " respawns=" + _player.Respawns + " top=" + _player.TopSpeed.ToString("F0"));
            _player.Finish();
            AudioManager.SetMusicPitch(1f);

            if (pWin)
            {
                _visual.SetVictory();
                HSJuice.Play(JuiceId.FinishWin, _player.transform.position);
                yield return new WaitForSecondsRealtime(1.1f);
                _rig.SetFinishMode(true);
                // Cinema : the runner keeps running, the camera goes round it, the bars close in, the colours burn.
                if (PostFX.Instance != null)
                {
                    PostFX.Instance.letterbox = 0.3f;
                    PostFX.Instance.intensity = 1f;
                }
                AudioManager.PlayMusic("music_win", 0.55f);
                // Let the shot breathe before the results come in.
                yield return new WaitForSecondsRealtime(1.8f);
            }
            else
            {
                _visual.SetResult(false);
                HSJuice.Play(JuiceId.FinishLose, _player.transform.position);
                if (PostFX.Instance != null) { PostFX.Instance.letterbox = 0f; PostFX.Instance.intensity = 0f; }
                yield return new WaitForSecondsRealtime(0.9f);
                AudioManager.PlayMusic("music_lose", 0.5f);
            }
            yield return new WaitForSecondsRealtime(0.3f);
            SetCursorLocked(false);

            int lRank = 0;
            bool lRecord = false;
            string lMargin = null;
            if (pWin)
            {
                // The rank is the lead you really had on the line : how long the rival still needed to finish, against
                // the length of your race. Crossing neck and neck is a C, however fast the stage was.
                float lLead = _endLead;
                float lShare = lLead / Mathf.Max(1f, _raceTime);
                lRank = lShare >= RANK_S ? 4 : lShare >= RANK_A ? 3 : lShare >= RANK_B ? 2 : 1;
                if (_player.Respawns >= 3 && lRank > 1) lRank--;
                lRecord = GameSession.RegisterWin(_levelIndex, _raceTime, lRank);
                lMargin = _level.chaser ? "ESCAPED BY " + lLead.ToString("F1") + " s" : "AHEAD BY " + lLead.ToString("F1") + " s";
                Debug.Log("[HS-RANK] stage=" + (_levelIndex + 1) + " lead=" + lLead.ToString("F1") + "s share=" + lShare.ToString("F2") + " rank=" + GameSession.RankLetter(lRank) + " respawns=" + _player.Respawns);
            }
            else if (_beaten) lMargin = RivalFactory.DisplayName(_level.rival) + " WON BY " + (_raceTime - _beatenTime).ToString("F1") + " s";
            bool lHasNext = _levelIndex + 1 < LevelDatabase.Count;
            _hud.ShowResult(pWin, pHeadline, _raceTime, lRank, lRecord, GameSession.GetBestTime(_levelIndex), lHasNext,
                () => { GameSession.SelectedLevel = _levelIndex + 1; SceneTransition.LoadStage(_levelIndex + 1); },
                Retry,
                ToStageSelect,
                lMargin);
            AudioManager.Play(pWin ? "sfx_rank" : "ui_back", 1f);
        }

        private void Retry()
        {
            if (_paused) Resume();
            SceneTransition.LoadScene(SceneName.CORE, 1.6f);
        }

        private void ToStageSelect()
        {
            if (_paused) Resume();
            Time.timeScale = 1f;
            TitleScreen.OpenLevelSelect = true;
            SceneTransition.LoadScene(SceneName.TITLE_SCREEN);
        }

        private void Pause()
        {
            if (_paused) return;
            _paused = true;
            Juice.Paused = true;
            Time.timeScale = 0f;
            HSJuice.Play(JuiceId.Pause, Vector3.zero);
            AudioManager.Duck(9999f);
            if (PostFX.Instance != null) PostFX.Instance.pause = 1f;
            SetCursorLocked(false);
            // Nothing of the race HUD stays over the pause menu.
            _hud.SetPaused(true);
            if (_cheatBar != null) _cheatBar.gameObject.SetActive(false);
            _pausePanel = _hud.ShowPause(Resume, Retry, ToStageSelect);
        }

        private void Resume()
        {
            if (!_paused) return;
            _paused = false;
            _hud.SetPaused(false);
            if (_cheatBar != null) _cheatBar.gameObject.SetActive(true);
            Juice.Paused = false;
            Time.timeScale = _slowCheat ? 0.35f : 1f;
            HSJuice.Play(JuiceId.Resume, Vector3.zero);
            AudioManager.Unduck();
            if (PostFX.Instance != null) PostFX.Instance.pause = 0f;
            SetCursorLocked(_phase != Phase.Ended);
            if (_pausePanel != null)
            {
                // Off at once : the press that closed it must not also click one of its buttons this frame.
                _pausePanel.SetActive(false);
                Destroy(_pausePanel);
            }
        }

        private static void SetCursorLocked(bool pLocked) => CursorGuard.Set(pLocked ? CursorMode.Game : CursorMode.Menu);

        // The window lost the focus (Alt-Tab, a click on another window) : the race waits for you in the pause menu.
        private void FocusLost()
        {
            if (Autopilot.Enabled || _paused || _phase == Phase.Ended || SceneTransition.IsBusy || SettingsMenu.Current != null) return;
            Pause();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CursorGuardBehaviour.OnFocusLost -= FocusLost;
            SetCursorLocked(false);
            RaceContext.Clear();
            AudioManager.StopAllLoops();
            Interactable.All.Clear();
            Breakable.All.Clear();
            GrindRail.All.Clear();
            TrackBaker.Chunks.Clear();
            Shader.SetGlobalFloat(ROAD_SPEED, 0f);
        }
    }
}
