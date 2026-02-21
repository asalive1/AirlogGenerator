namespace AirlogGenerator.Models
{
    public class AirLogRow
    {
        public DateTime AirDate { get; set; }
        public int AirTimeMs { get; set; }
        public string Status { get; set; } = "";

        // Unified "best available" fields
        public string Cart { get; set; } = "";
        public string Category { get; set; } = "";
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";

        public int LengthMs { get; set; }
        public long OriginalScheduledTime { get; set; }

        public string EntryType { get; set; } = "";
        public string EntryDescription { get; set; } = "";
        public string PartnerId { get; set; } = "";

        // Media fields
        public int PlaylistEntryId { get; set; }
        public string MediaCart { get; set; } = "";
        public string MediaCategory { get; set; } = "";
        public string MediaTitle { get; set; } = "";
        public string MediaArtist { get; set; } = "";
        public string MediaClass { get; set; } = "";

        // Playlist fields (rotator only)
        public string PlaylistCart { get; set; } = "";
        public string PlaylistCategory { get; set; } = "";
        public string PlaylistTitle { get; set; } = "";
        public string PlaylistArtist { get; set; } = "";
        public string PlaylistClass { get; set; } = "";
        public string PlaylistOriginalType { get; set; } = "";
        public string PlaylistType { get; set; } = "";
    }
}