using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Giant block standing on the terrain : flat colours, faces to run and climb on, top to land on.
    public class Monolith : MonoBehaviour
    {
        public float topY;
        public Vector3 size;
        public Color color;
        public bool IsBroken { get; private set; }

        public static Monolith Create(Transform pParent, Vector3 pBase, Quaternion pRotation, Vector3 pSize, float pTaper, Color pColor, Theme pTheme, int pSeed)
        {
            GameObject lObject = new GameObject("Block");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(pBase, pRotation);
            lObject.layer = Layers.PROP;
            Monolith lBlock = lObject.AddComponent<Monolith>();
            lBlock.topY = pBase.y + pSize.y;
            lBlock.size = pSize;
            lBlock.color = pColor;

            float lBury = 90f + pSize.y * 0.3f;
            float lW = pSize.x * 0.5f;
            float lL = pSize.z * 0.5f;
            float lTw = lW * (1f - pTaper);
            float lTl = lL * (1f - pTaper);
            System.Random lRandom = new System.Random(pSeed);
            float lSkewX = pTaper > 0f ? (float)(lRandom.NextDouble() - 0.5) * lW * pTaper : 0f;
            float lSkewZ = pTaper > 0f ? (float)(lRandom.NextDouble() - 0.5) * lL * pTaper : 0f;
            Vector3 lBa = new Vector3(-lW, -lBury, -lL);
            Vector3 lBb = new Vector3(lW, -lBury, -lL);
            Vector3 lBc = new Vector3(lW, -lBury, lL);
            Vector3 lBd = new Vector3(-lW, -lBury, lL);
            Vector3 lTa = new Vector3(-lTw + lSkewX, pSize.y, -lTl + lSkewZ);
            Vector3 lTb = new Vector3(lTw + lSkewX, pSize.y, -lTl + lSkewZ);
            Vector3 lTc = new Vector3(lTw + lSkewX, pSize.y, lTl + lSkewZ);
            Vector3 lTd = new Vector3(-lTw + lSkewX, pSize.y, lTl + lSkewZ);

            Color lTop = Color.Lerp(pColor, Color.white, 0.35f);
            Color lDark = Color.Lerp(pColor, pTheme.shade, 0.3f);
            MeshData lData = new MeshData();
            lData.FacetQuad(lTa, lTb, lTc, lTd, Vector3.up, lTop);
            lData.FacetQuad(lBa, lBb, lTb, lTa, Vector3.back, pColor);
            lData.FacetQuad(lBc, lBd, lTd, lTc, Vector3.forward, lDark);
            lData.FacetQuad(lBb, lBc, lTc, lTb, Vector3.right, lDark);
            lData.FacetQuad(lBd, lBa, lTa, lTd, Vector3.left, pColor);
            Mesh lMesh = lData.ToMesh("Block");
            lObject.AddComponent<MeshFilter>().sharedMesh = lMesh;
            lObject.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(pTheme.shade, false);
            MeshCollider lCollider = lObject.AddComponent<MeshCollider>();
            lCollider.sharedMesh = lMesh;
            lCollider.convex = true;
            return lBlock;
        }

        // World-breaker stages : the runner goes through at light speed.
        public void Smash(Vector3 pDirection, float pSpeed)
        {
            if (IsBroken) return;
            IsBroken = true;
            float lRadius = Mathf.Min(size.x, size.z) * 0.5f;
            Vector3 lCenter = transform.position + Vector3.up * Mathf.Min(size.y * 0.5f, 40f);
            Fracture.Explode(lCenter, Mathf.Clamp(lRadius, 12f, 40f), color, pDirection, pSpeed);
            Destroy(gameObject);
        }
    }
}
