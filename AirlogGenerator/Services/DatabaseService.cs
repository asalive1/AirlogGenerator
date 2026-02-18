using Npgsql;
using AirlogGenerator.Models;
using AirlogGenerator.Database;

namespace AirlogGenerator.Services
{
    public class DatabaseService
    {
        private string _host = "";
        private string _database = "";
        private string _user = "";
        private string _password = "";

        public void SetConnection(string host, string database, string user, string password)
        {
            // Assign the host and database from the UI
            _host = host;
            _database = database;

            // Use your fixed credentials
            _user = "ras";
            _password = "dmarc";
        }

        private string BuildConnectionString()
        {
            return $"Host={_host};Port=5432;Database={_database};Username={_user};Password={_password}";
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var conn = new NpgsqlConnection(BuildConnectionString());
                await conn.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetPostgresVersionAsync()
        {
            using var conn = new NpgsqlConnection(BuildConnectionString());
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand("SELECT version();", conn);
            return (await cmd.ExecuteScalarAsync())?.ToString() ?? "UNKNOWN";
        }

        public async Task<List<StationInfo>> GetStationListWithOffsetsAsync()
        {
            var list = new List<StationInfo>();

            using var conn = new NpgsqlConnection(BuildConnectionString());
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(SqlQueries.GetStationListWithOffset, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new StationInfo
                {
                    StationName = reader.GetString(0),
                    TimezoneOffset = reader.IsDBNull(1) ? 0 : reader.GetDouble(1)
                });
            }

            return list;
        }

        // DatabaseService.cs (only showing the air-log related parts)
        public async Task<List<AirLogRow>> GetRawAirLogRowsAsync(string station, DateTime date)
        {
            // Standard (media asset)
            return await ExecuteAirLogQueryAsync(
                station,
                date,
                SqlQueries.RawAirLogQuery,
                mapStandard: true);
        }

        internal async Task<List<AirLogRow>> GetRotatorRowsAsync(string station, DateTime date)
        {
            return await ExecuteAirLogQueryAsync(
                station,
                date,
                SqlQueries.RawAirLogQuery_Rotator,
                mapStandard: false);
        }

        internal async Task<List<AirLogRow>> GetPartnerIdRowsAsync(string station, DateTime date)
        {
            return await ExecuteAirLogQueryAsync(
                station,
                date,
                SqlQueries.RawAirLogQuery_PartnerId,
                mapStandard: false);
        }

        internal async Task<List<AirLogRow>> GetRotatorPartnerIdRowsAsync(string station, DateTime date)
        {
            return await ExecuteAirLogQueryAsync(
                station,
                date,
                SqlQueries.RawAirLogQuery_RotatorPartnerId,
                mapStandard: false);
        }

        // For Rotator+MAID, we’ll handle mapping directly in the strategy since it returns 2 lines per row.

        private async Task<List<AirLogRow>> ExecuteAirLogQueryAsync(
            string station,
            DateTime date,
            string sql,
            bool mapStandard)
        {
            var list = new List<AirLogRow>();

            using var conn = new NpgsqlConnection(BuildConnectionString());
            await conn.OpenAsync();

            var startDate = date.Date;
            var endDate = date.Date;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("station", station);
            cmd.Parameters.AddWithValue("startDate", startDate);
            cmd.Parameters.AddWithValue("endDate", endDate);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                // Columns 0..8 for standard/rotator/partner queries
                var row = new AirLogRow
                {
                    AirDate = reader.GetDateTime(0),
                    AirTimeMs = reader.GetInt32(1),
                    Status = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Cart = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Category = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Title = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Artist = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    LengthMs = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                    OriginalScheduledTime = reader.IsDBNull(8) ? 0 : reader.GetInt64(8)
                };

                list.Add(row);
            }

            return list;
        }
    }
}