/*
    PERSISTENT STATE BELONG TO TEAM
    SERIALIZED TO SAVE SLOT
*/
using System.Collections.Generic;
using RaceFatal.Vehicles;

namespace RaceFatal.Career
{
    public class TeamState
    {
        private readonly HashSet<string> unlockedTechnologyIds = new HashSet<string>();
        private readonly HashSet<string> unlockedChampionshipIds = new HashSet<string>();
        private readonly HashSet<string> eliminatedRacerIds = new HashSet<string>();

        public string TeamId { get; }
        public string TeamName { get; }
        public string PrimaryColor { get; private set;}
        public string SecondaryColor { get; private set;}
        public int Credits { get; private set; }
        public int Fame { get; private set; }
        public int ResearchPoints { get; private set; }
        public GarageState Garage { get; }


        public IReadOnlyCollection<string> UnlockedTechnologyIds =>
            unlockedTechnologyIds;
        public IReadOnlyCollection<string> UnlockedChampionshipIds =>
            unlockedChampionshipIds;
        public IReadOnlyCollection<string> EliminatedRacerIds =>
            eliminatedRacerIds;


        public TeamState(
            string teamId,
            string teamName,
            string primaryColor,
            string secondaryColor)
        {
            TeamId = teamId;
            TeamName = teamName;
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;

            Garage = new GarageState();
        }

        public void SetColors(string primary, string secondary)
        {
            PrimaryColor = primary;
            SecondaryColor = secondary;
        }

#region Economy Variables
        public void AddCredits(int amount)
        {
            if (amount > 0)
                Credits += amount;
        }

        public bool TrySpendCredits(int amount)
        {
            if (amount < 0 || Credits < amount)
                return false;

            Credits -= amount;
            return true;
        }

        public void AddFame(int amount)
        {
            if (amount > 0)
                Fame += amount;
        }

        public void AddResearchPoints(int amount)
        {
            if (amount > 0)
                ResearchPoints += amount;
        }

        public bool TrySpendResearchPoints(int amount)
        {
            if (amount < 0 || ResearchPoints < amount)
                return false;

            ResearchPoints -= amount;
            return true;
        }
#endregion

#region Technology
        public void UnlockTechnology(string technologyId)
        {
            unlockedTechnologyIds.Add(technologyId);
        }

        public bool IsTechnologyUnlocked(string technologyId)
        {
            return unlockedTechnologyIds.Contains(technologyId);
        }
#endregion

#region Championships
        public void UnlockChampionship(string championshipId)
        {
            unlockedChampionshipIds.Add(championshipId);
        }

        public bool IsChampionshipUnlocked(string championshipId)
        {
            return unlockedChampionshipIds.Contains(championshipId);
        }
#endregion

#region Racers
        public void PermanentlyEliminateRacer(string racerId)
        {
            eliminatedRacerIds.Add(racerId);
        }
        public bool IsRacerEliminated(string racerId)
        {
            return eliminatedRacerIds.Contains(racerId);
        }
#endregion
    }
}
