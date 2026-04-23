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

        // Shared abbreviation CASE expression
        private const string TypeAbbrev = @"
            CASE pe.type
                WHEN 'VOICE_OVER'     THEN 'VO'
                WHEN 'DIGITAL_AUDIO'  THEN 'DA'
                WHEN 'VOCAL_PROTECT'  THEN 'VP'
                WHEN 'DOUBLE_START'   THEN 'DS'
                WHEN 'VOICE_TRACK'    THEN 'VT'
                ELSE 'DA'
            END
        ";

        // ============================================================
        // STANDARD QUERY (media asset)
        // ============================================================
        public const string RawAirLogQuery_Unified = @"
WITH first_start AS (
    SELECT
        ale2.playlist_entry_id,
        MIN(ale2.air_time) AS first_air_time
    FROM air_log_entry ale2
    WHERE ale2.radio_station_name = @station
      AND ale2.air_date = @date
      AND ale2.status IN ('STARTED', 'COMPLETED')
    GROUP BY ale2.playlist_entry_id
),
first_workflow AS (
    SELECT
        ale3.playlist_entry_id,
        MIN(ale3.air_time) AS first_air_time
    FROM air_log_entry ale3
    WHERE ale3.radio_station_name = @station
      AND ale3.air_date = @date
      AND ale3.playlist_entry_class = 'WorkflowEntry'
    GROUP BY ale3.playlist_entry_id
)
SELECT
    ale.air_date,
    ale.air_time AS air_time,
    ale.status,
    ale.playlist_entry_id,

    -- Combined cart (media preferred)
    (CASE pe.type
        WHEN 'VOICE_OVER'     THEN 'VO'
        WHEN 'DIGITAL_AUDIO'  THEN 'DA'
        WHEN 'VOCAL_PROTECT'  THEN 'VP'
        WHEN 'DOUBLE_START'   THEN 'DS'
        WHEN 'VOICE_TRACK'    THEN 'VT'
        ELSE 'DA'
     END) || COALESCE(ale.media_asset_cart_name, ale.playlist_entry_cart_name, '') AS combined_cart,

    -- Media fields
    ale.media_asset_cart_name        AS media_cart,
    ale.media_asset_category_name    AS media_category,
    ale.media_asset_title            AS media_title,
    ale.media_asset_artist           AS media_artist,
    ale.media_asset_class            AS media_class,

    -- ⭐ ALWAYS RETURN PLAYLIST FIELDS WHEN THEY EXIST
    ale.playlist_entry_cart_name     AS playlist_cart,
    ale.playlist_entry_category_name AS playlist_category,
    ale.playlist_entry_title         AS playlist_title,
    ale.playlist_entry_artist        AS playlist_artist,
    ale.playlist_entry_class         AS playlist_class,
    ale.playlist_entry_original_type AS playlist_original_type,
    pe.type                          AS playlist_type,

    ale.media_asset_length           AS length_ms,

    -- Scheduled time from playlist_entry
    pe.time                          AS original_scheduled_time,

    pe.cart_name                     AS pe_cart_name,
    pe.category_name                 AS pe_category_name,
    pe.title                         AS pe_title,
    pe.artist                        AS pe_artist,
    pe.length                        AS pe_length,

    ale.playlist_entry_class         AS entry_type,
    ale.playlist_entry_title         AS entry_description,
    ale.playlist_entry_partner_id    AS partner_id

FROM air_log_entry ale
LEFT JOIN first_start fs
    ON fs.playlist_entry_id = ale.playlist_entry_id
LEFT JOIN playlist_entry pe
    ON ale.playlist_entry_id = pe.playlist_entry_id

WHERE ale.radio_station_name = @station
  AND ale.air_date = @date
  AND (

        ----------------------------------------------------------------------
        -- MEDIA ROWS
        ----------------------------------------------------------------------
        (
            -- Include both start and terminal rows; formatter pairs them so
            -- on-air can publish start time while preserving played duration.
            ale.playlist_entry_class = 'PlayableEntry'
            AND ale.status IN ('STARTED', 'COMPLETED', 'SKIPPED', 'FAILED', 'STOPPED')
        )

        ----------------------------------------------------------------------
        -- WORKFLOW / MEMO / MANUAL (non-media)
        ----------------------------------------------------------------------

        OR
        (
            ale.playlist_entry_class IN ('WorkflowEntry', 'MemoEntry', 'ManualEntry')
            AND ale.status = 'STARTED'
            AND COALESCE(ale.playlist_entry_title, '') <> ''
        )

        -- LIVECOPYENTRY (its own rule)
        OR
        (
            ale.playlist_entry_class = 'LiveCopyEntry'
            -- include STARTED or COMPLETED and accept entries that have either a title or a cart name
            AND ale.status IN ('COMPLETED', 'STARTED')
            AND COALESCE(ale.playlist_entry_title, ale.playlist_entry_cart_name, ale.media_asset_cart_name, '') <> ''
        )

        ----------------------------------------------------------------------
        -- SEGMENT RULESET (only STARTED row)
        ----------------------------------------------------------------------
        OR
        (
            ale.playlist_entry_class = 'SegmentRulesetEntry'
            AND ale.status = 'STARTED'
        )

    )  -- end giant AND block

