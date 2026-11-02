using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Rails and zip lines : caught from the air (magnet radius) or from the ground (V trough funnels you on),
    // ends buried in the next deck so you land directly instead of popping into the void.
    public partial class PlayerController
    {
        private GrindRail _rail;
        private float _railDistance;
        private int _railDirection;
        private float _railCooldown;
        private float _railBlend;
        private Vector3 _railFrom;

        private void TryGrind()
        {
            if (_railCooldown > 0f) return;
            foreach (GrindRail lRail in GrindRail.All)
            {
                if (_layout != null && (Progress < lRail.StartS - 15f || Progress > lRail.EndS + 15f)) continue;
                // Rails are caught from above, zip lines from below (hands reach up to the cable).
                Vector3 lProbe = transform.position + _gravityUp * (lRail.zip ? 2.2f : 0.3f);
                float lDistance, lRailDistance;
                if (!lRail.FindNearest(lProbe, RAIL_RADIUS, out lDistance, out lRailDistance)) continue;
                Vector3 lPos, lTangent, lUp;
                lRail.Evaluate(lRailDistance, out lPos, out lTangent, out lUp);
                float lAlong = Vector3.Dot(_velocity, lTangent);
                if (Mathf.Abs(lAlong) < 8f) continue;
                if (!lRail.zip && Vector3.Dot(transform.position - lPos, lUp) < -0.8f) continue;
                if (lRail.zip) AttachZip(lRail, lRailDistance, lAlong > 0f ? 1 : -1);
                else AttachRail(lRail, lRailDistance, lAlong > 0f ? 1 : -1, lPos);
                return;
            }
        }

        // Ground catch : rails lying in a trough or gutter magnetize you on your way in.
        private void RailMagnet(Deck pDeck, Vector3 pRight, float pDt)
        {
            if (_railCooldown > 0f) return;
            foreach (GrindRail lRail in GrindRail.All)
            {
                if (lRail.zip || Progress < lRail.StartS - 4f || Progress > lRail.EndS) continue;
                float lDistance, lRailDistance;
                if (!lRail.FindNearest(transform.position + _up * 0.3f, 4f, out lDistance, out lRailDistance)) continue;
                Vector3 lPos, lTangent, lUp;
                lRail.Evaluate(lRailDistance, out lPos, out lTangent, out lUp);
                float lSide = Vector3.Dot(lPos - transform.position, pRight);
                _slip = Mathf.MoveTowards(_slip, Mathf.Clamp(lSide * 4f, -14f, 14f), 60f * pDt);
                return;
            }
        }

        private bool TryGroundRail(Deck pDeck)
        {
            if (_railCooldown > 0f || _speed < 15f) return false;
            foreach (GrindRail lRail in GrindRail.All)
            {
                if (lRail.zip || Progress < lRail.StartS - 4f || Progress > lRail.EndS - 6f) continue;
                float lDistance, lRailDistance;
                if (!lRail.FindNearest(transform.position + _up * 0.3f, 1.4f, out lDistance, out lRailDistance)) continue;
                Vector3 lPos, lTangent, lUp;
                lRail.Evaluate(lRailDistance, out lPos, out lTangent, out lUp);
                float lVertical = Vector3.Dot(lPos - transform.position, _up);
                if (lVertical < -0.8f || lVertical > 1.2f) continue;
                float lAlong = Vector3.Dot(_moveDir * _speed, lTangent);
                if (Mathf.Abs(lAlong) < 15f) continue;
                AttachRail(lRail, lRailDistance, lAlong > 0f ? 1 : -1, lPos);
                return true;
            }
            return false;
        }

        private void AttachRail(GrindRail pRail, float pDistance, int pDirection, Vector3 pPoint)
        {
            _rail = pRail;
            _railDistance = pDistance;
            _railDirection = pDirection;
            _railBlend = 0.1f;
            _railFrom = transform.position;
            _speed = Mathf.Max(Speed, 40f) + 6f;
            SetState(PlayerState.Grind);
            Diving = false;
            _passThrough = false;
            HasDoubleJump = true;
            HasAirDash = true;
            AudioManager.Play("sfx_grind_start", 0.8f);
            FX.Sparks(pPoint, UIFactory.YELLOW, 25, 18f);
            Juice.Shake(0.15f);
            Juice.Popup("GRIND!", UIFactory.CYAN, 1f);
            AddCombo();
        }

        private void AttachZip(GrindRail pRail, float pDistance, int pDirection)
        {
            _rail = pRail;
            _railDistance = pDistance;
            _railDirection = pDirection;
            _railBlend = 0.1f;
            _railFrom = transform.position;
            _speed = Mathf.Max(Speed, 40f);
            SetState(PlayerState.Zip);
            Diving = false;
            HasDoubleJump = true;
            HasAirDash = true;
            AudioManager.Play("sfx_zip_start", 0.9f);
            Juice.Shake(0.2f);
            Juice.Popup("ZIP LINE!", UIFactory.CYAN, 1f);
            AddCombo();
        }

        private void GrindStep(float pDt)
        {
            bool lZip = State == PlayerState.Zip;
            Vector3 lPos, lTangent, lUp;
            _rail.Evaluate(_railDistance, out lPos, out lTangent, out lUp);
            float lDownhill = Vector3.Dot(lTangent * _railDirection, Vector3.down);
            _speed = Mathf.Min(MAX_SPEED, _speed + (lZip ? 5f + lDownhill * 45f : 8f) * pDt);
            if (lZip) _speed = Mathf.Max(_speed, 40f);
            _railDistance += _railDirection * _speed * pDt;
            _rail.Evaluate(_railDistance, out lPos, out lTangent, out lUp);
            _up = lZip ? Vector3.up : lUp;
            _gravityUp = _up;
            _heading = lTangent * _railDirection;
            _moveDir = _heading;
            _velocity = _heading * _speed;
            Vector3 lTarget = lZip ? lPos - Vector3.up * 2.15f : lPos + lUp * 0.25f;
            if (_railBlend > 0f)
            {
                _railBlend -= pDt;
                lTarget = Vector3.Lerp(lTarget, _railFrom, Mathf.Clamp01(_railBlend / 0.1f));
            }
            transform.position = lTarget;
            if (lZip) AudioManager.SetLoop("sfx_zip_loop", 0.6f, 0.8f + _speed * 0.005f);
            else AudioManager.SetLoop("sfx_grind_loop", 0.55f, 0.9f + _speed * 0.004f);
            if (Random.value < 0.6f) FX.Sparks(lPos, Color.Lerp(UIFactory.YELLOW, Color.white, Random.value), 2, 12f, 0.3f, 0.25f);

            // Buried rail end : a deck right under the feet takes over without any air time.
            if (!lZip && _railDistance > 2f && _railDistance < _rail.Length - 2f)
            {
                RaycastHit lHit;
                if (_rail.SpineS(_railDistance) > _rail.EndS - 10f && Physics.Raycast(transform.position + _up * 0.3f, -_up, out lHit, 0.7f, GroundMask, QueryTriggerInteraction.Ignore)
                    && Vector3.Dot(lHit.normal, Vector3.up) > 0.6f && DeckChunk.DeckOf(lHit.collider) != null)
                {
                    LeaveRail(false);
                    _velocity = _heading * (_speed + 5f);
                    Juice.Popup("RAIL BOOST", UIFactory.CYAN, 1f);
                    AudioManager.Play("sfx_boost_small", 0.7f);
                    Land(lHit.normal, lHit.point, DeckChunk.DeckOf(lHit.collider));
                    return;
                }
            }
            if (_railDistance <= 0f || _railDistance >= _rail.Length)
            {
                _velocity = _heading * (_speed + 5f) + _up * (lZip ? 2f : 7f);
                LeaveRail(true);
                Juice.Popup("RAIL BOOST", UIFactory.CYAN, 1f);
                AudioManager.Play("sfx_boost_small", 0.7f);
            }
        }

        private void LeaveRail(bool pToAir)
        {
            _railCooldown = 0.4f;
            _takeoffY = transform.position.y;
            _takeoffEdge = EdgeKind.Lip;
            if (pToAir) SetState(PlayerState.Air);
        }
    }
}
