using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Turtle-style track generator : a cursor frame walks forward and extrudes road / wall meshes.
    // Every public piece returns the builder so levels read like a script.
    public class TrackBuilder
    {
        public const float STEP = 2f;
        public const float THICKNESS = 2.5f;
        public const float DEFAULT_WIDTH = 16f;
        private const float EDGE = 0.8f;
        private const float BAND = 8f;
        private const float CHECKPOINT_SPACING = 170f;
        private const float WALL_LOW = -10f;
        private const float WALL_HIGH = 9f;

        private enum WallMode { None, Both, Zigzag, Left, Right }

        private struct Frame
        {
            public Vector3 position;
            public Quaternion rotation;
            public float width;
            public float distance;
            public Vector3 Right => rotation * Vector3.right;
            public Vector3 Up => rotation * Vector3.up;
            public Vector3 Forward => rotation * Vector3.forward;
        }

        private class MeshData
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Vector3> normals = new List<Vector3>();
            public readonly List<Color> colors = new List<Color>();
            public readonly List<int> triangles = new List<int>();

            public void Quad(Vector3 pA, Vector3 pB, Vector3 pC, Vector3 pD, Vector3 pNA, Vector3 pNB, Vector3 pNC, Vector3 pND, Color pColor)
            {
                int lIndex = vertices.Count;
                vertices.Add(pA); vertices.Add(pB); vertices.Add(pC); vertices.Add(pD);
                normals.Add(pNA); normals.Add(pNB); normals.Add(pNC); normals.Add(pND);
                colors.Add(pColor); colors.Add(pColor); colors.Add(pColor); colors.Add(pColor);
                Vector3 lFace = Vector3.Cross(pB - pA, pC - pA);
                if (Vector3.Dot(lFace, pNA + pNC) >= 0f)
                {
                    triangles.Add(lIndex); triangles.Add(lIndex + 1); triangles.Add(lIndex + 2);
                    triangles.Add(lIndex); triangles.Add(lIndex + 2); triangles.Add(lIndex + 3);
                }
                else
                {
                    triangles.Add(lIndex); triangles.Add(lIndex + 2); triangles.Add(lIndex + 1);
                    triangles.Add(lIndex); triangles.Add(lIndex + 3); triangles.Add(lIndex + 2);
                }
            }

            public void Quad(Vector3 pA, Vector3 pB, Vector3 pC, Vector3 pD, Vector3 pNormal, Color pColor)
            {
                Quad(pA, pB, pC, pD, pNormal, pNormal, pNormal, pNormal, pColor);
            }

            public void Clear()
            {
                vertices.Clear();
                normals.Clear();
                colors.Clear();
                triangles.Clear();
            }
        }

        public readonly TrackPath path = new TrackPath();
        public readonly Transform root;

        private readonly Theme _theme;
        private readonly Material _material;
        private readonly MeshData _mesh = new MeshData();
        private readonly bool _pillars;

        private Vector3 _position;
        private Quaternion _rotation = Quaternion.identity;
        private float _distance;
        private float _width = DEFAULT_WIDTH;
        private float _targetWidth = DEFAULT_WIDTH;
        private float _lastCheckpoint;
        private float _lastPillar;
        private int _chunkIndex;

        public float Distance => _distance;
        public Theme Theme => _theme;

        public TrackBuilder(Transform pRoot, Theme pTheme)
        {
            root = pRoot;
            _theme = pTheme;
            _material = Mats.Toon(Color.white, pTheme.shade, 0f, 0.15f, true);
            _pillars = pTheme.decor != DecorKind.Space && pTheme.decor != DecorKind.Void;
            AddSample(true);
        }

        private Frame Current => new Frame { position = _position, rotation = _rotation, width = _width, distance = _distance };

        private void AddSample(bool pSolid)
        {
            path.Add(new TrackSample { position = _position, rotation = _rotation, distance = _distance, width = _width, solid = pSolid });
        }

        // ---------------------------------------------------------------- core stepping

        private void Step(float pDs, float pYaw, float pPitch, float pRoll, float pLateral, bool pSolid)
        {
            Quaternion lBefore = _rotation;
            if (pYaw != 0f)
            {
                Vector3 lUp = _rotation * Vector3.up;
                Vector3 lAxis = Vector3.Dot(lUp, Vector3.up) > 0.5f ? Vector3.up : lUp;
                _rotation = Quaternion.AngleAxis(pYaw, lAxis) * _rotation;
            }
            if (pPitch != 0f) _rotation = _rotation * Quaternion.Euler(pPitch, 0f, 0f);
            if (pRoll != 0f) _rotation = _rotation * Quaternion.Euler(0f, 0f, pRoll);
            Quaternion lMid = Quaternion.Slerp(lBefore, _rotation, 0.5f);
            _position += lMid * Vector3.forward * pDs + lMid * Vector3.right * pLateral;
            _distance += Mathf.Sqrt(pDs * pDs + pLateral * pLateral);
            _width = Mathf.MoveTowards(_width, _targetWidth, pDs * 0.35f);
            AddSample(pSolid);
        }

        private delegate Vector3 AngleCurve(float pT);

        private void Run(float pLength, AngleCurve pAngles, float pLateral, bool pRoad, WallMode pWalls)
        {
            int lSteps = Mathf.Max(1, Mathf.CeilToInt(pLength / STEP));
            float lDs = pLength / lSteps;
            Vector3 lPrevAngles = pAngles(0f);
            Frame lPrev = Current;
            float lStart = _distance;
            for (int i = 1; i <= lSteps; i++)
            {
                float lT = i / (float)lSteps;
                Vector3 lAngles = pAngles(lT);
                Vector3 lDelta = lAngles - lPrevAngles;
                lPrevAngles = lAngles;
                Step(lDs, lDelta.x, lDelta.y, lDelta.z, pLateral / lSteps, pRoad);
                Frame lCur = Current;
                if (pRoad) EmitRoad(lPrev, lCur);
                if (pWalls != WallMode.None) EmitWalls(lPrev, lCur, pWalls, lPrev.distance - lStart);
                lPrev = lCur;
            }
            Flush();
        }

        private static float Smooth(float pT) => pT * pT * (3f - 2f * pT);

        private static float Bump(float pT, float pEdge) => Smooth(Mathf.Clamp01(pT / pEdge)) - Smooth(Mathf.Clamp01((pT - (1f - pEdge)) / pEdge));

        private void Flatten()
        {
            Vector3 lForward = Vector3.ProjectOnPlane(_rotation * Vector3.forward, Vector3.up);
            if (lForward.sqrMagnitude < 0.01f) lForward = _rotation * Vector3.up;
            _rotation = Quaternion.LookRotation(lForward.normalized, Vector3.up);
        }

        private void MaybeCheckpoint()
        {
            if (_distance - _lastCheckpoint < CHECKPOINT_SPACING) return;
            if (Vector3.Dot(_rotation * Vector3.up, Vector3.up) < 0.8f) return;
            _lastCheckpoint = _distance;
            path.checkpoints.Add(_distance);
            Frame lFrame = Current;
            Material lGlow = Mats.Glow(_theme.accent * 1.8f, false, 0f, 0f, true);
            for (int lSide = -1; lSide <= 1; lSide += 2)
            {
                GameObject lPost = Mats.Primitive(PrimitiveType.Cube, root, lFrame.position + lFrame.Right * lSide * (lFrame.width * 0.5f + 0.6f) + lFrame.Up * 3f, new Vector3(0.6f, 6f, 0.6f), lGlow);
                lPost.transform.rotation = lFrame.rotation;
            }
        }

        // ---------------------------------------------------------------- mesh emission

        private Vector3 Local(Vector3 pWorld) => root.InverseTransformPoint(pWorld);

        private static Vector3 Point(Frame pFrame, float pX, float pY) => pFrame.position + pFrame.Right * pX + pFrame.Up * pY;

        private void EmitRoad(Frame pA, Frame pB)
        {
            float lHalfA = pA.width * 0.5f;
            float lHalfB = pB.width * 0.5f;
            bool lBand = Mathf.FloorToInt(pA.distance / BAND) % 2 == 0;
            Color lCenter = lBand ? _theme.roadA : _theme.roadB;
            Color lLine = Color.Lerp(lCenter, Color.white, 0.55f);

            float[] lFractions = { -1f, -1f + EDGE / lHalfA, -0.03f, 0.03f, 1f - EDGE / lHalfA, 1f };
            Color[] lColors = { _theme.roadEdge, lCenter, lBand ? lLine : lCenter, lCenter, _theme.roadEdge };
            for (int i = 0; i < 5; i++)
            {
                Vector3 lA0 = Point(pA, lFractions[i] * lHalfA, 0f);
                Vector3 lA1 = Point(pA, lFractions[i + 1] * lHalfA, 0f);
                Vector3 lB0 = Point(pB, lFractions[i] * lHalfB, 0f);
                Vector3 lB1 = Point(pB, lFractions[i + 1] * lHalfB, 0f);
                _mesh.Quad(Local(lA0), Local(lB0), Local(lB1), Local(lA1), Dir(pA.Up), Dir(pB.Up), Dir(pB.Up), Dir(pA.Up), lColors[i]);
            }

            Color lSide = lBand ? _theme.roadSide : _theme.roadSide * 0.88f;
            for (int lSign = -1; lSign <= 1; lSign += 2)
            {
                Vector3 lTopA = Point(pA, lSign * lHalfA, 0f);
                Vector3 lTopB = Point(pB, lSign * lHalfB, 0f);
                Vector3 lBotA = Point(pA, lSign * lHalfA, -THICKNESS);
                Vector3 lBotB = Point(pB, lSign * lHalfB, -THICKNESS);
                _mesh.Quad(Local(lTopA), Local(lTopB), Local(lBotB), Local(lBotA), Dir(pA.Right * lSign), Dir(pB.Right * lSign), Dir(pB.Right * lSign), Dir(pA.Right * lSign), lSide);
            }
            _mesh.Quad(Local(Point(pA, -lHalfA, -THICKNESS)), Local(Point(pB, -lHalfB, -THICKNESS)), Local(Point(pB, lHalfB, -THICKNESS)), Local(Point(pA, lHalfA, -THICKNESS)),
                Dir(-pA.Up), Dir(-pB.Up), Dir(-pB.Up), Dir(-pA.Up), _theme.roadSide * 0.7f);

            if (_pillars && pA.distance - _lastPillar > 42f && Vector3.Dot(pA.Up, Vector3.up) > 0.95f)
            {
                _lastPillar = pA.distance;
                float lHeight = Random.Range(40f, 90f);
                GameObject lPillar = Mats.Primitive(PrimitiveType.Cube, root, pA.position - Vector3.up * (THICKNESS + lHeight * 0.5f), new Vector3(pA.width * 0.35f, lHeight, 3f), Mats.Toon(_theme.roadSide * 0.9f, _theme.shade, 0f));
                lPillar.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(pA.Forward, Vector3.up), Vector3.up);
            }
        }

        private Vector3 Dir(Vector3 pWorld) => root.InverseTransformDirection(pWorld);

        private void EmitWalls(Frame pA, Frame pB, WallMode pMode, float pLocalDistance)
        {
            for (int lSign = -1; lSign <= 1; lSign += 2)
            {
                if (!WallActive(pMode, lSign, pLocalDistance)) continue;
                float lInnerA = lSign * (pA.width * 0.5f + 0.4f);
                float lInnerB = lSign * (pB.width * 0.5f + 0.4f);
                float lOuterA = lInnerA + lSign * 1.4f;
                float lOuterB = lInnerB + lSign * 1.4f;
                bool lBand = Mathf.FloorToInt(pA.distance / BAND) % 2 == 0;
                Color lWall = lBand ? _theme.wall : Color.Lerp(_theme.wall, _theme.accent, 0.3f);

                // Inner face (the one you run on)
                _mesh.Quad(Local(Point(pA, lInnerA, WALL_HIGH)), Local(Point(pB, lInnerB, WALL_HIGH)), Local(Point(pB, lInnerB, WALL_LOW)), Local(Point(pA, lInnerA, WALL_LOW)),
                    Dir(-pA.Right * lSign), Dir(-pB.Right * lSign), Dir(-pB.Right * lSign), Dir(-pA.Right * lSign), lWall);
                // Outer face
                _mesh.Quad(Local(Point(pA, lOuterA, WALL_HIGH)), Local(Point(pB, lOuterB, WALL_HIGH)), Local(Point(pB, lOuterB, WALL_LOW)), Local(Point(pA, lOuterA, WALL_LOW)),
                    Dir(pA.Right * lSign), Dir(pB.Right * lSign), Dir(pB.Right * lSign), Dir(pA.Right * lSign), _theme.roadSide);
                // Top cap
                _mesh.Quad(Local(Point(pA, lInnerA, WALL_HIGH)), Local(Point(pB, lInnerB, WALL_HIGH)), Local(Point(pB, lOuterB, WALL_HIGH)), Local(Point(pA, lOuterA, WALL_HIGH)),
                    Dir(pA.Up), Dir(pB.Up), Dir(pB.Up), Dir(pA.Up), _theme.roadEdge);
                // Bottom cap
                _mesh.Quad(Local(Point(pA, lInnerA, WALL_LOW)), Local(Point(pB, lInnerB, WALL_LOW)), Local(Point(pB, lOuterB, WALL_LOW)), Local(Point(pA, lOuterA, WALL_LOW)),
                    Dir(-pA.Up), Dir(-pB.Up), Dir(-pB.Up), Dir(-pA.Up), _theme.roadSide * 0.7f);
                // Panel ends
                if (!WallActive(pMode, lSign, pLocalDistance - STEP))
                    _mesh.Quad(Local(Point(pA, lInnerA, WALL_HIGH)), Local(Point(pA, lOuterA, WALL_HIGH)), Local(Point(pA, lOuterA, WALL_LOW)), Local(Point(pA, lInnerA, WALL_LOW)), Dir(-pA.Forward), _theme.roadEdge);
                if (!WallActive(pMode, lSign, pLocalDistance + STEP))
                    _mesh.Quad(Local(Point(pB, lInnerB, WALL_HIGH)), Local(Point(pB, lOuterB, WALL_HIGH)), Local(Point(pB, lOuterB, WALL_LOW)), Local(Point(pB, lInnerB, WALL_LOW)), Dir(pB.Forward), _theme.roadEdge);
            }
        }

        private static bool WallActive(WallMode pMode, int pSign, float pLocalDistance)
        {
            if (pLocalDistance < 0f) return false;
            switch (pMode)
            {
                case WallMode.Both: return true;
                case WallMode.Left: return pSign < 0;
                case WallMode.Right: return pSign > 0;
                case WallMode.Zigzag:
                    for (int k = 0; k < 64; k++)
                    {
                        float lStart = k * 32f;
                        if (lStart > pLocalDistance) break;
                        int lSide = k % 2 == 0 ? -1 : 1;
                        if (lSide == pSign && pLocalDistance >= lStart && pLocalDistance <= lStart + 38f) return true;
                    }
                    return false;
            }
            return false;
        }

        private void Flush()
        {
            if (_mesh.vertices.Count == 0) return;
            Mesh lMesh = new Mesh();
            lMesh.name = "TrackChunk" + _chunkIndex;
            lMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            lMesh.SetVertices(_mesh.vertices);
            lMesh.SetNormals(_mesh.normals);
            lMesh.SetColors(_mesh.colors);
            lMesh.SetTriangles(_mesh.triangles, 0);
            lMesh.RecalculateBounds();

            GameObject lChunk = new GameObject("TrackChunk" + _chunkIndex++);
            lChunk.transform.SetParent(root, false);
            lChunk.AddComponent<MeshFilter>().sharedMesh = lMesh;
            lChunk.AddComponent<MeshRenderer>().sharedMaterial = _material;
            lChunk.AddComponent<MeshCollider>().sharedMesh = lMesh;
            _mesh.Clear();
        }

        // ---------------------------------------------------------------- pieces

        public TrackBuilder Straight(float pLength)
        {
            MaybeCheckpoint();
            Run(pLength, pT => Vector3.zero, 0f, true, WallMode.None);
            return this;
        }

        public TrackBuilder Turn(float pAngle, float pRadius = 90f, float pBank = 14f)
        {
            MaybeCheckpoint();
            float lLength = Mathf.Abs(pAngle) * Mathf.Deg2Rad * pRadius;
            float lSign = Mathf.Sign(pAngle);
            Run(lLength, pT => new Vector3(pAngle * Smooth(pT), 0f, -lSign * pBank * Mathf.Sin(pT * Mathf.PI)), 0f, true, WallMode.None);
            return this;
        }

        public TrackBuilder STurn(float pAngle, float pRadius = 90f)
        {
            return Turn(pAngle, pRadius).Turn(-pAngle, pRadius);
        }

        // Positive angle climbs, negative descends ; ends level.
        public TrackBuilder Hill(float pLength, float pAngle)
        {
            MaybeCheckpoint();
            Run(pLength, pT => new Vector3(0f, -pAngle * Bump(pT, 0.3f), 0f), 0f, true, WallMode.None);
            return this;
        }

        public TrackBuilder Drop(float pLength, float pAngle = 25f) => Hill(pLength, -pAngle);

        // Kicker : ends pitched up, must be followed by a Gap.
        public TrackBuilder Ramp(float pLength, float pAngle = 18f)
        {
            Run(pLength, pT => new Vector3(0f, -pAngle * Smooth(pT), 0f), 0f, true, WallMode.None);
            return this;
        }

        public TrackBuilder Gap(float pLength, float pDrop = 0f)
        {
            Flatten();
            int lSteps = Mathf.Max(1, Mathf.CeilToInt(pLength / STEP));
            float lDs = pLength / lSteps;
            for (int i = 0; i < lSteps; i++)
            {
                _position += _rotation * Vector3.forward * lDs - Vector3.up * (pDrop / lSteps);
                _distance += lDs;
                AddSample(false);
            }
            return this;
        }

        public TrackBuilder Loop(float pRadius = 24f)
        {
            MaybeCheckpoint();
            Flatten();
            float lLength = 2f * Mathf.PI * pRadius;
            float lShift = _width + 6f;
            Run(lLength, pT => new Vector3(0f, -360f * pT, 0f), lShift, true, WallMode.None);
            Flatten();
            return this;
        }

        public TrackBuilder Corkscrew(float pLength, float pTurns = 1f)
        {
            MaybeCheckpoint();
            Run(pLength, pT => new Vector3(0f, 0f, 360f * pTurns * Smooth(pT)), 0f, true, WallMode.None);
            return this;
        }

        // Banked wall ride : the road rolls up to pAngle then back, gravity follows.
        public TrackBuilder WallRide(float pLength, float pAngle = 80f, float pYaw = 0f)
        {
            MaybeCheckpoint();
            Run(pLength, pT => new Vector3(pYaw * Smooth(pT), 0f, pAngle * Bump(pT, 0.3f)), 0f, true, WallMode.None);
            return this;
        }

        public TrackBuilder Narrow(float pLength, float pWidth)
        {
            _targetWidth = pWidth;
            return Straight(pLength);
        }

        public TrackBuilder Widen(float pLength)
        {
            _targetWidth = DEFAULT_WIDTH;
            return Straight(pLength);
        }

        public TrackBuilder Corridor(float pLength, float pYaw = 0f)
        {
            MaybeCheckpoint();
            Run(pLength, pT => new Vector3(pYaw * Smooth(pT), 0f, 0f), 0f, true, WallMode.Both);
            return this;
        }

        // Floorless section : run on the walls and wall jump across.
        public TrackBuilder WallRun(float pLength, bool pZigzag = true)
        {
            MaybeCheckpoint();
            float lWidth = _width;
            _width = 13f;
            _targetWidth = 13f;
            Run(pLength, pT => Vector3.zero, 0f, false, pZigzag ? WallMode.Zigzag : WallMode.Both);
            _width = lWidth;
            _targetWidth = lWidth;
            return this;
        }

        // Floorless section crossed by a grind rail.
        public TrackBuilder RailGap(float pLength, float pYaw = 0f, float pDip = 0f)
        {
            MaybeCheckpoint();
            GrindRail lRail = new GameObject("GrindRail").AddComponent<GrindRail>();
            lRail.transform.SetParent(root, false);
            lRail.AddPoint(_position + (_rotation * Vector3.up) * 0.9f - (_rotation * Vector3.forward) * 6f, _rotation * Vector3.up);
            int lSteps = Mathf.Max(1, Mathf.CeilToInt(pLength / STEP));
            float lDs = pLength / lSteps;
            Vector3 lPrev = Vector3.zero;
            for (int i = 1; i <= lSteps; i++)
            {
                float lT = i / (float)lSteps;
                float lPrevT = (i - 1) / (float)lSteps;
                Vector3 lAngles = new Vector3(pYaw * Smooth(lT), pDip * Mathf.Sin(lT * Mathf.PI * 2f), 0f);
                Vector3 lDelta = lAngles - lPrev;
                lPrev = lAngles;
                Step(lDs, lDelta.x, lDelta.y, 0f, 0f, false);
                if (i % 2 == 0 || i == lSteps) lRail.AddPoint(_position + (_rotation * Vector3.up) * 0.9f, _rotation * Vector3.up);
            }
            lRail.AddPoint(_position + (_rotation * Vector3.up) * 0.9f + (_rotation * Vector3.forward) * 6f, _rotation * Vector3.up);
            lRail.Finish(_theme.accent, _theme.roadSide);
            Flatten();
            return this;
        }

        // Road with optional rails along both edges.
        public TrackBuilder SideRails(float pLength)
        {
            float lStart = _distance;
            Straight(pLength);
            for (int lSide = -1; lSide <= 1; lSide += 2)
            {
                GrindRail lRail = new GameObject("SideRail").AddComponent<GrindRail>();
                lRail.transform.SetParent(root, false);
                for (float lD = lStart + 4f; lD <= lStart + pLength - 4f; lD += 4f)
                {
                    TrackSample lSample = path.Evaluate(lD);
                    lRail.AddPoint(lSample.position + lSample.Right * lSide * (lSample.width * 0.5f + 1.2f) + lSample.Up * 2.2f, lSample.Up);
                }
                lRail.Finish(_theme.accent, _theme.roadSide);
            }
            return this;
        }

        // ---------------------------------------------------------------- features

        private Quaternion RotationAt(TrackSample pSample) => Quaternion.LookRotation(pSample.Forward, pSample.Up);

        public TrackBuilder Pad(float pLateral = 0f, float pBack = 6f)
        {
            TrackSample lSample = path.Evaluate(_distance - pBack);
            BoostPad.Create(root, lSample.position + lSample.Right * pLateral, RotationAt(lSample), _theme.accent);
            return this;
        }

        public TrackBuilder Launch(float pBack = 6f)
        {
            TrackSample lSample = path.Evaluate(_distance - pBack);
            LaunchPad.Create(root, lSample.position, RotationAt(lSample), _theme.accent);
            return this;
        }

        public TrackBuilder BoostStrip(float pLength, int pCount)
        {
            float lStart = _distance;
            Straight(pLength);
            for (int i = 0; i < pCount; i++)
            {
                float lD = lStart + (i + 0.5f) * pLength / pCount;
                TrackSample lSample = path.Evaluate(lD);
                float lLateral = pCount > 1 ? ((i % 3) - 1) * lSample.width * 0.25f : 0f;
                BoostPad.Create(root, lSample.position + lSample.Right * lLateral, RotationAt(lSample), _theme.accent);
            }
            return this;
        }

        public TrackBuilder Rings(float pGapLength, float pDrop, int pCount, float pArc = 7f)
        {
            Flatten();
            Vector3 lStart = _position;
            Quaternion lRotation = _rotation;
            Gap(pGapLength, pDrop);
            Vector3 lEnd = _position;
            for (int i = 0; i < pCount; i++)
            {
                float lT = (i + 1f) / (pCount + 1f);
                Vector3 lPoint = Vector3.Lerp(lStart, lEnd, lT) + Vector3.up * (pArc * 4f * lT * (1f - lT) + 2.5f);
                BoostRing.Create(root, lPoint, lRotation, _theme.accent, 4.5f);
            }
            return this;
        }

        public TrackBuilder RingLine(float pLength, int pCount, float pHeight = 3f)
        {
            float lStart = _distance;
            Straight(pLength);
            for (int i = 0; i < pCount; i++)
            {
                TrackSample lSample = path.Evaluate(lStart + (i + 0.5f) * pLength / pCount);
                BoostRing.Create(root, lSample.position + lSample.Up * pHeight, RotationAt(lSample), _theme.accent, 4f);
            }
            return this;
        }

        public TrackBuilder IceField(float pLength, float pRowSpacing = 6f)
        {
            Pad(0f, 10f);
            float lStart = _distance;
            Straight(pLength);
            for (float lD = lStart + 12f; lD < lStart + pLength - 4f; lD += pRowSpacing)
            {
                TrackSample lSample = path.Evaluate(lD);
                float lHalf = lSample.width * 0.5f - 1.2f;
                for (float lX = -lHalf; lX <= lHalf; lX += 3.2f)
                    PropFactory.Ice(root, lSample.position + lSample.Right * (lX + Random.Range(-0.5f, 0.5f)), RotationAt(lSample), Random.Range(1.2f, 1.8f));
            }
            return this;
        }

        public TrackBuilder Crates(float pLength, int pCount)
        {
            float lStart = _distance;
            Straight(pLength);
            for (int i = 0; i < pCount; i++)
            {
                TrackSample lSample = path.Evaluate(lStart + 10f + Random.value * (pLength - 15f));
                float lLateral = Random.Range(-lSample.width * 0.4f, lSample.width * 0.4f);
                PropFactory.Crate(root, lSample.position + lSample.Right * lLateral, RotationAt(lSample), Color.Lerp(_theme.decorB, new Color(0.85f, 0.6f, 0.3f), 0.5f));
            }
            return this;
        }

        public TrackBuilder Wall(float pBack = 0f)
        {
            // Full-width wall of crates : punch through or dash through.
            TrackSample lSample = path.Evaluate(_distance - pBack);
            float lHalf = lSample.width * 0.5f - 1.4f;
            for (float lX = -lHalf; lX <= lHalf + 0.1f; lX += 2.9f)
                PropFactory.Crate(root, lSample.position + lSample.Right * lX, RotationAt(lSample), _theme.accent);
            return this;
        }

        public TrackBuilder Traffic(float pLength, int pCount, float pSpeed = 22f)
        {
            float lStart = _distance;
            Straight(pLength);
            Color[] lColors = { new Color(1f, 0.85f, 0.1f), new Color(0.2f, 0.6f, 1f), new Color(1f, 0.3f, 0.4f), Color.white, new Color(0.5f, 1f, 0.4f) };
            for (int i = 0; i < pCount; i++)
            {
                float lD = lStart + 20f + i * (pLength - 20f) / pCount;
                TrackSample lSample = path.Evaluate(lD);
                float lLateral = (Random.Range(0, 3) - 1) * lSample.width * 0.3f;
                Breakable lCar = PropFactory.Car(root, lSample.position + lSample.Right * lLateral, RotationAt(lSample), lColors[i % lColors.Length]);
                TrafficCar lTraffic = lCar.gameObject.AddComponent<TrafficCar>();
                lTraffic.distance = lD;
                lTraffic.lateral = lLateral;
                lTraffic.speed = pSpeed * Random.Range(0.85f, 1.15f);
            }
            return this;
        }

        public TrackBuilder Meteors(float pLength, float pInterval = 0.8f)
        {
            float lStart = _distance;
            Straight(pLength);
            MeteorZone lZone = new GameObject("MeteorZone").AddComponent<MeteorZone>();
            lZone.transform.SetParent(root, false);
            lZone.startDistance = lStart;
            lZone.endDistance = lStart + pLength;
            lZone.interval = pInterval;
            return this;
        }

        public TrackBuilder Hint(string pText)
        {
            path.hints.Add(new KeyValuePair<float, string>(_distance, pText));
            return this;
        }

        public TrackBuilder Start()
        {
            Straight(60f);
            Gate(path.Evaluate(30f), false);
            return this;
        }

        public TrackBuilder Finish()
        {
            Straight(40f);
            path.finishDistance = _distance;
            Gate(path.Evaluate(_distance), true);
            Straight(220f);
            return this;
        }

        private void Gate(TrackSample pSample, bool pFinish)
        {
            Transform lGate = new GameObject(pFinish ? "FinishGate" : "StartGate").transform;
            lGate.SetParent(root, false);
            lGate.SetPositionAndRotation(pSample.position, RotationAt(pSample));
            float lHalf = pSample.width * 0.5f + 1.5f;
            Color lPost = pFinish ? UIFactory.RED : _theme.roadSide;
            PropFactory.Part(PrimitiveType.Cube, lGate, new Vector3(-lHalf, 6f, 0f), new Vector3(1.6f, 12f, 1.6f), lPost);
            PropFactory.Part(PrimitiveType.Cube, lGate, new Vector3(lHalf, 6f, 0f), new Vector3(1.6f, 12f, 1.6f), lPost);
            int lCount = 12;
            for (int i = 0; i < lCount; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    float lX = -lHalf + (i + 0.5f) * (lHalf * 2f / lCount);
                    Color lColor = (i + j) % 2 == 0 ? Color.white : UIFactory.BLACK;
                    PropFactory.Part(PrimitiveType.Cube, lGate, new Vector3(lX, 11f + j * 1.2f, 0f), new Vector3(lHalf * 2f / lCount, 1.2f, 0.6f), lColor, default(Vector3), 0f);
                }
            }
            PropFactory.GlowPart(PrimitiveType.Cube, lGate, new Vector3(0f, 0.08f, 0f), new Vector3(pSample.width, 0.1f, 1.2f), pFinish ? new Color(3f, 3f, 3f) : _theme.accent * 2f);
        }
    }
}
