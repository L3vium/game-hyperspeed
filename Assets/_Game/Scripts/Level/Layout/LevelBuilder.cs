using System;
using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum Side { Left = -1, Right = 1 }

    // Fluent level authoring : a cursor walks the spine and modules add decks (platforms), rails, pads and hazards.
    // Objects that need finished deck frames are queued and spawned in Build().
    public partial class LevelBuilder
    {
        public const float MAIN_WIDTH = 22f;
        public const float PLAZA_WIDTH = 32f;
        public const float PRE_START = 220f;
        public const float RUN_OUT = 220f;
        private const float WIDTH_RATE = 0.35f;
        private const float SAFE_SPACING = 40f;

        public readonly LevelLayout layout = new LevelLayout();
        public readonly Transform root;
        public readonly Theme theme;

        private readonly List<Action> _spawns = new List<Action>();
        private readonly List<WorldBreakZone> _zones = new List<WorldBreakZone>();

        private Vector3 _pos;
        private float _yaw;
        private float _s;
        private Deck _main;
        private float _mainWidth = MAIN_WIDTH;
        private float _startWidth = -1f;
        private float _widthRate = WIDTH_RATE;
        private EdgeKind _edges = EdgeKind.Wall;
        private int _nextId;
        private bool _laneSet;
        private int _laneSide = -1;
        private float _laneOffset = 46f;
        private float _laneHeight = -6f;
        private DeckStyle _laneStyle = DeckStyle.Road;
        private bool _hasLane = true;

        public float S => _s;
        public Spine Spine => layout.spine;
        public IReadOnlyList<WorldBreakZone> Zones => _zones;

        public LevelBuilder(Transform pRoot, Theme pTheme)
        {
            root = pRoot;
            theme = pTheme;
            _s = -PRE_START;
            _pos = new Vector3(0f, 0f, -PRE_START);
            AddSample();
            // Lead-in : spine only (chasers start back here, the intro camera flies over it).
            Advance(PRE_START - 60f, 0f, null, false);
        }

        // ---------------------------------------------------------------- spine

        private int Last => layout.spine.Count - 1;

        private void AddSample()
        {
            layout.spine.samples.Add(new SpineSample { position = _pos, yaw = _yaw, s = _s });
        }

        public static float Smooth(float pT) => pT * pT * (3f - 2f * pT);

        public static float Smoother(float pT) => pT * pT * pT * (pT * (6f * pT - 15f) + 10f);

        // Walks the spine forward. pHeight maps t (0..1) to a height offset from the start height.
        // Returns the index of the first new sample.
        private int Advance(float pLength, float pYawDelta, Func<float, float> pHeight, bool pMain, Func<float, float> pBank = null)
        {
            int lSteps = Mathf.Max(1, Mathf.RoundToInt(pLength / Spine.STEP));
            float lYaw0 = _yaw;
            float lH0 = _pos.y;
            int lFirst = layout.spine.Count;
            for (int i = 1; i <= lSteps; i++)
            {
                float lT = i / (float)lSteps;
                float lPrevYaw = _yaw;
                _yaw = lYaw0 + pYawDelta * Smooth(lT);
                float lMid = (lPrevYaw + _yaw) * 0.5f * Mathf.Deg2Rad;
                _pos += new Vector3(Mathf.Sin(lMid), 0f, Mathf.Cos(lMid)) * Spine.STEP;
                _pos.y = lH0 + (pHeight != null ? pHeight(lT) : 0f);
                _s += Spine.STEP;
                AddSample();
            }
            if (pMain)
            {
                ExtendMain(lFirst, Last, pBank);
                Route(layout.spine.SOf(lFirst) - Spine.STEP, _s, _main);
            }
            return lFirst;
        }

        // ---------------------------------------------------------------- decks

        private Deck NewDeck(int pI0, int pI1, DeckStyle pStyle, DeckSurface pSurface)
        {
            Deck lDeck = new Deck(_nextId++, pI0, pI1) { style = pStyle, surface = pSurface };
            layout.decks.Add(lDeck);
            return lDeck;
        }

        // Extends the route deck over new spine samples (creates it after a gap).
        private void ExtendMain(int pFrom, int pTo, Func<float, float> pBank)
        {
            if (_main == null)
            {
                _main = NewDeck(pFrom, pTo, DeckStyle.Rock, DeckSurface.Solid);
                _main.route = true;
                _main.underDepth = 14f;
                for (int k = 0; k < _main.Count; k++)
                {
                    _main.w[k] = _mainWidth;
                    _main.edgeL[k] = _edges;
                    _main.edgeR[k] = _edges;
                }
            }
            else _main.ExtendTo(pTo);

            int lCount = pTo - pFrom + 1;
            for (int i = pFrom; i <= pTo; i++)
            {
                int lK = i - _main.i0;
                float lPrevWidth = lK > 0 ? _main.w[lK - 1] : (_startWidth > 0f ? _startWidth : _mainWidth);
                _main.x[lK] = 0f;
                _main.h[lK] = layout.spine.samples[i].position.y;
                _main.w[lK] = lK == 0 && _startWidth > 0f ? _startWidth : Mathf.MoveTowards(lPrevWidth, _mainWidth, Spine.STEP * _widthRate);
                _main.edgeL[lK] = _edges;
                _main.edgeR[lK] = _edges;
                _main.profile[lK] = DeckProfile.Flat;
                _main.bank[lK] = pBank != null ? pBank((i - pFrom + 1) / (float)lCount) : 0f;
            }
            _startWidth = -1f;
            _widthRate = WIDTH_RATE;
        }

        // Applies a per-sample edit to the route deck over an s range (profiles, edges, ramps).
        private void EditMain(float pS0, float pS1, Action<int, float> pEdit)
        {
            if (_main == null) return;
            for (int k = 0; k < _main.Count; k++)
            {
                float lS = layout.spine.SOf(_main.i0 + k);
                if (lS >= pS0 - 0.01f && lS <= pS1 + 0.01f) pEdit(k, lS);
            }
        }

        // Ends the route deck : the next module starts after a gap.
        private void BreakMain(bool pCap = true)
        {
            if (_main == null) return;
            _main.endCap = pCap;
            _main = null;
        }

        // Side / upper / lower platform over an s range with per-s offsets (x lateral, h absolute height).
        private Deck AddDeck(float pS0, float pS1, DeckStyle pStyle, DeckSurface pSurface, Func<float, float> pX, Func<float, float> pH, Func<float, float> pW, EdgeKind pLeft, EdgeKind pRight)
        {
            int lI0 = layout.spine.IndexOf(pS0);
            int lI1 = layout.spine.IndexOf(pS1);
            Deck lDeck = NewDeck(lI0, Mathf.Max(lI0 + 1, lI1), pStyle, pSurface);
            for (int k = 0; k < lDeck.Count; k++)
            {
                float lS = layout.spine.SOf(lDeck.i0 + k);
                lDeck.x[k] = pX(lS);
                lDeck.h[k] = pH(lS);
                lDeck.w[k] = pW(lS);
                lDeck.edgeL[k] = pLeft;
                lDeck.edgeR[k] = pRight;
            }
            return lDeck;
        }

        private float RouteHeight(float pS) => layout.spine.Evaluate(pS).position.y;

        private void Route(float pS0, float pS1, Deck pDeck, float pLateral = 0f)
        {
            List<RouteNode> lRoute = layout.route;
            if (lRoute.Count > 0)
            {
                RouteNode lLast = lRoute[lRoute.Count - 1];
                if (lLast.deck == pDeck && Mathf.Approximately(lLast.lateral, pLateral) && Mathf.Abs(lLast.s1 - pS0) < Spine.STEP * 1.5f)
                {
                    lLast.s1 = pS1;
                    lRoute[lRoute.Count - 1] = lLast;
                    return;
                }
            }
            lRoute.Add(new RouteNode { s0 = pS0, s1 = pS1, deck = pDeck, lateral = pLateral });
        }

        private void Act(float pS, RouteAction pAction, Deck pDeck = null)
        {
            layout.acts.Add(new RouteAct { s = pS, action = pAction, deck = pDeck });
        }

        private void Spawn(Action pAction) => _spawns.Add(pAction);

        // ---------------------------------------------------------------- overlays

        public LevelBuilder Hint(string pText)
        {
            layout.hints.Add(new KeyValuePair<float, string>(_s, pText));
            return this;
        }

        // Default edge for the next route decks : Wall early in the game, Lip once the player knows the rules.
        public LevelBuilder Edges(EdgeKind pKind)
        {
            _edges = pKind;
            return this;
        }

        // Road (or track) the rival drives on, parallel to the route and pushed out of the way of every platform.
        public LevelBuilder Lane(Side pSide, float pOffset = 46f, float pHeight = -6f, DeckStyle pStyle = DeckStyle.Road)
        {
            _laneSet = true;
            _laneSide = (int)pSide;
            _laneOffset = pOffset;
            _laneHeight = pHeight;
            _laneStyle = pStyle;
            return this;
        }

        public LevelBuilder NoLane()
        {
            _hasLane = false;
            return this;
        }

        public LevelBuilder Meteors(float pLength, float pInterval = 0.8f)
        {
            _zones.Add(new WorldBreakZone { kind = WorldBreakKind.Meteors, s0 = _s, s1 = _s + pLength, value = pInterval });
            return this;
        }

        // Everything behind you crumbles : a wave chases the player at pWaveSpeed.
        public LevelBuilder Collapse(float pLength, float pWaveSpeed = 70f)
        {
            _zones.Add(new WorldBreakZone { kind = WorldBreakKind.Collapse, s0 = _s, s1 = _s + pLength, value = pWaveSpeed });
            return this;
        }

        // ---------------------------------------------------------------- build

        // Finalizes the layout (frames, indices, lane, safe points) then spawns meshes and objects.
        public LevelLayout Build()
        {
            BuildLane();
            layout.Finalize();
            BuildSafePoints();
            BreakDirector lDirector = BreakDirector.Create(layout, _zones, root, theme);
            foreach (Action lSpawn in _spawns) lSpawn();
            LevelGeometry.Build(layout, root, theme, lDirector);
            return layout;
        }

        private void BuildLane()
        {
            if (!_hasLane) return;
            int lCount = layout.spine.Count;
            float[] lPush = new float[lCount];
            // Push the lane out of every platform on its side (plus a margin) so it never crosses the route.
            for (int i = 0; i < lCount; i++) lPush[i] = _laneOffset;
            foreach (Deck lDeck in layout.decks)
            {
                for (int k = 0; k < lDeck.Count; k++)
                {
                    float lOuter = lDeck.x[k] * _laneSide + lDeck.w[k] * 0.5f + 18f + 7f;
                    int i = lDeck.i0 + k;
                    lPush[i] = Mathf.Max(lPush[i], lOuter);
                }
            }
            // Smooth over ~160 m (max over a window, then a moving average) : no sudden swerves.
            float[] lMax = new float[lCount];
            for (int i = 0; i < lCount; i++)
            {
                float lValue = 0f;
                for (int j = Mathf.Max(0, i - 40); j <= Mathf.Min(lCount - 1, i + 40); j++) lValue = Mathf.Max(lValue, lPush[j]);
                lMax[i] = lValue;
            }
            Deck lLane = NewDeck(0, lCount - 1, _laneStyle, DeckSurface.Lane);
            lLane.underDepth = 4f;
            lLane.startCap = false;
            lLane.endCap = false;
            for (int i = 0; i < lCount; i++)
            {
                float lSum = 0f;
                int lN = 0;
                for (int j = Mathf.Max(0, i - 40); j <= Mathf.Min(lCount - 1, i + 40); j++) { lSum += lMax[j]; lN++; }
                float lS = layout.spine.SOf(i);
                // Tighter beside the start plaza so the intro can frame runner and vehicle side by side.
                float lNear = PLAZA_WIDTH * 0.5f + 6f + 7f;
                float lStartBlend = Mathf.Clamp01((lS - 40f) / 160f);
                float lOffset = Mathf.Lerp(lNear, lSum / lN, lS < -60f ? 1f : lStartBlend);
                lLane.x[i] = _laneSide * lOffset;
                lLane.h[i] = layout.spine.samples[i].position.y + (lS < 40f ? 0f : _laneHeight * Mathf.Clamp01((lS - 40f) / 120f));
                lLane.w[i] = 14f;
                lLane.edgeL[i] = EdgeKind.Wall;
                lLane.edgeR[i] = EdgeKind.Wall;
            }
            layout.lane = lLane;
            layout.laneSide = _laneSide;
        }

        private void BuildSafePoints()
        {
            float lNext = 0f;
            float lCheckpoint = 250f;
            for (int i = 0; i < layout.spine.Count; i++)
            {
                float lS = layout.spine.SOf(i);
                if (lS < lNext || lS > layout.finishS) continue;
                foreach (Deck lDeck in layout.DecksAt(lS))
                {
                    if (!lDeck.route || lDeck.surface != DeckSurface.Solid || lDeck.Width(lS) < 10f) continue;
                    if (lDeck.endCap && lDeck.EndS - lS < 25f) continue;
                    if (lS - lDeck.StartS < 6f && lDeck.startCap) continue;
                    layout.safePoints.Add(new SafePoint { s = lS, deck = lDeck, lateral = 0f });
                    lNext = lS + SAFE_SPACING;
                    if (lS >= lCheckpoint)
                    {
                        layout.checkpoints.Add(lS);
                        lCheckpoint = lS + 250f;
                    }
                    break;
                }
            }
        }

        private bool InZone(float pS, WorldBreakKind pKind)
        {
            foreach (WorldBreakZone lZone in _zones)
                if (lZone.kind == pKind && pS >= lZone.s0 && pS <= lZone.s1) return true;
            return false;
        }
    }
}
