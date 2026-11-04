using System.Collections;
using Com.IsartDigital.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Persistent scene wipe tied to speed : streaks close in from the edges until the screen is covered, the stage name
    // hits on it while loading, then the center opens onto the next scene.
    public class SceneTransition : MonoBehaviour
    {
        private const float CLOSE_TIME = 0.26f;
        private const float OPEN_TIME = 0.34f;

        private static readonly int PROGRESS = Shader.PropertyToID("_Progress");
        private static readonly int OPENING = Shader.PropertyToID("_Opening");
        private static readonly int STREAK = Shader.PropertyToID("_Streak");

        private static SceneTransition _instance;

        private RawImage _wipe;
        private Material _material;
        private RectTransform _card;
        private TextMeshProUGUI _number;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _tag;
        private bool _busy;

        public static bool IsBusy => _instance != null && _instance._busy;

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
            _number = UIKit.Text(_card, "", 170f, UIKit.GOLD, new Vector2(0f, 0.5f), new Vector2(160f, 40f), new Vector2(320f, 240f), UIKit.Font.Heavy, TextAlignmentOptions.Right);
            _title = UIKit.Text(_card, "", 112f, UIKit.WHITE, new Vector2(0f, 0.5f), new Vector2(820f, 40f), new Vector2(980f, 160f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            _title.enableAutoSizing = true;
            _title.fontSizeMin = 40f;
            _title.fontSizeMax = 112f;
            _tag = UIKit.Text(_card, "", 34f, new Color(1f, 1f, 1f, 0.75f), new Vector2(0f, 0.5f), new Vector2(820f, -60f), new Vector2(980f, 50f), UIKit.Font.Bold, TextAlignmentOptions.Left);
            _tag.enableAutoSizing = true;
            _tag.fontSizeMin = 18f;
            _tag.fontSizeMax = 34f;
            _card.gameObject.SetActive(false);
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
            lTransition._number.text = (pLevel + 1).ToString("00");
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
            AudioManager.Play("ui_whoosh", 0.9f);

            _material.SetFloat(OPENING, 0f);
            for (float lTime = 0f; lTime < CLOSE_TIME / pSpeed; lTime += Time.unscaledDeltaTime)
            {
                float lT = lTime / (CLOSE_TIME / pSpeed);
                _material.SetFloat(PROGRESS, lT * lT);
                yield return null;
            }
            _material.SetFloat(PROGRESS, 1f);

            if (lShowCard)
            {
                _card.gameObject.SetActive(true);
                _card.localScale = Vector3.one * 1.5f;
            }
            float lCardTime = 0f;
            AsyncOperation lLoad = SceneManager.LoadSceneAsync(pScene);
            lLoad.allowSceneActivation = false;
            while (lCardTime < pHold / pSpeed || lLoad.progress < 0.9f)
            {
                lCardTime += Time.unscaledDeltaTime;
                if (lShowCard) _card.localScale = Vector3.Lerp(_card.localScale, Vector3.one, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                yield return null;
            }
            lLoad.allowSceneActivation = true;
            yield return lLoad;
            yield return null;

            _card.gameObject.SetActive(false);
            AudioManager.Play("ui_whoosh", 0.7f, 1.3f);
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
    }
}
