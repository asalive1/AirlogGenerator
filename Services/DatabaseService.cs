using AirlogGenerator.Config;
using AirlogGenerator.Database;
using AirlogGenerator.Models;
using Npgsql;

namespace AirlogGenerator.Services
{
    public class DatabaseService
    {
        private string _host = "";
        private string _database = "";
        private string _user = "";
        private string _password = "";

        /// <summary>
        /// Legacy-style connection setup used by /api/db/connect (UI).
        /// If user/password are empty, falls back to ras/dmarc.
        /// </summary>
        public void SetConnection(string host, string database, string user, string password)
        {
            Console.WriteLine("[DB] SetConnection called from UI.");
            Console.WriteLine($"[DB] Host={host}, Database={database}, User={user}");

            _host = host;
            _database = database;

            _user = string.IsNullOrEmpty(user) ? "ras" : user;
            _password = string.IsNullOrEmpty(password) ? "dmarc" : password;

            Console.WriteLine($"[DB] Effective user={_user}, password set={(string.IsNullOrEmpty(_password) ? "NO" : "YES")}");
        }

        /// <summary>
        /// New config-based setup that supports:
        /// - Direct password in system.json
        /// - AWS Secrets Manager (when secret is configured)
        /// - Legacy hardcoded fallback (ras/dmarc)
        /// Used by scheduler / on-demand flows.
        /// </summary>
        public async Task SetConnectionFromConfigAsync(SystemConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            Console.WriteLine("[DB] SetConnectionFromConfigAsync called.");

            // Prefer DefaultServerHost if present, otherwise fall back to DefaultServerIp
            _host = !string.IsNullOrWhiteSpace(config.DefaultServerHost)
                ? config.DefaultServerHost
                : config.DefaultServerIp;

            // Database name: use configured if present, otherwise keep existing or default
            if (!string.IsNullOrWhiteSpace(config.DefaultDatabaseName))
            {
                _database = config.DefaultDatabaseName;
            }
            else if (string.IsNullOrWhiteSpace(_database))
            {
                // UI currently uses "woar_server" hardcoded; keep that as a safe default
                _database = "woar_server";
            }

            Console.WriteLine($"[DB] Host={_host}, Database={_database}");

            // 1. If a direct password is provided → use it (non-AWS / legacy-friendly)
            if (!string.IsNullOrWhiteSpace(config.DefaultServerPassword))
            {
                Console.WriteLine("[DB] Using direct password from system.json.");
                _user = config.DefaultServerUser ?? "ras";
                _password = config.DefaultServerPassword;
                return;
            }

            // 2. If no direct password, but a secret is configured → fetch from AWS
            if (!string.IsNullOrWhiteSpace(config.DefaultServerPasswordSecret))
            {
                Console.WriteLine("[DB] Using AWS Secrets Manager for password.");
                _user = config.DefaultServerUser ?? "ras";
                _password = await SecretHelper.GetSecretAsync(
                    config.DefaultServerPasswordSecret,
                    config.DefaultServerPasswordSecretRegion
                );
                return;
            }

            // 3. Fallback → legacy hardcoded credentials (current behavior)
            Console.WriteLine("[DB] No password or secret configured. Falling back to legacy ras/dmarc.");
            _user = "ras";
            _password = "dmarc";
        }

        private string BuildConnectionString()
        {
            var cs = $"Host={_host};Port=5432;Database={_database};Username={_user};Password={_password}";
            Console.WriteLine($"[DB] Connection string (sanitized): Host={_host}; Database={_database}; User={_user}; Password=***");
            return cs;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var conn = new NpgsqlConnection(BuildConnectionString());
                await conn.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] TestConnectionAsync failed: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetPostgresVersionAsync()
        {
            using var conn = new NpgsqlConnection(BuildConnectionString());
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(SqlQueries.GetPostgresVersion, conn);
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

        public async Task<List<AirLogRow>> GetUnifiedRowsAsync(string station, DateTime date)
        {
            return await ExecuteAirLogQueryAsync(
                station,
                date,
                SqlQueries.RawAirLogQuery_Unified
            );
        }

        private async Task<List<AirLogRow>> ExecuteAirLogQueryAsync(
            string station,
            DateTime date,
            string sql)
        {
            var rows = new List<AirLogRow>();

            await using var conn = new NpgsqlConnection(BuildConnectionString());
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("station", station);
            cmd.Parameters.AddWithValue("date", date.Date);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var idxPlaylist = reader.GetOrdinal("playlist_entry_id");
                int playlistEntryId = reader.IsDBNull(idxPlaylist)
                    ? 0
                    : reader.GetInt32(idxPlaylist);

                var row = new AirLogRow
                {
                    AirDate = reader.GetDateTime(reader.GetOrdinal("air_date")),
                    AirTimeMs = reader.GetInt32(reader.GetOrdinal("air_time")),
                    Status = reader["status"] as string ?? "",

                    Cart = reader["combined_cart"] as string
                               ?? reader["media_cart"] as string
                               ?? reader["playlist_cart"] as string
                               ?? "",

                    Category = reader["media_category"] as string
                               ?? reader["playlist_category"] as string
                               ?? "",

                    Title = reader["media_title"] as string
                            ?? reader["playlist_title"] as string
                            ?? "",

                    Artist = reader["media_artist"] as string
                             ?? reader["playlist_artist"] as string
                             ?? "",

                    LengthMs = reader["length_ms"] is DBNull
                        ? 0
                        : Convert.ToInt32(reader["length_ms"]),

                    OriginalScheduledTime = reader["original_scheduled_time"] is DBNull
                        ? 0
                        : Convert.ToInt64(reader["original_scheduled_time"]),

                    EntryType = reader["entry_type"] as string ?? "",
                    EntryDescription = reader["entry_description"] as string ?? "",
                    PartnerId = reader["partner_id"] as string ?? "",

                    PlaylistCart = reader["playlist_cart"] as string,
                    PlaylistCategory = reader["playlist_category"] as string,
                    PlaylistTitle = reader["playlist_title"] as string,
                    PlaylistArtist = reader["playlist_artist"] as string,
                    PlaylistClass = reader["playlist_class"] as string,
                    PlaylistOriginalType = reader["playlist_original_type"] as string,
                    PlaylistType = reader["playlist_type"] as string,
                    PlaylistEntryId = playlistEntryId,

                    MediaCart = reader["media_cart"] as string,
                    MediaCategory = reader["media_category"] as string,
                    MediaTitle = reader["media_title"] as string,
                    MediaArtist = reader["media_artist"] as string,
                    MediaClass = reader["media_class"] as string
                };

                rows.Add(row);
            }

            // Collapse duplicate EVENT rows, but keep separate occurrences (by time)
            rows = rows
             .GroupBy(r => new
             {
                 r.PlaylistEntryId,
                 r.EntryType,
                 r.EntryDescription,
                 r.AirDate,
                 r.AirTimeMs,
                 r.Status
             })
             .Select(g =>
                 AirLogRowHelpers.IsNonMediaEvent(g.First())
                     ? g.OrderBy(x => x.AirTimeMs).First()
                     : g.First()
             )
             .ToList();

            return rows;
        }
    }
}