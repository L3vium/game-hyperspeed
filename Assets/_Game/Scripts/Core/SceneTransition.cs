using System.Collections;
using Com.IsartDigital.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Com.IsartDigital.Hyperspeed
{
    // Persistent slanted-shard wipe between scenes.
    public class SceneTransition : MonoBehaviour
    {
        private const int BAR_COUNT = 7;
        private const float BAR_HEIGHT = 260f;

        private static SceneTransition _instance;

        private RectTransform[] _bars;
        private TextMeshProUGUI _label;
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
            _bars = new RectTransform[BAR_COUNT];
            for (int i = 0; i < BAR_COUNT; i++)
            {
                Color lColor = i % 2 == 0 ? UIFactory.BLACK : (i % 3 == 0 ? UIFactory.WHITE : UIFactory.RED);
                UIShape lShape = UIFactory.CreateShape(lCanvas.transform, "Bar" + i, UIShape.ShapeKind.Parallelogram, lColor,
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3400f, BAR_HEIGHT + 30f), -12f);
                lShape.skew = 0.6f;
                _bars[i] = lShape.rectTransform;
            }
            _label = UIFactory.CreateText(lCanvas.transform, "", 120f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 300f), true);
            _label.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            SetProgress(0f, true);
        }

        // 0 = hidden on the right, 1 = covering, 2 = hidden on the left.
        private void SetProgress(float pValue, bool pEntering)
        {
            for (int i = 0; i < BAR_COUNT; i++)
            {
                float lDelay = i * 0.06f;
                float lLocal = Mathf.Clamp01((pValue - lDelay) / (1f - 0.06f * (BAR_COUNT - 1)));
                float lEase = 1f - Mathf.Pow(1f - lLocal, 3f);
                float lX = pEntering ? Mathf.Lerp(3600f, 0f, lEase) : Mathf.Lerp(0f, -3600f, lEase);
                float lY = (i - (BAR_COUNT - 1) * 0.5f) * BAR_HEIGHT;
                _bars[i].anchoredPosition = new Vector2(lX + lY * 0.21f, lY);
            }
            _label.gameObject.SetActive(pEntering ? pValue >= 1f : pValue <= 0.15f);
        }

        public static void LoadScene(SceneName pScene, string pLabel = "", float pSpeed = 1f)
        {
            if (Instance._busy) return;
            Instance.StartCoroutine(Instance.LoadRoutine(EnumUtils.EnumToString(pScene), pLabel, pSpeed));
        }

        private IEnumerator LoadRoutine(string pScene, string pLabel, float pSpeed)
        {
            _busy = true;
            _label.text = pLabel;
            AudioManager.Play("ui_whoosh", 0.8f);
            float lDuration = 0.45f / pSpeed;
            for (float lTime = 0f; lTime < lDuration; lTime += Time.unscaledDeltaTime)
            {
                SetProgress(lTime / lDuration, true);
                yield return null;
            }
            SetProgress(1f, true);
            Time.timeScale = 1f;

            AsyncOperation lLoad = SceneManager.LoadSceneAsync(pScene);
            while (!lLoad.isDone) yield return null;
            yield return null;
            if (!string.IsNullOrEmpty(pLabel)) yield return new WaitForSecondsRealtime(0.25f / pSpeed);

            AudioManager.Play("ui_whoosh", 0.6f, 1.2f);
            for (float lTime = 0f; lTime < lDuration; lTime += Time.unscaledDeltaTime)
            {
                SetProgress(lTime / lDuration, false);
                yield return null;
            }
            SetProgress(1f, false);
            _busy = false;
        }
    }
}
