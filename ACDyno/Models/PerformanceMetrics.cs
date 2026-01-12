namespace ACDyno.Models;

/// <summary>
/// Physics-based performance metrics calculated from car data.
/// Based on motorsport engineering formulas, dyno software methods, and Forza PI system research.
/// All internal calculations use SI units (meters, seconds, kg, Watts).
/// </summary>
public class PerformanceMetrics
{
    #region Raw Power Metrics
    public double PeakPowerHp { get; set; }
    public double PeakPowerKw => PeakPowerHp * Units.HP_TO_KW;
    public int PeakPowerRpm { get; set; }
    public double PeakTorqueNm { get; set; }
    public double PeakTorqueLbFt => PeakTorqueNm * Units.NM_TO_LBFT;
    public int PeakTorqueRpm { get; set; }
    
    // Boosted power (for turbo cars)
    public double PeakBoostedPowerHp { get; set; }
    public double PeakBoostedTorqueNm { get; set; }
    #endregion
    
    #region Drivetrain Metrics
    public DriveLayout DriveLayout { get; set; }
    public int GearCount { get; set; }
    public double FinalDriveRatio { get; set; }
    public double TopGearRatio { get; set; }
    
    /// <summary>
    /// Drivetrain efficiency: FWD=0.88, RWD=0.85, AWD=0.80
    /// </summary>
    public double DrivetrainEfficiency { get; set; }
    
    /// <summary>
    /// Power at the wheels after drivetrain losses (HP)
    /// </summary>
    public double WheelPowerHp { get; set; }
    
    /// <summary>
    /// Power at the wheels (Watts) for physics calculations
    /// </summary>
    public double WheelPowerW => WheelPowerHp * Units.HP_TO_W;
    #endregion
    
    #region Weight Metrics
    public double CurbWeightKg { get; set; }
    public double CurbWeightLbs => CurbWeightKg * Units.KG_TO_LB;
    
    /// <summary>
    /// Weight with driver (70kg) added for performance calcs
    /// </summary>
    public double TestWeightKg => CurbWeightKg + 70;
    public double TestWeightLbs => TestWeightKg * Units.KG_TO_LB;
    
    /// <summary>
    /// Front weight distribution (0.0-1.0, 0.5 = 50/50)
    /// </summary>
    public double WeightDistributionFront { get; set; }
    
    /// <summary>
    /// Power to weight ratio (HP per metric ton)
    /// </summary>
    public double PowerToWeightHpPerTon { get; set; }
    
    /// <summary>
    /// Wheel power to weight ratio (more accurate for acceleration)
    /// </summary>
    public double WheelPowerToWeightHpPerTon => WheelPowerHp / (CurbWeightKg * Units.KG_TO_TON);
    #endregion
    
    #region Tire Grip Metrics
    /// <summary>Front lateral grip coefficient (DY0)</summary>
    public double FrontLateralGrip { get; set; }
    /// <summary>Rear lateral grip coefficient (DY0)</summary>
    public double RearLateralGrip { get; set; }
    /// <summary>Front longitudinal grip coefficient (DX0)</summary>
    public double FrontLongitudinalGrip { get; set; }
    /// <summary>Rear longitudinal grip coefficient (DX0)</summary>
    public double RearLongitudinalGrip { get; set; }
    
    /// <summary>Average lateral grip (primary handling indicator)</summary>
    public double AverageLateralGrip => (FrontLateralGrip + RearLateralGrip) / 2;
    /// <summary>Average longitudinal grip (acceleration/braking)</summary>
    public double AverageLongitudinalGrip => (FrontLongitudinalGrip + RearLongitudinalGrip) / 2;
    /// <summary>Overall effective grip coefficient</summary>
    public double EffectiveGrip => (AverageLateralGrip + AverageLongitudinalGrip) / 2;
    
    /// <summary>Grip balance (positive = rear has more grip = understeer tendency)</summary>
    public double GripBalance => RearLateralGrip - FrontLateralGrip;
    
    public double FrontTireWidthMm { get; set; }
    public double RearTireWidthMm { get; set; }
    public double TireRadiusM { get; set; }
    public double TireCircumferenceM => 2 * Math.PI * TireRadiusM;
    #endregion
    
