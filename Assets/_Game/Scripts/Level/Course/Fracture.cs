using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Big smash : the rock bursts into huge chunks flying everywhere, lava blobs, rings and a flash.
    public static class Fracture
    {
        public static void Explode(Vector3 pCenter, float pRadius, Color pColor, Vector3 pDirection, float pSpeed)
        {
            Theme lTheme = RaceContext.Theme;
            Material lMaterial = Mats.Flat(lTheme != null ? lTheme.shade : new Color(0.6f, 0.55f, 0.8f), false, pColor);
            Material lLava = Mats.Glow(new Color(3.6f, 1.2f, 0.12f), false, 0f, 0f, true);
            Vector3 lDrive = pDirection.normalized * Mathf.Min(pSpeed, 400f) * 0.75f;
            for (int i = 0; i < 16; i++)
            {
                Vector3 lOut = Random.onUnitSphere;
                float lSize = pRadius * Random.Range(0.18f, 0.5f);
                GameObject lChunk = new GameObject("Chunk");
                lChunk.transform.position = pCenter + lOut * pRadius * 0.5f;
                lChunk.transform.localScale = Vector3.one * lSize;
                lChunk.transform.rotation = Random.rotation;
                lChunk.AddComponent<MeshFilter>().sharedMesh = RockSculptor.Boulder(i);
                lChunk.AddComponent<MeshRenderer>().sharedMaterial = lMaterial;
                ChunkAnim lAnim = lChunk.AddComponent<ChunkAnim>();
                lAnim.velocity = lOut * Random.Range(40f, 130f) * Mathf.Sqrt(pRadius / 16f) + lDrive + Vector3.up * Random.Range(10f, 60f);
                lAnim.spin = Random.onUnitSphere * Random.Range(180f, 720f);
                lAnim.life = Random.Range(1.6f, 2.8f);
            }
            for (int i = 0; i < 12; i++)
            {
                Vector3 lOut = Random.onUnitSphere;
                GameObject lBlob = Mats.Primitive(PrimitiveType.Sphere, null, pCenter, Vector3.one * pRadius * Random.Range(0.05f, 0.14f), lLava);
                ChunkAnim lAnim = lBlob.AddComponent<ChunkAnim>();
                lAnim.velocity = lOut * Random.Range(60f, 190f) + lDrive * 0.8f;
                lAnim.life = Random.Range(0.8f, 1.6f);
                lAnim.shrink = true;
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

        private float _age;
        private Vector3 _scale;

        private void Start() => _scale = transform.localScale;

        private void Update()
        {
            float lDt = Time.deltaTime;
            _age += lDt;
            velocity += Vector3.down * 70f * lDt;
            transform.position += velocity * lDt;
            transform.Rotate(spin * lDt, Space.World);
            float lLeft = life - _age;
            if (lLeft < 0.4f || shrink) transform.localScale = _scale * Mathf.Clamp01(shrink ? lLeft / life : lLeft / 0.4f);
            if (lLeft <= 0f) Destroy(gameObject);
        }
    }
}
