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

public static class CareerTeamRecoveryValidation
{
    private const int Slot = 1;
    [MenuItem("RACE//FATAL/Career/Validate Bike Shop and Recovery")]
    public static void Run()
    {
        var db = new GameDatabase();
        db.AddBikeDefinition(new BikeDefinition("bike", "Combat Bike", 1, 0, 0, 100, 1, 100));
        db.AddEngineDefinition(new EngineDefinition("engine", "Engine", default, 100, 10, 100, null));
        db.AddEngineDefinition(new EngineDefinition("locked-engine", "Advanced", default, 120, 12, 200, "engine-tech"));
        db.AddChassisDefinition(new ChassisDefinition("chassis", "Chassis", 1, 1, 50, null));
        db.AddEquipmentDefinition(new ShieldDefinition("shield", "Shield", default, 100, 10, 2, 25, "shield-tech"));
        db.AddRacerDefinition(new RacerDefinition("partner-template", "Partner", .5f, .5f, .5f, .5f, .5f));
        db.AddBikeBuildDefinition(new BikeBuildDefinition("player_starter", "Starter", "bike", "engine", "chassis",
            Array.Empty<EquipmentMountDefinition>(), true, 1000));
        var mount = new EquipmentMountDefinition("shield", default, 0);
        db.AddBikeBuildDefinition(new BikeBuildDefinition("advanced", "Advanced", "bike", "locked-engine", "chassis", new[] { mount }, true, 2000, "build-tech"));
        db.AddBikeBuildDefinition(new BikeBuildDefinition("invalid", "Invalid", "bike", "engine", "chassis", new[] { mount, mount }, true, 500));
        db.AddBikeBuildDefinition(new BikeBuildDefinition("hidden", "AI only", "bike", "engine", "chassis", Array.Empty<EquipmentMountDefinition>()));
        var shop = new ShopService(db);
        var buyer = new TeamState("buyer", "Buyer", "#123456", "#abcdef"); buyer.AddCredits(6000);
        Require(shop.FindOffer(ShopItemKind.Bike, "hidden") == null, "Unlisted AI builds not sold");
        Require(!shop.Purchase(buyer, ShopItemKind.Bike, "advanced").IsSuccess, "Build technology required");
        buyer.UnlockTechnology("build-tech");
        Require(!shop.Purchase(buyer, ShopItemKind.Bike, "advanced").IsSuccess, "Bundled engine technology required");
        buyer.UnlockTechnology("engine-tech");
        Require(!shop.Purchase(buyer, ShopItemKind.Bike, "advanced").IsSuccess, "Bundled equipment technology required");
        buyer.UnlockTechnology("shield-tech");
        Require(!shop.Purchase(buyer, ShopItemKind.Bike, "invalid").IsSuccess && buyer.Garage.Bikes.Count == 0 && buyer.Credits == 6000,
            "Invalid duplicate mounts cannot charge or leave partial inventory");
        Success(shop.Purchase(buyer, ShopItemKind.Bike, "advanced"), "Buy advanced bike");
        Success(shop.Purchase(buyer, ShopItemKind.Bike, "advanced"), "Buy second copy");
        Require(buyer.Credits == 2000 && buyer.Garage.Bikes.Count == 2 && buyer.Garage.Equipment.Count == 2 &&
            buyer.Garage.Bikes.All(b => b.IsRaceReady && b.PrimaryColor == buyer.PrimaryColor), "Complete bikes use team paint and exact charge");
        Require(buyer.Garage.Bikes.Select(b => b.BikeId).Distinct().Count() == 2 &&
            buyer.Garage.Engines.Select(e => e.EngineId).Distinct().Count() == 2 &&
            buyer.Garage.Chassis.Select(c => c.ChassisId).Distinct().Count() == 2 &&
            buyer.Garage.Equipment.Select(e => e.EquipmentId).Distinct().Count() == 2,
            "Every purchased bike/component has a unique physical ID");

        var factory = new BikeBuildFactory(db, new VehicleFactory(), new EquipmentFactory());
        var manager = new CareerManager(new CharacterFactory());
        var sessions = new GameSessionManager(db, manager, new WorldFactory(db, factory), factory);
        var mapper = new CampaignSaveMapper(db);
        var team = new TeamState("team", "Team", "#ffffff", "#000000");
        var player = new RacerState("player", "Player", team.TeamId, true);
        var deadPartner = new RacerState("partner-template", "Original Partner", team.TeamId, false);
        team.Roster.AddRacer(player); team.Roster.AddRacer(deadPartner); deadPartner.Kill();
        team.AddCredits(300); team.AddFame(25); team.AddResearchPoints(40);
        var build = db.GetBikeBuildDefinition("player_starter");
        var wreckA = factory.CreateInGarage(build, team.Garage, "#ffffff", "#000000").Value;
        var wreckB = factory.CreateInGarage(build, team.Garage, "#ffffff", "#000000").Value;
        wreckA.Destroy(); wreckB.Destroy();
        var session = new GameSessionState(team, new CareerRun("run", team, player), WorldState.Restore(null).Value,
            deadPartner.RacerId, wreckA.BikeId, wreckB.BikeId);
        var repo = new MemoryRepository { Data = mapper.Capture(session).Value };
        var saves = new CampaignSaveService(sessions, mapper, repo);
        Success(saves.LoadCampaign(Slot), "Load recovery fixture");
        var recovery = new TeamRecoveryService(db);
        var quote = recovery.Quote(sessions.Current); Success(quote, "Quote recovery");
        Require(quote.Value.NewBikes == 2 && quote.Value.RecruitPartner && quote.Value.TotalCost == 7000 &&
            quote.Value.Charge == 300 && quote.Value.Assistance == 6700, "Recovery spends available credits and covers only shortfall");
        Require(TeamPreparationService.Inspect(sessions.Current).Count(c => !c.Ready) >= 3, "Checklist shows multiple missing requirements");
        repo.Fail = true;
        Require(!saves.RecoverTeam(quote.Value.Signature).IsSuccess && sessions.Current.PlayerTeam.Credits == 300 &&
            sessions.Current.PlayerTeam.Garage.Bikes.Count == 2 && sessions.Current.PlayerTeam.Roster.Racers.Count == 2 &&
            string.IsNullOrEmpty(sessions.Current.PlayerTeam.RecoverySupportCheckpoint), "Save failure rolls back charge, kit, replacement and support receipt");
        repo.Fail = false;
        Success(saves.RecoverTeam(quote.Value.Signature), "Save team recovery");
        team = sessions.Current.PlayerTeam;
        var replacement = team.Roster.FindRacer(sessions.Current.SelectedPartnerRacerId);
        Require(replacement.RacerId != deadPartner.RacerId && replacement.DefinitionId == "partner-template" && replacement.CanRace &&
            team.Roster.FindRacer(deadPartner.RacerId).Status == RacerCareerStatus.Dead,
            "Replacement has a fresh identity and valid AI content; old racer stays dead");
        Require(team.Credits == 0 && team.Fame == 25 && team.ResearchPoints == 40 && team.Calendar.Week == 1 &&
            team.Garage.FindBike(wreckA.BikeId).IsDestroyed && team.Garage.FindBike(wreckB.BikeId).IsDestroyed,
            "Recovery preserves losses, calendar and progression");
        Require(TeamPreparationService.Inspect(sessions.Current).All(c => c.Ready), "Recovered assignments pass team checklist");
        Require(!saves.RecoverTeam(quote.Value.Signature).IsSuccess, "Repeated confirmation grants nothing");
        Reload(saves);
        team = sessions.Current.PlayerTeam;
        replacement = team.Roster.FindRacer(sessions.Current.SelectedPartnerRacerId);
        Require(replacement.DefinitionId == "partner-template" && team.RecoverySupportCheckpoint == "0" &&
            team.Garage.Bikes.Count == 4, "AI profile, kit, assignments and support receipt survive JSON reload");
        var participant = new RaceParticipantFactory(db, new BikePerformanceCalculator(db))
            .Create(replacement, team.Garage.FindBike(sessions.Current.SelectedPartnerBikeId), RaceParticipantRole.PlayerPartner);
        Success(participant, "Prepare recovered partner for race");
        team.Garage.FindBike(sessions.Current.SelectedPartnerBikeId).Destroy();
        Require(!recovery.Quote(sessions.Current).IsSuccess, "No repeat subsidy before another completed race");
        team.AddCredits(1000);
        var funded = recovery.Quote(sessions.Current); Success(funded, "Fully funded repair after support");
        Require(funded.Value.NewBikes == 1 && funded.Value.Assistance == 0 && !funded.Value.RecruitPartner,
            "Funded recovery buys only the missing bike");
        Success(saves.RecoverTeam(funded.Value.Signature), "Funded recovery");
        team = sessions.Current.PlayerTeam;
        team.AddCredits(2000);
        int beforeCount = team.Garage.Bikes.Count;
        repo.Fail = true;
        Require(!saves.PurchaseShopItem(ShopItemKind.Bike, "player_starter").IsSuccess && sessions.Current.PlayerTeam.Credits == 2000 &&
            sessions.Current.PlayerTeam.Garage.Bikes.Count == beforeCount, "Failed purchase save rolls back funds and complete kit");
        repo.Fail = false;
        Success(saves.PurchaseShopItem(ShopItemKind.Bike, "player_starter"), "Saved shop bike purchase");
        Reload(saves);
        Require(sessions.Current.PlayerTeam.Credits == 1000 && sessions.Current.PlayerTeam.Garage.Bikes.Count == beforeCount + 1,
            "Purchased bike persists exactly once");
        // A ready spare and active reserve should cost nothing.
        team = sessions.Current.PlayerTeam;
        var currentPartner = team.Roster.FindRacer(sessions.Current.SelectedPartnerRacerId); currentPartner.Kill();
        var reserve = new RacerState("reserve", "Reserve", team.TeamId, false, "partner-template"); team.Roster.AddRacer(reserve);
        var selectedBike = team.Garage.FindBike(sessions.Current.SelectedPartnerBikeId); selectedBike.Destroy();
        var spareQuote = recovery.Quote(sessions.Current); Success(spareQuote, "Use reserves");
        Require(spareQuote.Value.NewBikes == 0 && !spareQuote.Value.RecruitPartner && spareQuote.Value.TotalCost == 0,
            "Ready spare and active reserve are used before purchases");
        team.AddCredits(1);
        Require(!saves.RecoverTeam(spareQuote.Value.Signature).IsSuccess, "Stale quote cannot silently change the charge");
        var fresh = recovery.Quote(sessions.Current); Success(fresh, "Refresh quote");
        Success(saves.RecoverTeam(fresh.Value.Signature), "Assign reserve team");
        Require(sessions.Current.SelectedPartnerRacerId == reserve.RacerId, "Reserve assigned");
        team = sessions.Current.PlayerTeam;
        // Settling a real result, rather than skipping weeks or reloading, renews assistance.
        var raceResult = new RaceResult("recovery-race", new[] {
            new RaceResultEntry(sessions.Current.CareerRun.Player.RacerId, team.TeamId, 1, 3, RaceParticipantStatus.Finished),
            new RaceResultEntry(reserve.RacerId, team.TeamId, 2, 3, RaceParticipantStatus.Finished) }, "completed-recovery-race");
        manager.ResolvePostRace(raceResult, sessions.Current.CareerRun.Player);
        team.TrySpendCredits(team.Credits);
        team.Garage.FindBike(sessions.Current.SelectedPartnerBikeId).Destroy();
        var renewed = recovery.Quote(sessions.Current); Success(renewed, "Support renewed after race settlement");
        Require(renewed.Value.Assistance == 1000 && renewed.Value.Charge == 0, "Only replacement shortfall is covered after race");
        Success(saves.RecoverTeam(renewed.Value.Signature), "Renewed recovery");
        Reload(saves);
        Require(sessions.Current.PlayerTeam.RecoverySupportCheckpoint == "1", "Renewed support receipt persists");
        manager.RetireCurrentRun();
        Require(!recovery.Quote(sessions.Current).IsSuccess, "Recovery cannot bypass career succession");
        Debug.Log("Bike shop and recovery validation passed: bundle gates, unique inventory, saved purchases, support limits, permanent losses, AI profile persistence and preparation.");
    }
    private static void Reload(CampaignSaveService saves)
    {
        Success(saves.CloseCurrentCampaign(false), "Close campaign"); Success(saves.LoadCampaign(Slot), "Reload campaign");
    }
    private static void Success(Result result, string step) { Require(result.IsSuccess, step + ": " + result.ErrorMessage); }
    private static void Success<T>(Result<T> result, string step) { Require(result.IsSuccess, step + ": " + result.ErrorMessage); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class MemoryRepository : ICampaignSaveRepository
    {
        public CampaignSaveData Data; public bool Fail;
        public int SlotCount => 3;
        public bool Exists(int slotIndex) => slotIndex == Slot && Data != null;
        public Result Save(int slotIndex, CampaignSaveData data)
        { if (Fail) return Result.Failure("Simulated disk failure"); Data = Copy(data); return Result.Success(); }
        public Result<CampaignSaveData> Load(int slotIndex) => Result<CampaignSaveData>.Success(Copy(Data));
        public Result Delete(int slotIndex) { Data = null; return Result.Success(); }
        private static CampaignSaveData Copy(CampaignSaveData data) => JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(data));
    }
}
