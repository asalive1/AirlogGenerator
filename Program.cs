using AirlogGenerator.Config;
using AirlogGenerator.Models;
using AirlogGenerator.Services;
using AirlogGenerator.Services.Scheduler;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

if (OperatingSystem.IsWindows() && !Environment.UserInteractive)
{
    builder.Host.UseWindowsService();
}

// Load config paths

// Determine base path depending on OS
string appRoot;

if (OperatingSystem.IsWindows())
{
    // Running on Windows (Debug or Release)
    // AppContext.BaseDirectory points to bin\Debug or bin\Release
    appRoot = AppContext.BaseDirectory;
}
else
{
    // Running inside Linux container
    appRoot = "/app";
}
var systemConfigPath = Path.Combine(appRoot, "CONFIG", "system.json");
var stationConfigRoot = Path.Combine(appRoot, "CONFIG", "stations");
Console.WriteLine("====================================================");
Console.WriteLine("[STARTUP] Application starting");
Console.WriteLine($"[STARTUP] AppContext.BaseDirectory = {appRoot}");
Console.WriteLine($"[STARTUP] system.json expected at = {systemConfigPath}");
Console.WriteLine("====================================================");

Console.WriteLine($"[CONFIG] App Root: {appRoot}");
Console.WriteLine($"[CONFIG] system.json path: {systemConfigPath}");

Directory.CreateDirectory(stationConfigRoot);

// Register config service
builder.Services.AddSingleton<SystemConfigService>();

// Load initial config so LoggingConfig can be injected
var configService = new SystemConfigService();
var initialConfig = configService.Load();

// Configure Kestrel to use port from system.json
if (initialConfig.EnableWebUi)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(initialConfig.WebPort);

        var httpsCertPath = Environment.GetEnvironmentVariable("AG_HTTPS_CERT_PATH");
        var httpsCertPassword = Environment.GetEnvironmentVariable("AG_HTTPS_CERT_PASSWORD");

        if (!string.IsNullOrWhiteSpace(httpsCertPath) && File.Exists(httpsCertPath))
        {
            try
            {
                options.ListenAnyIP(initialConfig.WebPort + 1, listenOptions =>
                {
                    if (string.IsNullOrWhiteSpace(httpsCertPassword))
                    {
                        listenOptions.UseHttps(httpsCertPath);
                    }
                    else
                    {
                        listenOptions.UseHttps(httpsCertPath, httpsCertPassword);
                    }
                });

                Console.WriteLine($"[CONFIG] HTTPS enabled on port {initialConfig.WebPort + 1} using AG_HTTPS_CERT_PATH.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] HTTPS could not be enabled from AG_HTTPS_CERT_PATH: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("[CONFIG] HTTPS disabled (no AG_HTTPS_CERT_PATH provided). HTTP remains enabled.");
        }
    });
}
else
{
    Console.WriteLine("[CONFIG] EnableWebUi = false � Kestrel will NOT be started.");
}

// ======================================================
//  INITIALIZATION SYSTEM
// ======================================================

static void InitializeAppEnvironment(string appRoot, LogService? log)
{
    bool firstRun = false;

    // 1. LOG folder
    var logDir = Path.Combine(appRoot, "LOG");
    if (!Directory.Exists(logDir))
    {
        Directory.CreateDirectory(logDir);
        firstRun = true;
        log?.Info("INIT", "Created LOG folder.");
    }

    // 2. CONFIG folder
    var configDir = Path.Combine(appRoot, "CONFIG");
    if (!Directory.Exists(configDir))
    {
        Directory.CreateDirectory(configDir);
        firstRun = true;
        log?.Info("INIT", "Created CONFIG folder.");
    }

    // 3. CONFIG\stations folder
    var stationsDir = Path.Combine(configDir, "stations");
    if (!Directory.Exists(stationsDir))
    {
        Directory.CreateDirectory(stationsDir);
        firstRun = true;
        log?.Info("INIT", "Created CONFIG\\stations folder.");
    }

    // 4. system.json default file
    var systemJsonPath = Path.Combine(configDir, "system.json");
    if (!File.Exists(systemJsonPath))
    {
        EnsureDefaultSystemJson(systemJsonPath);
        firstRun = true;
        log?.Info("INIT", "Created default system.json.");
    }

    // 5. wwwroot folder
    var wwwrootDir = Path.Combine(appRoot, "wwwroot");
    if (!Directory.Exists(wwwrootDir))
    {
        Directory.CreateDirectory(wwwrootDir);
        firstRun = true;
        log?.Info("INIT", "Created wwwroot folder.");
    }

    // 6. Extract embedded wwwroot files
    ExtractEmbeddedWwwroot(wwwrootDir, log);

    if (firstRun)
    {
        log?.Success("INIT", "First-run initialization completed.");
    }
}

