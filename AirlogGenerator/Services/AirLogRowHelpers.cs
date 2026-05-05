namespace AirlogGenerator.Models
{
    public static class AirLogRowHelpers
    {
        public static bool IsNonMediaEvent(AirLogRow row)
        {
            if (row == null)
                return false;

            if (string.IsNullOrWhiteSpace(row.EntryType))
                return false;

            var type = row.EntryType;

            return type.Contains("WorkflowEntry", StringComparison.OrdinalIgnoreCase)
                || type.Contains("MemoEntry", StringComparison.OrdinalIgnoreCase)
                || type.Contains("SegmentRulesetEntry", StringComparison.OrdinalIgnoreCase)
                || type.Contains("LiveCopyEntry", StringComparison.OrdinalIgnoreCase)
                || type.Contains("ManualEntry", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetTypeAbbreviation(string playlistType)
        {
            if (string.IsNullOrWhiteSpace(playlistType))
                return "DA";

            return playlistType.ToUpperInvariant() switch
            {
                "VOICE_OVER" => "VO",
                "DIGITAL_AUDIO" => "DA",
                "VOCAL_PROTECT" => "VP",
                "DOUBLE_START" => "DS",
                "VOICE_TRACK" => "VT",
                _ => "DA"
            };
        }

        // ROTATOR RULE:
        // 1) playlist_cart != media_cart -> rotator
        // 2) Some traffic providers reuse the same cart for shell/cut, but with
        //    different titles. Treat those as rotators as well.
        public static bool IsRotator(AirLogRow row)
        {
            if (row == null)
                return false;

            if (!string.IsNullOrWhiteSpace(row.PlaylistCart) &&
                !string.IsNullOrWhiteSpace(row.MediaCart) &&
                !string.Equals(row.PlaylistCart, row.MediaCart, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Conservative same-cart rotator detection to avoid over-duplicating.
            if (!string.IsNullOrWhiteSpace(row.PlaylistCart) &&
                !string.IsNullOrWhiteSpace(row.MediaCart) &&
                string.Equals(row.PlaylistCart, row.MediaCart, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(row.PlaylistTitle) &&
                !string.IsNullOrWhiteSpace(row.MediaTitle) &&
                !string.Equals(row.PlaylistTitle.Trim(), row.MediaTitle.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }


}