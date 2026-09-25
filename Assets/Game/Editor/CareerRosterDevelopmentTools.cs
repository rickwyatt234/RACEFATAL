using RaceFatal.Content;
using RaceFatal.Content.Career;
using RaceFatal.Career;
using UnityEditor;
using UnityEngine;

public static class CareerRosterDevelopmentTools
{
    private const string Folder = "Assets/Game/RosterDevelopment";
    [MenuItem("RACE//FATAL/Career/Create Roster Development Perks")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var catalog = Selection.activeObject as GameContentCatalogSO;
        if (catalog == null)
        {
            EditorUtility.DisplayDialog("Roster Perks", "Select the GameContentCatalog used by Bootstrap, then run again.", "OK");
            return;
        }
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Game", "RosterDevelopment");
        var serialized = new SerializedObject(catalog);
        var perks = serialized.FindProperty("racerPerkDefinitions");
        Register(perks, Perk("DEV_PERK_RESERVE", "Reserve Cell", "Adds 20 energy capacity to this racer's bike each race.", 50, RacerPerkEffect.EnergyCapacity, 20));
        Register(perks, Perk("DEV_PERK_APEX", "Apex Reader", "Raises this AI racer's racing pace by 0.08.", 50, RacerPerkEffect.Pace, .08f));
        Register(perks, Perk("DEV_PERK_EVASION", "Evasion Protocol", "Raises this AI racer's defensive skill by 0.15.", 60, RacerPerkEffect.Defense, .15f));
        Register(perks, Perk("DEV_PERK_PREDATOR", "Predator Routine", "Raises this AI racer's weapon aggression by 0.15.", 60, RacerPerkEffect.WeaponAggression, .15f));
        serialized.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("Four sample perks registered. Restart from Bootstrap to load them. Existing perk assets were preserved.");
    }
    private static RacerPerkDefinitionSO Perk(string id, string name, string description, int cost,
        RacerPerkEffect effect, float strength)
    {
        string path = Folder + "/" + id + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<RacerPerkDefinitionSO>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<RacerPerkDefinitionSO>();
        var serialized = new SerializedObject(asset);
        serialized.FindProperty("id").stringValue = id;
        serialized.FindProperty("displayName").stringValue = name;
        serialized.FindProperty("description").stringValue = description;
        serialized.FindProperty("fameCost").intValue = cost;
        serialized.FindProperty("effect").intValue = (int)effect;
        serialized.FindProperty("strength").floatValue = strength;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
    private static void Register(SerializedProperty list, RacerPerkDefinitionSO asset)
    {
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == asset) return;
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = asset;
    }
}
