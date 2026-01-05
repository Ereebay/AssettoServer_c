using CarBalanceTool.Models;

namespace CarBalanceTool.Analysis;

// Note: SpeedLimitFactor is defined in Models/CarData.cs

/// <summary>
/// Analyzes balance between cars and suggests adjustments
/// </summary>
public class BalanceAnalyzer
{
    /// <summary>
    /// Comparison result between two cars
    /// </summary>
    public class ComparisonResult
    {
        public required CarData Car1 { get; init; }
        public required CarData Car2 { get; init; }

        public float MassDifference { get; set; }
        public float MassDifferencePercent { get; set; }

        public float PowerDifference { get; set; }
        public float PowerDifferencePercent { get; set; }

        public float PowerToWeightDifference { get; set; }
        public float PowerToWeightDifferencePercent { get; set; }

        public float TopSpeedDifference { get; set; }
        public float TopSpeedDifferencePercent { get; set; }

        public float AccelTimeDifference { get; set; }
        public float AccelTimeDifferencePercent { get; set; }

        public float PerformanceScoreDifference { get; set; }
        public float PerformanceScoreDifferencePercent { get; set; }

        public float DragDifference { get; set; }
        public float BrakeDifference { get; set; }

        public List<BalanceSuggestion> Suggestions { get; set; } = [];

        /// <summary>
        /// Overall balance rating (0-100, where 100 = perfectly balanced)
        /// </summary>
        public float BalanceRating { get; set; }
    }

    /// <summary>
    /// A suggestion for balancing cars
    /// </summary>
    public class BalanceSuggestion
    {
        public required string TargetCar { get; init; }
        public required string Parameter { get; init; }
        public required string File { get; init; }
        public required string Section { get; init; }
        public required string Key { get; init; }
        public float CurrentValue { get; init; }
        public float SuggestedValue { get; init; }
        public required string Rationale { get; init; }
        public SuggestionPriority Priority { get; init; }

        /// <summary>
        /// If true, this is an informational/warning message, not an actionable change
        /// </summary>
        public bool IsWarning { get; init; }

        /// <summary>
        /// Formatted string for display (e.g., "1.50 → 0.80")
        /// </summary>
        public string ValueDisplay => IsWarning ? "" : $"{CurrentValue:F2} → {SuggestedValue:F2}";
    }

    public enum SuggestionPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Thresholds for what is considered "unrealistic" for a car
    /// </summary>
    public static class RealisticLimits
    {
        public const float MaxRealisticPowerHp = 1500f;           // HP
        public const float MaxRealisticPowerToWeight = 0.75f;     // HP/kg (Koenigsegg Jesko is ~0.7)
        public const float MinRealistic0To60Mph = 2.2f;           // seconds (F1 cars ~2.6s, hypercars ~2.3s)
        public const float MaxRealisticTopSpeedKmh = 450f;        // km/h (~280 mph)
        public const float MaxRealisticBoost = 2.5f;              // bar

        /// <summary>
        /// Target values for bringing unrealistic cars back to reasonable levels
        /// </summary>
        public const float TargetMaxPowerHp = 1200f;
        public const float TargetMaxPowerToWeight = 0.55f;       // Sports car level
        public const float TargetMin0To60Mph = 3.5f;
    }

    /// <summary>
    /// Check if a car has unrealistic performance values
    /// </summary>
    public static bool IsUnrealistic(CarData car)
    {
        car.Metrics ??= PerformanceCalculator.Calculate(car);
        var m = car.Metrics;

        return car.Engine.PeakPowerHp > RealisticLimits.MaxRealisticPowerHp ||
               m.PowerToWeight > RealisticLimits.MaxRealisticPowerToWeight ||
               m.Est0To60Mph < RealisticLimits.MinRealistic0To60Mph ||
               m.TheoreticalTopSpeed > RealisticLimits.MaxRealisticTopSpeedKmh ||
               (car.Engine.Turbos.Count > 0 && car.Engine.MaxBoost > RealisticLimits.MaxRealisticBoost);
    }

