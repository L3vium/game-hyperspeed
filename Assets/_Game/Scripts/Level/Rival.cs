using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // The thing you race. Follows the track path with a light rubber band.
    // Chasers (Death, Supernova) start behind and end the run if they catch you.
    public class Rival : MonoBehaviour
    {
        public RivalKind kind;
        public float speed;
        public float startDelay;
        public bool chaser;
        public float distance;

        private RivalFactory.Placement _placement;
        private Transform _model;
        private Transform _hourHand;
        private Transform _minuteHand;
        private float _elapsed;
        private float _bobTime;
        private Quaternion _lastRotation = Quaternion.identity;
        private float _bank;
        private bool _playerWasAhead;

        public bool Finished { get; private set; }
        public float Gap => RaceContext.PlayerProgress - distance;

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
            lRival._placement = RivalFactory.GetPlacement(pKind, TrackBuilder.DEFAULT_WIDTH);
            lRival._hourHand = lRival._model.Find("HourHand");
            lRival._minuteHand = lRival._model.Find("MinuteHand");
            lRival.Place(0f);
            return lRival;
        }

        private void Update()
        {
            if (RaceContext.Running && !Finished)
            {
                _elapsed += Time.deltaTime;
                if (_elapsed > startDelay)
                {
                    float lGap = Gap;
                    float lRubber = chaser ? Mathf.Clamp(1f + (lGap - 140f) / 500f, 0.9f, 1.3f) : Mathf.Clamp(1f + lGap / 650f, 0.82f, 1.22f);
                    distance += speed * lRubber * Time.deltaTime;
                }
                if (!chaser && distance >= RaceContext.Path.finishDistance)
                {
                    distance = RaceContext.Path.finishDistance;
                    Finished = true;
                }

                bool lPlayerAhead = RaceContext.PlayerProgress > distance;
                if (!chaser && lPlayerAhead && !_playerWasAhead && _elapsed > startDelay + 0.5f)
                {
                    AudioManager.Play("sfx_pass", 1f);
                    Juice.Popup("OVERTAKE!", UIFactory.YELLOW, 1.4f);
                    Juice.Rollback(10f);
                    Juice.ImpactFrame(1f);
                    Juice.SlowMo(0.3f, 0.5f);
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

        // Intro fly-by : the rival storms past the start line, then gets parked back at its start distance.
        public void IntroOffset(float pDistance)
        {
            distance = pDistance;
            Place(0f);
        }

        private void Place(float pDt)
        {
            if (RaceContext.Path == null) return;
            float lDistance = Mathf.Clamp(distance, 0f, RaceContext.Path.Length);
            TrackSample lSample = RaceContext.Path.Evaluate(lDistance);
            _bobTime += pDt;
            float lBob = Mathf.Sin(_bobTime * 2.3f) * _placement.bob;
            Vector3 lLateralAxis = lSample.Right;
            Vector3 lUp = _placement.height > 0f ? Vector3.Slerp(lSample.Up, Vector3.up, 0.5f) : lSample.Up;
            Vector3 lPosition = lSample.position + lLateralAxis * _placement.lateral + lUp * (_placement.height + lBob);
            if (distance < 0f) lPosition -= lSample.Forward * -distance;
            if (distance > RaceContext.Path.Length) lPosition += lSample.Forward * (distance - RaceContext.Path.Length);

            Quaternion lRotation = Quaternion.LookRotation(lSample.Forward, lUp);
            float lYaw = Vector3.SignedAngle(_lastRotation * Vector3.forward, lSample.Forward, lUp);
            _lastRotation = lRotation;
            if (pDt > 0f) _bank = Mathf.Lerp(_bank, Mathf.Clamp(-lYaw / pDt * 0.6f, -35f, 35f), pDt * 3f);
            float lBankScale = _placement.height > 0f ? 1f : 0.2f;
            transform.SetPositionAndRotation(lPosition, lRotation * Quaternion.Euler(0f, 0f, _bank * lBankScale));
        }

        private void Animate()
        {
            if (_hourHand != null) _hourHand.localRotation = Quaternion.Euler(0f, 0f, -Time.time * 30f);
            if (_minuteHand != null) _minuteHand.localRotation = Quaternion.Euler(0f, 0f, -Time.time * 360f);
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
