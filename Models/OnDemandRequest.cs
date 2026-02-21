namespace AirlogGenerator.Models
{
    public class OnDemandRequest
    {
        public string Station { get; set; } = "";
        public List<string> Dates { get; set; } = new();
        public string Destination { get; set; } = "";
        public List<string> Destinations { get; set; } = new();

        // Accept ANY string from the UI
        public string QueryType { get; set; } = "Standard";
    }
}