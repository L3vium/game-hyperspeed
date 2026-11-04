using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum RouteAction : byte { None, Jump, Dive, Punch }

    // Autopilot / rival guidance : the lateral to aim for on a span (clear of blocks, onto ropes).
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
        public TerrainField field;
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

        public float Ground(float pS, float pLateral) => field.Height(pS, pLateral);

        public Vector3 GroundPoint(float pS, float pLateral) => field.Point(pS, pLateral);

        // Direction the world flows at s : the route, seen a short way ahead so the view anticipates turns.
        public Vector3 Flow(float pS) => spine.Forward(pS);

        public Vector3 FlowRight(float pS) => spine.Right(pS);

        // Below this you are not coming back : lowest ground around, far below.
        public float KillY(float pS, float pSpeed)
        {
            float lLowest = float.MaxValue;
            float lEnd = pS + Mathf.Max(150f, pSpeed * 2f);
            float lStep = (lEnd - (pS - 100f)) / 8f;
            for (float lS = pS - 100f; lS <= lEnd; lS += lStep) lLowest = Mathf.Min(lLowest, spine.Height(lS));
            return lLowest - 150f * Mathf.Max(1f, profile != null ? profile.k * 0.6f : 1f);
        }

        // Flat enough, well inside the corridor, no lip right ahead : a place to come back to.
        public bool IsSafe(float pS, float pLateral)
        {
            if (!field.Inside(pS, pLateral)) return false;
            if (Mathf.Abs(pLateral - field.Offset(pS)) > field.HalfWidth(pS) * 0.85f) return false;
            float lLook = 1.4f * (profile != null ? profile.cruise : 90f);
            float lHere = Ground(pS, pLateral);
            for (float lD = lLook * 0.25f; lD <= lLook; lD += lLook * 0.25f)
                if (Ground(pS + lD, pLateral) < lHere - 12f) return false;
            return true;
        }

        public float SkyY(float pS)
        {
            float lTop = spine.Height(pS);
            float lStep = field.HalfWidth(pS) * 0.5f;
            for (int i = -2; i <= 2; i++) lTop = Mathf.Max(lTop, Ground(pS, i * lStep));
            return lTop + 15f;
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
            return 0f;
        }
    }
}
