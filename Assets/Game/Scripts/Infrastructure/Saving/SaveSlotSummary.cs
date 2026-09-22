namespace RaceFatal.Infrastructure.Saving
{
    public class SaveSlotSummary
    {
        public int SlotIndex { get; }
        public bool Exists { get; }

        public int SaveVersion { get; }
        public string LastSavedUtc { get; }

        public string TeamName { get; }
        public string RacerName { get; }

        public int Credits { get; }
        public int TeamFame { get; }
        public int ResearchPoints { get; }

        public bool HasCareerRun { get; }
        public bool CareerActive { get; }

        public SaveSlotSummary(
            int slotIndex,
            bool exists,
            int saveVersion,
            string lastSavedUtc,
            string teamName,
            string racerName,
            int credits,
            int teamFame,
            int researchPoints,
            bool hasCareerRun,
            bool careerActive)
        {
            SlotIndex = slotIndex;
            Exists = exists;

            SaveVersion = saveVersion;
            LastSavedUtc = lastSavedUtc;

            TeamName = teamName;
            RacerName = racerName;

            Credits = credits;
            TeamFame = teamFame;
            ResearchPoints = researchPoints;

            HasCareerRun = hasCareerRun;
            CareerActive = careerActive;
        }

        public static SaveSlotSummary Empty(
            int slotIndex)
        {
            return new SaveSlotSummary(
                slotIndex,
                false,
                0,
                null,
                null,
                null,
                0,
                0,
                0,
                false,
                false);
        }
    }
}
