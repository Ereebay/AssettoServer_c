namespace ACDyno.Models;

/// <summary>
/// Drivetrain configuration from drivetrain.ini
/// </summary>
public class DrivetrainData
{
    public int Version { get; set; }
    
    // TRACTION section
    public DriveLayout DriveLayout { get; set; }
    
    // GEARS section
    public int GearCount { get; set; }
    public double ReverseRatio { get; set; }
    public List<double> GearRatios { get; set; } = new();
    public double FinalDrive { get; set; }
    
    // DIFFERENTIAL section (rear for RWD/AWD, front for FWD)
    public double DiffPower { get; set; } // 0-1 lock percentage under power
    public double DiffCoast { get; set; } // 0-1 lock percentage coasting
    public double DiffPreload { get; set; } // Nm
    
    // AWD section (only for AWD cars)
    public AwdConfig? AwdConfig { get; set; }
    
    // GEARBOX section
    public int ChangeUpTime { get; set; } // ms
    public int ChangeDownTime { get; set; } // ms
    public int AutoCutoffTime { get; set; } // ms
    public bool SupportsShifter { get; set; }
    public int ValidShiftRpmWindow { get; set; }
    public double ControlsWindowGain { get; set; }
    public double GearboxInertia { get; set; }
    
    // CLUTCH section
    public double ClutchMaxTorque { get; set; }
    
    // AUTO_SHIFTER section
    public int AutoShiftUp { get; set; }
    public int AutoShiftDown { get; set; }
    public double SlipThreshold { get; set; }
    
    /// <summary>
    /// Get overall ratio for a given gear (including final drive)
    /// </summary>
    public double GetOverallRatio(int gear)
    {
        if (gear < 1 || gear > GearRatios.Count) return 0;
        return GearRatios[gear - 1] * FinalDrive;
    }
    
    /// <summary>
    /// Get gear spread (ratio of 1st to top gear)
    /// </summary>
    public double GearSpread => GearRatios.Count > 0 
        ? GearRatios.First() / GearRatios.Last() 
        : 0;
    
    /// <summary>
    /// Estimate drivetrain efficiency loss
    /// </summary>
    public double DrivetrainLoss => DriveLayout switch
    {
        DriveLayout.RWD => 0.15, // ~15% loss
        DriveLayout.FWD => 0.12, // ~12% loss (shorter driveline)
        DriveLayout.AWD => 0.22, // ~22% loss (more components)
        _ => 0.15
    };
    
    /// <summary>
    /// Calculate theoretical speed at given RPM and gear
    /// </summary>
    public double CalculateSpeed(int rpm, int gear, double tyreRadius)
    {
        if (gear < 1 || gear > GearRatios.Count) return 0;
        double overallRatio = GetOverallRatio(gear);
        if (overallRatio == 0) return 0;
        
        // Speed (km/h) = (RPM * 60 * 2π * radius) / (overallRatio * 1000)
        return (rpm * 60 * 2 * Math.PI * tyreRadius) / (overallRatio * 1000);
    }
    
    /// <summary>
    /// Calculate RPM at given speed and gear
    /// </summary>
    public double CalculateRpm(double speedKmh, int gear, double tyreRadius)
    {
        if (gear < 1 || gear > GearRatios.Count) return 0;
        double overallRatio = GetOverallRatio(gear);
        
        // RPM = (Speed * overallRatio * 1000) / (60 * 2π * radius)
        return (speedKmh * overallRatio * 1000) / (60 * 2 * Math.PI * tyreRadius);
    }
}

public enum DriveLayout
{
    RWD,
    FWD,
    AWD
}

/// <summary>
/// AWD-specific configuration
/// </summary>
public class AwdConfig
{
    public double FrontShare { get; set; } // Percentage to front
    
    // Front differential
    public double FrontDiffPower { get; set; }
    public double FrontDiffCoast { get; set; }
    public double FrontDiffPreload { get; set; }
    
    // Centre differential
    public double CentreDiffPower { get; set; }
    public double CentreDiffCoast { get; set; }
    public double CentreDiffPreload { get; set; }
    
    // Rear differential
    public double RearDiffPower { get; set; }
    public double RearDiffCoast { get; set; }
    public double RearDiffPreload { get; set; }
    
    public double RearShare => 100 - FrontShare;
}
