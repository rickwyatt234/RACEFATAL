namespace RaceFatal.Career
{
    public class RaceReward
    {
        public int Credits { get; }
        public int TeamFame { get; }
        public int ResearchPoints { get; }
        public int CharacterFame { get; }

        public RaceReward(
            int credits,
            int teamFame,
            int researchPoints,
            int characterFame)
        {
            Credits = credits;
            TeamFame = teamFame;
            ResearchPoints = researchPoints;
            CharacterFame = characterFame;
        }
    }
}