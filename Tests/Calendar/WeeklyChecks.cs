using System;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Data;

static class WeeklyChecks
{
    public static void Run(TeamState team,GameDatabase db)
    {
        int checks=0;
        void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
        var data=team.Calendar.Export();
        data.seed=123;
        foreach(var e in data.schedules)
            e.starterOrder=e.eventId=="day2"?1:e.eventId=="day4"?2:e.eventId=="day6"?3:0;
        string Key(CareerCalendarData d,int week)=>string.Join(",",WeeklyCareerSchedule.Generate(d,week)
            .Select(e=>e.EventId+"@"+e.Day));
        var first=WeeklyCareerSchedule.Generate(data,1);
        Check(first.Any(e=>e.EventId=="day2" && e.Day==2),"First starter retains fixed day 2");
        Check(first.Any(e=>e.EventId=="day4" && e.Day==4),"Second starter retains fixed day 4");
        Check(first.Any(e=>e.EventId=="day6" && e.Day==6),"Third starter retains fixed day 6");
        var copied=data.Copy();
        Check(Key(data,1)==Key(copied,1),"Seed and schedule snapshot replay identical");
        for(int seed=1;seed<=15;seed++)
        {
            data.seed=seed;
            for(int week=2;week<=52;week++)
            {
                var events=WeeklyCareerSchedule.Generate(data,week);
                Check(events.Count(e=>e.RoundIndex==0)<=25,"Capped weekly offerings");
                Check(events.All(e=>(e.Day-1)/7+1==week),"Each championship stays within its chosen week");
                foreach(var g in events.GroupBy(e=>e.EventId))
                    Check(g.Select(e=>e.Day).SequenceEqual(Enumerable.Range(g.First().Day,g.Count())),"Championship rounds consecutive");
                Check(Key(data,week)==Key(data.Copy(),week),"Deterministic replay");
            }
        }
        // Shared days are intentional: each offering is an alternative, not a simulated race.
        var collision=new CareerCalendarData{seed=1,scheduleVersion=2,dayOfWeek=1};
        foreach(var id in new[]{"a","b","c"})
            collision.schedules.Add(new CareerEventScheduleData{eventId=id,firstDay=4,roundCount=1,roundSpacingDays=1,repeatEveryWeeks=5,starterOrder=collision.schedules.Count+1});
        Check(WeeklyCareerSchedule.Generate(collision,1).Count(e=>e.Day==4)>=3,"Simultaneous events share a date");
        collision.schedules[0].roundCount=3;
        var block=WeeklyCareerSchedule.Generate(collision,1);
        Check(block.Where(e=>e.EventId=="a").Count()==3,"Three-day championship length retained");

        var dense=new CareerCalendarData{seed=77,scheduleVersion=2,dayOfWeek=1};
        for(int i=0;i<150;i++)dense.schedules.Add(new CareerEventScheduleData{
            eventId="race_"+i.ToString("D3"),firstDay=1,repeatEveryWeeks=5,
            roundSpacingDays=1,roundCount=1,kind=CareerEventKind.Race
        });
        for(int i=0;i<25;i++)dense.schedules.Add(new CareerEventScheduleData{
            eventId="champ_"+i.ToString("D3"),firstDay=1,repeatEveryWeeks=5,
            roundSpacingDays=1,roundCount=3,kind=CareerEventKind.Championship
        });
        var full=WeeklyCareerSchedule.Generate(dense,1);
        Check(full.Count(e=>e.RoundIndex==0 && !e.EventId.StartsWith("champ_"))>=12,"Dense pool gives at least 12 regular events");
        Check(full.Count(e=>e.RoundIndex==0 && !e.EventId.StartsWith("champ_"))<=20,"At most 20 regular events");
        Check(full.Count(e=>e.RoundIndex==0 && e.EventId.StartsWith("champ_"))<=5,"Championship opportunities capped");

        Check(team.RestoreCalendar(data).IsSuccess,"Weekly state restores");
        var service=new CareerCalendarService(db);
        Check(service.GetAvailableEventsForWeek(team,1).Count>=3,"Week one retains three starters");
        Check(service.GetAvailableEventsForWeek(team,2).All(e=>e.Day>=8 && e.Day<=14),"Next-week dates accurate");
        var firstChamp=service.GetOccurrences(team,1,364).First(e=>e.EventId=="champ" && e.RoundIndex==0);
        Check(service.CanEnter(team,"champ",firstChamp.Day).IsSuccess,"Championship first round enterable");
        Check(!service.CanEnter(team,"champ",firstChamp.Day+1).IsSuccess,"Cannot enter championship midstream");
        Check(service.NextOccurrenceDay(team,"champ")==firstChamp.Day,"Next date matches schedule");
        Check(team.Calendar.AbsoluteDay==1,"Calendar browsing does not advance time");
        Console.WriteLine("PASS: "+checks+" dense calendar checks");
    }
}
