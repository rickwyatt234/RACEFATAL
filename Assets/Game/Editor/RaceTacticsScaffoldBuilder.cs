using RaceFatal.Content.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Tactics;
using RaceFatal.Presentation.Tracks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RaceTacticsScaffoldBuilder
{
    [MenuItem("RACE//FATAL/Racing/Build Tactical UI and Commands")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Exit Play Mode before building tactical UI."); return; }
        var runtime = Object.FindFirstObjectByType<RaceRuntimeController>();
        if (runtime == null) { Debug.LogError("Open the race scene containing RaceRuntimeController first."); return; }
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Build race tactical interface");
        if (runtime.GetComponent<TacticalRaceController>() == null) Undo.AddComponent<TacticalRaceController>(runtime.gameObject);
        var ui = runtime.GetComponent<TacticalRaceUI>() ?? Undo.AddComponent<TacticalRaceUI>(runtime.gameObject);
        bool hadCanvas = runtime.transform.Find("TacticalCanvas") != null;
        ui.BuildUI();
        if (!hadCanvas && runtime.transform.Find("TacticalCanvas") != null) Undo.RegisterCreatedObjectUndo(runtime.transform.Find("TacticalCanvas").gameObject,"Create tactical canvas");
        EditorUtility.SetDirty(ui);
        foreach (var strip in Object.FindObjectsByType<EnergyStripTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if (strip.GetComponent<EnergyStripAIAnchor>() == null) Undo.AddComponent<EnergyStripAIAnchor>(strip.gameObject);
                Undo.RecordObject(strip,"Rebalance recharge strip");
                var so = new SerializedObject(strip);var rate = so.FindProperty("rechargePerSecond");rate.floatValue = Mathf.Max(200f,rate.floatValue);so.ApplyModifiedProperties();
            }
        EditorSceneManager.MarkSceneDirty(runtime.gameObject.scene);
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = runtime.gameObject;
        Debug.Log("Tactical UI ready. Save the scene. Space toggles tactical; W/S pan, scroll zooms, MMB drag orbits, Q resets top-down, Tab cycles, Enter opens the action list, Home recenters. Resource rebalance is a separate menu action.");
    }

    [MenuItem("RACE//FATAL/Racing/Apply Energy and Survivability Rebalance")]
    public static void Rebalance()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        // Idempotent migration: existing larger values and explicit new energy costs are retained.
        foreach (string guid in AssetDatabase.FindAssets("t:BoosterDefinitionSO"))
        {
            var asset=AssetDatabase.LoadAssetAtPath<BoosterDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid)); Undo.RecordObject(asset,"Rebalance booster");
            var so=new SerializedObject(asset); var capacity=so.FindProperty("energyCapacity");capacity.floatValue=Mathf.Max(capacity.floatValue,1000f);
            var drain=so.FindProperty("energyPerSecond");drain.floatValue=Mathf.Max(drain.floatValue,60f);
            var strain=so.FindProperty("capacityStrainPerSecond");if(strain.floatValue<=0f)strain.floatValue=8f;
            so.ApplyModifiedProperties();EditorUtility.SetDirty(asset);
        }
        foreach(string guid in AssetDatabase.FindAssets("t:ShieldDefinitionSO"))
        {
            var asset=AssetDatabase.LoadAssetAtPath<ShieldDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));Undo.RecordObject(asset,"Rebalance shield");var so=new SerializedObject(asset);
            var rate=so.FindProperty("rechargePerSecond");var capacity=so.FindProperty("capacity");rate.floatValue=Mathf.Max(rate.floatValue,capacity.floatValue*0.4f);
            so.FindProperty("rechargeDelay").floatValue=2f;so.ApplyModifiedProperties();EditorUtility.SetDirty(asset);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Applied starting balance: booster capacity >= 1000, boost cost >= 60/s, strain 8/s when unset, shield recharge >= 40%/s after 2s. Tune weapon energy costs on definitions and strip recharge rates for track length.");
    }
}
