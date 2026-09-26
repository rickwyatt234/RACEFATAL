using System;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Racing;
using RaceFatal.Shared;
using RaceFatal.Vehicles;
using UnityEditor;
using UnityEngine;

public static class CareerSuccessionValidation
{
    private const int TestSlot = 1;

    [MenuItem("RACE//FATAL/Career/Validate Career Succession")]
    public static void Run()
    {
        ValidateCalendarNullSerialization();
        var db = new GameDatabase();
        db.AddBikeDefinition(new BikeDefinition("bike", "Bike", 0, 0, 0, 100, 1, 100));
        db.AddEngineDefinition(new EngineDefinition("engine", "Engine", default, 100, 10, 0, null));
        db.AddChassisDefinition(new ChassisDefinition("chassis", "Chassis", 1, 1, 0, null));
        db.AddBikeBuildDefinition(new BikeBuildDefinition("player_starter", "Starter", "bike", "engine", "chassis", Array.Empty<EquipmentMountDefinition>()));
        db.AddRacerPerkDefinition(new RacerPerkDefinition("starter-perk", "Energy Reserve", "More energy", 100, RacerPerkEffect.EnergyCapacity, 10));
        db.AddRacerPerkDefinition(new RacerPerkDefinition("ai-perk", "AI Pace", "AI only", 0, RacerPerkEffect.Pace, 1));
        var builds = new BikeBuildFactory(db, new VehicleFactory(), new EquipmentFactory());
        var manager = new CareerManager(new CharacterFactory());
        var sessions = new GameSessionManager(db, manager, new WorldFactory(db, builds), builds);
        var mapper = new CampaignSaveMapper(db);
        var team = new TeamState("team", "Continuity", "#ffffff", "#000000");
        var player = new RacerState("original", "Original", team.TeamId, true);
        var partner = new RacerState("partner", "Partner", team.TeamId, false);
        team.Roster.AddRacer(player); team.Roster.AddRacer(partner);
        player.RecordRaceEntered(); player.RecordFinish(1); player.Progression.AddFame(250);
        team.AddCredits(12000); team.AddFame(800); team.AddResearchPoints(1000);
        team.UnlockTechnology("legacy-tech");
        team.RestoreResearchContract(new ResearchContract("staff", "Staff", 10, 5));
        var bike = builds.CreateInGarage(db.GetBikeBuildDefinition("player_starter"), team.Garage, "#ffffff", "#000000").Value;
        var partnerBike = builds.CreateInGarage(db.GetBikeBuildDefinition("player_starter"), team.Garage, "#ffffff", "#000000").Value;
        var session = new GameSessionState(team, new CareerRun("original-run", team, player), WorldState.Restore(null).Value,
            partner.RacerId, bike.BikeId, partnerBike.BikeId);
        var fixture = mapper.Capture(session);
        RequireSuccess(fixture, "Capture fixture");
        RequireSuccess(mapper.Restore(fixture.Value), "Restore fixture before JSON");
        var repository = new MemoryRepository { Data = fixture.Value };
        var saves = new CampaignSaveService(sessions, mapper, repository);
        RequireSuccess(saves.LoadCampaign(TestSlot), "Load fixture");
        Require(!saves.StartSuccessor("Early").IsSuccess, "Cannot replace an active racer");
        team = sessions.Current.PlayerTeam; player = sessions.Current.CareerRun.Player;
        bike = team.Garage.FindBike(sessions.Current.SelectedPlayerBikeId);
        string wreckId = bike.BikeId;
        var calendar = team.Calendar.Export(); calendar.week = 8; calendar.drawWeek = 8;
        calendar.unlocked.Add("championship"); calendar.draw.Add("championship");
        calendar.active = Entry("fatal-attempt");
        Require(team.RestoreCalendar(calendar).IsSuccess, "Prepare championship");
        sessions.Current.CareerRun.EnterChampionship("championship");
        bike.Destroy(); manager.KillCurrentRun();
        Require(sessions.Current.CareerRun != null && !sessions.Current.CareerRun.IsActive &&
            sessions.Current.CareerRun.Player.Status == RacerCareerStatus.Dead, "Death retains ended run");
        Require(!saves.StartSuccessor("Too soon").IsSuccess, "Cannot bypass unsettled fatal event");
        team = sessions.Current.PlayerTeam; player = sessions.Current.CareerRun.Player;
        var fatal = new RaceResult("race", new[] {
            new RaceResultEntry(player.RacerId, team.TeamId, 1, 1, RaceParticipantStatus.Destroyed),
            new RaceResultEntry(partner.RacerId, team.TeamId, 2, 2, RaceParticipantStatus.Finished) }, "fatal-attempt", 7);
        manager.ResolvePostRace(fatal, player);
        Require(team.Calendar.Week == 9 && team.Calendar.Active == null && team.Calendar.LastEvent.withdrawn,
            "Fatal results settle before succession and close championship");
        Require(saves.SaveCurrentCampaign().IsSuccess, "Save death");
        Reload(saves);
        var summary = saves.GetSlotSummary(TestSlot).Value;
        Require(summary.HasCareerRun && !summary.CareerActive && summary.RacerName == "Original", "Ended save summary");
        team = sessions.Current.PlayerTeam;
        string preserved = JsonUtility.ToJson(repository.Data.playerTeam);
        Require(!saves.StartSuccessor("  ").IsSuccess && !saves.StartSuccessor(new string('x', 41)).IsSuccess &&
            !saves.StartSuccessor("bad\nname").IsSuccess, "Invalid names rejected");
        repository.Fail = true;
        Require(!saves.StartSuccessor("Unsaved").IsSuccess, "Failed write reports error");
        Require(!sessions.Current.CareerRun.IsActive && sessions.Current.PlayerTeam.Roster.Racers.Count == 2 &&
            sessions.Current.PlayerTeam.Garage.Bikes.Count == 2 &&
            JsonUtility.ToJson(mapper.Capture(sessions.Current).Value.playerTeam) == preserved, "Failed save rolls back racer and recovery kit");
        repository.Fail = false;
        repository.ThrowOnSave = true;
        Require(!saves.StartSuccessor("Exception").IsSuccess && sessions.Current.PlayerTeam.Roster.Racers.Count == 2,
            "Thrown write failure also rolls back");
        repository.ThrowOnSave = false;
        int credits = sessions.Current.PlayerTeam.Credits, rp = sessions.Current.PlayerTeam.ResearchPoints;
        int fame = sessions.Current.PlayerTeam.Fame;
        Require(saves.StartSuccessor("  Successor  ").IsSuccess, "Create successor and recovery bike");
        var next = sessions.Current.CareerRun.Player;
        string nextId = next.RacerId;
        team = sessions.Current.PlayerTeam;
        Require(next.Name == "Successor" && nextId != "original" && next.Progression.Fame == 0 && next.RacesEntered == 0,
            "Fresh identity and personal progression");
        Require(team.Credits == credits && team.ResearchPoints == rp && team.Fame == fame && team.Calendar.Week == 9 &&
            team.HasTechnology("legacy-tech") && team.ResearchContracts[0].RacesRemaining == 4,
            "Team economy, technology, staff and calendar preserved");
        Require(team.Garage.Bikes.Count == 3 && team.Garage.FindBike(wreckId).IsDestroyed &&
            team.Garage.FindBike(sessions.Current.SelectedPlayerBikeId).IsRaceReady &&
            sessions.Current.SelectedPartnerRacerId == partner.RacerId && sessions.Current.SelectedPartnerBikeId == partnerBike.BikeId,
            "Fresh recovery bike assigned; wreck and partner preserved");
        Require(next.Progression.HasPurchasedPerk("starter-perk") && !next.Progression.HasPurchasedPerk("ai-perk") &&
            sessions.Current.CareerRun.NeedsIntroduction, "Player-compatible starter perk and pending summary");
        var old = team.Roster.FindRacer("original");
        Require(old.Status == RacerCareerStatus.Dead && old.RacesWon == 1 && old.RacesEntered == 1,
            "Previous career history remains permanent");
        manager.ResolvePostRace(fatal, old);
        Require(team.Credits == credits && team.Calendar.Week == 9, "Old result cannot pay again after succession");
        Require(!saves.StartSuccessor("Duplicate").IsSuccess, "No repeated successor or kit");
        Reload(saves);
        Require(sessions.Current.CareerRun.Player.RacerId == nextId && sessions.Current.PlayerTeam.Garage.Bikes.Count == 3,
            "Successor and one recovery kit survive reload");
        Require(sessions.Current.CareerRun.NeedsIntroduction && sessions.Current.CareerRun.StartingPerkId == "starter-perk" &&
            sessions.Current.CareerRun.Player.Progression.PurchasedPerkIds.Count == 1, "Unseen summary and perk survive reload without reroll");
        repository.Fail = true;
        Require(!saves.AcknowledgeNewRacer().IsSuccess && sessions.Current.CareerRun.NeedsIntroduction, "Summary dismissal failure can retry");
        repository.Fail = false;
        Require(saves.AcknowledgeNewRacer().IsSuccess, "Acknowledge new racer summary");
        Reload(saves);
        Require(!sessions.Current.CareerRun.NeedsIntroduction, "Acknowledged summary stays dismissed");
        int week = sessions.Current.PlayerTeam.Calendar.Week;
        repository.Fail = true;
        Require(!saves.RetirePlayer().IsSuccess && sessions.Current.CareerRun.IsActive && sessions.Current.CareerRun.Player.CanRace,
            "Failed retirement save restores active racer");
        repository.Fail = false;
        Require(saves.RetirePlayer().IsSuccess && sessions.Current.CareerRun.Player.Status == RacerCareerStatus.Retired &&
            sessions.Current.PlayerTeam.Calendar.Week == week, "Retirement changes status without skipping a free week");
        Require(saves.StartSuccessor("Third").IsSuccess && sessions.Current.PlayerTeam.Garage.Bikes.Count == 3,
            "Retirement with ready bike grants no extra kit");
        calendar = sessions.Current.PlayerTeam.Calendar.Export(); calendar.active = Entry("pending");
        Require(sessions.Current.PlayerTeam.RestoreCalendar(calendar).IsSuccess, "Prepare interrupted paid event");
        Require(!saves.RetirePlayer().IsSuccess && sessions.Current.CareerRun.IsActive, "Pending paid race must be resolved or withdrawn first");
        calendar.active.pendingInstanceId = null; calendar.active.pendingRaceId = null; calendar.active.roundIndex = 1;
        sessions.Current.PlayerTeam.RestoreCalendar(calendar);
        Require(saves.RetirePlayer().IsSuccess && sessions.Current.PlayerTeam.Calendar.Active == null &&
            sessions.Current.PlayerTeam.Calendar.LastEvent.withdrawn && sessions.Current.PlayerTeam.Calendar.LastEvent.finalPrize == 0 &&
            sessions.Current.PlayerTeam.Calendar.Week == week + 1, "Mid-championship retirement withdraws and advances exactly once");
        Require(!saves.RetirePlayer().IsSuccess, "Repeated retirement rejected");
        Reload(saves);
        Require(sessions.Current.CareerRun.Player.Status == RacerCareerStatus.Retired, "Retired status survives reload");
        // Migrate legacy saves that cleared the run pointer at death/retirement.
        var legacy = mapper.Capture(sessions.Current).Value; legacy.careerRun = null; legacy.successorStarterBuildId = null;
        saves.CloseCurrentCampaign(false); repository.Data = legacy;
        RequireSuccess(saves.LoadCampaign(TestSlot), "Load legacy null-run campaign");
        Require(saves.StartSuccessor("Legacy successor").IsSuccess, "Legacy null-run campaign continues");
        Require(saves.RetirePlayer().IsSuccess, "Retire legacy successor");
        team = sessions.Current.PlayerTeam;
        team.Garage.FindBike(sessions.Current.SelectedPlayerBikeId).Destroy();
        var spare = builds.CreateInGarage(db.GetBikeBuildDefinition("player_starter"), team.Garage, "#ffffff", "#000000").Value;
        int bikesBefore = team.Garage.Bikes.Count;
        Require(saves.StartSuccessor("Spare rider").IsSuccess && sessions.Current.SelectedPlayerBikeId == spare.BikeId &&
            sessions.Current.PlayerTeam.Garage.Bikes.Count == bikesBefore, "Reuse an owned ready spare before granting recovery");
        // Missing recovery content fails before changing roster/inventory.
        saves.RetirePlayer();
        sessions.Current.PlayerTeam.Garage.FindBike(sessions.Current.SelectedPlayerBikeId).Destroy();
        var missing = mapper.Capture(sessions.Current).Value; missing.successorStarterBuildId = "missing-build";
        saves.CloseCurrentCampaign(false); repository.Data = missing;
        RequireSuccess(saves.LoadCampaign(TestSlot), "Load missing-content fixture");
        int rosterCount = sessions.Current.PlayerTeam.Roster.Racers.Count;
        Require(!saves.StartSuccessor("Unavailable kit").IsSuccess && sessions.Current.PlayerTeam.Roster.Racers.Count == rosterCount &&
            sessions.Current.PlayerTeam.Garage.Bikes.Count == bikesBefore, "Missing content grants no partial kit or racer");
        Debug.Log("Career succession validation passed: death settlement, retirement, team preservation, recovery, legacy saves, replay and save-failure rollback.");
    }

