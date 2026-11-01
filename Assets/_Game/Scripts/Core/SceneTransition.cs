using System.Collections;
using Com.IsartDigital.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Persistent scene wipe : a slanted red chevron panel sweeps across, a giant outlined word slides through,
    // and for stages a VS card (rival portrait + name) is shown while loading.
    public class SceneTransition : MonoBehaviour
    {
        private static SceneTransition _instance;

        private RectTransform _panel;
        private RectTransform _slash;
        private RectTransform _checker;
        private TextMeshProUGUI _bigWord;
        private RectTransform _card;
        private RawImage _cardImage;
        private TextMeshProUGUI _cardStage;
        private TextMeshProUGUI _cardTitle;
        private TextMeshProUGUI _cardTag;
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
            Transform lRoot = lCanvas.transform;

            UIShape lSlash = UIKit.Tile(lRoot, "Slash", UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2600f, 1500f), 0.35f);
            _slash = lSlash.rectTransform;
            UIShape lPanel = UIKit.Tile(lRoot, "Panel", UIKit.RED, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2500f, 1500f), 0.35f);
            UIKit.Chevrons(lPanel.transform, "Chevrons", UIKit.RED, new Color(0.78f, 0.03f, 0.08f), 220f);
            lPanel.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _panel = lPanel.rectTransform;
            _bigWord = UIKit.OutlineText(lPanel.transform, "", 380f, new Color(1f, 1f, 1f, 0.75f), new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(3000f, 420f), 0f, 0.06f);
            _checker = UIKit.Checker(lPanel.transform, "Checker", new Vector2(0.5f, 0.5f), new Vector2(0f, -470f), new Vector2(2600f, 60f), UIKit.WHITE, UIKit.RED_DARK, 200f).rectTransform;

            _card = UIFactory.CreateRect(lPanel.transform, "VsCard", new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1400f, 420f));
            UIShape lFrame = UIFactory.CreateShape(_card, "Frame", UIShape.ShapeKind.Chevron, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-260f, 0f), new Vector2(700f, 400f));
            lFrame.skew = 0.42f;
            UIShape lInner = UIFactory.CreateShape(_card, "Inner", UIShape.ShapeKind.Chevron, UIKit.RED_DEEP, new Vector2(0.5f, 0.5f), new Vector2(-260f, 0f), new Vector2(660f, 360f));
            lInner.skew = 0.42f;
            _cardImage = UIKit.MaskedImage(lInner, null);
            UIShape lBand = UIKit.Tile(_card, "Band", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(380f, -60f), new Vector2(760f, 160f), 0.3f);
            _cardStage = UIKit.Text(_card, "", 44f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(360f, 90f), new Vector2(700f, 60f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            _cardTitle = UIKit.Text(lBand.transform, "", 64f, UIKit.INK, new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(700f, 80f), UIKit.Font.Thin, TextAlignmentOptions.Left);
            _cardTag = UIKit.Text(lBand.transform, "", 26f, UIKit.RED_DARK, new Vector2(0.5f, 0.5f), new Vector2(0f, -44f), new Vector2(700f, 40f), UIKit.Font.Bold, TextAlignmentOptions.Left);
            SetProgress(0f, true);
        }

        // 0 = off screen right, 1 = covering ; leaving goes to the left.
        private void SetProgress(float pValue, bool pEntering)
        {
            float lEase = pEntering ? 1f - Mathf.Pow(1f - pValue, 3f) : pValue * pValue;
            float lX = pEntering ? Mathf.Lerp(2800f, 0f, lEase) : Mathf.Lerp(0f, -2900f, lEase);
            _panel.anchoredPosition = new Vector2(lX, 0f);
            _slash.anchoredPosition = new Vector2(lX + (pEntering ? 160f : -160f) * (1f - lEase), 0f);
            _bigWord.rectTransform.anchoredPosition = new Vector2(pEntering ? 600f * (1f - lEase) : -600f * lEase, 180f);
        }

        public static void LoadScene(SceneName pScene, string pLabel = "", float pSpeed = 1f)
        {
            if (Instance._busy) return;
            Instance._card.gameObject.SetActive(false);
            Instance.StartCoroutine(Instance.LoadRoutine(EnumUtils.EnumToString(pScene), string.IsNullOrEmpty(pLabel) ? "HYPER SPEED" : pLabel, pSpeed));
        }

        public static void LoadStage(int pIndex, float pSpeed = 1f)
        {
            if (Instance._busy) return;
            LevelDefinition lLevel = LevelDatabase.Get(pIndex);
            Instance._card.gameObject.SetActive(true);
            Instance._cardImage.texture = RivalThumbnails.Get(pIndex);
            Instance._cardStage.text = LevelDatabase.WORLDS[lLevel.world] + "  //  STAGE " + (pIndex + 1).ToString("00");
            Instance._cardTitle.text = lLevel.title;
            Instance._cardTag.text = lLevel.tagline;
            Instance.StartCoroutine(Instance.LoadRoutine(EnumUtils.EnumToString(SceneName.CORE), "VS", pSpeed));
        }

        private IEnumerator LoadRoutine(string pScene, string pWord, float pSpeed)
        {
            _busy = true;
            _bigWord.text = pWord;
            AudioManager.Play("ui_transition_in", 0.9f);
            float lDuration = 0.45f / pSpeed;
            for (float lTime = 0f; lTime < lDuration; lTime += Time.unscaledDeltaTime)
            {
                SetProgress(lTime / lDuration, true);
                yield return null;
            }
            SetProgress(1f, true);
            Time.timeScale = 1f;
            if (_card.gameObject.activeSelf) AudioManager.Play("sfx_vs", 0.9f);

            AsyncOperation lLoad = SceneManager.LoadSceneAsync(pScene);
            while (!lLoad.isDone) yield return null;
            yield return new WaitForSecondsRealtime(_card.gameObject.activeSelf ? 0.7f / pSpeed : 0.15f);

            AudioManager.Play("ui_transition_out", 0.8f);
            for (float lTime = 0f; lTime < lDuration; lTime += Time.unscaledDeltaTime)
            {
                SetProgress(lTime / lDuration, false);
                yield return null;
            }
            SetProgress(1f, false);
            SetProgress(0f, true);
            _busy = false;
        }
    }
}
