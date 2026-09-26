using System;
using System.Collections.Generic;
using System.Linq;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    // Serializable value snapshots. Only services mutate the live state's internal data.
    [Serializable]
    public sealed class ChampionshipStandingData
    {
        public string teamId, teamName;
        public int points;
        public ChampionshipStandingData Copy() => (ChampionshipStandingData)MemberwiseClone();
    }

    [Serializable]
    public sealed class CareerEventEntryData
    {
        public string eventId, displayName, description;
        public CareerEventKind kind;
        public int entryFee, roundIndex;
        public List<string> raceIds = new List<string>();
        public List<int> payouts = new List<int>();
        public List<int> prizes = new List<int>();
        public List<int> points = new List<int>();
        public List<ChampionshipStandingData> standings = new List<ChampionshipStandingData>();
        public string pendingInstanceId, pendingRaceId;
        public bool completed, withdrawn;
        public int finalRank, finalPrize;
        public CareerEventEntryData Copy()
        {
            var copy = (CareerEventEntryData)MemberwiseClone();
            copy.raceIds = new List<string>(raceIds);
            copy.payouts = new List<int>(payouts); copy.prizes = new List<int>(prizes); copy.points = new List<int>(points);
            copy.standings = standings.Select(s => s.Copy()).ToList();
            return copy;
        }
    }

    [Serializable]
    public sealed class CareerCalendarData
    {
        public int seed, week = 1, drawWeek;
        public List<string> draw = new List<string>();
        public List<string> unlocked = new List<string>();
        public List<string> cancelledAttempts = new List<string>();
        public CareerEventEntryData active, lastEvent;
        public CareerCalendarData Copy() => new CareerCalendarData {
            seed = seed, week = week, drawWeek = drawWeek, draw = new List<string>(draw),
            unlocked = new List<string>(unlocked), cancelledAttempts = new List<string>(cancelledAttempts), active = active?.Copy(), lastEvent = lastEvent?.Copy() };
    }

    public sealed class CareerCalendarState
    {
        internal CareerCalendarData Data { get; private set; }
        public int Week => Data.week;
        public IReadOnlyList<string> DrawIds => Data.draw.AsReadOnly();
        public IReadOnlyList<string> UnlockedIds => Data.unlocked.AsReadOnly();
        public CareerEventEntryData Active => Data.active?.Copy();
        public CareerEventEntryData LastEvent => Data.lastEvent?.Copy();
        public CareerCalendarState() { Data = new CareerCalendarData { seed = Guid.NewGuid().GetHashCode() }; }
        public CareerCalendarData Export() => Data.Copy();
        public static Result<CareerCalendarState> Restore(CareerCalendarData data)
        {
            if (data == null) return Result<CareerCalendarState>.Success(new CareerCalendarState());
            if (data.week < 1 || data.week == int.MaxValue || (data.drawWeek != 0 && data.drawWeek != data.week) ||
                !ValidIds(data.draw) || !ValidIds(data.unlocked) || !ValidIds(data.cancelledAttempts) || data.draw.Any(id => !data.unlocked.Contains(id)) ||
                (data.drawWeek == 0 && data.draw.Count != 0) || !ValidEntry(data.active, false) || !ValidEntry(data.lastEvent, true))
                return Result<CareerCalendarState>.Failure("Invalid career calendar save data.");
            if (data.active != null && !data.unlocked.Contains(data.active.eventId))
                return Result<CareerCalendarState>.Failure("Active calendar event is not unlocked.");
            if (data.active != null && data.cancelledAttempts.Contains(data.active.pendingInstanceId))
                return Result<CareerCalendarState>.Failure("Active calendar attempt has already been cancelled.");
            var state = new CareerCalendarState();
            state.Data = data.Copy();
            return Result<CareerCalendarState>.Success(state);
        }
        private static bool ValidIds(List<string> ids) => ids != null &&
            ids.All(id => !string.IsNullOrWhiteSpace(id)) && ids.Distinct().Count() == ids.Count;
        private static bool ValidEntry(CareerEventEntryData entry, bool history)
        {
            if (entry == null) return true;
            if (string.IsNullOrWhiteSpace(entry.eventId) || string.IsNullOrWhiteSpace(entry.displayName) ||
                (entry.kind != CareerEventKind.Race && entry.kind != CareerEventKind.Championship) ||
                entry.entryFee < 0 || entry.roundIndex < 0 || entry.raceIds == null || entry.raceIds.Count == 0 ||
                entry.raceIds.Any(string.IsNullOrWhiteSpace) || entry.roundIndex > entry.raceIds.Count ||
                (entry.kind == CareerEventKind.Race && entry.raceIds.Count != 1) ||
                (entry.kind == CareerEventKind.Championship && entry.raceIds.Count < 2) ||
                entry.payouts == null || entry.prizes == null || entry.points == null ||
                entry.payouts.Concat(entry.prizes).Concat(entry.points).Any(p => p < 0) ||
                entry.standings == null || entry.standings.Any(s => s == null || string.IsNullOrWhiteSpace(s.teamId) || s.points < 0) ||
                entry.standings.Select(s => s.teamId).Distinct().Count() != entry.standings.Count ||
                entry.finalRank < 0 || entry.finalPrize < 0) return false;
            if (history) return entry.completed && (entry.withdrawn || entry.roundIndex == entry.raceIds.Count) &&
                string.IsNullOrEmpty(entry.pendingInstanceId) && string.IsNullOrEmpty(entry.pendingRaceId);
            if (entry.completed || entry.withdrawn || entry.roundIndex >= entry.raceIds.Count) return false;
            return string.IsNullOrEmpty(entry.pendingInstanceId)
                ? string.IsNullOrEmpty(entry.pendingRaceId)
                : !string.IsNullOrWhiteSpace(entry.pendingInstanceId) && entry.pendingRaceId == entry.raceIds[entry.roundIndex];
        }
    }
}
