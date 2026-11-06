using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Giant obstacle the player bursts with a dash or a punch, or crashes into.
    public class Breakable : MonoBehaviour
    {
        public static readonly List<Breakable> All = new List<Breakable>();

        public float radius = 16f;
        public Color color = Color.white;
        // Worth the big reaction (impact frame) when it bursts.
        public bool big;

        public bool IsBroken { get; private set; }

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        public bool Overlaps(Vector3 pPosition, float pRadius)
        {
            if (IsBroken) return false;
            float lReach = radius + pRadius;
            return (pPosition - transform.position).sqrMagnitude < lReach * lReach;
        }

        public void Smash(Vector3 pDirection, float pPlayerSpeed)
        {
            if (IsBroken) return;
            IsBroken = true;
            All.Remove(this);
            Fracture.Explode(transform.position, radius, color, pDirection, pPlayerSpeed);
            Destroy(gameObject);
        }

        protected virtual void Update()
        {
        }
    }
}
