using RaceFatal.Racing;

namespace RaceFatal.Career
{
    public class PostRaceResult
    {
        public string RaceId { get; }
        public string PlayerRacerId { get; }
        public int PlayerPosition { get; }
        public RaceParticipantStatus PlayerRaceStatus { get; }
        public RaceReward Reward { get; }
        public int RaceResearchPoints { get; }
        public int EventResearchPoints { get; }
        public int ResearcherPoints { get; }
        public float AverageAudienceFavor { get; }
        public float AudienceFameMultiplier => RaceAudience.MultiplierFor(AverageAudienceFavor);
        public bool PlayerDied { get; }
        public bool CareerEnded { get; }

        public PostRaceResult(string raceId, string playerRacerId, int playerPosition, RaceParticipantStatus playerRaceStatus, RaceReward reward, bool playerDied, bool careerEnded, int eventResearchPoints = 0, int researcherPoints = 0, float averageAudienceFavor = 100f)
        {
            AverageAudienceFavor = RaceAudience.ClampFavor(averageAudienceFavor);
            RaceId = raceId;
            PlayerRacerId = playerRacerId;
            PlayerPosition = playerPosition;
            PlayerRaceStatus = playerRaceStatus;
            Reward = reward;
            EventResearchPoints = eventResearchPoints;
            ResearcherPoints = researcherPoints;
            RaceResearchPoints = reward.ResearchPoints - eventResearchPoints - researcherPoints;
            PlayerDied = playerDied;
            CareerEnded = careerEnded;
        }
    }
}
