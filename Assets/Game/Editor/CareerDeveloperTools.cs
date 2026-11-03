using System;
using System.Collections.Generic;
using RaceFatal.Infrastructure;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Presentation.Career;
using UnityEditor;
using UnityEngine;

// Inspector utility, excluded from player builds. No menu or runtime UI required.
public sealed class CareerDeveloperTools : ScriptableObject { }

[CustomEditor(typeof(CareerDeveloperTools))]
public sealed class CareerDeveloperToolsInspector : Editor
{
    private int credits = 10000;
    private int fame = 1000;
    private bool saveAfterGrant;
    private readonly Queue<string> log = new Queue<string>();
    private Vector2 logScroll;
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    private static GameContext Context => BootstrapController.Context;
    private static string Unavailable()
    {
        if (!Application.isPlaying) return "Enter Play Mode from Bootstrap and load a campaign.";
        if (Context?.Sessions?.Current?.PlayerTeam == null || Context.Saves?.HasActiveCampaign != true)
            return "Load a saved campaign first.";
        if (Context.RaceLaunch.HasPendingRace) return "Return to Career after settling the current race.";
        if (UnityEngine.Object.FindFirstObjectByType<CareerController>() == null) return "Open the Career scene to change team resources.";
        return null;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("RACE//FATAL — DEVELOPER RESOURCES", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Grant resources to the loaded player team during Play Mode. Fame grants increase BOTH available and lifetime fame. Later normal campaign saves also include these grants, even if Save after grant is off.", MessageType.Info);
        string unavailable = Unavailable();
        if (unavailable != null) EditorGUILayout.HelpBox(unavailable, MessageType.Info);
        var team = Application.isPlaying ? Context?.Sessions?.Current?.PlayerTeam : null;
        if (team != null)
        {
            EditorGUILayout.LabelField("Team", team.TeamName);
            EditorGUILayout.LabelField("Credits", team.Credits.ToString("N0"));
            EditorGUILayout.LabelField("Available fame", team.Fame.ToString("N0"));
            EditorGUILayout.LabelField("Lifetime fame", team.LifetimeFame.ToString("N0"));
        }
        using (new EditorGUI.DisabledScope(unavailable != null))
        {
            saveAfterGrant = EditorGUILayout.Toggle("Save after grant", saveAfterGrant);
            EditorGUILayout.LabelField("Quick credits", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+1,000")) Grant(1000, 0);
                if (GUILayout.Button("+10,000")) Grant(10000, 0);
                if (GUILayout.Button("+100,000")) Grant(100000, 0);
            }
            EditorGUILayout.LabelField("Quick fame", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+150")) Grant(0, 150);
                if (GUILayout.Button("+1,000")) Grant(0, 1000);
                if (GUILayout.Button("+10,000")) Grant(0, 10000);
            }
            credits = EditorGUILayout.IntField("Custom credits", credits);
            fame = EditorGUILayout.IntField("Custom fame", fame);
            if (GUILayout.Button("Grant custom amounts")) Grant(credits, fame);
            if (GUILayout.Button("Save campaign now / retry save")) Save();
        }
        EditorGUILayout.HelpBox("Reopen the current Career tab to refresh its displayed balances. The live balances above update immediately.", MessageType.None);
        EditorGUILayout.LabelField("Session log", EditorStyles.boldLabel);
        logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.Height(150));
        foreach (var entry in log) EditorGUILayout.LabelField(entry, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();
        if (GUILayout.Button("Clear log")) log.Clear();
    }
    private void Grant(int creditAmount, int fameAmount)
    {
        var error = Unavailable();
        if (error != null) { Record(error); return; }
        try { DeveloperResourceGrant.Apply(Context.Sessions.Current.PlayerTeam, creditAmount, fameAmount); }
        catch (Exception exception) when (exception is ArgumentException || exception is OverflowException)
        { Record("Rejected: " + exception.Message); return; }
        Record($"Granted {creditAmount:N0} credits and {fameAmount:N0} fame.");
        if (saveAfterGrant) Save();
    }
    private void Save()
    {
        var error = Unavailable();
        if (error != null) { Record(error); return; }
        var result = Context.Saves.SaveCurrentCampaign();
        Record(result.IsSuccess ? "Campaign saved." : "SAVE FAILED: " + result.ErrorMessage + " Grant remains applied. Retry Save; do not grant again.");
    }
    private void Record(string message)
    {
        if (log.Count >= 30) log.Dequeue();
        log.Enqueue(DateTime.Now.ToString("HH:mm:ss") + "  " + message);
        Debug.Log("[RF Developer Tools] " + message);
    }
}
