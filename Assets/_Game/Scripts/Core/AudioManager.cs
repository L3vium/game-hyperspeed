using System.Collections.Generic;
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
            _musicFilter = lMusic.AddComponent<AudioLowPassFilter>();
            _musicFilter.cutoffFrequency = 22000f;
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
            AudioClip lClip;
            if (!_clips.TryGetValue(pId, out lClip)) return;
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
                if (!_clips.TryGetValue(pId, out lClip)) return;
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
        public static void PlayMusicLayers(string pPrefix, float pVolume = 0.55f)
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
            double lStart = AudioSettings.dspTime + 0.08;
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

        private static readonly string[] LAYER_SUFFIXES = { "_base", "_hyper", "_light" };

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
            _musicA.volume = Mathf.MoveTowards(_musicA.volume, _musicVolume * MusicMaster * (lDuck > 0f ? 0.6f : 1f), MUSIC_FADE_SPEED * 2f * lDt);
            _musicB.volume = Mathf.MoveTowards(_musicB.volume, 0f, MUSIC_FADE_SPEED * lDt);
            if (_musicB.volume <= 0f && _musicB.isPlaying) _musicB.Stop();
            Layers:
            for (int i = 0; i < _layers.Length; i++)
            {
                float lTarget = _layersOn ? _layerTargets[i] * _layerVolume * MusicMaster * (lDuck > 0f ? 0.6f : 1f) : 0f;
                // The stems come in fast and leave slower : a kick of music on every surge of speed.
                float lRate = lTarget > _layers[i].volume ? 2.5f : 0.8f;
                _layers[i].volume = Mathf.MoveTowards(_layers[i].volume, lTarget, lRate * lDt);
                if (!_layersOn && _layers[i].volume <= 0f && _layers[i].isPlaying) _layers[i].Stop();
            }
        }
    }
}
