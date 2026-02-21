namespace AirlogGenerator.Models
{
    public class DbConnectRequest
    {
        public string Host { get; set; } = "";
        public string Database { get; set; } = "";
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
public class StationInfo
{
    public string StationName { get; set; } = "";
    public double TimezoneOffset { get; set; } = 0;
}