namespace ACDyno.Models;

/// <summary>
/// Engine configuration from engine.ini
/// </summary>
public class EngineData
{
    public int Version { get; set; }
    public string PowerCurveFile { get; set; } = "power.lut";
    public string CoastCurve { get; set; } = string.Empty;
    
    // ENGINE_DATA section
    public double AltitudeSensitivity { get; set; }
    public double Inertia { get; set; }
    public int Limiter { get; set; } // RPM
    public int LimiterHz { get; set; }
    public int MinimumRpm { get; set; } // Idle
    public double DefaultTurboAdjustment { get; set; } = 1.0;
    
    // COAST_REF section
    public int CoastRefRpm { get; set; }
    public double CoastRefTorque { get; set; }
    public double CoastNonLinearity { get; set; }
    
    // Turbo data (can have multiple turbos)
    public List<TurboData> Turbos { get; set; } = new();
    
    // Calculated properties
    public double PeakTorqueNa => PowerCurve?.PeakTorque ?? 0;
    public int PeakTorqueRpm => PowerCurve?.PeakTorqueRpm ?? 0;
    public double PeakPowerHp => PowerCurve?.PeakPowerHp ?? 0;
    public int PeakPowerRpm => PowerCurve?.PeakPowerRpm ?? 0;
    
    // Reference to power curve for calculations
    public PowerCurve? PowerCurve { get; set; }
    
    /// <summary>
    /// Calculate total boost multiplier from all turbos at given RPM
    /// </summary>
    public double GetBoostMultiplier(int rpm, double throttle = 1.0)
    {
        if (Turbos.Count == 0) return 1.0;
        
        double totalBoost = 0;
        foreach (var turbo in Turbos)
        {
            totalBoost += turbo.GetBoostAtRpm(rpm, throttle);
        }
        return 1.0 + totalBoost;
    }
    
    /// <summary>
    /// Get boosted torque at given RPM
    /// </summary>
    public double GetBoostedTorque(int rpm)
    {
        double baseTorque = PowerCurve?.GetTorqueAtRpm(rpm) ?? 0;
        return baseTorque * GetBoostMultiplier(rpm);
    }
    
    /// <summary>
    /// Get peak boosted torque
    /// </summary>
    public double GetPeakBoostedTorque()
    {
        if (PowerCurve == null) return 0;
        
        double peakTorque = 0;
        for (int rpm = MinimumRpm; rpm <= Limiter; rpm += 100)
        {
            double torque = GetBoostedTorque(rpm);
            if (torque > peakTorque) peakTorque = torque;
        }
        return peakTorque;
    }
    
    /// <summary>
    /// Get peak boosted power in HP
    /// </summary>
    public double GetPeakBoostedPowerHp()
    {
        if (PowerCurve == null) return 0;
        
        double peakPower = 0;
        for (int rpm = MinimumRpm; rpm <= Limiter; rpm += 100)
        {
            double torque = GetBoostedTorque(rpm);
            double power = (torque * rpm) / 7120.77; // Convert Nm*RPM to HP
            if (power > peakPower) peakPower = power;
        }
        return peakPower;
    }
}

/// <summary>
/// Turbo configuration from TURBO_X sections
/// </summary>
public class TurboData
{
    public int Index { get; set; }
    public double LagDown { get; set; }
    public double LagUp { get; set; }
    public double MaxBoost { get; set; }
    public double Wastegate { get; set; }
    public double DisplayMaxBoost { get; set; }
    public int ReferenceRpm { get; set; }
    public double Gamma { get; set; }
    public bool CockpitAdjustable { get; set; }
    
    /// <summary>
    /// Calculate boost at given RPM (simplified model)
    /// </summary>
    public double GetBoostAtRpm(int rpm, double throttle = 1.0)
    {
        if (rpm < ReferenceRpm)
        {
            // Linear ramp up to reference RPM
            double rampFactor = (double)rpm / ReferenceRpm;
            rampFactor = Math.Pow(rampFactor, 1.0 / Gamma);
            return MaxBoost * rampFactor * Math.Pow(throttle, Gamma);
        }
        else
        {
            // Full boost above reference RPM
            return Math.Min(MaxBoost, Wastegate) * Math.Pow(throttle, Gamma);
        }
    }
}

/// <summary>
/// Power curve data from power.lut
/// </summary>
public class PowerCurve
{
    public List<PowerPoint> Points { get; set; } = new();
    
    public double PeakTorque => Points.Count > 0 ? Points.Max(p => p.Torque) : 0;
    public int PeakTorqueRpm => Points.Count > 0 ? Points.MaxBy(p => p.Torque)?.Rpm ?? 0 : 0;
    
    public double PeakPowerHp
    {
        get
        {
            if (Points.Count == 0) return 0;
            return Points.Max(p => p.PowerHp);
        }
    }
    
    public int PeakPowerRpm
    {
        get
        {
            if (Points.Count == 0) return 0;
            return Points.MaxBy(p => p.PowerHp)?.Rpm ?? 0;
        }
    }
    
    /// <summary>
    /// Interpolate torque at given RPM
    /// </summary>
    public double GetTorqueAtRpm(int rpm)
    {
        if (Points.Count == 0) return 0;
        if (Points.Count == 1) return Points[0].Torque;
        
        // Find surrounding points
        var sorted = Points.OrderBy(p => p.Rpm).ToList();
        
        if (rpm <= sorted.First().Rpm) return sorted.First().Torque;
        if (rpm >= sorted.Last().Rpm) return sorted.Last().Torque;
        
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            if (rpm >= sorted[i].Rpm && rpm <= sorted[i + 1].Rpm)
            {
                // Linear interpolation
                double t = (double)(rpm - sorted[i].Rpm) / (sorted[i + 1].Rpm - sorted[i].Rpm);
                return sorted[i].Torque + t * (sorted[i + 1].Torque - sorted[i].Torque);
            }
        }
        
        return 0;
    }
}

/// <summary>
/// Single point on the power curve
/// </summary>
public class PowerPoint
{
    public int Rpm { get; set; }
    public double Torque { get; set; } // Nm
    
    public double PowerHp => (Torque * Rpm) / 7120.77; // Convert to HP
    public double PowerKw => PowerHp * 0.7457; // Convert to kW
    
    public PowerPoint() { }
    
    public PowerPoint(int rpm, double torque)
    {
        Rpm = rpm;
        Torque = torque;
    }
}
