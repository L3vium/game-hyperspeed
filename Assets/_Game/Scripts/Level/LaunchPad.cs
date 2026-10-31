using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Springy pad that throws the player up and forward over big gaps.
    public class LaunchPad : Interactable
    {
        public float upVelocity = 30f;
        public float forwardBoost = 10f;

        private Transform _spring;

        public static LaunchPad Create(Transform pParent, Vector3 pPosition, Quaternion pRotation, Color pColor)
        {
            GameObject lObject = new GameObject("LaunchPad");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(pPosition, pRotation);
            LaunchPad lPad = lObject.AddComponent<LaunchPad>();
            lPad.halfExtents = new Vector3(3.2f, 2.2f, 3.2f);

            Mats.Primitive(PrimitiveType.Cylinder, lObject.transform, new Vector3(0f, 0.15f, 0f), new Vector3(6.4f, 0.15f, 6.4f), Mats.Toon(new Color(0.12f, 0.1f, 0.16f), null, 0.02f));
            lPad._spring = Mats.Primitive(PrimitiveType.Cylinder, lObject.transform, new Vector3(0f, 0.35f, 0f), new Vector3(5f, 0.12f, 5f), Mats.Glow(pColor * 2f, false, 0f, 0f, true)).transform;
            GameObject lArrow = Mats.Primitive(PrimitiveType.Cube, lObject.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.2f, 0.1f, 3.2f), Mats.Toon(Color.white, null, 0.02f));
            lArrow.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            return lPad;
        }

        private void Update()
        {
            if (_spring != null) _spring.localPosition = new Vector3(0f, 0.35f + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.25f, 0f);
        }

        protected override void OnTouch(PlayerController pPlayer)
        {
            pPlayer.Launch(transform.up * upVelocity, forwardBoost);
        }
    }
}
