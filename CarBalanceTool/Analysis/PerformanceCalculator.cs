using CarBalanceTool.Models;

namespace CarBalanceTool.Analysis;

/// <summary>
/// Calculates performance metrics for cars
/// </summary>
public static class PerformanceCalculator
{
    // Physics constants
    private const float Gravity = 9.81f;
    private const float AirDensity = 1.225f; // kg/m³ at sea level
    private const float RollingResistanceCoeff = 0.015f;

    /// <summary>
    /// Calculate all performance metrics for a car
    /// </summary>
    public static PerformanceMetrics Calculate(CarData car)
    {
        var metrics = new PerformanceMetrics();

        // Drivetrain efficiency
        metrics.DrivetrainEfficiency = car.Drivetrain.Type switch
        {
            DrivetrainType.RWD => 0.85f,
            DrivetrainType.FWD => 0.88f,
            DrivetrainType.AWD => 0.78f,
            _ => 0.85f
        };

        // Power calculations
        var peakPowerHp = car.Engine.PeakPowerHp;
        metrics.WheelPowerHp = peakPowerHp * metrics.DrivetrainEfficiency;
        metrics.PowerToWeight = metrics.WheelPowerHp / car.Basic.TotalMass;

        // Theoretical top speed (limited by rev limiter in top gear)
        if (car.Drivetrain.GearRatios.Count > 0 && car.Tyres.Rear.Radius > 0)
        {
            var topGearRatio = car.Drivetrain.GearRatios[^1] * car.Drivetrain.FinalDrive;
            var wheelCircumference = car.Tyres.Rear.Circumference;
            var revLimiter = car.Engine.Limiter;

            // Speed = (RPM / ratio) * circumference * 60 / 1000 for km/h
            metrics.TheoreticalTopSpeed = (revLimiter / topGearRatio) * wheelCircumference * 60 / 1000;
        }

        // Braking metric
        metrics.BrakingPerKg = car.Brakes.MaxTorque / car.Basic.TotalMass;

        // Average tyre grip
        metrics.AverageTyreGrip = (car.Tyres.Front.DY0 + car.Tyres.Front.DX0 +
                                    car.Tyres.Rear.DY0 + car.Tyres.Rear.DX0) / 4;

        // Estimate 0-100 and 0-200 times (km/h)
        metrics.Est0To100 = EstimateAccelerationTime(car, 100, metrics.DrivetrainEfficiency);
        metrics.Est0To200 = EstimateAccelerationTime(car, 200, metrics.DrivetrainEfficiency);

        // Estimate 0-60 and 0-100 mph times (converted to km/h for calculation)
        // 60 mph = 96.56 km/h, 100 mph = 160.93 km/h
        metrics.Est0To60Mph = EstimateAccelerationTime(car, 96.56f, metrics.DrivetrainEfficiency);
        metrics.Est0To100Mph = EstimateAccelerationTime(car, 160.93f, metrics.DrivetrainEfficiency);

        // Calculate overall performance score (weighted combination)
        metrics.PerformanceScore = CalculatePerformanceScore(car, metrics);

        // Perform detailed speed analysis
        metrics.SpeedAnalysis = AnalyzeTopSpeed(car, metrics);

        // Perform clutch analysis
        metrics.ClutchAnalysis = AnalyzeClutch(car);

        return metrics;
    }

