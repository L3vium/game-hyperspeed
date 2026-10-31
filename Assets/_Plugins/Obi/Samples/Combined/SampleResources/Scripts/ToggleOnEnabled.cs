using UnityEngine;
using UnityEngine.Events;

namespace Obi.Samples
{
    public class ToggleOnEnabled : MonoBehaviour
    {
        public UnityEvent onEnabled;

        void Awake()
        {
            GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(onValueChanged);
        }

        // Update is called once per frame
        public void onValueChanged(bool isOn)
        {
            if (isOn)
                onEnabled.Invoke();
        }
    }
}
