using System;
using System.Collections.Generic;
using System.Linq;
using RaceFatal.Content;
using RaceFatal.Content.Career;
using UnityEditor;
using UnityEngine;

// Authoring only: existing campaign saves are never edited.
[CustomEditor(typeof(GameContentCatalogSO))]
public sealed class GameContentCatalogEditor : Editor
{
    private bool showRandomizer;
    private bool includeProjectRacers = true;
    private bool distributeAll = true;
    private int racersPerTeam = 2;
    private int seed = 1;
    private readonly List<RacerDefinitionSO> excluded = new List<RacerDefinitionSO>();
    private Plan preview;
    private string error;
    private Vector2 scroll;

    private sealed class Plan
    {
        public OpponentTeamDefinitionSO[] Teams;
        public List<RacerDefinitionSO>[] Rosters;
        public int Available;
        public int Assigned;
        public string Signature => string.Join("|", Teams.Select((t, i) =>
            t.GetInstanceID() + ":" + string.Join(",", Rosters[i].Select(r => r.GetInstanceID()))));
    }

    public override void OnInspectorGUI()
    {
        if (DrawDefaultInspector()) preview = null;
        EditorGUILayout.Space();
        showRandomizer = EditorGUILayout.Foldout(showRandomizer, "Randomize Opponent Team Rosters", true);
        if (!showRandomizer) return;

        EditorGUILayout.HelpBox("Replaces racer lists on all opponent teams in this catalog. " +
            "Assignments are unique and distributed evenly. Team philosophies, builds and racer personalities stay as authored. " +
            "Changes affect new campaigns, not existing saves.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            EditorGUI.BeginChangeCheck();
            includeProjectRacers = EditorGUILayout.Toggle("Use all project racers", includeProjectRacers);
            EditorGUILayout.LabelField(includeProjectRacers
                ? "Assigned project racers will also be registered in this catalog."
                : "Only racers already registered in this catalog are eligible.", EditorStyles.wordWrappedLabel);
            distributeAll = EditorGUILayout.Toggle("Distribute all eligible racers", distributeAll);
            if (!distributeAll) racersPerTeam = Mathf.Max(1, EditorGUILayout.IntField("Racers per team", racersPerTeam));
            seed = EditorGUILayout.IntField("Shuffle seed", seed);
            EditorGUILayout.LabelField("Starting partners are automatically excluded.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Additional exclusions", EditorStyles.boldLabel);
            for (int i = 0; i < excluded.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                excluded[i] = (RacerDefinitionSO)EditorGUILayout.ObjectField(excluded[i], typeof(RacerDefinitionSO), false);
                if (GUILayout.Button("Remove", GUILayout.Width(65))) { excluded.RemoveAt(i); i--; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Add exclusion")) excluded.Add(null);
            if (EditorGUI.EndChangeCheck()) { preview = null; error = null; }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview assignments")) GeneratePreview();
            if (GUILayout.Button("New shuffle"))
            {
                seed = Guid.NewGuid().GetHashCode();
                GeneratePreview();
            }
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (preview == null) return;

            EditorGUILayout.LabelField($"{preview.Assigned} assigned / {preview.Available} eligible / {preview.Teams.Length} teams");
            if (preview.Assigned < preview.Available)
                EditorGUILayout.HelpBox("Remaining racers are unassigned. Applying replaces the previous rosters.", MessageType.Info);
            EditorGUILayout.HelpBox("This does not create bikes. Ensure each team has enough starting builds for its intended race entrants.", MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(320));
            for (int i = 0; i < preview.Teams.Length; i++)
            {
                EditorGUILayout.ObjectField(preview.Teams[i], typeof(OpponentTeamDefinitionSO), false);
                foreach (var racer in preview.Rosters[i])
                    EditorGUILayout.LabelField("    " + racer.CreateDefinition().DisplayName + "  [" + racer.Id + "]");
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Apply preview to team assets (Undo supported)")) ApplyPreview();
        }
    }

    private void GeneratePreview()
    {
        try { preview = BuildPlan(); error = null; }
        catch (Exception e) { preview = null; error = e.Message; }
    }

    private Plan BuildPlan()
    {
        var catalog = (GameContentCatalogSO)target;
        var teams = catalog.OpponentTeamDefinitions.Where(t => t != null).Distinct()
            .OrderBy(t => AssetDatabase.GetAssetPath(t), StringComparer.Ordinal).ToArray();
        if (teams.Length == 0) throw new InvalidOperationException("Add opponent teams to this catalog first.");
        ValidateIds(teams.Select(t => t.Id), "Opponent team");
        foreach (var team in teams)
            if (!AssetDatabase.IsOpenForEdit(team))
                throw new InvalidOperationException($"Team asset '{team.name}' is not editable.");
        if (!AssetDatabase.IsOpenForEdit(catalog))
            throw new InvalidOperationException("The catalog asset is not editable.");

        var reservedIds = new HashSet<string>(excluded.Where(r => r != null).Select(r => r.Id));
        foreach (var defaults in FindAssets<NewCampaignDefaultsSO>())
            if (defaults.Partner != null) reservedIds.Add(defaults.Partner.Id);

        // Duplicate IDs are invalid even if represented by different asset files.
        var registered = catalog.RacerDefinitions.Where(r => r != null).Distinct().ToArray();
        ValidateIds(registered.Select(r => r.Id), "Catalog racer");
        var pool = (includeProjectRacers ? FindAssets<RacerDefinitionSO>() : registered)
            .Where(r => !reservedIds.Contains(r.Id)).Distinct()
            .OrderBy(r => AssetDatabase.GetAssetPath(r), StringComparer.Ordinal).ToList();
        ValidateIds(pool.Select(r => r.Id), "Eligible racer");
        foreach (var racer in pool)
            if (registered.Any(r => r.Id == racer.Id && r != racer))
                throw new InvalidOperationException($"Racer ID '{racer.Id}' conflicts with a catalog asset.");
        if (pool.Count < teams.Length)
            throw new InvalidOperationException("Not enough eligible racers to give every team at least one racer.");
        long required = distributeAll ? pool.Count : (long)teams.Length * racersPerTeam;
        if (required > pool.Count)
            throw new InvalidOperationException($"Need {required} unique racers, but only {pool.Count} are eligible.");

        var random = new System.Random(seed);
        Shuffle(pool, random);
        var teamOrder = Enumerable.Range(0, teams.Length).ToList();
        Shuffle(teamOrder, random);
        var plan = new Plan
        {
            Teams = teams,
            Rosters = teams.Select(t => new List<RacerDefinitionSO>()).ToArray(),
            Available = pool.Count,
            Assigned = (int)required
        };
        for (int i = 0; i < plan.Assigned; i++)
            plan.Rosters[teamOrder[i % teams.Length]].Add(pool[i]);
        return plan;
    }

    private void ApplyPreview()
    {
        try
        {
            // Recheck source content and editability before writing a previously displayed preview.
            var fresh = BuildPlan();
            if (preview == null || fresh.Signature != preview.Signature)
                throw new InvalidOperationException("Source content changed. Generate a new preview before applying.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Randomize Opponent Team Rosters");
            var catalog = (GameContentCatalogSO)target;
            try
            {
                foreach (var team in fresh.Teams) Undo.RegisterCompleteObjectUndo(team, "Randomize Opponent Team Rosters");
                Undo.RegisterCompleteObjectUndo(catalog, "Register Assigned Racers");
                for (int i = 0; i < fresh.Teams.Length; i++)
                {
                    var data = new SerializedObject(fresh.Teams[i]);
                    var racers = data.FindProperty("racers");
                    racers.arraySize = fresh.Rosters[i].Count;
                    for (int j = 0; j < fresh.Rosters[i].Count; j++)
                        racers.GetArrayElementAtIndex(j).objectReferenceValue = fresh.Rosters[i][j];
                    data.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(fresh.Teams[i]);
                }
                var catalogData = new SerializedObject(catalog);
                var registry = catalogData.FindProperty("racerDefinitions");
                var existing = new HashSet<RacerDefinitionSO>(catalog.RacerDefinitions);
                foreach (var racer in fresh.Rosters.SelectMany(roster => roster))
                {
                    if (!existing.Add(racer)) continue;
                    int index = registry.arraySize++;
                    registry.GetArrayElementAtIndex(index).objectReferenceValue = racer;
                }
                catalogData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
                Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
            // Leave dirty assets to Unity's normal Save Project workflow, including any subsequent Undo.
            serializedObject.Update();
            preview = null;
            error = null;
            Debug.Log($"Assigned {fresh.Assigned} racers to {fresh.Teams.Length} teams. Save Project to persist; Undo restores the previous rosters.", catalog);
        }
        catch (Exception e) { error = e.Message; }
    }

    private static T[] FindAssets<T>() where T : UnityEngine.Object
    {
        return AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(asset => asset != null).ToArray();
    }

    private static void ValidateIds(IEnumerable<string> ids, string label)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                throw new InvalidOperationException($"{label} IDs must be nonempty and unique. Check '{id}'.");
    }

    private static void Shuffle<T>(IList<T> values, System.Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T value = values[i]; values[i] = values[j]; values[j] = value;
        }
    }
}
