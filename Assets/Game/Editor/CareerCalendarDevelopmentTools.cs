using System.Linq;
using RaceFatal.Career;
using RaceFatal.Content;
using RaceFatal.Content.Career;
using RaceFatal.Content.Racing;
using UnityEditor;
using UnityEngine;

public static class CareerCalendarDevelopmentTools
{
    [MenuItem("RACE//FATAL/Career/Create Calendar Development Events")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var catalog = Selection.activeObject as GameContentCatalogSO;
        var races = catalog?.RaceDefinitions.Where(r => r != null).ToList();
        if (catalog == null || races == null || races.Count == 0)
        { EditorUtility.DisplayDialog("Calendar Events", "Select the Bootstrap GameContentCatalog with at least one race.", "OK"); return; }
        const string folder = "Assets/Game/CalendarDevelopment";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Game", "CalendarDevelopment");
        var serialized = new SerializedObject(catalog);
        var list = serialized.FindProperty("careerEventDefinitions");
        for (int i = 0; i < 6; i++)
        {
            string id = "DEV_EVENT_" + (i + 1);
            bool championship = i == 5;
            var asset = AssetDatabase.LoadAssetAtPath<CareerEventDefinitionSO>(folder + "/" + id + ".asset");
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CareerEventDefinitionSO>();
                var data = new SerializedObject(asset);
                data.FindProperty("id").stringValue = id;
                data.FindProperty("displayName").stringValue = championship ? "Neon Circuit Championship" : "Open Circuit " + (i + 1);
                data.FindProperty("description").stringValue = championship
                    ? "Three development rounds. Replace repeated tracks with authored rounds sharing the same class and grid size."
                    : "Development event. Tune the entry fee, Fame threshold and position payouts in this asset.";
                data.FindProperty("kind").intValue = (int)(championship ? CareerEventKind.Championship : CareerEventKind.Race);
                data.FindProperty("requiredFame").intValue = i == 4 ? 250 : 0;
                data.FindProperty("entryFee").intValue = i == 0 ? 0 : championship ? 2000 : 500;
                var rounds = data.FindProperty("rounds"); rounds.arraySize = championship ? 3 : 1;
                RaceDefinitionSO race = races[i % races.Count];
                for (int round = 0; round < rounds.arraySize; round++) rounds.GetArrayElementAtIndex(round).objectReferenceValue = race;
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(asset, folder + "/" + id + ".asset");
            }
            bool registered = false;
            for (int index = 0; index < list.arraySize; index++)
                if (list.GetArrayElementAtIndex(index).objectReferenceValue == asset) registered = true;
            if (!registered) { list.arraySize++; list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = asset; }
        }
        serialized.ApplyModifiedProperties(); AssetDatabase.SaveAssets();
        Debug.Log("Six calendar events registered; existing assets preserved. Restart from Bootstrap. Skip a week to replace an existing saved draw.");
    }
}
