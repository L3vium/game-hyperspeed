using System;
using System.Collections.Generic;
using System.IO;
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // Builds Assets/_Game/Resources/Juice/HS_Juice.prefab : one child per juice event, grouped by category, each with an
    // MMF_Player filled with its default recipe. Running it again only ADDS the events that are missing : whatever you
    // tuned stays as you left it. "Reset selected event" rebuilds one event from its recipe (undoable while open).
    public static class JuiceBankBuilder
    {
        public const string FOLDER = "Assets/_Game/Resources/Juice";
        public const string PATH = FOLDER + "/HS_Juice.prefab";

        [MenuItem("Hyperspeed/Juice/Create or Update Juice Bank")]
        public static void CreateOrUpdate() => Build(false);

        // Only for the first passes of the game's own tuning : throws away every hand edit.
        [MenuItem("Hyperspeed/Juice/Rebuild Every Event From Defaults (discards tweaks)")]
        public static void RebuildAll()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Rebuild juice", "Throw away every tweak of every juice event and rebuild them from their default recipes ?", "Rebuild", "Cancel")) return;
            Build(true);
        }

        public static void RebuildAllSilently() => Build(true);

        [MenuItem("Hyperspeed/Juice/Open Juice Bank")]
        public static void Open()
        {
            Build(false);
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(PATH));
        }

        [MenuItem("Hyperspeed/Juice/Reset Selected Event To Defaults")]
        public static void ResetSelected()
        {
            GameObject lSelected = Selection.activeGameObject;
            HSJuiceEvent lEvent = lSelected != null ? lSelected.GetComponent<HSJuiceEvent>() : null;
            if (lEvent == null)
            {
                EditorUtility.DisplayDialog("Reset juice event", "Select one juice event (a child of HS_Juice) first.", "OK");
                return;
            }
            MMF_Player lPlayer = lEvent.GetComponent<MMF_Player>();
            Undo.RecordObject(lPlayer, "Reset juice event");
            lPlayer.FeedbacksList.Clear();
            JuiceDefaults.Fill(lEvent.id, lPlayer);
            EditorUtility.SetDirty(lPlayer);
        }

        private static void Build(bool pRebuild)
        {
            if (!AssetDatabase.IsValidFolder(FOLDER))
            {
                Directory.CreateDirectory(FOLDER);
                AssetDatabase.Refresh();
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PATH) == null)
            {
                GameObject lNew = new GameObject("HS_Juice");
                lNew.AddComponent<HSJuiceBank>();
                PrefabUtility.SaveAsPrefabAsset(lNew, PATH);
                UnityEngine.Object.DestroyImmediate(lNew);
            }

            GameObject lRoot = PrefabUtility.LoadPrefabContents(PATH);
            int lAdded = 0;
            int lRebuilt = 0;
            try
            {
                if (lRoot.GetComponent<HSJuiceBank>() == null) lRoot.AddComponent<HSJuiceBank>();
                Dictionary<JuiceId, HSJuiceEvent> lHave = new Dictionary<JuiceId, HSJuiceEvent>();
                foreach (HSJuiceEvent lEvent in lRoot.GetComponentsInChildren<HSJuiceEvent>(true)) if (!lHave.ContainsKey(lEvent.id)) lHave.Add(lEvent.id, lEvent);
                foreach (JuiceId lId in Enum.GetValues(typeof(JuiceId)))
                {
                    HSJuiceEvent lEvent;
                    if (lHave.TryGetValue(lId, out lEvent))
                    {
                        if (!pRebuild) continue;
                        MMF_Player lExisting = lEvent.GetComponent<MMF_Player>();
                        if (lExisting.FeedbacksList != null) lExisting.FeedbacksList.Clear();
                        Configure(lExisting);
                        JuiceDefaults.Fill(lId, lExisting);
                        lEvent.notes = JuiceDefaults.Notes(lId);
                        lEvent.playWhilePaused = JuiceDefaults.PlaysWhilePaused(lId);
                        lEvent.minInterval = JuiceDefaults.MinInterval(lId);
                        lRebuilt++;
                        continue;
                    }
                    GameObject lObject = new GameObject("Juice_" + lId);
                    lObject.transform.SetParent(Group(lRoot.transform, JuiceDefaults.Category(lId)), false);
                    MMF_Player lPlayer = lObject.AddComponent<MMF_Player>();
                    Configure(lPlayer);
                    HSJuiceEvent lNew = lObject.AddComponent<HSJuiceEvent>();
                    lNew.id = lId;
                    lNew.notes = JuiceDefaults.Notes(lId);
                    lNew.playWhilePaused = JuiceDefaults.PlaysWhilePaused(lId);
                    lNew.minInterval = JuiceDefaults.MinInterval(lId);
                    JuiceDefaults.Fill(lId, lPlayer);
                    lAdded++;
                }
                // The look of each speed state (wide angle, camera distance, screen) : added once, then yours.
                HashSet<SpeedState> lLooks = new HashSet<SpeedState>();
                foreach (HSSpeedStateLook lLook in lRoot.GetComponentsInChildren<HSSpeedStateLook>(true)) lLooks.Add(lLook.state);
                foreach (SpeedState lState in Enum.GetValues(typeof(SpeedState)))
                {
                    if (lLooks.Contains(lState)) continue;
                    GameObject lObject = new GameObject("Juice_State_" + lState);
                    lObject.transform.SetParent(Group(lRoot.transform, "Speed States"), false);
                    HSSpeedStateLook.Defaults(lObject.AddComponent<HSSpeedStateLook>(), lState);
                    lAdded++;
                }
                if (lAdded > 0 || lRebuilt > 0) PrefabUtility.SaveAsPrefabAsset(lRoot, PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(lRoot);
            }
            Debug.Log("[HS-JUICE] juice bank : " + lAdded + " events added, " + lRebuilt + " rebuilt (" + PATH + ")");
        }

        private static Transform Group(Transform pRoot, string pName)
        {
            Transform lGroup = pRoot.Find(pName);
            if (lGroup != null) return lGroup;
            GameObject lObject = new GameObject(pName);
            lObject.transform.SetParent(pRoot, false);
            return lObject.transform;
        }

        private static void Configure(MMF_Player pPlayer)
        {
            pPlayer.InitializationMode = MMFeedbacks.InitializationModes.Awake;
            pPlayer.AutoInitialization = true;
            pPlayer.AutoPlayOnStart = false;
            pPlayer.AutoPlayOnEnable = false;
            // Hit-stops freeze the world : the feedbacks themselves must keep running on real time.
            pPlayer.ForceTimescaleMode = true;
            pPlayer.ForcedTimescaleMode = TimescaleModes.Unscaled;
            pPlayer.CanPlayWhileAlreadyPlaying = true;
            pPlayer.PerformanceMode = true;
            pPlayer.StopFeedbacksOnDisable = true;
        }
    }
}
