using ACDyno.Models;

namespace ACDyno.Services;

/// <summary>
/// Service to suggest balance changes for cars
/// </summary>
public class BalanceSuggester
{
    /// <summary>
    /// Generate suggestions to balance target car to reference car's PI
    /// </summary>
    public BalanceRecommendation SuggestBalance(CarData target, CarData reference)
    {
        var recommendation = new BalanceRecommendation
        {
            TargetCar = target,
            ReferenceCar = reference,
            TargetPI = reference.Metrics.PerformanceIndex,
            CurrentPIDifference = target.Metrics.PerformanceIndex - reference.Metrics.PerformanceIndex
        };
        
        // Determine direction of changes needed
        bool needsReduction = recommendation.CurrentPIDifference > 0;
        
        // Generate suggestions based on differences
        GeneratePowerSuggestions(target, reference, recommendation, needsReduction);
        GenerateWeightSuggestions(target, reference, recommendation, needsReduction);
        GenerateGripSuggestions(target, reference, recommendation, needsReduction);
        GenerateAeroSuggestions(target, reference, recommendation, needsReduction);
        
        // Categorize into quick fixes vs comprehensive
        CategorizeSuggestions(recommendation);
        
        // Generate summary
        recommendation.Summary = GenerateSummary(recommendation);
        
        // Estimate new PI
        recommendation.EstimatedNewPI = EstimateNewPI(target, recommendation.QuickFixes);
        
        // Check if changes preserve character
        recommendation.PreservesCharacter = CheckCharacterPreservation(target, recommendation);
        recommendation.CharacterNotes = GetCharacterNotes(target, reference);
        
        return recommendation;
    }
    
    /// <summary>
    /// Compare two cars and generate a detailed comparison
    /// </summary>
    public ComparisonResult CompareCars(CarData car1, CarData car2)
    {
        var result = new ComparisonResult
        {
            Car1 = car1,
            Car2 = car2
        };
        
        // Generate category comparisons
        result.CategoryComparisons = new List<CategoryComparison>
        {
            new()
            {
                Category = "Power",
                Car1Value = $"{car1.Metrics.PeakPowerHp:F0} HP",
                Car2Value = $"{car2.Metrics.PeakPowerHp:F0} HP",
                Difference = result.PowerDifference,
                Advantage = result.PowerDifference > 10 ? "Car1" : 
                           result.PowerDifference < -10 ? "Car2" : "Equal",
                Impact = "Affects acceleration and top speed"
            },
            new()
            {
                Category = "Weight",
                Car1Value = $"{car1.Metrics.CurbWeightKg:F0} kg",
                Car2Value = $"{car2.Metrics.CurbWeightKg:F0} kg",
                Difference = result.WeightDifference,
                Advantage = result.WeightDifference < -20 ? "Car1" : 
                           result.WeightDifference > 20 ? "Car2" : "Equal",
                Impact = "Lower weight improves all aspects of performance"
            },
            new()
            {
                Category = "Power/Weight",
                Car1Value = $"{car1.Metrics.PowerToWeightHpPerTon:F0} HP/ton",
                Car2Value = $"{car2.Metrics.PowerToWeightHpPerTon:F0} HP/ton",
                Difference = result.PowerToWeightDifference * 1000,
                Advantage = result.PowerToWeightDifference > 10 ? "Car1" : 
                           result.PowerToWeightDifference < -10 ? "Car2" : "Equal",
                Impact = "Primary acceleration indicator"
            },
            new()
            {
                Category = "Drivetrain",
                Car1Value = car1.Drivetrain.DriveLayout.ToString(),
                Car2Value = car2.Drivetrain.DriveLayout.ToString(),
                Difference = 0,
                Advantage = car1.Drivetrain.DriveLayout == DriveLayout.AWD && 
                           car2.Drivetrain.DriveLayout != DriveLayout.AWD ? "Car1" :
                           car2.Drivetrain.DriveLayout == DriveLayout.AWD && 
                           car1.Drivetrain.DriveLayout != DriveLayout.AWD ? "Car2" : "Different",
                Impact = "AWD has traction advantage, RWD more efficient"
            },
            new()
            {
                Category = "Grip",
                Car1Value = $"{car1.Metrics.EffectiveGrip:F3}",
                Car2Value = $"{car2.Metrics.EffectiveGrip:F3}",
                Difference = result.GripDifference,
                Advantage = result.GripDifference > 0.05 ? "Car1" : 
                           result.GripDifference < -0.05 ? "Car2" : "Equal",
                Impact = "Affects cornering speed and braking"
            },
            new()
            {
                Category = "Drag",
                Car1Value = $"{car1.Metrics.DragCoefficient:F2}",
                Car2Value = $"{car2.Metrics.DragCoefficient:F2}",
                Difference = result.DragDifference,
                Advantage = result.DragDifference < -0.1 ? "Car1" : 
                           result.DragDifference > 0.1 ? "Car2" : "Equal",
                Impact = "Lower drag = higher top speed"
            },
            new()
            {
                Category = "Performance Index",
                Car1Value = $"{car1.Metrics.PerformanceIndex} ({car1.Metrics.PerformanceClass})",
                Car2Value = $"{car2.Metrics.PerformanceIndex} ({car2.Metrics.PerformanceClass})",
                Difference = result.PIGDifference,
                Advantage = result.PIGDifference > 0 ? "Car1" : 
                           result.PIGDifference < 0 ? "Car2" : "Equal",
                Impact = "Overall performance rating"
            }
        };
        
        // Calculate race prediction
        result.RacePrediction = PredictRaceOutcome(car1, car2);
        
        return result;
    }
    
