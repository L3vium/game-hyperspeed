using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // Generates Art/Character/Runner.controller from the Mixamo clips that exist (missing clips are skipped) :
    // locomotion blend by speed, air blend by vertical speed, glide / dive / slide / grind / hang / wall run,
    // landings, flip, spin, stumble, victory / defeat, pre-race idles. Open it in the Animator window to tune.
    public static class RunnerAnimatorBuilder
    {
        public const string CONTROLLER = "Assets/_Game/Art/Character/Runner.controller";

        private static Dictionary<string, AnimationClip> _clips;

        private static AnimationClip Clip(string pName)
        {
            AnimationClip lClip;
            return _clips.TryGetValue(pName, out lClip) ? lClip : null;
        }

        private static AnimationClip First(params string[] pNames)
        {
            foreach (string lName in pNames)
            {
                AnimationClip lClip = Clip(lName);
                if (lClip != null) return lClip;
            }
            return null;
        }

        private static void LoadClips()
        {
            _clips = new Dictionary<string, AnimationClip>();
            foreach (string lGuid in AssetDatabase.FindAssets("t:Model", new[] { MixamoImporter.FOLDER.TrimEnd('/') }))
            {
                string lPath = AssetDatabase.GUIDToAssetPath(lGuid);
                string lName = Path.GetFileNameWithoutExtension(lPath).ToLower();
                foreach (Object lAsset in AssetDatabase.LoadAllAssetsAtPath(lPath))
                {
                    AnimationClip lClip = lAsset as AnimationClip;
                    if (lClip == null || lClip.name.StartsWith("__preview__")) continue;
                    _clips[lName] = lClip;
                    break;
                }
            }
        }

        public static bool HasClips()
        {
            LoadClips();
            return _clips.Count > 0;
        }

        [MenuItem("Hyperspeed/Build Runner Animator")]
        public static RuntimeAnimatorController Build()
        {
            LoadClips();
            if (_clips.Count == 0)
            {
                Debug.LogWarning("[Hyperspeed] No Mixamo clips in " + MixamoImporter.FOLDER);
                return null;
            }
            AssetDatabase.DeleteAsset(CONTROLLER);
            AnimatorController lController = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER);
            string[] lFloats = { "Speed", "Vertical" };
            string[] lBools = { "Grounded", "Glide", "Dive", "Slide", "Grind", "Hang", "WallRun", "WallClimb", "WallSlide", "Win", "Lose", "PreRace" };
            string[] lTriggers = { "Jump", "Flip", "Land", "LandHard", "LandRoll", "Spin", "Stumble", "Taunt" };
            foreach (string lName in lFloats) lController.AddParameter(lName, AnimatorControllerParameterType.Float);
            foreach (string lName in lBools) lController.AddParameter(lName, AnimatorControllerParameterType.Bool);
            foreach (string lName in lTriggers) lController.AddParameter(lName, AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine lMachine = lController.layers[0].stateMachine;

            // Locomotion : 1D blend on the normalized speed parameter (0 idle .. 1 sprint).
            BlendTree lLocoTree;
            AnimatorState lLoco = lController.CreateBlendTreeInController("Locomotion", out lLocoTree, 0);
            lLocoTree.blendType = BlendTreeType.Simple1D;
            lLocoTree.blendParameter = "Speed";
            AddMotion(lLocoTree, First("idle", "idle_ready"), 0f);
            AddMotion(lLocoTree, Clip("jog"), 0.18f);
            AddMotion(lLocoTree, Clip("run"), 0.38f);
            AddMotion(lLocoTree, Clip("run_fast"), 0.62f);
            AddMotion(lLocoTree, First("sprint", "run_fast"), 1f);
            lMachine.defaultState = lLoco;

            AnimatorState lReady = AddState(lMachine, "Ready", First("idle_ready", "look_around", "idle"), new Vector3(300f, -120f));
            AnimatorState lTaunt = AddState(lMachine, "Taunt", First("taunt", "idle_ready"), new Vector3(520f, -120f));

            // Air : one clean falling loop. The jump clip is a whole jump (take off, flight, landing) : only its take-off
            // part is played, on the Jump trigger, then the fall loop takes over. Never a landing pose in the air.
            AnimatorState lAir = AddState(lMachine, "Air", First("fall", "glide"), new Vector3(300f, 80f));
            AnimatorState lJumpStart = AddState(lMachine, "JumpStart", First("jump", "fall"), new Vector3(300f, 0f));
            if (lJumpStart != null)
            {
                lJumpStart.speed = 1.5f;
                lJumpStart.cycleOffset = 0.1f;
            }

            AnimatorState lGlide = AddState(lMachine, "Glide", First("glide", "fall"), new Vector3(560f, 80f));
            AnimatorState lDive = AddState(lMachine, "Dive", First("dive", "fall"), new Vector3(560f, 160f));
            AnimatorState lSlide = AddState(lMachine, "Slide", First("slide", "roll"), new Vector3(300f, 240f));
            AnimatorState lGrind = AddState(lMachine, "Grind", First("grind", "slide", "run"), new Vector3(560f, 240f));
            AnimatorState lHang = AddState(lMachine, "Hang", First("hang", "fall"), new Vector3(560f, 320f));
            AnimatorState lWall = AddState(lMachine, "WallRun", First("wall_run", "sprint", "run_fast"), new Vector3(560f, 400f));
            AnimatorState lClimb = AddState(lMachine, "WallClimb", First("wall_climb", "sprint", "run_fast"), new Vector3(760f, 400f));
            AnimatorState lWallSlide = AddState(lMachine, "WallSlide", First("wall_slide", "fall", "hang"), new Vector3(960f, 400f));
            AnimatorState lLandSoft = AddState(lMachine, "LandSoft", Clip("land_soft"), new Vector3(300f, 480f));
            AnimatorState lLandHard = AddState(lMachine, "LandHard", First("land_hard", "land_soft"), new Vector3(520f, 480f));
            AnimatorState lLandRoll = AddState(lMachine, "LandRoll", First("land_roll", "roll"), new Vector3(740f, 480f));
            AnimatorState lFlip = AddState(lMachine, "Flip", First("jump_flip", "jump"), new Vector3(780f, 80f));
            AnimatorState lSpin = AddState(lMachine, "Spin", Clip("turn_spin"), new Vector3(780f, 160f));
            AnimatorState lStumble = AddState(lMachine, "Stumble", Clip("stumble"), new Vector3(780f, 240f));
            AnimatorState lVictory = AddState(lMachine, "Victory", First("victory", "taunt"), new Vector3(780f, 320f));
            AnimatorState lDefeat = AddState(lMachine, "Defeat", First("defeat", "idle"), new Vector3(780f, 400f));

            // Priority : terminal results, then rails / hang / walls, then glide / dive, then air, then landings.
            if (lVictory != null) AnyTo(lMachine, lVictory, 0.25f, Cond("Win", true));
            if (lDefeat != null) AnyTo(lMachine, lDefeat, 0.25f, Cond("Lose", true));
            if (lStumble != null) AnyTrigger(lMachine, lStumble, "Stumble", 0.05f);
            if (lGrind != null) AnyTo(lMachine, lGrind, 0.08f, Cond("Grind", true));
            if (lHang != null) AnyTo(lMachine, lHang, 0.1f, Cond("Hang", true));
            if (lWall != null) AnyTo(lMachine, lWall, 0.1f, Cond("WallRun", true));
            if (lClimb != null) AnyTo(lMachine, lClimb, 0.1f, Cond("WallClimb", true));
            if (lWallSlide != null) AnyTo(lMachine, lWallSlide, 0.12f, Cond("WallSlide", true));
            if (lClimb != null) To(lClimb, lLoco, 0.12f, Cond("WallClimb", false));
            if (lWallSlide != null) To(lWallSlide, lLoco, 0.12f, Cond("WallSlide", false));
            if (lGlide != null) AnyTo(lMachine, lGlide, 0.15f, Cond("Glide", true), Cond("Grounded", false));
            if (lDive != null) AnyTo(lMachine, lDive, 0.1f, Cond("Dive", true), Cond("Grounded", false));
            if (lFlip != null) AnyTrigger(lMachine, lFlip, "Flip", 0.05f);
            if (lSpin != null) AnyTrigger(lMachine, lSpin, "Spin", 0.06f);
            if (lTaunt != null) AnyTrigger(lMachine, lTaunt, "Taunt", 0.2f);

            // A jump : the take-off part of the jump clip, then the fall loop (or straight back to the run on a short hop).
            foreach (AnimatorState lFrom in new[] { lLoco, lReady, lSlide, lGrind, lWall })
            {
                if (lFrom == null || lJumpStart == null) continue;
                TriggerTo(lFrom, lJumpStart, "Jump", 0.06f);
            }
            if (lJumpStart != null)
            {
                AnimatorStateTransition lToAir = lJumpStart.AddTransition(lAir);
                lToAir.hasExitTime = true;
                lToAir.exitTime = 0.45f;
                lToAir.duration = 0.2f;
                AnimatorStateTransition lHop = lJumpStart.AddTransition(lLoco);
                lHop.hasExitTime = true;
                lHop.exitTime = 0.3f;
                lHop.duration = 0.1f;
                lHop.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            }
            // Air from anything grounded / special once those flags drop (running off an edge).
            foreach (AnimatorState lFrom in new[] { lLoco, lReady, lSlide, lGrind, lHang, lWall, lGlide, lDive })
            {
                if (lFrom == null) continue;
                To(lFrom, lAir, 0.15f, Cond("Grounded", false), Cond("Glide", false), Cond("Dive", false), Cond("Grind", false), Cond("Hang", false), Cond("WallRun", false), Cond("WallClimb", false), Cond("WallSlide", false));
            }
            // Landing triggers are checked before the plain "grounded" exit : they are always used when they are set.
            if (lLandSoft != null) TriggerTo(lAir, lLandSoft, "Land", 0.05f);
            if (lLandHard != null) TriggerTo(lAir, lLandHard, "LandHard", 0.04f);
            if (lLandRoll != null) TriggerTo(lAir, lLandRoll, "LandRoll", 0.04f);
            To(lAir, lLoco, 0.1f, Cond("Grounded", true));
            foreach (AnimatorState lFrom in new[] { lGlide, lDive })
            {
                if (lFrom == null) continue;
                To(lFrom, lLoco, 0.1f, Cond("Grounded", true));
                if (lLandRoll != null) TriggerTo(lFrom, lLandRoll, "LandRoll", 0.04f);
            }
            foreach (AnimatorState lLand in new[] { lLandSoft, lLandHard, lLandRoll, lSpin, lStumble, lTaunt })
            {
                if (lLand == null) continue;
                AnimatorStateTransition lBack = lLand.AddTransition(lLoco);
                lBack.hasExitTime = true;
                lBack.exitTime = lLand == lTaunt ? 0.9f : 0.55f;
                lBack.duration = 0.18f;
            }
            if (lFlip != null)
            {
                AnimatorStateTransition lBack = lFlip.AddTransition(lAir);
                lBack.hasExitTime = true;
                lBack.exitTime = 0.85f;
                lBack.duration = 0.15f;
            }
            if (lSlide != null)
            {
                To(lLoco, lSlide, 0.12f, Cond("Slide", true), Cond("Grounded", true));
                To(lSlide, lLoco, 0.18f, Cond("Slide", false));
            }
            if (lReady != null)
            {
                To(lLoco, lReady, 0.3f, Cond("PreRace", true), CondFloat("Speed", false, 0.05f));
                To(lReady, lLoco, 0.15f, CondFloat("Speed", true, 0.05f));
                To(lReady, lLoco, 0.25f, Cond("PreRace", false));
            }
            foreach (AnimatorState lFrom in new[] { lGrind, lHang, lWall })
            {
                if (lFrom == null) continue;
                To(lFrom, lLoco, 0.12f, Cond("Grounded", true), Cond(lFrom == lGrind ? "Grind" : lFrom == lHang ? "Hang" : "WallRun", false));
            }

            EditorUtility.SetDirty(lController);
            AssetDatabase.SaveAssets();
            Debug.Log("[Hyperspeed] Runner animator built with " + _clips.Count + " clips.");
            return lController;
        }

        private static void AddMotion(BlendTree pTree, AnimationClip pClip, float pThreshold)
        {
            if (pClip != null) pTree.AddChild(pClip, pThreshold);
        }

        private static AnimatorState AddState(AnimatorStateMachine pMachine, string pName, AnimationClip pClip, Vector3 pPosition)
        {
            if (pClip == null) return null;
            AnimatorState lState = pMachine.AddState(pName, pPosition);
            lState.motion = pClip;
            return lState;
        }

        private struct Condition
        {
            public AnimatorConditionMode mode;
            public float threshold;
            public string parameter;
        }

        private static Condition Cond(string pParameter, bool pValue) => new Condition { parameter = pParameter, mode = pValue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot };

        private static Condition CondFloat(string pParameter, bool pGreater, float pThreshold) => new Condition { parameter = pParameter, mode = pGreater ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, threshold = pThreshold };

        private static void Apply(AnimatorStateTransition pTransition, float pDuration, Condition[] pConditions)
        {
            pTransition.hasExitTime = false;
            pTransition.duration = pDuration;
            pTransition.canTransitionToSelf = false;
            foreach (Condition lCondition in pConditions) pTransition.AddCondition(lCondition.mode, lCondition.threshold, lCondition.parameter);
        }

        private static void To(AnimatorState pFrom, AnimatorState pTo, float pDuration, params Condition[] pConditions)
        {
            if (pFrom == null || pTo == null) return;
            Apply(pFrom.AddTransition(pTo), pDuration, pConditions);
        }

        private static void TriggerTo(AnimatorState pFrom, AnimatorState pTo, string pTrigger, float pDuration)
        {
            if (pFrom == null || pTo == null) return;
            Apply(pFrom.AddTransition(pTo), pDuration, new[] { new Condition { parameter = pTrigger, mode = AnimatorConditionMode.If } });
        }

        private static void AnyTo(AnimatorStateMachine pMachine, AnimatorState pTo, float pDuration, params Condition[] pConditions)
        {
            Apply(pMachine.AddAnyStateTransition(pTo), pDuration, pConditions);
        }

        private static void AnyTrigger(AnimatorStateMachine pMachine, AnimatorState pTo, string pTrigger, float pDuration)
        {
            Apply(pMachine.AddAnyStateTransition(pTo), pDuration, new[] { new Condition { parameter = pTrigger, mode = AnimatorConditionMode.If } });
        }
    }
}
