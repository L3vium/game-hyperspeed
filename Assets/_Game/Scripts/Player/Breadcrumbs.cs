using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public struct Breadcrumb
    {
        public Vector3 position;
        public Vector3 heading;
        public float s;
        public Deck deck;
        public float lateral;
        public float speed;
        public bool safe;
    }

    // Ring buffer of where the runner has been (every 0.1 s) : the rewind replays it, respawn picks a safe one.
    public class Breadcrumbs
    {
        private const int CAPACITY = 90;
        private const float INTERVAL = 0.1f;

        private readonly Breadcrumb[] _crumbs = new Breadcrumb[CAPACITY];
        private int _head;
        private int _count;
        private float _last = -10f;

        public int Count => _count;

        public bool HasSafe
        {
            get
            {
                for (int i = 0; i < _count; i++) if (Get(i).safe) return true;
                return false;
            }
        }

        // i = 0 is the newest crumb.
        public Breadcrumb Get(int pIndex)
        {
            int lIndex = (_head - 1 - pIndex + CAPACITY * 2) % CAPACITY;
            return _crumbs[lIndex];
        }

        public void Clear()
        {
            _count = 0;
            _head = 0;
            _last = -10f;
        }

        public void Record(PlayerController pPlayer, Deck pDeck, float pS, float pLateral, float pSpeed, float pTime)
        {
            if (pTime - _last < INTERVAL) return;
            _last = pTime;
            float lHalf = pDeck.Width(pS) * 0.5f;
            bool lSafe = pDeck.surface == DeckSurface.Solid && lHalf - Mathf.Abs(pDeck.Lateral(pPlayer.transform.position, pS)) >= 2f
                && !(pDeck.endCap && pDeck.EndS - pS < 25f) && pS - pDeck.StartS > 4f;
            _crumbs[_head] = new Breadcrumb
            {
                position = pPlayer.transform.position,
                heading = pPlayer.Heading,
                s = pS,
                deck = pDeck,
                lateral = pDeck.Lateral(pPlayer.transform.position, pS),
                speed = pSpeed,
                safe = lSafe
            };
            _head = (_head + 1) % CAPACITY;
            _count = Mathf.Min(_count + 1, CAPACITY);
        }

        // Newest safe crumb at least 25 m before the death point, else the layout safe point before it.
        public Breadcrumb PickSafe(float pDeathS, LevelLayout pLayout)
        {
            for (int i = 0; i < _count; i++)
            {
                Breadcrumb lCrumb = Get(i);
                if (lCrumb.safe && lCrumb.s <= pDeathS - 25f) return lCrumb;
            }
            SafePoint lPoint = pLayout.SafePointBefore(pDeathS - 25f);
            Vector3 lPosition = lPoint.deck != null ? lPoint.deck.SurfacePoint(lPoint.s, 0f) : pLayout.startPosition;
            return new Breadcrumb { position = lPosition, s = lPoint.s, deck = lPoint.deck, lateral = 0f, speed = PlayerController.RUN_SPEED, safe = true };
        }
    }
}
