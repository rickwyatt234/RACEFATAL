using System;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Combat;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Racing;
using RaceFatal.Vehicles;
using UnityEditor;
using UnityEngine;

public static class DeathmatchValidation
{
    [MenuItem("RACE//FATAL/Career/Validate Deathmatch")]
    public static void Run()
    {
        var fixture = new Fixture(new DeathmatchRules(DeathmatchVictoryMode.Team, 1, 100, 30, 5, 2));
        var race = fixture.Race;
        race.StartRace(); race.Tick(5);
        Require(race.State.Participants.All(p => p.Status == RaceParticipantStatus.Racing), "Grid grace");
        race.Tick(1);
        Require(race.State.FindParticipant("player").BelowSpeedSeconds == 1, "Below-speed countdown");
        race.ReportSpeed("player", 30); race.Tick(.5f);
        Require(race.State.FindParticipant("player").BelowSpeedSeconds == 0, "Exact speed threshold resets continuous grace");
        foreach (var p in race.State.Participants) race.ReportSpeed(p.RacerId, 60);
        race.ReportSpeed("player", 0); race.Tick(2);
        Require(race.State.FindParticipant("player").Status == RaceParticipantStatus.Retired &&
            fixture.Session.CareerRun.IsActive && !race.State.FindParticipant("player").Bike.IsDestroyed, "Speed DQ preserves career and bike");
        Require(!race.State.IsFinished && race.ResolveRemainingRace() == null, "Partner continues; no premature resolution");
        race.RetireRacer("enemy-a"); race.RetireRacer("enemy-b"); race.Tick(.1f);
        Require(race.State.IsFinished && race.FinalRaceResult.Standings.Single(s => s.RacerId == "player").IsWinner,
            "Surviving partner wins for team");
        Require(fixture.Session.PlayerTeam.Credits == 1100 && fixture.Session.PlayerTeam.Calendar.Week == 2,
            "Team pays once in full even if player DQ");
        var receipt = race.PostRaceResult;
        race.Tick(1000); race.CompleteRace();
        new PostRaceResolutionService(new RaceRewardPolicy()).Resolve(race.FinalRaceResult, fixture.Session.PlayerTeam, fixture.Session.CareerRun.Player);
        Require(fixture.Session.PlayerTeam.Credits == 1100 && race.PostRaceResult == receipt, "Exactly-once completion");
        var reloaded = fixture.Reload();
        new PostRaceResolutionService(new RaceRewardPolicy()).Resolve(race.FinalRaceResult, reloaded.PlayerTeam, reloaded.CareerRun.Player);
        Require(reloaded.PlayerTeam.Credits == 1100 && reloaded.PlayerTeam.Calendar.LastEvent.finalRank == 1,
            "JSON reload retains receipt and history");
        Require(reloaded.PlayerTeam.Calendar.LastEvent.deathmatchResults.Count == 4 &&
            reloaded.PlayerTeam.Calendar.LastEvent.deathmatchResults.Single(s => s.racerId == "partner").winner, "Saved survival standings");

        fixture = new Fixture(new DeathmatchRules(DeathmatchVictoryMode.Individual, 2, 100, 0));
        race = fixture.Race; race.StartRace();
        for (int i = 0; i < 6; i++) race.ReportLapCompleted("player");
        Require(!race.State.IsFinished && race.State.FindParticipant("player").Status == RaceParticipantStatus.Racing, "Laps never end deathmatch");
        race.RetireRacer("enemy-a"); race.RetireRacer("enemy-b"); race.Tick(.1f);
        Require(race.FinalRaceResult.Standings.Count(s => s.IsWinner) == 2 && fixture.Session.CareerRun.Player.RacesWon == 1,
            "Multiple individual winners record wins");

        fixture = new Fixture(new DeathmatchRules(DeathmatchVictoryMode.Individual, 1, 100, 0));
        race = fixture.Race; race.StartRace();
        race.RetireRacer("enemy-a"); race.RetireRacer("enemy-b"); race.Tick(1);
        Require(!race.State.IsFinished, "Individual teammates compete independently");
        race.RetireRacer("partner"); race.Tick(1);
        Require(race.FinalRaceResult.Standings.Single(s => s.IsWinner).RacerId == "player", "Last racer standing");

        fixture = new Fixture(new DeathmatchRules(DeathmatchVictoryMode.Team, 1, 2, 0));
        race = fixture.Race; race.StartRace();
        race.ApplyDamage("player", "enemy-a", 100000, DamageCause.Weapon);
        race.Tick(2);
        Require(race.State.IsFinished && race.FinalRaceResult.Standings.Where(s => s.IsWinner).All(s => s.TeamId == "team"),
            "Timeout ranks surviving team members first");
        Require(race.FinalRaceResult.Standings.Single(s => s.RacerId == "player").Eliminations == 1,
            "Eliminations attributed once");

        fixture = new Fixture(new DeathmatchRules(DeathmatchVictoryMode.Individual, 1, 2, 0));
        race = fixture.Race; race.StartRace();
        race.ApplyDamage("player", "enemy-a", 100000, DamageCause.Weapon); race.Tick(2);
        Require(race.FinalRaceResult.Standings.Single(s => s.IsWinner).RacerId == "player", "Timeout breaks survivor tie by eliminations");

        fixture = new Fixture(new DeathmatchRules(DeathmatchVictoryMode.Team, 1, 1, 0, 15, 8, true));
        race = fixture.Race; race.StartRace(); race.Tick(1);
        Require(race.FinalRaceResult.Standings.All(s => s.IsWinner && s.Position == 1), "Exact timeout ties share rank");

        fixture = new Fixture(new DeathmatchRules(DeathmatchVictoryMode.Individual, 1, 1, 0));
        race = fixture.Race; race.StartRace(); race.Tick(1);
        Require(race.FinalRaceResult.Standings.Count(s => s.IsWinner) == 1, "Default timeout ties respect strict winner cap");

        fixture = new Fixture(new DeathmatchRules(DeathmatchVictoryMode.Individual, 1, 100, 30, 0, 1));
        race = fixture.Race; race.StartRace(); race.Tick(1);
        Require(race.State.IsFinished && !race.FinalRaceResult.Standings.Any(s => s.IsWinner), "Simultaneous speed elimination has no arbitrary winner");

        fixture = new Fixture(new DeathmatchRules(DeathmatchVictoryMode.Team, 1, 10, 0));
        race = fixture.Race; race.StartRace();
        race.ApplyDamage("enemy-a", "player", 100000, DamageCause.Weapon);
        Require(!fixture.Session.CareerRun.IsActive && !race.State.IsFinished, "Player death preserves live team contest");
        race.RetireRacer("enemy-a"); race.RetireRacer("enemy-b"); race.Tick(1);
        Require(race.PostRaceResult.PlayerDied && fixture.Session.PlayerTeam.Credits == 1100, "Death persists despite team victory");
        reloaded = fixture.Reload();
        Require(!reloaded.CareerRun.IsActive && reloaded.PlayerTeam.Garage.Bikes[0].IsDestroyed, "Permanent loss survives JSON");

        fixture = new Fixture(new DeathmatchRules(DeathmatchVictoryMode.Team, 1, 123, 42, 7, 3));
        reloaded = fixture.Reload();
        var active = reloaded.PlayerTeam.Calendar.Active;
        Require(active.RestoreDeathmatch().MinimumSpeedKph == 42 && active.RestoreDeathmatch().TimeLimitSeconds == 123,
            "Paid rules survive JSON");
        var resumed = fixture.Prepare(reloaded);
        resumed.UseDeathmatchRules(new DeathmatchRules(DeathmatchVictoryMode.Individual, 2, 4, 0));
        Require(fixture.Calendar.Register(reloaded, "dm", resumed).IsSuccess &&
            resumed.State.Deathmatch.MinimumSpeedKph == 42 && resumed.InstanceId == fixture.Race.InstanceId && reloaded.PlayerTeam.Credits == 100,
            "Resume restores paid rules and identity without charging twice");
        var corrupt = reloaded.PlayerTeam.Calendar.Export(); corrupt.active.minimumSpeedKph = -1;
        Require(!CareerCalendarState.Restore(corrupt).IsSuccess, "Corrupt deathmatch rules rejected");
        bool rejected = false;
        try { new DeathmatchRules(DeathmatchVictoryMode.Team, 0); } catch (ArgumentException) { rejected = true; }
        Require(rejected, "Invalid winner count rejected");
        Debug.Log("Deathmatch validation passed: survival modes, speed/grace, timeout, ties, kills, permanent death, settlement and JSON retry.");
    }