    /// <summary>
    /// Analyze what's limiting the car's top speed
    /// </summary>
    private static TopSpeedAnalysis AnalyzeTopSpeed(CarData car, PerformanceMetrics metrics)
    {
        var analysis = new TopSpeedAnalysis();

        if (car.Drivetrain.GearRatios.Count == 0 || car.Tyres.Rear.Radius <= 0)
        {
            analysis.LimitingFactor = SpeedLimitFactor.Unknown;
            analysis.Analysis = "Insufficient data for speed analysis";
            return analysis;
        }

        // Calculate gearing-limited top speed (rev limiter in top gear)
        var topGearRatio = car.Drivetrain.GearRatios[^1] * car.Drivetrain.FinalDrive;
        var wheelCircumference = car.Tyres.Rear.Circumference;
        var revLimiter = car.Engine.Limiter;
        analysis.GearingLimitedSpeed = (revLimiter / topGearRatio) * wheelCircumference * 60 / 1000;

        // Calculate power-limited top speed using physics
        // At top speed: Power = Drag force * velocity
        // Drag = 0.5 * rho * Cd * A * v^2
        // Power = 0.5 * rho * Cd * A * v^3
        // v = (2 * Power / (rho * Cd * A))^(1/3)

        var peakPowerWatts = metrics.WheelPowerHp * 745.7f; // HP to Watts
        var dragCoeff = car.Aero.TotalDragGain;

        // Use a reasonable drag area if not available
        if (dragCoeff <= 0)
            dragCoeff = 0.35f * 2.0f; // Typical car Cd * frontal area

        // Calculate theoretical power-limited top speed
        // Adding rolling resistance: F_total = F_drag + F_roll
        // At equilibrium: P = (0.5 * rho * Cd * v^2 + m * g * Crr) * v
        // This is a cubic equation, solve iteratively
        float powerLimitedSpeedMs = 0;
        for (float v = 1; v < 150; v += 0.5f) // Up to ~540 km/h
        {
            var dragForce = 0.5f * AirDensity * dragCoeff * v * v;
            var rollingForce = car.Basic.TotalMass * Gravity * RollingResistanceCoeff;
            var totalResistance = dragForce + rollingForce;
            var powerNeeded = totalResistance * v;

            if (powerNeeded > peakPowerWatts)
            {
                powerLimitedSpeedMs = v;
                break;
            }
        }

        // Fallback if iteration didn't converge
        if (powerLimitedSpeedMs <= 0)
        {
            // Simplified: v = (2P / (rho * Cd))^(1/3)
            powerLimitedSpeedMs = MathF.Pow(2 * peakPowerWatts / (AirDensity * dragCoeff), 1f / 3f);
        }

        analysis.PowerLimitedSpeed = powerLimitedSpeedMs * 3.6f; // m/s to km/h

        // Determine limiting factor
        if (analysis.GearingLimitedSpeed < analysis.PowerLimitedSpeed * 0.95f)
        {
            analysis.LimitingFactor = SpeedLimitFactor.Gearing;
            analysis.Analysis = $"Speed limited by gearing ({analysis.GearingLimitedSpeed:F0} km/h). " +
                $"Power could achieve {analysis.PowerLimitedSpeed:F0} km/h with longer gearing.";
        }
        else if (analysis.PowerLimitedSpeed < analysis.GearingLimitedSpeed * 0.95f)
        {
            analysis.LimitingFactor = SpeedLimitFactor.Power;
            analysis.Analysis = $"Speed limited by power/drag ({analysis.PowerLimitedSpeed:F0} km/h). " +
                $"Won't reach rev limiter speed of {analysis.GearingLimitedSpeed:F0} km/h in top gear.";
        }
        else
        {
            analysis.LimitingFactor = SpeedLimitFactor.Gearing;
            analysis.Analysis = $"Well-matched: gearing ({analysis.GearingLimitedSpeed:F0} km/h) and power " +
                $"({analysis.PowerLimitedSpeed:F0} km/h) limits are close.";
        }

        // Check for unrealistic combinations
        // A typical sports car can achieve about 5-6 km/h per HP with good aero
        // Hypercars are around 3-4 km/h per HP
        var speedPerHp = analysis.GearingLimitedSpeed / car.Engine.PeakPowerHp;

        // Very rough reference: Bugatti Chiron = 420 km/h / 1500 HP = 0.28 km/h per HP
        // Sports car reference: 911 Turbo = 320 km/h / 640 HP = 0.5 km/h per HP
        // If ratio is way off, flag as unrealistic
        if (speedPerHp > 0.8f && analysis.GearingLimitedSpeed > 300)
        {
            analysis.IsUnrealistic = true;
            analysis.UnrealisticReason = $"Top speed of {analysis.GearingLimitedSpeed:F0} km/h ({analysis.GearingLimitedSpeed * 0.621371f:F0} mph) " +
                $"seems unrealistic for {car.Engine.PeakPowerHp:F0} HP. This may indicate incorrect gearing or power.lut values. " +
                $"Consider reducing top gear ratio or checking power curve.";
        }
        else if (analysis.GearingLimitedSpeed > 450)
        {
            analysis.IsUnrealistic = true;
            analysis.UnrealisticReason = $"Top speed of {analysis.GearingLimitedSpeed:F0} km/h ({analysis.GearingLimitedSpeed * 0.621371f:F0} mph) " +
                $"exceeds land speed records for road cars. Check gearing or modify power.lut.";
        }

        return analysis;
    }

