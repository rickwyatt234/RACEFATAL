using System;
using System.Collections.Generic;
using System.Linq;
using RaceFatal.Content;
using RaceFatal.Content.Career;
using UnityEditor;
using UnityEngine;

public class ResearchTreeEditor : EditorWindow
{
    private GameContentCatalogSO catalog;
    private TechnologyDefinitionSO selected;
    private TechnologyDefinitionSO linkSource;
    private Vector2 pan = new Vector2(160, 520), inspectorScroll;
    private float zoom = 0.65f;
    private int dragged = -1;
    private string validation;
    private ResearchTreeLayoutSO Layout => catalog != null ? catalog.ResearchTreeLayout : null;

    [MenuItem("RACE//FATAL/Research/Tech Tree Editor")]
    public static void Open() => GetWindow<ResearchTreeEditor>("Research Tree");

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            catalog = (GameContentCatalogSO)EditorGUILayout.ObjectField(catalog, typeof(GameContentCatalogSO), false, GUILayout.Width(280));
            if (GUILayout.Button("Use Selected Catalog", EditorStyles.toolbarButton)) catalog = Selection.activeObject as GameContentCatalogSO;
            if (GUILayout.Button("Recenter", EditorStyles.toolbarButton)) { pan = new Vector2(160, 520); zoom = 0.65f; }
            if (GUILayout.Button("Validate", EditorStyles.toolbarButton)) validation = ValidateCatalog(catalog);
            if (GUILayout.Button("Save", EditorStyles.toolbarButton)) AssetDatabase.SaveAssets();
        }
        if (catalog == null) { EditorGUILayout.HelpBox("Choose the catalog used by Bootstrap.", MessageType.Info); return; }
        if (Layout == null)
        {
            EditorGUILayout.HelpBox("Assign a Research Tree Layout on the catalog or create one here.", MessageType.Info);
            if (GUILayout.Button("Create Layout")) CreateLayout();
            return;
        }
        Rect canvas = new Rect(0, 42, Mathf.Max(100, position.width - 340), Mathf.Max(100, position.height - 42));
        EditorGUI.DrawRect(canvas, new Color(0.035f, 0.055f, 0.075f));
        GUI.Label(new Rect(10, 23, 800, 20), "Drag nodes • Middle-drag to pan • Wheel to zoom • Connect: select prerequisite, then click dependent");
        GUI.BeginGroup(canvas);
        Handles.BeginGUI();
        foreach (var node in Layout.Nodes)
        {
            if (node?.technology == null) continue;
            foreach (string prerequisite in node.technology.Prerequisites)
            {
                var source = Layout.Nodes.FirstOrDefault(n => n?.technology != null && n.technology.Id == prerequisite);
                if (source == null) continue;
                Vector2 a = NodeRect(source).center, b = NodeRect(node).center;
                Handles.DrawBezier(a, b, a + Vector2.up * 50, b + Vector2.down * 50, Color.cyan, null, 2);
            }
        }
        Handles.EndGUI();
        for (int i = 0; i < Layout.Nodes.Count; i++)
        {
            var node = Layout.Nodes[i];
            if (node?.technology == null) continue;
            Rect rect = NodeRect(node);
            EditorGUI.DrawRect(rect, node.technology == selected ? new Color(.1f, .4f, .45f) : new Color(.12f, .19f, .23f));
            string cost;
            try { cost = node.technology.Cost(catalog.ResearchProgression) + " RP"; } catch { cost = "INVALID COST"; }
            GUI.Label(rect, $"{node.technology.DisplayName}\nT{node.technology.Tier} • {cost}", EditorStyles.whiteLabel);
        }
        GUI.EndGroup();
        HandleCanvas(canvas);
        GUILayout.BeginArea(new Rect(canvas.xMax + 8, 45, 324, position.height - 50));
        inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
        var catalogProperties = new SerializedObject(catalog);
        EditorGUILayout.PropertyField(catalogProperties.FindProperty("researchProgression"));
        EditorGUILayout.PropertyField(catalogProperties.FindProperty("researchTreeLayout"));
        catalogProperties.ApplyModifiedProperties();
        if (catalog.ResearchProgression != null)
        {
            var costs = new SerializedObject(catalog.ResearchProgression);
            EditorGUILayout.PropertyField(costs.FindProperty("tierCosts"), true);
            costs.ApplyModifiedProperties();
        }
        if (GUILayout.Button("Create Technology Node")) CreateTechnology();
        if (GUILayout.Button("Add Selected Technology Asset")) AddNode(Selection.activeObject as TechnologyDefinitionSO);
        if (GUILayout.Button("Add Missing Catalog Nodes"))
            foreach (var technology in catalog.TechnologyDefinitions) AddNode(technology);
        if (selected != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Node gameplay properties", EditorStyles.boldLabel);
            var serialized = new SerializedObject(selected);
            var iterator = serialized.GetIterator();
            bool children = true;
            while (iterator.NextVisible(children))
            {
                children = false;
                if (iterator.name != "m_Script") EditorGUILayout.PropertyField(iterator, true);
            }
            serialized.ApplyModifiedProperties();
            if (GUILayout.Button("Connect FROM this prerequisite")) linkSource = selected;
            if (linkSource != null && GUILayout.Button("Cancel Connection")) linkSource = null;
            if (GUILayout.Button("Remove Node From Layout")) RemoveSelected();
            EditorGUILayout.HelpBox("Remove Node affects layout only. Delete prerequisite IDs in the list above to disconnect edges. Edit the catalog to unregister content.", MessageType.Info);
            EditorGUILayout.LabelField("Shop unlocks", EditorStyles.boldLabel);
            foreach (var engine in catalog.EngineDefinitions)
                if (engine != null) ShowUnlock(new SerializedObject(engine).FindProperty("requiredTechnologyId").stringValue, engine.name);
            foreach (var chassis in catalog.ChassisDefinitions)
                if (chassis != null) ShowUnlock(new SerializedObject(chassis).FindProperty("requiredTechnologyId").stringValue, chassis.name);
            foreach (var equipment in catalog.EquipmentDefinitions)
                if (equipment != null) ShowUnlock(new SerializedObject(equipment).FindProperty("requiredTechnologyId").stringValue, equipment.name);
        }
        if (!string.IsNullOrEmpty(validation)) EditorGUILayout.HelpBox(validation, MessageType.Info);
        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    private void ShowUnlock(string id, string label) { if (selected != null && id == selected.Id) EditorGUILayout.LabelField(label); }
    private Rect NodeRect(ResearchTreeLayoutSO.Node node) => new Rect(pan + new Vector2(node.position.x, -node.position.y) * zoom, new Vector2(260, 92) * zoom);
    private void HandleCanvas(Rect canvas)
    {
        Event ev = Event.current;
        Vector2 local = ev.mousePosition - canvas.position;
        if (canvas.Contains(ev.mousePosition) && ev.type == EventType.ScrollWheel)
        {
            float next = Mathf.Clamp(zoom * Mathf.Pow(1.1f, -ev.delta.y), .25f, 1.5f);
            pan = local - (local - pan) * (next / zoom); zoom = next; ev.Use();
        }
        if (ev.type == EventType.MouseDown && ev.button == 0 && canvas.Contains(ev.mousePosition))
        {
            for (int i = Layout.Nodes.Count - 1; i >= 0; i--)
            {
                var node = Layout.Nodes[i];
                if (node?.technology == null || !NodeRect(node).Contains(local)) continue;
                selected = node.technology;
                if (linkSource != null) { Connect(linkSource, selected); linkSource = null; }
                else { dragged = i; Undo.RecordObject(Layout, "Move research node"); }
                ev.Use(); break;
            }
        }
        if (ev.type == EventType.MouseDrag)
        {
            if (dragged >= 0 && dragged < Layout.Nodes.Count)
            {
                Layout.Nodes[dragged].position += new Vector2(ev.delta.x, -ev.delta.y) / zoom;
                EditorUtility.SetDirty(Layout); ev.Use();
            }
            else if (ev.button == 2 && canvas.Contains(ev.mousePosition)) { pan += ev.delta; ev.Use(); }
        }
        if (ev.rawType == EventType.MouseUp) dragged = -1;
        if (ev.type == EventType.Used || ev.rawType == EventType.MouseUp) Repaint();
    }
    private void Connect(TechnologyDefinitionSO source, TechnologyDefinitionSO target)
    {
        if (source == target || target.Prerequisites.Contains(source.Id)) return;
        // Connecting source -> target is cyclic if source already depends on target.
        if (DependsOn(source, target.Id, new HashSet<string>())) { validation = "Connection rejected: prerequisite cycle."; return; }
        Undo.RecordObject(target, "Connect technology prerequisite");
        var serialized = new SerializedObject(target);
        var list = serialized.FindProperty("prerequisiteTechnologyIds");
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = source.Id;
        serialized.ApplyModifiedProperties();
    }
    private bool DependsOn(TechnologyDefinitionSO source, string id, HashSet<string> seen)
    {
        if (!seen.Add(source.Id)) return false;
        foreach (string prerequisite in source.Prerequisites)
        {
            if (prerequisite == id) return true;
            var parent = catalog.TechnologyDefinitions.FirstOrDefault(t => t != null && t.Id == prerequisite);
            if (parent != null && DependsOn(parent, id, seen)) return true;
        }
        return false;
    }
    private void CreateLayout()
    {
        string path = EditorUtility.SaveFilePanelInProject("Research Layout", "ResearchTreeLayout", "asset", "Save layout asset");
        if (string.IsNullOrEmpty(path)) return;
        var asset = CreateInstance<ResearchTreeLayoutSO>(); AssetDatabase.CreateAsset(asset, path);
        var serialized = new SerializedObject(catalog); serialized.FindProperty("researchTreeLayout").objectReferenceValue = asset; serialized.ApplyModifiedProperties();
    }
    private void CreateTechnology()
    {
        string path = EditorUtility.SaveFilePanelInProject("Technology", "Technology", "asset", "Save technology asset");
        if (string.IsNullOrEmpty(path)) return;
        var asset = CreateInstance<TechnologyDefinitionSO>();
        var serialized = new SerializedObject(asset);
        serialized.FindProperty("id").stringValue = "TECH_" + Guid.NewGuid().ToString("N");
        serialized.FindProperty("displayName").stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
        serialized.FindProperty("overrideTierCost").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(asset, path); AddNode(asset); selected = asset;
    }
    private void AddNode(TechnologyDefinitionSO technology)
    {
        if (technology == null) return;
        var content = new SerializedObject(catalog);
        var definitions = content.FindProperty("technologyDefinitions");
        if (!catalog.TechnologyDefinitions.Contains(technology))
        {
            definitions.arraySize++; definitions.GetArrayElementAtIndex(definitions.arraySize - 1).objectReferenceValue = technology; content.ApplyModifiedProperties();
        }
        if (Layout.Nodes.Any(n => n?.technology == technology)) return;
        var serialized = new SerializedObject(Layout); var nodes = serialized.FindProperty("nodes");
        int index = nodes.arraySize++;
        var node = nodes.GetArrayElementAtIndex(index);
        node.FindPropertyRelative("technology").objectReferenceValue = technology;
        node.FindPropertyRelative("position").vector2Value = new Vector2((index % 4) * 300, (technology.Tier - 1) * 170);
        serialized.ApplyModifiedProperties();
    }
    private void RemoveSelected()
    {
        var serialized = new SerializedObject(Layout); var nodes = serialized.FindProperty("nodes");
        for (int i = nodes.arraySize - 1; i >= 0; i--)
            if (nodes.GetArrayElementAtIndex(i).FindPropertyRelative("technology").objectReferenceValue == selected) nodes.DeleteArrayElementAtIndex(i);
        serialized.ApplyModifiedProperties(); selected = null;
    }
    public static string ValidateCatalog(GameContentCatalogSO catalog)
    {
        if (catalog == null) return "Choose a catalog.";
        var messages = new List<string>();
        if (catalog.ResearchProgression == null) messages.Add("Missing tier progression configuration.");
        else if (catalog.ResearchProgression.ValidateCosts() != null) messages.Add(catalog.ResearchProgression.ValidateCosts());
        var ids = new HashSet<string>();
        var db = new RaceFatal.Data.GameDatabase();
        foreach (var technology in catalog.TechnologyDefinitions)
        {
            if (technology == null) { messages.Add("Null technology registration."); continue; }
            if (string.IsNullOrWhiteSpace(technology.Id) || !ids.Add(technology.Id)) { messages.Add("Missing/duplicate ID: " + technology.Id); continue; }
            try { db.AddTechnologyDefinition(technology.CreateDefinition(catalog.ResearchProgression)); }
            catch (Exception ex) { messages.Add(technology.name + ": " + ex.Message); }
        }
        var team = new RaceFatal.Career.TeamState("validation", "Validation", "", ""); team.AddResearchPoints(int.MaxValue);
        var service = new RaceFatal.Career.ResearchService(db);
        // Unlock prerequisites only after checking graph integrity via CanResearch's content validation.
        foreach (var technology in db.TechnologyDefinitions.Values)
        {
            var result = service.CanResearch(team, technology.Id);
            if (!result.IsSuccess && result.ErrorMessage.StartsWith("INVALID")) messages.Add(technology.DisplayName + ": " + result.ErrorMessage);
            foreach (string id in technology.PrerequisiteTechnologyIds)
            {
                var parent = db.GetTechnologyDefinition(id);
                if (parent != null && (technology.Tier <= parent.Tier || technology.ResearchCost <= parent.ResearchCost))
                    messages.Add(technology.DisplayName + ": tier and cost should increase above " + parent.DisplayName);
            }
        }
        var layoutIds = new HashSet<string>();
        if (catalog.ResearchTreeLayout == null) messages.Add("No layout assigned.");
        else foreach (var node in catalog.ResearchTreeLayout.Nodes)
        {
            if (node?.technology == null) { messages.Add("Empty layout node."); continue; }
            if (!layoutIds.Add(node.technology.Id)) messages.Add("Duplicate layout node: " + node.technology.Id);
            if (!catalog.TechnologyDefinitions.Contains(node.technology)) messages.Add("Unregistered layout technology: " + node.technology.Id);
        }
        foreach (string id in ids) if (!layoutIds.Contains(id)) messages.Add("Missing layout node: " + id);
        var researcherIds = new HashSet<string>();
        foreach (var researcher in catalog.ResearcherDefinitions)
        {
            try
            {
                if (researcher == null) { messages.Add("Null researcher registration."); continue; }
                var offer = researcher.CreateDefinition();
                if (!researcherIds.Add(offer.Id)) messages.Add("Duplicate researcher ID: " + offer.Id);
                if (offer.CreditCost < 0 || offer.PointsPerRace <= 0 || offer.Duration <= 0) messages.Add("Invalid researcher terms: " + offer.Id);
            }
            catch (Exception ex) { messages.Add("Researcher: " + ex.Message); }
        }
        var components = catalog.EngineDefinitions.Cast<UnityEngine.Object>().Concat(catalog.ChassisDefinitions).Concat(catalog.EquipmentDefinitions);
        foreach (var component in components)
        {
            if (component == null) continue;
            string required = new SerializedObject(component).FindProperty("requiredTechnologyId").stringValue;
            if (!string.IsNullOrWhiteSpace(required) && !ids.Contains(required)) messages.Add(component.name + ": unknown technology " + required);
        }
        return messages.Count == 0 ? "Research content and layout are valid." : string.Join("\n", messages);
    }
}