    private sealed class Fixture
    {
        public readonly GameDatabase Database = new GameDatabase();
        public readonly CareerCalendarService Calendar;
        public readonly GameSessionState Session;
        public readonly RaceDirector Race;
        public Fixture(DeathmatchRules rules)
        {
            Database.AddBikeDefinition(new BikeDefinition("bike", "Bike", 0, 0, 0, 100, 1, 100));
            Database.AddEngineDefinition(new EngineDefinition("engine", "Engine", default, 100, 10, 0, null));
            Database.AddChassisDefinition(new ChassisDefinition("chassis", "Chassis", 1, 1, 0, null));
            Database.AddRaceDefinition(new RaceDefinition("race", "Deathmatch", "track", default, 1, 4, 2, 7, rules));
            Database.AddCareerEventDefinition(new CareerEventDefinition("dm", "Deathmatch", "", CareerEventKind.Deathmatch,
                0, 100, new[] { "race" }, new[] { 1000, 800, 500, 100 }, new int[0], new int[0]));
            Calendar = new CareerCalendarService(Database);
            var team = new TeamState("team", "Team", "#fff", "#000");
            var player = new RacerState("player", "Player", "team", true);
            team.Roster.AddRacer(player); team.Roster.AddRacer(new RacerState("partner", "Partner", "team", false));
            team.AddCredits(200);
            var opponent = new TeamState("opponent", "Opponent", "#fff", "#000");
            opponent.Roster.AddRacer(new RacerState("enemy-a", "Enemy A", "opponent", false));
            opponent.Roster.AddRacer(new RacerState("enemy-b", "Enemy B", "opponent", false));
            AddBikes(team); AddBikes(opponent);
            Session = new GameSessionState(team, new CareerRun("career", team, player), WorldState.Restore(new[] { opponent }).Value,
                "partner", team.Garage.Bikes[0].BikeId, team.Garage.Bikes[1].BikeId);
            Require(Calendar.Refresh(team) && team.Calendar.DrawIds.Contains("dm"), "Deathmatch appears on calendar");
            Race = Prepare(Session);
            var entry = Calendar.Register(Session, "dm", Race); Require(entry.IsSuccess, "Entry: " + entry.ErrorMessage);
        }
        public RaceDirector Prepare(GameSessionState session)
        {
            var career = new CareerManager(new CharacterFactory());
            var builds = new BikeBuildFactory(Database, new VehicleFactory(), new EquipmentFactory());
            var sessions = new GameSessionManager(Database, career, new WorldFactory(Database, builds), builds);
            Require(sessions.RestoreSession(session).IsSuccess, "Restore session");
            var participants = new RaceParticipantFactory(Database, new BikePerformanceCalculator(Database));
            var factory = new RaceFactory(new RaceGridValidator(new RaceEligibilityService()));
            var result = new RacePreparationService(Database, sessions, new RaceEntryBuilder(participants, factory, career)).PrepareSelectedRace("race");
            Require(result.IsSuccess, "Prepare: " + result.ErrorMessage); return result.Value;
        }
        public GameSessionState Reload()
        {
            var mapper = new CampaignSaveMapper(Database);
            var captured = mapper.Capture(Session); Require(captured.IsSuccess, "Capture: " + captured.ErrorMessage);
            var loaded = mapper.Restore(JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(captured.Value)));
            Require(loaded.IsSuccess, "Reload: " + loaded.ErrorMessage); return loaded.Value;
        }
        private void AddBikes(TeamState team)
        {
            var factory = new VehicleFactory();
            for (int i = 0; i < 2; i++)
            {
                var bike = factory.CreateBike(Database.GetBikeDefinition("bike"), "#fff", "#000");
                var engine = factory.CreateEngine(Database.GetEngineDefinition("engine"));
                var chassis = factory.CreateChassis(Database.GetChassisDefinition("chassis"));
                team.Garage.AddBike(bike); team.Garage.AddEngine(engine); team.Garage.AddChassis(chassis);
                Require(team.Garage.InstallEngine(bike.BikeId, engine.EngineId).IsSuccess &&
                    team.Garage.InstallChassis(bike.BikeId, chassis.ChassisId).IsSuccess, "Bike fixture");
            }
        }
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Deathmatch: " + message); }
}