    /// <summary>
    /// Analyze if clutch can handle engine torque
    /// </summary>
    private static ClutchAnalysis AnalyzeClutch(CarData car)
    {
        var analysis = new ClutchAnalysis
        {
            ClutchMaxTorque = car.Drivetrain.ClutchMaxTorque,
            EnginePeakTorqueBoosted = car.Engine.PeakTorqueBoosted
        };

        if (analysis.ClutchMaxTorque <= 0)
        {
            analysis.IsAdequate = true;
            analysis.HeadroomPercent = 100;
            analysis.Analysis = "Clutch torque not specified (assumed adequate)";
            return analysis;
        }

        // Calculate headroom
        if (analysis.EnginePeakTorqueBoosted > 0)
        {
            analysis.HeadroomPercent = ((analysis.ClutchMaxTorque - analysis.EnginePeakTorqueBoosted) / analysis.EnginePeakTorqueBoosted) * 100;
            analysis.IsAdequate = analysis.ClutchMaxTorque >= analysis.EnginePeakTorqueBoosted;

            if (analysis.IsAdequate)
            {
                if (analysis.HeadroomPercent > 50)
                {
                    analysis.Analysis = $"Clutch has {analysis.HeadroomPercent:F0}% headroom over engine torque (very strong)";
                }
                else if (analysis.HeadroomPercent > 20)
                {
                    analysis.Analysis = $"Clutch has {analysis.HeadroomPercent:F0}% headroom over engine torque (adequate)";
                }
                else
                {
                    analysis.Analysis = $"Clutch has only {analysis.HeadroomPercent:F0}% headroom - may slip under hard launches";
                }
            }
            else
            {
                analysis.Analysis = $"CLUTCH UNDERSIZED: {analysis.ClutchMaxTorque:F0} Nm clutch cannot handle " +
                    $"{analysis.EnginePeakTorqueBoosted:F0} Nm engine torque! Will slip and cause issues.";
            }
        }
        else
        {
            analysis.IsAdequate = true;
            analysis.HeadroomPercent = 100;
            analysis.Analysis = "No engine torque data to compare";
        }

        return analysis;
    }

    /// <summary>
    /// Estimate time to reach a target speed from standstill
    /// </summary>
    private static float EstimateAccelerationTime(CarData car, float targetSpeedKmh, float drivetrainEfficiency)
    {
        if (car.Drivetrain.GearRatios.Count == 0 || car.Engine.PowerCurve.Count == 0)
            return 999f;

        var mass = car.Basic.TotalMass;
        var wheelRadius = car.Tyres.Rear.Radius;
        var finalDrive = car.Drivetrain.FinalDrive;
        var maxBoost = 1 + car.Engine.MaxBoost;

        float currentSpeed = 0; // m/s
        float targetSpeed = targetSpeedKmh / 3.6f; // Convert to m/s
        float time = 0;
        float dt = 0.01f; // 10ms time step

        int currentGear = 0;
        var shiftRpm = car.Engine.Limiter - 500; // Shift before limiter

        // Traction limit based on drivetrain type and tyres
        var tractionCoeff = car.Drivetrain.Type switch
        {
            DrivetrainType.AWD => (car.Tyres.Front.DX0 + car.Tyres.Rear.DX0) / 2 * 0.95f,
            DrivetrainType.FWD => car.Tyres.Front.DX0 * 0.85f,
            DrivetrainType.RWD => car.Tyres.Rear.DX0 * 0.80f, // RWD loses more to wheelspin
            _ => car.Tyres.Rear.DX0 * 0.80f
        };

        var maxTractionForce = mass * Gravity * tractionCoeff;

        while (currentSpeed < targetSpeed && time < 60) // Max 60 seconds
        {
            // Calculate wheel RPM from speed
            var wheelRpm = (currentSpeed / wheelRadius) * 60 / (2 * MathF.PI);

            // Calculate engine RPM
            var gearRatio = car.Drivetrain.GearRatios[currentGear];
            var overallRatio = gearRatio * finalDrive;
            var engineRpm = wheelRpm * overallRatio;

            // Shift up if needed
            if (engineRpm >= shiftRpm && currentGear < car.Drivetrain.GearRatios.Count - 1)
            {
                currentGear++;
                time += car.Drivetrain.ShiftUpTime / 1000f; // Add shift time
                continue;
            }

            // Clamp engine RPM
            engineRpm = Math.Clamp(engineRpm, car.Engine.Minimum, car.Engine.Limiter);

            // Get torque at current RPM (interpolate power curve)
            var torque = InterpolateTorque(car.Engine.PowerCurve, (int)engineRpm);

            // Apply boost (simplified - full boost above reference RPM)
            var turbo = car.Engine.Turbos.FirstOrDefault();
            if (turbo != null && engineRpm >= turbo.ReferenceRpm)
            {
                torque *= maxBoost;
            }
            else if (turbo != null)
            {
                // Partial boost below reference RPM
                var boostRatio = engineRpm / turbo.ReferenceRpm;
                torque *= 1 + (maxBoost - 1) * boostRatio;
            }

            // Calculate wheel force
            var wheelTorque = torque * overallRatio * drivetrainEfficiency;
            var wheelForce = wheelTorque / wheelRadius;

            // Apply traction limit
            wheelForce = MathF.Min(wheelForce, maxTractionForce);

            // Calculate drag and rolling resistance
            var dragForce = 0.5f * AirDensity * car.Aero.TotalDragGain * currentSpeed * currentSpeed;
            var rollingResistance = mass * Gravity * RollingResistanceCoeff;

            // Net force and acceleration
            var netForce = wheelForce - dragForce - rollingResistance;
            var acceleration = netForce / mass;

            // Update speed
            currentSpeed += acceleration * dt;
            time += dt;
        }

        return time;
    }

