using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Goofy soft body for a rival : the whole thing squashes and stretches on every change of pace, wobbles in turns,
    // hops and flails when you pass it, and its parts lag behind like jelly. Each rival has its own character (the bike
    // pulls wheelies, the bus wallows, the train snakes, the bullet stretches like rubber...). Streaks pour off it.
    public class RivalWobble : MonoBehaviour
    {
        private struct Character
        {
            public float squash;
            public float wobble;
            public float hop;
            public float hopEvery;
            public float wheelie;
            public float snake;
            public float stretch;
            public float flail;
        }

        public float Speed01;

        private RivalKind _kind;
        private Character _c;
        private Transform _pivot;
        private Transform[] _parts;
        private Vector3[] _rest;
        private Vector3[] _offset;
        private Vector3[] _offsetVelocity;
        private Vector3 _lastPivot;
        private float _squash = 1f;
        private float _squashVelocity;
        private Vector3 _tilt;
        private Vector3 _tiltVelocity;
        private float _hop;
        private float _hopVelocity;
        private float _hopClock;
        private float _turn;
        private float _flail;
        private float _flailSide = 1f;
        private float _time;

        public static RivalWobble Attach(Transform pModel, RivalKind pKind)
        {
            // A pivot between the rival and its model : the rival keeps animating the model, the jelly moves the pivot.
            Transform lPivot = new GameObject("Jelly").transform;
            lPivot.SetParent(pModel.parent, false);
            pModel.SetParent(lPivot, false);
            RivalWobble lWobble = lPivot.gameObject.AddComponent<RivalWobble>();
            lWobble._pivot = lPivot;
            lWobble._kind = pKind;
            lWobble._c = CharacterOf(pKind);
            lWobble._time = Random.value * 10f;

            int lCount = pModel.childCount;
            lWobble._parts = new Transform[lCount];
            lWobble._rest = new Vector3[lCount];
            lWobble._offset = new Vector3[lCount];
            lWobble._offsetVelocity = new Vector3[lCount];
            for (int i = 0; i < lCount; i++)
            {
                lWobble._parts[i] = pModel.GetChild(i);
                lWobble._rest[i] = lWobble._parts[i].localPosition;
            }
            lWobble.BuildStreaks(pModel, pKind);
            return lWobble;
        }

        private static Character CharacterOf(RivalKind pKind)
        {
            switch (pKind)
            {
                // Vehicles : only their suspension moves. No spin, no flail, no roll : they drive.
                case RivalKind.Moto:
                case RivalKind.Bus:
                case RivalKind.SportsCar:
                case RivalKind.RaceCar:
                case RivalKind.Train: return new Character { squash = 0.03f, hop = 0.12f, hopEvery = 0.45f };
                case RivalKind.Birds: return new Character { squash = 0.25f, wobble = 18f, hop = 2f, hopEvery = 0.5f, flail = 1.4f };
                case RivalKind.Plane:
                case RivalKind.FighterJet:
                case RivalKind.Spaceship:
                case RivalKind.Bullet:
                case RivalKind.Rockets: return new Character { hopEvery = 2f };
                case RivalKind.Light: return new Character { squash = 0.08f, wobble = 6f, stretch = 0.5f, hopEvery = 2f, flail = 1f };
                case RivalKind.Time: return new Character { squash = 0.3f, wobble = 14f, hop = 2f, hopEvery = 0.8f, flail = 1.2f };
                case RivalKind.TRex: return new Character { squash = 0.2f, wobble = 12f, hop = 1.6f, hopEvery = 0.45f, wheelie = 10f, flail = 1.2f };
                case RivalKind.Death: return new Character { squash = 0.2f, wobble = 18f, hop = 0.8f, hopEvery = 1.2f, snake = 8f, flail = 1f };
                // The pack runs on its own legs (DogPack) : the root barely moves.
                case RivalKind.Dogs: return new Character { hopEvery = 2f };
                default: return new Character { squash = 0.1f, wobble = 6f, hopEvery = 2f, flail = 0.5f };
            }
        }

        // Change of pace (in cruise per second) : it squashes when it floors it, stretches when it brakes, and rears up.
        public void Accelerate(float pAmount)
        {
            pAmount = Mathf.Clamp(pAmount, -3f, 3f);
            _squashVelocity -= pAmount * _c.squash * 1.5f;
            _tiltVelocity.x -= pAmount * (_c.wheelie + (_c.flail > 0f ? 6f : 0.5f)) * 2f;
        }

        public void Turn(float pYawRate) => _turn = Mathf.Clamp(pYawRate, -90f, 90f);

        // Passed or passing : a goofy flail, a hop and a half spin.
        public void Hiccup(float pStrength)
        {
            if (_c.flail <= 0f) return;
            _flail = Mathf.Max(_flail, pStrength * _c.flail);
            _flailSide = Random.value < 0.5f ? -1f : 1f;
            _hopVelocity += 30f * pStrength * RivalFactory.GROW;
            _squashVelocity += 2.5f * pStrength;
        }

        private void LateUpdate()
        {
            float lDt = Time.deltaTime;
            if (lDt <= 0f || _pivot == null) return;
            _time += lDt;

            // Idle goofiness : hops at its own rhythm, faster when it pushes.
            _hopClock -= lDt * (0.7f + Speed01);
            if (_hopClock <= 0f && _c.hop > 0f)
            {
                _hopClock = _c.hopEvery * Random.Range(0.7f, 1.3f);
                _hopVelocity += _c.hop * 9f * RivalFactory.GROW;
                _squashVelocity += _c.squash * 3f;
                _tiltVelocity.x -= _c.wheelie * 3f;
            }

            // Springs : squash, tilt (pitch / yaw / roll), hop height.
            _squashVelocity += ((1f - _squash) * 140f - _squashVelocity * 7f) * lDt;
            _squash = Mathf.Clamp(_squash + _squashVelocity * lDt, 1f - _c.squash * 2f, 1f + _c.squash * 2f);
            Vector3 lTiltTarget = new Vector3(0f, _turn * 0.1f + Mathf.Sin(_time * 3.1f) * _c.snake, -_turn * _c.wobble * 0.02f + Mathf.Sin(_time * 4.3f) * _c.wobble * 0.25f);
            _tiltVelocity += ((lTiltTarget - _tilt) * 90f - _tiltVelocity * 6f) * lDt;
            _tilt += _tiltVelocity * lDt;
            _tilt.x = Mathf.Clamp(_tilt.x, -40f, 25f);
            _hopVelocity -= 220f * RivalFactory.GROW * lDt;
            _hop += _hopVelocity * lDt;
            if (_hop < 0f)
            {
                _hop = 0f;
                if (_hopVelocity < -20f) _squashVelocity -= Mathf.Min(3f, -_hopVelocity * 0.01f) * _c.squash * 4f;
                _hopVelocity = 0f;
            }
            _flail = Mathf.MoveTowards(_flail, 0f, lDt * 1.2f);

            float lStretch = 1f + _c.stretch * (Speed01 + Mathf.Sin(_time * 7f) * 0.3f);
            float lFlailSpin = Mathf.Sin(_flail * Mathf.PI) * 180f * _flailSide;
            _pivot.localPosition = Vector3.up * _hop;
            _pivot.localRotation = Quaternion.Euler(_tilt.x + Mathf.Sin(_flail * 25f) * 20f * _flail, _tilt.y + lFlailSpin * 0.5f, _tilt.z + lFlailSpin);
            float lSide = 1f / Mathf.Sqrt(_squash * lStretch);
            _pivot.localScale = new Vector3(lSide, _squash, lSide * lStretch * lStretch);

            // Parts trail behind like jelly : they lag the pivot's motion and spring back.
            Vector3 lMove = _pivot.position - _lastPivot;
            _lastPivot = _pivot.position;
            Vector3 lLocalMove = _pivot.InverseTransformVector(lMove);
            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i] == null || _c.flail <= 0f) continue;
                float lLag = 0.6f + i * 0.25f;
                _offsetVelocity[i] += (-_offset[i] * 120f - _offsetVelocity[i] * 9f) * lDt - Vector3.ClampMagnitude(lLocalMove, 30f) * lLag;
                _offset[i] += _offsetVelocity[i] * lDt;
                _offset[i] = Vector3.ClampMagnitude(_offset[i], 6f * RivalFactory.GROW);
                _offset[i].z = Mathf.Clamp(_offset[i].z, -3f, 3f);
                _parts[i].localPosition = _rest[i] + new Vector3(_offset[i].x, _offset[i].y + Mathf.Sin(_time * 9f + i) * 0.3f * _c.hop, 0f);
            }
        }

        // Ribbons of colour and thin streaks off its back : you see how fast it goes, and where.
        private void BuildStreaks(Transform pModel, RivalKind pKind)
        {
            Bounds lBounds = new Bounds(pModel.position, Vector3.one * 4f);
            foreach (Renderer lRenderer in pModel.GetComponentsInChildren<Renderer>()) lBounds.Encapsulate(lRenderer.bounds);
            Vector3 lLocal = pModel.InverseTransformPoint(lBounds.center);
            Vector3 lHalf = lBounds.extents;
            // Wind, not neon : thin pale lines peeling off the edges. Only the airliner leaves real wingtip vapour.
            Color lWind = new Color(1f, 1f, 1f, 0.35f);
            if (pKind == RivalKind.Plane)
            {
                for (int lSide = -1; lSide <= 1; lSide += 2)
                    Streak(new Vector3(lLocal.x + lSide * lHalf.x * 0.98f, lLocal.y, lLocal.z), new Color(1f, 1f, 1f, 0.6f), Mathf.Max(1f, lHalf.y * 0.05f), 1.4f, false);
                return;
            }
            for (int lSide = -1; lSide <= 1; lSide += 2)
            {
                Streak(new Vector3(lLocal.x + lSide * lHalf.x * 0.9f, lLocal.y + lHalf.y * 0.2f, lLocal.z - lHalf.z * 0.8f), lWind, Mathf.Max(0.3f, lHalf.y * 0.06f), 0.35f, false);
                Streak(new Vector3(lLocal.x + lSide * lHalf.x * 0.4f, lLocal.y + lHalf.y * 0.8f, lLocal.z - lHalf.z * 0.6f), lWind, Mathf.Max(0.2f, lHalf.y * 0.04f), 0.25f, false);
            }
        }

        private void Streak(Vector3 pLocal, Color pColor, float pWidth, float pTime, bool pAdditive = true)
        {
            GameObject lObject = new GameObject("Streak");
            lObject.transform.SetParent(transform, false);
            lObject.transform.localPosition = pLocal;
            TrailRenderer lTrail = lObject.AddComponent<TrailRenderer>();
            lTrail.sharedMaterial = Mats.Glow(pColor, pAdditive, 1f);
            lTrail.widthMultiplier = pWidth;
            lTrail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            lTrail.time = pTime;
            lTrail.minVertexDistance = 1f;
            lTrail.numCapVertices = 2;
            lTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
