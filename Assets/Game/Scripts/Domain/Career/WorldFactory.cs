using System;
using RaceFatal.Data;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Career
{
    public sealed class WorldFactory
    {
        private readonly GameDatabase database;
        private readonly BikeBuildFactory bikeBuildFactory;

        public WorldFactory(
            GameDatabase database,
            BikeBuildFactory bikeBuildFactory)
        {
            this.database =
                database ??
                throw new ArgumentNullException(
                    nameof(database));

            this.bikeBuildFactory =
                bikeBuildFactory ??
                throw new ArgumentNullException(
                    nameof(bikeBuildFactory));
        }

        public Result<WorldState>
            CreateOpponentWorld()
        {
            var world =
                new WorldState();

            foreach (
                OpponentTeamDefinition definition
                in database.OpponentTeamDefinitions)
            {
                Result<TeamState> result =
                    CreateOpponentTeam(
                        definition);

                if (!result.IsSuccess)
                {
                    return Result<WorldState>.Failure(
                        result.ErrorMessage);
                }

                world.AddOpponentTeam(
                    result.Value);
            }

            return Result<WorldState>.Success(
                world);
        }

        private Result<TeamState>
            CreateOpponentTeam(
                OpponentTeamDefinition definition)
        {
            if (definition == null)
            {
                return Result<TeamState>.Failure(
                    "Opponent team definition is required.");
            }

            /*
             * Authored teams use their authored ID.
             *
             * This is preferable to a random GUID because
             * TeamState.TeamId can later resolve back to:
             *
             * - Team philosophy
             * - Visual content
             * - AI definitions
             * - Save data
             */
            var team =
                new TeamState(
                    definition.Id,
                    definition.DisplayName,
                    definition.PrimaryColor,
                    definition.SecondaryColor);

            Result racerResult =
                CreateRacers(
                    definition,
                    team);

            if (!racerResult.IsSuccess)
            {
                return Result<TeamState>.Failure(
                    racerResult.ErrorMessage);
            }

            Result garageResult =
                CreateGarage(
                    definition,
                    team);

            if (!garageResult.IsSuccess)
            {
                return Result<TeamState>.Failure(
                    garageResult.ErrorMessage);
            }

            return Result<TeamState>.Success(
                team);
        }

        private Result CreateRacers(
            OpponentTeamDefinition definition,
            TeamState team)
        {
            foreach (string definitionId
                     in definition.RacerDefinitionIds)
            {
                RacerDefinition racerDefinition =
                    database.GetRacerDefinition(
                        definitionId);

                if (racerDefinition == null)
                {
                    return Result.Failure(
                        $"Racer definition " +
                        $"'{definitionId}' was not found.");
                }

                /*
                 * Premade racers also use their authored
                 * definition ID as their persistent ID.
                 */
                var racer =
                    new RacerState(
                        racerDefinition.Id,
                        racerDefinition.DisplayName,
                        team.TeamId,
                        false);

                Result<RacerState> addResult =
                    team.Roster.AddRacer(
                        racer);

                if (!addResult.IsSuccess)
                {
                    return Result.Failure(
                        addResult.ErrorMessage);
                }
            }

            return Result.Success();
        }

        private Result CreateGarage(
            OpponentTeamDefinition definition,
            TeamState team)
        {
            foreach (string buildId
                     in definition.StartingBikeBuildIds)
            {
                BikeBuildDefinition build =
                    database.GetBikeBuildDefinition(
                        buildId);

                if (build == null)
                {
                    return Result.Failure(
                        $"Bike build '{buildId}' " +
                        $"for team '{definition.Id}' " +
                        "was not found.");
                }

                Result<BikeState> result =
                    bikeBuildFactory.CreateInGarage(
                        build,
                        team.Garage,
                        team.PrimaryColor,
                        team.SecondaryColor);

                if (!result.IsSuccess)
                {
                    return Result.Failure(
                        result.ErrorMessage);
                }
            }

            return Result.Success();
        }
    }
}