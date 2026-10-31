using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Code-built particle bursts (shards, sparks, dust, shockwaves).
    public static class FX
    {
        private static Mesh _cubeMesh;
        private static Material _shardMaterial;

        private static Mesh CubeMesh
        {
            get
            {
                if (_cubeMesh == null)
                {
                    GameObject lCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _cubeMesh = lCube.GetComponent<MeshFilter>().sharedMesh;
                    Object.DestroyImmediate(lCube);
                }
                return _cubeMesh;
            }
        }

        private static Material ShardMaterial
        {
            get
            {
                if (_shardMaterial == null)
                {
                    _shardMaterial = new Material(Mats.Toon(Color.white, null, 0f, 0.6f, true));
                }
                return _shardMaterial;
            }
        }

        public static ParticleSystem CreateSystem(string pName, Vector3 pPosition, Material pMaterial, bool pMesh)
        {
            GameObject lObject = new GameObject(pName);
            lObject.transform.position = pPosition;
            ParticleSystem lSystem = lObject.AddComponent<ParticleSystem>();
            lSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule lMain = lSystem.main;
            lMain.playOnAwake = false;
            lMain.loop = false;
            lMain.simulationSpace = ParticleSystemSimulationSpace.World;
            lMain.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ParticleSystem.EmissionModule lEmission = lSystem.emission;
            lEmission.rateOverTime = 0f;
            ParticleSystemRenderer lRenderer = lObject.GetComponent<ParticleSystemRenderer>();
            lRenderer.sharedMaterial = pMaterial;
            if (pMesh)
            {
                lRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                lRenderer.mesh = CubeMesh;
            }
            return lSystem;
        }

        public static void Shards(Vector3 pPosition, Color pColor, int pCount, float pSpeed, float pSize, Vector3 pDirection)
        {
            ParticleSystem lSystem = CreateSystem("Shards", pPosition, ShardMaterial, true);
            ParticleSystem.MainModule lMain = lSystem.main;
            lMain.duration = 0.1f;
            lMain.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            lMain.startSpeed = new ParticleSystem.MinMaxCurve(pSpeed * 0.4f, pSpeed);
            lMain.startSize3D = true;
            lMain.startSizeX = new ParticleSystem.MinMaxCurve(pSize * 0.3f, pSize);
            lMain.startSizeY = new ParticleSystem.MinMaxCurve(pSize * 0.3f, pSize);
            lMain.startSizeZ = new ParticleSystem.MinMaxCurve(pSize * 0.3f, pSize);
            lMain.startRotation3D = true;
            lMain.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            lMain.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            lMain.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            lMain.startColor = new ParticleSystem.MinMaxGradient(pColor, Color.Lerp(pColor, Color.white, 0.5f));
            lMain.gravityModifier = 2.5f;
            lMain.maxParticles = 200;

            ParticleSystem.ShapeModule lShape = lSystem.shape;
            lShape.shapeType = ParticleSystemShapeType.Sphere;
            lShape.radius = 1f;

            ParticleSystem.RotationOverLifetimeModule lRotation = lSystem.rotationOverLifetime;
            lRotation.enabled = true;
            lRotation.separateAxes = true;
            lRotation.x = new ParticleSystem.MinMaxCurve(-8f, 8f);
            lRotation.y = new ParticleSystem.MinMaxCurve(-8f, 8f);

            ParticleSystem.SizeOverLifetimeModule lSize = lSystem.sizeOverLifetime;
            lSize.enabled = true;
            lSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            if (pDirection != Vector3.zero)
            {
                ParticleSystem.VelocityOverLifetimeModule lVelocity = lSystem.velocityOverLifetime;
                lVelocity.enabled = true;
                lVelocity.space = ParticleSystemSimulationSpace.World;
                lVelocity.x = new ParticleSystem.MinMaxCurve(pDirection.x);
                lVelocity.y = new ParticleSystem.MinMaxCurve(pDirection.y);
                lVelocity.z = new ParticleSystem.MinMaxCurve(pDirection.z);
            }

            lSystem.Emit(pCount);
            Object.Destroy(lSystem.gameObject, 2f);
        }

        public static void Sparks(Vector3 pPosition, Color pColor, int pCount, float pSpeed, float pSize = 0.35f, float pLifetime = 0.35f)
        {
            ParticleSystem lSystem = CreateSystem("Sparks", pPosition, Mats.Glow(pColor * 3f, true, 1f), false);
            ParticleSystem.MainModule lMain = lSystem.main;
            lMain.startLifetime = new ParticleSystem.MinMaxCurve(pLifetime * 0.5f, pLifetime);
            lMain.startSpeed = new ParticleSystem.MinMaxCurve(pSpeed * 0.3f, pSpeed);
            lMain.startSize = new ParticleSystem.MinMaxCurve(pSize * 0.5f, pSize);
            lMain.gravityModifier = 1f;
            ParticleSystem.ShapeModule lShape = lSystem.shape;
            lShape.shapeType = ParticleSystemShapeType.Sphere;
            lShape.radius = 0.3f;
            ParticleSystemRenderer lRenderer = lSystem.GetComponent<ParticleSystemRenderer>();
            lRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            lRenderer.velocityScale = 0.06f;
            lRenderer.lengthScale = 1f;
            ParticleSystem.SizeOverLifetimeModule lSize = lSystem.sizeOverLifetime;
            lSize.enabled = true;
            lSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            lSystem.Emit(pCount);
            Object.Destroy(lSystem.gameObject, pLifetime + 0.5f);
        }

        public static void Puff(Vector3 pPosition, Color pColor, int pCount, float pSize, float pSpeed = 4f)
        {
            Color lColor = pColor;
            lColor.a = 0.6f;
            ParticleSystem lSystem = CreateSystem("Puff", pPosition, Mats.Glow(Color.white, false, 1f), false);
            ParticleSystem.MainModule lMain = lSystem.main;
            lMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            lMain.startSpeed = new ParticleSystem.MinMaxCurve(pSpeed * 0.3f, pSpeed);
            lMain.startSize = new ParticleSystem.MinMaxCurve(pSize * 0.6f, pSize);
            lMain.startColor = lColor;
            ParticleSystem.ShapeModule lShape = lSystem.shape;
            lShape.shapeType = ParticleSystemShapeType.Sphere;
            lShape.radius = 0.5f;
            ParticleSystem.SizeOverLifetimeModule lSize = lSystem.sizeOverLifetime;
            lSize.enabled = true;
            lSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.4f));
            ParticleSystem.ColorOverLifetimeModule lFade = lSystem.colorOverLifetime;
            lFade.enabled = true;
            Gradient lGradient = new Gradient();
            lGradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                              new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            lFade.color = lGradient;
            lSystem.Emit(pCount);
            Object.Destroy(lSystem.gameObject, 1f);
        }

        // Expanding glowing ring (landing, shockwaves, explosions).
        public static void Shockwave(Vector3 pPosition, Vector3 pNormal, Color pColor, float pSize, float pDuration = 0.35f)
        {
            Color lColor = pColor * 1.6f;
            lColor.a = 0.35f;
            GameObject lRing = Mats.Primitive(PrimitiveType.Cylinder, null, pPosition, Vector3.one, Mats.Glow(lColor, true, 1f, 0f));
            lRing.transform.position = pPosition;
            lRing.transform.up = pNormal;
            lRing.AddComponent<ShockwaveAnim>().Init(pSize, pDuration);
        }

        public static void Explosion(Vector3 pPosition, Color pColor, float pScale)
        {
            Sparks(pPosition, pColor, 40, 30f * pScale, 0.8f * pScale, 0.6f);
            Sparks(pPosition, Color.white, 20, 20f * pScale, 0.5f * pScale, 0.4f);
            Puff(pPosition, new Color(0.3f, 0.25f, 0.3f), 14, 6f * pScale, 10f * pScale);
            Shockwave(pPosition, Vector3.up, pColor, 16f * pScale, 0.45f);
        }
    }

    public class ShockwaveAnim : MonoBehaviour
    {
        private float _size;
        private float _duration;
        private float _time;

        public void Init(float pSize, float pDuration)
        {
            _size = pSize;
            _duration = pDuration;
            transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
        }

        private void Update()
        {
            _time += Time.deltaTime;
            float lT = _time / _duration;
            float lEase = 1f - Mathf.Pow(1f - Mathf.Clamp01(lT), 3f);
            float lSize = _size * lEase;
            transform.localScale = new Vector3(lSize, 0.03f * (1f - lT) + 0.005f, lSize);
            if (lT >= 1f) Destroy(gameObject);
        }
    }
}
