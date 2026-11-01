using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Slow car driving along the track : dodge it, punch it or plow through at hyper speed.
    public class TrafficCar : MonoBehaviour
    {
        public float distance;
        public float lateral;
        public float speed = 22f;

        private Breakable _breakable;

        private void Start()
        {
            _breakable = GetComponent<Breakable>();
        }

        private void Update()
        {
            if (_breakable != null && _breakable.IsBroken) return;
            if (RaceContext.Path == null) return;
            if (RaceContext.Running) distance += speed * Time.deltaTime;
            if (distance >= RaceContext.Path.finishDistance - 20f) return;
            TrackSample lSample = RaceContext.Path.Evaluate(distance);
            if (!lSample.solid) return;
            transform.SetPositionAndRotation(lSample.position + lSample.Right * lateral, Quaternion.LookRotation(lSample.Forward, lSample.Up));
        }
    }
}
