namespace AirlogGenerator.Config
{
    public class SystemConfig
    {
        public int WebPort { get; set; } = 8030;

        // Existing field used everywhere today
        public string DefaultServerIp { get; set; } = "127.0.0.1";

        // New AWS-friendly host (RDS endpoint)
        public string? DefaultServerHost { get; set; }

        // Optional database name (if you want to override)
        public string? DefaultDatabaseName { get; set; }

        public int SchedulerIntervalMinutes { get; set; } = 1;

        public LoggingConfig Logging { get; set; } = new LoggingConfig();

        public bool EnableWebUi { get; set; } = true;

        // NEW: AWS / RDS / Secrets Manager fields
        public string? DefaultServerUser { get; set; }
        public string? DefaultServerPassword { get; set; }
        public string? DefaultServerPasswordSecret { get; set; }
        public string? DefaultServerPasswordSecretRegion { get; set; }
    }
}