    /// <summary>
    /// Get a description of why a car is unrealistic
    /// </summary>
    public static List<string> GetUnrealisticReasons(CarData car)
    {
        var reasons = new List<string>();
        car.Metrics ??= PerformanceCalculator.Calculate(car);
        var m = car.Metrics;

        if (car.Engine.PeakPowerHp > RealisticLimits.MaxRealisticPowerHp)
            reasons.Add($"Power ({car.Engine.PeakPowerHp:F0} HP) exceeds realistic limit of {RealisticLimits.MaxRealisticPowerHp} HP");

        if (m.PowerToWeight > RealisticLimits.MaxRealisticPowerToWeight)
            reasons.Add($"Power-to-weight ({m.PowerToWeight:F3} HP/kg) exceeds hypercar levels ({RealisticLimits.MaxRealisticPowerToWeight} HP/kg)");

        if (m.Est0To60Mph < RealisticLimits.MinRealistic0To60Mph)
            reasons.Add($"0-60 mph time ({m.Est0To60Mph:F2}s) is faster than physically possible ({RealisticLimits.MinRealistic0To60Mph}s)");

        if (m.TheoreticalTopSpeed > RealisticLimits.MaxRealisticTopSpeedKmh)
            reasons.Add($"Top speed ({m.TheoreticalTopSpeed:F0} km/h) exceeds land speed records");

        if (car.Engine.Turbos.Count > 0 && car.Engine.MaxBoost > RealisticLimits.MaxRealisticBoost)
            reasons.Add($"Turbo boost ({car.Engine.MaxBoost:F2} bar) is unrealistically high");

        return reasons;
    }

    /// <summary>
    /// Compare two cars and generate balance analysis
    /// </summary>
    public static ComparisonResult Compare(CarData car1, CarData car2)
    {
        // Ensure metrics are calculated
        car1.Metrics ??= PerformanceCalculator.Calculate(car1);
        car2.Metrics ??= PerformanceCalculator.Calculate(car2);

        var m1 = car1.Metrics;
        var m2 = car2.Metrics;

        var result = new ComparisonResult
        {
            Car1 = car1,
            Car2 = car2,

            // Mass comparison (Car1 - Car2, positive means Car1 is heavier)
            MassDifference = car1.Basic.TotalMass - car2.Basic.TotalMass,
            MassDifferencePercent = (car1.Basic.TotalMass - car2.Basic.TotalMass) / car2.Basic.TotalMass * 100,

            // Power comparison
            PowerDifference = car1.Engine.PeakPowerHp - car2.Engine.PeakPowerHp,
            PowerDifferencePercent = (car1.Engine.PeakPowerHp - car2.Engine.PeakPowerHp) / car2.Engine.PeakPowerHp * 100,

            // Power to weight
            PowerToWeightDifference = m1.PowerToWeight - m2.PowerToWeight,
            PowerToWeightDifferencePercent = (m1.PowerToWeight - m2.PowerToWeight) / m2.PowerToWeight * 100,

            // Top speed
            TopSpeedDifference = m1.TheoreticalTopSpeed - m2.TheoreticalTopSpeed,
            TopSpeedDifferencePercent = (m1.TheoreticalTopSpeed - m2.TheoreticalTopSpeed) / m2.TheoreticalTopSpeed * 100,

            // 0-100 time (negative is better for Car1)
            AccelTimeDifference = m1.Est0To100 - m2.Est0To100,
            AccelTimeDifferencePercent = (m1.Est0To100 - m2.Est0To100) / m2.Est0To100 * 100,

            // Performance score
            PerformanceScoreDifference = m1.PerformanceScore - m2.PerformanceScore,
            PerformanceScoreDifferencePercent = (m1.PerformanceScore - m2.PerformanceScore) / m2.PerformanceScore * 100,

            // Drag
            DragDifference = car1.Aero.TotalDragGain - car2.Aero.TotalDragGain,

            // Brakes
            BrakeDifference = car1.Brakes.MaxTorque - car2.Brakes.MaxTorque
        };

        // Calculate balance rating
        result.BalanceRating = CalculateBalanceRating(result);

        // Generate suggestions
        result.Suggestions = GenerateSuggestions(result);

        return result;
    }

