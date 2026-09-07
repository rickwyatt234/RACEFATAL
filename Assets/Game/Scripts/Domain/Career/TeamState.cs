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