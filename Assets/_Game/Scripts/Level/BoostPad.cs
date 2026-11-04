using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Huge glowing chevrons draped on the ground : run over them for a burst of speed.
    public class BoostPad : Interactable
    {
        private const int CHEVRONS = 4;
        private const int SEGMENTS = 8;

        public float boost = 26f;

        public static BoostPad CreateDraped(Transform pParent, LevelLayout pLayout, float pS, float pLateral, float pWidth, float pLength, Color pColor)
        {
            TerrainField lField = pLayout.field;
            Vector3 lPosition = lField.Point(pS, pLateral);
            Vector3 lForward = pLayout.spine.Forward(pS);
            Vector3 lNormal = lField.Normal(pS, pLateral, 6f);
            GameObject lObject = new GameObject("BoostPad");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(lPosition, Quaternion.LookRotation(Vector3.ProjectOnPlane(lForward, lNormal), lNormal));
            BoostPad lPad = lObject.AddComponent<BoostPad>();
            float lK = pLayout.profile.k;
            lPad.halfExtents = new Vector3(pWidth * 0.5f, 18f * Mathf.Sqrt(lK), pLength * 0.5f);
            lPad.boost = pLayout.profile.cruise * 0.16f;
            lPad.cooldown = 0.8f;

            Quaternion lInverse = Quaternion.Inverse(lObject.transform.rotation);
            MeshData lBack = new MeshData();
            MeshData lFront = new MeshData();
            float lHalf = pWidth * 0.5f;
            float lRowDepth = pLength / CHEVRONS;
            for (int lRow = -1; lRow < CHEVRONS; lRow++)
            {
                for (int i = 0; i < SEGMENTS; i++)
                {
                    float lU0 = -1f + 2f * i / SEGMENTS;
                    float lU1 = -1f + 2f * (i + 1) / SEGMENTS;
                    float lBase = -pLength * 0.5f + lRow * lRowDepth;
                    if (lRow < 0)
                    {
                        // Dark plate under the chevrons, follows the ground.
                        for (int k = 0; k < CHEVRONS; k++)
                        {
                            float lV0 = -pLength * 0.5f + k * lRowDepth;
                            Drape(lBack, lField, pS, pLateral, lU0 * lHalf, lU1 * lHalf, lV0, lV0, lRowDepth, 0.25f, lPosition, lInverse, new Color(0.08f, 0.06f, 0.12f));
                        }
                        continue;
                    }
                    float lPull0 = (1f - Mathf.Abs(lU0)) * lRowDepth * 0.55f;
                    float lPull1 = (1f - Mathf.Abs(lU1)) * lRowDepth * 0.55f;
                    Drape(lFront, lField, pS, pLateral, lU0 * lHalf, lU1 * lHalf, lBase + lPull0, lBase + lPull1, lRowDepth * 0.42f, 0.5f, lPosition, lInverse, Color.white);
                }
            }
            GameObject lPlate = new GameObject("Plate");
            lPlate.transform.SetParent(lObject.transform, false);
            lPlate.AddComponent<MeshFilter>().sharedMesh = lBack.ToMesh("PadPlate");
            lPlate.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(new Color(0.2f, 0.2f, 0.25f), true);
            GameObject lVisual = new GameObject("Chevrons");
            lVisual.transform.SetParent(lObject.transform, false);
            lVisual.AddComponent<MeshFilter>().sharedMesh = lFront.ToMesh("PadChevrons");
            lVisual.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(pColor * 2.6f, false, 0f, 0f, true);
            return lPad;
        }

        // A quad in pad space, its corners set on the terrain.
        private static void Drape(MeshData pData, TerrainField pField, float pS, float pLateral, float pL0, float pL1, float pV0, float pV1, float pDepth, float pLift, Vector3 pOrigin, Quaternion pInverse, Color pColor)
        {
            Vector3 lA = pInverse * (pField.Point(pS + pV0, pLateral + pL0) - pOrigin + Vector3.up * pLift);
            Vector3 lB = pInverse * (pField.Point(pS + pV1, pLateral + pL1) - pOrigin + Vector3.up * pLift);
            Vector3 lC = pInverse * (pField.Point(pS + pV1 + pDepth, pLateral + pL1) - pOrigin + Vector3.up * pLift);
            Vector3 lD = pInverse * (pField.Point(pS + pV0 + pDepth, pLateral + pL0) - pOrigin + Vector3.up * pLift);
            pData.Quad(lA, lB, lC, lD, Vector3.up, pColor);
        }

        protected override void OnTouch(PlayerController pPlayer)
        {
            pPlayer.ApplyBoost(boost, transform.forward, 0.5f, "BOOST!");
        }
    }
}
