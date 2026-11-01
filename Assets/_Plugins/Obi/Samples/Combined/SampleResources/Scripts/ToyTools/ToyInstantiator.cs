using UnityEngine;

namespace Obi.Samples
{
    public class ToyInstantiator : ToyTool
    {
        public GameObject prefab;

        public override void OnMouseDown(Vector3 pos)
        {
            if (prefab != null)
            {
                Instantiate(prefab, pos, prefab.transform.rotation, transform.parent);
            }
        }
    }
}
