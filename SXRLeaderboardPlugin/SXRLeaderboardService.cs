using System.Collections.Concurrent;
using System.Text.Json;
using Serilog;

namespace SXRLeaderboardPlugin;

/// <summary>
/// Service for managing all leaderboard data storage and retrieval
/// </summary>
public class SXRLeaderboardService : IDisposable
{
    private readonly SXRLeaderboardConfiguration _config;
    private readonly string _dataPath;
    
    // In-memory caches
    private readonly ConcurrentDictionary<string, PlayerRankEntry> _playerRanks = new();
    private readonly ConcurrentDictionary<string, List<TimeTrialEntry>> _timeTrialsByRoute = new();
    private readonly List<LongestRunEntry> _longestRuns = new();
    private readonly ConcurrentDictionary<string, ActiveRun> _activeRuns = new();
    
    // Route definitions
    private readonly Dictionary<string, TimeTrialRoute> _routes = new();
    
    // Locks for thread safety
    private readonly object _longestRunLock = new();
    private readonly object _saveLock = new();
    
    // Auto-save timer
    private Timer? _autoSaveTimer;
    private bool _isDirty = false;
    
    public SXRLeaderboardService(SXRLeaderboardConfiguration config)
    {
        _config = config;
        _dataPath = Path.Combine(
            Path.GetDirectoryName(typeof(SXRLeaderboardService).Assembly.Location) ?? "",
            "data");
        
        Directory.CreateDirectory(_dataPath);
        
        LoadAllData();
        LoadRoutes();
        
        // Auto-save every 5 minutes
        _autoSaveTimer = new Timer(_ => SaveIfDirty(), null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }
    
    // ============================================================================
    // PLAYER RANKS
    // ============================================================================
    
    /// <summary>
    /// Get or create a player rank entry
    /// </summary>
    public PlayerRankEntry GetOrCreatePlayerRank(string steamId, string playerName)
    {
        return _playerRanks.GetOrAdd(steamId, _ => new PlayerRankEntry
        {
            SteamId = steamId,
            PlayerName = playerName,
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
    }
    
    /// <summary>
    /// Update player rank entry
    /// </summary>
    public void UpdatePlayerRank(string steamId, Action<PlayerRankEntry> updater)
    {
        if (_playerRanks.TryGetValue(steamId, out var entry))
        {
            updater(entry);
            entry.LastSeen = DateTime.UtcNow;
            _isDirty = true;
        }
    }
    
    /// <summary>
    /// Record a race result
    /// </summary>
    public void RecordRaceResult(RaceResultSubmission result)
    {
        var entry = GetOrCreatePlayerRank(result.SteamId, result.PlayerName);
        
        entry.TotalRaces++;
        if (result.IsWin)
        {
            entry.Wins++;
            entry.SPBattleWins++;
            entry.CurrentStreak++;
            if (entry.CurrentStreak > entry.BestStreak)
                entry.BestStreak = entry.CurrentStreak;
        }
        else
        {
            entry.Losses++;
            entry.SPBattleLosses++;
            entry.CurrentStreak = 0;
        }
        
        // Update Elo
        entry.EloRating += result.EloChange;
        if (entry.EloRating > entry.PeakEloRating)
            entry.PeakEloRating = entry.EloRating;
        
        // Track favorite car
        TrackCarUsage(entry, result.CarModel);
        
        entry.LastRaceTime = DateTime.UtcNow;
        entry.LastSeen = DateTime.UtcNow;
        
        _isDirty = true;
        
        Log.Debug("Recorded race result for {Player}: {Result}", 
            result.PlayerName, result.IsWin ? "Win" : "Loss");
    }
    
    /// <summary>
    /// Get player ranks leaderboard
    /// </summary>
    public LeaderboardResponse<PlayerRankEntry> GetPlayerRanks(
        int page = 1, 
        int pageSize = 50,
        LeaderboardSortField sortBy = LeaderboardSortField.EloRating)
    {
        var sorted = sortBy switch
        {
            LeaderboardSortField.Wins => _playerRanks.Values
                .OrderByDescending(p => p.Wins)
                .ThenByDescending(p => p.WinRate),
            LeaderboardSortField.WinRate => _playerRanks.Values
                .Where(p => p.TotalRaces >= _config.MinRacesForRanking)
                .OrderByDescending(p => p.WinRate)
                .ThenByDescending(p => p.Wins),
            LeaderboardSortField.TotalRaces => _playerRanks.Values
                .OrderByDescending(p => p.TotalRaces),
            LeaderboardSortField.AvgSpeed => _playerRanks.Values
                .Where(p => p.AvgSpeedKph > 0)
                .OrderByDescending(p => p.AvgSpeedKph),
            _ => _playerRanks.Values
                .OrderByDescending(p => p.EloRating)
                .ThenByDescending(p => p.WinRate)
        };
        
        var entries = sorted.ToList();
        
        // Assign ranks
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].Rank = i + 1;
        }
        
        return new LeaderboardResponse<PlayerRankEntry>
        {
            Type = LeaderboardType.PlayerRanks,
            TotalEntries = entries.Count,
            Page = page,
            PageSize = pageSize,
            Entries = entries.Skip((page - 1) * pageSize).Take(pageSize).ToList()
        };
    }
    
    /// <summary>
    /// Get a specific player's rank
    /// </summary>
    public PlayerRankEntry? GetPlayerRank(string steamId)
    {
        return _playerRanks.TryGetValue(steamId, out var entry) ? entry : null;
    }
    
    private void TrackCarUsage(PlayerRankEntry entry, string carModel)
    {
        // Simple tracking - could be enhanced with Dictionary<string, int>
        entry.FavoriteCarRaces++;
        if (string.IsNullOrEmpty(entry.FavoriteCar) || carModel == entry.FavoriteCar)
        {
            entry.FavoriteCar = carModel;
        }
    }
    
    // ============================================================================
    // TIME TRIALS
    // ============================================================================
    
    /// <summary>
    /// Submit a time trial result
    /// </summary>
    public TimeTrialEntry? SubmitTimeTrial(TimeTrialSubmission submission)
    {
        if (!_routes.TryGetValue(submission.RouteId, out var route))
        {
            Log.Warning("Unknown route: {RouteId}", submission.RouteId);
            return null;
        }
        
        var entry = new TimeTrialEntry
        {
            SteamId = submission.SteamId,
            PlayerName = submission.PlayerName,
            RouteId = submission.RouteId,
            RouteName = route.RouteName,
            RouteCategory = route.Category,
            RouteDistanceKm = route.DistanceKm,
            TimeMs = submission.TimeMs,
            CarModel = submission.CarModel,
            CarClass = submission.CarClass,
            AvgSpeedKph = submission.AvgSpeedKph,
            TopSpeedKph = submission.TopSpeedKph,
            WallHits = submission.WallHits,
            TrafficHits = submission.TrafficHits,
            CutsDetected = submission.CutsDetected,
            IsDirty = submission.WallHits > 0 || submission.TrafficHits > 0 || submission.CutsDetected > 0,
            SectorTimesMs = submission.SectorTimesMs ?? new(),
            PostedAt = DateTime.UtcNow
        };
        
        // Get or create route entries list
        var routeEntries = _timeTrialsByRoute.GetOrAdd(submission.RouteId, _ => new List<TimeTrialEntry>());
        
        // Check if this is a personal best
        var existingPb = routeEntries.FirstOrDefault(e => e.SteamId == submission.SteamId);
        if (existingPb == null || entry.TimeMs < existingPb.TimeMs)
        {
            entry.IsPersonalBest = true;
            
            // Remove old PB if exists
            if (existingPb != null)
            {
                routeEntries.Remove(existingPb);
            }
            
            routeEntries.Add(entry);
            
            // Re-sort and assign ranks
            var sorted = routeEntries.OrderBy(e => e.TimeMs).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].LeaderboardRank = i + 1;
                sorted[i].IsWorldRecord = i == 0;
            }
            
            _timeTrialsByRoute[submission.RouteId] = sorted;
            _isDirty = true;
            
            Log.Information("New time trial PB: {Player} on {Route} - {Time}", 
                submission.PlayerName, route.RouteName, entry.TimeDisplay);
            
            return entry;
        }
        
