using System.Collections;
using Com.IsartDigital.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Persistent scene wipe tied to speed : jagged streaks rush in from every edge to the centre until the screen is
    // covered, the stage card hits on the black while loading, then a hole bursts open from the centre outward onto the
    // next scene (the transition of build 600da29, back on 2026-11-12 : no twisting picture, no spinning mark).
    // Anything that must finish before the reveal (warm-up of sounds, shaders, the wide view) calls Hold / Release.
    public class SceneTransition : MonoBehaviour
    {
        private const float CLOSE_TIME = 0.26f;
        private const float OPEN_TIME = 0.34f;
        // A breathing bar shows only when loading takes longer than this past the card's own time.
        private const float LOADING_MARK_AFTER = 0.35f;
        // Never stays black longer than this waiting for a Hold.
        private const float HOLD_TIMEOUT = 6f;

        private static readonly int PROGRESS = Shader.PropertyToID("_Progress");
        private static readonly int OPENING = Shader.PropertyToID("_Opening");
        private static readonly int STREAK = Shader.PropertyToID("_Streak");

        private static SceneTransition _instance;
        private static int _holds;

        private RawImage _wipe;
        private Material _material;
        private RectTransform _card;
        private TextMeshProUGUI _number;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _tag;
        private Image _loading;
        private bool _busy;

        public static bool IsBusy => _instance != null && _instance._busy;

        // True while the screen is fully covered : the new scene can work unseen.
        public static bool IsCovered { get; private set; }

        // Keeps the screen black after the scene is loaded until every Hold is released (or HOLD_TIMEOUT).
        public static void Hold() => _holds++;

        public static void Release() => _holds = Mathf.Max(0, _holds - 1);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _holds = 0;
            IsCovered = false;
        }

        private static SceneTransition Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject lObject = new GameObject("SceneTransition");
                    DontDestroyOnLoad(lObject);
                    _instance = lObject.AddComponent<SceneTransition>();
                    _instance.Build();
                }
                return _instance;
            }
        }

        private void Build()
        {
            Canvas lCanvas = UIFactory.CreateCanvas("TransitionCanvas", 1000, transform);
            RectTransform lRect = UIFactory.Stretch(UIFactory.CreateRect(lCanvas.transform, "Wipe", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            _wipe = lRect.gameObject.AddComponent<RawImage>();
            _material = new Material(Shader.Find("Hyperspeed/Wipe"));
            _wipe.material = _material;
            _wipe.raycastTarget = false;
            _wipe.enabled = false;

            _card = UIFactory.CreateRect(lCanvas.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 420f));
            _number = UIKit.Text(_card, "", 170f, UIKit.GOLD, new Vector2(0f, 0.5f), new Vector2(110f, 40f), new Vector2(360f, 240f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            _title = UIKit.Text(_card, "", 112f, UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(850f, 40f), new Vector2(960f, 160f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            _title.enableAutoSizing = true;
            _title.fontSizeMin = 40f;
            _title.fontSizeMax = 112f;
            _tag = UIKit.Text(_card, "", 34f, new Color(1f, 1f, 1f, 0.75f), new Vector2(0f, 0.5f), new Vector2(850f, -60f), new Vector2(960f, 50f), UIKit.Font.Bold, TextAlignmentOptions.Left);
            _tag.enableAutoSizing = true;
            _tag.fontSizeMin = 18f;
            _tag.fontSizeMax = 34f;
            _card.gameObject.SetActive(false);

            // Loading : a thin bar under the card that breathes, nothing that turns.
            _loading = UIFactory.CreateImage(lCanvas.transform, "Loading", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(0f, -250f), new Vector2(420f, 6f));
            _loading.raycastTarget = false;
            _loading.gameObject.SetActive(false);
        }

        public static void LoadScene(SceneName pScene, float pSpeed = 1f)
        {
            SceneTransition lTransition = Instance;
            if (lTransition._busy) return;
            lTransition._card.gameObject.SetActive(false);
            lTransition.StartCoroutine(lTransition.LoadRoutine(EnumUtils.EnumToString(pScene), pSpeed, 0.05f, UIKit.WHITE));
        }

        public static void LoadStage(int pLevel)
        {
            SceneTransition lTransition = Instance;
            if (lTransition._busy) return;
            LevelDefinition lLevel = LevelDatabase.Get(pLevel);
            lTransition._number.text = LevelDatabase.Code(pLevel);
            lTransition._title.text = lLevel.title;
            lTransition._tag.text = lLevel.tagline;
            lTransition._card.gameObject.SetActive(true);
            Color lAccent = lLevel.theme().accent;
            lTransition.StartCoroutine(lTransition.LoadRoutine(EnumUtils.EnumToString(SceneName.CORE), 1f, 0.45f, lAccent));
        }

        private IEnumerator LoadRoutine(string pScene, float pSpeed, float pHold, Color pStreak)
        {
            _busy = true;
            _material.SetColor(STREAK, pStreak);
            _wipe.enabled = true;
            bool lShowCard = _card.gameObject.activeSelf;
            _card.gameObject.SetActive(false);
            HSJuice.Play(JuiceId.TransitionOut, Vector3.zero);

            // Close : the streaks rush in from the edges to the centre.
            _material.SetFloat(OPENING, 0f);
            for (float lTime = 0f; lTime < CLOSE_TIME / pSpeed; lTime += Time.unscaledDeltaTime)
            {
                float lT = lTime / (CLOSE_TIME / pSpeed);
                _material.SetFloat(PROGRESS, lT * lT);
                yield return null;
            }
            _material.SetFloat(PROGRESS, 1f);
            IsCovered = true;

            if (lShowCard)
            {
                _card.gameObject.SetActive(true);
                _card.localScale = Vector3.one * 1.5f;
            }
            float lCardTime = 0f;
            float lMinimum = pHold / pSpeed;
            AsyncOperation lLoad = SceneManager.LoadSceneAsync(pScene);
            lLoad.allowSceneActivation = false;
            while (lCardTime < lMinimum || lLoad.progress < 0.9f)
            {
                lCardTime += Time.unscaledDeltaTime;
                UpdateCover(lShowCard, lCardTime - lMinimum);
                yield return null;
            }
            lLoad.allowSceneActivation = true;
            while (!lLoad.isDone)
            {
                lCardTime += Time.unscaledDeltaTime;
                UpdateCover(lShowCard, lCardTime - lMinimum);
                yield return null;
            }
            // The new scene's warm-up (sounds decoded, shaders compiled, the wide view rendered once) runs unseen.
            float lHeld = 0f;
            yield return null;
            while (_holds > 0 && lHeld < HOLD_TIMEOUT)
            {
                lHeld += Time.unscaledDeltaTime;
                lCardTime += Time.unscaledDeltaTime;
                UpdateCover(lShowCard, lCardTime - lMinimum);
                yield return null;
            }
            if (_holds > 0) Debug.LogWarning("[HS-LOAD] reveal forced after " + HOLD_TIMEOUT + " s with " + _holds + " hold(s) left");
            _holds = 0;
            // Two settle frames : the first frames of a new scene are the heavy ones.
            yield return null;
            yield return null;

            _card.gameObject.SetActive(false);
            _loading.gameObject.SetActive(false);
            IsCovered = false;
            HSJuice.Play(JuiceId.TransitionIn, Vector3.zero);
            // Open : a jagged hole bursts out from the centre.
            _material.SetFloat(OPENING, 1f);
            for (float lTime = 0f; lTime < OPEN_TIME / pSpeed; lTime += Time.unscaledDeltaTime)
            {
                float lT = lTime / (OPEN_TIME / pSpeed);
                _material.SetFloat(PROGRESS, 1f - (1f - lT) * (1f - lT));
                yield return null;
            }
            _wipe.enabled = false;
            _busy = false;
        }

        private void UpdateCover(bool pShowCard, float pOverTime)
        {
            float lDt = Time.unscaledDeltaTime;
            if (pShowCard) _card.localScale = Vector3.Lerp(_card.localScale, Vector3.one, 1f - Mathf.Exp(-18f * lDt));
            bool lLong = pOverTime > LOADING_MARK_AFTER;
            if (_loading.gameObject.activeSelf != lLong) _loading.gameObject.SetActive(lLong);
            if (!lLong) return;
            float lPulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            _loading.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.15f, 0.7f, lPulse));
            _loading.rectTransform.localScale = new Vector3(Mathf.Lerp(0.6f, 1f, lPulse), 1f, 1f);
        }
    }
}
