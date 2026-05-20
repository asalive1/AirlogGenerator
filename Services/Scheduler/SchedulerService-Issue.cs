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
        private readonly AirLogFormattingService _formatter;

        private readonly string _stationConfigRoot;
        private readonly JsonSerializerOptions _jsonOptions;

        private readonly Dictionary<string, DateTime> _lastRun = new();
        private readonly Dictionary<string, string> _scheduleSignatures = new();   // ⭐ NEW

        public SchedulerService(
            LogService log,
            DatabaseService db,
            SystemConfigService configService,
            AirLogFormattingService formatter)
        {
            _log = log;
            _db = db;
            _configService = configService;
            _formatter = formatter;

            _stationConfigRoot = Path.Combine(AppContext.BaseDirectory, "CONFIG", "stations");

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
                    var cfg = _configService.Load();
                    int interval = cfg.SchedulerIntervalMinutes;

                    if (interval > 0)
                        _log.Info("SCHEDULER", "Wake-up tick: checking for scheduled tasks...");

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

                    await RunSchedulerCycle(stoppingToken, inContinuousMode);
                }
                catch (Exception ex)
                {
                    _log.Error("SCHEDULER", $"Unhandled scheduler error: {ex.Message}");
                }

                var cfg2 = _configService.Load();
                int interval2 = cfg2.SchedulerIntervalMinutes;

                if (interval2 <= 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                else
                {
                    interval2 = Math.Clamp(interval2, 1, 120);
                    await Task.Delay(TimeSpan.FromMinutes(interval2), stoppingToken);
                }
            }
        }

        private async Task RunSchedulerCycle(CancellationToken token, bool continuousMode)
        {
            if (!Directory.Exists(_stationConfigRoot))
            {
                _log.Error("SCHEDULER", $"Station config folder missing: {_stationConfigRoot}");
                return;
            }

            var stationFiles = Directory.GetFiles(_stationConfigRoot, "*.json");

            foreach (var file in stationFiles)
            {
                if (token.IsCancellationRequested)
                    return;

                await ProcessStationConfig(file, token);
            }
        }

        private async Task ProcessStationConfig(string filePath, CancellationToken token)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath, token);
                var cfg = JsonSerializer.Deserialize<StationConfig>(json, _jsonOptions);

                if (cfg == null)
                {
                    _log.Error("SCHEDULER", $"Invalid station config: {filePath}");
                    return;
                }

                if (cfg.Schedule == null || cfg.Schedule.Count == 0)
                    return;

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

            // ⭐ NEW: compute signature
            string signature = JsonSerializer.Serialize(entry);

            // ⭐ NEW: detect schedule changes
            if (!_scheduleSignatures.ContainsKey(key) ||
                _scheduleSignatures[key] != signature)
            {
                _scheduleSignatures[key] = signature;
                _lastRun[key] = DateTime.MinValue;

                _log.Info("SCHEDULER",
                    $"Schedule changed for {station.StationName} entry {index}. Resetting last-run.");
            }

            DateTime now = DateTime.Now;
            DateTime lastRun = _lastRun.ContainsKey(key) ? _lastRun[key] : DateTime.MinValue;

            bool shouldRun = ScheduleEvaluator.ShouldRun(entry, now, lastRun, _log);

            if (!shouldRun)
                return;

            var dates = DateRangeResolver.ResolveDates(entry, now, _log);

            foreach (var date in dates)
            {
                if (token.IsCancellationRequested)
                    return;

                await RunGenerationForDate(station, entry, date, token);
            }

            _lastRun[key] = now;
        }

        private async Task RunGenerationForDate(
            StationConfig station,
            ScheduleEntry entry,
            DateTime date,
            CancellationToken token)
        {
            try
            {
                var strategy = AirLogQueryFactory.GetStrategy(entry.QueryType);

                if (string.IsNullOrWhiteSpace(station.Host))
                {
                    _log.Error("SCHEDULER",
                        $"Station {station.StationName} has NO host defined. Skipping.");
                    return;
                }

                _db.SetConnection(station.Host, "woar_server", "", "");

                var rows = await strategy.ExecuteAsync(_db, station.StationName, date);

                var lines = _formatter.GenerateLines(
                    rows,
                    date,
                    station.TimezoneOffset,
                    station.EnableTimezoneCalc
                );

                if (lines.Count == 0)
                    return;

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