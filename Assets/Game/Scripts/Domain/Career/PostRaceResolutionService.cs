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
            var calendarSettlement = CareerCalendarService.PreviewSettlement(team, raceResult);
            if (calendarSettlement != null)
                reward = new RaceReward(calendarSettlement.Credits, reward.TeamFame, reward.ResearchPoints, reward.CharacterFame);

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
            // Each teammate earns Character Fame for their own finish; team currency is still
            // awarded once from the player's result. Settlement receipts prevent repeat awards.
            var teammateRewards = new System.Collections.Generic.List<(RacerState racer, int fame)>();
            foreach (RaceResultEntry entry in raceResult.Standings)
            {
                if (entry.TeamId != team.TeamId || entry.RacerId == player.RacerId) continue;
                RacerState teammate = team.Roster.FindRacer(entry.RacerId);
                if (teammate == null || teammate.IsPlayerCharacter) continue;
                if (entry.Status != RaceParticipantStatus.Finished &&
                    entry.Status != RaceParticipantStatus.Destroyed && entry.Status != RaceParticipantStatus.Retired) continue;
                int fame = rewardPolicy.Calculate(entry.Position, entry.Status).CharacterFame;
                _ = checked(teammate.Progression.Fame + fame);
                teammateRewards.Add((teammate, fame));
            }
            team.AdvanceResearchContracts();

            team.AddCredits(
                reward.Credits);

            team.AddFame(
                reward.TeamFame);

            team.AddResearchPoints(
                reward.ResearchPoints);

            player.Progression.AddFame(
                reward.CharacterFame);
            foreach (var (teammate, fame) in teammateRewards)
                teammate.Progression.AddFame(fame);

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
            if (calendarSettlement != null) team.RestoreCalendar(calendarSettlement.Calendar);
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
