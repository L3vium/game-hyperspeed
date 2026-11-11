using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Base class for things the player touches (pads, rings...). Tested in local space by the player
    // every movement substep instead of physics triggers, so nothing is skipped at hyper speed.
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public Vector3 halfExtents = new Vector3(2.5f, 2f, 3f);
        public float cooldown = 0.6f;

        private float _lastTrigger = -10f;

        protected virtual void OnEnable() => All.Add(this);

        protected virtual void OnDisable() => All.Remove(this);

        public bool Test(Vector3 pPosition)
        {
            if (Time.time - _lastTrigger < cooldown) return false;
            Vector3 lLocal = transform.InverseTransformPoint(pPosition);
            Vector3 lScale = transform.lossyScale;
            lLocal.Scale(lScale);
            return Mathf.Abs(lLocal.x) <= halfExtents.x && Mathf.Abs(lLocal.y) <= halfExtents.y && Mathf.Abs(lLocal.z) <= halfExtents.z;
        }

        public void Touch(PlayerController pPlayer)
        {
            _lastTrigger = Time.time;
            OnTouch(pPlayer);
        }

        protected abstract void OnTouch(PlayerController pPlayer);

        // Pads and strips only work the way they point : running over one backward does nothing.
        public virtual bool Accepts(PlayerController pPlayer) => true;

        protected bool Along(PlayerController pPlayer)
        {
            Vector3 lMotion = pPlayer.OnGroundVelocity;
            if (lMotion.sqrMagnitude < 1f) lMotion = pPlayer.Heading;
            return Vector3.Dot(lMotion.normalized, transform.forward) > 0.3f;
        }
    }
}
