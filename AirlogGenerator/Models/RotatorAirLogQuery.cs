using AirlogGenerator.Services;

namespace AirlogGenerator.Models
{
    public class RotatorAirLogQuery : IAirLogQueryStrategy
    {
        public async Task<List<AirLogRow>> ExecuteAsync(
            DatabaseService db,
            string station,
            DateTime date)
        {
            return await db.GetRotatorRowsAsync(station, date);
        }
    }
}
