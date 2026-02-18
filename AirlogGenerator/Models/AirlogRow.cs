namespace AirlogGenerator.Models
{
    public class AirLogRow
    {
        public DateTime AirDate { get; set; }
        public int AirTimeMs { get; set; }
        public string Status { get; set; } = "";
        public string Cart { get; set; } = "";
        public string Category { get; set; } = "";
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public int LengthMs { get; set; }
        public long OriginalScheduledTime { get; set; }
    }
}