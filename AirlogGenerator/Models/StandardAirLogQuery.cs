using AirlogGenerator.Services;

namespace AirlogGenerator.Models
{
    public class StandardAirLogQuery : IAirLogQueryStrategy
    {
        public async Task<List<AirLogRow>> ExecuteAsync(
            DatabaseService db,
            string station,
            DateTime date)
        {
            return await db.GetRawAirLogRowsAsync(station, date);
        }
    }
}
