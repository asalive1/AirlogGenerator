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
                "STARTED" => "on-air",
                "SKIPPED" => "JUMPBY",
                "FAILED" => "Q-FAIL",
                "STOPPED" => "SHORT:",
                "SHORT" => "SHORT:",
                _       => "EVENT:"

            };

            string titleArtist;

            if (AirLogRowHelpers.IsNonMediaEvent(row))
            {
                string friendly = GetEventFriendlyName(row.EntryType);
                titleArtist = FormatTitleArtist(row.EntryDescription, friendly);
            }
            else
            {
                titleArtist = FormatTitleArtist(row.Title, row.Artist);
            }

            string length = TimeSpan.FromMilliseconds(row.LengthMs)
                .ToString(@"mm\:ss");

            string original = TimeSpan
                .FromMilliseconds(row.OriginalScheduledTime)
                .ToString(@"hh\:mm\:ss");

            const string HARD_CODED_FIELD = "00000|-|00000|00";

            return $"{dateStr},{timeStr},{status},{row.Cart},{row.Category}," +
                   $"{titleArtist},{length},{original},{HARD_CODED_FIELD}";
        }

        private static string FormatTitleArtist(string title, string artist)
        {
            title ??= "";
            artist ??= "";

            string t = title.Length > 23 ? title[..23] : title;
            string a = artist.Length > 25 ? artist[..25] : artist;

            string raw = $"{t.PadRight(23)}  {a.PadRight(25)}";
            string padded = raw.PadRight(50);

            return $"\"{padded}\"";
        }

        public static string GetEventFriendlyName(string entryType)
        {
            if (string.IsNullOrWhiteSpace(entryType))
                return "";

            if (entryType.Contains("WorkflowEntry", StringComparison.OrdinalIgnoreCase))
                return "WORKFLOW";

            if (entryType.Contains("MemoEntry", StringComparison.OrdinalIgnoreCase))
                return "MEMO";

            if (entryType.Contains("SegmentRulesetEntry", StringComparison.OrdinalIgnoreCase))
                return "RULESET";

            if (entryType.Contains("LiveCopyEntry", StringComparison.OrdinalIgnoreCase))
                return "LIVECOPY";

            if (entryType.Contains("ManualEntry", StringComparison.OrdinalIgnoreCase))
                return "Manual";

            return entryType;
        }

        public static string FormatNonMediaLine(DateTime corrected, AirLogRow row)
        {
            var status = "EVENT:";
            var cart = "------";
            var category = "---";

            var title = row.EntryDescription ?? "";

            var rawType = row.EntryType ?? "";
            var artist = rawType.EndsWith("Entry", StringComparison.OrdinalIgnoreCase)
                ? rawType[..^5]
                : rawType;

            var lengthDisplay = "00:00";

            var datePart = corrected.ToString("yy-MM-dd");
            var timePart = corrected.ToString("HH:mm:ss");

            var scheduled = "        ";
            var sequence = "00000|-|00000|00";

            var description = FormatTitleArtist(title, artist);

            return $"{datePart},{timePart},{status},{cart},{category},{description},{lengthDisplay},{scheduled},{sequence}";
        }

        /// <summary>
        /// Build a synthetic SHORT line from a start row and a stop row.
        /// </summary>
        public static AirLogRow BuildShortRow(AirLogRow startRow, AirLogRow stopRow)
        {
            var actualLengthMs = stopRow.AirTimeMs - startRow.AirTimeMs;
            if (actualLengthMs < 0)
                actualLengthMs = 0;

            return new AirLogRow
            {
                AirDate = stopRow.AirDate,
                AirTimeMs = stopRow.AirTimeMs,
                Status = "SHORT",

                Cart = startRow.Cart,
                Category = startRow.Category,
                Title = startRow.Title,
                Artist = startRow.Artist,

                LengthMs = actualLengthMs,
                OriginalScheduledTime = startRow.OriginalScheduledTime,

                EntryType = startRow.EntryType,
                EntryDescription = startRow.EntryDescription,
                PartnerId = startRow.PartnerId,

                PlaylistCart = startRow.PlaylistCart,
                PlaylistCategory = startRow.PlaylistCategory,
                PlaylistTitle = startRow.PlaylistTitle,
                PlaylistArtist = startRow.PlaylistArtist,
                PlaylistClass = startRow.PlaylistClass,
                PlaylistOriginalType = startRow.PlaylistOriginalType,
                PlaylistType = startRow.PlaylistType,

                MediaCart = startRow.MediaCart,
                MediaCategory = startRow.MediaCategory,
                MediaTitle = startRow.MediaTitle,
                MediaArtist = startRow.MediaArtist,
                MediaClass = startRow.MediaClass
            };
        }
    }
}