using System.Collections.Concurrent;
using System.Numerics;
using System.Reflection;
using AssettoServer.Server;
using AssettoServer.Server.Ai;
using AssettoServer.Server.Ai.Splines;
using AssettoServer.Server.Configuration;
using AssettoServer.Server.Plugin;
using AssettoServer.Shared.Services;
using Autofac;
using Microsoft.Extensions.Hosting;
using Serilog;
using SXRRealisticTrafficPlugin.Models;

namespace SXRRealisticTrafficPlugin;

/// <summary>
/// AssettoServer plugin for realistic traffic simulation.
/// Enhances the existing AI traffic with IDM car-following and MOBIL lane changes.
/// 
/// This version properly integrates with AssettoServer's AI system by:
/// 1. Using AiSpline for actual spline data and world position conversion
/// 2. Modifying AiState objects directly to control AI cars
/// 3. Using the existing SlowestAiStates for leader detection
/// 4. Smooth lane change transitions with quintic polynomial interpolation
/// </summary>
public class SXRRealisticTrafficPlugin : CriticalBackgroundService, IAssettoServerAutostart
{
    private readonly SXRTrafficManager _trafficManager;
    private readonly SXRTrafficConfiguration _config;
    private readonly EntryCarManager _entryCarManager;
    private readonly ACServerConfiguration _serverConfig;
    private readonly AiSpline _spline;
    private readonly SessionManager _sessionManager;
    private readonly ILogger _logger;
    
    private readonly float _tickInterval;
    private DateTime _lastUpdate = DateTime.UtcNow;
    
    // Track active lane changes for smooth transitions
    private readonly ConcurrentDictionary<AiState, ActiveLaneChange> _activeLaneChanges = new();
    
    // Reflection for accessing private AiState members
    private readonly FieldInfo? _currentSplinePointIdField;
    private readonly FieldInfo? _currentVecLengthField;
    private readonly FieldInfo? _currentVecProgressField;
    
    public SXRRealisticTrafficPlugin(
        SXRTrafficConfiguration config,
        EntryCarManager entryCarManager,
        ACServerConfiguration serverConfig,
        AiSpline spline,
        SessionManager sessionManager,
        IHostApplicationLifetime lifetime) : base(lifetime)
    {
        _config = config;
        _entryCarManager = entryCarManager;
        _serverConfig = serverConfig;
        _spline = spline;
        _sessionManager = sessionManager;
        _logger = Log.ForContext<SXRRealisticTrafficPlugin>();
        
        // Initialize traffic manager with spline integration
        _trafficManager = new SXRTrafficManager(config);
        _tickInterval = 1.0f / config.UpdateTickRate;
        
        // Cache reflection for smooth lane changes
        var aiStateType = typeof(AiState);
        _currentSplinePointIdField = aiStateType.GetField("_currentSplinePointId", BindingFlags.NonPublic | BindingFlags.Instance);
        _currentVecLengthField = aiStateType.GetField("_currentVecLength", BindingFlags.NonPublic | BindingFlags.Instance);
        _currentVecProgressField = aiStateType.GetField("_currentVecProgress", BindingFlags.NonPublic | BindingFlags.Instance);
        
        _logger.Information("SXRRealisticTrafficPlugin initialized");
        _logger.Information("  Tick rate: {TickRate}Hz", config.UpdateTickRate);
        _logger.Information("  Spline points: {Count}", _spline.Points.Length);
    }
    
    /// <summary>
    /// Tracks an active lane change in progress
    /// </summary>
    private class ActiveLaneChange
    {
        public int SourcePointId { get; init; }
        public int TargetPointId { get; init; }
        public float StartTime { get; init; }
        public float Duration { get; init; }
        public float LaneWidth { get; init; }
        public bool IsLeftChange { get; init; }
        
        public float GetProgress(float currentTime)
        {
            return Math.Clamp((currentTime - StartTime) / Duration, 0f, 1f);
        }
        
        public bool IsComplete(float currentTime) => GetProgress(currentTime) >= 1f;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("SXRRealisticTrafficPlugin starting...");
        
