using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum WorldBreakKind { Meteors, Collapse }

    public class WorldBreakZone
    {
        public WorldBreakKind kind;
        public float s0;
        public float s1;
        public float value;
    }

    // "Everything breaks around you" : platforms crumble behind the runner (a wave that can catch up),
    // meteors rain on the route with telegraphed impacts, far meteors streak across the sky all race long.
    // Everything is indexed on the spine progress and can be rewound when the player respawns.
    public class BreakDirector : MonoBehaviour
    {
        private const float WAVE_MAX_BEHIND = 110f;
        private const float WAVE_START_BEHIND = 80f;
        private const float METEOR_HEIGHT = 110f;
        private const float METEOR_SPEED = 95f;
        private const float WARNING = 1.2f;

        private class Tile
        {
            public DeckChunk chunk;
            public Vector3 pivot;
            public Vector3 offset;
            public Vector3 velocity;
            public Vector3 spinAxis;
            public float spin;
            public float angle;
            public float timer;
            public bool falling;
            public bool gone;
        }

        private class Meteor
        {
            public Transform body;
            public Transform warning;
            public Vector3 target;
            public Vector3 direction;
            public float timeToImpact;
            public bool active;
        }

        public static BreakDirector Instance { get; private set; }

        public readonly List<Vector2> CrumbleRanges = new List<Vector2>();

        private readonly List<Tile> _tiles = new List<Tile>();
        private readonly List<Tile> _falling = new List<Tile>();
        private readonly List<WorldBreakZone> _zones = new List<WorldBreakZone>();
        private readonly List<Meteor> _meteors = new List<Meteor>();
        private LevelLayout _layout;
        private Theme _theme;
        private float _wave = float.MinValue;
        private float _meteorTimer;
        private float _skyTimer;
        private float _rumble;
        private Material _meteorMaterial;
        private Material _warningMaterial;
        private Material _trailMaterial;

        public float WaveS => _wave;
        public bool WaveActive { get; private set; }

        public static BreakDirector Create(LevelLayout pLayout, IEnumerable<WorldBreakZone> pZones, Transform pRoot, Theme pTheme)
        {
            BreakDirector lDirector = new GameObject("BreakDirector").AddComponent<BreakDirector>();
            lDirector.transform.SetParent(pRoot, false);
            lDirector._layout = pLayout;
            lDirector._theme = pTheme;
            foreach (WorldBreakZone lZone in pZones)
            {
                lDirector._zones.Add(lZone);
                if (lZone.kind == WorldBreakKind.Collapse) lDirector.CrumbleRanges.Add(new Vector2(lZone.s0, lZone.s1));
            }
            Instance = lDirector;
            return lDirector;
        }

        public void AddTile(DeckChunk pChunk)
        {
            Bounds lBounds = pChunk.GetComponent<MeshFilter>().sharedMesh.bounds;
            _tiles.Add(new Tile { chunk = pChunk, pivot = lBounds.center });
        }

        private void Start()
        {
            _tiles.Sort((pA, pB) => pA.chunk.s0.CompareTo(pB.chunk.s0));
            _meteorMaterial = Mats.Toon(new Color(0.3f, 0.18f, 0.2f), _theme.shade, 0.03f, 0.6f);
            _warningMaterial = Mats.Glow(new Color(3f, 0.4f, 0.2f, 0.85f), true);
            _trailMaterial = Mats.Glow(new Color(3f, 1.2f, 0.3f), true);
            for (int i = 0; i < 10; i++) _meteors.Add(CreateMeteor());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            float lDt = Time.deltaTime;
            AnimateFalling(lDt);
            UpdateMeteors(lDt);
            SkyMeteors(lDt);
            if (!RaceContext.Running || RaceContext.Player == null) return;
            float lS = RaceContext.PlayerProgress;
            UpdateWave(lS, lDt);
            SpawnRouteMeteors(lS, lDt);
        }

        // ---------------------------------------------------------------- collapse

        private WorldBreakZone ZoneAt(float pS, WorldBreakKind pKind)
        {
            foreach (WorldBreakZone lZone in _zones)
                if (lZone.kind == pKind && pS >= lZone.s0 - 40f && pS <= lZone.s1 + 160f) return lZone;
            return null;
        }

        private void UpdateWave(float pS, float pDt)
        {
            WorldBreakZone lZone = ZoneAt(pS, WorldBreakKind.Collapse);
            WaveActive = lZone != null;
            if (lZone == null)
            {
                _rumble = Mathf.MoveTowards(_rumble, 0f, pDt);
                AudioManager.SetLoop("sfx_rumble_loop", 0f);
                return;
            }
            if (_wave < pS - WAVE_MAX_BEHIND - 50f) _wave = pS - WAVE_START_BEHIND;
            _wave = Mathf.Max(_wave + lZone.value * pDt, pS - WAVE_MAX_BEHIND);
            for (int i = 0; i < _tiles.Count; i++)
            {
                Tile lTile = _tiles[i];
                if (lTile.chunk.s0 > _wave) break;
                if (lTile.falling || lTile.gone || lTile.chunk.s1 > _wave) continue;
                Drop(lTile, 0f);
            }
            float lBehind = pS - _wave;
            _rumble = Mathf.Clamp01(1f - lBehind / 90f);
            AudioManager.SetLoop("sfx_rumble_loop", 0.25f + _rumble * 0.5f, 0.8f + _rumble * 0.3f);
            if (_rumble > 0.4f) Juice.Shake(_rumble * pDt * 0.9f);
        }

        // Disables the collider first (moving colliders are not auto-synced), then lets the tile fall and tumble.
        private void Drop(Tile pTile, float pDelay)
        {
            pTile.falling = true;
            pTile.timer = -pDelay;
            pTile.velocity = Vector3.down * Random.Range(1f, 5f) + Random.insideUnitSphere * 2f;
            pTile.spinAxis = Random.onUnitSphere;
            pTile.spin = Random.Range(10f, 40f);
            pTile.angle = 0f;
            pTile.offset = Vector3.zero;
            if (pTile.chunk.body != null) pTile.chunk.body.enabled = false;
            _falling.Add(pTile);
            if (Random.value < 0.35f) FX.Puff(pTile.pivot, Color.Lerp(_theme.roadSide, Color.white, 0.4f), 6, 5f, 10f);
        }

        private void AnimateFalling(float pDt)
        {
            for (int i = _falling.Count - 1; i >= 0; i--)
            {
                Tile lTile = _falling[i];
                lTile.timer += pDt;
                if (lTile.timer < 0f) continue;
                lTile.velocity += Vector3.down * 30f * pDt;
                lTile.offset += lTile.velocity * pDt;
                lTile.angle += lTile.spin * pDt;
                Quaternion lRotation = Quaternion.AngleAxis(lTile.angle, lTile.spinAxis);
                Transform lTransform = lTile.chunk.transform;
                lTransform.SetPositionAndRotation(lTile.pivot + lRotation * -lTile.pivot + lTile.offset, lRotation);
                if (lTile.timer > 3f)
                {
                    lTile.gone = true;
                    lTile.chunk.gameObject.SetActive(false);
                    _falling.RemoveAt(i);
                }
            }
        }

        // Respawn rewinds the world : every tile ahead of the respawn point is back, the wave restarts behind it.
        public void RewindTo(float pS)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                Tile lTile = _tiles[i];
                if (lTile.chunk.s1 < pS - 30f) continue;
                if (!lTile.falling && !lTile.gone) continue;
                lTile.falling = false;
                lTile.gone = false;
                lTile.chunk.gameObject.SetActive(true);
                lTile.chunk.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                if (lTile.chunk.body != null) lTile.chunk.body.enabled = true;
                _falling.Remove(lTile);
            }
            _wave = pS - WAVE_START_BEHIND - 40f;
            foreach (Meteor lMeteor in _meteors) Hide(lMeteor);
        }

        // Ahead-of-time crack : a tile shakes, glows, then drops (never the full width, always telegraphed).
        public void CrackAhead(float pS)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                Tile lTile = _tiles[i];
                if (lTile.falling || lTile.gone) continue;
                if (pS < lTile.chunk.s0 || pS > lTile.chunk.s1) continue;
                if (lTile.chunk.deck.route) continue;
                Drop(lTile, 0.9f);
            }
        }

        // ---------------------------------------------------------------- meteors

        private Meteor CreateMeteor()
        {
            Meteor lMeteor = new Meteor();
            GameObject lBody = new GameObject("Meteor");
            lBody.transform.SetParent(transform, false);
            lBody.AddComponent<MeshFilter>().sharedMesh = RockSculptor.Boulder(Random.Range(0, 6));
            lBody.AddComponent<MeshRenderer>().sharedMaterial = _meteorMaterial;
            lBody.transform.localScale = Vector3.one * 5f;
            TrailRenderer lTrail = lBody.AddComponent<TrailRenderer>();
            lTrail.time = 0.6f;
            lTrail.startWidth = 4.5f;
            lTrail.endWidth = 0f;
            lTrail.sharedMaterial = _trailMaterial;
            lTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lMeteor.body = lBody.transform;
            GameObject lWarning = new GameObject("MeteorWarning");
            lWarning.transform.SetParent(transform, false);
            lWarning.AddComponent<MeshFilter>().sharedMesh = MeshUtil.Ring(3.5f, 5f, 32);
            lWarning.AddComponent<MeshRenderer>().sharedMaterial = _warningMaterial;
            lMeteor.warning = lWarning.transform;
            Hide(lMeteor);
            return lMeteor;
        }

        private void Hide(Meteor pMeteor)
        {
            pMeteor.active = false;
            pMeteor.body.gameObject.SetActive(false);
            pMeteor.warning.gameObject.SetActive(false);
        }

        private void SpawnRouteMeteors(float pS, float pDt)
        {
            WorldBreakZone lZone = ZoneAt(pS, WorldBreakKind.Meteors);
            if (lZone == null || pS > lZone.s1) return;
            _meteorTimer -= pDt;
            if (_meteorTimer > 0f) return;
            _meteorTimer = lZone.value * Random.Range(0.7f, 1.3f);
            float lSpeed = Mathf.Max(40f, RaceContext.Player.Speed);
            float lTargetS = pS + lSpeed * WARNING + Random.Range(20f, 60f);
            Deck lDeck = _layout.MainDeckAt(lTargetS);
            if (lDeck == null || lDeck.surface == DeckSurface.Membrane) return;
            float lLateral = Random.Range(-0.35f, 0.35f) * lDeck.Width(lTargetS);
            Vector3 lTarget = lDeck.SurfacePoint(lTargetS, lLateral);
            foreach (GrindRail lRail in _layout.rails)
            {
                float lDistance, lRailDistance;
                if (lRail.FindNearest(lTarget, 6f, out lDistance, out lRailDistance)) return;
            }
            Meteor lMeteor = _meteors.Find(pM => !pM.active);
            if (lMeteor == null) return;
            lMeteor.active = true;
            lMeteor.target = lTarget;
            lMeteor.direction = (Vector3.down + _layout.spine.Forward(lTargetS) * 0.35f + Random.insideUnitSphere * 0.15f).normalized;
            lMeteor.timeToImpact = METEOR_HEIGHT / METEOR_SPEED + 0.05f;
            lMeteor.body.gameObject.SetActive(true);
            lMeteor.body.position = lTarget - lMeteor.direction * METEOR_HEIGHT;
            lMeteor.body.GetComponent<TrailRenderer>().Clear();
            lMeteor.warning.gameObject.SetActive(true);
            lMeteor.warning.position = lTarget + lDeck.SurfaceNormal(lTargetS, lLateral) * 0.15f;
            lMeteor.warning.rotation = Quaternion.LookRotation(lDeck.SurfaceNormal(lTargetS, lLateral));
            AudioManager.Play("sfx_meteor_warn", 0.5f);
        }

        private void UpdateMeteors(float pDt)
        {
            foreach (Meteor lMeteor in _meteors)
            {
                if (!lMeteor.active) continue;
                lMeteor.timeToImpact -= pDt;
                lMeteor.body.position = lMeteor.target - lMeteor.direction * Mathf.Max(0f, lMeteor.timeToImpact) * METEOR_SPEED;
                lMeteor.body.Rotate(Vector3.one * 200f * pDt, Space.Self);
                float lPulse = 0.6f + Mathf.PingPong(Time.time * 8f, 0.6f);
                lMeteor.warning.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.6f, Mathf.Clamp01(lMeteor.timeToImpact / WARNING)) * lPulse;
                if (lMeteor.timeToImpact > 0f) continue;
                Impact(lMeteor);
            }
        }

        private void Impact(Meteor pMeteor)
        {
            Vector3 lPoint = pMeteor.target;
            Hide(pMeteor);
            FX.Shockwave(lPoint + Vector3.up * 0.3f, Vector3.up, new Color(1f, 0.55f, 0.2f), 22f);
            FX.Sparks(lPoint, new Color(1f, 0.6f, 0.2f), 40, 40f, 0.6f, 0.6f);
            FX.Shards(lPoint, _theme.roadSide, 18, 24f, 1.6f, Vector3.up * 8f);
            FX.Puff(lPoint, new Color(0.4f, 0.3f, 0.3f), 10, 6f, 14f);
            AudioManager.Play("sfx_explosion", 0.9f);
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null) return;
            float lDistance = Vector3.Distance(lPlayer.transform.position, lPoint);
            if (lDistance < 60f) Juice.Shake(Mathf.Lerp(0.5f, 0.1f, lDistance / 60f));
            if (lDistance < 4.5f) lPlayer.Crash(lPoint, "BONK!");
        }

        // Far meteors streaking across the sky : pure atmosphere, the world is ending.
        private void SkyMeteors(float pDt)
        {
            _skyTimer -= pDt;
            if (_skyTimer > 0f || CameraRig.Instance == null) return;
            _skyTimer = Random.Range(0.8f, 2.4f);
            Transform lCamera = CameraRig.Instance.transform;
            Vector3 lForward = Vector3.ProjectOnPlane(lCamera.forward, Vector3.up).normalized;
            Vector3 lRight = Vector3.Cross(Vector3.up, lForward);
            Vector3 lStart = lCamera.position + lForward * Random.Range(250f, 700f) + lRight * Random.Range(-500f, 500f) + Vector3.up * Random.Range(150f, 380f);
            Vector3 lDirection = (Vector3.down * Random.Range(0.6f, 1f) + lRight * Random.Range(-0.8f, 0.8f) - lForward * Random.Range(0f, 0.4f)).normalized;
            FX.Streak(lStart, lDirection, Random.Range(120f, 220f), Random.Range(1.2f, 2.2f), new Color(3f, 1.4f, 0.5f));
        }
    }
}
