using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Boost ring in the air : fly through it for a kick along its axis.
    public class Ring : Interactable
    {
        private float _radius;
        private Transform _visual;

        public static Ring Create(Transform pParent, LevelLayout pLayout, float pS, float pLateral, float pHeight, float pRadius, Color pColor)
        {
            SpineSample lSample = pLayout.spine.Evaluate(pS);
            Vector3 lUp = pLayout.field.Normal(pS);
            Vector3 lPosition = pLayout.field.Point(pS, pLateral) + lUp * pHeight;
            GameObject lObject = new GameObject("Ring");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(lPosition, Quaternion.LookRotation(lSample.forward, lUp));
            Ring lRing = lObject.AddComponent<Ring>();
            lRing._radius = pRadius;
            lRing.halfExtents = new Vector3(pRadius, pRadius, Mathf.Max(5f, pLayout.profile.cruise * 0.04f));
            lRing.cooldown = 1.2f;

            MeshData lData = new MeshData();
            const int SEGMENTS = 32;
            float lThick = pRadius * 0.11f;
            for (int i = 0; i < SEGMENTS; i++)
            {
                float lA0 = i * Mathf.PI * 2f / SEGMENTS;
                float lA1 = (i + 1) * Mathf.PI * 2f / SEGMENTS;
                Vector3 lD0 = new Vector3(Mathf.Cos(lA0), Mathf.Sin(lA0), 0f);
                Vector3 lD1 = new Vector3(Mathf.Cos(lA1), Mathf.Sin(lA1), 0f);
                Vector3 lZ = Vector3.forward * lThick;
                lData.FacetQuad(lD0 * (pRadius - lThick) - lZ, lD1 * (pRadius - lThick) - lZ, lD1 * (pRadius + lThick) - lZ, lD0 * (pRadius + lThick) - lZ, Vector3.back, Color.white);
                lData.FacetQuad(lD0 * (pRadius - lThick) + lZ, lD1 * (pRadius - lThick) + lZ, lD1 * (pRadius + lThick) + lZ, lD0 * (pRadius + lThick) + lZ, Vector3.forward, Color.white);
                lData.FacetQuad(lD0 * (pRadius + lThick) - lZ, lD1 * (pRadius + lThick) - lZ, lD1 * (pRadius + lThick) + lZ, lD0 * (pRadius + lThick) + lZ, (lD0 + lD1), Color.white);
                lData.FacetQuad(lD0 * (pRadius - lThick) - lZ, lD1 * (pRadius - lThick) - lZ, lD1 * (pRadius - lThick) + lZ, lD0 * (pRadius - lThick) + lZ, -(lD0 + lD1), Color.white);
            }
            GameObject lVisual = new GameObject("Visual");
            lVisual.transform.SetParent(lObject.transform, false);
            lVisual.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("Ring");
            lVisual.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(Color.Lerp(pColor, new Color(1f, 0.85f, 0.2f), 0.6f) * 3f, false, 0f, 0f, true);
            lRing._visual = lVisual.transform;
            return lRing;
        }

        private void Update()
        {
            if (_visual != null) _visual.localRotation = Quaternion.Euler(0f, 0f, Time.time * 40f);
        }

        protected override void OnTouch(PlayerController pPlayer)
        {
            Vector3 lLocal = transform.InverseTransformPoint(pPlayer.transform.position);
            if (new Vector2(lLocal.x, lLocal.y).magnitude > _radius + 2f) return;
            pPlayer.ApplyBoost(pPlayer.Profile.cruise * 0.26f, transform.forward, 0f, null, true);
            FX.Shockwave(transform.position, transform.forward, Color.white, _radius * 2.6f, 0.3f);
            AudioManager.Play("sfx_ring", 0.9f, 1f + Random.Range(-0.05f, 0.1f));
            if (_visual != null) _visual.gameObject.SetActive(false);
            Destroy(gameObject, 0.6f);
        }
    }

    // Giant crate : dash through it and it bursts into burning pieces, throwing you forward. Run into it and it stops you.
    public class Crate : Breakable
    {
        private LevelLayout _layout;
        private float _s;
        private float _lateral;
        private float _speed;
        private float _lift;

        public static Crate Create(Transform pParent, LevelLayout pLayout, float pS, float pLateral, float pSize, bool pMoving, Theme pTheme)
        {
            GameObject lObject = new GameObject("Crate");
            lObject.transform.SetParent(pParent, false);
            Crate lCrate = lObject.AddComponent<Crate>();
            lCrate.radius = pSize * 0.62f;
            lCrate.big = true;
            lCrate.color = pTheme.decorB;
            lCrate._layout = pLayout;
            lCrate._s = pS;
            lCrate._lateral = pLateral;
            lCrate._lift = pSize * 0.5f;
            lCrate._speed = pMoving ? pLayout.profile.cruise * 0.55f : 0f;
            lCrate.Place();

            // Plank faces, dark bands, and glowing lava through the seams.
            MeshData lData = new MeshData();
            Color lBody = Color.Lerp(pTheme.decorB, Color.white, 0.12f);
            Color lBand = Color.Lerp(pTheme.decorB, Color.black, 0.55f);
            GateKit.Box(lData, Vector3.zero, Vector3.one * pSize, lBody);
            float lEdge = pSize * 0.08f;
            float lHalf = pSize * 0.5f;
            for (int lX = -1; lX <= 1; lX += 2)
            {
                for (int lY = -1; lY <= 1; lY += 2)
                {
                    GateKit.Box(lData, new Vector3(lX * lHalf, lY * lHalf, 0f), new Vector3(lEdge, lEdge, pSize + lEdge), lBand);
                    GateKit.Box(lData, new Vector3(lX * lHalf, 0f, lY * lHalf), new Vector3(lEdge, pSize + lEdge, lEdge), lBand);
                    GateKit.Box(lData, new Vector3(0f, lX * lHalf, lY * lHalf), new Vector3(pSize + lEdge, lEdge, lEdge), lBand);
                }
            }
            GameObject lMesh = new GameObject("Body");
            lMesh.transform.SetParent(lObject.transform, false);
            lMesh.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("Crate");
            lMesh.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(pTheme.shade, false);

            MeshData lSeams = new MeshData();
            float lSeam = pSize * 0.5f + 0.15f;
            GateKit.Box(lSeams, Vector3.back * lSeam, new Vector3(pSize * 0.86f, pSize * 0.05f, 0.1f), Color.white);
            GateKit.Box(lSeams, Vector3.back * lSeam, new Vector3(pSize * 0.05f, pSize * 0.86f, 0.1f), Color.white);
            GameObject lGlow = new GameObject("Lava");
            lGlow.transform.SetParent(lObject.transform, false);
            lGlow.AddComponent<MeshFilter>().sharedMesh = lSeams.ToMesh("CrateSeams");
            lGlow.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(new Color(3.4f, 1.1f, 0.12f), false, 0f, 0f, true);
            return lCrate;
        }

        private void Place()
        {
            Vector3 lUp = _layout.field.Normal(_s);
            transform.SetPositionAndRotation(_layout.field.Point(_s, _lateral) + lUp * _lift, Quaternion.LookRotation(_layout.spine.Forward(_s), lUp));
        }

        protected override void Update()
        {
            if (_speed <= 0f || !RaceContext.Running || IsBroken) return;
            // Woken up only as the runner comes near : every convoy gets its moment.
            if (Mathf.Abs(_s - RaceContext.PlayerProgress) > _layout.profile.cruise * 6f) return;
            _s = Mathf.Min(_s + _speed * Time.deltaTime, _layout.finishS - 30f);
            if (_layout.field.Solid(_s)) Place();
        }
    }

    // Beams across the track, from low jump bars to slanted fans. Touch one : you lose the speed.
    public class LaserGate : Interactable
    {
        private Transform _spinner;
        private float _spin;
        private float _halfWidth;
        private float[] _angles;
        private float _height;

        public static LaserGate Create(Transform pParent, LevelLayout pLayout, float pS, float pHeight, int pBeams, float pSpin, Theme pTheme)
        {
            SpineSample lSample = pLayout.spine.Evaluate(pS);
            Vector3 lUp = pLayout.field.Normal(pS);
            float lHalf = pLayout.field.HalfWidth(pS);
            GameObject lObject = new GameObject("Laser");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(pLayout.field.Point(pS, pLayout.field.CenterLateral(pS)) + lUp * pHeight, Quaternion.LookRotation(lSample.forward, lUp));
            LaserGate lGate = lObject.AddComponent<LaserGate>();
            lGate._halfWidth = lHalf + 8f;
            lGate._height = pHeight;
            lGate._spin = pSpin;
            lGate.halfExtents = new Vector3(lHalf + 8f, 40f, Mathf.Max(3f, pLayout.profile.cruise * 0.03f));
            lGate.cooldown = 1f;
            lGate._angles = new float[pBeams];

            Transform lSpinner = new GameObject("Beams").transform;
            lSpinner.SetParent(lObject.transform, false);
            lGate._spinner = lSpinner;
            Material lBeam = Mats.Glow(new Color(3.6f, 0.25f, 0.2f), false, 0f, 0f, true);
            Material lCore = Mats.Glow(new Color(4f, 3f, 3f), false, 0f, 0f, true);
            for (int i = 0; i < pBeams; i++)
            {
                float lAngle = pBeams == 1 ? 0f : -40f + 80f * i / (pBeams - 1) + (pBeams == 4 ? 7f : 0f);
                lGate._angles[i] = lAngle;
                Transform lPivot = new GameObject("Beam").transform;
                lPivot.SetParent(lSpinner, false);
                lPivot.localRotation = Quaternion.Euler(0f, 0f, lAngle);
                float lLength = (lHalf + 8f) * 2f / Mathf.Max(0.5f, Mathf.Cos(lAngle * Mathf.Deg2Rad));
                Mats.Primitive(PrimitiveType.Cube, lPivot, Vector3.zero, new Vector3(lLength, 0.7f, 0.7f), lBeam);
                Mats.Primitive(PrimitiveType.Cube, lPivot, Vector3.zero, new Vector3(lLength, 0.3f, 0.3f), lCore);
            }
            // Emitters on both sides.
            MeshData lPost = new MeshData();
            Color lDark = Color.Lerp(pTheme.decorC, Color.black, 0.6f);
            GateKit.Box(lPost, new Vector3(-lHalf - 8f, 0f, 0f), new Vector3(3f, pHeight * 2f + 8f, 3f), lDark);
            GateKit.Box(lPost, new Vector3(lHalf + 8f, 0f, 0f), new Vector3(3f, pHeight * 2f + 8f, 3f), lDark);
            GameObject lPosts = new GameObject("Posts");
            lPosts.transform.SetParent(lObject.transform, false);
            lPosts.AddComponent<MeshFilter>().sharedMesh = lPost.ToMesh("LaserPosts");
            lPosts.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(pTheme.shade, false);
            return lGate;
        }

        private void Update()
        {
            if (_spin != 0f && _spinner != null) _spinner.localRotation = Quaternion.Euler(0f, 0f, Time.time * _spin);
        }

        // Precise test : the gate is thin in z, each beam is a line in the local plane.
        private bool HitsBeam(Vector3 pLocal)
        {
            float lBase = _spin != 0f ? Time.time * _spin : 0f;
            foreach (float lAngle in _angles)
            {
                float lRad = (lAngle + lBase) * Mathf.Deg2Rad;
                float lDistance = Mathf.Abs(-Mathf.Sin(lRad) * pLocal.x + Mathf.Cos(lRad) * pLocal.y);
                if (lDistance < 2.2f) return true;
            }
            return false;
        }

        protected override void OnTouch(PlayerController pPlayer)
        {
            if (pPlayer.Dashing || pPlayer.LightForm) return;
            if (!HitsBeam(transform.InverseTransformPoint(pPlayer.transform.position + pPlayer.Up))) return;
            pPlayer.Crash(pPlayer.transform.position + pPlayer.Heading * 2f);
            FX.Sparks(pPlayer.transform.position, new Color(1f, 0.3f, 0.2f), 40, 40f);
        }
    }

    // Pendulum swinging across the track from a frame above it.
    public class Hammer : Interactable
    {
        private const float ARM = 48f;

        private Transform _pivot;
        private float _phase;
        private float _head;

        public static Hammer Create(Transform pParent, LevelLayout pLayout, float pS, float pPhase, Theme pTheme)
        {
            SpineSample lSample = pLayout.spine.Evaluate(pS);
            Vector3 lUp = pLayout.field.Normal(pS);
            float lHalf = pLayout.field.HalfWidth(pS);
            GameObject lObject = new GameObject("Hammer");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(pLayout.field.Point(pS, pLayout.field.CenterLateral(pS)) + lUp * (ARM + 6f), Quaternion.LookRotation(lSample.forward, lUp));
            Hammer lHammer = lObject.AddComponent<Hammer>();
            lHammer._phase = pPhase;
            lHammer._head = 15f;
            lHammer.cooldown = 1f;
            lHammer.halfExtents = new Vector3(lHalf + ARM, ARM + 20f, 24f);

            Color lFrame = Color.Lerp(pTheme.wall, Color.black, 0.25f);
            MeshData lData = new MeshData();
            GateKit.Box(lData, new Vector3(-lHalf - 6f, -ARM * 0.5f - 3f, 0f), new Vector3(6f, ARM + 12f, 6f), lFrame);
            GateKit.Box(lData, new Vector3(lHalf + 6f, -ARM * 0.5f - 3f, 0f), new Vector3(6f, ARM + 12f, 6f), lFrame);
            GateKit.Box(lData, new Vector3(0f, 3f, 0f), new Vector3(lHalf * 2f + 18f, 6f, 6f), lFrame);
            GameObject lBeam = new GameObject("Frame");
            lBeam.transform.SetParent(lObject.transform, false);
            lBeam.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("HammerFrame");
            lBeam.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(pTheme.shade, false);

            lHammer._pivot = new GameObject("Pivot").transform;
            lHammer._pivot.SetParent(lObject.transform, false);
            MeshData lSwing = new MeshData();
            GateKit.Box(lSwing, new Vector3(0f, -ARM * 0.5f, 0f), new Vector3(4f, ARM, 4f), lFrame);
            GateKit.Box(lSwing, new Vector3(0f, -ARM, 0f), new Vector3(30f, 24f, 24f), pTheme.accent);
            GateKit.Box(lSwing, new Vector3(0f, -ARM, 0f), new Vector3(32f, 7f, 26f), Color.Lerp(pTheme.accent, Color.white, 0.7f));
            GameObject lHead = new GameObject("Head");
            lHead.transform.SetParent(lHammer._pivot, false);
            lHead.AddComponent<MeshFilter>().sharedMesh = lSwing.ToMesh("Hammer");
            lHead.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(pTheme.shade, false);
            return lHammer;
        }

        private void Update()
        {
            if (_pivot != null) _pivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.1f + _phase) * 62f);
        }

        protected override void OnTouch(PlayerController pPlayer)
        {
            if (pPlayer.LightForm) return;
            Vector3 lHead = _pivot.TransformPoint(new Vector3(0f, -ARM, 0f));
            Vector3 lCenter = pPlayer.transform.position + pPlayer.Up;
            if ((lHead - lCenter).sqrMagnitude > _head * _head) return;
            pPlayer.Crash(lHead);
        }
    }
}
