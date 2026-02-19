using System.Text.Json;
using AirlogGenerator.Config;
using AirlogGenerator.Models;
using AirlogGenerator.Services;

namespace AirlogGenerator.Services.Scheduler
{
    public class SchedulerService : BackgroundService
    {
        private readonly LogService _log;
        private readonly DatabaseService _db;
        private readonly SystemConfigService _configService;

        private readonly string _stationConfigRoot;
        private readonly JsonSerializerOptions _jsonOptions;

        private readonly Dictionary<string, DateTime> _lastRun = new();

        public SchedulerService(
            LogService log,
            DatabaseService db,
            SystemConfigService configService)
        {
            _log = log;
            _db = db;
            _configService = configService;

            // Use environment variable or fallback to /app/CONFIG/stations for containers
            var configPath = Environment.GetEnvironmentVariable("AG_CONFIG_PATH") ?? "/app/CONFIG";
            
            // If the path doesn't exist and we're not in a container, use relative path
            if (!Directory.Exists(configPath))
            {
                var appRoot = AppContext.BaseDirectory;
                configPath = Path.Combine(appRoot, "CONFIG");
            }
            
            _stationConfigRoot = Path.Combine(configPath, "stations");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.Info("SCHEDULER", "SchedulerService started.");

            bool inContinuousMode = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Load config at the start of each cycle
                    var cfg = _configService.Load();
                    int interval = cfg.SchedulerIntervalMinutes;

                    // Log wake-up tick ONLY when interval > 0
                    if (interval > 0)
                    {
                        _log.Info("SCHEDULER", "Wake-up tick: checking for scheduled tasks...");
                    }

                    // Detect mode transitions (optional but very helpful)
                    if (interval <= 0 && !inContinuousMode)
                    {
                        _log.Info("SCHEDULER", "Entering continuous mode (interval=0). Checking every 1 second.");
                        inContinuousMode = true;
                    }
                    else if (interval > 0 && inContinuousMode)
                    {
                        _log.Info("SCHEDULER", $"Leaving continuous mode. Resuming {interval}-minute interval.");
                        inContinuousMode = false;
                    }

                    // Run the scheduler cycle
                    await RunSchedulerCycle(stoppingToken, inContinuousMode); ;
                }
                catch (Exception ex)
                {
                    _log.Error("SCHEDULER", $"Unhandled scheduler error: {ex.Message}");
                }

                // Load config again for sleep logic (in case user changed it)
                var cfg2 = _configService.Load();
                int interval2 = cfg2.SchedulerIntervalMinutes;

                if (interval2 <= 0)
                {
                    // Continuous mode — check every second
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                else
                {
                    // Normal mode — clamp to safe range
                    interval2 = Math.Clamp(interval2, 1, 120);
                    _log.Debug("SCHEDULER", $"Sleeping for {interval2} minute(s) before next cycle...");
                    await Task.Delay(TimeSpan.FromMinutes(interval2), stoppingToken);
                }
            }
        }

        private async Task RunSchedulerCycle(CancellationToken token, bool continuousMode) 
        {
            _log.Debug("SCHEDULER", "Starting scheduler cycle...");

            if (!Directory.Exists(_stationConfigRoot))
            {
                _log.Error("SCHEDULER", $"Station config folder missing: {_stationConfigRoot}");
                return;
            }

            var stationFiles = Directory.GetFiles(_stationConfigRoot, "*.json");
            if (!continuousMode)
            {
                _log.Info("SCHEDULER", $"Found {stationFiles.Length} station config file(s).");
            }

            foreach (var file in stationFiles)
            {
                if (token.IsCancellationRequested)
                    return;

                _log.Info("SCHEDULER", $"Processing station config: {Path.GetFileName(file)}");
                await ProcessStationConfig(file, token);
            }
        }

        private async Task ProcessStationConfig(string filePath, CancellationToken token)
        {
            try
            {
                _log.Debug("SCHEDULER", $"Reading station config: {filePath}");

                var json = await File.ReadAllTextAsync(filePath, token);
                var cfg = JsonSerializer.Deserialize<StationConfig>(json, _jsonOptions);

                if (cfg == null)
                {
                    _log.Error("SCHEDULER", $"Invalid station config: {filePath}");
                    return;
                }

                if (cfg.Schedule == null || cfg.Schedule.Count == 0)
                {
                    _log.Info("SCHEDULER", $"Station {cfg.StationName} has no schedules. Skipping.");
                    return;
                }

                _log.Info("SCHEDULER", $"Station {cfg.StationName} has {cfg.Schedule.Count} schedule(s).");

                foreach (var (entry, index) in cfg.Schedule.Select((s, i) => (s, i)))
                {
                    if (token.IsCancellationRequested)
                        return;

                    await EvaluateSchedule(cfg, entry, index, token);
                }
            }
            catch (Exception ex)
            {
                _log.Error("SCHEDULER", $"Failed to process station config {filePath}: {ex.Message}");
            }
        }

