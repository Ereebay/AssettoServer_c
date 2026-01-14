using System.Collections.Concurrent;
using AssettoServer.Network.Tcp;
using AssettoServer.Server;
using AssettoServer.Server.Configuration;
using AssettoServer.Server.Plugin;
using AssettoServer.Shared.Network.Packets.Incoming;
using AssettoServer.Shared.Services;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SXRLeaderboardPlugin;

/// <summary>
/// SXR Leaderboard Plugin - Centralized leaderboard system
/// Tracks Player Ranks, Time Trials, and Longest Runs
/// </summary>
public class SXRLeaderboardPlugin : CriticalBackgroundService, IAssettoServerAutostart
{
    private readonly EntryCarManager _entryCarManager;
    private readonly SXRLeaderboardConfiguration _config;
    private readonly SXRLeaderboardService _leaderboardService;
    private readonly CSPServerScriptProvider _scriptProvider;
    
    // Player session tracking
    private readonly ConcurrentDictionary<int, PlayerSession> _sessions = new();
    
    // External data providers
    private Func<string, int>? _getDriverLevel;
    private Func<string, int>? _getPrestigeRank;
    private Func<string, string>? _getClubTag;
    private Func<string, string>? _getCarDisplayName;
    private Func<string, string>? _getCarClass;
    
    // Events for other plugins to hook into
    public event Action<string, PlayerRankEntry>? OnRankUpdated;
    public event Action<string, TimeTrialEntry>? OnNewTimeTrial;
    public event Action<string, LongestRunEntry>? OnRunEnded;
    
    public SXRLeaderboardPlugin(
        EntryCarManager entryCarManager,
        SXRLeaderboardConfiguration config,
        SXRLeaderboardService leaderboardService,
        CSPServerScriptProvider scriptProvider,
        IHostApplicationLifetime applicationLifetime) : base(applicationLifetime)
    {
        _entryCarManager = entryCarManager;
        _config = config;
        _leaderboardService = leaderboardService;
        _scriptProvider = scriptProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled) return;
        
        // Subscribe to events
        _entryCarManager.ClientConnected += OnClientConnected;
        _entryCarManager.ClientDisconnected += OnClientDisconnected;
        
        foreach (var car in _entryCarManager.EntryCars)
        {
            car.PositionUpdateReceived += OnPositionUpdate;
            car.ResetInvoked += OnReset;
            car.CollisionReceived += OnCollision;
        }
        
        // Load Lua UI
        if (_config.EnableLuaUI)
        {
            LoadLuaUI();
        }
        
        Log.Information("SXR Leaderboard Plugin initialized");
        Log.Information("  - Player Ranks: Enabled");
        Log.Information("  - Time Trials: {Status}", _config.EnableTimeTrials ? "Enabled" : "Disabled");
        Log.Information("  - Longest Run: {Status}", _config.EnableLongestRun ? "Enabled" : "Disabled");
        
