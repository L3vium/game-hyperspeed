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
        private RivalWobble _wobble;

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
            lRival._wobble = RivalWobble.Attach(lRival._model, pKind);
            if (pKind != RivalKind.Plane && pKind != RivalKind.Supernova && pKind != RivalKind.Time) RivalBeacon.Attach(lRival.transform, lRival._model);
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
                    // Overtaken : it flails.
                    if (_wobble != null) _wobble.Hiccup(1f);
                }
                else if (!lPlayerAhead && _playerWasAhead && _wobble != null) _wobble.Hiccup(0.6f);
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

            // No surges any more : it never tries to really get away from you.
            bool lSurging = false;
            bool lClimax = pLayout.climaxS > 0f && RaceContext.PlayerProgress > pLayout.climaxS;
            bool lLastMeters = lClimax && RaceContext.PlayerProgress > Mathf.Lerp(pLayout.climaxS, pLayout.finishS, 0.55f);

            // Neck and neck : right beside you, a nose ahead then a nose behind. Chasers only ever hunt from behind.
            float lSwap = Mathf.Sin(_elapsed * Mathf.PI * 2f / 9f);
            float lTarget = chaser ? lCruise * 0.45f : lCruise * 0.07f * lSwap;
            // Before the last straight it pulls level and stays there ; in the very last metres it is yours to take.
            if (lClimax && !chaser) lTarget = lLastMeters ? lCruise * 0.18f : -lCruise * 0.03f;
            float lWanted = speed * Mathf.Clamp(1f + (lGap - lTarget) / (lCruise * 1.2f), 0.8f, 1.6f);
            // Far behind after a big mistake of yours : it keeps its lead and does not run away. Far behind because you
            // are much faster : it only gains a little, never teleports.
            if (lGap > lCruise * 2.5f) lWanted = Mathf.Min(lWanted, speed * 1.3f);
            // Base pace follows the runner, so "faster" and "slower" mean against you, not against a fixed number.
            float lPace = Mathf.Max(speed, RaceContext.Player != null ? RaceContext.Player.Speed * 0.97f : speed);
            if (lGap > lCruise * 2.5f) lPace = speed;
            lWanted *= lPace / speed;
            float lBefore = _rate;
            _rate = Mathf.MoveTowards(_rate, lWanted, lCruise * 2.2f * pDt);
            if (_wobble != null) _wobble.Accelerate((_rate - lBefore) / Mathf.Max(0.0001f, pDt) / lCruise);
            distance += _rate * pDt;

            // In front of you : things get in the way.
            if (lSurging && lGap < -lCruise * 0.4f)
            {
                _dropClock -= pDt;
                if (_dropClock <= 0f)
                {
                    _dropClock = DROP_EVERY;
                    float lS = distance - 10f;
                    if (pLayout.field.Solid(lS) && lS < pLayout.finishS - lCruise) Crate.Create(transform.parent, pLayout, lS, _lateral, 60f, false, RaceContext.Theme);
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
            float lStart = Mathf.Clamp(_placement.lateral * 4f, -lHalf * 0.7f, lHalf * 0.7f);
            float lRoute = lLayout.RouteLateral(lS) + Mathf.Clamp(_placement.lateral * 1.6f + Mathf.Sin(lS * 0.0031f) * lHalf * 0.25f, -lHalf * 0.6f, lHalf * 0.6f);
            float lLateralTarget = lS < lLayout.startLineS + 40f ? lStart : Mathf.Clamp(lRoute, lCenter - lHalf * 0.85f, lCenter + lHalf * 0.85f);
            // Close to you : it comes alongside, at arm's length, on its own side.
            if (Mathf.Abs(Gap) < lLayout.profile.cruise * 0.8f && RaceContext.Player != null && !_placement.flying)
            {
                float lSide = _placement.lateral >= 0f ? 1f : -1f;
                lLateralTarget = Mathf.Clamp(RaceContext.Player.Lateral + lSide * Mathf.Max(14f, lHalf * 0.07f), lCenter - lHalf * 0.85f, lCenter + lHalf * 0.85f);
            }
            // The airliner flies : wide sweeps from one side of the valley to the other.
            if (kind == RivalKind.Plane) lLateralTarget = lCenter + Mathf.Sin(_bobTime * 0.25f) * lHalf * 0.6f;
            if (pDt > 0f) _lateral = Mathf.MoveTowards(_lateral, lLateralTarget, Mathf.Max(speed * 0.5f, 30f) * pDt);

            _bobTime += pDt;
            float lBob = Mathf.Sin(_bobTime * 2.3f) * _placement.bob;
            float lHeight = (_placement.height + lBob + (_placement.flying && _placement.fromSky ? 18f : 0f)) * RivalFactory.GROW;
            // Climbs and dives over the track.
            if (kind == RivalKind.Plane) lHeight += Mathf.Sin(_bobTime * 0.33f) * 120f + Mathf.Sin(_bobTime * 0.9f) * 25f;
            Vector3 lPosition = lLayout.field.Point(lS, _lateral) + lUp * lHeight;
            if (distance < lLayout.spine.StartS) lPosition -= lForward * (lLayout.spine.StartS - distance);
            if (distance > lLayout.spine.EndS) lPosition += lForward * (distance - lLayout.spine.EndS);

            float lYaw = pDt > 0f ? Vector3.SignedAngle(_lastForward, lForward, lUp) / pDt : 0f;
            _lastForward = lForward;
            if (pDt > 0f) _bank = Mathf.Lerp(_bank, Mathf.Clamp(-lYaw * 0.6f, -35f, 35f), pDt * 3f);
            if (_wobble != null && pDt > 0f) _wobble.Turn(lYaw);
            float lBankScale = _placement.flying || kind == RivalKind.Moto ? 1f : 0.15f;
            if (kind == RivalKind.Plane) _bank = Mathf.Lerp(_bank, -Mathf.Cos(_bobTime * 0.25f) * 40f, pDt * 2f);
            Vector3 lFlat = Vector3.ProjectOnPlane(lForward, lUp);
            transform.SetPositionAndRotation(lPosition, Quaternion.LookRotation(lFlat.sqrMagnitude > 0.001f ? lFlat : lForward, lUp) * Quaternion.Euler(0f, 0f, _bank * lBankScale));
        }

        private void Animate()
        {
            if (_wobble != null) _wobble.Speed01 = Mathf.Clamp01(_rate / Mathf.Max(1f, speed * 2f));
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