    private void GeneratePowerSuggestions(CarData target, CarData reference, 
        BalanceRecommendation rec, bool needsReduction)
    {
        double powerDiff = target.Metrics.PeakPowerHp - reference.Metrics.PeakPowerHp;
        
        // If significant power difference exists
        if (Math.Abs(powerDiff) > 20)
        {
            // Check turbo first (easier to adjust)
            if (target.Engine.Turbos.Count > 0)
            {
                var turbo = target.Engine.Turbos[0];
                double targetBoost = turbo.MaxBoost * (reference.Metrics.PeakPowerHp / target.Metrics.PeakPowerHp);
                targetBoost = Math.Clamp(targetBoost, 0.1, 3.0);
                
                if (Math.Abs(turbo.MaxBoost - targetBoost) > 0.05)
                {
                    rec.Suggestions.Add(new BalanceSuggestion
                    {
                        File = "engine.ini",
                        Section = "TURBO_0",
                        Key = "MAX_BOOST",
                        CurrentValue = turbo.MaxBoost,
                        SuggestedValue = Math.Round(targetBoost, 2),
                        Reason = $"Adjust boost to match target power ({reference.Metrics.PeakPowerHp:F0} HP)",
                        Impact = BalanceImpact.Power,
                        PIChange = (targetBoost - turbo.MaxBoost) * 50
                    });
                    
                    // Also adjust wastegate
                    rec.Suggestions.Add(new BalanceSuggestion
                    {
                        File = "engine.ini",
                        Section = "TURBO_0",
                        Key = "WASTEGATE",
                        CurrentValue = turbo.Wastegate,
                        SuggestedValue = Math.Round(targetBoost, 2),
                        Reason = "Match wastegate to max boost",
                        Impact = BalanceImpact.Power,
                        PIChange = 0
                    });
                }
            }
            else if (needsReduction)
            {
                // No turbo - suggest power.lut scaling
                double scaleFactor = reference.Metrics.PeakPowerHp / target.Metrics.PeakPowerHp;
                rec.Suggestions.Add(new BalanceSuggestion
                {
                    File = "power.lut",
                    Section = "All points",
                    Key = "Torque values",
                    CurrentValue = target.Metrics.PeakTorqueNm,
                    SuggestedValue = target.Metrics.PeakTorqueNm * scaleFactor,
                    Reason = $"Scale torque curve by {scaleFactor:F2}x to reduce power",
                    Impact = BalanceImpact.Power,
                    PIChange = -(1 - scaleFactor) * 100
                });
            }
        }
    }
    
