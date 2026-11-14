using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // What an event knows when it plays : where, which way, how hard, how big, which colour, which word.
    public struct JuiceContext
    {
        public Vector3 position;
        public Vector3 direction;
        public Vector3 normal;
        public float intensity;
        public float size;
        public Color color;
        public bool hasColor;
        public string label;

        public static JuiceContext At(Vector3 pPosition, float pIntensity = 1f)
        {
            return new JuiceContext { position = pPosition, intensity = pIntensity, direction = Vector3.forward, normal = Vector3.up, size = 1f };
        }
    }

    // The one door to every juice event : HSJuice.Play(JuiceId.Dash, position). Each event is a named GameObject with
    // an MMF_Player in the HS_Juice prefab (Assets/_Game/Resources/Juice), tuned in the inspector, never in code.
    public static class HSJuice
    {
        public const string RESOURCE = "Juice/HS_Juice";

        // Global multiplier on every event (1 = as tuned).
        public static float Master = 1f;
        // The context of the event being played : the Hyperspeed feedbacks read it when they start.
        public static JuiceContext Current;

        private static HSJuiceBank _bank;
        private static bool _missingLogged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _bank = null;
            _missingLogged = false;
            Master = 1f;
            Current = default;
        }

        public static HSJuiceBank Bank
        {
            get
            {
                if (_bank != null) return _bank;
                _bank = Object.FindAnyObjectByType<HSJuiceBank>();
                if (_bank != null) return _bank;
                GameObject lPrefab = Resources.Load<GameObject>(RESOURCE);
                if (lPrefab == null)
                {
                    if (!_missingLogged) Debug.LogError("[HS-JUICE] Resources/" + RESOURCE + " is missing : run Hyperspeed > Juice > Create or Update Juice Bank.");
                    _missingLogged = true;
                    return null;
                }
                GameObject lInstance = Object.Instantiate(lPrefab);
                lInstance.name = "HS_Juice";
                _bank = lInstance.GetComponent<HSJuiceBank>();
                return _bank;
            }
        }

        // Builds the bank ahead of time (title screen, stage load) so the first event never hitches.
        public static void Warmup()
        {
            HSJuiceBank lBank = Bank;
            if (lBank != null) lBank.Prepare();
        }

        public static void Play(JuiceId pId, Vector3 pPosition, float pIntensity = 1f) => Play(pId, JuiceContext.At(pPosition, pIntensity));

        public static void Play(JuiceId pId, JuiceContext pContext)
        {
            HSJuiceBank lBank = Bank;
            if (lBank == null) return;
            HSJuiceEvent lEvent = lBank.Get(pId);
            if (lEvent == null) return;
            lEvent.Play(pContext, Master);
        }

        public static void Stop(JuiceId pId)
        {
            HSJuiceBank lBank = Bank;
            if (lBank == null) return;
            HSJuiceEvent lEvent = lBank.Get(pId);
            if (lEvent != null) lEvent.Stop();
        }
    }
}
