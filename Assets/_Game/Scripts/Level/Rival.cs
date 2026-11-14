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
        // What it really runs at right now (its base speed bent by the race).
        public float CurrentRate => Finished ? speed : _rate;
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
            RivalLife.Attach(lRival, lRival._model, pKind);
            if (pKind != RivalKind.Plane && pKind != RivalKind.Supernova && pKind != RivalKind.Time) RivalBeacon.Attach(lRival.transform, lRival._model);
            AudioManager.CreateLoop3D(RivalFactory.EngineSound(pKind), lRival.transform, 0.55f);
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
                    HSJuice.Play(JuiceId.Overtake, transform.position);
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
            float lTarget = chaser ? lCruise * 0.9f : lCruise * 0.07f * lSwap;
            // Before the last straight it pulls level and stays there ; in the very last metres it is yours to take.
            if (lClimax && !chaser) lTarget = lLastMeters ? lCruise * 0.18f : -lCruise * 0.03f;
            // Its own pace, only gently bent by the race : a little quicker when you are far ahead (it stays in the
            // fight), a little slower when it leads. It never copies your speed and never waits for you : beating it
            // by a lot really means you were much faster, and that is what the rank reads.
            // On a light highway the rival runs at the highway's pace : Light itself at 85 % of light speed ; the chasers just under
            // the highway's stream (40 %), so whoever missed the light is not caught for it, only left without a lead.
            float lBase = speed;
            float lHighwayEnd = pLayout.profile.LightEnabled ? pLayout.HighwayEnd(distance) : -1f;
            if (lHighwayEnd > 0f)
            {
                // Brakes over the highway's last 1.5 s of light, like the runner.
                float lBrake = pLayout.profile.lightSpeed * 1.5f;
                float lHigh = pLayout.profile.lightSpeed * (chaser ? 0.36f : 0.85f);
                lBase = Mathf.Lerp(Mathf.Max(speed, pLayout.profile.hyperEnter * 1.2f), lHigh, HSMath.Smooth(Mathf.Clamp01((lHighwayEnd - distance) / lBrake)));
            }
            // 2026-11-14, rubber band on YOUR real speed (asked) : behind you it speeds up to stay in the race, ahead of you
            // it eases off so you can catch it. Gentle on the first stages (an easy race), tighter on the last ones.
            float lDifficulty = pLayout.profile.difficulty;
            float lPlayerSpeed = RaceContext.Player != null ? Mathf.Max(RaceContext.Player.Speed, lCruise * 0.5f) : lBase;
            float lSeconds = (lGap - lTarget) / Mathf.Max(1f, Mathf.Max(lPlayerSpeed, lCruise));
            float lWanted;
            if (chaser)
            {
                // Hunts from behind at its distance : it only catches you if you stop running.
                lWanted = lPlayerSpeed * Mathf.Clamp(1f + lSeconds * 0.12f, 0.8f, Mathf.Lerp(1.08f, 1.18f, lDifficulty));
            }
            else if (lSeconds > 0f)
            {
                // You lead : past a second and a half it closes in, faster the further back it is. On the last straight it
                // stops pushing : a lead you built is yours to keep.
                float lPush = lClimax ? 0f : Mathf.Lerp(0.04f, 0.18f, lDifficulty) * Mathf.Clamp01((lSeconds - 1.5f) / 3f);
                lWanted = Mathf.Max(lBase * 0.9f, lPlayerSpeed * (1f + lPush));
            }
            else
            {
                // It leads : it eases off below your pace (a lot on the first stages, a little on the last ones).
                float lEase = Mathf.Lerp(0.4f, 0.08f, lDifficulty) * Mathf.Clamp01(-lSeconds / 2f);
                lWanted = Mathf.Min(lBase * 1.6f, lPlayerSpeed * (1f - lEase));
            }
            float lBefore = _rate;
            _rate = Mathf.MoveTowards(_rate, lWanted, Mathf.Max(lCruise * 2.2f, Mathf.Abs(lWanted - _rate) * 1.5f) * pDt);
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
            // Its own lane : a vehicle keeps to its side of the track, slowly following the clear line, never cutting across.
            if (lS >= lLayout.startLineS + 40f && !_placement.flying)
            {
                float lSide = _placement.lateral >= 0f ? 1f : -1f;
                lLateralTarget = Mathf.Clamp(lCenter + lSide * lHalf * 0.38f + Mathf.Sin(lS * 0.0013f) * lHalf * 0.06f, lCenter - lHalf * 0.8f, lCenter + lHalf * 0.8f);
            }
            // The airliner is far up in the sky, well ahead and off to one side : a huge shape to chase, never on top of you.
            if (kind == RivalKind.Plane) lLateralTarget = lCenter + (lHalf + 260f) * (_placement.lateral >= 0f ? 1f : -1f) + Mathf.Sin(_bobTime * 0.12f) * 80f;
            if (pDt > 0f) _lateral = Mathf.MoveTowards(_lateral, lLateralTarget, Mathf.Max(speed * 0.25f, 20f) * pDt);

            _bobTime += pDt;
            float lBob = Mathf.Sin(_bobTime * 2.3f) * _placement.bob;
            float lHeight = (_placement.height + lBob + (_placement.flying && _placement.fromSky ? 18f : 0f)) * RivalFactory.GROW;
            if (kind == RivalKind.Plane)
            {
                // Shown 300 m ahead of where it really is in the race, 420 m up, gently rising and sinking.
                lHeight = 420f + Mathf.Sin(_bobTime * 0.2f) * 50f;
                lS = Mathf.Clamp(distance + 300f, lLayout.spine.StartS, lLayout.spine.EndS);
                lUp = Vector3.up;
            }
            Vector3 lPosition = lLayout.field.Point(lS, _lateral) + lUp * lHeight;
            if (distance < lLayout.spine.StartS) lPosition -= lForward * (lLayout.spine.StartS - distance);
            if (distance > lLayout.spine.EndS) lPosition += lForward * (distance - lLayout.spine.EndS);

            float lYaw = pDt > 0f ? Vector3.SignedAngle(_lastForward, lForward, lUp) / pDt : 0f;
            _lastForward = lForward;
            if (pDt > 0f) _bank = Mathf.Lerp(_bank, Mathf.Clamp(-lYaw * 0.6f, -35f, 35f), pDt * 3f);
            if (_wobble != null && pDt > 0f) _wobble.Turn(lYaw);
            float lBankScale = _placement.flying || kind == RivalKind.Moto ? 1f : 0.15f;
            if (kind == RivalKind.Plane) _bank = Mathf.Lerp(_bank, Mathf.Clamp(-lYaw * 0.3f, -12f, 12f), pDt * 1.5f);
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
            else if (kind == RivalKind.Light)
            {
                _model.localRotation = Quaternion.Euler(0f, 0f, Time.time * 720f);
            }
        }
    }
}
