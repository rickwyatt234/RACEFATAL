using System;
using System.Collections.Generic;
using RaceFatal.Career;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Racing
{
    public class RaceEntryBuilder
    {
        private readonly RaceParticipantFactory
            participantFactory;

        private readonly RaceFactory
            raceFactory;

        private readonly CareerManager
            careerManager;

        public RaceEntryBuilder(
            RaceParticipantFactory participantFactory,
            RaceFactory raceFactory,
            CareerManager careerManager)
        {
            this.participantFactory =
                participantFactory
                ?? throw new ArgumentNullException(
                    nameof(participantFactory));

            this.raceFactory =
                raceFactory
                ?? throw new ArgumentNullException(
                    nameof(raceFactory));

            this.careerManager =
                careerManager
                ?? throw new ArgumentNullException(
                    nameof(careerManager));
        }

        public Result<RaceDirector> Build(
            RaceDefinition race,
            CareerRun playerRun,
            BikeState playerBike,
            RacerState playerPartner,
            BikeState partnerBike,
            IReadOnlyList<TeamState> opponentTeams)
        {
            if (race == null)
            {
                return Result<RaceDirector>.Failure(
                    "Race is required.");
            }

            if (playerRun == null ||
                !playerRun.IsActive)
            {
                return Result<RaceDirector>.Failure(
                    "An active player career is required.");
            }

            // Current RACE//FATAL race format:
            // two racers per team.
            if (race.TeamSize != 2)
            {
                return Result<RaceDirector>.Failure(
                    "Current race entry builder requires two racers per team.");
            }

            RacerState player =
                playerRun.Player;

            TeamState playerTeam =
                playerRun.Team;

            Result playerValidation =
                ValidatePlayerSelections(
                    race,
                    playerTeam,
                    player,
                    playerBike,
                    playerPartner,
                    partnerBike);

            if (!playerValidation.IsSuccess)
            {
                return Result<RaceDirector>.Failure(
                    playerValidation.ErrorMessage);
            }

            int totalTeamCount =
                race.EntrantCount /
                race.TeamSize;

            int requiredOpponentTeams =
                totalTeamCount - 1;

            if (opponentTeams == null ||
                opponentTeams.Count !=
                requiredOpponentTeams)
            {
                return Result<RaceDirector>.Failure(
                    $"Race requires exactly " +
                    $"{requiredOpponentTeams} opponent teams.");
            }

            var participants =
                new List<RaceParticipant>(
                    race.EntrantCount);

            // -------------------------------------------------
            // PLAYER TEAM
            // -------------------------------------------------

            Result<RaceParticipant>
                playerResult =
                    participantFactory.Create(
                        player,
                        playerBike,
                        RaceParticipantRole.Player);

            if (!playerResult.IsSuccess)
            {
                return Result<RaceDirector>.Failure(
                    playerResult.ErrorMessage);
            }

            participants.Add(
                playerResult.Value);

            Result<RaceParticipant>
                partnerResult =
                    participantFactory.Create(
                        playerPartner,
                        partnerBike,
                        RaceParticipantRole.PlayerPartner);

            if (!partnerResult.IsSuccess)
            {
                return Result<RaceDirector>.Failure(
                    partnerResult.ErrorMessage);
            }

            participants.Add(
                partnerResult.Value);

            // -------------------------------------------------
            // OPPONENT TEAMS
            // -------------------------------------------------

            var usedTeamIds =
                new HashSet<string>
                {
                    playerTeam.TeamId
                };

            foreach (TeamState team
                     in opponentTeams)
            {
                if (team == null)
                {
                    return Result<RaceDirector>.Failure(
                        "Opponent team cannot be null.");
                }

                if (!usedTeamIds.Add(
                        team.TeamId))
                {
                    return Result<RaceDirector>.Failure(
                        $"Team '{team.TeamId}' appears more than once.");
                }

                Result addResult =
                    AddOpponentTeam(
                        race,
                        team,
                        participants);

                if (!addResult.IsSuccess)
                {
                    return Result<RaceDirector>.Failure(
                        addResult.ErrorMessage);
                }
            }

            if (participants.Count !=
                race.EntrantCount)
            {
                return Result<RaceDirector>.Failure(
                    $"Expected {race.EntrantCount} racers, " +
                    $"but built {participants.Count}.");
            }

            return raceFactory.Create(
                race,
                participants,
                careerManager);
        }

        private Result ValidatePlayerSelections(
            RaceDefinition race,
            TeamState team,
            RacerState player,
            BikeState playerBike,
            RacerState partner,
            BikeState partnerBike)
        {
            if (team == null)
            {
                return Result.Failure(
                    "Player team is required.");
            }

            if (player == null ||
                !player.CanRace)
            {
                return Result.Failure(
                    "Player racer cannot enter this race.");
            }

            if (partner == null ||
                !partner.CanRace)
            {
                return Result.Failure(
                    "Player partner cannot enter this race.");
            }

            if (player.RacerId ==
                partner.RacerId)
            {
                return Result.Failure(
                    "Player and partner must be different racers.");
            }

            if (player.TeamId !=
                    team.TeamId ||
                partner.TeamId !=
                    team.TeamId)
            {
                return Result.Failure(
                    "Player and partner must belong to the player team.");
            }

            if (playerBike == null ||
                partnerBike == null)
            {
                return Result.Failure(
                    "Both player-team bikes are required.");
            }

            if (playerBike.BikeId ==
                partnerBike.BikeId)
            {
                return Result.Failure(
                    "Player and partner cannot use the same bike.");
            }

            if (!IsCorrectClass(
                    playerBike,
                    race.EngineClass))
            {
                return Result.Failure(
                    "Player bike is not eligible for this engine class.");
            }

            if (!IsCorrectClass(
                    partnerBike,
                    race.EngineClass))
            {
                return Result.Failure(
                    "Partner bike is not eligible for this engine class.");
            }

            return Result.Success();
        }

        private Result AddOpponentTeam(
            RaceDefinition race,
            TeamState team,
            List<RaceParticipant> participants)
        {
            IReadOnlyList<RacerState>
                eligibleRacers =
                    team.Roster
                        .GetRaceEligibleRacers();

            if (eligibleRacers.Count <
                race.TeamSize)
            {
                return Result.Failure(
                    $"Team '{team.TeamName}' does not have " +
                    $"{race.TeamSize} active racers.");
            }

            IReadOnlyList<BikeState>
                eligibleBikes =
                    team.Garage
                        .GetRaceReadyBikesFor(
                            race.EngineClass);

            if (eligibleBikes.Count <
                race.TeamSize)
            {
                return Result.Failure(
                    $"Team '{team.TeamName}' does not have " +
                    $"{race.TeamSize} race-ready " +
                    $"{race.EngineClass} bikes.");
            }

            // Prototype selection policy:
            // choose the first eligible pair.
            //
            // Later this becomes an opponent selection /
            // team strategy service.
            for (int i = 0;
                 i < race.TeamSize;
                 i++)
            {
                RacerState racer =
                    eligibleRacers[i];

                BikeState bike =
                    eligibleBikes[i];

                Result<RaceParticipant>
                    participantResult =
                        participantFactory.Create(
                            racer,
                            bike,
                            RaceParticipantRole.Opponent);

                if (!participantResult.IsSuccess)
                {
                    return Result.Failure(
                        participantResult.ErrorMessage);
                }
                participants.Add(
                    participantResult.Value);
            }

            return Result.Success();
        }

        private static bool IsCorrectClass(
            BikeState bike,
            EngineClass engineClass)
        {
            return bike != null &&
                   bike.IsRaceReady &&
                   bike.EngineClass.HasValue &&
                   bike.EngineClass.Value ==
                       engineClass;
        }
    }
}