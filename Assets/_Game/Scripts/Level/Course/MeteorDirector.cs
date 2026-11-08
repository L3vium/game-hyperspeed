using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Wakes the meteorites of the stage as the runner gets near : they aim at where it will be.
    public class MeteorDirector : MonoBehaviour
    {
        private struct Spot { public float s; public bool sky; public float radius; public float lateral; }

        private const float WARNING_TIME = 3f;

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
            Vector3 lFrom = lHit + pLayout.spine.Forward(pS) * lSpeed * 1.2f + pLayout.spine.Right(pS) * Random.Range(-1f, 1f) * 60f + lUp * 240f;
            lObject.transform.position = lFrom;
            lMeteor._velocity = (lHit - lFrom) / lTime;
            lMeteor._spin = Random.onUnitSphere * 160f;
            BuildVisual(lObject.transform, pRadius, pTheme, (int)pS);
            lMeteor._trail = FX.FireTrail(lObject.transform, pRadius);
            AudioManager.Play("sfx_meteor_warning", 0.7f);
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
            if (Physics.Raycast(transform.position, _velocity.normalized, out lHit, _velocity.magnitude * 0.06f + radius, 1 << Layers.DECK, QueryTriggerInteraction.Ignore)) Land();
        }

        private void Land()
        {
            _landed = true;
            FX.Shockwave(transform.position - Vector3.up * radius * 0.7f, Vector3.up, new Color(1f, 0.45f, 0.1f), radius * 3.2f, 0.5f);
            if (_trail != null) Destroy(_trail.gameObject);
            float lDistance = RaceContext.Player != null ? Vector3.Distance(RaceContext.Player.transform.position, transform.position) : 999f;
            if (lDistance < 500f) Juice.Shake(Mathf.Clamp01(0.5f - lDistance / 1000f));
            AudioManager.Play("sfx_rock", 0.7f, 0.7f);
            Destroy(gameObject, 8f);
        }
    }
}