    #region Aerodynamic Metrics
    /// <summary>Drag coefficient (Cd)</summary>
    public double DragCoefficient { get; set; }
    /// <summary>Frontal area in m²</summary>
    public double FrontalArea { get; set; }
    /// <summary>Drag area (Cd × A) - key aero metric</summary>
    public double DragArea => DragCoefficient * FrontalArea;
    
    /// <summary>Lift/downforce coefficient (negative = downforce)</summary>
    public double LiftCoefficient { get; set; }
    /// <summary>Aero efficiency (|CL|/Cd)</summary>
    public double AeroEfficiency => DragCoefficient > 0 ? Math.Abs(LiftCoefficient) / DragCoefficient : 0;
    #endregion
    
    #region Brake Metrics
    public double BrakeTorqueNm { get; set; }
    /// <summary>Brake bias (0.0-1.0, front share)</summary>
    public double BrakeBias { get; set; }
    #endregion
    
    #region Calculated Top Speed
    /// <summary>Top speed limited by aerodynamic drag (m/s)</summary>
    public double DragLimitedTopSpeedMs { get; set; }
    /// <summary>Top speed limited by gearing (m/s)</summary>
    public double GearLimitedTopSpeedMs { get; set; }
    /// <summary>Actual theoretical top speed (m/s)</summary>
    public double TheoreticalTopSpeedMs => Math.Min(DragLimitedTopSpeedMs, GearLimitedTopSpeedMs);
    
    public double TopSpeedMph => TheoreticalTopSpeedMs * Units.MPS_TO_MPH;
    public double TopSpeedKmh => TheoreticalTopSpeedMs * Units.MPS_TO_KMH;
    
    public string TopSpeedLimitedBy => DragLimitedTopSpeedMs < GearLimitedTopSpeedMs ? "Drag" : "Gearing";
    #endregion
    
    #region Estimated Performance Figures
    // Acceleration
    public double Est0To60Mph { get; set; }
    public double Est0To100Kmh { get; set; }
    public double EstQuarterMileTime { get; set; }
    public double EstQuarterMileTrapMph { get; set; }
    
    // Braking
    public double Est60To0Ft { get; set; }
    public double Est60To0M => Est60To0Ft * Units.FT_TO_M;
    public double Est100To0M { get; set; }
    public double Est100To0Ft => Est100To0M * Units.M_TO_FT;
    
    // Cornering
    public double MaxLateralGBase { get; set; }
    public double MaxLateralGWithAero { get; set; }
    #endregion
    
    #region Component Scores (0-100 scale)
    /// <summary>Speed score based on theoretical top speed (80mph=0, 280mph=100)</summary>
    public double SpeedScore { get; set; }
    /// <summary>Acceleration score based on 0-60 time (12s=0, 2s=100)</summary>
    public double AccelerationScore { get; set; }
    /// <summary>Handling score based on max lateral G (0.6G=0, 2.5G=100)</summary>
    public double HandlingScore { get; set; }
    /// <summary>Braking score based on 60-0 distance (400ft=0, 85ft=100)</summary>
    public double BrakingScore { get; set; }
    /// <summary>Launch score from drivetrain, grip, and torque (composite)</summary>
    public double LaunchScore { get; set; }
    #endregion
    
    #region Performance Index
    /// <summary>Overall PI rating (100-999, or 999 for unrealistic)</summary>
    public int PerformanceIndex { get; set; }
    /// <summary>Performance class (D, C, B, A, S1, S2, S3, X)</summary>
    public PerformanceClass PerformanceClass { get; set; }
    #endregion
    
    #region Validation
    public List<ValidationWarning> Warnings { get; set; } = new();
    public bool IsUnrealistic { get; set; }
    #endregion
    
    /// <summary>
    /// Calculate all performance metrics from car data using physics-based formulas
    /// </summary>
    public static PerformanceMetrics Calculate(CarData car)
    {
        var m = new PerformanceMetrics();
        
        // === STEP 1: Extract raw data ===
        m.ExtractRawData(car);
        
        // === STEP 2: Calculate drivetrain metrics ===
        m.CalculateDrivetrainMetrics(car);
        
        // === STEP 3: Calculate top speed (drag vs gear limited) ===
        m.CalculateTopSpeed(car);
        
        // === STEP 4: Estimate acceleration times ===
        m.CalculateAcceleration(car);
        
        // === STEP 5: Estimate braking distances ===
        m.CalculateBraking(car);
        
        // === STEP 6: Calculate max lateral G ===
        m.CalculateMaxLateralG(car);
        
        // === STEP 7: Validate for unrealistic values ===
        m.ValidateRealism();
        
        // === STEP 8: Calculate component scores (0-100) ===
        m.CalculateComponentScores();
        
        // === STEP 9: Calculate final PI ===
        m.CalculatePerformanceIndex();
        
        return m;
    }
    
