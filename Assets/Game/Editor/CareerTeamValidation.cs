using System;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Vehicles;
using UnityEditor;
using UnityEngine;

public static class CareerTeamValidation
{
    [MenuItem("RACE//FATAL/Career/Validate Team Management")]
    public static void Run()
    {
        var service = new TeamManagementService();
        var team = new TeamState("team", "Original", "#FFFFFF", "#000000");
        var database = new GameDatabase();
        var bikeDefinition = new BikeDefinition("bike", "Bike", 0, 0, 0, 100, 1, 100);
        database.AddBikeDefinition(bikeDefinition);
        var bike = bikeDefinition.CreateBikeState("owned-bike", "#111111", "#222222");
        Require(team.Garage.AddBike(bike).IsSuccess, "Own bike");
        team.AddCredits(2345);
        team.AddFame(67);
        team.AddResearchPoints(89);
        team.UnlockTechnology("tech");
        team.UnlockChampionship("championship");
        team.RestoreResearchContract(new ResearchContract("staff", "Research Staff", 10, 2));
        Require(!service.UpdateIdentity(null, "New", "#fff", "#000", true).IsSuccess, "Missing team rejected");
        foreach (string name in new[] { "", " ", new string('x', 41), "<b>Team</b>", "Team\nName" })
            Require(!service.UpdateIdentity(team, name, "#fff", "#000", true).IsSuccess, "Invalid name rejected");
        foreach (string color in new[] { "", "bad color", "#GGGGGG", "#12", "#12345" })
            Require(!service.UpdateIdentity(team, "New", "#abcdef", color, true).IsSuccess, "Invalid secondary color rejected");
        Require(team.TeamName == "Original" && team.PrimaryColor == "#FFFFFF" && bike.PrimaryColor == "#111111",
            "Validation failures cannot partially change identity or bike paint");
        Require(service.UpdateIdentity(team, "  New Team  ", "#abc", "001122", false).IsSuccess, "Valid edit");
        Require(team.TeamName == "New Team" && team.PrimaryColor == "#AABBCC" && team.SecondaryColor == "#001122",
            "Identity normalized");
        Require(bike.PrimaryColor == "#111111", "Custom bike paint preserved when repaint is off");
        Require(service.UpdateIdentity(team, "New Team", "#33ccff", "#ff3399", true).IsSuccess &&
            bike.PrimaryColor == "#33CCFF" && bike.SecondaryColor == "#FF3399", "Optional repaint applies");
        Require(team.Credits == 2345 && team.Fame == 67 && team.ResearchPoints == 89, "Identity changes preserve resources");
        var session = new GameSessionState(team, null, new WorldState(), null, null, null);
        var mapper = new CampaignSaveMapper(database);
        var captured = mapper.Capture(session);
        Require(captured.IsSuccess, "Capture succeeds");
        var serialized = JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(captured.Value));
        var result = mapper.Restore(serialized);
        Require(result.IsSuccess, "Restore succeeds: " + result.ErrorMessage);
        var restored = result.Value.PlayerTeam;
        Require(restored.TeamName == "New Team" && restored.PrimaryColor == "#33CCFF" &&
            restored.Garage.FindBike("owned-bike").SecondaryColor == "#FF3399", "Identity and paint survive reload");
        Require(restored.ResearchPoints == 89 && restored.HasTechnology("tech") &&
            restored.HasChampionshipUnlocked("championship") && restored.ResearchOutputPerRace == 10,
            "Team progression and research contracts survive reload");
        Debug.Log("Team management validation passed: input validation, optional repaint, resources and JSON save roundtrip.");
    }
    private static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("Team validation failed: " + description);
    }
}
