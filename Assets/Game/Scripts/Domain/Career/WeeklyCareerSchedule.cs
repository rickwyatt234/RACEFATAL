using System;
using System.Collections.Generic;
using System.Linq;

namespace RaceFatal.Career
{
    /// <summary>
    /// Seeded competition calendar. Multiple events may share a date; an entered
    /// championship remains the player's only active event until it is resolved.
    /// The schedule is deterministic from the saved seed, template pool and week.
    /// </summary>
    public static class WeeklyCareerSchedule
    {
        public static IReadOnlyList<CareerEventOccurrence> Generate(CareerCalendarData data,int week)
        {
            if(data==null || week<1 || week>CareerCalendarState.MaxDay/7)
                return Array.Empty<CareerEventOccurrence>();
            var all=data.schedules.Where(s=>s!=null && s.roundCount>=1 && s.roundCount<=7 &&
                (s.firstDay-1)/7+1<=week).OrderBy(s=>s.eventId,StringComparer.Ordinal).ToList();

            bool developmentPool=all.Count<12;
            var pool=all.Where(s=>developmentPool || s.starterOrder>0 && week==1 ||
                (s.starterOrder==0 && IsDue(s,week))).ToList();
            if(week==1 && !pool.Any(s=>s.starterOrder>0))
                pool.AddRange(all.Where(s=>s.roundCount==1 && !pool.Contains(s)).Take(Math.Max(0,3-pool.Count)));
            var results=new List<CareerEventOccurrence>();
            uint state=unchecked((uint)data.seed ^ ((uint)week*0x9E3779B9u));
            if(state==0)state=0xA341316Cu;
            var starters=pool.Where(s=>s.starterOrder>0 && week==1).OrderBy(s=>s.starterOrder).Take(3).ToList();
            foreach(var starter in starters)
                Add(results,starter,(week-1)*7+Math.Min(starter.starterOrder,8-starter.roundCount));
            var regular=Shuffle(pool.Where(s=>s.kind!=CareerEventKind.Championship && !starters.Contains(s)).ToList(),ref state);
            var championships=Shuffle(pool.Where(s=>s.kind==CareerEventKind.Championship && !starters.Contains(s)).ToList(),ref state);
            // At most 20 regular offerings per week when the authored pool supports it.
            int regularTarget=developmentPool?Math.Min(3,regular.Count):12+(int)(Next(ref state)%9u);
            int championshipTarget=developmentPool?Math.Min(1,championships.Count):2+(int)(Next(ref state)%4u);
            foreach(var s in regular.Take(regularTarget))
                Add(results,s,(week-1)*7+1+(int)(Next(ref state)%(uint)(8-s.roundCount)));
            foreach(var s in championships.Take(championshipTarget))
                Add(results,s,(week-1)*7+1+(int)(Next(ref state)%(uint)(8-s.roundCount)));
            return results.OrderBy(e=>e.Day).ThenBy(e=>e.EventId,StringComparer.Ordinal).ToList().AsReadOnly();
        }

        private static bool IsDue(CareerEventScheduleData s,int week)
        {
            int firstWeek=(s.firstDay-1)/7+1;
            int period=Math.Max(1,s.repeatEveryWeeks);
            int offset=week-firstWeek;
            return offset>=0 && offset%period==(int)(StableHash(s.eventId)%(uint)period);
        }

        private static uint StableHash(string id)
        {
            uint hash=2166136261u;
            foreach(char c in id ?? "")hash=unchecked((hash^c)*16777619u);
            return hash;
        }

        private static uint Next(ref uint state)
        {
            state^=state<<13;state^=state>>17;state^=state<<5;
            return state;
        }

        private static List<CareerEventScheduleData> Shuffle(List<CareerEventScheduleData> pool,ref uint state)
        {
            for(int i=pool.Count-1;i>0;i--)
            {
                int j=(int)(Next(ref state)%(uint)(i+1));
                var value=pool[j];pool[j]=pool[i];pool[i]=value;
            }
            return pool;
        }

        private static void Add(List<CareerEventOccurrence> result,CareerEventScheduleData s,int start)
        {
            for(int round=0;round<s.roundCount;round++)
                result.Add(new CareerEventOccurrence(s.eventId,start+round,round,false,start));
        }
    }
}