    /// <summary>
    /// Calculate balance rating (0-100, 100 = perfect balance)
    /// </summary>
    private static float CalculateBalanceRating(ComparisonResult result)
    {
        // Penalize based on percentage differences in key areas
        var penalties = 0f;

        // Power to weight is most important (heavily penalized)
        penalties += MathF.Abs(result.PowerToWeightDifferencePercent) * 2;

        // Top speed difference
        penalties += MathF.Abs(result.TopSpeedDifferencePercent);

        // Acceleration time difference
        penalties += MathF.Abs(result.AccelTimeDifferencePercent) * 1.5f;

        // Performance score difference
        penalties += MathF.Abs(result.PerformanceScoreDifferencePercent);

        // Convert to 0-100 scale (0 penalties = 100 rating)
        var rating = 100 - penalties;
        return MathF.Max(0, MathF.Min(100, rating));
    }

    /// <summary>
    /// Generate balance suggestions based on comparison
    /// </summary>
    private static List<BalanceSuggestion> GenerateSuggestions(ComparisonResult result)
    {
        var suggestions = new List<BalanceSuggestion>();
        var car1 = result.Car1;
        var car2 = result.Car2;
        var m1 = car1.Metrics!;
        var m2 = car2.Metrics!;

        // Check if either car has unrealistic values
        var car1Unrealistic = IsUnrealistic(car1);
        var car2Unrealistic = IsUnrealistic(car2);

        // If one car is unrealistic, prioritize making it realistic first
        if (car1Unrealistic || car2Unrealistic)
        {
            if (car1Unrealistic)
                suggestions.AddRange(GenerateRealisticSuggestions(car1));
            if (car2Unrealistic)
                suggestions.AddRange(GenerateRealisticSuggestions(car2));

            // Return early - fix the unrealistic car before trying to balance
            return suggestions;
        }

        // Determine which car is faster overall
        var car1Faster = m1.PerformanceScore > m2.PerformanceScore;
        var fasterCar = car1Faster ? car1 : car2;
        var slowerCar = car1Faster ? car2 : car1;
        var fasterMetrics = car1Faster ? m1 : m2;
        var slowerMetrics = car1Faster ? m2 : m1;

        var scoreDiff = MathF.Abs(result.PerformanceScoreDifferencePercent);

        // Only suggest changes if imbalance is significant (>5%)
        if (scoreDiff < 5)
            return suggestions;

        // Determine priority based on imbalance
        var priority = scoreDiff switch
        {
            > 20 => SuggestionPriority.Critical,
            > 15 => SuggestionPriority.High,
            > 10 => SuggestionPriority.Medium,
            _ => SuggestionPriority.Low
        };

        // OPTION A: Nerf the faster car

        // 1. Add ballast (mass) to faster car
        var massToAdd = CalculateBallastNeeded(fasterCar, slowerCar);
        if (massToAdd > 10)
        {
            suggestions.Add(new BalanceSuggestion
            {
                TargetCar = fasterCar.Name,
                Parameter = "Mass (Ballast)",
                File = "car.ini",
                Section = "BASIC",
                Key = "TOTALMASS",
                CurrentValue = fasterCar.Basic.TotalMass,
                SuggestedValue = fasterCar.Basic.TotalMass + massToAdd,
                Rationale = $"Add {massToAdd:F0}kg ballast to reduce power-to-weight advantage",
                Priority = priority
            });
        }

        // 2. Reduce boost on faster car (if turbocharged)
        if (fasterCar.Engine.Turbos.Count > 0)
        {
            var currentBoost = fasterCar.Engine.MaxBoost;
            var targetBoost = CalculateTargetBoost(fasterCar, slowerCar);

            if (targetBoost < currentBoost - 0.05f)
            {
                suggestions.Add(new BalanceSuggestion
                {
                    TargetCar = fasterCar.Name,
                    Parameter = "Turbo Boost",
                    File = "engine.ini",
                    Section = "TURBO_0",
                    Key = "MAX_BOOST",
                    CurrentValue = currentBoost,
                    SuggestedValue = targetBoost,
                    Rationale = $"Reduce max boost to decrease peak power",
                    Priority = priority
                });
            }
        }

        // 3. Increase drag on faster car (if significant top speed advantage)
        var topSpeedDiff = MathF.Abs(result.TopSpeedDifference);
        if (topSpeedDiff > 10 && fasterMetrics.TheoreticalTopSpeed > slowerMetrics.TheoreticalTopSpeed)
        {
            var bodyWing = fasterCar.Aero.Wings.FirstOrDefault(w =>
                w.Name.Contains("BODY", StringComparison.OrdinalIgnoreCase));

            if (bodyWing != null)
            {
                var dragIncrease = (topSpeedDiff / 50) * 0.3f; // Rough estimate
                suggestions.Add(new BalanceSuggestion
                {
                    TargetCar = fasterCar.Name,
                    Parameter = "Aerodynamic Drag",
                    File = "aero.ini",
                    Section = "WING_0",
                    Key = "CD_GAIN",
                    CurrentValue = bodyWing.CdGain,
                    SuggestedValue = bodyWing.CdGain + dragIncrease,
                    Rationale = $"Increase drag to reduce top speed advantage of {topSpeedDiff:F0} km/h",
                    Priority = SuggestionPriority.Medium
                });
            }
        }

        // OPTION B: Buff the slower car

        // 4. Increase boost on slower car (if turbocharged)
        if (slowerCar.Engine.Turbos.Count > 0)
        {
            var currentBoost = slowerCar.Engine.MaxBoost;
            var targetBoost = CalculateTargetBoost(slowerCar, fasterCar, buff: true);

            if (targetBoost > currentBoost + 0.05f)
            {
                suggestions.Add(new BalanceSuggestion
                {
                    TargetCar = slowerCar.Name,
                    Parameter = "Turbo Boost",
                    File = "engine.ini",
                    Section = "TURBO_0",
                    Key = "MAX_BOOST",
                    CurrentValue = currentBoost,
                    SuggestedValue = targetBoost,
                    Rationale = $"Increase max boost to improve acceleration",
                    Priority = priority
                });
            }
        }

        // 5. Reduce mass on slower car
        var massToRemove = CalculateBallastNeeded(slowerCar, fasterCar);
        if (massToRemove < -10)
        {
            suggestions.Add(new BalanceSuggestion
            {
                TargetCar = slowerCar.Name,
                Parameter = "Mass",
                File = "car.ini",
                Section = "BASIC",
                Key = "TOTALMASS",
                CurrentValue = slowerCar.Basic.TotalMass,
                SuggestedValue = slowerCar.Basic.TotalMass + massToRemove, // massToRemove is negative
                Rationale = $"Remove {-massToRemove:F0}kg to improve power-to-weight",
                Priority = priority
            });
        }

        // 6. Reduce drag on slower car
        if (topSpeedDiff > 10 && slowerMetrics.TheoreticalTopSpeed < fasterMetrics.TheoreticalTopSpeed)
        {
            var bodyWing = slowerCar.Aero.Wings.FirstOrDefault(w =>
                w.Name.Contains("BODY", StringComparison.OrdinalIgnoreCase));

            if (bodyWing != null && bodyWing.CdGain > 1.0f)
            {
                var dragReduction = (topSpeedDiff / 50) * 0.3f;
                var newDrag = MathF.Max(0.8f, bodyWing.CdGain - dragReduction);

                suggestions.Add(new BalanceSuggestion
                {
                    TargetCar = slowerCar.Name,
                    Parameter = "Aerodynamic Drag",
                    File = "aero.ini",
                    Section = "WING_0",
                    Key = "CD_GAIN",
                    CurrentValue = bodyWing.CdGain,
                    SuggestedValue = newDrag,
                    Rationale = $"Reduce drag to improve top speed",
                    Priority = SuggestionPriority.Medium
                });
            }
        }

        return suggestions;
    }