    private void ExtractRawData(CarData car)
    {
        // Weight
        CurbWeightKg = car.BasicInfo.TotalMass;
        if (CurbWeightKg <= 0) CurbWeightKg = 1500; // Default
        
        WeightDistributionFront = car.Suspension.CgLocation / 100.0; // Convert from percentage
        if (WeightDistributionFront <= 0 || WeightDistributionFront >= 1)
            WeightDistributionFront = 0.50;
        
        // Engine power
        if (car.Engine.Turbos.Count > 0)
        {
            PeakBoostedPowerHp = car.Engine.GetPeakBoostedPowerHp();
            PeakBoostedTorqueNm = car.Engine.GetPeakBoostedTorque();
            PeakPowerHp = PeakBoostedPowerHp;
            PeakTorqueNm = PeakBoostedTorqueNm;
        }
        else
        {
            PeakPowerHp = car.PowerCurve?.PeakPowerHp ?? 0;
            PeakTorqueNm = car.PowerCurve?.PeakTorque ?? 0;
            PeakBoostedPowerHp = PeakPowerHp;
            PeakBoostedTorqueNm = PeakTorqueNm;
        }
        
        if (PeakPowerHp <= 0) PeakPowerHp = 150; // Default
        if (PeakTorqueNm <= 0) PeakTorqueNm = 200;
        
        PeakPowerRpm = car.PowerCurve?.PeakPowerRpm ?? car.Engine.Limiter;
        PeakTorqueRpm = car.PowerCurve?.PeakTorqueRpm ?? (int)(car.Engine.Limiter * 0.6);
        
        // Drivetrain
        DriveLayout = car.Drivetrain.DriveLayout;
        GearCount = car.Drivetrain.GearCount;
        FinalDriveRatio = car.Drivetrain.FinalDrive;
        TopGearRatio = car.Drivetrain.GearRatios.Count > 0 
            ? car.Drivetrain.GearRatios.Last() 
            : 1.0;
        
        // Tire grip
        var tyres = car.Tyres?.DefaultCompound;
        if (tyres != null)
        {
            FrontLateralGrip = tyres.Front.DY0 > 0 ? tyres.Front.DY0 : 1.1;
            RearLateralGrip = tyres.Rear.DY0 > 0 ? tyres.Rear.DY0 : 1.1;
            FrontLongitudinalGrip = tyres.Front.DX0 > 0 ? tyres.Front.DX0 : 1.1;
            RearLongitudinalGrip = tyres.Rear.DX0 > 0 ? tyres.Rear.DX0 : 1.1;
            FrontTireWidthMm = tyres.Front.Width * 1000;
            RearTireWidthMm = tyres.Rear.Width * 1000;
            TireRadiusM = tyres.Front.Radius > 0 ? tyres.Front.Radius : 0.32;
        }
        else
        {
            FrontLateralGrip = RearLateralGrip = 1.1;
            FrontLongitudinalGrip = RearLongitudinalGrip = 1.1;
            FrontTireWidthMm = RearTireWidthMm = 225;
            TireRadiusM = 0.32;
        }
        
        // Aero - use actual body drag coefficient from LUT if available
        var bodyElement = car.Aero.BodyElement;
        if (bodyElement != null && bodyElement.BaseCd > 0)
        {
            DragCoefficient = bodyElement.EffectiveCd;
            FrontalArea = bodyElement.FrontalArea > 0 ? bodyElement.FrontalArea : EstimateFrontalArea(CurbWeightKg);
        }
        else
        {
            // Fallback: estimate from car type/weight
            DragCoefficient = 0.35; // Typical sports car Cd
            FrontalArea = EstimateFrontalArea(CurbWeightKg);
        }
        LiftCoefficient = car.Aero.TotalLiftGain;
        
        // Brakes
        BrakeTorqueNm = car.Brakes.MaxTorque > 0 ? car.Brakes.MaxTorque : 2000;
        BrakeBias = car.Brakes.FrontShare > 0 && car.Brakes.FrontShare < 1 
            ? car.Brakes.FrontShare 
            : 0.67;
    }
    
