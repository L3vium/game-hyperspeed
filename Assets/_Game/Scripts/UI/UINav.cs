using System;
using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Spatial menu navigation (keyboard / gamepad / mouse) over arbitrary tiles laid out on screen.
    public class UINav : MonoBehaviour
    {
        public class Item
        {
            public RectTransform rect;
            public Vector2 position;
            public Action onSubmit;
            public Action<bool> onSelect;
            public Action<int> onAdjust;
            public bool enabled = true;
        }

        private readonly List<Item> _items = new List<Item>();
        private int _selected = -1;
        private int _enabledFrame;
        private Vector2 _lastMouse;

        public Action onBack;
        public bool active = true;
        public int Selected => _selected;

        public static UINav Create(GameObject pHost)
        {
            return pHost.AddComponent<UINav>();
        }

        public Item Add(RectTransform pRect, Vector2 pNavPosition, Action pSubmit, Action<bool> pSelect = null, Action<int> pAdjust = null)
        {
            Item lItem = new Item { rect = pRect, position = pNavPosition, onSubmit = pSubmit, onSelect = pSelect, onAdjust = pAdjust };
            _items.Add(lItem);
            pSelect?.Invoke(false);
            return lItem;
        }

        public void Select(int pIndex, bool pSound = false)
        {
            if (pIndex < 0 || pIndex >= _items.Count) return;
            if (_selected >= 0 && _selected < _items.Count) _items[_selected].onSelect?.Invoke(false);
            _selected = pIndex;
            _items[_selected].onSelect?.Invoke(true);
            if (pSound) AudioManager.Play("ui_move", 0.6f);
        }

        private void OnEnable()
        {
            _enabledFrame = Time.frameCount;
        }

        private void Update()
        {
            if (!active || _items.Count == 0 || Time.frameCount <= _enabledFrame + 1 || SceneTransition.IsBusy) return;
            if (_selected < 0) Select(0);

            Vector2Int lMove = HSInput.MenuMove;
            Item lCurrent = _items[_selected];
            if (lMove != Vector2Int.zero)
            {
                if (lCurrent.onAdjust != null && lMove.x != 0) lCurrent.onAdjust(lMove.x);
                else
                {
                    int lBest = FindNeighbour(lMove);
                    if (lBest >= 0) Select(lBest, true);
                }
            }

            Vector2 lMouse = HSInput.MousePosition;
            bool lMoved = (lMouse - _lastMouse).sqrMagnitude > 4f;
            _lastMouse = lMouse;
            for (int i = 0; i < _items.Count; i++)
            {
                if (!_items[i].enabled || !_items[i].rect.gameObject.activeInHierarchy) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(_items[i].rect, lMouse, null)) continue;
                if (lMoved && i != _selected) Select(i, true);
                if (HSInput.MouseClicked)
                {
                    Select(i);
                    Submit();
                    return;
                }
            }

            if (HSInput.SubmitPressed) Submit();
            else if (HSInput.BackPressed && onBack != null)
            {
                AudioManager.Play("ui_back", 0.7f);
                onBack();
            }
        }

        private int FindNeighbour(Vector2Int pDir)
        {
            Vector2 lFrom = _items[_selected].position;
            Vector2 lDir = new Vector2(pDir.x, pDir.y);
            int lBest = -1;
            float lBestScore = float.MaxValue;
            for (int i = 0; i < _items.Count; i++)
            {
                if (i == _selected || !_items[i].enabled) continue;
                Vector2 lDelta = _items[i].position - lFrom;
                float lAlong = Vector2.Dot(lDelta, lDir);
                if (lAlong <= 0.01f) continue;
                float lAcross = Mathf.Abs(Vector2.Dot(lDelta, new Vector2(-lDir.y, lDir.x)));
                float lScore = lAlong + lAcross * 2.5f;
                if (lScore < lBestScore)
                {
                    lBestScore = lScore;
                    lBest = i;
                }
            }
            return lBest;
        }

        private void Submit()
        {
            Item lItem = _items[_selected];
            if (!lItem.enabled) return;
            AudioManager.Play("ui_select", 0.9f);
            lItem.onSubmit?.Invoke();
        }
    }

    // Standard highlight for a slanted tile : pop, tilt, white frame, color swap.
    public class NavTile : MonoBehaviour
    {
        public UIShape back;
        public UIShape frame;
        public TMPro.TextMeshProUGUI label;
        public Color normalBack;
        public Color selectedBack;
        public Color normalText;
        public Color selectedText;
        public float selectedScale = 1.07f;

        private bool _selected;
        private float _selectTime;
        private Vector3 _baseScale = Vector3.one;
        private bool _init;

        public void SetSelected(bool pValue)
        {
            if (!_init) { _baseScale = transform.localScale; _init = true; }
            _selected = pValue;
            _selectTime = Time.unscaledTime;
            if (back != null) back.color = pValue ? selectedBack : normalBack;
            if (label != null) label.color = pValue ? selectedText : normalText;
            if (frame != null) frame.gameObject.SetActive(pValue);
        }

        private void Update()
        {
            if (!_init) return;
            float lPop = _selected ? Mathf.Max(0f, 1f - (Time.unscaledTime - _selectTime) * 6f) : 0f;
            float lTarget = _selected ? selectedScale + lPop * 0.08f : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * lTarget, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
            if (frame != null && _selected)
                frame.rectTransform.anchoredPosition = new Vector2(10f + Mathf.Sin(Time.unscaledTime * 9f) * 3f, -8f);
        }
    }
}