    private static void ValidateCalendarNullSerialization()
    {
        var empty = new CareerCalendarState().Export();
        var roundTrip = JsonUtility.FromJson<CareerCalendarData>(JsonUtility.ToJson(empty));
        var restored = CareerCalendarState.Restore(roundTrip);
        RequireSuccess(restored, "Empty calendar JSON round trip");
        Require(restored.Value.Active == null && restored.Value.LastEvent == null && restored.Value.Week == 1 &&
            restored.Value.Export().seed == empty.seed, "Empty events remain absent without resetting calendar");

        var sentinel = empty.Copy();
        sentinel.active = new CareerEventEntryData(); sentinel.lastEvent = new CareerEventEntryData();
        RequireSuccess(CareerCalendarState.Restore(sentinel), "Default inline event placeholders");
        Require(sentinel.active != null && sentinel.lastEvent != null, "Restore does not mutate input snapshot");
        sentinel.active.eventId = "broken-event";
        Require(!CareerCalendarState.Restore(sentinel).IsSuccess, "Partially populated event is not discarded");
        sentinel.active = new CareerEventEntryData { entryFee = 100 };
        Require(!CareerCalendarState.Restore(sentinel).IsSuccess, "Missing identity with a paid fee remains invalid");
        sentinel.active = null; sentinel.lastEvent.completed = true;
        Require(!CareerCalendarState.Restore(sentinel).IsSuccess, "Incomplete history is not discarded");
        sentinel.lastEvent = null; sentinel.week = 0;
        Require(!CareerCalendarState.Restore(sentinel).IsSuccess, "Invalid week remains rejected");
        Require(CareerRunSaveData.IsAbsent(new CareerRunSaveData()), "Empty inline career run is absent");
        Require(!CareerRunSaveData.IsAbsent(new CareerRunSaveData { playerRacerId = "broken" }),
            "Partially populated career run still requires validation");
    }

