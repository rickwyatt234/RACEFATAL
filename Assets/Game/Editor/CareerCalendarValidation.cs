using System;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Energy;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Racing;
using RaceFatal.Vehicles;
using UnityEditor;
using UnityEngine;

public static class CareerCalendarValidation
{
    [MenuItem("RACE//FATAL/Career/Validate Race Calendar")]
    public static void Run()
    {
        var db = new GameDatabase();
        db.AddBikeDefinition(new BikeDefinition("test-bike", "Bike", 0, 0, 0, 100, 1, 100));
        db.AddEngineDefinition(new EngineDefinition("engine", "Engine", default, 100, 10, 0, null));
        db.AddChassisDefinition(new ChassisDefinition("chassis", "Chassis", 1, 1, 0, null));
        db.AddRaceDefinition(new RaceDefinition("race", "Test Race", "track", default, 3, 4, 2, 7));
        db.AddCareerEventDefinition(Event("free", 0, 0, false));
        db.AddCareerEventDefinition(Event("paid", 0, 200, false));
        db.AddCareerEventDefinition(Event("champ", 0, 300, true));
        db.AddCareerEventDefinition(Event("locked", 250, 0, false));
        db.AddCareerEventDefinition(new CareerEventDefinition("future", "Future", "", CareerEventKind.Deathmatch,
            0, 0, new[] { "race" }, new[] { 10 }, new int[0], new[] { 1 }));
        var service = new CareerCalendarService(db);
        var team = new TeamState("team", "Player Team", "#fff", "#000");
        var player = new RacerState("player", "Player", "team", true);
        team.Roster.AddRacer(player); team.Roster.AddRacer(new RacerState("partner", "Partner", "team", false));
        team.AddCredits(2000);
        team.RestoreResearchContract(new ResearchContract("staff", "Researcher", 10, 10));
        var opponent = new TeamState("opponent", "Opponent", "#fff", "#000");
        opponent.Roster.AddRacer(new RacerState("enemy-a", "Enemy A", "opponent", false));
        opponent.Roster.AddRacer(new RacerState("enemy-b", "Enemy B", "opponent", false));
        AddBikes(team, db); AddBikes(opponent, db);
        var session = new GameSessionState(team, new CareerRun("career", team, player), WorldState.Restore(new[] { opponent }).Value,
            "partner", team.Garage.Bikes[0].BikeId, team.Garage.Bikes[1].BikeId);
        Require(service.Refresh(team), "First draw generated");
        string draw = string.Join(",", team.Calendar.DrawIds);
        Require(team.Calendar.DrawIds.Count == 3 && !team.Calendar.DrawIds.Contains("locked") && !team.Calendar.DrawIds.Contains("future"),
            "Only unlocked supported events appear");
        Require(!service.Refresh(team) && string.Join(",", team.Calendar.DrawIds) == draw, "Refresh never rerolls");
        Require(!service.CanEnter(team, "locked").IsSuccess && !service.CanEnter(team, null).IsSuccess, "Entry validates unlock and ID");
        var mapper = new CampaignSaveMapper(db);
        session = Reload(session, mapper); team = session.PlayerTeam; player = session.CareerRun.Player;
        Require(string.Join(",", team.Calendar.DrawIds) == draw, "Weekly draw survives JSON reload");
        var prepared = Prepare(session, db);
        var transaction = service.Register(session, "paid", prepared);
        Require(transaction.IsSuccess && team.Credits == 1800, "Charge entry once");
        transaction.Value.Rollback(); transaction.Value.Rollback();
        Require(team.Credits == 2000 && team.Calendar.Active == null, "Failed-save rollback is idempotent");
        Require(service.Register(session, "paid", prepared).IsSuccess, "Register again after rollback");
        string attempt = prepared.InstanceId;
        session = Reload(session, mapper); team = session.PlayerTeam; player = session.CareerRun.Player;
        var resumed = Prepare(session, db);
        Require(service.Register(session, "paid", resumed).IsSuccess && team.Credits == 1800 && resumed.InstanceId == attempt,
            "Interrupted paid entry resumes with same attempt and no charge");
        var resolver = new PostRaceResolutionService(new RaceRewardPolicy());
        bool mismatch = false;
        try { resolver.Resolve(Result("wrong-instance"), team, player); } catch (InvalidOperationException) { mismatch = true; }
        Require(mismatch && team.Credits == 1800 && team.Calendar.Week == 1, "Unmatched result cannot mutate campaign");
        var race = Result(attempt);
        resolver.Resolve(race, team, player);
        Require(team.Credits == 2800 && team.Calendar.Week == 2 && team.Calendar.Active == null, "Payout and week advancement");
        Require(team.ResearchPoints == 67 && team.ResearchContracts[0].RacesRemaining == 9, "Event RP and researchers settle together");
        resolver.Resolve(race, team, player);
        Require(team.Credits == 2800 && team.Calendar.Week == 2, "Duplicate result cannot pay or advance twice");
        session = Reload(session, mapper); team = session.PlayerTeam; player = session.CareerRun.Player;
        resolver.Resolve(race, team, player);
        Require(team.Calendar.Week == 2 && team.Credits == 2800, "Receipt prevents replay after reload");
        service.Refresh(team);
        int startCredits = team.Credits;
        RaceResult finalRound = null;
        for (int round = 0; round < 3; round++)
        {
            var director = Prepare(session, db);
            Require(service.Register(session, "champ", director).IsSuccess, "Enter championship round");
            finalRound = Result(director.InstanceId);
            resolver.Resolve(finalRound, team, player);
            if (round < 2)
                Require(team.Calendar.Active.roundIndex == round + 1, "Next championship round saved");
            session = Reload(session, mapper); team = session.PlayerTeam; player = session.CareerRun.Player;
        }
        Require(team.Calendar.Week == 5 && team.Calendar.Active == null && team.Calendar.LastEvent.finalRank == 1 &&
            team.Calendar.LastEvent.finalPrize == 5000 && team.Credits == startCredits - 300 + 3000 + 5000,
            "Three-round standings, one fee and final prize survive reload");
        Require(team.Calendar.LastEvent.standings.Single(s => s.teamId == "team").points == 129, "Both teammates contribute points");
        int completionCredits = team.Credits;
        resolver.Resolve(finalRound, team, player);
        Require(team.Credits == completionCredits && team.Calendar.Week == 5, "Final prize cannot be repeated after reload");
        service.Refresh(team);
        Require(team.Calendar.UnlockedIds.Contains("locked"), "Earned Fame permanently unlocks new events");
        var attrition = Reload(session, mapper);
        var firstRound = Prepare(attrition, db);
        Require(service.Register(attrition, "champ", firstRound).IsSuccess, "Register fixed championship field");
        resolver.Resolve(Result(firstRound.InstanceId), attrition.PlayerTeam, attrition.CareerRun.Player);
        attrition.World.OpponentTeams[0].Roster.Racers[0].Kill();
        var reduced = Prepare(attrition, db);
        Require(reduced.State.Participants.Count == 2 && reduced.State.RaceDefinition.EntrantCount == 2,
            "An opponent unable to field two racers misses the next round");
        Require(service.Register(attrition, "champ", reduced).IsSuccess, "Continue reduced championship grid");
        resolver.Resolve(new RaceResult("race", Result(reduced.InstanceId).Standings.Take(2).ToList(), reduced.InstanceId, 7),
            attrition.PlayerTeam, attrition.CareerRun.Player);
        Require(attrition.PlayerTeam.Calendar.Active.standings.Single(s => s.teamId == "opponent").points == 27,
            "Absent team retains prior points and scores zero");
        var snapshot = team.Calendar.Export();
        snapshot.week = -1;
        Require(!team.RestoreCalendar(snapshot).IsSuccess, "Corrupt save rejected without replacing live calendar");
        int points = team.ResearchPoints, remaining = team.ResearchContracts[0].RacesRemaining;
        Require(service.SkipOrWithdraw(session).IsSuccess && team.ResearchPoints == points && team.ResearchContracts[0].RacesRemaining == remaining,
            "Skipping awards no research and does not consume staff contract");
        service.Refresh(team);
        int beforeWithdrawal = team.Credits;
        var withdrawnRace = Prepare(session, db);
        Require(service.Register(session, "champ", withdrawnRace).IsSuccess, "Enter championship for withdrawal");
        Require(service.SkipOrWithdraw(session).IsSuccess && team.Credits == beforeWithdrawal - 300 &&
            team.Calendar.Active == null && team.Calendar.LastEvent.withdrawn && team.Calendar.LastEvent.finalPrize == 0,
            "Withdrawal forfeits fee without awarding completion prize");
        bool withdrew = false;
        try { resolver.Resolve(Result(withdrawnRace.InstanceId), team, player); } catch (InvalidOperationException) { withdrew = true; }
        Require(withdrew && team.Credits == beforeWithdrawal - 300, "Withdrawn attempts cannot award late rewards");
        service.Refresh(team);
        var dnfDirector = Prepare(session, db);
        Require(service.Register(session, "free", dnfDirector).IsSuccess, "Enter free race");
        int beforeDnf = team.Credits;
        resolver.Resolve(Result(dnfDirector.InstanceId, RaceParticipantStatus.Retired), team, player);
        Require(team.Credits == beforeDnf + 400, "DNF earns exactly 40 percent of position payout");
        service.Refresh(team);
        var fatalDirector = Prepare(session, db);
        Require(service.Register(session, "champ", fatalDirector).IsSuccess, "Enter fatal championship");
        session.CareerRun.Kill();
        resolver.Resolve(Result(fatalDirector.InstanceId, RaceParticipantStatus.Destroyed), team, player);
        Require(team.Calendar.Active == null && team.Calendar.LastEvent.withdrawn && team.Calendar.LastEvent.finalPrize == 0,
            "Player death closes an unfinished championship without final prize");
        session = Reload(session, mapper); team = session.PlayerTeam;
        var poor = new TeamState("poor", "Poor", "#fff", "#000");
        service.Refresh(poor);
        Require(!service.CanEnter(poor, "paid").IsSuccess && service.CanEnter(poor, "free").IsSuccess, "Free entry remains available with no credits");
        var legacy = mapper.Capture(session).Value; legacy.playerTeam.calendar = null;
        Require(mapper.Restore(legacy).IsSuccess && mapper.Restore(legacy).Value.PlayerTeam.Calendar.Week == 1, "Legacy save starts week one");
        Debug.Log("Calendar validation passed: draw persistence, Fame gates, fees, rollback, interrupted entry, settlement, championship and save migration.");
    }
    private static CareerEventDefinition Event(string id, int fame, int fee, bool championship) =>
        new CareerEventDefinition(id, id, "", championship ? CareerEventKind.Championship : CareerEventKind.Race,
            fame, fee, championship ? new[] { "race", "race", "race" } : new[] { "race" },
            new[] { 1000, 800, 500, 100 }, new[] { 5000, 2500 }, new[] { 25, 18, 15, 12 });
    private static RaceResult Result(string instance, RaceParticipantStatus status = RaceParticipantStatus.Finished) => new RaceResult("race", new[] {
        new RaceResultEntry("player", "team", 1, 3, status),
        new RaceResultEntry("partner", "team", 2, 3, RaceParticipantStatus.Finished),
        new RaceResultEntry("enemy-a", "opponent", 3, 3, RaceParticipantStatus.Finished),
        new RaceResultEntry("enemy-b", "opponent", 4, 3, RaceParticipantStatus.Finished) }, instance, 7);
    private static GameSessionState Reload(GameSessionState session, CampaignSaveMapper mapper)
    {
        var captured = mapper.Capture(session); Require(captured.IsSuccess, "Capture");
        var restored = mapper.Restore(JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(captured.Value)));
        Require(restored.IsSuccess, "Restore: " + restored.ErrorMessage); return restored.Value;
    }
    private static RaceDirector Prepare(GameSessionState session, GameDatabase db)
    {
        var career = new CareerManager(new CharacterFactory());
        var builds = new BikeBuildFactory(db, new VehicleFactory(), new EquipmentFactory());
        var sessions = new GameSessionManager(db, career, new WorldFactory(db, builds), builds);
        Require(sessions.RestoreSession(session).IsSuccess, "Restore runtime session");
        var participants = new RaceParticipantFactory(db, new BikePerformanceCalculator(db));
        var factory = new RaceFactory(new RaceGridValidator(new RaceEligibilityService()));
        var entry = new RaceEntryBuilder(participants, factory, career);
        var result = new RacePreparationService(db, sessions, entry).PrepareSelectedRace("race");
        Require(result.IsSuccess, "Prepare calendar race: " + result.ErrorMessage);
        return result.Value;
    }
    private static void AddBikes(TeamState team, GameDatabase db)
    {
        var factory = new VehicleFactory();
        for (int i = 0; i < 2; i++)
        {
            var bike = factory.CreateBike(db.GetBikeDefinition("test-bike"), "#fff", "#000");
            var engine = factory.CreateEngine(db.GetEngineDefinition("engine"));
            var chassis = factory.CreateChassis(db.GetChassisDefinition("chassis"));
            team.Garage.AddBike(bike); team.Garage.AddEngine(engine); team.Garage.AddChassis(chassis);
            Require(team.Garage.InstallEngine(bike.BikeId, engine.EngineId).IsSuccess &&
                team.Garage.InstallChassis(bike.BikeId, chassis.ChassisId).IsSuccess, "Prepare owned bike");
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
