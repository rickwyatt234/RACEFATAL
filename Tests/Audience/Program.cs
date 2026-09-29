using System;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Combat;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Racing;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

static class Program
{
    static int checks;
    static void Equal(double actual, double expected, string message, double tolerance = 0.001)
    {
        checks++;
        if (double.IsNaN(actual) || Math.Abs(actual - expected) > tolerance)
            throw new Exception($"{message}: expected {expected}, got {actual}");
    }

    static void True(bool condition, string message) => Equal(condition ? 1 : 0, 1, message);
    sealed class Fixture
    {
        public readonly GameDatabase Database = new GameDatabase();
        public readonly TeamState Team = new TeamState("team", "Team", "white", "black");
        public readonly RaceParticipant Player, Enemy, Friend;
        public readonly RaceDirector Director;
        public Fixture(AudienceSettings settings = null, bool shield = false, DeathmatchRules deathmatch = null)
        {
            var platform = new BikeDefinition("bike", "Bike", 2, 0, 0);
            var chassis = new ChassisDefinition("chassis", "Chassis", 100, 1, 0, null, maxIntegrity: 10000);
            var engine = new EngineDefinition("engine", "Engine", default, 100, 10, 0, null);
            var shieldDefinition = new ShieldDefinition("shield", "Shield", NodeSize.Small, 20, 1, 1, 0, null);
            Database.AddBikeDefinition(platform);
            Database.AddChassisDefinition(chassis);
            Database.AddEngineDefinition(engine);
            Database.AddEquipmentDefinition(shieldDefinition);
            var booster = new BoosterDefinition("booster", "Booster", NodeSize.Small, 1, 1, 1, 0, null, 100);
            Database.AddEquipmentDefinition(booster);
            var factory = new VehicleFactory();
            var participants = new RaceParticipantFactory(Database, new BikePerformanceCalculator(Database));
            RaceParticipant Make(string id, string team, RaceParticipantRole role)
            {
                var bike = factory.CreateBike(platform, "white", "black");
                bike.Loadout.InstallEngine(factory.CreateEngine(engine));
                bike.Loadout.InstallChassis(factory.CreateChassis(chassis));
                bike.Loadout.InstallEquipment(new EquipmentFactory().Create(booster), NodeSize.Small, 1);
                if (shield)
                    bike.Loadout.InstallEquipment(new EquipmentFactory().Create(shieldDefinition), NodeSize.Small, 0);
                var racer = new RacerState(id, id, team, role == RaceParticipantRole.Player);
                if (team == Team.TeamId)
                    Team.Roster.AddRacer(racer);
                return participants.Create(racer, bike, role).Value;
            }

            Player = Make("player", "team", RaceParticipantRole.Player);
            Enemy = Make("enemy", "other", RaceParticipantRole.Opponent);
            Friend = Make("friend", "team", RaceParticipantRole.PlayerPartner);
            var career = new CareerManager(new CharacterFactory());
            career.LoadTeam(Team);
            Director = new RaceDirector(new RaceState(new RaceDefinition("race", "Race", "track", default, 2, 3, 1, deathmatch: deathmatch), new[] { Player, Enemy, Friend }), career);
            if (settings != null)
                Director.ConfigureAudience(settings);
        }

        public void Hit(float damage = 1f, string victim = "enemy", DamageCause cause = DamageCause.Weapon, bool ram = false) => Director.ApplyDamage("player", victim, damage, cause, isRamAttack: ram);
    }

