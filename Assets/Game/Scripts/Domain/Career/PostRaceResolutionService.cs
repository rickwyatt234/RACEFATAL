using System;
using RaceFatal.Racing;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public class PostRaceResolutionService
    {
        private readonly RaceRewardPolicy rewardPolicy;

        public PostRaceResolutionService(
            RaceRewardPolicy rewardPolicy)
        {
            this.rewardPolicy = rewardPolicy
                ?? throw new ArgumentNullException(
                    nameof(rewardPolicy));
        }

        public PostRaceResult Resolve(
            RaceResult raceResult,
            TeamState team,
            RacerState player)
        {
            if (raceResult == null)
            {
                throw new ArgumentNullException(
                    nameof(raceResult));
            }

            if (team == null)
            {
                throw new ArgumentNullException(
                    nameof(team));
            }

            if (player == null)
            {
                throw new ArgumentNullException(
                    nameof(player));
            }

            RaceResultEntry playerEntry =
                FindPlayerEntry(
                    raceResult,
                    player.RacerId);

            if (playerEntry == null)
            {
                throw new InvalidOperationException(
                    $"Race result '{raceResult.RaceId}' does not contain " +
                    $"player racer '{player.RacerId}'.");
            }

            RaceReward reward =
                rewardPolicy.Calculate(
                    playerEntry.Position,
                    playerEntry.Status);

            team.AddCredits(
                reward.Credits);

            team.AddFame(
                reward.TeamFame);

            team.AddResearchPoints(
                reward.ResearchPoints);

            player.Progression.AddFame(
                reward.CharacterFame);

            bool playerDied =
                playerEntry.Status ==
                    RaceParticipantStatus.Destroyed ||
                player.Status ==
                    RacerCareerStatus.Dead;

            bool careerEnded =
                player.Status !=
                    RacerCareerStatus.Active;

            return new PostRaceResult(
                raceResult.RaceId,
                player.RacerId,
                playerEntry.Position,
                playerEntry.Status,
                reward,
                playerDied,
                careerEnded);
        }

        private RaceResultEntry FindPlayerEntry(
            RaceResult raceResult,
            string playerRacerId)
        {
            foreach (RaceResultEntry entry
                     in raceResult.Standings)
            {
                if (entry.RacerId ==
                    playerRacerId)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}