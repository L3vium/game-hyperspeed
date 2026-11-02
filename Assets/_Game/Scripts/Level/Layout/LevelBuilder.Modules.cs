using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Level design vocabulary. Lengths are tuned for 60-120 m/s : every platform and rail is readable 1.5 s ahead.
    public partial class LevelBuilder
    {
        // ---------------------------------------------------------------- start / finish

        // Start plaza (free roam before the countdown) behind an energy barrier at s = 0.
        public LevelBuilder Start(float pLength = 90f)
        {
            _mainWidth = PLAZA_WIDTH;
            _edges = EdgeKind.Wall;
            Advance(80f, 0f, null, true);
            _main.startCap = true;
            _main.underDepth = 20f;
            _mainWidth = MAIN_WIDTH;
            Advance(Mathf.Max(20f, pLength - 20f), 0f, null, true);
            layout.startLineS = 0f;
            layout.startPosition = layout.spine.Point(-10f, 0f, RouteHeight(-10f));
            Deck lDeck = _main;
            Spawn(() => LevelProps.StartGate(root, lDeck, 0f, theme));
            return this;
        }

        public LevelBuilder Finish()
        {
            Advance(40f, 0f, null, true);
            layout.finishS = _s;
            Deck lDeck = _main;
            float lS = _s;
            Spawn(() => LevelProps.FinishGate(root, lDeck, lS, theme));
            _edges = EdgeKind.Wall;
            Advance(RUN_OUT, 0f, null, true);
            BreakMain();
            // The spine keeps going so rivals can drive through the line.
            Advance(300f, 0f, null, false);
            return this;
        }

        // ---------------------------------------------------------------- route shapes

        public LevelBuilder Run(float pLength, float pYaw = 0f)
        {
            Advance(pLength, pYaw, null, true);
            return this;
        }

        // Banked turn, radius >= 150 m so the road ahead stays in view.
        public LevelBuilder Bend(float pAngle, float pRadius = 260f)
        {
            float lLength = Mathf.Abs(pAngle) * Mathf.Deg2Rad * Mathf.Max(150f, pRadius);
            float lBank = Mathf.Sign(pAngle) * Mathf.Min(15f, 12f * 260f / pRadius);
            Advance(lLength, pAngle, null, true, pT => lBank * Mathf.Sin(pT * Mathf.PI));
            return this;
        }

        public LevelBuilder Rise(float pLength, float pHeight)
        {
            Advance(pLength, 0f, pT => pHeight * Smoother(pT), true);
            return this;
        }

        public LevelBuilder Fall(float pLength, float pHeight) => Rise(pLength, -pHeight);

        // Crest that throws you at cruise speed (sin² profile, launch speed ~60 m/s for 6 m over 70 m).
        public LevelBuilder Bump(float pLength, float pHeight)
        {
            Advance(pLength, 0f, pT => pHeight * Mathf.Pow(Mathf.Sin(pT * Mathf.PI), 2f), true);
            return this;
        }

        // ---------------------------------------------------------------- gaps and ramps

        public LevelBuilder Gap(float pLength, float pDrop = 0f)
        {
            Deck lFrom = _main;
            Act(_s - 4f, RouteAction.Jump, lFrom);
            BreakMain();
            Advance(pLength, 0f, pT => -pDrop * pT, false);
            Advance(30f, 0f, null, true);
            _main.startCap = true;
            return this;
        }

        // Ramp lip with a landing zone : the natural arc keeps the slope momentum, the assist only fixes misses.
        public LevelBuilder Kicker(float pAngle = 16f, float pGap = 50f, float pDrop = 0f, float pRamp = 30f, float pLand = 90f)
        {
            float lTan = Mathf.Tan(pAngle * Mathf.Deg2Rad);
            float lS0 = _s;
            Advance(pRamp, 0f, null, true);
            EditMain(lS0, _s, (pK, pS) =>
            {
                float lT = Mathf.Clamp01((pS - lS0) / pRamp);
                _main.h[pK] += pRamp * lTan * lT * lT * 0.5f;
            });
            layout.kickers.Add(new KickerDef { deck = _main, lipS = _s, zoneS0 = _s + pGap, zoneS1 = _s + pGap + pLand });
            Deck lRamp = _main;
            float lLip = _s;
            Spawn(() => LevelProps.RampChevrons(root, lRamp, lS0, lLip, theme));
            BreakMain();
            Advance(pGap, 0f, pT => -pDrop * pT, false);
            Advance(pLand, 0f, null, true);
            _main.startCap = true;
            return this;
        }

        public LevelBuilder BoostStrip(float pLength, int pCount)
        {
            float lS0 = _s;
            Advance(pLength, 0f, null, true);
            Deck lDeck = _main;
            for (int i = 0; i < pCount; i++)
            {
                float lS = lS0 + (i + 0.5f) * pLength / pCount;
                float lLateral = pCount > 1 ? ((i % 3) - 1) * 6f : 0f;
                Spawn(() => LevelProps.Boost(root, lDeck, lS, lLateral, theme));
            }
            return this;
        }

        public LevelBuilder Smash(float pLength, int pCount)
        {
            float lS0 = _s;
            Advance(pLength, 0f, null, true);
            Deck lDeck = _main;
            for (int i = 0; i < pCount; i++)
            {
                float lS = lS0 + 15f + Random.value * (pLength - 25f);
                float lLateral = Random.Range(-1f, 1f) * (MAIN_WIDTH * 0.5f - 3f);
                float lSize = Random.Range(0.9f, 1.3f);
                Spawn(() => LevelProps.Obstacle(root, lDeck, lS, lLateral, lSize, theme));
            }
            return this;
        }

        // ---------------------------------------------------------------- platforms

        // The route breaks into parallel chains of floating platforms (hop left / right), over a catch deck.
        public LevelBuilder Islands(float pLength = 320f, int pCount = 3, float pSpread = 24f, float pStep = 5f, bool pCrumble = false)
        {
            float lH0 = _pos.y;
            float lApron = pSpread * (pCount - 1) + 16f;
            _mainWidth = lApron;
            _widthRate = 0.8f;
            Advance(50f, 0f, null, true);
            Deck lApronDeck = _main;
            float lSplit = _s;
            Act(lSplit - 3f, RouteAction.Jump, lApronDeck);
            BreakMain();
            _mainWidth = MAIN_WIDTH;
            Advance(pLength, 0f, null, false);
            float lMerge = _s;
            const float LANE_WIDTH = 13f;
            for (int j = 0; j < pCount; j++)
            {
                float lX = (j - (pCount - 1) * 0.5f) * pSpread;
                bool lCenter = Mathf.Abs(lX) < 1f;
                EdgeKind lLeft = j == 0 ? EdgeKind.Wall : EdgeKind.Lip;
                EdgeKind lRight = j == pCount - 1 ? EdgeKind.Wall : EdgeKind.Lip;
                float lS = lSplit + (lCenter ? 9f : 12f);
                int lSegment = 0;
                while (lS < lMerge - 40f)
                {
                    float lLength = Mathf.Min(Random.Range(45f, 80f), lMerge - 14f - lS);
                    if (lLength < 25f) break;
                    bool lLast = lS + lLength >= lMerge - 40f;
                    float lHeight = lLast || (lCenter && lSegment == 0) ? lH0 : lH0 + Random.Range(-pStep, pStep) * (lCenter ? 0.5f : 1f);
                    float lXc = lX;
                    Deck lDeck = AddDeck(lS, lS + lLength, DeckStyle.Rock, pCrumble ? DeckSurface.Crumble : DeckSurface.Solid,
                        pS => lXc, pS => lHeight, pS => LANE_WIDTH, lLeft, lRight);
                    lDeck.underDepth = 9f;
                    if (lCenter)
                    {
                        Route(lS - 6f, lS + lLength, lDeck);
                        Act(lS + lLength - 3f, RouteAction.Jump, lDeck);
                    }
                    else if (Random.value < 0.65f)
                    {
                        float lPadS = lS + lLength * 0.5f;
                        Spawn(() => LevelProps.Boost(root, lDeck, lPadS, 0f, theme));
                    }
                    lS += lLength + Random.Range(lCenter ? 8f : 10f, lCenter ? 13f : 17f);
                    lSegment++;
                }
            }
            // Falling between platforms costs time, never a life.
            float lCatchH = lH0 - 18f;
            float lCatchW = lApron + 12f;
            Deck lCatch = AddDeck(lSplit + 4f, lMerge - 16f, DeckStyle.Rock, DeckSurface.Solid, pS => 0f, pS => lCatchH, pS => lCatchW, EdgeKind.Wall, EdgeKind.Wall);
            lCatch.underDepth = 16f;
            // Merge apron, wide enough to catch every chain.
            _startWidth = lApron;
            _widthRate = 0.6f;
            Advance(70f, 0f, null, true);
            _main.startCap = true;
            Deck lLanding = _main;
            float lPadAt = lMerge - 30f;
            float lLandAt = lMerge + 25f;
            Spawn(() => LevelProps.Jump(root, lCatch, lPadAt, 0f, lLanding, lLandAt, 0f, theme));
            return this;
        }

        // Jump pad up to a higher slab (boosts, faster line) that drops you back on the route.
        public LevelBuilder Upper(float pLength = 260f, float pHeight = 16f, float pLateral = 0f)
        {
            float lS0 = _s;
            Advance(pLength + 50f, 0f, null, true);
            Deck lMain = _main;
            float lPadS = lS0 + 12f;
            float lUp0 = lS0 + 70f;
            float lUp1 = lS0 + pLength;
            Deck lUpper = AddDeck(lUp0, lUp1, DeckStyle.Slab, DeckSurface.Solid, pS => pLateral, pS => RouteHeight(pS) + pHeight, pS => 16f, EdgeKind.Lip, EdgeKind.Lip);
            lUpper.underDepth = 3f;
            Route(lUp0, lUp1, lUpper, 0f);
            Spawn(() => LevelProps.Jump(root, lMain, lPadS, 0f, lUpper, lUp0 + 30f, 0f, theme));
            for (int i = 0; i < 2; i++)
            {
                float lS = lUp0 + 70f + i * 70f;
                if (lS < lUp1 - 20f) Spawn(() => LevelProps.Boost(root, lUpper, lS, 0f, theme));
            }
            return this;
        }

        // The route floor turns into a cracked membrane : crouch to drop to a boosted lower deck.
        public LevelBuilder Lower(float pLength = 240f, float pDepth = 14f)
        {
            Advance(20f, 0f, null, true);
            BreakMain(false);
            float lM0 = _s;
            Advance(pLength - 60f, 0f, null, true);
            Deck lMembrane = _main;
            lMembrane.surface = DeckSurface.Membrane;
            lMembrane.style = DeckStyle.Membrane;
            lMembrane.startCap = false;
            lMembrane.underDepth = 0.6f;
            float lM1 = _s;
            BreakMain(false);
            Advance(40f, 0f, null, true);
            _main.startCap = false;
            Deck lAfter = _main;
            Deck lLower = AddDeck(lM0 - 12f, lM1 + 24f, DeckStyle.Rock, DeckSurface.Solid, pS => 0f, pS => RouteHeight(pS) - pDepth, pS => MAIN_WIDTH, EdgeKind.Wall, EdgeKind.Wall);
            lLower.underDepth = 12f;
            Act(lM0 + 12f, RouteAction.DropThrough, lMembrane);
            Route(lM0 + 12f, lM1 + 24f, lLower);
            for (int i = 0; i < 3; i++)
            {
                float lS = lM0 + 30f + i * (lM1 - lM0 - 40f) / 3f;
                Spawn(() => LevelProps.Boost(root, lLower, lS, 0f, theme));
            }
            float lPadS = lM1 + 14f;
            float lLand = lM1 + 70f;
            Spawn(() => LevelProps.Jump(root, lLower, lPadS, 0f, lAfter, lLand, 0f, theme));
            Advance(60f, 0f, null, true);
            return this;
        }

        // Stairs of floating platforms : run off each lip (or dive for a landing boost).
        public LevelBuilder Cascade(int pLevels = 4, float pDrop = 18f, float pStep = 70f)
        {
            for (int i = 0; i < pLevels; i++)
            {
                Advance(pStep * 0.65f, 0f, null, true);
                Deck lFrom = _main;
                Act(_s + 3f, RouteAction.Dive, lFrom);
                BreakMain();
                Advance(pStep * 0.35f, 0f, pT => -pDrop * Smooth(pT), false);
            }
            Advance(90f, 0f, null, true);
            _main.startCap = true;
            return this;
        }

        // Stack of membranes over a deep floor : keep crouching to break through every layer.
        public LevelBuilder DiveShaft(int pLayers = 3, float pDrop = 12f)
        {
            Advance(20f, 0f, null, true);
            float lLip = _s;
            float lH0 = _pos.y;
            Act(lLip - 2f, RouteAction.Jump, _main);
            BreakMain();
            float lSpan = 20f + pLayers * 22f + 60f;
            float lBottomH = lH0 - pDrop * (pLayers + 1);
            for (int l = 0; l < pLayers; l++)
            {
                float lS0 = lLip + 12f + l * 22f;
                float lH = lH0 - pDrop * (l + 1);
                Deck lLayer = AddDeck(lS0, lS0 + 60f, DeckStyle.Membrane, DeckSurface.Membrane, pS => 0f, pS => lH, pS => MAIN_WIDTH, EdgeKind.Lip, EdgeKind.Lip);
                lLayer.underDepth = 0.6f;
                Act(lS0 + 6f, RouteAction.DropThrough, lLayer);
            }
            Advance(lSpan, 0f, pT => (lBottomH - lH0) * Smooth(Mathf.Clamp01(pT * 1.4f)), false);
            Deck lBottom = AddDeck(lLip + 8f, _s, DeckStyle.Rock, DeckSurface.Solid, pS => 0f, pS => lBottomH, pS => MAIN_WIDTH + 6f, EdgeKind.Wall, EdgeKind.Wall);
            lBottom.route = true;
            lBottom.underDepth = 18f;
            Route(lLip + 8f, _s, lBottom);
            _main = lBottom;
            _main.endCap = true;
            Advance(60f, 0f, null, true);
            return this;
        }

        // Long fall through a field of debris and rings down to a deep floor : dive to go faster.
        public LevelBuilder Plunge(float pDepth = 90f, float pLength = 200f, int pRings = 4)
        {
            Advance(10f, 0f, null, true);
            float lLip = _s;
            float lH0 = _pos.y;
            Act(lLip + 6f, RouteAction.Dive, _main);
            BreakMain();
            Advance(pLength, 0f, pT => -pDepth * Smooth(pT), false);
            float lBottomH = lH0 - pDepth;
            Deck lBottom = AddDeck(lLip + 30f, _s, DeckStyle.Rock, DeckSurface.Solid, pS => 0f, pS => lBottomH, pS => MAIN_WIDTH + 10f, EdgeKind.Wall, EdgeKind.Wall);
            lBottom.route = true;
            lBottom.underDepth = 24f;
            Route(lLip + 30f, _s, lBottom);
            _main = lBottom;
            for (int i = 1; i <= pRings; i++)
            {
                float lT = i / (pRings + 1f);
                float lS = lLip + pLength * 0.6f * lT;
                float lY = lH0 - pDepth * 0.85f * lT * lT - 4f;
                Spawn(() => LevelProps.Ring(root, layout, lS, 0f, lY, theme));
            }
            float lDebrisS0 = lLip;
            float lDebrisS1 = _s;
            float lTop = lH0;
            Spawn(() => LevelProps.DebrisField(root, layout, lDebrisS0, lDebrisS1, lTop, lBottomH, theme));
            Advance(80f, 0f, null, true);
            return this;
        }

        // ---------------------------------------------------------------- rails

        // Arrow-shaped approach onto a V trough, rail over the gap, rail ends buried in the landing trough.
        public LevelBuilder RailBridge(float pLength = 150f, float pDrop = 0f, float pYaw = 0f)
        {
            EdgeKind lSaved = _edges;
            _edges = EdgeKind.Wall;
            _mainWidth = 10f;
            _widthRate = 0.5f;
            float lA0 = _s;
            Advance(50f, 0f, null, true);
            float lGap0 = _s;
            EditMain(lGap0 - 34f, lGap0, (pK, pS) => { _main.profile[pK] = DeckProfile.Trough; _main.troughT[pK] = 0f; });
            Deck lFrom = _main;
            BreakMain();
            Advance(pLength, pYaw, pT => -pDrop * Smooth(pT), false);
            float lGap1 = _s;
            _startWidth = 10f;
            _mainWidth = MAIN_WIDTH;
            _widthRate = 0.4f;
            Advance(60f, 0f, null, true);
            _main.startCap = true;
            EditMain(lGap1, lGap1 + 30f, (pK, pS) => { _main.profile[pK] = DeckProfile.Trough; _main.troughT[pK] = 0f; });
            Deck lTo = _main;
            _edges = lSaved;
            Spawn(() => LevelProps.BridgeRail(root, layout, lFrom, lTo, lGap0, lGap1, theme));
            Spawn(() => LevelProps.RampChevrons(root, lFrom, lA0 + 10f, lGap0 - 6f, theme));
            return this;
        }

        // Gutter rail along one edge of the route : steer into the half V to grind it.
        public LevelBuilder RailSide(float pLength = 200f, Side pSide = Side.Right)
        {
            float lS0 = _s;
            Advance(pLength, 0f, null, true);
            int lSign = (int)pSide;
            float lS1 = _s;
            EditMain(lS0, lS1, (pK, pS) =>
            {
                _main.profile[pK] = lSign < 0 ? DeckProfile.GutterL : DeckProfile.GutterR;
                if (lSign < 0) _main.edgeL[pK] = EdgeKind.Wall;
                else _main.edgeR[pK] = EdgeKind.Wall;
            });
            Deck lDeck = _main;
            Spawn(() => LevelProps.GutterRail(root, layout, lDeck, lS0 + 6f, lS1 - 6f, lSign, theme));
            return this;
        }

        // Two or three rails weaving over a gap : hop from one to another, a catch deck below.
        public LevelBuilder RailWeave(float pLength = 260f, int pRails = 3)
        {
            float lS0 = _s;
            Advance(40f, 0f, null, true);
            float lTan = Mathf.Tan(10f * Mathf.Deg2Rad);
            float lRampS = _s - 20f;
            EditMain(lRampS, _s, (pK, pS) => { float lT = (pS - lRampS) / 20f; _main.h[pK] += 20f * lTan * lT * lT * 0.5f; });
            float lLip = _s;
            float lLipH = _pos.y + 20f * lTan * 0.5f;
            Act(lLip - 3f, RouteAction.Jump, _main);
            BreakMain();
            Advance(pLength, 0f, null, false);
            float lEnd = _s;
            Advance(60f, 0f, null, true);
            _main.startCap = true;
            Deck lLanding = _main;
            float lCatchH = lLipH - 16f;
            Deck lCatch = AddDeck(lLip + 6f, lEnd - 6f, DeckStyle.Rock, DeckSurface.Solid, pS => 0f, pS => lCatchH, pS => 34f, EdgeKind.Wall, EdgeKind.Wall);
            float lPadS = lEnd - 20f;
            float lLand = lEnd + 30f;
            Spawn(() => LevelProps.Jump(root, lCatch, lPadS, 0f, lLanding, lLand, 0f, theme));
            for (int r = 0; r < pRails; r++)
            {
                float lPhase = r * Mathf.PI * 2f / pRails;
                float lHeight = lLipH - 0.6f;
                Spawn(() => LevelProps.WeaveRail(root, layout, lLip + 6f, lEnd + 4f, lHeight, lLanding, lPhase, theme));
            }
            return this;
        }

        // Cable from an upper platform to a lower one : run under it and the hands grab it.
        public LevelBuilder ZipLine(float pLength = 110f, float pDrop = 14f)
        {
            Advance(30f, 0f, null, true);
            Deck lFrom = _main;
            float lS0 = _s;
            BreakMain();
            Advance(pLength, 0f, pT => -pDrop * pT, false);
            float lS1 = _s;
            Advance(50f, 0f, null, true);
            _main.startCap = true;
            Deck lTo = _main;
            float lCatchH = Mathf.Min(lFrom.h[lFrom.Count - 1], _pos.y) - 16f;
            Deck lCatch = AddDeck(lS0 + 4f, lS1 - 4f, DeckStyle.Rock, DeckSurface.Solid, pS => 0f, pS => lCatchH, pS => 30f, EdgeKind.Wall, EdgeKind.Wall);
            float lPadS = lS1 - 16f;
            float lLand = lS1 + 25f;
            Spawn(() => LevelProps.Jump(root, lCatch, lPadS, 0f, lTo, lLand, 0f, theme));
            Spawn(() => LevelProps.Zip(root, layout, lFrom, lTo, lS0, lS1, theme));
            return this;
        }

        // ---------------------------------------------------------------- speed sections

        // Straight between tall rock walls : the safest place to reach hyper speed.
        public LevelBuilder Canyon(float pLength = 300f, int pBoosts = 3)
        {
            EdgeKind lSaved = _edges;
            _edges = EdgeKind.Wall;
            float lS0 = _s;
            Advance(pLength, 0f, null, true);
            Deck lDeck = _main;
            layout.cliffs.Add(new CliffSpan { deck = lDeck, s0 = lS0, s1 = _s, side = -1, low = -2f, high = 14f, solid = false });
            layout.cliffs.Add(new CliffSpan { deck = lDeck, s0 = lS0, s1 = _s, side = 1, low = -2f, high = 14f, solid = false });
            for (int i = 0; i < pBoosts; i++)
            {
                float lS = lS0 + 30f + i * (pLength - 60f) / Mathf.Max(1, pBoosts - 1);
                Spawn(() => LevelProps.Boost(root, lDeck, lS, 0f, theme));
            }
            _edges = lSaved;
            return this;
        }

        // Banked run up to pAngle on one side (wall ride), hard edges.
        public LevelBuilder WallRide(float pLength = 200f, Side pSide = Side.Right, float pAngle = 60f)
        {
            EdgeKind lSaved = _edges;
            _edges = EdgeKind.Wall;
            float lAngle = Mathf.Min(70f, pAngle);
            int lSign = (int)pSide;
            Advance(pLength, 0f, null, true, pT => -lSign * lAngle * (Smooth(Mathf.Clamp01(pT / 0.3f)) - Smooth(Mathf.Clamp01((pT - 0.7f) / 0.3f))));
            _edges = lSaved;
            return this;
        }

        // Floorless stretch between two rock walls : wall run across, a catch deck below.
        public LevelBuilder WallRun(float pLength = 110f)
        {
            Advance(30f, 0f, null, true);
            float lTan = Mathf.Tan(10f * Mathf.Deg2Rad);
            float lRampS = _s - 20f;
            EditMain(lRampS, _s, (pK, pS) => { float lT = (pS - lRampS) / 20f; _main.h[pK] += 20f * lTan * lT * lT * 0.5f; });
            Deck lFrom = _main;
            float lLip = _s;
            float lH = _pos.y;
            Act(lLip - 3f, RouteAction.Jump, lFrom);
            BreakMain();
            Advance(pLength, 0f, null, false);
            float lEnd = _s;
            Advance(50f, 0f, null, true);
            _main.startCap = true;
            Deck lTo = _main;
            Deck lWalls = AddDeck(lLip, lEnd, DeckStyle.Rock, DeckSurface.Solid, pS => 0f, pS => lH - 14f, pS => 30f, EdgeKind.Wall, EdgeKind.Wall);
            layout.cliffs.Add(new CliffSpan { deck = lWalls, s0 = lLip - 6f, s1 = lEnd + 6f, side = -1, low = 4f, high = 26f, solid = true, inset = 10f });
            layout.cliffs.Add(new CliffSpan { deck = lWalls, s0 = lLip - 6f, s1 = lEnd + 6f, side = 1, low = 4f, high = 26f, solid = true, inset = 10f });
            float lPadS = lEnd - 16f;
            float lLand = lEnd + 25f;
            Spawn(() => LevelProps.Jump(root, lWalls, lPadS, 0f, lTo, lLand, 0f, theme));
            return this;
        }
    }
}
