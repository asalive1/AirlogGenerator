// StationConfig.cs
using AirlogGenerator.Models;

namespace AirlogGenerator.Models
{
    using System.Text.Json.Serialization;

    public class StationConfig
    {
        [JsonPropertyOrder(0)]
        public string Host { get; set; } = "";

        [JsonPropertyOrder(1)]
        public string StationName { get; set; } = "";

        [JsonPropertyOrder(2)]
        public double TimezoneOffset { get; set; }

        [JsonPropertyOrder(3)]
        public bool EnableTimezoneCalc { get; set; }

        [JsonPropertyOrder(4)]
        public List<string> Destinations { get; set; } = new();

        // If you have a top-level queryType, include it here:
        // [JsonPropertyOrder(5)]
        // public int QueryType { get; set; }

        [JsonPropertyOrder(10)]
        public List<ScheduleEntry> Schedule { get; set; } = new();
    }

    public class ScheduleEntry
    {
        public string Mode { get; set; } = "daily";
        public int Interval { get; set; } = 1;
        public string? Time { get; set; }
        public string? Day { get; set; }
        public int? DayOfMonth { get; set; }

        public List<string> Destinations { get; set; } = new();

        // NEW: per-schedule query type
        public AirLogQueryType QueryType { get; set; } = AirLogQueryType.Standard;
    }
}