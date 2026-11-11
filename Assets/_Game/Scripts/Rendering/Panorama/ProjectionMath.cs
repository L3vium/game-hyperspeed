using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Wide angle projections past the 180 degrees a perspective camera can never reach. One horizontal FOV, a shape s
    // (0.5 stereographic, 0 equidistant, 1 rectilinear) and a cylinder blend c (0 round fisheye, 1 Panini-like with
    // straight verticals). Mirrors Hyperspeed/Panorama (HS_Panorama.shader) exactly.
    public static class ProjectionMath
    {
        // Radius on the image plane of a ray at angle pTheta (radians) from the view axis.
        public static float F(float pTheta, float pShape)
        {
            if (pShape > 0.001f) return Mathf.Tan(pShape * pTheta) / pShape;
            if (pShape < -0.001f) return Mathf.Sin(-pShape * pTheta) / -pShape;
            return pTheta;
        }

        public static float FInverse(float pRadius, float pShape)
        {
            if (pShape > 0.001f) return Mathf.Atan(pShape * pRadius) / pShape;
            if (pShape < -0.001f) return Mathf.Asin(Mathf.Clamp(-pShape * pRadius, -1f, 1f)) / -pShape;
            return pRadius;
        }

        // Keeps s inside what can show this FOV (a rectilinear-ish shape can't reach the edge of a 300 degrees view).
        public static float SafeShape(float pShape, float pHalfFov)
        {
            if (pShape <= 0f) return pShape;
            return Mathf.Min(pShape, (Mathf.PI * 0.5f - 0.06f) / Mathf.Max(0.01f, pHalfFov));
        }

        // Radius of the image's left / right edge for a horizontal FOV in degrees.
        public static float EdgeRadius(float pHFovDegrees, float pShape) => F(pHFovDegrees * 0.5f * Mathf.Deg2Rad, pShape);

        // Screen point (normalised -1..1 on both axes) to a direction in camera space (+z forward).
        public static Vector3 ScreenToDirection(Vector2 pNdc, float pEdge, float pShape, float pCyl, float pAspect)
        {
            Vector2 lP = new Vector2(pNdc.x * pEdge, pNdc.y * pEdge / pAspect);
            // Round fisheye.
            float lR = lP.magnitude;
            float lTheta = FInverse(lR, pShape);
            Vector3 lRadial = lR > 1e-6f ? new Vector3(lP.x / lR * Mathf.Sin(lTheta), lP.y / lR * Mathf.Sin(lTheta), Mathf.Cos(lTheta)) : Vector3.forward;
            // Cylinder : the horizontal angle from x, then height along the cylinder.
            float lPhi = FInverse(lP.x, pShape);
            float lFx = F(lPhi, pShape);
            float lK = Mathf.Abs(lPhi) > 1e-4f ? Mathf.Sin(lPhi) / lFx : 1f;
            Vector3 lCyl = new Vector3(Mathf.Sin(lPhi), lP.y * lK, Mathf.Cos(lPhi)).normalized;
            return Vector3.Lerp(lRadial, lCyl, pCyl).normalized;
        }

        // Direction in camera space to a normalised screen point (approximate for blended shapes : the two pure
        // projections are blended). Returns false when the direction is outside the image.
        public static bool DirectionToScreen(Vector3 pDirection, float pEdge, float pShape, float pCyl, float pAspect, out Vector2 pNdc)
        {
            Vector3 lD = pDirection.normalized;
            float lTheta = Mathf.Acos(Mathf.Clamp(lD.z, -1f, 1f));
            Vector2 lSide = new Vector2(lD.x, lD.y);
            float lLen = lSide.magnitude;
            Vector2 lRadial = lLen > 1e-6f ? lSide / lLen * F(lTheta, pShape) : Vector2.zero;
            float lPhi = Mathf.Atan2(lD.x, lD.z);
            float lFlat = Mathf.Sqrt(lD.x * lD.x + lD.z * lD.z);
            float lFx = F(lPhi, pShape);
            float lK = Mathf.Abs(lPhi) > 1e-4f ? lFx / Mathf.Sin(lPhi) : 1f;
            Vector2 lCyl = new Vector2(lFx, lFlat > 1e-5f ? lD.y / lFlat * lK : Mathf.Sign(lD.y) * 99f);
            Vector2 lP = Vector2.Lerp(lRadial, lCyl, pCyl);
            pNdc = new Vector2(lP.x / pEdge, lP.y * pAspect / pEdge);
            return Mathf.Abs(pNdc.x) <= 1f && Mathf.Abs(pNdc.y) <= 1f && lTheta < Mathf.PI * 0.999f;
        }

        // The horizontal FOV that shows the centre of the screen at the same size as a perspective camera of vertical
        // FOV pVFov : a cinematic shot keeps its framing and simply gains its periphery.
        public static float EquivalentHFov(float pVFovDegrees, float pAspect, float pShape)
        {
            // Centre magnification of the wide view is 1 / edge, of the perspective one 1 / (aspect * tan(v/2)).
            float lEdge = pAspect * Mathf.Tan(pVFovDegrees * 0.5f * Mathf.Deg2Rad);
            return FInverse(lEdge, pShape) * 2f * Mathf.Rad2Deg;
        }
    }
}
