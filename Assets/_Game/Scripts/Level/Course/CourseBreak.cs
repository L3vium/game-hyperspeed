using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Late stages : you are going so fast the ground gives way behind you. Chunks fall out of the world once you are past,
    // and come back on a rewind.
    public class CourseBreak : MonoBehaviour
    {
        private class Falling
        {
            public TrackChunk chunk;
            public Vector3 home;
            public Quaternion homeRotation;
            public float time;
            public Vector3 spin;
        }

        public static CourseBreak Instance { get; private set; }

        private readonly List<Falling> _falling = new List<Falling>();
        private int _next;
        private float _behind;
        private float _scar;

        public static CourseBreak Create(LevelLayout pLayout)
        {
            CourseBreak lBreak = new GameObject("CourseBreak").AddComponent<CourseBreak>();
            lBreak._behind = pLayout.profile.cruise * 1.1f;
            Instance = lBreak;
            return lBreak;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null || !RaceContext.Running || RespawnDirector.HoldWorld) return;
            float lLine = lPlayer.GroundProgress - _behind;
            List<TrackChunk> lChunks = TrackBaker.Chunks;
            while (_next < lChunks.Count && lChunks[_next].s1 < lLine)
            {
                TrackChunk lChunk = lChunks[_next++];
                _falling.Add(new Falling
                {
                    chunk = lChunk,
                    home = lChunk.gameObject.transform.position,
                    homeRotation = lChunk.gameObject.transform.rotation,
                    spin = Random.onUnitSphere * 6f
                });
                lChunk.collider.enabled = false;
            }
            for (int i = _falling.Count - 1; i >= 0; i--)
            {
                Falling lItem = _falling[i];
                lItem.time += Time.deltaTime;
                Transform lTransform = lItem.chunk.gameObject.transform;
                lTransform.position = lItem.home + Vector3.down * 45f * lItem.time * lItem.time;
                lTransform.rotation = lItem.homeRotation * Quaternion.Euler(lItem.spin * lItem.time);
                if (lItem.time > 6f && lItem.chunk.gameObject.activeSelf) lItem.chunk.gameObject.SetActive(false);
            }
            // The ground bursts right behind the runner at light speed.
            if (lPlayer.LightForm && lPlayer.State == PlayerState.Ground)
            {
                _scar -= Time.deltaTime;
                if (_scar <= 0f)
                {
                    _scar = 0.09f;
                    FX.Shards(lPlayer.transform.position - lPlayer.Heading * 6f, RaceContext.Theme.roadSide, 8, 40f, 2.4f, lPlayer.Up * 26f);
                    FX.Shockwave(lPlayer.transform.position - lPlayer.Heading * 10f, lPlayer.Up, Color.white, 26f, 0.3f);
                }
            }
        }

        public void RewindTo(float pS)
        {
            float lLine = pS - _behind;
            for (int i = _falling.Count - 1; i >= 0; i--)
            {
                Falling lItem = _falling[i];
                if (lItem.chunk.s1 < lLine) continue;
                lItem.chunk.gameObject.SetActive(true);
                lItem.chunk.gameObject.transform.SetPositionAndRotation(lItem.home, lItem.homeRotation);
                lItem.chunk.collider.enabled = true;
                _falling.RemoveAt(i);
            }
            int lIndex = TrackBaker.Chunks.FindIndex(pChunk => pChunk.s1 >= lLine);
            if (lIndex >= 0) _next = Mathf.Min(_next, lIndex);
        }
    }
}