    static void Main()
    {
        Equal(RaceAudience.MultiplierFor(0), .5, "Minimum multiplier");
        Equal(RaceAudience.MultiplierFor(100), 1, "Neutral multiplier");
        Equal(RaceAudience.MultiplierFor(200), 1.5, "Maximum multiplier");
        Equal(RaceAudience.MultiplierFor(float.NaN), 1, "Invalid favor falls back to neutral");
        var f = new Fixture();
        f.Hit();
        f.Director.Tick(60);
        Equal(f.Director.Audience.Favor, 100, "No countdown scoring or decay");
        f.Director.StartRace();
        f.Director.Tick(4);
        Equal(f.Director.Audience.Favor, 100, "Grace period");
        f.Director.Tick(10);
        Equal(f.Director.Audience.Favor, 85, "Idle decay");
        Equal(f.Director.Audience.AverageFavor, (400 + 925) / 14.0, "Time-weighted average");
        f.Director.Tick(float.NaN);
        f.Director.Tick(float.PositiveInfinity);
        f.Director.Tick(-1);
        Equal(f.Director.Audience.ActiveSeconds, 14, "Invalid ticks ignored");
        f = new Fixture();
        f.Director.StartRace();
        f.Director.Tick(100);
        Equal(f.Director.Audience.Favor, 0, "Decay clamps at floor");
        Equal(f.Director.Audience.AverageFavor, (400 + 10000 / 3.0) / 100, "Floor integration");
        var sliced = new Fixture();
        sliced.Director.StartRace();
        for (int i = 0; i < 1000; i++)
            sliced.Director.Tick(.1f);
        Equal(sliced.Director.Audience.AverageFavor, f.Director.Audience.AverageFavor, "Frame-rate independent average", .003);
        f = new Fixture();
        f.Director.StartRace();
        f.Hit();
        Equal(f.Director.Audience.Favor, 102.04, "Successful weapon hit");
        for (int i = 0; i < 1000; i++)
            f.Hit();
        Equal(f.Director.Audience.Favor, 107, "Pellet and tick burst budget", .004);
        f.Hit(victim: "friend");
        f.Hit(victim: "player");
        f.Hit(cause: DamageCause.Environmental);
        Equal(f.Director.Audience.Favor, 107, "Friendly, self and environmental hits ignored", .004);
        True(!f.Director.ApplyDamage("player", "enemy", float.NaN, DamageCause.Weapon).IsSuccess, "Reject invalid damage");
        f.Director.Tick(1);
        f.Hit();
        f.Director.Tick(1);
        f.Hit();
        True(f.Director.Audience.LastAction == "SUSTAINED FIRE", "Sustained hits award excitement");
        f.Director.Tick(3);
        f.Hit();
        True(f.Director.Audience.LastAction == "WEAPON HIT", "Gap resets sustained streak");
        f = new Fixture(shield: true);
        f.Director.StartRace();
        f.Hit(20);
        Equal(f.Director.Audience.Favor, 114.8, "Shield-break reward");
        f.Hit(20000);
        True(f.Enemy.Status == RaceParticipantStatus.Destroyed, "Destruction transition");
        float afterKill = f.Director.Audience.Favor;
        True(f.Director.Audience.LastAction == "RACER DESTROYED", "Destruction reward");
        f.Hit(20000);
        Equal(f.Director.Audience.Favor, afterKill, "No repeat destruction reward");
        f = new Fixture();
        f.Director.StartRace();
        f.Hit(cause: DamageCause.Collision);
        Equal(f.Director.Audience.Favor, 105.04, "Collision favor");
        f = new Fixture();
        f.Director.StartRace();
        f.Hit(ram: true);
        Equal(f.Director.Audience.Favor, 110.04, "Ram favor");
        f = new Fixture(new AudienceSettings { decayPerSecond = 0 });
        f.Director.StartRace();
        f.Director.ReportCourseProgress("player", .1f);
        f.Director.ReportCourseProgress("enemy", .11f);
        f.Director.Tick(.1f);
        Equal(f.Director.Audience.Favor, 100, "Initial order awards nothing");
        f.Director.ReportCourseProgress("player", .12f);
        f.Director.Tick(.1f);
        Equal(f.Director.Audience.Favor, 103, "Clean pass");
        f.Director.ReportCourseProgress("player", .1f);
        f.Director.Tick(.1f);
        f.Director.ReportCourseProgress("player", .12f);
        f.Director.Tick(.1f);
        Equal(f.Director.Audience.Favor, 103, "Repeated pass cooldown");
        f.Director.ReportCourseProgress("player", .1f);
        f.Director.Tick(13);
        f.Director.RetireRacer("enemy");
        f.Director.ReportCourseProgress("player", .12f);
        f.Director.Tick(.1f);
        Equal(f.Director.Audience.Favor, 103, "Eliminated opponent is not an overtake");
        f = new Fixture(new AudienceSettings { decayPerSecond = 0 });
        f.Director.StartRace();
        f.Director.ReportCourseProgress("player", .99f);
        f.Director.ReportCourseProgress("enemy", .995f);
        f.Director.Tick(.1f);
        f.Director.ReportCourseProgress("player", .01f);
        f.Director.Tick(.1f);
        Equal(f.Director.Audience.Favor, 103, "Pass across start line");
        f = new Fixture(new AudienceSettings { decayPerSecond = 0 });
        f.Director.StartRace();
        f.Director.ReportCourseProgress("player", .1f);
        f.Director.ReportCourseProgress("enemy", .12f);
        f.Director.Tick(.1f);
        f.Director.ReportCourseProgress("enemy", .09f);
        f.Director.Tick(.1f);
        Equal(f.Director.Audience.Favor, 100, "Stationary player cannot farm reversing opponent");
        f = new Fixture();
        f.Director.StartRace();
        f.Director.Tick(30);
        f.Director.RetireRacer("player");
        float average = f.Director.Audience.AverageFavor, current = f.Director.Audience.Favor;
        f.Director.Tick(60);
        f.Hit();
        Equal(f.Director.Audience.AverageFavor, average, "Retirement freezes average");
        Equal(f.Director.Audience.Favor, current, "Retirement freezes favor");
        var result = f.Director.ResolveRemainingRace();
        Equal(result.Standings.Single(e => e.RacerId == "player").AverageAudienceFavor, average, "Final result captures average");
        Equal(f.Director.PostRaceResult.AverageAudienceFavor, average, "Settlement captures average");
        int fame = f.Team.Fame;
        f.Director.CompleteRace();
        Equal(f.Team.Fame, fame, "Settlement cannot award twice");
        var session = new GameSessionState(f.Team, null, WorldState.Restore(null).Value, null, null, null);
        var mapper = new CampaignSaveMapper(f.Database);
        var saved = mapper.Capture(session);
        True(saved.IsSuccess, "Capture settled race");
        var restored = mapper.Restore(saved.Value);
        True(restored.IsSuccess, "Restore settled race: " + restored.ErrorMessage);
        Equal(restored.Value.PlayerTeam.SettledRaceResults[result.InstanceId].AverageAudienceFavor, average, "Save preserves audience");
        saved.Value.playerTeam.settledRaceResults[0].hasAudienceFavor = false;
        restored = mapper.Restore(saved.Value);
        Equal(restored.Value.PlayerTeam.SettledRaceResults[result.InstanceId].AverageAudienceFavor, 100, "Legacy save is neutral");
        f = new Fixture();
        f.Director.StartRace();
        f.Director.Tick(12);
        f.Player.Finish(1, 12, false);
        average = f.Director.Audience.AverageFavor;
        f.Director.Tick(100);
        f.Hit();
        Equal(f.Director.Audience.AverageFavor, average, "Finished player freezes audience");
        f = new Fixture();
        f.Director.StartRace();
        f.Director.Tick(12);
        f.Director.ApplyDamage("enemy", "player", 20000, DamageCause.Weapon);
        average = f.Director.Audience.AverageFavor;
        f.Director.Tick(100);
        Equal(f.Director.Audience.AverageFavor, average, "Destroyed player freezes audience");
        f = new Fixture(deathmatch: new DeathmatchRules(DeathmatchVictoryMode.Individual, timeLimitSeconds: 10, minimumSpeedKph: 0));
        f.Director.StartRace();
        f.Hit(victim: "friend");
        True(f.Director.Audience.Favor > 100, "FFA allows same-team combat");
        f.Director.Tick(30);
        Equal(f.Director.Audience.ActiveSeconds, 10, "Deathmatch time limit clips audience interval");
        True(f.Director.State.IsFinished, "Deathmatch finalizes");
        var policy = new RaceRewardPolicy();
        foreach (float favor in new[]
        {
            0f,
            100f,
            200f
        }

        )
        {
            var reward = policy.ApplyAudience(policy.Calculate(1, RaceParticipantStatus.Finished), favor);
            Equal(reward.TeamFame, 50 + favor / 2, "Team fame adjustment");
            Equal(reward.CharacterFame, 50 + favor / 2, "Character fame adjustment");
            Equal(reward.Credits, 10000, "Credits unaffected");
            Equal(reward.ResearchPoints, 50, "Research unaffected");
        }

        Equal(new PostRaceResult("r", "p", 1, RaceParticipantStatus.Finished, policy.Calculate(1, RaceParticipantStatus.Finished), false, false).AudienceFameMultiplier, 1, "Legacy receipt neutral default");
        Console.WriteLine($"PASS: {checks} audience checks.");
    }
}
