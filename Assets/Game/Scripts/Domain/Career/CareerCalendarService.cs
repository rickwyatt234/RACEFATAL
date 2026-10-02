using System;
using System.Collections.Generic;
using System.Linq;
using RaceFatal.Data;
using RaceFatal.Racing;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public sealed class CareerCalendarService
    {
        private readonly GameDatabase database;
        public CareerCalendarService(GameDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public Result Validate(CareerEventDefinition definition)
        {
            if (definition == null || !definition.Supported)
                return Result.Failure("EVENT FORMAT IS NOT AVAILABLE.");
            if (definition.RequiredFame < 0 || definition.EntryFee < 0 || definition.RaceIds.Count == 0 || (definition.Kind != CareerEventKind.Championship && definition.RaceIds.Count != 1) || (definition.Kind == CareerEventKind.Championship && definition.RaceIds.Count < 2) || definition.RacePayouts.Concat(definition.ChampionshipPrizes).Concat(definition.PositionPoints).Any(p => p < 0))
                return Result.Failure("INVALID EVENT RULES.");
            if (definition.FirstWeek < 1 || definition.FirstWeek > CareerCalendarState.MaxDay / 7 || definition.FirstDayOfWeek < 1 || definition.FirstDayOfWeek > 7 || definition.RepeatEveryWeeks < 4 || definition.RepeatEveryWeeks > 6 || definition.RoundSpacingDays < 1 || (long)(definition.RaceIds.Count - 1) * definition.RoundSpacingDays >= definition.RepeatEveryWeeks * 7)
                return Result.Failure("INVALID EVENT DATES: rounds must finish before the next occurrence.");
            RaceDefinition first = null;
            foreach (string id in definition.RaceIds)
            {
                var race = string.IsNullOrWhiteSpace(id) ? null : database.GetRaceDefinition(id);
                if (race == null || race.TeamSize != 2 || race.EntrantCount < 2)
                    return Result.Failure("EVENT NEEDS VALID TWO-RACER TEAM RACES.");
                if ((definition.Kind == CareerEventKind.Deathmatch) != (race.Deathmatch != null))
                    return Result.Failure("EVENT FORMAT MUST MATCH THE RACE DEFINITION RULES.");
                if (first != null && (race.EngineClass != first.EngineClass || race.EntrantCount != first.EntrantCount))
                    return Result.Failure("CHAMPIONSHIP ROUNDS MUST SHARE ENGINE CLASS AND GRID SIZE.");
                first = race;
            }

            return Result.Success();
        }

        public bool Refresh(TeamState team)
        {
            var data = team.Calendar.Data;
            bool changed = false;
            foreach (var definition in database.CareerEventDefinitions.Values.OrderBy(d => d.Id, StringComparer.Ordinal))
                if (Validate(definition).IsSuccess && (team.Fame >= definition.RequiredFame || (definition.Kind == CareerEventKind.Championship && team.HasChampionshipUnlocked(definition.Id))) && !data.unlocked.Contains(definition.Id))
                {
                    data.unlocked.Add(definition.Id);
                    changed = true;
                }

            if (data.scheduleVersion == 0)
            {
                data.schedules = database.CareerEventDefinitions.Values.Where(d => Validate(d).IsSuccess)
                    .OrderBy(d => d.Id, StringComparer.Ordinal)
                    .Select(d => new CareerEventScheduleData { eventId = d.Id, firstDay = d.FirstAbsoluteDay, repeatEveryWeeks = d.RepeatEveryWeeks, roundSpacingDays = d.RoundSpacingDays, roundCount = d.RaceIds.Count }).ToList();
                data.scheduleVersion = 1;
                if (data.active != null)
                {
                    string key = CareerCalendarState.OccurrenceKey(data.active.eventId, data.active.occurrenceDay);
                    if (!data.consumedOccurrences.Contains(key)) data.consumedOccurrences.Add(key);
                }
                changed = true;
            }
            // Compatibility list used by the Races screen: upcoming unlocked event types.
            var upcoming = data.schedules.Where(e => data.unlocked.Contains(e.eventId) && NextOccurrenceDay(team, e.eventId).HasValue)
                .OrderBy(e => NextOccurrenceDay(team, e.eventId)).ThenBy(e => e.eventId, StringComparer.Ordinal).Select(e => e.eventId).ToList();
            if (data.drawWeek != data.week || !data.draw.SequenceEqual(upcoming))
            { data.draw = upcoming; data.drawWeek = data.week; changed = true; }
            return changed;
        }

        public int? NextOccurrenceDay(TeamState team, string eventId, int? fromDay = null)
        {
            if (team == null || string.IsNullOrWhiteSpace(eventId)) return null;
            var active = team.Calendar.Data.active;
            if (active?.eventId == eventId && (!fromDay.HasValue || active.scheduledDay >= fromDay.Value)) return active.scheduledDay;
            var schedule = team.Calendar.Data.schedules.FirstOrDefault(e => e.eventId == eventId);
            if (schedule == null) return null;
            long from = Math.Max(team.Calendar.AbsoluteDay, fromDay ?? team.Calendar.AbsoluteDay);
            long period = schedule.repeatEveryWeeks * 7;
            long day = schedule.firstDay + Math.Max(0L, (from - schedule.firstDay + period - 1) / period) * period;
            while (day <= CareerCalendarState.MaxDay && team.Calendar.IsConsumed(eventId, (int)day)) day += period;
            return day <= CareerCalendarState.MaxDay ? (int?)day : null;
        }

        public IReadOnlyList<CareerEventOccurrence> GetOccurrences(TeamState team, int fromDay, int throughDay)
        {
            var items = new List<CareerEventOccurrence>();
            if (team == null || fromDay < 1 || throughDay < fromDay || (long)throughDay - fromDay > 366)
                return items;
            foreach (var schedule in team.Calendar.Data.schedules)
            {
                long period = schedule.repeatEveryWeeks * 7;
                long earliestStart = (long)fromDay - (long)(schedule.roundCount - 1) * schedule.roundSpacingDays;
                long day = schedule.firstDay + Math.Max(0L, (earliestStart - schedule.firstDay + period - 1) / period) * period;
                for (; day <= throughDay && day <= CareerCalendarState.MaxDay; day += period)
                    for (int round = 0; round < schedule.roundCount; round++)
                    {
                        long roundDay = day + (long)round * schedule.roundSpacingDays;
                        if (roundDay >= fromDay && roundDay <= throughDay && roundDay <= CareerCalendarState.MaxDay)
                            items.Add(new CareerEventOccurrence(schedule.eventId, (int)roundDay, round, false, (int)day));
                    }
            }
            var active = team.Calendar.Data.active;
            if (active != null)
                for (int i = active.roundIndex; i < active.raceIds.Count; i++)
                {
                    long day = (long)active.scheduledDay + (i - active.roundIndex) * active.roundSpacingDays;
                    if (day >= fromDay && day <= throughDay && day <= CareerCalendarState.MaxDay)
                    {
                        items.RemoveAll(e => e.EventId == active.eventId && e.Day == day);
                        items.Add(new CareerEventOccurrence(active.eventId, (int)day, i, true, active.occurrenceDay));
                    }
                }
            return items.OrderBy(e => e.Day).ThenBy(e => e.EventId, StringComparer.Ordinal).ToList().AsReadOnly();
        }

        public Result CanEnter(TeamState team, string eventId, int occurrenceDay = 0)
        {
            if (team == null)
                return Result.Failure("NO TEAM LOADED.");
            var active = team.Calendar.Data.active;
            if (active != null)
                return active.eventId == eventId && (occurrenceDay == 0 || occurrenceDay == active.scheduledDay)
                    ? Result.Success() : Result.Failure("FINISH OR WITHDRAW FROM YOUR CURRENT EVENT; only its next round is enterable.");
            var definition = database.GetCareerEventDefinition(eventId);
            var valid = Validate(definition);
            if (!valid.IsSuccess)
                return valid;
            if (!team.Calendar.UnlockedIds.Contains(eventId))
                return Result.Failure("EVENT HAS NOT BEEN UNLOCKED.");
            var schedule = team.Calendar.Data.schedules.FirstOrDefault(e => e.eventId == eventId);
            var next = NextOccurrenceDay(team, eventId);
            int day = occurrenceDay == 0 ? next ?? 0 : occurrenceDay;
            if (schedule == null || day < team.Calendar.AbsoluteDay || day < schedule.firstDay || day > CareerCalendarState.MaxDay || (day - schedule.firstDay) % (schedule.repeatEveryWeeks * 7) != 0)
                return Result.Failure("EVENT IS NOT SCHEDULED ON THAT DATE, OR ITS DATE HAS PASSED.");
            if (definition.RaceIds.Count != schedule.roundCount)
                return Result.Failure("THE EVENT ROUND COUNT HAS CHANGED SINCE THIS CAMPAIGN WAS CREATED.");
            if (team.Calendar.IsConsumed(eventId, day))
                return Result.Failure("THIS EVENT OCCURRENCE HAS ALREADY BEEN ENTERED.");
            if ((long)day + (long)(definition.RaceIds.Count - 1) * schedule.roundSpacingDays > CareerCalendarState.MaxDay)
                return Result.Failure("EVENT WOULD EXCEED THE CALENDAR LIMIT.");
            if (team.Credits < definition.EntryFee)
                return Result.Failure($"ENTRY REQUIRES {definition.EntryFee:N0} CREDITS.");
            return Result.Success();
        }

        public string NextRaceId(TeamState team, string eventId)
        {
            var active = team?.Calendar.Data.active;
            if (active != null)
                return active.eventId == eventId ? active.raceIds[active.roundIndex] : null;
            return database.GetCareerEventDefinition(eventId)?.RaceIds.FirstOrDefault();
        }

        public Result<CareerEntryTransaction> Register(GameSessionState session, string eventId, RaceDirector race, int occurrenceDay = 0)
        {
            if (session == null || session.CareerRun == null || !session.CareerRun.IsActive || race == null || race.State.IsStarted)
                return Result<CareerEntryTransaction>.Failure("AN ACTIVE CAREER AND PREPARED RACE ARE REQUIRED.");
            var team = session.PlayerTeam;
            var allowed = CanEnter(team, eventId, occurrenceDay);
            if (!allowed.IsSuccess)
                return Result<CareerEntryTransaction>.Failure(allowed.ErrorMessage);
            if (race.State.RaceDefinition.Id != NextRaceId(team, eventId))
                return Result<CareerEntryTransaction>.Failure("PREPARED RACE DOES NOT MATCH THE EVENT ROUND.");
            if (!race.State.Participants.Any(p => p.RacerId == session.CareerRun.Player.RacerId && p.TeamId == team.TeamId && p.Role == RaceParticipantRole.Player))
                return Result<CareerEntryTransaction>.Failure("PREPARED RACE DOES NOT CONTAIN THIS CAREER'S PLAYER.");
            var before = team.Calendar.Export();
            string previousChampionship = session.CareerRun.ActiveChampionshipId;
            var active = team.Calendar.Data.active;
            int fee = 0;
            if (active == null)
            {
                var definition = database.GetCareerEventDefinition(eventId);
                if ((definition.Kind == CareerEventKind.Deathmatch) != (race.State.Deathmatch != null))
                    return Result<CareerEntryTransaction>.Failure("PREPARED RACE FORMAT DOES NOT MATCH THE EVENT.");
                fee = definition.EntryFee;
                if (!team.TrySpendCredits(fee))
                    return Result<CareerEntryTransaction>.Failure("NOT ENOUGH CREDITS.");
                var schedule = team.Calendar.Data.schedules.First(e => e.eventId == eventId);
                int day = occurrenceDay == 0 ? NextOccurrenceDay(team, eventId).Value : occurrenceDay;
                active = new CareerEventEntryData
                {
                    occurrenceDay = day,
                    scheduledDay = day,
                    roundSpacingDays = schedule.roundSpacingDays,
                    eventId = eventId,
                    displayName = definition.DisplayName,
                    description = definition.Description,
                    kind = definition.Kind,
                    entryFee = fee,
                    raceIds = definition.RaceIds.ToList(),
                    payouts = definition.RacePayouts.ToList(),
                    prizes = definition.ChampionshipPrizes.ToList(),
                    points = definition.PositionPoints.ToList(),
                    standings = race.State.Participants.GroupBy(p => p.TeamId).Select(g => new ChampionshipStandingData { teamId = g.Key, teamName = g.Key == team.TeamId ? team.TeamName : session.World.FindOpponentTeam(g.Key)?.TeamName ?? g.Key }).ToList()
                };
                active.CaptureDeathmatch(race.State.Deathmatch);
                team.Calendar.Data.active = active;
                team.Calendar.Data.consumedOccurrences.Add(CareerCalendarState.OccurrenceKey(eventId, day));
            }

            CareerCalendarState.SetDay(team.Calendar.Data, active.scheduledDay);
            race.UseDeathmatchRules(active.kind == CareerEventKind.Deathmatch ? active.RestoreDeathmatch() : null);
            if (string.IsNullOrEmpty(active.pendingInstanceId))
            {
                active.pendingInstanceId = race.InstanceId;
                active.pendingRaceId = race.State.RaceDefinition.Id;
            }
            else
                race.UseInstanceId(active.pendingInstanceId);
            if (active.kind == CareerEventKind.Championship)
                session.CareerRun.EnterChampionship(eventId);
            return Result<CareerEntryTransaction>.Success(new CareerEntryTransaction(session, before, previousChampionship, fee));
        }

        public Result SkipOrWithdraw(GameSessionState session)
        {
            if (session?.CareerRun == null || !session.CareerRun.IsActive)
                return Result.Failure("NO ACTIVE CAREER.");
            var data = session.PlayerTeam.Calendar.Data;
            if (data.active == null)
            {
                int? next = data.schedules.Where(e => data.unlocked.Contains(e.eventId))
                    .Select(e => NextOccurrenceDay(session.PlayerTeam, e.eventId, session.PlayerTeam.Calendar.AbsoluteDay == CareerCalendarState.MaxDay ? CareerCalendarState.MaxDay : session.PlayerTeam.Calendar.AbsoluteDay + 1))
                    .Where(d => d.HasValue && d.Value > session.PlayerTeam.Calendar.AbsoluteDay).OrderBy(d => d).FirstOrDefault();
                if (!next.HasValue) return Result.Failure("NO LATER UNLOCKED EVENTS ARE SCHEDULED.");
                CareerCalendarState.SetDay(data, next.Value);
                return Result.Success();
            }
            if (data.active != null)
            {
                if (!string.IsNullOrEmpty(data.active.pendingInstanceId))
                    data.cancelledAttempts.Add(data.active.pendingInstanceId);
                data.active.withdrawn = true;
                data.active.completed = true;
                data.active.pendingInstanceId = null;
                data.active.pendingRaceId = null;
                data.lastEvent = data.active;
                data.active = null;
                session.CareerRun.ExitChampionship();
            }

            data.drawWeek = 0;
            data.draw.Clear();
            return Result.Success();
        }

        public static CalendarSettlement PreviewSettlement(TeamState team, RaceResult race)
        {
            var data = team.Calendar.Export();
            var entry = data.active;
            if (data.cancelledAttempts.Contains(race.InstanceId))
                throw new InvalidOperationException("This calendar entry was withdrawn.");
            if (entry == null)
                return null;
            if (entry.pendingInstanceId != race.InstanceId || entry.pendingRaceId != race.RaceId)
                throw new InvalidOperationException("Result does not match the paid calendar entry.");
            if (race.Standings.Select(s => s.RacerId).Distinct().Count() != race.Standings.Count)
                throw new InvalidOperationException("Duplicate racer in calendar results.");
            foreach (var group in race.Standings.GroupBy(s => s.TeamId))
            {
                var standing = entry.standings.FirstOrDefault(s => s.teamId == group.Key);
                if (standing == null)
                    throw new InvalidOperationException("Unexpected team in championship results.");
                foreach (var result in group)
                    if (result.Status == RaceParticipantStatus.Finished)
                        standing.points = checked(standing.points + At(entry.points, result.Position));
            }

            entry.roundIndex++;
            entry.pendingInstanceId = null;
            entry.pendingRaceId = null;
            data.drawWeek = 0;
            data.draw.Clear();
            int bonus = 0;
            var player = race.Standings.FirstOrDefault(s => s.TeamId == team.TeamId && team.Roster.FindRacer(s.RacerId)?.IsPlayerCharacter == true);
            if (player == null)
                throw new InvalidOperationException("Calendar result has no player.");
            if (entry.roundIndex == entry.raceIds.Count || player.Status == RaceParticipantStatus.Destroyed)
            {
                entry.completed = true;
                entry.withdrawn = entry.roundIndex < entry.raceIds.Count;
                int ourPoints = entry.standings.First(s => s.teamId == team.TeamId).points;
                entry.finalRank = 1 + entry.standings.Count(s => s.points > ourPoints);
                if (!entry.withdrawn && entry.kind == CareerEventKind.Championship)
                    bonus = At(entry.prizes, entry.finalRank);
                entry.finalPrize = bonus;
                data.lastEvent = entry;
                data.active = null;
            }

            if (entry.kind == CareerEventKind.Deathmatch)
            {
                entry.finalRank = player.Position;
                entry.deathmatchResults = race.Standings.Select(s => new DeathmatchStandingData { racerId = s.RacerId, racerName = s.RacerName, teamId = s.TeamId, teamName = s.TeamName, position = s.Position, eliminations = s.Eliminations, winner = s.IsWinner, status = s.Status, eliminationReason = s.EliminationReason }).ToList();
            }

            int payout = At(entry.payouts, player.Position);
            bool teamSurvived = race.Deathmatch?.Mode == DeathmatchVictoryMode.Team && race.Standings.Any(s => s.TeamId == team.TeamId && s.Status == RaceParticipantStatus.Finished);
            if (player.Status != RaceParticipantStatus.Finished && !teamSurvived)
                payout = (int)Math.Round(payout * .4, MidpointRounding.AwayFromZero);
            // Completing a round leaves today on its race date. Entering the next round
            // advances to its date, preserving other dates in the same career week.
            if (data.active != null)
                data.active.scheduledDay = checked(data.active.scheduledDay + data.active.roundSpacingDays);
            var validation = CareerCalendarState.Restore(data);
            if (!validation.IsSuccess)
                throw new InvalidOperationException(validation.ErrorMessage);
            return new CalendarSettlement(data, checked(payout + bonus));
        }

        public static int At(IReadOnlyList<int> values, int position) => position > 0 && position <= values.Count ? values[position - 1] : 0;
    }

    public sealed class CalendarSettlement
    {
        public CareerCalendarData Calendar { get; }
        public int Credits { get; }

        public CalendarSettlement(CareerCalendarData calendar, int credits)
        {
            Calendar = calendar;
            Credits = credits;
        }
    }

    public sealed class CareerEntryTransaction
    {
        private readonly GameSessionState session;
        private readonly CareerCalendarData before;
        private readonly string previousChampionship;
        private readonly int fee;
        private bool rolledBack;
        internal CareerEntryTransaction(GameSessionState session, CareerCalendarData before, string championship, int fee)
        {
            this.session = session;
            this.before = before;
            previousChampionship = championship;
            this.fee = fee;
        }

        public void Rollback()
        {
            if (rolledBack)
                return;
            rolledBack = true;
            session.PlayerTeam.RestoreCalendar(before);
            session.PlayerTeam.AddCredits(fee);
            if (previousChampionship == null)
                session.CareerRun.ExitChampionship();
            else
                session.CareerRun.EnterChampionship(previousChampionship);
        }
    }
}
