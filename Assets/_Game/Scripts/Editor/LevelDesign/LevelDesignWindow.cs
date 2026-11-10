using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // Level design for Hyperspeed : every stage as editable data, shown in the Scene view.
    //  - Track   : the racing line as pieces. Click a piece, drag its handles (length, turn, climb, bank), insert presets.
    //  - Blocks  : the solid ground under the line (width, drop, tilt, walls, gaps).
    //  - Objects : click / box-select / move / duplicate / delete anything on the track, edit many at once.
    //  - Place   : paint, erase, and patterns (line, rows, walls, scatter, slalom) of any object.
    //  - Gameplay: finish, climax, hints, autopilot guidance, validation, play the stage.
    // Everything is saved in Resources/Stages/stage_N, which the game builds from.
    public partial class LevelDesignWindow : EditorWindow
    {
        private enum Tab { Track, Blocks, Objects, Place, Gameplay, Help }
        private enum Tool { None, Select, Track, Blocks, Paint, Erase, Pattern }
        private enum Pattern { Line, Rows, Wall, Scatter, Slalom, Grid }

        private const string PREVIEW_NAME = "LD_Preview";
        private const string ASSET_FOLDER = "Assets/_Game/Resources/Stages";
        private static readonly string[] TAB_NAMES = { "Track", "Blocks", "Objects", "Place", "Gameplay", "Help" };

        [SerializeField] private int _stage;
        [SerializeField] private Tab _tab = Tab.Track;
        [SerializeField] private bool _autoPreview = true;
        [SerializeField] private bool _showEdges = true;
        [SerializeField] private bool _showLabels = true;
        [SerializeField] private bool _playAutopilot;

        private StageAsset _asset;
        private LevelLayout _layout;
        private SpeedProfile _profile;
        private Theme _theme;
        private Tool _tool = Tool.Select;
        private Vector2 _scroll;
        private double _previewAt = -1;
        private GameObject _preview;

        // Selection, as indices into the data (they survive undo).
        private readonly List<int> _selected = new List<int>();
        private int _segment = -1;
        private int _block = -1;
        private readonly bool[] _visible = new bool[LDItems.KINDS.Length];

        // Scene caches.
        private Vector3[][] _segmentLines;
        private float[] _segmentStarts;
        private readonly List<Vector3[]> _edges = new List<Vector3[]>();
        private Vector3[] _itemPositions;

        StageData Data => _asset != null ? _asset.data : null;

        [MenuItem("Hyperspeed/Level Design %#l")]
        public static void Open()
        {
            LevelDesignWindow lWindow = GetWindow<LevelDesignWindow>("Level Design");
            lWindow.minSize = new Vector2(360f, 420f);
            lWindow.Show();
        }

        private void OnEnable()
        {
            for (int i = 0; i < _visible.Length; i++) _visible[i] = true;
            SceneView.duringSceneGui += OnScene;
            Undo.undoRedoPerformed += OnUndo;
            EditorApplication.update += OnUpdate;
            LoadStage(_stage, false);
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnScene;
            Undo.undoRedoPerformed -= OnUndo;
            EditorApplication.update -= OnUpdate;
        }

        private void OnUndo()
        {
            if (_asset == null) return;
            ClampSelection();
            Changed();
        }

        private void OnUpdate()
        {
            if (_previewAt > 0 && EditorApplication.timeSinceStartup >= _previewAt && GUIUtility.hotControl == 0)
            {
                _previewAt = -1;
                BuildPreview();
            }
        }

        // ================================================================ stage

        private static string AssetPath(int pIndex) => ASSET_FOLDER + "/stage_" + pIndex + ".asset";

        private void LoadStage(int pIndex, bool pPreview = true)
        {
            _stage = Mathf.Clamp(pIndex, 0, LevelDatabase.Count - 1);
            LevelDefinition lLevel = LevelDatabase.Get(_stage);
            _theme = lLevel.theme();
            _profile = LevelDatabase.Profile(lLevel);
            _asset = AssetDatabase.LoadAssetAtPath<StageAsset>(AssetPath(_stage));
            _selected.Clear();
            _segment = -1;
            _block = -1;
            _layout = null;
            if (_asset != null)
            {
                RebuildLayout();
                if (pPreview && _autoPreview) SchedulePreview(0.05f);
            }
            SceneView.RepaintAll();
            Repaint();
        }

        // The stage's recipe turned into an editable asset (overwrites any previous edit).
        [MenuItem("Hyperspeed/Bake all stages to Level Design assets")]
        public static void BakeAll()
        {
            for (int i = 0; i < LevelDatabase.Count; i++) BakeFromRecipe(i);
            Debug.Log("[HS-LD] baked " + LevelDatabase.Count + " stages into " + ASSET_FOLDER);
        }

        public static void BakeFromRecipe(int pIndex)
        {
            StageData lData = LevelDatabase.Recipe(pIndex, null).Bake();
            if (!AssetDatabase.IsValidFolder(ASSET_FOLDER))
            {
                Directory.CreateDirectory(ASSET_FOLDER);
                AssetDatabase.Refresh();
            }
            string lPath = AssetPath(pIndex);
            StageAsset lAsset = AssetDatabase.LoadAssetAtPath<StageAsset>(lPath);
            if (lAsset == null)
            {
                lAsset = CreateInstance<StageAsset>();
                lAsset.data = lData;
                AssetDatabase.CreateAsset(lAsset, lPath);
            }
            else
            {
                Undo.RecordObject(lAsset, "Bake stage from recipe");
                lAsset.data = lData;
                EditorUtility.SetDirty(lAsset);
            }
            AssetDatabase.SaveAssets();
        }

        private void Changed(bool pPreview = true)
        {
            if (_asset == null) return;
            EditorUtility.SetDirty(_asset);
            RebuildLayout();
            if (pPreview && _autoPreview) SchedulePreview(0.6f);
            SceneView.RepaintAll();
            Repaint();
        }

        private void Record(string pWhat)
        {
            if (_asset != null) Undo.RecordObject(_asset, pWhat);
        }

        private void SchedulePreview(float pDelay) => _previewAt = EditorApplication.timeSinceStartup + pDelay;

        // ================================================================ layout and caches

        private void RebuildLayout()
        {
            if (Data == null || Data.segments.Count == 0) { _layout = null; return; }
            _layout = StageBuilder.BuildLayout(Data, _profile);
            Spine lSpine = _layout.spine;

            int lCount = Data.segments.Count;
            _segmentLines = new Vector3[lCount][];
            _segmentStarts = new float[lCount + 1];
            float lS = 0f;
            for (int i = 0; i < lCount; i++)
            {
                _segmentStarts[i] = lS;
                float lEnd = lS + Data.segments[i].Length;
                List<Vector3> lPoints = new List<Vector3>();
                float lStep = Mathf.Max(Spine.STEP, (lEnd - lS) / 80f);
                for (float lT = lS; lT < lEnd; lT += lStep) lPoints.Add(lSpine.Evaluate(lT).position);
                lPoints.Add(lSpine.Evaluate(lEnd).position);
                _segmentLines[i] = lPoints.ToArray();
                lS = lEnd;
            }
            _segmentStarts[lCount] = lS;

            _edges.Clear();
            foreach (BlockData lBlock in Data.blocks)
            {
                for (int lSide = -1; lSide <= 1; lSide += 2)
                {
                    List<Vector3> lPoints = new List<Vector3>();
                    float lStep = Mathf.Max(8f, (lBlock.s1 - lBlock.s0) / 60f);
                    for (float lT = lBlock.s0; lT <= lBlock.s1; lT += lStep)
                        lPoints.Add(_layout.field.Point(Mathf.Clamp(lT, lBlock.s0 + 0.5f, lBlock.s1 - 0.5f), lBlock.offset + lSide * lBlock.halfWidth));
                    if (lPoints.Count > 1) _edges.Add(lPoints.ToArray());
                }
            }
            RefreshItemPositions();
        }

        private void RefreshItemPositions()
        {
            if (_layout == null || Data == null) return;
            _itemPositions = new Vector3[Data.items.Count];
            for (int i = 0; i < Data.items.Count; i++) _itemPositions[i] = StageBuilder.ItemPosition(Data.items[i], _layout);
        }

        // The data may have changed under us (undo, re-bake, asset edited elsewhere) : caches follow.
        private void Sync()
        {
            if (Data == null) return;
            bool lStale = _layout == null || _segmentLines == null || _segmentLines.Length != Data.segments.Count
                || _itemPositions == null || _itemPositions.Length != Data.items.Count;
            ClampSelection();
            if (lStale) RebuildLayout();
        }

        private void ClampSelection()
        {
            if (Data == null) { _selected.Clear(); return; }
            _selected.RemoveAll(pIndex => pIndex < 0 || pIndex >= Data.items.Count);
            if (_segment >= Data.segments.Count) _segment = -1;
            if (_block >= Data.blocks.Count) _block = -1;
        }

        // ================================================================ preview

        private void BuildPreview()
        {
            ClearPreview();
            if (Data == null) return;
            HashSet<GameObject> lBefore = new HashSet<GameObject>(EditorSceneManager.GetActiveScene().GetRootGameObjects());
            _preview = new GameObject(PREVIEW_NAME);
            Theme lOldTheme = RaceContext.Theme;
            RaceContext.Theme = _theme;
            TrackBaker.Chunks.Clear();
            try
            {
                StageBuilder.Build(Data, _theme, _profile, _preview.transform);
            }
            catch (System.Exception lException)
            {
                Debug.LogException(lException);
            }
            finally
            {
                RaceContext.Theme = lOldTheme;
                TrackBaker.Chunks.Clear();
            }
            // Anything the build left at the root of the scene goes under the preview too.
            foreach (GameObject lRoot in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                if (!lBefore.Contains(lRoot) && lRoot != _preview) lRoot.transform.SetParent(_preview.transform, true);
            foreach (Transform lChild in _preview.GetComponentsInChildren<Transform>(true))
                lChild.gameObject.hideFlags = HideFlags.DontSave;
            SceneView.RepaintAll();
        }

        private void ClearPreview()
        {
            if (_preview == null) _preview = GameObject.Find(PREVIEW_NAME);
            while (_preview != null)
            {
                DestroyImmediate(_preview);
                _preview = GameObject.Find(PREVIEW_NAME);
            }
        }

        // ================================================================ play

        private void PlayStage()
        {
            if (_asset != null) AssetDatabase.SaveAssetIfDirty(_asset);
            ClearPreview();
            EditorPrefs.SetInt(EditorPlayStage.KEY_STAGE, _stage);
            EditorPrefs.SetBool(EditorPlayStage.KEY_AUTOPILOT, _playAutopilot);
            SceneAsset lTitle = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Game/Scenes/title_screen.unity");
            if (lTitle != null) EditorSceneManager.playModeStartScene = lTitle;
            EditorApplication.EnterPlaymode();
        }

        // ================================================================ window

        private void OnGUI()
        {
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Level design is paused while the game runs.", MessageType.Info);
                return;
            }
            Sync();
            DrawHeader();
            if (_asset == null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("This stage is still built from its recipe in LevelDatabase. Bake it to edit it here : the asset becomes what the game plays.", MessageType.Info);
                if (GUILayout.Button("Bake this stage from its recipe", GUILayout.Height(30f)))
                {
                    BakeFromRecipe(_stage);
                    LoadStage(_stage);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Bake ALL stages from their recipes"))
                {
                    if (EditorUtility.DisplayDialog("Bake all stages", "Create editable assets for all " + LevelDatabase.Count + " stages ? Existing assets are overwritten.", "Bake all", "Cancel"))
                    {
                        for (int i = 0; i < LevelDatabase.Count; i++) BakeFromRecipe(i);
                        LoadStage(_stage);
                        GUIUtility.ExitGUI();
                    }
                }
                return;
            }

            _tab = (Tab)GUILayout.Toolbar((int)_tab, TAB_NAMES);
            EditorGUILayout.Space(4f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case Tab.Track: DrawTrackTab(); break;
                case Tab.Blocks: DrawBlocksTab(); break;
                case Tab.Objects: DrawObjectsTab(); break;
                case Tab.Place: DrawPlaceTab(); break;
                case Tab.Gameplay: DrawGameplayTab(); break;
                case Tab.Help: DrawHelpTab(); break;
            }
            EditorGUILayout.EndScrollView();
            HandleShortcuts(Event.current);
        }

        private void DrawHeader()
        {
            string[] lNames = new string[LevelDatabase.Count];
            for (int i = 0; i < lNames.Length; i++) lNames[i] = LevelDatabase.Code(i) + "  " + LevelDatabase.Get(i).title;
            EditorGUILayout.BeginHorizontal();
            int lStage = EditorGUILayout.Popup(_stage, lNames);
            if (lStage != _stage) { LoadStage(lStage); GUIUtility.ExitGUI(); }
            if (GUILayout.Button("Play", GUILayout.Width(50f))) PlayStage();
            _playAutopilot = GUILayout.Toggle(_playAutopilot, "Autopilot", GUILayout.Width(72f));
            EditorGUILayout.EndHorizontal();
            if (_asset == null) return;

            EditorGUILayout.BeginHorizontal();
            bool lUse = GUILayout.Toggle(_asset.useInGame, "Used in game", GUILayout.Width(100f));
            if (lUse != _asset.useInGame)
            {
                Record("Use stage asset");
                _asset.useInGame = lUse;
                EditorUtility.SetDirty(_asset);
            }
            if (GUILayout.Button("Save")) AssetDatabase.SaveAssetIfDirty(_asset);
            if (GUILayout.Button("Preview")) BuildPreview();
            if (GUILayout.Button("Clear")) ClearPreview();
            _autoPreview = GUILayout.Toggle(_autoPreview, "Auto", GUILayout.Width(46f));
            if (GUILayout.Button("Frame")) FrameS(0f, Data.finishS);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _showEdges = GUILayout.Toggle(_showEdges, "Edges", GUILayout.Width(56f));
            _showLabels = GUILayout.Toggle(_showLabels, "Labels", GUILayout.Width(60f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Re-bake from recipe", GUILayout.Width(140f)) && EditorUtility.DisplayDialog("Re-bake", "Throw away every edit of this stage and bake it again from its recipe ?", "Re-bake", "Cancel"))
            {
                BakeFromRecipe(_stage);
                LoadStage(_stage);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Length " + (Data.finishS / 1000f).ToString("F1") + " km   segments " + Data.segments.Count + "   blocks " + Data.blocks.Count + "   objects " + Data.items.Count, EditorStyles.miniLabel);
        }

        private void ToolButton(Tool pTool, string pLabel)
        {
            Color lOld = GUI.backgroundColor;
            if (_tool == pTool) GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button(pLabel)) { _tool = _tool == pTool ? Tool.None : pTool; SceneView.RepaintAll(); }
            GUI.backgroundColor = lOld;
        }

        private void FrameS(float pS0, float pS1)
        {
            if (_layout == null || SceneView.lastActiveSceneView == null) return;
            Bounds lBounds = new Bounds(_layout.spine.Evaluate(pS0).position, Vector3.one * 50f);
            for (float lS = pS0; lS <= pS1; lS += Mathf.Max(20f, (pS1 - pS0) / 40f)) lBounds.Encapsulate(_layout.spine.Evaluate(lS).position);
            lBounds.Encapsulate(_layout.spine.Evaluate(pS1).position);
            SceneView.lastActiveSceneView.Frame(lBounds, false);
        }

        private void HandleShortcuts(Event pEvent)
        {
            if (pEvent.type != EventType.KeyDown) return;
            if (pEvent.keyCode == KeyCode.Delete && _selected.Count > 0) { DeleteSelected(); pEvent.Use(); }
        }
    }
}
