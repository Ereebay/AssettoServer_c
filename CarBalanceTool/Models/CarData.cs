namespace CarBalanceTool.Models;

/// <summary>
/// Complete car data parsed from data folder
/// </summary>
public class CarData
{
    public required string Name { get; init; }
    public required string FolderPath { get; init; }

    public required CarBasicData Basic { get; init; }
    public required EngineData Engine { get; init; }
    public required DrivetrainData Drivetrain { get; init; }
    public required AeroData Aero { get; init; }
    public required BrakeData Brakes { get; init; }
    public required TyreData Tyres { get; init; }

    /// <summary>
    /// Calculated performance metrics
    /// </summary>
    public PerformanceMetrics? Metrics { get; set; }

    /// <summary>
    /// Path to a preview image from the skins folder (if found)
    /// </summary>
    public string? PreviewImagePath { get; set; }

    /// <summary>
    /// Returns the car name for display and filtering
    /// </summary>
    public override string ToString() => Name;
}

public class CarBasicData
{
    public string ScreenName { get; set; } = "";
    public float TotalMass { get; set; }
    public float SteerLock { get; set; }
    public float SteerRatio { get; set; }
    public float FuelConsumption { get; set; }
    public float MaxFuel { get; set; }
}

public class EngineData
{
    public int Limiter { get; set; }
    public int Minimum { get; set; }
    public float Inertia { get; set; }
    public List<TurboData> Turbos { get; set; } = [];
    public List<PowerPoint> PowerCurve { get; set; } = [];

    /// <summary>
    /// Peak torque from power curve (without boost)
    /// </summary>
    public float PeakTorque => PowerCurve.Count > 0 ? PowerCurve.Max(p => p.Torque) : 0;

    /// <summary>
    /// RPM at peak torque
    /// </summary>
    public int PeakTorqueRpm => PowerCurve.Count > 0
        ? PowerCurve.OrderByDescending(p => p.Torque).First().Rpm
        : 0;

    /// <summary>
    /// RPM at peak power
    /// </summary>
    public int PeakPowerRpm => PowerCurve.Count > 0
        ? PowerCurve.OrderByDescending(p => p.PowerHp).First().Rpm
        : 0;

    /// <summary>
    /// Maximum boost from all turbos
    /// </summary>
    public float MaxBoost => Turbos.Count > 0 ? Turbos.Max(t => t.MaxBoost) : 0;

    /// <summary>
    /// Peak torque with boost applied
    /// </summary>
    public float PeakTorqueBoosted => PeakTorque * (1 + MaxBoost);

    /// <summary>
    /// Estimated peak power in kW
    /// </summary>
    public float PeakPowerKw => PeakTorqueBoosted * 2 * MathF.PI * PeakTorqueRpm / 60000;

    /// <summary>
    /// Estimated peak power in HP
    /// </summary>
    public float PeakPowerHp => PeakPowerKw * 1.341f;
}

public class TurboData
{
    public float MaxBoost { get; set; }
    public float Wastegate { get; set; }
    public int ReferenceRpm { get; set; }
    public float Gamma { get; set; }
    public float LagUp { get; set; }
    public float LagDown { get; set; }
}

public class PowerPoint
{
    public int Rpm { get; set; }
    public float Torque { get; set; }

    /// <summary>
    /// Power in kW at this RPM (without boost)
    /// </summary>
    public float PowerKw => Torque * 2 * MathF.PI * Rpm / 60000;

    /// <summary>
    /// Power in HP at this RPM (without boost)
    /// </summary>
    public float PowerHp => PowerKw * 1.341f;
}

public enum DrivetrainType
{
    RWD,
    FWD,
    AWD
}

public class DrivetrainData
{
    public DrivetrainType Type { get; set; }
    public int GearCount { get; set; }
    public float FinalDrive { get; set; }
    public List<float> GearRatios { get; set; } = [];
    public float ReverseGear { get; set; }

    public DifferentialData Differential { get; set; } = new();
    public AwdData? Awd { get; set; }

    public int ShiftUpTime { get; set; }
    public int ShiftDownTime { get; set; }

    /// <summary>
    /// Maximum clutch torque capacity (Nm)
    /// </summary>
    public float ClutchMaxTorque { get; set; }

    /// <summary>
    /// Overall ratio for each gear (gear ratio * final drive)
    /// </summary>
    public List<float> OverallRatios => GearRatios.Select(g => g * FinalDrive).ToList();

    /// <summary>
    /// Ratio spread (1st gear / top gear)
    /// </summary>
    public float RatioSpread => GearRatios.Count >= 2
        ? GearRatios[0] / GearRatios[^1]
        : 1;
}

public class DifferentialData
{
    public float PowerLock { get; set; }
    public float CoastLock { get; set; }
    public float Preload { get; set; }
}

public class AwdData
{
    public float FrontShare { get; set; }
    public float CentreDiffPower { get; set; }
    public float CentreDiffCoast { get; set; }
    public float FrontDiffPower { get; set; }
    public float FrontDiffCoast { get; set; }
    public float RearDiffPower { get; set; }
    public float RearDiffCoast { get; set; }
}

public class AeroData
{
    public List<WingData> Wings { get; set; } = [];

