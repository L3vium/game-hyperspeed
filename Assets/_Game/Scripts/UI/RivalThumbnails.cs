using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Com.IsartDigital.Hyperspeed
{
    // Renders a small portrait of every rival into RenderTextures once (stage cards, VS cards, transitions).
    public static class RivalThumbnails
    {
        private static readonly Dictionary<int, RenderTexture> _cache = new Dictionary<int, RenderTexture>();

        public static Texture Get(int pLevel)
        {
            RenderTexture lTexture;
            if (_cache.TryGetValue(pLevel, out lTexture) && lTexture != null) return lTexture;
            lTexture = Render(LevelDatabase.Get(pLevel));
            _cache[pLevel] = lTexture;
            return lTexture;
        }

        private static RenderTexture Render(LevelDefinition pLevel)
        {
            Theme lTheme = pLevel.theme();
            GameObject lRoot = new GameObject("ThumbnailStudio");
            lRoot.transform.position = new Vector3(0f, -4000f, 0f);

            Transform lModel = RivalFactory.Build(pLevel.rival, lRoot.transform);
            lModel.localRotation = Quaternion.Euler(0f, pLevel.rival == RivalKind.Time ? 180f : 215f, 0f);
            Bounds lBounds = new Bounds(lModel.position, Vector3.zero);
            foreach (Renderer lRenderer in lModel.GetComponentsInChildren<Renderer>()) lBounds.Encapsulate(lRenderer.bounds);

            // Backdrop : sky gradient-ish plates in the stage palette.
            Material lBack = Mats.Glow(lTheme.skyHorizon, false, 0f, 0f, true);
            GameObject lPlate = Mats.Primitive(PrimitiveType.Quad, lRoot.transform, Vector3.zero, Vector3.one * 400f, lBack);
            lPlate.transform.position = lBounds.center + new Vector3(0f, 0f, 60f);
            GameObject lStripe = Mats.Primitive(PrimitiveType.Quad, lRoot.transform, Vector3.zero, new Vector3(400f, 18f, 1f), Mats.Glow(lTheme.skyTop, false, 0f, 0f, true));
            lStripe.transform.position = lBounds.center + new Vector3(0f, -lBounds.extents.y - 6f, 55f);
            lStripe.transform.rotation = Quaternion.Euler(0f, 0f, -8f);

            GameObject lLightObject = new GameObject("Key");
            lLightObject.transform.SetParent(lRoot.transform, false);
            lLightObject.transform.rotation = Quaternion.Euler(35f, 160f, 0f);
            Light lLight = lLightObject.AddComponent<Light>();
            lLight.type = LightType.Directional;
            lLight.intensity = 1.4f;

            Camera lCamera = new GameObject("ThumbCamera").AddComponent<Camera>();
            lCamera.transform.SetParent(lRoot.transform, false);
            float lSize = Mathf.Max(lBounds.size.x, lBounds.size.y, lBounds.size.z);
            lCamera.transform.position = lBounds.center + new Vector3(lSize * 0.35f, lSize * 0.3f, -lSize * 2.1f);
            lCamera.transform.LookAt(lBounds.center);
            lCamera.fieldOfView = 40f;
            lCamera.nearClipPlane = 0.1f;
            lCamera.farClipPlane = lSize * 10f + 200f;
            lCamera.clearFlags = CameraClearFlags.SolidColor;
            lCamera.backgroundColor = lTheme.skyHorizon;
            lCamera.cullingMask = ~0;
            UniversalAdditionalCameraData lData = lCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            lData.renderPostProcessing = false;

            RenderTexture lTexture = new RenderTexture(512, 288, 24, RenderTextureFormat.ARGB32);
            lTexture.name = "Rival_" + pLevel.rival;
            lTexture.Create();
            RenderPipeline.StandardRequest lRequest = new RenderPipeline.StandardRequest();
            if (RenderPipeline.SupportsRenderRequest(lCamera, lRequest))
            {
                lRequest.destination = lTexture;
                RenderPipeline.SubmitRenderRequest(lCamera, lRequest);
            }
            else
            {
                lCamera.targetTexture = lTexture;
                lCamera.Render();
                lCamera.targetTexture = null;
            }
            Object.DestroyImmediate(lRoot);
            return lTexture;
        }
    }
}