    /// <summary>
    /// Generate suggestions to bring an unrealistic car back to realistic performance levels
    /// </summary>
    private static List<BalanceSuggestion> GenerateRealisticSuggestions(CarData car)
    {
        var suggestions = new List<BalanceSuggestion>();
        car.Metrics ??= PerformanceCalculator.Calculate(car);
        var m = car.Metrics;

        // Warning header suggestion
        suggestions.Add(new BalanceSuggestion
        {
            TargetCar = car.Name,
            Parameter = "⚠️ UNREALISTIC CAR DETECTED",
            File = "",
            Section = "",
            Key = "",
            CurrentValue = 0,
            SuggestedValue = 0,
            Rationale = "This car has unrealistic performance values. Bring it to realistic levels before balancing against other cars.",
            Priority = SuggestionPriority.Critical,
            IsWarning = true
        });

        // Check power - suggest reducing if too high
        if (car.Engine.PeakPowerHp > RealisticLimits.MaxRealisticPowerHp)
        {
            // If turbocharged, suggest reducing boost
            if (car.Engine.Turbos.Count > 0)
            {
                // Estimate boost needed for target power
                // Rough estimate: reducing boost by 50% roughly halves the turbo power contribution
                var powerReductionNeeded = car.Engine.PeakPowerHp - RealisticLimits.TargetMaxPowerHp;
                var currentBoost = car.Engine.MaxBoost;
                var estimatedBoostReduction = (powerReductionNeeded / car.Engine.PeakPowerHp) * currentBoost * 1.5f;
                var targetBoost = MathF.Max(0.3f, currentBoost - estimatedBoostReduction);

                suggestions.Add(new BalanceSuggestion
                {
                    TargetCar = car.Name,
                    Parameter = "Turbo Boost (Reduce Power)",
                    File = "engine.ini",
                    Section = "TURBO_0",
                    Key = "MAX_BOOST",
                    CurrentValue = currentBoost,
                    SuggestedValue = targetBoost,
                    Rationale = $"Reduce boost from {currentBoost:F2} to {targetBoost:F2} bar to bring power closer to {RealisticLimits.TargetMaxPowerHp:F0} HP",
                    Priority = SuggestionPriority.Critical
                });
            }
            else
            {
                // NA engine - suggest checking power curve or adding mass as workaround
                suggestions.Add(new BalanceSuggestion
                {
                    TargetCar = car.Name,
                    Parameter = "Power (NA Engine)",
                    File = "power.lut",
                    Section = "POWER",
                    Key = "CURVE",
                    CurrentValue = car.Engine.PeakPowerHp,
                    SuggestedValue = RealisticLimits.TargetMaxPowerHp,
                    Rationale = $"Power ({car.Engine.PeakPowerHp:F0} HP) is unrealistic. Edit power.lut to reduce peak power to ~{RealisticLimits.TargetMaxPowerHp:F0} HP",
                    Priority = SuggestionPriority.Critical
                });
            }
        }

        // Check power-to-weight - suggest adding mass
        if (m.PowerToWeight > RealisticLimits.MaxRealisticPowerToWeight)
        {
            // Calculate mass needed for target power-to-weight
            var currentPower = m.WheelPowerHp;
            var targetMass = currentPower / RealisticLimits.TargetMaxPowerToWeight;
            var massToAdd = targetMass - car.Basic.TotalMass;

            if (massToAdd > 50)
            {
                suggestions.Add(new BalanceSuggestion
                {
                    TargetCar = car.Name,
                    Parameter = "Mass (Reduce P/W Ratio)",
                    File = "car.ini",
                    Section = "BASIC",
                    Key = "TOTALMASS",
                    CurrentValue = car.Basic.TotalMass,
                    SuggestedValue = targetMass,
                    Rationale = $"Add {massToAdd:F0}kg to bring power-to-weight from {m.PowerToWeight:F3} to {RealisticLimits.TargetMaxPowerToWeight:F3} HP/kg (sports car level)",
                    Priority = SuggestionPriority.High
                });
            }
        }

        // Check turbo boost specifically
        if (car.Engine.Turbos.Count > 0 && car.Engine.MaxBoost > RealisticLimits.MaxRealisticBoost)
        {
            suggestions.Add(new BalanceSuggestion
            {
                TargetCar = car.Name,
                Parameter = "Turbo Boost (Unrealistic)",
                File = "engine.ini",
                Section = "TURBO_0",
                Key = "MAX_BOOST",
                CurrentValue = car.Engine.MaxBoost,
                SuggestedValue = 1.5f, // Reasonable boost level
                Rationale = $"Boost ({car.Engine.MaxBoost:F2} bar) is unrealistic. Real turbos typically run 0.5-2.0 bar",
                Priority = SuggestionPriority.High
            });
        }

        // Check top speed analysis
        if (m.SpeedAnalysis?.IsUnrealistic == true)
        {
            // Determine if gearing or power is the issue
            if (m.SpeedAnalysis.LimitingFactor == SpeedLimitFactor.Gearing)
            {
                // Gearing allows too high speed for the power - suggest reducing top gear
                var topGearRatio = car.Drivetrain.GearRatios[^1];
                var suggestedRatio = topGearRatio * 1.15f; // ~15% shorter gearing

                suggestions.Add(new BalanceSuggestion
                {
                    TargetCar = car.Name,
                    Parameter = "Top Gear Ratio (Speed Issue)",
                    File = "drivetrain.ini",
                    Section = "GEARS",
                    Key = $"GEAR_{car.Drivetrain.GearCount}",
                    CurrentValue = topGearRatio,
                    SuggestedValue = suggestedRatio,
                    Rationale = m.SpeedAnalysis.UnrealisticReason ?? "Top speed unrealistic for power level",
                    Priority = SuggestionPriority.High
                });
            }
            else
            {
                // Power is unrealistic - suggest modifying power.lut
                suggestions.Add(new BalanceSuggestion
                {
                    TargetCar = car.Name,
                    Parameter = "Power Curve (Speed Issue)",
                    File = "power.lut",
                    Section = "ENGINE",
                    Key = "TORQUE_CURVE",
                    CurrentValue = car.Engine.PeakPowerHp,
                    SuggestedValue = RealisticLimits.TargetMaxPowerHp,
                    Rationale = m.SpeedAnalysis.UnrealisticReason ?? "Power unrealistic - modify power.lut to reduce torque values",
                    Priority = SuggestionPriority.High
                });
            }
        }

        // Check clutch adequacy
        if (m.ClutchAnalysis != null && !m.ClutchAnalysis.IsAdequate)
        {
            var suggestedClutch = m.ClutchAnalysis.EnginePeakTorqueBoosted * 1.25f; // 25% headroom

            suggestions.Add(new BalanceSuggestion
            {
                TargetCar = car.Name,
                Parameter = "Clutch Torque (Undersized)",
                File = "drivetrain.ini",
                Section = "CLUTCH",
                Key = "MAX_TORQUE",
                CurrentValue = m.ClutchAnalysis.ClutchMaxTorque,
                SuggestedValue = suggestedClutch,
                Rationale = m.ClutchAnalysis.Analysis,
                Priority = SuggestionPriority.High
            });
        }

        return suggestions;
    }

