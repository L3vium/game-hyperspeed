using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Procedural animation on any Humanoid avatar through muscle space (HumanPoseHandler).
    // Poses are written as targets every frame, then smoothed per muscle so transitions keep overlap and follow-through.
    // Calibrated conventions (Ellen, facing +Z) : leg/arm Front-Back negative = forward, Lower Leg / Forearm Stretch 1 = straight.
    public class HumanoidAnimator
    {
        public enum M
        {
            SpineFB, SpineLR, SpineTwist, ChestFB, ChestLR, ChestTwist, NeckNod, NeckTilt, NeckTurn, HeadNod, HeadTilt, HeadTurn,
            LUpLegFB, LUpLegIO, LUpLegTwist, LLowLeg, LFoot, RUpLegFB, RUpLegIO, RUpLegTwist, RLowLeg, RFoot,
            LShoulderDU, LShoulderFB, LArmDU, LArmFB, LArmTwist, LForearm, LHandDU,
            RShoulderDU, RShoulderFB, RArmDU, RArmFB, RArmTwist, RForearm, RHandDU,
            Count
        }

        private static readonly string[] NAMES =
        {
            "Spine Front-Back", "Spine Left-Right", "Spine Twist Left-Right", "Chest Front-Back", "Chest Left-Right", "Chest Twist Left-Right",
            "Neck Nod Down-Up", "Neck Tilt Left-Right", "Neck Turn Left-Right", "Head Nod Down-Up", "Head Tilt Left-Right", "Head Turn Left-Right",
            "Left Upper Leg Front-Back", "Left Upper Leg In-Out", "Left Upper Leg Twist In-Out", "Left Lower Leg Stretch", "Left Foot Up-Down",
            "Right Upper Leg Front-Back", "Right Upper Leg In-Out", "Right Upper Leg Twist In-Out", "Right Lower Leg Stretch", "Right Foot Up-Down",
            "Left Shoulder Down-Up", "Left Shoulder Front-Back", "Left Arm Down-Up", "Left Arm Front-Back", "Left Arm Twist In-Out", "Left Forearm Stretch", "Left Hand Down-Up",
            "Right Shoulder Down-Up", "Right Shoulder Front-Back", "Right Arm Down-Up", "Right Arm Front-Back", "Right Arm Twist In-Out", "Right Forearm Stretch", "Right Hand Down-Up"
        };

        private readonly HumanPoseHandler _handler;
        private HumanPose _pose;
        private readonly float[] _base;
        private readonly int[] _index = new int[(int)M.Count];
        private readonly float[] _target = new float[(int)M.Count];
        private readonly float[] _current = new float[(int)M.Count];
        private readonly int[] _fingers;
        private readonly Vector3 _baseBody;
        private float _fist;

        public float BodyOffsetY;
        public float BodyOffsetZ;

        public HumanoidAnimator(Animator pAnimator)
        {
            _handler = new HumanPoseHandler(pAnimator.avatar, pAnimator.transform);
            _handler.GetHumanPose(ref _pose);
            _base = (float[])_pose.muscles.Clone();
            _baseBody = _pose.bodyPosition;
            string[] lAll = HumanTrait.MuscleName;
            for (int i = 0; i < _index.Length; i++)
            {
                _index[i] = System.Array.IndexOf(lAll, NAMES[i]);
                _current[i] = _index[i] >= 0 ? _base[_index[i]] : 0f;
            }
            System.Collections.Generic.List<int> lFingers = new System.Collections.Generic.List<int>();
            for (int i = 0; i < lAll.Length; i++)
                if (lAll[i].EndsWith("Stretched")) lFingers.Add(i);
            _fingers = lFingers.ToArray();
        }

        public void Dispose() => _handler.Dispose();

        // Thigh angle in degrees forward from vertical -> muscle value.
        public static float Leg(float pDegrees) => pDegrees >= 27f ? -(pDegrees - 27f) / 90f : (27f - pDegrees) / 48f;

        // Knee bend in degrees -> Lower Leg Stretch.
        public static float Knee(float pBend) => Mathf.Clamp(1f - pBend / 90f, -1f, 1f);

        // Elbow bend in degrees -> Forearm Stretch.
        public static float Elbow(float pBend) => Mathf.Clamp(1f - pBend / 90f, -1f, 1f);

        public void ResetTargets()
        {
            for (int i = 0; i < _target.Length; i++) _target[i] = _index[i] >= 0 ? _base[_index[i]] : 0f;
            _target[(int)M.LArmDU] = -0.55f;
            _target[(int)M.RArmDU] = -0.55f;
            _target[(int)M.LArmFB] = 0f;
            _target[(int)M.RArmFB] = 0f;
            _target[(int)M.LForearm] = Elbow(40f);
            _target[(int)M.RForearm] = Elbow(40f);
            BodyOffsetY = 0f;
            BodyOffsetZ = 0f;
        }

        public void Set(M pMuscle, float pValue) => _target[(int)pMuscle] = pValue;

        public void Add(M pMuscle, float pValue) => _target[(int)pMuscle] += pValue;

        public void SetFist(float pAmount) => _fist = pAmount;

        public void Apply(float pDt, float pSharpness)
        {
            float lT = 1f - Mathf.Exp(-pSharpness * pDt);
            for (int i = 0; i < _target.Length; i++)
            {
                if (_index[i] < 0) continue;
                _current[i] = Mathf.Lerp(_current[i], Mathf.Clamp(_target[i], -1.2f, 1.2f), lT);
                _pose.muscles[_index[i]] = _current[i];
            }
            foreach (int lFinger in _fingers) _pose.muscles[lFinger] = Mathf.Lerp(_base[lFinger], -0.9f, _fist);
            _pose.bodyPosition = _baseBody + new Vector3(0f, BodyOffsetY, BodyOffsetZ);
            _pose.bodyRotation = Quaternion.identity;
            _handler.SetHumanPose(ref _pose);
        }
    }
}
