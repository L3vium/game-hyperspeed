using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Com.IsartDigital.Hyperspeed
{
    // Root of the HS_Juice prefab : finds its events (by id, wherever they sit in the hierarchy) and lives across scenes.
    public class HSJuiceBank : MonoBehaviour
    {
        private readonly Dictionary<JuiceId, HSJuiceEvent> _events = new Dictionary<JuiceId, HSJuiceEvent>();
        private bool _prepared;

        private void Awake()
        {
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
            Prepare();
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        public void Prepare()
        {
            if (_prepared) return;
            _prepared = true;
            _events.Clear();
            foreach (HSJuiceEvent lEvent in GetComponentsInChildren<HSJuiceEvent>(true))
            {
                if (_events.ContainsKey(lEvent.id)) Debug.LogWarning("[HS-JUICE] two events with id " + lEvent.id + " : " + lEvent.name);
                else _events.Add(lEvent.id, lEvent);
            }
        }

        public HSJuiceEvent Get(JuiceId pId)
        {
            Prepare();
            HSJuiceEvent lEvent;
            return _events.TryGetValue(pId, out lEvent) ? lEvent : null;
        }

        // Nothing started in a stage keeps running into the next one.
        private void OnSceneUnloaded(Scene pScene)
        {
            foreach (HSJuiceEvent lEvent in _events.Values) if (lEvent != null) lEvent.Stop();
        }
    }
}
