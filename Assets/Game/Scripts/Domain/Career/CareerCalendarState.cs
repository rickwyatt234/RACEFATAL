using System;
using System.Collections.Generic;
using System.Linq;
using RaceFatal.Shared;
using RaceFatal.Racing;

namespace RaceFatal.Career
{
    [Serializable]
    public sealed class ChampionshipStandingData
    {
        public string teamId, teamName;
        public int points;
        public ChampionshipStandingData Copy() => (ChampionshipStandingData)MemberwiseClone();
    }

    [Serializable]
    public sealed class DeathmatchStandingData
    {
        public string racerId, racerName, teamId, teamName, eliminationReason;
        public int position, eliminations;
        public bool winner;
        public RaceParticipantStatus status;
        public DeathmatchStandingData Copy() => (DeathmatchStandingData)MemberwiseClone();
    }

    [Serializable]
    public sealed class CareerEventEntryData
    {
        public string eventId, displayName, description;
        public CareerEventKind kind;
        public int entryFee, roundIndex;
        public int occurrenceDay, scheduledDay, roundSpacingDays;
        public List<string> raceIds = new List<string>();
        public List<int> payouts = new List<int>();
        public List<int> prizes = new List<int>();
        public List<int> points = new List<int>();
        public List<ChampionshipStandingData> standings = new List<ChampionshipStandingData>();
        public string pendingInstanceId, pendingRaceId;
        public bool completed, withdrawn;
        public int finalRank, finalPrize;
        public List<DeathmatchStandingData> deathmatchResults = new List<DeathmatchStandingData>();
        public int deathmatchVersion, deathmatchMode, allowedWinners;
        public bool shareTimeoutTies;
        public float timeLimitSeconds, minimumSpeedKph, startGraceSeconds, belowSpeedGraceSeconds;
        public void CaptureDeathmatch(DeathmatchRules rules)
        {
            if (rules == null)
                return;
            shareTimeoutTies = rules.ShareTimeoutTies;
            deathmatchVersion = 1;
            deathmatchMode = (int)rules.Mode;
            allowedWinners = rules.AllowedWinners;
            timeLimitSeconds = rules.TimeLimitSeconds;
            minimumSpeedKph = rules.MinimumSpeedKph;
            startGraceSeconds = rules.StartGraceSeconds;
            belowSpeedGraceSeconds = rules.BelowSpeedGraceSeconds;
        }