        // Wait for server to be ready
        await Task.Delay(5000, stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var elapsed = (float)(now - _lastUpdate).TotalSeconds;
                
                if (elapsed >= _tickInterval)
                {
                    // Update player positions from entry cars
                    UpdatePlayerPositions();
                    
                    // Process AI cars with IDM/MOBIL
                    ProcessAiTraffic(elapsed);
                    
                    _lastUpdate = now;
                    
                    if (_config.DebugLogging)
                    {
                        var aiCount = _entryCarManager.EntryCars.Count(c => c.AiControlled);
                        _logger.Debug("Traffic update: {AiCount} AI cars processed", aiCount);
                    }
                }
                
                // Sleep until next tick
                var sleepTime = (int)((_tickInterval - elapsed) * 1000);
                if (sleepTime > 0)
                {
                    await Task.Delay(sleepTime, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in traffic update loop");
                await Task.Delay(1000, stoppingToken);
            }
        }
        
        _logger.Information("SXRRealisticTrafficPlugin stopped");
    }
    
    private void UpdatePlayerPositions()
    {
        foreach (var car in _entryCarManager.EntryCars)
        {
            if (car.Client == null || car.AiControlled) continue;
            
            var worldPos = car.Status.Position;
            var velocity = car.Status.Velocity;
            
            // Get spline position using AssettoServer's spline system
            var (splinePointId, _) = _spline.WorldToSpline(worldPos);
            
            if (splinePointId >= 0)
            {
                _trafficManager.UpdatePlayerPosition(
                    car.SessionId, 
                    splinePointId, 
                    worldPos, 
                    velocity);
            }
        }
    }
    
    /// <summary>
    /// Process AI traffic using IDM/MOBIL algorithms.
    /// This modifies the actual AiState objects to control car behavior.
    /// </summary>
    private void ProcessAiTraffic(float deltaTime)
    {
        float currentTime = _sessionManager.ServerTimeMilliseconds / 1000f;
        
        // Collect all initialized AI states
        var aiStates = new List<AiState>();
        foreach (var car in _entryCarManager.EntryCars)
        {
            if (!car.AiControlled) continue;
            car.GetInitializedStates(aiStates);
        }
        
        // Clean up lane changes for despawned AI
        CleanupInactiveLaneChanges(aiStates);
        
        foreach (var aiState in aiStates)
        {
            if (!aiState.Initialized) continue;
            
            int currentPointId = aiState.CurrentSplinePointId;
            if (currentPointId < 0) continue;
            
            // === UPDATE ACTIVE LANE CHANGE ===
            UpdateLaneChangePosition(aiState, currentTime);
            
            // === IDM CAR-FOLLOWING ===
            ApplyIdmBehavior(aiState, currentPointId, deltaTime);
            
            // === MOBIL LANE CHANGES (only if not already changing) ===
            if (!_activeLaneChanges.ContainsKey(aiState))
            {
                TryMobilLaneChange(aiState, currentPointId, currentTime);
            }
        }
    }
    
    /// <summary>
    /// Update position for cars actively changing lanes
    /// </summary>
    private void UpdateLaneChangePosition(AiState aiState, float currentTime)
    {
        if (!_activeLaneChanges.TryGetValue(aiState, out var laneChange))
            return;
        
        float progress = laneChange.GetProgress(currentTime);
        
        if (laneChange.IsComplete(currentTime))
        {
            // Lane change complete - finalize the spline switch
            FinalizeLaneChange(aiState, laneChange);
            _activeLaneChanges.TryRemove(aiState, out _);
            
            if (_config.LogLaneChanges)
            {
                _logger.Debug("AI {SessionId} completed lane change", aiState.EntryCar.SessionId);
            }
            return;
        }
        
        // Calculate lateral offset using quintic polynomial for smooth S-curve
        float lateralOffset = LaneChangeTrajectory.QuinticLateralOffset(progress, laneChange.LaneWidth);
        
        // Apply direction (negative for left, positive for right in most coordinate systems)
        if (laneChange.IsLeftChange)
        {
            lateralOffset = -lateralOffset;
        }
        
        // Get the perpendicular direction to the car's heading
        var forward = aiState.Status.Velocity;
        if (forward.LengthSquared() < 0.01f)
        {
            // Car is stopped, use rotation to determine forward
            float yaw = aiState.Status.Rotation.X;
            forward = new Vector3(-MathF.Sin(yaw), 0, -MathF.Cos(yaw));
        }
        forward = Vector3.Normalize(forward);
        
        // Calculate right vector (perpendicular to forward, in XZ plane)
        var right = new Vector3(-forward.Z, 0, forward.X);
        
        // Apply lateral offset to position
        var offset = right * lateralOffset;
        aiState.Status.Position += offset;
    }
    
