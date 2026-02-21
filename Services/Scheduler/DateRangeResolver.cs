using AirlogGenerator.Config;
using AirlogGenerator.Services;
using AirlogGenerator.Models;

namespace AirlogGenerator.Services.Scheduler
{
    public static class DateRangeResolver
    {
        public static List<DateTime> ResolveDates(
            ScheduleEntry entry,
            DateTime now,
            LogService log)
        {
            var mode = (entry.Mode ?? "").Trim().ToLowerInvariant();

            return mode switch
            {
                "daily" => ResolveDaily(now, log),
                "hourly" => ResolveHourly(now, log),
                "minutes" => ResolveMinutes(now, log),
                "weekly" => ResolveWeekly(now, log),
                "monthly" => ResolveMonthly(now, log),
                _ => new List<DateTime>()
            };
        }

        private static List<DateTime> ResolveDaily(DateTime now, LogService log)
        {
            var date = now.Date.AddDays(-1);
            return new List<DateTime> { date };
        }

        private static List<DateTime> ResolveHourly(DateTime now, LogService log)
        {
            return new List<DateTime> { now.Date };
        }

        private static List<DateTime> ResolveMinutes(DateTime now, LogService log)
        {
            return new List<DateTime> { now.Date };
        }

        private static List<DateTime> ResolveWeekly(DateTime now, LogService log)
        {
            var end = now.Date.AddDays(-1);      // yesterday
            var start = end.AddDays(-6);         // 7‑day window

            var dates = new List<DateTime>();
            for (var d = start; d <= end; d = d.AddDays(1))
                dates.Add(d);

            return dates;
        }

        private static List<DateTime> ResolveMonthly(DateTime now, LogService log)
        {
            var end = now.Date.AddDays(-1);      // yesterday
            var start = end.AddDays(-29);        // 30‑day window

            var dates = new List<DateTime>();
            for (var d = start; d <= end; d = d.AddDays(1))
                dates.Add(d);

            return dates;
        }
    }
}