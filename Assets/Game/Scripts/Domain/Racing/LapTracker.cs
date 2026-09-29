namespace RaceFatal.Racing
{
    public sealed class LapTracker
    {
        private const float RequiredTraversal = 0.90f;
        public int RequiredLaps { get; }

        public LapTracker(int requiredLaps)
        {
            RequiredLaps = requiredLaps;
        }

        public bool CompleteLap(RaceParticipant participant)
        {
            if (participant == null)
                return false;
            if (participant.Status != RaceParticipantStatus.Racing)
            {
                return false;
            }

            if (!participant.HasCompletedLapTraversal(RequiredTraversal))
            {
                return false;
            }

            participant.ConfirmLapTraversal();
            participant.CompleteLap();
            return participant.CompletedLaps >= RequiredLaps;
        }
    }
}
