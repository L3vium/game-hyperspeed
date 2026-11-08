using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Air rushing past, in the whole level : long streaks of blue, violet, magenta and white around the camera, thrown back
    // along the movement. There are more of them, and they are longer, the faster you go.
    public class WindField : MonoBehaviour
    {
        private static readonly Color[] COLORS =
        {
            new Color(0.5f, 1f, 2.6f),
            new Color(1.2f, 0.6f, 2.6f),
            new Color(2.6f, 0.6f, 1.9f),
            new Color(2.4f, 2.4f, 2.6f)
        };

        private ParticleSystem _system;
        private float _budget;
        private Transform _ambient;

        private void Awake()
        {
            _system = FX.CreateSystem("WindStreaks", Vector3.zero, Mats.Glow(Color.white, true, 1f), false);
            _system.transform.SetParent(transform, false);
            ParticleSystem.MainModule lMain = _system.main;
            lMain.maxParticles = 700;
            ParticleSystemRenderer lRenderer = _system.GetComponent<ParticleSystemRenderer>();
            lRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            lRenderer.velocityScale = 0.11f;
            lRenderer.lengthScale = 2f;
            lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ParticleSystem.ColorOverLifetimeModule lFade = _system.colorOverLifetime;
            lFade.enabled = true;
            Gradient lGradient = new Gradient();
            lGradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                              new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.2f), new GradientAlphaKey(0f, 1f) });
            lFade.color = lGradient;
        }

        // Fireflies in the green places, embers near fire, drifting lines in the cold and the void, wind everywhere else.
        private void Start()
        {
            Theme lTheme = RaceContext.Theme;
            GameObject[] lPrefabs = GameAssets.Instance.ambientParticles;
            if (lTheme == null || lPrefabs == null) return;
            string lWanted = "Wind";
            switch (lTheme.decor)
            {
                case DecorKind.Forest:
                case DecorKind.Prehistoric:
                case DecorKind.Night: lWanted = "FireFlies"; break;
                case DecorKind.Industrial:
                case DecorKind.Volcano:
                case DecorKind.Canyon: lWanted = "Embers"; break;
                case DecorKind.Ice:
                case DecorKind.Space:
                case DecorKind.Void:
                case DecorKind.Neon: lWanted = "Etherial"; break;
            }
            foreach (GameObject lPrefab in lPrefabs)
            {
                if (lPrefab == null || !lPrefab.name.Contains(lWanted)) continue;
                _ambient = Instantiate(lPrefab, transform).transform;
                break;
            }
        }

        private void Update()
        {
            PlayerController lPlayer = RaceContext.Player;
            CameraRig lRig = CameraRig.Instance;
            if (lPlayer == null || lRig == null || Time.deltaTime <= 0f) return;
            if (_ambient != null) _ambient.position = lRig.transform.position + lRig.transform.forward * 40f;
            float lSpeed = lPlayer.TotalSpeed;
            if (lSpeed < 12f) return;
            float lLevel = Mathf.Clamp01(lPlayer.Speed01 * 1.2f + 0.12f);
            _budget += Mathf.Lerp(30f, 520f, lLevel) * Time.deltaTime;
            int lCount = Mathf.FloorToInt(_budget);
            if (lCount <= 0) return;
            _budget -= lCount;

            Transform lCamera = lRig.transform;
            Vector3 lVelocity = lPlayer.OnGroundVelocity;
            Vector3 lDirection = lVelocity.sqrMagnitude > 1f ? lVelocity.normalized : lCamera.forward;
            Vector3 lSide = Vector3.Cross(lDirection, Vector3.up).sqrMagnitude > 0.01f ? Vector3.Cross(lDirection, Vector3.up).normalized : lCamera.right;
            Vector3 lOver = Vector3.Cross(lSide, lDirection).normalized;
            ParticleSystem.EmitParams lParams = new ParticleSystem.EmitParams();
            for (int i = 0; i < lCount; i++)
            {
                // A tube around the line of travel, with the middle left open.
                Vector2 lRing = Random.insideUnitCircle.normalized * Random.Range(5f, 48f);
                float lAhead = Random.Range(0.1f, 0.9f) * Mathf.Max(60f, lSpeed * 0.45f);
                lParams.position = lCamera.position + lDirection * lAhead + lSide * lRing.x + lOver * lRing.y;
                lParams.velocity = -lDirection * lSpeed * Random.Range(0.25f, 0.6f);
                lParams.startColor = COLORS[Random.Range(0, COLORS.Length)];
                lParams.startSize = Random.Range(0.05f, 0.16f) * (1f + lLevel * 1.5f);
                lParams.startLifetime = Random.Range(0.25f, 0.55f);
                _system.Emit(lParams, 1);
            }
        }
    }
}