    private void GenerateWeightSuggestions(CarData target, CarData reference,
        BalanceRecommendation rec, bool needsReduction)
    {
        double weightDiff = target.Metrics.CurbWeightKg - reference.Metrics.CurbWeightKg;
        
        if (Math.Abs(weightDiff) > 30)
        {
            double suggestedWeight = target.Metrics.CurbWeightKg - weightDiff * 0.7;
            suggestedWeight = Math.Round(suggestedWeight / 5) * 5; // Round to nearest 5
            
            rec.Suggestions.Add(new BalanceSuggestion
            {
                File = "car.ini",
                Section = "BASIC",
                Key = "TOTALMASS",
                CurrentValue = target.Metrics.CurbWeightKg,
                SuggestedValue = suggestedWeight,
                Reason = $"Adjust mass to better match reference ({reference.Metrics.CurbWeightKg:F0} kg)",
                Impact = BalanceImpact.Weight,
                PIChange = (target.Metrics.CurbWeightKg - suggestedWeight) * 0.1
            });
        }
    }
    
    private void GenerateGripSuggestions(CarData target, CarData reference,
        BalanceRecommendation rec, bool needsReduction)
    {
        var targetTyres = target.Tyres.DefaultCompound;
        var refTyres = reference.Tyres.DefaultCompound;
        
        if (targetTyres == null || refTyres == null) return;
        
        double gripDiff = targetTyres.AverageLateralGrip - refTyres.AverageLateralGrip;
        
        if (Math.Abs(gripDiff) > 0.05)
        {
            double targetGripFront = targetTyres.Front.DY0 - gripDiff * 0.5;
            double targetGripRear = targetTyres.Rear.DY0 - gripDiff * 0.5;
            
            rec.Suggestions.Add(new BalanceSuggestion
            {
                File = "tyres.ini",
                Section = "FRONT",
                Key = "DY0",
                CurrentValue = targetTyres.Front.DY0,
                SuggestedValue = Math.Round(targetGripFront, 4),
                Reason = "Adjust front lateral grip to match reference",
                Impact = BalanceImpact.Grip,
                PIChange = -gripDiff * 30
            });
            
            rec.Suggestions.Add(new BalanceSuggestion
            {
                File = "tyres.ini",
                Section = "REAR",
                Key = "DY0",
                CurrentValue = targetTyres.Rear.DY0,
                SuggestedValue = Math.Round(targetGripRear, 4),
                Reason = "Adjust rear lateral grip to match reference",
                Impact = BalanceImpact.Grip,
                PIChange = -gripDiff * 30
            });
        }
    }
    
    private void GenerateAeroSuggestions(CarData target, CarData reference,
        BalanceRecommendation rec, bool needsReduction)
    {
        double dragDiff = target.Metrics.DragCoefficient - reference.Metrics.DragCoefficient;
        
        if (Math.Abs(dragDiff) > 0.15)
        {
            var bodyWing = target.Aero.BodyElement;
            if (bodyWing != null)
            {
                double suggestedDrag = bodyWing.CDGain - dragDiff * 0.6;
                suggestedDrag = Math.Clamp(suggestedDrag, 0.5, 3.0);
                
                rec.Suggestions.Add(new BalanceSuggestion
                {
                    File = "aero.ini",
                    Section = "WING_0 (BODY)",
                    Key = "CD_GAIN",
                    CurrentValue = bodyWing.CDGain,
                    SuggestedValue = Math.Round(suggestedDrag, 2),
                    Reason = $"Adjust body drag to match reference ({reference.Metrics.DragCoefficient:F2})",
                    Impact = BalanceImpact.TopSpeed,
                    PIChange = dragDiff * 10
                });
            }
        }
    }
    