    private void CalculateDrivetrainMetrics(CarData car)
    {
        // Drivetrain efficiency based on layout
        DrivetrainEfficiency = DriveLayout switch
        {
            DriveLayout.FWD => 0.88,
            DriveLayout.RWD => 0.85,
            DriveLayout.AWD => 0.80,
            _ => 0.85
        };
        
        // Wheel power
        WheelPowerHp = PeakPowerHp * DrivetrainEfficiency;
        
        // Power to weight
        PowerToWeightHpPerTon = PeakPowerHp / (CurbWeightKg * Units.KG_TO_TON);
    }
    
    private void CalculateTopSpeed(CarData car)
    {
        // Drag-limited top speed: v = ∛(2P / (ρ × Cd × A))
        double dragArea = DragArea;
        if (dragArea <= 0) dragArea = 0.7; // Default Cd*A
        
        DragLimitedTopSpeedMs = Math.Pow(
            2 * WheelPowerW / (Units.AIR_DENSITY_SEA * dragArea),
            1.0 / 3.0
        );
        
        // Gear-limited top speed
        double overallTopGearRatio = TopGearRatio * FinalDriveRatio;
        if (overallTopGearRatio <= 0) overallTopGearRatio = 3.0;
        
        int redline = car.Engine.Limiter > 0 ? car.Engine.Limiter : 7000;
        GearLimitedTopSpeedMs = (redline * TireCircumferenceM) / (overallTopGearRatio * 60);
    }
    
    private void CalculateAcceleration(CarData car)
    {
        // === 0-60 mph calculation ===
        double v60 = Units.V_60_MPH;
        
        // Energy-based theoretical minimum
        double energyRequired = 0.5 * TestWeightKg * v60 * v60;
        double avgPowerFactor = 0.85; // Account for power curve, not always at peak
        double theoreticalTime = energyRequired / (WheelPowerW * avgPowerFactor);
        
        // Apply correction factors
        double shiftPenalty = 1.10; // Gear changes add ~10%
        double launchPenalty = GetLaunchPenalty();
        
        double powerLimitedTime = theoreticalTime * shiftPenalty * launchPenalty;
        
        // Traction-limited time
        double tractionG = EffectiveGrip * GetTractionWeightFraction();
        double tractionLimitedTime = v60 / (tractionG * Units.G);
        
        // Use whichever is slower (limiting factor)
        Est0To60Mph = Math.Max(powerLimitedTime, tractionLimitedTime * 1.1);
        Est0To60Mph = Math.Max(1.5, Est0To60Mph); // Physical minimum
        
        // 0-100 km/h (slightly longer distance)
        double v100 = Units.V_100_KMH;
        Est0To100Kmh = Est0To60Mph * (v100 * v100) / (v60 * v60);
        
        // Quarter mile (Hale formula)
        double weightLbs = TestWeightLbs;
        EstQuarterMileTime = 5.825 * Math.Pow(weightLbs / WheelPowerHp, 1.0 / 3.0);
        EstQuarterMileTrapMph = 234 * Math.Pow(WheelPowerHp / weightLbs, 1.0 / 3.0);
        
        // Apply traction penalty if car is traction-limited
        if (tractionLimitedTime > powerLimitedTime * 0.9)
        {
            EstQuarterMileTime *= 1.05; // 5% penalty for traction-limited launches
        }
    }
    
