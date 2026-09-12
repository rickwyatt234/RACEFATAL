using System;
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
                    partnerBikeResult.Value.BikeId);

            return Result<GameSessionState>.Success(
                Current);
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