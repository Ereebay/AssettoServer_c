using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace AssettoServer.Server.Configuration.Extra;

/// <summary>
/// Configuration for lane change behavior using MOBIL algorithm with driver personality system.
/// </summary>
#pragma warning disable CS0657
[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public partial class LaneChangeParams : ObservableObject
{
    [YamlMember(Description = "Enable AI lane changes using MOBIL algorithm")]
    public bool EnableLaneChanges { get; init; } = true;
    
    [YamlMember(Description = "Minimum time between lane changes in seconds")]
    public float LaneChangeCooldownSeconds { get; init; } = 5.0f;
    
    [YamlMember(Description = "Lane width in meters for lateral offset calculation")]
    public float LaneWidthMeters { get; init; } = 3.5f;
    
    [YamlMember(Description = "Base lane change duration at 100 km/h in seconds")]
    public float BaseLaneChangeDurationSeconds { get; init; } = 3.5f;
    
    [YamlMember(Description = "Minimum lane change duration in seconds")]
    public float MinLaneChangeDurationSeconds { get; init; } = 2.5f;
    
    [YamlMember(Description = "Maximum lane change duration in seconds")]
    public float MaxLaneChangeDurationSeconds { get; init; } = 7.0f;
    
    [YamlMember(Description = "MOBIL politeness factor (0 = selfish, 0.5 = very cooperative)")]
    public float MobilPoliteness { get; init; } = 0.25f;
    
    [YamlMember(Description = "Maximum safe deceleration for following vehicle (m/s²)")]
    public float MobilSafeDeceleration { get; init; } = 4.0f;
    
    [YamlMember(Description = "Minimum acceleration advantage to justify lane change (m/s²)")]
    public float MobilThreshold { get; init; } = 0.15f;
    
    [YamlMember(Description = "Bias for staying in slow lane after passing (m/s²). For left-hand traffic (Japan)")]
    public float MobilKeepSlowLaneBias { get; init; } = 0.3f;
    
    [YamlMember(Description = "Minimum speed to consider lane change (m/s)")]
    public float MinSpeedForLaneChange { get; init; } = 10.0f;
    
    [YamlMember(Description = "Maximum distance to leader to trigger lane change evaluation (meters)")]
    public float MaxLeaderDistanceForLaneChange { get; init; } = 100.0f;
    
    [YamlMember(Description = "Lookahead distance for finding vehicles in adjacent lanes (meters)")]
    public float LaneChangeLookaheadMeters { get; init; } = 200.0f;
    
    [YamlMember(Description = "Look behind distance for safety check in target lane (meters)")]
    public float LaneChangeLookbehindMeters { get; init; } = 100.0f;
    
    // === PERSONALITY SYSTEM ===
    [YamlMember(Description = "Enable driver personality/aggressiveness variation (0-1 per driver)")]
    public bool EnablePersonalitySystem { get; init; } = true;
    
    [YamlMember(Description = "Speed modifier for passive drivers (aggression=0) in km/h. Negative = under limit")]
    public float PassiveSpeedOffsetKmh { get; init; } = -10.0f;
    
    [YamlMember(Description = "Speed modifier for aggressive drivers (aggression=1) in km/h. Positive = over limit")]
    public float AggressiveSpeedOffsetKmh { get; init; } = 30.0f;
    
    [YamlMember(Description = "Enable proactive lane changes for aggressive drivers (look ahead before being blocked)")]
    public bool EnableProactiveLaneChanges { get; init; } = true;
    
    [YamlMember(Description = "Log lane change events to console for debugging", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool DebugLogging { get; init; } = false;
}
