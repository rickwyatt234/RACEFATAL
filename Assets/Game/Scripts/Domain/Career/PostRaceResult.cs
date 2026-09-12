using RaceFatal.Racing;

namespace RaceFatal.Career
{
    public class PostRaceResult
    {
        public string RaceId { get; }
        public string PlayerRacerId { get; }

        public int PlayerPosition { get; }

        public RaceParticipantStatus PlayerRaceStatus {
            get;
        }

        public RaceReward Reward { get; }

        public bool PlayerDied { get; }
        public bool CareerEnded { get; }

        public PostRaceResult(
            string raceId,
            string playerRacerId,
            int playerPosition,
            RaceParticipantStatus playerRaceStatus,
            RaceReward reward,
            bool playerDied,
            bool careerEnded)
        {
            RaceId = raceId;
            PlayerRacerId = playerRacerId;

            PlayerPosition = playerPosition;
            PlayerRaceStatus = playerRaceStatus;

            Reward = reward;

            PlayerDied = playerDied;
            CareerEnded = careerEnded;
        }
    }
}