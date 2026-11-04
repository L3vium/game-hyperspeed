using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Stage select : not a menu, a place. The stages stand along a road going to the horizon : a giant number planted in
    // the ground beside the actual vehicle you will race. Click one and you are in. Drag or scroll to travel the road.
    public class StageRoad : MonoBehaviour
    {
        private const float SPACING = 120f;
        private const float DRAG_PIXELS = 14f;
        private const float HIT_RADIUS = 44f;

        private class Station
        {
            public int index;
            public Transform root;
            public Transform model;
            public Transform number;
            public Vector3 center;
            public float lift;
        }

        private readonly List<Station> _stations = new List<Station>();
        private Camera _camera;
        private Action<int> _onPick;
        private float _position;
        private float _target;
        private int _hover = -1;
        private bool _pressed;
        private Vector2 _pressAt;
        private float _pressPosition;
        private bool _dragged;
        private Vector3 _cameraPosition;

        public int Selected => Mathf.Clamp(Mathf.RoundToInt(_target), 0, _stations.Count - 1);

        public static StageRoad Create(Camera pCamera, Vector3 pOrigin, int pStart, Action<int> pOnPick)
        {
            StageRoad lRoad = new GameObject("StageRoad").AddComponent<StageRoad>();
            lRoad.transform.position = pOrigin;
            lRoad._camera = pCamera;
            lRoad._onPick = pOnPick;
            lRoad.Build();
            lRoad._position = pStart;
            lRoad._target = pStart;
            lRoad.gameObject.SetActive(false);
            return lRoad;
        }

        private void Build()
        {
            Material lMaterial = Mats.Flat(new Color(0.62f, 0.58f, 0.78f), false);
            float lLength = LevelDatabase.Count * SPACING + 2400f;

            // The road : a band of each league's colour, wide, going far past the last stage.
            MeshData lGround = new MeshData();
            int lLeague = 0;
            for (int i = -6; i < LevelDatabase.Count + 18; i++)
            {
                int lIndex = Mathf.Clamp(i, 0, LevelDatabase.Count - 1);
                lLeague = LevelDatabase.Get(lIndex).world;
                Theme lTheme = LevelDatabase.Get(lIndex).theme();
                float lZ0 = i * SPACING - SPACING * 0.5f;
                float lZ1 = lZ0 + SPACING;
                Plane(lGround, -4000f, -110f, lZ0, lZ1, lTheme.roadA);
                Plane(lGround, 110f, 4000f, lZ0, lZ1, lTheme.roadB);
                Plane(lGround, -110f, 110f, lZ0, lZ1, lTheme.road);
                if (((i % 2) + 2) % 2 == 0) Plane(lGround, -3f, 3f, lZ0 + 10f, lZ0 + 70f, Color.white);
            }
            GameObject lRoadObject = new GameObject("Ground");
            lRoadObject.transform.SetParent(transform, false);
            lRoadObject.AddComponent<MeshFilter>().sharedMesh = lGround.ToMesh("StageRoad");
            lRoadObject.AddComponent<MeshRenderer>().sharedMaterial = lMaterial;

            // Enormous shapes on both sides, for depth.
            System.Random lRandom = new System.Random(5);
            MeshData lScenery = new MeshData();
            for (int i = 0; i < 70; i++)
            {
                float lZ = -200f + i * lLength / 70f;
                int lIndex = Mathf.Clamp(Mathf.RoundToInt(lZ / SPACING), 0, LevelDatabase.Count - 1);
                Theme lTheme = LevelDatabase.Get(lIndex).theme();
                int lSide = i % 2 == 0 ? -1 : 1;
                float lSize = 60f + (float)lRandom.NextDouble() * 220f;
                float lX = lSide * (260f + (float)lRandom.NextDouble() * 900f);
                Color lColor = i % 3 == 0 ? lTheme.decorA : i % 3 == 1 ? lTheme.decorB : lTheme.decorC;
                GateKit.Box(lScenery, new Vector3(lX, lSize * 0.5f, lZ), new Vector3(lSize * 0.8f, lSize, lSize * 0.8f), lColor);
            }
            GameObject lSceneryObject = new GameObject("Scenery");
            lSceneryObject.transform.SetParent(transform, false);
            lSceneryObject.AddComponent<MeshFilter>().sharedMesh = lScenery.ToMesh("StageScenery");
            lSceneryObject.AddComponent<MeshRenderer>().sharedMaterial = lMaterial;

            int lLastWorld = -1;
            for (int i = 0; i < LevelDatabase.Count; i++)
            {
                LevelDefinition lLevel = LevelDatabase.Get(i);
                BuildStation(i, lLevel, lMaterial);
                if (lLevel.world != lLastWorld)
                {
                    lLastWorld = lLevel.world;
                    PaintWord(LevelDatabase.WORLDS[lLevel.world], new Vector3(0f, 0.6f, i * SPACING - SPACING * 0.5f + 6f), 34f, 200f, Color.white);
                }
            }
        }

        private static void Plane(MeshData pData, float pX0, float pX1, float pZ0, float pZ1, Color pColor)
        {
            pData.Quad(new Vector3(pX0, 0f, pZ0), new Vector3(pX1, 0f, pZ0), new Vector3(pX1, 0f, pZ1), new Vector3(pX0, 0f, pZ1), Vector3.up, pColor);
        }

        private void BuildStation(int pIndex, LevelDefinition pLevel, Material pMaterial)
        {
            Theme lTheme = pLevel.theme();
            bool lUnlocked = GameSession.IsUnlocked(pIndex);
            int lSide = pIndex % 2 == 0 ? -1 : 1;
            Vector3 lCenter = new Vector3(lSide * 58f, 0f, pIndex * SPACING);

            GameObject lRoot = new GameObject("Stage" + pIndex);
            lRoot.transform.SetParent(transform, false);
            lRoot.transform.localPosition = lCenter;
            Station lStation = new Station { index = pIndex, root = lRoot.transform, center = lCenter };

            // Slab under the number and the vehicle.
            MeshData lData = new MeshData();
            Color lPlate = lUnlocked ? lTheme.accent : new Color(0.32f, 0.32f, 0.36f);
            GateKit.Box(lData, new Vector3(0f, 2f, 0f), new Vector3(92f, 4f, 72f), lPlate);
            GameObject lSlab = new GameObject("Slab");
            lSlab.transform.SetParent(lRoot.transform, false);
            lSlab.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("Slab");
            lSlab.AddComponent<MeshRenderer>().sharedMaterial = pMaterial;

            // The number, as tall as a building.
            Color lFace = lUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.64f);
            lStation.number = Text3D.Create(lRoot.transform, (pIndex + 1).ToString(), 36f, 46f, lFace, Color.Lerp(lPlate, Color.black, 0.45f), 14, 0.03f);
            lStation.number.localPosition = new Vector3(-lSide * 22f, 21f, 12f);
            lStation.number.localRotation = Quaternion.Euler(0f, -lSide * 12f, 0f);

            // The thing you race, for real.
            if (lUnlocked)
            {
                Transform lHolder = new GameObject("Vehicle").transform;
                lHolder.SetParent(lRoot.transform, false);
                lHolder.localPosition = new Vector3(lSide * 20f, 4f, 0f);
                Transform lModel = RivalFactory.Build(pLevel.rival, lHolder);
                FitInto(lModel, 34f);
                lStation.model = lHolder;
                foreach (AudioSource lSource in lModel.GetComponentsInChildren<AudioSource>()) lSource.enabled = false;
            }

            // Name painted on the road in front of it.
            string lLine = lUnlocked ? pLevel.title : "LOCKED";
            PaintWord(lLine, lCenter + new Vector3(0f, 4.6f, -27f), 14f, 86f, Color.white);
            int lRank = GameSession.GetRank(pIndex);
            if (lUnlocked && lRank > 0)
                PaintWord("RANK " + GameSession.RankLetter(lRank) + "   " + GameSession.FormatTime(GameSession.GetBestTime(pIndex)), lCenter + new Vector3(0f, 4.6f, -15f), 8f, 80f, UIKit.GOLD);
            _stations.Add(lStation);
        }

        private static void FitInto(Transform pModel, float pSize)
        {
            Bounds lBounds = new Bounds(pModel.position, Vector3.zero);
            bool lAny = false;
            foreach (Renderer lRenderer in pModel.GetComponentsInChildren<Renderer>())
            {
                if (!lAny) { lBounds = lRenderer.bounds; lAny = true; }
                else lBounds.Encapsulate(lRenderer.bounds);
            }
            if (!lAny) return;
            float lLargest = Mathf.Max(lBounds.size.x, Mathf.Max(lBounds.size.y, lBounds.size.z));
            if (lLargest > 0.01f) pModel.localScale *= pSize / lLargest;
            float lBottom = (lBounds.min.y - pModel.position.y) * pSize / lLargest;
            pModel.localPosition -= Vector3.up * lBottom;
        }

        private void PaintWord(string pText, Vector3 pLocal, float pSize, float pWidth, Color pColor)
        {
            GameObject lObject = new GameObject("Painted_" + pText);
            lObject.transform.SetParent(transform, false);
            lObject.transform.localPosition = pLocal;
            lObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro lText = lObject.AddComponent<TextMeshPro>();
            lText.font = GameAssets.TitleFont;
            lText.text = pText;
            lText.fontStyle = FontStyles.Italic;
            lText.alignment = TextAlignmentOptions.Center;
            lText.textWrappingMode = TextWrappingModes.NoWrap;
            lText.color = pColor;
            lText.enableAutoSizing = true;
            lText.fontSizeMin = 1f;
            lText.fontSizeMax = pSize * 10f;
            lText.rectTransform.sizeDelta = new Vector2(pWidth, pSize);
        }

        public void Show(int pStage)
        {
            gameObject.SetActive(true);
            _position = pStage;
            _target = pStage;
            _pressed = false;
            _hover = -1;
            UpdateCamera(1f);
            _cameraPosition = _camera.transform.position;
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            float lDt = Time.unscaledDeltaTime;
            HandleInput();
            _position = Mathf.Lerp(_position, _target, 1f - Mathf.Exp(-7f * lDt));
            UpdateCamera(lDt);
            Animate(lDt);
        }

        private void HandleInput()
        {
            if (SceneTransition.IsBusy) return;
            Vector2 lMouse = HSInput.MousePosition;
            _hover = PickAt(lMouse);

            // Press, then either a click on a stage or a drag along the road.
            if (HSInput.MouseClicked)
            {
                _pressed = true;
                _dragged = false;
                _pressAt = lMouse;
                _pressPosition = _target;
            }
            if (_pressed && HSInput.MouseHeld)
            {
                Vector2 lDelta = lMouse - _pressAt;
                if (!_dragged && lDelta.magnitude > DRAG_PIXELS) _dragged = true;
                if (_dragged) _target = Mathf.Clamp(_pressPosition - (lDelta.x + lDelta.y) / Screen.height * 5f, 0f, _stations.Count - 1);
            }
            if (_pressed && !HSInput.MouseHeld)
            {
                _pressed = false;
                if (!_dragged && _hover >= 0)
                {
                    _onPick(_hover);
                    return;
                }
                if (_dragged) _target = Mathf.Round(_target);
            }

            float lWheel = HSInput.MouseWheel;
            if (Mathf.Abs(lWheel) > 0.01f) _target = Mathf.Clamp(Mathf.Round(_target) + (lWheel > 0f ? 1f : -1f), 0f, _stations.Count - 1);

            Vector2Int lMove = HSInput.MenuMove;
            int lStep = lMove.x + lMove.y;
            if (lStep != 0)
            {
                _target = Mathf.Clamp(Mathf.Round(_target) + lStep, 0f, _stations.Count - 1);
                AudioManager.Play("ui_move", 0.6f);
            }
            if (HSInput.SubmitPressed) _onPick(Selected);
        }

        // Nearest station whose volume the mouse ray crosses.
        private int PickAt(Vector2 pMouse)
        {
            Ray lRay = _camera.ScreenPointToRay(pMouse);
            int lBest = -1;
            float lBestDistance = float.MaxValue;
            foreach (Station lStation in _stations)
            {
                Vector3 lCenter = lStation.root.position + Vector3.up * 18f;
                Vector3 lToCenter = lCenter - lRay.origin;
                float lAlong = Vector3.Dot(lToCenter, lRay.direction);
                if (lAlong < 0f) continue;
                float lMiss = (lRay.origin + lRay.direction * lAlong - lCenter).magnitude;
                if (lMiss < HIT_RADIUS && lAlong < lBestDistance)
                {
                    lBestDistance = lAlong;
                    lBest = lStation.index;
                }
            }
            return lBest;
        }

        private void UpdateCamera(float pDt)
        {
            float lZ = _position * SPACING;
            Vector2 lTilt = UIMouse.Tilt;
            Vector3 lDesired = transform.position + new Vector3(lTilt.x * 30f, 96f + lTilt.y * 12f, lZ - 210f);
            Vector3 lLook = transform.position + new Vector3(lTilt.x * 16f, 8f + lTilt.y * 8f, lZ + 150f);
            _cameraPosition = pDt >= 1f ? lDesired : Vector3.Lerp(_cameraPosition, lDesired, 1f - Mathf.Exp(-8f * pDt));
            _camera.transform.SetPositionAndRotation(_cameraPosition, Quaternion.LookRotation(lLook - _cameraPosition, Vector3.up));
            _camera.fieldOfView = 58f;
        }

        private void Animate(float pDt)
        {
            foreach (Station lStation in _stations)
            {
                bool lFocus = lStation.index == _hover || (_hover < 0 && lStation.index == Selected);
                lStation.lift = Mathf.Lerp(lStation.lift, lFocus ? 1f : 0f, 1f - Mathf.Exp(-12f * pDt));
                lStation.root.localPosition = lStation.center + Vector3.up * lStation.lift * 7f;
                lStation.root.localScale = Vector3.one * (1f + lStation.lift * 0.1f);
                if (lStation.model != null) lStation.model.Rotate(0f, (20f + lStation.lift * 70f) * pDt, 0f, Space.Self);
                Vector2 lTilt = UIMouse.Tilt;
                int lSide = lStation.index % 2 == 0 ? -1 : 1;
                lStation.number.localRotation = Quaternion.Euler(lTilt.y * -12f, -lSide * 12f + lTilt.x * 20f * (0.4f + lStation.lift), 0f);
            }
        }
    }
}
