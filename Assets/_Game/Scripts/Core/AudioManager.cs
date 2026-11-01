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

        public static void PlayMusic(string pId, float pVolume = 0.55f)
        {
            AudioManager lManager = Instance;
            if (lManager._currentMusic == pId) return;
            AudioClip lClip;
            if (!lManager._clips.TryGetValue(pId, out lClip)) return;
            lManager._currentMusic = pId;
            lManager._musicVolume = pVolume;
            AudioSource lTmp = lManager._musicA;
            lManager._musicA = lManager._musicB;
            lManager._musicB = lTmp;
            lManager._musicA.clip = lClip;
            lManager._musicA.volume = 0f;
            lManager._musicA.Play();
        }

        // Muffles the music during freeze frames and slow motion.
        public static void Duck(float pDuration)
        {
            Instance._duckTimer = Mathf.Max(Instance._duckTimer, pDuration);
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
            lSource.dopplerLevel = 1.6f;
            lSource.rolloffMode = AudioRolloffMode.Linear;
            lSource.minDistance = 10f;
            lSource.maxDistance = 220f;
            lSource.Play();
            return lSource;
        }

        public static void SetMusicPitch(float pPitch)
        {
            if (_instance != null) _instance._musicA.pitch = pPitch;
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
            _musicA.volume = Mathf.MoveTowards(_musicA.volume, _musicVolume * MusicMaster * (lDuck > 0f ? 0.6f : 1f), MUSIC_FADE_SPEED * 2f * lDt);
            _musicB.volume = Mathf.MoveTowards(_musicB.volume, 0f, MUSIC_FADE_SPEED * lDt);
            if (_musicB.volume <= 0f && _musicB.isPlaying) _musicB.Stop();
        }
    }
}
