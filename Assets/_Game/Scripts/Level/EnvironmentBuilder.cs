using UnityEngine;
using UnityEngine.Rendering;

namespace Com.IsartDigital.Hyperspeed
{
    // Sky, light and fog of a stage.
    public static class EnvironmentBuilder
    {

        public static Light Build(Theme pTheme, Transform pRoot, SpeedProfile pProfile = null)
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
            // Enormous views : the fog opens with the world scale.
            float lWorld = 1f;
            RenderSettings.fogDensity = pTheme.fogDensity * 0.24f / lWorld;
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
    }
}
