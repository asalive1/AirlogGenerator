namespace AirlogGenerator.Config
{
    public class SystemConfig
    {
        public bool EnableWebUi { get; set; } = true;
        public int WebPort { get; set; } = 8030;

        public string DefaultServerIp { get; set; } = "192.168.50.150";

        public int SchedulerIntervalMinutes { get; set; } = 1;

        public LoggingConfig Logging { get; set; } = new();
    }
}