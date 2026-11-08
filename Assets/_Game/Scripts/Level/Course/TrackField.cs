using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public static class Layers
    {
        public const int DECK = 8;
        // Everything around the track you can still land on : scenery, the safety net under the world.
        public const int SCENERY = 9;
        public const int PROP = 10;
        public const int DEBRIS = 11;

        public const int GROUND = (1 << 0) | (1 << DECK) | (1 << SCENERY) | (1 << PROP);
    }

    public enum BlockLook : byte { Ground, Road, Slab, Pipe, Plaza }

    // One solid piece of track : a long stretch or a big block, shifted, lowered and tilted against the line.
    public class TrackBlock
    {
        public float s0;
        public float s1;
        public float offset;
        public float drop;
        // Degrees around the line, positive leans right.
        public float tilt;
        public float halfWidth;
        // Rideable sides : height (0 = none) and lean from the vertical, in degrees. Never a vertical face.
        public float wallLeft;
        public float wallRight;
        public float wallLean = 28f;
        // Mountain sides : rocky crest, strata colours, a slide that brings you back to the floor.
        public bool mountain;
        public float thickness = 26f;
        public BlockLook look;
        public int seed;

        public float Cos => Mathf.Cos(tilt * Mathf.Deg2Rad);
        public float Sin => Mathf.Sin(tilt * Mathf.Deg2Rad);
        public float WallRun(float pHeight) => pHeight * Mathf.Tan(wallLean * Mathf.Deg2Rad);
    }

    // The ground of a level : blocks hung on the 3D line. Same formulas for the mesh, the pads, the rivals and respawn.
    public class TrackField
    {
        public readonly Spine spine;
        public readonly List<TrackBlock> blocks = new List<TrackBlock>();
        private int[] _blockAt;

        public TrackField(Spine pSpine)
        {
            spine = pSpine;
        }

        public void Seal()
        {
            _blockAt = new int[spine.Count];
            for (int i = 0; i < _blockAt.Length; i++) _blockAt[i] = -1;
            for (int b = 0; b < blocks.Count; b++)
            {
                int lA = spine.IndexOf(blocks[b].s0);
                int lZ = spine.IndexOf(blocks[b].s1);
                for (int i = lA; i <= lZ; i++) _blockAt[i] = b;
            }
        }

        public TrackBlock BlockAt(float pS)
        {
            int lIndex = _blockAt[spine.IndexOf(pS)];
            return lIndex >= 0 ? blocks[lIndex] : null;
        }

        // Nearest block in either direction : gaps still have a width, an aim and a height.
        private TrackBlock NearBlock(float pS)
        {
            int lI = spine.IndexOf(pS);
            for (int lD = 0; lD < _blockAt.Length; lD++)
            {
                if (lI + lD < _blockAt.Length && _blockAt[lI + lD] >= 0) return blocks[_blockAt[lI + lD]];
                if (lI - lD >= 0 && _blockAt[lI - lD] >= 0) return blocks[_blockAt[lI - lD]];
            }
            return blocks[0];
        }

        public bool Solid(float pS) => BlockAt(pS) != null;

        public float HalfWidth(float pS) => NearBlock(pS).halfWidth;

        public float CenterLateral(float pS) => NearBlock(pS).offset;

        // Side room including the walls.
        public float TotalHalfWidth(float pS)
        {
            TrackBlock lBlock = NearBlock(pS);
            return lBlock.halfWidth + Mathf.Max(lBlock.WallRun(lBlock.wallLeft), lBlock.WallRun(lBlock.wallRight)) + 6f;
        }

        public bool OverGround(float pS, float pLateral)
        {
            TrackBlock lBlock = BlockAt(pS);
            return lBlock != null && Mathf.Abs(pLateral - lBlock.offset) <= lBlock.halfWidth + 2f;
        }

        // World point at (s, lateral) on the floor of the block there.
        public Vector3 Point(float pS, float pLateral)
        {
            TrackBlock lBlock = NearBlock(pS);
            SpineSample lSample = spine.Evaluate(pS);
            float lLocal = pLateral - lBlock.offset;
            return lSample.position + lSample.Right * (lBlock.offset + lLocal * lBlock.Cos) + lSample.up * (-lBlock.drop - lLocal * lBlock.Sin);
        }

        public Vector3 Normal(float pS)
        {
            TrackBlock lBlock = NearBlock(pS);
            SpineSample lSample = spine.Evaluate(pS);
            return (lSample.up * lBlock.Cos + lSample.Right * lBlock.Sin).normalized;
        }
    }
}
