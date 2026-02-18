using AirlogGenerator.Services;

namespace AirlogGenerator.Models
{
    public class PartnerIdAirLogQuery : IAirLogQueryStrategy
    {
        public async Task<List<AirLogRow>> ExecuteAsync(
            DatabaseService db,
            string station,
            DateTime date)
        {
            return await db.GetPartnerIdRowsAsync(station, date);
        }
    }
}
