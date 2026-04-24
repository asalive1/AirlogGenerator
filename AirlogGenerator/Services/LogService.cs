using System.Collections.Concurrent;
using AirlogGenerator.Config;
using System.Reflection;

namespace AirlogGenerator.Services
{
    public class LogService
    {
        private readonly SystemConfigService _configService;
        private readonly string _logFolder;

        private readonly ConcurrentQueue<string> _buffer = new();

        private string? _currentLogFile;
        private DateTime _currentLogDate;
        private bool _wroteVersionHeaderToday = false;

        public event Action<string>? OnNewLogLine;

        public LogService(SystemConfigService configService)
        {
            _configService = configService;

            var logOverride = Environment.GetEnvironmentVariable("AG_LOG_PATH");
            if (!string.IsNullOrWhiteSpace(logOverride))
            {
                _logFolder = logOverride;
            }
            else
            {
                var preferredFolder = Path.Combine(AppContext.BaseDirectory, "LOG");
                if (CanWriteToDirectory(preferredFolder))
                {
                    _logFolder = preferredFolder;
                }
                else
                {
                    _logFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "AirlogGenerator",
                        "LOG"
                    );
                }
            }

            Directory.CreateDirectory(_logFolder);

            _currentLogDate = DateTime.Now.Date;
            _currentLogFile = GetDailyLogFilePath(_currentLogDate);

            CleanupOldLogs();
        }

        private static bool CanWriteToDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                var probeFile = Path.Combine(path, $".write-test-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probeFile, "ok");
                File.Delete(probeFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private LoggingConfig GetLoggingConfig()
        {
            var cfg = _configService.Load().Logging;

            return cfg ?? new LoggingConfig
            {
                Level = "INFO",
                RetentionDays = 7,
                MaxSizeMb = 10
            };
        }

        private string GetDailyLogFilePath(DateTime date)
        {
            string baseName = date.ToString("yyMMdd");
            string path = Path.Combine(_logFolder, $"{baseName}.log");

            return path;
        }

        private string GetRolloverFilePath(DateTime date, int index)
        {
            string baseName = date.ToString("yyMMdd");
            return Path.Combine(_logFolder, $"{baseName}-{index:00}.log");
        }

        private void EnsureLogFile()
        {
            DateTime today = DateTime.Now.Date;

            // New day → new file
            if (today != _currentLogDate)
            {
                _currentLogDate = today;
                _currentLogFile = GetDailyLogFilePath(today);
                _wroteVersionHeaderToday = false;
            }

            // If file exceeds size → roll over
            var cfg = GetLoggingConfig();
            long maxBytes = cfg.MaxSizeMb * 1024L * 1024L;

            if (File.Exists(_currentLogFile))
            {
                var info = new FileInfo(_currentLogFile);
                if (info.Length > maxBytes)
                {
                    int index = 1;
                    string rollover;

                    do
                    {
                        rollover = GetRolloverFilePath(today, index);
                        index++;
                    }
                    while (File.Exists(rollover));

                    File.Move(_currentLogFile, rollover, true);
                }
            }

            // Ensure file exists
            if (!File.Exists(_currentLogFile))
            {
                File.WriteAllText(_currentLogFile, "");
                _wroteVersionHeaderToday = false;
            }

            // Write version header once per day
            if (!_wroteVersionHeaderToday)
            {
                WriteVersionHeader();
                _wroteVersionHeaderToday = true;
            }
        }

        private void WriteVersionHeader()
        {
            var asm = Assembly.GetEntryAssembly();
            var version = asm?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "unknown";

            string header =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] [SYSTEM] AirlogGenerator version {version} starting.";

            File.AppendAllText(_currentLogFile!, header + Environment.NewLine);
            OnNewLogLine?.Invoke(header);
        }

        private void CleanupOldLogs()
        {
            try
            {
                var cfg = GetLoggingConfig();
                int days = cfg.RetentionDays;

                var files = Directory.GetFiles(_logFolder, "*.log");

                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    if (info.CreationTime < DateTime.Now.AddDays(-days))
                    {
                        info.Delete();
                    }
                }
            }
            catch
            {
                // Never crash on cleanup
            }
        }

        private void Write(string level, string source, string message)
        {
            var cfg = GetLoggingConfig();

            // Respect log level
            if (cfg.Level == "ERROR" && level != "ERROR")
                return;

            if (cfg.Level == "INFO" && level == "DEBUG")
                return;

            EnsureLogFile();

            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] [{source}] {message}";

            try
            {
                File.AppendAllText(_currentLogFile!, line + Environment.NewLine);
            }
            catch
            {
                // Ignore write failures
            }

            _buffer.Enqueue(line);
            OnNewLogLine?.Invoke(line);
        }

        public void Info(string src, string msg) => Write("INFO", src, msg);
        public void Debug(string src, string msg) => Write("DEBUG", src, msg);
        public void Error(string src, string msg) => Write("ERROR", src, msg);
        public void Success(string src, string msg) => Write("SUCCESS", src, msg);
    }
}