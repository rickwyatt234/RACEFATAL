using System;
using System.Collections.Generic;
using RaceFatal.Vehicles;

namespace RaceFatal.Career
{
    public class TeamState
    {
        private readonly HashSet<string>
            unlockedTechnologyIds =
                new HashSet<string>();

        private readonly HashSet<string>
            unlockedChampionshipIds =
                new HashSet<string>();

        private readonly HashSet<string>
            eliminatedRacerIds =
                new HashSet<string>();

        private readonly List<ResearchContract> researchContracts = new List<ResearchContract>();
        private readonly Dictionary<string, PostRaceResult> settledRaceResults = new Dictionary<string, PostRaceResult>();
        public IReadOnlyList<ResearchContract> ResearchContracts => researchContracts.AsReadOnly();
        public IReadOnlyDictionary<string, PostRaceResult> SettledRaceResults => settledRaceResults;
        public int ResearchOutputPerRace
        {
            get
            {
                int total = 0;
                foreach (var contract in researchContracts)
                    if (contract.RacesRemaining > 0) total = checked(total + contract.PointsPerRace);
                return total;
            }
        }
        internal void ReplaceResearchContract(ResearchContract contract)
        {
            researchContracts.RemoveAll(c => c.DefinitionId == contract.DefinitionId);
            researchContracts.Add(contract);
        }
        public void RestoreResearchContract(ResearchContract contract)
        {
            if (contract == null || researchContracts.Exists(c => c.DefinitionId == contract.DefinitionId))
                throw new ArgumentException("Duplicate or missing research contract.");
            researchContracts.Add(contract);
        }
        internal int AdvanceResearchContracts()
        {
            int total = ResearchOutputPerRace;
            foreach (var contract in researchContracts) contract.Advance();
            return total;
        }
        public void RestoreSettledRace(string instanceId, PostRaceResult result)
        {
            if (string.IsNullOrWhiteSpace(instanceId) || result == null) throw new ArgumentException("Invalid race receipt.");
            settledRaceResults.Add(instanceId, result);
        }
        public string TeamId { get; }

        public string TeamName {
            get;
            private set;
        }

        public string PrimaryColor {
            get;
            private set;
        }

        public string SecondaryColor {
            get;
            private set;
        }

        public int Credits {
            get;
            private set;
        }

        public int Fame {
            get;
            private set;
        }

        public int ResearchPoints {
            get;
            private set;
        }

        public GarageState Garage { get; }

        public TeamRosterState Roster { get; }

        public IReadOnlyCollection<string>
            UnlockedTechnologyIds =>
                unlockedTechnologyIds;

        public IReadOnlyCollection<string>
            UnlockedChampionshipIds =>
                unlockedChampionshipIds;

        public IReadOnlyCollection<string>
            EliminatedRacerIds =>
                eliminatedRacerIds;

        public TeamState(
            string teamId,
            string teamName,
            string primaryColor,
            string secondaryColor)
        {
            TeamId = teamId
                ?? throw new ArgumentNullException(
                    nameof(teamId));

            TeamName = teamName
                ?? throw new ArgumentNullException(
                    nameof(teamName));

            PrimaryColor =
                primaryColor;

            SecondaryColor =
                secondaryColor;

            Garage =
                new GarageState();

            Roster =
                new TeamRosterState();
        }

        public void SetColors(
            string primaryColor,
            string secondaryColor)
        {
            PrimaryColor =
                primaryColor;

            SecondaryColor =
                secondaryColor;
        }

        public void AddCredits(
            int amount)
        {
            if (amount <= 0)
                return;

            Credits += amount;
        }

        public bool TrySpendCredits(
            int amount)
        {
            if (amount < 0)
                return false;

            if (Credits < amount)
                return false;

            Credits -= amount;

            return true;
        }

        public void AddFame(
            int amount)
        {
            if (amount <= 0)
                return;

            Fame += amount;
        }

        public void AddResearchPoints(
            int amount)
        {
            if (amount <= 0)
                return;

            ResearchPoints +=
                amount;
        }

        public bool TrySpendResearchPoints(
            int amount)
        {
            if (amount < 0)
                return false;

            if (ResearchPoints < amount)
                return false;

            ResearchPoints -=
                amount;

            return true;
        }

        public bool UnlockTechnology(
            string technologyId)
        {
            if (string.IsNullOrWhiteSpace(
                    technologyId))
            {
                return false;
            }

            return unlockedTechnologyIds.Add(
                technologyId);
        }

        public bool HasTechnology(
            string technologyId)
        {
            return unlockedTechnologyIds.Contains(
                technologyId);
        }

        public bool UnlockChampionship(
            string championshipId)
        {
            if (string.IsNullOrWhiteSpace(
                    championshipId))
            {
                return false;
            }

            return unlockedChampionshipIds.Add(
                championshipId);
        }

        public bool HasChampionshipUnlocked(
            string championshipId)
        {
            return unlockedChampionshipIds.Contains(
                championshipId);
        }

        public void PermanentlyEliminateRacer(
            string racerId)
        {
            if (string.IsNullOrWhiteSpace(
                    racerId))
            {
                return;
            }

            eliminatedRacerIds.Add(
                racerId);
        }

        public bool IsRacerEliminated(
            string racerId)
        {
            return eliminatedRacerIds.Contains(
                racerId);
        }
    }
}