        return null; // Not a PB
    }
    
    /// <summary>
    /// Clean time threshold - clean times within this of a dirty time rank higher
    /// </summary>
    private const long CleanTimeThresholdMs = 120000; // 2 minutes
    
    /// <summary>
    /// Get time trials for a specific route
    /// </summary>
    public LeaderboardResponse<TimeTrialEntry> GetTimeTrials(
        string? routeId = null,
        string? category = null,
        int page = 1,
        int pageSize = 50,
        bool cleanOnly = false)
    {
        List<TimeTrialEntry> entries;
        
        if (!string.IsNullOrEmpty(routeId))
        {
            // Single route
            entries = _timeTrialsByRoute.TryGetValue(routeId, out var routeEntries) 
                ? routeEntries.ToList() 
                : new();
        }
        else if (!string.IsNullOrEmpty(category))
        {
            // Filter by category (e.g., "C1", "Wangan", "Yokohane")
            var categoryRoutes = _routes.Values
                .Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.RouteId)
                .ToHashSet();
            
            entries = _timeTrialsByRoute
                .Where(kvp => categoryRoutes.Contains(kvp.Key))
                .SelectMany(kvp => kvp.Value)
                .ToList();
        }
        else
        {
            // All routes
            entries = _timeTrialsByRoute.Values
                .SelectMany(e => e)
                .ToList();
        }
        
        if (cleanOnly)
        {
            entries = entries.Where(e => !e.IsDirty).ToList();
        }
        
        // Sort with clean time priority:
        // Clean times within 2 minutes of a dirty time should rank higher
        entries = SortTimeTrialsWithCleanPriority(entries);
        
        // Assign ranks
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].LeaderboardRank = i + 1;
        }
        
        return new LeaderboardResponse<TimeTrialEntry>
        {
            Type = LeaderboardType.TimeTrials,
            TotalEntries = entries.Count,
            Page = page,
            PageSize = pageSize,
            Entries = entries.Skip((page - 1) * pageSize).Take(pageSize).ToList()
        };
    }
    
    /// <summary>
    /// Sort time trials with clean time priority
    /// Clean times within 2 minutes of a dirty time rank higher than the dirty time
    /// </summary>
    private List<TimeTrialEntry> SortTimeTrialsWithCleanPriority(List<TimeTrialEntry> entries)
    {
        // Custom comparer that considers clean/dirty status
        return entries.OrderBy(e => GetEffectiveTimeForRanking(e, entries)).ToList();
    }
    
    /// <summary>
    /// Calculate effective time for ranking purposes
    /// Clean times get a bonus if they're within threshold of faster dirty times
    /// </summary>
    private long GetEffectiveTimeForRanking(TimeTrialEntry entry, List<TimeTrialEntry> allEntries)
    {
        if (entry.IsDirty)
        {
            // For dirty times, check if there's a clean time within threshold
            // If so, the dirty time gets penalized
            var cleanTimesWithinThreshold = allEntries
                .Where(e => !e.IsDirty && 
                           e.RouteId == entry.RouteId &&
                           e.TimeMs <= entry.TimeMs + CleanTimeThresholdMs &&
                           e.TimeMs >= entry.TimeMs)
                .Any();
            
            if (cleanTimesWithinThreshold)
            {
                // Penalize dirty time - add threshold to push it below clean times
                return entry.TimeMs + CleanTimeThresholdMs;
            }
            
            return entry.TimeMs;
        }
        else
        {
            // Clean time - check if it's within threshold of any faster dirty time
            var fasterDirtyTime = allEntries
                .Where(e => e.IsDirty && 
                           e.RouteId == entry.RouteId &&
                           e.TimeMs < entry.TimeMs)
                .OrderBy(e => e.TimeMs)
                .FirstOrDefault();
            
            if (fasterDirtyTime != null && entry.TimeMs - fasterDirtyTime.TimeMs <= CleanTimeThresholdMs)
            {
                // Clean time is within threshold of dirty time - rank it higher
                // Use dirty time - 1ms to ensure clean ranks above
                return fasterDirtyTime.TimeMs - 1;
            }
            
            return entry.TimeMs;
        }
    }
    
    /// <summary>
    /// Get all unique route categories
    /// </summary>
    public List<string> GetRouteCategories()
    {
        return _routes.Values
            .Where(r => r.IsActive)
            .Select(r => r.Category)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }
    
    /// <summary>
    /// Get a specific route by ID
    /// </summary>
    public TimeTrialRoute? GetRoute(string routeId)
    {
        return _routes.TryGetValue(routeId, out var route) ? route : null;
    }
    
    /// <summary>
    /// Create a new route
    /// </summary>
    public RouteOperationResult CreateRoute(TimeTrialRoute route)
    {
        if (string.IsNullOrEmpty(route.RouteId))
        {
            return new RouteOperationResult 
            { 
                Success = false, 
                Message = "Route ID is required" 
            };
        }
        
        if (_routes.ContainsKey(route.RouteId))
        {
            return new RouteOperationResult 
            { 
                Success = false, 
                Message = "Route ID already exists" 
            };
        }
        
        _routes[route.RouteId] = route;
        SaveRoutes();
        
        Log.Information("Created new route: {RouteId} - {RouteName}", route.RouteId, route.RouteName);
        
        return new RouteOperationResult 
        { 
            Success = true, 
            Message = "Route created successfully",
            Route = route
        };
    }
    
    /// <summary>
    /// Update an existing route
    /// </summary>
    public RouteOperationResult UpdateRoute(TimeTrialRoute route)
    {
        if (string.IsNullOrEmpty(route.RouteId))
        {
            return new RouteOperationResult 
            { 
                Success = false, 
                Message = "Route ID is required" 
            };
        }
        
        if (!_routes.ContainsKey(route.RouteId))
        {
            return new RouteOperationResult 
            { 
                Success = false, 
                Message = "Route not found" 
            };
        }
        
        _routes[route.RouteId] = route;
        SaveRoutes();
        
        Log.Information("Updated route: {RouteId} - {RouteName}", route.RouteId, route.RouteName);
        
        return new RouteOperationResult 
        { 
            Success = true, 
            Message = "Route updated successfully",
            Route = route
        };
    }
    
    /// <summary>
    /// Delete a route
    /// </summary>
    public RouteOperationResult DeleteRoute(string routeId)
    {
        if (!_routes.ContainsKey(routeId))
        {
            return new RouteOperationResult 
            { 
                Success = false, 
                Message = "Route not found" 
            };
        }
        
        _routes.Remove(routeId);
        
        // Also remove time trials for this route
        _timeTrialsByRoute.TryRemove(routeId, out _);
        
        SaveRoutes();
        _isDirty = true;
        
        Log.Information("Deleted route: {RouteId}", routeId);
        
        return new RouteOperationResult 
        { 
            Success = true, 
            Message = "Route deleted successfully"
        };
    }
    
    /// <summary>
    /// Get all available routes
    /// </summary>
    public List<TimeTrialRoute> GetRoutes()
    {
        return _routes.Values.Where(r => r.IsActive).ToList();
    }
    
    /// <summary>
    /// Get player's best times on all routes
    /// </summary>
    public List<TimeTrialEntry> GetPlayerBestTimes(string steamId)
    {
        return _timeTrialsByRoute.Values
            .SelectMany(e => e)
            .Where(e => e.SteamId == steamId)
            .ToList();
    }
    
    // ============================================================================
    // LONGEST RUN
    // ============================================================================
    
    /// <summary>
    /// Start or get an active run for a player
    /// </summary>
    public ActiveRun GetOrStartActiveRun(string steamId, string playerName, int sessionId, string carModel)
    {
        return _activeRuns.GetOrAdd(steamId, _ => new ActiveRun
        {
            SteamId = steamId,
            PlayerName = playerName,
            SessionId = sessionId,
            CurrentCar = carModel,
            StartedAt = DateTime.UtcNow,
            LastUpdateAt = DateTime.UtcNow
        });
    }
    
    /// <summary>
    /// Update an active run with race results
    /// </summary>
    public void UpdateActiveRun(string steamId, bool isWin, string carModel)
    {
        if (_activeRuns.TryGetValue(steamId, out var run))
        {
            run.RaceCount++;
            if (isWin) run.WinsInRun++;
            else run.LossesInRun++;
            
            run.CurrentCar = carModel;
            if (!run.CarUsage.ContainsKey(carModel))
                run.CarUsage[carModel] = 0;
            run.CarUsage[carModel]++;
            
            run.LastUpdateAt = DateTime.UtcNow;
            
            Log.Debug("Updated active run for {Player}: {Races} races", 
                run.PlayerName, run.RaceCount);
        }
    }
    
    /// <summary>
    /// Update run position data (for distance tracking)
    /// </summary>
    public void UpdateRunPosition(string steamId, float x, float y, float z, float speedKph)
    {
        if (_activeRuns.TryGetValue(steamId, out var run))
        {
            // Calculate distance from last position
            if (run.LastX != 0 || run.LastZ != 0)
            {
                float dx = x - run.LastX;
                float dy = y - run.LastY;
                float dz = z - run.LastZ;
                float distM = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
                
                // Only count reasonable movements (not teleports)
                if (distM < 100f && distM > 0.1f)
                {
                    run.TotalDistanceKm += distM / 1000f;
                }
            }
            
            run.LastX = x;
            run.LastY = y;
            run.LastZ = z;
            
            // Update speeds
            if (speedKph > run.TopSpeedKph)
                run.TopSpeedKph = speedKph;
            
            // Running average
            if (run.AvgSpeedKph == 0)
                run.AvgSpeedKph = speedKph;
            else
                run.AvgSpeedKph = run.AvgSpeedKph * 0.99f + speedKph * 0.01f;
            
            run.LastUpdateAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Record a collision/reset
    /// </summary>
    public void RecordRunCollision(string steamId, bool isReset = false)
    {
        if (_activeRuns.TryGetValue(steamId, out var run))
        {
            if (isReset)
                run.ResetCount++;
            else
                run.TotalCollisions++;
        }
    }
    
    /// <summary>
    /// End an active run (player pitted or disconnected)
    /// </summary>
    public LongestRunEntry? EndActiveRun(string steamId, RunEndReason reason)
    {
        if (!_activeRuns.TryRemove(steamId, out var run))
            return null;
        
        // Only record runs with at least 1 race
        if (run.RaceCount < _config.MinRacesForLongestRun)
        {
            Log.Debug("Run too short to record: {Races} races", run.RaceCount);
            return null;
        }
        
        var entry = run.ToEntry(reason);
        
        lock (_longestRunLock)
        {
            _longestRuns.Add(entry);
            
            // Sort and assign ranks
            var sorted = _longestRuns
                .OrderByDescending(e => e.RaceCount)
                .ThenByDescending(e => e.WinRateInRun)
                .ToList();
            
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].LeaderboardRank = i + 1;
            }
            
            // Keep only top N entries
            if (_longestRuns.Count > _config.MaxLongestRunEntries)
            {
                _longestRuns.RemoveRange(_config.MaxLongestRunEntries, 
                    _longestRuns.Count - _config.MaxLongestRunEntries);
            }
        }
        
        _isDirty = true;
        
        Log.Information("Recorded longest run: {Player} - {Races} races, {Distance:F1} km", 
            entry.PlayerName, entry.RaceCount, entry.TotalDistanceKm);
        
        return entry;
    }
    
    /// <summary>
    /// Get active run for a player
    /// </summary>
    public ActiveRun? GetActiveRun(string steamId)
    {
        return _activeRuns.TryGetValue(steamId, out var run) ? run : null;
    }
    
    /// <summary>
    /// Get all active runs
    /// </summary>
    public List<ActiveRun> GetAllActiveRuns()
    {
        return _activeRuns.Values.ToList();
    }
    
    /// <summary>
    /// Get longest run leaderboard
    /// </summary>
    public LeaderboardResponse<LongestRunEntry> GetLongestRuns(
        int page = 1,
        int pageSize = 50,
        LeaderboardSortField sortBy = LeaderboardSortField.RaceCount)
    {
        List<LongestRunEntry> sorted;
        
        lock (_longestRunLock)
        {
            sorted = sortBy switch
            {
                LeaderboardSortField.Distance => _longestRuns
                    .OrderByDescending(e => e.TotalDistanceKm).ToList(),
                LeaderboardSortField.Duration => _longestRuns
                    .OrderByDescending(e => e.DurationSeconds).ToList(),
                _ => _longestRuns
                    .OrderByDescending(e => e.RaceCount)
                    .ThenByDescending(e => e.WinRateInRun).ToList()
            };
        }
        
        // Re-assign ranks
        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].LeaderboardRank = i + 1;
        }
        
        return new LeaderboardResponse<LongestRunEntry>
        {
            Type = LeaderboardType.LongestRun,
            TotalEntries = sorted.Count,
            Page = page,
            PageSize = pageSize,
            Entries = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList()
        };
    }
    
    /// <summary>
    /// Get player's best longest run
    /// </summary>
    public LongestRunEntry? GetPlayerBestRun(string steamId)
    {
        lock (_longestRunLock)
        {
            return _longestRuns
                .Where(e => e.SteamId == steamId)
                .OrderByDescending(e => e.RaceCount)
                .FirstOrDefault();
        }
    }
    
    // ============================================================================
    // PLAYER SUMMARY
    // ============================================================================
    
    /// <summary>
    /// Get a player's summary across all leaderboards
    /// </summary>
    public PlayerLeaderboardSummary? GetPlayerSummary(string steamId)
    {
        var rank = GetPlayerRank(steamId);
        if (rank == null) return null;
        
        var bestTimes = GetPlayerBestTimes(steamId);
        var bestRun = GetPlayerBestRun(steamId);
        var activeRun = GetActiveRun(steamId);
        
        var bestTimeTrial = bestTimes.OrderBy(t => t.LeaderboardRank).FirstOrDefault();
        
        return new PlayerLeaderboardSummary
        {
            SteamId = steamId,
            PlayerName = rank.PlayerName,
            OverallRank = rank.Rank,
            EloRating = rank.EloRating,
            BestTimeTrialRoute = bestTimeTrial?.RouteName ?? "",
            BestTimeTrialRank = bestTimeTrial?.LeaderboardRank ?? 0,
            BestTimeTrialTime = bestTimeTrial?.TimeDisplay ?? "",
            LongestRunRaces = bestRun?.RaceCount ?? 0,
            LongestRunRank = bestRun?.LeaderboardRank ?? 0,
            HasActiveRun = activeRun != null,
            CurrentRunRaces = activeRun?.RaceCount ?? 0
        };
    }
    
    // ============================================================================
    // DATA PERSISTENCE
    // ============================================================================
    
    private void LoadAllData()
    {
        try
        {
            // Load player ranks
            string playerRanksPath = Path.Combine(_dataPath, "player_ranks.json");
            if (File.Exists(playerRanksPath))
            {
                var json = File.ReadAllText(playerRanksPath);
                var data = JsonSerializer.Deserialize<List<PlayerRankEntry>>(json);
                if (data != null)
                {
                    foreach (var entry in data)
                    {
                        _playerRanks[entry.SteamId] = entry;
                    }
                }
                Log.Information("Loaded {Count} player rank entries", _playerRanks.Count);
            }
            
            // Load time trials
            string timeTrialsPath = Path.Combine(_dataPath, "time_trials.json");
            if (File.Exists(timeTrialsPath))
            {
                var json = File.ReadAllText(timeTrialsPath);
                var data = JsonSerializer.Deserialize<Dictionary<string, List<TimeTrialEntry>>>(json);
                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        _timeTrialsByRoute[kvp.Key] = kvp.Value;
                    }
                }
                Log.Information("Loaded time trials for {Count} routes", _timeTrialsByRoute.Count);
            }
            
            // Load longest runs
            string longestRunsPath = Path.Combine(_dataPath, "longest_runs.json");
            if (File.Exists(longestRunsPath))
            {
                var json = File.ReadAllText(longestRunsPath);
                var data = JsonSerializer.Deserialize<List<LongestRunEntry>>(json);
                if (data != null)
                {
                    lock (_longestRunLock)
                    {
                        _longestRuns.AddRange(data);
                    }
                }
                Log.Information("Loaded {Count} longest run entries", _longestRuns.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load leaderboard data");
        }
    }
    
    private void LoadRoutes()
    {
        try
        {
            string routesPath = Path.Combine(
                Path.GetDirectoryName(typeof(SXRLeaderboardService).Assembly.Location) ?? "",
                "cfg", "routes.json");
            
            if (File.Exists(routesPath))
            {
                var json = File.ReadAllText(routesPath);
                var routes = JsonSerializer.Deserialize<List<TimeTrialRoute>>(json);
                if (routes != null)
                {
                    foreach (var route in routes)
                    {
                        _routes[route.RouteId] = route;
                    }
                }
                Log.Information("Loaded {Count} time trial routes", _routes.Count);
            }
            else
            {
                // Create default routes
                CreateDefaultRoutes();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load routes");
            CreateDefaultRoutes();
        }
    }
    
    private void CreateDefaultRoutes()
    {
        var defaultRoutes = new List<TimeTrialRoute>
        {
            new() { RouteId = "c1_inner", RouteName = "C1 Inner Loop", Category = "C1", DistanceKm = 14.8f },
            new() { RouteId = "c1_outer", RouteName = "C1 Outer Loop", Category = "C1", DistanceKm = 15.2f },
            new() { RouteId = "wangan_east", RouteName = "Wangan Eastbound", Category = "Wangan", DistanceKm = 12.5f },
            new() { RouteId = "wangan_west", RouteName = "Wangan Westbound", Category = "Wangan", DistanceKm = 12.5f },
            new() { RouteId = "yokohane_up", RouteName = "Yokohane Upline", Category = "Yokohane", DistanceKm = 8.3f },
            new() { RouteId = "yokohane_down", RouteName = "Yokohane Downline", Category = "Yokohane", DistanceKm = 8.3f },
            new() { RouteId = "shibuya_loop", RouteName = "Shibuya Loop", Category = "Shibuya", DistanceKm = 5.2f },
            new() { RouteId = "full_loop", RouteName = "Full Shuto Loop", Category = "Full", DistanceKm = 45.0f }
        };
        
        foreach (var route in defaultRoutes)
        {
            _routes[route.RouteId] = route;
        }
        
        // Save default routes
        SaveRoutes();
        Log.Information("Created {Count} default routes", defaultRoutes.Count);
    }
    
    private void SaveRoutes()
    {
        try
        {
            string routesPath = Path.Combine(
                Path.GetDirectoryName(typeof(SXRLeaderboardService).Assembly.Location) ?? "",
                "cfg", "routes.json");
            
            var json = JsonSerializer.Serialize(_routes.Values.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(routesPath, json);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save routes");
        }
    }
    
    public void SaveAll()
    {
        lock (_saveLock)
        {
            try
            {
                // Save player ranks
                var playerRanksJson = JsonSerializer.Serialize(_playerRanks.Values.ToList(), 
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(_dataPath, "player_ranks.json"), playerRanksJson);
                
                // Save time trials
                var timeTrialsJson = JsonSerializer.Serialize(
                    _timeTrialsByRoute.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(_dataPath, "time_trials.json"), timeTrialsJson);
                
                // Save longest runs
                List<LongestRunEntry> runsToSave;
                lock (_longestRunLock)
                {
                    runsToSave = _longestRuns.ToList();
                }
                var longestRunsJson = JsonSerializer.Serialize(runsToSave,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(_dataPath, "longest_runs.json"), longestRunsJson);
                
                _isDirty = false;
                Log.Debug("Saved all leaderboard data");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save leaderboard data");
            }
        }
    }
    
    private void SaveIfDirty()
    {
        if (_isDirty)
        {
            SaveAll();
        }
    }
    
    public void Dispose()
    {
        _autoSaveTimer?.Dispose();
        SaveAll();
    }
}
