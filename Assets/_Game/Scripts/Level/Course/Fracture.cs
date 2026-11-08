using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Big smash : the rock bursts into huge chunks flying everywhere, lava blobs, rings and a flash. Chunks are pooled :
    // the world breaks all the time now.
    public static class Fracture
    {
        private static readonly Stack<ChunkAnim> _rocks = new Stack<ChunkAnim>();
        private static readonly Stack<ChunkAnim> _blobs = new Stack<ChunkAnim>();

        private static ChunkAnim Take(Stack<ChunkAnim> pPool, bool pBlob, int pIndex)
        {
            while (pPool.Count > 0)
            {
                ChunkAnim lReady = pPool.Pop();
                if (lReady != null)
                {
                    lReady.gameObject.SetActive(true);
                    return lReady;
                }
            }
            GameObject lObject;
            if (pBlob) lObject = Mats.Primitive(PrimitiveType.Sphere, null, Vector3.zero, Vector3.one, null);
            else
            {
                lObject = new GameObject("Chunk");
                lObject.AddComponent<MeshFilter>().sharedMesh = RockSculptor.Boulder(pIndex % 6);
                lObject.AddComponent<MeshRenderer>();
            }
            ChunkAnim lAnim = lObject.AddComponent<ChunkAnim>();
            lAnim.pool = pPool;
            return lAnim;
        }

        public static void Explode(Vector3 pCenter, float pRadius, Color pColor, Vector3 pDirection, float pSpeed, float pAmount = 1f)
        {
            Theme lTheme = RaceContext.Theme;
            Material lMaterial = Mats.Flat(lTheme != null ? lTheme.shade : new Color(0.6f, 0.55f, 0.8f), false, pColor);
            Material lLava = Mats.Glow(new Color(3.6f, 1.2f, 0.12f), false, 0f, 0f, true);
            Vector3 lDrive = pDirection.normalized * Mathf.Min(pSpeed, 400f) * 0.75f;
            int lRocks = Mathf.Max(4, Mathf.RoundToInt(16 * pAmount));
            for (int i = 0; i < lRocks; i++)
            {
                Vector3 lOut = Random.onUnitSphere;
                float lSize = pRadius * Random.Range(0.18f, 0.5f);
                ChunkAnim lAnim = Take(_rocks, false, i);
                lAnim.transform.SetPositionAndRotation(pCenter + lOut * pRadius * 0.5f, Random.rotation);
                lAnim.GetComponent<MeshRenderer>().sharedMaterial = lMaterial;
                lAnim.Launch(Vector3.one * lSize, lOut * Random.Range(40f, 130f) * Mathf.Sqrt(pRadius / 16f) + lDrive + Vector3.up * Random.Range(10f, 60f), Random.onUnitSphere * Random.Range(180f, 720f), Random.Range(1.6f, 2.8f), false);
            }
            int lBlobs = Mathf.Max(3, Mathf.RoundToInt(12 * pAmount));
            for (int i = 0; i < lBlobs; i++)
            {
                Vector3 lOut = Random.onUnitSphere;
                ChunkAnim lAnim = Take(_blobs, true, i);
                lAnim.transform.position = pCenter;
                lAnim.GetComponent<MeshRenderer>().sharedMaterial = lLava;
                lAnim.Launch(Vector3.one * pRadius * Random.Range(0.05f, 0.14f), lOut * Random.Range(60f, 190f) + lDrive * 0.8f, Vector3.zero, Random.Range(0.8f, 1.6f), true);
            }
            FX.Shockwave(pCenter, -pDirection, new Color(1f, 0.6f, 0.15f), pRadius * 4.5f, 0.45f);
            FX.Shockwave(pCenter, Vector3.up, Color.white, pRadius * 3f, 0.3f);
            FX.Sparks(pCenter, new Color(1f, 0.7f, 0.2f), 60, 80f, 1.4f, 0.7f);
            FX.Puff(pCenter, new Color(0.45f, 0.35f, 0.35f), 12, pRadius * 0.5f, 30f);
            AudioManager.Play("sfx_rock", 1f, Random.Range(0.8f, 1f));
            AudioManager.Play("sfx_shatter", 0.8f, 0.8f);
        }
    }

    public class ChunkAnim : MonoBehaviour
    {
        public Vector3 velocity;
        public Vector3 spin;
        public float life = 2f;
        public bool shrink;
        [System.NonSerialized] public Stack<ChunkAnim> pool;

        private float _age;
        private Vector3 _scale;
        private bool _launched;

        public void Launch(Vector3 pScale, Vector3 pVelocity, Vector3 pSpin, float pLife, bool pShrink)
        {
            _launched = true;
            _scale = pScale;
            transform.localScale = pScale;
            velocity = pVelocity;
            spin = pSpin;
            life = pLife;
            shrink = pShrink;
            _age = 0f;
        }

        private void Start()
        {
            if (!_launched) _scale = transform.localScale;
        }

        private void Update()
        {
            float lDt = Time.deltaTime;
            _age += lDt;
            velocity += Vector3.down * 70f * lDt;
            transform.position += velocity * lDt;
            transform.Rotate(spin * lDt, Space.World);
            float lLeft = life - _age;
            if (lLeft < 0.4f || shrink) transform.localScale = _scale * Mathf.Clamp01(shrink ? lLeft / life : lLeft / 0.4f);
            if (lLeft > 0f) return;
            if (pool != null)
            {
                gameObject.SetActive(false);
                pool.Push(this);
            }
            else Destroy(gameObject);
        }
    }
}
