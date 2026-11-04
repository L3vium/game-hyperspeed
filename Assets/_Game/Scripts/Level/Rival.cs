using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // What you race, on the same terrain as you, right next to you from the start line. Ground rivals follow the route's
    // clear line and fly off crests with real gravity, fliers ride above the highest ground. Light rubber band.
    // Chasers (T-rex, Death, Supernova) start behind and end the run if they catch you.
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
        private float _lateral;
        private float _y;
        private float _vy;
        private bool _groundReady;

        public bool Finished { get; private set; }
        public float Gap => RaceContext.PlayerProgress - distance;
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
            lRival._lateral = lRival._placement.lateral;
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
                    float lScale = lLayout != null ? lLayout.profile.k : 1f;
                    float lRubber = chaser ? Mathf.Clamp(1f + (lGap - 200f * lScale) / (700f * lScale), 0.9f, 1.3f) : Mathf.Clamp(1f + lGap / (900f * lScale), 0.85f, 1.2f);
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

        public void SetDistance(float pDistance)
        {
            distance = pDistance;
            Place(0f);
        }

        private void Place(float pDt)
        {
            LevelLayout lLayout = RaceContext.Layout;
            if (lLayout == null) return;
            float lS = Mathf.Clamp(distance, lLayout.spine.StartS, lLayout.spine.EndS);
            SpineSample lSample = lLayout.spine.Evaluate(lS);
            Vector3 lForward = lSample.Forward;
            Vector3 lUp = Vector3.up;

            // Beside the runner at the start, then on the clear line with a slow wander.
            float lStart = _placement.lateral;
            float lRoute = lLayout.RouteLateral(lS) + _placement.lateral * 0.4f + Mathf.Sin(lS * 0.0031f / lLayout.profile.k) * lLayout.field.HalfWidth(lS) * 0.18f;
            float lLateralTarget = lS < lLayout.startLineS + 40f ? lStart : lRoute;
            if (pDt > 0f) _lateral = Mathf.MoveTowards(_lateral, lLateralTarget, Mathf.Max(speed * 0.35f, 30f) * pDt);

            _bobTime += pDt;
            float lBob = Mathf.Sin(_bobTime * 2.3f) * _placement.bob;
            float lGround = lLayout.Ground(lS, _lateral);
            float lHeight;
            if (_placement.flying)
            {
                lHeight = (_placement.fromSky ? lLayout.SkyY(lS) : lGround) + _placement.height + lBob;
                _y = lHeight;
            }
            else
            {
                // Real gravity : off a lip it flies, it lands where the ground meets it.
                if (!_groundReady || pDt <= 0f)
                {
                    _y = lGround;
                    _vy = 0f;
                    _groundReady = true;
                }
                else
                {
                    _vy -= Ballistics.G_FALL * Ballistics.Scale * pDt;
                    _y += _vy * pDt;
                    if (_y <= lGround)
                    {
                        float lSlopeY = (lGround - _y) / pDt;
                        _vy = Mathf.Clamp(lSlopeY, 0f, speed * 0.4f);
                        _y = lGround;
                    }
                }
                lHeight = _y + _placement.height + lBob;
                bool lGrounded = _y <= lGround + 0.2f;
                float lPitchStep = Mathf.Max(8f, 10f * lLayout.profile.k);
                if (lGrounded)
                {
                    Vector3 lNormal = lLayout.field.Normal(lS, _lateral, lPitchStep);
                    lUp = lNormal;
                    lForward = Vector3.ProjectOnPlane(lForward, lNormal).normalized;
                }
            }
            Vector3 lPosition = lLayout.spine.Point(lS, _lateral, lHeight);
            if (distance < lLayout.spine.StartS) lPosition -= lForward * (lLayout.spine.StartS - distance);
            if (distance > lLayout.spine.EndS) lPosition += lForward * (distance - lLayout.spine.EndS);

            float lYaw = pDt > 0f ? Vector3.SignedAngle(_lastForward, lForward, Vector3.up) / pDt : 0f;
            _lastForward = lForward;
            if (pDt > 0f) _bank = Mathf.Lerp(_bank, Mathf.Clamp(-lYaw * 0.6f, -35f, 35f), pDt * 3f);
            float lBankScale = _placement.flying || kind == RivalKind.Moto ? 1f : 0.15f;
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
