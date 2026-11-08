using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // The world never holds still. Around the runner, all the time : meteorites crossing the sky (small ones and immense
    // ones, never fewer than a handful in view), some of them crashing into the mountains ; scenery on the crests that
    // sways ahead and topples and bursts once you are past ; things falling onto the track ahead (cars, trees, rocks,
    // castle stones, lava bombs) that stay there to dash through ; platforms that give way behind you.
    public class ChaosDirector : MonoBehaviour
    {
        private const int SKY_ROCKS = 10;
        private const int IMMENSE = 3;
        private const int MAX_TOPPLERS = 36;
        private const float TOPPLE_AHEAD = 3200f;

        public static ChaosDirector Instance { get; private set; }

        private class SkyRock
        {
            public Transform transform;
            public TrailRenderer fire;
            public TrailRenderer smoke;
            public Vector3 velocity;
            public Vector3 spin;
            public float radius;
            public float life;
            public bool immense;
            public bool strike;
            public float check;
        }

        private class Toppler
        {
            public Transform transform;
            public float s;
            public float size;
            public float fall = -1f;
            public Vector3 axis;
            public Quaternion rest;
            public float phase;
            public Color color;
            public Vector3 outward;
        }

        private class Sinking
        {
            public TrackChunk chunk;
            public Vector3 home;
            public Quaternion homeRotation;
            public float time;
            public Vector3 spin;
        }

        private readonly List<SkyRock> _sky = new List<SkyRock>();
        private readonly List<Toppler> _topplers = new List<Toppler>();
        private readonly List<Sinking> _sinking = new List<Sinking>();
        private LevelLayout _layout;
        private Theme _theme;
        private bool _items;
        private System.Random _random;
        private Material _rockMaterial;
        private Material _lavaMaterial;
        private float _nextToppleS;
        private float _debrisTimer = 2f;
        private float _aimedTimer = 6f;
        private float _crumbleTimer;
        private int _nextSlab;

        public static ChaosDirector Create(Transform pParent, LevelLayout pLayout, Theme pTheme, bool pItems, int pSeed)
        {
            ChaosDirector lDirector = new GameObject("Chaos").AddComponent<ChaosDirector>();
            lDirector.transform.SetParent(pParent, false);
            lDirector._layout = pLayout;
            lDirector._theme = pTheme;
            lDirector._items = pItems;
            lDirector._random = new System.Random(pSeed);
            lDirector._rockMaterial = Mats.Flat(pTheme.shade, false, Color.Lerp(pTheme.decorC, new Color(0.18f, 0.1f, 0.12f), 0.6f));
            lDirector._lavaMaterial = Mats.Glow(new Color(3.2f, 1.1f, 0.15f), false, 0f, 0f, true);
            lDirector._nextToppleS = pLayout.startLineS;
            Instance = lDirector;
            return lDirector;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private float R(float pMin, float pMax) => pMin + (float)_random.NextDouble() * (pMax - pMin);

        private void Update()
        {
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null || _layout == null) return;
            float lDt = Time.deltaTime;
            if (lDt <= 0f) return;
            UpdateSky(lPlayer, lDt);
            UpdateTopplers(lPlayer, lDt);
            if (!RaceContext.Running) return;
            UpdateSlabs(lPlayer, lDt);
            UpdateCrumble(lPlayer, lDt);
            if (_items) UpdateFalling(lPlayer, lDt);
        }

        // ================================================================ frame

        private Vector3 FlatForward(float pS)
        {
            Vector3 lFlat = Vector3.ProjectOnPlane(_layout.Flow(pS), Vector3.up);
            return lFlat.sqrMagnitude > 0.01f ? lFlat.normalized : Vector3.forward;
        }

        // Top of the mountain side at s, on a side (-1 / 1). False when that block has no mountain.
        private bool Crest(float pS, int pSide, out Vector3 pPoint, out Vector3 pOutward, out float pHeight)
        {
            pPoint = Vector3.zero;
            pOutward = Vector3.zero;
            pHeight = 0f;
            TrackBlock lBlock = _layout.field.BlockAt(pS);
            if (lBlock == null || !lBlock.mountain) return false;
            SpineSample lSample = _layout.spine.Evaluate(pS);
            float lWall = pSide < 0 ? lBlock.wallLeft : lBlock.wallRight;
            float lRun = lBlock.WallRun(lWall);
            pOutward = lSample.Right * pSide;
            pHeight = lWall;
            pPoint = lSample.position + lSample.Right * (lBlock.offset + pSide * (lBlock.halfWidth + lRun * 1.05f)) - lSample.up * lBlock.drop + lSample.up * lWall * 0.95f;
            return true;
        }

        // ================================================================ sky

        private void UpdateSky(PlayerController pPlayer, float pDt)
        {
            while (_sky.Count < SKY_ROCKS) _sky.Add(BuildSkyRock());
            Vector3 lAt = pPlayer.transform.position;
            Vector3 lForward = FlatForward(pPlayer.Progress);
            int lImmense = 0;
            foreach (SkyRock lRock in _sky) if (lRock.life > 0f && lRock.immense) lImmense++;
            foreach (SkyRock lRock in _sky)
            {
                if (lRock.life <= 0f)
                {
                    SpawnSkyRock(lRock, pPlayer, lImmense < IMMENSE);
                    if (lRock.immense) lImmense++;
                    continue;
                }
                lRock.life -= pDt;
                lRock.transform.position += lRock.velocity * pDt;
                lRock.transform.Rotate(lRock.spin * pDt, Space.World);
                Vector3 lFrom = lRock.transform.position - lAt;
                // Left behind, or well under the world : it comes back somewhere ahead.
                if (Vector3.Dot(lFrom, lForward) < -900f - lRock.radius * 3f || lFrom.y < -2500f) lRock.life = 0f;
                if (!lRock.strike) continue;
                lRock.check -= pDt;
                if (lRock.check > 0f) continue;
                lRock.check = 0.08f;
                RaycastHit lHit;
                float lSpeed = lRock.velocity.magnitude;
                if (Physics.Raycast(lRock.transform.position, lRock.velocity / lSpeed, out lHit, lSpeed * 0.1f + lRock.radius, (1 << Layers.DECK) | (1 << Layers.SCENERY), QueryTriggerInteraction.Ignore)
                    && lHit.collider.GetComponent<SafetyNet>() == null)
                {
                    Impact(lHit.point, lRock, lAt);
                    lRock.life = 0f;
                }
            }
        }

        private SkyRock BuildSkyRock()
        {
            SkyRock lRock = new SkyRock();
            GameObject lObject = new GameObject("SkyRock");
            lObject.transform.SetParent(transform, false);
            lRock.transform = lObject.transform;
            GameObject lBody = new GameObject("Rock");
            lBody.transform.SetParent(lObject.transform, false);
            lBody.transform.localScale = Vector3.one * 1.9f;
            lBody.AddComponent<MeshFilter>().sharedMesh = RockSculptor.Boulder(_sky.Count % 6);
            lBody.AddComponent<MeshRenderer>().sharedMaterial = _rockMaterial;
            Mats.Primitive(PrimitiveType.Sphere, lObject.transform, Vector3.zero, Vector3.one * 1.7f, _lavaMaterial, "Lava");
            lRock.fire = AddTrail(lObject.transform, Mats.Glow(new Color(3.4f, 1.3f, 0.2f), true, 1f));
            lRock.smoke = AddTrail(lObject.transform, Mats.Glow(new Color(0.16f, 0.1f, 0.12f, 0.5f), false, 1f));
            return lRock;
        }

        private static TrailRenderer AddTrail(Transform pParent, Material pMaterial)
        {
            GameObject lObject = new GameObject("Trail");
            lObject.transform.SetParent(pParent, false);
            TrailRenderer lTrail = lObject.AddComponent<TrailRenderer>();
            lTrail.sharedMaterial = pMaterial;
            lTrail.minVertexDistance = 4f;
            lTrail.numCapVertices = 2;
            lTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lTrail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.15f));
            return lTrail;
        }

        // Somewhere ahead in the sky, falling across the view. Immense ones far away and slow, small ones close and fast.
        private void SpawnSkyRock(SkyRock pRock, PlayerController pPlayer, bool pImmense)
        {
            Vector3 lAt = pPlayer.transform.position;
            Vector3 lForward = FlatForward(pPlayer.Progress);
            Vector3 lRight = Vector3.Cross(Vector3.up, lForward);
            float lAhead = pImmense ? R(2600f, 6500f) : R(500f, 2400f) + pPlayer.Speed * 0.8f;
            float lSide = (R(0f, 1f) < 0.5f ? -1f : 1f) * (pImmense ? R(900f, 4200f) : R(150f, 1700f));
            float lHeight = pImmense ? R(900f, 3200f) : R(250f, 1300f);
            pRock.immense = pImmense;
            pRock.radius = pImmense ? R(130f, 420f) : R(6f, 48f);
            pRock.transform.position = lAt + lForward * lAhead + lRight * lSide + Vector3.up * lHeight;
            pRock.transform.localScale = Vector3.one * pRock.radius;
            pRock.strike = false;
            Vector3 lDrift = (lRight * -Mathf.Sign(lSide) * R(0.2f, 1f) + lForward * R(-0.6f, 0.4f) - Vector3.up * R(0.6f, 1.2f)).normalized;
            pRock.velocity = lDrift * (pImmense ? R(70f, 180f) : R(160f, 420f));
            // A third of the small ones are aimed at the mountains along the track : they hit, the mountain bursts.
            if (!pImmense && R(0f, 1f) < 0.35f)
            {
                float lS = pPlayer.Progress + R(500f, 1600f) + pPlayer.Speed;
                Vector3 lCrest, lOut;
                float lWallHeight;
                if (Crest(lS, lSide < 0f ? -1 : 1, out lCrest, out lOut, out lWallHeight))
                {
                    float lTime = R(1.6f, 3.2f);
                    pRock.transform.position = lCrest + lOut * R(200f, 900f) + Vector3.up * R(500f, 1100f) + lForward * R(-200f, 400f);
                    pRock.velocity = (lCrest - lOut * lWallHeight * 0.3f - pRock.transform.position) / lTime;
                    pRock.strike = true;
                    pRock.radius = R(18f, 55f);
                    pRock.transform.localScale = Vector3.one * pRock.radius;
                }
            }
            pRock.spin = Random.onUnitSphere * (pImmense ? 12f : 120f);
            pRock.life = pImmense ? 40f : 14f;
            pRock.check = 0f;
            foreach (TrailRenderer lTrail in new[] { pRock.fire, pRock.smoke })
            {
                lTrail.Clear();
                lTrail.widthMultiplier = pRock.radius * (lTrail == pRock.fire ? 1.7f : 2.4f);
                lTrail.time = pImmense ? (lTrail == pRock.fire ? 5f : 8f) : (lTrail == pRock.fire ? 1.1f : 2.2f);
            }
        }

        private void Impact(Vector3 pPoint, SkyRock pRock, Vector3 pPlayer)
        {
            Fracture.Explode(pPoint, pRock.radius * 1.6f, _theme.wall, pRock.velocity, pRock.velocity.magnitude, 0.6f);
            FX.Shockwave(pPoint, Vector3.up, new Color(1f, 0.5f, 0.15f), pRock.radius * 7f, 0.6f);
            float lDistance = Vector3.Distance(pPoint, pPlayer);
            if (lDistance < 1400f) Juice.Shake(Mathf.Clamp01(0.45f - lDistance / 3000f));
        }

        // ================================================================ crests

        // Scenery stands on the mountain crests ahead, swaying ; once you are past it topples over and bursts.
        private void UpdateTopplers(PlayerController pPlayer, float pDt)
        {
            float lP = pPlayer.Progress;
            if (_nextToppleS < lP) _nextToppleS = lP + 200f;
            while (_nextToppleS < lP + TOPPLE_AHEAD && _nextToppleS < _layout.finishS && _topplers.Count < MAX_TOPPLERS)
            {
                for (int lSide = -1; lSide <= 1; lSide += 2)
                    if (R(0f, 1f) < 0.7f) AddToppler(_nextToppleS + R(-40f, 40f), lSide);
                _nextToppleS += R(170f, 330f);
            }
            float lTime = Time.time;
            for (int i = _topplers.Count - 1; i >= 0; i--)
            {
                Toppler lItem = _topplers[i];
                if (lItem.transform == null)
                {
                    _topplers.RemoveAt(i);
                    continue;
                }
                if (lItem.fall < 0f)
                {
                    // Alive : it sways on the crest as if the whole world trembled.
                    float lSway = Mathf.Sin(lTime * 1.7f + lItem.phase) * 3f;
                    lItem.transform.rotation = lItem.rest * Quaternion.AngleAxis(lSway, Vector3.forward) * Quaternion.AngleAxis(Mathf.Sin(lTime * 1.1f + lItem.phase * 2f) * 2f, Vector3.right);
                    if (lItem.s < lP - 60f) lItem.fall = 0f;
                    else if (lItem.s > lP + TOPPLE_AHEAD + 600f)
                    {
                        Destroy(lItem.transform.gameObject);
                        _topplers.RemoveAt(i);
                    }
                    continue;
                }
                lItem.fall += pDt;
                lItem.transform.rotation = Quaternion.AngleAxis(55f * lItem.fall * lItem.fall, lItem.axis) * lItem.rest;
                lItem.transform.position -= Vector3.up * 40f * lItem.fall * pDt;
                if (lItem.fall < 1.15f) continue;
                Vector3 lCenter = lItem.transform.position + lItem.transform.up * lItem.size * 0.5f;
                Fracture.Explode(lCenter, lItem.size * 0.35f, lItem.color, lItem.outward + Vector3.down, 160f, 0.55f);
                Destroy(lItem.transform.gameObject);
                _topplers.RemoveAt(i);
            }
        }

        private void AddToppler(float pS, int pSide)
        {
            Vector3 lCrest, lOut;
            float lHeight;
            if (!Crest(pS, pSide, out lCrest, out lOut, out lHeight)) return;
            float lSize = R(50f, 170f);
            MeshData lData = new MeshData();
            MeshData lGlow = new MeshData();
            PropKit lKit = new PropKit(lData, lGlow, _theme, _random, lCrest);
            lKit.Near(lCrest, lSize);
            if (lData.Count == 0) return;
            GameObject lObject = new GameObject("Toppler");
            lObject.transform.SetParent(transform, false);
            lObject.transform.position = lCrest;
            lObject.layer = Layers.DEBRIS;
            lObject.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("Toppler");
            MeshRenderer lRenderer = lObject.AddComponent<MeshRenderer>();
            lRenderer.sharedMaterial = Mats.Flat(_theme.shade, false);
            lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (lGlow.Count > 0)
            {
                GameObject lLit = new GameObject("Lit");
                lLit.transform.SetParent(lObject.transform, false);
                lLit.AddComponent<MeshFilter>().sharedMesh = lGlow.ToMesh("TopplerLit");
                lLit.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(_theme.accent * 2.8f, false, 0f, 0f, true);
            }
            Vector3 lAlong = Vector3.Cross(lOut, Vector3.up);
            _topplers.Add(new Toppler
            {
                transform = lObject.transform,
                s = pS,
                size = lSize,
                rest = lObject.transform.rotation,
                phase = R(0f, 6.28f),
                // Most fall away from the track, some across it, behind you.
                axis = (R(0f, 1f) < 0.7f ? -lAlong : lAlong).normalized,
                color = Color.Lerp(_theme.decorA, _theme.wall, R(0f, 1f)),
                outward = lOut
            });
        }

        // Pieces of the mountain crests break off behind the runner : the world comes apart in your wake.
        private void UpdateCrumble(PlayerController pPlayer, float pDt)
        {
            _crumbleTimer -= pDt;
            if (_crumbleTimer > 0f) return;
            _crumbleTimer = R(0.1f, 0.22f);
            Vector3 lCrest, lOut;
            float lHeight;
            float lS = pPlayer.Progress - R(80f, 420f);
            int lSide = R(0f, 1f) < 0.5f ? -1 : 1;
            if (!Crest(lS, lSide, out lCrest, out lOut, out lHeight)) return;
            FX.Shards(lCrest - Vector3.up * lHeight * R(0f, 0.5f), _theme.wall, 10, 60f, R(4f, 9f), lOut * 30f + Vector3.up * 50f);
            if (R(0f, 1f) < 0.25f) Fracture.Explode(lCrest, R(14f, 36f), _theme.wall, lOut + Vector3.down, 120f, 0.35f);
        }

        // ================================================================ platforms

        // Platforms give way once you are off them. A rewind puts them back.
        private void UpdateSlabs(PlayerController pPlayer, float pDt)
        {
            if (CourseBreak.Instance != null) return;
            List<TrackChunk> lChunks = TrackBaker.Chunks;
            float lLine = pPlayer.Progress - 160f;
            while (_nextSlab < lChunks.Count && lChunks[_nextSlab].s1 < lLine)
            {
                TrackChunk lChunk = lChunks[_nextSlab++];
                if (lChunk.look != BlockLook.Slab || lChunk.gameObject == null) continue;
                _sinking.Add(new Sinking { chunk = lChunk, home = lChunk.gameObject.transform.position, homeRotation = lChunk.gameObject.transform.rotation, spin = Random.onUnitSphere * 9f });
                lChunk.collider.enabled = false;
                FX.Shards(lChunk.gameObject.transform.position, _theme.roadSide, 14, 50f, 8f, Vector3.up * 30f);
            }
            for (int i = _sinking.Count - 1; i >= 0; i--)
            {
                Sinking lItem = _sinking[i];
                if (lItem.chunk.gameObject == null) { _sinking.RemoveAt(i); continue; }
                lItem.time += pDt;
                // A shudder first, then the drop.
                float lShake = lItem.time < 0.35f ? Mathf.Sin(lItem.time * 90f) * 1.5f : 0f;
                float lDrop = lItem.time < 0.35f ? 0f : 60f * (lItem.time - 0.35f) * (lItem.time - 0.35f);
                Transform lTransform = lItem.chunk.gameObject.transform;
                lTransform.position = lItem.home + Vector3.down * lDrop + Vector3.right * lShake;
                lTransform.rotation = lItem.homeRotation * Quaternion.Euler(lItem.spin * Mathf.Max(0f, lItem.time - 0.35f));
                if (lItem.time > 7f && lItem.chunk.gameObject.activeSelf) lItem.chunk.gameObject.SetActive(false);
            }
        }

        public void RewindTo(float pS)
        {
            float lLine = pS - 160f;
            for (int i = _sinking.Count - 1; i >= 0; i--)
            {
                Sinking lItem = _sinking[i];
                if (lItem.chunk.s1 < lLine || lItem.chunk.gameObject == null) continue;
                lItem.chunk.gameObject.SetActive(true);
                lItem.chunk.gameObject.transform.SetPositionAndRotation(lItem.home, lItem.homeRotation);
                lItem.chunk.collider.enabled = true;
                _sinking.RemoveAt(i);
            }
            int lIndex = TrackBaker.Chunks.FindIndex(pChunk => pChunk.s1 >= lLine);
            if (lIndex >= 0) _nextSlab = Mathf.Min(_nextSlab, lIndex);
            _nextToppleS = Mathf.Min(_nextToppleS, pS + 200f);
        }

        // ================================================================ falling things

        private void UpdateFalling(PlayerController pPlayer, float pDt)
        {
            float lSpeed = Mathf.Max(pPlayer.Speed, _layout.profile.cruise);
            _debrisTimer -= pDt;
            if (_debrisTimer <= 0f)
            {
                _debrisTimer = R(0.9f, 1.9f);
                float lS = pPlayer.Progress + Mathf.Max(500f, lSpeed * 1.8f);
                TrackBlock lBlock = _layout.field.BlockAt(lS);
                if (lBlock != null && lBlock.look != BlockLook.Pipe && !_layout.spine.samples[_layout.spine.IndexOf(lS)].localGravity)
                {
                    float lLateral = lBlock.offset + R(-0.85f, 0.85f) * lBlock.halfWidth;
                    // Most land out of your line : it is a hail around you, not a wall.
                    if (Mathf.Abs(lLateral - pPlayer.Lateral) < 50f && R(0f, 1f) < 0.6f) lLateral += Mathf.Sign(lLateral - pPlayer.Lateral + 0.01f) * 110f;
                    lLateral = Mathf.Clamp(lLateral, lBlock.offset - lBlock.halfWidth * 0.9f, lBlock.offset + lBlock.halfWidth * 0.9f);
                    FallingDebris.Create(transform, _layout, lS, lLateral, _theme, _random);
                }
            }
            // Now and then a meteorite comes for the track ahead : dash through it.
            _aimedTimer -= pDt;
            if (_aimedTimer <= 0f)
            {
                _aimedTimer = R(7f, 12f);
                float lS = pPlayer.Progress + lSpeed * 2.6f;
                if (_layout.field.Solid(lS))
                {
                    float lAim = R(0f, 1f) < 0.5f ? float.NaN : _layout.field.CenterLateral(lS) + R(-0.8f, 0.8f) * _layout.field.HalfWidth(lS);
                    Meteor.CreateIncoming(transform, _layout, pPlayer, lS, R(28f, 60f), _theme, lAim);
                }
            }
        }
    }

    // Something falling out of the sky onto the track ahead. It hits, bounces, then stays : dash through it or go round.
    public class FallingDebris : Breakable
    {
        private enum Kind { Car, Tree, Boulder, Stone, LavaBomb, Crystal, Asteroid }

        private static readonly Dictionary<int, Mesh> _meshes = new Dictionary<int, Mesh>();

        private Vector3 _velocity;
        private Vector3 _spin;
        private Vector3 _up;
        private int _bounces;
        private bool _resting;
        private float _check;
        private LevelLayout _layout;
        private float _s;

        public static FallingDebris Create(Transform pParent, LevelLayout pLayout, float pS, float pLateral, Theme pTheme, System.Random pRandom)
        {
            Kind lKind = Pick(pTheme.decor, pRandom);
            float lSize = lKind == Kind.Car ? 14f + (float)pRandom.NextDouble() * 12f : lKind == Kind.Tree ? 40f + (float)pRandom.NextDouble() * 50f : 18f + (float)pRandom.NextDouble() * 40f;
            Vector3 lUp = pLayout.field.Normal(pS);
            Vector3 lGround = pLayout.GroundPoint(pS, pLateral);
            float lHeight = 280f + (float)pRandom.NextDouble() * 220f;
            const float FALL = 1.35f;

            GameObject lObject = new GameObject("Debris");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.position = lGround + lUp * lHeight + pLayout.spine.Forward(pS) * ((float)pRandom.NextDouble() * 120f - 60f);
            lObject.transform.rotation = Random.rotation;
            lObject.layer = Layers.DEBRIS;
            FallingDebris lDebris = lObject.AddComponent<FallingDebris>();
            lDebris.radius = lSize * 0.5f;
            lDebris.big = lSize > 40f;
            lDebris._layout = pLayout;
            lDebris._s = pS;
            lDebris._up = lUp;
            lDebris._velocity = (lGround - lObject.transform.position) / FALL - lUp * 0f;
            lDebris._spin = Random.onUnitSphere * Random.Range(60f, 260f);
            Color lColor = Tint(lKind, pTheme, pRandom);
            lDebris.color = lColor;

            GameObject lBody = new GameObject("Body");
            lBody.transform.SetParent(lObject.transform, false);
            lBody.transform.localScale = Vector3.one * lSize;
            lBody.AddComponent<MeshFilter>().sharedMesh = MeshFor(lKind, pTheme);
            lBody.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(pTheme.shade, false, lColor);
            if (lKind == Kind.LavaBomb) Mats.Primitive(PrimitiveType.Sphere, lObject.transform, Vector3.zero, Vector3.one * lSize * 0.8f, Mats.Glow(new Color(3.2f, 1.1f, 0.15f), false, 0f, 0f, true));
            // A streak behind it as it falls, so you see it coming.
            TrailRenderer lTrail = new GameObject("Trail").AddComponent<TrailRenderer>();
            lTrail.transform.SetParent(lObject.transform, false);
            lTrail.sharedMaterial = Mats.Glow(lKind == Kind.LavaBomb ? new Color(3f, 1.2f, 0.2f) : new Color(1.6f, 1.6f, 1.8f, 0.7f), true, 1f);
            lTrail.widthMultiplier = lSize * 0.6f;
            lTrail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            lTrail.time = 0.5f;
            lTrail.minVertexDistance = 3f;
            return lDebris;
        }

        private static Kind Pick(DecorKind pDecor, System.Random pRandom)
        {
            int lRoll = pRandom.Next(0, 3);
            switch (pDecor)
            {
                case DecorKind.City:
                case DecorKind.Neon:
                case DecorKind.Night: return lRoll == 0 ? Kind.Stone : Kind.Car;
                case DecorKind.Forest:
                case DecorKind.Prehistoric:
                case DecorKind.Islands: return lRoll == 0 ? Kind.Boulder : lRoll == 1 ? Kind.Car : Kind.Tree;
                case DecorKind.Desert:
                case DecorKind.Canyon: return lRoll == 0 ? Kind.Car : Kind.Boulder;
                case DecorKind.Castle: return lRoll == 0 ? Kind.Boulder : Kind.Stone;
                case DecorKind.Industrial:
                case DecorKind.Volcano: return lRoll == 0 ? Kind.Boulder : Kind.LavaBomb;
                case DecorKind.Ice: return lRoll == 0 ? Kind.Boulder : Kind.Crystal;
                default: return Kind.Asteroid;
            }
        }

        private static Color Tint(Kind pKind, Theme pTheme, System.Random pRandom)
        {
            switch (pKind)
            {
                case Kind.Car:
                    Color[] lPaint = { pTheme.accent, pTheme.decorA, pTheme.roadEdge, new Color(0.95f, 0.95f, 1f), new Color(1f, 0.35f, 0.3f) };
                    return lPaint[pRandom.Next(0, lPaint.Length)];
                case Kind.Tree: return pTheme.decorA;
                case Kind.Stone: return Color.Lerp(pTheme.wall, Color.white, 0.2f);
                case Kind.Crystal: return pTheme.decorC;
                case Kind.LavaBomb: return new Color(0.2f, 0.1f, 0.1f);
                default: return Color.Lerp(pTheme.decorB, pTheme.roadSide, 0.5f);
            }
        }

        // Unit-sized meshes, built once per kind and place.
        private static Mesh MeshFor(Kind pKind, Theme pTheme)
        {
            int lKey = (int)pKind * 1000 + (int)pTheme.decor;
            Mesh lMesh;
            if (_meshes.TryGetValue(lKey, out lMesh) && lMesh != null) return lMesh;
            MeshData lData = new MeshData();
            Color lDark = new Color(0.12f, 0.12f, 0.15f);
            switch (pKind)
            {
                case Kind.Car:
                    GateKit.Box(lData, new Vector3(0f, 0.16f, 0f), new Vector3(0.46f, 0.22f, 1f), Color.white);
                    GateKit.Box(lData, new Vector3(0f, 0.34f, -0.06f), new Vector3(0.4f, 0.16f, 0.5f), new Color(0.75f, 0.85f, 1f));
                    for (int lX = -1; lX <= 1; lX += 2)
                        for (int lZ = -1; lZ <= 1; lZ += 2)
                            GateKit.Box(lData, new Vector3(lX * 0.24f, 0.05f, lZ * 0.32f), new Vector3(0.08f, 0.16f, 0.16f), lDark);
                    break;
                case Kind.Tree:
                    GateKit.Box(lData, new Vector3(0f, 0.3f, 0f), new Vector3(0.08f, 0.6f, 0.08f), new Color(0.45f, 0.3f, 0.22f));
                    lData.Append(RockSculptor.Boulder(1), Matrix4x4.TRS(new Vector3(0f, 0.7f, 0f), Quaternion.identity, new Vector3(0.5f, 0.3f, 0.5f)), Color.white);
                    lData.Append(RockSculptor.Boulder(3), Matrix4x4.TRS(new Vector3(0.1f, 0.88f, 0.05f), Quaternion.identity, new Vector3(0.34f, 0.22f, 0.34f)), Color.white);
                    break;
                case Kind.Stone:
                    GateKit.Box(lData, Vector3.zero, new Vector3(0.9f, 0.6f, 0.6f), Color.white);
                    for (int i = -1; i <= 1; i++) GateKit.Box(lData, new Vector3(i * 0.32f, 0.4f, 0f), new Vector3(0.2f, 0.2f, 0.6f), Color.white);
                    break;
                case Kind.Crystal:
                    lData.Append(RockSculptor.Shard(2), Matrix4x4.Scale(new Vector3(0.4f, 1f, 0.4f)), Color.white);
                    break;
                default:
                    lData.Append(RockSculptor.Boulder((int)pKind % 6), Matrix4x4.Scale(Vector3.one), Color.white);
                    break;
            }
            lMesh = lData.ToMesh("Debris" + pKind);
            _meshes[lKey] = lMesh;
            return lMesh;
        }

        protected override void Update()
        {
            base.Update();
            float lDt = Time.deltaTime;
            PlayerController lPlayer = RaceContext.Player;
            // Far behind : gone.
            if (lPlayer != null && _s < lPlayer.Progress - 400f)
            {
                Destroy(gameObject);
                return;
            }
            if (_resting || lDt <= 0f) return;
            _velocity -= _up * 220f * lDt;
            transform.position += _velocity * lDt;
            transform.Rotate(_spin * lDt, Space.World);
            _check -= lDt;
            if (_check > 0f) return;
            _check = 0.03f;
            RaycastHit lHit;
            float lReach = Mathf.Max(0f, -Vector3.Dot(_velocity, _up)) * 0.05f + radius;
            if (!Physics.Raycast(transform.position, -_up, out lHit, lReach, 1 << Layers.DECK, QueryTriggerInteraction.Ignore)) return;
            transform.position = lHit.point + _up * radius;
            FX.Shockwave(lHit.point + _up, _up, Color.white, radius * 4f, 0.35f);
            FX.Shards(lHit.point, RaceContext.Theme != null ? RaceContext.Theme.roadSide : Color.gray, 10, 40f, radius * 0.15f, _up * 40f);
            if (lPlayer != null)
            {
                float lDistance = Vector3.Distance(lPlayer.transform.position, lHit.point);
                if (lDistance < 600f) Juice.Shake(Mathf.Clamp01(0.3f - lDistance / 2000f));
            }
            if (++_bounces >= 2)
            {
                _resting = true;
                transform.position = lHit.point;
                transform.rotation = Quaternion.FromToRotation(transform.up, _up) * transform.rotation;
                return;
            }
            _velocity = Vector3.ProjectOnPlane(_velocity, _up) * 0.5f + _up * Mathf.Abs(Vector3.Dot(_velocity, _up)) * 0.3f;
            _spin *= 0.5f;
        }
    }
}