    /// <summary>
    /// Generate a list of all issues/problems detected with a car
    /// </summary>
    public static List<CarIssue> AnalyzeCarIssues(CarData car)
    {
        var issues = new List<CarIssue>();
        car.Metrics ??= PerformanceCalculator.Calculate(car);
        var m = car.Metrics;

        // Check power
        if (car.Engine.PeakPowerHp > RealisticLimits.MaxRealisticPowerHp)
        {
            issues.Add(new CarIssue
            {
                Category = IssueCategory.Engine,
                Severity = IssueSeverity.Critical,
                Title = "Excessive Power",
                Description = $"Engine produces {car.Engine.PeakPowerHp:F0} HP, exceeding the {RealisticLimits.MaxRealisticPowerHp:F0} HP limit",
                Suggestion = "Reduce torque values in power.lut or reduce turbo boost"
            });
        }

        // Check power-to-weight
        if (m.PowerToWeight > RealisticLimits.MaxRealisticPowerToWeight)
        {
            issues.Add(new CarIssue
            {
                Category = IssueCategory.Performance,
                Severity = IssueSeverity.Critical,
                Title = "Unrealistic Power-to-Weight",
                Description = $"Power-to-weight ratio of {m.PowerToWeight:F3} HP/kg exceeds hypercar levels",
                Suggestion = "Add ballast weight or reduce engine power"
            });
        }

        // Check acceleration
        if (m.Est0To60Mph < RealisticLimits.MinRealistic0To60Mph)
        {
            issues.Add(new CarIssue
            {
                Category = IssueCategory.Performance,
                Severity = IssueSeverity.High,
                Title = "Impossible Acceleration",
                Description = $"0-60 mph time of {m.Est0To60Mph:F2}s is faster than physically achievable",
                Suggestion = "Reduce power or add mass to achieve realistic acceleration"
            });
        }

        // Check turbo boost
        if (car.Engine.Turbos.Count > 0 && car.Engine.MaxBoost > RealisticLimits.MaxRealisticBoost)
        {
            issues.Add(new CarIssue
            {
                Category = IssueCategory.Engine,
                Severity = IssueSeverity.High,
                Title = "Unrealistic Turbo Boost",
                Description = $"Max boost of {car.Engine.MaxBoost:F2} bar exceeds realistic limits ({RealisticLimits.MaxRealisticBoost} bar)",
                Suggestion = "Reduce MAX_BOOST in engine.ini TURBO sections"
            });
        }

        // Check top speed
        if (m.SpeedAnalysis?.IsUnrealistic == true)
        {
            issues.Add(new CarIssue
            {
                Category = IssueCategory.Drivetrain,
                Severity = IssueSeverity.High,
                Title = "Unrealistic Top Speed",
                Description = m.SpeedAnalysis.UnrealisticReason ?? "Top speed doesn't match power output",
                Suggestion = m.SpeedAnalysis.LimitingFactor == SpeedLimitFactor.Gearing
                    ? "Increase top gear ratio to reduce theoretical top speed"
                    : "Check power.lut values and ensure torque curve is realistic"
            });
        }

        // Check clutch
        if (m.ClutchAnalysis != null && !m.ClutchAnalysis.IsAdequate)
        {
            issues.Add(new CarIssue
            {
                Category = IssueCategory.Drivetrain,
                Severity = IssueSeverity.High,
                Title = "Undersized Clutch",
                Description = $"Clutch ({m.ClutchAnalysis.ClutchMaxTorque:F0} Nm) cannot handle engine torque ({m.ClutchAnalysis.EnginePeakTorqueBoosted:F0} Nm)",
                Suggestion = "Increase MAX_TORQUE in drivetrain.ini [CLUTCH] section"
            });
        }
        else if (m.ClutchAnalysis != null && m.ClutchAnalysis.HeadroomPercent < 10 && m.ClutchAnalysis.HeadroomPercent >= 0)
        {
            issues.Add(new CarIssue
            {
                Category = IssueCategory.Drivetrain,
                Severity = IssueSeverity.Low,
                Title = "Low Clutch Headroom",
                Description = $"Clutch has only {m.ClutchAnalysis.HeadroomPercent:F0}% headroom over engine torque",
                Suggestion = "Consider increasing clutch MAX_TORQUE for reliability"
            });
        }

        // Check braking
        if (m.BrakingPerKg < 1.0f)
        {
            issues.Add(new CarIssue
            {
                Category = IssueCategory.Brakes,
                Severity = IssueSeverity.Medium,
                Title = "Weak Brakes",
                Description = $"Brake force per kg ({m.BrakingPerKg:F2} Nm/kg) is below average",
                Suggestion = "Consider increasing MAX_TORQUE in brakes.ini"
            });
        }

        // Check tyre grip
        if (m.AverageTyreGrip < 1.0f)
        {
            issues.Add(new CarIssue
            {
                Category = IssueCategory.Tyres,
                Severity = IssueSeverity.Medium,
                Title = "Low Tyre Grip",
                Description = $"Average tyre grip coefficient ({m.AverageTyreGrip:F2}) is below typical values",
                Suggestion = "Check DX0/DY0 values in tyres.ini"
            });
        }
        else if (m.AverageTyreGrip > 2.0f)
        {
            issues.Add(new CarIssue
            {
                Category = IssueCategory.Tyres,
                Severity = IssueSeverity.High,
                Title = "Unrealistic Tyre Grip",
                Description = $"Average tyre grip coefficient ({m.AverageTyreGrip:F2}) exceeds realistic limits",
                Suggestion = "Reduce DX0/DY0 values in tyres.ini (typical range: 1.2-1.8)"
            });
        }

        return issues;
    }

