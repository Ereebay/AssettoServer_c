namespace ACDyno.Models;

/// <summary>
/// Suspension configuration from suspensions.ini
/// </summary>
public class SuspensionData
{
    public int Version { get; set; }
    
    // BASIC section
    public double Wheelbase { get; set; } // meters
    public double CgLocation { get; set; } // Front weight distribution percentage
    
    // ARB section
    public double FrontArb { get; set; } // Nm
    public double RearArb { get; set; } // Nm
    
    // Individual axle data
    public SuspensionAxle Front { get; set; } = new();
    public SuspensionAxle Rear { get; set; } = new();
    
    /// <summary>
    /// ARB ratio (higher = more understeer tendency)
    /// </summary>
    public double ArbRatio => RearArb > 0 ? FrontArb / RearArb : 0;
    
    /// <summary>
    /// Total roll stiffness
    /// </summary>
    public double TotalRollStiffness => FrontArb + RearArb;
    
    /// <summary>
    /// Spring rate ratio front/rear (higher = stiffer front)
    /// </summary>
    public double SpringRatio => Rear.SpringRate > 0 
        ? Front.SpringRate / Rear.SpringRate 
        : 0;
    
    /// <summary>
    /// Rear weight distribution percentage
    /// </summary>
    public double RearWeight => 100 - CgLocation;
}

/// <summary>
/// Single axle suspension configuration
/// </summary>
public class SuspensionAxle
{
    public SuspensionType Type { get; set; }
    
    // Basic geometry
    public double BaseY { get; set; } // CoG height offset
    public double Track { get; set; } // Track width in meters
    public double RodLength { get; set; } // Push/pull rod length
    public double HubMass { get; set; } // Unsprung mass (kg)
    public double RimOffset { get; set; }
    
    // Alignment
    public double ToeOut { get; set; } // As steering arm length
    public double StaticCamber { get; set; } // degrees
    
    // Springs
    public double SpringRate { get; set; } // N/m
    public double ProgressiveSpringRate { get; set; } // N/m/m
    
    // Bump stops
    public double BumpStopRate { get; set; } // N/m
    public double BumpstopUp { get; set; } // meters to upper bumpstop
    public double BumpstopDown { get; set; } // meters to lower bumpstop
    public double PackerRange { get; set; } // Total travel before packers
    
    // Damping
    public double DampBump { get; set; } // N sec/m
    public double DampFastBump { get; set; }
    public double DampFastBumpThreshold { get; set; }
    public double DampRebound { get; set; } // N sec/m
    public double DampFastRebound { get; set; }
    public double DampFastReboundThreshold { get; set; }
    
    // Geometry points (for DWB/Strut types)
    public double[] WbCarTopFront { get; set; } = new double[3];
    public double[] WbCarTopRear { get; set; } = new double[3];
    public double[] WbCarBottomFront { get; set; } = new double[3];
    public double[] WbCarBottomRear { get; set; } = new double[3];
    public double[] WbTyreTop { get; set; } = new double[3];
    public double[] WbTyreBottom { get; set; } = new double[3];
    public double[] WbCarSteer { get; set; } = new double[3];
    public double[] WbTyreSteer { get; set; } = new double[3];
    
    // Strut specific
    public double[] StrutCar { get; set; } = new double[3];
    public double[] StrutTyre { get; set; } = new double[3];
    
    /// <summary>
    /// Natural frequency of spring/mass system (Hz)
    /// Assumes typical corner mass
    /// </summary>
    public double NaturalFrequency(double cornerMass)
    {
        if (cornerMass <= 0) return 0;
        return Math.Sqrt(SpringRate / cornerMass) / (2 * Math.PI);
    }
    
    /// <summary>
    /// Damping ratio (0.5-0.7 typical, >1 overdamped)
    /// </summary>
    public double DampingRatio(double cornerMass)
    {
        if (cornerMass <= 0 || SpringRate <= 0) return 0;
        double criticalDamping = 2 * Math.Sqrt(SpringRate * cornerMass);
        return (DampBump + DampRebound) / 2 / criticalDamping;
    }
    
    /// <summary>
    /// Total suspension travel (up + down)
    /// </summary>
    public double TotalTravel => BumpstopUp + BumpstopDown;
}

public enum SuspensionType
{
    DWB, // Double wishbone
    STRUT, // MacPherson strut
    AXLE, // Solid axle
    ML // Multilink
}
