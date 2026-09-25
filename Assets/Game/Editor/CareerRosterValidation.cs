using System;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Racing;
using UnityEditor;
using UnityEngine;

public static class CareerRosterValidation
{
    [MenuItem("RACE//FATAL/Career/Validate Roster Progression")]
    public static void Run()
    {
        var database = new GameDatabase();
        database.AddRacerDefinition(new RacerDefinition("partner-a", "Pilot A", .5f, .5f, .5f, .5f, .5f));
        database.AddRacerDefinition(new RacerDefinition("partner-b", "Pilot B", .5f, .5f, .5f, .5f, .5f));
        database.AddRacerPerkDefinition(new RacerPerkDefinition("energy", "Reserve Cell", "", 50,
            RacerPerkEffect.EnergyCapacity, 20));
        database.AddRacerPerkDefinition(new RacerPerkDefinition("pace", "Apex Reader", "", 60,
            RacerPerkEffect.Pace, .08f));
        var team = new TeamState("test", "Test", "#fff", "#000");
        var player = new RacerState("player", "Player", team.TeamId, true);
        var partner = new RacerState("partner-a", "Pilot A", team.TeamId, false);
        Require(team.Roster.AddRacer(player).IsSuccess && team.Roster.AddRacer(partner).IsSuccess, "Starter roster");
        var session = new GameSessionState(team, new CareerRun("run", team, player), new WorldState(), "partner-a", null, null);
        var roster = new RosterService(database);
        Require(!session.SelectPartnerRacer("player").IsSuccess && !session.SelectPartnerRacer("partner-b").IsSuccess,
            "Only active teammates can be assigned");
        Require(!roster.Recruit(team, null).IsSuccess && !roster.Recruit(team, "unknown").IsSuccess,
            "Unknown recruits are rejected");
        Require(!roster.Recruit(team, "partner-b").IsSuccess, "Recruitment needs credits");
        team.AddCredits(RosterService.RecruitmentCost);
        Require(roster.Recruit(team, "partner-b").IsSuccess && team.Credits == 0, "Exact balance recruitment");
        Require(!roster.Recruit(team, "partner-b").IsSuccess && team.Roster.Racers.Count == 3,
            "A recruit cannot be purchased twice");
        Require(session.SelectPartnerRacer("partner-b").IsSuccess && session.SelectedPartnerRacerId == "partner-b",
            "Partner assignment uses recruited teammate");
        var recruit = team.Roster.FindRacer("partner-b");
        Require(!roster.PurchasePerk(team, "partner-b", "energy").IsSuccess, "Fame required");
        recruit.Progression.AddFame(110);
        Require(roster.PurchasePerk(team, "partner-b", "energy").IsSuccess && recruit.Progression.Fame == 60,
            "Perk costs this racer's Fame");
        Require(!roster.PurchasePerk(team, "partner-b", "energy").IsSuccess && recruit.Progression.Fame == 60,
            "Duplicate perk cannot charge twice");
        Require(roster.PurchasePerk(team, "partner-b", "pace").IsSuccess && recruit.Progression.Fame == 0,
            "Second perk can use exact balance");
        Require(RacerPerkBonuses.For(recruit, database).EnergyCapacity == 20 &&
                Math.Abs(RacerPerkBonuses.For(recruit, database).Pace - .08f) < .0001f,
            "Purchased perks affect race bonuses");
        player.Progression.AddFame(60);
        Require(!roster.PurchasePerk(team, "player", "pace").IsSuccess && player.Progression.Fame == 60,
            "AI-only perk cannot spend player Fame");
        Require(roster.PurchasePerk(team, "player", "energy").IsSuccess && player.Progression.Fame == 10,
            "Shared energy perk works for player");

        var mapper = new CampaignSaveMapper(database);
        var data = JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(mapper.Capture(session).Value));
        var restored = mapper.Restore(data);
        Require(restored.IsSuccess, "Save restores: " + restored.ErrorMessage);
        Require(restored.Value.SelectedPartnerRacerId == "partner-b" &&
                restored.Value.PlayerTeam.Roster.FindRacer("partner-b").Progression.HasPurchasedPerk("pace") &&
                restored.Value.PlayerTeam.Roster.FindRacer("player").Progression.Fame == 10,
            "Roster, selection and perk purchases survive JSON reload");
        data.selectedPartnerRacerId = null;
        Require(mapper.Restore(data).Value.SelectedPartnerRacerId == "partner-a", "Older saves use default partner");
        data.selectedPartnerRacerId = "unknown";
        Require(!mapper.Restore(data).IsSuccess, "Unknown saved partner is rejected");
        partner.Kill();
        Require(!session.SelectPartnerRacer("partner-a").IsSuccess && session.SelectedPartnerRacerId == "partner-b",
            "Dead partners cannot be selected");

        // Teammate Fame is earned once per finalized race and uses their own result.
        var standings = new[] {
            new RaceResultEntry("player", team.TeamId, 1, 3, RaceParticipantStatus.Finished),
            new RaceResultEntry("partner-b", team.TeamId, 3, 3, RaceParticipantStatus.Finished)
        };
        var race = new RaceResult("test-race", standings, "unique-race");
        var resolution = new PostRaceResolutionService(new RaceRewardPolicy());
        resolution.Resolve(race, team, player);
        Require(recruit.Progression.Fame == 65, "Partner earned own finish Fame");
        resolution.Resolve(race, team, player);
        Require(recruit.Progression.Fame == 65, "Repeat settlement does not duplicate Fame");
        Debug.Log("Roster validation passed: recruitment, partner selection, perks, save compatibility and race Fame.");
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("Roster validation failed: " + description);
    }
}
