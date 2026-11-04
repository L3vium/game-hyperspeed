using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Launch strip along a whole platform lip : no aiming, you keep your speed and your side, it adds the height that
    // carries you over the gap.
    public class JumpStrip : Interactable
    {
        private LevelLayout _layout;
        private float _targetS;
        private float _targetLateral;
        private float _targetHeight;

        public static JumpStrip Create(Transform pParent, LevelLayout pLayout, float pS, float pLateral, float pHalfWidth, float pDepth, float pTargetS, float pTargetLateral, float pTargetHeight, Color pColor)
        {
            TerrainField lField = pLayout.field;
            Vector3 lPosition = lField.Point(pS, pLateral);
            Vector3 lForward = pLayout.spine.Forward(pS);
            GameObject lObject = new GameObject("JumpStrip");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(lPosition, Quaternion.LookRotation(lForward, Vector3.up));
            JumpStrip lStrip = lObject.AddComponent<JumpStrip>();
            lStrip._layout = pLayout;
            lStrip._targetS = pTargetS;
            lStrip._targetLateral = pTargetLateral;
            lStrip._targetHeight = pTargetHeight;
            lStrip.halfExtents = new Vector3(pHalfWidth, 20f, Mathf.Max(6f, pDepth * 0.5f));
            lStrip.cooldown = 1f;

            // Bright bands draped on the ground, as wide as the platform.
            MeshData lData = new MeshData();
            Quaternion lInverse = Quaternion.Inverse(lObject.transform.rotation);
            int lSegments = Mathf.Clamp(Mathf.RoundToInt(pHalfWidth / 12f), 6, 40);
            for (int lRow = 0; lRow < 4; lRow++)
            {
                float lS0 = pS - pDepth * 0.5f + pDepth * lRow / 4f;
                float lDepth = pDepth / 4f * 0.55f;
                for (int i = 0; i < lSegments; i++)
                {
                    float lU0 = -1f + 2f * i / lSegments;
                    float lU1 = -1f + 2f * (i + 1) / lSegments;
                    float lBend0 = Mathf.Abs(lU0) * pDepth * 0.2f;
                    float lBend1 = Mathf.Abs(lU1) * pDepth * 0.2f;
                    float lW = pLateral + lU0 * pHalfWidth;
                    float lE = pLateral + lU1 * pHalfWidth;
                    Vector3 lA = lField.Point(lS0 - lBend0, lW) + Vector3.up * 0.5f - lPosition;
                    Vector3 lB = lField.Point(lS0 - lBend1, lE) + Vector3.up * 0.5f - lPosition;
                    Vector3 lC = lField.Point(lS0 - lBend1 + lDepth, lE) + Vector3.up * 0.5f - lPosition;
                    Vector3 lD = lField.Point(lS0 - lBend0 + lDepth, lW) + Vector3.up * 0.5f - lPosition;
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
            Vector3 lFrom = pPlayer.transform.position;
            float lAlong = Mathf.Max(pPlayer.Speed, _layout.profile.cruise * 0.8f);
            float lDistance = Mathf.Max(20f, _targetS - pPlayer.Progress);
            float lLateral = float.IsNaN(_targetLateral) ? pPlayer.Lateral : _targetLateral;
            float lHeight = float.IsNaN(_targetHeight) ? _layout.Ground(_targetS, lLateral) + 70f * Mathf.Pow(_layout.profile.k, 0.7f) : _targetHeight;
            float lVertical = Ballistics.SolveVerticalSpeed(lDistance, lAlong, lHeight - lFrom.y);
            float lSide = float.IsNaN(_targetLateral) ? pPlayer.LateralSpeed : (lLateral - pPlayer.Lateral) / (lDistance / lAlong);
            pPlayer.Launch(lAlong, lSide, lVertical);
            FX.Shockwave(lFrom, Vector3.up, UIFactory.YELLOW, 22f);
        }
    }
}
