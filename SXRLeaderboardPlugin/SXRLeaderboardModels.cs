namespace SXRLeaderboardPlugin;

// ============================================================================
// LEADERBOARD TYPES
// ============================================================================

public enum LeaderboardType
{
    PlayerRanks,
    TimeTrials,
    LongestRun
}

public enum LeaderboardSortField
{
    // Player Ranks
    Rank,
    Wins,
    WinRate,
    EloRating,
    AvgSpeed,
    TotalRaces,
    
    // Time Trials
    Time,
    
    // Longest Run
    Distance,
    Duration,
    RaceCount
}

// ============================================================================
// PLAYER RANKS LEADERBOARD
// ============================================================================

/// <summary>
/// Player ranking entry - overall competitive standings
/// </summary>
public class PlayerRankEntry
{
    public string SteamId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public int Rank { get; set; } = 0;
    public int DriverLevel { get; set; } = 1;
    public int PrestigeRank { get; set; } = 0;
    
    // Racing stats
    public int TotalRaces { get; set; } = 0;
    public int Wins { get; set; } = 0;
    public int Losses { get; set; } = 0;
    public float WinRate => TotalRaces > 0 ? (float)Wins / TotalRaces * 100 : 0;
    
    // SP Battle stats
    public int SPBattleWins { get; set; } = 0;
    public int SPBattleLosses { get; set; } = 0;
    public int CurrentStreak { get; set; } = 0;
    public int BestStreak { get; set; } = 0;
    
    // Elo/Rating
    public int EloRating { get; set; } = 1000;
    public int PeakEloRating { get; set; } = 1000;
    
    // Favorite car (most used)
    public string FavoriteCar { get; set; } = "";
    public string FavoriteCarDisplayName { get; set; } = "";
    public int FavoriteCarRaces { get; set; } = 0;
    
    // Club
    public string ClubTag { get; set; } = "";
    public string ClubName { get; set; } = "";
    
    // Performance
    public float AvgSpeedKph { get; set; } = 0;
    public float TopSpeedKph { get; set; } = 0;
    public float TotalDistanceKm { get; set; } = 0;
    public double TotalDriveTimeSeconds { get; set; } = 0;
    
    // Timestamps
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public DateTime LastRaceTime { get; set; } = DateTime.MinValue;
    
    // Display helpers
    public string WinLossDisplay => $"{Wins}/{TotalRaces}";
    public string DriverLevelDisplay => PrestigeRank > 0 
        ? $"P{PrestigeRank} - {DriverLevel}" 
        : DriverLevel.ToString();
    public string AvgSpeedDisplay => $"{AvgSpeedKph:F1} km/h";
}

// ============================================================================
// TIME TRIALS LEADERBOARD
// ============================================================================

/// <summary>
/// Time trial route definition
/// </summary>
public class TimeTrialRoute
{
    public string RouteId { get; set; } = "";
    public string RouteName { get; set; } = "";
    public string Category { get; set; } = ""; // e.g., "C1 Inner", "Wangan", "Yokohane"
    public float DistanceKm { get; set; } = 0;
    public string Description { get; set; } = "";
    
    // Checkpoint positions (for validation)
    public List<RouteCheckpoint> Checkpoints { get; set; } = new();
    
    // Start/Finish zones
    public RouteZone StartZone { get; set; } = new();
    public RouteZone FinishZone { get; set; } = new();
    
    public bool IsActive { get; set; } = true;
}

public class RouteCheckpoint
{
    public int Order { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Radius { get; set; } = 50f;
}

public class RouteZone
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Radius { get; set; } = 30f;
}

/// <summary>
/// Time trial entry - fastest times on specific routes
/// </summary>
public class TimeTrialEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string SteamId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public int LeaderboardRank { get; set; } = 0;
    
    // Route info
    public string RouteId { get; set; } = "";
    public string RouteName { get; set; } = "";
    public string RouteCategory { get; set; } = "";
    public float RouteDistanceKm { get; set; } = 0;
    
    // Time
    public long TimeMs { get; set; } = 0;
    public TimeSpan Time => TimeSpan.FromMilliseconds(TimeMs);
    public string TimeDisplay => Time.ToString(@"mm\:ss\.fff");
    public float AvgSpeedKph { get; set; } = 0;
    public float TopSpeedKph { get; set; } = 0;
    
    // Car used
    public string CarModel { get; set; } = "";
    public string CarDisplayName { get; set; } = "";
    public string CarClass { get; set; } = "D";
    
    // Run quality
    public bool IsDirty { get; set; } = false;
    public int WallHits { get; set; } = 0;
    public int TrafficHits { get; set; } = 0;
    public int CutsDetected { get; set; } = 0;
    public string CleanDirtyDisplay => IsDirty ? "Dirty" : "Clean";
    
    // Validation
    public bool IsVerified { get; set; } = false;
    public bool IsWorldRecord { get; set; } = false;
    public bool IsPersonalBest { get; set; } = false;
    
    // Sector times (if tracked)
    public List<long> SectorTimesMs { get; set; } = new();
    
    // Timestamps
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    public string PostedAtDisplay => PostedAt.ToString("yyyy-MM-dd HH:mm");
}

// ============================================================================
// LONGEST RUN LEADERBOARD
// ============================================================================

