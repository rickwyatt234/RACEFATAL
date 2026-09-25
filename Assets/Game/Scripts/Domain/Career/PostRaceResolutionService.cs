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

            if (string.IsNullOrWhiteSpace(raceResult.InstanceId))
                throw new InvalidOperationException("Race instance ID is required before settlement.");
            if (!raceResult.IsFinalized) throw new InvalidOperationException("Only finalized races award rewards.");
            if (team.SettledRaceResults.TryGetValue(raceResult.InstanceId, out var previous)) return previous;

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

            if (playerEntry.Status != RaceParticipantStatus.Finished &&
                playerEntry.Status != RaceParticipantStatus.Destroyed && playerEntry.Status != RaceParticipantStatus.Retired)
                throw new InvalidOperationException("Player has no finalized race outcome.");
            int researcherPoints = team.ResearchOutputPerRace;
            int totalRP = checked(reward.ResearchPoints + raceResult.ResearchPointBonus + researcherPoints);
            reward = new RaceReward(reward.Credits, reward.TeamFame, totalRP, reward.CharacterFame);
            // Check totals before any contract or currency mutations.
            _ = checked(team.ResearchPoints + totalRP);
            _ = checked(team.Credits + reward.Credits);
            _ = checked(team.Fame + reward.TeamFame);
            _ = checked(player.Progression.Fame + reward.CharacterFame);
            team.AdvanceResearchContracts();

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

            var result = new PostRaceResult(
                raceResult.RaceId,
                player.RacerId,
                playerEntry.Position,
                playerEntry.Status,
                reward,
                playerDied,
                careerEnded, raceResult.ResearchPointBonus, researcherPoints);
            team.RestoreSettledRace(raceResult.InstanceId, result);
            return result;
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