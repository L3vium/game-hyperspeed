using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // Track and blocks : the shape of the road.
    public partial class LevelDesignWindow
    {
        private Vector2 _segmentScroll;
        private Vector2 _blockScroll;

        private float Cruise => _profile != null ? _profile.cruise : 250f;

        private float SegStart(int pIndex) => Data.SegmentStart(pIndex);

        // ================================================================ track tab

        private void DrawTrackTab()
        {
            EditorGUILayout.BeginHorizontal();
            ToolButton(Tool.Track, "Edit line in the Scene");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Click a piece of the line in the Scene. Drag the arrow to change its length, the yellow ring to turn, the red ring to climb, the blue ring to bank. Everything placed after it follows.", MessageType.None);

            if (_segment >= 0 && _segment < Data.segments.Count)
            {
                SegmentData lSegment = Data.segments[_segment];
                float lStart = SegStart(_segment);
                EditorGUILayout.LabelField("Piece " + _segment + "  (" + lSegment.label + ")   s " + lStart.ToString("F0") + " to " + (lStart + lSegment.Length).ToString("F0"), EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                string lLabel = EditorGUILayout.TextField("Name", lSegment.label);
                float lLength = EditorGUILayout.FloatField("Length (m)", lSegment.Length);
                float lYaw = EditorGUILayout.FloatField("Turn (deg, + right)", lSegment.yaw);
                float lPitch = EditorGUILayout.FloatField("Climb (deg, + up)", lSegment.pitch);
                float lRoll = EditorGUILayout.FloatField("Bank (deg, + right)", lSegment.roll);
                float lLinear = EditorGUILayout.Slider("Even curve", lSegment.linear, 0f, 1f);
                bool lGravity = EditorGUILayout.Toggle("Track holds gravity", lSegment.localGravity);
                Vector3 lShift = EditorGUILayout.Vector3Field("Shift (right, up, fwd)", lSegment.shift);
                if (EditorGUI.EndChangeCheck())
                {
                    Record("Edit track piece");
                    lSegment.label = lLabel;
                    lSegment.yaw = lYaw;
                    lSegment.pitch = lPitch;
                    lSegment.roll = lRoll;
                    lSegment.linear = lLinear;
                    lSegment.localGravity = lGravity;
                    lSegment.shift = lShift;
                    SetLength(_segment, lLength);
                    Changed();
                }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Frame")) FrameS(lStart, lStart + lSegment.Length);
                if (GUILayout.Button("Split")) SplitSegment(_segment);
                if (GUILayout.Button("Duplicate")) InsertSegments(_segment + 1, new List<SegmentData> { lSegment.Clone() }, "Duplicate piece");
                if (GUILayout.Button("Straighten")) { Record("Straighten"); lSegment.yaw = lSegment.pitch = lSegment.roll = 0f; lSegment.shift = Vector3.zero; Changed(); }
                if (GUILayout.Button("Delete")) DeleteSegment(_segment);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("< Previous") && _segment > 0) _segment--;
                if (GUILayout.Button("Next >") && _segment < Data.segments.Count - 1) _segment++;
                EditorGUILayout.EndHorizontal();
            }
            else EditorGUILayout.LabelField("No piece selected.", EditorStyles.miniLabel);

            EditorGUILayout.Space();
            int lAt = _segment >= 0 ? _segment + 1 : Mathf.Max(0, Data.segments.Count - 2);
            EditorGUILayout.LabelField("Insert after " + (_segment >= 0 ? "piece " + _segment : "the last stretch"), EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Straight")) InsertPreset(lAt, "Straight");
            if (GUILayout.Button("Turn L")) InsertPreset(lAt, "TurnL");
            if (GUILayout.Button("Turn R")) InsertPreset(lAt, "TurnR");
            if (GUILayout.Button("Big turn L")) InsertPreset(lAt, "BigL");
            if (GUILayout.Button("Big turn R")) InsertPreset(lAt, "BigR");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Hill")) InsertPreset(lAt, "Hill");
            if (GUILayout.Button("Dip")) InsertPreset(lAt, "Dip");
            if (GUILayout.Button("Climb")) InsertPreset(lAt, "Climb");
            if (GUILayout.Button("Descent")) InsertPreset(lAt, "Descent");
            if (GUILayout.Button("S-bend")) InsertPreset(lAt, "SBend");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Loop L")) InsertPreset(lAt, "LoopL");
            if (GUILayout.Button("Loop R")) InsertPreset(lAt, "LoopR");
            if (GUILayout.Button("Twist")) InsertPreset(lAt, "Twist");
            if (GUILayout.Button("Wall turn L")) InsertPreset(lAt, "WallL");
            if (GUILayout.Button("Wall turn R")) InsertPreset(lAt, "WallR");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pieces", EditorStyles.boldLabel);
            _segmentScroll = EditorGUILayout.BeginScrollView(_segmentScroll, GUILayout.Height(260f));
            float lS = 0f;
            for (int i = 0; i < Data.segments.Count; i++)
            {
                SegmentData lSegment = Data.segments[i];
                string lText = i + "  " + lSegment.label + "   " + lSegment.Length.ToString("F0") + " m"
                    + (Mathf.Abs(lSegment.yaw) > 0.1f ? "  turn " + lSegment.yaw.ToString("F0") : "")
                    + (Mathf.Abs(lSegment.pitch) > 0.1f ? "  climb " + lSegment.pitch.ToString("F0") : "")
                    + (Mathf.Abs(lSegment.roll) > 0.1f ? "  bank " + lSegment.roll.ToString("F0") : "")
                    + (lSegment.localGravity ? "  [gravity]" : "") + (lS > Data.finishS ? "  (after finish)" : "");
                GUIStyle lStyle = i == _segment ? EditorStyles.whiteBoldLabel : EditorStyles.label;
                Color lOld = GUI.backgroundColor;
                if (i == _segment) GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);
                if (GUILayout.Button(lText, EditorStyles.miniButton)) { _segment = i; _tool = Tool.Track; FrameS(lS, lS + lSegment.Length); }
                GUI.backgroundColor = lOld;
                lS += lSegment.Length;
            }
            EditorGUILayout.EndScrollView();
        }

        // ================================================================ track edits

        private void SetLength(int pIndex, float pLength)
        {
            SegmentData lSegment = Data.segments[pIndex];
            float lStart = SegStart(pIndex);
            float lOld = lSegment.Length;
            lSegment.Length = Mathf.Max(Spine.STEP, pLength);
            Data.Reflow(lStart, lStart + lOld, lStart + lSegment.Length);
        }

        private void InsertSegments(int pIndex, List<SegmentData> pSegments, string pWhat)
        {
            Record(pWhat);
            pIndex = Mathf.Clamp(pIndex, 0, Data.segments.Count);
            float lAt = SegStart(pIndex);
            float lLength = 0f;
            foreach (SegmentData lSegment in pSegments) lLength += lSegment.Length;
            Data.segments.InsertRange(pIndex, pSegments);
            Data.Reflow(lAt, lAt, lAt + lLength);
            // New road needs ground : stretch the block that ended right there if nothing covers it.
            bool lCovered = false;
            foreach (BlockData lBlock in Data.blocks) if (lBlock.s0 <= lAt + 1f && lBlock.s1 >= lAt + lLength - 1f) lCovered = true;
            if (!lCovered)
            {
                BlockData lBefore = null;
                foreach (BlockData lBlock in Data.blocks) if (lBlock.s1 <= lAt + 1f && (lBefore == null || lBlock.s1 > lBefore.s1)) lBefore = lBlock;
                if (lBefore != null) lBefore.s1 = Mathf.Max(lBefore.s1, lAt + lLength);
            }
            _segment = pIndex;
            Changed();
        }

        private void DeleteSegment(int pIndex)
        {
            if (Data.segments.Count <= 1) return;
            Record("Delete track piece");
            float lStart = SegStart(pIndex);
            float lEnd = lStart + Data.segments[pIndex].Length;
            Data.items.RemoveAll(pItem => pItem.s > lStart && pItem.s < lEnd);
            Data.segments.RemoveAt(pIndex);
            Data.Reflow(lStart, lEnd, lStart);
            Data.blocks.RemoveAll(pBlock => pBlock.s1 - pBlock.s0 < Spine.STEP);
            _selected.Clear();
            _segment = Mathf.Min(pIndex, Data.segments.Count - 1);
            Changed();
        }

        private void SplitSegment(int pIndex)
        {
            Record("Split track piece");
            SegmentData lA = Data.segments[pIndex];
            if (lA.steps < 2) return;
            SegmentData lB = lA.Clone();
            lB.steps = lA.steps / 2;
            lA.steps -= lB.steps;
            lA.yaw *= 0.5f; lB.yaw = lA.yaw;
            lA.pitch *= 0.5f; lB.pitch = lA.pitch;
            lA.roll *= 0.5f; lB.roll = lA.roll;
            lA.shift *= 0.5f; lB.shift = lA.shift;
            Data.segments.Insert(pIndex + 1, lB);
            Changed();
        }

        private SegmentData Seg(string pLabel, float pLength, float pYaw = 0f, float pPitch = 0f, float pRoll = 0f, bool pGravity = false, float pLinear = 0f, Vector3 pShift = default(Vector3))
        {
            SegmentData lSegment = new SegmentData { label = pLabel, yaw = pYaw, pitch = pPitch, roll = pRoll, localGravity = pGravity, linear = pLinear, shift = pShift };
            lSegment.Length = pLength;
            return lSegment;
        }

        // Ready-made pieces, sized for the speed of this stage (same rules as the recipes).
        private void InsertPreset(int pIndex, string pPreset)
        {
            float lC = Cruise;
            float lAt = SegStart(Mathf.Clamp(pIndex, 0, Data.segments.Count));
            float lHalf = _layout != null ? _layout.field.HalfWidth(lAt) : 300f;
            float lRadius = Mathf.Max(Mathf.Max(lHalf * 2.4f + 30f, lC * 3f), 950f);
            List<SegmentData> lList = new List<SegmentData>();
            float lGate = -1f;
            switch (pPreset)
            {
                case "Straight": lList.Add(Seg("Straight", lC * 2f)); break;
                case "TurnL":
                case "TurnR":
                case "BigL":
                case "BigR":
                case "WallL":
                case "WallR":
                {
                    int lSide = pPreset.EndsWith("R") ? 1 : -1;
                    bool lBig = pPreset.StartsWith("Big");
                    bool lWall = pPreset.StartsWith("Wall");
                    float lDegrees = lBig ? 100f : lWall ? 110f : 60f;
                    float lBank = lBig ? 18f : lWall ? 74f : 28f;
                    float lLength = lRadius * (lBig ? 1.7f : 1f) * lDegrees * Mathf.Deg2Rad;
                    string lName = lBig ? "BigTurn" : lWall ? "WallTurn" : "Turn";
                    lList.Add(Seg(lName, lLength * 0.25f, lSide * lDegrees * 0.15f, 0f, lSide * lBank, lWall));
                    lList.Add(Seg(lName, lLength * 0.5f, lSide * lDegrees * 0.7f, 0f, 0f, lWall));
                    lList.Add(Seg(lName, lLength * 0.25f, lSide * lDegrees * 0.15f, 0f, -lSide * lBank, lWall));
                    break;
                }
                case "SBend":
                    lList.Add(Seg("SBend", lRadius * 0.7f, -40f));
                    lList.Add(Seg("SBend", lRadius * 0.7f, 40f));
                    break;
                case "Hill":
                case "Dip":
                {
                    float lSign = pPreset == "Hill" ? 1f : -1f;
                    lList.Add(Seg(pPreset, lC * 0.8f, 0f, 12f * lSign));
                    lList.Add(Seg(pPreset, lC * 1.6f, 0f, -24f * lSign));
                    lList.Add(Seg(pPreset, lC * 0.8f, 0f, 12f * lSign));
                    break;
                }
                case "Climb":
                case "Descent":
                {
                    float lSign = pPreset == "Climb" ? 1f : -1f;
                    lList.Add(Seg(pPreset, lC * 0.75f, 0f, 18f * lSign));
                    lList.Add(Seg(pPreset, lC * 1.5f));
                    lList.Add(Seg(pPreset, lC * 0.75f, 0f, -18f * lSign));
                    break;
                }
                case "LoopL":
                case "LoopR":
                {
                    int lSide = pPreset == "LoopR" ? 1 : -1;
                    float lLoopRadius = Mathf.Max(lC * 1.5f, lHalf * 1.9f);
                    lList.Add(Seg("Loop", lC * 1.5f));
                    lList.Add(Seg("Loop", lLoopRadius * Mathf.PI * 2f * 1.08f, 0f, 360f, 0f, true, 0.55f, new Vector3(lSide * (lHalf * 2.6f + 140f), 0f, 0f)));
                    lList.Add(Seg("Loop", lC * 1.2f));
                    lGate = lAt + lC * 1.2f;
                    break;
                }
                case "Twist":
                    lList.Add(Seg("Twist", lC * 0.9f));
                    lList.Add(Seg("Twist", lC * 3.2f * 1.4f, 0f, 0f, 360f, true, 0.5f));
                    lList.Add(Seg("Twist", lC * 0.3f));
                    lGate = lAt + lC * 0.6f;
                    break;
            }
            if (lList.Count == 0) return;
            InsertSegments(pIndex, lList, "Insert " + pPreset);
            if (lGate > 0f)
            {
                Data.items.Add(new ItemData { kind = ItemKind.GravityGate, s = lGate });
                Changed();
            }
        }

        // ================================================================ blocks tab

        private void DrawBlocksTab()
        {
            ToolButton(Tool.Blocks, "Pick blocks in the Scene");
            EditorGUILayout.HelpBox("A block is the solid ground under a stretch of line. Gaps between blocks are holes to jump. Click a block in the Scene, then drag its edges (width) or the white arrow (drop).", MessageType.None);
            if (_block >= 0 && _block < Data.blocks.Count)
            {
                BlockData lBlock = Data.blocks[_block];
                EditorGUILayout.LabelField("Block " + _block, EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                BlockLook lLook = (BlockLook)EditorGUILayout.EnumPopup("Look", lBlock.look);
                float lS0 = EditorGUILayout.FloatField("Starts at s", lBlock.s0);
                float lS1 = EditorGUILayout.FloatField("Ends at s", lBlock.s1);
                float lHalf = EditorGUILayout.FloatField("Half width", lBlock.halfWidth);
                float lOffset = EditorGUILayout.FloatField("Sideways offset", lBlock.offset);
                float lDrop = EditorGUILayout.FloatField("Drop (below line)", lBlock.drop);
                float lTilt = EditorGUILayout.Slider("Tilt (deg)", lBlock.tilt, -40f, 40f);
                float lThick = EditorGUILayout.FloatField("Thickness", lBlock.thickness);
                bool lMountain = EditorGUILayout.Toggle("Mountain sides", lBlock.mountain);
                float lLeft = EditorGUILayout.FloatField("Left wall height", lBlock.wallLeft);
                float lRight = EditorGUILayout.FloatField("Right wall height", lBlock.wallRight);
                float lLean = EditorGUILayout.Slider("Wall lean", lBlock.wallLean, 0f, 70f);
                if (EditorGUI.EndChangeCheck())
                {
                    Record("Edit block");
                    lBlock.look = lLook;
                    lBlock.s0 = lS0;
                    lBlock.s1 = Mathf.Max(lS0 + Spine.STEP, lS1);
                    lBlock.halfWidth = Mathf.Max(5f, lHalf);
                    lBlock.offset = lOffset;
                    lBlock.drop = lDrop;
                    lBlock.tilt = lTilt;
                    lBlock.thickness = Mathf.Max(2f, lThick);
                    lBlock.mountain = lMountain;
                    lBlock.wallLeft = Mathf.Max(0f, lLeft);
                    lBlock.wallRight = Mathf.Max(0f, lRight);
                    lBlock.wallLean = lLean;
                    Changed();
                }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Frame")) FrameS(lBlock.s0, lBlock.s1);
                if (GUILayout.Button("Split in two")) SplitBlock(_block);
                if (GUILayout.Button("Cut a gap")) CutGap(_block);
                if (GUILayout.Button("Delete (hole)")) { Record("Delete block"); Data.blocks.RemoveAt(_block); _block = -1; Changed(); }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Extend to next block")) ExtendBlock(_block);
                if (GUILayout.Button("Width to all blocks")) { Record("Width to all"); foreach (BlockData lOther in Data.blocks) if (lOther.look != BlockLook.Plaza) lOther.halfWidth = lBlock.halfWidth; Changed(); }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Blocks", EditorStyles.boldLabel);
            _blockScroll = EditorGUILayout.BeginScrollView(_blockScroll, GUILayout.Height(240f));
            float lPrevious = float.MinValue;
            for (int i = 0; i < Data.blocks.Count; i++)
            {
                BlockData lBlock = Data.blocks[i];
                if (lPrevious > float.MinValue && lBlock.s0 > lPrevious + Spine.STEP)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("   gap " + (lBlock.s0 - lPrevious).ToString("F0") + " m", EditorStyles.miniLabel);
                    if (GUILayout.Button("Fill", EditorStyles.miniButton, GUILayout.Width(40f))) FillGap(lPrevious, lBlock.s0, i);
                    EditorGUILayout.EndHorizontal();
                }
                Color lOld = GUI.backgroundColor;
                if (i == _block) GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);
                if (GUILayout.Button(i + "  " + lBlock.look + "   " + lBlock.s0.ToString("F0") + " to " + lBlock.s1.ToString("F0") + "   width " + (lBlock.halfWidth * 2f).ToString("F0")
                    + (Mathf.Abs(lBlock.tilt) > 0.5f ? "  tilt " + lBlock.tilt.ToString("F0") : "") + (lBlock.drop > 0.5f ? "  drop " + lBlock.drop.ToString("F0") : ""), EditorStyles.miniButton))
                {
                    _block = i;
                    _tool = Tool.Blocks;
                    FrameS(lBlock.s0, lBlock.s1);
                }
                GUI.backgroundColor = lOld;
                lPrevious = Mathf.Max(lPrevious, lBlock.s1);
            }
            EditorGUILayout.EndScrollView();
        }

        private void SplitBlock(int pIndex)
        {
            Record("Split block");
            BlockData lA = Data.blocks[pIndex];
            BlockData lB = lA.Clone();
            float lMid = (lA.s0 + lA.s1) * 0.5f;
            lA.s1 = lMid;
            lB.s0 = lMid;
            lB.seed = Data.blocks.Count;
            Data.blocks.Insert(pIndex + 1, lB);
            Changed();
        }

        // A hole in the middle third of the block : something to jump.
        private void CutGap(int pIndex)
        {
            Record("Cut a gap");
            BlockData lA = Data.blocks[pIndex];
            BlockData lB = lA.Clone();
            float lLength = lA.s1 - lA.s0;
            float lGap = Mathf.Min(lLength * 0.3f, Cruise * 0.2f);
            float lMid = (lA.s0 + lA.s1) * 0.5f;
            lA.s1 = lMid - lGap * 0.5f;
            lB.s0 = lMid + lGap * 0.5f;
            lB.drop += 10f;
            lB.seed = Data.blocks.Count;
            Data.blocks.Insert(pIndex + 1, lB);
            Data.acts.Add(new ActData { s = lA.s1 - Cruise * 0.12f, action = RouteAction.Jump });
            Changed();
        }

        private void ExtendBlock(int pIndex)
        {
            BlockData lBlock = Data.blocks[pIndex];
            float lNext = float.MaxValue;
            foreach (BlockData lOther in Data.blocks) if (lOther != lBlock && lOther.s0 >= lBlock.s1 - 0.5f) lNext = Mathf.Min(lNext, lOther.s0);
            if (lNext == float.MaxValue) lNext = Data.EndS;
            Record("Extend block");
            lBlock.s1 = lNext;
            Changed();
        }

        private void FillGap(float pFrom, float pTo, int pBefore)
        {
            Record("Fill gap");
            BlockData lTemplate = Data.blocks[Mathf.Max(0, pBefore - 1)].Clone();
            lTemplate.s0 = pFrom;
            lTemplate.s1 = pTo;
            lTemplate.seed = Data.blocks.Count;
            Data.blocks.Insert(pBefore, lTemplate);
            Changed();
        }
    }
}
