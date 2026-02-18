namespace AirlogGenerator.Config
{
    public class LoggingConfig
    {
        public string Level { get; set; } = "INFO";
        public int RetentionDays { get; set; } = 7;
        public int MaxSizeMb { get; set; } = 50;
    }
}