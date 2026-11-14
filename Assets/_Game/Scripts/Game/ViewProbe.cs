using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Dev check (2026-11-14) : is the track ahead really visible ? Twice a second it samples the track 2, 5 and 10 s ahead
    // of the runner (on its line) and asks : inside the camera's frame, nothing solid between the eye and it, not lost in
    // the fog. Every ten seconds it logs the share of samples that were visible ([HS-VIEW]) ; the autopilot matrix reads it.
    public class ViewProbe : MonoBehaviour
    {
        private static readonly float[] AHEAD = { 2f, 5f, 10f };
        private const float EVERY = 0.5f;
        private const float WINDOW = 10f;
        private const int BLOCKERS = (1 << Layers.DECK) | (1 << Layers.SCENERY) | (1 << Layers.PROP);

        private float _timer;
        private float _window;
        private int _seen;
        private int _total;
        private int _blocked;
        private int _outside;
        private int _fogged;
        private readonly System.Collections.Generic.Dictionary<string, int> _blockers = new System.Collections.Generic.Dictionary<string, int>();

        public static void Create()
        {
            if (!Debug.isDebugBuild && !Autopilot.Enabled) return;
            new GameObject("ViewProbe").AddComponent<ViewProbe>();
        }

        private void Update()
        {
            PlayerController lPlayer = RaceContext.Player;
            LevelLayout lLayout = RaceContext.Layout;
            Camera lCamera = CameraRig.Instance != null ? CameraRig.Instance.Cam : null;
            if (lPlayer == null || lLayout == null || lCamera == null || !RaceContext.Running || lPlayer.State == PlayerState.Dead) return;
            _timer -= Time.deltaTime;
            _window += Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = EVERY;
                float lSpeed = Mathf.Max(lPlayer.Speed, lLayout.profile.cruise);
                foreach (float lSeconds in AHEAD)
                {
                    float lS = lPlayer.Progress + lSpeed * lSeconds;
                    if (lS > lLayout.finishS) continue;
                    Sample(lCamera, lLayout.GroundPoint(lS, lLayout.field.CenterLateral(lS)) + lLayout.field.Normal(lS) * 3f);
                }
            }
            if (_window >= WINDOW && _total > 0)
            {
                string lTop = "";
                foreach (System.Collections.Generic.KeyValuePair<string, int> lPair in _blockers) lTop += " " + lPair.Key + "x" + lPair.Value;
                _blockers.Clear();
                Debug.Log("[HS-VIEW] visible=" + (_seen * 100 / _total) + "% samples=" + _total + " outside=" + _outside + " blocked=" + _blocked + " fogged=" + _fogged + " s=" + lPlayer.Progress.ToString("F0") + " by:" + lTop);
                _window = 0f;
                _seen = _total = _blocked = _outside = _fogged = 0;
            }
        }

        private void Sample(Camera pCamera, Vector3 pPoint)
        {
            _total++;
            Vector3 lViewport = pCamera.WorldToViewportPoint(pPoint);
            if (lViewport.z <= 0f || lViewport.x < 0f || lViewport.x > 1f || lViewport.y < 0f || lViewport.y > 1f || lViewport.z > pCamera.farClipPlane)
            {
                _outside++;
                return;
            }
            Vector3 lEye = pCamera.transform.position;
            Vector3 lDelta = pPoint - lEye;
            float lDistance = lDelta.magnitude;
            // Something solid in the way (the track itself near the point does not count).
            if (lDistance > 20f && Physics.Raycast(lEye, lDelta / lDistance, out RaycastHit lHit, lDistance - Mathf.Max(15f, lDistance * 0.02f), BLOCKERS, QueryTriggerInteraction.Ignore))
            {
                _blocked++;
                string lName = lHit.collider.name + (lHit.distance < lDistance * 0.3f ? "(near)" : "(far)");
                int lCount;
                _blockers.TryGetValue(lName, out lCount);
                _blockers[lName] = lCount + 1;
                return;
            }
            // Fog : squared exponential, capped on the track (HS_Flat _HS_TrackFogCap).
            float lFog = RenderSettings.fog ? 1f - Mathf.Exp(-Mathf.Pow(RenderSettings.fogDensity * lDistance, 2f)) : 0f;
            lFog = Mathf.Min(lFog, Shader.GetGlobalFloat("_HS_TrackFogCap") > 0f ? Shader.GetGlobalFloat("_HS_TrackFogCap") : 1f);
            if (lFog > 0.6f)
            {
                _fogged++;
                return;
            }
            _seen++;
        }
    }
}
