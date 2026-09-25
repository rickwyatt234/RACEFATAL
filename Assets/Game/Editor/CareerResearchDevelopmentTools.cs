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
        var progression = AssetDatabase.LoadAssetAtPath<ResearchProgressionSO>(Folder + "/ResearchProgression.asset");
        if (progression == null) { progression = ScriptableObject.CreateInstance<ResearchProgressionSO>(); AssetDatabase.CreateAsset(progression, Folder + "/ResearchProgression.asset"); }
        var layout = AssetDatabase.LoadAssetAtPath<ResearchTreeLayoutSO>(Folder + "/ResearchLayout.asset");
        if (layout == null) { layout = ScriptableObject.CreateInstance<ResearchTreeLayoutSO>(); AssetDatabase.CreateAsset(layout, Folder + "/ResearchLayout.asset"); }
        var foundation = Technology("DEV_TECH_ENGINE_01", "Engine Calibration", ResearchField.EngineTechnology, 50, null, 0);
        var propulsion = Technology("DEV_TECH_ENGINE_02", "Advanced Propulsion", ResearchField.EngineTechnology, 125, foundation.Id, 1, 2);
        var defense = Technology("DEV_TECH_SHIELD_01", "Shield Modulation", ResearchField.ShieldTechnology, 50, null, 2);
        var advancedDefense = Technology("DEV_TECH_SHIELD_02", "Adaptive Shielding", ResearchField.ShieldTechnology, 125, defense.Id, 3, 2);
        var hybrid = Technology("DEV_TECH_HYBRID_03", "Integrated Combat Systems", ResearchField.EnergyTechnology, 250, propulsion.Id, 4, 3, advancedDefense.Id);
        var engineer = Researcher("DEV_RESEARCHER_ENGINEER", "Contract Engineer", 2000, 20, 5);
        var lab = Researcher("DEV_RESEARCHER_LAB", "Research Lab", 5000, 45, 5);
        var hybridEngine = CopyComponent(engine, "DEV_ENGINE_HYBRID", "Integrated Engine", hybrid.Id, 400);
        var hybridShield = CopyComponent(shield, "DEV_SHIELD_HYBRID", "Integrated Shield", hybrid.Id, 400);
        var engineCopy = CopyComponent(engine, "DEV_ENGINE_RESEARCH", "Development Research Engine", propulsion.Id, 150);
        var shieldCopy = CopyComponent(shield, "DEV_SHIELD_RESEARCH", "Development Research Shield", defense.Id, 100);
        Undo.RecordObject(catalog, "Register research development content");
        var serialized = new SerializedObject(catalog);
        if (catalog.ResearchProgression == null) serialized.FindProperty("researchProgression").objectReferenceValue = progression;
        if (catalog.ResearchTreeLayout == null) serialized.FindProperty("researchTreeLayout").objectReferenceValue = layout;
        Append(serialized, "researcherDefinitions", engineer);
        Append(serialized, "researcherDefinitions", lab);
        Append(serialized, "engineDefinitions", hybridEngine);
        Append(serialized, "equipmentDefinitions", hybridShield);
        Append(serialized, "technologyDefinitions", advancedDefense);
        Append(serialized, "technologyDefinitions", hybrid);
        Append(serialized, "technologyDefinitions", foundation);
        Append(serialized, "technologyDefinitions", propulsion);
        Append(serialized, "technologyDefinitions", defense);
        Append(serialized, "engineDefinitions", engineCopy);
        Append(serialized, "equipmentDefinitions", shieldCopy);
        serialized.ApplyModifiedProperties();
        AddLayout(catalog.ResearchTreeLayout, foundation, new Vector2(0, 0));
        AddLayout(catalog.ResearchTreeLayout, propulsion, new Vector2(0, 190));
        AddLayout(catalog.ResearchTreeLayout, defense, new Vector2(400, 0));
        AddLayout(catalog.ResearchTreeLayout, advancedDefense, new Vector2(400, 190));
        AddLayout(catalog.ResearchTreeLayout, hybrid, new Vector2(200, 380));
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("Research development content registered. Existing assets are preserved on repeat runs. Restart from Bootstrap to reload the catalog.");
    }

    private static TechnologyDefinitionSO Technology(string id, string label, ResearchField field, int cost, string prerequisite, int order, int tier = 1, string additionalPrerequisite = null)
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
        serialized.FindProperty("tier").intValue = tier;
        serialized.FindProperty("overrideTierCost").boolValue = false;
        serialized.FindProperty("displayOrder").intValue = order;
        var prerequisites = serialized.FindProperty("prerequisiteTechnologyIds");
        prerequisites.arraySize = prerequisite == null ? 0 : 1;
        if (prerequisite != null) prerequisites.GetArrayElementAtIndex(0).stringValue = prerequisite;
        if (additionalPrerequisite != null)
        {
            int index = prerequisites.arraySize++;
            prerequisites.GetArrayElementAtIndex(index).stringValue = additionalPrerequisite;
        }
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

    private static ResearcherDefinitionSO Researcher(string id, string name, int cost, int output, int duration)
    {
        string path = Folder + "/" + id + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<ResearcherDefinitionSO>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<ResearcherDefinitionSO>();
        var serialized = new SerializedObject(asset);
        serialized.FindProperty("id").stringValue = id;
        serialized.FindProperty("displayName").stringValue = name;
        serialized.FindProperty("creditCost").intValue = cost;
        serialized.FindProperty("pointsPerRace").intValue = output;
        serialized.FindProperty("duration").intValue = duration;
        serialized.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(asset, path); return asset;
    }
    private static void AddLayout(ResearchTreeLayoutSO layout, TechnologyDefinitionSO technology, Vector2 position)
    {
        foreach (var node in layout.Nodes) if (node?.technology == technology) return;
        var serialized = new SerializedObject(layout); var nodes = serialized.FindProperty("nodes");
        int index = nodes.arraySize++;
        nodes.GetArrayElementAtIndex(index).FindPropertyRelative("technology").objectReferenceValue = technology;
        nodes.GetArrayElementAtIndex(index).FindPropertyRelative("position").vector2Value = position;
        serialized.ApplyModifiedProperties();
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
