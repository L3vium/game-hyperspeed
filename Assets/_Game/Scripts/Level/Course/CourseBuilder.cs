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
    // holds at 90 and at 420 m/s. The terrain is one enormous ribbon of hills, plateaus and valleys, blocks stand on it.
    public class CourseBuilder
    {
        private const float PLAZA = 320f;

        private struct BlockDef { public float s, lateral, width, length, height, taper; public int palette; public bool wall; }
        private struct StripDef { public float s, lateral, halfWidth, target, targetLateral, targetHeight; }
        private struct RopeDef { public float s0, s1, lateral, catchRadius; public bool sky; public float skyHeight; }
        private struct MeteorDef { public float s; public bool fromSky; public float radius; public float lateral; }

        private readonly Transform _root;
        private readonly Theme _theme;
        private readonly SpeedProfile _p;
        private readonly LevelLayout _layout = new LevelLayout();
        private readonly System.Random _random;
        private readonly int _seed;

        private readonly List<float> _hw = new List<float>();
        private readonly List<float> _off = new List<float>();
        private readonly List<float> _hill = new List<float>();
        private readonly List<float> _road = new List<float>();
        private readonly List<float> _creases = new List<float>();
        private readonly List<TerrainStamp> _stamps = new List<TerrainStamp>();
        private readonly List<BlockDef> _blocks = new List<BlockDef>();
        private readonly List<StripDef> _strips = new List<StripDef>();
        private readonly List<RopeDef> _ropes = new List<RopeDef>();
        private readonly List<MeteorDef> _meteors = new List<MeteorDef>();
        private readonly List<Vector2> _noPads = new List<Vector2>();

        private Vector3 _pos;
        private float _yaw;
        private float _y;
        private float _s;
        private float _halfWidth;
        private bool _started;
        private bool _noScatter;
        private int _dives;
        private float _maxY = float.MinValue;

        // Stretches the open modules (not the jumps) : later stages last as long as the plan says despite their speed.
        public float pace = 1f;

        public float K => _p.k;
        public float Scale => Mathf.Pow(_p.k, 0.7f);
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

        // ================================================================ spine
        private void Push(float pYawDelta, float pY, float pHill, float pRoad)
        {
            _yaw += pYawDelta;
            SpineSample lSample = new SpineSample { yaw = _yaw, s = _s };
            if (_layout.spine.samples.Count > 0) _pos += lSample.Forward * Spine.STEP;
            _y = pY;
            lSample.position = new Vector3(_pos.x, _y, _pos.z);
            _layout.spine.samples.Add(lSample);
            _hw.Add(_halfWidth);
            _off.Add(0f);
            _hill.Add(pHill);
            _road.Add(pRoad);
            _maxY = Mathf.Max(_maxY, _y);
            _s += Spine.STEP;
        }

        private void Span(float pLength, float pDeltaY, float pYawTotal, float pHill, float pRoad, bool pSmoothY = true)
        {
            int lSteps = Mathf.Max(1, Mathf.RoundToInt(pLength / Spine.STEP));
            float lFrom = _y;
            float lPreviousYaw = 0f;
            for (int i = 1; i <= lSteps; i++)
            {
                float lT = i / (float)lSteps;
                float lYaw = pYawTotal * HSMath.Smooth(lT);
                Push(lYaw - lPreviousYaw, lFrom + pDeltaY * (pSmoothY ? HSMath.Smooth(lT) : lT), pHill, pRoad);
                lPreviousYaw = lYaw;
            }
        }

        // Vertical drop between two samples : the mesh gets a clean cliff there.
        private void Cliff(float pDrop)
        {
            _creases.Add(_s - Spine.STEP);
            Push(0f, _y - pDrop, 0f, 0f);
        }

        private void NoPads(float pFrom, float pTo) => _noPads.Add(new Vector2(pFrom, pTo));

        // Title backdrop : no gameplay items on the attract run.
        public CourseBuilder NoPadsAnywhere()
        {
            NoPads(float.MinValue, float.MaxValue);
            return this;
        }

        private float HillAmp(float pAmount) => 30f * K * pAmount;

        private void Route(float pS0, float pS1, float pLateral)
        {
            if (pS1 > pS0) _layout.route.Add(new RouteNode { s0 = pS0, s1 = pS1, lateral = pLateral });
        }

        // ================================================================ modules
        public CourseBuilder Start(float pWidth, float pAltitude)
        {
            _halfWidth = pWidth * 0.5f;
            _y = pAltitude;
            _yaw = 90f;
            _pos = new Vector3(-8000f * K, 0f, 0f);
            _s = -PLAZA;
            _started = true;
            int lSteps = Mathf.RoundToInt(PLAZA / Spine.STEP);
            for (int i = 0; i <= lSteps; i++) Push(0f, _y, 0f, 0f);
            _layout.startLineS = 0f;
            _layout.spawnS = -130f;
            // Backstop : a giant block closing the plaza.
            _blocks.Add(new BlockDef { s = -PLAZA + 25f, lateral = 0f, width = pWidth + 260f, length = 40f, height = 220f, taper = 0f, palette = 0, wall = true });
            NoPads(-PLAZA, M(1.5f));
            return this;
        }

        public CourseBuilder Hint(string pText)
        {
            _layout.hints.Add(new KeyValuePair<float, string>(_s, pText));
            return this;
        }

        public CourseBuilder Width(float pWidth)
        {
            _halfWidth = pWidth * 0.5f;
            return this;
        }

        // Rolling ground with mountains to run over. Trend = height change per second of play.
        public CourseBuilder Hills(float pSeconds, float pAmount = 1f, float pTrend = -12f)
        {
            pSeconds *= pace;
            float lS0 = _s;
            float lLength = M(pSeconds);
            Span(lLength, pTrend * pSeconds * K, 0f, HillAmp(pAmount), 0f, false);
            int lMounds = Mathf.RoundToInt(pSeconds * 0.8f * pAmount);
            for (int i = 0; i < lMounds; i++)
            {
                float lS = lS0 + lLength * (i + Rand(0.2f, 0.8f)) / Mathf.Max(1, lMounds);
                _stamps.Add(new TerrainStamp
                {
                    s = lS,
                    lateral = Rand(-_halfWidth * 0.9f, _halfWidth * 0.9f),
                    radiusS = Rand(170f, 320f) * K,
                    radiusL = Rand(_halfWidth * 0.35f, _halfWidth * 0.75f),
                    height = Rand(35f, 80f) * K * pAmount * (_random.NextDouble() < 0.2 ? -0.7f : 1f)
                });
            }
            return this;
        }

        // Flat, immense straight with a road painted on it.
        public CourseBuilder Road(float pSeconds, float pTrend = -6f)
        {
            pSeconds *= pace;
            Span(M(pSeconds), pTrend * pSeconds * K, 0f, HillAmp(0.05f), 1f, false);
            return this;
        }

        public CourseBuilder Turn(Side pSide, float pDegrees, float pHills = 0.35f)
        {
            // Heading stays within [0, 180] : x only ever grows, so the route can never cross itself. Asked the other way,
            // the turn goes to the side that has room.
            int lSide = (int)pSide;
            float lRoom = lSide > 0 ? 180f - _yaw : _yaw;
            float lOther = lSide > 0 ? _yaw : 180f - _yaw;
            if (lRoom < pDegrees && lOther > lRoom)
            {
                lSide = -lSide;
                lRoom = lOther;
            }
            float lDegrees = Mathf.Min(pDegrees, lRoom);
            if (lDegrees < 5f) return this;
            // Wide enough that the inside of the ribbon never folds and both legs of a hairpin stay apart.
            float lRadius = Mathf.Max(_halfWidth + 150f * Scale, _p.cruise * _p.cruise / 200f);
            // The eased yaw covers less ground sideways than a true arc : stretch it so the legs really end up apart.
            float lLength = lRadius * lDegrees * Mathf.Deg2Rad * 1.2f;
            Span(lLength, -lLength * 0.01f, lSide * lDegrees, HillAmp(pHills), 0f, false);
            return this;
        }

        // The stage turns around : half a circle, wide enough that both legs stay side by side.
        public CourseBuilder Hairpin(Side pSide)
        {
            return Turn(_yaw <= 90f ? Side.Right : Side.Left, 180f, 0.15f);
        }

        // Long slope that feeds your speed.
        public CourseBuilder Descent(float pSeconds, float pDrop)
        {
            Span(M(pSeconds), -pDrop * K, 0f, HillAmp(0.15f), 0f);
            return this;
        }

        public CourseBuilder Climb(float pSeconds, float pRise)
        {
            float lLength = M(pSeconds);
            Span(lLength, Mathf.Min(pRise * K, lLength * 0.42f), 0f, HillAmp(0.2f), 0f);
            return this;
        }

        // Run off a colossal cliff and dive : the valley below lasts long enough to land anywhere.
        public CourseBuilder Dive(float pDrop = 420f)
        {
            float lDrop = pDrop * Scale;
            Span(M(2f), 8f * K, 0f, 0f, 0f);
            float lLip = _s;
            Cliff(lDrop);
            NoPads(lLip - M(1f), lLip + M(1f));
            _layout.acts.Add(new RouteAct { s = lLip + M(0.5f), action = RouteAction.Dive });
            _dives++;
            float lFall = Mathf.Sqrt(2f * lDrop / (Ballistics.G_FALL * _p.gravityScale));
            Span(M(Mathf.Max(5f, lFall * 2f)), -6f * K, 0f, HillAmp(0.08f), 0f, false);
            return this;
        }

        // Big platforms separated by valleys : a jump strip runs along the whole lip, missing it just costs time.
        public CourseBuilder Plateaus(int pCount, float pPlatform = 4.5f)
        {
            float lDepth = 90f * Scale;
            for (int i = 0; i < pCount; i++)
            {
                Span(M(pPlatform), 0f, 0f, 0f, i % 2 == 0 ? 1f : 0f);
                float lLip = _s;
                float lPlatformY = _y;
                Cliff(lDepth);
                Span(M(1.5f), 0f, 0f, 0f, 0f);
                Span(M(1.1f), lPlatformY - _y, 0f, 0f, 0f);
                NoPads(lLip - M(1.4f), _s);
                _strips.Add(new StripDef { s = lLip - M(0.25f), lateral = 0f, halfWidth = _halfWidth + 30f, target = _s + M(0.5f), targetLateral = float.NaN, targetHeight = lPlatformY });
            }
            return this;
        }

        // A hole with a rope over it : the ground funnels you onto the rope, missing it means a valley and a ramp back up.
        public CourseBuilder Rope(float pSeconds = 3f, int pCount = 1)
        {
            float lFunnel = M(3f);
            float lS0 = _s;
            float lLateralBase = pCount == 1 ? Rand(-_halfWidth * 0.2f, _halfWidth * 0.2f) : 0f;
            Span(lFunnel, 0f, 0f, 0f, 0f);
            float lLip = _s;
            float lTop = _y;
            float lDepth = 150f * Scale;
            if (pCount == 1)
                _stamps.Add(new TerrainStamp { s = lLip - lFunnel * 0.5f, lateral = lLateralBase, radiusS = lFunnel * 0.55f, radiusL = 150f * Mathf.Sqrt(K), height = 26f * Mathf.Sqrt(K), funnel = true });
            Cliff(lDepth);
            float lSpan = M(pSeconds);
            Span(lSpan, 0f, 0f, 0f, 0f);
            float lFar = _s;
            Span(M(1f), lTop - _y, 0f, 0f, 0f);
            NoPads(lS0, _s + M(0.5f));
            float lSpacing = pCount > 1 ? _halfWidth * 1.3f / (pCount - 1) : 0f;
            for (int i = 0; i < pCount; i++)
            {
                float lLateral = lLateralBase + (pCount > 1 ? -_halfWidth * 0.65f + i * lSpacing : 0f);
                _ropes.Add(new RopeDef { s0 = lLip - M(1.1f), s1 = _s + M(0.5f), lateral = lLateral, catchRadius = 22f * Mathf.Sqrt(K) });
            }
            Route(lS0, lFar + M(1.5f), pCount == 1 ? lLateralBase : -_halfWidth * 0.65f);
            Span(M(1.5f), 0f, 0f, 0f, 0f);
            return this;
        }

        // Rope hanging in the sky over rolling ground : a launch strip sends you up to it.
        public CourseBuilder SkyRope(float pSeconds = 3.5f)
        {
            float lS0 = _s;
            float lLateral = Rand(-_halfWidth * 0.4f, _halfWidth * 0.4f);
            float lHeight = 70f * Scale;
            Span(M(1.2f), 0f, 0f, 0f, 1f);
            float lStripS = _s - M(0.1f);
            float lRopeS = _s + M(0.8f);
            Span(M(0.8f + pSeconds + 0.5f), -10f * K, 0f, HillAmp(0.4f), 0f, false);
            float lTop = _layout.spine.samples.Count > 0 ? _y : 0f;
            _strips.Add(new StripDef { s = lStripS, lateral = lLateral, halfWidth = 90f * Mathf.Sqrt(K), target = lRopeS, targetLateral = lLateral, targetHeight = float.NaN });
            _ropes.Add(new RopeDef { s0 = lRopeS - M(0.05f), s1 = lRopeS + M(pSeconds), lateral = lLateral, catchRadius = 30f * Mathf.Sqrt(K), sky = true, skyHeight = lHeight });
            Route(lS0, lRopeS + M(pSeconds), lLateral);
            NoPads(lS0, lRopeS);
            return this;
        }

        // Field of giant blocks to weave through, climb or run along.
        public CourseBuilder Blocks(float pSeconds, float pDensity = 1f)
        {
            pSeconds *= pace;
            float lS0 = _s;
            float lLength = M(pSeconds);
            Span(lLength, -6f * pSeconds * K, 0f, HillAmp(0.2f), 0f, false);
            float lRow = M(0.75f) / pDensity;
            float lGate = Rand(-1f, 1f) * _halfWidth * 0.5f;
            for (float lS = lS0 + lRow * 0.5f; lS < lS0 + lLength - lRow * 0.4f; lS += lRow)
            {
                lGate = Mathf.Clamp(lGate + Rand(-1f, 1f) * _halfWidth * 0.35f, -_halfWidth * 0.7f, _halfWidth * 0.7f);
                Route(lS - lRow * 0.5f, lS + lRow * 0.5f, lGate);
                float lGap = 60f * Scale;
                for (int lSide = -1; lSide <= 1; lSide += 2)
                {
                    float lFrom = lGate + lSide * lGap;
                    float lRoom = _halfWidth - lSide * lFrom;
                    if (lRoom < 40f) continue;
                    float lWidth = Mathf.Min(Rand(0.3f, 0.8f) * lRoom, Rand(60f, 170f) * Scale);
                    if (_random.NextDouble() < 0.25) continue;
                    _blocks.Add(new BlockDef
                    {
                        s = lS,
                        lateral = lFrom + lSide * lWidth * 0.5f,
                        width = lWidth,
                        length = Mathf.Min(Rand(50f, 170f) * Scale, lRow * 0.75f),
                        height = Rand(35f, 170f) * Scale,
                        taper = Rand(0f, 0.18f),
                        palette = _random.Next(0, 3)
                    });
                }
            }
            NoPads(lS0, lS0 + lLength);
            return this;
        }

        // Corridor between two endless walls : wall run, wall jump.
        public CourseBuilder Canyon(float pSeconds)
        {
            float lS0 = _s;
            float lHalf = _halfWidth;
            _halfWidth = Mathf.Min(_halfWidth, 80f * Scale);
            Span(M(0.6f), 0f, 0f, 0f, 0f);
            float lS1 = _s;
            Span(M(pSeconds), -4f * pSeconds * K, 0f, 0f, 1f, false);
            float lSegment = 180f * Scale;
            for (float lS = lS1; lS < _s - lSegment * 0.5f; lS += lSegment)
            {
                for (int lSide = -1; lSide <= 1; lSide += 2)
                {
                    _blocks.Add(new BlockDef
                    {
                        s = lS + lSegment * 0.5f,
                        lateral = lSide * (_halfWidth + 12f * Scale),
                        width = 26f * Scale,
                        length = lSegment * 0.96f,
                        height = Rand(120f, 210f) * Scale,
                        taper = 0f,
                        palette = lSide > 0 ? 1 : 2,
                        wall = true
                    });
                }
            }
            _halfWidth = lHalf;
            Span(M(0.6f), 0f, 0f, 0f, 0f);
            NoPads(lS0, lS1);
            return this;
        }

        // Meteorites coming at you : punch them apart.
        public CourseBuilder Meteors(float pSeconds, float pEverySeconds = 2f)
        {
            pSeconds *= pace;
            float lS0 = _s;
            Span(M(pSeconds), -6f * pSeconds * K, 0f, HillAmp(0.3f), 0f, false);
            for (float lT = pEverySeconds * 0.5f; lT < pSeconds; lT += pEverySeconds)
                _meteors.Add(new MeteorDef { s = lS0 + M(lT), fromSky = true, radius = Rand(16f, 34f) * Mathf.Sqrt(K) });
            return this;
        }

        // Boulders blocking the way : a punch goes through them.
        public CourseBuilder Smash(float pSeconds, int pCount)
        {
            float lS0 = _s;
            Span(M(pSeconds), -4f * pSeconds * K, 0f, 0f, 1f, false);
            for (int i = 0; i < pCount; i++)
                _meteors.Add(new MeteorDef { s = lS0 + M(pSeconds) * (i + 0.5f) / pCount, fromSky = false, radius = Rand(16f, 26f) * Mathf.Sqrt(K), lateral = Rand(-1f, 1f) * _halfWidth * 0.45f });
            return this;
        }

        public CourseBuilder Finish()
        {
            Span(M(1f), 0f, 0f, 0f, 1f);
            _layout.finishS = _s;
            NoPads(_s - M(0.8f), _s + 99999f);
            Span(M(4.5f), 0f, 0f, 0f, 1f);
            return this;
        }

        // ================================================================ build
        private static float[] Blur(List<float> pValues, int pRadius)
        {
            int lCount = pValues.Count;
            float[] lResult = new float[lCount];
            float lSum = 0f;
            int lWindow = 0;
            for (int i = 0; i < lCount; i++)
            {
                if (i + pRadius < lCount) { lSum += pValues[i + pRadius]; lWindow++; }
                if (i == 0)
                {
                    for (int j = 0; j < pRadius && j < lCount; j++) { lSum += pValues[j]; lWindow++; }
                }
                if (i - pRadius - 1 >= 0) { lSum -= pValues[i - pRadius - 1]; lWindow--; }
                lResult[i] = lSum / Mathf.Max(1, lWindow);
            }
            return lResult;
        }

        public LevelLayout Build()
        {
            if (!_started) throw new InvalidOperationException("CourseBuilder.Start() was not called");
            float lStart = Time.realtimeSinceStartup;

            TerrainField lField = new TerrainField(_layout.spine, _seed);
            int lBlur = Mathf.Max(4, Mathf.RoundToInt(M(0.7f) / Spine.STEP));
            lField.halfWidth = Blur(_hw, lBlur);
            lField.offset = Blur(_off, lBlur);
            lField.hill = Blur(_hill, lBlur);
            lField.road = _road.ToArray();
            lField.hillWave = 300f * K;
            lField.bermWidth = 70f * Scale;
            lField.bermHeight = 32f * Scale;
            lField.skirtWidth = 40f * Scale;
            lField.creases.AddRange(_creases);
            foreach (TerrainStamp lStamp in _stamps) lField.AddStamp(lStamp);
            lField.Seal();
            _layout.field = lField;

            _layout.route.Sort((pA, pB) => pA.s0.CompareTo(pB.s0));
            _layout.acts.Sort((pA, pB) => pA.s.CompareTo(pB.s));

            CourseValidator.Check(_layout, _dives);
            TerrainBaker.Bake(_layout, _theme, _root);
            float lTerrainDone = Time.realtimeSinceStartup;

            Transform lProps = new GameObject("Props").transform;
            lProps.SetParent(_root, false);
            Scatter();
            foreach (BlockDef lBlock in _blocks) PlaceBlock(lBlock, lProps);
            foreach (StripDef lStrip in _strips) PlaceStrip(lStrip, lProps);
            foreach (RopeDef lRope in _ropes) PlaceRope(lRope, lProps);
            PlacePads(lProps);
            MeteorDirector lDirector = new GameObject("Meteors").AddComponent<MeteorDirector>();
            lDirector.transform.SetParent(lProps, false);
            foreach (MeteorDef lMeteor in _meteors) lDirector.Add(lMeteor.s, lMeteor.fromSky, lMeteor.radius, lMeteor.lateral);
            lDirector.Init(_layout, _theme);

            StartGate.Create(lProps, _layout, _theme);
            FinishGate(lProps);
            CourseScenery.Build(_layout, _theme, _root, _seed * 7 + 3);

            Debug.Log("[HS-GEN] length=" + _layout.finishS.ToString("F0") + "m terrain=" + ((lTerrainDone - lStart) * 1000f).ToString("F0") + "ms total=" + ((Time.realtimeSinceStartup - lStart) * 1000f).ToString("F0")
                + "ms blocks=" + _blocks.Count + " strips=" + _strips.Count + " ropes=" + _ropes.Count + " meteors=" + _meteors.Count + " dives=" + _dives);
            return _layout;
        }

        // Blocks and pillars on both sides all along the stage, off the clear line : never in front of a lip or a rope.
        private void Scatter()
        {
            if (_noScatter) return;
            float lStep = M(0.42f);
            TerrainField lField = _layout.field;
            for (float lS = M(1f); lS < _layout.finishS - M(1f); lS += lStep)
            {
                if (!PadAllowed(lS)) continue;
                int lSide = _random.Next(0, 2) * 2 - 1;
                float lHalf = lField.HalfWidth(lS);
                float lRoute = _layout.RouteLateral(lS);
                float lClear = 55f * Scale;
                float lFrom = lRoute + lSide * lClear;
                float lRoom = lHalf + lField.bermWidth * 0.3f - lSide * lFrom;
                if (lRoom < 30f) continue;
                bool lPillar = _random.NextDouble() < 0.35;
                float lWidth = lPillar ? Rand(10f, 22f) * Scale : Rand(26f, 90f) * Scale;
                float lLength = lPillar ? lWidth : Mathf.Min(Rand(26f, 90f) * Scale, lStep * 0.8f);
                float lLateral = lFrom + lSide * (lWidth * 0.5f + Rand(0f, Mathf.Max(0f, lRoom - lWidth)));
                _blocks.Add(new BlockDef
                {
                    s = lS,
                    lateral = lLateral,
                    width = lWidth,
                    length = lLength,
                    height = lPillar ? Rand(70f, 200f) * Scale : Rand(14f, 90f) * Scale,
                    taper = Rand(0f, 0.25f),
                    palette = _random.Next(0, 3)
                });
            }
        }

        private void PlaceBlock(BlockDef pDef, Transform pParent)
        {
            TerrainField lField = _layout.field;
            SpineSample lSample = _layout.spine.Evaluate(pDef.s);
            Vector3 lBase = _layout.spine.Point(pDef.s, pDef.lateral, lField.Height(pDef.s, pDef.lateral));
            Color lColor = pDef.palette == 0 ? _theme.decorA : pDef.palette == 1 ? _theme.decorB : _theme.decorC;
            Quaternion lRotation = Quaternion.Euler(0f, lSample.yaw + (pDef.wall ? 0f : Rand(-14f, 14f)), 0f);
            Monolith.Create(pParent, lBase, lRotation, new Vector3(pDef.width, pDef.height, pDef.length), pDef.taper, lColor, _theme, _seed + _blocks.Count + (int)pDef.s);
        }

        private void PlaceStrip(StripDef pDef, Transform pParent)
        {
            JumpStrip.Create(pParent, _layout, pDef.s, pDef.lateral, pDef.halfWidth, M(0.35f), pDef.target, pDef.targetLateral, pDef.targetHeight, _theme.roadEdge);
        }

        private void PlaceRope(RopeDef pDef, Transform pParent)
        {
            GameObject lObject = new GameObject("Rope");
            lObject.transform.SetParent(pParent, false);
            GrindRail lRail = lObject.AddComponent<GrindRail>();
            lRail.catchRadius = pDef.catchRadius;
            TerrainField lField = _layout.field;
            float lStep = Mathf.Max(8f, 12f * K);
            float lGroundEnd = pDef.sky ? pDef.s0 : pDef.s0 + M(1.1f);
            float lLipY = pDef.sky ? 0f : lField.Height(lGroundEnd - Spine.STEP, pDef.lateral) + 1.4f;
            float lEndY = pDef.sky ? 0f : lField.Height(pDef.s1, pDef.lateral) + 0.6f;
            float lSkyY = pDef.sky ? lField.Height(pDef.s0, pDef.lateral) + pDef.skyHeight : 0f;
            for (float lS = pDef.s0; lS < pDef.s1; lS += lStep)
            {
                float lY;
                if (pDef.sky)
                {
                    float lT = (lS - pDef.s0) / (pDef.s1 - pDef.s0);
                    lY = Mathf.Lerp(lSkyY, lField.Height(pDef.s1, pDef.lateral) + 0.6f, lT * lT);
                    lY = Mathf.Max(lY, lField.Height(lS, pDef.lateral) + 6f * (1f - lT));
                }
                else if (lS < lGroundEnd) lY = lField.Height(lS, pDef.lateral) + 1.4f;
                else
                {
                    float lT = (lS - lGroundEnd) / (pDef.s1 - lGroundEnd);
                    lY = Mathf.Lerp(lLipY, lEndY, lT) - Mathf.Sin(lT * Mathf.PI) * 9f * Mathf.Sqrt(K);
                }
                lRail.AddPoint(_layout.spine.Point(lS, pDef.lateral, lY), Vector3.up, lS);
            }
            lRail.Finish(_theme.accent, 1.1f * Mathf.Sqrt(K));
            _layout.rails.Add(lRail);
        }

        private bool PadAllowed(float pS)
        {
            foreach (Vector2 lZone in _noPads)
                if (pS >= lZone.x && pS <= lZone.y) return false;
            return true;
        }

        // Evenly spread over the whole stage, never bunched up at the end.
        private void PlacePads(Transform pParent)
        {
            float lStep = M(3.6f);
            int lIndex = 0;
            float lWidth = 62f * Mathf.Sqrt(K);
            float lLength = M(0.55f);
            for (float lS = M(2f); lS < _layout.finishS - M(1f); lS += lStep, lIndex++)
            {
                float lShift = lS;
                while (!PadAllowed(lShift) && lShift < lS + lStep * 0.8f) lShift += M(0.5f);
                if (!PadAllowed(lShift)) continue;
                float lHalf = _layout.field.HalfWidth(lShift) - lWidth * 0.6f;
                if (lHalf < 0f) lHalf = 0f;
                float lRoute = _layout.RouteLateral(lShift);
                if (lIndex % 3 == 2)
                {
                    BoostPad.CreateDraped(pParent, _layout, lShift, -lHalf * 0.55f, lWidth, lLength, _theme.accent);
                    BoostPad.CreateDraped(pParent, _layout, lShift, lHalf * 0.55f, lWidth, lLength, _theme.accent);
                    BoostPad.CreateDraped(pParent, _layout, lShift, lRoute, lWidth, lLength, _theme.accent);
                }
                else BoostPad.CreateDraped(pParent, _layout, lShift, Mathf.Clamp(lRoute + Rand(-0.25f, 0.25f) * lHalf, -lHalf, lHalf), lWidth, lLength, _theme.accent);
            }
        }

        private void FinishGate(Transform pParent)
        {
            float lS = _layout.finishS;
            float lHalf = _layout.field.HalfWidth(lS);
            GateKit.Portal(pParent, _layout, lS, lHalf, 150f * Scale, _theme.accent, Color.white, "FINISH");
        }
    }
}
