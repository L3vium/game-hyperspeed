using System;
using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Everything placed on a stage, as editable data. Positions are (s, lateral) on the racing line, so moving or bending
    // the road carries every object along with it.
    public enum ItemKind
    {
        Pad, Strip, Ring, Crate, Laser, Hammer, Pane, Rope, Mover, Bridge, Meteor, Prop, Arch, GravityGate,
        // 2026-11-12, sky lanes : a ramp out of the ground lane that throws you up, pads and launch strips on the decks.
        Kicker, DeckPad, DeckStrip,
        // 2026-11-12, side loops : an electric rope that winds into a vertical loop beside the line (size = radius,
        // length = advance per radian, width = lead on the ground at each end, value = sideways drift over the loop).
        LoopRope
    }

    // One piece of the racing line : it advances by `steps` samples of Spine.STEP metres while yaw, pitch and roll ease by
    // their totals. `shift` moves the line without turning it, in the frame of the heading at the start of the piece
    // (x right, y up, z forward).
    [Serializable]
    public class SegmentData
    {
        public int steps = 100;
        public float yaw;
        public float pitch;
        public float roll;
        public Vector3 shift;
        [Range(0f, 1f)] public float linear;
        public bool localGravity;
        public string label = "";

        public float Length
        {
            get => steps * Spine.STEP;
            set => steps = Mathf.Max(1, Mathf.RoundToInt(value / Spine.STEP));
        }

        public SegmentData Clone() => (SegmentData)MemberwiseClone();
    }

    // A solid block of track between s0 and s1 (see TrackBlock).
    [Serializable]
    public class BlockData
    {
        public float s0;
        public float s1;
        public BlockLook look = BlockLook.Ground;
        public float halfWidth = 200f;
        public float offset;
        public float drop;
        public float tilt;
        public float thickness = 30f;
        public float wallLeft;
        public float wallRight;
        public float wallLean = 28f;
        public bool mountain;
        public int seed;

        public TrackBlock ToBlock() => new TrackBlock
        {
            s0 = s0, s1 = s1, look = look, halfWidth = halfWidth, offset = offset, drop = drop, tilt = tilt, thickness = thickness,
            wallLeft = wallLeft, wallRight = wallRight, wallLean = wallLean, mountain = mountain, seed = seed
        };

        public static BlockData From(TrackBlock pBlock) => new BlockData
        {
            s0 = pBlock.s0, s1 = pBlock.s1, look = pBlock.look, halfWidth = pBlock.halfWidth, offset = pBlock.offset, drop = pBlock.drop,
            tilt = pBlock.tilt, thickness = pBlock.thickness, wallLeft = pBlock.wallLeft, wallRight = pBlock.wallRight,
            wallLean = pBlock.wallLean, mountain = pBlock.mountain, seed = pBlock.seed
        };

        public BlockData Clone() => (BlockData)MemberwiseClone();
    }

    // Any object on the track. Which fields matter depends on the kind (see ItemInfo).
    [Serializable]
    public class ItemData
    {
        public ItemKind kind;
        public float s;
        // End of the span for ropes and bridges.
        public float s1;
        public float lateral;
        public float height;
        // End height of a rope.
        public float height1;
        public float width;
        public float length;
        public float size;
        // Boost of a pad, launch of a strip, spin of a laser, phase of a hammer, speed of a convoy, lean of a prop.
        public float value;
        // Beams of a laser, pane kind, palette of a prop.
        public int variant;
        // Moving crate, meteor falling from the sky, lit arch.
        public bool flag;
        // Prop only : yaw jitter and roughness.
        public float jitter;
        public float rough = 0.2f;

        public ItemData Clone() => (ItemData)MemberwiseClone();
    }

    [Serializable]
    public class RouteData
    {
        public float s0;
        public float s1;
        public float lateral;
    }

    [Serializable]
    public class ActData
    {
        public float s;
        public RouteAction action;
    }

    [Serializable]
    public class HintData
    {
        public float s;
        public string text;
    }

    [Serializable]
    public class StageData
    {
        public const float PLAZA = 300f;

        public float altitude = 2400f;
        public int seed = 1;
        public int dives;
        public float startLineS;
        public float spawnS = -130f;
        public float finishS = 20000f;
        public float climaxS = -1f;
        // Light highways (2026-11-12) : stretches built for light speed, (s0, s1).
        public List<Vector2> highways = new List<Vector2>();
        public List<SegmentData> segments = new List<SegmentData>();
        public List<BlockData> blocks = new List<BlockData>();
        public List<ItemData> items = new List<ItemData>();
        public List<RouteData> route = new List<RouteData>();
        public List<ActData> acts = new List<ActData>();
        public List<HintData> hints = new List<HintData>();

        // Where the first segment starts : the plaza ends at s = 0.
        public float SegmentStart(int pIndex)
        {
            float lS = 0f;
            for (int i = 0; i < pIndex && i < segments.Count; i++) lS += segments[i].steps * Spine.STEP;
            return lS;
        }

        public float EndS => SegmentStart(segments.Count);

        public int SegmentAt(float pS)
        {
            float lS = 0f;
            for (int i = 0; i < segments.Count; i++)
            {
                lS += segments[i].steps * Spine.STEP;
                if (pS <= lS) return i;
            }
            return segments.Count - 1;
        }

        // A piece of line [pFrom, pOldTo] now ends at pNewTo : everything inside stretches, everything after slides.
        public void Reflow(float pFrom, float pOldTo, float pNewTo)
        {
            float lOld = pOldTo - pFrom;
            float lDelta = pNewTo - pOldTo;
            if (Mathf.Abs(lDelta) < 0.0001f) return;
            Func<float, float> lMap = pS =>
            {
                if (pS <= pFrom) return pS;
                if (pS >= pOldTo) return pS + lDelta;
                return lOld <= 0.0001f ? pFrom : pFrom + (pS - pFrom) * (pNewTo - pFrom) / lOld;
            };
            foreach (BlockData lBlock in blocks) { lBlock.s0 = lMap(lBlock.s0); lBlock.s1 = lMap(lBlock.s1); }
            foreach (ItemData lItem in items) { lItem.s = lMap(lItem.s); lItem.s1 = lMap(lItem.s1); }
            foreach (RouteData lNode in route) { lNode.s0 = lMap(lNode.s0); lNode.s1 = lMap(lNode.s1); }
            foreach (ActData lAct in acts) lAct.s = lMap(lAct.s);
            foreach (HintData lHint in hints) lHint.s = lMap(lHint.s);
            finishS = lMap(finishS);
            if (climaxS > 0f) climaxS = lMap(climaxS);
        }

        public StageData Clone()
        {
            StageData lCopy = (StageData)MemberwiseClone();
            lCopy.segments = segments.ConvertAll(pX => pX.Clone());
            lCopy.blocks = blocks.ConvertAll(pX => pX.Clone());
            lCopy.items = items.ConvertAll(pX => pX.Clone());
            lCopy.route = route.ConvertAll(pX => new RouteData { s0 = pX.s0, s1 = pX.s1, lateral = pX.lateral });
            lCopy.acts = acts.ConvertAll(pX => new ActData { s = pX.s, action = pX.action });
            lCopy.hints = hints.ConvertAll(pX => new HintData { s = pX.s, text = pX.text });
            return lCopy;
        }
    }
}
