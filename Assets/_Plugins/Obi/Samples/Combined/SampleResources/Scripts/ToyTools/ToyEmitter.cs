using UnityEngine;

namespace Obi.Samples
{
    public class ToyEmitter : ToyTool
    {
        protected ObiEmitter emitter;
        public Color hotColor = Color.red;
        public float emissionSpeed = 1;

        public void Awake()
        {
            emitter = GetComponent<ObiEmitter>();
        }

        public override void OnDeselect()
        {
            if (emitter != null)
            {
                emitter.speed = 0;
            }
        }

        public override void OnMousePressed(Vector3 pos, Vector3 prevPos)
        {
            if (emitter != null)
            {
                emitter.speed = emissionSpeed;
                emitter.transform.position = pos;
            }
        }

        public override void OnMouseUp(Vector3 pos)
        {
            if (emitter != null)
            {
                emitter.speed = 0;
            }
        }
    }
}