/// <summary>
/// Longest run entry - races completed before pitting
/// </summary>
public class LongestRunEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string SteamId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public int LeaderboardRank { get; set; } = 0;
    
    // Run stats
    public int RaceCount { get; set; } = 0; // Number of SP Battles/races completed
    public int WinsInRun { get; set; } = 0;
    public int LossesInRun { get; set; } = 0;
    public float WinRateInRun => RaceCount > 0 ? (float)WinsInRun / RaceCount * 100 : 0;
    
    // Distance/Duration
    public float TotalDistanceKm { get; set; } = 0;
    public double DurationSeconds { get; set; } = 0;
    public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);
    public string DurationDisplay => Duration.ToString(@"hh\:mm\:ss");
    public string DistanceDisplay => $"{TotalDistanceKm:F1} km";
    
    // Performance
    public float AvgSpeedKph { get; set; } = 0;
    public float TopSpeedKph { get; set; } = 0;
    
    // Car used (primary car during run)
    public string PrimaryCar { get; set; } = "";
    public string PrimaryCarDisplayName { get; set; } = "";
    public List<string> CarsUsed { get; set; } = new(); // All cars used during run
    
    // Run quality
    public int TotalCollisions { get; set; } = 0;
    public int ResetCount { get; set; } = 0;
    
    // How the run ended
    public RunEndReason EndReason { get; set; } = RunEndReason.Pitted;
    
    // Timestamps
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime EndedAt { get; set; } = DateTime.UtcNow;
    public string PostedAtDisplay => EndedAt.ToString("yyyy-MM-dd HH:mm");
}

public enum RunEndReason
{
    Pitted,       // Normal end - player pitted
    Disconnected, // Player left server
    Crashed,      // Too many collisions/resets
    ServerRestart // Server was restarted
}

// ============================================================================
// ACTIVE RUN TRACKING
// ============================================================================

/// <summary>
/// Tracks an active run (before pitting)
/// </summary>
public class ActiveRun
{
    public string SteamId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public int SessionId { get; set; }
    
    // Current stats
    public int RaceCount { get; set; } = 0;
    public int WinsInRun { get; set; } = 0;
    public int LossesInRun { get; set; } = 0;
    public float TotalDistanceKm { get; set; } = 0;
    public float AvgSpeedKph { get; set; } = 0;
    public float TopSpeedKph { get; set; } = 0;
    public int TotalCollisions { get; set; } = 0;
    public int ResetCount { get; set; } = 0;
    
    // Car tracking
    public string CurrentCar { get; set; } = "";
    public Dictionary<string, int> CarUsage { get; set; } = new(); // CarModel -> races
    
    // Timing
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateAt { get; set; } = DateTime.UtcNow;
    
    // Position tracking for distance
    public float LastX { get; set; }
    public float LastY { get; set; }
    public float LastZ { get; set; }
    
    /// <summary>
    /// Get the primary car (most used during run)
    /// </summary>
    public string GetPrimaryCar()
    {
        if (CarUsage.Count == 0) return CurrentCar;
        return CarUsage.OrderByDescending(kvp => kvp.Value).First().Key;
    }
    
    /// <summary>
    /// Convert to leaderboard entry
    /// </summary>
    public LongestRunEntry ToEntry(RunEndReason endReason)
    {
        return new LongestRunEntry
        {
            SteamId = SteamId,
            PlayerName = PlayerName,
            RaceCount = RaceCount,
            WinsInRun = WinsInRun,
            LossesInRun = LossesInRun,
            TotalDistanceKm = TotalDistanceKm,
            DurationSeconds = (DateTime.UtcNow - StartedAt).TotalSeconds,
            AvgSpeedKph = AvgSpeedKph,
            TopSpeedKph = TopSpeedKph,
            PrimaryCar = GetPrimaryCar(),
            CarsUsed = CarUsage.Keys.ToList(),
            TotalCollisions = TotalCollisions,
            ResetCount = ResetCount,
            EndReason = endReason,
            StartedAt = StartedAt,
            EndedAt = DateTime.UtcNow
        };
    }
}

// ============================================================================
// API RESPONSE MODELS
// ============================================================================

public class LeaderboardResponse<T>
{
    public LeaderboardType Type { get; set; }
    public string TypeName => Type.ToString();
    public int TotalEntries { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalPages => (int)Math.Ceiling((double)TotalEntries / PageSize);
    public List<T> Entries { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class PlayerLeaderboardSummary
{
    public string SteamId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    
    // Player Ranks position
    public int OverallRank { get; set; }
    public int EloRating { get; set; }
    
    // Best time trial
    public string BestTimeTrialRoute { get; set; } = "";
    public int BestTimeTrialRank { get; set; }
    public string BestTimeTrialTime { get; set; } = "";
    
    // Longest run
    public int LongestRunRaces { get; set; }
    public int LongestRunRank { get; set; }
    
    // Current run (if active)
    public bool HasActiveRun { get; set; }
    public int CurrentRunRaces { get; set; }
}

// ============================================================================
// SUBMISSION MODELS
// ============================================================================

public class TimeTrialSubmission
{
    public string SteamId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string RouteId { get; set; } = "";
    public long TimeMs { get; set; }
    public string CarModel { get; set; } = "";
    public string CarClass { get; set; } = "D";
    public float AvgSpeedKph { get; set; }
    public float TopSpeedKph { get; set; }
    public int WallHits { get; set; }
    public int TrafficHits { get; set; }
    public int CutsDetected { get; set; }
    public List<long>? SectorTimesMs { get; set; }
}

public class RaceResultSubmission
{
    public string SteamId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public bool IsWin { get; set; }
    public string CarModel { get; set; } = "";
    public int EloChange { get; set; } = 0;
    public string OpponentSteamId { get; set; } = "";
}

public class RunEndSubmission
{
    public string SteamId { get; set; } = "";
    public RunEndReason Reason { get; set; } = RunEndReason.Pitted;
}

/// <summary>
/// Result of route CRUD operations
/// </summary>
public class RouteOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public TimeTrialRoute? Route { get; set; }
}