static void EnsureDefaultSystemJson(string path)
{
    var defaultConfig = new SystemConfig
    {
        WebPort = 8030,
        DefaultServerIp = "127.0.0.1",
        SchedulerIntervalMinutes = 1,
        Logging = new LoggingConfig
        {
            Level = "INFO",
            RetentionDays = 14,
            MaxSizeMb = 10
        },
        EnableWebUi = true   // <-- add this
    };

    var json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    File.WriteAllText(path, json);
}

static void ExtractEmbeddedWwwroot(string targetDir, LogService? log)
{
    var asm = Assembly.GetExecutingAssembly();
    var resources = asm.GetManifestResourceNames();

    foreach (var res in resources)
    {
        if (!res.Contains("wwwroot")) continue;

        // Convert resource name to file path
        var relative = res.Substring(res.IndexOf("wwwroot"));
        var filePath = Path.Combine(targetDir, relative.Replace('.', Path.DirectorySeparatorChar));

        var dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        

        if (!File.Exists(filePath))
        {
            using var stream = asm.GetManifestResourceStream(res);
            using var fs = File.Create(filePath);
            stream.CopyTo(fs);

            log?.Info("INIT", $"Extracted wwwroot file: {relative}");
        }
    }
}

// Register services
builder.Services.AddSingleton<LogService>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddHttpClient();
builder.Services.AddRouting();
builder.Services.AddHostedService<SchedulerService>();
builder.Services.AddSingleton<AirLogFormattingService>();

var tempProvider = builder.Services.BuildServiceProvider();
var tempLog = tempProvider.GetService<LogService>();

// Log version info
var asm = Assembly.GetEntryAssembly();

var info = asm?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
var file = asm?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
var assembly = asm?.GetName().Version?.ToString();

tempLog?.Info("SYSTEM", $"Version Info ? Informational: {info}, File: {file}, Assembly: {assembly}");

InitializeAppEnvironment(appRoot, tempLog);

var app = builder.Build();

// Load config again for runtime decisions
var cfgForRuntime = app.Services
    .GetRequiredService<SystemConfigService>()
    .Load();

// Initialize DatabaseService from system.json for scheduler / on-demand flows
var dbForRuntime = app.Services.GetRequiredService<DatabaseService>();
await dbForRuntime.SetConnectionFromConfigAsync(cfgForRuntime);

