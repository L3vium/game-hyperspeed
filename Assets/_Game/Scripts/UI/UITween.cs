using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    public enum Ease { Linear, OutCubic, OutQuint, OutBack, OutExpo, OutElastic, InCubic, InOutSine, InOutCubic }

    // Tiny tween runner for the code-built UI (unscaled time by default) : move, scale, rotate, fade, color, value.
    public class UITween : MonoBehaviour
    {
        private class Tween
        {
            public object target;
            public float delay;
            public float duration;
            public float time;
            public Ease ease;
            public bool unscaled;
            public Action<float> step;
            public Action complete;
        }

        private static UITween _runner;
        private readonly List<Tween> _tweens = new List<Tween>();
        private readonly List<Tween> _adding = new List<Tween>();

        private static UITween Runner
        {
            get
            {
                if (_runner == null) _runner = new GameObject("UITween").AddComponent<UITween>();
                return _runner;
            }
        }

        public static float Evaluate(Ease pEase, float pT)
        {
            pT = Mathf.Clamp01(pT);
            switch (pEase)
            {
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - pT, 3f);
                case Ease.OutQuint: return 1f - Mathf.Pow(1f - pT, 5f);
                case Ease.OutBack:
                    const float C1 = 1.70158f;
                    const float C3 = C1 + 1f;
                    return 1f + C3 * Mathf.Pow(pT - 1f, 3f) + C1 * Mathf.Pow(pT - 1f, 2f);
                case Ease.OutExpo: return pT >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * pT);
                case Ease.OutElastic:
                    if (pT <= 0f || pT >= 1f) return pT;
                    return Mathf.Pow(2f, -10f * pT) * Mathf.Sin((pT * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
                case Ease.InCubic: return pT * pT * pT;
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * pT) - 1f) * 0.5f;
                case Ease.InOutCubic: return pT < 0.5f ? 4f * pT * pT * pT : 1f - Mathf.Pow(-2f * pT + 2f, 3f) * 0.5f;
            }
            return pT;
        }

        public static void Value(object pTarget, float pDuration, Ease pEase, Action<float> pStep, float pDelay = 0f, Action pComplete = null, bool pUnscaled = true)
        {
            Runner._adding.Add(new Tween { target = pTarget, duration = Mathf.Max(0.0001f, pDuration), ease = pEase, step = pStep, delay = pDelay, complete = pComplete, unscaled = pUnscaled });
        }

        public static void Move(RectTransform pRect, Vector2 pTo, float pDuration, Ease pEase, float pDelay = 0f, Action pComplete = null)
        {
            Vector2 lFrom = pRect.anchoredPosition;
            bool lStarted = false;
            Value(pRect, pDuration, pEase, pT =>
            {
                if (!lStarted) { lFrom = pRect.anchoredPosition; lStarted = true; }
                pRect.anchoredPosition = Vector2.LerpUnclamped(lFrom, pTo, pT);
            }, pDelay, pComplete);
        }

        public static void MoveFrom(RectTransform pRect, Vector2 pFrom, float pDuration, Ease pEase, float pDelay = 0f)
        {
            Vector2 lTo = pRect.anchoredPosition;
            pRect.anchoredPosition = pFrom;
            Value(pRect, pDuration, pEase, pT => pRect.anchoredPosition = Vector2.LerpUnclamped(pFrom, lTo, pT), pDelay);
        }

        public static void Scale(Transform pTransform, Vector3 pTo, float pDuration, Ease pEase, float pDelay = 0f, Action pComplete = null)
        {
            Vector3 lFrom = pTransform.localScale;
            bool lStarted = false;
            Value(pTransform, pDuration, pEase, pT =>
            {
                if (!lStarted) { lFrom = pTransform.localScale; lStarted = true; }
                pTransform.localScale = Vector3.LerpUnclamped(lFrom, pTo, pT);
            }, pDelay, pComplete);
        }

        public static void Punch(Transform pTransform, float pAmount, float pDuration = 0.35f)
        {
            Vector3 lBase = Vector3.one;
            Value(pTransform, pDuration, Ease.Linear, pT => pTransform.localScale = lBase * (1f + pAmount * Mathf.Sin(pT * Mathf.PI) * (1f - pT)));
        }

        public static void Rotate(Transform pTransform, float pToZ, float pDuration, Ease pEase, float pDelay = 0f)
        {
            float lFrom = pTransform.localEulerAngles.z;
            Value(pTransform, pDuration, pEase, pT => pTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(lFrom, pToZ, pT)), pDelay);
        }

        public static void Fade(CanvasGroup pGroup, float pTo, float pDuration, Ease pEase = Ease.OutCubic, float pDelay = 0f, Action pComplete = null)
        {
            float lFrom = pGroup.alpha;
            bool lStarted = false;
            Value(pGroup, pDuration, pEase, pT =>
            {
                if (!lStarted) { lFrom = pGroup.alpha; lStarted = true; }
                pGroup.alpha = Mathf.LerpUnclamped(lFrom, pTo, pT);
            }, pDelay, pComplete);
        }

        public static void Tint(Graphic pGraphic, Color pTo, float pDuration, Ease pEase = Ease.OutCubic, float pDelay = 0f)
        {
            Color lFrom = pGraphic.color;
            Value(pGraphic, pDuration, pEase, pT => pGraphic.color = Color.LerpUnclamped(lFrom, pTo, pT), pDelay);
        }

        public static void Kill(object pTarget)
        {
            if (_runner == null) return;
            _runner._tweens.RemoveAll(pTween => pTween.target == pTarget);
            _runner._adding.RemoveAll(pTween => pTween.target == pTarget);
        }

        private void Update()
        {
            if (_adding.Count > 0)
            {
                _tweens.AddRange(_adding);
                _adding.Clear();
            }
            for (int i = _tweens.Count - 1; i >= 0; i--)
            {
                Tween lTween = _tweens[i];
                if (lTween.target is UnityEngine.Object && (UnityEngine.Object)lTween.target == null)
                {
                    _tweens.RemoveAt(i);
                    continue;
                }
                float lDt = lTween.unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                if (lTween.delay > 0f)
                {
                    lTween.delay -= lDt;
                    continue;
                }
                lTween.time += lDt;
                float lT = Mathf.Clamp01(lTween.time / lTween.duration);
                lTween.step(Evaluate(lTween.ease, lT));
                if (lT >= 1f)
                {
                    _tweens.RemoveAt(i);
                    lTween.complete?.Invoke();
                }
            }
        }

        private void OnDestroy()
        {
            if (_runner == this) _runner = null;
        }
    }
}
