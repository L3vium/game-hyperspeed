using UnityEngine;
using UnityEngine.Rendering;

namespace Com.IsartDigital.Hyperspeed
{
    // Sky, light, fog and scenery props scattered around the track path.
    public static class EnvironmentBuilder
    {
        public static Light Build(Theme pTheme, Transform pRoot)
        {
            Material lSky = new Material(Shader.Find("Hyperspeed/Sky"));
            lSky.SetColor("_TopColor", pTheme.skyTop);
            lSky.SetColor("_HorizonColor", pTheme.skyHorizon);
            lSky.SetColor("_BottomColor", pTheme.skyBottom);
            lSky.SetColor("_CloudColor", pTheme.cloud);
            lSky.SetColor("_SunColor", pTheme.sun);
            lSky.SetVector("_SunDir", pTheme.sunDirection.normalized);
            lSky.SetFloat("_CloudAmount", pTheme.clouds);
            lSky.SetFloat("_Stars", pTheme.stars);
            lSky.SetFloat("_Rays", pTheme.rays);
            RenderSettings.skybox = lSky;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = pTheme.fog;
            RenderSettings.fogDensity = pTheme.fogDensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = pTheme.ambient;

            GameObject lLightObject = new GameObject("Sun");
            lLightObject.transform.SetParent(pRoot, false);
            Light lLight = lLightObject.AddComponent<Light>();
            lLight.type = LightType.Directional;
            lLight.color = pTheme.light;
            lLight.intensity = pTheme.lightIntensity;
            lLight.shadows = LightShadows.Soft;
            lLight.shadowStrength = 0.75f;
            lLightObject.transform.rotation = Quaternion.LookRotation(-pTheme.sunDirection.normalized + Vector3.down * 0.9f);
            RenderSettings.sun = lLight;
            return lLight;
        }

        public static void Decorate(Theme pTheme, TrackPath pPath, Transform pRoot, int pSeed)
        {
            Random.State lState = Random.state;
            Random.InitState(pSeed);
            Transform lDecorRoot = new GameObject("Decor").transform;
            lDecorRoot.SetParent(pRoot, false);

            float lLength = pPath.Length;
            for (float lD = 0f; lD < lLength; lD += Random.Range(14f, 30f))
            {
                TrackSample lSample = pPath.Evaluate(lD);
                if (Vector3.Dot(lSample.Up, Vector3.up) < 0.7f) continue;
                int lSide = Random.value < 0.5f ? -1 : 1;
                float lLateral = lSide * Random.Range(lSample.width * 0.5f + 22f, 160f);
                Vector3 lFlatForward = Vector3.ProjectOnPlane(lSample.Forward, Vector3.up).normalized;
                Vector3 lFlatRight = Vector3.Cross(Vector3.up, lFlatForward);
                Vector3 lPosition = lSample.position + lFlatRight * lLateral + Vector3.up * Random.Range(-35f, 8f);
                if (TooClose(pPath, lPosition, 26f)) continue;
                float lScale = Random.Range(0.8f, 1.6f) * Mathf.Lerp(1f, 2.2f, Mathf.Abs(lLateral) / 160f);
                PropFactory.Decor(pTheme.decor, pTheme, lDecorRoot, lPosition, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), lScale);
            }

            // Giant far silhouettes for depth.
            Bounds lBounds = new Bounds(pPath.samples[0].position, Vector3.zero);
            foreach (TrackSample lSample in pPath.samples) lBounds.Encapsulate(lSample.position);
            for (int i = 0; i < 40; i++)
            {
                float lAngle = Random.Range(0f, Mathf.PI * 2f);
                float lRadius = Mathf.Max(lBounds.extents.x, lBounds.extents.z) + Random.Range(250f, 700f);
                Vector3 lPosition = lBounds.center + new Vector3(Mathf.Cos(lAngle), 0f, Mathf.Sin(lAngle)) * lRadius + Vector3.up * Random.Range(-120f, 20f);
                PropFactory.Decor(pTheme.decor, pTheme, lDecorRoot, lPosition, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Random.Range(5f, 11f));
            }
            Random.state = lState;
        }

        private static bool TooClose(TrackPath pPath, Vector3 pPosition, float pDistance)
        {
            float lSqr = pDistance * pDistance;
            for (int i = 0; i < pPath.samples.Count; i += 5)
                if ((pPath.samples[i].position - pPosition).sqrMagnitude < lSqr) return true;
            return false;
        }
    }
}
