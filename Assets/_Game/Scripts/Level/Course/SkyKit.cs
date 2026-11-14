using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // The ant-hill (2026-11-12) : routes above the ground lane. A sky lane is a set of level shelves hanging in the
    // void : it follows the racing line's path seen from above, at an absolute altitude (pHeight is a world Y), so the
    // ground dropping away under it (slab edges, cliffs, dives) only adds air under its decks, ropes and rings.
    public static class SkyKit
    {
        public static Vector3 Point(LevelLayout pLayout, float pS, float pLateral, float pHeight)
        {
            SpineSample lSample = pLayout.spine.Evaluate(pS);
            Vector3 lForward = Flat(lSample.forward);
            Vector3 lRight = Vector3.Cross(Vector3.up, lForward);
            Vector3 lPoint = lSample.position + lRight * pLateral;
            lPoint.y = pHeight;
            return lPoint;
        }

        public static Quaternion Frame(LevelLayout pLayout, float pS) => Quaternion.LookRotation(Flat(pLayout.spine.Forward(pS)), Vector3.up);

        private static Vector3 Flat(Vector3 pForward)
        {
            Vector3 lFlat = new Vector3(pForward.x, 0f, pForward.z);
            return lFlat.sqrMagnitude > 0.0001f ? lFlat.normalized : Vector3.forward;
        }

        // Flat chevron pad lying on a deck.
        public static BoostPad DeckPad(Transform pParent, LevelLayout pLayout, float pS, float pLateral, float pHeight, float pWidth, float pLength, Color pColor, float pBoost)
        {
            GameObject lObject = new GameObject("DeckPad");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(Point(pLayout, pS, pLateral, pHeight + 0.3f), Frame(pLayout, pS));
            BoostPad lPad = lObject.AddComponent<BoostPad>();
            lPad.halfExtents = new Vector3(pWidth * 0.5f, 8f, pLength * 0.5f);
            lPad.boost = pLayout.profile.cruise * pBoost * BoostPad.PAD_POWER;
            lPad.cooldown = 0.4f;
            MeshData lBack = new MeshData();
            MeshData lFront = new MeshData();
            float lHalf = pWidth * 0.5f;
            const int CHEVRONS = 4;
            float lRow = pLength / CHEVRONS;
            lBack.Quad(new Vector3(-lHalf, 0f, -pLength * 0.5f), new Vector3(lHalf, 0f, -pLength * 0.5f), new Vector3(lHalf, 0f, pLength * 0.5f), new Vector3(-lHalf, 0f, pLength * 0.5f), Vector3.up, new Color(0.08f, 0.06f, 0.12f));
            for (int k = 0; k < CHEVRONS; k++)
            {
                float lZ = -pLength * 0.5f + k * lRow;
                // A chevron : two slanted bars meeting at the centre, pointing forward.
                lFront.Quad(new Vector3(-lHalf, 0.1f, lZ), new Vector3(0f, 0.1f, lZ + lRow * 0.55f), new Vector3(0f, 0.1f, lZ + lRow * 0.95f), new Vector3(-lHalf, 0.1f, lZ + lRow * 0.4f), Vector3.up, Color.white);
                lFront.Quad(new Vector3(0f, 0.1f, lZ + lRow * 0.55f), new Vector3(lHalf, 0.1f, lZ), new Vector3(lHalf, 0.1f, lZ + lRow * 0.4f), new Vector3(0f, 0.1f, lZ + lRow * 0.95f), Vector3.up, Color.white);
            }
            GameObject lPlate = new GameObject("Plate");
            lPlate.transform.SetParent(lObject.transform, false);
            lPlate.AddComponent<MeshFilter>().sharedMesh = lBack.ToMesh("DeckPadPlate");
            lPlate.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(new Color(0.2f, 0.2f, 0.25f), false);
            GameObject lVisual = new GameObject("Chevrons");
            lVisual.transform.SetParent(lObject.transform, false);
            lVisual.AddComponent<MeshFilter>().sharedMesh = lFront.ToMesh("DeckPadChevrons");
            lVisual.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(pColor * 2.6f, false, 0f, 0f, true);
            return lPad;
        }

        // Launch strip on a deck : throws you up toward rings or the next level.
        public static Launcher DeckStrip(Transform pParent, LevelLayout pLayout, float pS, float pLateral, float pHeight, float pHalfWidth, float pUp, Color pColor)
        {
            float lDepth = Mathf.Max(10f, pLayout.profile.cruise * 0.25f);
            Launcher lLauncher = Launcher.Create(pParent, Point(pLayout, pS, pLateral, pHeight + 0.3f), Frame(pLayout, pS), pHalfWidth, lDepth, pUp);
            MeshData lData = new MeshData();
            for (int lRow = 0; lRow < 4; lRow++)
            {
                float lZ = -lDepth * 0.5f + lDepth * lRow / 4f;
                lData.Quad(new Vector3(-pHalfWidth, 0.1f, lZ), new Vector3(pHalfWidth, 0.1f, lZ), new Vector3(pHalfWidth, 0.1f, lZ + lDepth * 0.14f), new Vector3(-pHalfWidth, 0.1f, lZ + lDepth * 0.14f), Vector3.up, Color.white);
            }
            GameObject lVisual = new GameObject("Visual");
            lVisual.transform.SetParent(lLauncher.transform, false);
            lVisual.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("DeckStrip");
            lVisual.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(pColor * 2.4f, false, 0f, 0f, true);
            return lLauncher;
        }

        // Kicker : a ramp rising out of the ground lane from s0 to its lip at s1, pHeight high at the lip. You run up it and
        // the lip throws you up to a sky lane (launch speed pUp along the ramp's normal). Solid : you can run on it.
        public static Launcher Kicker(Transform pParent, LevelLayout pLayout, float pS0, float pS1, float pLateral, float pHalfWidth, float pHeight, float pUp, Theme pTheme)
        {
            TrackField lField = pLayout.field;
            Vector3 lOrigin = lField.Point(pS0, pLateral);
            MeshData lData = new MeshData();
            MeshData lGlow = new MeshData();
            const int STEPS = 12;
            Vector3 lPrevL = Vector3.zero, lPrevR = Vector3.zero, lPrevBL = Vector3.zero, lPrevBR = Vector3.zero;
            float lSpan = pS1 - pS0;
            for (int i = 0; i <= STEPS; i++)
            {
                float lT = i / (float)STEPS;
                float lS = pS0 + lSpan * lT;
                Vector3 lNormal = lField.Normal(lS);
                Vector3 lRight = pLayout.spine.Right(lS);
                // A gentle start then a straight ramp : no step to trip on at the foot.
                float lRise = pHeight * Mathf.Lerp(lT * lT, lT, Mathf.Clamp01(lT * 2.5f));
                // Wider at the foot than on top : the sides are slopes, never walls.
                float lTop = pHalfWidth;
                float lFoot = pHalfWidth + lRise * 0.8f;
                Vector3 lBase = lField.Point(lS, pLateral) - lOrigin;
                Vector3 lL = lBase - lRight * lTop + lNormal * lRise;
                Vector3 lR = lBase + lRight * lTop + lNormal * lRise;
                Vector3 lBL = lBase - lRight * lFoot - lNormal * 2f;
                Vector3 lBR = lBase + lRight * lFoot - lNormal * 2f;
                if (i > 0)
                {
                    lData.FacetQuad(lPrevL, lL, lR, lPrevR, lNormal, Color.Lerp(pTheme.roadA, Color.white, 0.2f));
                    lData.FacetQuad(lPrevBL, lBL, lL, lPrevL, -lRight + lNormal, pTheme.roadSide);
                    lData.FacetQuad(lPrevR, lR, lBR, lPrevBR, lRight + lNormal, pTheme.roadSide);
                    // Glowing chevrons running up the ramp.
                    if (i % 2 == 0)
                    {
                        Vector3 lMid = (lL + lR) * 0.5f + lNormal * 0.25f;
                        Vector3 lPrevMid = (lPrevL + lPrevR) * 0.5f + lNormal * 0.25f;
                        lGlow.FacetQuad(lPrevL + lNormal * 0.25f + lRight * 2f, lMid, lMid - (lMid - lPrevMid) * 0.35f, lPrevL + lNormal * 0.25f + lRight * 2f + (lMid - lPrevMid) * 0.3f, lNormal, Color.white);
                        lGlow.FacetQuad(lMid, lPrevR + lNormal * 0.25f - lRight * 2f, lPrevR + lNormal * 0.25f - lRight * 2f + (lMid - lPrevMid) * 0.3f, lMid - (lMid - lPrevMid) * 0.35f, lNormal, Color.white);
                    }
                }
                lPrevL = lL; lPrevR = lR; lPrevBL = lBL; lPrevBR = lBR;
            }
            // The lip's face, falling back to the ground.
            Vector3 lLipNormal = pLayout.spine.Forward(pS1);
            lData.FacetQuad(lPrevR, lPrevL, lPrevBL, lPrevBR, lLipNormal, Color.Lerp(pTheme.roadSide, Color.black, 0.3f));
            GameObject lObject = new GameObject("Kicker");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.position = lOrigin;
            lObject.layer = Layers.DECK;
            Mesh lMesh = lData.ToMesh("Kicker");
            lObject.AddComponent<MeshFilter>().sharedMesh = lMesh;
            lObject.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(pTheme.shade, true);
            lObject.AddComponent<MeshCollider>().sharedMesh = lMesh;
            GameObject lLit = new GameObject("Chevrons");
            lLit.transform.SetParent(lObject.transform, false);
            lLit.AddComponent<MeshFilter>().sharedMesh = lGlow.ToMesh("KickerChevrons");
            lLit.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(Color.Lerp(pTheme.accent, Color.white, 0.4f) * 3f, false, 0f, 0f, true);

            // The launcher sits on the top of the lip, tilted like the ramp.
            Vector3 lLipBase = lField.Point(pS1, pLateral);
            Vector3 lLipNormalUp = lField.Normal(pS1);
            Vector3 lForward = pLayout.spine.Forward(pS1);
            Vector3 lRampDir = (lForward * lSpan * 0.4f + lLipNormalUp * pHeight * 0.4f).normalized;
            Vector3 lRampUp = Vector3.Cross(lRampDir, Vector3.Cross(lLipNormalUp, lRampDir)).normalized;
            float lDepth = Mathf.Max(14f, lSpan * 0.3f);
            Launcher lLauncher = Launcher.Create(lObject.transform, lLipBase + lLipNormalUp * pHeight - lRampDir * lDepth * 0.4f, Quaternion.LookRotation(lRampDir, lRampUp), pHalfWidth + 6f, lDepth, pUp);
            lLauncher.halfExtents = new Vector3(pHalfWidth + 6f, 14f, lDepth * 0.5f);
            return lLauncher;
        }

        // Small glowing anchor at the end of a rope hung in the void : the rope is tied to the deck, nothing holds it up.
        public static void Anchor(Transform pParent, Vector3 pPosition, Vector3 pUp, Color pColor)
        {
            GameObject lOrb = Mats.Primitive(PrimitiveType.Sphere, pParent, Vector3.zero, Vector3.one * 5f, Mats.Glow(Color.Lerp(pColor, Color.white, 0.5f) * 3f, false, 0f, 0f, true), "Anchor");
            lOrb.transform.position = pPosition;
            GameObject lPost = Mats.Primitive(PrimitiveType.Cylinder, pParent, Vector3.zero, new Vector3(1.2f, 4f, 1.2f), Mats.Flat(new Color(0.15f, 0.15f, 0.2f), false, new Color(0.3f, 0.3f, 0.36f)), "AnchorPost");
            lPost.transform.position = pPosition - pUp * 4f;
            lPost.transform.rotation = Quaternion.FromToRotation(Vector3.up, pUp);
        }
    }

    // A launch zone at any position (kicker lips, deck strips) : a high jump along its own up.
    public class Launcher : Interactable
    {
        private float _up;

        public static Launcher Create(Transform pParent, Vector3 pPosition, Quaternion pRotation, float pHalfWidth, float pDepth, float pUp)
        {
            GameObject lObject = new GameObject("Launcher");
            lObject.transform.SetParent(pParent, true);
            lObject.transform.SetPositionAndRotation(pPosition, pRotation);
            Launcher lLauncher = lObject.AddComponent<Launcher>();
            lLauncher._up = pUp;
            lLauncher.halfExtents = new Vector3(pHalfWidth, 10f, pDepth * 0.5f);
            lLauncher.cooldown = 1f;
            return lLauncher;
        }

        public override bool Accepts(PlayerController pPlayer) => Along(pPlayer);

        protected override void OnTouch(PlayerController pPlayer) => pPlayer.Launch(_up);
    }
}
