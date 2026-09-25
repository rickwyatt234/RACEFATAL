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

        public RaceResultEntry(
            string racerId,
            string racerName,
            string teamId,
            string teamName,
            int position,
            int completedLaps,
            RaceParticipantStatus status,
            float? raceTimeSeconds,
            bool wasFastResolved)
        {
            RacerId = racerId;
            RacerName = racerName;

            TeamId = teamId;
            TeamName = teamName;

            Position = position;
            CompletedLaps = completedLaps;

            Status = status;

            RaceTimeSeconds = raceTimeSeconds;
            WasFastResolved = wasFastResolved;
        }

        /*
         * Compatibility constructor for any older code that
         * still creates RaceResultEntry directly.
         */
        public RaceResultEntry(
            string racerId,
            string teamId,
            int position,
            int completedLaps,
            RaceParticipantStatus status)
            : this(
                racerId,
                racerId,
                teamId,
                teamId,
                position,
                completedLaps,
                status,
                null,
                false)
        {
        }
    }

    public class RaceResult
    {
        public string RaceId { get; }
        public string InstanceId { get; }
        public int ResearchPointBonus { get; }
        public bool IsFinalized { get; }

        public IReadOnlyList<RaceResultEntry> Standings {
            get;
        }

        public RaceResult(
            string raceId,
            IReadOnlyList<RaceResultEntry> standings, string instanceId = null, int researchPointBonus = 0, bool isFinalized = true)
        {
            RaceId = raceId;
            InstanceId = instanceId ?? System.Guid.NewGuid().ToString("N");
            ResearchPointBonus = System.Math.Max(0, researchPointBonus);
            IsFinalized = isFinalized;
            Standings = standings;
        }
    }
}