    private void CalculateBraking(CarData car)
    {
        double v60 = Units.V_60_MPH;
        double v100kmh = Units.V_100_KMH;
        
        // Average speed during braking (for aero calculations)
        double avgBrakeSpeed60 = 0.7 * v60;
        double avgBrakeSpeed100 = 0.7 * v100kmh;
        
        // Aero downforce contribution at braking speeds
        double aeroDownforce60 = CalculateAeroDownforceG(avgBrakeSpeed60);
        double aeroDownforce100 = CalculateAeroDownforceG(avgBrakeSpeed100);
        
        // Brake balance efficiency (optimal is front bias + weight transfer)
        double optimalBias = WeightDistributionFront + 0.08; // Weight shifts forward under braking
        double brakeEfficiency = 1.0 - Math.Abs(BrakeBias - optimalBias) * 0.4;
        brakeEfficiency = Math.Clamp(brakeEfficiency, 0.7, 1.0);
        
        // Effective braking G
        double brakeG60 = AverageLateralGrip * (1 + aeroDownforce60) * brakeEfficiency * 0.95;
        double brakeG100 = AverageLateralGrip * (1 + aeroDownforce100) * brakeEfficiency * 0.95;
        
        // Distance = v² / (2 × a)
        double dist60_m = (v60 * v60) / (2 * brakeG60 * Units.G);
        double dist100_m = (v100kmh * v100kmh) / (2 * brakeG100 * Units.G);
        
        Est60To0Ft = dist60_m * Units.M_TO_FT;
        Est100To0M = dist100_m;
    }
    
    private void CalculateMaxLateralG(CarData car)
    {
        // Base grip with weight distribution penalty
        double weightBalanceFactor = 1.0 - Math.Abs(WeightDistributionFront - 0.50) * 0.15;
        MaxLateralGBase = AverageLateralGrip * weightBalanceFactor;
        
        // Tire width bonus
        double avgTireWidth = (FrontTireWidthMm + RearTireWidthMm) / 2;
        double tireWidthBonus = 1.0 + (avgTireWidth - 205) / 500;
        tireWidthBonus = Math.Clamp(tireWidthBonus, 0.9, 1.15);
        
        MaxLateralGBase *= tireWidthBonus;
        
        // Aero contribution at 100 mph reference
        double aeroG = CalculateAeroDownforceG(Units.V_100_MPH);
        MaxLateralGWithAero = MaxLateralGBase + aeroG * 0.8;
    }
    
    private void ValidateRealism()
    {
        Warnings.Clear();
        IsUnrealistic = false;
        
        // Power-to-weight extremes
        if (PowerToWeightHpPerTon > 1500)
        {
            Warnings.Add(new ValidationWarning("PWR", $"Power-to-weight ({PowerToWeightHpPerTon:F0} HP/ton) exceeds F1 levels", ValidationSeverity.Critical));
            IsUnrealistic = true;
        }
        else if (PowerToWeightHpPerTon > 1000)
        {
            Warnings.Add(new ValidationWarning("PWR", $"Power-to-weight ({PowerToWeightHpPerTon:F0} HP/ton) is hypercar+ territory", ValidationSeverity.Warning));
        }
        
        // Mass extremes
        if (CurbWeightKg < 500)
        {
            Warnings.Add(new ValidationWarning("MASS", $"Mass ({CurbWeightKg:F0} kg) unrealistically low", ValidationSeverity.Critical));
            IsUnrealistic = true;
        }
        
        // Grip extremes
        if (EffectiveGrip > 2.0)
        {
            Warnings.Add(new ValidationWarning("GRIP", $"Grip coefficient ({EffectiveGrip:F2}) exceeds realistic bounds", ValidationSeverity.Critical));
            IsUnrealistic = true;
        }
        else if (EffectiveGrip < 0.5)
        {
            Warnings.Add(new ValidationWarning("GRIP", $"Grip coefficient ({EffectiveGrip:F2}) unrealistically low", ValidationSeverity.Critical));
            IsUnrealistic = true;
        }
        
        // Drag extremes
        if (DragCoefficient < 0.18)
        {
            Warnings.Add(new ValidationWarning("AERO", $"Drag coefficient ({DragCoefficient:F2}) below theoretical minimum", ValidationSeverity.Warning));
            IsUnrealistic = true;
        }
        
        // Downforce extremes
        if (Math.Abs(LiftCoefficient) > 5.0)
        {
            Warnings.Add(new ValidationWarning("AERO", $"Lift coefficient ({LiftCoefficient:F2}) unrealistically extreme", ValidationSeverity.Critical));
            IsUnrealistic = true;
        }
        
        // Performance extremes
        if (Est0To60Mph < 1.5)
        {
            Warnings.Add(new ValidationWarning("ACCEL", $"0-60 time ({Est0To60Mph:F1}s) below physical limits", ValidationSeverity.Critical));
            IsUnrealistic = true;
        }
        
        // Power extremes
        if (PeakPowerHp > 2500)
        {
            Warnings.Add(new ValidationWarning("POWER", $"Power ({PeakPowerHp:F0} HP) extremely high for street car", ValidationSeverity.Warning));
        }
        if (PeakPowerHp > 5000)
        {
            IsUnrealistic = true;
        }
    }
    
