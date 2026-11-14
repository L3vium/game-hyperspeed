using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Persistent 2D audio : pooled one-shots, keyed loops (wind, grind...) and crossfaded music.
    public class AudioManager : MonoBehaviour
    {
        private const int POOL_SIZE = 28;
        private const float LOOP_FADE_SPEED = 6f;
        private const float MUSIC_FADE_SPEED = 1.2f;

        private static AudioManager _instance;

        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AudioSource> _loops = new Dictionary<string, AudioSource>();
        private readonly Dictionary<string, float> _loopTargets = new Dictionary<string, float>();
        private readonly List<AudioSource> _pool = new List<AudioSource>();
        private int _poolIndex;

        private AudioSource _musicA;
        private AudioSource _musicB;
        // Race music in three synchronised stems (base, hyper, light) mixed by the speed state.
        private readonly AudioSource[] _layers = new AudioSource[3];
        private readonly float[] _layerTargets = { 1f, 0f, 0f };
        private string _layerKey;
        private float _layerVolume;
        private bool _layersOn;
        private AudioLowPassFilter _musicFilter;
        private float _duckTimer;
        private float _musicVolume = 0.6f;
        private string _currentMusic;

        public static float MasterSfx = 0.9f;
        public static float MusicMaster = 0.8f;

        // ---------------- audio v6 (2026-11-12, Tools/audio_v6, Resources/audio_manifest.json)
        // The stage's world (1..3) : "sfx_hyper" plays sfx_hyper_w2 on a world 2 stage when that variant exists.
        public static int World = 1;
        // Footstep surface of the stage (grass, sand, snow, rock, stone, energy).
        public static string StepSurface = "grass";
        // Drum & bass at 174 BPM : one bar, and the 3 bars of a world's intro before its drop.
        public const double BAR = 240.0 / 174.0;
        public const int INTRO_BARS = 3;
        private const float HERO_DUCK = 0.708f;
        private const float HERO_DUCK_TIME = 0.5f;
        private readonly Dictionary<string, int> _takeCount = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();
        private readonly HashSet<string> _hero = new HashSet<string>();
        private readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        private readonly Dictionary<string, int> _lastTake = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _climbTime = new Dictionary<string, float>();
        private float _heroDuck;
        private AudioSource _intro;

        public static void LoadVolumes()
        {
            MasterSfx = PlayerPrefs.GetFloat("hs_sfx", 0.9f);
            MusicMaster = PlayerPrefs.GetFloat("hs_music", 0.8f);
        }

        public static void SaveVolumes()
        {
            PlayerPrefs.SetFloat("hs_sfx", MasterSfx);
            PlayerPrefs.SetFloat("hs_music", MusicMaster);
            PlayerPrefs.Save();
        }

        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject lObject = new GameObject("AudioManager");
                    DontDestroyOnLoad(lObject);
                    _instance = lObject.AddComponent<AudioManager>();
                    _instance.Init();
                }
                return _instance;
            }
        }

        private void Init()
        {
            foreach (AudioClip lClip in Resources.LoadAll<AudioClip>("Audio"))
                _clips[lClip.name] = lClip;
            IndexClips();

            for (int i = 0; i < POOL_SIZE; i++)
            {
                AudioSource lSource = gameObject.AddComponent<AudioSource>();
                lSource.playOnAwake = false;
                _pool.Add(lSource);
            }

            GameObject lMusic = new GameObject("Music");
            lMusic.transform.SetParent(transform, false);
            _musicA = CreateMusicSource(lMusic);
            _musicB = CreateMusicSource(lMusic);
            for (int i = 0; i < _layers.Length; i++) _layers[i] = CreateMusicSource(lMusic);
            _intro = CreateMusicSource(lMusic);
            _intro.loop = false;
            _musicFilter = lMusic.AddComponent<AudioLowPassFilter>();
            _musicFilter.cutoffFrequency = 22000f;
        }

        // Numbered takes (sfx_jump_1..4 -> sfx_jump : 4), and from the manifest the cooldowns and the hero sounds (they
        // pull the music down 3 dB for half a second).
        private void IndexClips()
        {
            foreach (string lName in _clips.Keys)
            {
                int lCut = lName.LastIndexOf('_');
                int lTake;
                if (lCut <= 0 || !int.TryParse(lName.Substring(lCut + 1), out lTake)) continue;
                string lBase = lName.Substring(0, lCut);
                int lCount;
                _takeCount[lBase] = _takeCount.TryGetValue(lBase, out lCount) ? Mathf.Max(lCount, lTake) : lTake;
            }
            TextAsset lManifest = Resources.Load<TextAsset>("audio_manifest");
            if (lManifest == null) return;
            // Keys are written sorted : category and cooldown_ms come before any nested object of an entry.
            foreach (Match lMatch in Regex.Matches(lManifest.text, "\"([a-z0-9_]+)\"\\s*:\\s*\\{[^{}]*?\"category\"\\s*:\\s*\"([a-z_]+)\"[^{}]*?\"cooldown_ms\"\\s*:\\s*(\\d+)"))
            {
                string lId = lMatch.Groups[1].Value;
                if (lMatch.Groups[2].Value == "hero") _hero.Add(lId);
                int lMs = int.Parse(lMatch.Groups[3].Value);
                if (lMs > 0) _cooldowns[lId] = lMs / 1000f;
            }
        }

        // The clip actually played for an id : its world variant, then one of its takes (rings and drift tiers climb
        // through theirs while they follow each other, the rest never play the same take twice in a row).
        private string Resolve(string pId, out string pFamily)
        {
            string lWorld = pId + "_w" + World;
            if (_clips.ContainsKey(lWorld) || _takeCount.ContainsKey(lWorld)) pId = lWorld;
            pFamily = pId;
            int lCount;
            if (!_takeCount.TryGetValue(pId, out lCount) || lCount < 1) return pId;
            int lTake;
            if (pId.StartsWith("sfx_ring") || pId.StartsWith("sfx_drift_tier"))
            {
                float lLast;
                bool lFollows = _climbTime.TryGetValue(pId, out lLast) && Time.unscaledTime - lLast < 2.5f;
                int lPrevious;
                lTake = lFollows && _lastTake.TryGetValue(pId, out lPrevious) ? Mathf.Min(lPrevious + 1, lCount) : 1;
                _climbTime[pId] = Time.unscaledTime;
            }
            else
            {
                int lPrevious;
                _lastTake.TryGetValue(pId, out lPrevious);
                lTake = Random.Range(1, lCount + 1);
                if (lCount > 1 && lTake == lPrevious) lTake = lTake % lCount + 1;
            }
            _lastTake[pId] = lTake;
            string lName = pId + "_" + lTake;
            return _clips.ContainsKey(lName) ? lName : pId;
        }

        private AudioSource CreateMusicSource(GameObject pHost)
        {
            AudioSource lSource = pHost.AddComponent<AudioSource>();
            lSource.loop = true;
            lSource.playOnAwake = false;
            lSource.volume = 0f;
            return lSource;
        }

        public static void Play(string pId, float pVolume = 1f, float pPitch = 1f, float pPitchVariance = 0.06f)
        {
            Instance.PlayInternal(pId, pVolume, pPitch, pPitchVariance);
        }

        private void PlayInternal(string pId, float pVolume, float pPitch, float pPitchVariance)
        {
            string lFamily;
            string lName = Resolve(pId, out lFamily);
            AudioClip lClip;
            if (!_clips.TryGetValue(lName, out lClip)) return;
            // The manifest's cooldown : the same sound never machine-guns.
            float lCooldown, lLast;
            if (_cooldowns.TryGetValue(lName, out lCooldown) && _lastPlayed.TryGetValue(lFamily, out lLast) && Time.unscaledTime - lLast < lCooldown) return;
            _lastPlayed[lFamily] = Time.unscaledTime;
            // Only a loud, close hero sound ducks the music : the storm's thunder rolls all the time, far away.
            if ((_hero.Contains(lName) || _hero.Contains(lFamily)) && pVolume >= 0.7f && !lFamily.StartsWith("sfx_thunder")) _heroDuck = HERO_DUCK_TIME;
            AudioSource lSource = _pool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _pool.Count;
            lSource.pitch = pPitch + Random.Range(-pPitchVariance, pPitchVariance);
            lSource.volume = pVolume * MasterSfx;
            lSource.clip = lClip;
            lSource.Play();
        }

        public static void SetLoop(string pId, float pVolume, float pPitch = 1f)
        {
            Instance.SetLoopInternal(pId, pVolume, pPitch);
        }

        private void SetLoopInternal(string pId, float pVolume, float pPitch)
        {
            AudioSource lSource;
            if (!_loops.TryGetValue(pId, out lSource))
            {
                AudioClip lClip;
                // A loop's world variant when it has one (sfx_light_loop_w3).
                if (!_clips.TryGetValue(pId + "_w" + World, out lClip) && !_clips.TryGetValue(pId, out lClip)) return;
                lSource = gameObject.AddComponent<AudioSource>();
                lSource.clip = lClip;
                lSource.loop = true;
                lSource.volume = 0f;
                lSource.Play();
                _loops[pId] = lSource;
            }
            _loopTargets[pId] = pVolume * MasterSfx;
            lSource.pitch = pPitch;
        }

        public static void StopAllLoops()
        {
            if (_instance == null) return;
            foreach (string lKey in new List<string>(_instance._loopTargets.Keys))
                _instance._loopTargets[lKey] = 0f;
        }

        // pBase, then pBase's hyper and light stems if they exist (pBase without its "_base" suffix + "_hyper", "_light").
        public static void PlayMusicLayers(string pPrefix, float pVolume = 0.55f) => PlayMusicLayersAt(pPrefix, pVolume, AudioSettings.dspTime + 0.08);

        // The world's 3-bar intro under the count, then its stems exactly on the drop (sample-synchronous). Returns the
        // dsp time of the drop : GO is shown then. -1 when the world has no intro.
        public static double PlayRaceIntro(string pPrefix, float pVolume)
        {
            AudioManager lManager = Instance;
            AudioClip lIntro;
            if (!lManager._clips.TryGetValue(pPrefix + "_intro", out lIntro)) return -1.0;
            double lStart = AudioSettings.dspTime + 0.12;
            double lDrop = lStart + INTRO_BARS * BAR;
            lManager._intro.Stop();
            lManager._intro.clip = lIntro;
            lManager._intro.volume = pVolume * MusicMaster;
            lManager._intro.PlayScheduled(lStart);
            PlayMusicLayersAt(pPrefix, pVolume, lDrop);
            return lDrop;
        }

        // Fades whatever music plays out (the fall onto the plaza is quiet : the intro comes with the count).
        public static void FadeOutMusic()
        {
            AudioManager lManager = Instance;
            lManager._musicVolume = 0f;
            lManager._currentMusic = null;
            lManager._layersOn = false;
            lManager._layerKey = null;
            lManager._intro.Stop();
        }

        // Footsteps : the stage's surface, run or sprint, one of its six takes.
        public static void PlayStep(bool pSprint, float pVolume)
        {
            string lId = "sfx_step_" + StepSurface + (pSprint ? "_sprint" : "_run");
            Play(Instance._takeCount.ContainsKey(lId) ? lId : "sfx_step", pVolume, 1f, 0.04f);
        }

        public static void PlayMusicLayersAt(string pPrefix, float pVolume, double pStart)
        {
            AudioManager lManager = Instance;
            if (lManager._layersOn && lManager._layerKey == pPrefix) return;
            AudioClip lBase, lHyper, lLight;
            if (!lManager._clips.TryGetValue(pPrefix + "_base", out lBase))
            {
                PlayMusic(pPrefix, pVolume);
                return;
            }
            lManager._clips.TryGetValue(pPrefix + "_hyper", out lHyper);
            lManager._clips.TryGetValue(pPrefix + "_light", out lLight);
            lManager._layerKey = pPrefix;
            lManager._layerVolume = pVolume;
            lManager._layersOn = true;
            lManager._currentMusic = null;
            // The plain music fades out under the stems.
            lManager._musicVolume = 0f;
            double lStart = pStart;
            AudioClip[] lClips = { lBase, lHyper, lLight };
            for (int i = 0; i < lManager._layers.Length; i++)
            {
                AudioSource lSource = lManager._layers[i];
                lSource.Stop();
                lSource.clip = lClips[i];
                lSource.volume = 0f;
                if (lClips[i] != null) lSource.PlayScheduled(lStart);
            }
            lManager._layerTargets[0] = 1f;
            lManager._layerTargets[1] = 0f;
            lManager._layerTargets[2] = 0f;
        }

        public static bool HasClip(string pId) => Instance._clips.ContainsKey(pId);

        // A stinger played once, then its loop scheduled to start exactly when it ends (falls back to looping the
        // stinger alone when there is no loop).
        public static void PlayMusicSequence(string pStinger, string pLoop, float pVolume = 0.55f)
        {
            AudioManager lManager = Instance;
            AudioClip lStinger, lLoop;
            if (!lManager._clips.TryGetValue(pStinger, out lStinger)) return;
            if (!lManager._clips.TryGetValue(pLoop, out lLoop))
            {
                PlayMusic(pStinger, pVolume);
                return;
            }
            PlayMusic(pStinger, pVolume);
            lManager._musicA.loop = false;
            double lEnd = AudioSettings.dspTime + 0.05 + lStinger.length;
            lManager._musicB.Stop();
            lManager._musicB.clip = lLoop;
            lManager._musicB.loop = true;
            lManager._musicB.volume = pVolume * MusicMaster;
            lManager._musicB.PlayScheduled(lEnd);
            lManager._sequenceSwap = lEnd;
            lManager._currentMusic = pLoop;
        }

        // When a scheduled loop takes over from its stinger, the two sources swap roles.
        private double _sequenceSwap = -1.0;

        // Decodes a set of stems (prefix_base / _hyper / _light) ahead of time, off the main thread : call it while the
        // screen is covered, then wait for MusicReady before GO (2026-11-12 : the stems used to decode at GO).
        public static void PrepareMusicLayers(string pPrefix)
        {
            AudioManager lManager = Instance;
            foreach (string lSuffix in LAYER_SUFFIXES)
            {
                AudioClip lClip;
                if (lManager._clips.TryGetValue(pPrefix + lSuffix, out lClip) && lClip.loadState == AudioDataLoadState.Unloaded) lClip.LoadAudioData();
            }
        }

        public static bool MusicLayersReady(string pPrefix)
        {
            AudioManager lManager = Instance;
            foreach (string lSuffix in LAYER_SUFFIXES)
            {
                AudioClip lClip;
                if (lManager._clips.TryGetValue(pPrefix + lSuffix, out lClip) && lClip.loadState == AudioDataLoadState.Loading) return false;
            }
            return true;
        }

        private static readonly string[] LAYER_SUFFIXES = { "_base", "_hyper", "_light", "_intro" };

        // 0..1 each : how much of the hyper and light stems plays over the base.
        public static void SetMusicIntensity(float pHyper, float pLight)
        {
            if (_instance == null) return;
            _instance._layerTargets[1] = Mathf.Clamp01(pHyper);
            _instance._layerTargets[2] = Mathf.Clamp01(pLight);
        }

        public static void PlayMusic(string pId, float pVolume = 0.55f)
        {
            AudioManager lManager = Instance;
            lManager._layersOn = false;
            lManager._layerKey = null;
            lManager._intro.Stop();
            if (lManager._currentMusic == pId) return;
            AudioClip lClip;
            if (!lManager._clips.TryGetValue(pId, out lClip)) return;
            lManager._currentMusic = pId;
            lManager._musicVolume = pVolume;
            AudioSource lTmp = lManager._musicA;
            lManager._musicA = lManager._musicB;
            lManager._musicB = lTmp;
            lManager._sequenceSwap = -1.0;
            lManager._musicA.loop = true;
            lManager._musicA.clip = lClip;
            lManager._musicA.volume = 0f;
            lManager._musicA.Play();
        }

        // Muffles the music during freeze frames and slow motion.
        public static void Duck(float pDuration)
        {
            Instance._duckTimer = Mathf.Max(Instance._duckTimer, pDuration);
        }

        public static void Unduck()
        {
            Instance._duckTimer = 0f;
        }

        // Positional looping source (rival engines), doppler included.
        public static AudioSource CreateLoop3D(string pId, Transform pParent, float pVolume)
        {
            AudioClip lClip;
            if (!Instance._clips.TryGetValue(pId, out lClip)) return null;
            AudioSource lSource = pParent.gameObject.AddComponent<AudioSource>();
            lSource.clip = lClip;
            lSource.loop = true;
            lSource.volume = pVolume * MasterSfx;
            lSource.spatialBlend = 1f;
            // No Doppler : at hyper and light speed it threw the engines' pitch all over the place (2026-11-12).
            lSource.dopplerLevel = 0f;
            lSource.rolloffMode = AudioRolloffMode.Linear;
            lSource.minDistance = 10f;
            lSource.maxDistance = 220f;
            lSource.Play();
            return lSource;
        }

        public static void SetMusicPitch(float pPitch)
        {
            if (_instance == null) return;
            _instance._musicA.pitch = pPitch;
            foreach (AudioSource lLayer in _instance._layers) lLayer.pitch = pPitch;
        }

        // One random take among numbered variations ("sfx_step" plays sfx_step_1 .. sfx_step_N when they exist).
        public static void PlayVaried(string pId, int pCount, float pVolume = 1f, float pPitch = 1f, float pPitchVariance = 0.05f)
        {
            Play(pId + "_" + Random.Range(1, pCount + 1), pVolume, pPitch, pPitchVariance);
        }

        private void Update()
        {
            float lDt = Time.unscaledDeltaTime;
            foreach (KeyValuePair<string, AudioSource> lPair in _loops)
            {
                float lTarget = _loopTargets[lPair.Key];
                lPair.Value.volume = Mathf.MoveTowards(lPair.Value.volume, lTarget, LOOP_FADE_SPEED * lDt);
            }

            _duckTimer -= lDt;
            float lDuck = _duckTimer > 0f ? 1f : 0f;
            // A hero sound (hyper, light, finish, explosions...) pulls the music 3 dB down for half a second.
            _heroDuck -= lDt;
            float lHero = _heroDuck > 0f ? HERO_DUCK : 1f;
            _musicFilter.cutoffFrequency = Mathf.Lerp(_musicFilter.cutoffFrequency, lDuck > 0f ? 900f : 22000f, 1f - Mathf.Exp(-(lDuck > 0f ? 30f : 6f) * lDt));
            // A stinger hands over to its scheduled loop : once the loop has started, it becomes the current music.
            if (_sequenceSwap > 0.0)
            {
                if (AudioSettings.dspTime >= _sequenceSwap)
                {
                    AudioSource lTmp = _musicA;
                    _musicA = _musicB;
                    _musicB = lTmp;
                    _musicB.Stop();
                    _sequenceSwap = -1.0;
                }
                else
                {
                    _musicA.volume = _musicVolume * MusicMaster;
                    _musicB.volume = _musicVolume * MusicMaster;
                    goto Layers;
                }
            }
            _musicA.volume = Mathf.MoveTowards(_musicA.volume, _musicVolume * MusicMaster * lHero * (lDuck > 0f ? 0.6f : 1f), MUSIC_FADE_SPEED * 2f * lDt);
            _musicB.volume = Mathf.MoveTowards(_musicB.volume, 0f, MUSIC_FADE_SPEED * lDt);
            if (_musicB.volume <= 0f && _musicB.isPlaying) _musicB.Stop();
            Layers:
            for (int i = 0; i < _layers.Length; i++)
            {
                float lTarget = _layersOn ? _layerTargets[i] * _layerVolume * MusicMaster * lHero * (lDuck > 0f ? 0.6f : 1f) : 0f;
                // The stems come in fast and leave slower : a kick of music on every surge of speed (the hero duck
                // goes down and back quickly).
                float lRate = lTarget > _layers[i].volume ? 2.5f : _heroDuck > 0f ? 2.5f : 0.8f;
                _layers[i].volume = Mathf.MoveTowards(_layers[i].volume, lTarget, lRate * lDt);
                if (!_layersOn && _layers[i].volume <= 0f && _layers[i].isPlaying) _layers[i].Stop();
            }
        }
    }
}