// ===============================
// WEB UI ENABLED
// ===============================
if (cfgForRuntime.EnableWebUi)
{
    Console.WriteLine("====================================================");
    Console.WriteLine("[STARTUP] Web UI enabled. Initializing HTTP endpoints...");
    Console.WriteLine("====================================================");

    // Serve static files
    app.UseDefaultFiles();
    app.UseStaticFiles();

    // JSON options
    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    // API: status
    app.MapGet("/api/status", (SystemConfigService svc, LogService log) =>
    {
        log.Info("SYSTEM", "Status API called.");

        var cfg = svc.Load();

        var entryAssembly = Assembly.GetEntryAssembly();
        var version = entryAssembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

        return Results.Ok(new
        {
            status = "OK",
            version,
            defaultServerIp = cfg.DefaultServerIp
        });
    });


    // Live log stream
    app.MapGet("/api/log/live", async (HttpContext context, LogService log) =>
    {
        context.Response.Headers.Append("Content-Type", "text/event-stream");

        var response = context.Response;
        var cancellation = context.RequestAborted;

        await response.WriteAsync("data: Log stream connected\n\n");
        await response.Body.FlushAsync();

        void Handler(string line)
        {
            if (!cancellation.IsCancellationRequested)
            {
                response.WriteAsync($"data: {line}\n\n");
                response.Body.FlushAsync();
            }
        }

        log.OnNewLogLine += Handler;

        try
        {
            while (!cancellation.IsCancellationRequested)
                await Task.Delay(500, cancellation);
        }
        catch (TaskCanceledException)
        {
        }
        finally
        {
            log.OnNewLogLine -= Handler;
        }
    });

    // DB connect
    app.MapPost("/api/db/connect", async (
        DatabaseService db,
        LogService log,
        DbConnectRequest req) =>
    {
        db.SetConnection(req.Host, req.Database, req.User, req.Password);
        log.Info("USER", $"Attempting DB connection to {req.Host}/{req.Database}");

        bool ok = await db.TestConnectionAsync();
        return Results.Ok(new { success = ok });
    });

    // DB version
    app.MapGet("/api/db/version", async (DatabaseService db, LogService log) =>
    {
        log.Info("SERVICE", "DB version endpoint hit.");

        var version = await db.GetPostgresVersionAsync();
        return Results.Ok(new { version });
    });

    // Set server IP
    app.MapPost("/api/server/setip", (ServerIpRequest req, SystemConfigService svc, LogService log) =>
    {
        var cfg = svc.Load();
        cfg.DefaultServerIp = req.Ip;
        svc.Save(cfg);

        log.Info("CONFIG", $"DefaultServerIp updated to {req.Ip}");

        return Results.Ok();
    });

    // Get server version
    app.MapGet("/api/server/version", async (HttpClient http, SystemConfigService svc, LogService log) =>
    {
        try
        {
            var cfg = svc.Load();
            var url = $"http://{cfg.DefaultServerIp}/internal/statusz";
            var text = await http.GetStringAsync(url);

            var versionLine = text.Split('\n').FirstOrDefault(l => l.StartsWith("Version:"));
            var version = versionLine?.Replace("Version:", "").Trim();

            log.Success("SERVICE", $"Central server version: {version}");

            return Results.Ok(new { version });
        }
        catch (Exception ex)
        {
            log.Error("SERVICE", $"Failed to get server version: {ex.Message}");
            return Results.Ok(new { version = "ERROR" });
        }
    });

    // System JSON File
    app.MapGet("/api/system", (SystemConfigService svc) =>
    {
        var cfg = svc.Load();
        return Results.Ok(cfg);
    });


    app.MapPost("/api/system", async (HttpRequest req, SystemConfigService svc, LogService log) =>
    {
        using var reader = new StreamReader(req.Body);
        var jsonBody = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(jsonBody);
        var root = doc.RootElement;

        var updated = JsonSerializer.Deserialize<SystemConfig>(jsonBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (updated == null)
            return Results.BadRequest("Invalid system.json");

        var current = svc.Load();
        var merged = current;

        static bool HasProperty(JsonElement element, string propertyName)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        if (HasProperty(root, "webPort"))
            merged.WebPort = updated.WebPort;

        if (HasProperty(root, "defaultServerIp"))
            merged.DefaultServerIp = updated.DefaultServerIp;

        if (HasProperty(root, "defaultServerHost"))
            merged.DefaultServerHost = updated.DefaultServerHost;

        if (HasProperty(root, "defaultDatabaseName"))
            merged.DefaultDatabaseName = updated.DefaultDatabaseName;

        if (HasProperty(root, "schedulerIntervalMinutes"))
            merged.SchedulerIntervalMinutes = updated.SchedulerIntervalMinutes;

        if (HasProperty(root, "enableWebUi"))
            merged.EnableWebUi = updated.EnableWebUi;

        if (HasProperty(root, "defaultServerUser"))
            merged.DefaultServerUser = updated.DefaultServerUser;

        if (HasProperty(root, "defaultServerPassword"))
            merged.DefaultServerPassword = updated.DefaultServerPassword;

        if (HasProperty(root, "defaultServerPasswordSecret"))
            merged.DefaultServerPasswordSecret = updated.DefaultServerPasswordSecret;

        if (HasProperty(root, "defaultServerPasswordSecretRegion"))
            merged.DefaultServerPasswordSecretRegion = updated.DefaultServerPasswordSecretRegion;

        if (TryGetProperty(root, "logging", out var loggingNode))
        {
            merged.Logging ??= new LoggingConfig();
            var incomingLogging = updated.Logging ?? new LoggingConfig();

            if (HasProperty(loggingNode, "level"))
                merged.Logging.Level = incomingLogging.Level;

            if (HasProperty(loggingNode, "retentionDays"))
                merged.Logging.RetentionDays = incomingLogging.RetentionDays;

            if (HasProperty(loggingNode, "maxSizeMb"))
                merged.Logging.MaxSizeMb = incomingLogging.MaxSizeMb;
        }

        svc.Save(merged);

        log.Success("CONFIG", "system.json updated");

        return Results.Ok(new { success = true });
    });


    // Station list
    app.MapGet("/api/stations", async (DatabaseService db, LogService log) =>
    {
        log.Info("SERVICE", "Station list endpoint hit.");

        try
        {
            var stations = await db.GetStationListWithOffsetsAsync();
            log.Info("SERVICE", $"Station list returned {stations.Count} stations.");
            return Results.Ok(stations);
        }
        catch (Exception ex)
        {
            log.Error("SERVICE", $"Station list failed: {ex.Message}");
            return Results.Problem("Failed to load stations");
        }
    });

    // Load station config
    app.MapGet("/api/stations/{name}", (string name, LogService log) =>
    {
        var file = Path.Combine(stationConfigRoot, $"{name}.json");

        if (!File.Exists(file))
        {
            log.Info("CONFIG", $"Config file not found. Creating default for: {file}");
            return Results.NotFound();
        }

        log.Info("CONFIG", $"Loading station config from: {file}");

        var json = File.ReadAllText(file);
        var config = JsonSerializer.Deserialize<StationConfig>(json, jsonOptions);

        return Results.Ok(config);
    });

    // Save station config
    app.MapPost("/api/stations/{name}", async (
        string name,
        HttpRequest req,
        SystemConfigService svc,
        LogService log) =>
    {
        var file = Path.Combine(stationConfigRoot, $"{name}.json");

        Directory.CreateDirectory(stationConfigRoot);

        using var reader = new StreamReader(req.Body);
        var jsonBody = await reader.ReadToEndAsync();

        log.Info("CONFIG", $"Saving station config to: {file}");

        var cfg = JsonSerializer.Deserialize<StationConfig>(jsonBody, jsonOptions);

        if (cfg == null)
        {
            log.Error("CONFIG", $"Invalid station config posted for {name}");
            return Results.BadRequest("Invalid station config");
        }

        // NEW � enforce host ALWAYS
        if (string.IsNullOrWhiteSpace(cfg.Host))
        {
            // Pull from system.json
            var sys = svc.Load();
            cfg.Host = sys.DefaultServerIp;

            log.Info("CONFIG",
                $"Station {cfg.StationName} had no host set. Backfilled from system.json: {cfg.Host}");
        }

        // Serialize with host included
        var jsonOut = JsonSerializer.Serialize(cfg, jsonOptions);

        await File.WriteAllTextAsync(file, jsonOut);

        log.Success("CONFIG", $"Saved station config: {file}");

        return Results.Ok();
    });

    // On-demand generation (Save / Write)
    app.MapPost("/api/ondemand", async (
        OnDemandRequest req,
        LogService log,
        DatabaseService db) =>
    {
        var requestedDestinations = new List<string>();

        if (req.Destinations is not null && req.Destinations.Count > 0)
            requestedDestinations.AddRange(req.Destinations.Where(d => !string.IsNullOrWhiteSpace(d)));

        if (!string.IsNullOrWhiteSpace(req.Destination))
            requestedDestinations.Add(req.Destination);

        var destinationPaths = requestedDestinations
            .Select(d => Path.IsPathRooted(d) ? d : Path.Combine(AppContext.BaseDirectory, d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (destinationPaths.Count == 0)
            return Results.BadRequest("At least one destination is required.");

        log.Info("ONDEMAND",
            $"Request for {req.Station} ? {req.Dates.Count} date(s) ? {destinationPaths.Count} destination(s)");

        foreach (var destinationPath in destinationPaths)
        {
            try { Directory.CreateDirectory(destinationPath); }
            catch (Exception ex)
            {
                return Results.Problem($"Cannot create destination '{destinationPath}': {ex.Message}");
            }
        }

        var cfgFile = Path.Combine(stationConfigRoot, $"{req.Station}.json");
        if (!File.Exists(cfgFile))
            return Results.Problem("Station config not found.");

        var cfgJson = await File.ReadAllTextAsync(cfgFile);
        var cfg = JsonSerializer.Deserialize<StationConfig>(cfgJson, jsonOptions)
                  ?? throw new Exception("Invalid station config");

        double offset = cfg.TimezoneOffset;
        bool useOffset = cfg.EnableTimezoneCalc;

        if (!Enum.TryParse<AirLogQueryType>(req.QueryType, out var queryType))
        {
            log.Error("ONDEMAND", $"Invalid queryType '{req.QueryType}', defaulting to Standard");
            queryType = AirLogQueryType.Standard;
        }

        log.Info("ONDEMAND",
            $"Using query type: {queryType} for station {req.Station}");

        var generated = new List<string>();

        foreach (var dateStr in req.Dates)
        {
            if (!DateTime.TryParse(dateStr, out var date))
                continue;

            var result = await GenerateAirLogForDateAsync(
                cfg.StationName,
                date,
                queryType,
                offset,
                useOffset,
                db,
                log);

            if (result is null)
                continue;

            var fileName = result.Value.FileName;
            var lines = result.Value.Lines;

            foreach (var destinationPath in destinationPaths)
            {
                string filePath = Path.Combine(destinationPath, fileName);
                await File.WriteAllLinesAsync(filePath, lines);

                log.Success("ONDEMAND",
                    $"Wrote {lines.Count} lines ? {filePath}");

                generated.Add(filePath);
            }
        }

        return Results.Ok(new { success = true, files = generated });
    });

    // On-demand preview (no file write; returns lines to UI)
    app.MapPost("/api/ondemand/preview", async (
        OnDemandRequest req,
        LogService log,
        DatabaseService db) =>
    {
        if (req.Dates == null || req.Dates.Count == 0)
            return Results.BadRequest("At least one date is required.");

        var cfgFile = Path.Combine(stationConfigRoot, $"{req.Station}.json");
        if (!File.Exists(cfgFile))
            return Results.Problem("Station config not found.");

        var cfgJson = await File.ReadAllTextAsync(cfgFile);
        var cfg = JsonSerializer.Deserialize<StationConfig>(cfgJson, jsonOptions)
                  ?? throw new Exception("Invalid station config");

        double offset = cfg.TimezoneOffset;
        bool useOffset = cfg.EnableTimezoneCalc;

        if (!Enum.TryParse<AirLogQueryType>(req.QueryType, out var queryType))
        {
            log.Error("ONDEMAND", $"Invalid queryType '{req.QueryType}', defaulting to Standard");
            queryType = AirLogQueryType.Standard;
        }

        log.Info("ONDEMAND",
            $"[PREVIEW] Using query type: {queryType} for station {req.Station}");

        // For now: combine all selected dates into a single preview
        string? firstFileName = null;
        var allLines = new List<string>();

        foreach (var dateStr in req.Dates)
        {
            if (!DateTime.TryParse(dateStr, out var date))
                continue;

            var result = await GenerateAirLogForDateAsync(
                cfg.StationName,
                date,
                queryType,
                offset,
                useOffset,
                db,
                log);

            if (result is null)
                continue;

            var fileName = result.Value.FileName;
            var lines = result.Value.Lines;

            if (firstFileName == null)
                firstFileName = fileName;

            // Optional: separator between days if multiple
            if (req.Dates.Count > 1)
            {
                allLines.Add($"# --- {date:yyyy-MM-dd} ({fileName}) ---");
            }

            allLines.AddRange(lines);
        }

        if (allLines.Count == 0)
        {
            return Results.Ok(new
            {
                success = false,
                message = "No log lines generated for the selected date(s)."
            });
        }

        return Results.Ok(new
        {
            success = true,
            fileName = firstFileName ?? "preview.air",
            lines = allLines
        });
    });

    // On-demand download (no write to destination; returns file to browser)
    app.MapPost("/api/ondemand/download", async (
        OnDemandRequest req,
        LogService log,
        DatabaseService db) =>
    {
        if (req.Dates == null || req.Dates.Count == 0)
            return Results.BadRequest("At least one date is required.");

        var cfgFile = Path.Combine(stationConfigRoot, $"{req.Station}.json");
        if (!File.Exists(cfgFile))
            return Results.Problem("Station config not found.");

        var cfgJson = await File.ReadAllTextAsync(cfgFile);
        var cfg = JsonSerializer.Deserialize<StationConfig>(cfgJson, jsonOptions)
                  ?? throw new Exception("Invalid station config");

        double offset = cfg.TimezoneOffset;
        bool useOffset = cfg.EnableTimezoneCalc;

        if (!Enum.TryParse<AirLogQueryType>(req.QueryType, out var queryType))
        {
            log.Error("ONDEMAND", $"Invalid queryType '{req.QueryType}', defaulting to Standard");
            queryType = AirLogQueryType.Standard;
        }

        log.Info("ONDEMAND",
            $"[DOWNLOAD] Using query type: {queryType} for station {req.Station}");

        string? firstFileName = null;
        var allLines = new List<string>();

        foreach (var dateStr in req.Dates)
        {
            if (!DateTime.TryParse(dateStr, out var date))
                continue;

            var result = await GenerateAirLogForDateAsync(
                cfg.StationName,
                date,
                queryType,
                offset,
                useOffset,
                db,
                log);

            if (result is null)
                continue;

            var generatedFileName = result.Value.FileName;
            var lines = result.Value.Lines;

            if (firstFileName == null)
                firstFileName = generatedFileName;

            if (req.Dates.Count > 1)
            {
                allLines.Add($"# --- {date:yyyy-MM-dd} ({generatedFileName}) ---");
            }

            allLines.AddRange(lines);
        }

        if (allLines.Count == 0)
        {
            return Results.Problem("No log lines generated for the selected date(s).");
        }

        // Join lines into a single text payload
        var text = string.Join(Environment.NewLine, allLines);
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);

        // Base name from first generated file, e.g. "260101.air"
        var baseName = firstFileName ?? "ondemand.air";

        // Optional: sanitize station name (no spaces, no slashes)
        var safeStation = (req.Station ?? "Station")
            .Replace(" ", "_")
            .Replace("/", "_")
            .Replace("\\", "_");

        // Final file name: Station-YYMMDD.air
        var fileName = $"{safeStation}-{baseName}";

        return Results.File(
            bytes,
            contentType: "text/plain",
            fileDownloadName: fileName);
    });

    // Shared helper for on-demand generation (one station, one date)
    async Task<(string FileName, List<string> Lines)?> GenerateAirLogForDateAsync(
        string stationName,
        DateTime date,
        AirLogQueryType queryType,
        double offset,
        bool useOffset,
        DatabaseService db,
        LogService log)
    {
        var strategy = AirLogQueryFactory.GetStrategy(queryType);

        log.Info("ONDEMAND",
            $"[Helper] Generating AIR log for {stationName} on {date:yyyy-MM-dd} using query type {queryType}");

        var rows = await strategy.ExecuteAsync(db, stationName, date);

        var formatter = app.Services.GetRequiredService<AirLogFormattingService>();

        var lines = formatter.GenerateLines(
            rows,
            date,
            offset,
            useOffset
        );

        if (lines.Count == 0)
        {
            log.Info("ONDEMAND",
                $"[Helper] No entries for {stationName} on {date:yyyy-MM-dd}");
            return null;
        }

        string fileName = date.ToString("yyMMdd") + ".air";
        return (fileName, lines);
    }
}

    // ===============================
    // START HOST (ALWAYS)
    // ===============================
    if (cfgForRuntime.EnableWebUi)
    {
        Console.WriteLine("[STARTUP] Starting Kestrel (Web UI mode)...");
        app.Run();
    }
    else
    {
        Console.WriteLine("[CONFIG] EnableWebUi = false");
        Console.WriteLine("[STARTUP] Web interface disabled. Scheduler-only mode.");
        Console.WriteLine("[STARTUP] Hosted services (SchedulerService) will continue running.");

        // This keeps the host alive so background services run
        await app.RunAsync();
    }

public record ServerIpRequest(string Ip);

