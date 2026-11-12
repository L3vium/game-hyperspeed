using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // The world around the track, three layers deep : things rushing past close to the edge, a middle ground of big set
    // pieces, huge silhouettes far away, and clouds drifting under the track. Flat colours, one kit per place, no collider.
    public static class CourseScenery
    {
        private const float CHUNK = 700f;

        public static void Build(LevelLayout pLayout, Theme pTheme, Transform pRoot, int pSeed)
        {
            Transform lParent = new GameObject("Scenery").transform;
            lParent.SetParent(pRoot, false);
            System.Random lRandom = new System.Random(pSeed);
            Spine lSpine = pLayout.spine;
            Material lMaterial = Mats.Flat(pTheme.shade, false);
            Material lGlow = Mats.Glow(pTheme.accent * 2.8f, false, 0f, 0f, true);

            for (float lS0 = lSpine.StartS; lS0 < lSpine.EndS; lS0 += CHUNK)
            {
                // On light highways only one chunk in twelve gets scenery, and none of it is solid : at 20 km/s the road
                // has its own dressing (HighwayDressing), and 300 km of colliders would never be touched.
                bool lHighway = pLayout.InHighway(lS0);
                if (lHighway && Mathf.RoundToInt(lS0 / CHUNK) % 12 != 0) continue;
                MeshData lData = new MeshData();
                MeshData lLit = new MeshData();
                Vector3 lOrigin = lSpine.Evaluate(lS0).position;
                PropKit lKit = new PropKit(lData, lLit, pTheme, lRandom, lOrigin);

                Layer(pLayout, lKit, lRandom, lS0, 7, 16f, 60f, 8f, 40f, Depth.Near);
                Layer(pLayout, lKit, lRandom, lS0, 4, 70f, 180f, 60f, 200f, Depth.Middle);
                Layer(pLayout, lKit, lRandom, lS0, 2, 300f, 700f, 300f, 1100f, Depth.Far);
                Clouds(pLayout, lKit, lRandom, lS0);

                if (lData.Count == 0 && lLit.Count == 0) continue;
                Spawn(lParent, lOrigin, lData, lMaterial, "Scenery", !lHighway);
                if (lLit.Count > 0) Spawn(lParent, lOrigin, lLit, lGlow, "SceneryLit", !lHighway);
            }
            GroundSheet.Create(pRoot, pLayout, pTheme);
            SafetyNet.Create(pRoot, pLayout);
        }

        private enum Depth { Near, Middle, Far }

        private static void Spawn(Transform pParent, Vector3 pOrigin, MeshData pData, Material pMaterial, string pName, bool pSolid = true)
        {
            GameObject lObject = new GameObject(pName);
            lObject.transform.SetParent(pParent, false);
            lObject.transform.position = pOrigin;
            // Solid : anything you can see beside the track can catch you, and you bounce back from it.
            lObject.layer = Layers.SCENERY;
            Mesh lMesh = pData.ToMesh(pName);
            if (!pSolid)
            {
                lObject.AddComponent<MeshFilter>().sharedMesh = lMesh;
                lObject.AddComponent<MeshRenderer>().sharedMaterial = pMaterial;
                return;
            }
            // Collider first (it would otherwise cook the render mesh too), from a copy whose huge triangles are split.
            MeshCollider lCollider = lObject.AddComponent<MeshCollider>();
            System.Collections.Generic.List<Vector3> lVertices = new System.Collections.Generic.List<Vector3>(lMesh.vertices);
            System.Collections.Generic.List<int> lIndices = new System.Collections.Generic.List<int>(lMesh.triangles);
            TrackBaker.SubdivideLong(lVertices, lIndices, 380f);
            Mesh lCollision = new Mesh { name = pName + "Collision" };
            lCollision.indexFormat = lVertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            lCollision.SetVertices(lVertices);
            lCollision.triangles = lIndices.ToArray();
            lCollider.sharedMesh = lCollision;
            lObject.AddComponent<MeshFilter>().sharedMesh = lMesh;
            MeshRenderer lRenderer = lObject.AddComponent<MeshRenderer>();
            lRenderer.sharedMaterial = pMaterial;
            lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static float Rand(System.Random pRandom, float pMin, float pMax) => pMin + (float)pRandom.NextDouble() * (pMax - pMin);

        private static void Layer(LevelLayout pLayout, PropKit pKit, System.Random pRandom, float pS0, int pCount, float pSizeMin, float pSizeMax, float pGapMin, float pGapMax, Depth pDepth)
        {
            Spine lSpine = pLayout.spine;
            for (int i = 0; i < pCount; i++)
            {
                float lS = pS0 + (float)pRandom.NextDouble() * CHUNK;
                if (lS > lSpine.EndS) continue;
                SpineSample lSample = lSpine.Evaluate(lS);
                int lSide = pRandom.Next(0, 2) * 2 - 1;
                float lSize = Rand(pRandom, pSizeMin, pSizeMax);
                float lHalf = pLayout.field.TotalHalfWidth(lS);
                float lGap = Rand(pRandom, pGapMin, pGapMax);
                Vector3 lFlat = Vector3.ProjectOnPlane(lSample.forward, Vector3.up);
                Vector3 lRight = lFlat.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, lFlat.normalized) : Vector3.right;
                float lReach = lHalf + lSize * 0.55f + 45f + lGap;
                Vector3 lBase = lSample.position + lRight * lSide * lReach;
                // Near pieces stand up from below the track and come up beside it ; far ones rise from the floor of the world.
                float lDrop = pDepth == Depth.Near ? Rand(pRandom, lSize * 0.4f, lSize * 1.1f) : pDepth == Depth.Middle ? Rand(pRandom, 40f, 180f) : Rand(pRandom, 200f, 420f);
                lBase.y -= lDrop;
                if (!CourseValidator.IsClear(lBase + Vector3.up * lDrop, lSize * 0.6f)) continue;
                switch (pDepth)
                {
                    case Depth.Near: pKit.Near(lBase, lSize); break;
                    case Depth.Middle: pKit.Middle(lBase, lSize); break;
                    default: pKit.Far(lBase, lSize); break;
                }
            }
        }

        private static void Clouds(LevelLayout pLayout, PropKit pKit, System.Random pRandom, float pS0)
        {
            if (!pKit.HasClouds) return;
            Spine lSpine = pLayout.spine;
            for (int i = 0; i < 3; i++)
            {
                float lS = pS0 + (float)pRandom.NextDouble() * CHUNK;
                if (lS > lSpine.EndS) continue;
                SpineSample lSample = lSpine.Evaluate(lS);
                float lSize = Rand(pRandom, 60f, 240f);
                Vector3 lPosition = lSample.position + new Vector3(Rand(pRandom, -520f, 520f), Rand(pRandom, -240f, -90f), Rand(pRandom, -520f, 520f));
                if (!CourseValidator.IsClear(lPosition, lSize * 0.5f)) continue;
                pKit.Cloud(lPosition, lSize);
            }
        }
    }

    // A huge flat sheet far under the track, following the runner : the floor of the world under its fog. It always
    // stays under the lowest piece of track around (behind and far ahead), so the bottom of a loop or a platform far
    // below is never hidden by it.
    public class GroundSheet : MonoBehaviour
    {
        public const float BEHIND = 600f;
        public const float AHEAD = 9000f;
        public const float UNDER = 620f;

        private LevelLayout _layout;
        private float _y = float.NaN;

        public static void Create(Transform pParent, LevelLayout pLayout, Theme pTheme)
        {
            if (pTheme.decor == DecorKind.Space || pTheme.decor == DecorKind.Void) return;
            GameObject lObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DestroyImmediate(lObject.GetComponent<Collider>());
            lObject.name = "GroundSheet";
            lObject.transform.SetParent(pParent, false);
            lObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            lObject.transform.localScale = Vector3.one * 30000f;
            lObject.layer = Layers.DEBRIS;
            lObject.GetComponent<Renderer>().sharedMaterial = Mats.Flat(pTheme.shade, false, Color.Lerp(pTheme.groundC, pTheme.fog, 0.45f));
            lObject.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GroundSheet lSheet = lObject.AddComponent<GroundSheet>();
            lSheet._layout = pLayout;
        }

        private void LateUpdate()
        {
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null) return;
            Vector3 lAt = lPlayer.transform.position;
            float lP = lPlayer.Progress;
            float lTarget = _layout.LowestBetween(lP - BEHIND, lP + AHEAD) - UNDER;
            // Down at once (something low is coming), back up slowly (never a visible jump of the horizon).
            if (float.IsNaN(_y) || lTarget < _y) _y = lTarget;
            else _y = Mathf.MoveTowards(_y, lTarget, 40f * Time.deltaTime);
            transform.position = new Vector3(lAt.x, _y, lAt.z);
        }

        public float Height => _y;
    }

    // Invisible floor a little under the track, following the runner : falling is (almost) never the end. Touching it
    // throws you back onto the track ahead (PlayerController.Recover).
    public class SafetyNet : MonoBehaviour
    {
        private const float DEPTH = 190f;

        private LevelLayout _layout;

        public static void Create(Transform pParent, LevelLayout pLayout)
        {
            GameObject lObject = new GameObject("SafetyNet");
            lObject.transform.SetParent(pParent, false);
            lObject.layer = Layers.SCENERY;
            BoxCollider lBox = lObject.AddComponent<BoxCollider>();
            lBox.size = new Vector3(9000f, 20f, 9000f);
            lBox.center = new Vector3(0f, -10f, 0f);
            SafetyNet lNet = lObject.AddComponent<SafetyNet>();
            lNet._layout = pLayout;
        }

        private void LateUpdate()
        {
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null || _layout == null) return;
            Vector3 lAt = lPlayer.transform.position;
            float lS = lPlayer.Progress;
            // Below the lowest of here and a bit ahead, so a dive onto a lower block is never caught early.
            float lFloor = Mathf.Min(_layout.field.Point(lS, _layout.field.CenterLateral(lS)).y, _layout.field.Point(lS + 300f, _layout.field.CenterLateral(lS + 300f)).y);
            // Inside a loop or a twist, the bottom of the whole section counts : falling from its top is not a fall out.
            lFloor = Mathf.Min(lFloor, _layout.LowestOfSection(lS), _layout.LowestBetween(lS - 50f, lS + 300f));
            transform.position = new Vector3(lAt.x, lFloor - DEPTH, lAt.z);
        }
    }

    // One piece of scenery at a time, merged in the chunk's meshes.
    public class PropKit
    {
        private readonly MeshData _d;
        private readonly MeshData _g;
        private readonly Theme _t;
        private readonly System.Random _r;
        private readonly Vector3 _origin;

        public PropKit(MeshData pData, MeshData pGlow, Theme pTheme, System.Random pRandom, Vector3 pOrigin)
        {
            _d = pData;
            _g = pGlow;
            _t = pTheme;
            _r = pRandom;
            _origin = pOrigin;
        }

        public bool HasClouds => _t.decor != DecorKind.Space && _t.decor != DecorKind.Industrial && _t.decor != DecorKind.Night;

        private float R(float pMin, float pMax) => pMin + (float)_r.NextDouble() * (pMax - pMin);

        private Matrix4x4 At(Vector3 pPosition, float pTilt = 0f) => Matrix4x4.TRS(pPosition - _origin, Quaternion.Euler(R(-pTilt, pTilt), R(0f, 360f), R(-pTilt, pTilt)), Vector3.one);

        // Tapered box in the frame pM : base centred at pAt, top pushed by pShift.
        private void Slab(MeshData pData, Matrix4x4 pM, Vector3 pAt, float pW, float pH, float pD, float pTop, Color pColor, Vector3 pShift = default(Vector3))
        {
            Vector3 lA = pM.MultiplyPoint3x4(pAt + new Vector3(-pW * 0.5f, 0f, -pD * 0.5f));
            Vector3 lB = pM.MultiplyPoint3x4(pAt + new Vector3(pW * 0.5f, 0f, -pD * 0.5f));
            Vector3 lC = pM.MultiplyPoint3x4(pAt + new Vector3(pW * 0.5f, 0f, pD * 0.5f));
            Vector3 lE = pM.MultiplyPoint3x4(pAt + new Vector3(-pW * 0.5f, 0f, pD * 0.5f));
            Vector3 lTop = pAt + Vector3.up * pH + pShift;
            Vector3 lF = pM.MultiplyPoint3x4(lTop + new Vector3(-pW * 0.5f * pTop, 0f, -pD * 0.5f * pTop));
            Vector3 lG = pM.MultiplyPoint3x4(lTop + new Vector3(pW * 0.5f * pTop, 0f, -pD * 0.5f * pTop));
            Vector3 lH = pM.MultiplyPoint3x4(lTop + new Vector3(pW * 0.5f * pTop, 0f, pD * 0.5f * pTop));
            Vector3 lK = pM.MultiplyPoint3x4(lTop + new Vector3(-pW * 0.5f * pTop, 0f, pD * 0.5f * pTop));
            Vector3 lCenter = (lA + lB + lC + lE + lF + lG + lH + lK) * 0.125f;
            Color lDark = Color.Lerp(pColor, _t.shade, 0.25f);
            pData.FacetQuad(lF, lG, lH, lK, (lF + lH) * 0.5f - lCenter, Color.Lerp(pColor, Color.white, 0.12f));
            pData.FacetQuad(lA, lB, lG, lF, (lA + lG) * 0.5f - lCenter, pColor);
            pData.FacetQuad(lB, lC, lH, lG, (lB + lH) * 0.5f - lCenter, lDark);
            pData.FacetQuad(lC, lE, lK, lH, (lC + lK) * 0.5f - lCenter, pColor);
            pData.FacetQuad(lE, lA, lF, lK, (lE + lK) * 0.5f - lCenter, lDark);
        }

        private void Rock(Matrix4x4 pM, Vector3 pAt, Vector3 pScale, Color pColor)
        {
            _d.Append(RockSculptor.Boulder(_r.Next(0, 6)), pM * Matrix4x4.TRS(pAt, Quaternion.Euler(0f, R(0f, 360f), 0f), pScale), pColor);
        }

        private void Spike(Matrix4x4 pM, Vector3 pAt, Vector3 pScale, Color pColor, float pLean = 12f)
        {
            _d.Append(RockSculptor.Shard(_r.Next(0, 6)), pM * Matrix4x4.TRS(pAt, Quaternion.Euler(R(-pLean, pLean), R(0f, 360f), R(-pLean, pLean)), pScale), pColor);
        }

        private Color Band(int pIndex)
        {
            switch (pIndex % 5)
            {
                case 0: return _t.roadSide;
                case 1: return _t.wall;
                case 2: return Color.Lerp(_t.roadSide, Color.black, 0.3f);
                case 3: return _t.decorB;
                default: return Color.Lerp(_t.wall, Color.white, 0.2f);
            }
        }

        // ---------------------------------------------------------------- pieces

        // A block of layered rock : strata all the way down, a cap of the local ground on top.
        public void Strata(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt, 6f);
            float lW = pSize * R(0.7f, 1.1f);
            float lD = pSize * R(0.7f, 1.1f);
            float lY = 0f;
            int lLayers = _r.Next(4, 7);
            for (int i = 0; i < lLayers; i++)
            {
                float lH = pSize * R(0.12f, 0.26f);
                float lTop = R(0.92f, 1.04f);
                Slab(_d, lM, new Vector3(R(-0.03f, 0.03f) * pSize, lY, R(-0.03f, 0.03f) * pSize), lW, lH, lD, lTop, Band(i + _r.Next(0, 2)));
                lY += lH;
                lW *= lTop;
                lD *= lTop;
            }
            Slab(_d, lM, new Vector3(0f, lY, 0f), lW * 1.06f, pSize * 0.05f, lD * 1.06f, 1f, _t.roadA);
        }

        // Thin striped trunk, leaning, carrying flat round canopies.
        public void CanopyTree(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt, 10f);
            float lY = 0f;
            float lThick = pSize * 0.05f;
            Vector3 lDrift = new Vector3(R(-0.06f, 0.06f), 0f, R(-0.06f, 0.06f)) * pSize;
            Vector3 lPos = Vector3.zero;
            for (int i = 0; i < 6; i++)
            {
                float lH = pSize * 0.15f;
                Slab(_d, lM, lPos + Vector3.up * lY, lThick, lH, lThick, 0.92f, i % 2 == 0 ? _t.wall : Color.Lerp(_t.roadSide, Color.black, 0.45f), lDrift * 0.15f);
                lPos += lDrift * 0.15f;
                lY += lH;
                lThick *= 0.92f;
            }
            Vector3 lTop = lPos + Vector3.up * lY;
            for (int i = 0; i < 4; i++)
            {
                Vector3 lOff = new Vector3(R(-0.34f, 0.34f), R(-0.03f, 0.12f), R(-0.34f, 0.34f)) * pSize;
                Color lLeaf = i % 2 == 0 ? _t.decorA : Color.Lerp(_t.decorA, _t.decorB, 0.6f);
                Rock(lM, lTop + lOff - Vector3.up * pSize * 0.08f, new Vector3(pSize * R(0.32f, 0.5f), pSize * 0.17f, pSize * R(0.32f, 0.5f)), lLeaf);
            }
        }

        // Big bands around a peak.
        public void BandedPeak(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt, 7f);
            float lY = 0f;
            float lW = pSize;
            int lLayers = _r.Next(5, 9);
            Vector3 lShift = new Vector3(R(-0.05f, 0.05f), 0f, R(-0.05f, 0.05f)) * pSize;
            Vector3 lPos = Vector3.zero;
            for (int i = 0; i < lLayers; i++)
            {
                float lH = pSize * 1.6f / lLayers;
                float lTop = i == lLayers - 1 ? 0.05f : 0.82f;
                Slab(_d, lM, lPos + Vector3.up * lY, lW, lH, lW * 0.9f, lTop, Color.Lerp(Band(i), _t.fog, 0.15f), lShift);
                lPos += lShift;
                lY += lH;
                lW *= lTop;
            }
        }

        public void Cloud(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = Matrix4x4.TRS(pAt - _origin, Quaternion.Euler(0f, R(0f, 360f), 0f), Vector3.one);
            Color lColor = Color.Lerp(_t.cloud, Color.white, 0.3f);
            int lPuffs = _r.Next(3, 6);
            for (int i = 0; i < lPuffs; i++)
                Rock(lM, new Vector3(R(-0.6f, 0.6f), R(-0.04f, 0.04f), R(-0.4f, 0.4f)) * pSize, new Vector3(pSize * R(0.5f, 0.9f), pSize * R(0.14f, 0.26f), pSize * R(0.4f, 0.7f)), lColor);
        }

        public void Tower(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt);
            float lW = pSize * R(0.32f, 0.5f);
            float lH = pSize * R(1.6f, 3f);
            Color lBody = _r.Next(0, 3) == 0 ? _t.decorC : _t.wall;
            Slab(_d, lM, Vector3.zero, lW, lH * 0.6f, lW, 0.92f, lBody);
            Slab(_d, lM, Vector3.up * lH * 0.6f, lW * 0.8f, lH * 0.4f, lW * 0.8f, 0.86f, Color.Lerp(lBody, Color.white, 0.14f));
            Slab(_d, lM, Vector3.up * lH, lW * 0.2f, lH * 0.14f, lW * 0.2f, 0.2f, _t.roadSide);
            // Lit floors on two sides.
            int lFloors = Mathf.Clamp(Mathf.RoundToInt(lH / 26f), 3, 14);
            for (int i = 1; i < lFloors; i++)
            {
                float lY = lH * 0.6f * i / lFloors;
                GlowBar(lM, new Vector3(0f, lY, -lW * 0.47f), new Vector3(lW * 0.75f, pSize * 0.016f + 0.6f, 0.4f));
                GlowBar(lM, new Vector3(lW * 0.47f, lY, 0f), new Vector3(0.4f, pSize * 0.016f + 0.6f, lW * 0.75f));
            }
        }

        private void GlowBar(Matrix4x4 pM, Vector3 pCenter, Vector3 pSize)
        {
            Slab(_g, pM, pCenter - Vector3.up * pSize.y * 0.5f, pSize.x, pSize.y, pSize.z, 1f, Color.white);
        }

        // Tall board on legs : a huge flat colour, passing fast.
        public void Billboard(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt);
            float lW = pSize * 1.2f;
            float lH = pSize * 0.6f;
            Slab(_d, lM, Vector3.zero, pSize * 0.12f, pSize, pSize * 0.12f, 1f, _t.roadSide);
            Slab(_d, lM, Vector3.up * pSize, lW, lH, pSize * 0.05f, 1f, _t.decorA);
            Slab(_d, lM, Vector3.up * (pSize + lH * 0.15f), lW * 0.75f, lH * 0.16f, pSize * 0.055f, 1f, _t.decorB);
            Slab(_d, lM, Vector3.up * (pSize + lH * 0.5f), lW * 0.5f, lH * 0.26f, pSize * 0.055f, 1f, Color.white);
        }

        public void Cactus(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt, 5f);
            float lT = pSize * 0.14f;
            Color lColor = _t.decorA;
            Slab(_d, lM, Vector3.zero, lT, pSize, lT, 0.8f, lColor);
            for (int lSide = -1; lSide <= 1; lSide += 2)
            {
                float lY = pSize * R(0.3f, 0.55f);
                float lOut = pSize * R(0.2f, 0.32f);
                Slab(_d, lM, new Vector3(lSide * lOut * 0.5f, lY, 0f), lOut, lT * 0.7f, lT * 0.7f, 1f, lColor);
                Slab(_d, lM, new Vector3(lSide * lOut, lY, 0f), lT * 0.7f, pSize * R(0.25f, 0.4f), lT * 0.7f, 0.8f, lColor);
            }
        }

        // Wall section with a burning arch and a dome.
        public void Fortress(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt);
            float lW = pSize * 1.8f;
            float lH = pSize * 0.8f;
            Color lDark = Color.Lerp(_t.decorB, Color.black, 0.35f);
            Slab(_d, lM, Vector3.zero, lW, lH, pSize * 0.3f, 0.92f, lDark);
            Slab(_g, lM, new Vector3(0f, 0f, -pSize * 0.16f), pSize * 0.24f, lH * 0.62f, 0.6f, 0.2f, Color.white);
            Slab(_d, lM, new Vector3(lW * 0.3f, lH, 0f), pSize * 0.3f, pSize * 0.3f, pSize * 0.3f, 0.7f, _t.decorC);
            Rock(lM, new Vector3(-lW * 0.25f, lH * 0.85f, 0f), new Vector3(pSize * 0.5f, pSize * 0.36f, pSize * 0.5f), lDark);
            // A beam thrown up into the sky.
            Vector3 lTip = new Vector3(R(-0.6f, 0.6f), 4f, R(-0.6f, 0.6f)) * pSize;
            Slab(_g, lM, new Vector3(lW * 0.3f, lH + pSize * 0.3f, 0f), pSize * 0.03f, pSize * 4f, pSize * 0.03f, 0.3f, Color.white, lTip * 0.25f);
        }

        public void Crystals(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt, 8f);
            int lCount = _r.Next(3, 7);
            for (int i = 0; i < lCount; i++)
            {
                Vector3 lOff = new Vector3(R(-0.4f, 0.4f), 0f, R(-0.4f, 0.4f)) * pSize;
                float lS = pSize * R(0.14f, 0.34f);
                Color lColor = i % 3 == 0 ? _t.decorC : i % 3 == 1 ? _t.decorA : _t.decorB;
                Spike(lM, lOff, new Vector3(lS, lS * R(0.9f, 1.6f), lS), lColor, 22f);
            }
        }

        // Chimney with a fire at the top, rings around it.
        public void Stack(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt);
            float lW = pSize * 0.3f;
            float lH = pSize * R(1.1f, 1.9f);
            Slab(_d, lM, Vector3.zero, lW, lH, lW, 0.66f, _t.decorA);
            for (int i = 1; i <= 3; i++)
                Slab(_d, lM, Vector3.up * lH * i * 0.25f, lW * (1f - 0.09f * i), pSize * 0.03f, lW * (1f - 0.09f * i), 1f, _t.roadEdge);
            Slab(_g, lM, Vector3.up * lH, lW * 0.5f, pSize * 0.14f, lW * 0.5f, 0.15f, Color.white);
            Slab(_d, lM, new Vector3(lW * 1.2f, lH * 0.22f, 0f), pSize * 0.8f, pSize * 0.06f, pSize * 0.06f, 1f, _t.decorC);
        }

        // Huge trunk with hanging rings and a canopy lost above.
        public void GiantTree(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt, 4f);
            float lW = pSize * 0.24f;
            float lH = pSize * 2.2f;
            Slab(_d, lM, Vector3.zero, lW, lH, lW, 0.55f, _t.decorC);
            for (int i = 1; i <= 3; i++)
            {
                float lY = lH * i * 0.24f;
                float lPad = lW * R(1.5f, 2.3f);
                Slab(_d, lM, Vector3.up * lY, lPad, pSize * 0.05f, lPad, 0.85f, _t.roadEdge);
            }
            for (int i = 0; i < 4; i++)
                Rock(lM, Vector3.up * lH + new Vector3(R(-0.6f, 0.6f), R(-0.12f, 0.2f), R(-0.6f, 0.6f)) * pSize, new Vector3(pSize * 0.7f, pSize * 0.32f, pSize * 0.7f), i % 2 == 0 ? _t.decorA : _t.decorB);
        }

        public void CastleTower(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt);
            float lW = pSize * 0.34f;
            float lH = pSize * 1.6f;
            Color lBody = _t.wall;
            Slab(_d, lM, Vector3.zero, lW, lH, lW, 0.96f, lBody);
            Slab(_d, lM, Vector3.up * lH, lW * 1.2f, pSize * 0.1f, lW * 1.2f, 1f, Color.Lerp(lBody, Color.white, 0.25f));
            Slab(_d, lM, Vector3.up * (lH + pSize * 0.1f), lW, pSize * 0.45f, lW, 0.05f, _t.decorC);
            for (int i = 1; i <= 4; i++)
                Slab(_d, lM, new Vector3(0f, lH * i * 0.19f, -lW * 0.5f), lW * 0.14f, pSize * 0.1f, 0.4f, 1f, Color.Lerp(_t.roadSide, Color.black, 0.3f));
            Slab(_d, lM, new Vector3(lW * 0.5f, lH * 0.65f, 0f), 0.4f, pSize * 0.3f, lW * 0.3f, 1f, _t.accent);
        }

        // Floating broken rock.
        public void Asteroid(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt + Vector3.up * pSize * R(0.4f, 1.6f), 40f);
            Color lColor = _r.Next(0, 2) == 0 ? _t.decorA : _t.decorC;
            Rock(lM, Vector3.zero, new Vector3(pSize * R(0.5f, 0.9f), pSize * R(0.4f, 0.8f), pSize * R(0.5f, 0.9f)), lColor);
            if (_r.Next(0, 3) == 0) GlowBar(lM, new Vector3(0f, pSize * 0.4f, 0f), new Vector3(pSize * 1.4f, 0.8f, 0.8f));
        }

        // Wide ring of light standing in the distance.
        public void Halo(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = Matrix4x4.TRS(pAt - _origin + Vector3.up * pSize, Quaternion.Euler(R(60f, 110f), R(0f, 360f), 0f), Vector3.one);
            const int SEGMENTS = 22;
            float lThick = pSize * 0.015f + 1f;
            for (int i = 0; i < SEGMENTS; i++)
            {
                float lA0 = i * Mathf.PI * 2f / SEGMENTS;
                float lA1 = (i + 1) * Mathf.PI * 2f / SEGMENTS;
                Vector3 lP0 = lM.MultiplyPoint3x4(new Vector3(Mathf.Cos(lA0), 0f, Mathf.Sin(lA0)) * pSize);
                Vector3 lP1 = lM.MultiplyPoint3x4(new Vector3(Mathf.Cos(lA1), 0f, Mathf.Sin(lA1)) * pSize);
                Vector3 lUp = lM.MultiplyVector(Vector3.up);
                _g.FacetQuad(lP0 - lUp * lThick, lP1 - lUp * lThick, lP1 + lUp * lThick, lP0 + lUp * lThick, Vector3.Cross(lP1 - lP0, lUp), Color.white);
                _g.FacetQuad(lP0 - lUp * lThick, lP1 - lUp * lThick, lP1 + lUp * lThick, lP0 + lUp * lThick, -Vector3.Cross(lP1 - lP0, lUp), Color.white);
            }
        }

        public void DeadTree(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt, 12f);
            Color lColor = _t.decorA;
            Spike(lM, Vector3.zero, new Vector3(pSize * 0.1f, pSize * 0.5f, pSize * 0.1f), lColor, 6f);
            for (int i = 0; i < 5; i++)
                Spike(lM, Vector3.up * pSize * R(0.3f, 0.9f), new Vector3(pSize * 0.04f, pSize * R(0.14f, 0.28f), pSize * 0.04f), lColor, 60f);
        }

        // Dark blade of rock with a glowing heart at its foot.
        public void CanyonRock(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt, 8f);
            Slab(_d, lM, Vector3.zero, pSize * 0.6f, pSize * 1.2f, pSize * 0.5f, 0.32f, _t.decorA, new Vector3(R(-0.1f, 0.1f), 0f, R(-0.1f, 0.1f)) * pSize);
            Slab(_d, lM, new Vector3(pSize * 0.3f, 0f, pSize * 0.1f), pSize * 0.3f, pSize * 0.6f, pSize * 0.3f, 0.4f, _t.decorC);
            Spike(lM, new Vector3(-pSize * 0.35f, 0f, -pSize * 0.2f), Vector3.one * pSize * 0.12f, Color.Lerp(_t.accent, Color.white, 0.5f), 10f);
            GlowBar(lM, new Vector3(-pSize * 0.35f, pSize * 0.1f, -pSize * 0.2f), Vector3.one * pSize * 0.07f);
        }

        // A volcano : banded slopes, a glowing crater, lava running down its sides, a plume of fire above.
        public void VolcanoCone(Vector3 pAt, float pSize)
        {
            Matrix4x4 lM = At(pAt, 3f);
            float lY = 0f;
            float lW = pSize * 1.4f;
            int lLayers = 5;
            for (int i = 0; i < lLayers; i++)
            {
                float lH = pSize * 1.1f / lLayers;
                float lTop = i == lLayers - 1 ? 0.55f : 0.8f;
                Slab(_d, lM, Vector3.up * lY, lW, lH, lW, lTop, i % 2 == 0 ? _t.decorA : _t.decorC);
                lY += lH;
                lW *= lTop;
            }
            Slab(_g, lM, Vector3.up * lY, lW * 0.7f, pSize * 0.04f, lW * 0.7f, 1f, Color.white);
            for (int i = 0; i < 3; i++)
            {
                float lAngle = R(0f, 360f) * Mathf.Deg2Rad;
                Vector3 lDir = new Vector3(Mathf.Cos(lAngle), 0f, Mathf.Sin(lAngle));
                Slab(_g, lM, lDir * pSize * 0.35f, pSize * 0.05f, lY * 0.9f, pSize * 0.05f, 0.4f, Color.white, -lDir * pSize * 0.3f);
            }
            Slab(_g, lM, Vector3.up * lY, pSize * 0.06f, pSize * 1.6f, pSize * 0.06f, 2.5f, Color.white, new Vector3(R(-0.2f, 0.2f), 0f, R(-0.2f, 0.2f)) * pSize);
        }

        // ---------------------------------------------------------------- what each place grows

        private int Pick(int pCount) => _r.Next(0, pCount);

        public void Near(Vector3 pAt, float pSize)
        {
            switch (_t.decor)
            {
                case DecorKind.City: if (Pick(3) == 0) Billboard(pAt, pSize); else Tower(pAt, pSize * 0.8f); break;
                case DecorKind.Desert: if (Pick(2) == 0) Cactus(pAt, pSize * 1.4f); else Strata(pAt, pSize); break;
                case DecorKind.Industrial: if (Pick(2) == 0) Stack(pAt, pSize); else Strata(pAt, pSize); break;
                case DecorKind.Volcano: if (Pick(3) == 0) VolcanoCone(pAt, pSize * 0.8f); else CanyonRock(pAt, pSize); break;
                case DecorKind.Ice: Crystals(pAt, pSize * 1.2f); break;
                case DecorKind.Space:
                case DecorKind.Void: Asteroid(pAt, pSize); break;
                case DecorKind.Night: if (Pick(2) == 0) DeadTree(pAt, pSize * 1.4f); else CanyonRock(pAt, pSize); break;
                case DecorKind.Prehistoric:
                case DecorKind.Forest: if (Pick(2) == 0) CanopyTree(pAt, pSize * 1.5f); else Strata(pAt, pSize); break;
                case DecorKind.Canyon: CanyonRock(pAt, pSize); break;
                case DecorKind.Castle: if (Pick(2) == 0) CastleTower(pAt, pSize); else Strata(pAt, pSize * 0.7f); break;
                case DecorKind.Neon: if (Pick(2) == 0) Tower(pAt, pSize * 0.7f); else Asteroid(pAt, pSize * 0.7f); break;
                default: if (Pick(2) == 0) CanopyTree(pAt, pSize * 1.6f); else Strata(pAt, pSize); break;
            }
        }

        public void Middle(Vector3 pAt, float pSize)
        {
            switch (_t.decor)
            {
                case DecorKind.City: Tower(pAt, pSize); break;
                case DecorKind.Desert: if (Pick(2) == 0) Fortress(pAt, pSize); else Cactus(pAt, pSize * 1.3f); break;
                case DecorKind.Industrial: Stack(pAt, pSize); break;
                case DecorKind.Volcano: VolcanoCone(pAt, pSize); break;
                case DecorKind.Ice: Crystals(pAt, pSize * 1.3f); break;
                case DecorKind.Space:
                case DecorKind.Void: if (Pick(3) == 0) Halo(pAt, pSize * 0.8f); else Asteroid(pAt, pSize); break;
                case DecorKind.Night: if (Pick(2) == 0) DeadTree(pAt, pSize * 1.3f); else Fortress(pAt, pSize); break;
                case DecorKind.Prehistoric:
                case DecorKind.Forest: GiantTree(pAt, pSize); break;
                case DecorKind.Canyon: if (Pick(2) == 0) Fortress(pAt, pSize); else CanyonRock(pAt, pSize); break;
                case DecorKind.Castle: CastleTower(pAt, pSize); break;
                case DecorKind.Neon: if (Pick(3) == 0) Halo(pAt, pSize); else Tower(pAt, pSize); break;
                default: if (Pick(2) == 0) Strata(pAt, pSize); else CanopyTree(pAt, pSize * 1.4f); break;
            }
        }

        public void Far(Vector3 pAt, float pSize)
        {
            switch (_t.decor)
            {
                case DecorKind.City:
                case DecorKind.Neon: Tower(pAt, pSize); break;
                case DecorKind.Space:
                case DecorKind.Void: if (Pick(2) == 0) Halo(pAt, pSize); else Asteroid(pAt, pSize); break;
                case DecorKind.Ice: Crystals(pAt, pSize * 1.4f); break;
                case DecorKind.Volcano: VolcanoCone(pAt, pSize); break;
                case DecorKind.Prehistoric:
                case DecorKind.Forest: GiantTree(pAt, pSize * 0.7f); break;
                case DecorKind.Castle: CastleTower(pAt, pSize * 0.7f); break;
                case DecorKind.Canyon:
                case DecorKind.Night: CanyonRock(pAt, pSize); break;
                default: BandedPeak(pAt, pSize); break;
            }
        }
    }
}
