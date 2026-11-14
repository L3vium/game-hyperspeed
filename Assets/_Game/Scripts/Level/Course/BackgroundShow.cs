using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // The background is where most things happen (2026-11-12, "le background doit être le plus vivant") : colossal
    // machines walking along the horizon and firing beams, explosions going off kilometres away with their fireball,
    // flash and column of smoke, all the time, on every side. Cheap shapes, far away, a fixed number of them.
    public class BackgroundShow : MonoBehaviour
    {
        private const int MACHINES = 4;
        private const int BLASTS = 10;

        private class Machine
        {
            public Transform root;
            public Transform[] legs;
            public Transform eye;
            public float s;
            public int side;
            public float distance;
            public float phase;
            public float beamTimer;
            public LineRenderer beam;
            public float beamLife;
        }

        private class Blast
        {
            public Transform fire;
            public Transform smoke;
            public Material fireMaterial;
            public Material smokeMaterial;
            public float age = 99f;
            public float size;
        }

        private readonly List<Machine> _machines = new List<Machine>();
        private readonly List<Blast> _blasts = new List<Blast>();
        private LevelLayout _layout;
        private Theme _theme;
        private float _blastTimer = 1f;
        private float _storm;

        public static BackgroundShow Create(Transform pParent, LevelLayout pLayout, Theme pTheme, float pStorm)
        {
            BackgroundShow lShow = new GameObject("BackgroundShow").AddComponent<BackgroundShow>();
            lShow.transform.SetParent(pParent, false);
            lShow._layout = pLayout;
            lShow._theme = pTheme;
            lShow._storm = pStorm;
            for (int i = 0; i < MACHINES; i++) lShow._machines.Add(lShow.BuildMachine(i));
            for (int i = 0; i < BLASTS; i++) lShow._blasts.Add(lShow.BuildBlast());
            return lShow;
        }

        private Machine BuildMachine(int pIndex)
        {
            Machine lMachine = new Machine();
            lMachine.root = new GameObject("Machine" + pIndex).transform;
            lMachine.root.SetParent(transform, false);
            Material lHull = Mats.Flat(Color.Lerp(_theme.shade, Color.black, 0.5f), false, Color.Lerp(_theme.wall, new Color(0.2f, 0.2f, 0.25f), 0.6f));
            Material lGlow = Mats.Glow(new Color(4f, 0.6f, 0.3f), false, 0f, 0f, true);
            float lScale = Random.Range(0.8f, 1.4f);
            lMachine.root.localScale = Vector3.one * lScale;
            // A walker the size of a mountain : a hull on four long legs, a burning eye, lights along its back.
            Mats.Primitive(PrimitiveType.Cube, lMachine.root, new Vector3(0f, 620f, 0f), new Vector3(320f, 160f, 560f), lHull, "Hull");
            Mats.Primitive(PrimitiveType.Cube, lMachine.root, new Vector3(0f, 720f, 120f), new Vector3(200f, 90f, 240f), lHull, "Deck");
            Transform lHead = Mats.Primitive(PrimitiveType.Cube, lMachine.root, new Vector3(0f, 640f, 330f), new Vector3(140f, 110f, 140f), lHull, "Head").transform;
            lMachine.eye = Mats.Primitive(PrimitiveType.Sphere, lHead, new Vector3(0f, 0f, 0.55f), new Vector3(0.6f, 0.35f, 0.2f), lGlow, "Eye").transform;
            for (int k = 0; k < 6; k++) Mats.Primitive(PrimitiveType.Cube, lMachine.root, new Vector3(Random.Range(-120f, 120f), 705f, -200f + k * 70f), new Vector3(14f, 14f, 14f), lGlow, "Light");
            lMachine.legs = new Transform[4];
            for (int l = 0; l < 4; l++)
            {
                Transform lHip = new GameObject("Hip" + l).transform;
                lHip.SetParent(lMachine.root, false);
                lHip.localPosition = new Vector3(l % 2 == 0 ? -170f : 170f, 600f, l < 2 ? 220f : -220f);
                Mats.Primitive(PrimitiveType.Cylinder, lHip, new Vector3(0f, -300f, 0f), new Vector3(46f, 300f, 46f), lHull, "Leg");
                lMachine.legs[l] = lHip;
            }
            lMachine.beam = new GameObject("Beam").AddComponent<LineRenderer>();
            lMachine.beam.transform.SetParent(lMachine.root, false);
            lMachine.beam.useWorldSpace = true;
            lMachine.beam.positionCount = 2;
            lMachine.beam.widthMultiplier = 40f;
            lMachine.beam.sharedMaterial = Mats.Glow(new Color(4f, 0.5f, 0.3f, 0.9f), true);
            lMachine.beam.enabled = false;
            lMachine.side = pIndex % 2 == 0 ? 1 : -1;
            lMachine.distance = Random.Range(5000f, 9000f);
            lMachine.s = -1f;
            lMachine.phase = Random.value * 10f;
            lMachine.beamTimer = Random.Range(2f, 6f);
            return lMachine;
        }

        private Blast BuildBlast()
        {
            Blast lBlast = new Blast();
            lBlast.fireMaterial = new Material(Mats.Glow(new Color(4f, 1.6f, 0.4f, 1f), true, 1f));
            lBlast.smokeMaterial = new Material(Mats.Glow(new Color(0.15f, 0.13f, 0.14f, 0.8f), true, 1f));
            lBlast.fire = Mats.Primitive(PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one, lBlast.fireMaterial, "Blast").transform;
            lBlast.smoke = Mats.Primitive(PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one, lBlast.smokeMaterial, "Smoke").transform;
            lBlast.fire.gameObject.SetActive(false);
            lBlast.smoke.gameObject.SetActive(false);
            return lBlast;
        }

        private void Update()
        {
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null || _layout == null) return;
            float lDt = Time.deltaTime;
            if (lDt <= 0f) return;
            Vector3 lAt = lPlayer.transform.position;
            Vector3 lForward = Vector3.ProjectOnPlane(_layout.Flow(lPlayer.Progress), Vector3.up).normalized;
            if (lForward.sqrMagnitude < 0.01f) lForward = Vector3.forward;
            Vector3 lRight = Vector3.Cross(Vector3.up, lForward);
            float lSpeed = Mathf.Max(lPlayer.Speed, _layout.profile.cruise);

            foreach (Machine lMachine in _machines) UpdateMachine(lMachine, lPlayer, lForward, lRight, lSpeed, lDt);
            // The colossal machines' rumble, far off, under everything.
            AudioManager.SetLoop("sfx_bg_machine_loop", RaceContext.Running ? 0.22f : 0f);

            // Explosions somewhere out there, all the time : more of them when the storm is heavier.
            _blastTimer -= lDt;
            if (_blastTimer <= 0f)
            {
                _blastTimer = Random.Range(0.35f, 1.1f) / Mathf.Lerp(0.6f, 1.4f, _storm);
                Blast lFree = _blasts.Find(pBlast => pBlast.age > 6f);
                if (lFree != null)
                {
                    float lAngle = Random.Range(-100f, 100f) * Mathf.Deg2Rad;
                    float lFar = Random.Range(2500f, 9000f);
                    Vector3 lDir = lForward * Mathf.Cos(lAngle) + lRight * Mathf.Sin(lAngle);
                    Vector3 lPoint = lAt + lDir * lFar + lForward * lSpeed * 1.5f;
                    lPoint.y = lAt.y - Random.Range(200f, 900f);
                    lFree.fire.position = lPoint;
                    lFree.smoke.position = lPoint;
                    lFree.size = Random.Range(220f, 900f);
                    lFree.age = 0f;
                    lFree.fire.gameObject.SetActive(true);
                    lFree.smoke.gameObject.SetActive(true);
                    ChaosDirector.SkyFlash = Mathf.Max(ChaosDirector.SkyFlash, 0.25f * (1f - lFar / 9000f) + 0.1f);
                    // v6 : the distant blasts are their own sounds (no pitched-down explosion any more).
                    if (lFar < 6000f) AudioManager.Play("sfx_bg_explosion", Mathf.Lerp(0.55f, 0.2f, lFar / 6000f), 1f, 0.03f);
                }
            }
            foreach (Blast lBlast in _blasts)
            {
                if (lBlast.age > 6f) continue;
                lBlast.age += lDt;
                float lFireT = Mathf.Clamp01(lBlast.age / 1.4f);
                lBlast.fire.localScale = Vector3.one * lBlast.size * Mathf.Sqrt(lFireT);
                lBlast.fireMaterial.SetColor("_Color", new Color(4f, 1.6f, 0.4f, 1f - lFireT));
                float lSmokeT = Mathf.Clamp01(lBlast.age / 6f);
                lBlast.smoke.localScale = Vector3.one * lBlast.size * (0.6f + lSmokeT * 1.6f);
                lBlast.smoke.position += Vector3.up * lBlast.size * 0.25f * lDt;
                lBlast.smokeMaterial.SetColor("_Color", new Color(0.15f, 0.13f, 0.14f, 0.75f * Mathf.Sin(lSmokeT * Mathf.PI)));
                if (lBlast.age > 6f)
                {
                    lBlast.fire.gameObject.SetActive(false);
                    lBlast.smoke.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateMachine(Machine pMachine, PlayerController pPlayer, Vector3 pForward, Vector3 pRight, float pSpeed, float pDt)
        {
            // They walk the same way, slower than you : you pass them, new ones rise ahead.
            if (pMachine.s < 0f || pMachine.s < pPlayer.Progress - 6000f) pMachine.s = pPlayer.Progress + Random.Range(4000f, 14000f);
            pMachine.s += pSpeed * 0.15f * pDt;
            SpineSample lSample = _layout.spine.Evaluate(pMachine.s);
            Vector3 lFlat = Vector3.ProjectOnPlane(lSample.forward, Vector3.up).normalized;
            Vector3 lSide = Vector3.Cross(Vector3.up, lFlat) * pMachine.side;
            Vector3 lBase = lSample.position + lSide * pMachine.distance;
            lBase.y = lSample.position.y - 700f;
            pMachine.root.position = lBase;
            pMachine.root.rotation = Quaternion.LookRotation(lFlat, Vector3.up);
            pMachine.phase += pDt * 0.6f;
            for (int l = 0; l < pMachine.legs.Length; l++)
                pMachine.legs[l].localRotation = Quaternion.Euler(Mathf.Sin(pMachine.phase + (l == 0 || l == 3 ? 0f : Mathf.PI)) * 18f, 0f, 0f);
            pMachine.root.position += Vector3.up * Mathf.Abs(Mathf.Sin(pMachine.phase * 2f)) * 25f;
            // Now and then it fires : a red beam down into the valley, an explosion where it lands.
            pMachine.beamTimer -= pDt;
            if (pMachine.beamTimer <= 0f && !pMachine.beam.enabled)
            {
                pMachine.beamTimer = Random.Range(3f, 8f);
                pMachine.beamLife = 0.9f;
                pMachine.beam.enabled = true;
            }
            if (pMachine.beam.enabled)
            {
                pMachine.beamLife -= pDt;
                Vector3 lFrom = pMachine.eye.position;
                Vector3 lTo = lFrom - lSide * pMachine.distance * 0.35f + lFlat * 1500f - Vector3.up * 900f;
                pMachine.beam.SetPosition(0, lFrom);
                pMachine.beam.SetPosition(1, lTo);
                pMachine.beam.widthMultiplier = 40f * Mathf.Clamp01(pMachine.beamLife * 3f) * (0.8f + 0.2f * Mathf.Sin(Time.time * 40f));
                if (pMachine.beamLife <= 0f) pMachine.beam.enabled = false;
            }
        }
    }
}
