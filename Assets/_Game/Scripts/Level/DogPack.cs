using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // The first rival (2026-11-12) : a pack of greyhounds, built in code from simple shapes like the other code-built
    // rivals. Each dog gallops on its own rhythm (rotary gallop : the legs reach and gather, the spine flexes, the head
    // bobs, the ears and tail stream), and the pack moves as a loose ground flock around the rival's point : apart,
    // aligned, never in a line, now and then one surging ahead of the others.
    public class DogPack : MonoBehaviour
    {
        private const int DOGS = 9;
        // A greyhound is about 1.2 m long : scaled up like every rival so it reads from far back.
        private const float SCALE = 3.6f;
        private const float SPREAD_X = 26f;
        private const float SPREAD_Z = 34f;

        private Dog[] _dogs;
        private Vector3 _lastPosition;
        private float _speed;

        private class Dog
        {
            public Transform root;
            public Transform body;
            public Transform head;
            public Transform tail;
            public Transform[] upper = new Transform[4];
            public Transform[] lower = new Transform[4];
            public Transform earL;
            public Transform earR;
            public Vector2 slot;
            public Vector2 offset;
            public Vector2 velocity;
            public float phase;
            public float rate;
            public float surge;
            public float surgeClock;
        }

        public static Transform Build(Transform pParent)
        {
            DogPack lPack = new GameObject("DogPack").AddComponent<DogPack>();
            lPack.transform.SetParent(pParent, false);
            lPack.Create();
            return lPack.transform;
        }

        private void Create()
        {
            _dogs = new Dog[DOGS];
            Color[] lCoats =
            {
                new Color(0.82f, 0.68f, 0.5f), new Color(0.32f, 0.3f, 0.34f), new Color(0.9f, 0.88f, 0.84f),
                new Color(0.62f, 0.42f, 0.28f), new Color(0.5f, 0.52f, 0.6f)
            };
            for (int i = 0; i < DOGS; i++)
            {
                Color lCoat = lCoats[i % lCoats.Length];
                // Mats.Flat : shade colour first, lit colour last.
                Material lMaterial = Mats.Flat(Color.Lerp(lCoat, Color.black, 0.55f), false, lCoat);
                Material lDark = Mats.Flat(Color.Lerp(lCoat, Color.black, 0.75f), false, Color.Lerp(lCoat, Color.black, 0.45f));
                Dog lDog = new Dog();
                lDog.root = new GameObject("Dog" + i).transform;
                lDog.root.SetParent(transform, false);
                lDog.root.localScale = Vector3.one * SCALE * Random.Range(0.9f, 1.1f);
                // Formation slots : a loose wedge, the leaders in front.
                int lRow = i / 3;
                lDog.slot = new Vector2(((i % 3) - 1) * SPREAD_X * 0.45f + Random.Range(-4f, 4f), -lRow * SPREAD_Z * 0.4f + Random.Range(-5f, 5f));
                lDog.offset = lDog.slot;
                lDog.phase = Random.value;
                lDog.rate = Random.Range(2.6f, 3.1f);
                lDog.surgeClock = Random.Range(2f, 7f);

                lDog.body = new GameObject("Body").transform;
                lDog.body.SetParent(lDog.root, false);
                lDog.body.localPosition = new Vector3(0f, 0.72f, 0f);
                // Deep chest, tucked waist : a greyhound.
                Part(PrimitiveType.Sphere, lDog.body, new Vector3(0f, 0.04f, 0.22f), new Vector3(0.34f, 0.42f, 0.6f), lMaterial);
                Part(PrimitiveType.Sphere, lDog.body, new Vector3(0f, 0.06f, -0.22f), new Vector3(0.26f, 0.26f, 0.52f), lMaterial);
                // Neck and head, the long snout.
                lDog.head = new GameObject("Head").transform;
                lDog.head.SetParent(lDog.body, false);
                lDog.head.localPosition = new Vector3(0f, 0.22f, 0.5f);
                Part(PrimitiveType.Capsule, lDog.head, new Vector3(0f, -0.06f, -0.08f), new Vector3(0.14f, 0.18f, 0.14f), lMaterial).localRotation = Quaternion.Euler(50f, 0f, 0f);
                Part(PrimitiveType.Sphere, lDog.head, new Vector3(0f, 0.08f, 0.06f), new Vector3(0.2f, 0.2f, 0.26f), lMaterial);
                Part(PrimitiveType.Sphere, lDog.head, new Vector3(0f, 0.04f, 0.24f), new Vector3(0.1f, 0.1f, 0.28f), lMaterial);
                Part(PrimitiveType.Sphere, lDog.head, new Vector3(0f, 0.06f, 0.38f), new Vector3(0.06f, 0.06f, 0.06f), lDark);
                lDog.earL = Part(PrimitiveType.Cube, lDog.head, new Vector3(-0.07f, 0.17f, -0.02f), new Vector3(0.04f, 0.1f, 0.12f), lDark);
                lDog.earR = Part(PrimitiveType.Cube, lDog.head, new Vector3(0.07f, 0.17f, -0.02f), new Vector3(0.04f, 0.1f, 0.12f), lDark);
                // Tail.
                lDog.tail = new GameObject("Tail").transform;
                lDog.tail.SetParent(lDog.body, false);
                lDog.tail.localPosition = new Vector3(0f, 0.1f, -0.46f);
                Part(PrimitiveType.Capsule, lDog.tail, new Vector3(0f, 0f, -0.2f), new Vector3(0.04f, 0.22f, 0.04f), lMaterial).localRotation = Quaternion.Euler(80f, 0f, 0f);
                // Four legs in two segments : front pair under the chest, hind pair under the hips.
                for (int lLeg = 0; lLeg < 4; lLeg++)
                {
                    bool lFront = lLeg < 2;
                    float lSide = lLeg % 2 == 0 ? -0.1f : 0.1f;
                    Transform lUpper = new GameObject("Leg" + lLeg).transform;
                    lUpper.SetParent(lDog.body, false);
                    lUpper.localPosition = new Vector3(lSide, -0.08f, lFront ? 0.3f : -0.3f);
                    Part(PrimitiveType.Capsule, lUpper, new Vector3(0f, -0.17f, 0f), new Vector3(0.08f, 0.18f, 0.08f), lMaterial);
                    Transform lLower = new GameObject("Shin").transform;
                    lLower.SetParent(lUpper, false);
                    lLower.localPosition = new Vector3(0f, -0.34f, 0f);
                    Part(PrimitiveType.Capsule, lLower, new Vector3(0f, -0.15f, 0f), new Vector3(0.05f, 0.16f, 0.05f), lDark);
                    lDog.upper[lLeg] = lUpper;
                    lDog.lower[lLeg] = lLower;
                }
                _dogs[i] = lDog;
            }
            _lastPosition = transform.position;
        }

        private static Transform Part(PrimitiveType pType, Transform pParent, Vector3 pPosition, Vector3 pScale, Material pMaterial)
        {
            GameObject lPart = Mats.Primitive(pType, pParent, pPosition, pScale, pMaterial);
            lPart.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return lPart.transform;
        }

        private void Update()
        {
            float lDt = Time.deltaTime;
            if (lDt <= 0f) return;
            _speed = Mathf.Lerp(_speed, (transform.position - _lastPosition).magnitude / lDt, 1f - Mathf.Exp(-4f * lDt));
            _lastPosition = transform.position;
            for (int i = 0; i < _dogs.Length; i++) UpdateDog(_dogs[i], i, lDt);
        }

        private void UpdateDog(Dog pDog, int pIndex, float pDt)
        {
            // Ground flock, in the pack's own plane : back to its slot, away from the others, now and then a surge.
            pDog.surgeClock -= pDt;
            if (pDog.surgeClock <= 0f)
            {
                pDog.surgeClock = Random.Range(3f, 8f);
                pDog.surge = Random.Range(10f, 22f);
            }
            pDog.surge = Mathf.MoveTowards(pDog.surge, 0f, pDt * 6f);
            Vector2 lTarget = pDog.slot + new Vector2(Mathf.Sin(Time.time * 0.4f + pIndex) * 4f, pDog.surge);
            Vector2 lSteer = (lTarget - pDog.offset) * 1.6f;
            for (int j = 0; j < _dogs.Length; j++)
            {
                if (j == pIndex) continue;
                Vector2 lAway = pDog.offset - _dogs[j].offset;
                float lDistance = lAway.magnitude;
                if (lDistance < 9f && lDistance > 0.01f) lSteer += lAway / lDistance * (9f - lDistance) * 3f;
            }
            pDog.velocity = Vector2.Lerp(pDog.velocity, lSteer, 1f - Mathf.Exp(-3f * pDt));
            pDog.offset += pDog.velocity * pDt;
            pDog.root.localPosition = new Vector3(pDog.offset.x, 0f, pDog.offset.y);
            pDog.root.localRotation = Quaternion.Euler(0f, Mathf.Clamp(pDog.velocity.x * 1.5f, -25f, 25f), 0f);

            // Rotary gallop : faster strides when the pack runs faster.
            float lRate = pDog.rate * Mathf.Lerp(0.8f, 1.6f, Mathf.Clamp01(_speed / 300f));
            pDog.phase = Mathf.Repeat(pDog.phase + lRate * pDt, 1f);
            float lA = pDog.phase * Mathf.PI * 2f;
            // Spine flexes : gathered (back arched) then fully stretched.
            float lFlex = Mathf.Sin(lA);
            pDog.body.localRotation = Quaternion.Euler(lFlex * 7f, 0f, 0f);
            pDog.body.localPosition = new Vector3(0f, 0.72f + Mathf.Max(0f, Mathf.Sin(lA * 1f + 0.6f)) * 0.12f, 0f);
            pDog.body.localScale = new Vector3(1f, 1f, 1f + lFlex * 0.08f);
            pDog.head.localRotation = Quaternion.Euler(-lFlex * 8f, 0f, 0f);
            pDog.tail.localRotation = Quaternion.Euler(-10f + Mathf.Sin(lA * 2f) * 12f, Mathf.Sin(lA) * 10f, 0f);
            float lEars = -40f - _speed * 0.05f;
            pDog.earL.localRotation = Quaternion.Euler(lEars, 0f, -15f);
            pDog.earR.localRotation = Quaternion.Euler(lEars, 0f, 15f);
            // Front legs reach forward together (a little apart), hind legs drive back : the double suspension gallop.
            for (int lLeg = 0; lLeg < 4; lLeg++)
            {
                bool lFront = lLeg < 2;
                float lOffset = (lFront ? 0f : 0.5f) + (lLeg % 2) * 0.08f;
                float lP = (pDog.phase + lOffset) * Mathf.PI * 2f;
                float lSwing = Mathf.Sin(lP) * (lFront ? 55f : 50f);
                float lKnee = Mathf.Max(0f, Mathf.Cos(lP)) * (lFront ? -60f : 70f);
                pDog.upper[lLeg].localRotation = Quaternion.Euler(lFront ? -lSwing : lSwing, 0f, 0f);
                pDog.lower[lLeg].localRotation = Quaternion.Euler(lKnee, 0f, 0f);
            }
        }
    }
}