    private void CalculateComponentScores()
    {
        // Speed Score: 80 mph = 0, 280 mph = 100
        SpeedScore = Math.Clamp((TopSpeedMph - 80) / (280 - 80) * 100, 0, 100);
        
        // Acceleration Score: 12s = 0, 2s = 100
        AccelerationScore = Math.Clamp((12.0 - Est0To60Mph) / (12.0 - 2.0) * 100, 0, 100);
        
        // Handling Score: 0.6G = 0, 2.5G = 100
        HandlingScore = Math.Clamp((MaxLateralGWithAero - 0.6) / (2.5 - 0.6) * 100, 0, 100);
        
        // Braking Score: 400ft = 0, 85ft = 100
        BrakingScore = Math.Clamp((400 - Est60To0Ft) / (400 - 85) * 100, 0, 100);
        
        // Launch Score (composite)
        double drivetrainBonus = DriveLayout switch
        {
            DriveLayout.AWD => 35,
            DriveLayout.RWD => 20,
            DriveLayout.FWD => 15,
            _ => 20
        };
        double tractionBonus = Math.Min(30, AverageLateralGrip * 25);
        double torqueBonus = Math.Min(20, (PeakTorqueNm / CurbWeightKg) * 100);
        double weightBonus = Math.Min(15, (2000 - CurbWeightKg) / 100);
        weightBonus = Math.Max(0, weightBonus);
        
        LaunchScore = Math.Clamp(drivetrainBonus + tractionBonus + torqueBonus + weightBonus, 0, 100);
    }
    
    private void CalculatePerformanceIndex()
    {
        if (IsUnrealistic)
        {
            PerformanceIndex = 999;
            PerformanceClass = PerformanceClass.X;
            return;
        }
        
        // Weighted composite score
        double compositeScore =
            SpeedScore * 0.20 +
            AccelerationScore * 0.25 +
            HandlingScore * 0.30 +
            BrakingScore * 0.15 +
            LaunchScore * 0.10;
        
        // Map 0-100 composite to 100-999 PI range
        double piRaw = 100 + (compositeScore * 9.0);
        
        // Power-to-weight adjustment (strongest single predictor)
        double pwBonus = Math.Min(100, (PowerToWeightHpPerTon - 100) * 0.4);
        pwBonus = Math.Max(0, pwBonus);
        piRaw += pwBonus;
        
        // Diminishing returns at high performance
        if (piRaw > 800)
            piRaw = 800 + (piRaw - 800) * 0.6;
        if (piRaw > 900)
            piRaw = 900 + (piRaw - 900) * 0.4;
        
        PerformanceIndex = (int)Math.Clamp(piRaw, 100, 998);
        
        // Determine class
        PerformanceClass = PerformanceIndex switch
        {
            >= 901 => PerformanceClass.S3,
            >= 801 => PerformanceClass.S2,
            >= 701 => PerformanceClass.S1,
            >= 601 => PerformanceClass.A,
            >= 501 => PerformanceClass.B,
            >= 401 => PerformanceClass.C,
            _ => PerformanceClass.D
        };
    }
    
    #region Helper Methods
    private double GetTractionWeightFraction()
    {
        double weightTransfer = 0.10; // 10% weight transfer under hard acceleration
        return DriveLayout switch
        {
            DriveLayout.RWD => (1 - WeightDistributionFront) + weightTransfer,
            DriveLayout.FWD => WeightDistributionFront - weightTransfer,
            DriveLayout.AWD => 1.0, // All wheels drive
            _ => 0.5
        };
    }
    
    private double GetLaunchPenalty()
    {
        // AWD with high grip = best launch
        // FWD or low grip = worst launch
        double gripFactor = EffectiveGrip;
        
        return DriveLayout switch
        {
            DriveLayout.AWD when gripFactor > 1.3 => 1.05,
            DriveLayout.RWD when gripFactor > 1.2 => 1.10,
            DriveLayout.AWD => 1.08,
            DriveLayout.RWD => 1.12,
            DriveLayout.FWD => 1.18,
            _ => 1.15
        };
    }
    
