using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum RouteAction : byte { None, Jump, Dive, Dash }

    // Autopilot / rival guidance : the lateral to aim for on a span (clear of hazards, centre of a block, onto ropes).
    public struct RouteNode
    {
        public float s0;
        public float s1;
        public float lateral;
    }

    public struct RouteAct
    {
        public float s;
        public RouteAction action;
    }

    // Pure data of a generated level. Every gameplay query reads this.
    public class LevelLayout
    {
        public readonly Spine spine = new Spine();
        public TrackField field;
        public SpeedProfile profile;
        public readonly List<GrindRail> rails = new List<GrindRail>();
        public readonly List<RouteNode> route = new List<RouteNode>();
        public readonly List<RouteAct> acts = new List<RouteAct>();
        public readonly List<float> checkpoints = new List<float>();
        public readonly List<KeyValuePair<float, string>> hints = new List<KeyValuePair<float, string>>();

        public float finishS;
        // Where the last straight begins : the neck and neck climax.
        public float climaxS = -1f;
        public float startLineS;
        public float spawnS;

        public float Length => spine.EndS;

        public Vector3 GroundPoint(float pS, float pLateral) => field.Point(pS, pLateral);

        public Vector3 Flow(float pS) => spine.Forward(pS);

        public Vector3 FlowRight(float pS) => spine.Right(pS);

        // Where gravity pulls at s : world down, except on the sections where the track carries you upside down.
        public Vector3 GravityUp(float pS)
        {
            SpineSample lSample = spine.samples[spine.IndexOf(pS)];
            return lSample.localGravity ? lSample.up : Vector3.up;
        }

        // Nothing to catch you : far from the line, in any direction.
        public bool IsLost(Vector3 pPosition, float pS)
        {
            // The safety net catches you far out : only truly lost is lost.
            float lReach = 1200f + field.TotalHalfWidth(pS) * 2f;
            return (pPosition - spine.Evaluate(pS).position).sqrMagnitude > lReach * lReach;
        }

        // Solid, right way up, well inside the block, no gap right ahead : a place to come back to.
        public bool IsSafe(float pS, float pLateral)
        {
            TrackBlock lBlock = field.BlockAt(pS);
            if (lBlock == null || lBlock.look == BlockLook.Slab) return false;
            SpineSample lSample = spine.Evaluate(pS);
            if (lSample.localGravity || lSample.up.y < 0.7f) return false;
            float lHalf = field.HalfWidth(pS);
            if (Mathf.Abs(pLateral - field.CenterLateral(pS)) > lHalf * 0.75f) return false;
            float lLook = 1.4f * (profile != null ? profile.cruise : 120f);
            for (float lD = lLook * 0.25f; lD <= lLook; lD += lLook * 0.25f)
                if (!field.Solid(pS + lD)) return false;
            return true;
        }

        // ---------------- lowest solid point along the line (world floor, safety net)
        private const float LOW_BUCKET = 50f;
        private float[] _low;
        private float[] _sectionLow;

        // Lowest point of the track (floor edges and the underside of each block) between two s : nothing between
        // them may ever sit under the world floor that follows the runner.
        public float LowestBetween(float pS0, float pS1)
        {
            EnsureLowProfile();
            int lA = LowIndex(pS0);
            int lB = LowIndex(pS1);
            float lMin = float.MaxValue;
            for (int i = lA; i <= lB; i++) if (_low[i] < lMin) lMin = _low[i];
            return lMin == float.MaxValue ? spine.Evaluate(pS0).position.y : lMin;
        }

        // Inside a loop, a twist or an inverted run : the lowest point of that whole section (plus a margin either side),
        // so falling from its top is never mistaken for falling out of the stage. Elsewhere : MaxValue.
        public float LowestOfSection(float pS)
        {
            EnsureLowProfile();
            return _sectionLow[LowIndex(pS)];
        }

        private int LowIndex(float pS) => Mathf.Clamp(Mathf.FloorToInt((pS - spine.StartS) / LOW_BUCKET), 0, _low.Length - 1);

        private void EnsureLowProfile()
        {
            if (_low != null || field == null) return;
            int lCount = Mathf.Max(1, Mathf.CeilToInt((spine.EndS - spine.StartS) / LOW_BUCKET) + 1);
            _low = new float[lCount];
            _sectionLow = new float[lCount];
            bool[] lLocal = new bool[lCount];
            for (int i = 0; i < lCount; i++) { _low[i] = float.MaxValue; _sectionLow[i] = float.MaxValue; }
            for (int i = 0; i < spine.Count; i += 2)
            {
                float lS = spine.SOf(i);
                int lK = LowIndex(lS);
                if (spine.samples[i].localGravity) lLocal[lK] = true;
                TrackBlock lBlock = field.BlockAt(lS);
                if (lBlock == null) continue;
                SpineSample lSample = spine.samples[i];
                float lHalf = lBlock.halfWidth + Mathf.Max(lBlock.WallRun(lBlock.wallLeft), lBlock.WallRun(lBlock.wallRight));
                Vector3 lCenter = field.Point(lS, lBlock.offset);
                Vector3 lLeft = field.Point(lS, lBlock.offset - lHalf);
                Vector3 lRight = field.Point(lS, lBlock.offset + lHalf);
                // The underside hangs about 1.15 thickness under the floor, along the block's own up.
                float lUnder = Mathf.Abs(lSample.up.y) * lBlock.thickness * 1.2f + 4f;
                float lY = Mathf.Min(lCenter.y, Mathf.Min(lLeft.y, lRight.y)) - lUnder;
                // Upside down : the block's "underside" points up, its floor is the low part.
                if (lY < _low[lK]) _low[lK] = lY;
            }
            // Gaps keep the height of their neighbours.
            for (int i = 1; i < lCount; i++) if (_low[i] == float.MaxValue) _low[i] = _low[i - 1];
            for (int i = lCount - 2; i >= 0; i--) if (_low[i] == float.MaxValue) _low[i] = _low[i + 1];
            // Each run of local gravity, widened by 600 m either side, shares its lowest point.
            const int PAD = 12;
            int lRun = 0;
            while (lRun < lCount)
            {
                if (!lLocal[lRun]) { lRun++; continue; }
                int lEnd = lRun;
                while (lEnd + 1 < lCount && lLocal[lEnd + 1]) lEnd++;
                int lA = Mathf.Max(0, lRun - PAD);
                int lB = Mathf.Min(lCount - 1, lEnd + PAD);
                float lMin = float.MaxValue;
                for (int i = lA; i <= lB; i++) lMin = Mathf.Min(lMin, _low[i]);
                for (int i = lA; i <= lB; i++) _sectionLow[i] = Mathf.Min(_sectionLow[i], lMin);
                lRun = lEnd + 1;
            }
        }

        public float RouteLateral(float pS)
        {
            // Sorted by s, a few hundred entries at most.
            int lLow = 0;
            int lHigh = route.Count - 1;
            while (lLow <= lHigh)
            {
                int lMid = (lLow + lHigh) / 2;
                RouteNode lNode = route[lMid];
                if (pS < lNode.s0) lHigh = lMid - 1;
                else if (pS > lNode.s1) lLow = lMid + 1;
                else return lNode.lateral;
            }
            return field != null ? field.CenterLateral(pS) : 0f;
        }
    }
}
