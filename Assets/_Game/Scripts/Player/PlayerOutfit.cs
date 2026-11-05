using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // The runner's silhouette : big hard shapes bolted on the rig, readable from far behind at any speed. A pointed helmet
    // with a tall swept crest, huge angled shoulder plates, a hard chest plate, a backpack with two burning nozzles,
    // gauntlets and heavy boots with glowing soles.
    public static class PlayerOutfit
    {
        private static readonly Color RED = new Color(0.86f, 0.05f, 0.09f);
        private static readonly Color WHITE = new Color(0.95f, 0.95f, 0.97f);
        private static readonly Color BLACK = new Color(0.07f, 0.06f, 0.1f);
        private static readonly Color GOLD = new Color(1f, 0.76f, 0.1f);
        private static readonly Color GLOW = new Color(0.4f, 2.6f, 3.2f);

        // Local axes of a piece : x right, y up, z forward of the runner at bind pose.
        private static void Prism(MeshData pData, Vector3 pCenter, Vector3 pBottom, Vector3 pTop, Vector3 pTopShift, Color pColor)
        {
            Vector3 lB = pBottom * 0.5f;
            Vector3 lT = pTop * 0.5f;
            float lH = pBottom.y * 0.5f;
            Vector3 lA0 = pCenter + new Vector3(-lB.x, -lH, -lB.z);
            Vector3 lA1 = pCenter + new Vector3(lB.x, -lH, -lB.z);
            Vector3 lA2 = pCenter + new Vector3(lB.x, -lH, lB.z);
            Vector3 lA3 = pCenter + new Vector3(-lB.x, -lH, lB.z);
            Vector3 lC = pCenter + pTopShift + Vector3.up * lH;
            Vector3 lB0 = lC + new Vector3(-lT.x, 0f, -lT.z);
            Vector3 lB1 = lC + new Vector3(lT.x, 0f, -lT.z);
            Vector3 lB2 = lC + new Vector3(lT.x, 0f, lT.z);
            Vector3 lB3 = lC + new Vector3(-lT.x, 0f, lT.z);
            Color lDark = Color.Lerp(pColor, Color.black, 0.22f);
            pData.FacetQuad(lB0, lB1, lB2, lB3, Vector3.up, Color.Lerp(pColor, Color.white, 0.14f));
            pData.FacetQuad(lA0, lA1, lA2, lA3, Vector3.down, lDark);
            pData.FacetQuad(lA0, lA1, lB1, lB0, Vector3.back, lDark);
            pData.FacetQuad(lA2, lA3, lB3, lB2, Vector3.forward, pColor);
            pData.FacetQuad(lA1, lA2, lB2, lB1, Vector3.right, pColor);
            pData.FacetQuad(lA3, lA0, lB0, lB3, Vector3.left, pColor);
        }

        // Flat blade : a triangle with some thickness.
        private static void Blade(MeshData pData, Vector3 pA, Vector3 pB, Vector3 pC, float pThick, Color pColor)
        {
            Vector3 lNormal = Vector3.Cross(pB - pA, pC - pA).normalized * pThick;
            pData.FacetTri(pA + lNormal, pB + lNormal, pC + lNormal, lNormal, pColor);
            pData.FacetTri(pA - lNormal, pB - lNormal, pC - lNormal, -lNormal, Color.Lerp(pColor, Color.black, 0.2f));
            pData.FacetQuad(pA + lNormal, pB + lNormal, pB - lNormal, pA - lNormal, (pA + pB) * 0.5f - (pA + pB + pC) / 3f, pColor);
            pData.FacetQuad(pB + lNormal, pC + lNormal, pC - lNormal, pB - lNormal, (pB + pC) * 0.5f - (pA + pB + pC) / 3f, Color.Lerp(pColor, Color.white, 0.2f));
            pData.FacetQuad(pC + lNormal, pA + lNormal, pA - lNormal, pC - lNormal, (pC + pA) * 0.5f - (pA + pB + pC) / 3f, pColor);
        }

        // Builds at bind pose in the runner's frame, then rides the bone.
        private static void Attach(Transform pRoot, Transform pBone, Vector3 pAt, MeshData pData, MeshData pGlow, Material pMaterial, Material pGlowMaterial)
        {
            if (pBone == null) return;
            GameObject lPiece = new GameObject("Plate");
            lPiece.transform.SetPositionAndRotation(pRoot.TransformPoint(pAt), pRoot.rotation);
            lPiece.AddComponent<MeshFilter>().sharedMesh = pData.ToMesh("OutfitPlate");
            MeshRenderer lRenderer = lPiece.AddComponent<MeshRenderer>();
            lRenderer.sharedMaterial = pMaterial;
            lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            lPiece.transform.SetParent(pBone, true);
            if (pGlow == null) return;
            GameObject lGlow = new GameObject("Glow");
            lGlow.transform.SetParent(lPiece.transform, false);
            lGlow.AddComponent<MeshFilter>().sharedMesh = pGlow.ToMesh("OutfitGlow");
            lGlow.AddComponent<MeshRenderer>().sharedMaterial = pGlowMaterial;
        }

        public static void Dress(Transform pRoot, Animator pAnimator)
        {
            Material lMaterial = Mats.Toon(Color.white, new Color(0.62f, 0.56f, 0.82f), 0.02f, 0.5f, true);
            Material lGlowMaterial = Mats.Glow(GLOW, false, 0f, 0f, true);
            Material lHeat = Mats.Glow(new Color(3.6f, 1.6f, 0.3f), false, 0f, 0f, true);

            Transform lHead = pAnimator.GetBoneTransform(HumanBodyBones.Head);
            Transform lNeck = pAnimator.GetBoneTransform(HumanBodyBones.Neck);
            Transform lChest = pAnimator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (lChest == null) lChest = pAnimator.GetBoneTransform(HumanBodyBones.Chest);
            Transform lHips = pAnimator.GetBoneTransform(HumanBodyBones.Hips);

            // ---------------- helmet
            if (lHead != null && lNeck != null)
            {
                Vector3 lAt = pRoot.InverseTransformPoint(lHead.position) + Vector3.up * 0.16f + Vector3.forward * 0.02f;
                MeshData lHelmet = new MeshData();
                Prism(lHelmet, Vector3.zero, new Vector3(0.44f, 0.4f, 0.5f), new Vector3(0.3f, 0f, 0.3f), new Vector3(0f, 0f, -0.05f), WHITE);
                Prism(lHelmet, new Vector3(0f, -0.26f, 0.02f), new Vector3(0.42f, 0.14f, 0.46f), new Vector3(0.44f, 0f, 0.5f), Vector3.zero, BLACK);
                // Tall crest sweeping back : the first thing you see from behind.
                Blade(lHelmet, new Vector3(0f, 0.2f, 0.2f), new Vector3(0f, 0.72f, -0.62f), new Vector3(0f, 0.1f, -0.26f), 0.035f, RED);
                // Fins on both sides.
                Blade(lHelmet, new Vector3(0.22f, 0.08f, 0.12f), new Vector3(0.5f, 0.3f, -0.42f), new Vector3(0.22f, -0.1f, -0.16f), 0.025f, GOLD);
                Blade(lHelmet, new Vector3(-0.22f, 0.08f, 0.12f), new Vector3(-0.5f, 0.3f, -0.42f), new Vector3(-0.22f, -0.1f, -0.16f), 0.025f, GOLD);
                MeshData lVisor = new MeshData();
                GateKit.Box(lVisor, new Vector3(0f, 0.02f, 0.25f), new Vector3(0.4f, 0.09f, 0.03f), Color.white);
                Attach(pRoot, lHead, lAt, lHelmet, lVisor, lMaterial, lGlowMaterial);
            }

            // ---------------- chest, shoulders, backpack
            if (lChest != null)
            {
                Vector3 lAt = pRoot.InverseTransformPoint(lChest.position);
                MeshData lTorso = new MeshData();
                // V-shaped chest plate.
                Prism(lTorso, new Vector3(0f, 0.04f, 0.1f), new Vector3(0.4f, 0.5f, 0.2f), new Vector3(0.66f, 0f, 0.24f), new Vector3(0f, 0f, 0.02f), RED);
                Blade(lTorso, new Vector3(-0.2f, 0.3f, 0.22f), new Vector3(0.2f, 0.3f, 0.22f), new Vector3(0f, -0.14f, 0.22f), 0.012f, WHITE);
                // Backpack.
                Prism(lTorso, new Vector3(0f, 0.08f, -0.26f), new Vector3(0.46f, 0.56f, 0.26f), new Vector3(0.4f, 0f, 0.2f), Vector3.zero, BLACK);
                Prism(lTorso, new Vector3(-0.19f, -0.1f, -0.36f), new Vector3(0.18f, 0.34f, 0.18f), new Vector3(0.14f, 0f, 0.14f), Vector3.zero, WHITE);
                Prism(lTorso, new Vector3(0.19f, -0.1f, -0.36f), new Vector3(0.18f, 0.34f, 0.18f), new Vector3(0.14f, 0f, 0.14f), Vector3.zero, WHITE);
                MeshData lNozzles = new MeshData();
                GateKit.Box(lNozzles, new Vector3(-0.19f, -0.3f, -0.36f), new Vector3(0.13f, 0.04f, 0.13f), Color.white);
                GateKit.Box(lNozzles, new Vector3(0.19f, -0.3f, -0.36f), new Vector3(0.13f, 0.04f, 0.13f), Color.white);
                Attach(pRoot, lChest, lAt, lTorso, lNozzles, lMaterial, lHeat);
            }
            Shoulder(pRoot, pAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm), -1f, lMaterial);
            Shoulder(pRoot, pAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm), 1f, lMaterial);

            // ---------------- hips
            if (lHips != null)
            {
                MeshData lBelt = new MeshData();
                Prism(lBelt, new Vector3(0f, -0.02f, 0f), new Vector3(0.48f, 0.16f, 0.34f), new Vector3(0.44f, 0f, 0.3f), Vector3.zero, BLACK);
                Blade(lBelt, new Vector3(-0.24f, 0.04f, -0.14f), new Vector3(-0.42f, -0.4f, -0.4f), new Vector3(-0.1f, -0.04f, -0.18f), 0.02f, RED);
                Blade(lBelt, new Vector3(0.24f, 0.04f, -0.14f), new Vector3(0.42f, -0.4f, -0.4f), new Vector3(0.1f, -0.04f, -0.18f), 0.02f, RED);
                Attach(pRoot, lHips, pRoot.InverseTransformPoint(lHips.position), lBelt, null, lMaterial, null);
            }

            // ---------------- limbs
            Gauntlet(pRoot, pAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm), pAnimator.GetBoneTransform(HumanBodyBones.LeftHand), lMaterial, lGlowMaterial);
            Gauntlet(pRoot, pAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm), pAnimator.GetBoneTransform(HumanBodyBones.RightHand), lMaterial, lGlowMaterial);
            Boot(pRoot, pAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg), pAnimator.GetBoneTransform(HumanBodyBones.LeftFoot), lMaterial, lGlowMaterial);
            Boot(pRoot, pAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg), pAnimator.GetBoneTransform(HumanBodyBones.RightFoot), lMaterial, lGlowMaterial);
        }

        private static void Shoulder(Transform pRoot, Transform pBone, float pSide, Material pMaterial)
        {
            if (pBone == null) return;
            MeshData lData = new MeshData();
            Prism(lData, new Vector3(pSide * 0.08f, 0.02f, 0f), new Vector3(0.34f, 0.26f, 0.42f), new Vector3(0.2f, 0f, 0.26f), new Vector3(pSide * 0.02f, 0f, 0f), WHITE);
            // A big blade rising outward and back.
            Blade(lData, new Vector3(pSide * 0.12f, 0.1f, 0.2f), new Vector3(pSide * 0.52f, 0.42f, -0.34f), new Vector3(pSide * 0.14f, 0.12f, -0.22f), 0.03f, RED);
            Prism(lData, new Vector3(pSide * 0.1f, -0.13f, 0f), new Vector3(0.36f, 0.05f, 0.44f), new Vector3(0.36f, 0f, 0.44f), Vector3.zero, GOLD);
            Attach(pRoot, pBone, pRoot.InverseTransformPoint(pBone.position), lData, null, pMaterial, null);
        }

        private static void Gauntlet(Transform pRoot, Transform pBone, Transform pHand, Material pMaterial, Material pGlow)
        {
            if (pBone == null || pHand == null) return;
            float lLength = Vector3.Distance(pBone.position, pHand.position);
            Vector3 lAxis = pRoot.InverseTransformDirection((pHand.position - pBone.position).normalized);
            Vector3 lMiddle = pRoot.InverseTransformPoint(pBone.position) + lAxis * lLength * 0.55f;
            Quaternion lTurn = Quaternion.FromToRotation(Vector3.up, lAxis);
            MeshData lData = new MeshData();
            MeshData lBlock = new MeshData();
            Prism(lBlock, Vector3.zero, new Vector3(0.14f, lLength * 0.9f, 0.14f), new Vector3(0.24f, 0f, 0.24f), Vector3.zero, RED);
            AddRotated(lData, lBlock, lTurn);
            MeshData lStrip = new MeshData();
            MeshData lStripBlock = new MeshData();
            GateKit.Box(lStripBlock, new Vector3(0f, 0f, 0.1f), new Vector3(0.04f, lLength * 0.7f, 0.1f), Color.white);
            AddRotated(lStrip, lStripBlock, lTurn);
            Attach(pRoot, pBone, lMiddle, lData, lStrip, pMaterial, pGlow);
        }

        private static void Boot(Transform pRoot, Transform pBone, Transform pFoot, Material pMaterial, Material pGlow)
        {
            if (pBone == null || pFoot == null) return;
            float lLength = Vector3.Distance(pBone.position, pFoot.position);
            Vector3 lAxis = pRoot.InverseTransformDirection((pFoot.position - pBone.position).normalized);
            Vector3 lMiddle = pRoot.InverseTransformPoint(pBone.position) + lAxis * lLength * 0.58f;
            Quaternion lTurn = Quaternion.FromToRotation(Vector3.down, lAxis);
            MeshData lData = new MeshData();
            MeshData lShin = new MeshData();
            Prism(lShin, Vector3.zero, new Vector3(0.26f, lLength * 0.86f, 0.3f), new Vector3(0.2f, 0f, 0.22f), Vector3.zero, WHITE);
            Blade(lShin, new Vector3(0f, lLength * 0.42f, 0.14f), new Vector3(0f, lLength * 0.62f, -0.14f), new Vector3(0f, -lLength * 0.1f, -0.16f), 0.02f, RED);
            AddRotated(lData, lShin, lTurn);
            Attach(pRoot, pBone, lMiddle, lData, null, pMaterial, null);

            // The foot itself : oversized, wedge-shaped, with a burning sole.
            Vector3 lFootAt = pRoot.InverseTransformPoint(pFoot.position);
            MeshData lShoe = new MeshData();
            Prism(lShoe, new Vector3(0f, -0.03f, 0.1f), new Vector3(0.28f, 0.2f, 0.54f), new Vector3(0.22f, 0f, 0.3f), new Vector3(0f, 0f, -0.08f), BLACK);
            Blade(lShoe, new Vector3(0f, 0.04f, 0.36f), new Vector3(0f, 0.16f, -0.26f), new Vector3(0f, -0.1f, -0.12f), 0.03f, GOLD);
            MeshData lSole = new MeshData();
            GateKit.Box(lSole, new Vector3(0f, -0.14f, 0.1f), new Vector3(0.26f, 0.03f, 0.52f), Color.white);
            Attach(pRoot, pFoot, lFootAt, lShoe, lSole, pMaterial, pGlow);
        }

        private static void AddRotated(MeshData pInto, MeshData pFrom, Quaternion pRotation)
        {
            int lOffset = pInto.Count;
            for (int i = 0; i < pFrom.Count; i++)
                pInto.Vertex(pRotation * pFrom.vertices[i], pRotation * pFrom.normals[i], pFrom.colors[i], pFrom.uvs[i]);
            foreach (int lIndex in pFrom.triangles) pInto.triangles.Add(lIndex + lOffset);
        }
    }
}
