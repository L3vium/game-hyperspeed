using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // A huge block rolling forward along the track like a convoy : land on it and it carries you, hit its back and you
    // climb it, hit it head on and it hurts. Woken up only when the runner comes near.
    public class MovingPlatform : MonoBehaviour
    {
        public static readonly List<MovingPlatform> All = new List<MovingPlatform>();

        private LevelLayout _layout;
        private float _s;
        private float _lateral;
        private float _speed;
        private float _phase;
        private float _lift;
        private float _baseHeight;
        private Vector3 _lastPosition;

        // How far it moved this frame : whoever stands on it moves with it.
        public Vector3 Delta { get; private set; }

        public static MovingPlatform Create(Transform pParent, LevelLayout pLayout, float pS, float pLateral, float pWidth, float pLength, float pHeight, float pSpeed, Theme pTheme)
        {
            GameObject lObject = new GameObject("Convoy");
            lObject.transform.SetParent(pParent, false);
            lObject.layer = Layers.DECK;
            MovingPlatform lPlatform = lObject.AddComponent<MovingPlatform>();
            lPlatform._layout = pLayout;
            lPlatform._s = pS;
            lPlatform._lateral = pLateral;
            lPlatform._speed = pSpeed;
            lPlatform._phase = Random.Range(0f, 6.28f);
            lPlatform._lift = pHeight * 0.5f;

            // A long box with a lit top edge, wheels-like dark bands under it.
            MeshData lData = new MeshData();
            Color lBody = Color.Lerp(pTheme.decorA, Color.white, 0.2f);
            GateKit.Box(lData, Vector3.zero, new Vector3(pWidth, pHeight, pLength), lBody);
            GateKit.Box(lData, new Vector3(0f, -pHeight * 0.5f, 0f), new Vector3(pWidth * 1.02f, pHeight * 0.2f, pLength * 1.01f), Color.Lerp(pTheme.decorA, Color.black, 0.55f));
            for (int i = -2; i <= 2; i++)
                GateKit.Box(lData, new Vector3(0f, 0f, i * pLength * 0.2f), new Vector3(pWidth * 1.03f, pHeight * 0.9f, pLength * 0.012f), Color.Lerp(pTheme.decorA, Color.black, 0.3f));
            Mesh lMesh = lData.ToMesh("Convoy");
            lObject.AddComponent<MeshFilter>().sharedMesh = lMesh;
            lObject.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(pTheme.shade, false);
            BoxCollider lCollider = lObject.AddComponent<BoxCollider>();
            lCollider.size = new Vector3(pWidth, pHeight, pLength);

            MeshData lGlow = new MeshData();
            GateKit.Box(lGlow, new Vector3(-pWidth * 0.5f, pHeight * 0.5f, 0f), new Vector3(0.8f, 0.4f, pLength), Color.white);
            GateKit.Box(lGlow, new Vector3(pWidth * 0.5f, pHeight * 0.5f, 0f), new Vector3(0.8f, 0.4f, pLength), Color.white);
            GameObject lEdge = new GameObject("Edge");
            lEdge.transform.SetParent(lObject.transform, false);
            lEdge.AddComponent<MeshFilter>().sharedMesh = lGlow.ToMesh("ConvoyEdge");
            lEdge.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(Color.Lerp(pTheme.accent, Color.white, 0.3f) * 3f, false, 0f, 0f, true);

            lPlatform._baseHeight = 0f;
            lPlatform.Place(true);
            lPlatform._lastPosition = lObject.transform.position;
            return lPlatform;
        }

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        private void Place(bool pFirst)
        {
            float lLateral = _lateral + Mathf.Sin(Time.time * 0.6f + _phase) * 12f;
            SpineSample lSample = _layout.spine.Evaluate(_s);
            Vector3 lUp;
            Vector3 lBase;
            if (_layout.field.Solid(_s))
            {
                lUp = _layout.field.Normal(_s);
                lBase = _layout.field.Point(_s, lLateral);
                // Beside a narrow, tilted slab the plane of the slab rises in the air : the convoy stays below the
                // slab's nearest edge, so from the slab you always drop onto it, you never run into its side.
                TrackBlock lBlock = _layout.field.BlockAt(_s);
                if (lBlock != null && Mathf.Abs(lLateral - lBlock.offset) > lBlock.halfWidth)
                {
                    float lEdge = Mathf.Clamp(lLateral, lBlock.offset - lBlock.halfWidth, lBlock.offset + lBlock.halfWidth);
                    float lEdgeHeight = Vector3.Dot(_layout.field.Point(_s, lEdge) - lSample.position, lSample.up);
                    float lTop = Vector3.Dot(lBase - lSample.position, lSample.up) + _lift * 2f;
                    float lLimit = lEdgeHeight - 6f;
                    if (lTop > lLimit) lBase -= lSample.up * (lTop - lLimit);
                }
                _baseHeight = Vector3.Dot(lBase - lSample.position, lSample.up);
            }
            else
            {
                // Over a gap it keeps its height, like a truck flying off a ramp.
                lUp = lSample.up;
                lBase = lSample.position + lSample.Right * lLateral + lSample.up * _baseHeight;
            }
            transform.SetPositionAndRotation(lBase + lUp * _lift, Quaternion.LookRotation(Vector3.ProjectOnPlane(lSample.forward, lUp), lUp));
        }

        private void Update()
        {
            Delta = Vector3.zero;
            if (_layout == null || !RaceContext.Running) return;
            if (Mathf.Abs(_s - RaceContext.PlayerProgress) > _layout.profile.cruise * 6f) return;
            _s = Mathf.Min(_s + _speed * Time.deltaTime, _layout.finishS - 60f);
            Place(false);
            Delta = transform.position - _lastPosition;
            _lastPosition = transform.position;
        }
    }
}
