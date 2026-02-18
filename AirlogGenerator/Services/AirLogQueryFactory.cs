// AirLogQueryFactory.cs
using AirlogGenerator.Models;

namespace AirlogGenerator.Services
{
    public static class AirLogQueryFactory
    {
        public static IAirLogQueryStrategy GetStrategy(AirLogQueryType type)
        {
            return type switch
            {
                AirLogQueryType.Standard => new StandardAirLogQuery(),
                AirLogQueryType.withPartnerID => new PartnerIdAirLogQuery(),
                AirLogQueryType.withRotIDonly => new RotatorAirLogQuery(),
                AirLogQueryType.withRotMAID => new RotatorMediaAssetQuery(),
                AirLogQueryType.withRotandMAID => new RotatorPartnerIdQuery(),
                AirLogQueryType.Custom => new StandardAirLogQuery(), // placeholder for future

                _ => new StandardAirLogQuery()
            };
        }
    }
}