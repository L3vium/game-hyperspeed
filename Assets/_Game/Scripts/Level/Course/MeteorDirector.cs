using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Wakes the meteorites of the stage as the runner gets near : they aim at where it will be.
    public class MeteorDirector : MonoBehaviour
    {
        private struct Spot { public float s; public bool sky; public float radius; public float lateral; }

        private const float WARNING_TIME = 4.5f;

        private readonly List<Spot> _spots = new List<Spot>();
        private LevelLayout _layout;
        private Theme _theme;
        private int _next;

        public void Add(float pS, bool pSky, float pRadius, float pLateral) => _spots.Add(new Spot { s = pS, sky = pSky, radius = pRadius, lateral = pLateral });

        public void Init(LevelLayout pLayout, Theme pTheme)
        {
            _layout = pLayout;
            _theme = pTheme;
            _spots.Sort((pA, pB) => pA.s.CompareTo(pB.s));
            // Boulders are simply there from the start.
            for (int i = _spots.Count - 1; i >= 0; i--)
            {
                if (_spots[i].sky) continue;
                Meteor.CreateStanding(transform, pLayout, _spots[i].s, _spots[i].lateral, _spots[i].radius, pTheme);
                _spots.RemoveAt(i);
            }
        }

        private void Update()
        {
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null || !RaceContext.Running) return;
            // A rewind brings earlier meteors back.
            while (_next > 0 && _spots[_next - 1].s > lPlayer.Progress + lPlayer.Speed * WARNING_TIME) _next--;
            while (_next < _spots.Count && _spots[_next].s <= lPlayer.Progress + Mathf.Max(lPlayer.Speed, 40f) * WARNING_TIME)
            {
                Spot lSpot = _spots[_next++];
                if (lSpot.s < lPlayer.Progress) continue;
                Meteor.CreateIncoming(transform, _layout, lPlayer, lSpot.s, lSpot.radius, _theme);
            }
        }
    }

    // Giant rock on fire falling on your path : punch it apart, or be fast enough to go through.
    public class Meteor : Breakable
    {
        private Vector3 _velocity;
        private Vector3 _spin;
        private LevelLayout _layout;
        private float _groundCheck;
        private bool _landed;
        private ParticleSystem _trail;

        private static void BuildVisual(Transform pParent, float pRadius, Theme pTheme, int pSeed)
        {
            GameObject lRock = new GameObject("Rock");
            lRock.transform.SetParent(pParent, false);
            lRock.transform.localScale = Vector3.one * pRadius * 1.9f;
            lRock.transform.localPosition = Vector3.down * pRadius * 0.9f;
            lRock.AddComponent<MeshFilter>().sharedMesh = RockSculptor.Boulder(pSeed);
            Color lColor = Color.Lerp(pTheme.decorC, new Color(0.18f, 0.1f, 0.12f), 0.6f);
            lRock.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(pTheme.shade, false, lColor);
            // Lava cracks : glowing core poking through the rock.
            GameObject lCore = Mats.Primitive(PrimitiveType.Sphere, pParent, Vector3.zero, Vector3.one * pRadius * 1.72f, Mats.Glow(new Color(3.2f, 1.1f, 0.15f), false, 0f, 0f, true));
            lCore.name = "Lava";
        }

        public static Meteor CreateIncoming(Transform pParent, LevelLayout pLayout, PlayerController pPlayer, float pS, float pRadius, Theme pTheme, float pAimLateral = float.NaN)
        {
            GameObject lObject = new GameObject("Meteor");
            lObject.transform.SetParent(pParent, false);
            Meteor lMeteor = lObject.AddComponent<Meteor>();
            lMeteor.radius = pRadius;
            lMeteor.big = true;
            lMeteor.color = new Color(0.32f, 0.2f, 0.2f);
            lMeteor._layout = pLayout;

            float lSpeed = Mathf.Max(pPlayer.Speed, pLayout.profile.cruise * 0.7f);
            float lTime = Mathf.Max(0.8f, (pS - pPlayer.Progress) / lSpeed);
            float lAim = float.IsNaN(pAimLateral) ? pPlayer.Lateral + Random.Range(-1f, 1f) * pRadius * 0.8f : pAimLateral;
            Vector3 lUp = pLayout.field.Normal(pS);
            Vector3 lHit = pLayout.GroundPoint(pS, lAim) + lUp * pRadius * 0.9f;
            // From high and far : a fire streak you see coming across the whole sky.
            Vector3 lFrom = lHit + pLayout.spine.Forward(pS) * lSpeed * 1.6f + pLayout.spine.Right(pS) * Random.Range(-1f, 1f) * 200f + lUp * 900f;
            lObject.transform.position = lFrom;
            lMeteor._velocity = (lHit - lFrom) / lTime;
            lMeteor._spin = Random.onUnitSphere * 160f;
            // A red circle where it will land, closing until the impact (2026-11-12).
            TelegraphRing.Create(pLayout.GroundPoint(pS, lAim), lUp, pRadius * 1.6f, lTime, true);
            BuildVisual(lObject.transform, pRadius, pTheme, (int)pS);
            lMeteor._trail = FX.FireTrail(lObject.transform, pRadius);
            AudioManager.Play("sfx_meteor_warn", 0.7f);
            return lMeteor;
        }

        public static Meteor CreateStanding(Transform pParent, LevelLayout pLayout, float pS, float pLateral, float pRadius, Theme pTheme)
        {
            GameObject lObject = new GameObject("Boulder");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.position = pLayout.GroundPoint(pS, pLateral) + pLayout.field.Normal(pS) * pRadius * 0.85f;
            Meteor lMeteor = lObject.AddComponent<Meteor>();
            lMeteor.radius = pRadius;
            lMeteor.big = true;
            lMeteor.color = new Color(0.32f, 0.2f, 0.2f);
            lMeteor._landed = true;
            BuildVisual(lObject.transform, pRadius, pTheme, (int)pS);
            return lMeteor;
        }

        protected override void Update()
        {
            base.Update();
            if (_landed || IsBroken) return;
            float lDt = Time.deltaTime;
            transform.position += _velocity * lDt;
            transform.Rotate(_spin * lDt, Space.World);
            _groundCheck -= lDt;
            if (_groundCheck > 0f || _layout == null) return;
            _groundCheck = 0.05f;
            RaycastHit lHit;
            if (Physics.Raycast(transform.position, _velocity.normalized, out lHit, _velocity.magnitude * 0.06f + radius, (1 << Layers.DECK) | (1 << Layers.SCENERY) | (1 << Layers.PROP), QueryTriggerInteraction.Ignore)) Land();
            // Missed everything (over a gap) : it burns out far below instead of falling for ever.
            else if (RaceContext.Player != null && transform.position.y < RaceContext.Player.transform.position.y - 1500f) Destroy(gameObject);
        }

        // It hits the ground and explodes : fire ring, rock flying everywhere, a boom you feel if you are close. Under
        // it is a crash.
        private void Land()
        {
            _landed = true;
            Vector3 lAt = transform.position - Vector3.up * radius * 0.7f;
            Color lFire = new Color(1f, 0.45f, 0.1f);
            FX.Shockwave(lAt, Vector3.up, lFire, radius * 4f, 0.55f);
            FX.Shockwave(lAt + Vector3.up, Vector3.up, Color.white, radius * 2.2f, 0.3f);
            FX.Burst(transform.position, Vector3.up, lFire, radius * 2.5f);
            FX.Sparks(transform.position, lFire, 60, radius * 3f, 1.2f, 0.8f);
            FX.Puff(lAt, new Color(0.25f, 0.2f, 0.2f), 18, radius * 0.9f, radius * 1.2f);
            if (_trail != null) Destroy(_trail.gameObject);
            TrackHoles.Punch(lAt, Mathf.Clamp(radius * 1.2f, 14f, 40f));
            PlayerController lPlayer = RaceContext.Player;
            float lDistance = lPlayer != null ? Vector3.Distance(lPlayer.transform.position, transform.position) : 999f;
            if (lPlayer != null && lDistance < radius * 1.6f && !lPlayer.Dashing && !lPlayer.LightForm) lPlayer.Crash(transform.position);
            HSJuice.Play(JuiceId.MeteorNear, transform.position, Mathf.Clamp01(1f - lDistance / 1200f));
            Smash(Vector3.up, radius * 6f);
        }
    }
}
