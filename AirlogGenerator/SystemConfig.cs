namespace AirlogGenerator.Config
{
    public class SystemConfig
    {
        public bool EnableWebUi { get; set; } = true;
        public int WebPort { get; set; } = 8030;

        public string DefaultServerIp { get; set; } = "192.168.50.150";
        public string DefaultServerHost { get; set; } = "";
        public string DefaultDatabaseName { get; set; } = "woar_server";
        public string DefaultServerUser { get; set; } = "";
        public string DefaultServerPassword { get; set; } = "";
        public string DefaultServerPasswordSecret { get; set; } = "";
        public string DefaultServerPasswordSecretRegion { get; set; } = "";

        public int SchedulerIntervalMinutes { get; set; } = 1;

        public LoggingConfig Logging { get; set; } = new();
    }
}