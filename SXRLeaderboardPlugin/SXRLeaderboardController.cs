using Microsoft.AspNetCore.Mvc;

namespace SXRLeaderboardPlugin;

/// <summary>
/// HTTP API Controller for Leaderboards
/// </summary>
[ApiController]
[Route("sxrleaderboards")]
public class SXRLeaderboardController : ControllerBase
{
    private readonly SXRLeaderboardPlugin _plugin;
    private readonly SXRLeaderboardService _service;
    private readonly SXRLeaderboardConfiguration _config;
    
    public SXRLeaderboardController(
        SXRLeaderboardPlugin plugin,
        SXRLeaderboardService service,
        SXRLeaderboardConfiguration config)
    {
        _plugin = plugin;
        _service = service;
        _config = config;
    }
    
    // ============================================================================
    // PLAYER RANKS
    // ============================================================================
    
    /// <summary>
    /// Get player ranks leaderboard
    /// </summary>
    [HttpGet("ranks")]
    public ActionResult<LeaderboardResponse<PlayerRankEntry>> GetPlayerRanks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "elo")
    {
        if (!_config.EnableHttpApi) return NotFound();
        
        pageSize = Math.Min(pageSize, _config.MaxPageSize);
        var sort = ParseSortField(sortBy, LeaderboardSortField.EloRating);
        
        return _plugin.GetPlayerRanks(page, pageSize, sort);
    }
    
    /// <summary>
    /// Get a specific player's rank
    /// </summary>
    [HttpGet("ranks/{steamId}")]
    public ActionResult<PlayerRankEntry> GetPlayerRank(string steamId)
    {
        if (!_config.EnableHttpApi) return NotFound();
        
        var entry = _plugin.GetPlayerRank(steamId);
        if (entry == null) return NotFound();
        
        return entry;
    }
    
    /// <summary>
    /// Get player summary across all leaderboards
    /// </summary>
    [HttpGet("summary/{steamId}")]
    public ActionResult<PlayerLeaderboardSummary> GetPlayerSummary(string steamId)
    {
        if (!_config.EnableHttpApi) return NotFound();
        
        var summary = _plugin.GetPlayerSummary(steamId);
        if (summary == null) return NotFound();
        
        return summary;
    }
    
    // ============================================================================
    // TIME TRIALS
    // ============================================================================
    
    /// <summary>
    /// Get time trials leaderboard
    /// </summary>
    [HttpGet("timetrials")]
    public ActionResult<LeaderboardResponse<TimeTrialEntry>> GetTimeTrials(
        [FromQuery] string? route = null,
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool cleanOnly = false)
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableTimeTrials) return NotFound("Time trials not enabled");
        
        pageSize = Math.Min(pageSize, _config.MaxPageSize);
        
        return _service.GetTimeTrials(route, category, page, pageSize, cleanOnly);
    }
    
    /// <summary>
    /// Get available routes
    /// </summary>
    [HttpGet("routes")]
    public ActionResult<List<TimeTrialRoute>> GetRoutes()
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableTimeTrials) return NotFound("Time trials not enabled");
        
        return _service.GetRoutes();
    }
    
    /// <summary>
    /// Get a specific route
    /// </summary>
    [HttpGet("routes/{routeId}")]
    public ActionResult<TimeTrialRoute> GetRoute(string routeId)
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableTimeTrials) return NotFound("Time trials not enabled");
        
        var route = _service.GetRoute(routeId);
        if (route == null) return NotFound("Route not found");
        
        return route;
    }
    
    /// <summary>
    /// Create a new route (admin only)
    /// </summary>
    [HttpPost("routes")]
    public ActionResult<RouteOperationResult> CreateRoute(
        [FromQuery] string adminSteamId,
        [FromBody] TimeTrialRoute route)
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableTimeTrials) return NotFound("Time trials not enabled");
        
        if (string.IsNullOrEmpty(adminSteamId))
            return BadRequest("adminSteamId is required");
        
        // TODO: Validate admin status
        
        var result = _service.CreateRoute(route);
        return result;
    }
    
    /// <summary>
    /// Update an existing route (admin only)
    /// </summary>
    [HttpPut("routes/{routeId}")]
    public ActionResult<RouteOperationResult> UpdateRoute(
        string routeId,
        [FromQuery] string adminSteamId,
        [FromBody] TimeTrialRoute route)
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableTimeTrials) return NotFound("Time trials not enabled");
        
        if (string.IsNullOrEmpty(adminSteamId))
            return BadRequest("adminSteamId is required");
        
        route.RouteId = routeId;
        var result = _service.UpdateRoute(route);
        return result;
    }
    
    /// <summary>
    /// Delete a route (admin only)
    /// </summary>
    [HttpDelete("routes/{routeId}")]
    public ActionResult<RouteOperationResult> DeleteRoute(
        string routeId,
        [FromQuery] string adminSteamId)
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableTimeTrials) return NotFound("Time trials not enabled");
        
        if (string.IsNullOrEmpty(adminSteamId))
            return BadRequest("adminSteamId is required");
        
        var result = _service.DeleteRoute(routeId);
        return result;
    }
    
    /// <summary>
    /// Get available route categories
    /// </summary>
    [HttpGet("categories")]
    public ActionResult<List<string>> GetCategories()
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableTimeTrials) return NotFound("Time trials not enabled");
        
        return _service.GetRouteCategories();
    }
    
    /// <summary>
    /// Get player's best times on all routes
    /// </summary>
    [HttpGet("timetrials/player/{steamId}")]
    public ActionResult<List<TimeTrialEntry>> GetPlayerBestTimes(string steamId)
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableTimeTrials) return NotFound("Time trials not enabled");
        
        return _service.GetPlayerBestTimes(steamId);
    }
    
    /// <summary>
    /// Submit a time trial result
    /// </summary>
    [HttpPost("timetrials")]
    public ActionResult<TimeTrialEntry> SubmitTimeTrial([FromBody] TimeTrialSubmission submission)
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableTimeTrials) return NotFound("Time trials not enabled");
        
        var entry = _plugin.SubmitTimeTrial(submission);
        if (entry == null)
            return BadRequest("Not a personal best or invalid route");
        
        return entry;
    }
    
    // ============================================================================
    // LONGEST RUN
    // ============================================================================
    
    /// <summary>
    /// Get longest run leaderboard
    /// </summary>
    [HttpGet("longestruns")]
    public ActionResult<LeaderboardResponse<LongestRunEntry>> GetLongestRuns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "races")
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableLongestRun) return NotFound("Longest run not enabled");
        
        pageSize = Math.Min(pageSize, _config.MaxPageSize);
        var sort = ParseSortField(sortBy, LeaderboardSortField.RaceCount);
        
        return _plugin.GetLongestRuns(page, pageSize, sort);
    }
    
    /// <summary>
    /// Get player's best longest run
    /// </summary>
    [HttpGet("longestruns/player/{steamId}")]
    public ActionResult<LongestRunEntry> GetPlayerBestRun(string steamId)
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableLongestRun) return NotFound("Longest run not enabled");
        
        var entry = _service.GetPlayerBestRun(steamId);
        if (entry == null) return NotFound();
        
        return entry;
    }
    
    /// <summary>
    /// Get all active runs
    /// </summary>
    [HttpGet("activeruns")]
    public ActionResult<List<ActiveRun>> GetActiveRuns()
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableLongestRun) return NotFound("Longest run not enabled");
        
        return _plugin.GetAllActiveRuns();
    }
    
    /// <summary>
    /// Get a player's active run
    /// </summary>
    [HttpGet("activeruns/{steamId}")]
    public ActionResult<ActiveRun> GetActiveRun(string steamId)
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableLongestRun) return NotFound("Longest run not enabled");
        
        var run = _plugin.GetActiveRun(steamId);
        if (run == null) return NotFound();
        
        return run;
    }
    
    /// <summary>
    /// End a player's run (pit detection)
    /// </summary>
    [HttpPost("endrun/{steamId}")]
    public ActionResult<LongestRunEntry> EndRun(string steamId, [FromBody] RunEndSubmission? submission = null)
    {
        if (!_config.EnableHttpApi) return NotFound();
        if (!_config.EnableLongestRun) return NotFound("Longest run not enabled");
        
        var reason = submission?.Reason ?? RunEndReason.Pitted;
        var entry = _plugin.EndPlayerRun(steamId, reason);
        
        if (entry == null)
            return BadRequest("No active run or run too short");
        
        return entry;
    }
    
    // ============================================================================
    // LEADERBOARD DATA (for Lua UI)
    // ============================================================================
    
    /// <summary>
    /// Get all leaderboard data for Lua UI
    /// </summary>
    [HttpGet("data")]
    public ActionResult<LeaderboardDataResponse> GetLeaderboardData(
        [FromQuery] LeaderboardType type = LeaderboardType.PlayerRanks,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? route = null,
        [FromQuery] string? category = null)
    {
        if (!_config.EnableHttpApi) return NotFound();
        
        pageSize = Math.Min(pageSize, _config.MaxPageSize);
        
        return new LeaderboardDataResponse
        {
            Type = type,
            PlayerRanks = type == LeaderboardType.PlayerRanks 
                ? _plugin.GetPlayerRanks(page, pageSize) : null,
            TimeTrials = type == LeaderboardType.TimeTrials && _config.EnableTimeTrials
                ? _service.GetTimeTrials(route, category, page, pageSize) : null,
            LongestRuns = type == LeaderboardType.LongestRun && _config.EnableLongestRun
                ? _plugin.GetLongestRuns(page, pageSize) : null,
            Routes = _config.EnableTimeTrials ? _service.GetRoutes() : new(),
            Categories = _config.EnableTimeTrials ? _service.GetRouteCategories() : new()
        };
    }
    
    // ============================================================================
    // HELPERS
    // ============================================================================
    
    private LeaderboardSortField ParseSortField(string input, LeaderboardSortField defaultValue)
    {
        return input.ToLower() switch
        {
            "elo" or "rating" => LeaderboardSortField.EloRating,
            "wins" => LeaderboardSortField.Wins,
            "winrate" or "win_rate" => LeaderboardSortField.WinRate,
            "races" or "total" => LeaderboardSortField.TotalRaces,
            "speed" or "avgspeed" => LeaderboardSortField.AvgSpeed,
            "time" => LeaderboardSortField.Time,
            "distance" => LeaderboardSortField.Distance,
            "duration" => LeaderboardSortField.Duration,
            "racecount" => LeaderboardSortField.RaceCount,
            _ => defaultValue
        };
    }
}

/// <summary>
/// Combined leaderboard data response (for Lua UI efficiency)
/// </summary>
public class LeaderboardDataResponse
{
    public LeaderboardType Type { get; set; }
    public LeaderboardResponse<PlayerRankEntry>? PlayerRanks { get; set; }
    public LeaderboardResponse<TimeTrialEntry>? TimeTrials { get; set; }
    public LeaderboardResponse<LongestRunEntry>? LongestRuns { get; set; }
    public List<TimeTrialRoute> Routes { get; set; } = new();
    public List<string> Categories { get; set; } = new();
}
