using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Where the sky lanes of the current stage are (kickers, decks, ropes and rings), filled while the stage is built.
    // The autopilot's sky mode follows them : the proof that every sky route can be taken from end to end.
    public static class SkyRoute
    {
        public struct Piece
        {
            public float s0, s1, lateral0, lateral1, height0, height1;
            public bool kicker;

            public float LateralAt(float pS) => Mathf.Lerp(lateral0, lateral1, Mathf.InverseLerp(s0, s1, pS));
            public float HeightAt(float pS) => Mathf.Lerp(height0, height1, Mathf.InverseLerp(s0, s1, pS));
        }

        public static readonly List<Piece> Kickers = new List<Piece>();
        public static readonly List<Piece> Pieces = new List<Piece>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            Kickers.Clear();
            Pieces.Clear();
        }

        public static void Add(ItemData pItem)
        {
            switch (pItem.kind)
            {
                case ItemKind.Kicker:
                    Kickers.Add(new Piece { s0 = pItem.s, s1 = pItem.s1, lateral0 = pItem.lateral, lateral1 = pItem.lateral, kicker = true });
                    break;
                case ItemKind.Bridge:
                    if (pItem.flag) Pieces.Add(new Piece { s0 = pItem.s, s1 = pItem.s1, lateral0 = pItem.lateral, lateral1 = pItem.lateral, height0 = pItem.height, height1 = pItem.height });
                    break;
                case ItemKind.Rope:
                    if (pItem.flag) Pieces.Add(new Piece { s0 = pItem.s, s1 = pItem.s1, lateral0 = pItem.lateral, lateral1 = pItem.lateral, height0 = pItem.height, height1 = pItem.height1 });
                    break;
                case ItemKind.Ring:
                    if (pItem.flag) Pieces.Add(new Piece { s0 = pItem.s - 30f, s1 = pItem.s + 30f, lateral0 = pItem.lateral, lateral1 = pItem.lateral, height0 = pItem.height, height1 = pItem.height });
                    break;
            }
        }

        // The sky piece under s, or the next one ahead within pAhead.
        public static bool Find(float pS, float pAhead, out Piece pPiece)
        {
            pPiece = default(Piece);
            float lBest = float.MaxValue;
            foreach (Piece lPiece in Pieces)
            {
                // A piece ending right here is behind already : steering to it would turn the runner around.
                if (lPiece.s1 < pS + 30f || lPiece.s0 > pS + pAhead) continue;
                float lStart = Mathf.Max(0f, lPiece.s0 - pS);
                if (lStart < lBest)
                {
                    lBest = lStart;
                    pPiece = lPiece;
                }
            }
            return lBest < float.MaxValue;
        }

        public static bool NextKicker(float pS, float pAhead, out Piece pPiece)
        {
            pPiece = default(Piece);
            float lBest = float.MaxValue;
            foreach (Piece lKicker in Kickers)
            {
                if (lKicker.s1 < pS || lKicker.s0 > pS + pAhead) continue;
                if (lKicker.s0 < lBest)
                {
                    lBest = lKicker.s0;
                    pPiece = lKicker;
                }
            }
            return lBest < float.MaxValue;
        }
    }
}
