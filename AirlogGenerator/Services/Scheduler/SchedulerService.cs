using System.Text.Json;
using System.IO;
using System.Threading;
using AirlogGenerator.Models;
using AirlogGenerator.Config;

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
        private readonly SemaphoreSlim _stateLock = new(1, 1);
        private readonly string _stateFilePath = Path.Combine(AppContext.BaseDirectory, "CONFIG", "scheduler-state.json");

        private readonly Dictionary<string, DateTime> _lastRun = new();
        private readonly Dictionary<string, string> _scheduleSignatures = new();

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
            LoadSchedulerState(); // Load state on startup

            while (!stoppingToken.IsCancellationRequested)
            {
                DateTime now = DateTime.Now; // Capture once per cycle
                await RunSchedulerCycle(now, stoppingToken);

                var cfg = _configService.Load();
                int interval = cfg.SchedulerIntervalMinutes;
                await Task.Delay(interval <= 0 ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(interval), stoppingToken);
            }
        }

        private void LoadSchedulerState()
        {
            if (!File.Exists(_stateFilePath)) return;

            try
            {
                var json = File.ReadAllText(_stateFilePath);
                var state = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json);
                if (state != null)
                    foreach (var kvp in state)
                        _lastRun[kvp.Key] = kvp.Value;
            }
            catch (Exception ex)
            {
                _log.Error("SCHEDULER", $"Failed to load scheduler state: {ex.Message}");
            }
        }

        private async Task SaveSchedulerState()
        {
            await _stateLock.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(_lastRun, _jsonOptions);
                await File.WriteAllTextAsync(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                _log.Error("SCHEDULER", $"Failed to save scheduler state: {ex.Message}");
            }
            finally
            {
                _stateLock.Release();
            }
        }

        private async Task RunSchedulerCycle(DateTime now, CancellationToken token)
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

                await ProcessStationConfig(file, now, token);
            }
        }

        private async Task ProcessStationConfig(string filePath, DateTime now, CancellationToken token)
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

                    await EvaluateSchedule(cfg, entry, index, now, token);
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
            DateTime now,
            CancellationToken token)
        {
            string key = $"{station.StationName}:{index}";
            string signature = JsonSerializer.Serialize(entry);

            if (!_scheduleSignatures.ContainsKey(key) ||
                _scheduleSignatures[key] != signature)
            {
                _scheduleSignatures[key] = signature;
                _lastRun[key] = DateTime.MinValue;
                _log.Info("SCHEDULER", $"Schedule changed for {station.StationName} entry {index}. Resetting last-run.");
            }

            DateTime lastRun = _lastRun.ContainsKey(key) ? _lastRun[key] : DateTime.MinValue;
            bool shouldRun = ScheduleEvaluator.ShouldRun(entry, now, lastRun, _log);

            if (!shouldRun) return;

            var dates = DateRangeResolver.ResolveDates(entry, now, _log);
            foreach (var date in dates)
            {
                if (token.IsCancellationRequested) return;
                await RunGenerationForDate(station, entry, date, token);
            }

            _lastRun[key] = now;
            await SaveSchedulerState();
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
                    _log.Error("SCHEDULER", $"Station {station.StationName} has NO host defined. Skipping.");
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
                _log.Error("SCHEDULER", $"Failed to run generation for {station.StationName} on {date:yyyy-MM-dd}: {ex.Message}");
            }
        }
    }
}