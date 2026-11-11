using System.Collections;
using Com.IsartDigital.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Scene transition (2026-11-11 : "a real in and out") :
    //   out  : an inverted ink impact frame of the current picture, then the whole picture is sucked into the centre of
    //          the screen with a twist and converging streaks, down to a fully black screen (nothing lingers) ;
    //   hold : black, the stage card for a race, and a small loading mark that only appears if loading takes a while ;
    //   in   : an inverted ink flash, then the next scene bursts open from the centre, streaks flying outward.
    public class SceneTransition : MonoBehaviour
    {
        private const float INK_TIME = 0.07f;
        private const float CLOSE_TIME = 0.42f;
        private const float OPEN_TIME = 0.45f;
        // The loading mark waits this long before showing : a quick load never shows it.
        private const float LOADER_DELAY = 0.35f;

        private static readonly int PROGRESS = Shader.PropertyToID("_Progress");
        private static readonly int MODE = Shader.PropertyToID("_Mode");
        private static readonly int INK = Shader.PropertyToID("_Ink");
        private static readonly int STREAK = Shader.PropertyToID("_Streak");

        private static SceneTransition _instance;

        private RawImage _wipe;
        private Material _material;
        private RectTransform _card;
        private TextMeshProUGUI _number;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _tag;
        private RectTransform _loader;
        private CanvasGroup _loaderGroup;
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

            // Loading mark : two chevrons turning, bottom right.
            _loader = UIFactory.CreateRect(lCanvas.transform, "Loading", new Vector2(1f, 0f), new Vector2(-120f, 110f), new Vector2(90f, 90f));
            _loaderGroup = _loader.gameObject.AddComponent<CanvasGroup>();
            for (int i = 0; i < 2; i++)
            {
                UIShape lChevron = UIFactory.CreateShape(_loader, "Chevron" + i, UIShape.ShapeKind.Chevron, i == 0 ? UIKit.RED : UIKit.WHITE, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 46f), i * 180f);
                lChevron.skew = 0.4f;
                lChevron.rectTransform.anchoredPosition = new Vector2(0f, i == 0 ? 18f : -18f);
            }
            _loader.gameObject.AddComponent<UISpin>().speed = -360f;
            _loader.gameObject.SetActive(false);
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
            bool lShowCard = _card.gameObject.activeSelf;
            _card.gameObject.SetActive(false);
            _material.SetColor(STREAK, pStreak);

            // ---------------- out : the current picture, inked, then sucked into the centre
            yield return new WaitForEndOfFrame();
            Texture2D lPicture = ScreenCapture.CaptureScreenshotAsTexture();
            _wipe.texture = lPicture;
            _wipe.enabled = true;
            _material.SetFloat(MODE, 0f);
            _material.SetFloat(PROGRESS, 0f);
            _material.SetFloat(INK, 1f);
            HSJuice.Play(JuiceId.TransitionOut, Vector3.zero);
            for (float lTime = 0f; lTime < INK_TIME; lTime += Time.unscaledDeltaTime) yield return null;
            _material.SetFloat(INK, 0f);
            float lClose = CLOSE_TIME / pSpeed;
            for (float lTime = 0f; lTime < lClose; lTime += Time.unscaledDeltaTime)
            {
                float lT = lTime / lClose;
                // Slow at first, then everything rushes into the centre.
                _material.SetFloat(PROGRESS, lT * lT * (3f - 2f * lT) * 0.35f + lT * lT * 0.65f);
                yield return null;
            }
            _material.SetFloat(PROGRESS, 1f);
            _material.SetFloat(MODE, 1f);
            _wipe.texture = null;
            Destroy(lPicture);

            // ---------------- hold : black, the stage card, a loading mark only if it takes long
            if (lShowCard)
            {
                _card.gameObject.SetActive(true);
                _card.localScale = Vector3.one * 1.5f;
            }
            float lHoldTime = 0f;
            AsyncOperation lLoad = SceneManager.LoadSceneAsync(pScene);
            lLoad.allowSceneActivation = false;
            while (lHoldTime < pHold / pSpeed || lLoad.progress < 0.9f)
            {
                lHoldTime += Time.unscaledDeltaTime;
                if (lShowCard) _card.localScale = Vector3.Lerp(_card.localScale, Vector3.one, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                ShowLoader(lHoldTime);
                yield return null;
            }
            lLoad.allowSceneActivation = true;
            while (!lLoad.isDone)
            {
                lHoldTime += Time.unscaledDeltaTime;
                ShowLoader(lHoldTime);
                yield return null;
            }
            // Two frames for the new scene to settle : the loading hitch never eats the opening.
            yield return null;
            yield return null;
            _loader.gameObject.SetActive(false);
            _card.gameObject.SetActive(false);

            // ---------------- in : an inverted ink flash, then the scene bursts open from the centre
            HSJuice.Play(JuiceId.TransitionIn, Vector3.zero);
            _material.SetFloat(MODE, 3f);
            for (float lTime = 0f; lTime < INK_TIME; lTime += Time.unscaledDeltaTime) yield return null;
            _material.SetFloat(MODE, 2f);
            float lOpen = OPEN_TIME / pSpeed;
            for (float lTime = 0f; lTime < lOpen; lTime += Time.unscaledDeltaTime)
            {
                float lT = lTime / lOpen;
                _material.SetFloat(PROGRESS, 1f - (1f - lT) * (1f - lT) * (1f - lT));
                yield return null;
            }
            _wipe.enabled = false;
            _busy = false;
        }

        private void ShowLoader(float pTime)
        {
            bool lShow = pTime > LOADER_DELAY;
            if (_loader.gameObject.activeSelf != lShow) _loader.gameObject.SetActive(lShow);
            if (lShow) _loaderGroup.alpha = Mathf.Clamp01((pTime - LOADER_DELAY) / 0.2f);
        }
    }
}
