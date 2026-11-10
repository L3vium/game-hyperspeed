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
        private ParticleSystem _motes;
        private float _budget;
        private float _moteBudget;
        private Color _moteColor = Color.white;

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

        // Dust, pollen, ash or snow hanging in the air of the place : few, soft, still in the world, so they rush past
        // the camera instead of following it.
        private void Start()
        {
            Theme lTheme = RaceContext.Theme;
            if (lTheme == null) return;
            _moteColor = Color.Lerp(lTheme.fog, Color.white, 0.55f);
            switch (lTheme.decor)
            {
                case DecorKind.Industrial:
                case DecorKind.Volcano:
                case DecorKind.Canyon: _moteColor = new Color(1.6f, 0.7f, 0.25f); break;
                case DecorKind.Ice: _moteColor = new Color(1.3f, 1.4f, 1.6f); break;
            }
            _moteColor.a = 0.55f;
            _motes = FX.CreateSystem("Motes", Vector3.zero, Mats.Glow(Color.white, false, 1f), false);
            _motes.transform.SetParent(transform, false);
            ParticleSystem.MainModule lMain = _motes.main;
            lMain.maxParticles = 900;
            lMain.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystemRenderer lRenderer = _motes.GetComponent<ParticleSystemRenderer>();
            lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ParticleSystem.ColorOverLifetimeModule lFade = _motes.colorOverLifetime;
            lFade.enabled = true;
            Gradient lGradient = new Gradient();
            lGradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                              new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            lFade.color = lGradient;
        }

        private void Update()
        {
            PlayerController lPlayer = RaceContext.Player;
            CameraRig lRig = CameraRig.Instance;
            if (lPlayer == null || lRig == null || Time.deltaTime <= 0f) return;
            float lSpeed = lPlayer.TotalSpeed;
            EmitMotes(lRig.transform, lPlayer, lSpeed);
            if (lSpeed < 12f) return;
            float lLevel = Mathf.Clamp01(lPlayer.Speed01 * 1.2f + 0.12f);
            _budget += Mathf.Lerp(24f, 400f, lLevel) * Time.deltaTime;
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
                Vector2 lRing = Random.insideUnitCircle.normalized * Random.Range(12f, 56f);
                float lAhead = Random.Range(0.1f, 0.9f) * Mathf.Max(60f, lSpeed * 0.45f);
                lParams.position = lCamera.position + lDirection * lAhead + lSide * lRing.x + lOver * lRing.y;
                lParams.velocity = -lDirection * lSpeed * Random.Range(0.25f, 0.6f);
                lParams.startColor = COLORS[Random.Range(0, COLORS.Length)];
                lParams.startSize = Random.Range(0.05f, 0.16f) * (1f + lLevel * 1.5f);
                lParams.startLifetime = Random.Range(0.25f, 0.55f);
                _system.Emit(lParams, 1);
            }
        }

        // Spread ahead of the camera, where the runner is going, so each one is seen coming and flashing by.
        private void EmitMotes(Transform pCamera, PlayerController pPlayer, float pSpeed)
        {
            if (_motes == null) return;
            _moteBudget += 60f * Time.deltaTime;
            int lCount = Mathf.FloorToInt(_moteBudget);
            if (lCount <= 0) return;
            _moteBudget -= lCount;
            Vector3 lVelocity = pPlayer.OnGroundVelocity;
            Vector3 lDirection = lVelocity.sqrMagnitude > 1f ? lVelocity.normalized : pCamera.forward;
            float lReach = Mathf.Max(120f, pSpeed * 2.5f);
            ParticleSystem.EmitParams lParams = new ParticleSystem.EmitParams();
            for (int i = 0; i < lCount; i++)
            {
                lParams.position = pCamera.position + lDirection * Random.Range(30f, lReach) + Random.insideUnitSphere * 70f;
                lParams.velocity = Random.insideUnitSphere * 2f + Vector3.down * 0.6f;
                lParams.startColor = _moteColor;
                lParams.startSize = Random.Range(0.25f, 0.7f);
                lParams.startLifetime = Random.Range(2.5f, 4f);
                _motes.Emit(lParams, 1);
            }
        }
    }
}
