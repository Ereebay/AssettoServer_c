using AssettoServer.Server.Configuration;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace SXRLeaderboardPlugin;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public class SXRLeaderboardConfiguration : IValidateConfiguration<SXRLeaderboardConfigurationValidator>
{
    /// <summary>
    /// Enable/disable the leaderboard plugin
    /// </summary>
    public bool Enabled { get; init; } = true;
    
    /// <summary>
    /// Enable Lua UI for in-game leaderboard display
    /// </summary>
    public bool EnableLuaUI { get; init; } = true;
    
    // ============================================================================
    // FEATURE TOGGLES
    // ============================================================================
    
    /// <summary>
    /// Enable time trial leaderboards
    /// </summary>
    public bool EnableTimeTrials { get; init; } = true;
    
    /// <summary>
    /// Enable longest run (endurance) tracking
    /// </summary>
    public bool EnableLongestRun { get; init; } = true;
    
    // ============================================================================
    // PLAYER RANKS
    // ============================================================================
    
    /// <summary>
    /// Minimum races to appear in ranked leaderboards
    /// </summary>
    public int MinRacesForRanking { get; init; } = 5;
    
    /// <summary>
    /// Starting Elo rating for new players
    /// </summary>
    public int StartingElo { get; init; } = 1000;
    
    /// <summary>
    /// Elo K-factor for rating calculations
    /// </summary>
    public int EloKFactor { get; init; } = 32;
    
    // ============================================================================
    // TIME TRIALS
    // ============================================================================
    
    /// <summary>
    /// Maximum entries to keep per route
    /// </summary>
    public int MaxTimeTrialEntriesPerRoute { get; init; } = 1000;
    
    /// <summary>
    /// Whether to allow dirty (collision) times on leaderboard
    /// </summary>
    public bool AllowDirtyTimes { get; init; } = true;
    
    /// <summary>
    /// Maximum wall hits before run is invalidated
    /// </summary>
    public int MaxWallHitsForValid { get; init; } = 5;
    
    // ============================================================================
    // LONGEST RUN
    // ============================================================================
    
    /// <summary>
    /// Minimum races in a run to be recorded
    /// </summary>
    public int MinRacesForLongestRun { get; init; } = 3;
    
    /// <summary>
    /// Maximum entries to keep in longest run leaderboard
    /// </summary>
    public int MaxLongestRunEntries { get; init; } = 500;
    
    // ============================================================================
    // UI SETTINGS
    // ============================================================================
    
    /// <summary>
    /// Default entries per page
    /// </summary>
    public int DefaultPageSize { get; init; } = 20;
    
    /// <summary>
    /// Maximum entries per page (API limit)
    /// </summary>
    public int MaxPageSize { get; init; } = 100;
    
    /// <summary>
    /// Auto-refresh interval for Lua UI (seconds)
    /// </summary>
    public int UiRefreshIntervalSeconds { get; init; } = 30;
    
    // ============================================================================
    // HTTP API
    // ============================================================================
    
    /// <summary>
    /// Enable HTTP API
    /// </summary>
    public bool EnableHttpApi { get; init; } = true;
}

[UsedImplicitly]
public class SXRLeaderboardConfigurationValidator : IValidator<SXRLeaderboardConfiguration>
{
    public bool IsValid(SXRLeaderboardConfiguration value, out string errorMessage)
    {
        errorMessage = "";
        
        if (value.MinRacesForRanking < 0)
        {
            errorMessage = "MinRacesForRanking must be >= 0";
            return false;
        }
        
        if (value.StartingElo < 0)
        {
            errorMessage = "StartingElo must be >= 0";
            return false;
        }
        
        if (value.DefaultPageSize < 1 || value.DefaultPageSize > 100)
        {
            errorMessage = "DefaultPageSize must be 1-100";
            return false;
        }
        
        return true;
    }
}
