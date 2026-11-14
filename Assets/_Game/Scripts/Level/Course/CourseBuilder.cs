using System;
using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum Side { Left = -1, Right = 1 }

    public static class HSMath
    {
        public static float Smooth(float pT)
        {
            pT = Mathf.Clamp01(pT);
            return pT * pT * (3f - 2f * pT);
        }
    }

    // Authoring API of a stage : modules laid one after the other, sized in seconds at cruise speed so the same recipe
    // holds at any speed. A turtle draws the line in 3D (yaw, pitch, roll) and every module opens solid blocks on it :
    // long stretches, big tilted slabs, pipes, loops, inverted runs.
    public class CourseBuilder
    {
        private const float PLAZA = 300f;
        // The world is six times wider than the stage recipes say : the runner is tiny in a huge circuit.
        public const float WIDTH_SCALE = 6f;
        // Sides are mountains, not kerbs : a gentle foot that steepens, high enough that nobody flies over.
        private const float MOUNTAIN_MIN = 120f;
        private const float MOUNTAIN_MAX = 210f;
        private const float MOUNTAIN_LEAN = 52f;
        // Hazards and props grow with the world, not as much as its width.
        private const float PROP_BASE = 3f;
        private float PROP_SCALE => PROP_BASE * SizeRoot;
        // 2026-11-14 : how much bigger this stage is than 1-1 (1 to 10, its speed ratio). Widths grow with it fully,
        // heights and props with its square root (gravity and the jump do not scale), lengths through the cruise.
        public float size = 1f;
        private float SizeRoot => Mathf.Sqrt(Mathf.Max(1f, size));
        // 0 on 1-1, 1 on the last stage : how hard this stage may be (hazards, sky levels, spirals).
        public float difficulty;

        private struct StripDef { public float s, lateral, halfWidth, up; }
        private struct RopeDef { public float s0, s1, lateral, height0, height1; public bool sky; }
        private struct LoopDef { public float s0, s1, lateral, drift, radius, advance, lead; }
        private struct MeteorDef { public float s; public bool fromSky; public float radius; public float lateral; }
        private struct PadDef { public float s, lateral, width, length, boost; }
        private struct RingDef { public float s, lateral, height, radius; public bool line; }
        private struct LaneDef { public float s0, s1, lateral; public int level, style; }
        private struct KickerDef { public float s0, s1, lateral, halfWidth, height, up; }
        private struct DeckPadDef { public float s, lateral, height, width, length, boost; }
        private struct DeckStripDef { public float s, lateral, height, halfWidth, up; }
        private struct CrateDef { public float s, lateral, size; public bool moving; }
        private struct LaserDef { public float s, height, spin; public int beams; }
        private struct HammerDef { public float s, phase; }
        private struct BlockDef { public float s, lateral, width, length, height, lean; public int palette; }
        private struct ArchDef { public float s; public bool lit; }
        private struct PaneDef { public float s, lateral, width, height; public PaneKind kind; }
        private struct MoverDef { public float s, lateral, width, length, height, speed; }
        private struct BridgeDef { public float s0, s1, lateral, height, halfWidth; public bool sky; }
        // A rock floating in the air (height above the line, or an absolute altitude when sky).
        private struct FloatRockDef { public float s, lateral, height, size; public bool sky; }
        // A crater : a real hole through the ground lane.
        private struct CraterDef { public float s, lateral, radius; }
        private readonly List<CraterDef> _craters = new List<CraterDef>();

        // Craters dug into a stretch of open ground : holes to jump or to go round, more of them on the late stages.
        private void DigCraters(float pS0, float pS1)
        {
            if (pS1 - pS0 < M(2f)) return;
            int lCount = Chance(Mathf.Lerp(0.35f, 0.95f, difficulty)) ? 1 + (Chance(difficulty) ? 1 : 0) : 0;
            for (int i = 0; i < lCount; i++)
            {
                float lRadius = Rand(18f, 40f) * SizeRoot;
                float lS = Mathf.Lerp(pS0 + M(0.8f), pS1 - M(0.8f), Rand(0.1f, 0.9f));
                _craters.Add(new CraterDef { s = lS, lateral = Rand(-0.5f, 0.5f) * _halfWidth, radius = lRadius });
                NoPads(lS - lRadius * 1.6f, lS + lRadius * 1.6f);
            }
        }
        private readonly List<FloatRockDef> _floatRocks = new List<FloatRockDef>();

        private readonly Transform _root;
        private readonly Theme _theme;
        private readonly SpeedProfile _p;
        private readonly LevelLayout _layout = new LevelLayout();
        private readonly System.Random _random;
        private readonly int _seed;

        private readonly List<TrackBlock> _blocks = new List<TrackBlock>();
        private readonly List<StripDef> _strips = new List<StripDef>();
        private readonly List<RopeDef> _ropes = new List<RopeDef>();
        private readonly List<LoopDef> _loops = new List<LoopDef>();
        private readonly List<MeteorDef> _meteors = new List<MeteorDef>();
        private readonly List<PadDef> _pads = new List<PadDef>();
        private readonly List<RingDef> _rings = new List<RingDef>();
        private readonly List<CrateDef> _crates = new List<CrateDef>();
        private readonly List<LaserDef> _lasers = new List<LaserDef>();
        private readonly List<HammerDef> _hammers = new List<HammerDef>();
        private readonly List<BlockDef> _props = new List<BlockDef>();
        private readonly List<ArchDef> _arches = new List<ArchDef>();
        private readonly List<float> _gravityGates = new List<float>();
        private readonly List<PaneDef> _panes = new List<PaneDef>();
        private readonly List<MoverDef> _movers = new List<MoverDef>();
        private readonly List<BridgeDef> _bridges = new List<BridgeDef>();
        private readonly List<LaneDef> _lanes = new List<LaneDef>();
        private readonly List<KickerDef> _kickers = new List<KickerDef>();
        private readonly List<DeckPadDef> _deckPads = new List<DeckPadDef>();
        private readonly List<DeckStripDef> _deckStrips = new List<DeckStripDef>();
        // Every piece of line, as data : what the level design tools edit.
        private readonly List<SegmentData> _segments = new List<SegmentData>();
        private float _altitude;
        // Name of the module laying the line : shown on each piece in the level design tools.
        private string _module = "";

        // A hoop of light right before the track takes over gravity : you know the world is about to turn.
        private void GravityGate(float pS) => _gravityGates.Add(pS);
        private readonly List<Vector2> _noPads = new List<Vector2>();

        private Vector3 _pos;
        private float _yaw;
        private float _pitch;
        private float _roll;
        private float _s;
        private float _halfWidth;
        private bool _started;
        private bool _localGravity;
        private bool _noItems;
        private int _dives;
        private TrackBlock _open;

        // Stretches the open modules (not the jumps) : later stages last as long as the plan says despite their speed.
        public float pace = 1f;
        // How many scattered pads : few on the first stages, so their speed stays under control.
        public float padDensity = 1f;
        // Every stage is this much wider than its recipe : small and tight early, huge when the runner is fastest.
        public float widthScale = 1f;
        // 0 street, 1 machine, 2 cosmic : how hard the line may bend (2026-11-12 : curves, loops and twists were far
        // too violent). Every module reads its caps from here.
        public int world;

        private float Cap(float pStreet, float pMachine, float pCosmic) => world <= 0 ? pStreet : world == 1 ? pMachine : pCosmic;

        public float K => _p.k;
        public float Cruise => _p.cruise;
        public LevelLayout Layout => _layout;

        public CourseBuilder(Transform pRoot, Theme pTheme, SpeedProfile pProfile, int pSeed)
        {
            _root = pRoot;
            _theme = pTheme;
            _p = pProfile;
            _seed = pSeed;
            _random = new System.Random(pSeed);
            _layout.profile = pProfile;
        }

        private float M(float pSeconds) => pSeconds * _p.cruise;

        private float Rand(float pMin, float pMax) => pMin + (float)_random.NextDouble() * (pMax - pMin);

        private bool Chance(float pProbability) => _random.NextDouble() < pProbability;

        // ================================================================ line

        private Quaternion Frame() => Quaternion.AngleAxis(_yaw, Vector3.up) * Quaternion.AngleAxis(-_pitch, Vector3.right) * Quaternion.AngleAxis(-_roll, Vector3.forward);

        private void Push(Vector3 pShift = default(Vector3))
        {
            Quaternion lFrame = Frame();
            Vector3 lForward = lFrame * Vector3.forward;
            if (_layout.spine.Count > 0) _pos += lForward * Spine.STEP + pShift;
            _layout.spine.samples.Add(new SpineSample { position = _pos, forward = lForward, up = lFrame * Vector3.up, s = _s, localGravity = _localGravity });
            _s += Spine.STEP;
        }

        // Advances the line while yaw, pitch and roll ease by the given totals. pShift moves it sideways or down without
        // changing where it points (keeps the two legs of a loop apart, lets a drop stay level).
        private void Span(float pLength, float pYaw = 0f, float pPitch = 0f, float pRoll = 0f, Vector3 pShift = default(Vector3), float pLinear = 0f)
        {
            int lSteps = Mathf.Max(1, Mathf.RoundToInt(pLength / Spine.STEP));
            float lYaw0 = _yaw, lPitch0 = _pitch, lRoll0 = _roll;
            _segments.Add(new SegmentData
            {
                steps = lSteps, yaw = pYaw, pitch = pPitch, roll = pRoll, linear = pLinear, localGravity = _localGravity,
                shift = Quaternion.Inverse(Quaternion.AngleAxis(lYaw0, Vector3.up)) * pShift, label = _module
            });
            float lPrevious = 0f;
            for (int i = 1; i <= lSteps; i++)
            {
                float lT = i / (float)lSteps;
                // pLinear spreads the turn evenly : no sharp peak of curvature in the middle of a loop.
                float lE = Mathf.Lerp(HSMath.Smooth(lT), lT, pLinear);
                _yaw = lYaw0 + pYaw * lE;
                _pitch = lPitch0 + pPitch * lE;
                _roll = lRoll0 + pRoll * lE;
                Push(pShift * (lE - lPrevious));
                lPrevious = lE;
            }
        }

        private void Wrap()
        {
            _pitch = Mathf.Repeat(_pitch + 180f, 360f) - 180f;
            _roll = Mathf.Repeat(_roll + 180f, 360f) - 180f;
        }

        // ================================================================ blocks

        private TrackBlock Open(BlockLook pLook, float pWall = 0f, float pHalfWidth = -1f, float pOffset = 0f, float pDrop = 0f, float pTilt = 0f, float pThickness = -1f)
        {
            Close();
            float lLeft = pWall;
            float lRight = pWall;
            bool lMountain = false;
            if (pWall > 0.01f)
            {
                if (pLook == BlockLook.Pipe) lLeft = lRight = pWall * PROP_SCALE;
                else if (pLook == BlockLook.Slab) lLeft = lRight = 0f;
                else
                {
                    lMountain = true;
                    float lMountainScale = Mathf.Pow(Mathf.Max(1f, size), 0.6f);
                    lLeft = Rand(MOUNTAIN_MIN, MOUNTAIN_MAX) * lMountainScale;
                    lRight = Rand(MOUNTAIN_MIN, MOUNTAIN_MAX) * lMountainScale;
                }
            }
            _open = new TrackBlock
            {
                s0 = _s - Spine.STEP,
                look = pLook,
                wallLeft = lLeft,
                wallRight = lRight,
                mountain = lMountain,
                wallLean = lMountain ? MOUNTAIN_LEAN : 28f,
                halfWidth = pHalfWidth > 0f ? pHalfWidth : _halfWidth,
                offset = pOffset,
                drop = pDrop,
                tilt = pTilt,
                thickness = pThickness > 0f ? pThickness : Rand(22f, 44f) * SizeRoot,
                seed = _blocks.Count
            };
            _blocks.Add(_open);
            return _open;
        }

        private void Close()
        {
            if (_open == null) return;
            _open.s1 = _s - Spine.STEP;
            _open = null;
        }

        private void NoPads(float pFrom, float pTo) => _noPads.Add(new Vector2(pFrom, pTo));

        private void Route(float pS0, float pS1, float pLateral)
        {
            if (pS1 > pS0) _layout.route.Add(new RouteNode { s0 = pS0, s1 = pS1, lateral = pLateral });
        }

        private void Act(float pS, RouteAction pAction) => _layout.acts.Add(new RouteAct { s = pS, action = pAction });

        // Title backdrop : nothing to touch on the attract run.
        public CourseBuilder NoItems()
        {
            _module = "NoItems";
            _noItems = true;
            return this;
        }

        // ================================================================ modules

        public CourseBuilder Start(float pWidth, float pAltitude)
        {
            _module = "Start";
            _halfWidth = pWidth * 0.5f * WIDTH_SCALE * widthScale;
            _yaw = 90f;
            _pos = new Vector3(-8000f, pAltitude, 0f);
            _altitude = pAltitude;
            _s = -PLAZA;
            _started = true;
            Open(BlockLook.Plaza, 6f, _halfWidth + 60f);
            int lSteps = Mathf.RoundToInt(PLAZA / Spine.STEP);
            for (int i = 0; i <= lSteps; i++) Push();
            _layout.startLineS = 0f;
            _layout.spawnS = -130f;
            NoPads(-PLAZA, M(1.2f));
            Open(BlockLook.Road, 3f);
            return this;
        }

        public CourseBuilder Hint(string pText)
        {
            _module = "Hint";
            _layout.hints.Add(new KeyValuePair<float, string>(_s, pText));
            return this;
        }

        public CourseBuilder Width(float pWidth)
        {
            _module = "Width";
            _halfWidth = pWidth * 0.5f * WIDTH_SCALE * widthScale;
            return this;
        }

        // Classic stretch : a road on a long block, low kerbs.
        public CourseBuilder Road(float pSeconds)
        {
            _module = "Road";
            Open(BlockLook.Road, 3f);
            Span(M(pSeconds * pace), 0f, 0f, 0f);
            return this;
        }

        // Rolling ground. Your speed follows the slopes.
        public CourseBuilder Hills(float pSeconds, float pDegrees = 14f)
        {
            _module = "Hills";
            pSeconds *= pace;
            TrackBlock lHillBlock = Open(BlockLook.Ground, 7f);
            lHillBlock.mounds = Rand(14f, 26f) * SizeRoot;
            AirRocks(_s, _s + M(pSeconds), 0.5f);
            DigCraters(_s, _s + M(pSeconds));
            int lHumps = Mathf.Max(1, Mathf.RoundToInt(pSeconds / 3.4f));
            float lLength = M(pSeconds) / lHumps;
            for (int i = 0; i < lHumps; i++)
            {
                float lAngle = pDegrees * Rand(0.7f, 1.2f) * Cap(0.6f, 0.8f, 1f);
                Span(lLength * 0.25f, 0f, lAngle);
                Span(lLength * 0.5f, 0f, -lAngle * 2f);
                Span(lLength * 0.25f, 0f, lAngle);
            }
            _pitch = 0f;
            return this;
        }

        private float TurnRoom(int pSide) => pSide > 0 ? 170f - _yaw : _yaw - 10f;

        // Banked turn. The heading stays within a half circle : the stage always gets away from its own past, so legs
        // never cross. Asked the other way, it turns toward the side that has room.
        public CourseBuilder Turn(Side pSide, float pDegrees, float pBank = 28f, float pRadiusScale = 1f)
        {
            _module = "Turn";
            int lSide = (int)pSide;
            if (TurnRoom(lSide) < pDegrees && TurnRoom(-lSide) > TurnRoom(lSide)) lSide = -lSide;
            float lDegrees = Mathf.Min(pDegrees, TurnRoom(lSide));
            if (lDegrees < 5f) return this;
            // Wide and soft : the yaw rate never jumps (2026-11-12).
            float lRadius = Mathf.Max(Mathf.Max(_halfWidth * 3f + 30f, _p.cruise * 4.5f), 1200f) * pRadiusScale;
            float lLength = lRadius * lDegrees * Mathf.Deg2Rad;
            pBank = Mathf.Min(pBank, Cap(12f, 18f, 24f));
            if (_open == null || _open.look == BlockLook.Slab) Open(BlockLook.Ground, 10f);
            Span(lLength * 0.25f, lSide * lDegrees * 0.15f, 0f, lSide * pBank);
            Span(lLength * 0.5f, lSide * lDegrees * 0.7f);
            Span(lLength * 0.25f, lSide * lDegrees * 0.15f, 0f, -lSide * pBank);
            _roll = 0f;
            return this;
        }

        // The stage turns around : half a circle with a hard bank, both legs far apart.
        public CourseBuilder Hairpin(Side pSide)
        {
            _module = "Hairpin";
            Open(BlockLook.Pipe, 16f);
            return Turn(_yaw <= 90f ? Side.Right : Side.Left, 150f, Cap(24f, 30f, 36f));
        }

        // Linked curves, left and right : no rest for the mouse.
        public CourseBuilder Sweeps(int pCount, float pDegrees = 42f)
        {
            _module = "Sweeps";
            Open(BlockLook.Ground, 9f);
            int lSide = Chance(0.5f) ? 1 : -1;
            for (int i = 0; i < pCount; i++)
            {
                Turn(lSide > 0 ? Side.Right : Side.Left, pDegrees * Rand(0.75f, 1.2f), 26f);
                if (i < pCount - 1) Span(M(0.5f));
                lSide = -lSide;
            }
            return this;
        }

        // A turn so banked it is a wall : you ride it, gravity follows the track for the length of it.
        public CourseBuilder WallTurn(Side pSide, float pDegrees = 110f)
        {
            _module = "WallTurn";
            Open(BlockLook.Pipe, 20f);
            bool lWas = _localGravity;
            _localGravity = true;
            Turn(pSide, pDegrees, Cap(60f, 65f, 70f));
            _localGravity = lWas;
            return this;
        }

        // Long slope that feeds your speed.
        public CourseBuilder Descent(float pSeconds, float pDegrees = 22f)
        {
            _module = "Descent";
            float lLength = M(pSeconds);
            Open(BlockLook.Ground, 8f);
            Span(lLength * 0.25f, 0f, -pDegrees);
            Span(lLength * 0.5f);
            Span(lLength * 0.25f, 0f, pDegrees);
            _pitch = 0f;
            return this;
        }

        public CourseBuilder Climb(float pSeconds, float pDegrees = 18f)
        {
            _module = "Climb";
            float lLength = M(pSeconds);
            Open(BlockLook.Ground, 8f);
            Span(lLength * 0.25f, 0f, pDegrees);
            Span(lLength * 0.5f);
            Span(lLength * 0.25f, 0f, -pDegrees);
            _pitch = 0f;
            return this;
        }

        // Full loop. The two legs are shifted apart so the way out never meets the way in.
        public CourseBuilder Loop(Side pSide = Side.Right)
        {
            _module = "Loop";
            // Sized for a boosted runner, not for cruise : a loop is a big, slow sweep, never a wall (2026-11-12 : 3x cruise).
            float lRadius = Mathf.Max(_p.cruise * 3f, _halfWidth * 2.4f);
            float lLength = lRadius * Mathf.PI * 2f * 1.08f;
            Quaternion lYaw = Quaternion.AngleAxis(_yaw, Vector3.up);
            Vector3 lShift = lYaw * Vector3.right * (int)pSide * (_halfWidth * 2.6f + 140f);
            Open(BlockLook.Pipe, 12f);
            NoPads(_s, _s + M(1.5f));
            Span(M(1.5f));
            _localGravity = true;
            NoPads(_s, _s + lLength);
            Span(lLength, 0f, 360f, 0f, lShift, 0.55f);
            _localGravity = false;
            _pitch = 0f;
            Span(M(1.2f));
            Route(_s - lLength - M(1.6f), _s, 0f);
            GravityGate(_s - lLength - M(1.2f) - M(0.3f));
            return this;
        }

        // Corkscrew : a loop with the track rolling all the way round at the same time.
        public CourseBuilder Corkscrew(Side pSide = Side.Left)
        {
            _module = "Corkscrew";
            float lRadius = Mathf.Max(_p.cruise * 3.2f, _halfWidth * 2.6f);
            float lLength = lRadius * Mathf.PI * 2f * 1.2f;
            Quaternion lYaw = Quaternion.AngleAxis(_yaw, Vector3.up);
            Vector3 lShift = lYaw * Vector3.right * (int)pSide * (_halfWidth * 3f + 180f);
            float lHalf = _halfWidth;
            _halfWidth *= 1.25f;
            Open(BlockLook.Pipe, 18f);
            Span(M(1.2f));
            GravityGate(_s - M(0.3f));
            _localGravity = true;
            NoPads(_s, _s + lLength);
            Span(lLength, 0f, 360f, (int)pSide * 360f, lShift, 0.55f);
            _halfWidth = lHalf;
            _localGravity = false;
            Wrap();
            _pitch = 0f;
            _roll = 0f;
            Span(M(0.4f));
            return this;
        }

        // The track turns around its own axis : the wall becomes the ceiling, the ceiling the floor.
        public CourseBuilder Twist(float pSeconds = 3.2f, int pTurns = 1, Side pSide = Side.Right)
        {
            _module = "Twist";
            Open(BlockLook.Pipe, 18f);
            Span(M(0.9f));
            GravityGate(_s - M(0.3f));
            _localGravity = true;
            // Never faster than a full turn in 9 s at cruise.
            pSeconds = Mathf.Max(pSeconds * 1.4f, 9f * pTurns);
            NoPads(_s, _s + M(pSeconds));
            Span(M(pSeconds), 0f, 0f, (int)pSide * 360f * pTurns, default(Vector3), 0.5f);
            _localGravity = false;
            _roll = 0f;
            Span(M(0.3f));
            return this;
        }

        // Up the wall, across the ceiling, down the other side : a whole stretch runs upside down.
        public CourseBuilder Inverted(float pSeconds = 3f, Side pSide = Side.Left)
        {
            _module = "Inverted";
            Open(BlockLook.Pipe, 22f);
            int lSide = (int)pSide;
            Span(M(0.9f));
            GravityGate(_s - M(0.3f));
            _localGravity = true;
            Span(M(Cap(4.5f, 4f, 3.5f)), 0f, 0f, lSide * 180f);
            Span(M(pSeconds * pace));
            Span(M(Cap(4.5f, 4f, 3.5f)), 0f, 0f, lSide * 180f);
            _localGravity = false;
            _roll = 0f;
            Span(M(0.3f));
            return this;
        }

        // Corridor between two leaning walls : ride them up, come back down with the speed.
        public CourseBuilder Corridor(float pSeconds, float pWall = 40f, float pLean = 30f)
        {
            _module = "Corridor";
            float lHalf = _halfWidth;
            _halfWidth = Mathf.Min(_halfWidth, (26f + 4f * K) * 4f * SizeRoot);
            TrackBlock lBlock = Open(BlockLook.Pipe, pWall);
            lBlock.wallLean = pLean;
            Span(M(pSeconds * pace));
            _halfWidth = lHalf;
            return this;
        }

        // Big slabs, each one shifted, lowered and tilted against the last : some touch, some leave a gap to jump.
        public CourseBuilder Slabs(float pSeconds, float pDensity = 1f)
        {
            _module = "Slabs";
            pSeconds *= pace;
            float lEnd = _s + M(pSeconds);
            float lOffset = 0f;
            float lDrop = 0f;
            float lHalf = _halfWidth;
            float lPrevDrop = 0f, lPrevOffset = 0f, lPrevHalf = lHalf, lPrevTilt = 0f;
            while (_s < lEnd)
            {
                float lLength = M(Rand(0.8f, 1.9f) / pDensity);
                float lNarrow = lHalf * Rand(0.55f, 0.95f);
                float lTilt = Rand(-1f, 1f) * Cap(6f, 10f, 14f);
                if (_s + lLength >= lEnd) lTilt = 0f;
                lDrop = Mathf.Max(lDrop, StepDown(lPrevDrop, lPrevOffset, lPrevHalf, lPrevTilt, lOffset, lNarrow, lTilt));
                TrackBlock lBlock = Open(BlockLook.Slab, 0f, lNarrow, lOffset, lDrop, lTilt, Rand(60f, 140f));
                lPrevDrop = lDrop;
                lPrevOffset = lOffset;
                lPrevHalf = lNarrow;
                lPrevTilt = lTilt;
                float lS0 = _s;
                Span(lLength, Rand(-5f, 5f) * ((_yaw > 30f && _yaw < 150f) ? 1f : 0f));
                Close();
                Route(lS0, _s, lOffset);
                lOffset = Mathf.Clamp(lOffset + Rand(-1f, 1f) * lHalf * 0.4f, -lHalf * 0.5f, lHalf * 0.5f);
                bool lGap = Chance(0.3f);
                // Always lower than the last one : you drop onto the next block, you never hit its side.
                float lNext = lDrop + Rand(5f, 26f) * SizeRoot;
                if (lGap)
                {
                    float lGapLength = M(Rand(0.1f, 0.22f));
                    Act(_s - M(0.12f), RouteAction.Jump);
                    float lGapS = _s;
                    Span(lGapLength);
                    NoPads(lS0 + lLength * 0.7f, _s + M(0.3f));
                    GapRocks(lGapS, _s, lOffset, lNarrow, lDrop);
                }
                lDrop = lNext;
            }
            // Back to the line for the next module, below the last slab.
            float lBack = Mathf.Max(lDrop, StepDown(lPrevDrop, lPrevOffset, lPrevHalf, lPrevTilt, 0f, lHalf, 0f));
            Close();
            Span(M(0.2f), 0f, 0f, 0f, Vector3.down * lBack);
            Open(BlockLook.Ground, 4f);
            return this;
        }

        // Run off a colossal cliff : look down to dive. The block below lasts long enough to land anywhere on it.
        public CourseBuilder Dive(float pDrop = 320f)
        {
            _module = "Dive";
            pDrop *= SizeRoot;
            Open(BlockLook.Ground, 1f);
            Span(M(1.2f), 0f, 5f);
            Span(M(0.3f), 0f, -5f);
            _pitch = 0f;
            Close();
            float lLip = _s;
            Act(lLip - M(0.1f), RouteAction.Jump);
            Act(lLip + M(0.25f), RouteAction.Dive);
            NoPads(lLip - M(0.8f), lLip + M(3f));
            float lFall = Mathf.Sqrt(2f * pDrop / Ballistics.G_FALL);
            Span(M(lFall * 0.9f), 0f, 0f, 0f, Vector3.down * pDrop);
            float lHalf = _halfWidth;
            _halfWidth = lHalf * 1.5f;
            Open(BlockLook.Ground, 10f);
            Span(M(Mathf.Max(3.5f, lFall * 1.5f)));
            _halfWidth = lHalf;
            _dives++;
            return this;
        }

        // Platforms separated by holes : a launch strip on each lip. Missing it is a jump at best, a fall at worst.
        public CourseBuilder Plateaus(int pCount, float pPlatform = 2.4f)
        {
            _module = "Plateaus";
            for (int i = 0; i < pCount; i++)
            {
                Open(BlockLook.Slab, 0f, _halfWidth * Rand(0.8f, 1f), 0f, 0f, Rand(-8f, 8f), Rand(40f, 90f));
                Span(M(pPlatform * pace));
                Close();
                float lLip = _s;
                _strips.Add(new StripDef { s = lLip - M(0.22f), lateral = 0f, halfWidth = _halfWidth * 0.75f, up = 72f });
                NoPads(lLip - M(0.8f), lLip + M(1.5f));
                Span(M(Rand(0.45f, 0.65f)));
            }
            Open(BlockLook.Ground, 4f);
            return this;
        }

        // A long rope over a descent : jump onto it for the electric ride. Miss it and you just run down the slope : the
        // ground under it is continuous, every step goes down, there is no pit and no wall at the far end.
        public CourseBuilder Rope(float pSeconds = 4f)
        {
            _module = "Rope";
            float lS0 = _s;
            Open(BlockLook.Ground, 6f);
            Span(M(0.9f));
            float lLateral = Rand(-_halfWidth * 0.3f, _halfWidth * 0.3f);
            float lLip = _s;
            Act(lLip - M(0.15f), RouteAction.Jump);
            // High in the air (2026-11-11) : a launch strip on the lip throws you up to it.
            _strips.Add(new StripDef { s = lLip - M(0.18f), lateral = lLateral, halfWidth = _halfWidth * 0.45f, up = ROPE_LAUNCH });
            Span(M(pSeconds * 0.3f), 0f, -9f);
            Span(M(pSeconds * 0.4f));
            Span(M(pSeconds * 0.3f), 0f, 9f);
            _pitch = 0f;
            _ropes.Add(new RopeDef { s0 = lLip - M(0.05f), s1 = _s - M(0.1f), lateral = lLateral, height0 = ROPE_HIGH, height1 = ROPE_LOW });
            Span(M(0.4f));
            Route(lS0, _s, lLateral);
            NoPads(lS0, _s + M(0.4f));
            return this;
        }

        // Ropes hang this high above the ground (start, end) ; the strip under their start launches you this fast.
        private float ROPE_HIGH => 36f * SizeRoot;
        private float ROPE_LOW => 28f * SizeRoot;
        private float ROPE_LAUNCH => Mathf.Sqrt(2f * Ballistics.G_RISE * (ROPE_HIGH + 6f));

        // Ramp : you climb it, top a crest and come down the far side with the speed. Up, then down : the rhythm of the
        // track. Missing nothing, falling nowhere.
        public CourseBuilder Ramp(float pSeconds = 3.5f, float pDegrees = 14f)
        {
            _module = "Ramp";
            pDegrees = Mathf.Min(pDegrees, Cap(8f, 10f, 12f));
            float lLength = M(pSeconds);
            Open(BlockLook.Ground, 7f);
            Span(lLength * 0.3f, 0f, pDegrees);
            Span(lLength * 0.12f);
            Span(lLength * 0.22f, 0f, -pDegrees * 2f);
            Span(lLength * 0.18f);
            Span(lLength * 0.18f, 0f, pDegrees);
            _pitch = 0f;
            return this;
        }

        // Rope in the sky above the track : a launch strip throws you straight up, the rest is on you.
        public CourseBuilder SkyRope(float pSeconds = 4f)
        {
            _module = "SkyRope";
            float lS0 = _s;
            float lLateral = Rand(-_halfWidth * 0.3f, _halfWidth * 0.3f);
            Open(BlockLook.Road, 3f);
            Span(M(0.9f));
            float lStrip = _s - M(0.15f);
            _strips.Add(new StripDef { s = lStrip, lateral = lLateral, halfWidth = _halfWidth * 0.35f, up = ROPE_LAUNCH * 1.15f });
            Span(M(pSeconds + 0.8f));
            _ropes.Add(new RopeDef { s0 = lStrip + M(0.3f), s1 = lStrip + M(0.3f + pSeconds), lateral = lLateral, height0 = ROPE_HIGH + 8f, height1 = ROPE_LOW });
            Route(lS0, _s, lLateral);
            NoPads(lS0, _s);
            return this;
        }

        // Chain of small speed pads along the line : take them all to really fly.
        public CourseBuilder PadRun(float pSeconds, float pEvery = 0.45f)
        {
            _module = "PadRun";
            float lS0 = _s;
            Open(BlockLook.Road, 4f);
            Span(M(pSeconds * pace));
            float lWeave = Rand(0f, 6f);
            for (float lT = 0.2f; lT < pSeconds * pace; lT += pEvery)
                _pads.Add(new PadDef { s = lS0 + M(lT), lateral = Mathf.Sin(lT * 1.3f + lWeave) * _halfWidth * 0.55f, width = _halfWidth * 0.5f, length = M(0.14f), boost = 0.3f });
            Route(lS0, _s, 0f);
            NoPads(lS0, _s);
            return this;
        }

        // Rings in an arc above the ground : worth a boost each, if you stay up there.
        public CourseBuilder RingArc(int pCount = 5)
        {
            _module = "RingArc";
            float lS0 = _s;
            Open(BlockLook.Ground, 3f);
            Span(M(0.5f + pCount * 0.34f));
            float lLateral = Rand(-_halfWidth * 0.5f, _halfWidth * 0.5f);
            Act(lS0 + M(0.25f), RouteAction.Jump);
            for (int i = 0; i < pCount; i++)
            {
                float lT = (i + 0.5f) / pCount;
                _rings.Add(new RingDef { s = lS0 + M(0.5f + i * 0.34f), lateral = lLateral, height = (30f + Mathf.Sin(lT * Mathf.PI) * 34f) * SizeRoot, radius = 60f * SizeRoot });
            }
            Route(lS0, _s, lLateral);
            NoPads(lS0, _s);
            return this;
        }

        // Giant crates in the way. Dash through them : they burst and throw you forward. Run into them : it hurts.
        public CourseBuilder Crates(float pSeconds, int pCount, bool pWall = false)
        {
            _module = "Crates";
            float lS0 = _s;
            Open(BlockLook.Road, 4f);
            Span(M(pSeconds * pace));
            float lLength = _s - lS0;
            for (int i = 0; i < pCount; i++)
            {
                float lS = lS0 + lLength * (i + 0.6f) / pCount;
                float lSize = Rand(26f, 40f) * PROP_SCALE * 0.8f;
                if (pWall)
                {
                    int lCount = Mathf.CeilToInt(_halfWidth * 2f / lSize);
                    for (int c = 0; c < lCount; c++)
                        _crates.Add(new CrateDef { s = lS, lateral = -_halfWidth + lSize * (c + 0.5f), size = lSize });
                    Act(lS - M(0.32f), RouteAction.Dash);
                }
                else
                {
                    float lLateral = Rand(-0.6f, 0.6f) * _halfWidth;
                    _crates.Add(new CrateDef { s = lS, lateral = lLateral, size = lSize });
                    Act(lS - M(0.32f), RouteAction.Dash);
                    Route(lS - M(0.5f), lS + M(0.1f), lLateral);
                }
            }
            NoPads(lS0, _s);
            return this;
        }

        // Crates rolling along the route, slower than you : weave through or dash through.
        public CourseBuilder Convoy(float pSeconds, int pCount)
        {
            _module = "Convoy";
            float lS0 = _s;
            Open(BlockLook.Road, 4f);
            Span(M(pSeconds * pace));
            float lLength = _s - lS0;
            for (int i = 0; i < pCount; i++)
                _crates.Add(new CrateDef { s = lS0 + lLength * (i + 0.5f) / pCount, lateral = Rand(-0.75f, 0.75f) * _halfWidth, size = Rand(20f, 30f) * PROP_SCALE * 0.8f, moving = true });
            NoPads(lS0, _s);
            return this;
        }

        // Rows of laser beams : low ones to jump, slanted ones to read, spinning ones to time. Touching one costs your speed.
        public CourseBuilder Lasers(float pSeconds, float pEvery = 0.9f)
        {
            _module = "Lasers";
            float lS0 = _s;
            Open(BlockLook.Ground, 6f);
            Span(M(pSeconds * pace));
            for (float lT = pEvery; lT < pSeconds * pace - 0.3f; lT += pEvery)
            {
                float lS = lS0 + M(lT);
                int lKind = _random.Next(0, 3);
                _lasers.Add(new LaserDef { s = lS, height = lKind == 0 ? 2.2f : Rand(5f, 9f), beams = lKind == 0 ? 1 : lKind == 1 ? 3 : 4, spin = lKind == 2 ? Rand(30f, 70f) * (Chance(0.5f) ? 1f : -1f) : 0f });
                Act(lS - M(0.3f), lKind == 0 ? RouteAction.Jump : RouteAction.Dash);
            }
            NoPads(lS0, _s);
            return this;
        }

        // Pendulums swinging across the track.
        public CourseBuilder Hammers(float pSeconds, float pEvery = 1.2f)
        {
            _module = "Hammers";
            float lS0 = _s;
            Open(BlockLook.Road, 4f);
            Span(M(pSeconds * pace));
            for (float lT = pEvery; lT < pSeconds * pace - 0.3f; lT += pEvery)
                _hammers.Add(new HammerDef { s = lS0 + M(lT), phase = Rand(0f, 6.28f) });
            NoPads(lS0, _s);
            return this;
        }

        // Pillars and leaning blocks standing on the track : weave.
        public CourseBuilder Pillars(float pSeconds, float pDensity = 1f)
        {
            _module = "Pillars";
            float lS0 = _s;
            Open(BlockLook.Ground, 4f);
            Span(M(pSeconds * pace));
            float lRow = M(0.55f) / pDensity;
            float lGate = 0f;
            for (float lS = lS0 + lRow; lS < _s - lRow * 0.3f; lS += lRow)
            {
                lGate = Mathf.Clamp(lGate + Rand(-1f, 1f) * _halfWidth * 0.4f, -_halfWidth * 0.6f, _halfWidth * 0.6f);
                Route(lS - lRow * 0.5f, lS + lRow * 0.5f, lGate);
                for (int lSide = -1; lSide <= 1; lSide += 2)
                {
                    float lFrom = lGate + lSide * 20f * PROP_SCALE;
                    float lRoom = _halfWidth - lSide * lFrom;
                    if (lRoom < 12f * PROP_SCALE || Chance(0.2f)) continue;
                    float lWidth = Mathf.Min(lRoom, Rand(12f, 34f) * PROP_SCALE);
                    _props.Add(new BlockDef { s = lS, lateral = lFrom + lSide * lWidth * 0.5f, width = lWidth, length = Rand(10f, 34f) * PROP_SCALE, height = Rand(26f, 90f) * PROP_SCALE, lean = Rand(-16f, 16f), palette = _random.Next(0, 3) });
                }
            }
            NoPads(lS0, _s);
            return this;
        }

        // Tunnel of lit arches.
        public CourseBuilder Tunnel(float pSeconds)
        {
            _module = "Tunnel";
            float lS0 = _s;
            float lHalf = _halfWidth;
            _halfWidth = Mathf.Min(_halfWidth, (24f + 4f * K) * 4f * SizeRoot);
            TrackBlock lBlock = Open(BlockLook.Pipe, 30f);
            lBlock.wallLean = 16f;
            Span(M(pSeconds * pace));
            for (float lT = 0.1f; lT < pSeconds * pace; lT += 0.22f) _arches.Add(new ArchDef { s = lS0 + M(lT), lit = true });
            _halfWidth = lHalf;
            return this;
        }

        // A wide, slow-banking turn you can see the end of : the place to drift.
        public CourseBuilder BigTurn(Side pSide, float pDegrees = 100f)
        {
            _module = "BigTurn";
            Open(BlockLook.Ground, 9f);
            return Turn(pSide, pDegrees, 18f, 1.7f);
        }

        // Natural ground : it rolls, banks and wanders a little, never a straight road.
        public CourseBuilder Meadow(float pSeconds)
        {
            // Less plain ground (2026-11-11) : a long meadow ends on a run of slabs.
            if (pSeconds >= 3f && !_meadowSplit)
            {
                _meadowSplit = true;
                Meadow(pSeconds * 0.6f);
                Slabs(pSeconds * 0.4f, 1.1f);
                _meadowSplit = false;
                return this;
            }
            _module = "Meadow";
            pSeconds *= pace;
            // Hills here and there, flat elsewhere (2026-11-14) : one meadow in three or so stays flat.
            TrackBlock lMeadow = Open(BlockLook.Ground, 6f);
            if (Chance(0.7f)) lMeadow.mounds = Rand(10f, 24f) * SizeRoot;
            float lEnd = _s + M(pSeconds);
            DigCraters(_s, lEnd);
            while (_s < lEnd)
            {
                float lLength = M(Rand(1.2f, 2f));
                float lYaw = Rand(-1f, 1f) * Cap(6f, 8f, 10f);
                if (TurnRoom(lYaw > 0f ? 1 : -1) < Mathf.Abs(lYaw) + 5f) lYaw = -lYaw;
                float lPitch = Rand(-1f, 1f) * Cap(4f, 6f, 8f);
                float lRoll = Rand(-1f, 1f) * Cap(5f, 8f, 10f);
                Span(lLength * 0.5f, lYaw * 0.5f, lPitch, lRoll);
                Span(lLength * 0.5f, lYaw * 0.5f, -lPitch, -lRoll);
                // (The low floating islands are gone : the sky lanes are the routes above now, 2026-11-12.)
                // Rocks floating in the air over the edges (never on the ground) : they rush past and show the speed.
                if (Chance(0.7f))
                {
                    int lSide = Chance(0.5f) ? 1 : -1;
                    float lSize = Rand(8f, 20f) * PROP_SCALE * 0.6f;
                    _floatRocks.Add(new FloatRockDef { s = _s - lLength * Rand(0.2f, 0.8f), lateral = lSide * _halfWidth * Rand(0.75f, 1.05f), height = Rand(28f, 90f) * SizeRoot, size = lSize });
                }
            }
            _pitch = 0f;
            _roll = 0f;
            return this;
        }

        private bool _meadowSplit;

        // Huge platforms at different heights, rolled hard to one side or the other, some of them rolling forward like a
        // convoy you can land on. Always lower than the last : you drop onto them.
        public CourseBuilder Platforms(float pSeconds, int pMovers = 2)
        {
            _module = "Platforms";
            pSeconds *= pace;
            float lS0 = _s;
            float lEnd = _s + M(pSeconds);
            float lOffset = 0f;
            float lDrop = 0f;
            float lHalf = _halfWidth;
            // The ground before : the first platform must sit below it everywhere, even its raised edge.
            float lPrevDrop = 0f, lPrevOffset = 0f, lPrevHalf = lHalf, lPrevTilt = 0f;
            while (_s < lEnd)
            {
                float lLength = M(Rand(1f, 2f));
                float lNarrow = lHalf * Rand(0.45f, 0.8f);
                float lTilt = Rand(Cap(4f, 8f, 10f), Cap(8f, 12f, 14f)) * (Chance(0.5f) ? 1f : -1f);
                if (_s + lLength >= lEnd) lTilt = 0f;
                lDrop = Mathf.Max(lDrop, StepDown(lPrevDrop, lPrevOffset, lPrevHalf, lPrevTilt, lOffset, lNarrow, lTilt));
                Open(BlockLook.Slab, 0f, lNarrow, lOffset, lDrop, lTilt, Rand(70f, 150f));
                lPrevDrop = lDrop;
                lPrevOffset = lOffset;
                lPrevHalf = lNarrow;
                lPrevTilt = lTilt;
                float lBlockS = _s;
                Span(lLength);
                Close();
                Route(lBlockS, _s, lOffset - Mathf.Sign(lTilt) * lNarrow * 0.2f);
                lOffset = Mathf.Clamp(lOffset + Rand(-1f, 1f) * lHalf * 0.45f, -lHalf * 0.45f, lHalf * 0.45f);
                float lNext = lDrop + Rand(8f, 28f) * SizeRoot;
                if (Chance(0.35f))
                {
                    Act(_s - M(0.12f), RouteAction.Jump);
                    float lGapS = _s;
                    Span(M(Rand(0.1f, 0.2f)));
                    NoPads(lBlockS + lLength * 0.7f, _s + M(0.3f));
                    // Rocks floating in the void between the platforms, beside the jump line.
                    GapRocks(lGapS, _s, lOffset, lNarrow, lDrop);
                }
                lDrop = lNext;
            }
            // The ground after : below the last platform too.
            float lBack = Mathf.Max(lDrop, StepDown(lPrevDrop, lPrevOffset, lPrevHalf, lPrevTilt, 0f, lHalf, 0f));
            Close();
            Span(M(0.2f), 0f, 0f, 0f, Vector3.down * lBack);
            Open(BlockLook.Ground, 4f);
            float lSpan = _s - lS0;
            for (int i = 0; i < pMovers; i++)
            {
                float lWidth = lHalf * Rand(0.18f, 0.28f);
                _movers.Add(new MoverDef { s = lS0 + lSpan * (i + 0.3f) / pMovers, lateral = (i % 2 == 0 ? 1f : -1f) * lHalf * Rand(0.45f, 0.7f), width = lWidth, length = lWidth * Rand(2.2f, 3.2f), height = 8f, speed = _p.cruise * Rand(0.4f, 0.6f) });
            }
            NoPads(lS0, _s);
            return this;
        }

        // The smallest drop for block B so that, wherever it overlaps block A sideways, B's floor is under A's : you
        // always fall onto the next block, you never run into its face. Floor height is -drop - (lateral - offset) * sin.
        private static float StepDown(float pDropA, float pOffA, float pHalfA, float pTiltA, float pOffB, float pHalfB, float pTiltB)
        {
            float lFrom = Mathf.Max(pOffA - pHalfA, pOffB - pHalfB);
            float lTo = Mathf.Min(pOffA + pHalfA, pOffB + pHalfB);
            if (lTo < lFrom) return pDropA + 8f;
            float lSinA = Mathf.Sin(pTiltA * Mathf.Deg2Rad);
            float lSinB = Mathf.Sin(pTiltB * Mathf.Deg2Rad);
            float lNeed = float.MinValue;
            foreach (float lX in new[] { lFrom, lTo })
                lNeed = Mathf.Max(lNeed, pDropA + (lX - pOffA) * lSinA - (lX - pOffB) * lSinB + 8f);
            return lNeed;
        }

        // A raised lane above the track, reached from a launch strip : full of rings, faster for those who take it.
        public CourseBuilder Shortcut(float pSeconds = 4f)
        {
            _module = "Shortcut";
            float lS0 = _s;
            Open(BlockLook.Road, 4f);
            Span(M(pSeconds * pace + 1f));
            float lLateral = Rand(-0.45f, 0.45f) * _halfWidth;
            float lHeight = 20f * SizeRoot;
            _strips.Add(new StripDef { s = lS0 + M(0.35f), lateral = lLateral, halfWidth = _halfWidth * 0.14f, up = Mathf.Sqrt(2f * Ballistics.G_RISE * (lHeight + 6f)) * 1.15f });
            BridgeDef lBridge = new BridgeDef { s0 = lS0 + M(0.8f), s1 = _s - M(0.2f), lateral = lLateral, height = lHeight, halfWidth = _halfWidth * 0.16f };
            _bridges.Add(lBridge);
            for (float lS = lBridge.s0 + M(0.4f); lS < lBridge.s1 - M(0.2f); lS += M(0.45f))
                _rings.Add(new RingDef { s = lS, lateral = lLateral, height = lHeight + 20f * SizeRoot, radius = 22f * SizeRoot });
            NoPads(lS0, _s);
            return this;
        }

        // ================================================================ 2026-11-14 : parallel routes, spirals, rocks

        // Rocks floating in the void of a gap, beside the jump line, around the level of the platforms : nothing stands on
        // the ground, everything hangs between the platforms or in the air.
        private void GapRocks(float pS0, float pS1, float pOffset, float pHalf, float pDrop)
        {
            int lCount = 1 + _random.Next(0, 2 + Mathf.RoundToInt(difficulty * 2f));
            for (int i = 0; i < lCount; i++)
            {
                float lSide = Chance(0.5f) ? 1f : -1f;
                float lSize = Rand(14f, 36f) * SizeRoot;
                _floatRocks.Add(new FloatRockDef
                {
                    s = Mathf.Lerp(pS0, pS1, Rand(0.1f, 0.9f)),
                    lateral = pOffset + lSide * (pHalf * Rand(0.55f, 1.15f) + lSize),
                    height = -pDrop + Rand(-90f, 25f) * SizeRoot,
                    size = lSize
                });
            }
        }

        // Rocks in the air at both sides of a stretch, well above the edges : they rush past and show the speed.
        private void AirRocks(float pS0, float pS1, float pDensity = 1f)
        {
            float lStep = M(Rand(0.7f, 1.2f)) / Mathf.Max(0.2f, pDensity);
            for (float lS = pS0 + lStep * 0.5f; lS < pS1; lS += lStep * Rand(0.7f, 1.3f))
            {
                float lSide = Chance(0.5f) ? 1f : -1f;
                float lSize = Rand(10f, 30f) * SizeRoot;
                _floatRocks.Add(new FloatRockDef { s = lS, lateral = lSide * _halfWidth * Rand(0.8f, 1.25f), height = Rand(30f, 120f) * SizeRoot, size = lSize });
            }
        }

        // Helter-skelter : the line winds down round a wide helix, banked inward, each round well under the last. A pipe
        // with low rims, so nothing of one round ever reaches the round above.
        public CourseBuilder Spiral(Side pSide = Side.Right, int pTurns = 1)
        {
            _module = "Spiral";
            int lSide = (int)pSide;
            float lHalf = _halfWidth;
            _halfWidth = Mathf.Min(_halfWidth, Mathf.Max(110f, _p.cruise * 0.8f));
            // About fifteen seconds a round at cruise, whatever the stage : a long, readable sweep.
            float lRadius = Mathf.Max(_p.cruise * 2.2f, _halfWidth * 2f + 60f);
            float lDegrees = 360f * Mathf.Max(1, pTurns);
            float lLength = lRadius * lDegrees * Mathf.Deg2Rad;
            float lDropPerTurn = Mathf.Max(330f * SizeRoot, _halfWidth * 0.8f);
            float lPitch = -Mathf.Atan2(lDropPerTurn, lRadius * 2f * Mathf.PI) * Mathf.Rad2Deg;
            float lBank = Cap(14f, 18f, 22f);
            // Ground before it : whoever arrives in the air lands first, then enters.
            Open(BlockLook.Road, 4f);
            Span(M(1.2f));
            // High rims (well under the next round) : the helix holds a runner at light speed.
            Open(BlockLook.Pipe, 14f);
            Span(M(0.8f));
            // The helix carries you like a loop does : gravity follows its surface, so even at light speed it holds you.
            GravityGate(_s - M(0.3f));
            float lHelix0 = _s;
            _localGravity = true;
            Span(lLength * 0.08f, lSide * lDegrees * 0.02f, lPitch, lSide * lBank);
            Span(lLength * 0.84f, lSide * lDegrees * 0.96f, 0f, 0f, default(Vector3), 1f);
            Span(lLength * 0.08f, lSide * lDegrees * 0.02f, -lPitch, -lSide * lBank);
            _localGravity = false;
            NoPads(lHelix0, _s);
            Route(lHelix0, _s, 0f);
            // Whole turns : the heading comes back where it was, the stage carries on away from its past.
            _yaw -= lSide * lDegrees;
            _pitch = 0f;
            _roll = 0f;
            Span(M(0.8f));
            _halfWidth = lHalf;
            Open(BlockLook.Ground, 6f);
            return this;
        }

        // Parallel routes at the same place (asked 2026-11-14) : a pad line on the ground, a rope high above that same line,
        // ring chains in the air on the left and on the right, a launch strip under each. Take one, switch on the way.
        public CourseBuilder Parallel(float pSeconds)
        {
            _module = "Parallel";
            pSeconds *= pace;
            float lS0 = _s;
            Open(BlockLook.Road, 4f);
            Span(M(0.9f));
            float lStart = _s;
            Span(M(pSeconds));
            float lEnd = _s;
            float lPadLateral = Rand(-0.15f, 0.15f) * _halfWidth;
            for (float lS = lStart + M(0.15f); lS < lEnd - M(0.2f); lS += M(Mathf.Lerp(0.6f, 0.38f, difficulty)))
                _pads.Add(new PadDef { s = lS, lateral = lPadLateral, width = _halfWidth * 0.26f, length = M(0.12f), boost = 0.35f });
            // Above the pads : a rope over that same line, reached from a strip just before it.
            float lRopeLateral = lPadLateral + (Chance(0.5f) ? 1f : -1f) * _halfWidth * 0.1f;
            float lRopeHigh = ROPE_HIGH * 1.5f;
            _strips.Add(new StripDef { s = lS0 + M(0.5f), lateral = lRopeLateral, halfWidth = _halfWidth * 0.14f, up = Mathf.Sqrt(2f * Ballistics.G_RISE * (lRopeHigh + 8f)) });
            _ropes.Add(new RopeDef { s0 = lStart + M(0.05f), s1 = lEnd - M(0.1f), lateral = lRopeLateral, height0 = lRopeHigh, height1 = lRopeHigh * 0.85f });
            // Left and right : rings in the air, nothing under them but the wide ground far below.
            for (int lSide = -1; lSide <= 1; lSide += 2)
            {
                float lLateral = lSide * _halfWidth * Rand(0.55f, 0.72f);
                float lHeight = Rand(38f, 60f) * SizeRoot;
                _strips.Add(new StripDef { s = lS0 + M(0.55f), lateral = lLateral, halfWidth = _halfWidth * 0.13f, up = Mathf.Sqrt(2f * Ballistics.G_RISE * (lHeight + 10f)) });
                int lCount = Mathf.Max(4, Mathf.RoundToInt((lEnd - lStart) / M(0.3f)));
                for (int i = 0; i < lCount; i++)
                {
                    float lT = (i + 0.5f) / lCount;
                    _rings.Add(new RingDef
                    {
                        s = Mathf.Lerp(lStart, lEnd - M(0.2f), lT),
                        lateral = lLateral + Mathf.Sin(lT * Mathf.PI * 2f + lSide) * _halfWidth * 0.08f,
                        height = lHeight + Mathf.Sin(lT * Mathf.PI) * 20f * SizeRoot,
                        radius = 26f * SizeRoot
                    });
                }
            }
            AirRocks(lStart, lEnd, 0.6f);
            Route(lS0, lEnd, lPadLateral);
            NoPads(lS0, lEnd);
            return this;
        }

        // The road rises at the end of a stretch and throws you into the next part : a launching pad.
        public CourseBuilder LaunchRamp(float pSeconds = 1.6f, float pDegrees = 12f)
        {
            _module = "LaunchRamp";
            float lLength = M(pSeconds);
            Open(BlockLook.Road, 4f);
            Span(lLength * 0.6f, 0f, Mathf.Min(pDegrees, Cap(10f, 12f, 14f)));
            float lLip = _s + lLength * 0.4f;
            Span(lLength * 0.4f);
            _strips.Add(new StripDef { s = lLip - M(0.15f), lateral = 0f, halfWidth = _halfWidth * 0.6f, up = 80f * Mathf.Pow(Mathf.Max(1f, size), 0.25f) });
            Act(lLip - M(0.1f), RouteAction.Jump);
            NoPads(lLip - M(0.6f), lLip + M(0.6f));
            _pitch = 0f;
            // The ground after the lip is lower : you land, you never hit a face.
            Close();
            Span(M(0.25f), 0f, 0f, 0f, Vector3.down * 30f * SizeRoot);
            Open(BlockLook.Ground, 6f);
            return this;
        }

        // ================================================================ sky lanes (2026-11-12, the ant-hill)

        // A route in the sky over the next pSeconds of whatever line follows : a kicker on the ground lane throws you up
        // to floating decks linked by ropes hung in the void, ring chains with nothing under them, pad decks that chain
        // into hyper and launch strips climbing to a higher level. Missing it costs nothing : the ground lane goes on.
        // pLevel : 0 the lowest sky level of this world, higher is higher. pStyle : 0 ropes, 1 rings, 2 pads, 3 climb,
        // -1 a mix.
        public CourseBuilder Sky(float pSeconds, int pLevel = 0, Side pSide = Side.Right, int pStyle = -1)
        {
            _lanes.Add(new LaneDef { s0 = _s, s1 = _s + M(pSeconds), lateral = (int)pSide * _halfWidth * Rand(0.2f, 0.4f), level = pLevel, style = pStyle });
            return this;
        }

        // Side loop (2026-11-12) : beside the line an electric rope runs along the ground, winds up into a vertical loop
        // and comes back down further on. Catch it as you pass, ride the loop upside down, get thrown out ahead with a
        // boost (a step of the chain). Built on a straight flat stretch, with no random draw : the rest of the stage keeps
        // the choices it had.
        public CourseBuilder SideLoop(Side pSide = Side.Right)
        {
            _module = "SideLoop";
            float lRadius = Mathf.Clamp(_p.cruise * 0.22f, 30f * SizeRoot, 110f * SizeRoot);
            float lAdvance = lRadius * 0.45f;
            float lLead = Mathf.Max(90f, _p.cruise * 0.4f);
            float lLoop = lAdvance * Mathf.PI * 2f;
            if (_open == null || _open.look == BlockLook.Slab || _open.look == BlockLook.Pipe || _open.look == BlockLook.Plaza
                || Mathf.Abs(_open.drop) > 0.01f || Mathf.Abs(_open.tilt) > 0.01f)
                Open(BlockLook.Road, 0f, -1f, 0f, 0f, 0f, 30f);
            float lLateral = _open.offset + (int)pSide * _open.halfWidth * 0.5f;
            float lDrift = -(int)pSide * Mathf.Min(lRadius * 0.4f, _open.halfWidth * 0.3f);
            // A straight run-up to line up with the rope, the rope, a short run-out.
            float lApproach = _s;
            float lS0 = _s + M(1.2f);
            float lS1 = lS0 + lLead * 2f + lLoop;
            Span(lS1 + M(0.3f) - _s);
            _loops.Add(new LoopDef { s0 = lS0, s1 = lS1, lateral = lLateral, drift = lDrift, radius = lRadius, advance = lAdvance, lead = lLead });
            Route(lApproach, lS0 + lLead, lLateral);
            Route(lS0 + lLead + 1f, _s, lLateral + lDrift);
            NoPads(lApproach, _s);
            return this;
        }

        // Pads in one straight line, close enough to chain (the chain is what opens hyper on the stages that have it).
        public CourseBuilder PadLine(float pSeconds, float pEvery = 0.4f, Side pSide = Side.Left)
        {
            _module = "PadLine";
            float lS0 = _s;
            Open(BlockLook.Road, 4f);
            Span(M(pSeconds));
            float lLateral = (int)pSide * _halfWidth * Rand(0.1f, 0.35f);
            for (float lT = 0.3f; lT < pSeconds - 0.2f; lT += pEvery)
                _pads.Add(new PadDef { s = lS0 + M(lT), lateral = lLateral, width = _halfWidth * 0.32f, length = M(0.12f), boost = 0.35f });
            Route(lS0, _s, lLateral);
            NoPads(lS0, _s);
            return this;
        }

        // Sky levels (metres above the line) per world : street 70 / 120, machine 150 / 280 / 400, cosmic 200 to 650.
        private float LaneHeight(int pLevel)
        {
            float[] lLevels = world <= 0 ? new[] { 70f, 120f } : world == 1 ? new[] { 150f, 280f, 400f } : new[] { 200f, 350f, 500f, 650f };
            return lLevels[Mathf.Clamp(pLevel, 0, lLevels.Length - 1)] * Mathf.Pow(Mathf.Max(1f, size), 0.35f);
        }

        // World altitude of the floor under the sky point (s, lateral) (lanes are level shelves : absolute heights).
        private float FloorAbove(float pS, float pLateral)
        {
            if (_layout.field.BlockAt(pS) == null) return -100000f;
            SpineSample lSample = _layout.spine.Evaluate(pS);
            // The sky point's lateral is measured flat ; close enough to the track's own lateral on gentle roll.
            TrackBlock lBlock = _layout.field.BlockAt(pS);
            float lLateral = Mathf.Clamp(pLateral, lBlock.offset - lBlock.halfWidth - 200f, lBlock.offset + lBlock.halfWidth + 200f);
            float lFloor = _layout.field.Point(pS, lLateral).y;
            // Walls and mountains rise at the sides : keep clear of them too.
            float lSide = Mathf.Abs(pLateral - lBlock.offset) - lBlock.halfWidth;
            if (lSide > 0f) lFloor += Mathf.Max(lBlock.WallRun(lBlock.wallLeft) > 0f ? Mathf.Max(lBlock.wallLeft, lBlock.wallRight) : 0f, 0f);
            return Mathf.Max(lFloor, lSample.position.y - 400f);
        }

        // A stretch where a deck may hang : no loop, no pipe, no cliff under it, the line roughly level.
        private bool LaneOk(float pS0, float pS1)
        {
            if (pS0 < 0f || pS1 > _layout.finishS - M(2f)) return false;
            foreach (Vector2 lHighway in _highways) if (pS1 >= lHighway.x && pS0 <= lHighway.y) return false;
            float lLast = _layout.spine.Evaluate(pS0).position.y;
            for (float lS = pS0; lS <= pS1; lS += 20f)
            {
                SpineSample lSample = _layout.spine.samples[_layout.spine.IndexOf(lS)];
                if (lSample.localGravity || lSample.up.y < 0.85f) return false;
                TrackBlock lBlock = _layout.field.BlockAt(lS);
                if (lBlock != null && lBlock.look == BlockLook.Pipe) return false;
                // Decks are level shelves : the line may drop away under them, only a steep climb would reach them.
                if (lSample.position.y - lLast > 20f * 0.45f) return false;
                lLast = lSample.position.y;
            }
            return true;
        }

        // The next deck's altitude : the wanted one, raised to clear the ground under it, and -1 when that would be
        // higher than pMax (a step up a rope or a gap cannot climb).
        private float NextHeight(float pS0, float pS1, float pLateral, float pHalfWidth, float pWanted, float pMax)
        {
            float lClear = DeckHeight(pS0, pS1, pLateral, pHalfWidth, -100000f);
            float lHeight = Mathf.Max(pWanted, lClear);
            return lHeight > pMax ? -1f : lHeight;
        }

        // The deck height over a span : at least its level, and well clear of any floor under it.
        private float DeckHeight(float pS0, float pS1, float pLateral, float pHalfWidth, float pLevelHeight)
        {
            float lHighest = -1000f;
            for (float lS = pS0; lS <= pS1; lS += 20f)
                for (int k = -1; k <= 1; k++) lHighest = Mathf.Max(lHighest, FloorAbove(lS, pLateral + k * pHalfWidth));
            return Mathf.Max(pLevelHeight, lHighest + 55f);
        }

        private int _laneDecks;
        private int _laneRopes;

        private void BuildLanes()
        {
            foreach (LaneDef lLane in _lanes)
            {
                // A lane stopped by a loop, a pipe or a cliff starts again behind it with a new kicker.
                LaneDef lPart = lLane;
                for (int lTry = 0; lTry < 6 && lPart.s1 - lPart.s0 > M(8f); lTry++)
                {
                    float lStopped = BuildLane(lPart);
                    lPart.s0 = Mathf.Max(lPart.s0 + M(3f), lStopped + M(1.5f));
                }
            }
            Debug.Log("[HS-LD] sky lanes : " + _lanes.Count + " asked, " + _kickers.Count + " kickers, " + _laneDecks + " decks, " + _laneRopes + " ropes in the void");
        }

        // Builds one lane from its start ; returns where it stopped.
        private float BuildLane(LaneDef pLane)
        {
            float lCruise = _p.cruise;
            float lHalf = _layout.field.HalfWidth(pLane.s0);
            float lLateral = pLane.lateral;
            int lLevel = pLane.level;
            float lHeight = LaneHeight(lLevel);
            // Kicker : a ramp in the ground lane, its lip throws you up to the first deck.
            float lKick0 = pLane.s0;
            float lKickLength = M(0.45f);
            for (int lTry = 0; lTry < 10 && !KickerOk(lKick0, lKick0 + lKickLength); lTry++) lKick0 += M(0.5f);
            if (!KickerOk(lKick0, lKick0 + lKickLength)) return lKick0;
            float lKickHeight = lKickLength * Mathf.Tan(15f * Mathf.Deg2Rad);
            float lLip = lKick0 + lKickLength;
            float lKickFloor = _layout.field.Point(lLip, lLateral).y;
            // The lane's altitude : its level above the ground where it starts.
            lHeight += lKickFloor;
            float lLevelBase = lKickFloor;
            float lRise = Mathf.Max(30f, lHeight - (lKickFloor + lKickHeight) + 25f);
            float lUp = Mathf.Sqrt(2f * Ballistics.G_RISE * lRise);
            float lFlight = lUp / Ballistics.G_RISE + Mathf.Sqrt(2f * 25f / Ballistics.G_FALL);
            // Lands you anywhere from 0.85x to 1.8x cruise (2.6x where hyper exists).
            float lDeck0 = lLip + 0.85f * lCruise * lFlight;
            float lDeckLength = Mathf.Max(M(1.5f), (_p.hyperEnabled ? 1.75f : 0.95f) * lCruise * lFlight);
            float lDeckHalf = lHalf * Rand(0.13f, 0.2f);
            if (!LaneOk(lDeck0, lDeck0 + lDeckLength)) return lKick0;
            float lDeckH = DeckHeight(lDeck0, lDeck0 + lDeckLength, lLateral, lDeckHalf, lHeight);
            _kickers.Add(new KickerDef { s0 = lKick0, s1 = lLip, lateral = lLateral, halfWidth = Mathf.Max(lDeckHalf, lHalf * 0.12f), height = lKickHeight, up = lUp });
            NoPads(lKick0 - M(0.6f), lLip + M(0.3f));

            float lS = lDeck0;
            float lEnd = lDeck0 + lDeckLength;
            AddDeck(lS, lEnd, lLateral, lDeckH, lDeckHalf, pLane.style == 2);
            int lStep = 0;
            while (lEnd < pLane.s1 && lStep < 24)
            {
                lStep++;
                // Ropes in the void are the signature of the sky routes : about a third of every lane, whatever its style.
                int lStyle = Chance(0.35f) ? 0 : pLane.style >= 0 && Chance(0.7f) ? pLane.style : _random.Next(0, 4);
                float lNextHalf = lHalf * Rand(0.13f, 0.2f);
                float lNextLateral = Mathf.Clamp(lLateral + Rand(-1f, 1f) * lHalf * 0.18f, -lHalf * 0.55f, lHalf * 0.55f);
                if (lStyle == 0)
                {
                    // A rope in the void to the next deck, a little lower.
                    float lGap = M(Rand(1.4f, 2.4f));
                    float lNext0 = lEnd + lGap;
                    float lNextEnd = lNext0 + M(Rand(1.2f, 2f));
                    if (!LaneOk(lEnd, lNextEnd)) break;
                    float lNextH = NextHeight(lNext0, lNextEnd, lLateral, lNextHalf, Mathf.Min(lHeight, lDeckH), lDeckH + 30f);
                    if (lNextH < 0f) break;
                    _ropes.Add(new RopeDef { s0 = lEnd - M(0.12f), s1 = lNext0 + M(0.15f), lateral = lLateral, height0 = lDeckH + 7f, height1 = lNextH + 6f, sky = true });
                    _laneRopes++;
                    // Rocks hanging in the void beside the rope.
                    for (int r = 0; r < 2; r++)
                        _floatRocks.Add(new FloatRockDef { s = Mathf.Lerp(lEnd, lNext0, Rand(0.2f, 0.8f)), lateral = lLateral + (r == 0 ? 1f : -1f) * Rand(70f, 170f) * SizeRoot, height = Mathf.Lerp(lDeckH, lNextH, 0.5f) + Rand(-60f, 40f) * SizeRoot, size = Rand(16f, 40f) * SizeRoot, sky = true });
                    AddDeck(lNext0, lNextEnd, lLateral, lNextH, lNextHalf, false);
                    lEnd = lNextEnd;
                    lDeckH = lNextH;
                }
                else if (lStyle == 1)
                {
                    // A strip on the lip, rings over the void, the next deck lower : fly through the chain.
                    float lGap = M(Rand(1.1f, 1.6f));
                    float lNext0 = lEnd + lGap;
                    float lNextEnd = lNext0 + M(Rand(1.2f, 2f));
                    if (!LaneOk(lEnd, lNextEnd)) break;
                    float lNextH = NextHeight(lNext0, lNextEnd, lNextLateral, lNextHalf, lDeckH - 25f, lDeckH + 5f);
                    if (lNextH < 0f) break;
                    _deckStrips.Add(new DeckStripDef { s = lEnd - M(0.15f), lateral = lLateral, height = lDeckH, halfWidth = lDeckHalf * 0.8f, up = 42f });
                    int lRings = Mathf.Max(3, Mathf.RoundToInt((lNext0 - lEnd) / M(0.3f)));
                    for (int r = 0; r < lRings; r++)
                    {
                        float lT = (r + 0.5f) / lRings;
                        float lArc = Mathf.Sin(lT * Mathf.PI) * 8f;
                        _rings.Add(new RingDef { s = Mathf.Lerp(lEnd, lNext0, lT), lateral = Mathf.Lerp(lLateral, lNextLateral, lT), height = Mathf.Lerp(lDeckH, lNextH, lT) + 8f + lArc, radius = 24f, line = true });
                    }
                    AddDeck(lNext0, lNextEnd, lNextLateral, lNextH, lNextHalf, false);
                    lEnd = lNextEnd;
                    lDeckH = lNextH;
                    lLateral = lNextLateral;
                    lDeckHalf = lNextHalf;
                }
                else if (lStyle == 2)
                {
                    // A short gap onto a long pad deck : the chain grows.
                    float lNext0 = lEnd + M(Rand(0.2f, 0.35f));
                    float lNextEnd = lNext0 + M(Rand(2f, 3f));
                    if (!LaneOk(lEnd, lNextEnd)) break;
                    float lNextH = NextHeight(lNext0, lNextEnd, lNextLateral, lNextHalf, lDeckH - 8f, lDeckH + 2f);
                    if (lNextH < 0f) break;
                    AddDeck(lNext0, lNextEnd, lNextLateral, lNextH, lNextHalf, true);
                    lEnd = lNextEnd;
                    lDeckH = lNextH;
                    lLateral = lNextLateral;
                    lDeckHalf = lNextHalf;
                }
                else
                {
                    // Climb : a strip on the lip throws you up to the next level.
                    float lTargetH = LaneHeight(lLevel + 1) + lLevelBase;
                    if (lTargetH <= lDeckH + 20f) { lStyle = 0; continue; }
                    float lClimbUp = Mathf.Sqrt(2f * Ballistics.G_RISE * (lTargetH - lDeckH + 25f));
                    float lClimbFlight = lClimbUp / Ballistics.G_RISE + Mathf.Sqrt(2f * 25f / Ballistics.G_FALL);
                    float lNext0 = lEnd + 0.85f * lCruise * lClimbFlight;
                    float lNextEnd = lNext0 + Mathf.Max(M(1.5f), (_p.hyperEnabled ? 1.75f : 0.95f) * lCruise * lClimbFlight);
                    if (!LaneOk(lEnd, lNextEnd)) break;
                    float lNextH = DeckHeight(lNext0, lNextEnd, lNextLateral, lNextHalf, lTargetH);
                    _deckStrips.Add(new DeckStripDef { s = lEnd - M(0.15f), lateral = lLateral, height = lDeckH, halfWidth = lDeckHalf * 0.8f, up = lClimbUp });
                    AddDeck(lNext0, lNextEnd, lNextLateral, lNextH, lNextHalf, false);
                    lLevel++;
                    lHeight = lTargetH;
                    lEnd = lNextEnd;
                    lDeckH = lNextH;
                    lLateral = lNextLateral;
                    lDeckHalf = lNextHalf;
                }
            }
            return lEnd;
        }

        private bool KickerOk(float pS0, float pS1)
        {
            for (float lS = pS0 - M(0.3f); lS <= pS1 + M(0.3f); lS += 10f)
            {
                if (!_layout.field.Solid(lS)) return false;
                SpineSample lSample = _layout.spine.samples[_layout.spine.IndexOf(lS)];
                if (lSample.localGravity) return false;
                TrackBlock lBlock = _layout.field.BlockAt(lS);
                if (lBlock.look == BlockLook.Pipe || lBlock.look == BlockLook.Slab || lBlock.look == BlockLook.Plaza) return false;
            }
            return true;
        }

        private void AddDeck(float pS0, float pS1, float pLateral, float pHeight, float pHalf, bool pPads)
        {
            _bridges.Add(new BridgeDef { s0 = pS0, s1 = pS1, lateral = pLateral, height = pHeight, halfWidth = pHalf, sky = true });
            _laneDecks++;
            if (!pPads) return;
            for (float lS = pS0 + M(0.25f); lS < pS1 - M(0.2f); lS += M(0.38f))
                _deckPads.Add(new DeckPadDef { s = lS, lateral = pLateral, height = pHeight, width = pHalf * 1.2f, length = M(0.12f), boost = 0.35f });
        }

        // ================================================================ light highway (2026-11-12)

        // Built for light speed : pSeconds at light speed (12 to 20 km/s), so 100 to 300 km of road two kilometres wide,
        // gently bending, rimmed by mountains, lit posts racing past along the edges and arches every few kilometres.
        // Whoever reaches it without light still rides its stream (PlayerController : 40 % of light speed).
        public CourseBuilder LightHighway(float pSeconds)
        {
            _module = "LightHighway";
            if (!_p.LightEnabled) return Road(pSeconds);
            float lHalf = _halfWidth;
            float lS0 = _s;
            _halfWidth = Mathf.Max(_halfWidth * 4f, 2000f);
            Open(BlockLook.Road, 8f);
            float lLength = pSeconds * _p.lightSpeed;
            int lPieces = Mathf.Max(3, Mathf.RoundToInt(lLength / 25000f));
            int lSide = Chance(0.5f) ? 1 : -1;
            for (int i = 0; i < lPieces; i++)
            {
                float lYaw = lSide * Rand(8f, 16f);
                if (TurnRoom(lSide) < Mathf.Abs(lYaw) + 5f) lYaw = -lYaw;
                Span(lLength / lPieces, lYaw, 0f, 0f, default(Vector3), 1f);
                lSide = -lSide;
            }
            _highways.Add(new Vector2(lS0, _s));
            NoPads(lS0 - M(1f), _s + M(1f));
            _halfWidth = lHalf;
            Open(BlockLook.Road, 4f);
            Span(M(1.5f));
            return this;
        }

        private readonly List<Vector2> _highways = new List<Vector2>();

        // Meteorites coming at you : dash through them.
        public CourseBuilder Meteors(float pSeconds, float pEverySeconds = 2f)
        {
            _module = "Meteors";
            pSeconds *= pace;
            float lS0 = _s;
            Open(BlockLook.Ground, 5f);
            Span(M(pSeconds));
            for (float lT = pEverySeconds * 0.5f; lT < pSeconds; lT += pEverySeconds)
                _meteors.Add(new MeteorDef { s = lS0 + M(lT), fromSky = true, radius = Rand(12f, 22f) * PROP_SCALE });
            return this;
        }

        // Last straight : pads everywhere, the world narrows, everything goes.
        public CourseBuilder FinalRush(float pSeconds = 7f)
        {
            _module = "FinalRush";
            float lS0 = _s;
            // The last straight : the rival comes alongside, the bars close in, the colours burn.
            _layout.climaxS = lS0 - M(1.2f);
            float lHalf = _halfWidth;
            Open(BlockLook.Road, 8f);
            Span(M(pSeconds * 0.5f), 0f, -10f);
            Span(M(pSeconds * 0.5f), 0f, 10f);
            _pitch = 0f;
            // 2026-11-14 : the last straight is full of pads : one lane on the first stages, three lanes on the last ones.
            int lLanes = difficulty < 0.3f ? 0 : 1;
            for (float lT = 0.15f; lT < pSeconds; lT += Mathf.Lerp(0.32f, 0.2f, difficulty))
                for (int lLane = -lLanes; lLane <= lLanes; lLane++)
                    _pads.Add(new PadDef { s = lS0 + M(lT + (lLane + 1) * 0.07f), lateral = lLane * _halfWidth * 0.55f + Mathf.Sin(lT * 2.1f + lLane) * _halfWidth * 0.12f, width = _halfWidth * 0.42f, length = M(0.13f), boost = 0.25f });
            for (float lT = 0f; lT < pSeconds; lT += 0.5f) _arches.Add(new ArchDef { s = lS0 + M(lT), lit = true });
            _halfWidth = lHalf;
            NoPads(lS0, _s);
            return this;
        }

        public CourseBuilder Finish()
        {
            _module = "Finish";
            Open(BlockLook.Road, 6f);
            Span(M(0.6f));
            _layout.finishS = _s;
            NoPads(_s - M(0.8f), _s + 99999f);
            // A long run-out : the runner keeps running past the line while the results show.
            Span(M(10f));
            Close();
            return this;
        }

        // ================================================================ build

        public LevelLayout Build()
        {
            return StageBuilder.Build(Bake(), _theme, _p, _root, _noItems);
        }

        // The whole stage as data : the line, the blocks, every object, the autopilot guidance. Random choices are made
        // here once, so building from this data gives exactly this stage.
        public StageData Bake()
        {
            if (!_started) throw new InvalidOperationException("CourseBuilder.Start() was not called");
            Close();

            TrackField lField = new TrackField(_layout.spine);
            lField.blocks.AddRange(_blocks);
            lField.Seal();
            _layout.field = lField;
            FixRises(lField);

            StageData lData = new StageData
            {
                altitude = _altitude, seed = _seed, dives = _dives, startLineS = _layout.startLineS, spawnS = _layout.spawnS,
                finishS = _layout.finishS, climaxS = _layout.climaxS
            };
            lData.highways.AddRange(_highways);
            _layout.highways.AddRange(_highways);
            lData.segments.AddRange(_segments);
            foreach (TrackBlock lBlock in _blocks) lData.blocks.Add(BlockData.From(lBlock));
            List<ItemData> lItems = lData.items;

            if (!_noItems)
            {
                BuildLanes();
                foreach (BlockDef lProp in _props)
                    lItems.Add(new ItemData { kind = ItemKind.Prop, s = lProp.s, lateral = lProp.lateral, width = lProp.width, length = lProp.length, height = lProp.height, value = lProp.lean, variant = lProp.palette, jitter = Rand(-14f, 14f), rough = Rand(0.12f, 0.3f) });
                foreach (StripDef lStrip in _strips) lItems.Add(new ItemData { kind = ItemKind.Strip, s = lStrip.s, lateral = lStrip.lateral, width = lStrip.halfWidth, value = lStrip.up });
                foreach (RopeDef lRope in _ropes) lItems.Add(new ItemData { kind = ItemKind.Rope, s = lRope.s0, s1 = lRope.s1, lateral = lRope.lateral, height = lRope.height0, height1 = lRope.height1, flag = lRope.sky });
                foreach (LoopDef lLoop in _loops) lItems.Add(new ItemData { kind = ItemKind.LoopRope, s = lLoop.s0, s1 = lLoop.s1, lateral = lLoop.lateral, value = lLoop.drift, size = lLoop.radius, length = lLoop.advance, width = lLoop.lead });
                foreach (RingDef lRing in _rings) lItems.Add(new ItemData { kind = ItemKind.Ring, s = lRing.s, lateral = lRing.lateral, height = lRing.height, size = lRing.radius, flag = lRing.line });
                foreach (KickerDef lKicker in _kickers) lItems.Add(new ItemData { kind = ItemKind.Kicker, s = lKicker.s0, s1 = lKicker.s1, lateral = lKicker.lateral, width = lKicker.halfWidth, height = lKicker.height, value = lKicker.up });
                foreach (DeckPadDef lPad in _deckPads) lItems.Add(new ItemData { kind = ItemKind.DeckPad, s = lPad.s, lateral = lPad.lateral, height = lPad.height, width = lPad.width, length = lPad.length, value = lPad.boost });
                foreach (DeckStripDef lStrip in _deckStrips) lItems.Add(new ItemData { kind = ItemKind.DeckStrip, s = lStrip.s, lateral = lStrip.lateral, height = lStrip.height, width = lStrip.halfWidth, value = lStrip.up });
                foreach (CrateDef lCrate in _crates) lItems.Add(new ItemData { kind = ItemKind.Crate, s = lCrate.s, lateral = lCrate.lateral, size = lCrate.size, flag = lCrate.moving });
                foreach (LaserDef lLaser in _lasers) lItems.Add(new ItemData { kind = ItemKind.Laser, s = lLaser.s, height = lLaser.height, value = lLaser.spin, variant = lLaser.beams });
                foreach (HammerDef lHammer in _hammers) lItems.Add(new ItemData { kind = ItemKind.Hammer, s = lHammer.s, value = lHammer.phase });
                PlaceBreakWalls();
                foreach (MoverDef lMover in _movers) lItems.Add(new ItemData { kind = ItemKind.Mover, s = lMover.s, lateral = lMover.lateral, width = lMover.width, length = lMover.length, height = lMover.height, value = lMover.speed });
                foreach (BridgeDef lBridge in _bridges) lItems.Add(new ItemData { kind = ItemKind.Bridge, s = lBridge.s0, s1 = lBridge.s1, lateral = lBridge.lateral, height = lBridge.height, width = lBridge.halfWidth, flag = lBridge.sky });
                PlaceJumpStrips(lItems);
                foreach (PaneDef lPane in _panes) lItems.Add(new ItemData { kind = ItemKind.Pane, s = lPane.s, lateral = lPane.lateral, width = lPane.width, height = lPane.height, variant = (int)lPane.kind });
                PlacePads(lItems);
                foreach (MeteorDef lMeteor in _meteors) lItems.Add(new ItemData { kind = ItemKind.Meteor, s = lMeteor.s, lateral = lMeteor.lateral, size = lMeteor.radius, flag = lMeteor.fromSky });
                foreach (CraterDef lCrater in _craters)
                    if (_layout.field.Solid(lCrater.s) && !_layout.spine.samples[_layout.spine.IndexOf(lCrater.s)].localGravity)
                        lItems.Add(new ItemData { kind = ItemKind.Crater, s = lCrater.s, lateral = lCrater.lateral, size = lCrater.radius });
                foreach (FloatRockDef lRock in _floatRocks) lItems.Add(new ItemData { kind = ItemKind.FloatRock, s = lRock.s, lateral = lRock.lateral, height = lRock.height, size = lRock.size, flag = lRock.sky, variant = _random.Next(0, 6), jitter = Rand(0f, 360f) });
            }
            foreach (ArchDef lArch in _arches) lItems.Add(new ItemData { kind = ItemKind.Arch, s = lArch.s, flag = lArch.lit });
            foreach (float lGate in _gravityGates) lItems.Add(new ItemData { kind = ItemKind.GravityGate, s = lGate });

            _layout.route.Sort((pA, pB) => pA.s0.CompareTo(pB.s0));
            _layout.acts.Sort((pA, pB) => pA.s.CompareTo(pB.s));
            foreach (RouteNode lNode in _layout.route) lData.route.Add(new RouteData { s0 = lNode.s0, s1 = lNode.s1, lateral = lNode.lateral });
            foreach (RouteAct lAct in _layout.acts) lData.acts.Add(new ActData { s = lAct.s, action = lAct.action });
            foreach (KeyValuePair<float, string> lHint in _layout.hints) lData.hints.Add(new HintData { s = lHint.Key, text = lHint.Value });
            return lData;
        }

        // The level rule, enforced : the next block never starts higher than the last one ends. A block that would is
        // lowered (and the ones after it in turn if they would then rise above it).
        private void FixRises(TrackField pField)
        {
            List<TrackBlock> lBlocks = new List<TrackBlock>(pField.blocks);
            lBlocks.Sort((pA, pB) => pA.s0.CompareTo(pB.s0));
            int lFixed = 0;
            for (int i = 1; i < lBlocks.Count; i++)
            {
                TrackBlock lA = lBlocks[i - 1];
                TrackBlock lB = lBlocks[i];
                if (lA.look == BlockLook.Plaza || lB.look == BlockLook.Plaza || lB.s0 - lA.s1 > 400f) continue;
                SpineSample lEndA = _layout.spine.Evaluate(lA.s1);
                SpineSample lStartB = _layout.spine.Evaluate(lB.s0);
                if (lEndA.localGravity || lStartB.localGravity) continue;
                float lFrom = Mathf.Max(lA.offset - lA.halfWidth, lB.offset - lB.halfWidth);
                float lTo = Mathf.Min(lA.offset + lA.halfWidth, lB.offset + lB.halfWidth);
                if (lTo < lFrom) continue;
                float lWorst = float.MinValue;
                for (int k = 0; k <= 6; k++)
                {
                    float lX = Mathf.Lerp(lFrom, lTo, k / 6f);
                    lWorst = Mathf.Max(lWorst, StageBuilder.FloorY(lStartB, lB, lX) - StageBuilder.FloorY(lEndA, lA, lX));
                }
                if (lWorst <= 1f) continue;
                lB.drop += (lWorst + 8f) / Mathf.Max(0.3f, lStartB.up.y);
                lFixed++;
            }
            if (lFixed > 0) Debug.Log("[HS-LD] lowered " + lFixed + " block(s) that started higher than the one before");
        }

        // Walls of glass, ice or doors across the whole track, here and there on the straights : dash through them.
        private void PlaceBreakWalls()
        {
            PaneKind lKind = _theme.decor == DecorKind.Ice ? PaneKind.Ice : _theme.decor == DecorKind.Industrial || _theme.decor == DecorKind.Neon ? PaneKind.Door : PaneKind.Glass;
            for (float lS = M(5f); lS < _layout.finishS - M(3f); lS += M(Rand(7f, 11f)))
            {
                if (!PadAllowed(lS) || !PadAllowed(lS + M(0.6f)) || !PadAllowed(lS - M(0.6f))) continue;
                TrackBlock lBlock = _layout.field.BlockAt(lS);
                if (lBlock == null || lBlock.look == BlockLook.Slab || lBlock.look == BlockLook.Plaza) continue;
                PaneKind lThis = Chance(0.3f) ? (PaneKind)_random.Next(0, 3) : lKind;
                int lCount = Mathf.Clamp(Mathf.RoundToInt(lBlock.halfWidth * 2f / 46f), 4, 14);
                float lWidth = lBlock.halfWidth * 2f / lCount;
                float lHeight = Mathf.Clamp(lWidth * 0.8f, 18f, 40f);
                for (int c = 0; c < lCount; c++)
                    _panes.Add(new PaneDef { s = lS, lateral = lBlock.offset - lBlock.halfWidth + lWidth * (c + 0.5f), width = lWidth * 0.98f, height = lHeight, kind = lThis });
                Act(lS - M(0.32f), RouteAction.Dash);
            }
        }

        // Launch strips here and there on open ground, wherever there is solid track to land on.
        private void PlaceJumpStrips(List<ItemData> pItems)
        {
            for (float lS = M(4f); lS < _layout.finishS - M(3f); lS += M(Rand(6f, 10f) / Mathf.Clamp(padDensity * 1.4f, 0.4f, 1.2f)))
            {
                if (!PadAllowed(lS) || !_layout.field.Solid(lS + M(0.6f)) || !_layout.field.Solid(lS + M(1.2f))) continue;
                TrackBlock lBlock = _layout.field.BlockAt(lS);
                if (lBlock == null || lBlock.look == BlockLook.Slab) continue;
                float lLateral = lBlock.offset + Rand(-0.5f, 0.5f) * lBlock.halfWidth;
                pItems.Add(new ItemData { kind = ItemKind.Strip, s = lS, lateral = lLateral, width = lBlock.halfWidth * 0.2f, value = 60f });
            }
        }

        private bool PadAllowed(float pS)
        {
            if (!_layout.field.Solid(pS)) return false;
            if (_layout.spine.samples[_layout.spine.IndexOf(pS)].localGravity) return false;
            foreach (Vector2 lZone in _noPads)
                if (pS >= lZone.x && pS <= lZone.y) return false;
            return true;
        }

        private void PlacePads(List<ItemData> pItems)
        {
            foreach (PadDef lPad in _pads)
            {
                if (!_layout.field.Solid(lPad.s)) continue;
                pItems.Add(new ItemData { kind = ItemKind.Pad, s = lPad.s, lateral = lPad.lateral, width = lPad.width, length = lPad.length, value = lPad.boost });
            }
            // Small pads spread over the whole stage, near the line. Few on the first stages.
            float lStep = M(1.7f) / Mathf.Max(0.2f, padDensity);
            for (float lS = M(2f); lS < _layout.finishS - M(1f); lS += lStep * Rand(0.8f, 1.2f))
            {
                if (!PadAllowed(lS)) continue;
                TrackBlock lBlock = _layout.field.BlockAt(lS);
                if (lBlock == null) continue;
                float lWidth = lBlock.halfWidth * 0.42f;
                float lRange = lBlock.halfWidth - lWidth * 0.5f;
                float lLateral = lBlock.offset + Rand(-0.7f, 0.7f) * lRange;
                pItems.Add(new ItemData { kind = ItemKind.Pad, s = lS, lateral = lLateral, width = lWidth, length = M(0.16f), value = 0.5f });
            }
        }
    }
}
