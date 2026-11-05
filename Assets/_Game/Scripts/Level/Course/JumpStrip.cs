using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Launch strip : a plain, very high jump. You keep your speed and your line, where you land is up to you.
    public class JumpStrip : Interactable
    {
        private float _up;

        public static JumpStrip Create(Transform pParent, LevelLayout pLayout, float pS, float pLateral, float pHalfWidth, float pUp, Color pColor)
        {
            TrackField lField = pLayout.field;
            Vector3 lPosition = lField.Point(pS, pLateral);
            Vector3 lNormal = lField.Normal(pS);
            Vector3 lForward = Vector3.ProjectOnPlane(pLayout.spine.Forward(pS), lNormal).normalized;
            float lDepth = pLayout.profile.cruise * 0.3f;
            GameObject lObject = new GameObject("JumpStrip");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(lPosition, Quaternion.LookRotation(lForward, lNormal));
            JumpStrip lStrip = lObject.AddComponent<JumpStrip>();
            lStrip._up = pUp;
            lStrip.halfExtents = new Vector3(pHalfWidth, 8f, Mathf.Max(6f, lDepth * 0.5f));
            lStrip.cooldown = 1f;

            // Bright bands draped on the ground.
            MeshData lData = new MeshData();
            Quaternion lInverse = Quaternion.Inverse(lObject.transform.rotation);
            int lSegments = Mathf.Clamp(Mathf.RoundToInt(pHalfWidth / 5f), 4, 24);
            for (int lRow = 0; lRow < 4; lRow++)
            {
                float lS0 = pS - lDepth * 0.5f + lDepth * lRow / 4f;
                float lBandDepth = lDepth / 4f * 0.55f;
                for (int i = 0; i < lSegments; i++)
                {
                    float lU0 = -1f + 2f * i / lSegments;
                    float lU1 = -1f + 2f * (i + 1) / lSegments;
                    float lBend0 = Mathf.Abs(lU0) * lDepth * 0.2f;
                    float lBend1 = Mathf.Abs(lU1) * lDepth * 0.2f;
                    float lW = pLateral + lU0 * pHalfWidth;
                    float lE = pLateral + lU1 * pHalfWidth;
                    Vector3 lA = lField.Point(lS0 - lBend0, lW) + lNormal * 0.35f - lPosition;
                    Vector3 lB = lField.Point(lS0 - lBend1, lE) + lNormal * 0.35f - lPosition;
                    Vector3 lC = lField.Point(lS0 - lBend1 + lBandDepth, lE) + lNormal * 0.35f - lPosition;
                    Vector3 lD = lField.Point(lS0 - lBend0 + lBandDepth, lW) + lNormal * 0.35f - lPosition;
                    lData.Quad(lInverse * lA, lInverse * lB, lInverse * lC, lInverse * lD, Vector3.up, Color.white);
                }
            }
            GameObject lVisual = new GameObject("Visual");
            lVisual.transform.SetParent(lObject.transform, false);
            lVisual.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("JumpStrip");
            lVisual.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(pColor * 2.4f, false, 0f, 0f, true);
            return lStrip;
        }

        protected override void OnTouch(PlayerController pPlayer)
        {
            pPlayer.Launch(_up);
            FX.Shockwave(pPlayer.transform.position, transform.up, Color.white, 18f);
        }
    }
}
