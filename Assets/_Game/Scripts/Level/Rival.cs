using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // The thing you race. Ground vehicles drive their own lane (road / track running beside the route), fliers ride
    // above the highest platform. Light rubber band. Chasers (Death, Supernova) start behind and end the run if they catch you.
    public class Rival : MonoBehaviour
    {
        public RivalKind kind;
        public float speed;
        public float startDelay;
        public bool chaser;
        public float distance;

        private RivalFactory.Placement _placement;
        private Transform _model;
        private float _elapsed;
        private float _bobTime;
        private Vector3 _lastForward = Vector3.forward;
        private float _bank;
        private bool _playerWasAhead;
        private bool _onLane;

        public bool Finished { get; private set; }
        public float Gap => RaceContext.PlayerProgress - distance;
        public bool OnLane => _onLane;
        public Transform Model => _model;

        public static Rival Create(RivalKind pKind, float pSpeed, float pStartDelay, bool pChaser, float pStartDistance, Transform pParent)
        {
            Rival lRival = new GameObject("Rival").AddComponent<Rival>();
            lRival.transform.SetParent(pParent, false);
            lRival.kind = pKind;
            lRival.speed = pSpeed;
            lRival.startDelay = pStartDelay;
            lRival.chaser = pChaser;
            lRival.distance = pStartDistance;
            lRival._model = RivalFactory.Build(pKind, lRival.transform);
            AudioManager.CreateLoop3D(RivalFactory.EngineSound(pKind), lRival.transform, 1f);
            lRival._placement = RivalFactory.GetPlacement(pKind);
            lRival._onLane = lRival._placement.onLane && RaceContext.Layout != null && RaceContext.Layout.lane != null;
            lRival.Place(0f);
            return lRival;
        }

        private void Update()
        {
            LevelLayout lLayout = RaceContext.Layout;
            if (RaceContext.Running && !Finished)
            {
                _elapsed += Time.deltaTime;
                if (_elapsed > startDelay)
                {
                    float lGap = Gap;
                    float lRubber = chaser ? Mathf.Clamp(1f + (lGap - 140f) / 500f, 0.9f, 1.3f) : Mathf.Clamp(1f + lGap / 650f, 0.85f, 1.2f);
                    distance += speed * lRubber * Time.deltaTime;
                }
                if (!chaser && lLayout != null && distance >= lLayout.finishS)
                {
                    distance = lLayout.finishS;
                    Finished = true;
                }

                bool lPlayerAhead = RaceContext.PlayerProgress > distance;
                if (!chaser && lPlayerAhead && !_playerWasAhead && _elapsed > startDelay + 0.5f)
                {
                    AudioManager.Play("sfx_pass", 1f);
                    Juice.Popup("OVERTAKE!", UIFactory.YELLOW, 1.4f);
                    Juice.Rollback(6f);
                    Juice.SlowMo(0.4f, 0.4f);
                }
                _playerWasAhead = lPlayerAhead;
            }
            else if (Finished && !chaser)
            {
                distance += speed * Time.deltaTime;
            }

            Place(Time.deltaTime);
            Animate();
        }

        // Cinematics can park the rival anywhere along its line.
        public void SetDistance(float pDistance)
        {
            distance = pDistance;
            Place(0f);
        }

        public void Frame(out Vector3 pPosition, out Quaternion pRotation)
        {
            pPosition = transform.position;
            pRotation = transform.rotation;
        }

        private void Place(float pDt)
        {
            LevelLayout lLayout = RaceContext.Layout;
            if (lLayout == null) return;
            float lS = Mathf.Clamp(distance, lLayout.spine.StartS, lLayout.spine.EndS);
            Vector3 lPosition;
            Vector3 lForward;
            Vector3 lUp;
            _bobTime += pDt;
            float lBob = Mathf.Sin(_bobTime * 2.3f) * _placement.bob;
            if (_onLane)
            {
                Vector3 lC, lR;
                float lW;
                lLayout.lane.Frame(lS, out lC, out lForward, out lR, out lUp, out lW);
                lPosition = lLayout.lane.SurfacePoint(lS, _placement.lateral) + lUp * lBob;
            }
            else
            {
                SpineSample lSample = lLayout.spine.Evaluate(lS);
                lForward = lSample.Forward;
                lUp = Vector3.up;
                float lHeight = _placement.fromSky ? lLayout.SkyY(lS) + _placement.height : lSample.position.y + _placement.height;
                lPosition = lLayout.spine.Point(lS, _placement.lateral, lHeight + lBob);
            }
            if (distance < lLayout.spine.StartS) lPosition -= lForward * (lLayout.spine.StartS - distance);
            if (distance > lLayout.spine.EndS) lPosition += lForward * (distance - lLayout.spine.EndS);

            float lYaw = pDt > 0f ? Vector3.SignedAngle(_lastForward, lForward, Vector3.up) / pDt : 0f;
            _lastForward = lForward;
            if (pDt > 0f) _bank = Mathf.Lerp(_bank, Mathf.Clamp(-lYaw * 0.6f, -35f, 35f), pDt * 3f);
            float lBankScale = _onLane ? (kind == RivalKind.Moto ? 1f : 0.15f) : 1f;
            transform.SetPositionAndRotation(lPosition, Quaternion.LookRotation(lForward, lUp) * Quaternion.Euler(0f, 0f, _bank * lBankScale));
        }

        private void Animate()
        {
            if (kind == RivalKind.Supernova)
            {
                float lPulse = 1f + Mathf.Sin(Time.time * 9f) * 0.06f + Mathf.Sin(Time.time * 23f) * 0.03f;
                _model.localScale = Vector3.one * lPulse;
            }
            else if (kind == RivalKind.Light || kind == RivalKind.Bullet)
            {
                _model.localRotation = Quaternion.Euler(0f, 0f, Time.time * 720f);
            }
        }
    }
}