    /// <summary>
    /// Interpolate torque at given RPM from power curve
    /// </summary>
    private static float InterpolateTorque(List<PowerPoint> curve, int rpm)
    {
        if (curve.Count == 0)
            return 0;

        if (curve.Count == 1)
            return curve[0].Torque;

        // Find surrounding points
        for (int i = 0; i < curve.Count - 1; i++)
        {
            if (rpm >= curve[i].Rpm && rpm <= curve[i + 1].Rpm)
            {
                var t = (float)(rpm - curve[i].Rpm) / (curve[i + 1].Rpm - curve[i].Rpm);
                return curve[i].Torque + t * (curve[i + 1].Torque - curve[i].Torque);
            }
        }

        // Extrapolate
        if (rpm < curve[0].Rpm)
            return curve[0].Torque;

        return curve[^1].Torque;
    }

    /// <summary>
    /// Calculate overall performance score
    /// </summary>
    private static float CalculatePerformanceScore(CarData car, PerformanceMetrics metrics)
    {
        // Weighted score (higher = better performance)
        // This is a simplified approximation for quick comparison

        var score = 0f;

        // Power to weight (40% weight) - normalized around 0.3 HP/kg
        score += (metrics.PowerToWeight / 0.3f) * 40;

        // Top speed (20% weight) - normalized around 300 km/h
        score += (metrics.TheoreticalTopSpeed / 300f) * 20;

        // 0-100 time (25% weight) - inverted, normalized around 4 seconds
        if (metrics.Est0To100 > 0 && metrics.Est0To100 < 60)
            score += (4f / metrics.Est0To100) * 25;

        // Braking (10% weight) - normalized around 3 Nm/kg
        score += (metrics.BrakingPerKg / 3f) * 10;

        // Tyre grip (5% weight) - normalized around 1.6
        score += (metrics.AverageTyreGrip / 1.6f) * 5;

        return score;
    }

    /// <summary>
    /// Calculate speed at given RPM in given gear
    /// </summary>
    public static float CalculateSpeed(CarData car, int rpm, int gearIndex)
    {
        if (gearIndex < 0 || gearIndex >= car.Drivetrain.GearRatios.Count)
            return 0;

        var gearRatio = car.Drivetrain.GearRatios[gearIndex];
        var overallRatio = gearRatio * car.Drivetrain.FinalDrive;
        var wheelCircumference = car.Tyres.Rear.Circumference;

        // Speed in km/h
        return (rpm / overallRatio) * wheelCircumference * 60 / 1000;
    }

    /// <summary>
    /// Calculate RPM at given speed in given gear
    /// </summary>
    public static float CalculateRpm(CarData car, float speedKmh, int gearIndex)
    {
        if (gearIndex < 0 || gearIndex >= car.Drivetrain.GearRatios.Count)
            return 0;

        var gearRatio = car.Drivetrain.GearRatios[gearIndex];
        var overallRatio = gearRatio * car.Drivetrain.FinalDrive;
        var wheelCircumference = car.Tyres.Rear.Circumference;

        // Speed in m/s
        var speedMs = speedKmh / 3.6f;
        var wheelRpm = (speedMs / wheelCircumference) * 60;

        return wheelRpm * overallRatio;
    }
}
