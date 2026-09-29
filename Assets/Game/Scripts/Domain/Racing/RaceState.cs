using System;
using System.Collections.Generic;
using System.Linq;

namespace RaceFatal.Racing
{
    public class RaceState
    {
        private readonly List<RaceParticipant> participants = new List<RaceParticipant>();
        public RaceDefinition RaceDefinition { get; }
        public DeathmatchRules Deathmatch { get; internal set; }
        public IReadOnlyList<RaceParticipant> Participants => participants;
        public bool IsStarted { get; private set; }
        public bool IsFinished { get; private set; }

        public RaceState(RaceDefinition raceDefinition, IEnumerable<RaceParticipant> participants)
        {
            RaceDefinition = raceDefinition ?? throw new ArgumentNullException(nameof(raceDefinition));
            Deathmatch = raceDefinition.Deathmatch;
            this.participants = participants.ToList() ?? throw new ArgumentNullException(nameof(participants));
        }

        internal void StartRace()
        {
            if (IsStarted)
            {
                throw new InvalidOperationException("Race has already started.");
            }

            foreach (var participant in participants)
            {
                participant.Start();
            }

            IsStarted = true;
        }

        internal void FinishRace()
        {
            if (!IsStarted)
            {
                throw new InvalidOperationException("Race has not started yet.");
            }

            IsFinished = true;
        }

        public RaceParticipant FindParticipant(string racerId)
        {
            return participants.FirstOrDefault(p => p.RacerId == racerId);
        }

        public IReadOnlyList<RaceParticipant> GetCurrentOrder()
        {
            if (Deathmatch != null)
                return participants.OrderBy(p => DeathmatchRank(p)).ThenBy(p => p.RacerId, StringComparer.Ordinal).ToList();
            return participants.OrderBy(GetStatusPriority).ThenBy(p => p.Status == RaceParticipantStatus.Finished ? p.FinishPosition : int.MaxValue).ThenByDescending(p => p.CompletedLaps).ThenByDescending(p => p.CourseProgress).ToList();
        }

        public int GetCurrentPosition(string racerId)
        {
            IReadOnlyList<RaceParticipant> order = GetCurrentOrder();
            for (int i = 0; i < order.Count; i++)
            {
                if (order[i].RacerId == racerId)
                    return Deathmatch != null ? DeathmatchRank(order[i]) : i + 1;
            }

            return 0;
        }

        public string ContenderId(RaceParticipant p) => Deathmatch.Mode == DeathmatchVictoryMode.Team ? p.TeamId : p.RacerId;
        public int SurvivingContenders => participants.Where(p => p.Status == RaceParticipantStatus.Racing).Select(ContenderId).Distinct().Count();

        public int DeathmatchRank(RaceParticipant participant)
        {
            if (participant.DeathmatchPosition > 0)
                return participant.DeathmatchPosition;
            var groups = participants.GroupBy(ContenderId).Select(g => new { Id = g.Key, Survivors = g.Count(p => p.Status == RaceParticipantStatus.Racing), Kills = g.Sum(p => p.Eliminations), OutAt = g.Max(p => p.EliminationTime), Distance = g.Sum(p => p.CompletedLaps + p.CourseProgress), Grid = g.Min(p => participants.IndexOf(p)) }).ToList();
            var own = groups.Single(g => g.Id == ContenderId(participant));
            return 1 + groups.Count(g => g.Survivors > own.Survivors || (g.Survivors == own.Survivors && (own.Survivors > 0 ? g.Kills > own.Kills || (g.Kills == own.Kills && !Deathmatch.ShareTimeoutTies && (g.Distance > own.Distance || (g.Distance == own.Distance && g.Grid < own.Grid))) : g.OutAt > own.OutAt)));
        }

        private static int GetStatusPriority(RaceParticipant participant)
        {
            return participant.Status switch {  RaceParticipantStatus.Finished => 0,  RaceParticipantStatus.Racing => 1,
                RaceParticipantStatus.Ready => 2,  RaceParticipantStatus.Retired => 3,  RaceParticipantStatus.Destroyed => 4,  _ => 5 };
        }
    }
}