    private void CategorizeSuggestions(BalanceRecommendation rec)
    {
        // Quick fixes = single file changes with big impact
        rec.QuickFixes = rec.Suggestions
            .OrderByDescending(s => Math.Abs(s.PIChange))
            .Take(2)
            .ToList();
        
        // Comprehensive = all suggestions
        rec.ComprehensiveChanges = rec.Suggestions.ToList();
    }
    
    private string GenerateSummary(BalanceRecommendation rec)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"Target: {rec.TargetCar.BasicInfo.ScreenName} (PI: {rec.TargetCar.Metrics.PerformanceIndex})");
        sb.AppendLine($"Reference: {rec.ReferenceCar.BasicInfo.ScreenName} (PI: {rec.ReferenceCar.Metrics.PerformanceIndex})");
        sb.AppendLine($"PI Difference: {rec.CurrentPIDifference:+0;-0;0}");
        sb.AppendLine();
        
        if (rec.QuickFixes.Any())
        {
            sb.AppendLine("Quick Balance (Minimal Changes):");
            foreach (var fix in rec.QuickFixes)
            {
                sb.AppendLine($"  • {fix.Key}: {fix.CurrentValue:F2} → {fix.SuggestedValue:F2}");
            }
        }
        
        return sb.ToString();
    }
    
    private int EstimateNewPI(CarData car, List<BalanceSuggestion> suggestions)
    {
        double piChange = suggestions.Sum(s => s.PIChange);
        return (int)Math.Clamp(car.Metrics.PerformanceIndex + piChange, 100, 999);
    }
    
    private bool CheckCharacterPreservation(CarData target, BalanceRecommendation rec)
    {
        // Check if changes are within reasonable bounds
        foreach (var suggestion in rec.Suggestions)
        {
            double changePercent = Math.Abs(suggestion.ChangePercent);
            if (changePercent > 30)
                return false; // More than 30% change breaks character
        }
        return true;
    }
    
    private string GetCharacterNotes(CarData target, CarData reference)
    {
        var notes = new List<string>();
        
        if (target.Drivetrain.DriveLayout != reference.Drivetrain.DriveLayout)
        {
            notes.Add($"Different drivetrains ({target.Drivetrain.DriveLayout} vs {reference.Drivetrain.DriveLayout}) - unique handling preserved");
        }
        
        if (Math.Abs(target.Suspension.CgLocation - reference.Suspension.CgLocation) > 5)
        {
            notes.Add("Different weight distributions - cornering behavior will differ");
        }
        
        if (target.Engine.Turbos.Count > 0 && reference.Engine.Turbos.Count == 0)
        {
            notes.Add("Turbo vs NA - different power delivery character");
        }
        
        return string.Join("; ", notes);
    }
    
    private RaceOutcomePrediction PredictRaceOutcome(CarData car1, CarData car2)
    {
        var prediction = new RaceOutcomePrediction();
        
        // Calculate advantage factors
        double accelAdvantage1 = (car1.Metrics.PowerToWeightHpPerTon - car2.Metrics.PowerToWeightHpPerTon) / 100;
        double gripAdvantage1 = (car1.Metrics.EffectiveGrip - car2.Metrics.EffectiveGrip) * 2;
        double aeroAdvantage1 = (car2.Metrics.DragCoefficient - car1.Metrics.DragCoefficient) / 2;
        double tractionAdvantage1 = car1.Drivetrain.DriveLayout == DriveLayout.AWD && 
                                   car2.Drivetrain.DriveLayout != DriveLayout.AWD ? 0.15 : 0;
        
        // Drag race (standing start)
        prediction.Car1DragRaceWin = 0.5 + accelAdvantage1 * 0.3 + tractionAdvantage1;
        prediction.Car1DragRaceWin = Math.Clamp(prediction.Car1DragRaceWin, 0.1, 0.9);
        
        // Rolling race (40 mph start)
        prediction.Car1RollingRaceWin = 0.5 + accelAdvantage1 * 0.4 + aeroAdvantage1 * 0.1;
        prediction.Car1RollingRaceWin = Math.Clamp(prediction.Car1RollingRaceWin, 0.1, 0.9);
        
        // Twisty road
        prediction.Car1TwistyRoadWin = 0.5 + gripAdvantage1 * 0.4 + accelAdvantage1 * 0.1;
        prediction.Car1TwistyRoadWin = Math.Clamp(prediction.Car1TwistyRoadWin, 0.1, 0.9);
        
        // Highway
        prediction.Car1HighwayWin = 0.5 + accelAdvantage1 * 0.3 + aeroAdvantage1 * 0.2;
        prediction.Car1HighwayWin = Math.Clamp(prediction.Car1HighwayWin, 0.1, 0.9);
        
        // Street race with traffic (driver skill matters more, wider probability range)
        // skillVariance represents how much driver skill can affect outcome
        double baseTrafficWin = 0.5 + (accelAdvantage1 + gripAdvantage1) * 0.15;
        // Narrow the range toward 0.5 to reflect skill > vehicle in traffic
        prediction.Car1TrafficRaceWin = 0.5 + (baseTrafficWin - 0.5) * 0.7; // 30% skill variance
        prediction.Car1TrafficRaceWin = Math.Clamp(prediction.Car1TrafficRaceWin, 0.35, 0.65);
        
        // Overall probability (weighted average)
        prediction.Car1WinProbability = 
            prediction.Car1DragRaceWin * 0.15 +
            prediction.Car1RollingRaceWin * 0.20 +
            prediction.Car1TwistyRoadWin * 0.25 +
            prediction.Car1HighwayWin * 0.15 +
            prediction.Car1TrafficRaceWin * 0.25;
        
        prediction.Car2WinProbability = 1 - prediction.Car1WinProbability;
        
        // Generate analysis
        prediction.Analysis = GenerateRaceAnalysis(car1, car2, prediction);
        
        return prediction;
    }
    
    private string GenerateRaceAnalysis(CarData car1, CarData car2, RaceOutcomePrediction prediction)
    {
        var advantages1 = new List<string>();
        var advantages2 = new List<string>();
        
        if (car1.Metrics.PowerToWeightHpPerTon > car2.Metrics.PowerToWeightHpPerTon * 1.05)
            advantages1.Add("better acceleration");
        else if (car2.Metrics.PowerToWeightHpPerTon > car1.Metrics.PowerToWeightHpPerTon * 1.05)
            advantages2.Add("better acceleration");
        
        if (car1.Drivetrain.DriveLayout == DriveLayout.AWD && car2.Drivetrain.DriveLayout != DriveLayout.AWD)
            advantages1.Add("AWD traction");
        else if (car2.Drivetrain.DriveLayout == DriveLayout.AWD && car1.Drivetrain.DriveLayout != DriveLayout.AWD)
            advantages2.Add("AWD traction");
        
        if (car1.Metrics.EffectiveGrip > car2.Metrics.EffectiveGrip * 1.02)
            advantages1.Add("more grip");
        else if (car2.Metrics.EffectiveGrip > car1.Metrics.EffectiveGrip * 1.02)
            advantages2.Add("more grip");
        
        if (car1.Metrics.DragCoefficient < car2.Metrics.DragCoefficient * 0.9)
            advantages1.Add("less drag");
        else if (car2.Metrics.DragCoefficient < car1.Metrics.DragCoefficient * 0.9)
            advantages2.Add("less drag");
        
        var analysis = new System.Text.StringBuilder();
        
        if (advantages1.Any())
            analysis.AppendLine($"{car1.BasicInfo.ScreenName} advantages: {string.Join(", ", advantages1)}");
        if (advantages2.Any())
            analysis.AppendLine($"{car2.BasicInfo.ScreenName} advantages: {string.Join(", ", advantages2)}");
        
        analysis.AppendLine();
        analysis.AppendLine($"In a street race with traffic, driver skill will significantly impact outcomes.");
        analysis.AppendLine($"Similar-class cars should be competitive regardless of these differences.");
        
        return analysis.ToString();
    }
}
