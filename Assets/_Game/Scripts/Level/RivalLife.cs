using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // What makes each rival alive on top of its jelly (2026-11-11 : "the rocket must spin a little, smoke behind it...
    // ask the question for every one of them") : rockets roll with flames and a thick smoke trail and do barrel rolls,
    // vehicles bounce on their suspension with spinning wheels and exhaust puffs, planes draw contrails and burn their
    // afterburners, the spaceship pulses its thrusters, and creatures move like soft active ragdolls (springs on every
    // bone). The birds are a real flock (boids, see BirdFlock).
    public class RivalLife : MonoBehaviour
    {
        private Rival _rival;
        private RivalKind _kind;
        private Transform _model;
        private Transform _body;
        private Bounds _bounds;
        private float _time;

        // Rockets
        private float _roll;
        private float _rollSpeed;
        private float _barrel = 1f;
        private float _barrelClock;
        private readonly List<Transform> _flames = new List<Transform>();
        private TrailRenderer _smoke;

        // Vehicles
        private readonly List<Transform> _wheels = new List<Transform>();
        private float _wheelAngle;
        private float _bounce;
        private float _bounceVelocity;
        private float _pitch;
        private float _lastRate;
        private float _puffClock;
        private Vector3 _exhaust;

        // Planes
        private readonly List<TrailRenderer> _contrails = new List<TrailRenderer>();

        // Soft bones (creatures)
        private SpringBones _springs;

        public static RivalLife Attach(Rival pRival, Transform pModel, RivalKind pKind)
        {
            RivalLife lLife = pRival.gameObject.AddComponent<RivalLife>();
            lLife._rival = pRival;
            lLife._kind = pKind;
            lLife._model = pModel;
            lLife._body = pModel.childCount > 0 ? pModel.GetChild(0) : pModel;
            lLife.Build();
            return lLife;
        }

        private void Build()
        {
            _bounds = new Bounds(_model.position, Vector3.zero);
            bool lFirst = true;
            foreach (Renderer lRenderer in _model.GetComponentsInChildren<Renderer>())
            {
                Bounds lB = lRenderer.bounds;
                Vector3 lMin = _model.InverseTransformPoint(lB.min);
                Vector3 lMax = _model.InverseTransformPoint(lB.max);
                Bounds lLocal = new Bounds((lMin + lMax) * 0.5f, new Vector3(Mathf.Abs(lMax.x - lMin.x), Mathf.Abs(lMax.y - lMin.y), Mathf.Abs(lMax.z - lMin.z)));
                if (lFirst) _bounds = lLocal;
                else _bounds.Encapsulate(lLocal);
                lFirst = false;
            }
            Vector3 lSize = _bounds.size;
            switch (_kind)
            {
                case RivalKind.Rockets:
                    _rollSpeed = 160f;
                    _barrelClock = Random.Range(3f, 6f);
                    AddFlame(new Vector3(0f, 0f, _bounds.min.z), Mathf.Max(lSize.x, lSize.y) * 0.35f, lSize.z * 0.5f, new Color(3.2f, 1.4f, 0.35f));
                    _smoke = AddSmoke(new Vector3(0f, 0f, _bounds.min.z - lSize.z * 0.1f), Mathf.Max(lSize.x, lSize.y) * 0.5f, 3.2f);
                    break;
                case RivalKind.Bullet:
                    AddFlame(new Vector3(0f, 0f, _bounds.min.z), Mathf.Max(lSize.x, lSize.y) * 0.4f, lSize.z * 0.8f, new Color(2.6f, 2f, 1.2f));
                    _rollSpeed = 900f;
                    break;
                case RivalKind.Moto:
                case RivalKind.Bus:
                case RivalKind.SportsCar:
                case RivalKind.RaceCar:
                    AddWheels(lSize, _kind == RivalKind.Moto ? 2 : 4);
                    _exhaust = new Vector3(_kind == RivalKind.Moto ? 0f : lSize.x * 0.25f, lSize.y * 0.18f + _bounds.min.y, _bounds.min.z);
                    break;
                case RivalKind.Train:
                    _exhaust = new Vector3(0f, _bounds.max.y, _bounds.max.z - lSize.z * 0.1f);
                    break;
                case RivalKind.Plane:
                case RivalKind.FighterJet:
                    for (int lSide = -1; lSide <= 1; lSide += 2)
                    {
                        TrailRenderer lTrail = AddTrail(new Vector3(lSide * lSize.x * 0.48f, _bounds.center.y, _bounds.center.z - lSize.z * 0.1f), new Color(1f, 1f, 1f, 0.55f), lSize.x * 0.03f + 0.5f, _kind == RivalKind.Plane ? 6f : 2.5f);
                        _contrails.Add(lTrail);
                    }
                    if (_kind == RivalKind.FighterJet) AddFlame(new Vector3(0f, _bounds.center.y, _bounds.min.z), lSize.y * 0.45f, lSize.z * 0.45f, new Color(3.4f, 1.6f, 0.6f));
                    break;
                case RivalKind.Spaceship:
                    for (int lSide = -1; lSide <= 1; lSide += 2)
                        AddFlame(new Vector3(lSide * lSize.x * 0.18f, _bounds.center.y, _bounds.min.z), lSize.y * 0.4f, lSize.z * 0.5f, new Color(0.6f, 1.8f, 3.6f));
                    _smoke = AddSmoke(new Vector3(0f, _bounds.center.y, _bounds.min.z), lSize.y * 0.6f, 1.2f, new Color(0.5f, 0.9f, 1.4f, 0.35f));
                    break;
                case RivalKind.TRex:
                case RivalKind.Birds:
                    _springs = SpringBones.Attach(_model, _kind == RivalKind.TRex ? 70f : 140f, _kind == RivalKind.TRex ? 7f : 12f);
                    break;
            }
        }

        // ---------------------------------------------------------------- builders

        private void AddFlame(Vector3 pLocal, float pRadius, float pLength, Color pColor)
        {
            Transform lFlame = new GameObject("Flame").transform;
            lFlame.SetParent(_body, false);
            lFlame.localPosition = _body.InverseTransformPoint(_model.TransformPoint(pLocal));
            lFlame.localRotation = Quaternion.identity;
            lFlame.gameObject.AddComponent<MeshFilter>().sharedMesh = MeshUtil.Cone(pRadius / _body.lossyScale.x, 0.05f, pLength / _body.lossyScale.z, 14);
            MeshRenderer lRenderer = lFlame.gameObject.AddComponent<MeshRenderer>();
            lRenderer.sharedMaterial = Mats.Glow(pColor, true, 0f, 1.5f);
            lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _flames.Add(lFlame);
            // A white hot core inside.
            Transform lCore = new GameObject("Core").transform;
            lCore.SetParent(lFlame, false);
            lCore.localScale = new Vector3(0.5f, 0.5f, 0.6f);
            lCore.gameObject.AddComponent<MeshFilter>().sharedMesh = lFlame.GetComponent<MeshFilter>().sharedMesh;
            MeshRenderer lCoreRenderer = lCore.gameObject.AddComponent<MeshRenderer>();
            lCoreRenderer.sharedMaterial = Mats.Glow(new Color(4f, 3.6f, 3f), true);
            lCoreRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private TrailRenderer AddSmoke(Vector3 pLocal, float pWidth, float pTime, Color? pColor = null)
        {
            Transform lAnchor = new GameObject("Smoke").transform;
            lAnchor.SetParent(_model, false);
            lAnchor.localPosition = pLocal;
            TrailRenderer lTrail = lAnchor.gameObject.AddComponent<TrailRenderer>();
            lTrail.time = pTime;
            lTrail.minVertexDistance = Mathf.Max(0.5f, pWidth * 0.2f);
            // Thin at the nozzle, billowing out behind.
            lTrail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.15f, 1f), new Keyframe(1f, 3.2f));
            lTrail.widthMultiplier = pWidth;
            lTrail.sharedMaterial = Mats.Glow(pColor ?? new Color(0.88f, 0.86f, 0.9f, 0.55f), false, 1f);
            Gradient lFade = new Gradient();
            lFade.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.8f, 0.6f), 0f), new GradientColorKey(Color.white, 0.2f), new GradientColorKey(new Color(0.7f, 0.7f, 0.75f), 1f) },
                          new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
            lTrail.colorGradient = lFade;
            lTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return lTrail;
        }

        private TrailRenderer AddTrail(Vector3 pLocal, Color pColor, float pWidth, float pTime)
        {
            Transform lAnchor = new GameObject("Contrail").transform;
            lAnchor.SetParent(_model, false);
            lAnchor.localPosition = pLocal;
            TrailRenderer lTrail = lAnchor.gameObject.AddComponent<TrailRenderer>();
            lTrail.time = pTime;
            lTrail.minVertexDistance = 2f;
            lTrail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 2.4f));
            lTrail.widthMultiplier = pWidth;
            lTrail.sharedMaterial = Mats.Glow(pColor, false, 1f);
            Gradient lFade = new Gradient();
            lFade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                          new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
            lTrail.colorGradient = lFade;
            lTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return lTrail;
        }

        // The models are one piece : spinning wheel discs are laid over where the wheels sit.
        private void AddWheels(Vector3 pSize, int pCount)
        {
            float lRadius = pSize.y * (_kind == RivalKind.Bus ? 0.17f : _kind == RivalKind.Moto ? 0.3f : 0.22f);
            float lY = _bounds.min.y + lRadius;
            float lFront = _bounds.center.z + pSize.z * (_kind == RivalKind.Bus ? 0.32f : 0.34f);
            float lBack = _bounds.center.z - pSize.z * 0.32f;
            float lSide = pCount == 2 ? 0f : pSize.x * 0.5f + 0.02f;
            Material lTyre = Mats.Flat(new Color(0.12f, 0.1f, 0.14f), false);
            Material lRim = Mats.Glow(new Color(2.4f, 2.2f, 2.8f), false, 0f, 0f, true);
            float[] lZ = { lFront, lBack };
            foreach (float lZPos in lZ)
            {
                for (int s = pCount == 2 ? 0 : -1; s <= (pCount == 2 ? 0 : 1); s += 2)
                {
                    Transform lWheel = new GameObject("Wheel").transform;
                    lWheel.SetParent(_model, false);
                    lWheel.localPosition = new Vector3(s * lSide, lY, lZPos);
                    Transform lTyreT = Mats.Primitive(PrimitiveType.Cylinder, lWheel, Vector3.zero, new Vector3(lRadius * 2f, pCount == 2 ? lRadius * 0.4f : lRadius * 0.5f, lRadius * 2f), lTyre, "Tyre").transform;
                    lTyreT.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    // Two bright spokes : the spin reads from far.
                    Transform lSpoke = Mats.Primitive(PrimitiveType.Cube, lWheel, new Vector3(s * lRadius * 0.27f, 0f, 0f), new Vector3(lRadius * 0.08f, lRadius * 1.6f, lRadius * 0.3f), lRim, "Spoke").transform;
                    Mats.Primitive(PrimitiveType.Cube, lWheel, new Vector3(s * lRadius * 0.27f, 0f, 0f), new Vector3(lRadius * 0.08f, lRadius * 0.3f, lRadius * 1.6f), lRim, "Spoke");
                    _wheels.Add(lWheel);
                }
            }
        }

        // ---------------------------------------------------------------- per frame

        private void LateUpdate()
        {
            float lDt = Time.deltaTime;
            if (lDt <= 0f) return;
            _time += lDt;
            float lRate = _rival != null ? _rival.CurrentRate : 0f;
            switch (_kind)
            {
                case RivalKind.Rockets:
                case RivalKind.Bullet:
                    UpdateRoll(lDt);
                    break;
                case RivalKind.Moto:
                case RivalKind.Bus:
                case RivalKind.SportsCar:
                case RivalKind.RaceCar:
                case RivalKind.Train:
                    UpdateVehicle(lDt, lRate);
                    break;
            }
            // Flames flicker and lick back.
            for (int i = 0; i < _flames.Count; i++)
            {
                float lFlicker = 1f + Mathf.Sin(_time * 47f + i * 2.1f) * 0.12f + Mathf.PerlinNoise(_time * 18f, i) * 0.35f;
                _flames[i].localScale = new Vector3(1f + Mathf.Sin(_time * 31f + i) * 0.08f, 1f + Mathf.Sin(_time * 29f + i) * 0.08f, lFlicker);
            }
        }

        private void UpdateRoll(float pDt)
        {
            // A steady roll, and now and then a full barrel roll.
            _barrelClock -= pDt;
            if (_barrelClock <= 0f && _barrel >= 1f)
            {
                _barrel = 0f;
                _barrelClock = Random.Range(4f, 8f);
            }
            float lExtra = 0f;
            if (_barrel < 1f)
            {
                _barrel = Mathf.MoveTowards(_barrel, 1f, pDt / 0.9f);
                float lT = _barrel;
                lExtra = (lT < 0.5f ? 4f * lT * lT * lT : 1f - Mathf.Pow(-2f * lT + 2f, 3f) * 0.5f) * 360f;
            }
            _roll += _rollSpeed * pDt;
            _body.localRotation = Quaternion.Euler(Mathf.Sin(_time * 1.7f) * 3f, Mathf.Sin(_time * 1.3f) * 4f, _roll + lExtra);
        }

        private void UpdateVehicle(float pDt, float pRate)
        {
            // Wheels spin with the pace.
            float lRadius = Mathf.Max(0.5f, _bounds.size.y * 0.22f * _model.lossyScale.y);
            _wheelAngle += pRate / lRadius * Mathf.Rad2Deg * pDt;
            foreach (Transform lWheel in _wheels) lWheel.localRotation = Quaternion.Euler(_wheelAngle, 0f, 0f);
            // Suspension : speed changes pitch the body, bumps bounce it.
            float lAccel = (pRate - _lastRate) / pDt;
            _lastRate = pRate;
            float lPitchTarget = Mathf.Clamp(-lAccel * 0.02f, -5f, 5f);
            _pitch = Mathf.Lerp(_pitch, lPitchTarget, 1f - Mathf.Exp(-4f * pDt));
            _bounceVelocity += (-_bounce * 90f - _bounceVelocity * 6f + (Mathf.PerlinNoise(_time * 3.1f, 0.5f) - 0.5f) * 60f) * pDt;
            _bounce += _bounceVelocity * pDt;
            float lSway = _kind == RivalKind.Bus ? Mathf.Sin(_time * 1.6f) * 3f : 0f;
            _body.localPosition = new Vector3(0f, _bounce * 0.05f * _bounds.size.y, 0f);
            _body.localRotation = Quaternion.Euler(_pitch, 0f, lSway + (_kind == RivalKind.Moto ? Mathf.Sin(_time * 0.9f) * 6f : 0f));
            // Exhaust : a puff now and then, more when it speeds up.
            _puffClock -= pDt * (1f + Mathf.Max(0f, lAccel) * 0.05f);
            if (_puffClock <= 0f)
            {
                _puffClock = _kind == RivalKind.Train ? 0.12f : 0.18f;
                Vector3 lAt = _model.TransformPoint(_exhaust);
                Color lPuff = _kind == RivalKind.Train ? new Color(0.95f, 0.95f, 1f, 0.7f) : new Color(0.55f, 0.55f, 0.6f, 0.6f);
                FX.Puff(lAt, lPuff, _kind == RivalKind.Train ? 3 : 1, (_kind == RivalKind.Train ? 4f : 1.2f) * _model.lossyScale.y, 3f);
            }
        }
    }

    // Soft bones : every bone of a skinned creature springs toward its animated pose instead of snapping to it, so the
    // whole body lags, overshoots and wobbles (heads bob, tails whip, arms flop) like an active ragdoll.
    public class SpringBones : MonoBehaviour
    {
        private Transform[] _bones;
        private Quaternion[] _current;
        private Vector3[] _velocity;
        private float _stiffness;
        private float _damping;
        private bool _ready;

        public static SpringBones Attach(Transform pRoot, float pStiffness, float pDamping)
        {
            SpringBones lSprings = pRoot.gameObject.AddComponent<SpringBones>();
            lSprings._stiffness = pStiffness;
            lSprings._damping = pDamping;
            List<Transform> lBones = new List<Transform>();
            foreach (SkinnedMeshRenderer lSkin in pRoot.GetComponentsInChildren<SkinnedMeshRenderer>())
                foreach (Transform lBone in lSkin.bones)
                    if (lBone != null && !lBones.Contains(lBone)) lBones.Add(lBone);
            // Parents first : a child's animated pose then already carries its parent's lag.
            lBones.Sort((a, b) => Depth(a).CompareTo(Depth(b)));
            // The root of each skeleton follows exactly, everything else springs.
            if (lBones.Count > 0) lBones.RemoveAt(0);
            lSprings._bones = lBones.ToArray();
            lSprings._current = new Quaternion[lBones.Count];
            lSprings._velocity = new Vector3[lBones.Count];
            return lSprings;
        }

        private static int Depth(Transform pT)
        {
            int lDepth = 0;
            while (pT.parent != null) { pT = pT.parent; lDepth++; }
            return lDepth;
        }

        private void LateUpdate()
        {
            float lDt = Mathf.Min(Time.deltaTime, 0.05f);
            if (lDt <= 0f || _bones == null) return;
            for (int i = 0; i < _bones.Length; i++)
            {
                Transform lBone = _bones[i];
                if (lBone == null) continue;
                Quaternion lTarget = lBone.rotation;
                if (!_ready)
                {
                    _current[i] = lTarget;
                    continue;
                }
                Quaternion lDelta = lTarget * Quaternion.Inverse(_current[i]);
                if (lDelta.w < 0f) lDelta = new Quaternion(-lDelta.x, -lDelta.y, -lDelta.z, -lDelta.w);
                float lAngle;
                Vector3 lAxis;
                lDelta.ToAngleAxis(out lAngle, out lAxis);
                if (float.IsNaN(lAxis.x) || lAngle > 120f)
                {
                    _current[i] = lTarget;
                    _velocity[i] = Vector3.zero;
                    continue;
                }
                Vector3 lError = lAxis * (lAngle * Mathf.Deg2Rad);
                _velocity[i] += (lError * _stiffness - _velocity[i] * _damping) * lDt;
                float lSpin = _velocity[i].magnitude * lDt;
                if (lSpin > 1e-6f) _current[i] = Quaternion.AngleAxis(lSpin * Mathf.Rad2Deg, _velocity[i] / _velocity[i].magnitude) * _current[i];
                lBone.rotation = _current[i];
            }
            _ready = true;
        }
    }

    // The falcons are a real flock : each bird steers by the boids rules (keep apart, match the others, stay together)
    // around the rival's position, flaps, banks into its turns, and the whole flock dives now and then.
    public class BirdFlock : MonoBehaviour
    {
        private const float RADIUS = 55f;

        private Transform[] _birds;
        private Vector3[] _position;
        private Vector3[] _velocity;
        private float _diveClock = 4f;
        private float _dive;
        private float _time;

        public static BirdFlock Attach(Transform pFlockRoot, Transform[] pBirds)
        {
            BirdFlock lFlock = pFlockRoot.gameObject.AddComponent<BirdFlock>();
            lFlock._birds = pBirds;
            lFlock._position = new Vector3[pBirds.Length];
            lFlock._velocity = new Vector3[pBirds.Length];
            for (int i = 0; i < pBirds.Length; i++)
            {
                lFlock._position[i] = Random.insideUnitSphere * RADIUS * 0.6f;
                lFlock._velocity[i] = Random.insideUnitSphere * 8f;
            }
            return lFlock;
        }

        private void LateUpdate()
        {
            float lDt = Mathf.Min(Time.deltaTime, 0.05f);
            if (lDt <= 0f) return;
            _time += lDt;
            _diveClock -= lDt;
            if (_diveClock <= 0f)
            {
                _diveClock = Random.Range(5f, 9f);
                _dive = 1f;
            }
            _dive = Mathf.MoveTowards(_dive, 0f, lDt / 1.6f);
            // The flock's anchor dips in a dive, then climbs back.
            Vector3 lAnchor = new Vector3(Mathf.Sin(_time * 0.7f) * 8f, -Mathf.Sin(_dive * Mathf.PI) * 28f, 0f);
            int lCount = _birds.Length;
            for (int i = 0; i < lCount; i++)
            {
                Vector3 lSeparate = Vector3.zero, lAlign = Vector3.zero, lCenter = Vector3.zero;
                int lNear = 0;
                for (int j = 0; j < lCount; j++)
                {
                    if (i == j) continue;
                    Vector3 lDelta = _position[i] - _position[j];
                    float lDistance = lDelta.magnitude;
                    if (lDistance < 22f) lSeparate += lDelta / Mathf.Max(0.5f, lDistance * lDistance) * 22f;
                    if (lDistance < 30f)
                    {
                        lAlign += _velocity[j];
                        lCenter += _position[j];
                        lNear++;
                    }
                }
                Vector3 lSteer = lSeparate * 6f;
                if (lNear > 0)
                {
                    lSteer += (lAlign / lNear - _velocity[i]) * 0.8f;
                    lSteer += (lCenter / lNear - _position[i]) * 0.4f;
                }
                // Home : never far from the rival's spot.
                Vector3 lHome = lAnchor - _position[i];
                lSteer += lHome * (lHome.magnitude > RADIUS ? 1.6f : 0.25f);
                lSteer += new Vector3(Mathf.PerlinNoise(_time * 0.6f, i) - 0.5f, Mathf.PerlinNoise(i, _time * 0.6f) - 0.5f, Mathf.PerlinNoise(_time * 0.5f, i * 3f) - 0.5f) * 30f;
                _velocity[i] = Vector3.ClampMagnitude(_velocity[i] + lSteer * lDt, 28f);
                _position[i] += _velocity[i] * lDt;

                Transform lBird = _birds[i];
                if (lBird == null) continue;
                lBird.localPosition = _position[i];
                // Flies forward with the flock, bent by its own drift, banked into it.
                Vector3 lHeading = Vector3.forward * 60f + _velocity[i];
                float lBank = -Mathf.Clamp(_velocity[i].x * 2.2f, -50f, 50f);
                lBird.localRotation = Quaternion.LookRotation(lHeading.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, lBank);
            }
        }
    }
}
