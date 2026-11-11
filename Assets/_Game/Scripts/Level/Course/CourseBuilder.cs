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
        private const float PROP_SCALE = 3f;

        private struct StripDef { public float s, lateral, halfWidth, up; }
        private struct RopeDef { public float s0, s1, lateral, height0, height1; }
        private struct MeteorDef { public float s; public bool fromSky; public float radius; public float lateral; }
        private struct PadDef { public float s, lateral, width, length, boost; }
        private struct RingDef { public float s, lateral, height, radius; }
        private struct CrateDef { public float s, lateral, size; public bool moving; }
        private struct LaserDef { public float s, height, spin; public int beams; }
        private struct HammerDef { public float s, phase; }
        private struct BlockDef { public float s, lateral, width, length, height, lean; public int palette; }
        private struct ArchDef { public float s; public bool lit; }
        private struct PaneDef { public float s, lateral, width, height; public PaneKind kind; }
        private struct MoverDef { public float s, lateral, width, length, height, speed; }
        private struct BridgeDef { public float s0, s1, lateral, height, halfWidth; }

        private readonly Transform _root;
        private readonly Theme _theme;
        private readonly SpeedProfile _p;
        private readonly LevelLayout _layout = new LevelLayout();
        private readonly System.Random _random;
        private readonly int _seed;

        private readonly List<TrackBlock> _blocks = new List<TrackBlock>();
        private readonly List<StripDef> _strips = new List<StripDef>();
        private readonly List<RopeDef> _ropes = new List<RopeDef>();
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
                    lLeft = Rand(MOUNTAIN_MIN, MOUNTAIN_MAX);
                    lRight = Rand(MOUNTAIN_MIN, MOUNTAIN_MAX);
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
                thickness = pThickness > 0f ? pThickness : Rand(22f, 44f),
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
            Open(BlockLook.Ground, 7f);
            int lHumps = Mathf.Max(1, Mathf.RoundToInt(pSeconds / 3.4f));
            float lLength = M(pSeconds) / lHumps;
            for (int i = 0; i < lHumps; i++)
            {
                float lAngle = pDegrees * Rand(0.7f, 1.2f);
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
            float lRadius = Mathf.Max(Mathf.Max(_halfWidth * 2.4f + 30f, _p.cruise * 3f), 950f) * pRadiusScale;
            float lLength = lRadius * lDegrees * Mathf.Deg2Rad;
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
            return Turn(_yaw <= 90f ? Side.Right : Side.Left, 150f, 46f);
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
            Turn(pSide, pDegrees, 74f);
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
            // Sized for a boosted runner, not for cruise : a loop is a big sweep, never a wall.
            float lRadius = Mathf.Max(_p.cruise * 2f, _halfWidth * 2.4f);
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
            float lRadius = Mathf.Max(_p.cruise * 2.2f, _halfWidth * 2.6f);
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
            pSeconds *= 1.4f;
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
            Span(M(2.2f), 0f, 0f, lSide * 180f);
            Span(M(pSeconds * pace));
            Span(M(2.2f), 0f, 0f, lSide * 180f);
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
            _halfWidth = Mathf.Min(_halfWidth, (26f + 4f * K) * 4f);
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
                float lTilt = Rand(-10f, 10f);
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
                float lNext = lDrop + Rand(5f, 26f);
                if (lGap)
                {
                    float lGapLength = M(Rand(0.1f, 0.22f));
                    Act(_s - M(0.12f), RouteAction.Jump);
                    Span(lGapLength);
                    NoPads(lS0 + lLength * 0.7f, _s + M(0.3f));
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
        private const float ROPE_HIGH = 36f;
        private const float ROPE_LOW = 28f;
        private const float ROPE_LAUNCH = 96f;

        // Ramp : you climb it, top a crest and come down the far side with the speed. Up, then down : the rhythm of the
        // track. Missing nothing, falling nowhere.
        public CourseBuilder Ramp(float pSeconds = 3.5f, float pDegrees = 14f)
        {
            _module = "Ramp";
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
                _rings.Add(new RingDef { s = lS0 + M(0.5f + i * 0.34f), lateral = lLateral, height = 30f + Mathf.Sin(lT * Mathf.PI) * 34f, radius = 60f });
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
            _halfWidth = Mathf.Min(_halfWidth, (24f + 4f * K) * 4f);
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
            Open(BlockLook.Ground, 6f);
            float lEnd = _s + M(pSeconds);
            while (_s < lEnd)
            {
                float lLength = M(Rand(0.9f, 1.7f));
                float lYaw = Rand(-12f, 12f);
                if (TurnRoom(lYaw > 0f ? 1 : -1) < Mathf.Abs(lYaw) + 5f) lYaw = -lYaw;
                float lPitch = Rand(-9f, 9f);
                float lRoll = Rand(-11f, 11f);
                Span(lLength * 0.5f, lYaw * 0.5f, lPitch, lRoll);
                Span(lLength * 0.5f, lYaw * 0.5f, -lPitch, -lRoll);
                // Floating islands left and right, above head height : a jump (or a strip) takes you up there.
                if (Chance(0.4f))
                {
                    int lIslandSide = Chance(0.5f) ? 1 : -1;
                    float lIslandS = _s - lLength * Rand(0.6f, 0.9f);
                    float lIslandLateral = lIslandSide * _halfWidth * Rand(0.4f, 0.68f);
                    float lIslandHalf = _halfWidth * Rand(0.07f, 0.12f);
                    _bridges.Add(new BridgeDef { s0 = lIslandS, s1 = lIslandS + M(Rand(0.35f, 0.7f)), lateral = lIslandLateral, height = Rand(16f, 26f), halfWidth = lIslandHalf });
                    if (Chance(0.5f)) _strips.Add(new StripDef { s = lIslandS - M(0.3f), lateral = lIslandLateral, halfWidth = lIslandHalf * 1.3f, up = 66f });
                }
                // Boulders and spurs at the edges : they rush past and show the speed.
                if (Chance(0.6f))
                {
                    int lSide = Chance(0.5f) ? 1 : -1;
                    float lSize = Rand(8f, 20f) * PROP_SCALE;
                    _props.Add(new BlockDef { s = _s - lLength * Rand(0.2f, 0.8f), lateral = lSide * _halfWidth * Rand(0.72f, 0.92f), width = lSize, length = lSize * Rand(0.8f, 1.6f), height = lSize * Rand(0.6f, 1.4f), lean = Rand(-14f, 14f), palette = _random.Next(0, 3) });
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
                float lTilt = Rand(10f, 18f) * (Chance(0.5f) ? 1f : -1f);
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
                float lNext = lDrop + Rand(8f, 28f);
                if (Chance(0.35f))
                {
                    Act(_s - M(0.12f), RouteAction.Jump);
                    Span(M(Rand(0.1f, 0.2f)));
                    NoPads(lBlockS + lLength * 0.7f, _s + M(0.3f));
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
            float lHeight = 20f;
            _strips.Add(new StripDef { s = lS0 + M(0.35f), lateral = lLateral, halfWidth = _halfWidth * 0.14f, up = 72f });
            BridgeDef lBridge = new BridgeDef { s0 = lS0 + M(0.8f), s1 = _s - M(0.2f), lateral = lLateral, height = lHeight, halfWidth = _halfWidth * 0.16f };
            _bridges.Add(lBridge);
            for (float lS = lBridge.s0 + M(0.4f); lS < lBridge.s1 - M(0.2f); lS += M(0.45f))
                _rings.Add(new RingDef { s = lS, lateral = lLateral, height = lHeight + 20f, radius = 22f });
            NoPads(lS0, _s);
            return this;
        }

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
            for (float lT = 0.2f; lT < pSeconds; lT += 0.3f)
                _pads.Add(new PadDef { s = lS0 + M(lT), lateral = Mathf.Sin(lT * 2.1f) * _halfWidth * 0.5f, width = _halfWidth * 0.8f, length = M(0.13f), boost = 0.25f });
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
            lData.segments.AddRange(_segments);
            foreach (TrackBlock lBlock in _blocks) lData.blocks.Add(BlockData.From(lBlock));
            List<ItemData> lItems = lData.items;

            if (!_noItems)
            {
                foreach (BlockDef lProp in _props)
                    lItems.Add(new ItemData { kind = ItemKind.Prop, s = lProp.s, lateral = lProp.lateral, width = lProp.width, length = lProp.length, height = lProp.height, value = lProp.lean, variant = lProp.palette, jitter = Rand(-14f, 14f), rough = Rand(0.12f, 0.3f) });
                foreach (StripDef lStrip in _strips) lItems.Add(new ItemData { kind = ItemKind.Strip, s = lStrip.s, lateral = lStrip.lateral, width = lStrip.halfWidth, value = lStrip.up });
                foreach (RopeDef lRope in _ropes) lItems.Add(new ItemData { kind = ItemKind.Rope, s = lRope.s0, s1 = lRope.s1, lateral = lRope.lateral, height = lRope.height0, height1 = lRope.height1 });
                foreach (RingDef lRing in _rings) lItems.Add(new ItemData { kind = ItemKind.Ring, s = lRing.s, lateral = lRing.lateral, height = lRing.height, size = lRing.radius });
                foreach (CrateDef lCrate in _crates) lItems.Add(new ItemData { kind = ItemKind.Crate, s = lCrate.s, lateral = lCrate.lateral, size = lCrate.size, flag = lCrate.moving });
                foreach (LaserDef lLaser in _lasers) lItems.Add(new ItemData { kind = ItemKind.Laser, s = lLaser.s, height = lLaser.height, value = lLaser.spin, variant = lLaser.beams });
                foreach (HammerDef lHammer in _hammers) lItems.Add(new ItemData { kind = ItemKind.Hammer, s = lHammer.s, value = lHammer.phase });
                PlaceBreakWalls();
                foreach (MoverDef lMover in _movers) lItems.Add(new ItemData { kind = ItemKind.Mover, s = lMover.s, lateral = lMover.lateral, width = lMover.width, length = lMover.length, height = lMover.height, value = lMover.speed });
                foreach (BridgeDef lBridge in _bridges) lItems.Add(new ItemData { kind = ItemKind.Bridge, s = lBridge.s0, s1 = lBridge.s1, lateral = lBridge.lateral, height = lBridge.height, width = lBridge.halfWidth });
                PlaceJumpStrips(lItems);
                foreach (PaneDef lPane in _panes) lItems.Add(new ItemData { kind = ItemKind.Pane, s = lPane.s, lateral = lPane.lateral, width = lPane.width, height = lPane.height, variant = (int)lPane.kind });
                PlacePads(lItems);
                foreach (MeteorDef lMeteor in _meteors) lItems.Add(new ItemData { kind = ItemKind.Meteor, s = lMeteor.s, lateral = lMeteor.lateral, size = lMeteor.radius, flag = lMeteor.fromSky });
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
