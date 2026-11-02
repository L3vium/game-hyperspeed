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

        // Scenery is rebuilt from scratch with the new platform levels (organic decks, crumbling world).
        public static void Decorate(Theme pTheme, TrackPath pPath, Transform pRoot, int pSeed)
        {
        }

        // Theme ambience (Sherbbs particles) carried by the camera.
        public static void AttachAmbient(Theme pTheme, Transform pCamera)
        {
            GameObject[] lAmbient = GameAssets.Instance.ambientParticles;
            if (lAmbient == null || lAmbient.Length < 4) return;
            GameObject lPrefab;
            switch (pTheme.decor)
            {
                case DecorKind.Night: lPrefab = lAmbient[0]; break;
                case DecorKind.Space:
                case DecorKind.Void: lPrefab = lAmbient[1]; break;
                case DecorKind.Industrial: lPrefab = lAmbient[3]; break;
                default: lPrefab = lAmbient[2]; break;
            }
            if (lPrefab == null) return;
            GameObject lInstance = Object.Instantiate(lPrefab, pCamera);
            lInstance.transform.localPosition = new Vector3(0f, 0f, 25f);
            lInstance.transform.localRotation = Quaternion.identity;
            // Pack uses built-in pipeline particle shaders : swap for our URP glow.
            foreach (ParticleSystemRenderer lRenderer in lInstance.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                lRenderer.sharedMaterial = Mats.Glow(new Color(1f, 1f, 1f, 0.7f), true, 1f);
                lRenderer.trailMaterial = Mats.Glow(new Color(1f, 1f, 1f, 0.5f), true);
            }
        }
    }
}
