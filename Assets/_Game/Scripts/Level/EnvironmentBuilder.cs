using UnityEngine;
using UnityEngine.Rendering;

namespace Com.IsartDigital.Hyperspeed
{
    // Sky, light, fog, and the scenery of a broken world : speed props rushing past the route, floating islands and
    // spires in the distance, wind streaks living in the level (never glued to the camera).
    public static class EnvironmentBuilder
    {
        private const float CHUNK = 220f;

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

        public static void Decorate(Theme pTheme, LevelLayout pLayout, Transform pRoot, int pSeed)
        {
            Transform lDecor = new GameObject("Decor").transform;
            lDecor.SetParent(pRoot, false);
            System.Random lRandom = new System.Random(pSeed);
            Material lMaterial = Mats.Toon(Color.white, pTheme.shade, 0f, 0.3f, true);

            float lEnd = pLayout.spine.EndS;
            for (float lChunk = pLayout.spine.StartS; lChunk < lEnd; lChunk += CHUNK)
            {
                MeshData lData = new MeshData();
                SpeedProps(lData, pTheme, pLayout, lChunk, Mathf.Min(lEnd, lChunk + CHUNK), lRandom);
                DistantScenery(lData, pTheme, pLayout, lChunk, Mathf.Min(lEnd, lChunk + CHUNK), lRandom);
                if (lData.Count == 0) continue;
                GameObject lObject = new GameObject("Scenery" + Mathf.RoundToInt(lChunk));
                lObject.transform.SetParent(lDecor, false);
                lObject.layer = Layers.DEBRIS;
                lObject.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh(lObject.name);
                MeshRenderer lRenderer = lObject.AddComponent<MeshRenderer>();
                lRenderer.sharedMaterial = lMaterial;
                lRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            WorldWind.Create(pTheme, pRoot);
        }

        // Outermost platform extent on one side at s (props never come closer than that + margin).
        private static float Extent(LevelLayout pLayout, float pS, int pSide)
        {
            float lExtent = 12f;
            foreach (Deck lDeck in pLayout.DecksAt(pS))
                lExtent = Mathf.Max(lExtent, lDeck.LateralOffset(pS) * pSide + lDeck.Width(pS) * 0.5f);
            return lExtent;
        }

        private static float Rand(System.Random pRandom, float pMin, float pMax) => pMin + (float)pRandom.NextDouble() * (pMax - pMin);

        // Close props on both sides, every 25-50 m : the eye reads speed from things flashing by.
        private static void SpeedProps(MeshData pData, Theme pTheme, LevelLayout pLayout, float pS0, float pS1, System.Random pRandom)
        {
            for (float lS = pS0 + Rand(pRandom, 5f, 25f); lS < pS1; lS += Rand(pRandom, 25f, 50f))
            {
                int lSide = pRandom.NextDouble() < 0.5 ? -1 : 1;
                float lLateral = lSide * (Extent(pLayout, lS, lSide) + Rand(pRandom, 8f, 40f));
                float lBase = pLayout.FloorY(lS) - Rand(pRandom, 10f, 40f);
                float lRoute = pLayout.spine.Height(lS);
                double lKind = pRandom.NextDouble();
                Color lColor = Color.Lerp(pTheme.decorB, pTheme.decorC, (float)pRandom.NextDouble());
                if (lKind < 0.45)
                {
                    // Spire rising from the abyss, top around route height.
                    float lHeight = lRoute - lBase + Rand(pRandom, -6f, 25f);
                    float lWidth = Rand(pRandom, 3f, 8f);
                    RockSculptor.Place(pData, RockSculptor.Shard(pRandom.Next(0, 6)), pLayout.spine.Point(lS, lLateral, lBase), Quaternion.Euler(Rand(pRandom, -6f, 6f), Rand(pRandom, 0f, 360f), Rand(pRandom, -6f, 6f)),
                        new Vector3(lWidth, lHeight / 2.6f, lWidth), lColor);
                }
                else if (lKind < 0.85)
                {
                    // Floating chunk of broken ground.
                    float lSize = Rand(pRandom, 3f, 10f);
                    float lY = lRoute + Rand(pRandom, -12f, 20f);
                    RockSculptor.Place(pData, RockSculptor.Boulder(pRandom.Next(0, 6)), pLayout.spine.Point(lS, lLateral, lY), Quaternion.Euler(Rand(pRandom, 0f, 360f), Rand(pRandom, 0f, 360f), Rand(pRandom, 0f, 360f)),
                        new Vector3(lSize, lSize * 0.7f, lSize), lColor);
                }
                else
                {
                    // Broken arch leaning over the void.
                    float lHeight = Rand(pRandom, 18f, 34f);
                    Vector3 lFoot = pLayout.spine.Point(lS, lLateral, lRoute - 15f);
                    RockSculptor.Place(pData, RockSculptor.Shard(pRandom.Next(0, 6)), lFoot, Quaternion.Euler(0f, 0f, -lSide * 18f), new Vector3(4f, lHeight / 2.6f, 4f), lColor);
                }
            }
        }

        // Far islands and mountains : depth and scale, the world keeps going in every direction.
        private static void DistantScenery(MeshData pData, Theme pTheme, LevelLayout pLayout, float pS0, float pS1, System.Random pRandom)
        {
            for (float lS = pS0 + Rand(pRandom, 0f, 60f); lS < pS1; lS += Rand(pRandom, 60f, 120f))
            {
                int lSide = pRandom.NextDouble() < 0.5 ? -1 : 1;
                float lLateral = lSide * Rand(pRandom, 160f, 650f);
                float lRoute = pLayout.spine.Height(lS);
                Color lColor = Color.Lerp(pTheme.decorB, pTheme.fog, Mathf.InverseLerp(160f, 650f, Mathf.Abs(lLateral)) * 0.6f);
                if (pRandom.NextDouble() < 0.6)
                {
                    float lSize = Rand(pRandom, 25f, 80f);
                    Vector3 lCenter = pLayout.spine.Point(lS, lLateral, lRoute + Rand(pRandom, -80f, 60f));
                    // Floating island : flat-ish top + long keel.
                    RockSculptor.Place(pData, RockSculptor.Boulder(pRandom.Next(0, 6)), lCenter, Quaternion.Euler(0f, Rand(pRandom, 0f, 360f), 0f), new Vector3(lSize, lSize * 0.35f, lSize), Color.Lerp(pTheme.roadA, lColor, 0.4f));
                    RockSculptor.Place(pData, RockSculptor.Shard(pRandom.Next(0, 6)), lCenter, Quaternion.Euler(180f, Rand(pRandom, 0f, 360f), 0f), new Vector3(lSize * 0.8f, lSize * 0.5f, lSize * 0.8f), lColor);
                }
                else
                {
                    float lHeight = Rand(pRandom, 120f, 320f);
                    RockSculptor.Place(pData, RockSculptor.Shard(pRandom.Next(0, 6)), pLayout.spine.Point(lS, lLateral, lRoute - 200f), Quaternion.Euler(Rand(pRandom, -8f, 8f), Rand(pRandom, 0f, 360f), Rand(pRandom, -8f, 8f)),
                        new Vector3(lHeight * 0.35f, lHeight / 2.6f, lHeight * 0.35f), lColor);
                }
            }
        }
    }

