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
        // If both playlist and media metadata exist and differ, treat as rotator.
        // Some traffic providers reuse the same cart for shell+cut, so cart mismatch
        // alone is not enough.
        public static bool IsRotator(AirLogRow row)
        {
            if (row == null)
                return false;

            var hasPlaylistData =
                !string.IsNullOrWhiteSpace(row.PlaylistCart) ||
                !string.IsNullOrWhiteSpace(row.PlaylistTitle) ||
                !string.IsNullOrWhiteSpace(row.PlaylistArtist) ||
                !string.IsNullOrWhiteSpace(row.PlaylistCategory);

            var hasMediaData =
                !string.IsNullOrWhiteSpace(row.MediaCart) ||
                !string.IsNullOrWhiteSpace(row.MediaTitle) ||
                !string.IsNullOrWhiteSpace(row.MediaArtist) ||
                !string.IsNullOrWhiteSpace(row.MediaCategory);

            if (!hasPlaylistData || !hasMediaData)
                return false;

            static bool Diff(string? a, string? b) =>
                !string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);

            return Diff(row.PlaylistCart, row.MediaCart)
                || Diff(row.PlaylistTitle, row.MediaTitle)
                || Diff(row.PlaylistArtist, row.MediaArtist)
                || Diff(row.PlaylistCategory, row.MediaCategory);
        }
    }


}