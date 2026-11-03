using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Physics layers used by the generated level.
    public static class Layers
    {
        public const int DECK = 8;
        public const int MEMBRANE = 9;
        public const int PROP = 10;
        public const int DEBRIS = 11;

        public const int GROUND = (1 << 0) | (1 << DECK) | (1 << MEMBRANE) | (1 << PROP);
        public const int GROUND_NO_MEMBRANE = (1 << 0) | (1 << DECK) | (1 << PROP);
    }

    // Links a deck collider chunk to its deck (registry keyed by collider : no GetComponent on ground hits).
    public class DeckChunk : MonoBehaviour
    {
        private static readonly Dictionary<Collider, DeckChunk> _byCollider = new Dictionary<Collider, DeckChunk>();

        [System.NonSerialized] public Deck deck;
        public float s0;
        public float s1;
        public Collider body;

        public static DeckChunk Register(GameObject pObject, Deck pDeck, float pS0, float pS1, Collider pCollider)
        {
            DeckChunk lChunk = pObject.AddComponent<DeckChunk>();
            lChunk.deck = pDeck;
            lChunk.s0 = pS0;
            lChunk.s1 = pS1;
            lChunk.body = pCollider;
            if (pCollider != null) _byCollider[pCollider] = lChunk;
            return lChunk;
        }

        public static Deck DeckOf(Collider pCollider)
        {
            DeckChunk lChunk;
            return pCollider != null && _byCollider.TryGetValue(pCollider, out lChunk) ? lChunk.deck : null;
        }

        public static void ClearAll() => _byCollider.Clear();

        private void OnDestroy()
        {
            if (body != null) _byCollider.Remove(body);
        }
    }
}
