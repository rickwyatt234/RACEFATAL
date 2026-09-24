using System;
using RaceFatal.Content;
using RaceFatal.Content.Career;
using RaceFatal.Content.Equipment;
using RaceFatal.Content.Vehicles;
using RaceFatal.Shared;
using UnityEditor;
using UnityEngine;

public static class CareerResearchDevelopmentTools
{
    private const string Folder = "Assets/Game/ResearchDevelopment";

    [MenuItem("RACE//FATAL/Career/Create Research Development Content")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var catalog = Selection.activeObject as GameContentCatalogSO;
        if (catalog == null)
        {
            EditorUtility.DisplayDialog("Research Development Content",
                "Select the GameContentCatalog asset used by Bootstrap, then run this tool again.", "OK");
            return;
        }
        EngineDefinitionSO engine = null;
        ShieldDefinitionSO shield = null;
        foreach (var item in catalog.EngineDefinitions)
            if (item != null && !AssetDatabase.GetAssetPath(item).StartsWith(Folder + "/", StringComparison.Ordinal)) { engine = item; break; }
        foreach (var item in catalog.EquipmentDefinitions)
            if (item is ShieldDefinitionSO candidate && !AssetDatabase.GetAssetPath(item).StartsWith(Folder + "/", StringComparison.Ordinal)) { shield = candidate; break; }
        if (engine == null || shield == null)
        {
            EditorUtility.DisplayDialog("Research Development Content", "The selected catalog needs an engine and a shield to copy.", "OK");
            return;
        }
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Game", "ResearchDevelopment");
        var foundation = Technology("DEV_TECH_ENGINE_01", "Engine Calibration", ResearchField.EngineTechnology, 50, null, 0);
        var propulsion = Technology("DEV_TECH_ENGINE_02", "Advanced Propulsion", ResearchField.EngineTechnology, 100, foundation.Id, 1);
        var defense = Technology("DEV_TECH_SHIELD_01", "Shield Modulation", ResearchField.ShieldTechnology, 75, null, 2);
        var engineCopy = CopyComponent(engine, "DEV_ENGINE_RESEARCH", "Development Research Engine", propulsion.Id, 150);
        var shieldCopy = CopyComponent(shield, "DEV_SHIELD_RESEARCH", "Development Research Shield", defense.Id, 100);
        Undo.RecordObject(catalog, "Register research development content");
        var serialized = new SerializedObject(catalog);
        Append(serialized, "technologyDefinitions", foundation);
        Append(serialized, "technologyDefinitions", propulsion);
        Append(serialized, "technologyDefinitions", defense);
        Append(serialized, "engineDefinitions", engineCopy);
        Append(serialized, "equipmentDefinitions", shieldCopy);
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("Research development content registered. Existing assets are preserved on repeat runs. Restart from Bootstrap to reload the catalog.");
    }

    private static TechnologyDefinitionSO Technology(string id, string label, ResearchField field, int cost, string prerequisite, int order)
    {
        string path = Folder + "/" + id + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<TechnologyDefinitionSO>(path);
        if (existing != null) return existing;
        var asset = ScriptableObject.CreateInstance<TechnologyDefinitionSO>();
        var serialized = new SerializedObject(asset);
        serialized.FindProperty("id").stringValue = id;
        serialized.FindProperty("displayName").stringValue = label;
        serialized.FindProperty("description").stringValue = "Development technology for testing career progression. Costs and component stats are placeholders.";
        serialized.FindProperty("researchField").intValue = (int)field;
        serialized.FindProperty("researchCost").intValue = cost;
        serialized.FindProperty("displayOrder").intValue = order;
        var prerequisites = serialized.FindProperty("prerequisiteTechnologyIds");
        prerequisites.arraySize = prerequisite == null ? 0 : 1;
        if (prerequisite != null) prerequisites.GetArrayElementAtIndex(0).stringValue = prerequisite;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static T CopyComponent<T>(T source, string id, string label, string technology, int credits) where T : ScriptableObject
    {
        string path = Folder + "/" + id + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        T copy = UnityEngine.Object.Instantiate(source);
        copy.name = id;
        var serialized = new SerializedObject(copy);
        serialized.FindProperty("id").stringValue = id;
        serialized.FindProperty("displayName").stringValue = label;
        serialized.FindProperty("requiredTechnologyId").stringValue = technology;
        serialized.FindProperty("creditCost").intValue = credits;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(copy, path);
        return copy;
    }

    private static void Append(SerializedObject serialized, string field, UnityEngine.Object value)
    {
        var list = serialized.FindProperty(field);
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == value) return;
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = value;
    }
}
