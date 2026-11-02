using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Targeted launcher : solves the arc to land on a chosen point (another platform), with a marker shown there.
    public class JumpPad : Interactable
    {
        public Vector3 target;
        public float bonus = 10f;
        // Front face of the deck the pad sends you onto (distance ahead, height of its top above the pad) : the arc
        // gets steeper and slower when it would otherwise reach that face before being above it.
        public float faceDistance;
        public float faceHeight;

        private Transform _spring;
        private Transform _marker;

        public static JumpPad Create(Transform pParent, Vector3 pPosition, Quaternion pRotation, Vector3 pTarget, Vector3 pTargetUp, Color pColor)
        {
            GameObject lObject = new GameObject("JumpPad");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(pPosition, pRotation);
            JumpPad lPad = lObject.AddComponent<JumpPad>();
            lPad.halfExtents = new Vector3(3.6f, 2.4f, 3.6f);
            lPad.target = pTarget;

            Material lGlow = Mats.Glow(pColor * 2.4f, false, 0f, 0f, true);
            GameObject lBase = new GameObject("Base");
            lBase.transform.SetParent(lObject.transform, false);
            lBase.AddComponent<MeshFilter>().sharedMesh = MeshUtil.Ring(2.2f, 3.6f, 32);
            lBase.AddComponent<MeshRenderer>().sharedMaterial = lGlow;
            lBase.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            lBase.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lPad._spring = new GameObject("Spring").transform;
            lPad._spring.SetParent(lObject.transform, false);
            for (int i = 0; i < 3; i++)
            {
                GameObject lRing = new GameObject("Ring" + i);
                lRing.transform.SetParent(lPad._spring, false);
                lRing.transform.localPosition = new Vector3(0f, 0.4f + i * 0.55f, 0f);
                lRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                lRing.transform.localScale = Vector3.one * (1f - i * 0.22f);
                lRing.AddComponent<MeshFilter>().sharedMesh = MeshUtil.Ring(1.6f, 2.1f, 24);
                lRing.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(pColor * (1.6f - i * 0.3f), true);
            }

            // Landing marker : you always know where the pad sends you.
            lPad._marker = new GameObject("LandingMarker").transform;
            lPad._marker.SetParent(pParent, false);
            lPad._marker.position = pTarget + pTargetUp * 0.12f;
            lPad._marker.rotation = Quaternion.LookRotation(pTargetUp) * Quaternion.Euler(0f, 0f, 0f);
            lPad._marker.gameObject.AddComponent<MeshFilter>().sharedMesh = MeshUtil.Ring(3.2f, 4.2f, 40);
            lPad._marker.gameObject.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(pColor * 1.8f, true);
            return lPad;
        }

        private void Update()
        {
            if (_spring != null) _spring.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.time * 5f)) * 0.12f);
            if (_marker != null) _marker.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 4f) * 0.08f);
        }

        protected override void OnTouch(PlayerController pPlayer)
        {
            Vector3 lFrom = pPlayer.transform.position;
            Vector3 lFlat = target - lFrom;
            lFlat.y = 0f;
            float lDistance = lFlat.magnitude;
            // A pad is a scripted arc : capped horizontal speed (predictable landing) and an apex that always clears
            // the target deck by 3 m (never into its front face at high speed).
            float lHorizontal = Mathf.Clamp(pPlayer.Speed, 55f, 100f) + bonus;
            float lVertical = Ballistics.SolveVerticalSpeed(lDistance, lHorizontal, target.y - lFrom.y);
            lVertical = Mathf.Max(lVertical, Mathf.Sqrt(2f * Ballistics.G_RISE * Mathf.Max(0f, target.y - lFrom.y + 3f)));
            if (faceDistance > 1f && faceHeight > -1f)
            {
                float lClimb = Mathf.Min(Mathf.Max(lVertical, 30f), 48f);
                float lDisc = lClimb * lClimb - 2f * Ballistics.G_RISE * (faceHeight + 1.5f);
                if (lDisc > 0f)
                {
                    float lRise = (lClimb - Mathf.Sqrt(lDisc)) / Ballistics.G_RISE;
                    float lMax = faceDistance / Mathf.Max(0.05f, lRise);
                    if (lHorizontal > lMax)
                    {
                        lHorizontal = Mathf.Max(30f, lMax);
                        lVertical = Mathf.Max(lVertical, lClimb);
                    }
                }
            }
            Vector3 lVelocity = (lDistance > 0.1f ? lFlat / lDistance : transform.forward) * lHorizontal + Vector3.up * lVertical;
            pPlayer.LaunchTo(lVelocity, target);
            FX.Shockwave(transform.position + transform.up * 0.2f, transform.up, UIFactory.YELLOW, 10f);
        }
    }
}
