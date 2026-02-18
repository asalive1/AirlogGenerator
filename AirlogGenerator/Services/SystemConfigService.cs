using AirlogGenerator.Config;
using System.Text.Json;

public class SystemConfigService
{
    private readonly string _path;

    public SystemConfigService()
    {
        var appRoot = AppContext.BaseDirectory;
        _path = Path.Combine(appRoot, "CONFIG", "system.json");
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

        Console.WriteLine("[CONFIG] Raw JSON loaded:");
        Console.WriteLine(json);

        var cfg = JsonSerializer.Deserialize<SystemConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new SystemConfig();

        Console.WriteLine("[CONFIG] Parsed values:");
        Console.WriteLine($"  WebPort = {cfg.WebPort}");
        Console.WriteLine($"  DefaultServerIp = {cfg.DefaultServerIp}");
        Console.WriteLine($"  SchedulerIntervalMinutes = {cfg.SchedulerIntervalMinutes}");
        Console.WriteLine($"  Logging.Level = {cfg.Logging?.Level}");
        Console.WriteLine($"  Logging.RetentionDays = {cfg.Logging?.RetentionDays}");
        Console.WriteLine($"  Logging.MaxSizeMb = {cfg.Logging?.MaxSizeMb}");

        return cfg;
    }

    public void Save(SystemConfig cfg)
    {
        Console.WriteLine($"[CONFIG] Saving system.json to: {_path}");

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