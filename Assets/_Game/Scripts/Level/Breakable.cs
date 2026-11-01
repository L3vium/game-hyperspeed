using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum BreakableKind { Ice, Crate, Rock, Car }

    // Obstacle the player smashes (punch, dash, hyper speed) or crashes into.
    public class Breakable : MonoBehaviour
    {
        public static readonly List<Breakable> All = new List<Breakable>();

        public BreakableKind kind;
        public float radius = 1.5f;
        public float height = 3f;
        public Color color = Color.white;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private float _flash;
        private bool _launched;
        private Vector3 _launchVelocity;
        private Vector3 _spin;

        public bool IsBroken { get; private set; }

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
        }

        public void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>();
        }

        // Sphere vs vertical capsule-ish test in the obstacle's local up.
        public bool Overlaps(Vector3 pPosition, float pRadius)
        {
            if (IsBroken) return false;
            Vector3 lLocal = pPosition - transform.position;
            float lUp = Vector3.Dot(lLocal, transform.up);
            if (lUp < -1.5f || lUp > height + 1f) return false;
            Vector3 lFlat = lLocal - transform.up * lUp;
            return lFlat.magnitude < radius + pRadius;
        }

        public void Smash(Vector3 pDirection, float pPlayerSpeed)
        {
            if (IsBroken) return;
            IsBroken = true;
            All.Remove(this);
            Vector3 lCenter = transform.position + transform.up * height * 0.5f;
            Vector3 lPush = pDirection * Mathf.Min(pPlayerSpeed, 60f) * 0.5f;

            switch (kind)
            {
                case BreakableKind.Ice:
                    FX.Shards(lCenter, color, 22, 18f, 0.8f, lPush);
                    FX.Sparks(lCenter, new Color(0.6f, 1f, 1f), 18, 25f);
                    AudioManager.Play("sfx_shatter", 0.75f, Random.Range(0.9f, 1.2f));
                    break;
                case BreakableKind.Crate:
                    FX.Shards(lCenter, color, 16, 16f, 0.9f, lPush);
                    FX.Puff(lCenter, new Color(0.9f, 0.8f, 0.6f), 8, 3f);
                    AudioManager.Play("sfx_crate", 0.8f);
                    break;
                case BreakableKind.Rock:
                    FX.Shards(lCenter, color, 20, 20f, 1.2f, lPush);
                    FX.Puff(lCenter, new Color(0.6f, 0.5f, 0.5f), 10, 4f);
                    AudioManager.Play("sfx_rock", 0.85f);
                    break;
                case BreakableKind.Car:
                    _launched = true;
                    _launchVelocity = pDirection * Mathf.Max(30f, pPlayerSpeed * 0.9f) + Vector3.up * 28f;
                    _spin = new Vector3(Random.Range(-720f, 720f), Random.Range(-360f, 360f), Random.Range(-720f, 720f));
                    FX.Sparks(lCenter, new Color(1f, 0.8f, 0.3f), 30, 30f);
                    AudioManager.Play("sfx_carhit", 0.9f);
                    Flash();
                    Destroy(gameObject, 2.5f);
                    return;
            }
            Destroy(gameObject);
        }

        public void Flash()
        {
            _flash = 1f;
        }

        private void Update()
        {
            if (_launched)
            {
                _launchVelocity += Vector3.down * 40f * Time.deltaTime;
                transform.position += _launchVelocity * Time.deltaTime;
                transform.Rotate(_spin * Time.deltaTime, Space.World);
            }

            if (_flash > 0f)
            {
                _flash = Mathf.MoveTowards(_flash, 0f, Time.deltaTime * 4f);
                if (_renderers == null) CacheRenderers();
                _block.SetFloat(Mats.FLASH, _flash > 0.5f ? 1f : 0f);
                foreach (Renderer lRenderer in _renderers)
                    if (lRenderer != null) lRenderer.SetPropertyBlock(_block);
            }
        }
    }
}
