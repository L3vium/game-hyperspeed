using System.Collections;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Stage presentation with side camera angles : the world crumbling, the vehicle, the runner, both side by side
    // with the objective, then the camera settles behind the runner. Scaled time (pause freezes it), skippable.
    public class IntroDirector
    {
        private readonly LevelLayout _layout;
        private readonly LevelDefinition _level;
        private readonly int _index;
        private readonly PlayerController _player;
        private readonly Rival _rival;
        private readonly CameraRig _rig;
        private readonly HUD _hud;
        private bool _skip;

        public IntroDirector(LevelLayout pLayout, LevelDefinition pLevel, int pIndex, PlayerController pPlayer, Rival pRival, CameraRig pRig, HUD pHud)
        {
            _layout = pLayout;
            _level = pLevel;
            _index = pIndex;
            _player = pPlayer;
            _rival = pRival;
            _rig = pRig;
            _hud = pHud;
        }

        public static string Objective(LevelDefinition pLevel, LevelLayout pLayout)
        {
            string lKm = (pLayout.finishS / 1000f).ToString("0.0") + " KM";
            string lName = RivalFactory.DisplayName(pLevel.rival);
            if (pLevel.chaser) return "OUTRUN " + lName + " FOR " + lKm;
            return "BEAT " + lName + " TO THE FINISH  —  " + lKm;
        }

        public IEnumerator Play()
        {
            Vector3 lSide = _layout.spine.Right(0f) * _layout.laneSide;
            Vector3 lForward = _layout.spine.Forward(0f);
            Vector3 lRunner = _player.FocusPosition;

            // 1. Establishing : high and wide over the plaza, the route running into the crumbling world.
            _hud.IntroTitle(_level, _index, RaceContext.Theme != null ? RaceContext.Theme.name : "");
            Vector3 lFrom = _layout.spine.Point(40f, -lSide.magnitude * 0f, _layout.spine.Height(40f) + 30f) - lSide * 40f;
            yield return Shot(1.5f, pT => lFrom + lForward * (pT * 25f), pT => lRunner + lForward * 60f, 60f);
            if (_skip) yield break;

            // 2. The vehicle, filmed from its outer side so the route sits behind it.
            Vector3 lVehicle = _rival.transform.position + Vector3.up * VehicleHeight();
            _hud.IntroRival(_level);
            float lVehicleDistance = Mathf.Max(9f, RivalFactory.Size(_level.rival).magnitude * 0.9f);
            yield return Shot(1.7f, pT => lVehicle + lSide * lVehicleDistance + Vector3.up * 1.2f + lForward * Mathf.Lerp(-4f, 3f, pT), pT => lVehicle + lForward * Mathf.Lerp(-1f, 1f, pT), 48f);
            if (_skip) yield break;

            // 3. The runner, from the route side so the vehicle is in the background.
            _hud.IntroRunner();
            _player.Visual?.Taunt();
            yield return Shot(1.4f, pT => lRunner - lSide * 6.5f + Vector3.up * 0.4f + lForward * Mathf.Lerp(2.5f, -1f, pT), pT => lRunner + Vector3.up * 0.2f, 42f);
            if (_skip) yield break;

            // 4. Side by side on the line, with the objective.
            Vector3 lMid = (lRunner + lVehicle) * 0.5f;
            _hud.IntroObjective(Objective(_level, _layout));
            yield return Shot(1.9f, pT => lMid - lForward * Mathf.Lerp(26f, 20f, pT) + Vector3.up * Mathf.Lerp(7f, 5f, pT), pT => lMid + lForward * 14f, 62f);
        }

        private float VehicleHeight() => Mathf.Clamp(RivalFactory.Size(_level.rival).y * 0.5f, 0.6f, 6f);

        private IEnumerator Shot(float pDuration, System.Func<float, Vector3> pPosition, System.Func<float, Vector3> pLook, float pFov)
        {
            _rig.CutTo(pPosition(0f), pLook(0f), pFov);
            for (float lTime = 0f; lTime < pDuration; lTime += Time.deltaTime)
            {
                if (HSInput.SubmitPressed || HSInput.JumpPressed)
                {
                    _skip = true;
                    _hud.IntroClear();
                    yield break;
                }
                float lT = LevelBuilder.Smooth(Mathf.Clamp01(lTime / pDuration));
                _rig.SetOverride(pPosition(lT), pLook(lT), pFov);
                yield return null;
            }
        }
    }
}