        private async Task EvaluateSchedule(
            StationConfig station,
            ScheduleEntry entry,
            int index,
            CancellationToken token)
        {
            string key = $"{station.StationName}:{index}";

            _log.Debug("SCHEDULER",
                $"Evaluating schedule #{index} for {station.StationName} (mode={entry.Mode}, interval={entry.Interval})");


            if (entry.Destinations == null || entry.Destinations.Count == 0)
            {
                _log.Error("SCHEDULER",
                    $"Schedule #{index} for {station.StationName} has NO destinations. Skipping.");
                return;
            }

            DateTime now = DateTime.Now;
            DateTime lastRun = _lastRun.ContainsKey(key) ? _lastRun[key] : DateTime.MinValue;

            bool shouldRun = ScheduleEvaluator.ShouldRun(entry, now, lastRun, _log);

            if (!shouldRun)
            {
                _log.Debug("SCHEDULER",
                    $"Schedule #{index} for {station.StationName} does not need to run now.");
                return;
            }

            _log.Info("SCHEDULER",
                $"Running schedule #{index} for station {station.StationName} (mode={entry.Mode})");

            var dates = DateRangeResolver.ResolveDates(entry, now, _log);

            _log.Info("SCHEDULER",
                $"Schedule #{index} resolved to {dates.Count} date(s) for execution.");

            foreach (var date in dates)
            {
                if (token.IsCancellationRequested)
                    return;

                await RunGenerationForDate(station, entry, date, token);
            }

            _lastRun[key] = now;
        }

        // SchedulerService.cs – in RunGenerationForDate
        private async Task RunGenerationForDate(
            StationConfig station,
            ScheduleEntry entry,
            DateTime date,
            CancellationToken token)

        {
            try
            {
                _log.Info("SCHEDULER",
                    $"Generating AIR log for {station.StationName} on {date:yyyy-MM-dd}");

                var queryType = entry.QueryType;
                _log.Info("SCHEDULER",
                    $"Using query type: {queryType} for station {station.StationName}, schedule entry mode={entry.Mode}");

                var strategy = AirLogQueryFactory.GetStrategy(queryType);

                // NEW — strict host requirement
                // NEW — strict host requirement
                if (string.IsNullOrWhiteSpace(station.Host))
                {
                    _log.Error("SCHEDULER",
                        $"Station {station.StationName} has NO host defined in station.json. Scheduled task skipped.");
                    return;
                }

                // NEW — explicit log showing scheduler is using station.json host
                _log.Info("SCHEDULER",
                    $"Connecting to host '{station.Host}' for station '{station.StationName}' (from station.json)");

                // Configure DB connection for this station
                _db.SetConnection(station.Host, "woar_server", "", "");

                var rows = await strategy.ExecuteAsync(_db, station.StationName, date);

                var lines = new List<string>();

                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];

                    var serverTimestamp = row.AirDate.Date.AddMilliseconds(row.AirTimeMs);

                    var corrected = station.EnableTimezoneCalc
                        ? AirLogProcessor.ApplyOffset(serverTimestamp, station.TimezoneOffset)
                        : serverTimestamp;

                    if (!AirLogProcessor.BelongsToBroadcastDay(corrected, date))
                        continue;

                    // Detect rotator pair (row + next row)
                    if (i + 1 < rows.Count && AirLogRowHelpers.IsRotatorPair(row, rows[i + 1]))
                    {
                        // Line 1: playlist entry (rotator cut)
                        lines.Add(AirLogProcessor.FormatAirLine(corrected, row));

                        // Line 2: media asset (actual played cut)
                        lines.Add(AirLogProcessor.FormatAirLine(corrected, rows[i + 1]));

                        i++; // Skip the next row — already processed
                    }
                    else
                    {
                        // Non‑rotator: output only this row
                        lines.Add(AirLogProcessor.FormatAirLine(corrected, row));
                    }
                }

                if (lines.Count == 0)
                {
                    _log.Info("SCHEDULER",
                        $"No entries for {station.StationName} on {date:yyyy-MM-dd}");
                    return;
                }

                foreach (var dest in entry.Destinations)
                {
                    string destPath = Path.IsPathRooted(dest)
                        ? dest
                        : Path.Combine(AppContext.BaseDirectory, dest);

                    Directory.CreateDirectory(destPath);

                    string fileName = date.ToString("yyMMdd") + ".air";
                    string filePath = Path.Combine(destPath, fileName);

                    await File.WriteAllLinesAsync(filePath, lines, token);

                    _log.Success("SCHEDULER",
                        $"Wrote {lines.Count} lines → {filePath}");
                }
            }
            catch (Exception ex)
            {
                _log.Error("SCHEDULER",
                    $"Error generating AIR log for {station.StationName} on {date:yyyy-MM-dd}: {ex.Message}");
            }
        }
    }
}