using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Electric ropes : they catch you only if you actually reach them, you ride with growing speed, get thrown off the far
    // end with a boost.
    public partial class PlayerController
    {
        private GrindRail _rail;
        private float _railDistance;
        private int _railDirection;
        private float _railCooldown;
        private float _railBlend;
        private Vector3 _railFrom;

        public GrindRail CurrentRail => State == PlayerState.Grind ? _rail : null;

        private bool TryGrind()
        {
            if (_railCooldown > 0f) return false;
            foreach (GrindRail lRail in GrindRail.All)
            {
                if (_layout != null && (Progress < lRail.StartS - lRail.catchRadius || Progress > lRail.EndS - 10f)) continue;
                float lDistance, lRailDistance;
                if (!lRail.FindNearest(transform.position + _gravityUp, lRail.catchRadius, out lDistance, out lRailDistance)) continue;
                Vector3 lPos, lTangent, lUp;
                lRail.Evaluate(lRailDistance, out lPos, out lTangent, out lUp);
                Vector3 lVelocity = OnSurface ? _moveDir * _speed : _velocity;
                float lAlong = Vector3.Dot(lVelocity, lTangent);
                if (lAlong < 8f) continue;
                AttachRail(lRail, lRailDistance, lPos, lUp);
                return true;
            }
            return false;
        }

        private void AttachRail(GrindRail pRail, float pDistance, Vector3 pPoint, Vector3 pUp)
        {
            _rail = pRail;
            _railDistance = pDistance;
            _railDirection = 1;
            _railBlend = 0.1f;
            _railFrom = transform.position;
            _speed = Mathf.Min(SpeedProfile.SAFETY_MAX, Mathf.Max(Speed, _profile.cruise * 0.85f) + _profile.cruise * 0.12f);
            SetState(PlayerState.Grind);
            Diving = false;
            HasDoubleJump = true;
            HSJuice.Play(JuiceId.RopeCatch, Jx(pPoint));
            AddCombo();
        }

        private void GrindStep(float pDt)
        {
            Vector3 lPos, lTangent, lUp;
            _speed = Mathf.Min(SpeedProfile.SAFETY_MAX, _speed + _profile.cruise * 0.14f * pDt);
            _railDistance += _railDirection * _speed * pDt;
            _rail.Evaluate(_railDistance, out lPos, out lTangent, out lUp);
            _up = lUp;
            _gravityUp = _up;
            _heading = lTangent * _railDirection;
            _moveDir = _heading;
            _velocity = _heading * _speed;
            // Riding on top of the rope, not inside it.
            Vector3 lTarget = lPos + lUp * 1.35f;
            if (_railBlend > 0f)
            {
                _railBlend -= pDt;
                lTarget = Vector3.Lerp(lTarget, _railFrom, Mathf.Clamp01(_railBlend / 0.1f));
            }
            transform.position = lTarget;
            AudioManager.SetLoop("sfx_grind_loop", 0.55f, 0.9f + Mathf.Min(_speed * 0.002f, 0.8f));
            if (Random.value < 0.6f) FX.Sparks(lPos, Color.Lerp(UIFactory.CYAN, Color.white, Random.value), 2, 18f, 0.5f, 0.25f);

            if (_railDistance >= _rail.Length - 1f)
            {
                _speed = Mathf.Min(SpeedProfile.SAFETY_MAX, _speed + _profile.cruise * 0.2f);
                _velocity = _heading * _speed + _up * 14f;
                LeaveRail();
                _boostTimer = Mathf.Max(_boostTimer, 0.8f);
                HSJuice.Play(JuiceId.RopeExit, Jx(transform.position));
            }
        }

        private void LeaveRail()
        {
            _railCooldown = 0.5f;
            SetState(PlayerState.Air);
        }
    }
}
