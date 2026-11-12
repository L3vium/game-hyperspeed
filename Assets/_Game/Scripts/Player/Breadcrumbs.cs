using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public struct Breadcrumb
    {
        public Vector3 position;
        public Vector3 heading;
        public float s;
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

        public void Record(PlayerController pPlayer, LevelLayout pLayout, float pS, float pLateral, float pSpeed, float pTime)
        {
            if (pTime - _last < INTERVAL) return;
            _last = pTime;
            _crumbs[_head] = new Breadcrumb
            {
                position = pPlayer.transform.position,
                heading = pPlayer.Heading,
                s = pS,
                lateral = pLateral,
                speed = pSpeed,
                safe = pLayout.IsSafe(pS, pLateral)
            };
            _head = (_head + 1) % CAPACITY;
            _count = Mathf.Min(_count + 1, CAPACITY);
        }

        // Newest safe crumb about a second and a half before the death point, else the safe ground found on the route.
        public Breadcrumb PickSafe(float pDeathS, float pSpeed, LevelLayout pLayout)
        {
            float lBack = Mathf.Max(100f, Mathf.Max(pSpeed, pLayout.profile.cruise) * 1.5f);
            for (int i = 0; i < _count; i++)
            {
                Breadcrumb lCrumb = Get(i);
                if (lCrumb.safe && lCrumb.s <= pDeathS - lBack && (TrackHoles.Count == 0 || !TrackHoles.Inside(lCrumb.position, 40f))) return lCrumb;
            }
            float lS = Mathf.Max(pDeathS - lBack, pLayout.startLineS + 5f);
            for (int i = 0; i < 60 && !pLayout.IsSafe(lS, pLayout.field.CenterLateral(lS)) && lS > pLayout.startLineS + 5f; i++) lS -= lBack * 0.25f;
            lS = Mathf.Max(lS, pLayout.startLineS + 5f);
            float lCenter = pLayout.field.CenterLateral(lS);
            return new Breadcrumb { position = pLayout.GroundPoint(lS, lCenter), heading = pLayout.Flow(lS), s = lS, lateral = lCenter, speed = pLayout.profile.cruise, safe = true };
        }
    }
}