        public DeathmatchRules RestoreDeathmatch() => new DeathmatchRules((DeathmatchVictoryMode)deathmatchMode, allowedWinners, timeLimitSeconds, minimumSpeedKph, startGraceSeconds, belowSpeedGraceSeconds, shareTimeoutTies);
        public bool ValidDeathmatch()
        {
            if (kind != CareerEventKind.Deathmatch)
                return !shareTimeoutTies && deathmatchVersion == 0 && deathmatchMode == 0 && allowedWinners == 0 && timeLimitSeconds == 0 && minimumSpeedKph == 0 && startGraceSeconds == 0 && belowSpeedGraceSeconds == 0;
            if (deathmatchVersion != 1)
                return false;
            try
            {
                RestoreDeathmatch();
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public CareerEventEntryData Copy()
        {
            var copy = (CareerEventEntryData)MemberwiseClone();
            copy.raceIds = new List<string>(raceIds);
            copy.payouts = new List<int>(payouts);
            copy.prizes = new List<int>(prizes);
            copy.points = new List<int>(points);
            copy.standings = standings.Select(s => s.Copy()).ToList();
            copy.deathmatchResults = (deathmatchResults ?? new List<DeathmatchStandingData>()).Select(s => s.Copy()).ToList();
            return copy;
        }
    }

    // Captured once per campaign. Version 1 expands recurrence rules; version 2
    // uses the same bank with the saved seed to allocate three events per week.
    [Serializable]
    public sealed class CareerEventScheduleData
    {
        public string eventId;
        public int firstDay, repeatEveryWeeks, roundSpacingDays, roundCount;
        public int starterOrder;
        public CareerEventScheduleData Copy() => (CareerEventScheduleData)MemberwiseClone();
    }

    public sealed class CareerEventOccurrence
    {
        public string EventId { get; }
        public int Day { get; }
        public int RoundIndex { get; }
        public int OccurrenceDay { get; }
        public bool IsActiveRound { get; }
        public CareerEventOccurrence(string id, int day, int roundIndex = 0, bool active = false, int occurrenceDay = 0)
        { EventId = id; Day = day; RoundIndex = roundIndex; IsActiveRound = active; OccurrenceDay = occurrenceDay == 0 ? day : occurrenceDay; }
    }

    [Serializable]
    public sealed class CareerCalendarData
    {
        public int seed, week = 1, drawWeek;
        public int dayOfWeek, scheduleVersion;
        public List<CareerEventScheduleData> schedules = new List<CareerEventScheduleData>();
        public List<string> consumedOccurrences = new List<string>();
        public List<string> draw = new List<string>();
        public List<string> unlocked = new List<string>();
        public List<string> cancelledAttempts = new List<string>();
        public CareerEventEntryData active, lastEvent;
        public CareerCalendarData Copy() => new CareerCalendarData { dayOfWeek = dayOfWeek, scheduleVersion = scheduleVersion, schedules = schedules.Select(s => s.Copy()).ToList(), consumedOccurrences = new List<string>(consumedOccurrences), seed = seed, week = week, drawWeek = drawWeek,  draw = new List<string>(draw),  unlocked =
            new List<string>(unlocked),  cancelledAttempts = new List<string>(cancelledAttempts),  active = active?.Copy(),  lastEvent = lastEvent?.Copy() };
    }

    public sealed class CareerCalendarState
    {
        internal CareerCalendarData Data { get; private set; }
        public const int MaxDay = int.MaxValue - 7;
        public int Week => Data.week;
        public int DayOfWeek => Data.dayOfWeek;
        public int AbsoluteDay => checked((Week - 1) * 7 + DayOfWeek);
        public string DateLabel => FormatDay(AbsoluteDay);
        public static string FormatDay(int day) => $"WEEK {(day - 1) / 7 + 1} / DAY {(day - 1) % 7 + 1}";
        public IReadOnlyList<CareerEventScheduleData> Schedules => Data.schedules.Select(s => s.Copy()).ToList().AsReadOnly();
        public bool IsConsumed(string eventId, int day) => Data.consumedOccurrences.Contains(OccurrenceKey(eventId, day));
        internal static string OccurrenceKey(string id, int day) => id + "@" + day.ToString(System.Globalization.CultureInfo.InvariantCulture);
        internal static void SetDay(CareerCalendarData data, int day)
        {
            if (day < 1 || day > MaxDay) throw new ArgumentOutOfRangeException(nameof(day));
            data.week = (day - 1) / 7 + 1;
            data.dayOfWeek = (day - 1) % 7 + 1;
            data.drawWeek = 0;
            data.draw.Clear();
        }
        public IReadOnlyList<string> DrawIds => Data.draw.AsReadOnly();
        public IReadOnlyList<string> UnlockedIds => Data.unlocked.AsReadOnly();
        public CareerEventEntryData Active => Data.active?.Copy();
        public CareerEventEntryData LastEvent => Data.lastEvent?.Copy();

        public CareerCalendarState()
        {
            Data = new CareerCalendarData { dayOfWeek = 1, seed = Guid.NewGuid().GetHashCode() };
        }

        public CareerCalendarData Export() => Data.Copy();
        public static Result<CareerCalendarState> Restore(CareerCalendarData data)
        {
            if (data == null)
                return Result<CareerCalendarState>.Success(new CareerCalendarState());
            data = new CareerCalendarData { dayOfWeek = data.dayOfWeek, scheduleVersion = data.scheduleVersion, schedules = data.schedules, consumedOccurrences = data.consumedOccurrences, seed = data.seed,  week = data.week,  drawWeek = data.drawWeek,  draw = data.draw,  unlocked =
                data.unlocked,  cancelledAttempts = data.cancelledAttempts,  active = IsEmptyEntry(data.active) ? null : data.active,  lastEvent = IsEmptyEntry(data.lastEvent) ? null : data.lastEvent };
            if (data.scheduleVersion == 0)
            {
                // Legacy saves had only a week. Keep it and start on its first day.
                if (data.dayOfWeek == 0) data.dayOfWeek = 1;
                data.schedules = data.schedules ?? new List<CareerEventScheduleData>();
                data.consumedOccurrences = data.consumedOccurrences ?? new List<string>();
            }
            if (data.dayOfWeek < 1 || data.dayOfWeek > 7 || data.week < 1 || (long)(data.week - 1) * 7 + data.dayOfWeek > MaxDay || data.scheduleVersion < 0 || data.scheduleVersion > 2)
                return Result<CareerCalendarState>.Failure("Invalid calendar date or schedule version.");
            if (data.schedules == null || data.schedules.Any(s => s == null || string.IsNullOrWhiteSpace(s.eventId) || s.firstDay < 1 || s.firstDay > MaxDay || s.repeatEveryWeeks < 4 || s.repeatEveryWeeks > 6 || s.roundSpacingDays < 1 || s.roundCount < 1 || (data.scheduleVersion == 2 && (s.roundCount > 5 || s.roundSpacingDays != 1 || s.starterOrder < 0 || s.starterOrder > 3)) || (long)(s.roundCount - 1) * s.roundSpacingDays >= s.repeatEveryWeeks * 7) || data.schedules.Select(s => s.eventId).Distinct().Count() != data.schedules.Count || !ValidIds(data.consumedOccurrences))
                return Result<CareerCalendarState>.Failure("Invalid recurring event schedule.");
            if (data.active != null && !ValidEntry(data.active, false, data.scheduleVersion == 0))
                return Result<CareerCalendarState>.Failure("Invalid active calendar event.");
            if (data.active != null && data.active.scheduledDay == 0 && data.scheduleVersion == 0)
            {
                data.active = data.active.Copy();
                data.active.scheduledDay = (data.week - 1) * 7 + data.dayOfWeek;
                data.active.roundSpacingDays = 7;
                data.active.occurrenceDay = Math.Max(1, data.active.scheduledDay - data.active.roundIndex * 7);
            }
            if (data.active != null && (data.active.scheduledDay < (long)(data.week - 1) * 7 + data.dayOfWeek || data.active.scheduledDay > MaxDay))
                return Result<CareerCalendarState>.Failure("Active event date is behind the calendar.");
            if (data.week < 1 || data.week == int.MaxValue || (data.drawWeek != 0 && data.drawWeek != data.week))
                return Result<CareerCalendarState>.Failure($"Invalid career calendar week (week={data.week}, drawWeek={data.drawWeek}).");
            if (!ValidIds(data.draw) || !ValidIds(data.unlocked) || !ValidIds(data.cancelledAttempts))
                return Result<CareerCalendarState>.Failure("Invalid career calendar ID lists.");
            if (data.draw.Any(id => !data.unlocked.Contains(id)) || (data.drawWeek == 0 && data.draw.Count != 0))
                return Result<CareerCalendarState>.Failure("Invalid career calendar draw.");
            if (!ValidEntry(data.active, false))
                return Result<CareerCalendarState>.Failure("Invalid active calendar event: " + data.active?.eventId);
            if (!ValidEntry(data.lastEvent, true))
                return Result<CareerCalendarState>.Failure("Invalid previous calendar event: " + data.lastEvent?.eventId);
            if (data.active != null && !data.unlocked.Contains(data.active.eventId))
                return Result<CareerCalendarState>.Failure("Active calendar event is not unlocked.");
            if (data.active != null && data.cancelledAttempts.Contains(data.active.pendingInstanceId))
                return Result<CareerCalendarState>.Failure("Active calendar attempt has already been cancelled.");
            var state = new CareerCalendarState();
            state.Data = data.Copy();
            return Result<CareerCalendarState>.Success(state);
        }

        private static bool Empty<T>(List<T> values) => values == null || values.Count == 0;
        private static bool IsEmptyEntry(CareerEventEntryData entry) => entry == null || (entry.ValidDeathmatch() && entry.deathmatchVersion == 0 && string.IsNullOrEmpty(entry.eventId) && string.IsNullOrEmpty(entry.displayName) && string.IsNullOrEmpty(entry.description) && entry.kind == default && entry.occurrenceDay == 0 && entry.scheduledDay == 0 && entry.roundSpacingDays == 0 && entry.entryFee == 0 && entry.roundIndex == 0 && entry.finalRank == 0 && entry.finalPrize == 0 && !entry.completed && !entry.withdrawn && string.IsNullOrEmpty(entry.pendingInstanceId) && string.IsNullOrEmpty(entry.pendingRaceId) && Empty(entry.raceIds) && Empty(entry.payouts) && Empty(entry.prizes) && Empty(entry.points) && Empty(entry.standings) && Empty(entry.deathmatchResults));
        private static bool ValidIds(List<string> ids) => ids != null && ids.All(id => !string.IsNullOrWhiteSpace(id)) && ids.Distinct().Count() == ids.Count;
        private static bool ValidEntry(CareerEventEntryData entry, bool history, bool allowUndated = false)
        {
            if (entry == null)
                return true;
            if (string.IsNullOrWhiteSpace(entry.eventId) || string.IsNullOrWhiteSpace(entry.displayName) || (entry.kind != CareerEventKind.Race && entry.kind != CareerEventKind.Championship && entry.kind != CareerEventKind.Deathmatch && entry.kind != CareerEventKind.Other) || !entry.ValidDeathmatch() || entry.entryFee < 0 || entry.roundIndex < 0 || entry.raceIds == null || entry.raceIds.Count == 0 || entry.raceIds.Any(string.IsNullOrWhiteSpace) || entry.roundIndex > entry.raceIds.Count || (entry.kind != CareerEventKind.Championship && entry.raceIds.Count != 1) || (entry.kind == CareerEventKind.Championship && entry.raceIds.Count < 2) || entry.payouts == null || entry.prizes == null || entry.points == null || entry.payouts.Concat(entry.prizes).Concat(entry.points).Any(p => p < 0) || entry.standings == null || entry.standings.Any(s => s == null || string.IsNullOrWhiteSpace(s.teamId) || s.points < 0) || entry.standings.Select(s => s.teamId).Distinct().Count() != entry.standings.Count || entry.finalRank < 0 || entry.finalPrize < 0)
                return false;
            if (entry.scheduledDay != 0 && (entry.occurrenceDay < 1 || entry.occurrenceDay > entry.scheduledDay || entry.scheduledDay > MaxDay || entry.roundSpacingDays < 1))
                return false;
            if (!history && !allowUndated && entry.scheduledDay == 0) return false;
            if (entry.kind == CareerEventKind.Deathmatch && entry.allowedWinners >= (entry.deathmatchMode == (int)DeathmatchVictoryMode.Team ? entry.standings.Count : entry.standings.Count * 2))
                return false;
            if (!Empty(entry.deathmatchResults))
            {
                if (entry.kind != CareerEventKind.Deathmatch || !history || entry.withdrawn || entry.deathmatchResults.Any(s => s == null || string.IsNullOrWhiteSpace(s.racerId) || s.position < 1 || s.eliminations < 0 || !entry.standings.Any(t => t.teamId == s.teamId) || (s.status != RaceParticipantStatus.Finished && s.status != RaceParticipantStatus.Destroyed && s.status != RaceParticipantStatus.Retired)) || entry.deathmatchResults.Select(s => s.racerId).Distinct().Count() != entry.deathmatchResults.Count)
                    return false;
            }

            if (entry.kind == CareerEventKind.Deathmatch && history && !entry.withdrawn && Empty(entry.deathmatchResults))
                return false;
            if (history)
                return entry.completed && (entry.withdrawn || entry.roundIndex == entry.raceIds.Count) && string.IsNullOrEmpty(entry.pendingInstanceId) && string.IsNullOrEmpty(entry.pendingRaceId);
            if (entry.completed || entry.withdrawn || entry.roundIndex >= entry.raceIds.Count)
                return false;
            return string.IsNullOrEmpty(entry.pendingInstanceId) ? string.IsNullOrEmpty(entry.pendingRaceId) : !string.IsNullOrWhiteSpace(entry.pendingInstanceId) && entry.pendingRaceId == entry.raceIds[entry.roundIndex];
        }
    }
}
