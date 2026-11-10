using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // Objects : selection, editing many at once, and mass placement.
    public partial class LevelDesignWindow
    {
        [SerializeField] private ItemKind _brushKind = ItemKind.Crate;
        [SerializeField] private float _brushSpacing = 120f;
        [SerializeField] private float _brushJitter;
        [SerializeField] private float _eraseRadius = 60f;
        [SerializeField] private bool _eraseOnlyBrushKind;
        [SerializeField] private Pattern _pattern = Pattern.Line;
        [SerializeField] private int _patternCount = 8;
        [SerializeField] private int _patternAcross = 3;
        [SerializeField] private float _patternWidth = 0.8f;
        [SerializeField] private float _patternAmplitude = 0.5f;
        [SerializeField] private bool _patternDash = true;
        [SerializeField] private float _duplicateOffset = 200f;
        private ItemData _template;
        private float _patternS0 = -1f, _patternS1 = -1f, _patternL0, _patternL1;
        private Vector2 _objectScroll;

        // ================================================================ objects tab

        private void DrawObjectsTab()
        {
            EditorGUILayout.BeginHorizontal();
            ToolButton(Tool.Select, "Select in the Scene");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Click an object, Shift-click to add, Ctrl-click to toggle, drag a box to select many. Drag the move handle to slide them along / across the track. Del deletes, Ctrl+D duplicates, F frames.", MessageType.None);

            EditorGUILayout.LabelField("Show / pick", EditorStyles.boldLabel);
            int[] lCounts = new int[LDItems.KINDS.Length];
            foreach (ItemData lItem in Data.items) lCounts[(int)lItem.kind]++;
            for (int i = 0; i < LDItems.KINDS.Length; i += 3)
            {
                EditorGUILayout.BeginHorizontal();
                for (int k = i; k < Mathf.Min(i + 3, LDItems.KINDS.Length); k++)
                {
                    Color lOld = GUI.contentColor;
                    GUI.contentColor = LDItems.ColorOf(LDItems.KINDS[k]);
                    _visible[k] = GUILayout.Toggle(_visible[k], LDItems.KINDS[k] + " (" + lCounts[k] + ")", GUILayout.Width(118f));
                    GUI.contentColor = lOld;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Show all")) { for (int i = 0; i < _visible.Length; i++) _visible[i] = true; SceneView.RepaintAll(); }
            if (GUILayout.Button("Hide all")) { for (int i = 0; i < _visible.Length; i++) _visible[i] = false; SceneView.RepaintAll(); }
            if (GUILayout.Button("Select all shown")) { _selected.Clear(); for (int i = 0; i < Data.items.Count; i++) if (_visible[(int)Data.items[i].kind]) _selected.Add(i); SceneView.RepaintAll(); }
            if (GUILayout.Button("Select none")) { _selected.Clear(); SceneView.RepaintAll(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selection : " + _selected.Count, EditorStyles.boldLabel);
            if (_selected.Count == 0) return;
            DrawSelectionFields();

            EditorGUILayout.LabelField("Move", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("<< 100 m")) Nudge(-100f, 0f);
            if (GUILayout.Button("< 10 m")) Nudge(-10f, 0f);
            if (GUILayout.Button("10 m >")) Nudge(10f, 0f);
            if (GUILayout.Button("100 m >>")) Nudge(100f, 0f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Left 20")) Nudge(0f, -20f);
            if (GUILayout.Button("Right 20")) Nudge(0f, 20f);
            if (GUILayout.Button("Centre")) EditSelected("Centre", pItem => pItem.lateral = _layout.field.CenterLateral(pItem.s));
            if (GUILayout.Button("Mirror")) EditSelected("Mirror", pItem => pItem.lateral = 2f * _layout.field.CenterLateral(pItem.s) - pItem.lateral);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            _duplicateOffset = EditorGUILayout.FloatField("Duplicate offset", _duplicateOffset);
            if (GUILayout.Button("Duplicate")) DuplicateSelected(_duplicateOffset);
            if (GUILayout.Button("Delete")) DeleteSelected();
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Frame selection")) FrameSelection();

            _objectScroll = EditorGUILayout.BeginScrollView(_objectScroll, GUILayout.Height(160f));
            foreach (int lIndex in _selected)
            {
                ItemData lItem = Data.items[lIndex];
                EditorGUILayout.LabelField("#" + lIndex + "  " + lItem.kind + "   s " + lItem.s.ToString("F0") + "   lateral " + lItem.lateral.ToString("F0"), EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        // Fields of the selection : s and lateral when one object is picked, the rest applies to every selected object
        // of the same kind.
        private void DrawSelectionFields()
        {
            ItemData lFirst = Data.items[_selected[0]];
            bool lSingle = _selected.Count == 1;
            if (lSingle)
            {
                EditorGUI.BeginChangeCheck();
                float lS = EditorGUILayout.FloatField("Distance (s)", lFirst.s);
                float lLateral = LDItems.FullWidth(lFirst.kind) ? lFirst.lateral : EditorGUILayout.FloatField("Lateral", lFirst.lateral);
                if (EditorGUI.EndChangeCheck())
                {
                    Record("Move object");
                    float lDelta = lS - lFirst.s;
                    lFirst.s = lS;
                    if (LDItems.Spans(lFirst.kind)) lFirst.s1 += lDelta;
                    lFirst.lateral = lLateral;
                    Changed();
                }
            }
            else EditorGUILayout.LabelField(lFirst.kind + " fields apply to every selected " + lFirst.kind, EditorStyles.miniLabel);

            foreach (LDItems.Field lField in LDItems.FieldsOf(lFirst.kind))
            {
                EditorGUI.BeginChangeCheck();
                object lValue = lField.info.GetValue(lFirst);
                object lNew = lValue;
                switch (lField.type)
                {
                    case LDItems.FieldType.Float: lNew = EditorGUILayout.FloatField(lField.label, (float)lValue); break;
                    case LDItems.FieldType.Int: lNew = EditorGUILayout.IntField(lField.label, (int)lValue); break;
                    case LDItems.FieldType.Bool: lNew = EditorGUILayout.Toggle(lField.label, (bool)lValue); break;
                    case LDItems.FieldType.PaneKind: lNew = (int)(PaneKind)EditorGUILayout.EnumPopup(lField.label, (PaneKind)Mathf.Clamp((int)lValue, 0, 2)); break;
                }
                if (!EditorGUI.EndChangeCheck()) continue;
                Record("Edit " + lField.label);
                foreach (int lIndex in _selected)
                {
                    ItemData lItem = Data.items[lIndex];
                    if (lItem.kind != lFirst.kind) continue;
                    // A span end moves with its object : apply the same offset rather than the same value.
                    if (lField.name == "s1" && !lSingle) lItem.s1 += (float)lNew - (float)lValue;
                    else lField.info.SetValue(lItem, lNew);
                }
                Changed();
            }
        }

        private void EditSelected(string pWhat, System.Action<ItemData> pEdit)
        {
            if (_selected.Count == 0) return;
            Record(pWhat);
            foreach (int lIndex in _selected) pEdit(Data.items[lIndex]);
            Changed();
        }

        private void Nudge(float pDs, float pDl) => EditSelected("Nudge", pItem =>
        {
            pItem.s += pDs;
            if (LDItems.Spans(pItem.kind)) pItem.s1 += pDs;
            pItem.lateral += pDl;
        });

        private void DuplicateSelected(float pOffset)
        {
            if (_selected.Count == 0) return;
            Record("Duplicate objects");
            List<int> lNew = new List<int>();
            foreach (int lIndex in _selected)
            {
                ItemData lCopy = Data.items[lIndex].Clone();
                lCopy.s += pOffset;
                if (LDItems.Spans(lCopy.kind)) lCopy.s1 += pOffset;
                Data.items.Add(lCopy);
                lNew.Add(Data.items.Count - 1);
            }
            _selected.Clear();
            _selected.AddRange(lNew);
            Changed();
        }

        private void DeleteSelected()
        {
            if (_selected.Count == 0) return;
            Record("Delete objects");
            List<int> lSorted = new List<int>(_selected);
            lSorted.Sort();
            for (int i = lSorted.Count - 1; i >= 0; i--) Data.items.RemoveAt(lSorted[i]);
            _selected.Clear();
            Changed();
        }

        private void FrameSelection()
        {
            if (_selected.Count == 0 || _itemPositions == null || SceneView.lastActiveSceneView == null) return;
            Bounds lBounds = new Bounds(_itemPositions[_selected[0]], Vector3.one * 60f);
            foreach (int lIndex in _selected) if (lIndex < _itemPositions.Length) lBounds.Encapsulate(_itemPositions[lIndex]);
            lBounds.Expand(80f);
            SceneView.lastActiveSceneView.Frame(lBounds, false);
        }

        // ================================================================ place tab

        private ItemData Template(float pS, float pLateral)
        {
            if (_template == null || _template.kind != _brushKind) _template = LDItems.Create(_brushKind, pS, pLateral, _layout);
            ItemData lItem = _template.Clone();
            float lSpan = lItem.s1 - lItem.s;
            lItem.s = pS;
            lItem.s1 = pS + lSpan;
            lItem.lateral = pLateral;
            if (lItem.kind == ItemKind.Hammer) lItem.value = Random.Range(0f, 6.28f);
            if (lItem.kind == ItemKind.Prop) lItem.jitter = Random.Range(-14f, 14f);
            return lItem;
        }

        private void DrawPlaceTab()
        {
            EditorGUILayout.LabelField("Object to place", EditorStyles.boldLabel);
            ItemKind lKind = (ItemKind)EditorGUILayout.EnumPopup("Kind", _brushKind);
            if (lKind != _brushKind) { _brushKind = lKind; _template = null; }
            if (_template == null || _template.kind != _brushKind) _template = LDItems.Create(_brushKind, 0f, 0f, _layout);
            foreach (LDItems.Field lField in LDItems.FieldsOf(_brushKind))
            {
                if (lField.name == "s1")
                {
                    _template.s1 = _template.s + EditorGUILayout.FloatField("Span length", _template.s1 - _template.s);
                    continue;
                }
                object lValue = lField.info.GetValue(_template);
                switch (lField.type)
                {
                    case LDItems.FieldType.Float: lField.info.SetValue(_template, EditorGUILayout.FloatField(lField.label, (float)lValue)); break;
                    case LDItems.FieldType.Int: lField.info.SetValue(_template, EditorGUILayout.IntField(lField.label, (int)lValue)); break;
                    case LDItems.FieldType.Bool: lField.info.SetValue(_template, EditorGUILayout.Toggle(lField.label, (bool)lValue)); break;
                    case LDItems.FieldType.PaneKind: lField.info.SetValue(_template, (int)(PaneKind)EditorGUILayout.EnumPopup(lField.label, (PaneKind)Mathf.Clamp((int)lValue, 0, 2))); break;
                }
            }
            if (GUILayout.Button("Reset to stage defaults")) _template = null;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            ToolButton(Tool.Paint, "Paint");
            ToolButton(Tool.Erase, "Erase");
            ToolButton(Tool.Pattern, "Pattern");
            EditorGUILayout.EndHorizontal();
            if (_tool == Tool.Paint)
            {
                _brushSpacing = EditorGUILayout.FloatField("Spacing along (m)", _brushSpacing);
                _brushJitter = EditorGUILayout.FloatField("Random sideways (m)", _brushJitter);
                EditorGUILayout.HelpBox("Click or drag on the track to place.", MessageType.None);
            }
            if (_tool == Tool.Erase)
            {
                _eraseRadius = EditorGUILayout.FloatField("Radius (m)", _eraseRadius);
                _eraseOnlyBrushKind = EditorGUILayout.Toggle("Only " + _brushKind, _eraseOnlyBrushKind);
                EditorGUILayout.HelpBox("Drag on the track to erase.", MessageType.None);
            }
            if (_tool == Tool.Pattern) DrawPatternPanel();
            _patternDash = EditorGUILayout.Toggle("Autopilot dashes through walls", _patternDash);
        }

        private void DrawPatternPanel()
        {
            _pattern = (Pattern)EditorGUILayout.EnumPopup("Pattern", _pattern);
            EditorGUILayout.HelpBox("Click the track twice : start then end. " + PatternHelp(), MessageType.None);
            _patternS0 = EditorGUILayout.FloatField("From s", _patternS0);
            _patternS1 = EditorGUILayout.FloatField("To s", _patternS1);
            _patternL0 = EditorGUILayout.FloatField("From lateral", _patternL0);
            _patternL1 = EditorGUILayout.FloatField("To lateral", _patternL1);
            _patternCount = Mathf.Max(1, EditorGUILayout.IntField(_pattern == Pattern.Wall ? "Walls" : "Count along", _patternCount));
            if (_pattern == Pattern.Rows || _pattern == Pattern.Grid) _patternAcross = Mathf.Max(1, EditorGUILayout.IntField("Across", _patternAcross));
            if (_pattern != Pattern.Line) _patternWidth = EditorGUILayout.Slider("Share of the width", _patternWidth, 0.05f, 1f);
            if (_pattern == Pattern.Slalom) _patternAmplitude = EditorGUILayout.Slider("Amplitude", _patternAmplitude, 0f, 1f);
            GUI.enabled = _patternS1 > _patternS0 && _patternS0 >= 0f;
            if (GUILayout.Button("Place pattern", GUILayout.Height(26f))) ApplyPattern();
            GUI.enabled = true;
        }

        private string PatternHelp()
        {
            switch (_pattern)
            {
                case Pattern.Line: return "Line : objects evenly spaced from start to end.";
                case Pattern.Rows: return "Rows : rows across the track, evenly spaced.";
                case Pattern.Wall: return "Wall : full-width walls of the object (panes, crates) to dash through.";
                case Pattern.Scatter: return "Scatter : random positions inside the span.";
                case Pattern.Slalom: return "Slalom : a weaving line, left and right.";
                case Pattern.Grid: return "Grid : a regular grid of objects.";
            }
            return "";
        }

        private void ApplyPattern()
        {
            if (_layout == null) return;
            Record("Place " + _pattern);
            int lBefore = Data.items.Count;
            float lS0 = _patternS0, lS1 = _patternS1;
            for (int i = 0; i < _patternCount; i++)
            {
                float lT = _patternCount == 1 ? 0.5f : i / (float)(_patternCount - 1);
                float lS = Mathf.Lerp(lS0, lS1, lT);
                float lCenter = _layout.field.CenterLateral(lS);
                float lHalf = _layout.field.HalfWidth(lS) * _patternWidth;
                switch (_pattern)
                {
                    case Pattern.Line:
                        Data.items.Add(Template(lS, Mathf.Lerp(_patternL0, _patternL1, lT)));
                        break;
                    case Pattern.Slalom:
                        Data.items.Add(Template(lS, lCenter + Mathf.Sin(lT * Mathf.PI * Mathf.Max(1, _patternCount / 2)) * lHalf * _patternAmplitude));
                        break;
                    case Pattern.Scatter:
                        Data.items.Add(Template(Random.Range(lS0, lS1), lCenter + Random.Range(-lHalf, lHalf)));
                        break;
                    case Pattern.Rows:
                    case Pattern.Grid:
                        for (int c = 0; c < _patternAcross; c++)
                        {
                            float lU = _patternAcross == 1 ? 0.5f : c / (float)(_patternAcross - 1);
                            float lShift = _pattern == Pattern.Rows && (i & 1) == 1 ? 0.5f / _patternAcross : 0f;
                            Data.items.Add(Template(lS, lCenter + Mathf.Lerp(-lHalf, lHalf, Mathf.Clamp01(lU + lShift))));
                        }
                        break;
                    case Pattern.Wall:
                    {
                        float lSize = _brushKind == ItemKind.Pane ? Mathf.Max(10f, _template.width) : _brushKind == ItemKind.Crate ? Mathf.Max(10f, _template.size) : 50f;
                        int lCount = Mathf.Max(1, Mathf.RoundToInt(lHalf * 2f / lSize));
                        float lWidth = lHalf * 2f / lCount;
                        for (int c = 0; c < lCount; c++)
                        {
                            ItemData lItem = Template(lS, lCenter - lHalf + lWidth * (c + 0.5f));
                            if (lItem.kind == ItemKind.Pane) lItem.width = lWidth * 0.98f;
                            if (lItem.kind == ItemKind.Crate) lItem.size = lWidth * 0.98f;
                            Data.items.Add(lItem);
                        }
                        if (_patternDash) Data.acts.Add(new ActData { s = lS - Cruise * 0.32f, action = RouteAction.Dash });
                        break;
                    }
                }
            }
            _selected.Clear();
            for (int i = lBefore; i < Data.items.Count; i++) _selected.Add(i);
            Changed();
        }

        // ================================================================ brush operations (from the Scene)

        private float _lastPaintS = float.MinValue;

        private void PaintAt(float pS, float pLateral, bool pFirst)
        {
            if (!pFirst && Mathf.Abs(pS - _lastPaintS) < Mathf.Max(2f, _brushSpacing)) return;
            Record("Paint " + _brushKind);
            _lastPaintS = pS;
            float lLateral = LDItems.FullWidth(_brushKind) ? _layout.field.CenterLateral(pS) : pLateral + Random.Range(-_brushJitter, _brushJitter);
            Data.items.Add(Template(pS, lLateral));
            if (_patternDash && (_brushKind == ItemKind.Crate || _brushKind == ItemKind.Pane)) Data.acts.Add(new ActData { s = pS - Cruise * 0.32f, action = RouteAction.Dash });
            Changed();
        }

        private void EraseAt(Vector3 pPoint, bool pFirst)
        {
            if (_itemPositions == null) return;
            bool lAny = false;
            for (int i = Data.items.Count - 1; i >= 0; i--)
            {
                if (i >= _itemPositions.Length) continue;
                ItemData lItem = Data.items[i];
                if (!_visible[(int)lItem.kind] || (_eraseOnlyBrushKind && lItem.kind != _brushKind)) continue;
                if ((_itemPositions[i] - pPoint).sqrMagnitude > _eraseRadius * _eraseRadius) continue;
                if (!lAny) Record("Erase objects");
                lAny = true;
                Data.items.RemoveAt(i);
            }
            if (!lAny) return;
            _selected.Clear();
            Changed();
        }
    }
}
