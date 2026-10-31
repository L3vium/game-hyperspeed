using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Obi.Samples
{
    public class ToyTool : MonoBehaviour
    {
        [TextArea]
        public string tooltipText;
        public virtual void OnDeselect() { }
        public virtual void OnSelect() { }
        public virtual void OnMouseDown(Vector3 pos) { }
        public virtual void OnMousePressed(Vector3 pos, Vector3 prevPos) { }
        public virtual void OnMouseUp(Vector3 pos) { }
    }
}