    private double CalculateAeroDownforceG(double speedMs)
    {
        // Downforce = 0.5 × ρ × |CL| × A × v²
        double downforceN = 0.5 * Units.AIR_DENSITY_SEA * Math.Abs(LiftCoefficient) * FrontalArea * speedMs * speedMs;
        // Convert to G
        return downforceN / (CurbWeightKg * Units.G);
    }
    
    private static double EstimateFrontalArea(double massKg)
    {
        // Rough correlation: light car ≈ 1.8m², heavy car ≈ 2.5m²
        return 1.5 + (massKg - 800) * 0.0005;
    }
    #endregion
    
    #region Summary Generation
    public string GetSummary(bool useMetric = false)
    {
        if (useMetric)
        {
            return $"""
                Performance Index: {PerformanceIndex} ({PerformanceClass})
                
                Power: {PeakPowerHp:F0} HP ({PeakPowerKw:F0} kW) @ {PeakPowerRpm} RPM
                Torque: {PeakTorqueNm:F0} Nm @ {PeakTorqueRpm} RPM
                Weight: {CurbWeightKg:F0} kg
                Power/Weight: {PowerToWeightHpPerTon:F0} HP/ton
                
                Est. 0-100 km/h: {Est0To100Kmh:F1}s
                Est. Top Speed: {TopSpeedKmh:F0} km/h ({TopSpeedLimitedBy}-limited)
                Est. 100-0 km/h: {Est100To0M:F1}m
                Max Lateral G: {MaxLateralGWithAero:F2}g
                
                Scores: Spd {SpeedScore:F0} | Accel {AccelerationScore:F0} | Hndl {HandlingScore:F0} | Brk {BrakingScore:F0} | Lnch {LaunchScore:F0}
                """;
        }
        else
        {
            return $"""
                Performance Index: {PerformanceIndex} ({PerformanceClass})
                
                Power: {PeakPowerHp:F0} HP @ {PeakPowerRpm} RPM
                Torque: {PeakTorqueLbFt:F0} lb-ft @ {PeakTorqueRpm} RPM
                Weight: {CurbWeightLbs:F0} lbs
                Power/Weight: {PowerToWeightHpPerTon:F0} HP/ton
                
                Est. 0-60 mph: {Est0To60Mph:F1}s
                Est. 1/4 Mile: {EstQuarterMileTime:F1}s @ {EstQuarterMileTrapMph:F0} mph
                Est. Top Speed: {TopSpeedMph:F0} mph ({TopSpeedLimitedBy}-limited)
                Est. 60-0: {Est60To0Ft:F0} ft
                Max Lateral G: {MaxLateralGWithAero:F2}g
                
                Scores: Spd {SpeedScore:F0} | Accel {AccelerationScore:F0} | Hndl {HandlingScore:F0} | Brk {BrakingScore:F0} | Lnch {LaunchScore:F0}
                """;
        }
    }
    
