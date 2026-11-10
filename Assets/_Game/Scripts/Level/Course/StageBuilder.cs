using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Builds a stage from its data : replays the racing line, lays the blocks, bakes the track and places every object.
    // Recipes (CourseBuilder) and hand-edited stages (StageAsset) both end up here, so they always build the same way.
    public static class StageBuilder
    {
        // ================================================================ line

        // Racing line and blocks only : what the level design tools need to place things, without any mesh.
        public static LevelLayout BuildLayout(StageData pData, SpeedProfile pProfile)
        {
            LevelLayout lLayout = new LevelLayout { profile = pProfile };
            ReplaySpine(pData, lLayout.spine);
            TrackField lField = new TrackField(lLayout.spine);
            foreach (BlockData lBlock in pData.blocks) lField.blocks.Add(lBlock.ToBlock());
            lField.Seal();
            lLayout.field = lField;
            lLayout.startLineS = pData.startLineS;
            lLayout.spawnS = pData.spawnS;
            lLayout.finishS = pData.finishS;
            lLayout.climaxS = pData.climaxS;
            foreach (RouteData lNode in pData.route) lLayout.route.Add(new RouteNode { s0 = lNode.s0, s1 = lNode.s1, lateral = lNode.lateral });
            foreach (ActData lAct in pData.acts) lLayout.acts.Add(new RouteAct { s = lAct.s, action = lAct.action });
            foreach (HintData lHint in pData.hints) lLayout.hints.Add(new KeyValuePair<float, string>(lHint.s, lHint.text));
            lLayout.route.Sort((pA, pB) => pA.s0.CompareTo(pB.s0));
            lLayout.acts.Sort((pA, pB) => pA.s.CompareTo(pB.s));
            lLayout.hints.Sort((pA, pB) => pA.Key.CompareTo(pB.Key));
            return lLayout;
        }

        public static void ReplaySpine(StageData pData, Spine pSpine)
        {
            pSpine.samples.Clear();
            Turtle lTurtle = new Turtle(pSpine, pData.altitude);
            lTurtle.Plaza();
            foreach (SegmentData lSegment in pData.segments) lTurtle.Segment(lSegment);
        }

        // Same walk as CourseBuilder : yaw, pitch and roll eased along a piece, one sample every Spine.STEP metres.
        public class Turtle
        {
            private readonly Spine _spine;
            public Vector3 pos;
            public float yaw = 90f;
            public float pitch;
            public float roll;
            public float s = -StageData.PLAZA;
            public bool localGravity;

            public Turtle(Spine pSpine, float pAltitude)
            {
                _spine = pSpine;
                pos = new Vector3(-8000f, pAltitude, 0f);
            }

            public Quaternion Frame() => Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(-pitch, Vector3.right) * Quaternion.AngleAxis(-roll, Vector3.forward);

            public void Push(Vector3 pShift = default(Vector3))
            {
                Quaternion lFrame = Frame();
                Vector3 lForward = lFrame * Vector3.forward;
                if (_spine.Count > 0) pos += lForward * Spine.STEP + pShift;
                _spine.samples.Add(new SpineSample { position = pos, forward = lForward, up = lFrame * Vector3.up, s = s, localGravity = localGravity });
                s += Spine.STEP;
            }

            public void Plaza()
            {
                int lSteps = Mathf.RoundToInt(StageData.PLAZA / Spine.STEP);
                for (int i = 0; i <= lSteps; i++) Push();
            }

            public void Segment(SegmentData pSegment)
            {
                int lSteps = Mathf.Max(1, pSegment.steps);
                float lYaw0 = yaw, lPitch0 = pitch, lRoll0 = roll;
                Vector3 lShift = Quaternion.AngleAxis(lYaw0, Vector3.up) * pSegment.shift;
                localGravity = pSegment.localGravity;
                float lPrevious = 0f;
                for (int i = 1; i <= lSteps; i++)
                {
                    float lT = i / (float)lSteps;
                    float lE = Mathf.Lerp(HSMath.Smooth(lT), lT, pSegment.linear);
                    yaw = lYaw0 + pSegment.yaw * lE;
                    pitch = lPitch0 + pSegment.pitch * lE;
                    roll = lRoll0 + pSegment.roll * lE;
                    Push(lShift * (lE - lPrevious));
                    lPrevious = lE;
                }
                localGravity = false;
            }
        }

        // ================================================================ world

        public static LevelLayout Build(StageData pData, Theme pTheme, SpeedProfile pProfile, Transform pRoot, bool pNoItems = false)
        {
            float lStart = Time.realtimeSinceStartup;
            LevelLayout lLayout = BuildLayout(pData, pProfile);
            CourseValidator.Check(lLayout, pData.dives);
            TrackBaker.Bake(lLayout, pTheme, pRoot);
            float lTrackDone = Time.realtimeSinceStartup;

            Transform lProps = new GameObject("Props").transform;
            lProps.SetParent(pRoot, false);
            MeteorDirector lDirector = new GameObject("Meteors").AddComponent<MeteorDirector>();
            lDirector.transform.SetParent(lProps, false);
            int lCount = 0;
            foreach (ItemData lItem in pData.items)
            {
                if (pNoItems && lItem.kind != ItemKind.Arch && lItem.kind != ItemKind.GravityGate) continue;
                if (Place(lItem, lLayout, pData, pTheme, lProps, lDirector) != null) lCount++;
            }
            lDirector.Init(lLayout, pTheme);
            StartGate.Create(lProps, lLayout, pTheme);
            GateKit.FinishGate(lProps, lLayout, lLayout.finishS, lLayout.field.TotalHalfWidth(lLayout.finishS));
            CourseScenery.Build(lLayout, pTheme, pRoot, pData.seed * 7 + 3);
            ChaosDirector.Create(pRoot, lLayout, pTheme, !pNoItems, pData.seed * 13 + 5);

            Debug.Log("[HS-GEN] length=" + lLayout.finishS.ToString("F0") + "m track=" + ((lTrackDone - lStart) * 1000f).ToString("F0") + "ms total=" + ((Time.realtimeSinceStartup - lStart) * 1000f).ToString("F0")
                + "ms segments=" + pData.segments.Count + " blocks=" + pData.blocks.Count + " chunks=" + TrackBaker.Chunks.Count + " items=" + lCount + " dives=" + pData.dives);
            return lLayout;
        }

        // One object in the world. Returns its root (null for meteors handed to the director, or nothing placed).
        public static Object Place(ItemData pItem, LevelLayout pLayout, StageData pData, Theme pTheme, Transform pParent, MeteorDirector pDirector)
        {
            switch (pItem.kind)
            {
                case ItemKind.Pad:
                    if (!pLayout.field.Solid(pItem.s)) return null;
                    return BoostPad.CreateDraped(pParent, pLayout, pItem.s, pItem.lateral, pItem.width, pItem.length, pTheme.accent, pItem.value);
                case ItemKind.Strip:
                    return JumpStrip.Create(pParent, pLayout, pItem.s, pItem.lateral, pItem.width, pItem.value, pTheme.roadEdge);
                case ItemKind.Ring:
                    return Ring.Create(pParent, pLayout, pItem.s, pItem.lateral, pItem.height, pItem.size, pTheme.accent);
                case ItemKind.Crate:
                    return Crate.Create(pParent, pLayout, pItem.s, pItem.lateral, pItem.size, pItem.flag, pTheme);
                case ItemKind.Laser:
                    return LaserGate.Create(pParent, pLayout, pItem.s, pItem.height, Mathf.Max(1, pItem.variant), pItem.value, pTheme);
                case ItemKind.Hammer:
                    return Hammer.Create(pParent, pLayout, pItem.s, pItem.value, pTheme);
                case ItemKind.Pane:
                    return Pane.Create(pParent, pLayout, pItem.s, pItem.lateral, pItem.width, pItem.height, (PaneKind)Mathf.Clamp(pItem.variant, 0, 2), pTheme);
                case ItemKind.Rope:
                    return PlaceRope(pItem, pLayout, pTheme, pParent);
                case ItemKind.Mover:
                    return MovingPlatform.Create(pParent, pLayout, pItem.s, pItem.lateral, pItem.width, pItem.length, pItem.height, pItem.value, pTheme);
                case ItemKind.Bridge:
                    return BuildBridge(pItem, pLayout, pTheme, pParent);
                case ItemKind.Meteor:
                    if (pDirector != null) pDirector.Add(pItem.s, pItem.flag, pItem.size, pItem.lateral);
                    return null;
                case ItemKind.Prop:
                    return PlaceProp(pItem, pLayout, pData, pTheme, pParent);
                case ItemKind.Arch:
                    GateKit.Arch(pParent, pLayout, pItem.s, pTheme);
                    return null;
                case ItemKind.GravityGate:
                    GateKit.GravityHoop(pParent, pLayout, pItem.s, pTheme);
                    return null;
            }
            return null;
        }

        private static Object PlaceProp(ItemData pItem, LevelLayout pLayout, StageData pData, Theme pTheme, Transform pParent)
        {
            SpineSample lSample = pLayout.spine.Evaluate(pItem.s);
            Vector3 lBase = pLayout.field.Point(pItem.s, pItem.lateral);
            Vector3 lUp = pLayout.field.Normal(pItem.s);
            Color lColor = pItem.variant == 0 ? pTheme.decorA : pItem.variant == 1 ? pTheme.decorB : pTheme.decorC;
            Quaternion lRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(lSample.forward, lUp), lUp) * Quaternion.Euler(pItem.value * 0.5f, pItem.jitter, pItem.value);
            return Monolith.Create(pParent, lBase, lRotation, new Vector3(pItem.width, pItem.height, pItem.length), pItem.rough, lColor, pTheme, pData.seed + (int)pItem.s);
        }

        private static Object PlaceRope(ItemData pItem, LevelLayout pLayout, Theme pTheme, Transform pParent)
        {
            GameObject lObject = new GameObject("Rope");
            lObject.transform.SetParent(pParent, false);
            GrindRail lRail = lObject.AddComponent<GrindRail>();
            lRail.catchRadius = 14f;
            float lStep = Mathf.Max(8f, 10f * pLayout.profile.k);
            float lSpan = Mathf.Max(1f, pItem.s1 - pItem.s);
            for (float lS = pItem.s; lS < pItem.s1; lS += lStep)
            {
                float lT = (lS - pItem.s) / lSpan;
                SpineSample lSample = pLayout.spine.Evaluate(lS);
                float lHeight = Mathf.Lerp(pItem.height, pItem.height1, lT);
                lRail.AddPoint(lSample.position + lSample.Right * pItem.lateral + lSample.up * lHeight, lSample.up, lS);
            }
            lRail.Finish(pTheme.accent, 4f);
            pLayout.rails.Add(lRail);
            return lObject;
        }

        // The raised lane : a thick deck following the line, a glowing edge on each side.
        private static Object BuildBridge(ItemData pItem, LevelLayout pLayout, Theme pTheme, Transform pParent)
        {
            MeshData lData = new MeshData();
            MeshData lGlow = new MeshData();
            float lThick = 6f;
            float lHalf = Mathf.Max(4f, pItem.width);
            Vector3 lPrevL = Vector3.zero, lPrevR = Vector3.zero, lPrevDown = Vector3.zero;
            bool lFirst = true;
            for (float lS = pItem.s; lS <= pItem.s1 + 0.1f; lS += 8f)
            {
                SpineSample lSample = pLayout.spine.Evaluate(Mathf.Min(lS, pItem.s1));
                Vector3 lUp = lSample.up;
                Vector3 lCenter = lSample.position + lSample.Right * pItem.lateral + lUp * pItem.height;
                Vector3 lL = lCenter - lSample.Right * lHalf;
                Vector3 lR = lCenter + lSample.Right * lHalf;
                Vector3 lDown = -lUp * lThick;
                if (!lFirst)
                {
                    lData.FacetQuad(lPrevL, lL, lR, lPrevR, lUp, Color.Lerp(pTheme.roadA, Color.white, 0.15f));
                    lData.FacetQuad(lPrevL + lPrevDown, lPrevL, lL, lL + lDown, -lSample.Right, pTheme.roadSide);
                    lData.FacetQuad(lPrevR, lPrevR + lPrevDown, lR + lDown, lR, lSample.Right, pTheme.roadSide);
                    lData.FacetQuad(lPrevL + lPrevDown, lL + lDown, lR + lDown, lPrevR + lPrevDown, -lUp, Color.Lerp(pTheme.roadSide, Color.black, 0.3f));
                    lGlow.FacetQuad(lPrevL + lUp * 0.2f, lL + lUp * 0.2f, lL + lUp * 0.2f + lSample.Right * 2f, lPrevL + lUp * 0.2f + lSample.Right * 2f, lUp, Color.white);
                    lGlow.FacetQuad(lPrevR + lUp * 0.2f - lSample.Right * 2f, lR + lUp * 0.2f - lSample.Right * 2f, lR + lUp * 0.2f, lPrevR + lUp * 0.2f, lUp, Color.white);
                }
                lPrevL = lL;
                lPrevR = lR;
                lPrevDown = lDown;
                lFirst = false;
            }
            GameObject lObject = new GameObject("ShortcutBridge");
            lObject.transform.SetParent(pParent, false);
            lObject.layer = Layers.PROP;
            Mesh lMesh = lData.ToMesh("Bridge");
            lObject.AddComponent<MeshFilter>().sharedMesh = lMesh;
            lObject.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(pTheme.shade, true);
            lObject.AddComponent<MeshCollider>().sharedMesh = lMesh;
            GameObject lEdges = new GameObject("Edges");
            lEdges.transform.SetParent(lObject.transform, false);
            lEdges.AddComponent<MeshFilter>().sharedMesh = lGlow.ToMesh("BridgeEdges");
            lEdges.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(Color.Lerp(pTheme.accent, Color.white, 0.3f) * 3f, false, 0f, 0f, true);
            return lObject;
        }

        // ================================================================ positions (tools)

        // Where an object stands in the world : on the track at (s, lateral), raised by its height.
        public static Vector3 ItemPosition(ItemData pItem, LevelLayout pLayout)
        {
            float lS = Mathf.Clamp(pItem.s, pLayout.spine.StartS, pLayout.spine.EndS);
            float lLateral = pItem.kind == ItemKind.Laser || pItem.kind == ItemKind.Hammer || pItem.kind == ItemKind.Arch || pItem.kind == ItemKind.GravityGate
                ? pLayout.field.CenterLateral(lS) : pItem.lateral;
            if (pItem.kind == ItemKind.Rope || pItem.kind == ItemKind.Bridge)
            {
                SpineSample lSample = pLayout.spine.Evaluate(lS);
                return lSample.position + lSample.Right * pItem.lateral + lSample.up * pItem.height;
            }
            float lHeight = pItem.kind == ItemKind.Ring || pItem.kind == ItemKind.Laser ? pItem.height : pItem.kind == ItemKind.Crate ? pItem.size * 0.5f : 0f;
            return pLayout.field.Point(lS, lLateral) + pLayout.field.Normal(lS) * lHeight;
        }
    }
}
