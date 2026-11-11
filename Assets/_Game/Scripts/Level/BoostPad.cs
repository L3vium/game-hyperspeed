using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Glowing chevrons draped on the track : run over them for one instant kick of speed, which then fades.
    public class BoostPad : Interactable
    {
        private const int CHEVRONS = 4;
        private const int SEGMENTS = 5;
        private const float LIFT = 0.28f;
        public const float PAD_POWER = 2.8f;

        public float boost = 26f;

        public static BoostPad CreateDraped(Transform pParent, LevelLayout pLayout, float pS, float pLateral, float pWidth, float pLength, Color pColor, float pBoost = 0.5f)
        {
            TrackField lField = pLayout.field;
            Vector3 lPosition = lField.Point(pS, pLateral);
            Vector3 lForward = pLayout.spine.Forward(pS);
            Vector3 lNormal = lField.Normal(pS);
            GameObject lObject = new GameObject("BoostPad");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(lPosition, Quaternion.LookRotation(Vector3.ProjectOnPlane(lForward, lNormal), lNormal));
            BoostPad lPad = lObject.AddComponent<BoostPad>();
            lPad.halfExtents = new Vector3(pWidth * 0.5f, 7f, pLength * 0.5f);
            // 2026-11-11 : pads kick almost three times harder than the stage data says (the run itself got calmer).
            lPad.boost = pLayout.profile.cruise * pBoost * PAD_POWER;
            lPad.cooldown = 0.4f;

            // Short pieces following the same line as the track mesh : the pad lies on the real surface.
            Quaternion lInverse = Quaternion.Inverse(lObject.transform.rotation);
            MeshData lBack = new MeshData();
            MeshData lFront = new MeshData();
            float lHalf = pWidth * 0.5f;
            float lRowDepth = pLength / CHEVRONS;
            for (int i = 0; i < SEGMENTS; i++)
            {
                float lU0 = -1f + 2f * i / SEGMENTS;
                float lU1 = -1f + 2f * (i + 1) / SEGMENTS;
                for (int k = 0; k < CHEVRONS; k++)
                {
                    float lV0 = -pLength * 0.5f + k * lRowDepth;
                    Drape(lBack, lField, pS, pLateral, lU0 * lHalf, lU1 * lHalf, lV0, lV0, lRowDepth, LIFT, lPosition, lInverse, new Color(0.08f, 0.06f, 0.12f));
                    float lPull0 = (1f - Mathf.Abs(lU0)) * lRowDepth * 0.55f;
                    float lPull1 = (1f - Mathf.Abs(lU1)) * lRowDepth * 0.55f;
                    Drape(lFront, lField, pS, pLateral, lU0 * lHalf, lU1 * lHalf, lV0 + lPull0, lV0 + lPull1, lRowDepth * 0.42f, LIFT + 0.08f, lPosition, lInverse, Color.white);
                }
            }
            GameObject lPlate = new GameObject("Plate");
            lPlate.transform.SetParent(lObject.transform, false);
            lPlate.AddComponent<MeshFilter>().sharedMesh = lBack.ToMesh("PadPlate");
            lPlate.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(new Color(0.2f, 0.2f, 0.25f), false);
            GameObject lVisual = new GameObject("Chevrons");
            lVisual.transform.SetParent(lObject.transform, false);
            lVisual.AddComponent<MeshFilter>().sharedMesh = lFront.ToMesh("PadChevrons");
            lVisual.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(pColor * 2.6f, false, 0f, 0f, true);
            return lPad;
        }

        // A quad in pad space, its corners set on the track.
        private static void Drape(MeshData pData, TrackField pField, float pS, float pLateral, float pL0, float pL1, float pV0, float pV1, float pDepth, float pLift, Vector3 pOrigin, Quaternion pInverse, Color pColor)
        {
            Vector3 lA = pInverse * (pField.Point(pS + pV0, pLateral + pL0) + pField.Normal(pS + pV0) * pLift - pOrigin);
            Vector3 lB = pInverse * (pField.Point(pS + pV1, pLateral + pL1) + pField.Normal(pS + pV1) * pLift - pOrigin);
            Vector3 lC = pInverse * (pField.Point(pS + pV1 + pDepth, pLateral + pL1) + pField.Normal(pS + pV1 + pDepth) * pLift - pOrigin);
            Vector3 lD = pInverse * (pField.Point(pS + pV0 + pDepth, pLateral + pL0) + pField.Normal(pS + pV0 + pDepth) * pLift - pOrigin);
            pData.Quad(lA, lB, lC, lD, Vector3.up, pColor);
        }

        public override bool Accepts(PlayerController pPlayer) => Along(pPlayer);

        protected override void OnTouch(PlayerController pPlayer)
        {
            pPlayer.ApplyBoost(boost, transform.forward, 0f, null, false, true);
        }
    }
}
