using System.Collections;
using Com.IsartDigital.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Persistent scene wipe : one oversized slanted panel sweeps in from the right and leaves to the left, revealing
    // the next scene from the right. Everything that moves lives inside the panel mask (nothing can peek out).
    // Stage loads show a stage card (world, stage, rival portrait, objective) on the panel while loading.
    public class SceneTransition : MonoBehaviour
    {
        private const float WIDTH = 3400f;
        private const float HEIGHT = 1700f;
        private const float SKEW = 0.22f;

        private static SceneTransition _instance;

        private RectTransform _panel;
        private RectTransform _lines;
        private TextMeshProUGUI _bigWord;
        private RectTransform _card;
        private RawImage _cardImage;
        private TextMeshProUGUI _cardStage;
        private TextMeshProUGUI _cardTitle;
        private TextMeshProUGUI _cardTag;
        private RectTransform _loading;
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
            UIShape lPanel = UIKit.Tile(lCanvas.transform, "Panel", UIKit.INK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(WIDTH, HEIGHT), SKEW);
            lPanel.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _panel = lPanel.rectTransform;

            // Leading edge : red band + thin white line, inside the mask.
            UIKit.Tile(_panel, "EdgeRed", UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2(-WIDTH * 0.5f + 260f, 0f), new Vector2(420f, HEIGHT), SKEW);
            UIKit.Tile(_panel, "EdgeWhite", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-WIDTH * 0.5f + 500f, 0f), new Vector2(26f, HEIGHT), SKEW);
            _lines = UIFactory.CreateRect(_panel, "SpeedLines", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(WIDTH, HEIGHT));
            for (int i = 0; i < 26; i++)
            {
                float lY = Random.Range(-HEIGHT * 0.45f, HEIGHT * 0.45f);
                float lLength = Random.Range(200f, 900f);
                Color lColor = i % 5 == 0 ? new Color(UIKit.RED.r, UIKit.RED.g, UIKit.RED.b, 0.7f) : new Color(1f, 1f, 1f, Random.Range(0.05f, 0.2f));
                UIShape lLine = UIKit.Tile(_lines, "Line", lColor, new Vector2(0.5f, 0.5f), new Vector2(0f, lY), new Vector2(lLength, Random.Range(3f, 9f)), 0f);
                UIScrollLoop lLoop = lLine.gameObject.AddComponent<UIScrollLoop>();
                lLoop.speed = -Random.Range(2000f, 3400f);
                lLoop.wrap = WIDTH;
                lLoop.phase = Random.Range(0f, WIDTH);
            }

            _bigWord = UIKit.OutlineText(_panel, "", 360f, new Color(1f, 1f, 1f, 0.18f), new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(3000f, 420f), 0f, 0.05f);

            _card = UIFactory.CreateRect(_panel, "StageCard", new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1500f, 560f));
            UIShape lFrame = UIKit.Tile(_card, "Frame", UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(380f, 10f), new Vector2(760f, 470f), SKEW);
            UIShape lInner = UIKit.Tile(_card, "Inner", UIKit.RED_DEEP, new Vector2(0.5f, 0.5f), new Vector2(380f, 10f), new Vector2(720f, 430f), SKEW);
            _cardImage = UIKit.MaskedImage(lInner, null);
            _cardStage = UIKit.Text(_card, "", 40f, UIKit.RED, new Vector2(0.5f, 0.5f), new Vector2(-380f, 170f), new Vector2(720f, 50f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            _cardTitle = UIKit.Text(_card, "", 92f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-380f, 70f), new Vector2(760f, 120f), UIKit.Font.Heavy, TextAlignmentOptions.Left);
            _cardTitle.enableAutoSizing = true;
            _cardTitle.fontSizeMin = 40f;
            _cardTitle.fontSizeMax = 92f;
            _cardTag = UIKit.Text(_card, "", 30f, UIKit.WHITE, new Vector2(0.5f, 0.5f), new Vector2(-380f, -10f), new Vector2(720f, 44f), UIKit.Font.Bold, TextAlignmentOptions.Left);
            _loading = UIFactory.CreateRect(_card, "Loading", new Vector2(0.5f, 0.5f), new Vector2(-380f, -110f), new Vector2(720f, 30f));
            UIKit.Tile(_loading, "Track", new Color(1f, 1f, 1f, 0.15f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 14f), 0.6f);
            RectTransform lChevrons = UIFactory.CreateRect(_loading, "Chevrons", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 30f));
            for (int i = 0; i < 6; i++) UIFactory.CreateShape(lChevrons, "C", UIShape.ShapeKind.Chevron, UIKit.GOLD, new Vector2(0.5f, 0.5f), new Vector2(-300f + i * 120f, 0f), new Vector2(60f, 24f));
            UIScrollLoop lChevronLoop = lChevrons.gameObject.AddComponent<UIScrollLoop>();
            lChevronLoop.speed = 400f;
            lChevronLoop.wrap = 120f;
            SetX(WIDTH);
        }

        private void SetX(float pX)
        {
            _panel.anchoredPosition = new Vector2(pX, 0f);
            _bigWord.rectTransform.anchoredPosition = new Vector2(-pX * 0.35f, 230f);
        }

        public static void LoadScene(SceneName pScene, string pLabel = "", float pSpeed = 1f)
        {
            if (Instance._busy) return;
            Instance._card.gameObject.SetActive(false);
            Instance.StartCoroutine(Instance.LoadRoutine(EnumUtils.EnumToString(pScene), string.IsNullOrEmpty(pLabel) ? "HYPER SPEED" : pLabel, pSpeed, 0.15f));
        }

        // Stage card while loading : the next stage sweeps in from the right.
        public static void LoadStage(int pIndex, float pSpeed = 1f)
        {
            if (Instance._busy) return;
            LevelDefinition lLevel = LevelDatabase.Get(pIndex);
            Instance._card.gameObject.SetActive(true);
            Instance._cardImage.texture = RivalThumbnails.Get(pIndex);
            Instance._cardStage.text = LevelDatabase.WORLDS[lLevel.world] + "  //  STAGE " + (pIndex + 1).ToString("00");
            Instance._cardTitle.text = lLevel.title;
            Instance._cardTag.text = lLevel.tagline;
            Instance.StartCoroutine(Instance.LoadRoutine(EnumUtils.EnumToString(SceneName.CORE), "STAGE " + (pIndex + 1).ToString("00"), pSpeed, 0.8f));
        }

        private IEnumerator LoadRoutine(string pScene, string pWord, float pSpeed, float pHold)
        {
            _busy = true;
            _bigWord.text = pWord;
            AudioManager.Play("ui_transition_in", 0.9f);
            float lDuration = 0.42f / pSpeed;
            for (float lTime = 0f; lTime < lDuration; lTime += Time.unscaledDeltaTime)
            {
                SetX(Mathf.Lerp(WIDTH, 0f, UITween.Evaluate(Ease.OutExpo, lTime / lDuration)));
                yield return null;
            }
            SetX(0f);
            Time.timeScale = 1f;

            AsyncOperation lLoad = SceneManager.LoadSceneAsync(pScene);
            while (!lLoad.isDone) yield return null;
            yield return new WaitForSecondsRealtime(pHold / pSpeed);

            AudioManager.Play("ui_transition_out", 0.8f);
            for (float lTime = 0f; lTime < lDuration; lTime += Time.unscaledDeltaTime)
            {
                SetX(Mathf.Lerp(0f, -WIDTH, UITween.Evaluate(Ease.InCubic, lTime / lDuration)));
                yield return null;
            }
            SetX(WIDTH);
            _busy = false;
        }
    }

    // Scrolls a rect horizontally and wraps it around its start (speed lines, chevron rows).
    public class UIScrollLoop : MonoBehaviour
    {
        public float speed = 100f;
        public float wrap = 240f;
        public float phase;
        private float _offset;
        private Vector2 _base;
        private bool _init;

        private void Update()
        {
            RectTransform lRect = (RectTransform)transform;
            if (!_init) { _base = lRect.anchoredPosition; _offset = phase; _init = true; }
            _offset = Mathf.Repeat(_offset + speed * Time.unscaledDeltaTime, wrap);
            lRect.anchoredPosition = _base + new Vector2(_offset - wrap * 0.5f, 0f);
        }
    }
}
