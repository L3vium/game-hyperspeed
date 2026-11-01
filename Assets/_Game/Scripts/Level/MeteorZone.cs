using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Rains meteors ahead of the player on a stretch of track. Impacts leave smashable rocks.
    public class MeteorZone : MonoBehaviour
    {
        private const float FALL_HEIGHT = 110f;
        private const float FALL_SPEED = 95f;

        public float startDistance;
        public float endDistance;
        public float interval = 0.8f;

        private float _timer;
        private readonly List<Meteor> _meteors = new List<Meteor>();

        private class Meteor
        {
            public Transform body;
            public Transform warning;
            public Vector3 target;
            public Vector3 up;
            public Quaternion rotation;
            public float height;
        }

        private void Update()
        {
            if (!RaceContext.Running || RaceContext.Path == null) return;
            float lProgress = RaceContext.PlayerProgress;
            if (lProgress > startDistance - 120f && lProgress < endDistance)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _timer = interval * Random.Range(0.7f, 1.3f);
                    Spawn(lProgress);
                }
            }

            for (int i = _meteors.Count - 1; i >= 0; i--)
            {
                Meteor lMeteor = _meteors[i];
                lMeteor.height -= FALL_SPEED * Time.deltaTime;
                lMeteor.body.position = lMeteor.target + (lMeteor.up * 1f + lMeteor.rotation * Vector3.forward * 0.35f) * lMeteor.height;
                lMeteor.body.Rotate(new Vector3(200f, 120f, 0f) * Time.deltaTime);
                float lPulse = 1f + Mathf.Sin(Time.time * 20f) * 0.15f;
                lMeteor.warning.localScale = new Vector3(9f * lPulse, 0.05f, 9f * lPulse) * (1.2f - lMeteor.height / FALL_HEIGHT * 0.6f);
                if (lMeteor.height <= 0f)
                {
                    Land(lMeteor);
                    _meteors.RemoveAt(i);
                }
            }
        }

        private void Spawn(float pProgress)
        {
            float lDistance = Mathf.Min(endDistance, pProgress + Random.Range(70f, 150f));
            if (lDistance < startDistance) lDistance = startDistance + Random.Range(0f, 40f);
            TrackSample lSample = RaceContext.Path.Evaluate(lDistance);
            if (!lSample.solid) return;
            float lLateral = Random.Range(-lSample.width * 0.4f, lSample.width * 0.4f);

            Meteor lMeteor = new Meteor();
            lMeteor.target = lSample.position + lSample.Right * lLateral;
            lMeteor.up = lSample.Up;
            lMeteor.rotation = lSample.rotation;
            lMeteor.height = FALL_HEIGHT;

            lMeteor.body = new GameObject("Meteor").transform;
            lMeteor.body.SetParent(transform, false);
            PropFactory.Part(PrimitiveType.Sphere, lMeteor.body, Vector3.zero, Vector3.one * 4f, new Color(0.35f, 0.2f, 0.25f), default(Vector3), 0.04f);
            PropFactory.GlowPart(PrimitiveType.Sphere, lMeteor.body, Vector3.zero, Vector3.one * 4.8f, new Color(3f, 1f, 0.2f, 0.5f));
            TrailRenderer lTrail = lMeteor.body.gameObject.AddComponent<TrailRenderer>();
            lTrail.time = 0.35f;
            lTrail.startWidth = 4f;
            lTrail.endWidth = 0f;
            lTrail.sharedMaterial = Mats.Glow(new Color(3f, 1.2f, 0.3f), true);

            lMeteor.warning = Mats.Primitive(PrimitiveType.Cylinder, transform, Vector3.zero, Vector3.one, Mats.Glow(new Color(3f, 0.1f, 0.1f, 0.6f), true, 1f)).transform;
            lMeteor.warning.SetPositionAndRotation(lMeteor.target + lMeteor.up * 0.15f, Quaternion.LookRotation(lSample.Forward, lSample.Up));
            _meteors.Add(lMeteor);
            AudioManager.Play("sfx_meteor_fall", 0.35f, Random.Range(0.9f, 1.1f));
        }

        private void Land(Meteor pMeteor)
        {
            Destroy(pMeteor.body.gameObject);
            Destroy(pMeteor.warning.gameObject);
            FX.Explosion(pMeteor.target, new Color(1f, 0.5f, 0.1f), 0.8f);
            PropFactory.Rock(transform, pMeteor.target, pMeteor.rotation, new Color(0.45f, 0.3f, 0.32f), 2.6f);

            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null) return;
            float lDistance = Vector3.Distance(lPlayer.transform.position, pMeteor.target);
            if (lDistance < 60f)
            {
                Juice.Shake(0.5f * (1f - lDistance / 60f));
                AudioManager.Play("sfx_explosion", 0.9f * (1f - lDistance / 70f));
            }
            if (lDistance < 4.5f) lPlayer.Crash(pMeteor.target, "BONK!");
        }
    }
}
