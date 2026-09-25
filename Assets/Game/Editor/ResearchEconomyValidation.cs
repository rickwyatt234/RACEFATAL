using System;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Racing;
using UnityEditor;
using UnityEngine;

public static class ResearchEconomyValidation
{
    [MenuItem("RACE//FATAL/Career/Validate Research Economy")]
    public static void Run()
    {
        CareerResearchValidation.Run();
        var db = new GameDatabase();
        db.AddResearcherDefinition(new ResearcherDefinition("staff", "Researcher", 100, 10, 2));
        db.AddResearcherDefinition(new ResearcherDefinition("bad", "Invalid", -1, 10, 2));
        db.AddResearcherDefinition(new ResearcherDefinition("free", "Volunteer", 0, 3, 1));
        var team = new TeamState("team", "Team", "#fff", "#000");
        var player = new RacerState("player", "Player", team.TeamId, true);
        team.Roster.AddRacer(player);
        var hiring = new ResearcherService(db);
        Require(!hiring.Hire(team, "staff").IsSuccess && team.ResearchContracts.Count == 0, "Insufficient credit hire is atomic");
        team.AddCredits(100);
        Require(!hiring.Hire(team, "bad").IsSuccess && !hiring.Hire(team, null).IsSuccess, "Bad offer rejected");
        Require(hiring.Hire(team, "staff").IsSuccess && team.Credits == 0, "Exact-balance prepaid contract");
        Require(!hiring.Hire(team, "staff").IsSuccess, "Active contract cannot be stacked");
        Require(team.ResearchOutputPerRace == 10, "Forecast output");
        var resolver = new PostRaceResolutionService(new RaceRewardPolicy());
        bool invalidIdentity = false;
        try { resolver.Resolve(MakeResult("", RaceParticipantStatus.Finished), team, player); }
        catch (InvalidOperationException) { invalidIdentity = true; }
        Require(invalidIdentity && team.Credits == 0 && team.ResearchPoints == 0 && team.ResearchContracts[0].RacesRemaining == 2,
            "Missing settlement identity must fail before any mutation");
        var race = MakeResult("first", RaceParticipantStatus.Finished);
        var payout = resolver.Resolve(race, team, player);
        Require(payout.RaceResearchPoints == 50 && payout.EventResearchPoints == 7 && payout.ResearcherPoints == 10, "Separate reward sources");
        Require(team.ResearchPoints == 67 && team.ResearchContracts[0].RacesRemaining == 1, "One contract tick per final result");
        int credits = team.Credits, fame = player.Progression.Fame;
        Require(ReferenceEquals(payout, resolver.Resolve(race, team, player)), "Repeated result returns same receipt");
        Require(team.ResearchPoints == 67 && team.Credits == credits && player.Progression.Fame == fame && team.ResearchContracts[0].RacesRemaining == 1, "Repeated result changes nothing");
        var mapper = new CampaignSaveMapper(db);
        var captured = mapper.Capture(new GameSessionState(team, null, new WorldState(), null, null, null));
        var data = JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(captured.Value));
        var loaded = mapper.Restore(data);
        Require(loaded.IsSuccess, "Restore contract and receipt: " + loaded.ErrorMessage);
        team = loaded.Value.PlayerTeam;
        player = team.Roster.FindRacer("player");
        var replay = resolver.Resolve(MakeResult("first", RaceParticipantStatus.Finished), team, player);
        Require(team.ResearchPoints == 67 && replay.ResearcherPoints == 10 && team.ResearchContracts[0].RacesRemaining == 1, "Receipt prevents duplicate payout after reload");
        bool rejected = false;
        try { resolver.Resolve(MakeResult("abandoned", RaceParticipantStatus.Retired, false), team, player); }
        catch (InvalidOperationException) { rejected = true; }
        Require(rejected && team.ResearchContracts[0].RacesRemaining == 1, "Abandoned race cannot advance contract");
        var dnf = resolver.Resolve(MakeResult("second", RaceParticipantStatus.Retired), team, player);
        Require(dnf.RaceResearchPoints == 25 && dnf.EventResearchPoints == 7 && dnf.ResearcherPoints == 10, "Resolved DNF earns contractual and event RP");
        Require(team.ResearchOutputPerRace == 0 && team.ResearchContracts[0].RacesRemaining == 0, "Contract expires exactly");
        var expired = resolver.Resolve(MakeResult("third", RaceParticipantStatus.Finished), team, player);
        Require(expired.ResearcherPoints == 0, "Expired contract does not pay");
        Require(hiring.Hire(team, "staff").IsSuccess && team.ResearchContracts.Count == 1, "Renew expired contract without duplicate entry");
        Require(hiring.Hire(team, "free").IsSuccess && team.ResearchOutputPerRace == 13, "Independent contracts combine");
        var fourth = resolver.Resolve(MakeResult("fourth", RaceParticipantStatus.Destroyed), team, player);
        Require(fourth.ResearcherPoints == 13, "Destroyed outcome advances staff once");
        Require(team.ResearchOutputPerRace == 10, "Different duration contracts expire independently");
        data.playerTeam.settledRaceResults[0].status = "999";
        Require(!mapper.Restore(data).IsSuccess, "Invalid receipt status rejected");
        data.playerTeam.settledRaceResults[0].status = RaceParticipantStatus.Finished.ToString();
        data.playerTeam.settledRaceResults.Add(data.playerTeam.settledRaceResults[0]);
        Require(!mapper.Restore(data).IsSuccess, "Duplicate receipt rejected");
        data.playerTeam.settledRaceResults.RemoveAt(1);
        data.playerTeam.researchContracts[0].racesRemaining = -1;
        Require(!mapper.Restore(data).IsSuccess, "Corrupt contract rejected");
        data.playerTeam.researchContracts = null; data.playerTeam.settledRaceResults = null;
        Require(mapper.Restore(data).IsSuccess, "Old saves without contracts/receipts remain compatible");
        Debug.Log("Research economy passed: hiring, renewal, expiry, DNF, abandonment, duplicate payouts, save/reload and legacy saves.");
    }
    private static RaceResult MakeResult(string instance, RaceParticipantStatus status, bool final = true) => new RaceResult("same-event",
        new[] { new RaceResultEntry("player", "team", 1, 3, status) }, instance, 7, final);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
