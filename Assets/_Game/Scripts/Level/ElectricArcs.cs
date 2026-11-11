using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Lightning crawling along a rope, seen from far away, and bursting around the runner while it rides. The arcs wander
    // wider than the tube, so the electricity is never hidden inside it.
    public class ElectricArcs : MonoBehaviour
    {
        private const int POINTS = 9;
        private const int ARCS = 12;
        private const float SEEN_FROM = 3200f;

        private GrindRail _rail;
        private LineRenderer[] _lines;
        private float _timer;

        public static void Attach(GrindRail pRail, Color pColor)
        {
            ElectricArcs lArcs = pRail.gameObject.AddComponent<ElectricArcs>();
            lArcs._rail = pRail;
            lArcs._lines = new LineRenderer[ARCS];
            Material lMaterial = Mats.Glow(Color.Lerp(pColor, Color.white, 0.55f) * 3.2f, true);
            for (int i = 0; i < ARCS; i++)
            {
                LineRenderer lLine = new GameObject("Arc" + i).AddComponent<LineRenderer>();
                lLine.transform.SetParent(pRail.transform, false);
                lLine.positionCount = POINTS;
                lLine.sharedMaterial = lMaterial;
                lLine.startWidth = 2.4f;
                lLine.endWidth = 0.4f;
                lLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lLine.enabled = false;
                lArcs._lines[i] = lLine;
            }
        }

        private void Update()
        {
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null || _rail.Length < 1f) return;
            bool lRiding = lPlayer.CurrentRail == _rail;
            bool lNear = lRiding || _rail.Area.SqrDistance(lPlayer.transform.position) < SEEN_FROM * SEEN_FROM;
            for (int i = 0; i < ARCS; i++) _lines[i].enabled = lNear;
            if (!lNear) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = 0.045f;
            float lRider = 0f;
            if (lRiding)
            {
                float lIgnored;
                _rail.FindNearest(lPlayer.transform.position, 50f, out lIgnored, out lRider);
            }
            for (int i = 0; i < ARCS; i++)
            {
                // Riding : bursts around you, a little ahead. Otherwise : crawling anywhere along the whole rope.
                bool lAround = lRiding && i < 5;
                float lLength = lAround ? 46f : 80f;
                float lCenter = lAround ? lRider + Random.Range(-1.5f, 1f) * lLength * 0.4f : Random.Range(0f, _rail.Length);
                float lWander = lAround ? 11f : 7f;
                for (int j = 0; j < POINTS; j++)
                {
                    float lT = j / (float)(POINTS - 1);
                    Vector3 lPos, lTangent, lUp;
                    _rail.Evaluate(lCenter + (lT - 0.5f) * lLength, out lPos, out lTangent, out lUp);
                    _lines[i].SetPosition(j, lPos + Random.insideUnitSphere * Mathf.Sin(lT * Mathf.PI) * lWander);
                }
            }
        }
    }
}
