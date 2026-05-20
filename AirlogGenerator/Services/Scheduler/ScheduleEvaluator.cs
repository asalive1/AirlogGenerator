using AirlogGenerator.Models;


namespace AirlogGenerator.Services.Scheduler
{
    public static class ScheduleEvaluator
    {
        public static bool ShouldRun(
            ScheduleEntry entry,
            DateTime now,
            DateTime lastRun,
            LogService log)
        {
            var mode = (entry.Mode ?? "").Trim().ToLowerInvariant();

            if (mode != "daily" &&
                mode != "hourly" &&
                mode != "minutes" &&
                mode != "weekly" &&
                mode != "monthly")
            {
                log.Error("SCHEDULER",
                    $"Unknown schedule mode '{entry.Mode}'. Skipping.");
                return false;
            }

            return mode switch
            {
                "daily" => ShouldRunDaily(entry, now, lastRun, log),
                "hourly" => ShouldRunHourly(entry, now, lastRun, log),
                "minutes" => ShouldRunMinutes(entry, now, lastRun, log),
                "weekly" => ShouldRunWeekly(entry, now, lastRun, log),
                "monthly" => ShouldRunMonthly(entry, now, lastRun, log),
                _ => false
            };
        }

        private static bool IsTimeMatch(string? timeString, DateTime now, LogService log)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return true;

            if (!TimeSpan.TryParse(timeString, out var target))
            {
                log.Error("SCHEDULER", $"Invalid time format '{timeString}'. Expected HH:mm.");
                return false;
            }

            var targetTime = now.Date.Add(target);
            var tolerance = TimeSpan.FromMinutes(2);
            return now >= targetTime - tolerance && now <= targetTime + tolerance;
        }

        private static bool ShouldRunDaily(
            ScheduleEntry entry,
            DateTime now,
            DateTime lastRun,
            LogService log)
        {
            int interval = Math.Max(entry.Interval, 1);

            if (!IsTimeMatch(entry.Time, now, log))
                return false;

            if (lastRun == DateTime.MinValue)
                return true;

            var daysSince = (now.Date - lastRun.Date).TotalDays;
            return daysSince >= interval;
        }

        private static bool ShouldRunHourly(
            ScheduleEntry entry,
            DateTime now,
            DateTime lastRun,
            LogService log)
        {
            int interval = Math.Max(entry.Interval, 1);

            if (lastRun == DateTime.MinValue)
                return true;

            var hoursSince = (now - lastRun).TotalHours;
            return hoursSince >= interval;
        }

        private static bool ShouldRunMinutes(
            ScheduleEntry entry,
            DateTime now,
            DateTime lastRun,
            LogService log)
        {
            int interval = Math.Max(entry.Interval, 1);

            if (lastRun == DateTime.MinValue)
                return true;

            var minutesSince = (now - lastRun).TotalMinutes;
            return minutesSince >= interval;
        }

        private static bool ShouldRunWeekly(
            ScheduleEntry entry,
            DateTime now,
            DateTime lastRun,
            LogService log)
        {
            int interval = Math.Max(entry.Interval, 1);

            if (!string.IsNullOrWhiteSpace(entry.Day))
            {
                if (!IsTimeMatch(entry.Time, now, log))
                    return false;

                var normalized = NormalizeDay(entry.Day);
                if (normalized == null)
                    return false;

                if (now.DayOfWeek != normalized.Value)
                    return false;

                if (lastRun == DateTime.MinValue)
                    return true;

                var weeksSince = (now.Date - lastRun.Date).TotalDays / 7.0;
                return weeksSince >= interval;
            }

            if (lastRun == DateTime.MinValue)
                return true;

            if (!IsTimeMatch(entry.Time, now, log))
                return false;

            var weeks = (now - lastRun).TotalDays / 7.0;
            return weeks >= interval;
        }

        private static bool ShouldRunMonthly(
            ScheduleEntry entry,
            DateTime now,
            DateTime lastRun,
            LogService log)
        {
            int interval = Math.Max(entry.Interval, 1);

            if (entry.DayOfMonth.HasValue)
            {
                if (!IsTimeMatch(entry.Time, now, log))
                    return false;

                if (now.Day != entry.DayOfMonth.Value)
                    return false;

                if (lastRun == DateTime.MinValue)
                    return true;

                var monthsSince = MonthsBetween(lastRun.Date, now.Date);
                return monthsSince >= interval;
            }

            if (lastRun == DateTime.MinValue)
                return true;

            if (!IsTimeMatch(entry.Time, now, log))
                return false;

            var months = MonthsBetween(lastRun.Date, now.Date);
            return months >= interval;
        }

        private static DayOfWeek? NormalizeDay(string day)
        {
            return day.Trim().ToLowerInvariant() switch
            {
                "mon" or "monday" => DayOfWeek.Monday,
                "tue" or "tuesday" => DayOfWeek.Tuesday,
                "wed" or "wednesday" => DayOfWeek.Wednesday,
                "thu" or "thursday" => DayOfWeek.Thursday,
                "fri" or "friday" => DayOfWeek.Friday,
                "sat" or "saturday" => DayOfWeek.Saturday,
                "sun" or "sunday" => DayOfWeek.Sunday,
                _ => null
            };
        }

        private static int MonthsBetween(DateTime earlier, DateTime later)
        {
            int months = (later.Year - earlier.Year) * 12 + (later.Month - earlier.Month);
            if (later.Day < earlier.Day)
                months--;
            return months;
        }
    };
}