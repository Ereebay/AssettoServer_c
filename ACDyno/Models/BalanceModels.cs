namespace ACDyno.Models;

/// <summary>
/// Result of comparing two cars
/// </summary>
public class ComparisonResult
{
    public CarData Car1 { get; set; } = null!;
    public CarData Car2 { get; set; } = null!;
    
    public PerformanceMetrics Metrics1 => Car1.Metrics;
    public PerformanceMetrics Metrics2 => Car2.Metrics;
    
    // Differences (positive = Car1 is higher)
    public double PowerDifference => Metrics1.PeakPowerHp - Metrics2.PeakPowerHp;
    public double WeightDifference => Metrics1.CurbWeightKg - Metrics2.CurbWeightKg;
    public double PowerToWeightDifference => Metrics1.PowerToWeightHpPerTon - Metrics2.PowerToWeightHpPerTon;
    public double GripDifference => Metrics1.EffectiveGrip - Metrics2.EffectiveGrip;
    public double DragDifference => Metrics1.DragCoefficient - Metrics2.DragCoefficient;
    public int PIGDifference => Metrics1.PerformanceIndex - Metrics2.PerformanceIndex;
    
    // Percentage differences
    public double PowerDifferencePercent => Metrics2.PeakPowerHp > 0 
        ? (PowerDifference / Metrics2.PeakPowerHp) * 100 : 0;
    public double WeightDifferencePercent => Metrics2.CurbWeightKg > 0 
        ? (WeightDifference / Metrics2.CurbWeightKg) * 100 : 0;
    
    // Win probability estimation for street racing
    public RaceOutcomePrediction RacePrediction { get; set; } = new();
    
    // Category comparisons
    public List<CategoryComparison> CategoryComparisons { get; set; } = new();
    
    /// <summary>
    /// Are these cars in the same performance class?
    /// </summary>
    public bool SameClass => Metrics1.PerformanceClass == Metrics2.PerformanceClass;
    
    /// <summary>
    /// Get the class difference (positive = Car1 is higher class)
    /// </summary>
    public int ClassDifference => (int)Metrics1.PerformanceClass - (int)Metrics2.PerformanceClass;
}

/// <summary>
/// Category-specific comparison
/// </summary>
public class CategoryComparison
{
    public string Category { get; set; } = string.Empty;
    public string Car1Value { get; set; } = string.Empty;
    public string Car2Value { get; set; } = string.Empty;
    public double Difference { get; set; }
    public string Advantage { get; set; } = string.Empty; // "Car1", "Car2", "Equal"
    public string Impact { get; set; } = string.Empty; // Description of what this means
}

/// <summary>
/// Prediction of race outcome between two cars
/// </summary>
public class RaceOutcomePrediction
{
    public double Car1WinProbability { get; set; } // 0-1
    public double Car2WinProbability { get; set; } // 0-1
    
    // Scenario-specific probabilities
    public double Car1DragRaceWin { get; set; }
    public double Car1RollingRaceWin { get; set; }
    public double Car1TwistyRoadWin { get; set; }
    public double Car1HighwayWin { get; set; }
    public double Car1TrafficRaceWin { get; set; } // Street racing with traffic
    
    public string PredictedWinner => Car1WinProbability > 0.5 ? "Car1" : 
                                     Car1WinProbability < 0.5 ? "Car2" : "Tie";
    
    public string Analysis { get; set; } = string.Empty;
}

/// <summary>
/// A suggestion for balancing a car
/// </summary>
public class BalanceSuggestion
{
    public string File { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double SuggestedValue { get; set; }
    public string Reason { get; set; } = string.Empty;
    public BalanceImpact Impact { get; set; }
    public double PIChange { get; set; } // Estimated PI change
    
    /// <summary>
    /// Get the percentage change
    /// </summary>
    public double ChangePercent => CurrentValue != 0 
        ? ((SuggestedValue - CurrentValue) / CurrentValue) * 100 
        : 0;
}

/// <summary>
/// What aspect of performance this balance change affects
/// </summary>
public enum BalanceImpact
{
    Power,
    Weight,
    Grip,
    Acceleration,
    TopSpeed,
    Braking,
    Handling,
    Launch,
    Aero
}

/// <summary>
/// Complete balance recommendation for matching two cars
/// </summary>
public class BalanceRecommendation
{
    public CarData TargetCar { get; set; } = null!;
    public CarData ReferenceCar { get; set; } = null!;
    
