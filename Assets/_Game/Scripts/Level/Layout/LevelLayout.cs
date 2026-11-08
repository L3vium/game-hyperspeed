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
            if (!field.Solid(pS)) return false;
            SpineSample lSample = spine.Evaluate(pS);
            if (lSample.localGravity || lSample.up.y < 0.7f) return false;
            float lHalf = field.HalfWidth(pS);
            if (Mathf.Abs(pLateral - field.CenterLateral(pS)) > lHalf * 0.75f) return false;
            float lLook = 1.4f * (profile != null ? profile.cruise : 120f);
            for (float lD = lLook * 0.25f; lD <= lLook; lD += lLook * 0.25f)
                if (!field.Solid(pS + lD)) return false;
            return true;
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