ORDER BY ale.air_date, air_time;
";
        // ============================================================
        // ROTATOR QUERY (playlist fields)
        // ============================================================
        public const string RawAirLogQuery_Rotator = @"
    SELECT
        ale.air_date,
        ale.air_time,
        ale.status,
        (" + TypeAbbrev + @") || COALESCE(ale.playlist_entry_cart_name, '') AS combined_cart,
        ale.playlist_entry_category_name,
        ale.playlist_entry_title,
        ale.playlist_entry_artist,
        ale.media_asset_length,
        pe.original_scheduled_time,
        ale.playlist_entry_class,
        ale.playlist_entry_title
    FROM air_log_entry AS ale
    LEFT JOIN playlist_entry AS pe
        ON ale.playlist_entry_id = pe.playlist_entry_id
    WHERE ale.radio_station_name = @station
      AND ale.air_date BETWEEN @startDate AND @endDate
      AND ale.status IN ('COMPLETED', 'SKIPPED', 'FAILED', 'STOPPED')
    ORDER BY ale.air_date, ale.air_time;
";

        // ============================================================
        // PARTNER ID QUERY (Partner ID replaces Artist)
        // ============================================================
        public const string RawAirLogQuery_PartnerId = @"
    SELECT
        ale.air_date,
        ale.air_time,
        ale.status,
        (" + TypeAbbrev + @") || COALESCE(ale.media_asset_cart_name, '') AS combined_cart,
        ale.media_asset_category_name,
        ale.media_asset_title,
        ale.playlist_entry_partner_id,
        ale.media_asset_length,
        pe.original_scheduled_time,
        ale.playlist_entry_class,
        ale.playlist_entry_title
    FROM air_log_entry AS ale
    LEFT JOIN playlist_entry AS pe
        ON ale.playlist_entry_id = pe.playlist_entry_id
    WHERE ale.radio_station_name = @station
      AND ale.air_date BETWEEN @startDate AND @endDate
      AND ale.status IN ('COMPLETED', 'SKIPPED', 'FAILED', 'STOPPED')
    ORDER BY ale.air_date, ale.air_time;
";

        // ============================================================
        // ROTATOR + PARTNER ID
        // ============================================================
        public const string RawAirLogQuery_RotatorPartnerId = @"
    SELECT
        ale.air_date,
        ale.air_time,
        ale.status,
        (" + TypeAbbrev + @") || COALESCE(ale.playlist_entry_cart_name, '') AS combined_cart,
        ale.playlist_entry_category_name,
        ale.playlist_entry_title,
        ale.playlist_entry_partner_id,
        ale.media_asset_length,
        pe.original_scheduled_time,
        ale.playlist_entry_class,
        ale.playlist_entry_title
    FROM air_log_entry AS ale
    LEFT JOIN playlist_entry AS pe
        ON ale.playlist_entry_id = pe.playlist_entry_id
    WHERE ale.radio_station_name = @station
      AND ale.air_date BETWEEN @startDate AND @endDate
      AND ale.status IN ('COMPLETED', 'SKIPPED', 'FAILED', 'STOPPED')
    ORDER BY ale.air_date, ale.air_time;
";

        // ============================================================
        // ROTATOR + MEDIA ASSET (two-row expansion)
        // ============================================================
        public const string RawAirLogQuery_RotatorPlusCart = @"
    SELECT
        ale.air_date,
        ale.air_time,
        ale.status,
        (" + TypeAbbrev + @") || COALESCE(ale.playlist_entry_cart_name, '') AS rotator_cart,
        ale.playlist_entry_category_name,
        ale.playlist_entry_title,
        ale.playlist_entry_artist,
        (" + TypeAbbrev + @") || COALESCE(ale.media_asset_cart_name, '') AS media_asset_cart,
        ale.media_asset_category_name,
        ale.media_asset_title,
        ale.media_asset_artist,
        ale.media_asset_length,
        pe.original_scheduled_time,
        ale.playlist_entry_class,
        ale.playlist_entry_title
    FROM air_log_entry AS ale
    LEFT JOIN playlist_entry AS pe
        ON ale.playlist_entry_id = pe.playlist_entry_id
    WHERE ale.radio_station_name = @station
      AND ale.air_date BETWEEN @startDate AND @endDate
      AND ale.status IN ('COMPLETED', 'SKIPPED', 'FAILED', 'STOPPED')
    ORDER BY ale.air_date, ale.air_time;
";
    }
}