    public int TargetPI { get; set; }
    public int CurrentPIDifference { get; set; }
    
    public List<BalanceSuggestion> Suggestions { get; set; } = new();
    
    /// <summary>
    /// Quick balance suggestions (minimal changes)
    /// </summary>
    public List<BalanceSuggestion> QuickFixes { get; set; } = new();
    
    /// <summary>
    /// Comprehensive balance (more changes, better result)
    /// </summary>
    public List<BalanceSuggestion> ComprehensiveChanges { get; set; } = new();
    
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// Estimated PI after applying all suggestions
    /// </summary>
    public int EstimatedNewPI { get; set; }
    
    /// <summary>
    /// Will applying these changes preserve the car's character?
    /// </summary>
    public bool PreservesCharacter { get; set; }
    
    public string CharacterNotes { get; set; } = string.Empty;
}

/// <summary>
/// Configuration for class-based racing balance targets
/// </summary>
public class ClassBalanceConfig
{
    public PerformanceClass TargetClass { get; set; }
    public int MinPI { get; set; }
    public int MaxPI { get; set; }
    public int TargetPI { get; set; }
    
    // Allowed ranges for the class
    public double MaxPowerHp { get; set; }
    public double MinWeightKg { get; set; }
    public double MaxGripCoefficient { get; set; }
    
    // Tolerance for matching
    public int PITolerance { get; set; } = 20;
    public double AccelTimeTolerance { get; set; } = 0.5; // seconds
    public double TopSpeedTolerance { get; set; } = 10; // km/h
    
    public static ClassBalanceConfig GetDefault(PerformanceClass perfClass)
    {
        return perfClass switch
        {
            PerformanceClass.D => new ClassBalanceConfig
            {
                TargetClass = PerformanceClass.D,
                MinPI = 100, MaxPI = 400, TargetPI = 300,
                MaxPowerHp = 200, MinWeightKg = 1000, MaxGripCoefficient = 1.2
            },
            PerformanceClass.C => new ClassBalanceConfig
            {
                TargetClass = PerformanceClass.C,
                MinPI = 401, MaxPI = 500, TargetPI = 450,
                MaxPowerHp = 350, MinWeightKg = 1100, MaxGripCoefficient = 1.3
            },
            PerformanceClass.B => new ClassBalanceConfig
            {
                TargetClass = PerformanceClass.B,
                MinPI = 501, MaxPI = 600, TargetPI = 550,
                MaxPowerHp = 500, MinWeightKg = 1200, MaxGripCoefficient = 1.4
            },
            PerformanceClass.A => new ClassBalanceConfig
            {
                TargetClass = PerformanceClass.A,
                MinPI = 601, MaxPI = 700, TargetPI = 650,
                MaxPowerHp = 700, MinWeightKg = 1200, MaxGripCoefficient = 1.5
            },
            PerformanceClass.S1 => new ClassBalanceConfig
            {
                TargetClass = PerformanceClass.S1,
                MinPI = 701, MaxPI = 800, TargetPI = 750,
                MaxPowerHp = 900, MinWeightKg = 1100, MaxGripCoefficient = 1.6
            },
            PerformanceClass.S2 => new ClassBalanceConfig
            {
                TargetClass = PerformanceClass.S2,
                MinPI = 801, MaxPI = 900, TargetPI = 850,
                MaxPowerHp = 1200, MinWeightKg = 1000, MaxGripCoefficient = 1.7
            },
            PerformanceClass.S3 => new ClassBalanceConfig
            {
                TargetClass = PerformanceClass.S3,
                MinPI = 901, MaxPI = 998, TargetPI = 950,
                MaxPowerHp = 1500, MinWeightKg = 900, MaxGripCoefficient = 1.9
            },
            PerformanceClass.X => new ClassBalanceConfig
            {
                TargetClass = PerformanceClass.X,
                MinPI = 999, MaxPI = 999, TargetPI = 999,
                MaxPowerHp = 3000, MinWeightKg = 500, MaxGripCoefficient = 2.5
            },
            _ => GetDefault(PerformanceClass.B)
        };
    }
}
