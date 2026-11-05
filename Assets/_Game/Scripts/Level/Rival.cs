using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // What you race, on the same track as you. It never lets you breathe : it hunts you down from behind, then surges past,
    // stays in front a moment and drops something in your way before falling back. Chasers (T-rex, Death, Supernova) end
    // the run if they catch you from behind, but a pass is only a threat.
    public class Rival : MonoBehaviour
    {
        private const float SURGE_EVERY = 19f;
        private const float SURGE_TIME = 3.6f;
        private const float DROP_EVERY = 1.1f;

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
        private float _surgeClock;
        private float _surgeLeft;
        private float _dropClock;
        private float _rate;

        public bool Finished { get; private set; }
        public float Gap => RaceContext.PlayerProgress - distance;
        public Transform Model => _model;
        // Going past you on purpose : being overtaken this way is not being caught.
        public bool Passing { get; private set; }

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
            lRival._rate = pSpeed;
            lRival._surgeClock = SURGE_EVERY * 0.6f;
            lRival.Place(0f);
            return lRival;
        }

        private void Update()
        {
            LevelLayout lLayout = RaceContext.Layout;
            if (RaceContext.Running && !Finished && lLayout != null)
            {
                float lDt = Time.deltaTime;
                _elapsed += lDt;
                if (_elapsed > startDelay) Pursue(lLayout, lDt);
                if (!chaser && distance >= lLayout.finishS)
                {
                    distance = lLayout.finishS;
                    Finished = true;
                }

                bool lPlayerAhead = RaceContext.PlayerProgress > distance;
                if (!chaser && lPlayerAhead && !_playerWasAhead && _elapsed > startDelay + 0.5f)
                {
                    AudioManager.Play("sfx_pass", 1f);
                    Juice.Rollback(6f);
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

        private void Pursue(LevelLayout pLayout, float pDt)
        {
            float lCruise = pLayout.profile.cruise;
            float lGap = Gap;
            bool lNearEnd = RaceContext.PlayerProgress > pLayout.finishS * 0.88f;

            _surgeClock -= pDt;
            if (_surgeClock <= 0f && !lNearEnd && lGap < lCruise * 3f && lGap > -lCruise * 2f)
            {
                _surgeClock = SURGE_EVERY * Random.Range(0.8f, 1.2f);
                _surgeLeft = SURGE_TIME;
                Passing = true;
                AudioManager.Play("sfx_pass", 0.8f);
            }
            _surgeLeft -= pDt;
            bool lSurging = _surgeLeft > 0f;

            // Hanging just behind you, or just in front during a surge : always close enough to feel.
            float lTarget = lSurging ? -lCruise * 1.1f : lCruise * 0.9f;
            float lWanted = speed * Mathf.Clamp(1f + (lGap - lTarget) / (lCruise * 2.4f), 0.72f, lSurging ? 2.6f : 1.7f);
            _rate = Mathf.MoveTowards(_rate, lWanted, lCruise * 1.6f * pDt);
            distance += _rate * pDt;

            // In front of you : things get in the way.
            if (lSurging && lGap < -lCruise * 0.4f)
            {
                _dropClock -= pDt;
                if (_dropClock <= 0f)
                {
                    _dropClock = DROP_EVERY;
                    float lS = distance - 10f;
                    if (pLayout.field.Solid(lS) && lS < pLayout.finishS - lCruise) Crate.Create(transform.parent, pLayout, lS, _lateral, 22f, false, RaceContext.Theme);
                }
            }
            if (Passing && !lSurging && lGap > lCruise * 0.15f) Passing = false;
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
            Vector3 lForward = lSample.forward;
            Vector3 lUp = lLayout.field.Normal(lS);

            // Beside the runner at the start, then on the clear line with a slow wander.
            float lHalf = lLayout.field.HalfWidth(lS);
            float lCenter = lLayout.field.CenterLateral(lS);
            float lStart = Mathf.Clamp(_placement.lateral, -lHalf * 0.7f, lHalf * 0.7f);
            float lRoute = lLayout.RouteLateral(lS) + Mathf.Clamp(_placement.lateral * 0.4f + Mathf.Sin(lS * 0.0031f) * lHalf * 0.25f, -lHalf * 0.6f, lHalf * 0.6f);
            float lLateralTarget = lS < lLayout.startLineS + 40f ? lStart : Mathf.Clamp(lRoute, lCenter - lHalf * 0.85f, lCenter + lHalf * 0.85f);
            if (pDt > 0f) _lateral = Mathf.MoveTowards(_lateral, lLateralTarget, Mathf.Max(speed * 0.5f, 30f) * pDt);

            _bobTime += pDt;
            float lBob = Mathf.Sin(_bobTime * 2.3f) * _placement.bob;
            float lHeight = _placement.height + lBob + (_placement.flying && _placement.fromSky ? 18f : 0f);
            Vector3 lPosition = lLayout.field.Point(lS, _lateral) + lUp * lHeight;
            if (distance < lLayout.spine.StartS) lPosition -= lForward * (lLayout.spine.StartS - distance);
            if (distance > lLayout.spine.EndS) lPosition += lForward * (distance - lLayout.spine.EndS);

            float lYaw = pDt > 0f ? Vector3.SignedAngle(_lastForward, lForward, lUp) / pDt : 0f;
            _lastForward = lForward;
            if (pDt > 0f) _bank = Mathf.Lerp(_bank, Mathf.Clamp(-lYaw * 0.6f, -35f, 35f), pDt * 3f);
            float lBankScale = _placement.flying || kind == RivalKind.Moto ? 1f : 0.15f;
            Vector3 lFlat = Vector3.ProjectOnPlane(lForward, lUp);
            transform.SetPositionAndRotation(lPosition, Quaternion.LookRotation(lFlat.sqrMagnitude > 0.001f ? lFlat : lForward, lUp) * Quaternion.Euler(0f, 0f, _bank * lBankScale));
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
