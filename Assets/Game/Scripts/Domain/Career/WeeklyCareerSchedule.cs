using System;
using System.Collections.Generic;
using System.Linq;

namespace RaceFatal.Career
{
    // Pure, versioned generation: the saved seed and pool determine every week, even
    // when weeks are browsed out of order. Never uses Unity's global random state.
    public static class WeeklyCareerSchedule
    {
        public static IReadOnlyList<CareerEventOccurrence> Generate(CareerCalendarData data, int week)
        {
            var result = new List<CareerEventOccurrence>();
            if (week < 1 || week > CareerCalendarState.MaxDay / 7) return result;
            var pool = data.schedules.Where(s => s.roundCount <= 5 &&
                (week == 1 ? s.starterOrder > 0 : (s.firstDay - 1) / 7 + 1 <= week))
                .OrderBy(s => s.starterOrder).ThenBy(s => s.eventId, StringComparer.Ordinal).ToList();
            // Small test/development catalogs without designated starters still work.
            if (week == 1 && pool.Count == 0)
                pool = data.schedules.Where(s => s.roundCount == 1).OrderBy(s => s.eventId, StringComparer.Ordinal).Take(3).ToList();
            uint random = unchecked((uint)data.seed ^ ((uint)week * 0x9E3779B9u));
            if (random == 0) random = 0xA341316Cu;
            if (week > 1)
                for (int i = pool.Count - 1; i > 0; i--)
                {
                    random ^= random << 13; random ^= random >> 17; random ^= random << 5;
                    int j = (int)(random % (uint)(i + 1));
                    var temp = pool[i]; pool[i] = pool[j]; pool[j] = temp;
                }
            // Backtrack over placement and selection so a long championship cannot
            // fragment the week and prevent three otherwise compatible events fitting.
            for (int count = Math.Min(3, pool.Count); count > 0; count--)
                if (Place(pool, 0, count, 0, (week - 1) * 7, result)) break;
            return result.OrderBy(e => e.Day).ToList().AsReadOnly();
        }

        private static bool Place(List<CareerEventScheduleData> pool, int index, int remaining,
            int occupied, int weekOffset, List<CareerEventOccurrence> result)
        {
            if (remaining == 0) return true;
            for (int i = index; i <= pool.Count - remaining; i++)
            {
                var item = pool[i];
                int preferred = (item.firstDay - 1) % 7 + 1;
                // Distance first, later first on ties; a whole block must stay in-week.
                foreach (int day in Enumerable.Range(1, 7).OrderBy(d => Math.Abs(d - preferred)).ThenByDescending(d => d))
                {
                    if (day + item.roundCount - 1 > 7) continue;
                    int mask = ((1 << item.roundCount) - 1) << (day - 1);
                    if ((occupied & mask) != 0) continue;
                    int before = result.Count;
                    for (int round = 0; round < item.roundCount; round++)
                        result.Add(new CareerEventOccurrence(item.eventId, weekOffset + day + round, round, false, weekOffset + day));
                    if (Place(pool, i + 1, remaining - 1, occupied | mask, weekOffset, result)) return true;
                    result.RemoveRange(before, result.Count - before);
                }
            }
            return false;
        }
    }
}
