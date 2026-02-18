using AirlogGenerator.Models;

namespace AirlogGenerator.Services
{
    public static class AirLogProcessor
    {
        public static DateTime ApplyOffset(DateTime serverTimestamp, double offsetHours)
        {
            return serverTimestamp.AddHours(offsetHours);
        }

        public static bool BelongsToBroadcastDay(DateTime corrected, DateTime targetDate)
        {
            return corrected >= targetDate.Date &&
                   corrected < targetDate.Date.AddDays(1);
        }

        public static string FormatAirLine(
            DateTime corrected,
            AirLogRow row)
        {
            string dateStr = corrected.ToString("yy-MM-dd");
            string timeStr = corrected.ToString("HH:mm:ss");

            string status = row.Status switch
            {
                "COMPLETED" => "on-air",
                "SKIPPED" => "JUMPBY",
                "FAILED" => "QFAIL",
                "STOPPED" => "STOPPED",
                _ => "on-air"
            };

            string title = (row.Title ?? "").PadRight(24);
            string artist = (row.Artist ?? "").PadRight(24);

            string length = TimeSpan.FromMilliseconds(row.LengthMs)
                .ToString(@"mm\:ss");

            string original = DateTimeOffset
                .FromUnixTimeSeconds(row.OriginalScheduledTime)
                .ToLocalTime()
                .ToString("HH:mm:ss");

            const string HARD_CODED_FIELD = "00000|-|00000|00";

            return $"{dateStr},{timeStr},{status},{row.Cart},{row.Category}," +
                   $"\"{title}{artist}\",{length},{original},{HARD_CODED_FIELD}";
        }
    }
}