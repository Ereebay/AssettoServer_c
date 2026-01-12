namespace ACDyno.Models;

/// <summary>
/// Brake configuration from brakes.ini
/// </summary>
public class BrakeData
{
    public int Version { get; set; }
    
    public double MaxTorque { get; set; } // Nm
    public double FrontShare { get; set; } // 0-1 percentage
    public double HandbrakeTorque { get; set; } // Nm
    public bool CockpitAdjustable { get; set; }
    public double AdjustStep { get; set; } // percentage per step
    
    // Graphics
    public string DiscLF { get; set; } = string.Empty;
    public string DiscRF { get; set; } = string.Empty;
    public string DiscLR { get; set; } = string.Empty;
    public string DiscRR { get; set; } = string.Empty;
    public double FrontMaxGlow { get; set; }
    public double RearMaxGlow { get; set; }
    public double LagHot { get; set; }
    public double LagCool { get; set; }
    
    /// <summary>
    /// Rear brake share
    /// </summary>
    public double RearShare => 1 - FrontShare;
    
    /// <summary>
    /// Front brake torque
    /// </summary>
    public double FrontTorque => MaxTorque * FrontShare;
    
    /// <summary>
    /// Rear brake torque
    /// </summary>
    public double RearTorque => MaxTorque * RearShare;
    
    /// <summary>
    /// Brake bias percentage to front (for display)
    /// </summary>
    public double FrontBiasPercent => FrontShare * 100;
    
    /// <summary>
    /// Calculate theoretical deceleration (G) given car mass and tire grip
    /// </summary>
    public double TheoreticalDecel(double mass, double gripCoeff, double tyreRadius)
    {
        // Max braking force = friction * normal force = grip * mass * g
        double maxBrakeForce = gripCoeff * mass * 9.81;
        // Deceleration = Force / mass
        return maxBrakeForce / mass / 9.81; // in G
    }
    
    /// <summary>
    /// Calculate if brake torque is sufficient for given car
    /// </summary>
    public bool IsBrakeTorqueSufficient(double mass, double tyreRadius, double gripCoeff)
    {
        // Required torque per wheel = (mass * grip * g * radius) / 4
        double requiredTorque = (mass * gripCoeff * 9.81 * tyreRadius) / 2; // per axle
        return MaxTorque >= requiredTorque;
    }
}
