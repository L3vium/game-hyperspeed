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
            _noItems = true;
            return this;
        }

        // ================================================================ modules

        public CourseBuilder Start(float pWidth, float pAltitude)
        {
            _halfWidth = pWidth * 0.5f * WIDTH_SCALE;
            _yaw = 90f;
            _pos = new Vector3(-8000f, pAltitude, 0f);
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
            _layout.hints.Add(new KeyValuePair<float, string>(_s, pText));
            return this;
        }

        public CourseBuilder Width(float pWidth)
        {
            _halfWidth = pWidth * 0.5f * WIDTH_SCALE;
            return this;
        }

        // Classic stretch : a road on a long block, low kerbs.
        public CourseBuilder Road(float pSeconds)
        {
            Open(BlockLook.Road, 3f);
            Span(M(pSeconds * pace), 0f, 0f, 0f);
            return this;
        }

        // Rolling ground. Your speed follows the slopes.
        public CourseBuilder Hills(float pSeconds, float pDegrees = 14f)
        {
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
        public CourseBuilder Turn(Side pSide, float pDegrees, float pBank = 28f)
        {
            int lSide = (int)pSide;
            if (TurnRoom(lSide) < pDegrees && TurnRoom(-lSide) > TurnRoom(lSide)) lSide = -lSide;
            float lDegrees = Mathf.Min(pDegrees, TurnRoom(lSide));
            if (lDegrees < 5f) return this;
            float lRadius = Mathf.Max(Mathf.Max(_halfWidth * 2.4f + 30f, _p.cruise * 3f), 950f);
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
            Open(BlockLook.Pipe, 16f);
            return Turn(_yaw <= 90f ? Side.Right : Side.Left, 150f, 46f);
        }

        // Linked curves, left and right : no rest for the mouse.
        public CourseBuilder Sweeps(int pCount, float pDegrees = 42f)
        {
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
            // Sized for a boosted runner, not for cruise : a loop is a big sweep, never a wall.
            float lRadius = Mathf.Max(_p.cruise * 1.5f, _halfWidth * 1.9f);
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
            float lRadius = Mathf.Max(_p.cruise * 1.6f, _halfWidth * 2.1f);
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
            pSeconds *= pace;
            float lEnd = _s + M(pSeconds);
            float lOffset = 0f;
            float lDrop = 0f;
            float lHalf = _halfWidth;
            while (_s < lEnd)
            {
                float lLength = M(Rand(0.8f, 1.9f) / pDensity);
                float lNarrow = lHalf * Rand(0.55f, 0.95f);
                TrackBlock lBlock = Open(BlockLook.Slab, 0f, lNarrow, lOffset, lDrop, Rand(-10f, 10f), Rand(60f, 140f));
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
            // Back to the line for the next module.
            float lBack = lDrop;
            Close();
            Span(M(0.2f), 0f, 0f, 0f, Vector3.down * lBack);
            Open(BlockLook.Ground, 4f);
            return this;
        }

        // Run off a colossal cliff : look down to dive. The block below lasts long enough to land anywhere on it.
        public CourseBuilder Dive(float pDrop = 320f)
        {
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

        // A hole with a rope over it. Jump onto it : it never catches you on its own. Below, a low block and a launch.
        public CourseBuilder Rope(float pSeconds = 2.2f)
        {
            float lS0 = _s;
            Open(BlockLook.Road, 3f);
            Span(M(1.1f));
            float lLateral = Rand(-_halfWidth * 0.3f, _halfWidth * 0.3f);
            Close();
            float lLip = _s;
            Act(lLip - M(0.2f), RouteAction.Jump);
            float lDepth = 70f;
            // Safety net : a sunken block, a launch strip at its end brings you back up.
            Open(BlockLook.Slab, 0f, _halfWidth * 1.1f, 0f, lDepth, 0f, 40f);
            Span(M(pSeconds));
            _strips.Add(new StripDef { s = _s - M(0.3f), lateral = 0f, halfWidth = _halfWidth, up = 92f, });
            Close();
            Span(M(0.2f));
            _ropes.Add(new RopeDef { s0 = lLip - M(0.25f), s1 = _s + M(0.1f), lateral = lLateral, height0 = 7f, height1 = 3f });
            Route(lS0, _s + M(0.2f), lLateral);
            NoPads(lS0, _s + M(0.6f));
            Open(BlockLook.Road, 3f);
            return this;
        }

        // Rope in the sky above the track : a launch strip throws you straight up, the rest is on you.
        public CourseBuilder SkyRope(float pSeconds = 2.4f)
        {
            float lS0 = _s;
            float lLateral = Rand(-_halfWidth * 0.3f, _halfWidth * 0.3f);
            Open(BlockLook.Road, 3f);
            Span(M(0.9f));
            float lStrip = _s - M(0.15f);
            _strips.Add(new StripDef { s = lStrip, lateral = lLateral, halfWidth = _halfWidth * 0.3f, up = 58f });
            Span(M(pSeconds + 0.8f));
            _ropes.Add(new RopeDef { s0 = lStrip + M(0.4f), s1 = lStrip + M(0.4f + pSeconds), lateral = lLateral, height0 = 13f, height1 = 3f });
            Route(lS0, _s, lLateral);
            NoPads(lS0, _s);
            return this;
        }

        // Chain of small speed pads along the line : take them all to really fly.
        public CourseBuilder PadRun(float pSeconds, float pEvery = 0.45f)
        {
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
            float lS0 = _s;
            Open(BlockLook.Ground, 3f);
            Span(M(0.5f + pCount * 0.34f));
            float lLateral = Rand(-_halfWidth * 0.5f, _halfWidth * 0.5f);
            Act(lS0 + M(0.25f), RouteAction.Jump);
            for (int i = 0; i < pCount; i++)
            {
                float lT = (i + 0.5f) / pCount;
                _rings.Add(new RingDef { s = lS0 + M(0.5f + i * 0.34f), lateral = lLateral, height = 10f + Mathf.Sin(lT * Mathf.PI) * 18f, radius = 24f });
            }
            Route(lS0, _s, lLateral);
            NoPads(lS0, _s);
            return this;
        }

        // Giant crates in the way. Dash through them : they burst and throw you forward. Run into them : it hurts.
        public CourseBuilder Crates(float pSeconds, int pCount, bool pWall = false)
        {
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

        // Meteorites coming at you : dash through them.
        public CourseBuilder Meteors(float pSeconds, float pEverySeconds = 2f)
        {
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
            float lS0 = _s;
            float lHalf = _halfWidth;
            _halfWidth = Mathf.Max(24f * WIDTH_SCALE, _halfWidth * 0.8f);
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
            Open(BlockLook.Road, 6f);
            Span(M(0.6f));
            _layout.finishS = _s;
            NoPads(_s - M(0.8f), _s + 99999f);
            Span(M(3.5f));
            Close();
            return this;
        }

        // ================================================================ build

        public LevelLayout Build()
        {
            if (!_started) throw new InvalidOperationException("CourseBuilder.Start() was not called");
            float lStart = Time.realtimeSinceStartup;
            Close();

            TrackField lField = new TrackField(_layout.spine);
            lField.blocks.AddRange(_blocks);
            lField.Seal();
            _layout.field = lField;

            _layout.route.Sort((pA, pB) => pA.s0.CompareTo(pB.s0));
            _layout.acts.Sort((pA, pB) => pA.s.CompareTo(pB.s));

            CourseValidator.Check(_layout, _dives);
            TrackBaker.Bake(_layout, _theme, _root);
            float lTrackDone = Time.realtimeSinceStartup;

            Transform lProps = new GameObject("Props").transform;
            lProps.SetParent(_root, false);
            MeteorDirector lDirector = new GameObject("Meteors").AddComponent<MeteorDirector>();
            lDirector.transform.SetParent(lProps, false);
            if (!_noItems)
            {
                foreach (BlockDef lBlock in _props) PlaceProp(lBlock, lProps);
                foreach (StripDef lStrip in _strips) JumpStrip.Create(lProps, _layout, lStrip.s, lStrip.lateral, lStrip.halfWidth, lStrip.up, _theme.roadEdge);
                foreach (RopeDef lRope in _ropes) PlaceRope(lRope, lProps);
                foreach (RingDef lRing in _rings) Ring.Create(lProps, _layout, lRing.s, lRing.lateral, lRing.height, lRing.radius, _theme.accent);
                foreach (CrateDef lCrate in _crates) Crate.Create(lProps, _layout, lCrate.s, lCrate.lateral, lCrate.size, lCrate.moving, _theme);
                foreach (LaserDef lLaser in _lasers) LaserGate.Create(lProps, _layout, lLaser.s, lLaser.height, lLaser.beams, lLaser.spin, _theme);
                foreach (HammerDef lHammer in _hammers) Hammer.Create(lProps, _layout, lHammer.s, lHammer.phase, _theme);
                PlaceBreakWalls();
                _layout.acts.Sort((pA, pB) => pA.s.CompareTo(pB.s));
                foreach (PaneDef lPane in _panes) Pane.Create(lProps, _layout, lPane.s, lPane.lateral, lPane.width, lPane.height, lPane.kind, _theme);
                PlacePads(lProps);
                foreach (MeteorDef lMeteor in _meteors) lDirector.Add(lMeteor.s, lMeteor.fromSky, lMeteor.radius, lMeteor.lateral);
            }
            lDirector.Init(_layout, _theme);
            foreach (ArchDef lArch in _arches) GateKit.Arch(lProps, _layout, lArch.s, _theme);
            foreach (float lGate in _gravityGates) GateKit.GravityHoop(lProps, _layout, lGate, _theme);

            StartGate.Create(lProps, _layout, _theme);
            GateKit.Portal(lProps, _layout, _layout.finishS, lField.TotalHalfWidth(_layout.finishS), 60f, _theme.accent, Color.white, "FINISH");
            CourseScenery.Build(_layout, _theme, _root, _seed * 7 + 3);
            ChaosDirector.Create(_root, _layout, _theme, !_noItems, _seed * 13 + 5);

            Debug.Log("[HS-GEN] length=" + _layout.finishS.ToString("F0") + "m track=" + ((lTrackDone - lStart) * 1000f).ToString("F0") + "ms total=" + ((Time.realtimeSinceStartup - lStart) * 1000f).ToString("F0")
                + "ms blocks=" + _blocks.Count + " chunks=" + TrackBaker.Chunks.Count + " pads=" + _pads.Count + " rings=" + _rings.Count + " crates=" + _crates.Count + " lasers=" + _lasers.Count + " dives=" + _dives);
            return _layout;
        }

        private void PlaceProp(BlockDef pDef, Transform pParent)
        {
            SpineSample lSample = _layout.spine.Evaluate(pDef.s);
            Vector3 lBase = _layout.field.Point(pDef.s, pDef.lateral);
            Vector3 lUp = _layout.field.Normal(pDef.s);
            Color lColor = pDef.palette == 0 ? _theme.decorA : pDef.palette == 1 ? _theme.decorB : _theme.decorC;
            Quaternion lRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(lSample.forward, lUp), lUp) * Quaternion.Euler(pDef.lean * 0.5f, Rand(-14f, 14f), pDef.lean);
            Monolith.Create(pParent, lBase, lRotation, new Vector3(pDef.width, pDef.height, pDef.length), Rand(0.12f, 0.3f), lColor, _theme, _seed + (int)pDef.s);
        }

        private void PlaceRope(RopeDef pDef, Transform pParent)
        {
            GameObject lObject = new GameObject("Rope");
            lObject.transform.SetParent(pParent, false);
            GrindRail lRail = lObject.AddComponent<GrindRail>();
            lRail.catchRadius = 6f;
            float lStep = Mathf.Max(8f, 10f * K);
            for (float lS = pDef.s0; lS < pDef.s1; lS += lStep)
            {
                float lT = (lS - pDef.s0) / (pDef.s1 - pDef.s0);
                SpineSample lSample = _layout.spine.Evaluate(lS);
                float lHeight = Mathf.Lerp(pDef.height0, pDef.height1, lT);
                lRail.AddPoint(lSample.position + lSample.Right * pDef.lateral + lSample.up * lHeight, lSample.up, lS);
            }
            lRail.Finish(_theme.accent, 1f);
            _layout.rails.Add(lRail);
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

        private bool PadAllowed(float pS)
        {
            if (!_layout.field.Solid(pS)) return false;
            if (_layout.spine.samples[_layout.spine.IndexOf(pS)].localGravity) return false;
            foreach (Vector2 lZone in _noPads)
                if (pS >= lZone.x && pS <= lZone.y) return false;
            return true;
        }

        private void PlacePads(Transform pParent)
        {
            foreach (PadDef lPad in _pads)
            {
                if (!_layout.field.Solid(lPad.s)) continue;
                BoostPad.CreateDraped(pParent, _layout, lPad.s, lPad.lateral, lPad.width, lPad.length, _theme.accent, lPad.boost);
            }
            // Small pads spread over the whole stage, near the line.
            float lStep = M(1.7f);
            for (float lS = M(2f); lS < _layout.finishS - M(1f); lS += lStep * Rand(0.8f, 1.2f))
            {
                if (!PadAllowed(lS)) continue;
                TrackBlock lBlock = _layout.field.BlockAt(lS);
                if (lBlock == null) continue;
                float lWidth = lBlock.halfWidth * 0.42f;
                float lRange = lBlock.halfWidth - lWidth * 0.5f;
                float lLateral = lBlock.offset + Rand(-0.7f, 0.7f) * lRange;
                BoostPad.CreateDraped(pParent, _layout, lS, lLateral, lWidth, M(0.16f), _theme.accent, 0.5f);
            }
        }
    }
}
