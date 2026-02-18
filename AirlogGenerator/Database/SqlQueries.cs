// SqlQueries.cs
namespace AirlogGenerator.Database
{
    public static class SqlQueries
    {
        public const string GetPostgresVersion = "SELECT version();";

        public const string GetStationList = @"
        SELECT station_name 
        FROM public.radio_station
        ORDER BY radio_station_id ASC;
    ";

        public const string GetStationListWithOffset = @"
        SELECT DISTINCT
            r.station_name,
            rss.timezone_offset / 3600000.0 AS timezone_offset_hours
        FROM radio_station_scheduler AS rss
        JOIN radio_station_workstation AS rsw
            ON rss.radio_station_workstation_id = rsw.radio_station_workstation_id
        JOIN radio_station AS r
            ON rsw.radio_station_id = r.radio_station_id
        ORDER BY
            r.station_name;
    ";

        // Standard (media asset)
        public const string RawAirLogQuery = @"
    SELECT
        air_date,
        air_time,
        status,
        media_asset_cart_name,
        media_asset_category_name,
        media_asset_title,
        media_asset_artist,
        media_asset_length,
        original_scheduled_time
    FROM air_log_entry
    WHERE radio_station_name = @station
      AND air_date BETWEEN @startDate AND @endDate
      AND status IN ('COMPLETED', 'SKIPPED', 'FAILED', 'STOPPED')
    ORDER BY air_date, air_time;
";

        // Rotator (playlist fields)
        public const string RawAirLogQuery_Rotator = @"
    SELECT
        air_date,
        air_time,
        status,
        playlist_entry_cart_name,
        playlist_entry_category_name,
        playlist_entry_title,
        playlist_entry_artist,
        media_asset_length,
        original_scheduled_time
    FROM air_log_entry
    WHERE radio_station_name = @station
      AND air_date BETWEEN @startDate AND @endDate
      AND status IN ('COMPLETED', 'SKIPPED', 'FAILED', 'STOPPED')
    ORDER BY air_date, air_time;
";

        // PartnerID (Partner ID replaces Artist)
        public const string RawAirLogQuery_PartnerId = @"
    SELECT
        air_date,
        air_time,
        status,
        media_asset_cart_name,
        media_asset_category_name,
        media_asset_title,
        playlist_entry_partner_id,
        media_asset_length,
        original_scheduled_time
    FROM air_log_entry
    WHERE radio_station_name = @station
      AND air_date BETWEEN @startDate AND @endDate
      AND status IN ('COMPLETED', 'SKIPPED', 'FAILED', 'STOPPED')
    ORDER BY air_date, air_time;
";

        // Rotator + PartnerID (rotator fields + partner id as Artist)
        public const string RawAirLogQuery_RotatorPartnerId = @"
    SELECT
        air_date,
        air_time,
        status,
        playlist_entry_cart_name,
        playlist_entry_category_name,
        playlist_entry_title,
        playlist_entry_partner_id,
        media_asset_length,
        original_scheduled_time
    FROM air_log_entry
    WHERE radio_station_name = @station
      AND air_date BETWEEN @startDate AND @endDate
      AND status IN ('COMPLETED', 'SKIPPED', 'FAILED', 'STOPPED')
    ORDER BY air_date, air_time;
";

        // Rotator + Cart (Rotator + Media Asset)
        // We'll use this same shape as Rotator and then generate a second line from media_asset_* in the strategy
        public const string RawAirLogQuery_RotatorPlusCart = @"
    SELECT
        air_date,
        air_time,
        status,
        playlist_entry_cart_name,
        playlist_entry_category_name,
        playlist_entry_title,
        playlist_entry_artist,
        media_asset_cart_name,
        media_asset_category_name,
        media_asset_title,
        media_asset_artist,
        media_asset_length,
        original_scheduled_time
    FROM air_log_entry
    WHERE radio_station_name = @station
      AND air_date BETWEEN @startDate AND @endDate
      AND status IN ('COMPLETED', 'SKIPPED', 'FAILED', 'STOPPED')
    ORDER BY air_date, air_time;
";
    }
}