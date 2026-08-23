/*
    PERSISTENT IDENTITY AND HISTORY OF ONE RACER

    TO DO: RETIRE LOGIC
*/
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public class RacerState
    {
        public string RacerId { get; }
        public string Name { get; }
        public string TeamId { get;}
        public bool IsPlayerCharacter { get; }
        public RacerCareerStatus CareerStatus { get; private set; }
        public int RacesEntered { get; private set; } = 0;
        public int RacesWon { get; private set; } = 0;
        public int Podiums { get; private set; } = 0;
        public int RacersEliminated { get; private set; } = 0;

        public CharacterProgression Progression { get; }


        public RacerState(
            string racerId,
            string name,
            string teamId,
            bool isPlayerCharacter)
        {
            RacerId = racerId;
            Name = name;
            TeamId = teamId;
            IsPlayerCharacter = isPlayerCharacter;
            Progression = new CharacterProgression();
        }

        public void RecordRaceEntered()
        {
            if (CareerStatus == RacerCareerStatus.Active)
            {
                RacesEntered++;
            }
        }
        public void RecordFinish(int position)
        {
            if (CareerStatus == RacerCareerStatus.Active)
            {
                if (position == 1)
                {
                    RacesWon++;
                }
                if (position is 2 or 3)
                {
                    Podiums++;
                }
            }
        }
        public void RecordElimination()
        {
            if (CareerStatus == RacerCareerStatus.Active)
            {
                RacersEliminated++;
            }
        }
        public void Kill()
        {
            CareerStatus = RacerCareerStatus.Dead;
        }
        public void Retire()
        {
            CareerStatus = RacerCareerStatus.Retired;
        }
    }
}


