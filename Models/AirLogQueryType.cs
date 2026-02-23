namespace AirlogGenerator.Models
{
    public enum AirLogQueryType
    {
        Standard = 0,
        withPartnerID = 1,
        withRotIDonly = 2,
        withRotMAID = 3,
        withRotandMAID = 4,
        RotatorMAIDpid = 5,
        Custom = 6
    }
}