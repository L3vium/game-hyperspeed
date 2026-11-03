using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Ground : deck-relative steering, speed economy, analytic crest takeoff, intent-based edge guard, drop-through.
    public partial class PlayerController
    {
        private const float SNAP_REACH = 0.35f;
        private const float SEAM_DOT = 0.97f;
        private const float EDGE_BAND = 3.5f;
        private const float EDGE_MARGIN = 0.7f;
        private const float EXIT_INTENT = 0.3f;

        public enum TakeoffReason { Edge, Lip, Crest, Jump, Pad }

        private float _psi;
        private float _slip;
        private readonly float[] _edgePush = new float[2];
        private bool _exitGranted;
        private float _takeoffY;
        private EdgeKind _takeoffEdge;

        // ================================================================ ground step

        private void GroundStep(float pDt)
        {
            _lastGrounded = Time.time;
            Deck lDeck = _deck != null && _deck.ContainsS(Progress) ? _deck : null;
            Vector3 lForward = DeckForward(lDeck);

            UpdateDrift(pDt);
            if (_freeRoam) FreeRoamSteer(pDt);
            else
            {
                float lLateralMax = Mathf.Lerp(28f, 40f, Mathf.Clamp01((_speed - 55f) / 145f));
                float lPsiMax = Mathf.Asin(Mathf.Clamp01(lLateralMax / Mathf.Max(_speed, lLateralMax))) * Mathf.Rad2Deg;
                if (Drifting) lPsiMax *= 1.3f;
                float lTarget = (Drifting ? DriftDirection * 0.55f + Steer * 0.6f : Steer) * lPsiMax;
                if (State == PlayerState.Finished) lTarget = Mathf.Clamp(-Lateral * 2f, -10f, 10f);
                float lRate = Mathf.Abs(Steer) > 0.05f ? 9f : 6f;
                _psi = Mathf.Lerp(_psi, lTarget, 1f - Mathf.Exp(-lRate * pDt));
                _heading = Quaternion.AngleAxis(_psi, _up) * lForward;
            }

            // Speed economy.
            Sliding = _slideHeld && !Drifting && !_freeRoam;
            float lSlope = Vector3.Dot(_heading, Vector3.down);
            bool lSteep = Vector3.Dot(_up, Vector3.up) < 0.5f;
            _speed += lSlope * SLOPE_ACCEL * (lSteep && lSlope < 0f ? 0.3f : 1f) * (Sliding ? 1.8f : 1f) * pDt;
            if (lSteep && !_freeRoam) _speed = Mathf.Max(_speed, 40f);
            if (!_freeRoam)
            {
                float lTargetRun = RUN_SPEED * (1f + 0.12f * Mathf.Max(0f, HSInput.Forward));
                if (State == PlayerState.Finished) lTargetRun = Progress > _layout.finishS + 110f ? 0f : 30f;
                if (_speed < lTargetRun) _speed = Mathf.MoveTowards(_speed, lTargetRun, RUN_ACCEL * (Sliding ? 0.3f : 1f) * pDt);
                else
                {
                    float lDecay = ((_speed - lTargetRun) * 0.18f + 3f) * (Sliding ? 0.4f : 1f) * (lSlope > 0.1f ? 0.3f : 1f);
                    // Past the line : brake well inside the run-out (121 m/s stops in ~110 m).
                    if (State == PlayerState.Finished) lDecay = 50f + (_speed - lTargetRun) * 0.5f;
                    _speed = Mathf.Max(lTargetRun, _speed - lDecay * pDt);
                }
            }
            _speed = Mathf.Min(_speed, MAX_SPEED);

            // Drift slip : the motion direction lags behind the heading.
            _moveDir = Vector3.ProjectOnPlane(_moveDir, _up).normalized;
            if (Drifting)
            {
                _moveDir = Vector3.RotateTowards(_moveDir, _heading, 1.6f * pDt, 0f);
                if (Vector3.Angle(_moveDir, _heading) > DRIFT_SLIP) _moveDir = Vector3.RotateTowards(_heading, _moveDir, DRIFT_SLIP * Mathf.Deg2Rad, 0f);
            }
            else _moveDir = Vector3.RotateTowards(_moveDir, _heading, 9f * pDt, 0f);

            Vector3 lRight = Vector3.Cross(_up, lForward).normalized;
            _slip = Mathf.MoveTowards(_slip, 0f, 4f * pDt * Mathf.Max(1f, Mathf.Abs(_slip)));
            if (lDeck != null && !_freeRoam) RailMagnet(lDeck, lRight, pDt);
            _velocity = _moveDir * _speed + lRight * _slip;
            if (lDeck != null) EdgeGuardPre(lDeck, lRight, pDt);

            Move(_velocity * pDt);
            if (State != PlayerState.Ground && State != PlayerState.Finished) return;

            if (!ProbeGround(lDeck, pDt))
            {
                TakeOff(lDeck != null && lDeck.endCap && Progress >= lDeck.EndS - 3f ? TakeoffReason.Lip : TakeoffReason.Edge);
                return;
            }
            lDeck = _deck;

            // Crest : when the road curves away faster than gravity + grip can follow, you fly off with its momentum.
            if (lDeck != null && !_freeRoam)
            {
                float lCos = Mathf.Cos(_psi * Mathf.Deg2Rad);
                float lCurvature = lDeck.Convexity(Progress) * lCos * lCos;
                if (lCurvature > 0f && _speed * _speed * lCurvature > Ballistics.G_FALL * Mathf.Max(0.1f, _up.y) + lDeck.adhesion)
                {
                    TakeOff(TakeoffReason.Crest);
                    return;
                }
            }
            if (lDeck != null) EdgeGuardPost(lDeck);

            if (!_freeRoam && TryGroundRail(lDeck)) return;
            if (_slidePressed && lDeck != null && lDeck.surface == DeckSurface.Membrane && !_freeRoam)
            {
                DropThrough();
                return;
            }
            if (Time.time - _jumpBufferedAt < JUMP_BUFFER && State == PlayerState.Ground) TryJump();
        }

        private Vector3 DeckForward(Deck pDeck)
        {
            Vector3 lForward;
            if (pDeck != null)
            {
                Vector3 lC, lR, lU;
                float lW;
                pDeck.Frame(Progress, out lC, out lForward, out lR, out lU, out lW);
            }
            else lForward = _layout != null ? _layout.spine.Forward(Progress) : _heading;
            lForward = Vector3.ProjectOnPlane(lForward, _up);
            return lForward.sqrMagnitude > 0.0001f ? lForward.normalized : _heading;
        }

        // Before GO (and on the title) : camera-relative jog anywhere on the plaza.
        private void FreeRoamSteer(float pDt)
        {
            Vector3 lCameraForward = CameraRig.Instance != null ? CameraRig.Instance.transform.forward : _heading;
            Vector3 lFlatForward = Vector3.ProjectOnPlane(lCameraForward, _up).normalized;
            Vector3 lFlatRight = Vector3.Cross(_up, lFlatForward).normalized;
            Vector3 lInput = InputLocked ? Vector3.zero : lFlatForward * HSInput.Forward + lFlatRight * HSInput.Steer;
            float lAmount = Mathf.Clamp01(lInput.magnitude);
            if (lAmount > 0.1f) _heading = Vector3.RotateTowards(_heading, lInput.normalized, 10f * pDt, 0f);
            _heading = Vector3.ProjectOnPlane(_heading, _up).normalized;
            float lTarget = lAmount * FREE_ROAM_SPEED * (HSInput.DashHeld && !InputLocked ? 1.6f : 1f);
            _speed = Mathf.MoveTowards(_speed, lTarget, (lTarget > _speed ? 45f : 60f) * pDt);
            _psi = 0f;
        }

        // Ray-only ground probe. The sphere fallback is only trusted on coplanar seams (never on a ramp lip).
        private bool ProbeGround(Deck pDeck, float pDt)
        {
            RaycastHit lHit;
            Vector3 lOrigin = transform.position + _up * 1.1f;
            if (Physics.Raycast(lOrigin, -_up, out lHit, 1.1f + SNAP_REACH, GroundMask, QueryTriggerInteraction.Ignore)
                && Vector3.Dot(lHit.normal, _up) >= GROUND_MIN_DOT && lHit.normal.y >= GROUND_MIN_WORLD_DOT)
            {
                Stick(lHit, pDt);
                return true;
            }
            if (pDeck != null && pDeck.ContainsS(Progress + 1f) && Mathf.Abs(pDeck.Lateral(transform.position, Progress)) < pDeck.Width(Progress) * 0.5f - 0.5f
                && Vector3.Dot(_velocity, _up) <= 0.5f
                && Physics.SphereCast(lOrigin, 0.3f, -_up, out lHit, 0.8f + SNAP_REACH, GroundMask, QueryTriggerInteraction.Ignore)
                && Vector3.Dot(lHit.normal, _up) >= SEAM_DOT)
            {
                float lLateral;
                float lAbove = pDeck.HeightAbove(lHit.point, Progress, out lLateral);
                if (Mathf.Abs(lAbove) <= 0.3f)
                {
                    Stick(lHit, pDt);
                    return true;
                }
            }
            return false;
        }

        private void Stick(RaycastHit pHit, float pDt)
        {
            transform.position = pHit.point;
            _up = Vector3.Slerp(_up, pHit.normal, 1f - Mathf.Exp(-40f * pDt)).normalized;
            _gravityUp = _up;
            Deck lDeck = DeckChunk.DeckOf(pHit.collider);
            if (lDeck != null) _deck = lDeck;
        }

        // Leaves the ground with the exact tangent velocity of the surface : slope momentum is kept.
        private void TakeOff(TakeoffReason pReason)
        {
            Vector3 lRight = Vector3.Cross(_up, _moveDir).normalized;
            _velocity = _moveDir * _speed + lRight * _slip;
            // A lip or an edge never throws the runner up more steeply than ~27 deg (ramps and crests stay natural).
            float lClimb = _velocity.y - 0.45f * _velocity.magnitude;
            if (lClimb > 0f) _velocity.y -= lClimb;
            _gravityUp = Vector3.Dot(_up, Vector3.up) > 0.85f ? Vector3.up : _up;
            _takeoffY = transform.position.y;
            _takeoffEdge = EdgeKind.Lip;
            if (_deck != null)
            {
                float lLateral = _deck.Lateral(transform.position, Progress);
                _takeoffEdge = Mathf.Abs(lLateral) > _deck.Width(Progress) * 0.5f - 1f ? _deck.Edge(Progress, lLateral < 0f ? -1 : 1) : EdgeKind.Lip;
            }
            if (pReason == TakeoffReason.Lip && _layout != null)
            {
                KickerDef lKicker = _layout.KickerAt(_deck, Progress);
                if (lKicker != null)
                {
                    _velocity += _up * 3f;
                    KickerAssist(lKicker);
                }
            }
            SetState(PlayerState.Air);
            float lFlight = Vector3.Dot(_velocity, Vector3.up);
            if (pReason == TakeoffReason.Crest || (pReason == TakeoffReason.Lip && lFlight > 6f)) Juice.Popup("AIR!", Color.white, 0.8f);
            Debug.Log("[HS-MOVE] takeoff " + pReason + " s=" + Progress.ToString("F0") + " v=" + _speed.ToString("F0") + " vy=" + lFlight.ToString("F1"));
        }

        // ================================================================ edge guard

        private bool EdgeIsHard(EdgeKind pKind)
        {
            return pKind == EdgeKind.Wall || (pKind == EdgeKind.Lip && (Hyper || BoostAmount > 0f || Dashing || State == PlayerState.Crash));
        }

        // Magnetised lip : outward lateral speed is capped near hard or un-intended edges, exit needs 0.3 s of push.
        private void EdgeGuardPre(Deck pDeck, Vector3 pRight, float pDt)
        {
            float lHalf = pDeck.Width(Progress) * 0.5f;
            float lLateral = pDeck.Lateral(transform.position, Progress);
            int lSide = lLateral < 0f ? -1 : 1;
            int lIndex = lSide < 0 ? 0 : 1;
            EdgeKind lKind = pDeck.Edge(Progress, lSide);
            float lMargin = lHalf - Mathf.Abs(lLateral);

            bool lPushing = lMargin <= EDGE_MARGIN + 0.25f && Steer * lSide >= 0.6f;
            _edgePush[lIndex] = lPushing ? _edgePush[lIndex] + pDt : Mathf.MoveTowards(_edgePush[lIndex], 0f, pDt * 4f);
            _edgePush[1 - lIndex] = Mathf.MoveTowards(_edgePush[1 - lIndex], 0f, pDt * 4f);
            bool lHard = EdgeIsHard(lKind);
            _exitGranted = !lHard && lKind == EdgeKind.Lip && _edgePush[lIndex] >= EXIT_INTENT;
            EdgeTeeter01 = lKind == EdgeKind.Lip && !lHard ? Mathf.Clamp01(_edgePush[lIndex] / EXIT_INTENT) : 0f;
            EdgeTeeterSide = lSide;

            if (lKind == EdgeKind.Open || lKind == EdgeKind.Joined || _exitGranted) return;
            float lOutward = Vector3.Dot(_velocity, pRight) * lSide;
            if (lOutward <= 0f || lMargin >= EDGE_BAND) return;
            float lCap = 30f * Mathf.Clamp01((lMargin - EDGE_MARGIN) / (EDGE_BAND - EDGE_MARGIN));
            if (lOutward > lCap)
            {
                _velocity -= pRight * lSide * (lOutward - lCap);
                if (_psi * lSide > 0f) _psi = Mathf.Lerp(_psi, 0f, 1f - Mathf.Exp(-12f * pDt));
                if (_slip * lSide > 0f) _slip = 0f;
            }
        }

        private void EdgeGuardPost(Deck pDeck)
        {
            float lHalf = pDeck.Width(Progress) * 0.5f;
            float lLateral = pDeck.Lateral(transform.position, Progress);
            int lSide = lLateral < 0f ? -1 : 1;
            EdgeKind lKind = pDeck.Edge(Progress, lSide);
            if (lKind == EdgeKind.Open || lKind == EdgeKind.Joined) return;
            if (_exitGranted)
            {
                _slip = lSide * Mathf.Max(Mathf.Abs(_slip), 6f);
                return;
            }
            float lLimit = lHalf - EDGE_MARGIN;
            if (Mathf.Abs(lLateral) <= lLimit) return;
            Vector3 lC, lF, lR, lU;
            float lW;
            pDeck.Frame(Progress, out lC, out lF, out lR, out lU, out lW);
            transform.position += lR * lSide * (lLimit - Mathf.Abs(lLateral));
            if (_psi * lSide > 0f) _psi = 0f;
            if (EdgeIsHard(lKind) && Hyper && Random.value < 0.3f) FX.Sparks(transform.position + lR * lSide * 0.6f + _up, UIFactory.CYAN, 3, 8f, 0.25f, 0.2f);
        }

        // ================================================================ drift

        private void UpdateDrift(float pDt)
        {
            if (State != PlayerState.Ground || _freeRoam || (_deck != null && _deck.surface == DeckSurface.Membrane))
            {
                EndDrift(false);
                return;
            }
            if (!Drifting && _slideHeld && Mathf.Abs(Steer) > 0.35f && _speed > 30f)
            {
                Drifting = true;
                DriftDirection = Steer > 0f ? 1 : -1;
                DriftCharge = 0f;
                AudioManager.Play("sfx_drift_start", 0.7f);
            }
            if (!Drifting) return;
            if (!_slideHeld)
            {
                EndDrift(true);
                return;
            }
            int lTierBefore = DriftTier;
            DriftCharge += pDt * (0.55f + Mathf.Abs(Steer) * 0.65f);
            if (DriftTier > lTierBefore)
            {
                AudioManager.Play("sfx_drift_tier", 0.6f, 0.9f + DriftTier * 0.15f, 0f);
                Juice.Pulse(0.25f);
            }
            AudioManager.SetLoop("sfx_drift_loop", 0.45f, 0.9f + DriftTier * 0.12f);
        }

        private void EndDrift(bool pRelease)
        {
            if (!Drifting) return;
            Drifting = false;
            AudioManager.SetLoop("sfx_drift_loop", 0f);
            if (!pRelease) { DriftCharge = 0f; return; }
            int lTier = DriftTier;
            DriftCharge = 0f;
            if (lTier == 0) return;
            float[] lBoosts = { 0f, 14f, 24f, 36f };
            string[] lNames = { "", "DRIFT!", "SUPER DRIFT!", "ULTRA DRIFT!!" };
            ApplyBoost(lBoosts[lTier], _heading, 0f, lNames[lTier]);
        }

        // ================================================================ membranes

        private bool _passThrough;
        private float _dropDeckY;
        private int _breakthroughs;

        // Crouch on a cracked membrane : break through to the deck below.
        private void DropThrough()
        {
            _passThrough = true;
            _breakthroughs = 0;
            _dropDeckY = transform.position.y;
            _velocity = _moveDir * _speed + Vector3.down * 45f;
            _gravityUp = Vector3.up;
            _takeoffY = transform.position.y;
            Diving = true;
            _diveStart = Time.time;
            SetState(PlayerState.Air);
        }

        private void Breakthrough()
        {
            _breakthroughs++;
            Vector3 lFlat = Vector3.ProjectOnPlane(_velocity, Vector3.up);
            if (lFlat.sqrMagnitude > 1f) _velocity += lFlat.normalized * 8f;
            Juice.Popup(_breakthroughs > 1 ? "BREAKTHROUGH x" + _breakthroughs : "BREAKTHROUGH!", UIFactory.CYAN, 1.1f);
            Juice.Shake(0.25f);
            FX.Shards(transform.position, RaceContext.Theme != null ? RaceContext.Theme.accent : Color.cyan, 22, 18f, 0.6f, Vector3.down * 10f);
            AudioManager.Play("sfx_shatter", 0.8f, 1.2f);
            AddCombo();
        }

        // While passing through, count every membrane layer crossed ; solid again once below and crouch released.
        private void UpdatePassThrough(Vector3 pBefore)
        {
            if (!_passThrough && !(Diving && _slideHeld)) return;
            if (_layout != null)
            {
                foreach (Deck lDeck in _layout.DecksAt(Progress))
                {
                    if (lDeck.surface != DeckSurface.Membrane || !lDeck.ContainsS(Progress)) continue;
                    float lLateral;
                    float lBefore = lDeck.HeightAbove(pBefore, Progress, out lLateral);
                    float lAfter = lDeck.HeightAbove(transform.position, Progress, out lLateral);
                    if (lBefore >= 0f && lAfter < 0f && Mathf.Abs(lLateral) < lDeck.Width(Progress) * 0.5f) Breakthrough();
                }
            }
            if (_passThrough && transform.position.y < _dropDeckY - 2.5f && !_slideHeld) _passThrough = false;
        }
    }
}
