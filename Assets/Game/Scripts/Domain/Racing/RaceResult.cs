using System.Collections.Generic;

namespace RaceFatal.Racing
{
    public class RaceResultEntry
    {
        public string RacerId { get; }
        public string TeamId { get; }
        public int Position { get; }
        public int CompletedLaps { get; }
        public RaceParticipantStatus Status { get; }

        public RaceResultEntry(
            string racerId,
            string teamId,
            int position,
            int completedLaps,
            RaceParticipantStatus status)
        {
            RacerId = racerId;
            TeamId = teamId;
            Position = position;
            CompletedLaps = completedLaps;
            Status = status;
        }
        
    }

    public class RaceResult
    {
        public string RaceId { get; }
        public IReadOnlyList<RaceResultEntry> Standings { get; }

        public RaceResult(string raceId, IReadOnlyList<RaceResultEntry> standings)
        {
            RaceId = raceId;
            Standings = standings;
        }
    }
}