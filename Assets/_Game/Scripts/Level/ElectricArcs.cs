using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Lightning crawling along a rope, and bursting around the runner while it rides.
    public class ElectricArcs : MonoBehaviour
    {
        private const int POINTS = 9;
        private const int ARCS = 3;

        private GrindRail _rail;
        private LineRenderer[] _lines;
        private float _timer;

        public static void Attach(GrindRail pRail, Color pColor)
        {
            ElectricArcs lArcs = pRail.gameObject.AddComponent<ElectricArcs>();
            lArcs._rail = pRail;
            lArcs._lines = new LineRenderer[ARCS];
            Material lMaterial = Mats.Glow(Color.Lerp(pColor, Color.white, 0.5f) * 3f, true);
            for (int i = 0; i < ARCS; i++)
            {
                LineRenderer lLine = new GameObject("Arc" + i).AddComponent<LineRenderer>();
                lLine.transform.SetParent(pRail.transform, false);
                lLine.positionCount = POINTS;
                lLine.sharedMaterial = lMaterial;
                lLine.startWidth = 0.9f;
                lLine.endWidth = 0.2f;
                lLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lLine.enabled = false;
                lArcs._lines[i] = lLine;
            }
        }

        private void Update()
        {
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null) return;
            bool lRiding = lPlayer.CurrentRail == _rail;
            float lDistance;
            float lRailDistance;
            bool lNear = _rail.FindNearest(lPlayer.transform.position, 300f, out lDistance, out lRailDistance);
            for (int i = 0; i < ARCS; i++) _lines[i].enabled = lNear;
            if (!lNear) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = 0.04f;
            float lLength = lRiding ? 26f : 40f;
            for (int i = 0; i < ARCS; i++)
            {
                float lCenter = lRailDistance + Random.Range(-1f, 3f) * lLength * (lRiding ? 0.3f : 1f);
                for (int j = 0; j < POINTS; j++)
                {
                    float lT = j / (float)(POINTS - 1);
                    Vector3 lPos, lTangent, lUp;
                    _rail.Evaluate(lCenter + (lT - 0.5f) * lLength, out lPos, out lTangent, out lUp);
                    float lWander = Mathf.Sin(lT * Mathf.PI) * (lRiding ? 4.5f : 3f);
                    _lines[i].SetPosition(j, lPos + Random.insideUnitSphere * lWander);
                }
            }
        }
    }
}
