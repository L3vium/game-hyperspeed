using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Com.IsartDigital.Hyperspeed
{
    // Cached material factory around the custom Hyperspeed shaders.
    public static class Mats
    {
        private static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();
        private static Shader _toon;
        private static Shader _glow;

        public static readonly int BASE_COLOR = Shader.PropertyToID("_BaseColor");
        public static readonly int FLASH = Shader.PropertyToID("_Flash");
        public static readonly int COLOR = Shader.PropertyToID("_Color");

        private static Shader ToonShader => _toon != null ? _toon : (_toon = Shader.Find("Hyperspeed/Toon"));
        private static Shader GlowShader => _glow != null ? _glow : (_glow = Shader.Find("Hyperspeed/Glow"));

        public static Material Toon(Color pColor, Color? pShade = null, float pOutline = 0.02f, float pRim = 0.35f, bool pVertexColor = false)
        {
            Color lShade = pShade ?? new Color(0.6f, 0.52f, 0.78f);
            string lKey = "toon" + pColor + lShade + pOutline + pRim + pVertexColor;
            Material lMat;
            if (_cache.TryGetValue(lKey, out lMat) && lMat != null) return lMat;
            lMat = new Material(ToonShader);
            lMat.SetColor(BASE_COLOR, pColor);
            lMat.SetColor("_ShadeColor", lShade);
            lMat.SetFloat("_OutlineWidth", pOutline);
            lMat.SetFloat("_RimAmount", pRim);
            lMat.SetFloat("_UseVertexColor", pVertexColor ? 1f : 0f);
            lMat.enableInstancing = true;
            _cache[lKey] = lMat;
            return lMat;
        }

        public static Material ToonUnique(Color pColor)
        {
            Material lMat = new Material(Toon(pColor));
            return lMat;
        }

        public static Material Glow(Color pColor, bool pAdditive = true, float pSoft = 0f, float pFresnel = 0f, bool pZWrite = false)
        {
            string lKey = "glow" + pColor + pAdditive + pSoft + pFresnel + pZWrite;
            Material lMat;
            if (_cache.TryGetValue(lKey, out lMat) && lMat != null) return lMat;
            lMat = new Material(GlowShader);
            lMat.SetColor(COLOR, pColor);
            lMat.SetFloat("_Soft", pSoft);
            lMat.SetFloat("_Fresnel", pFresnel);
            lMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            lMat.SetFloat("_DstBlend", pAdditive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            lMat.SetFloat("_ZWrite", pZWrite ? 1f : 0f);
            lMat.renderQueue = pZWrite ? 2450 : 3000;
            lMat.enableInstancing = true;
            _cache[lKey] = lMat;
            return lMat;
        }

        public static Material GlowStripes(Color pColor, float pFrequency, float pScroll)
        {
            Material lMat = new Material(Glow(pColor, false, 0f, 0f, true));
            lMat.SetFloat("_StripeFreq", pFrequency);
            lMat.SetFloat("_Scroll", pScroll);
            return lMat;
        }

        // Primitive helper : creates a primitive without collider using a material.
        public static GameObject Primitive(PrimitiveType pType, Transform pParent, Vector3 pLocalPos, Vector3 pScale, Material pMaterial, string pName = null)
        {
            GameObject lObject = GameObject.CreatePrimitive(pType);
            Object.DestroyImmediate(lObject.GetComponent<Collider>());
            if (pName != null) lObject.name = pName;
            lObject.transform.SetParent(pParent, false);
            lObject.transform.localPosition = pLocalPos;
            lObject.transform.localScale = pScale;
            Renderer lRenderer = lObject.GetComponent<Renderer>();
            lRenderer.sharedMaterial = pMaterial;
            return lObject;
        }
    }
}
