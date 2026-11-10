using TMPro;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Massive arch across the whole terrain (start, finish) with a word in extruded 3D letters.
    public static class GateKit
    {
        public static Transform Portal(Transform pParent, LevelLayout pLayout, float pS, float pHalf, float pHeight, Color pColor, Color pTextColor, string pWord)
        {
            Theme lTheme = RaceContext.Theme;
            Vector3 lBase = pLayout.field.Point(pS, pLayout.field.CenterLateral(pS));
            Vector3 lNormal = pLayout.field.Normal(pS);
            Quaternion lRotation = Quaternion.LookRotation(pLayout.spine.Forward(pS), lNormal);
            GameObject lRoot = new GameObject("Gate");
            lRoot.transform.SetParent(pParent, false);
            lRoot.transform.SetPositionAndRotation(lBase, lRotation);

            float lPillar = pHeight * 0.14f;
            float lBeam = pHeight * 0.22f;
            MeshData lData = new MeshData();
            Color lDark = Color.Lerp(pColor, Color.black, 0.45f);
            Box(lData, new Vector3(-pHalf - lPillar * 0.5f, pHeight * 0.5f, 0f), new Vector3(lPillar, pHeight, lPillar), lDark);
            Box(lData, new Vector3(pHalf + lPillar * 0.5f, pHeight * 0.5f, 0f), new Vector3(lPillar, pHeight, lPillar), lDark);
            Box(lData, new Vector3(0f, pHeight, 0f), new Vector3(pHalf * 2f + lPillar * 2f, lBeam, lPillar), pColor);
            GameObject lMesh = new GameObject("Arch");
            lMesh.transform.SetParent(lRoot.transform, false);
            lMesh.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("Gate");
            lMesh.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(lTheme != null ? lTheme.shade : new Color(0.6f, 0.55f, 0.8f), false);

            if (!string.IsNullOrEmpty(pWord))
            {
                Transform lText = Text3D.Create(lRoot.transform, pWord, lBeam * 0.85f, pHalf * 1.6f, pTextColor, Color.Lerp(pColor, Color.black, 0.6f));
                lText.localPosition = new Vector3(0f, pHeight - lBeam * 0.05f, -lPillar * 0.5f - 0.5f);
            }
            return lRoot.transform;
        }

        // The finish : a colossal yellow and black gate with a giant word, glowing pillars and two beams of light into the
        // sky. It reads from the other end of the stage.
        public static Transform FinishGate(Transform pParent, LevelLayout pLayout, float pS, float pHalf)
        {
            Color lYellow = new Color(1f, 0.86f, 0.1f);
            Color lInk = new Color(0.06f, 0.05f, 0.08f);
            float lHeight = Mathf.Max(180f, pHalf * 0.75f);
            Vector3 lBase = pLayout.field.Point(pS, pLayout.field.CenterLateral(pS));
            Vector3 lNormal = pLayout.field.Normal(pS);
            GameObject lRoot = new GameObject("FinishGate");
            lRoot.transform.SetParent(pParent, false);
            lRoot.transform.SetPositionAndRotation(lBase, Quaternion.LookRotation(pLayout.spine.Forward(pS), lNormal));

            float lPillar = lHeight * 0.12f;
            float lBeam = lHeight * 0.26f;
            MeshData lData = new MeshData();
            // Hazard bands up the pillars and along the beam.
            int lBands = 10;
            for (int i = 0; i < lBands; i++)
            {
                float lY = lHeight * (i + 0.5f) / lBands;
                Color lC = i % 2 == 0 ? lYellow : lInk;
                Box(lData, new Vector3(-pHalf - lPillar * 0.5f, lY, 0f), new Vector3(lPillar, lHeight / lBands, lPillar), lC);
                Box(lData, new Vector3(pHalf + lPillar * 0.5f, lY, 0f), new Vector3(lPillar, lHeight / lBands, lPillar), lC);
            }
            int lCells = 24;
            float lSpan = pHalf * 2f + lPillar * 2f;
            for (int i = 0; i < lCells; i++)
            {
                float lX = -lSpan * 0.5f + lSpan * (i + 0.5f) / lCells;
                Box(lData, new Vector3(lX, lHeight + lBeam * 0.25f, 0f), new Vector3(lSpan / lCells, lBeam * 0.5f, lPillar * 0.9f), i % 2 == 0 ? Color.white : lInk);
                Box(lData, new Vector3(lX, lHeight + lBeam * 0.75f, 0f), new Vector3(lSpan / lCells, lBeam * 0.5f, lPillar * 0.9f), i % 2 == 0 ? lInk : Color.white);
            }
            // A chequered line on the ground.
            for (int i = 0; i < lCells; i++)
            {
                float lX = -pHalf + pHalf * 2f * (i + 0.5f) / lCells;
                Box(lData, new Vector3(lX, 0.3f, 0f), new Vector3(pHalf * 2f / lCells, 0.6f, 12f), i % 2 == 0 ? Color.white : lInk);
                Box(lData, new Vector3(lX, 0.3f, 12f), new Vector3(pHalf * 2f / lCells, 0.6f, 12f), i % 2 == 0 ? lInk : Color.white);
            }
            GameObject lMesh = new GameObject("Arch");
            lMesh.transform.SetParent(lRoot.transform, false);
            lMesh.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("FinishGate");
            lMesh.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(new Color(1f, 0.95f, 0.85f), false);

            // Glow trims and beams of light.
            MeshData lGlow = new MeshData();
            Box(lGlow, new Vector3(-pHalf, lHeight * 0.5f, -lPillar * 0.55f), new Vector3(2.5f, lHeight, 1f), Color.white);
            Box(lGlow, new Vector3(pHalf, lHeight * 0.5f, -lPillar * 0.55f), new Vector3(2.5f, lHeight, 1f), Color.white);
            Box(lGlow, new Vector3(0f, lHeight - 1f, -lPillar * 0.5f), new Vector3(lSpan, 2.5f, 1f), Color.white);
            GameObject lTrim = new GameObject("Trim");
            lTrim.transform.SetParent(lRoot.transform, false);
            lTrim.AddComponent<MeshFilter>().sharedMesh = lGlow.ToMesh("FinishTrim");
            lTrim.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(new Color(3f, 2.4f, 0.4f), false, 0f, 0f, true);
            for (int lSide = -1; lSide <= 1; lSide += 2)
            {
                GameObject lBeamObject = Mats.Primitive(PrimitiveType.Cylinder, lRoot.transform, new Vector3(lSide * (pHalf + lPillar * 0.5f), lHeight + 1500f, 0f), new Vector3(lPillar * 0.8f, 1500f, lPillar * 0.8f), Mats.Glow(new Color(2.4f, 1.9f, 0.3f, 0.45f), true, 1f, 1f));
                lBeamObject.name = "SkyBeam";
            }

            Transform lText = Text3D.Create(lRoot.transform, "FINISH", lBeam * 0.9f, lSpan * 0.8f, lYellow, lInk, 14, 0.02f);
            lText.localPosition = new Vector3(0f, lHeight + lBeam * 0.5f, -lPillar * 0.5f - 1f);
            Transform lBack = Text3D.Create(lRoot.transform, "FINISH", lBeam * 0.9f, lSpan * 0.8f, lYellow, lInk, 4, 0.02f);
            lBack.localPosition = new Vector3(0f, lHeight + lBeam * 0.5f, lPillar * 0.5f + 1f);
            lBack.localRotation = Quaternion.Euler(0f, 180f, 0f);
            return lRoot.transform;
        }

        // Lit arch over the track : a frame that rushes past, for tunnels and big moments.
        public static void Arch(Transform pParent, LevelLayout pLayout, float pS, Theme pTheme)
        {
            float lHalf = pLayout.field.TotalHalfWidth(pS);
            float lHeight = lHalf * 0.9f + 14f;
            Vector3 lNormal = pLayout.field.Normal(pS);
            GameObject lRoot = new GameObject("Arch");
            lRoot.transform.SetParent(pParent, false);
            lRoot.transform.SetPositionAndRotation(pLayout.field.Point(pS, pLayout.field.CenterLateral(pS)), Quaternion.LookRotation(pLayout.spine.Forward(pS), lNormal));
            MeshData lGlow = new MeshData();
            float lT = 1.5f;
            Box(lGlow, new Vector3(-lHalf * 1.02f, lHeight * 0.5f, 0f), new Vector3(lT, lHeight, lT), Color.white);
            Box(lGlow, new Vector3(lHalf * 1.02f, lHeight * 0.5f, 0f), new Vector3(lT, lHeight, lT), Color.white);
            Box(lGlow, new Vector3(0f, lHeight, 0f), new Vector3(lHalf * 2.04f + lT, lT, lT), Color.white);
            lRoot.AddComponent<MeshFilter>().sharedMesh = lGlow.ToMesh("Arch");
            lRoot.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(Color.Lerp(pTheme.accent, Color.white, 0.25f) * 3.2f, false, 0f, 0f, true);
        }

        // Gravity gate : a thick glowing hoop with chevrons, the sign that the surface is about to carry you.
        public static void GravityHoop(Transform pParent, LevelLayout pLayout, float pS, Theme pTheme)
        {
            float lRadius = pLayout.field.TotalHalfWidth(pS) * 1.04f;
            Vector3 lNormal = pLayout.field.Normal(pS);
            GameObject lRoot = new GameObject("GravityHoop");
            lRoot.transform.SetParent(pParent, false);
            lRoot.transform.SetPositionAndRotation(pLayout.field.Point(pS, pLayout.field.CenterLateral(pS)), Quaternion.LookRotation(pLayout.spine.Forward(pS), lNormal));
            MeshData lGlow = new MeshData();
            const int SEGMENTS = 40;
            float lThick = Mathf.Max(5f, lRadius * 0.025f);
            for (int i = 0; i < SEGMENTS; i++)
            {
                float lA = Mathf.PI * i / (SEGMENTS - 1);
                Vector3 lP = new Vector3(Mathf.Cos(lA) * lRadius, Mathf.Sin(lA) * lRadius, 0f);
                float lArc = Mathf.PI * lRadius / SEGMENTS * 1.15f;
                Vector3 lTangent = new Vector3(-Mathf.Sin(lA), Mathf.Cos(lA), 0f);
                AddBeam(lGlow, lP, lTangent, lArc, lThick, Color.white);
                // Chevrons on the inside, pointing the way.
                if (i % 4 == 2)
                {
                    Vector3 lIn = -lP.normalized;
                    AddBeam(lGlow, lP + lIn * lThick * 3f, Vector3.Normalize(lTangent + Vector3.forward), lThick * 4f, lThick * 0.6f, Color.white);
                    AddBeam(lGlow, lP + lIn * lThick * 3f, Vector3.Normalize(-lTangent + Vector3.forward), lThick * 4f, lThick * 0.6f, Color.white);
                }
            }
            lRoot.AddComponent<MeshFilter>().sharedMesh = lGlow.ToMesh("GravityHoop");
            lRoot.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(Color.Lerp(pTheme.accent, Color.white, 0.35f) * 4f, false, 0f, 0f, true);
        }

        private static void AddBeam(MeshData pData, Vector3 pCenter, Vector3 pAlong, float pLength, float pThick, Color pColor)
        {
            Quaternion lRot = Quaternion.LookRotation(pAlong, Vector3.forward == pAlong ? Vector3.up : Vector3.forward);
            Vector3 lH = new Vector3(pThick, pThick, pLength) * 0.5f;
            Vector3[] lC = new Vector3[8];
            for (int i = 0; i < 8; i++) lC[i] = pCenter + lRot * Vector3.Scale(lH, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            pData.FacetQuad(lC[2], lC[3], lC[7], lC[6], lRot * Vector3.up, pColor);
            pData.FacetQuad(lC[0], lC[4], lC[5], lC[1], lRot * Vector3.down, pColor);
            pData.FacetQuad(lC[0], lC[2], lC[6], lC[4], lRot * Vector3.left, pColor);
            pData.FacetQuad(lC[1], lC[5], lC[7], lC[3], lRot * Vector3.right, pColor);
            pData.FacetQuad(lC[0], lC[1], lC[3], lC[2], lRot * Vector3.back, pColor);
            pData.FacetQuad(lC[4], lC[6], lC[7], lC[5], lRot * Vector3.forward, pColor);
        }

        public static void Box(MeshData pData, Vector3 pCenter, Vector3 pSize, Color pColor)
        {
            Vector3 lH = pSize * 0.5f;
            Vector3 lA = pCenter + new Vector3(-lH.x, -lH.y, -lH.z);
            Vector3 lB = pCenter + new Vector3(lH.x, -lH.y, -lH.z);
            Vector3 lC = pCenter + new Vector3(lH.x, -lH.y, lH.z);
            Vector3 lD = pCenter + new Vector3(-lH.x, -lH.y, lH.z);
            Vector3 lE = pCenter + new Vector3(-lH.x, lH.y, -lH.z);
            Vector3 lF = pCenter + new Vector3(lH.x, lH.y, -lH.z);
            Vector3 lG = pCenter + new Vector3(lH.x, lH.y, lH.z);
            Vector3 lK = pCenter + new Vector3(-lH.x, lH.y, lH.z);
            pData.FacetQuad(lE, lF, lG, lK, Vector3.up, Color.Lerp(pColor, Color.white, 0.25f));
            pData.FacetQuad(lA, lB, lF, lE, Vector3.back, pColor);
            pData.FacetQuad(lC, lD, lK, lG, Vector3.forward, pColor);
            pData.FacetQuad(lB, lC, lG, lF, Vector3.right, pColor * 0.85f);
            pData.FacetQuad(lD, lA, lE, lK, Vector3.left, pColor * 0.85f);
            pData.FacetQuad(lD, lC, lB, lA, Vector3.down, pColor * 0.7f);
        }
    }

    // World-space word thickened by stacking copies : reads as an extruded 3D title at any angle.
    public static class Text3D
    {
        public static Transform Create(Transform pParent, string pText, float pSize, float pMaxWidth, Color pFace, Color pSide, int pLayers = 10, float pDepthRatio = 0.016f)
        {
            GameObject lRoot = new GameObject("Text3D");
            lRoot.transform.SetParent(pParent, false);
            float lStep = pSize * pDepthRatio;
            for (int i = pLayers; i >= 0; i--)
            {
                GameObject lLayer = new GameObject(i == 0 ? "Face" : "Side" + i);
                lLayer.transform.SetParent(lRoot.transform, false);
                lLayer.transform.localPosition = new Vector3(0f, 0f, i * lStep);
                TextMeshPro lTmp = lLayer.AddComponent<TextMeshPro>();
                lTmp.font = GameAssets.TitleFont;
                lTmp.text = pText;
                lTmp.fontStyle = FontStyles.Italic;
                lTmp.alignment = TextAlignmentOptions.Center;
                lTmp.textWrappingMode = TextWrappingModes.NoWrap;
                lTmp.color = i == 0 ? pFace : Color.Lerp(pSide, Color.black, i / (float)pLayers * 0.35f);
                lTmp.enableAutoSizing = true;
                lTmp.fontSizeMin = 1f;
                lTmp.fontSizeMax = 2000f;
                lTmp.rectTransform.sizeDelta = new Vector2(pMaxWidth, pSize);
            }
            return lRoot.transform;
        }

        public static void SetText(Transform pRoot, string pText, Color pFace)
        {
            TextMeshPro[] lLayers = pRoot.GetComponentsInChildren<TextMeshPro>(true);
            foreach (TextMeshPro lLayer in lLayers)
            {
                lLayer.text = pText;
                if (lLayer.name == "Face") lLayer.color = pFace;
            }
        }
    }
}
