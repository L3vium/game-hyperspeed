using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Airborne hoop : redirects along its axis, boosts, refreshes air abilities.
    public class BoostRing : Interactable
    {
        public float boost = 16f;

        private Transform _visual;
        private float _spin;

        public static BoostRing Create(Transform pParent, Vector3 pPosition, Quaternion pRotation, Color pColor, float pRadius = 4f)
        {
            GameObject lObject = new GameObject("BoostRing");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(pPosition, pRotation);
            BoostRing lRing = lObject.AddComponent<BoostRing>();
            lRing.halfExtents = new Vector3(pRadius, pRadius, 2.2f);
            lRing.cooldown = 1f;

            lRing._visual = new GameObject("Visual").transform;
            lRing._visual.SetParent(lObject.transform, false);
            Material lGlow = Mats.Glow(pColor * 2.5f, false, 0f, 0f, true);
            Material lCore = Mats.Toon(Color.white, null, 0.03f);
            const int SEGMENTS = 16;
            for (int i = 0; i < SEGMENTS; i++)
            {
                float lAngle = i * 360f / SEGMENTS;
                Vector3 lDir = Quaternion.Euler(0f, 0f, lAngle) * Vector3.up;
                GameObject lSeg = Mats.Primitive(PrimitiveType.Cube, lRing._visual, lDir * pRadius, new Vector3(pRadius * 0.42f, 0.45f, 0.6f), i % 2 == 0 ? lGlow : lCore);
                lSeg.transform.localRotation = Quaternion.Euler(0f, 0f, lAngle);
            }
            return lRing;
        }

        private void Update()
        {
            _spin += Time.deltaTime * 90f;
            if (_visual != null) _visual.localRotation = Quaternion.Euler(0f, 0f, _spin);
        }

        protected override void OnTouch(PlayerController pPlayer)
        {
            pPlayer.ApplyRing(boost, transform.forward);
            if (_visual != null) _visual.localScale = Vector3.one * 1.4f;
        }

        private void LateUpdate()
        {
            if (_visual != null) _visual.localScale = Vector3.Lerp(_visual.localScale, Vector3.one, Time.deltaTime * 6f);
        }
    }
}
