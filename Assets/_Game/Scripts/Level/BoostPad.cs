using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Glowing chevrons on the road : speed burst + partial alignment with the pad direction.
    public class BoostPad : Interactable
    {
        public float boost = 26f;

        private Transform _visual;

        public static BoostPad Create(Transform pParent, Vector3 pPosition, Quaternion pRotation, Color pColor)
        {
            GameObject lObject = new GameObject("BoostPad");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(pPosition, pRotation);
            BoostPad lPad = lObject.AddComponent<BoostPad>();
            lPad.halfExtents = new Vector3(2.6f, 2.2f, 3.2f);

            lPad._visual = new GameObject("Visual").transform;
            lPad._visual.SetParent(lObject.transform, false);
            Mats.Primitive(PrimitiveType.Cube, lPad._visual, new Vector3(0f, 0.04f, 0f), new Vector3(5f, 0.08f, 6.4f), Mats.Toon(new Color(0.1f, 0.08f, 0.15f), null, 0f));
            Material lGlow = Mats.Glow(pColor * 2.2f, false, 0f, 0f, true);
            for (int i = 0; i < 3; i++)
            {
                for (int lSide = -1; lSide <= 1; lSide += 2)
                {
                    GameObject lBar = Mats.Primitive(PrimitiveType.Cube, lPad._visual, new Vector3(lSide * 0.9f, 0.12f, -1.8f + i * 1.8f), new Vector3(0.45f, 0.06f, 2.3f), lGlow);
                    lBar.transform.localRotation = Quaternion.Euler(0f, -lSide * 45f, 0f);
                }
            }
            return lPad;
        }

        private void Update()
        {
            if (_visual == null) return;
            float lPulse = 1f + Mathf.Sin(Time.time * 14f) * 0.04f;
            _visual.localScale = new Vector3(lPulse, 1f, lPulse);
        }

        protected override void OnTouch(PlayerController pPlayer)
        {
            pPlayer.ApplyBoost(boost, transform.forward, 0.5f, "BOOST!");
        }
    }
}
