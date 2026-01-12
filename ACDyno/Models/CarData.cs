namespace ACDyno.Models;

/// <summary>
/// Main container for all car configuration data parsed from AC mod files
/// </summary>
public class CarData
{
    public string FolderPath { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    
    // car.ini
    public CarBasicInfo BasicInfo { get; set; } = new();
    
    // engine.ini
    public EngineData Engine { get; set; } = new();
    
    // drivetrain.ini
    public DrivetrainData Drivetrain { get; set; } = new();
    
    // tyres.ini
    public TyreSetData Tyres { get; set; } = new();
    
    // suspensions.ini
    public SuspensionData Suspension { get; set; } = new();
    
    // brakes.ini
    public BrakeData Brakes { get; set; } = new();
    
    // aero.ini
    public AeroData Aero { get; set; } = new();
    
    // power.lut
    public PowerCurve PowerCurve { get; set; } = new();
    
    // Calculated metrics
    public PerformanceMetrics Metrics { get; set; } = new();
    
    // Validation results
    public List<ValidationIssue> ValidationIssues { get; set; } = new();
    
    public DateTime LoadedAt { get; set; } = DateTime.Now;
    public bool IsModified { get; set; }
}

/// <summary>
/// Basic car info from car.ini
/// </summary>
public class CarBasicInfo
{
    public int Version { get; set; }
    public string ScreenName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public double TotalMass { get; set; } // kg
    public double[] Inertia { get; set; } = new double[3]; // x, y, z
    public double SteerLock { get; set; } // degrees
    public double SteerRatio { get; set; }
    public double FuelConsumption { get; set; }
    public double Fuel { get; set; }
    public double MaxFuel { get; set; }
    public double FFMultiplier { get; set; }
    
    // Graphics positions
    public double[] GraphicsOffset { get; set; } = new double[3];
    public double[] DriverEyes { get; set; } = new double[3];
}

/// <summary>
/// Represents an issue detected during validation
/// </summary>
public class ValidationIssue
{
    public ValidationSeverity Severity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public double? CurrentValue { get; set; }
    public double? SuggestedMin { get; set; }
    public double? SuggestedMax { get; set; }
}

public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
