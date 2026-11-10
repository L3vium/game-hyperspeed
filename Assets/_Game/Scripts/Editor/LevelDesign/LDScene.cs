using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // Scene view : draws the line, the blocks and every object, and turns clicks and drags into edits.
    public partial class LevelDesignWindow
    {
        private static readonly Color LINE = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color LINE_GRAVITY = new Color(0.3f, 1f, 0.9f, 0.9f);
        private static readonly Color SELECTED = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color EDGE = new Color(1f, 0.6f, 0.2f, 0.35f);

        private bool _boxing;
        private Vector2 _boxStart;
        private bool _painting;
        private bool _hasHover;
        private float _hoverS;
        private float _hoverLateral;
        private Vector3 _hoverPoint;
        private int _patternClicks;

        private void OnScene(SceneView pView)
        {
            if (_asset == null || EditorApplication.isPlaying || Data == null) return;
            Sync();
            if (_layout == null) return;
            Event lEvent = Event.current;
            int lControl = GUIUtility.GetControlID(FocusType.Passive);
            if (_tool != Tool.None && lEvent.type == EventType.Layout) HandleUtility.AddDefaultControl(lControl);

            if (lEvent.type == EventType.MouseMove || lEvent.type == EventType.MouseDrag || lEvent.type == EventType.MouseDown)
                _hasHover = PickTrack(lEvent.mousePosition, out _hoverS, out _hoverLateral, out _hoverPoint);

            DrawLine();
            DrawItems(pView);

            switch (_tool)
            {
                case Tool.Select: SelectTool(lEvent, lControl); break;
                case Tool.Track: TrackTool(lEvent); break;
                case Tool.Blocks: BlockTool(lEvent); break;
                case Tool.Paint: PaintTool(lEvent, lControl); break;
                case Tool.Erase: EraseTool(lEvent, lControl); break;
                case Tool.Pattern: PatternTool(lEvent, lControl); break;
            }
            SceneKeys(lEvent);
            DrawOverlay(pView);
            if (lEvent.type == EventType.MouseMove) pView.Repaint();
        }

        // ================================================================ drawing

        private void DrawLine()
        {
            if (_segmentLines == null) return;
            for (int i = 0; i < _segmentLines.Length; i++)
            {
                bool lSelected = i == _segment && _tool == Tool.Track;
                Handles.color = lSelected ? SELECTED : Data.segments[i].localGravity ? LINE_GRAVITY : (i & 1) == 0 ? LINE : new Color(0.75f, 0.85f, 1f, 0.55f);
                Handles.DrawAAPolyLine(lSelected ? 7f : 3f, _segmentLines[i]);
            }
            if (_showEdges)
            {
                Handles.color = EDGE;
                foreach (Vector3[] lEdge in _edges) Handles.DrawAAPolyLine(2f, lEdge);
            }
            if (_tool == Tool.Blocks && _block >= 0 && _block < Data.blocks.Count)
            {
                BlockData lBlock = Data.blocks[_block];
                Handles.color = SELECTED;
                for (int lSide = -1; lSide <= 1; lSide += 2)
                {
                    List<Vector3> lPoints = new List<Vector3>();
                    for (float lS = lBlock.s0; lS <= lBlock.s1; lS += Mathf.Max(8f, (lBlock.s1 - lBlock.s0) / 60f))
                        lPoints.Add(_layout.field.Point(Mathf.Clamp(lS, lBlock.s0 + 0.5f, lBlock.s1 - 0.5f), lBlock.offset + lSide * lBlock.halfWidth));
                    if (lPoints.Count > 1) Handles.DrawAAPolyLine(6f, lPoints.ToArray());
                }
            }
            // Start, sprint and finish markers.
            Marker(0f, "START", Color.green);
            if (Data.climaxS > 0f) Marker(Data.climaxS, "FINAL SPRINT", new Color(1f, 0.6f, 0.1f));
            Marker(Data.finishS, "FINISH", new Color(1f, 0.9f, 0.1f));
            if (_showLabels)
            {
                foreach (HintData lHint in Data.hints) Label(_layout.spine.Evaluate(lHint.s).position + Vector3.up * 30f, "\"" + lHint.text + "\"", new Color(0.6f, 1f, 1f));
                for (int i = 0; i < Data.segments.Count; i++)
                    if (i == _segment || Data.segments[i].label != (i > 0 ? Data.segments[i - 1].label : ""))
                        Label(_layout.spine.Evaluate(_segmentStarts[i] + 1f).position + Vector3.up * 12f, i + " " + Data.segments[i].label, i == _segment ? SELECTED : new Color(1f, 1f, 1f, 0.8f));
            }
        }

        private void Marker(float pS, string pText, Color pColor)
        {
            float lS = Mathf.Clamp(pS, _layout.spine.StartS, _layout.spine.EndS);
            float lHalf = _layout.field.TotalHalfWidth(lS);
            Vector3 lA = _layout.field.Point(lS, _layout.field.CenterLateral(lS) - lHalf);
            Vector3 lB = _layout.field.Point(lS, _layout.field.CenterLateral(lS) + lHalf);
            Handles.color = pColor;
            Handles.DrawAAPolyLine(8f, lA, lB);
            Label((lA + lB) * 0.5f + Vector3.up * 40f, pText, pColor);
        }

        private static void Label(Vector3 pPosition, string pText, Color pColor)
        {
            GUIStyle lStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = pColor } };
            Handles.Label(pPosition, pText, lStyle);
        }

        private void DrawItems(SceneView pView)
        {
            if (_itemPositions == null || _itemPositions.Length != Data.items.Count) RefreshItemPositions();
            Vector3 lCamera = pView.camera.transform.position;
            HashSet<int> lSelected = new HashSet<int>(_selected);
            for (int i = 0; i < Data.items.Count; i++)
            {
                ItemData lItem = Data.items[i];
                if (!_visible[(int)lItem.kind]) continue;
                Vector3 lPosition = _itemPositions[i];
                bool lIsSelected = lSelected.Contains(i);
                Color lColor = lIsSelected ? SELECTED : LDItems.ColorOf(lItem.kind);
                Handles.color = lColor;
                float lSize = HandleUtility.GetHandleSize(lPosition) * (lIsSelected ? 0.09f : 0.06f);
                if (LDItems.Spans(lItem.kind))
                {
                    List<Vector3> lPoints = new List<Vector3>();
                    float lSpan = Mathf.Max(1f, lItem.s1 - lItem.s);
                    for (float lT = 0f; lT <= 1.001f; lT += 0.1f)
                    {
                        float lS = Mathf.Clamp(lItem.s + lSpan * lT, _layout.spine.StartS, _layout.spine.EndS);
                        SpineSample lSample = _layout.spine.Evaluate(lS);
                        float lHeight = lItem.kind == ItemKind.Rope ? Mathf.Lerp(lItem.height, lItem.height1, lT) : lItem.height;
                        lPoints.Add(lSample.position + lSample.Right * lItem.lateral + lSample.up * lHeight);
                    }
                    Handles.DrawAAPolyLine(lIsSelected ? 8f : 5f, lPoints.ToArray());
                }
                else if (LDItems.FullWidth(lItem.kind))
                {
                    float lS = Mathf.Clamp(lItem.s, _layout.spine.StartS, _layout.spine.EndS);
                    float lHalf = _layout.field.HalfWidth(lS);
                    float lCenter = _layout.field.CenterLateral(lS);
                    Vector3 lUp = _layout.field.Normal(lS) * (lItem.kind == ItemKind.Laser ? lItem.height : 4f);
                    Handles.DrawAAPolyLine(lIsSelected ? 6f : 3f, _layout.field.Point(lS, lCenter - lHalf) + lUp, _layout.field.Point(lS, lCenter + lHalf) + lUp);
                }
                else if (lItem.kind == ItemKind.Pad || lItem.kind == ItemKind.Pane || lItem.kind == ItemKind.Mover)
                {
                    float lS = Mathf.Clamp(lItem.s, _layout.spine.StartS, _layout.spine.EndS);
                    Vector3 lRight = _layout.spine.Right(lS) * lItem.width * 0.5f;
                    Vector3 lUp = _layout.field.Normal(lS) * 2f;
                    Handles.DrawAAPolyLine(lIsSelected ? 6f : 3f, lPosition - lRight + lUp, lPosition + lRight + lUp);
                }
                if ((lPosition - lCamera).sqrMagnitude > 9000f * 9000f) continue;
                Handles.DotHandleCap(0, lPosition, Quaternion.identity, lSize, EventType.Repaint);
            }
        }

        private void DrawOverlay(SceneView pView)
        {
            Handles.BeginGUI();
            string lTool = _tool == Tool.None ? "No tool" : _tool.ToString();
            string lText = "Level Design : " + lTool + (_selected.Count > 0 ? "   |   " + _selected.Count + " selected" : "")
                + (_hasHover ? "   |   s " + _hoverS.ToString("F0") + "  lateral " + _hoverLateral.ToString("F0") : "");
            GUI.Label(new Rect(10f, pView.position.height - 52f, 900f, 20f), lText, EditorStyles.whiteBoldLabel);
            if (_boxing)
            {
                Rect lRect = BoxRect(Event.current.mousePosition);
                GUI.color = new Color(1f, 0.85f, 0.2f, 0.25f);
                GUI.DrawTexture(lRect, EditorGUIUtility.whiteTexture);
                GUI.color = Color.white;
            }
            Handles.EndGUI();
            if ((_tool == Tool.Paint || _tool == Tool.Erase || _tool == Tool.Pattern) && _hasHover)
            {
                Handles.color = _tool == Tool.Erase ? new Color(1f, 0.3f, 0.3f, 0.9f) : LDItems.ColorOf(_brushKind);
                Handles.DrawWireDisc(_hoverPoint, _layout.field.Normal(_hoverS), _tool == Tool.Erase ? _eraseRadius : 12f);
            }
            if (_tool == Tool.Pattern && _patternS0 >= 0f)
            {
                Handles.color = LDItems.ColorOf(_brushKind);
                Vector3 lA = _layout.field.Point(_patternS0, _patternL0);
                Vector3 lB = _patternS1 > _patternS0 ? _layout.field.Point(_patternS1, _patternL1) : _hoverPoint;
                Handles.DrawDottedLine(lA, lB, 6f);
            }
        }

        private Rect BoxRect(Vector2 pMouse) => Rect.MinMaxRect(Mathf.Min(_boxStart.x, pMouse.x), Mathf.Min(_boxStart.y, pMouse.y), Mathf.Max(_boxStart.x, pMouse.x), Mathf.Max(_boxStart.y, pMouse.y));

        // ================================================================ picking

        // The point of the track under the mouse, as (s, lateral). Tested against the floor of every block, nearest wins.
        private bool PickTrack(Vector2 pMouse, out float pS, out float pLateral, out Vector3 pPoint)
        {
            pS = 0f;
            pLateral = 0f;
            pPoint = Vector3.zero;
            Ray lRay = HandleUtility.GUIPointToWorldRay(pMouse);
            Spine lSpine = _layout.spine;
            float lBest = float.MaxValue;
            int lBestIndex = -1;
            for (int i = 0; i < lSpine.Count; i += 4)
            {
                SpineSample lSample = lSpine.samples[i];
                Vector3 lNormal = _layout.field.Normal(lSample.s);
                Vector3 lCenter = _layout.field.Point(lSample.s, _layout.field.CenterLateral(lSample.s));
                float lDenominator = Vector3.Dot(lNormal, lRay.direction);
                if (Mathf.Abs(lDenominator) < 1e-4f) continue;
                float lDistance = Vector3.Dot(lCenter - lRay.origin, lNormal) / lDenominator;
                if (lDistance <= 0f || lDistance >= lBest) continue;
                Vector3 lHit = lRay.origin + lRay.direction * lDistance;
                Vector3 lLocal = lHit - lCenter;
                if (Mathf.Abs(Vector3.Dot(lLocal, lSample.forward)) > Spine.STEP * 3f) continue;
                if (Mathf.Abs(Vector3.Dot(lLocal, lSample.Right)) > _layout.field.TotalHalfWidth(lSample.s) + 40f) continue;
                lBest = lDistance;
                lBestIndex = i;
                pPoint = lHit;
            }
            if (lBestIndex < 0) return false;
            WorldToTrack(pPoint, lBestIndex, out pS, out pLateral);
            return true;
        }

        // (s, lateral) of a world point, searching around a sample index (or everywhere with -1).
        private void WorldToTrack(Vector3 pPoint, int pAround, out float pS, out float pLateral)
        {
            Spine lSpine = _layout.spine;
            int lFrom = pAround < 0 ? 0 : Mathf.Max(0, pAround - 12);
            int lTo = pAround < 0 ? lSpine.Count - 1 : Mathf.Min(lSpine.Count - 1, pAround + 12);
            int lBest = lFrom;
            float lBestDistance = float.MaxValue;
            for (int i = lFrom; i <= lTo; i += pAround < 0 ? 2 : 1)
            {
                float lDistance = (lSpine.samples[i].position - pPoint).sqrMagnitude;
                if (lDistance < lBestDistance) { lBestDistance = lDistance; lBest = i; }
            }
            SpineSample lSample = lSpine.samples[lBest];
            pS = lSample.s + Vector3.Dot(pPoint - lSample.position, lSample.forward);
            TrackBlock lBlock = _layout.field.BlockAt(pS);
            float lOffset = lBlock != null ? lBlock.offset : 0f;
            float lCos = lBlock != null ? Mathf.Max(0.3f, lBlock.Cos) : 1f;
            float lSide = Vector3.Dot(pPoint - lSample.position, lSample.Right);
            pLateral = lOffset + (lSide - lOffset) / lCos;
        }

        private int PickItem(Vector2 pMouse)
        {
            int lBest = -1;
            float lBestDistance = 14f * 14f;
            for (int i = 0; i < Data.items.Count; i++)
            {
                if (i >= _itemPositions.Length || !_visible[(int)Data.items[i].kind]) continue;
                Vector2 lScreen = HandleUtility.WorldToGUIPoint(_itemPositions[i]);
                if (!InFront(_itemPositions[i])) continue;
                float lDistance = (lScreen - pMouse).sqrMagnitude;
                if (lDistance < lBestDistance) { lBestDistance = lDistance; lBest = i; }
            }
            return lBest;
        }

        private static bool InFront(Vector3 pPoint)
        {
            Camera lCamera = Camera.current;
            return lCamera == null || Vector3.Dot(pPoint - lCamera.transform.position, lCamera.transform.forward) > 0f;
        }

        private int PickSegment(Vector2 pMouse)
        {
            int lBest = -1;
            float lBestDistance = 18f * 18f;
            for (int i = 0; i < _segmentLines.Length; i++)
                foreach (Vector3 lPoint in _segmentLines[i])
                {
                    if (!InFront(lPoint)) continue;
                    float lDistance = (HandleUtility.WorldToGUIPoint(lPoint) - pMouse).sqrMagnitude;
                    if (lDistance < lBestDistance) { lBestDistance = lDistance; lBest = i; }
                }
            return lBest;
        }

        // ================================================================ tools

        private void SelectTool(Event pEvent, int pControl)
        {
            // Move handle on the selection : slides everything along and across the track.
            if (_selected.Count > 0 && !_boxing)
            {
                Vector3 lCentroid = Vector3.zero;
                int lCount = 0;
                foreach (int lIndex in _selected) if (lIndex < _itemPositions.Length) { lCentroid += _itemPositions[lIndex]; lCount++; }
                if (lCount > 0)
                {
                    lCentroid /= lCount;
                    EditorGUI.BeginChangeCheck();
                    Vector3 lMoved = Handles.PositionHandle(lCentroid, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        float lS0, lL0, lS1, lL1;
                        WorldToTrack(lCentroid, -1, out lS0, out lL0);
                        WorldToTrack(lMoved, -1, out lS1, out lL1);
                        if (Mathf.Abs(lS1 - lS0) < 2000f)
                        {
                            Record("Move objects");
                            foreach (int lIndex in _selected)
                            {
                                ItemData lItem = Data.items[lIndex];
                                lItem.s += lS1 - lS0;
                                if (LDItems.Spans(lItem.kind)) lItem.s1 += lS1 - lS0;
                                lItem.lateral += lL1 - lL0;
                            }
                            Changed();
                        }
                    }
                }
            }

            switch (pEvent.type)
            {
                case EventType.MouseDown:
                    if (pEvent.button != 0 || pEvent.alt || GUIUtility.hotControl != 0) return;
                    int lPicked = PickItem(pEvent.mousePosition);
                    if (lPicked >= 0)
                    {
                        if (pEvent.control || pEvent.command) { if (!_selected.Remove(lPicked)) _selected.Add(lPicked); }
                        else if (pEvent.shift) { if (!_selected.Contains(lPicked)) _selected.Add(lPicked); }
                        else { _selected.Clear(); _selected.Add(lPicked); }
                        _tab = Tab.Objects;
                        Repaint();
                    }
                    else
                    {
                        _boxing = true;
                        _boxStart = pEvent.mousePosition;
                        GUIUtility.hotControl = pControl;
                    }
                    pEvent.Use();
                    break;
                case EventType.MouseDrag:
                    if (_boxing) { pEvent.Use(); SceneView.RepaintAll(); }
                    break;
                case EventType.MouseUp:
                    if (!_boxing) return;
                    _boxing = false;
                    GUIUtility.hotControl = 0;
                    Rect lRect = BoxRect(pEvent.mousePosition);
                    if (!pEvent.shift && !pEvent.control) _selected.Clear();
                    if (lRect.width > 4f || lRect.height > 4f)
                    {
                        for (int i = 0; i < Data.items.Count; i++)
                        {
                            if (i >= _itemPositions.Length || !_visible[(int)Data.items[i].kind] || !InFront(_itemPositions[i])) continue;
                            if (lRect.Contains(HandleUtility.WorldToGUIPoint(_itemPositions[i])) && !_selected.Contains(i)) _selected.Add(i);
                        }
                        if (_selected.Count > 0) _tab = Tab.Objects;
                    }
                    pEvent.Use();
                    Repaint();
                    SceneView.RepaintAll();
                    break;
            }
        }

        private void TrackTool(Event pEvent)
        {
            if (_segment >= 0 && _segment < Data.segments.Count) SegmentHandles();
            if (pEvent.type == EventType.MouseDown && pEvent.button == 0 && !pEvent.alt && GUIUtility.hotControl == 0)
            {
                int lPicked = PickSegment(pEvent.mousePosition);
                if (lPicked >= 0)
                {
                    _segment = lPicked;
                    _tab = Tab.Track;
                    Repaint();
                    pEvent.Use();
                }
            }
        }

        // Length arrow, turn / climb / bank rings at the end of the selected piece.
        private void SegmentHandles()
        {
            SegmentData lSegment = Data.segments[_segment];
            float lEndS = _segmentStarts[_segment] + lSegment.Length;
            SpineSample lEnd = _layout.spine.Evaluate(Mathf.Min(lEndS, _layout.spine.EndS));
            Vector3 lPosition = lEnd.position;
            float lSize = HandleUtility.GetHandleSize(lPosition);

            Handles.color = new Color(0.3f, 1f, 0.4f);
            EditorGUI.BeginChangeCheck();
            Vector3 lSlid = Handles.Slider(lPosition, lEnd.forward, lSize * 1.4f, Handles.ArrowHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
            {
                Record("Piece length");
                SetLength(_segment, lSegment.Length + Vector3.Dot(lSlid - lPosition, lEnd.forward));
                Changed();
            }
            Vector3 lFlat = Vector3.ProjectOnPlane(lEnd.forward, Vector3.up).normalized;
            Rotation(lPosition, Vector3.up, lFlat, lSize * 1.1f, new Color(1f, 0.85f, 0.2f), () => lSegment.yaw, pValue => lSegment.yaw = pValue, 1f, "Turn");
            Rotation(lPosition, lEnd.Right, lEnd.forward, lSize * 0.9f, new Color(1f, 0.3f, 0.3f), () => lSegment.pitch, pValue => lSegment.pitch = pValue, -1f, "Climb");
            Rotation(lPosition, lEnd.forward, lEnd.up, lSize * 0.7f, new Color(0.3f, 0.6f, 1f), () => lSegment.roll, pValue => lSegment.roll = pValue, -1f, "Bank");
        }

        private int _discControl;
        private float _discStart;

        // A ring returns the rotation since the drag began : the value is set from where it was at that moment.
        private void Rotation(Vector3 pPosition, Vector3 pAxis, Vector3 pReference, float pSize, Color pColor, System.Func<float> pGet, System.Action<float> pSet, float pSign, string pWhat)
        {
            Handles.color = pColor;
            EditorGUI.BeginChangeCheck();
            Quaternion lTotal = Handles.Disc(Quaternion.identity, pPosition, pAxis, pSize, false, 0f);
            if (!EditorGUI.EndChangeCheck()) return;
            if (GUIUtility.hotControl != _discControl)
            {
                _discControl = GUIUtility.hotControl;
                _discStart = pGet();
            }
            float lAngle = Vector3.SignedAngle(pReference, lTotal * pReference, pAxis);
            Record(pWhat);
            pSet(Mathf.Round((_discStart + lAngle * pSign) * 10f) / 10f);
            Changed();
        }

        private void BlockTool(Event pEvent)
        {
            if (_block >= 0 && _block < Data.blocks.Count)
            {
                BlockData lBlock = Data.blocks[_block];
                float lMid = Mathf.Clamp((lBlock.s0 + lBlock.s1) * 0.5f, _layout.spine.StartS, _layout.spine.EndS);
                SpineSample lSample = _layout.spine.Evaluate(lMid);
                for (int lSide = -1; lSide <= 1; lSide += 2)
                {
                    Vector3 lEdge = _layout.field.Point(lMid, lBlock.offset + lSide * lBlock.halfWidth);
                    Handles.color = SELECTED;
                    EditorGUI.BeginChangeCheck();
                    Vector3 lMoved = Handles.Slider(lEdge, lSample.Right * lSide, HandleUtility.GetHandleSize(lEdge) * 1.2f, Handles.ArrowHandleCap, 0f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Record("Block width");
                        float lDelta = Vector3.Dot(lMoved - lEdge, lSample.Right * lSide);
                        lBlock.halfWidth = Mathf.Max(5f, lBlock.halfWidth + lDelta * 0.5f);
                        lBlock.offset += lSide * lDelta * 0.5f;
                        Changed();
                    }
                }
                Vector3 lTop = _layout.field.Point(lMid, lBlock.offset);
                Handles.color = Color.white;
                EditorGUI.BeginChangeCheck();
                Vector3 lDropped = Handles.Slider(lTop, -lSample.up, HandleUtility.GetHandleSize(lTop) * 1.2f, Handles.ArrowHandleCap, 0f);
                if (EditorGUI.EndChangeCheck())
                {
                    Record("Block drop");
                    lBlock.drop += Vector3.Dot(lDropped - lTop, -lSample.up);
                    Changed();
                }
            }
            if (pEvent.type == EventType.MouseDown && pEvent.button == 0 && !pEvent.alt && GUIUtility.hotControl == 0 && _hasHover)
            {
                for (int i = Data.blocks.Count - 1; i >= 0; i--)
                {
                    if (_hoverS < Data.blocks[i].s0 || _hoverS > Data.blocks[i].s1) continue;
                    _block = i;
                    _tab = Tab.Blocks;
                    Repaint();
                    pEvent.Use();
                    break;
                }
            }
        }

        private void PaintTool(Event pEvent, int pControl)
        {
            if (pEvent.button != 0 || pEvent.alt) return;
            if (pEvent.type == EventType.MouseDown && _hasHover)
            {
                _painting = true;
                GUIUtility.hotControl = pControl;
                PaintAt(_hoverS, _hoverLateral, true);
                pEvent.Use();
            }
            else if (pEvent.type == EventType.MouseDrag && _painting)
            {
                if (_hasHover) PaintAt(_hoverS, _hoverLateral, false);
                pEvent.Use();
            }
            else if (pEvent.type == EventType.MouseUp && _painting)
            {
                _painting = false;
                GUIUtility.hotControl = 0;
                pEvent.Use();
            }
        }

        private void EraseTool(Event pEvent, int pControl)
        {
            if (pEvent.button != 0 || pEvent.alt) return;
            if (pEvent.type == EventType.MouseDown && _hasHover)
            {
                _painting = true;
                GUIUtility.hotControl = pControl;
                EraseAt(_hoverPoint, true);
                pEvent.Use();
            }
            else if (pEvent.type == EventType.MouseDrag && _painting)
            {
                if (_hasHover) EraseAt(_hoverPoint, false);
                pEvent.Use();
            }
            else if (pEvent.type == EventType.MouseUp && _painting)
            {
                _painting = false;
                GUIUtility.hotControl = 0;
                pEvent.Use();
            }
        }

        private void PatternTool(Event pEvent, int pControl)
        {
            if (pEvent.type != EventType.MouseDown || pEvent.button != 0 || pEvent.alt || !_hasHover) return;
            if (_patternClicks == 0 || _patternS1 > _patternS0 && _patternClicks >= 2)
            {
                _patternS0 = _hoverS;
                _patternL0 = _hoverLateral;
                _patternS1 = -1f;
                _patternClicks = 1;
            }
            else
            {
                float lS = _hoverS, lL = _hoverLateral;
                if (lS < _patternS0) { _patternS1 = _patternS0; _patternL1 = _patternL0; _patternS0 = lS; _patternL0 = lL; }
                else { _patternS1 = lS; _patternL1 = lL; }
                _patternClicks = 2;
            }
            _tab = Tab.Place;
            Repaint();
            pEvent.Use();
        }

        private void SceneKeys(Event pEvent)
        {
            if (pEvent.type != EventType.KeyDown) return;
            switch (pEvent.keyCode)
            {
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    if (_selected.Count > 0) { DeleteSelected(); pEvent.Use(); }
                    break;
                case KeyCode.D:
                    if ((pEvent.control || pEvent.command) && _selected.Count > 0) { DuplicateSelected(_duplicateOffset); pEvent.Use(); }
                    break;
                case KeyCode.F:
                    if (_selected.Count > 0) { FrameSelection(); pEvent.Use(); }
                    else if (_segment >= 0 && _tool == Tool.Track) { FrameS(_segmentStarts[_segment], _segmentStarts[_segment] + Data.segments[_segment].Length); pEvent.Use(); }
                    break;
                case KeyCode.Escape:
                    _tool = Tool.None;
                    _boxing = false;
                    Repaint();
                    pEvent.Use();
                    break;
            }
        }
    }
}