    /// <summary>
    /// Finalize lane change by switching to target spline point
    /// </summary>
    private void FinalizeLaneChange(AiState aiState, ActiveLaneChange laneChange)
    {
        if (_currentSplinePointIdField == null) return;
        
        try
        {
            // Update SlowestAiStates tracking
            _spline.SlowestAiStates.Leave(laneChange.SourcePointId, aiState);
            _spline.SlowestAiStates.Enter(laneChange.TargetPointId, aiState);
            
            // Set the new point ID
            _currentSplinePointIdField.SetValue(aiState, laneChange.TargetPointId);
            
            // Update vector length for new lane segment
            if (_currentVecLengthField != null && _currentVecProgressField != null)
            {
                var points = _spline.Points;
                ref readonly var targetPoint = ref points[laneChange.TargetPointId];
                
                if (targetPoint.NextId >= 0)
                {
                    float newLength = (points[targetPoint.NextId].Position - targetPoint.Position).Length();
                    _currentVecLengthField.SetValue(aiState, newLength);
                    _currentVecProgressField.SetValue(aiState, 0f);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to finalize lane change");
        }
    }
    
    /// <summary>
    /// Clean up lane change tracking for despawned AI
    /// </summary>
    private void CleanupInactiveLaneChanges(List<AiState> activeStates)
    {
        var activeSet = new HashSet<AiState>(activeStates);
        var toRemove = _activeLaneChanges.Keys.Where(s => !activeSet.Contains(s) || !s.Initialized).ToList();
        
        foreach (var key in toRemove)
        {
            _activeLaneChanges.TryRemove(key, out _);
        }
    }
    
    /// <summary>
    /// Apply IDM (Intelligent Driver Model) to adjust acceleration
    /// </summary>
    private void ApplyIdmBehavior(AiState aiState, int currentPointId, float deltaTime)
    {
        // Find leader using AssettoServer's SlowestAiStates
        var leaderInfo = FindLeader(aiState, currentPointId);
        
        float desiredSpeed = aiState.InitialMaxSpeed;
        float currentSpeed = aiState.CurrentSpeed;
        
        float acceleration;
        
        if (leaderInfo != null)
        {
            // IDM with leader
            float gap = leaderInfo.Distance;
            float leaderSpeed = leaderInfo.Speed;
            float approachingRate = currentSpeed - leaderSpeed;
            
            // IDM parameters
            float a = 2.5f;                             // Max acceleration
            float b = _config.SafeDeceleration;         // Comfortable deceleration
            float s0 = _config.MinimumGap;              // Minimum gap
            float T = _config.DefaultTimeHeadway;       // Time headway
            float delta = 4.0f;                         // Acceleration exponent
            
            // Desired gap: s* = s0 + v*T + (v*deltaV)/(2*sqrt(a*b))
            float sStar = s0 + currentSpeed * T + 
                          (currentSpeed * approachingRate) / (2 * MathF.Sqrt(a * b));
            sStar = MathF.Max(s0, sStar);
            
            // IDM acceleration: a * [1 - (v/v0)^delta - (s*/s)^2]
            float freeRoadTerm = desiredSpeed > 0 ? MathF.Pow(currentSpeed / desiredSpeed, delta) : 0;
            float interactionTerm = gap > 0.1f ? MathF.Pow(sStar / gap, 2) : 1.0f;
            acceleration = a * (1 - freeRoadTerm - interactionTerm);
        }
        else
        {
            // Free road - accelerate toward desired speed
            float a = 2.5f;
            float delta = 4.0f;
            float freeRoadTerm = desiredSpeed > 0 ? MathF.Pow(currentSpeed / desiredSpeed, delta) : 0;
            acceleration = a * (1 - freeRoadTerm);
        }
        
        // Clamp and apply acceleration
        acceleration = Math.Clamp(acceleration, -_config.SafeDeceleration * 1.5f, 2.5f);
        
        // Only override if our calculation suggests more braking than default
        if (acceleration < aiState.Acceleration)
        {
            aiState.Acceleration = acceleration;
        }
    }
    
    /// <summary>
    /// Try MOBIL lane change for an AI car
    /// </summary>
    private void TryMobilLaneChange(AiState aiState, int currentPointId, float currentTime)
    {
        // Check cooldown
        if (!_trafficManager.CanAttemptLaneChange(aiState))
            return;
        
        var point = _spline.Points[currentPointId];
        float currentSpeed = aiState.CurrentSpeed;
        float targetSpeed = aiState.TargetSpeed;
        
        // Only consider lane change if we're blocked
        if (currentSpeed > targetSpeed * 0.85f && currentSpeed > 5f)
            return;
        
        // Find leader in current lane
        var leader = FindLeader(aiState, currentPointId);
        if (leader == null || leader.Distance > 100f) // LaneChangeConsiderationDistance
            return;
        
        // Calculate current acceleration with leader
        float accCurrent = CalculateIdmAcceleration(aiState.CurrentSpeed, leader.Speed, leader.Distance);
        
        // Try left lane (faster lane in Japan)
        if (point.LeftId >= 0)
        {
            var leftLeader = FindLeaderInLane(currentPointId, point.LeftId);
            var leftFollower = FindFollowerInLane(currentPointId, point.LeftId);
            
            if (EvaluateMobilLaneChange(aiState, accCurrent, leftLeader, leftFollower, true))
            {
                StartSmoothLaneChange(aiState, currentPointId, point.LeftId, true, currentTime);
                return;
            }
        }
        
        // Try right lane
        if (point.RightId >= 0)
        {
            var rightLeader = FindLeaderInLane(currentPointId, point.RightId);
            var rightFollower = FindFollowerInLane(currentPointId, point.RightId);
            
            if (EvaluateMobilLaneChange(aiState, accCurrent, rightLeader, rightFollower, false))
            {
                StartSmoothLaneChange(aiState, currentPointId, point.RightId, false, currentTime);
                return;
            }
        }
    }
    
    /// <summary>
    /// Start a smooth lane change transition
    /// </summary>
    private void StartSmoothLaneChange(AiState aiState, int sourcePointId, int targetPointId, bool isLeft, float currentTime)
    {
        float speed = aiState.CurrentSpeed;
        float duration = LaneChangeTrajectory.CalculateDuration(speed);
        
        var laneChange = new ActiveLaneChange
        {
            SourcePointId = sourcePointId,
            TargetPointId = targetPointId,
            StartTime = currentTime,
            Duration = duration,
            LaneWidth = _config.LaneWidth,
            IsLeftChange = isLeft
        };
        
        _activeLaneChanges[aiState] = laneChange;
        _trafficManager.RecordLaneChange(aiState);
        
        if (_config.LogLaneChanges)
        {
            _logger.Debug("AI {SessionId} starting {Direction} lane change (duration: {Duration:F1}s)", 
                aiState.EntryCar.SessionId, isLeft ? "left" : "right", duration);
        }
    }
    
    private bool EvaluateMobilLaneChange(
        AiState aiState, 
        float accCurrent, 
        LeaderInfo? newLeader, 
        FollowerInfo? newFollower,
        bool isLeft)
    {
        // Safety criterion: new follower must not need to brake hard
        if (newFollower != null)
        {
            float followerAccAfter = CalculateIdmAcceleration(newFollower.Speed, aiState.CurrentSpeed, newFollower.Distance);
            if (followerAccAfter < -_config.SafeDeceleration)
                return false; // Unsafe
        }
        
        // Incentive criterion
        float accNew = newLeader != null 
            ? CalculateIdmAcceleration(aiState.CurrentSpeed, newLeader.Speed, newLeader.Distance) 
            : 2.5f; // Free road max acceleration
        
        float followerDisadvantage = 0f;
        if (newFollower != null)
        {
            // Estimate follower's acceleration change
            float accFollowerBefore = newFollower.CurrentAcceleration;
            float accFollowerAfter = CalculateIdmAcceleration(newFollower.Speed, aiState.CurrentSpeed, newFollower.Distance);
            followerDisadvantage = accFollowerBefore - accFollowerAfter;
        }
        
        float myAdvantage = accNew - accCurrent;
        
        // Keep-left bias for Japan (left-hand traffic)
        float bias = isLeft ? -0.3f : 0.3f;
        
        // MOBIL criterion: advantage > politeness * disadvantage + threshold + bias
        float incentive = myAdvantage - _config.DefaultPoliteness * followerDisadvantage - bias;
        
        return incentive > _config.LaneChangeThreshold;
    }
    
    private float CalculateIdmAcceleration(float mySpeed, float leaderSpeed, float gap)
    {
        float desiredSpeed = _serverConfig.Extra.AiParams.MaxSpeedMs;
        float a = 2.5f;
        float b = _config.SafeDeceleration;
        float s0 = _config.MinimumGap;
        float T = _config.DefaultTimeHeadway;
        float delta = 4.0f;
        
        float approachingRate = mySpeed - leaderSpeed;
        float sStar = s0 + mySpeed * T + (mySpeed * approachingRate) / (2 * MathF.Sqrt(a * b));
        sStar = MathF.Max(s0, sStar);
        
        float freeRoadTerm = desiredSpeed > 0 ? MathF.Pow(mySpeed / desiredSpeed, delta) : 0;
        float interactionTerm = gap > 0.1f ? MathF.Pow(sStar / gap, 2) : 1.0f;
        
        return a * (1 - freeRoadTerm - interactionTerm);
    }
    
    // === LEADER/FOLLOWER DETECTION ===
    
    private LeaderInfo? FindLeader(AiState aiState, int currentPointId)
    {
        var points = _spline.Points;
        float distanceTravelled = 0;
        int pointId = currentPointId;
        float maxLookahead = 200f; // Lookahead distance
        
        while (distanceTravelled < maxLookahead)
        {
            ref readonly var point = ref points[pointId];
            if (point.NextId < 0) break;
            
            distanceTravelled += point.Length;
            pointId = point.NextId;
            
            var slowest = _spline.SlowestAiStates[pointId];
            if (slowest != null && slowest != aiState && slowest.Initialized)
            {
                return new LeaderInfo
                {
                    State = slowest,
                    Distance = distanceTravelled,
                    Speed = slowest.CurrentSpeed
                };
            }
        }
        
        return null;
    }
    
    private LeaderInfo? FindLeaderInLane(int fromPointId, int lanePointId)
    {
        if (lanePointId < 0) return null;
        
        var ops = _spline.Operations;
        if (!ops.IsSameDirection(fromPointId, lanePointId))
            return null;
        
        var points = _spline.Points;
        float distanceTravelled = 0;
        int pointId = lanePointId;
        
        while (distanceTravelled < 200f)
        {
            ref readonly var point = ref points[pointId];
            if (point.NextId < 0) break;
            
            distanceTravelled += point.Length;
            pointId = point.NextId;
            
            var slowest = _spline.SlowestAiStates[pointId];
            if (slowest != null && slowest.Initialized)
            {
                return new LeaderInfo
                {
                    State = slowest,
                    Distance = distanceTravelled,
                    Speed = slowest.CurrentSpeed
                };
            }
        }
        
        return null;
    }
    
    private FollowerInfo? FindFollowerInLane(int fromPointId, int lanePointId)
    {
        if (lanePointId < 0) return null;
        
        var ops = _spline.Operations;
        if (!ops.IsSameDirection(fromPointId, lanePointId))
            return null;
        
        var points = _spline.Points;
        float distanceTravelled = 0;
        int pointId = lanePointId;
        float safeGap = 50f; // SafeLaneChangeGap
        
        while (distanceTravelled < safeGap)
        {
            ref readonly var point = ref points[pointId];
            if (point.PreviousId < 0) break;
            
            distanceTravelled += point.Length;
            pointId = point.PreviousId;
            
            var slowest = _spline.SlowestAiStates[pointId];
            if (slowest != null && slowest.Initialized)
            {
                return new FollowerInfo
                {
                    State = slowest,
                    Distance = distanceTravelled,
                    Speed = slowest.CurrentSpeed,
                    CurrentAcceleration = slowest.Acceleration
                };
            }
        }
        
        return null;
    }
    
    private class LeaderInfo
    {
        public required AiState State { get; init; }
        public float Distance { get; init; }
        public float Speed { get; init; }
    }
    
    private class FollowerInfo
    {
        public required AiState State { get; init; }
        public float Distance { get; init; }
        public float Speed { get; init; }
        public float CurrentAcceleration { get; init; }
    }
}

/// <summary>
/// Module for dependency injection registration
/// </summary>
public class RealisticTrafficModule : AssettoServerModule<SXRTrafficConfiguration>
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SXRRealisticTrafficPlugin>()
            .AsSelf()
            .As<IHostedService>()
            .As<IAssettoServerAutostart>()
            .SingleInstance();
    }
}
