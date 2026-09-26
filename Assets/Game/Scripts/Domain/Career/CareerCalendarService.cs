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
        public const int WeeklyEventCount = 4;
        private readonly GameDatabase database;
        public CareerCalendarService(GameDatabase database) { this.database = database ?? throw new ArgumentNullException(nameof(database)); }

        public Result Validate(CareerEventDefinition definition)
        {
            if (definition == null || !definition.Supported) return Result.Failure("EVENT FORMAT IS NOT AVAILABLE.");
            if (definition.RequiredFame < 0 || definition.EntryFee < 0 || definition.RaceIds.Count == 0 ||
                (definition.Kind == CareerEventKind.Race && definition.RaceIds.Count != 1) ||
                (definition.Kind == CareerEventKind.Championship && definition.RaceIds.Count < 2) ||
                definition.RacePayouts.Concat(definition.ChampionshipPrizes).Concat(definition.PositionPoints).Any(p => p < 0))
                return Result.Failure("INVALID EVENT RULES.");
            RaceDefinition first = null;
            foreach (string id in definition.RaceIds)
            {
                var race = string.IsNullOrWhiteSpace(id) ? null : database.GetRaceDefinition(id);
                if (race == null || race.TeamSize != 2 || race.EntrantCount < 2)
                    return Result.Failure("EVENT NEEDS VALID TWO-RACER TEAM RACES.");
                if (first != null && (race.EngineClass != first.EngineClass || race.EntrantCount != first.EntrantCount))
                    return Result.Failure("CHAMPIONSHIP ROUNDS MUST SHARE ENGINE CLASS AND GRID SIZE.");
                first = race;
            }
            return Result.Success();
        }

        // Called on opening Races, then saved by the UI before entry is enabled.
        public bool Refresh(TeamState team)
        {
            var data = team.Calendar.Data;
            bool changed = false;
            foreach (var definition in database.CareerEventDefinitions.Values.OrderBy(d => d.Id, StringComparer.Ordinal))
                if (Validate(definition).IsSuccess && (team.Fame >= definition.RequiredFame ||
                    (definition.Kind == CareerEventKind.Championship && team.HasChampionshipUnlocked(definition.Id))) && !data.unlocked.Contains(definition.Id))
                { data.unlocked.Add(definition.Id); changed = true; }
            if (data.active != null || data.drawWeek == data.week) return changed;
            var candidates = database.CareerEventDefinitions.Values
                .Where(d => data.unlocked.Contains(d.Id) && Validate(d).IsSuccess).OrderBy(d => d.Id, StringComparer.Ordinal).ToList();
            var random = new Random(unchecked(data.seed ^ data.week * 7919));
            for (int i = candidates.Count - 1; i > 0; i--)
            { int j = random.Next(i + 1); var swap = candidates[i]; candidates[i] = candidates[j]; candidates[j] = swap; }
            var draw = candidates.Take(WeeklyEventCount).ToList();
            // Keep a free route into racing whenever the unlocked pool contains one.
            var free = candidates.FirstOrDefault(e => e.EntryFee == 0 && e.Kind == CareerEventKind.Race);
            if (free != null && !draw.Any(e => e.EntryFee == 0 && e.Kind == CareerEventKind.Race))
            { if (draw.Count == WeeklyEventCount) draw.RemoveAt(draw.Count - 1); draw.Add(free); }
            data.draw = draw.Select(e => e.Id).ToList(); data.drawWeek = data.week;
            return true;
        }

        public Result CanEnter(TeamState team, string eventId)
        {
            if (team == null) return Result.Failure("NO TEAM LOADED.");
            var active = team.Calendar.Data.active;
            if (active != null)
                return active.eventId == eventId ? Result.Success() : Result.Failure("FINISH OR WITHDRAW FROM YOUR CURRENT EVENT.");
            var definition = database.GetCareerEventDefinition(eventId);
            var valid = Validate(definition);
            if (!valid.IsSuccess) return valid;
            if (!team.Calendar.UnlockedIds.Contains(eventId)) return Result.Failure("EVENT HAS NOT BEEN UNLOCKED.");
            if (!team.Calendar.DrawIds.Contains(eventId) || team.Calendar.Data.drawWeek != team.Calendar.Week)
                return Result.Failure("EVENT IS NOT ON THIS WEEK'S CALENDAR.");
            if (team.Credits < definition.EntryFee) return Result.Failure($"ENTRY REQUIRES {definition.EntryFee:N0} CREDITS.");
            return Result.Success();
        }

        public string NextRaceId(TeamState team, string eventId)
        {
            var active = team?.Calendar.Data.active;
            if (active != null) return active.eventId == eventId ? active.raceIds[active.roundIndex] : null;
            return database.GetCareerEventDefinition(eventId)?.RaceIds.FirstOrDefault();
        }

        public Result<CareerEntryTransaction> Register(GameSessionState session, string eventId, RaceDirector race)
        {
            if (session == null || session.CareerRun == null || !session.CareerRun.IsActive || race == null || race.State.IsStarted)
                return Result<CareerEntryTransaction>.Failure("AN ACTIVE CAREER AND PREPARED RACE ARE REQUIRED.");
            var team = session.PlayerTeam;
            var allowed = CanEnter(team, eventId);
            if (!allowed.IsSuccess) return Result<CareerEntryTransaction>.Failure(allowed.ErrorMessage);
            if (race.State.RaceDefinition.Id != NextRaceId(team, eventId))
                return Result<CareerEntryTransaction>.Failure("PREPARED RACE DOES NOT MATCH THE EVENT ROUND.");
            if (!race.State.Participants.Any(p => p.RacerId == session.CareerRun.Player.RacerId &&
                p.TeamId == team.TeamId && p.Role == RaceParticipantRole.Player))
                return Result<CareerEntryTransaction>.Failure("PREPARED RACE DOES NOT CONTAIN THIS CAREER'S PLAYER.");
            var before = team.Calendar.Export();
            string previousChampionship = session.CareerRun.ActiveChampionshipId;
            var active = team.Calendar.Data.active;
            int fee = 0;
            if (active == null)
            {
                var definition = database.GetCareerEventDefinition(eventId);
                fee = definition.EntryFee;
                if (!team.TrySpendCredits(fee)) return Result<CareerEntryTransaction>.Failure("NOT ENOUGH CREDITS.");
                active = new CareerEventEntryData {
                    eventId = eventId, displayName = definition.DisplayName, description = definition.Description,
                    kind = definition.Kind, entryFee = fee, raceIds = definition.RaceIds.ToList(),
                    payouts = definition.RacePayouts.ToList(), prizes = definition.ChampionshipPrizes.ToList(), points = definition.PositionPoints.ToList(),
                    standings = race.State.Participants.GroupBy(p => p.TeamId).Select(g => new ChampionshipStandingData {
                        teamId = g.Key, teamName = g.Key == team.TeamId ? team.TeamName : session.World.FindOpponentTeam(g.Key)?.TeamName ?? g.Key }).ToList() };
                team.Calendar.Data.active = active;
            }
            if (string.IsNullOrEmpty(active.pendingInstanceId))
            { active.pendingInstanceId = race.InstanceId; active.pendingRaceId = race.State.RaceDefinition.Id; }
            else race.UseInstanceId(active.pendingInstanceId);
            if (active.kind == CareerEventKind.Championship) session.CareerRun.EnterChampionship(eventId);
            return Result<CareerEntryTransaction>.Success(new CareerEntryTransaction(session, before, previousChampionship, fee));
        }

        public Result SkipOrWithdraw(GameSessionState session)
        {
            if (session?.CareerRun == null || !session.CareerRun.IsActive) return Result.Failure("NO ACTIVE CAREER.");
            var data = session.PlayerTeam.Calendar.Data;
            if (data.week >= int.MaxValue - 1) return Result.Failure("CALENDAR WEEK LIMIT REACHED.");
            if (data.active != null)
            {
                if (!string.IsNullOrEmpty(data.active.pendingInstanceId)) data.cancelledAttempts.Add(data.active.pendingInstanceId);
                data.active.withdrawn = true; data.active.completed = true;
                data.active.pendingInstanceId = null; data.active.pendingRaceId = null;
                data.lastEvent = data.active; data.active = null;
                session.CareerRun.ExitChampionship();
            }
            data.week++; data.drawWeek = 0; data.draw.Clear();
            return Result.Success();
        }

        public static CalendarSettlement PreviewSettlement(TeamState team, RaceResult race)
        {
            var data = team.Calendar.Export();
            var entry = data.active;
            if (data.cancelledAttempts.Contains(race.InstanceId)) throw new InvalidOperationException("This calendar entry was withdrawn.");
            if (entry == null) return null; // Existing prototype races retain their original reward policy.
            if (entry.pendingInstanceId != race.InstanceId || entry.pendingRaceId != race.RaceId)
                throw new InvalidOperationException("Result does not match the paid calendar entry.");
            if (data.week >= int.MaxValue - 1) throw new InvalidOperationException("Calendar week limit reached.");
            if (race.Standings.Select(s => s.RacerId).Distinct().Count() != race.Standings.Count)
                throw new InvalidOperationException("Duplicate racer in calendar results.");
            foreach (var group in race.Standings.GroupBy(s => s.TeamId))
            {
                var standing = entry.standings.FirstOrDefault(s => s.teamId == group.Key);
                if (standing == null) throw new InvalidOperationException("Unexpected team in championship results.");
                foreach (var result in group)
                    if (result.Status == RaceParticipantStatus.Finished)
                        standing.points = checked(standing.points + At(entry.points, result.Position));
            }
            entry.roundIndex++; entry.pendingInstanceId = null; entry.pendingRaceId = null;
            data.week++; data.drawWeek = 0; data.draw.Clear();
            int bonus = 0;
            var player = race.Standings.FirstOrDefault(s => s.TeamId == team.TeamId && team.Roster.FindRacer(s.RacerId)?.IsPlayerCharacter == true);
            if (player == null) throw new InvalidOperationException("Calendar result has no player.");
            if (entry.roundIndex == entry.raceIds.Count || player.Status == RaceParticipantStatus.Destroyed)
            {
                entry.completed = true; entry.withdrawn = entry.roundIndex < entry.raceIds.Count;
                int ourPoints = entry.standings.First(s => s.teamId == team.TeamId).points;
                entry.finalRank = 1 + entry.standings.Count(s => s.points > ourPoints); // Equal points share rank and prize.
                if (!entry.withdrawn && entry.kind == CareerEventKind.Championship) bonus = At(entry.prizes, entry.finalRank);
                entry.finalPrize = bonus; data.lastEvent = entry; data.active = null;
            }
            int payout = At(entry.payouts, player.Position);
            if (player.Status != RaceParticipantStatus.Finished) payout = (int)Math.Round(payout * .4, MidpointRounding.AwayFromZero);
            var validation = CareerCalendarState.Restore(data);
            if (!validation.IsSuccess) throw new InvalidOperationException(validation.ErrorMessage);
            return new CalendarSettlement(data, checked(payout + bonus));
        }
        public static int At(IReadOnlyList<int> values, int position) => position > 0 && position <= values.Count ? values[position - 1] : 0;
    }

    public sealed class CalendarSettlement
    {
        public CareerCalendarData Calendar { get; }
        public int Credits { get; }
        public CalendarSettlement(CareerCalendarData calendar, int credits) { Calendar = calendar; Credits = credits; }
    }

    public sealed class CareerEntryTransaction
    {
        private readonly GameSessionState session;
        private readonly CareerCalendarData before;
        private readonly string previousChampionship;
        private readonly int fee;
        private bool rolledBack;
        internal CareerEntryTransaction(GameSessionState session, CareerCalendarData before, string championship, int fee)
        { this.session = session; this.before = before; previousChampionship = championship; this.fee = fee; }
        public void Rollback()
        {
            if (rolledBack) return;
            rolledBack = true;
            session.PlayerTeam.RestoreCalendar(before);
            session.PlayerTeam.AddCredits(fee);
            if (previousChampionship == null) session.CareerRun.ExitChampionship();
            else session.CareerRun.EnterChampionship(previousChampionship);
        }
    }
}
