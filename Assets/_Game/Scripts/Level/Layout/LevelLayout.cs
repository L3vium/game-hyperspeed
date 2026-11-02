using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Ramp lip whose natural arc should land in a zone (the kicker assist corrects only outside of it).
    public class KickerDef
    {
        public Deck deck;
        public float lipS;
        public float zoneS0;
        public float zoneS1;
    }

    public enum RouteAction : byte { None, Jump, Dive, DropThrough, Glide, Punch }

    // Autopilot guidance : which deck / lateral to follow on a span.
    public struct RouteNode
    {
        public float s0;
        public float s1;
        public Deck deck;
        public float lateral;
    }

    // One-shot autopilot action (jump before a lip, dive, drop through a membrane, glide over a long gap).
    public struct RouteAct
    {
        public float s;
        public RouteAction action;
        public Deck deck;
    }

    // Tall rock wall along a deck edge (canyons, wall runs). pSolid ones get a collider.
    public struct CliffSpan
    {
        public Deck deck;
        public float s0;
        public float s1;
        public int side;
        public float low;
        public float high;
        public bool solid;
        public float inset;
    }

    public struct SafePoint
    {
        public float s;
        public Deck deck;
        public float lateral;
    }

    public struct DeckHit
    {
        public Deck deck;
        public Vector3 point;
        public Vector3 normal;
        public float lateral;
        public float height;
    }

    // Pure data of a generated level : spine, decks, links, route, safe points. Every gameplay query reads this.
    public class LevelLayout
    {
        public readonly Spine spine = new Spine();
        public readonly List<Deck> decks = new List<Deck>();
        public readonly List<GrindRail> rails = new List<GrindRail>();
        public readonly List<KickerDef> kickers = new List<KickerDef>();
        public readonly List<RouteNode> route = new List<RouteNode>();
        public readonly List<RouteAct> acts = new List<RouteAct>();
        public readonly List<CliffSpan> cliffs = new List<CliffSpan>();
        public readonly List<SafePoint> safePoints = new List<SafePoint>();
        public readonly List<float> checkpoints = new List<float>();
        public readonly List<KeyValuePair<float, string>> hints = new List<KeyValuePair<float, string>>();

        public Deck lane;
        public int laneSide = -1;
        public float finishS;
        public float startLineS;
        public Vector3 startPosition;
        public float startYaw;

        private List<Deck>[] _decksAt;
        private float[] _floorY;
        private float[] _skyY;

        public float Length => spine.EndS;

        public void Finalize()
        {
            foreach (Deck lDeck in decks) lDeck.Finalize(spine);
            _decksAt = new List<Deck>[spine.Count];
            _floorY = new float[spine.Count];
            _skyY = new float[spine.Count];
            for (int i = 0; i < spine.Count; i++)
            {
                _decksAt[i] = new List<Deck>(4);
                _floorY[i] = float.MaxValue;
                _skyY[i] = spine.samples[i].position.y;
            }
            foreach (Deck lDeck in decks)
            {
                for (int i = lDeck.i0; i <= lDeck.i1; i++)
                {
                    _decksAt[i].Add(lDeck);
                    float lY = lDeck.center[i - lDeck.i0].y;
                    if (lDeck.surface != DeckSurface.Lane) _floorY[i] = Mathf.Min(_floorY[i], lY);
                }
            }
            for (int i = 0; i < spine.Count; i++)
            {
                if (_floorY[i] == float.MaxValue) _floorY[i] = spine.samples[i].position.y;
                float lSky = spine.samples[i].position.y;
                for (int j = Mathf.Max(0, i - 15); j <= Mathf.Min(spine.Count - 1, i + 15); j++)
                    foreach (Deck lDeck in _decksAt[j])
                        if (lDeck.surface != DeckSurface.Lane) lSky = Mathf.Max(lSky, lDeck.center[j - lDeck.i0].y);
                _skyY[i] = lSky + 15f;
            }
        }

        public List<Deck> DecksAt(float pS) => _decksAt[spine.IndexOf(pS)];

        public float FloorY(float pS) => _floorY[spine.IndexOf(pS)];

        public float SkyY(float pS) => _skyY[spine.IndexOf(pS)];

        // Highest surface under a point within the deck widths (+ slack), ignoring membranes if asked.
        public bool SurfaceBelow(Vector3 pPosition, float pS, float pSlack, float pMaxDrop, bool pIgnoreMembrane, out DeckHit pHit)
        {
            pHit = default(DeckHit);
            float lBest = float.MaxValue;
            List<Deck> lDecks = _decksAt[spine.IndexOf(pS)];
            for (int i = 0; i < lDecks.Count; i++)
            {
                Deck lDeck = lDecks[i];
                if (pIgnoreMembrane && lDeck.surface == DeckSurface.Membrane) continue;
                if (!lDeck.ContainsS(pS)) continue;
                float lLateral;
                float lHeight = lDeck.HeightAbove(pPosition, pS, out lLateral);
                if (Mathf.Abs(lLateral) > lDeck.Width(pS) * 0.5f + pSlack) continue;
                if (lHeight < -0.8f || lHeight > pMaxDrop) continue;
                if (lHeight < lBest)
                {
                    lBest = lHeight;
                    pHit.deck = lDeck;
                    pHit.lateral = lLateral;
                    pHit.height = lHeight;
                }
            }
            if (pHit.deck == null) return false;
            pHit.point = pHit.deck.SurfacePoint(pS, Mathf.Clamp(pHit.lateral, -pHit.deck.Width(pS) * 0.5f, pHit.deck.Width(pS) * 0.5f));
            pHit.normal = pHit.deck.SurfaceNormal(pS, pHit.lateral);
            return true;
        }

        public KickerDef KickerAt(Deck pDeck, float pS)
        {
            foreach (KickerDef lKicker in kickers)
                if (lKicker.deck == pDeck && Mathf.Abs(lKicker.lipS - pS) < 6f) return lKicker;
            return null;
        }

        public SafePoint SafePointBefore(float pS)
        {
            SafePoint lResult = safePoints.Count > 0 ? safePoints[0] : default(SafePoint);
            foreach (SafePoint lPoint in safePoints)
            {
                if (lPoint.s > pS) break;
                lResult = lPoint;
            }
            return lResult;
        }

        public RouteNode RouteAt(float pS, Deck pCurrent)
        {
            RouteNode lMain = default(RouteNode);
            bool lFound = false;
            foreach (RouteNode lNode in route)
            {
                if (pS < lNode.s0 || pS > lNode.s1) continue;
                if (lNode.deck == pCurrent && pCurrent != null) return lNode;
                if (!lFound) { lMain = lNode; lFound = true; }
            }
            return lMain;
        }

        public Deck MainDeckAt(float pS)
        {
            foreach (Deck lDeck in DecksAt(pS))
                if (lDeck.route && lDeck.ContainsS(pS)) return lDeck;
            return null;
        }
    }
}
