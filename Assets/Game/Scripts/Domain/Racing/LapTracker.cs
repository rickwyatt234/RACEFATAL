namespace RaceFatal.Racing
{
    public class LapTracker
    {
        public int RequiredLaps { get; }
        public LapTracker(int requiredLaps)
        {
            RequiredLaps = requiredLaps;
        }

        public bool CompleteLap(RaceParticipant participant)
        {
            if (participant.Status != RaceParticipantStatus.Racing)
                return false;
            participant.CompleteLap();
            return participant.CompletedLaps >= RequiredLaps;
        }
    }
}