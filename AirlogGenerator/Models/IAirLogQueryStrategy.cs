// IAirLogQueryStrategy.cs
using AirlogGenerator.Services;

namespace AirlogGenerator.Models
{
    public interface IAirLogQueryStrategy
    {
        Task<List<AirLogRow>> ExecuteAsync(
            DatabaseService db,
            string station,
            DateTime date);
    }
}