        // Periodic position update task (for distance tracking)
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
            UpdateAllActiveRuns();
        }
    }
    
    private void LoadLuaUI()
    {
        try
        {
            string basePath = Path.GetDirectoryName(typeof(SXRLeaderboardPlugin).Assembly.Location) ?? "";
            
            // Load main leaderboard UI
            string luaPath = Path.Combine(basePath, "lua", "sxrleaderboards.lua");
            if (File.Exists(luaPath))
            {
                _scriptProvider.AddScript(File.ReadAllText(luaPath), "sxrleaderboards.lua");
                Log.Information("SXR Leaderboard Lua UI loaded");
            }
            else
            {
                Log.Warning("SXR Leaderboard Lua UI not found at {Path}", luaPath);
            }
            
            // Load route editor UI (admin tool)
            string editorPath = Path.Combine(basePath, "lua", "sxrrouteeditor.lua");
            if (File.Exists(editorPath))
            {
                _scriptProvider.AddScript(File.ReadAllText(editorPath), "sxrrouteeditor.lua");
                Log.Information("SXR Route Editor Lua UI loaded (F9 for admins)");
            }
            else
            {
                Log.Warning("SXR Route Editor Lua UI not found at {Path}", editorPath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load SXR Leaderboard Lua UI");
        }
    }
    
    // ============================================================================
    // INTEGRATION METHODS
    // ============================================================================
    
    public void SetDriverLevelProvider(Func<string, int> provider)
    {
        _getDriverLevel = provider;
        Log.Debug("Driver level provider set for Leaderboard");
    }
    
    public void SetPrestigeRankProvider(Func<string, int> provider)
    {
        _getPrestigeRank = provider;
        Log.Debug("Prestige rank provider set for Leaderboard");
    }
    
    public void SetClubTagProvider(Func<string, string> provider)
    {
        _getClubTag = provider;
        Log.Debug("Club tag provider set for Leaderboard");
    }
    
    public void SetCarDisplayNameProvider(Func<string, string> provider)
    {
        _getCarDisplayName = provider;
        Log.Debug("Car display name provider set for Leaderboard");
    }
    
    public void SetCarClassProvider(Func<string, string> provider)
    {
        _getCarClass = provider;
        Log.Debug("Car class provider set for Leaderboard");
    }
    
    // ============================================================================
    // API METHODS (for other plugins)
    // ============================================================================
    
    /// <summary>
    /// Record a race result (called by SP Battle plugin)
    /// </summary>
    public void RecordRaceResult(string steamId, string playerName, bool isWin, 
        string carModel, int eloChange = 0, string? opponentSteamId = null)
    {
        var submission = new RaceResultSubmission
        {
            SteamId = steamId,
            PlayerName = playerName,
            IsWin = isWin,
            CarModel = carModel,
            EloChange = eloChange,
            OpponentSteamId = opponentSteamId ?? ""
        };
        
        _leaderboardService.RecordRaceResult(submission);
        
        // Update active run
        if (_config.EnableLongestRun)
        {
            _leaderboardService.UpdateActiveRun(steamId, isWin, carModel);
        }
        
        // Update external data
        var entry = _leaderboardService.GetPlayerRank(steamId);
        if (entry != null)
        {
            entry.DriverLevel = _getDriverLevel?.Invoke(steamId) ?? 1;
            entry.PrestigeRank = _getPrestigeRank?.Invoke(steamId) ?? 0;
            entry.ClubTag = _getClubTag?.Invoke(steamId) ?? "";
            
            if (!string.IsNullOrEmpty(carModel))
            {
                entry.FavoriteCarDisplayName = _getCarDisplayName?.Invoke(carModel) ?? carModel;
            }
            
            OnRankUpdated?.Invoke(steamId, entry);
        }
    }
    
    /// <summary>
    /// Submit a time trial (called by Time Trial plugin or route detection)
    /// </summary>
    public TimeTrialEntry? SubmitTimeTrial(TimeTrialSubmission submission)
    {
        if (!_config.EnableTimeTrials) return null;
        
        submission.CarClass = _getCarClass?.Invoke(submission.CarModel) ?? "D";
        
        var entry = _leaderboardService.SubmitTimeTrial(submission);
        
        if (entry != null)
        {
            entry.CarDisplayName = _getCarDisplayName?.Invoke(submission.CarModel) ?? submission.CarModel;
            OnNewTimeTrial?.Invoke(submission.SteamId, entry);
        }
        
        return entry;
    }
    
    /// <summary>
    /// Manually end a player's run (called when pit detected)
    /// </summary>
    public LongestRunEntry? EndPlayerRun(string steamId, RunEndReason reason = RunEndReason.Pitted)
    {
        if (!_config.EnableLongestRun) return null;
        
        var entry = _leaderboardService.EndActiveRun(steamId, reason);
        
        if (entry != null)
        {
            entry.PrimaryCarDisplayName = _getCarDisplayName?.Invoke(entry.PrimaryCar) ?? entry.PrimaryCar;
            OnRunEnded?.Invoke(steamId, entry);
        }
        
        return entry;
    }
    
    /// <summary>
    /// Get player ranks leaderboard
    /// </summary>
    public LeaderboardResponse<PlayerRankEntry> GetPlayerRanks(
        int page = 1, int pageSize = 50, LeaderboardSortField sortBy = LeaderboardSortField.EloRating)
    {
        return _leaderboardService.GetPlayerRanks(page, pageSize, sortBy);
    }
    
    /// <summary>
    /// Get time trials leaderboard
    /// </summary>
    public LeaderboardResponse<TimeTrialEntry> GetTimeTrials(
        string? routeId = null, string? category = null, int page = 1, int pageSize = 50, bool cleanOnly = false)
    {
        return _leaderboardService.GetTimeTrials(routeId, category, page, pageSize, cleanOnly);
    }
    
    /// <summary>
    /// Get route categories
    /// </summary>
    public List<string> GetRouteCategories()
    {
        return _leaderboardService.GetRouteCategories();
    }
    
    /// <summary>
    /// Get longest run leaderboard
    /// </summary>
    public LeaderboardResponse<LongestRunEntry> GetLongestRuns(
        int page = 1, int pageSize = 50, LeaderboardSortField sortBy = LeaderboardSortField.RaceCount)
    {
        return _leaderboardService.GetLongestRuns(page, pageSize, sortBy);
    }
    
    /// <summary>
    /// Get available time trial routes
    /// </summary>
    public List<TimeTrialRoute> GetRoutes()
    {
        return _leaderboardService.GetRoutes();
    }
    
    /// <summary>
    /// Get player summary across all leaderboards
    /// </summary>
    public PlayerLeaderboardSummary? GetPlayerSummary(string steamId)
    {
        return _leaderboardService.GetPlayerSummary(steamId);
    }
    
    /// <summary>
    /// Get player's current active run
    /// </summary>
    public ActiveRun? GetActiveRun(string steamId)
    {
        return _leaderboardService.GetActiveRun(steamId);
    }
    
    /// <summary>
    /// Get all active runs (for spectating/monitoring)
    /// </summary>
    public List<ActiveRun> GetAllActiveRuns()
    {
        return _leaderboardService.GetAllActiveRuns();
    }
    
    /// <summary>
    /// Get a player's rank entry
    /// </summary>
    public PlayerRankEntry? GetPlayerRank(string steamId)
    {
        return _leaderboardService.GetPlayerRank(steamId);
    }
    
    // ============================================================================
    // EVENT HANDLERS
    // ============================================================================
    
    private void OnClientConnected(ACTcpClient client, EventArgs args)
    {
        string steamId = client.Guid.ToString();
        string playerName = client.Name ?? "Unknown";
        string carModel = client.EntryCar.Model;
        
        // Create session
        _sessions[client.SessionId] = new PlayerSession
        {
            SteamId = steamId,
            PlayerName = playerName,
            SessionId = client.SessionId,
            CarModel = carModel,
            ConnectedAt = DateTime.UtcNow
        };
        
        // Ensure player has a rank entry
        var entry = _leaderboardService.GetOrCreatePlayerRank(steamId, playerName);
        entry.DriverLevel = _getDriverLevel?.Invoke(steamId) ?? 1;
        entry.PrestigeRank = _getPrestigeRank?.Invoke(steamId) ?? 0;
        entry.ClubTag = _getClubTag?.Invoke(steamId) ?? "";
        entry.LastSeen = DateTime.UtcNow;
        
        // Start active run tracking
        if (_config.EnableLongestRun)
        {
            _leaderboardService.GetOrStartActiveRun(steamId, playerName, client.SessionId, carModel);
        }
        
        Log.Debug("Player connected: {Name} ({SteamId})", playerName, steamId);
    }
    
    private void OnClientDisconnected(ACTcpClient client, EventArgs args)
    {
        if (!_sessions.TryRemove(client.SessionId, out var session))
            return;
        
        // End active run
        if (_config.EnableLongestRun)
        {
            EndPlayerRun(session.SteamId, RunEndReason.Disconnected);
        }
        
        Log.Debug("Player disconnected: {Name}", session.PlayerName);
    }
    
    private void OnPositionUpdate(EntryCar sender, in PositionUpdateIn update)
    {
        if (!_config.EnableLongestRun) return;
        
        if (_sessions.TryGetValue(sender.SessionId, out var session))
        {
            float speedKph = update.Velocity.Length() * 3.6f;
            
            _leaderboardService.UpdateRunPosition(
                session.SteamId,
                update.Position.X,
                update.Position.Y,
                update.Position.Z,
                speedKph);
            
            // Update top speed in player rank
            var entry = _leaderboardService.GetPlayerRank(session.SteamId);
            if (entry != null && speedKph > entry.TopSpeedKph)
            {
                entry.TopSpeedKph = speedKph;
            }
        }
    }
    
    private void OnReset(EntryCar sender, EventArgs args)
    {
        if (!_config.EnableLongestRun) return;
        
        if (_sessions.TryGetValue(sender.SessionId, out var session))
        {
            _leaderboardService.RecordRunCollision(session.SteamId, isReset: true);
        }
    }
    
    private void OnCollision(EntryCar sender, CollisionEventArgs args)
    {
        if (!_config.EnableLongestRun) return;
        
        if (_sessions.TryGetValue(sender.SessionId, out var session))
        {
            _leaderboardService.RecordRunCollision(session.SteamId, isReset: false);
        }
    }
    
    private void UpdateAllActiveRuns()
    {
        // Update average speeds based on recent data
        foreach (var session in _sessions.Values)
        {
            var entry = _leaderboardService.GetPlayerRank(session.SteamId);
            var run = _leaderboardService.GetActiveRun(session.SteamId);
            
            if (entry != null && run != null)
            {
                // Update running average speed
                entry.TotalDriveTimeSeconds += 1;
                
                if (entry.AvgSpeedKph == 0)
                    entry.AvgSpeedKph = run.AvgSpeedKph;
                else
                    entry.AvgSpeedKph = entry.AvgSpeedKph * 0.999f + run.AvgSpeedKph * 0.001f;
                
                entry.TotalDistanceKm += run.TotalDistanceKm;
            }
        }
    }
    
    public override void Dispose()
    {
        _leaderboardService.SaveAll();
        _leaderboardService.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Player session tracking
/// </summary>
internal class PlayerSession
{
    public string SteamId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public int SessionId { get; set; }
    public string CarModel { get; set; } = "";
    public DateTime ConnectedAt { get; set; }
}
