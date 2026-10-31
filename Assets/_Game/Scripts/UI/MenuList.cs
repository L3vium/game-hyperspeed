using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Persona-style vertical menu : slanted panels, jittery selection, keyboard / pad / mouse.
    public class MenuList : MonoBehaviour
    {
        private class Item
        {
            public RectTransform rect;
            public UIShape back;
            public UIShape accent;
            public TextMeshProUGUI label;
            public Action action;
            public Vector2 basePosition;
        }

        private readonly List<Item> _items = new List<Item>();
        private int _selected;
        private float _selectTime;
        private Vector2 _lastMouse;

        public bool Interactable = true;
        public Action OnBack;

        public static MenuList Create(Transform pParent, Vector2 pAnchor, Vector2 pPosition)
        {
            RectTransform lRect = UIFactory.CreateRect(pParent, "Menu", pAnchor, pPosition, new Vector2(700f, 600f));
            return lRect.gameObject.AddComponent<MenuList>();
        }

        public void Add(string pLabel, Action pAction)
        {
            int lIndex = _items.Count;
            Vector2 lPosition = new Vector2(lIndex * -28f, -lIndex * 105f);
            RectTransform lRect = UIFactory.CreateRect(transform, "Item_" + pLabel, new Vector2(0.5f, 1f), lPosition, new Vector2(560f, 86f));
            Item lItem = new Item { rect = lRect, action = pAction, basePosition = lPosition };
            lItem.accent = UIFactory.CreateShape(lRect, "Accent", UIShape.ShapeKind.Parallelogram, UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(14f, -8f), new Vector2(560f, 86f));
            lItem.back = UIFactory.CreateShape(lRect, "Back", UIShape.ShapeKind.Parallelogram, UIFactory.BLACK, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 86f));
            lItem.label = UIFactory.CreateText(lRect, pLabel, 60f, UIFactory.WHITE, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(540f, 86f), true);
            lItem.label.fontStyle = FontStyles.Italic;
            _items.Add(lItem);
            Refresh(true);
        }

        public void SetSelected(int pIndex)
        {
            _selected = Mathf.Clamp(pIndex, 0, _items.Count - 1);
            _selectTime = Time.unscaledTime;
            Refresh(true);
        }

        private int _enabledFrame;

        private void OnEnable()
        {
            _enabledFrame = Time.frameCount;
        }

        private void Update()
        {
            if (!Interactable || _items.Count == 0) return;
            // Ignore the input that opened this menu.
            if (Time.frameCount <= _enabledFrame + 1) return;

            int lMove = -HSInput.MenuMove.y;
            if (lMove != 0)
            {
                _selected = (_selected + lMove + _items.Count) % _items.Count;
                _selectTime = Time.unscaledTime;
                AudioManager.Play("ui_move", 0.6f);
            }

            Vector2 lMouse = HSInput.MousePosition;
            bool lMouseMoved = (lMouse - _lastMouse).sqrMagnitude > 4f;
            _lastMouse = lMouse;
            for (int i = 0; i < _items.Count; i++)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(_items[i].rect, lMouse, null)) continue;
                if (lMouseMoved && _selected != i)
                {
                    _selected = i;
                    _selectTime = Time.unscaledTime;
                    AudioManager.Play("ui_move", 0.5f);
                }
                if (HSInput.MouseClicked)
                {
                    _selected = i;
                    Submit();
                    return;
                }
            }

            if (HSInput.SubmitPressed)
            {
                Submit();
                return;
            }
            if (HSInput.BackPressed && OnBack != null)
            {
                AudioManager.Play("ui_back", 0.7f);
                OnBack();
                return;
            }
            Refresh(false);
        }

        private void Submit()
        {
            AudioManager.Play("ui_select", 0.8f);
            Juice.Flash(Color.white, 0.15f);
            _items[_selected].action?.Invoke();
        }

        private void Refresh(bool pInstant)
        {
            float lT = pInstant ? 1f : 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
            for (int i = 0; i < _items.Count; i++)
            {
                Item lItem = _items[i];
                bool lSelected = i == _selected;
                float lSince = Time.unscaledTime - _selectTime;
                float lPop = lSelected ? Mathf.Max(0f, 1f - lSince * 5f) : 0f;
                Vector2 lTarget = lItem.basePosition + (lSelected ? new Vector2(40f, 0f) : Vector2.zero);
                lItem.rect.anchoredPosition = Vector2.Lerp(lItem.rect.anchoredPosition, lTarget, lT);
                float lScale = lSelected ? 1.12f + lPop * 0.15f : 0.92f;
                lItem.rect.localScale = Vector3.Lerp(lItem.rect.localScale, Vector3.one * lScale, lT);
                lItem.rect.localRotation = Quaternion.Euler(0f, 0f, lSelected ? 3f + Mathf.Sin(Time.unscaledTime * 6f) * 1.5f : -2f);
                lItem.back.color = lSelected ? UIFactory.RED : UIFactory.BLACK;
                lItem.accent.color = lSelected ? UIFactory.WHITE : UIFactory.DARK_RED;
                lItem.accent.rectTransform.anchoredPosition = lSelected ? new Vector2(18f + Mathf.Sin(Time.unscaledTime * 20f) * 2f, -10f) : new Vector2(10f, -6f);
                lItem.label.color = lSelected ? UIFactory.WHITE : new Color(0.85f, 0.85f, 0.85f);
            }
        }
    }
}
