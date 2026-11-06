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
