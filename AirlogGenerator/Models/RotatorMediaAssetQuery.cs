using AirlogGenerator.Database;
using AirlogGenerator.Services;
using Npgsql;

namespace AirlogGenerator.Models
{
    public class RotatorMediaAssetQuery : IAirLogQueryStrategy
    {
        public async Task<List<AirLogRow>> ExecuteAsync(
            DatabaseService db,
            string station,
            DateTime date)
        {
            var list = new List<AirLogRow>();

            using var conn = new NpgsqlConnection(db.GetType()
                .GetMethod("BuildConnectionString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(db, null)?.ToString());

            await conn.OpenAsync();

            var startDate = date.Date;
            var endDate = date.Date;

            using var cmd = new NpgsqlCommand(SqlQueries.RawAirLogQuery_RotatorPlusCart, conn);
            cmd.Parameters.AddWithValue("station", station);
            cmd.Parameters.AddWithValue("startDate", startDate);
            cmd.Parameters.AddWithValue("endDate", endDate);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var airDate = reader.GetDateTime(0);
                var airTimeMs = reader.GetInt32(1);
                var status = reader.IsDBNull(2) ? "" : reader.GetString(2);

                // Rotator (playlist entry) row
                var rotatorRow = new AirLogRow
                {
                    AirDate = airDate,
                    AirTimeMs = airTimeMs,
                    Status = status,
                    Cart = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Category = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Title = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Artist = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    LengthMs = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                    OriginalScheduledTime = reader.IsDBNull(12) ? 0 : reader.GetInt64(12)
                };

                // Media asset row
                var maidRow = new AirLogRow
                {
                    AirDate = airDate,
                    AirTimeMs = airTimeMs,
                    Status = status,
                    Cart = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    Category = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    Title = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    Artist = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    LengthMs = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                    OriginalScheduledTime = reader.IsDBNull(12) ? 0 : reader.GetInt64(12)
                };

                // Determine if this entry is a true rotator
                bool isRotator =
                    !string.IsNullOrWhiteSpace(rotatorRow.Cart) &&
                    !string.Equals(rotatorRow.Cart, maidRow.Cart, StringComparison.OrdinalIgnoreCase) &&
                    (
                        !string.Equals(rotatorRow.Title, maidRow.Title, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(rotatorRow.Artist, maidRow.Artist, StringComparison.OrdinalIgnoreCase)
                    );

                if (isRotator)
                {
                    // Emit both rows (playlist entry + media asset)
                    list.Add(rotatorRow);
                    list.Add(maidRow);
                }
                else
                {
                    // Non‑rotator → emit only the media asset row
                    list.Add(maidRow);
                }
            }

            return list;
        }
    }
}
