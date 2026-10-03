using System;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Data;

static class WeeklyChecks
{
    public static void Run(TeamState team, GameDatabase db)
    {
        int checks = 0;
        void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
        var data = team.Calendar.Export();
        data.seed = 123;
        foreach (var e in data.schedules)
            e.starterOrder = e.eventId == "day2" ? 1 : e.eventId == "day4" ? 2 : e.eventId == "day6" ? 3 : 0;
        string Key(CareerCalendarData d, int week) => string.Join(",", WeeklyCareerSchedule.Generate(d, week).Select(e => e.EventId + "@" + e.Day));
        var first = WeeklyCareerSchedule.Generate(data, 1);
        Check(first.Select(e => e.Day).SequenceEqual(new[] {2,4,6}), "Fixed starter dates");
        var other = data.Copy(); other.seed = 98765;
        Check(Key(data, 1) == Key(other, 1), "Week one seed independent");
        Check(Enumerable.Range(2, 20).Any(w => Key(data,w) != Key(other,w)), "Different campaigns draw different calendars");
        for (int seed = 1; seed <= 40; seed++)
        {
            data.seed = seed;
            for (int week = 2; week <= 52; week++)
            {
                var events = WeeklyCareerSchedule.Generate(data,week);
                Check(events.Count(e => e.RoundIndex == 0) == 3, "Exactly three weekly events");
                Check(events.Select(e => e.Day).Distinct().Count() == events.Count, "No shared slots");
                Check(events.All(e => (e.Day-1)/7+1 == week), "Whole blocks remain in-week");
                foreach (var group in events.GroupBy(e => e.EventId))
                    Check(group.Select(e => e.Day).SequenceEqual(Enumerable.Range(group.First().Day,group.Count())), "Consecutive championship days");
                Check(Key(data,week) == Key(data.Copy(),week), "Stable snapshot generation");
            }
        }
        var collision = new CareerCalendarData { seed=1, scheduleVersion=2, dayOfWeek=1 };
        foreach (var id in new[]{"a","b","c"}) collision.schedules.Add(new CareerEventScheduleData {eventId=id, firstDay=4,roundCount=1,roundSpacingDays=1,repeatEveryWeeks=5,starterOrder=collision.schedules.Count+1});
        Check(Key(collision,1) == "c@3,a@4,b@5", "Nearest slot prefers later on ties");
        foreach (var e in collision.schedules) e.firstDay=7;
        Check(Key(collision,1) == "c@5,b@6,a@7", "Week boundary moves earlier");
        collision.schedules[0].roundCount=3;
        var block=WeeklyCareerSchedule.Generate(collision,1);
        Check(block.Where(e=>e.EventId=="a").Select(e=>e.Day).SequenceEqual(new[]{5,6,7}), "Three-day block shifts earlier as a whole");
        Check(block.Select(e=>e.Day).Distinct().Count()==5, "Other events avoid all championship slots");
        Check(team.RestoreCalendar(data).IsSuccess, "Weekly state restores");
        var service = new CareerCalendarService(db);
        Check(service.GetAvailableEventsForWeek(team,1).Count==3, "This week lists exactly the three available starters");
        Check(service.GetAvailableEventsForWeek(team,2).Count==3 && service.GetAvailableEventsForWeek(team,2).All(e=>e.Day>=8 && e.Day<=14), "Upcoming events are confined to next week");
        var scheduled=service.GetOccurrences(team,8,364).First(e=>e.EventId=="champ" && e.RoundIndex==0);
        Check(service.CanEnter(team,"champ",scheduled.Day).IsSuccess, "Generated championship start is enterable");
        Check(!service.CanEnter(team,"champ",scheduled.Day+1).IsSuccess, "Unentered championship cannot start at round two");
        Check(service.NextOccurrenceDay(team,"champ")==scheduled.Day, "Next date agrees with calendar");
        Check(team.Calendar.AbsoluteDay==1,"Browsing does not advance time");
        Console.WriteLine($"PASS: {checks} weekly-pool checks");
    }
}
