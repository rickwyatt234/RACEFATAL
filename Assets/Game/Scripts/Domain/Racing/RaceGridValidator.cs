using System.Collections.Generic;
using RaceFatal.Shared;

namespace RaceFatal.Racing
{
    public class RaceGridValidator
    {
        private readonly RaceEligibilityService eligibilityService;

        public RaceGridValidator(RaceEligibilityService eligibilityService)
        {
            this.eligibilityService = eligibilityService;
        }

        public Result<bool> ValidateRaceGrid(RaceDefinition raceDefinition, List<RaceParticipant> participants)
        {
            if (raceDefinition == null)
            {
                return Result<bool>.Failure("Race definition is null.");
            }
            if (participants == null || participants.Count == 0)
            {
                return Result<bool>.Failure("No participants in the race grid.");
            }
            if (participants.Count != raceDefinition.EntrantCount)
            {
                return Result<bool>.Failure($"Number of participants ({participants.Count}) does not match the required entrant count ({raceDefinition.EntrantCount}).");
            }

            var racerIds = new HashSet<string>();
            var bikeIds = new HashSet<string>();

            var teamCounts = new Dictionary<string, int>();

            RaceParticipant player = null;
            RaceParticipant playerPartner = null;

            foreach (RaceParticipant participant in participants)
            {
                if (participant == null)
                {
                    return Result<bool>.Failure("A participant in the race grid is null.");
                }
                if (!racerIds.Add(participant.Racer.RacerId))
                {
                    return Result<bool>.Failure($"Duplicate racer found: {participant.Racer.RacerId}");
                }
                if (!bikeIds.Add(participant.Bike.BikeId))
                {
                    return Result<bool>.Failure($"Duplicate bike found: {participant.Bike.BikeId}");
                }

                Result<bool> eligibilityResult = eligibilityService.CheckBike(raceDefinition, participant.Bike);
                if (!eligibilityResult.IsSuccess)
                {
                    return eligibilityResult;
                }
                if (!teamCounts.ContainsKey(participant.TeamId))
                {
                    teamCounts[participant.TeamId] = 0;
                }
                teamCounts[participant.TeamId]++;

                if (participant.Role == RaceParticipantRole.Player)
                {
                    if (player != null)
                    {
                        return Result<bool>.Failure("Multiple players found in the race grid.");
                    }
                    player = participant;
                }
                if (participant.Role == RaceParticipantRole.PlayerPartner)
                {
                    if (playerPartner != null)
                    {
                        return Result<bool>.Failure("Multiple player partners found in the race grid.");
                    }
                    playerPartner = participant;
                }
            }
            if (player == null)
            {
                return Result<bool>.Failure("No player found in the race grid.");
            }
            if (player.TeamId != playerPartner?.TeamId)
            {
                return Result<bool>.Failure("Player and player partner are not on the same team.");
            }

            foreach (var pair in teamCounts)
            {
                if (pair.Value != raceDefinition.TeamSize)
                {
                    return Result<bool>.Failure($"Team {pair.Key} has {pair.Value} participants, but the required team size is {raceDefinition.TeamSize}.");
                }
            }

            return Result<bool>.Success(true);
        }
    }
}
