using MoreMountains.Feedbacks;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // One juice event (a child of HS_Juice) : its MMF_Player holds every feedback, tune them in the inspector.
    // To silence an event, untick "Can Play" on its MMF_Player rather than deleting it (the builder would add it back).
    [RequireComponent(typeof(MMF_Player))]
    public class HSJuiceEvent : MonoBehaviour
    {
        public JuiceId id;
        [TextArea(2, 6)] public string notes;
        [Tooltip("Plays even while the game is paused (menus).")]
        public bool playWhilePaused;
        [Tooltip("Minimum time between two plays of this event (unscaled seconds).")]
        public float minInterval;
        [Tooltip("Scales every feedback of this event (on top of each one's own Amount).")]
        public float intensityScale = 1f;

        public JuiceContext Last { get; private set; }

        private MMF_Player _player;
        private float _lastPlay = -100f;

        public MMF_Player Player
        {
            get
            {
                if (_player == null) _player = GetComponent<MMF_Player>();
                return _player;
            }
        }

        public void Play(JuiceContext pContext, float pMaster)
        {
            if (Juice.Paused && !playWhilePaused) return;
            float lNow = Time.unscaledTime;
            if (lNow - _lastPlay < minInterval) return;
            _lastPlay = lNow;
            Last = pContext;
            HSJuice.Current = pContext;
            MMF_Player lPlayer = Player;
            if (lPlayer == null || !lPlayer.CanPlay) return;
            lPlayer.PlayFeedbacks(pContext.position, Mathf.Max(0f, pContext.intensity) * intensityScale * pMaster);
        }

        public void Stop()
        {
            if (Player != null) Player.StopFeedbacks();
        }
    }
}