    /// <summary>
    /// Total drag coefficient gain
    /// </summary>
    public float TotalDragGain => Wings.Sum(w => w.CdGain * w.Chord * w.Span);

    /// <summary>
    /// Total lift coefficient gain (negative = downforce)
    /// </summary>
    public float TotalLiftGain => Wings.Sum(w => w.ClGain * w.Chord * w.Span);

    /// <summary>
    /// Total CL gain (raw sum of wing CL gains)
    /// </summary>
    public float TotalClGain => Wings.Sum(w => w.ClGain);
}

public class WingData
{
    public string Name { get; set; } = "";
    public float Chord { get; set; }
    public float Span { get; set; }
    public float ClGain { get; set; }
    public float CdGain { get; set; }
    public float Angle { get; set; }
}

public class BrakeData
{
    public float MaxTorque { get; set; }
    public float FrontShare { get; set; }
    public float HandbrakeTorque { get; set; }
}

public class TyreData
{
    public TyreCompound Front { get; set; } = new();
    public TyreCompound Rear { get; set; } = new();
}

public class TyreCompound
{
    public string Name { get; set; } = "";
    public float Width { get; set; }
    public float Radius { get; set; }
    public float RimRadius { get; set; }
    public float DY0 { get; set; }  // Lateral grip coefficient
    public float DX0 { get; set; }  // Longitudinal grip coefficient
    public float AngularInertia { get; set; }

    /// <summary>
    /// Circumference in meters
    /// </summary>
    public float Circumference => 2 * MathF.PI * Radius;
}

/// <summary>
/// Analysis of what's limiting the car's top speed
/// </summary>
public class TopSpeedAnalysis
{
    /// <summary>
    /// Top speed limited by rev limiter in top gear (km/h)
    /// </summary>
    public float GearingLimitedSpeed { get; set; }

    /// <summary>
    /// Estimated top speed based on power vs drag (km/h)
    /// </summary>
    public float PowerLimitedSpeed { get; set; }

    /// <summary>
    /// The actual limiting factor
    /// </summary>
    public SpeedLimitFactor LimitingFactor { get; set; }

    /// <summary>
    /// Human-readable analysis
    /// </summary>
    public string Analysis { get; set; } = "";

    /// <summary>
    /// True if the speed seems unrealistic given the power
    /// </summary>
    public bool IsUnrealistic { get; set; }

    /// <summary>
    /// Explanation if unrealistic
    /// </summary>
    public string? UnrealisticReason { get; set; }
}

public enum SpeedLimitFactor
{
    Gearing,       // Rev limiter reached before power can't overcome drag
    Power,         // Power runs out before hitting rev limiter
    Unknown
}

/// <summary>
/// Analysis of clutch adequacy
/// </summary>
public class ClutchAnalysis
{
    /// <summary>
    /// Maximum torque the clutch can handle (Nm)
    /// </summary>
    public float ClutchMaxTorque { get; set; }

    /// <summary>
    /// Peak engine torque with boost (Nm)
    /// </summary>
    public float EnginePeakTorqueBoosted { get; set; }

    /// <summary>
    /// True if clutch can handle engine torque
    /// </summary>
    public bool IsAdequate { get; set; }

    /// <summary>
    /// Headroom percentage (how much extra capacity the clutch has)
    /// </summary>
    public float HeadroomPercent { get; set; }

    /// <summary>
    /// Human-readable analysis
    /// </summary>
    public string Analysis { get; set; } = "";
}

/// <summary>
/// Calculated performance metrics for comparison
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Power to weight ratio in HP/kg
    /// </summary>
    public float PowerToWeight { get; set; }

    /// <summary>
    /// Estimated top speed in km/h (limited by rev limiter in top gear)
    /// </summary>
    public float TheoreticalTopSpeed { get; set; }

    /// <summary>
    /// Estimated 0-100 km/h time in seconds
    /// </summary>
    public float Est0To100 { get; set; }

    /// <summary>
    /// Estimated 0-200 km/h time in seconds
    /// </summary>
    public float Est0To200 { get; set; }

    /// <summary>
    /// Estimated 0-60 mph time in seconds
    /// </summary>
    public float Est0To60Mph { get; set; }

    /// <summary>
    /// Estimated 0-100 mph time in seconds
    /// </summary>
    public float Est0To100Mph { get; set; }

    /// <summary>
    /// Braking force per kg of mass
    /// </summary>
    public float BrakingPerKg { get; set; }

    /// <summary>
    /// Average tyre grip (DY0 + DX0) / 2
    /// </summary>
    public float AverageTyreGrip { get; set; }

    /// <summary>
    /// Drivetrain efficiency estimate (RWD=0.85, FWD=0.88, AWD=0.78)
    /// </summary>
    public float DrivetrainEfficiency { get; set; }

    /// <summary>
    /// Effective wheel power in HP
    /// </summary>
    public float WheelPowerHp { get; set; }

    /// <summary>
    /// Overall "performance score" for quick comparison
    /// </summary>
    public float PerformanceScore { get; set; }

    /// <summary>
    /// Detailed analysis of top speed limiting factors
    /// </summary>
    public TopSpeedAnalysis? SpeedAnalysis { get; set; }

    /// <summary>
    /// Analysis of clutch adequacy
    /// </summary>
    public ClutchAnalysis? ClutchAnalysis { get; set; }
}