    /// <summary>
    /// Get detailed raw data dump for AI analysis
    /// </summary>
    public string GetRawDataForAI()
    {
        return $"""
            === RAW VEHICLE DATA ===
            
            [WEIGHT & BALANCE]
            Curb Weight: {CurbWeightKg:F0} kg ({CurbWeightLbs:F0} lbs)
            Weight Distribution: {WeightDistributionFront * 100:F1}% front / {(1 - WeightDistributionFront) * 100:F1}% rear
            
            [ENGINE]
            Peak Power: {PeakPowerHp:F0} HP ({PeakPowerKw:F0} kW) @ {PeakPowerRpm} RPM
            Peak Torque: {PeakTorqueNm:F0} Nm ({PeakTorqueLbFt:F0} lb-ft) @ {PeakTorqueRpm} RPM
            Boosted Power: {PeakBoostedPowerHp:F0} HP
            Boosted Torque: {PeakBoostedTorqueNm:F0} Nm
            
            [DRIVETRAIN]
            Layout: {DriveLayout}
            Gears: {GearCount}
            Final Drive: {FinalDriveRatio:F3}
            Top Gear Ratio: {TopGearRatio:F3}
            Drivetrain Efficiency: {DrivetrainEfficiency * 100:F0}%
            Wheel Power: {WheelPowerHp:F0} HP
            
            [TIRES]
            Front Lateral Grip (DY0): {FrontLateralGrip:F3}
            Rear Lateral Grip (DY0): {RearLateralGrip:F3}
            Front Longitudinal Grip (DX0): {FrontLongitudinalGrip:F3}
            Rear Longitudinal Grip (DX0): {RearLongitudinalGrip:F3}
            Average Grip: {EffectiveGrip:F3}
            Grip Balance: {GripBalance:F3} (+ = understeer tendency)
            Front Tire Width: {FrontTireWidthMm:F0} mm
            Rear Tire Width: {RearTireWidthMm:F0} mm
            Tire Radius: {TireRadiusM:F3} m
            
            [AERODYNAMICS]
            Drag Coefficient (Cd): {DragCoefficient:F3}
            Frontal Area: {FrontalArea:F2} m²
            Drag Area (CdA): {DragArea:F3} m²
            Lift Coefficient (CL): {LiftCoefficient:F3} (negative = downforce)
            Aero Efficiency (|CL|/Cd): {AeroEfficiency:F2}
            
            [BRAKES]
            Max Brake Torque: {BrakeTorqueNm:F0} Nm
            Brake Bias: {BrakeBias * 100:F0}% front
            
            [CALCULATED TOP SPEED]
            Drag-Limited: {DragLimitedTopSpeedMs * Units.MPS_TO_MPH:F0} mph ({DragLimitedTopSpeedMs * Units.MPS_TO_KMH:F0} km/h)
            Gear-Limited: {GearLimitedTopSpeedMs * Units.MPS_TO_MPH:F0} mph ({GearLimitedTopSpeedMs * Units.MPS_TO_KMH:F0} km/h)
            Actual Top Speed: {TopSpeedMph:F0} mph ({TopSpeedKmh:F0} km/h) - {TopSpeedLimitedBy}-limited
            
            [ESTIMATED PERFORMANCE]
            0-60 mph: {Est0To60Mph:F2} seconds
            0-100 km/h: {Est0To100Kmh:F2} seconds
            Quarter Mile: {EstQuarterMileTime:F2} seconds @ {EstQuarterMileTrapMph:F0} mph trap
            60-0 mph Braking: {Est60To0Ft:F0} ft ({Est60To0M:F1} m)
            100-0 km/h Braking: {Est100To0M:F1} m ({Est100To0Ft:F0} ft)
            Max Lateral G (base): {MaxLateralGBase:F2} g
            Max Lateral G (with aero): {MaxLateralGWithAero:F2} g
            
            [COMPONENT SCORES (0-100)]
            Speed Score: {SpeedScore:F1}
            Acceleration Score: {AccelerationScore:F1}
            Handling Score: {HandlingScore:F1}
            Braking Score: {BrakingScore:F1}
            Launch Score: {LaunchScore:F1}
            
            [PERFORMANCE INDEX]
            PI: {PerformanceIndex}
            Class: {PerformanceClass}
            Power-to-Weight: {PowerToWeightHpPerTon:F0} HP/ton
            Unrealistic: {IsUnrealistic}
            
            [VALIDATION WARNINGS]
            {(Warnings.Count == 0 ? "None" : string.Join("\n", Warnings.Select(w => $"[{w.Code}] {w.Message}")))}
            """;
    }
    #endregion
}

/// <summary>
/// Forza-style performance classes
/// </summary>
public enum PerformanceClass
{
    D,   // 100-400 - Economy cars, base compacts
    C,   // 401-500 - Hot hatches, entry sports cars
    B,   // 501-600 - Sports cars, muscle cars
    A,   // 601-700 - High-performance sports, light supercars
    S1,  // 701-800 - Supercars, GT4 race cars
    S2,  // 801-900 - Hypercars, GT3 race cars
    S3,  // 901-998 - Le Mans prototypes, extreme builds
    X    // 999 - Unrealistic/broken configurations
}

/// <summary>
/// Validation warning for unrealistic values
/// </summary>
public class ValidationWarning
{
    public string Code { get; set; }
    public string Message { get; set; }
    public ValidationSeverity Severity { get; set; }
    
    public ValidationWarning(string code, string message, ValidationSeverity severity)
    {
        Code = code;
        Message = message;
        Severity = severity;
    }
}
