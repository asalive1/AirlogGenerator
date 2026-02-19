using AirlogGenerator.Config;
using System.Text.Json;

public class SystemConfigService
{
    private readonly string _path;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SystemConfigService()
    {
        var configRoot = Environment.GetEnvironmentVariable("AG_CONFIG_PATH");
        if (string.IsNullOrWhiteSpace(configRoot))
        {
            configRoot = Path.Combine(AppContext.BaseDirectory, "CONFIG");
        }

        _path = Path.Combine(configRoot, "system.json");
    }

    public SystemConfig Load()
    {
        Console.WriteLine($"[CONFIG] Loading system.json from: {_path}");

        if (!File.Exists(_path))
        {
            Console.WriteLine("[CONFIG] ERROR: system.json does NOT exist!");
            return new SystemConfig();
        }

        var json = File.ReadAllText(_path);

        if (string.IsNullOrWhiteSpace(json))
        {
            Console.WriteLine("[CONFIG] WARNING: system.json is empty. Using defaults.");
            return new SystemConfig();
        }

        Console.WriteLine("[CONFIG] Raw JSON loaded:");
        Console.WriteLine(json);

        SystemConfig cfg;
        try
        {
            cfg = JsonSerializer.Deserialize<SystemConfig>(json, _jsonOptions) ?? new SystemConfig();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[CONFIG] WARNING: system.json invalid JSON ({ex.Message}). Using defaults.");
            return new SystemConfig();
        }

        Console.WriteLine("[CONFIG] Parsed values:");
        Console.WriteLine($"  WebPort = {cfg.WebPort}");
        Console.WriteLine($"  DefaultServerIp = {cfg.DefaultServerIp}");
        Console.WriteLine($"  DefaultServerHost = {cfg.DefaultServerHost}");
        Console.WriteLine($"  DefaultServerUser = {cfg.DefaultServerUser}");
        Console.WriteLine($"  DefaultServerPassword (empty? {string.IsNullOrWhiteSpace(cfg.DefaultServerPassword)})");
        Console.WriteLine($"  DefaultServerPasswordSecret = {cfg.DefaultServerPasswordSecret}");
        Console.WriteLine($"  DefaultServerPasswordSecretRegion = {cfg.DefaultServerPasswordSecretRegion}");
        Console.WriteLine($"  SchedulerIntervalMinutes = {cfg.SchedulerIntervalMinutes}");
        Console.WriteLine($"  Logging.Level = {cfg.Logging?.Level}");
        Console.WriteLine($"  Logging.RetentionDays = {cfg.Logging?.RetentionDays}");
        Console.WriteLine($"  Logging.MaxSizeMb = {cfg.Logging?.MaxSizeMb}");
        Console.WriteLine($"  EnableWebUi = {cfg.EnableWebUi}");

        return cfg;
    }

    public void Save(SystemConfig cfg)
    {
        Console.WriteLine($"[CONFIG] Saving system.json to: {_path}");

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        Console.WriteLine("[CONFIG] JSON being written:");
        Console.WriteLine(json);

        File.WriteAllText(_path, json);

        Console.WriteLine("[CONFIG] Save complete.");
    }
}