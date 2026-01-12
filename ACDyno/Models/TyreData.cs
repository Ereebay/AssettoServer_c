namespace ACDyno.Models;

/// <summary>
/// Complete tyre configuration from tyres.ini
/// </summary>
public class TyreSetData
{
    public int Version { get; set; }
    public int DefaultCompoundIndex { get; set; }
    
    /// <summary>
    /// Available tyre compounds (FRONT, FRONT_1, FRONT_2, etc.)
    /// </summary>
    public List<TyreCompound> Compounds { get; set; } = new();
    
    /// <summary>
    /// Get the default/first compound
    /// </summary>
    public TyreCompound? DefaultCompound => Compounds.FirstOrDefault();
    
    /// <summary>
    /// Get compound by index
    /// </summary>
    public TyreCompound? GetCompound(int index)
    {
        return Compounds.ElementAtOrDefault(index);
    }
}

/// <summary>
/// A tyre compound with front and rear configurations
/// </summary>
public class TyreCompound
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    
    public TyreData Front { get; set; } = new();
    public TyreData Rear { get; set; } = new();
    public TyreThermalData? FrontThermal { get; set; }
    public TyreThermalData? RearThermal { get; set; }
    
    /// <summary>
    /// Average lateral grip coefficient
    /// </summary>
    public double AverageLateralGrip => (Front.DY0 + Rear.DY0) / 2;
    
    /// <summary>
    /// Average longitudinal grip coefficient
    /// </summary>
    public double AverageLongitudinalGrip => (Front.DX0 + Rear.DX0) / 2;
    
    /// <summary>
    /// Grip balance (positive = more rear grip)
    /// </summary>
    public double GripBalance => Rear.DY0 - Front.DY0;
}

/// <summary>
/// Single tyre configuration (front or rear)
/// </summary>
public class TyreData
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    
    // Physical dimensions
    public double Width { get; set; } // meters
    public double Radius { get; set; } // meters
    public double RimRadius { get; set; } // meters
    
    // Physical properties
    public double AngularInertia { get; set; }
    public double Damp { get; set; } // N sec/m
    public double Rate { get; set; } // N/m spring rate
    
    // Grip coefficients - THE KEY PERFORMANCE VALUES
    public double DY0 { get; set; } // Lateral grip coefficient
    public double DY1 { get; set; } // Lateral grip load sensitivity
    public double DX0 { get; set; } // Longitudinal grip coefficient
    public double DX1 { get; set; } // Longitudinal grip load sensitivity
    
    // Reference values
    public double DXRef { get; set; }
    public double DYRef { get; set; }
    public double FZ0 { get; set; } // Reference load
    
    // Load sensitivity exponents
    public double LSExpY { get; set; }
    public double LSExpX { get; set; }
    
    // Wear and performance
    public string WearCurveFile { get; set; } = string.Empty;
    public double SpeedSensitivity { get; set; }
    public double RelaxationLength { get; set; }
    
    // Rolling resistance
    public double RollingResistance0 { get; set; }
    public double RollingResistance1 { get; set; }
    public double RollingResistanceSlip { get; set; }
    
    // Flex and camber
    public double Flex { get; set; }
    public double FlexGain { get; set; }
    public double CamberGain { get; set; }
    public double DCamber0 { get; set; }
    public double DCamber1 { get; set; }
    
    // Friction
    public double FrictionLimitAngle { get; set; }
    public double XMu { get; set; }
    
    // Pressure
    public double PressureStatic { get; set; } // PSI
    public double PressureIdeal { get; set; } // PSI
    public double PressureSpringGain { get; set; }
    public double PressureFlexGain { get; set; }
    public double PressureRRGain { get; set; }
    public double PressureDGain { get; set; }
    
    // Performance falloff
    public double FalloffLevel { get; set; }
    public double FalloffSpeed { get; set; }
    
    // Other
    public double CXMult { get; set; }
    public double RadiusAngularK { get; set; }
    public double BrakeDXMod { get; set; }
    public double CombinedFactor { get; set; }
    
    /// <summary>
    /// Calculate contact patch width based on width and pressure
    /// </summary>
    public double ContactPatchWidth => Width * 0.8; // Approximate
    
    /// <summary>
    /// Sidewall height (tire radius minus rim radius)
    /// </summary>
    public double SidewallHeight => Radius - RimRadius;
    
    /// <summary>
    /// Tire circumference
    /// </summary>
    public double Circumference => 2 * Math.PI * Radius;
}

/// <summary>
/// Thermal behavior configuration
/// </summary>
public class TyreThermalData
{
    public double SurfaceTransfer { get; set; }
    public double PatchTransfer { get; set; }
    public double CoreTransfer { get; set; }
    public double InternalCoreTransfer { get; set; }
    public double FrictionK { get; set; }
    public double RollingK { get; set; }
    public string PerformanceCurveFile { get; set; } = string.Empty;
    
    // Grain and blister
    public double GrainGamma { get; set; }
    public double GrainGain { get; set; }
    public double BlisterGamma { get; set; }
    public double BlisterGain { get; set; }
    
    public double CoolFactor { get; set; }
    public double SurfaceRollingK { get; set; }
}
