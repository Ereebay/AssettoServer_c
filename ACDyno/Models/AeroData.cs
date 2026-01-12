namespace ACDyno.Models;

/// <summary>
/// Aerodynamic configuration from aero.ini
/// </summary>
public class AeroData
{
    public int Version { get; set; }
    
    /// <summary>
    /// All wing/aero elements (WING_0, WING_1, etc.)
    /// </summary>
    public List<WingData> Wings { get; set; } = new();
    
    /// <summary>
    /// Get body drag element (usually WING_0 named "BODY")
    /// </summary>
    public WingData? BodyElement => Wings.FirstOrDefault(w => 
        w.Name.Equals("BODY", StringComparison.OrdinalIgnoreCase));
    
    /// <summary>
    /// Get front aero element
    /// </summary>
    public WingData? FrontElement => Wings.FirstOrDefault(w => 
        w.Name.Equals("FRONT", StringComparison.OrdinalIgnoreCase));
    
    /// <summary>
    /// Get rear wing element
    /// </summary>
    public WingData? RearElement => Wings.FirstOrDefault(w => 
        w.Name.Equals("REAR", StringComparison.OrdinalIgnoreCase));
    
    /// <summary>
    /// Get diffuser element
    /// </summary>
    public WingData? DiffuserElement => Wings.FirstOrDefault(w => 
        w.Name.Contains("DIFFUSER", StringComparison.OrdinalIgnoreCase));
    
    /// <summary>
    /// Total drag coefficient multiplier
    /// </summary>
    public double TotalDragGain => Wings.Sum(w => w.EffectiveCd * w.FrontalArea);
    
    /// <summary>
    /// Total downforce coefficient multiplier
    /// </summary>
    public double TotalLiftGain => Wings.Sum(w => w.EffectiveCl * w.FrontalArea);
    
    /// <summary>
    /// Body drag coefficient (actual Cd from LUT * CDGain)
    /// </summary>
    public double BodyDragGain => BodyElement?.EffectiveCd ?? 0.35;
    
    /// <summary>
    /// Aero efficiency (lift/drag ratio)
    /// </summary>
    public double AeroEfficiency => TotalDragGain > 0 
        ? Math.Abs(TotalLiftGain) / TotalDragGain 
        : 0;
    
    /// <summary>
    /// Front/rear downforce balance (0.5 = balanced, <0.5 = rear heavy)
    /// </summary>
    public double AeroBalance
    {
        get
        {
            double frontLift = FrontElement?.CLGain ?? 0;
            double rearLift = RearElement?.CLGain ?? 0;
            double total = Math.Abs(frontLift) + Math.Abs(rearLift);
            return total > 0 ? Math.Abs(frontLift) / total : 0.5;
        }
    }
}

/// <summary>
/// Single aerodynamic element/wing
/// </summary>
public class WingData
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Physical dimensions
    public double Chord { get; set; } // meters (length)
    public double Span { get; set; } // meters (width)
    public double[] Position { get; set; } = new double[3]; // x, y, z from CoG
    
    // Coefficient lookup tables
    public string LutAoaCL { get; set; } = string.Empty; // Angle of attack vs lift
    public string LutGhCL { get; set; } = string.Empty; // Ground height vs lift
    public string LutAoaCD { get; set; } = string.Empty; // Angle of attack vs drag
    public string LutGhCD { get; set; } = string.Empty; // Ground height vs drag
    
    // Coefficient gains/multipliers
    public double CLGain { get; set; } // Lift coefficient multiplier
    public double CDGain { get; set; } // Drag coefficient multiplier
    
    // Base coefficients from LUT files (at 0 degrees AOA)
    public double BaseCd { get; set; } // Actual drag coefficient from LUT
    public double BaseCl { get; set; } // Actual lift coefficient from LUT
    
    public double Angle { get; set; } // degrees
    public double YawCLGain { get; set; }
    
    // Damage zones
    public double ZoneFrontCL { get; set; }
    public double ZoneFrontCD { get; set; }
    public double ZoneRearCL { get; set; }
    public double ZoneRearCD { get; set; }
    public double ZoneLeftCL { get; set; }
    public double ZoneLeftCD { get; set; }
    public double ZoneRightCL { get; set; }
    public double ZoneRightCD { get; set; }
    
    /// <summary>
    /// Frontal area of this element (chord * span)
    /// </summary>
    public double FrontalArea => Chord * Span;
    
    /// <summary>
    /// Effective drag coefficient (BaseCd * CDGain)
    /// </summary>
    public double EffectiveCd => BaseCd > 0 ? BaseCd * CDGain : CDGain;
    
    /// <summary>
    /// Effective lift coefficient (BaseCl * CLGain)
    /// </summary>
    public double EffectiveCl => BaseCl * CLGain;
    
    /// <summary>
    /// Is this element producing downforce (negative lift)?
    /// </summary>
    public bool IsDownforceElement => CLGain < 0 || Name.Contains("DIFFUSER", StringComparison.OrdinalIgnoreCase);
    
    /// <summary>
    /// Position relative to center - positive = front
    /// </summary>
    public double LongitudinalPosition => Position.Length > 2 ? Position[2] : 0;
}
