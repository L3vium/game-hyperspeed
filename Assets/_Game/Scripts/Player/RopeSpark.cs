using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // On an electric rope the runner is wrapped in electricity (2026-11-12, "comme le pylône à étincelles") : a sheath of
    // jagged arcs crackling all around the body, a few bolts jumping down to the rope, sparks streaming behind. The body
    // stays visible inside it ; the aura shader (PlayerVisual) glows white-cyan on top.
    public class RopeSpark : MonoBehaviour
    {
        private const int SHEATH = 7;
        private const int TO_ROPE = 2;
        private const int POINTS = 7;
        private const float REFRESH = 0.035f;
        private const float RADIUS = 2.2f;

        private LineRenderer[] _lines;
        private ParticleSystem _sparks;
        private float _timer;
        private float _amount;

        public static RopeSpark Create(Transform pParent)
        {
            RopeSpark lSpark = new GameObject("RopeSpark").AddComponent<RopeSpark>();
            lSpark.transform.SetParent(pParent, false);
            lSpark.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            lSpark.Build();
            return lSpark;
        }

        private void Build()
        {
            Material lMaterial = Mats.Glow(new Color(1.6f, 2.8f, 4f), true);
            _lines = new LineRenderer[SHEATH + TO_ROPE];
            for (int i = 0; i < _lines.Length; i++)
            {
                LineRenderer lLine = new GameObject("Arc" + i).AddComponent<LineRenderer>();
                lLine.transform.SetParent(transform, false);
                lLine.useWorldSpace = true;
                lLine.positionCount = POINTS;
                lLine.sharedMaterial = lMaterial;
                lLine.startWidth = i < SHEATH ? 0.22f : 0.45f;
                lLine.endWidth = 0.06f;
                lLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lLine.receiveShadows = false;
                lLine.enabled = false;
                _lines[i] = lLine;
            }
            _sparks = FX.CreateSystem("RopeSparks", transform.position, Mats.Glow(new Color(1.5f, 2.6f, 4f), true, 1f), false);
            _sparks.transform.SetParent(transform, false);
            _sparks.transform.localPosition = Vector3.zero;
            ParticleSystem.MainModule lMain = _sparks.main;
            lMain.loop = true;
            lMain.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            lMain.startSpeed = new ParticleSystem.MinMaxCurve(4f, 16f);
            lMain.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
            lMain.maxParticles = 400;
            ParticleSystem.ShapeModule lShape = _sparks.shape;
            lShape.shapeType = ParticleSystemShapeType.Sphere;
            lShape.radius = RADIUS * 0.7f;
            ParticleSystemRenderer lRenderer = _sparks.GetComponent<ParticleSystemRenderer>();
            lRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            lRenderer.velocityScale = 0.05f;
            lRenderer.lengthScale = 1.5f;
        }

        // pOn : riding a rope now. pRopePoint : the rope under the runner.
        public void Tick(bool pOn, Vector3 pRopePoint, float pDt)
        {
            _amount = Mathf.MoveTowards(_amount, pOn ? 1f : 0f, pDt * (pOn ? 8f : 4f));
            bool lShow = _amount > 0.01f;
            ParticleSystem.EmissionModule lEmission = _sparks.emission;
            lEmission.rateOverTime = 260f * _amount;
            if (!lShow)
            {
                for (int i = 0; i < _lines.Length; i++) if (_lines[i].enabled) _lines[i].enabled = false;
                return;
            }
            if (!_sparks.isPlaying) _sparks.Play();
            // The screen edges crackle in the rope's colour.
            Juice.Vignette(new Color(0.45f, 0.85f, 1f), 0.35f * _amount);
            _timer -= pDt;
            if (_timer > 0f) return;
            _timer = REFRESH;
            Vector3 lCentre = transform.position;
            for (int i = 0; i < _lines.Length; i++)
            {
                LineRenderer lLine = _lines[i];
                lLine.enabled = Random.value < 0.85f * _amount + 0.1f;
                if (!lLine.enabled) continue;
                Vector3 lFrom, lTo;
                if (i < SHEATH)
                {
                    // Around the body : from one point of a shell to another, bowing outward.
                    lFrom = lCentre + Random.onUnitSphere * RADIUS * Random.Range(0.6f, 1f);
                    lTo = lCentre + Random.onUnitSphere * RADIUS * Random.Range(0.6f, 1f);
                }
                else
                {
                    lFrom = lCentre + Random.insideUnitSphere * 0.8f;
                    lTo = pRopePoint + Random.insideUnitSphere * 1.2f;
                }
                for (int j = 0; j < POINTS; j++)
                {
                    float lT = j / (float)(POINTS - 1);
                    Vector3 lPoint = Vector3.Lerp(lFrom, lTo, lT);
                    if (i < SHEATH) lPoint = lCentre + (lPoint - lCentre).normalized * Mathf.Max((lPoint - lCentre).magnitude, RADIUS * (0.7f + 0.35f * Mathf.Sin(lT * Mathf.PI)));
                    lPoint += Random.insideUnitSphere * 0.55f * Mathf.Sin(lT * Mathf.PI);
                    lLine.SetPosition(j, lPoint);
                }
            }
        }
    }
}