    private static CareerEventEntryData Entry(string pending) => new CareerEventEntryData {
        eventId = "championship", displayName = "Championship", kind = CareerEventKind.Championship,
        raceIds = new System.Collections.Generic.List<string> { "race", "race" },
        payouts = new System.Collections.Generic.List<int> { 100, 50 },
        prizes = new System.Collections.Generic.List<int> { 500 },
        points = new System.Collections.Generic.List<int> { 10, 5 },
        standings = new System.Collections.Generic.List<ChampionshipStandingData> {
            new ChampionshipStandingData { teamId = "team", teamName = "Continuity" } },
        pendingInstanceId = pending, pendingRaceId = "race" };
    private static void Reload(CampaignSaveService saves)
    {
        Require(saves.CloseCurrentCampaign(false).IsSuccess, "Close campaign");
        RequireSuccess(saves.LoadCampaign(TestSlot), "Reload campaign");
    }
    private static void RequireSuccess<T>(Result<T> result, string step)
    {
        Require(result.IsSuccess, step + ": " + result.ErrorMessage);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class MemoryRepository : ICampaignSaveRepository
    {
        public CampaignSaveData Data;
        public bool Fail, ThrowOnSave;
        public int SlotCount => 3;
        public bool Exists(int slotIndex) => slotIndex == TestSlot && Data != null;
        public Result Save(int slotIndex, CampaignSaveData data)
        {
            if (ThrowOnSave) throw new InvalidOperationException("Simulated disk exception");
            if (Fail) return Result.Failure("Simulated disk failure");
            Data = Copy(data); return Result.Success();
        }
        public Result<CampaignSaveData> Load(int slotIndex) => Result<CampaignSaveData>.Success(Copy(Data));
        public Result Delete(int slotIndex) { Data = null; return Result.Success(); }
        private static CampaignSaveData Copy(CampaignSaveData data) => JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(data));
    }
}
