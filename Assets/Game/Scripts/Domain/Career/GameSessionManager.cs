using System;
using System.Linq;
using RaceFatal.Data;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Career
{
    public class GameSessionManager
    {
        private readonly GameDatabase database;
        private readonly CareerManager careerManager;
        private readonly WorldFactory worldFactory;
        private readonly BikeBuildFactory bikeBuildFactory;

        public GameSessionState Current { get; private set; }

        public bool HasSession =>
            Current != null;

        public GameSessionManager(
            GameDatabase database,
            CareerManager careerManager,
            WorldFactory worldFactory,
            BikeBuildFactory bikeBuildFactory)
        {
            this.database =
                database
                ?? throw new ArgumentNullException(
                    nameof(database));

            this.careerManager =
                careerManager
                ?? throw new ArgumentNullException(
                    nameof(careerManager));

            this.worldFactory =
                worldFactory
                ?? throw new ArgumentNullException(
                    nameof(worldFactory));

            this.bikeBuildFactory =
                bikeBuildFactory
                ?? throw new ArgumentNullException(
                    nameof(bikeBuildFactory));

            this.careerManager.CurrentRunChanged +=
                OnCurrentRunChanged;
        }

        public Result<GameSessionState> CreateNewSession(
            NewGameRequest request)
        {
            if (request == null)
            {
                return Result<GameSessionState>.Failure(
                    "New game request is required.");
            }

            if (HasSession)
            {
                return Result<GameSessionState>.Failure(
                    "A game session is already active.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.TeamName))
            {
                return Result<GameSessionState>.Failure(
                    "Team name is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.PlayerName))
            {
                return Result<GameSessionState>.Failure(
                    "Player racer name is required.");
            }

            // -------------------------------------------------
            // PLAYER TEAM
            // -------------------------------------------------

            string teamId =
                Guid.NewGuid()
                    .ToString("N");

            TeamState team =
                new TeamState(
                    teamId,
                    request.TeamName,
                    request.PrimaryColor,
                    request.SecondaryColor);

            // -------------------------------------------------
            // PLAYER
            // -------------------------------------------------

            RacerState player =
                new RacerState(
                    Guid.NewGuid()
                        .ToString("N"),
                    request.PlayerName,
                    team.TeamId,
                    true);

            Result<RacerState> addPlayer =
                team.Roster.AddRacer(
                    player);

            if (!addPlayer.IsSuccess)
            {
                return Result<GameSessionState>.Failure(
                    addPlayer.ErrorMessage);
            }

            // -------------------------------------------------
            // PARTNER
            // -------------------------------------------------

            RacerDefinition partnerDefinition =
                database.GetRacerDefinition(
                    request.PartnerDefinitionId);

            if (partnerDefinition == null)
            {
                return Result<GameSessionState>.Failure(
                    $"Partner racer definition " +
                    $"'{request.PartnerDefinitionId}' " +
                    "was not found.");
            }

            RacerState partner =
                new RacerState(
                    partnerDefinition.Id,
                    partnerDefinition.DisplayName,
                    team.TeamId,
                    false);

            Result<RacerState> addPartner =
                team.Roster.AddRacer(
                    partner);

            if (!addPartner.IsSuccess)
            {
                return Result<GameSessionState>.Failure(
                    addPartner.ErrorMessage);
            }

            // -------------------------------------------------
            // PLAYER STARTER BIKE
            // -------------------------------------------------

            BikeBuildDefinition playerBuild =
                database.GetBikeBuildDefinition(
                    request.PlayerStarterBuildId);

            if (playerBuild == null)
            {
                return Result<GameSessionState>.Failure(
                    $"Player starter build " +
                    $"'{request.PlayerStarterBuildId}' " +
                    "was not found.");
            }

            Result<BikeState> playerBikeResult =
                bikeBuildFactory.CreateInGarage(
                    playerBuild,
                    team.Garage,
                    team.PrimaryColor,
                    team.SecondaryColor);

            if (!playerBikeResult.IsSuccess)
            {
                return Result<GameSessionState>.Failure(
                    playerBikeResult.ErrorMessage);
            }

            // -------------------------------------------------
            // PARTNER STARTER BIKE
            // -------------------------------------------------

            BikeBuildDefinition partnerBuild =
                database.GetBikeBuildDefinition(
                    request.PartnerStarterBuildId);

            if (partnerBuild == null)
            {
                return Result<GameSessionState>.Failure(
                    $"Partner starter build " +
                    $"'{request.PartnerStarterBuildId}' " +
                    "was not found.");
            }

            Result<BikeState> partnerBikeResult =
                bikeBuildFactory.CreateInGarage(
                    partnerBuild,
                    team.Garage,
                    team.PrimaryColor,
                    team.SecondaryColor);

            if (!partnerBikeResult.IsSuccess)
            {
                return Result<GameSessionState>.Failure(
                    partnerBikeResult.ErrorMessage);
            }

            // -------------------------------------------------
            // OPPONENT WORLD
            // -------------------------------------------------

            Result<WorldState> worldResult =
                worldFactory.CreateOpponentWorld();

            if (!worldResult.IsSuccess)
            {
                return Result<GameSessionState>.Failure(
                    worldResult.ErrorMessage);
            }

            // -------------------------------------------------
            // PLAYER CAREER
            // -------------------------------------------------

            CareerRun careerRun =
                new CareerRun(
                    Guid.NewGuid().ToString("N"),
                    team,
                    player);

            careerRun.PrepareIntroduction("NEW CAMPAIGN STARTER BIKE", GrantStartingPerk(player));

            careerManager.InitializeNewGame(
                team,
                careerRun);

            // -------------------------------------------------
            // SESSION
            // -------------------------------------------------

            Current =
                new GameSessionState(
                    team,
                    careerRun,
                    worldResult.Value,
                    partner.RacerId,
                    playerBikeResult.Value.BikeId,
                    partnerBikeResult.Value.BikeId,
                    successorStarterBuildId: request.PlayerStarterBuildId);

            return Result<GameSessionState>.Success(
                Current);
        }

        public Result RestoreSession(
            GameSessionState session)
        {
            if (session == null)
            {
                return Result.Failure(
                    "Game session is required.");
            }

            if (HasSession)
            {
                return Result.Failure(
                    "A game session is already active.");
            }

            try
            {
                careerManager.RestoreState(
                    session.PlayerTeam,
                    session.CareerRun);

                Current =
                    session;

                return Result.Success();
            }
            catch (Exception exception)
            {
                careerManager.Clear();
                Current = null;

                return Result.Failure(
                    "Failed to restore game session: " +
                    exception.Message);
            }
        }

        public Result StartSuccessor(string name)
        {
            if (Current == null) return Result.Failure("No campaign is loaded.");
            if (careerManager.HasActiveRun) return Result.Failure("Your current racer is still active.");
            if (Current.PlayerTeam.Calendar.Active != null)
                return Result.Failure("Resolve or withdraw from the previous event before creating a racer.");
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 40 || name.Any(char.IsControl))
                return Result.Failure("Enter a racer name of 1–40 characters.");
            try
            {
                string bikeSource = "RETAINED TEAM BIKE";
                var garage = Current.PlayerTeam.Garage;
                var bike = garage.FindBike(Current.SelectedPlayerBikeId);
                if (bike?.IsRaceReady != true)
                {
                    bikeSource = "READY SPARE ASSIGNED";
                    bike = garage.Bikes.FirstOrDefault(b => b.IsRaceReady && b.BikeId != Current.SelectedPartnerBikeId);
                    if (bike == null)
                    {
                        var build = database.GetBikeBuildDefinition(Current.SuccessorStarterBuildId);
                        if (build == null) return Result.Failure("The campaign's successor starter build is missing from the content catalog.");
                        // Build in a temporary garage first: invalid authored content cannot
                        // leave half a recovery kit in the team's persistent inventory.
                        var kit = new GarageState();
                        var created = bikeBuildFactory.CreateInGarage(build, kit,
                            Current.PlayerTeam.PrimaryColor, Current.PlayerTeam.SecondaryColor);
                        if (!created.IsSuccess) return Result.Failure(created.ErrorMessage);
                        foreach (var item in kit.Bikes) garage.AddBike(item);
                        foreach (var item in kit.Engines) garage.AddEngine(item);
                        foreach (var item in kit.Chassis) garage.AddChassis(item);
                        foreach (var item in kit.Equipment) garage.AddEquipment(item);
                        bike = created.Value;
                        bikeSource = "FRESH STARTER RECOVERY BIKE — NO CREDIT COST";
                    }
                    var assigned = Current.SelectPlayerBike(bike.BikeId);
                    if (!assigned.IsSuccess) return assigned;
                }
                var run = careerManager.StartNewRun(name);
                run.PrepareIntroduction(bikeSource, GrantStartingPerk(run.Player));
                return Result.Success();
            }
            catch (ArgumentException exception) { return Result.Failure(exception.Message); }
            catch (InvalidOperationException exception) { return Result.Failure(exception.Message); }
        }

        private string GrantStartingPerk(RacerState player)
        {
            // Player-supported effects only; draw from the cheapest authored tier.
            // Granted once at creation, and saved before showing the introduction.
            var candidates = database.RacerPerkDefinitions.Values.Where(p =>
                p.Effect == RacerPerkEffect.EnergyCapacity && p.FameCost >= 0 && p.Strength > 0 &&
                !float.IsNaN(p.Strength) && !float.IsInfinity(p.Strength)).ToList();
            if (candidates.Count == 0) return null;
            int tierCost = candidates.Min(p => p.FameCost);
            candidates = candidates.Where(p => p.FameCost == tierCost).OrderBy(p => p.Id, StringComparer.Ordinal).ToList();
            var perk = candidates[new Random().Next(candidates.Count)];
            player.Progression.TryPurchasePerk(perk.Id, 0);
            return perk.Id;
        }

        public Result AcknowledgeNewRacer()
        {
            if (Current?.CareerRun?.IsActive != true) return Result.Failure("No active racer.");
            Current.CareerRun.AcknowledgeIntroduction();
            return Result.Success();
        }

        public Result RetirePlayer()
        {
            if (Current?.CareerRun?.IsActive != true) return Result.Failure("No active racer to retire.");
            try { careerManager.RetireCurrentRun(); return Result.Success(); }
            catch (InvalidOperationException exception) { return Result.Failure(exception.Message); }
        }

        public void ClearSession()
        {
            Current = null;

            careerManager.Clear();
        }

        private void OnCurrentRunChanged(
            CareerRun run)
        {
            if (Current == null)
                return;

            Current.SetCareerRun(
                run);
        }
    }
}