    // Streaks of wind / embers / snow placed in the world ahead of the runner : they stay where they are spawned,
    // so they fly past at your real speed (stretched by the camera motion).
    public class WorldWind : MonoBehaviour
    {
        private ParticleSystem _system;
        private ParticleSystem.EmitParams _params;

        public static WorldWind Create(Theme pTheme, Transform pParent)
        {
            WorldWind lWind = new GameObject("WorldWind").AddComponent<WorldWind>();
            lWind.transform.SetParent(pParent, false);
            Color lColor = pTheme.decor == DecorKind.Industrial ? new Color(2.5f, 1.2f, 0.4f, 0.8f)
                : pTheme.decor == DecorKind.Night ? new Color(1.6f, 0.3f, 0.4f, 0.7f)
                : new Color(1.6f, 1.7f, 1.9f, 0.55f);
            lWind._system = FX.CreateSystem("Wind", Vector3.zero, Mats.Glow(lColor, true, 1f), false);
            lWind._system.transform.SetParent(lWind.transform, false);
            ParticleSystem.MainModule lMain = lWind._system.main;
            lMain.maxParticles = 900;
            lMain.startLifetime = 1.6f;
            lMain.startSpeed = 0f;
            lMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            lMain.gravityModifier = 0f;
            ParticleSystemRenderer lRenderer = lWind._system.GetComponent<ParticleSystemRenderer>();
            lRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            lRenderer.cameraVelocityScale = 0.06f;
            lRenderer.velocityScale = 0f;
            lRenderer.lengthScale = 1f;
            return lWind;
        }

        private void Update()
        {
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null || lPlayer.State == PlayerState.Frozen) return;
            float lSpeed = lPlayer.Speed;
            int lCount = Mathf.RoundToInt(Mathf.Clamp01((lSpeed - 20f) / 120f) * 180f * Time.deltaTime);
            if (Random.value < (lSpeed - 20f) / 120f * 180f * Time.deltaTime - lCount) lCount++;
            Vector3 lForward = Vector3.ProjectOnPlane(lPlayer.Heading, Vector3.up).normalized;
            Vector3 lRight = Vector3.Cross(Vector3.up, lForward);
            for (int i = 0; i < lCount; i++)
            {
                Vector2 lRing = Random.insideUnitCircle.normalized * Random.Range(3f, 22f);
                _params.position = lPlayer.transform.position + lForward * Random.Range(25f, 70f) + lRight * lRing.x + Vector3.up * (Mathf.Abs(lRing.y) * 0.7f + 0.5f);
                _params.velocity = Vector3.zero;
                _system.Emit(_params, 1);
            }
        }
    }
}
