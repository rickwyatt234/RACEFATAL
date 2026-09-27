using System.Linq;
using RaceFatal.Career;
using RaceFatal.Content;
using RaceFatal.Content.Career;
using RaceFatal.Content.Racing;
using UnityEditor;
using UnityEngine;

public static class DeathmatchDevelopmentTools
{
    [MenuItem("RACE//FATAL/Career/Create Deathmatch Development Events")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var catalog = Selection.activeObject as GameContentCatalogSO;
        var template = catalog?.RaceDefinitions.FirstOrDefault(r => r != null);
        if (template == null)
        { EditorUtility.DisplayDialog("Deathmatch", "Select the Bootstrap GameContentCatalog with a configured circuit race.", "OK"); return; }
        const string folder = "Assets/Game/DeathmatchDevelopment";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Game", "DeathmatchDevelopment");
        var content = new SerializedObject(catalog);
        for (int i = 0; i < 2; i++)
        {
            string id = i == 0 ? "DEV_DM_TEAM" : "DEV_DM_INDIVIDUAL";
            string name = i == 0 ? "Team Survival" : "Last Racer Standing";
            string racePath = folder + "/" + id + "_RACE.asset";
            var race = AssetDatabase.LoadAssetAtPath<RaceDefinitionSO>(racePath);
            if (race == null)
            {
                race = Object.Instantiate(template);
                var data = new SerializedObject(race);
                data.FindProperty("id").stringValue = id + "_RACE";
                data.FindProperty("displayName").stringValue = name;
                data.FindProperty("deathmatch").boolValue = true;
                data.FindProperty("victoryMode").intValue = i == 0 ? 1 : 0;
                data.FindProperty("allowedWinners").intValue = 1;
                data.FindProperty("timeLimitSeconds").floatValue = 300;
                data.FindProperty("minimumSpeedKph").floatValue = 30;
                data.FindProperty("startGraceSeconds").floatValue = 15;
                data.FindProperty("belowSpeedGraceSeconds").floatValue = 8;
                data.FindProperty("shareTimeoutTies").boolValue = false;
                data.FindProperty("entrantCount").intValue = 12;
                data.FindProperty("teamSize").intValue = 2;
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(race, racePath);
            }
            string eventPath = folder + "/" + id + ".asset";
            var entry = AssetDatabase.LoadAssetAtPath<CareerEventDefinitionSO>(eventPath);
            if (entry == null)
            {
                entry = ScriptableObject.CreateInstance<CareerEventDefinitionSO>();
                var data = new SerializedObject(entry);
                data.FindProperty("id").stringValue = id;
                data.FindProperty("displayName").stringValue = name;
                data.FindProperty("description").stringValue = "Survive while circulating. Falling below the speed limit causes disqualification, not permanent death.";
                data.FindProperty("kind").intValue = (int)CareerEventKind.Deathmatch;
                var rounds = data.FindProperty("rounds"); rounds.arraySize = 1;
                rounds.GetArrayElementAtIndex(0).objectReferenceValue = race;
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(entry, eventPath);
            }
            Register(content.FindProperty("raceDefinitions"), race);
            Register(content.FindProperty("careerEventDefinitions"), entry);
        }
        content.ApplyModifiedProperties(); AssetDatabase.SaveAssets();
        Debug.Log("Deathmatch assets registered; existing assets preserved. Tune race rules and event fees/Fame/payouts in the Inspector. Restart Bootstrap, then advance a week for a new draw.");
    }
    private static void Register(SerializedProperty list, Object asset)
    {
        for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == asset) return;
        list.arraySize++; list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = asset;
    }
}
