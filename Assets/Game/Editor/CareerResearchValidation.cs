using System;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Shared;
using RaceFatal.Vehicles;
using UnityEditor;
using UnityEngine;

public static class CareerResearchValidation
{
    [MenuItem("RACE//FATAL/Career/Validate Research Progression")]
    public static void Run()
    {
        var database = new GameDatabase();
        Add(database, "base", 50);
        Add(database, "engine", 100, "base");
        Add(database, "free", 0);
        Add(database, "negative", -1);
        Add(database, "missing", 1, "unknown");
        Add(database, "cycle-a", 1, "cycle-b");
        Add(database, "cycle-b", 1, "cycle-a");
        Add(database, "self", 1, "self");
        Add(database, "both", 0, "engine", "free");
        Add(database, "expensive", 1000);
        var research = new ResearchService(database);
        var team = new TeamState("test", "Test", "#ffffff", "#000000");
        team.AddResearchPoints(150);
        Require(!research.Research(null, "base").IsSuccess, "Missing team rejected");
        foreach (string id in new[] { null, "", "unknown", "negative", "missing", "cycle-a", "self", "engine", "both", "expensive" })
            Require(!research.Research(team, id).IsSuccess, "Rejected invalid or unavailable technology: " + id);
        Require(team.ResearchPoints == 150 && team.UnlockedTechnologyIds.Count == 0, "Failures cannot mutate funds or unlocks");

        database.AddEngineDefinition(new EngineDefinition("engine-item", "Test Engine", default, 100, 10, 25, "engine"));
        var shop = new ShopService(database);
        team.AddCredits(25);
        Require(!shop.CanPurchase(team, ShopItemKind.Engine, "engine-item").IsSuccess, "Shop starts locked");
        Require(research.Research(team, "base").IsSuccess && team.ResearchPoints == 100, "Prerequisite costs exactly 50 RP");
        Require(!research.Research(team, "base").IsSuccess && team.ResearchPoints == 100, "Repeat research cannot charge twice");
        Require(research.Research(team, "engine").IsSuccess && team.ResearchPoints == 0, "Exact-balance research");
        Require(!research.CanResearch(team, "both").IsSuccess, "Every prerequisite is required");
        Require(research.Research(team, "free").IsSuccess && team.ResearchPoints == 0, "Zero-cost research");
        Require(research.Research(team, "both").IsSuccess, "Multiple prerequisites satisfied");
        Require(shop.Purchase(team, ShopItemKind.Engine, "engine-item").IsSuccess, "Research unlocks Shop purchase");
        Require(team.Credits == 0 && team.Garage.Engines.Count == 1, "Purchase enters Garage");
        team.AddResearchPoints(17);
        var mapper = new CampaignSaveMapper(database);
        var session = new GameSessionState(team, null, new WorldState(), null, null, null);
        var captured = mapper.Capture(session);
        Require(captured.IsSuccess, "Capture succeeds");
        // Exercise the actual save DTO serialization and mapper, not a second research-only format.
        var data = JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(captured.Value));
        var restored = mapper.Restore(data);
        Require(restored.IsSuccess, "Restore succeeds: " + restored.ErrorMessage);
        var loaded = restored.Value.PlayerTeam;
        Require(loaded.ResearchPoints == 17 && loaded.HasTechnology("base") && loaded.HasTechnology("engine") && loaded.HasTechnology("both"), "RP and unlocks survive JSON roundtrip");
        Require(loaded.Credits == 0 && loaded.Garage.Engines.Count == 1, "Purchase survives reload");
        Require(!research.Research(loaded, "engine").IsSuccess && loaded.ResearchPoints == 17, "Reload cannot enable repeat research");
        // A pre-research save with no unlocks still restores without requiring technologies.
        var oldSession = new GameSessionState(new TeamState("old", "Old", "#fff", "#000"), null, new WorldState(), null, null, null);
        Require(new CampaignSaveMapper(new GameDatabase()).Restore(mapper.Capture(oldSession).Value).IsSuccess, "Campaign without research remains compatible");
        Debug.Log("Research validation passed: invalid content, prerequisites, funds, duplicate protection, Shop/Garage integration and JSON save roundtrip.");
    }

    private static void Add(GameDatabase database, string id, int cost, params string[] prerequisites)
    {
        database.AddTechnologyDefinition(new TechnologyDefinition(id, id, "", ResearchField.EngineTechnology, cost, prerequisites));
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("Research validation failed: " + description);
    }
}
