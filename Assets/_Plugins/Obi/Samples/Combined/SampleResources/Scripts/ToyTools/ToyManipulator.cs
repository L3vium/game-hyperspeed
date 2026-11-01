using UnityEngine;

namespace Obi.Samples
{
    public class ToyManipulator : ToyTool
    {
        private ObiForceZone forceZone;
        public float intensity = 50;
        public float damping = 8;
        public bool intensityFromSpeed;

        public void Awake()
        {
            forceZone = GetComponent<ObiForceZone>();
        }

        public override void OnDeselect()
        {
            if (forceZone != null)
                forceZone.gameObject.SetActive(false);
        }

        public override void OnMousePressed(Vector3 pos, Vector3 prevPos)
        {
            if (forceZone != null)
            {
                Vector3 dir = pos - prevPos;
                float magnitude = Vector3.Magnitude(dir);

                forceZone.gameObject.SetActive(true);
                forceZone.transform.position = pos;

                if (magnitude > 0.001f)
                    forceZone.transform.rotation = Quaternion.LookRotation(Vector3.Normalize(dir), Vector3.forward);

                forceZone.damping = damping;
                forceZone.intensity = intensityFromSpeed ? magnitude * intensity : intensity;
            }
        }

        public override void OnMouseUp(Vector3 pos)
        {
            if (forceZone != null)
                forceZone.gameObject.SetActive(false);
        }
    }
}
