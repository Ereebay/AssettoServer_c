using ACDyno.Models;

namespace ACDyno.Services;

/// <summary>
/// Service to detect unrealistic or unbalanced values in car configurations
/// </summary>
public class UnrealisticDetector
{
    public ValidationThresholds Thresholds { get; set; } = new();
    
    /// <summary>
    /// Validate a car and return list of issues
    /// </summary>
    public List<ValidationIssue> Validate(CarData car)
    {
        var issues = new List<ValidationIssue>();
        
        ValidateMass(car, issues);
        ValidatePower(car, issues);
        ValidateTyres(car, issues);
        ValidateAero(car, issues);
        ValidateSuspension(car, issues);
        ValidateBrakes(car, issues);
        ValidateDrivetrain(car, issues);
        ValidateOverallBalance(car, issues);
        
        return issues;
    }
    
    private void ValidateMass(CarData car, List<ValidationIssue> issues)
    {
        var mass = car.BasicInfo.TotalMass;
        
        if (mass < Thresholds.MinMassKg)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Weight",
                File = "car.ini",
                Section = "BASIC",
                Key = "TOTALMASS",
                Message = $"Mass ({mass:F0} kg) is unrealistically low for a street car",
                CurrentValue = mass,
                SuggestedMin = Thresholds.MinMassKg,
                SuggestedMax = Thresholds.MaxMassKg
            });
        }
        
        if (mass > Thresholds.MaxMassKg)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Weight",
                File = "car.ini",
                Section = "BASIC",
                Key = "TOTALMASS",
                Message = $"Mass ({mass:F0} kg) is unusually high",
                CurrentValue = mass,
                SuggestedMin = Thresholds.MinMassKg,
                SuggestedMax = Thresholds.MaxMassKg
            });
        }
    }
    
    private void ValidatePower(CarData car, List<ValidationIssue> issues)
    {
        var peakPower = car.Metrics.PeakPowerHp;
        var powerToWeight = car.Metrics.PowerToWeightHpPerTon;
        
        if (peakPower > Thresholds.MaxPowerHp)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Power",
                File = "engine.ini / power.lut",
                Section = "ENGINE_DATA",
                Key = "Power Output",
                Message = $"Peak power ({peakPower:F0} HP) is extremely high",
                CurrentValue = peakPower,
                SuggestedMax = Thresholds.MaxPowerHp
            });
        }
        
        if (powerToWeight > Thresholds.MaxPowerToWeight)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Category = "Balance",
                File = "Multiple",
                Section = "Power/Weight",
                Key = "Ratio",
                Message = $"Power-to-weight ratio ({powerToWeight:F0} HP/ton) is unrealistically high for a street car",
                CurrentValue = powerToWeight,
                SuggestedMax = Thresholds.MaxPowerToWeight
            });
        }
        
        // Check turbo boost
        foreach (var turbo in car.Engine.Turbos)
        {
            if (turbo.MaxBoost > Thresholds.MaxBoost)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Power",
                    File = "engine.ini",
                    Section = $"TURBO_{turbo.Index}",
                    Key = "MAX_BOOST",
                    Message = $"Turbo boost ({turbo.MaxBoost:F2}) is extremely high",
                    CurrentValue = turbo.MaxBoost,
                    SuggestedMax = Thresholds.MaxBoost
                });
            }
        }
    }
    
    private void ValidateTyres(CarData car, List<ValidationIssue> issues)
    {
        var compound = car.Tyres.DefaultCompound;
        if (compound == null) return;
        
        // Check grip coefficients
        void CheckGrip(string location, double dy, double dx)
        {
            if (dy > Thresholds.MaxGripCoefficient)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Grip",
                    File = "tyres.ini",
                    Section = location,
                    Key = "DY0",
                    Message = $"{location} lateral grip ({dy:F4}) is unrealistically high - typical street tires are 1.0-1.4",
                    CurrentValue = dy,
                    SuggestedMax = Thresholds.MaxGripCoefficient
                });
            }
            
            if (dx > Thresholds.MaxGripCoefficient)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Grip",
                    File = "tyres.ini",
                    Section = location,
                    Key = "DX0",
                    Message = $"{location} longitudinal grip ({dx:F4}) is unrealistically high",
                    CurrentValue = dx,
                    SuggestedMax = Thresholds.MaxGripCoefficient
                });
            }
            
            if (dy < Thresholds.MinGripCoefficient)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Info,
                    Category = "Grip",
                    File = "tyres.ini",
                    Section = location,
                    Key = "DY0",
                    Message = $"{location} lateral grip ({dy:F4}) is very low - even budget tires are typically 0.85+",
                    CurrentValue = dy,
                    SuggestedMin = Thresholds.MinGripCoefficient
                });
            }
        }
        
        CheckGrip("FRONT", compound.Front.DY0, compound.Front.DX0);
        CheckGrip("REAR", compound.Rear.DY0, compound.Rear.DX0);
        
        // Check for extreme grip imbalance
        var gripDiff = Math.Abs(compound.Front.DY0 - compound.Rear.DY0);
        if (gripDiff > 0.3)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Balance",
                File = "tyres.ini",
                Section = "FRONT/REAR",
                Key = "DY0 Balance",
                Message = $"Large grip difference between front ({compound.Front.DY0:F4}) and rear ({compound.Rear.DY0:F4})",
                CurrentValue = gripDiff
            });
        }
    }
    
    private void ValidateAero(CarData car, List<ValidationIssue> issues)
    {
        var drag = car.Aero.BodyDragGain;
        
        if (drag < Thresholds.MinDragCoefficient)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Aero",
                File = "aero.ini",
                Section = "WING_0 (BODY)",
                Key = "CD_GAIN",
                Message = $"Body drag ({drag:F2}) is unrealistically low",
                CurrentValue = drag,
                SuggestedMin = Thresholds.MinDragCoefficient
            });
        }
        
        if (drag > Thresholds.MaxDragCoefficient)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Aero",
                File = "aero.ini",
                Section = "WING_0 (BODY)",
                Key = "CD_GAIN",
                Message = $"Body drag ({drag:F2}) is very high - will significantly limit top speed",
                CurrentValue = drag,
                SuggestedMax = Thresholds.MaxDragCoefficient
            });
        }
    }
    
    private void ValidateSuspension(CarData car, List<ValidationIssue> issues)
    {
        // Check spring rates
        if (car.Suspension.Front.SpringRate > Thresholds.MaxSpringRate)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Suspension",
                File = "suspensions.ini",
                Section = "FRONT",
                Key = "SPRING_RATE",
                Message = $"Front spring rate ({car.Suspension.Front.SpringRate:F0} N/m) is extremely stiff",
                CurrentValue = car.Suspension.Front.SpringRate,
                SuggestedMax = Thresholds.MaxSpringRate
            });
        }
        
        if (car.Suspension.Rear.SpringRate > Thresholds.MaxSpringRate)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Suspension",
                File = "suspensions.ini",
                Section = "REAR",
                Key = "SPRING_RATE",
                Message = $"Rear spring rate ({car.Suspension.Rear.SpringRate:F0} N/m) is extremely stiff",
                CurrentValue = car.Suspension.Rear.SpringRate,
                SuggestedMax = Thresholds.MaxSpringRate
            });
        }
        
        // Check weight distribution
        var weightDist = car.Suspension.CgLocation;
        if (weightDist < 40 || weightDist > 65)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Balance",
                File = "suspensions.ini",
                Section = "BASIC",
                Key = "CG_LOCATION",
                Message = $"Weight distribution ({weightDist:F1}% front) is unusual for a production car",
                CurrentValue = weightDist,
                SuggestedMin = 45,
                SuggestedMax = 60
            });
        }
    }
    
    private void ValidateBrakes(CarData car, List<ValidationIssue> issues)
    {
        // Check if brakes are adequate for the car's performance
        var tyres = car.Tyres.DefaultCompound;
        if (tyres == null) return;
        
        if (!car.Brakes.IsBrakeTorqueSufficient(car.BasicInfo.TotalMass, 
            tyres.Rear.Radius, tyres.AverageLateralGrip))
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Brakes",
                File = "brakes.ini",
                Section = "DATA",
                Key = "MAX_TORQUE",
                Message = "Brake torque may be insufficient to lock wheels under maximum braking",
                CurrentValue = car.Brakes.MaxTorque
            });
        }
        
        // Check brake bias
        var bias = car.Brakes.FrontShare * 100;
        if (bias < 55 || bias > 80)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Brakes",
                File = "brakes.ini",
                Section = "DATA",
                Key = "FRONT_SHARE",
                Message = $"Brake bias ({bias:F0}% front) is outside typical range (55-80%)",
                CurrentValue = bias,
                SuggestedMin = 55,
                SuggestedMax = 80
            });
        }
    }
    
    private void ValidateDrivetrain(CarData car, List<ValidationIssue> issues)
    {
        // Check gear spread
        if (car.Drivetrain.GearRatios.Count > 1)
        {
            var spread = car.Drivetrain.GearSpread;
            if (spread < 2.5 || spread > 6.0)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Info,
                    Category = "Drivetrain",
                    File = "drivetrain.ini",
                    Section = "GEARS",
                    Key = "Gear Spread",
                    Message = $"Gear spread ({spread:F2}:1) is unusual - typical is 3.0-5.0",
                    CurrentValue = spread,
                    SuggestedMin = 3.0,
                    SuggestedMax = 5.0
                });
            }
        }
        
        // Check final drive
        if (car.Drivetrain.FinalDrive < 2.0 || car.Drivetrain.FinalDrive > 6.0)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Drivetrain",
                File = "drivetrain.ini",
                Section = "GEARS",
                Key = "FINAL",
                Message = $"Final drive ratio ({car.Drivetrain.FinalDrive:F2}) is outside typical range",
                CurrentValue = car.Drivetrain.FinalDrive,
                SuggestedMin = 2.5,
                SuggestedMax = 5.0
            });
        }
    }
    
    private void ValidateOverallBalance(CarData car, List<ValidationIssue> issues)
    {
        // Performance Index sanity check
        if (car.Metrics.PerformanceIndex > 950)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Balance",
                File = "Overall",
                Section = "Performance Index",
                Key = "PI",
                Message = $"Performance Index ({car.Metrics.PerformanceIndex}) indicates an extremely overpowered car",
                CurrentValue = car.Metrics.PerformanceIndex
            });
        }
        
        // Check estimated 0-100 time sanity
        if (car.Metrics.Est0To100Kmh < 2.0)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Balance",
                File = "Overall",
                Section = "Performance",
                Key = "0-100 Time",
                Message = $"Estimated 0-100 km/h ({car.Metrics.Est0To100Kmh:F1}s) is faster than any production car",
                CurrentValue = car.Metrics.Est0To100Kmh
            });
        }
    }
}

/// <summary>
/// Configurable thresholds for validation
/// </summary>
public class ValidationThresholds
{
    // Mass
    public double MinMassKg { get; set; } = 500;
    public double MaxMassKg { get; set; } = 3000;
    
    // Power
    public double MaxPowerHp { get; set; } = 2000;
    public double MaxPowerToWeight { get; set; } = 1000; // HP/ton
    public double MaxBoost { get; set; } = 3.0;
    
    // Grip
    public double MinGripCoefficient { get; set; } = 0.8;
    public double MaxGripCoefficient { get; set; } = 2.0;
    
    // Aero
    public double MinDragCoefficient { get; set; } = 0.5;
    public double MaxDragCoefficient { get; set; } = 3.0;
    
    // Suspension
    public double MaxSpringRate { get; set; } = 500000; // N/m
}
