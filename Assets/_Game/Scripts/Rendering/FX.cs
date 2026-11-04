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

        // Flames dragged behind a falling meteor.
        public static ParticleSystem FireTrail(Transform pParent, float pRadius)
        {
            ParticleSystem lSystem = CreateSystem("FireTrail", pParent.position, Mats.Glow(new Color(3.4f, 1.3f, 0.2f), true, 1f), false);
            lSystem.transform.SetParent(pParent, true);
            ParticleSystem.MainModule lMain = lSystem.main;
            lMain.loop = true;
            lMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            lMain.startSpeed = 0f;
            lMain.startSize = new ParticleSystem.MinMaxCurve(pRadius * 0.8f, pRadius * 1.5f);
            lMain.maxParticles = 200;
            ParticleSystem.EmissionModule lEmission = lSystem.emission;
            lEmission.rateOverTime = 70f;
            ParticleSystem.ShapeModule lShape = lSystem.shape;
            lShape.shapeType = ParticleSystemShapeType.Sphere;
            lShape.radius = pRadius * 0.7f;
            ParticleSystem.SizeOverLifetimeModule lSize = lSystem.sizeOverLifetime;
            lSize.enabled = true;
            lSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            lSystem.Play();
            return lSystem;
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

        // Persistent stretched-spark system fed with Emit() (drift sparks, wind spikes).
        public static ParticleSystem CreateSpikeSystem(string pName, int pMax)
        {
            ParticleSystem lSystem = CreateSystem(pName, Vector3.zero, Mats.Glow(Color.white * 2.5f, true, 1f), false);
            ParticleSystem.MainModule lMain = lSystem.main;
            lMain.maxParticles = pMax;
            lMain.startLifetime = 0.25f;
            lMain.gravityModifier = 0.6f;
            ParticleSystemRenderer lRenderer = lSystem.GetComponent<ParticleSystemRenderer>();
            lRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            lRenderer.velocityScale = 0.045f;
            lRenderer.lengthScale = 2.5f;
            ParticleSystem.SizeOverLifetimeModule lSize = lSystem.sizeOverLifetime;
            lSize.enabled = true;
            lSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            return lSystem;
        }

        // Ring flash + radial spikes : boost pads, rings, big events.
        public static void Burst(Vector3 pPosition, Vector3 pDirection, Color pColor, float pSize)
        {
            GameObject lRing = new GameObject("BurstRing");
            lRing.transform.position = pPosition;
            lRing.transform.rotation = Quaternion.LookRotation(pDirection.sqrMagnitude > 0.01f ? pDirection : Vector3.forward);
            lRing.AddComponent<MeshFilter>().sharedMesh = RingMesh;
            Color lColor = pColor * 2.5f;
            lColor.a = 0.9f;
            lRing.AddComponent<MeshRenderer>().sharedMaterial = new Material(Mats.Glow(lColor, true));
            lRing.AddComponent<BurstAnim>().Init(pSize, 0.35f);

            ParticleSystem lSystem = CreateSpikeSystem("BurstSpikes", 80);
            lSystem.transform.position = pPosition;
            ParticleSystem.MainModule lMain = lSystem.main;
            lMain.startSpeed = new ParticleSystem.MinMaxCurve(pSize * 1.5f, pSize * 3.5f);
            lMain.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            lMain.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            lMain.startColor = new ParticleSystem.MinMaxGradient(pColor * 2.5f, Color.white * 2f);
            ParticleSystem.ShapeModule lShape = lSystem.shape;
            lShape.shapeType = ParticleSystemShapeType.Sphere;
            lShape.radius = 0.5f;
            lSystem.Emit(60);
            Object.Destroy(lSystem.gameObject, 1f);
        }

        private static Mesh _ringMesh;
        private static Mesh RingMesh => _ringMesh != null ? _ringMesh : (_ringMesh = MeshUtil.Ring(0.85f, 1f, 48));

        // Glowing head with a long trail moving in a straight line (sky meteors).
        public static void Streak(Vector3 pPosition, Vector3 pDirection, float pSpeed, float pDuration, Color pColor)
        {
            GameObject lHead = new GameObject("Streak");
            lHead.transform.position = pPosition;
            TrailRenderer lTrail = lHead.AddComponent<TrailRenderer>();
            lTrail.time = 0.9f;
            lTrail.startWidth = 6f;
            lTrail.endWidth = 0f;
            lTrail.minVertexDistance = 4f;
            lTrail.sharedMaterial = Mats.Glow(pColor, true);
            lTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lHead.AddComponent<StreakAnim>().Init(pDirection * pSpeed, pDuration);
        }

        public static void Explosion(Vector3 pPosition, Color pColor, float pScale)
        {
            Sparks(pPosition, pColor, 40, 30f * pScale, 0.8f * pScale, 0.6f);
            Sparks(pPosition, Color.white, 20, 20f * pScale, 0.5f * pScale, 0.4f);
            Puff(pPosition, new Color(0.3f, 0.25f, 0.3f), 14, 6f * pScale, 10f * pScale);
            Shockwave(pPosition, Vector3.up, pColor, 16f * pScale, 0.45f);
        }
    }

    public class BurstAnim : MonoBehaviour
    {
        private float _size;
        private float _duration;
        private float _time;
        private Material _material;
        private Color _color;

        public void Init(float pSize, float pDuration)
        {
            _size = pSize;
            _duration = pDuration;
            _material = GetComponent<MeshRenderer>().sharedMaterial;
            _color = _material.GetColor(Mats.COLOR);
            transform.localScale = Vector3.one * 0.2f;
        }

        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            float lT = Mathf.Clamp01(_time / _duration);
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, _size, 1f - Mathf.Pow(1f - lT, 3f));
            Color lColor = _color;
            lColor.a = _color.a * (1f - lT);
            _material.SetColor(Mats.COLOR, lColor);
            if (lT >= 1f)
            {
                Destroy(_material);
                Destroy(gameObject);
            }
        }
    }

    public class StreakAnim : MonoBehaviour
    {
        private Vector3 _velocity;
        private float _life;

        public void Init(Vector3 pVelocity, float pLife)
        {
            _velocity = pVelocity;
            _life = pLife;
        }

        private void Update()
        {
            if (_life > 0f) transform.position += _velocity * Time.deltaTime;
            _life -= Time.deltaTime;
            if (_life <= -1f) Destroy(gameObject);
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
