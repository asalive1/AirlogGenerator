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
        // A rotator is identified by a category+cart pair mismatch between the
        // playlist (container) entry and the media asset (selected cut).
        // Category and cart are both required for identification because the same
        // cart number can exist in different categories.
        // Title differences alone are NOT sufficient — only category+cart uniquely
        // identifies an asset in the automation system.
        public static bool IsRotator(AirLogRow row)
        {
            if (row == null)
                return false;

            // Both sides must have a cart for comparison to be meaningful.
            if (string.IsNullOrWhiteSpace(row.PlaylistCart) ||
                string.IsNullOrWhiteSpace(row.MediaCart))
                return false;

            var playlistPair = $"{row.PlaylistCategory?.Trim()}|{row.PlaylistCart.Trim()}";
            var mediaPair    = $"{row.MediaCategory?.Trim()}|{row.MediaCart.Trim()}";

            return !string.Equals(playlistPair, mediaPair, StringComparison.OrdinalIgnoreCase);
        }
    }


}