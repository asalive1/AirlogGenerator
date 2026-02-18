using AirlogGenerator.Services;

namespace AirlogGenerator.Models
{
    public class RotatorPartnerIdQuery : IAirLogQueryStrategy
    {
        public async Task<List<AirLogRow>> ExecuteAsync(
            DatabaseService db,
            string station,
            DateTime date)
        {
            return await db.GetRotatorPartnerIdRowsAsync(station, date);
        }
    }
}
