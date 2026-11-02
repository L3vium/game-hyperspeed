using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // F6 (cheat builds) : shows the analytic layout over the world : spine, deck edges by kind, safe points, landing prediction.
    public class LayoutDebugDraw : MonoBehaviour
    {
        private LevelLayout _layout;
        private PlayerController _player;
        private Transform _root;
        private LineRenderer _landing;

        public void Init(LevelLayout pLayout, PlayerController pPlayer)
        {
            _layout = pLayout;
            _player = pPlayer;
        }

        private void Build()
        {
            _root = new GameObject("LayoutDebug").transform;
            List<Vector3> lSpine = new List<Vector3>();
            for (int i = 0; i < _layout.spine.Count; i += 2) lSpine.Add(_layout.spine.samples[i].position + Vector3.up * 0.3f);
            Line("Spine", lSpine, Color.white, 0.25f);
            foreach (Deck lDeck in _layout.decks)
            {
                for (int lSide = -1; lSide <= 1; lSide += 2)
                {
                    List<Vector3> lPoints = new List<Vector3>();
                    EdgeKind lKind = lSide < 0 ? lDeck.edgeL[0] : lDeck.edgeR[0];
                    for (int k = 0; k < lDeck.Count; k++)
                    {
                        EdgeKind lHere = lSide < 0 ? lDeck.edgeL[k] : lDeck.edgeR[k];
                        if (lHere != lKind && lPoints.Count > 1)
                        {
                            Line("Edge", lPoints, KindColor(lKind), 0.35f);
                            lPoints = new List<Vector3> { lPoints[lPoints.Count - 1] };
                            lKind = lHere;
                        }
                        lPoints.Add(lDeck.center[k] + lDeck.right[k] * lSide * lDeck.w[k] * 0.5f + lDeck.up[k] * 0.4f);
                    }
                    if (lPoints.Count > 1) Line("Edge", lPoints, KindColor(lKind), 0.35f);
                }
            }
            foreach (SafePoint lPoint in _layout.safePoints)
            {
                Vector3 lBase = lPoint.deck.SurfacePoint(lPoint.s, 0f);
                Line("Safe", new List<Vector3> { lBase, lBase + Vector3.up * 4f }, Color.cyan, 0.4f);
            }
            _landing = Line("Landing", new List<Vector3> { Vector3.zero, Vector3.up }, Color.green, 0.6f);
        }

        private static Color KindColor(EdgeKind pKind)
        {
            switch (pKind)
            {
                case EdgeKind.Lip: return Color.yellow;
                case EdgeKind.Wall: return Color.red;
                case EdgeKind.Open: return Color.green;
            }
            return Color.blue;
        }

        private LineRenderer Line(string pName, List<Vector3> pPoints, Color pColor, float pWidth)
        {
            LineRenderer lLine = new GameObject(pName).AddComponent<LineRenderer>();
            lLine.transform.SetParent(_root, false);
            lLine.positionCount = pPoints.Count;
            lLine.SetPositions(pPoints.ToArray());
            lLine.startWidth = pWidth;
            lLine.endWidth = pWidth;
            lLine.sharedMaterial = Mats.Glow(pColor * 2f, false, 0f, 0f, true);
            lLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return lLine;
        }

        private void Update()
        {
            UnityEngine.InputSystem.Keyboard lKeyboard = UnityEngine.InputSystem.Keyboard.current;
            if (lKeyboard != null && lKeyboard.f6Key.wasPressedThisFrame)
            {
                if (_root == null) Build();
                else _root.gameObject.SetActive(!_root.gameObject.activeSelf);
            }
            if (_root == null || !_root.gameObject.activeSelf || _landing == null) return;
            _landing.enabled = _player.HasLandingPrediction;
            if (_player.HasLandingPrediction) _landing.SetPositions(new[] { _player.LandingPoint, _player.LandingPoint + Vector3.up * 6f });
        }
    }
}
