using System;
using System.Collections.Generic;
using RaceFatal.Vehicles;

namespace RaceFatal.Career
{
    public class TeamState
    {
        public string RecoverySupportCheckpoint { get; private set; }

        public void RestoreRecoverySupport(string checkpoint)
        {
            RecoverySupportCheckpoint = checkpoint;
        }

        private readonly HashSet<string> ownedTeamPerkIds = new HashSet<string>(StringComparer.Ordinal);
        public IReadOnlyCollection<string> OwnedTeamPerkIds => ownedTeamPerkIds;
        public bool HasTeamPerk(string id) => !string.IsNullOrWhiteSpace(id) && ownedTeamPerkIds.Contains(id);
        public bool GrantTeamPerk(string id)
        {
            if (TeamPerkCatalog.Find(id) == null) return false;
            return ownedTeamPerkIds.Add(id);
        }
        public void RestoreTeamPerks(IEnumerable<string> ids)
        {
            ownedTeamPerkIds.Clear();
            if(ids==null)return;
            foreach(var id in ids)
                if(TeamPerkCatalog.Find(id)!=null)ownedTeamPerkIds.Add(id);
        }
        private readonly HashSet<string> unlockedChampionshipIds = new HashSet<string>();
        private readonly HashSet<string> eliminatedRacerIds = new HashSet<string>();
        private readonly Dictionary<string, PostRaceResult> settledRaceResults = new Dictionary<string, PostRaceResult>();
        public IReadOnlyDictionary<string, PostRaceResult> SettledRaceResults => settledRaceResults;

        public void RestoreSettledRace(string instanceId, PostRaceResult result)
        {
            if (string.IsNullOrWhiteSpace(instanceId) || result == null)
                throw new ArgumentException("Invalid race receipt.");
            settledRaceResults.Add(instanceId, result);
        }

        public string TeamId { get; }
        public string TeamName { get; private set; }
        public string PrimaryColor { get; private set; }
        public string SecondaryColor { get; private set; }
        public int Credits { get; private set; }
        public int Fame { get; private set; }
        public int LifetimeFame { get; private set; }
        public BrandProgress Brands { get; private set; } = new BrandProgress();
        public void RestoreBrands(BrandProgress value) { Brands = value ?? new BrandProgress(); Brands.Validate(); }
        public void RestoreFame(int available, int lifetime)
        {
            if (available < 0 || lifetime < available) throw new ArgumentException("Invalid team fame.");
            Fame = available; LifetimeFame = lifetime;
        }
        public bool TrySpendFame(int amount)
        {
            if (amount < 0 || Fame < amount) return false;
            Fame -= amount; return true;
        }
        public void RefundFame(int amount)
        {
            if (amount < 0 || (long)Fame + amount > LifetimeFame) throw new ArgumentException("Invalid fame refund.");
            Fame = checked(Fame + amount);
        }
        public GarageState Garage { get; }
        public TeamRosterState Roster { get; }
        public CareerCalendarState Calendar { get; private set; } = new CareerCalendarState();

        public RaceFatal.Shared.Result RestoreCalendar(CareerCalendarData data)
        {
            var restored = CareerCalendarState.Restore(data);
            if (!restored.IsSuccess)
                return RaceFatal.Shared.Result.Failure(restored.ErrorMessage);
            Calendar = restored.Value;
            return RaceFatal.Shared.Result.Success();
        }

        public IReadOnlyCollection<string> UnlockedChampionshipIds => unlockedChampionshipIds;
        public IReadOnlyCollection<string> EliminatedRacerIds => eliminatedRacerIds;

        public TeamState(string teamId, string teamName, string primaryColor, string secondaryColor)
        {
            TeamId = teamId ?? throw new ArgumentNullException(nameof(teamId));
            TeamName = teamName ?? throw new ArgumentNullException(nameof(teamName));
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
            Garage = new GarageState();
            Roster = new TeamRosterState();
        }

        internal void Rename(string name)
        {
            TeamName = name;
        }

        public void SetColors(string primaryColor, string secondaryColor)
        {
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
        }

        public void AddCredits(int amount)
        {
            if (amount <= 0)
                return;
            Credits += amount;
        }

        public bool TrySpendCredits(int amount)
        {
            if (amount < 0)
                return false;
            if (Credits < amount)
                return false;
            Credits -= amount;
            return true;
        }

        public void AddFame(int amount)
        {
            if (amount <= 0)
                return;
            int lifetime = checked(LifetimeFame + amount);
            Fame = checked(Fame + amount);
            LifetimeFame = lifetime;
        }

        public bool UnlockChampionship(string championshipId)
        {
            if (string.IsNullOrWhiteSpace(championshipId))
            {
                return false;
            }

            return unlockedChampionshipIds.Add(championshipId);
        }

        public bool HasChampionshipUnlocked(string championshipId)
        {
            return unlockedChampionshipIds.Contains(championshipId);
        }

        public void PermanentlyEliminateRacer(string racerId)
        {
            if (string.IsNullOrWhiteSpace(racerId))
            {
                return;
            }

            eliminatedRacerIds.Add(racerId);
        }

        public bool IsRacerEliminated(string racerId)
        {
            return eliminatedRacerIds.Contains(racerId);
        }
    }
}
