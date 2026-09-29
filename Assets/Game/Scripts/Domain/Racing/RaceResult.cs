using System.Collections.Generic;

namespace RaceFatal.Racing
{
    public class RaceResultEntry
    {
        public string RacerId { get; }
        public string RacerName { get; }
        public string TeamId { get; }
        public string TeamName { get; }
        public int Position { get; }
        public int CompletedLaps { get; }
        public RaceParticipantStatus Status { get; }
        public float? RaceTimeSeconds { get; }
        public bool WasFastResolved { get; }
        public bool IsWinner { get; }
        public int Eliminations { get; }
        public string EliminationReason { get; }
        public float AverageAudienceFavor { get; }
        public float AudienceFameMultiplier => RaceAudience.MultiplierFor(AverageAudienceFavor);

        public RaceResultEntry(string racerId, string racerName, string teamId, string teamName, int position, int completedLaps, RaceParticipantStatus status, float? raceTimeSeconds, bool wasFastResolved, bool isWinner = false, int eliminations = 0, string eliminationReason = null, float averageAudienceFavor = 100f)
        {
            AverageAudienceFavor = RaceAudience.ClampFavor(averageAudienceFavor);
            RacerId = racerId;
            RacerName = racerName;
            TeamId = teamId;
            TeamName = teamName;
            Position = position;
            CompletedLaps = completedLaps;
            Status = status;
            RaceTimeSeconds = raceTimeSeconds;
            WasFastResolved = wasFastResolved;
            IsWinner = isWinner;
            Eliminations = eliminations;
            EliminationReason = eliminationReason;
        }

        public RaceResultEntry(string racerId, string teamId, int position, int completedLaps, RaceParticipantStatus status) : this(racerId, racerId, teamId, teamId, position, completedLaps, status, null, false)
        {
        }
    }

    public class RaceResult
    {
        public string RaceId { get; }
        public string InstanceId { get; }
        public int ResearchPointBonus { get; }
        public bool IsFinalized { get; }
        public DeathmatchRules Deathmatch { get; }
        public IReadOnlyList<RaceResultEntry> Standings { get; }

        public RaceResult(string raceId, IReadOnlyList<RaceResultEntry> standings, string instanceId = null, int researchPointBonus = 0, bool isFinalized = true, DeathmatchRules deathmatch = null)
        {
            Deathmatch = deathmatch;
            RaceId = raceId;
            InstanceId = instanceId ?? System.Guid.NewGuid().ToString("N");
            ResearchPointBonus = System.Math.Max(0, researchPointBonus);
            IsFinalized = isFinalized;
            Standings = standings;
        }
    }
}