    /// <summary>
    /// Represents an issue detected with a car
    /// </summary>
    public class CarIssue
    {
        public IssueCategory Category { get; init; }
        public IssueSeverity Severity { get; init; }
        public required string Title { get; init; }
        public required string Description { get; init; }
        public required string Suggestion { get; init; }
    }

    public enum IssueCategory
    {
        Engine,
        Drivetrain,
        Brakes,
        Tyres,
        Aero,
        Performance
    }

    public enum IssueSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Calculate ballast needed to balance power-to-weight
    /// </summary>
    private static float CalculateBallastNeeded(CarData targetCar, CarData referenceCar)
    {
        targetCar.Metrics ??= PerformanceCalculator.Calculate(targetCar);
        referenceCar.Metrics ??= PerformanceCalculator.Calculate(referenceCar);

        var targetPtW = targetCar.Metrics.PowerToWeight;
        var refPtW = referenceCar.Metrics.PowerToWeight;

        if (targetPtW <= refPtW)
            return 0; // Target car is already slower or equal

        // Calculate mass needed to match power-to-weight
        // targetPower / (targetMass + ballast) = refPtW
        // ballast = (targetPower / refPtW) - targetMass
        var targetPower = targetCar.Metrics.WheelPowerHp;
        var neededMass = targetPower / refPtW;
        var ballast = neededMass - targetCar.Basic.TotalMass;

        return ballast;
    }

