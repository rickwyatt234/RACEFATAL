using System;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Racing;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

static class Program
{
    static int checks;
    static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    static T Ok<T>(Result<T> result) { Check(result.IsSuccess, result.ErrorMessage); return result.Value; }
    static void Ok(Result result) => Check(result.IsSuccess, result.ErrorMessage);
    static void Main()
    {
        var db = new GameDatabase();
        db.AddBikeDefinition(new BikeDefinition("bike", "Bike", 1, 0, 0));
        db.AddEngineDefinition(new EngineDefinition("engine", "Engine", default, 100, 10, 100));
        db.AddChassisDefinition(new ChassisDefinition("chassis", "Chassis", 250, 1, 100));
        db.AddEquipmentDefinition(new ShieldDefinition("shield", "Shield", NodeSize.Small, 50, 5, 2, 100));
        db.AddBikeBuildDefinition(new BikeBuildDefinition("build", "Build", "bike", "engine", "chassis", Array.Empty<EquipmentMountDefinition>(), true, 1000));
        db.AddRacerDefinition(new RacerDefinition("partner", "Partner", .5f, .5f, .5f, .5f, .5f));
        db.AddRaceDefinition(new RaceDefinition("race", "Race", "track", default, 3, 2, 2));
        db.AddRaceDefinition(new RaceDefinition("race2", "Race2", "track2", default, 3, 2, 2));
        var term = new BrandContractTemplate { Id = "kills", Title = "Destroy three", Objective = BrandObjective.DestroyRacers, Target = 3, Credits = 500, StandingReward = 2 };
        db.AddBrandDefinition(new BrandDefinition("brand", "BRAND", "Tag", "#00ffff", BrandCategory.Shields, TeamPhilosophy.Defensive,
            new[] { new BrandProduct { Kind = ShopItemKind.Engine, DefinitionId = "engine", BasicStock = true }, new BrandProduct { Kind = ShopItemKind.Equipment, DefinitionId = "shield" }, new BrandProduct { Kind = ShopItemKind.Bike, DefinitionId = "build" } }, new[] { term }));
        var career = new CareerManager(new CharacterFactory());
        var builds = new BikeBuildFactory(db, new VehicleFactory(), new EquipmentFactory());
        var sessions = new GameSessionManager(db, career, new WorldFactory(db, builds), builds);
        var session = Ok(sessions.CreateNewSession(new NewGameRequest("Team", "#fff", "#000", "Player", "partner", "build", "build")));
        var team = session.PlayerTeam;
        var brands = new BrandService(db, new Random(42));
        var mapper = new CampaignSaveMapper(db);
        var player = session.CareerRun.Player;
        var shop = new ShopService(db);
        team.AddFame(3000);
        Check(team.Fame == 3000 && team.LifetimeFame == 3000, "New fame increases both ledgers");
        Check(!team.TrySpendFame(-1) && !team.TrySpendFame(3001), "Invalid spending rejected");
        Ok(brands.BuyPermit(team, BrandCategory.Shields));
        Check(team.Fame == 2500 && team.LifetimeFame == 3000, "Permit spends available fame only");
        Check(!brands.BuyPermit(team, BrandCategory.Shields).IsSuccess && team.Fame == 2500, "Duplicate permit cannot charge");
        Check(!brands.OpenBox(team, BrandCategory.Chassis).IsSuccess && team.Fame == 2500, "Unowned category cannot charge");
        var first = Ok(brands.OpenBox(team, BrandCategory.Shields));
        Check(first.newBrand && team.Brands.contracts.Count == 3 && team.Brands.relationships.Count == 1, "Box grants a permanent discovery and three separate contracts");
        Check(!brands.OpenBox(team).IsSuccess && team.Brands.contracts.Count == 3, "Pending reveal prevents a second purchase");
        Check(shop.MissingBrandAccess(team, ShopItemKind.Engine, "engine") == null && shop.MissingBrandAccess(team, ShopItemKind.Equipment, "shield") != null, "Basic stock opens; advanced stock stays gated");
        term.Target = 900;
        Check(team.Brands.contracts.All(c => c.terms.Target == 3), "Acquired terms are independent snapshots");
        term.Target = 3;
        var captured = Ok(mapper.Capture(session));
        team.Brands.contracts[0].progress = 1;
        Check(captured.playerTeam.brands.contracts[0].progress == 0, "Save capture does not alias live state");
        team.Brands.contracts[0].progress = 0;
        var roundtrip = Ok(mapper.Restore(UnityEngine.JsonUtility.FromJson<CampaignSaveData>(UnityEngine.JsonUtility.ToJson(captured))));
        Check(roundtrip.PlayerTeam.Brands.pendingReveal.id == first.id && roundtrip.PlayerTeam.Fame == team.Fame && roundtrip.PlayerTeam.LifetimeFame == 3000, "Box receipt and fame survive reload before reveal");
        team.Brands.pendingReveal = null;
        var duplicate = Ok(brands.OpenBox(team));
        Check(!duplicate.newBrand && team.Brands.contracts.Count == 6 && team.Brands.contracts.Select(c => c.instanceId).Distinct().Count() == 6, "Duplicate brand adds contracts without replacing earlier copies");
        team.Brands.pendingReveal = null;
        foreach (var contract in team.Brands.contracts.Take(3)) Ok(brands.SetActive(team, contract.instanceId, true));
        Check(!brands.SetActive(team, team.Brands.contracts[3].instanceId, true).IsSuccess, "Fourth active contract rejected");
        var resolver = new PostRaceResolutionService(new RaceRewardPolicy());
        RaceResult Result(int kills, int partnerKills, int place = 1, RaceParticipantStatus status = RaceParticipantStatus.Finished, string track = "track", bool finalized = true) => new RaceResult("race", new[] {
            new RaceResultEntry(player.RacerId, "Player", team.TeamId, "Team", place, 3, status, null, false, false, kills),
            new RaceResultEntry(session.SelectedPartnerRacerId, "Partner", team.TeamId, "Team", 1, 3, RaceParticipantStatus.Finished, null, false, false, partnerKills)
        }, isFinalized: finalized, trackId: track);
        var race = Result(1, 20);
        resolver.Resolve(race, team, player);
        Check(team.Brands.contracts.Take(3).All(c => c.progress == 1) && team.Brands.contracts.Skip(3).All(c => c.progress == 0), "Only player kills count; inactive contracts do not progress");
        int credits = team.Credits;
        resolver.Resolve(race, team, player);
        Check(team.Credits == credits && team.Brands.contracts[0].progress == 1, "Repeated settlement neither pays nor advances contracts");
        var afterRace = Ok(mapper.Restore(UnityEngine.JsonUtility.FromJson<CampaignSaveData>(UnityEngine.JsonUtility.ToJson(Ok(mapper.Capture(session))))));
        resolver.Resolve(race, afterRace.PlayerTeam, afterRace.CareerRun.Player);
        Check(afterRace.PlayerTeam.Brands.contracts[0].progress == 1, "Settlement replay remains idempotent after reload");
        try { resolver.Resolve(Result(3, 0, finalized: false), team, player); throw new Exception("Nonfinal accepted"); } catch (InvalidOperationException) { checks++; }
        resolver.Resolve(Result(2, 0, status: RaceParticipantStatus.Retired), team, player);
        Check(team.Brands.contracts.Take(3).All(c => c.completed && !c.active), "Valid DNF combat counts and completed contracts release slots");
        credits = team.Credits;
        Ok(brands.Claim(team, first.contractIds[0]));
        Check(team.Credits == credits + 500 && team.Brands.Find("brand").standing == 2, "Claim awards credits and permanent standing");
        Check(!brands.Claim(team, first.contractIds[0]).IsSuccess && team.Credits == credits + 500, "Claim cannot pay twice");
        var streak = team.Brands.contracts[3]; streak.terms.Objective = BrandObjective.ConsecutivePodiums; streak.terms.Target = 3;
        Ok(brands.SetActive(team, streak.instanceId, true));
        resolver.Resolve(Result(0, 0), team, player); resolver.Resolve(Result(0, 0, 3), team, player);
        Check(streak.progress == 2, "Consecutive podium progress");
        resolver.Resolve(Result(0, 0, 4), team, player); Check(streak.progress == 0, "Missed podium resets streak");
        resolver.Resolve(Result(0, 0), team, player); Ok(brands.SetActive(team, streak.instanceId, false));
        Check(streak.progress == 0, "Deactivation resets streak to prevent banking");
        var venue = team.Brands.contracts[4]; venue.terms.Objective = BrandObjective.WinRaces; venue.terms.Target = 1; venue.terms.TrackId = "track2";
        Ok(brands.SetActive(team, venue.instanceId, true));
        resolver.Resolve(Result(0, 0), team, player); Check(venue.progress == 0, "Wrong track ignored");
        resolver.Resolve(Result(0, 0, track: "track2"), team, player); Check(venue.completed, "Matching track win completes");
        venue.terms.RewardKind = BrandRewardKind.FreeItem; venue.terms.ItemKind = ShopItemKind.Equipment; venue.terms.ItemId = "shield";
        int inventory = team.Garage.Equipment.Count;
        Ok(brands.Claim(team, venue.instanceId));
        Check(team.Garage.Equipment.Count == inventory + 1 && shop.MissingBrandAccess(team, ShopItemKind.Equipment, "shield") == null, "Free item includes replacement access");
        team.Garage.Equipment.Last().Destroy();
        Check(shop.CanPurchase(team, ShopItemKind.Equipment, "shield").IsSuccess, "Destroyed exclusive can be repurchased");
        var access = team.Brands.contracts[1]; access.terms.RewardKind = BrandRewardKind.PurchaseAccess; access.terms.ItemKind = ShopItemKind.Bike; access.terms.ItemId = "build";
        int bikes = team.Garage.Bikes.Count;
        Ok(brands.Claim(team, access.instanceId));
        Check(team.Garage.Bikes.Count == bikes && shop.MissingBrandAccess(team, ShopItemKind.Bike, "build") == null, "Purchase-access reward does not grant inventory");
        var bikeReward = team.Brands.contracts[2]; bikeReward.terms.RewardKind = BrandRewardKind.FreeItem; bikeReward.terms.ItemKind = ShopItemKind.Bike; bikeReward.terms.ItemId = "build";
        Ok(brands.Claim(team, bikeReward.instanceId)); Check(team.Garage.Bikes.Count == bikes + 1 && team.Garage.Bikes.Last().IsRaceReady, "Complete-bike reward builds usable inventory");
        var missing = team.Brands.contracts[5]; missing.terms.Target = 1; missing.progress = 1; missing.completed = true; missing.terms.RewardKind = BrandRewardKind.FreeItem; missing.terms.ItemId = "removed";
        Check(!brands.Claim(team, missing.instanceId).IsSuccess && !missing.claimed, "Missing reward content retains claim");
        missing.terms.RewardKind = BrandRewardKind.Credits;
        var valid = Ok(mapper.Capture(session)); valid.saveVersion = 1; valid.playerTeam.hasLifetimeFame = false; valid.playerTeam.brands = null;
        var legacy = Ok(mapper.Restore(valid)); Check(legacy.PlayerTeam.Fame == legacy.PlayerTeam.LifetimeFame && legacy.PlayerTeam.Brands.contracts.Count == 0, "Legacy saves initialize lifetime fame and empty brand collections");
        var corrupt = Ok(mapper.Capture(session)); corrupt.playerTeam.lifetimeFame = -1;
        Check(!mapper.Restore(corrupt).IsSuccess, "Corrupt fame rejected");
        corrupt = Ok(mapper.Capture(session)); corrupt.playerTeam.brands.contracts.Add(corrupt.playerTeam.brands.contracts[0]);
        Check(!mapper.Restore(corrupt).IsSuccess, "Duplicate contract identity rejected");
        int count = team.Brands.contracts.Count;
        career.RetireCurrentRun(); career.StartNewRun("Successor"); Check(team.Brands.contracts.Count == count && team.Brands.Find("brand").standing >= 2, "Succession preserves team brand state");
        var opponent = new TeamState("opponent", "Opponent", "#fff", "#000");
        brands.ApplyOpponentPreferences(opponent, new OpponentTeamDefinition("opponent", "Opponent", "#fff", "#000", TeamPhilosophy.Defensive, Array.Empty<string>(), Array.Empty<string>()), 2000);
        Check(opponent.Brands.preferredBrandIds.SequenceEqual(new[]{"brand"}) && opponent.Brands.CanBuy("brand", db.BrandDefinitions["brand"].Products[1]), "Opponent philosophy determines brands and advancing access");
        brands.ApplyOpponentPreferences(opponent, db.GetOpponentTeamDefinition("unknown"), 0);
        Check(opponent.Brands.Find("brand").standing == 1000, "Opponent standing does not regress");
        // Real event registration proves rollback refunds available fame without inflating lifetime.
        db.AddCareerEventDefinition(new CareerEventDefinition("champ", "Champ", "", CareerEventKind.Championship, 400, 100, new[]{"race","race2"}, new[]{100}, new[]{1000}, new[]{25,18}, fameEntryFee:150));
        sessions.ClearSession();
        session = Ok(sessions.CreateNewSession(new NewGameRequest("Fresh", "#fff", "#000", "Driver", "partner", "build", "build")));
        team = session.PlayerTeam; team.AddFame(500); team.AddCredits(200);
        var calendar = new CareerCalendarService(db); calendar.Refresh(team);
        Check(team.TrySpendFame(400), "Spend before entry");
        Check(team.Calendar.UnlockedIds.Contains("champ") && !calendar.CanEnter(team,"champ").IsSuccess, "Lifetime eligibility survives spending, insufficient entry balance blocks entry");
        team.RefundFame(400);
        var prep = new RacePreparationService(db, sessions, new RaceEntryBuilder(new RaceParticipantFactory(db, new BikePerformanceCalculator(db)), new RaceFactory(new RaceGridValidator(new RaceEligibilityService())), career));
        var director = Ok(prep.PrepareSelectedRace("race"));
        var transaction = Ok(calendar.Register(session,"champ",director));
        Check(team.Fame == 350 && team.LifetimeFame == 500, "Championship charges available fame");
        transaction.Rollback(); transaction.Rollback(); Check(team.Fame == 500 && team.LifetimeFame == 500, "Entry rollback refunds once without lifetime inflation");
        var defaults = new GameDatabase(); BrandDefaults.Ensure(defaults); BrandDefaults.Ensure(defaults);
        Check(defaults.BrandDefinitions.Count == 14 && defaults.BrandDefinitions.Values.All(b => b.Contracts.Count >= 3), "Default catalog has 14 brands and repeat initialization is safe");
        CareerShopValidation.Run(); CareerTeamValidation.Run(); CareerTeamRecoveryValidation.Run();
        Console.WriteLine($"PASS: {checks} brand progression, persistence, payout, succession and entry-fee checks");
    }
}
