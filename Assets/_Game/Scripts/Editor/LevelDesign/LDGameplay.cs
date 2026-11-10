using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // Gameplay data : finish, final sprint, hints, autopilot guidance, and a validation pass.
    public partial class LevelDesignWindow
    {
        private Vector2 _hintScroll;
        private Vector2 _actScroll;
        private readonly List<string> _report = new List<string>();

        private void DrawGameplayTab()
        {
            EditorGUILayout.LabelField("Race", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            float lFinish = EditorGUILayout.FloatField("Finish line (s)", Data.finishS);
            float lClimax = EditorGUILayout.FloatField("Final sprint from (s)", Data.climaxS);
            float lSpawn = EditorGUILayout.FloatField("Spawn (s, on the plaza)", Data.spawnS);
            int lSeed = EditorGUILayout.IntField("Scenery seed", Data.seed);
            if (EditorGUI.EndChangeCheck())
            {
                Record("Edit race");
                Data.finishS = Mathf.Clamp(lFinish, 100f, Data.EndS - 20f);
                Data.climaxS = lClimax;
                Data.spawnS = Mathf.Clamp(lSpawn, -StageData.PLAZA + 20f, -10f);
                Data.seed = lSeed;
                Changed();
            }
            if (_segment >= 0)
            {
                float lEnd = SegStart(_segment) + Data.segments[_segment].Length;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Finish at end of piece " + _segment)) { Record("Move finish"); Data.finishS = Mathf.Min(lEnd, Data.EndS - 20f); Changed(); }
                if (GUILayout.Button("Sprint from piece " + _segment)) { Record("Move sprint"); Data.climaxS = SegStart(_segment); Changed(); }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hints on screen", EditorStyles.boldLabel);
            _hintScroll = EditorGUILayout.BeginScrollView(_hintScroll, GUILayout.Height(110f));
            for (int i = 0; i < Data.hints.Count; i++)
            {
                HintData lHint = Data.hints[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                float lS = EditorGUILayout.FloatField(lHint.s, GUILayout.Width(70f));
                string lText = EditorGUILayout.TextField(lHint.text);
                if (EditorGUI.EndChangeCheck()) { Record("Edit hint"); lHint.s = lS; lHint.text = lText; Changed(false); }
                if (GUILayout.Button("x", GUILayout.Width(22f))) { Record("Delete hint"); Data.hints.RemoveAt(i); Changed(false); break; }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Add hint" + (_segment >= 0 ? " at piece " + _segment : "")))
            {
                Record("Add hint");
                Data.hints.Add(new HintData { s = _segment >= 0 ? SegStart(_segment) : 0f, text = "NEW HINT" });
                Changed(false);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Autopilot and rival guidance", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Route : " + Data.route.Count + " spans    Actions : " + Data.acts.Count, EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Route follows the blocks")) RouteFromBlocks();
            if (GUILayout.Button("Clear actions")) { Record("Clear actions"); Data.acts.Clear(); Changed(false); }
            EditorGUILayout.EndHorizontal();
            _actScroll = EditorGUILayout.BeginScrollView(_actScroll, GUILayout.Height(110f));
            for (int i = 0; i < Data.acts.Count; i++)
            {
                ActData lAct = Data.acts[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                float lS = EditorGUILayout.FloatField(lAct.s, GUILayout.Width(70f));
                RouteAction lAction = (RouteAction)EditorGUILayout.EnumPopup(lAct.action);
                if (EditorGUI.EndChangeCheck()) { Record("Edit action"); lAct.s = lS; lAct.action = lAction; Changed(false); }
                if (GUILayout.Button("x", GUILayout.Width(22f))) { Record("Delete action"); Data.acts.RemoveAt(i); Changed(false); break; }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginHorizontal();
            float lAt = _segment >= 0 ? SegStart(_segment) : 0f;
            if (GUILayout.Button("+ Jump")) AddAct(lAt, RouteAction.Jump);
            if (GUILayout.Button("+ Dash")) AddAct(lAt, RouteAction.Dash);
            if (GUILayout.Button("+ Dive")) AddAct(lAt, RouteAction.Dive);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            if (GUILayout.Button("Check the stage", GUILayout.Height(26f))) Validate();
            foreach (string lLine in _report) EditorGUILayout.HelpBox(lLine, lLine.StartsWith("OK") ? MessageType.Info : MessageType.Warning);
        }

        private void AddAct(float pS, RouteAction pAction)
        {
            Record("Add action");
            Data.acts.Add(new ActData { s = pS, action = pAction });
            Data.acts.Sort((pA, pB) => pA.s.CompareTo(pB.s));
            Changed(false);
        }

        // The autopilot and the rival aim for the middle of each block.
        private void RouteFromBlocks()
        {
            Record("Route from blocks");
            Data.route.Clear();
            foreach (BlockData lBlock in Data.blocks)
                if (lBlock.s1 > 0f) Data.route.Add(new RouteData { s0 = Mathf.Max(0f, lBlock.s0), s1 = lBlock.s1, lateral = lBlock.offset });
            Changed(false);
        }

        private void Validate()
        {
            _report.Clear();
            if (_layout == null) return;
            // Holes.
            List<BlockData> lBlocks = new List<BlockData>(Data.blocks);
            lBlocks.Sort((pA, pB) => pA.s0.CompareTo(pB.s0));
            float lCovered = -StageData.PLAZA;
            foreach (BlockData lBlock in lBlocks)
            {
                if (lBlock.s0 > lCovered + Cruise * 1.8f && lCovered > 0f && lCovered < Data.finishS)
                    _report.Add("Long hole of " + (lBlock.s0 - lCovered).ToString("F0") + " m at s " + lCovered.ToString("F0") + " : hard to cross at " + Cruise.ToString("F0") + " m/s.");
                lCovered = Mathf.Max(lCovered, lBlock.s1);
            }
            if (lCovered < Data.finishS + 50f) _report.Add("No ground after s " + lCovered.ToString("F0") + " : the finish line at " + Data.finishS.ToString("F0") + " floats in the void.");
            // Objects.
            int lOnHole = 0, lAfter = 0, lBadSpan = 0;
            foreach (ItemData lItem in Data.items)
            {
                if (lItem.s > Data.finishS && lItem.kind != ItemKind.Arch) lAfter++;
                if ((lItem.kind == ItemKind.Pad || lItem.kind == ItemKind.Strip || lItem.kind == ItemKind.Crate || lItem.kind == ItemKind.Pane) && !_layout.field.Solid(lItem.s)) lOnHole++;
                if (LDItems.Spans(lItem.kind) && lItem.s1 <= lItem.s) lBadSpan++;
            }
            if (lOnHole > 0) _report.Add(lOnHole + " pads, strips, crates or panes stand over a hole (pads there are skipped).");
            if (lAfter > 0) _report.Add(lAfter + " objects are after the finish line.");
            if (lBadSpan > 0) _report.Add(lBadSpan + " ropes or bridges end before they start.");
            // Gravity sections need a gate before them.
            for (int i = 0; i < Data.segments.Count; i++)
            {
                if (!Data.segments[i].localGravity || (i > 0 && Data.segments[i - 1].localGravity)) continue;
                float lStart = SegStart(i);
                bool lGate = false;
                foreach (ItemData lItem in Data.items) if (lItem.kind == ItemKind.GravityGate && lItem.s > lStart - Cruise * 2.5f && lItem.s <= lStart + 10f) lGate = true;
                if (!lGate) _report.Add("Piece " + i + " holds gravity but has no gravity gate just before it (place one so players see it coming).");
            }
            // The line crossing itself.
            Spine lSpine = _layout.spine;
            int lStep = 20;
            for (int a = 0; a < lSpine.Count && _report.Count < 30; a += lStep)
                for (int b = a + 400; b < lSpine.Count; b += lStep)
                {
                    float lMin = _layout.field.TotalHalfWidth(lSpine.samples[a].s) + _layout.field.TotalHalfWidth(lSpine.samples[b].s);
                    if ((lSpine.samples[a].position - lSpine.samples[b].position).sqrMagnitude < lMin * lMin * 0.25f)
                    {
                        _report.Add("The track passes through itself around s " + lSpine.samples[a].s.ToString("F0") + " and s " + lSpine.samples[b].s.ToString("F0") + ".");
                        b = lSpine.Count;
                        a += 400;
                    }
                }
            if (_report.Count == 0) _report.Add("OK : no problem found.");
        }

        private void DrawHelpTab()
        {
            EditorGUILayout.HelpBox(
                "LEVEL DESIGN - HOW IT WORKS\n\n" +
                "Each stage is an asset in Assets/_Game/Resources/Stages. When it exists and 'Used in game' is on, the game builds the stage from it. " +
                "Bake a stage from its recipe once, then edit it freely. Everything is undoable (Ctrl+Z).\n\n" +
                "Positions are distances along the racing line (s, in metres) and a sideways offset (lateral). Bending or stretching the road carries every object with it.\n\n" +
                "TRACK : the line is a chain of pieces. Each piece has a length and the turn, climb and bank it adds. Select one in the Scene, drag its handles, or insert presets.\n" +
                "BLOCKS : the ground under the line. Width, offset, drop, tilt, walls. Holes between blocks are jumps.\n" +
                "OBJECTS : pick, box-select, move, duplicate, delete, edit many at once.\n" +
                "PLACE : paint objects along the track, erase with a brush, or lay patterns (lines, rows, walls, scatter, slalom, grid).\n" +
                "GAMEPLAY : finish line, final sprint, hints, autopilot actions, and a check of the stage.\n\n" +
                "SCENE SHORTCUTS\n" +
                "Click : select    Shift-click : add    Ctrl-click : toggle    Drag : box select\n" +
                "Del : delete    Ctrl+D : duplicate    F : frame selection    Esc : drop the tool\n\n" +
                "Play runs the stage straight away (Autopilot drives it for you). Preview builds the real meshes in the Scene; Auto rebuilds them after each edit.",
                MessageType.None);
        }
    }
}