    /// <summary>
    /// Calculate target boost to balance performance
    /// </summary>
    private static float CalculateTargetBoost(CarData targetCar, CarData referenceCar, bool buff = false)
    {
        if (targetCar.Engine.Turbos.Count == 0)
            return 0;

        targetCar.Metrics ??= PerformanceCalculator.Calculate(targetCar);
        referenceCar.Metrics ??= PerformanceCalculator.Calculate(referenceCar);

        var currentBoost = targetCar.Engine.MaxBoost;
        var scoreDiff = (targetCar.Metrics.PerformanceScore - referenceCar.Metrics.PerformanceScore) /
                        referenceCar.Metrics.PerformanceScore;

        // Adjust boost proportionally to score difference
        // Each 0.1 boost ≈ 10% more power
        float boostAdjustment;
        if (buff)
        {
            // Buffing: increase boost to catch up
            boostAdjustment = MathF.Abs(scoreDiff) * 0.2f;
            return MathF.Min(2.0f, currentBoost + boostAdjustment);
        }
        else
        {
            // Nerfing: decrease boost
            boostAdjustment = MathF.Abs(scoreDiff) * 0.15f;
            return MathF.Max(0.0f, currentBoost - boostAdjustment);
        }
    }

    /// <summary>
    /// Analyze a group of cars and find the reference (median) car
    /// </summary>
    public static CarData? FindReferenceCar(IEnumerable<CarData> cars)
    {
        var carList = cars.ToList();
        if (carList.Count == 0)
            return null;

        // Calculate metrics for all
        foreach (var car in carList)
        {
            car.Metrics ??= PerformanceCalculator.Calculate(car);
        }

        // Sort by performance score and take median
        var sorted = carList.OrderBy(c => c.Metrics!.PerformanceScore).ToList();
        return sorted[sorted.Count / 2];
    }

    /// <summary>
    /// Analyze balance of all cars against a reference
    /// </summary>
    public static List<ComparisonResult> AnalyzeFleet(IEnumerable<CarData> cars, CarData? reference = null)
    {
        var carList = cars.ToList();
        reference ??= FindReferenceCar(carList);

        if (reference == null)
            return [];

        return carList
            .Where(c => c != reference)
            .Select(c => Compare(c, reference))
            .OrderByDescending(r => MathF.Abs(r.PerformanceScoreDifferencePercent))
            .ToList();
    }
}
