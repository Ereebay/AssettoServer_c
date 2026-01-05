using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CarBalanceTool.Analysis;
using CarBalanceTool.Models;
using CarBalanceTool.Parsing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarBalanceTool.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    // ============================================
    // SETTINGS
    // ============================================
    [ObservableProperty]
    private bool _showSettings;

    [ObservableProperty]
    private bool _useImperialUnits = true; // Default to imperial

    public bool UseMetricUnits
    {
        get => !UseImperialUnits;
        set => UseImperialUnits = !value;
    }

    partial void OnUseImperialUnitsChanged(bool value)
    {
        // Refresh all displays when units change
        OnPropertyChanged(nameof(UseMetricUnits));
        OnPropertyChanged(nameof(SpeedUnitLabel));
        OnPropertyChanged(nameof(PowerUnitLabel));
        OnPropertyChanged(nameof(AccelLabel1));
        OnPropertyChanged(nameof(AccelLabel2));

        // Refresh current car displays
        if (SelectedAnalyzeCar != null)
        {
            var temp = SelectedAnalyzeCar;
            SelectedAnalyzeCar = null;
            SelectedAnalyzeCar = temp;
        }
        if (EditorSelectedCar != null)
        {
            var temp = EditorSelectedCar;
            EditorSelectedCar = null;
            EditorSelectedCar = temp;
        }
        UpdateComparison();
        UpdateFleetBalance();
    }

    // Unit labels
    public string SpeedUnitLabel => UseImperialUnits ? "mph" : "km/h";
    public string PowerUnitLabel => UseImperialUnits ? "HP" : "kW";
    public string AccelLabel1 => UseImperialUnits ? "0-60 MPH" : "0-100 KM/H";
    public string AccelLabel2 => UseImperialUnits ? "0-100 MPH" : "0-200 KM/H";

    // Unit conversion helpers
    public float ConvertSpeed(float kmh) => UseImperialUnits ? kmh * 0.621371f : kmh;
    public float ConvertPower(float hp) => UseImperialUnits ? hp : hp * 0.7457f;
    public string FormatSpeed(float kmh) => $"{ConvertSpeed(kmh):F0} {SpeedUnitLabel}";
    public string FormatPower(float hp) => $"{ConvertPower(hp):F0} {PowerUnitLabel}";

    // Folder selection
    [ObservableProperty]
    private string _carsFolder = "";

    [ObservableProperty]
    private bool _hasCarsLoaded;

    [ObservableProperty]
    private string _statusMessage = "Select a folder containing car data folders to begin";

    // Filtered cars list for ComboBox
    [ObservableProperty]
    private string _carSearchText = "";

    // All loaded cars
    public ObservableCollection<CarData> LoadedCars { get; } = [];

    // ============================================
    // ANALYZE TAB
    // ============================================
    [ObservableProperty]
    private CarData? _selectedAnalyzeCar;

    [ObservableProperty]
    private CarData? _selectedCar;

    [ObservableProperty]
    private PerformanceMetrics? _selectedCarMetrics;

    [ObservableProperty]
    private bool _hasSelectedCar;

    [ObservableProperty]
    private string _turboDisplay = "No";

    [ObservableProperty]
    private IBrush _turboColor = new SolidColorBrush(Color.Parse("#64748b"));

    [ObservableProperty]
    private string _est0To100MphDisplay = "N/A";

    [ObservableProperty]
    private string _drivetrainEfficiencyDisplay = "";

    // Additional display properties for values that don't bind well
    [ObservableProperty]
    private string _peakPowerRpmDisplay = "";

    [ObservableProperty]
    private string _peakTorqueRpmDisplay = "";

    [ObservableProperty]
    private string _shiftUpTimeDisplay = "";

    [ObservableProperty]
    private string _shiftDownTimeDisplay = "";

    [ObservableProperty]
    private string _est0To60Display = "";

    [ObservableProperty]
    private string _est0To100Display = "";

    [ObservableProperty]
    private string _topSpeedDisplay = "";

    [ObservableProperty]
    private string _powerDisplay = "";

    [ObservableProperty]
    private string _powerKwDisplay = "";

    [ObservableProperty]
    private string _wheelPowerDisplay = "";

    [ObservableProperty]
    private string _gearingSpeedDisplay = "";

    [ObservableProperty]
    private string _powerSpeedDisplay = "";

    [ObservableProperty]
    private Bitmap? _carPreviewImage;

    [ObservableProperty]
    private bool _hasPreviewImage;

    public ObservableCollection<GearRatioItem> GearRatioDisplay { get; } = [];

    partial void OnSelectedAnalyzeCarChanged(CarData? value)
    {
        if (value != null)
        {
            value.Metrics ??= PerformanceCalculator.Calculate(value);

            // Update all properties
            SelectedCar = value;
            SelectedCarMetrics = value.Metrics;
            HasSelectedCar = true;

            // Update turbo display
            if (value.Engine.Turbos.Count > 0)
            {
                TurboDisplay = $"{value.Engine.MaxBoost:F2}";
                TurboColor = new SolidColorBrush(Color.Parse("#22c55e"));
            }
            else
            {
                TurboDisplay = "No";
                TurboColor = new SolidColorBrush(Color.Parse("#64748b"));
            }

            // Update acceleration displays (use appropriate units)
            if (UseImperialUnits)
            {
                Est0To60Display = $"{value.Metrics.Est0To60Mph:F2}";
                Est0To100Display = value.Metrics.Est0To100Mph < 60 ? $"{value.Metrics.Est0To100Mph:F2}" : "N/A";
            }
            else
            {
                Est0To60Display = $"{value.Metrics.Est0To100:F2}"; // 0-100 km/h
                Est0To100Display = value.Metrics.Est0To200 < 60 ? $"{value.Metrics.Est0To200:F2}" : "N/A"; // 0-200 km/h
            }
            Est0To100MphDisplay = Est0To100Display; // For backward compatibility

            // Update drivetrain efficiency display
            DrivetrainEfficiencyDisplay = $"{value.Metrics.DrivetrainEfficiency:P0}";

            // Update engine RPM displays
            PeakPowerRpmDisplay = value.Engine.PeakPowerRpm.ToString("N0");
            PeakTorqueRpmDisplay = value.Engine.PeakTorqueRpm.ToString("N0");

            // Update shift time displays
            ShiftUpTimeDisplay = value.Drivetrain.ShiftUpTime.ToString();
            ShiftDownTimeDisplay = value.Drivetrain.ShiftDownTime.ToString();

            // Update top speed display with unit conversion
            TopSpeedDisplay = $"{ConvertSpeed(value.Metrics.TheoreticalTopSpeed):F0}";

            // Update power displays
            PowerDisplay = $"{ConvertPower(value.Engine.PeakPowerHp):F0}";
            PowerKwDisplay = $"{value.Engine.PeakPowerKw:F0}";
            WheelPowerDisplay = $"{ConvertPower(value.Metrics.WheelPowerHp):F0}";

            // Update speed analysis displays
            if (value.Metrics.SpeedAnalysis != null)
            {
                GearingSpeedDisplay = $"{ConvertSpeed(value.Metrics.SpeedAnalysis.GearingLimitedSpeed):F0}";
                PowerSpeedDisplay = $"{ConvertSpeed(value.Metrics.SpeedAnalysis.PowerLimitedSpeed):F0}";
            }

            // Load preview image
            LoadPreviewImage(value.PreviewImagePath);

            // Update gear ratios
            GearRatioDisplay.Clear();
            for (int i = 0; i < value.Drivetrain.GearRatios.Count; i++)
            {
                GearRatioDisplay.Add(new GearRatioItem
                {
                    Gear = $"G{i + 1}",
                    Ratio = value.Drivetrain.GearRatios[i]
                });
            }

            // Force UI refresh for all bindings
            OnPropertyChanged(nameof(SelectedCar));
            OnPropertyChanged(nameof(SelectedCarMetrics));
            OnPropertyChanged(nameof(PeakPowerRpmDisplay));
            OnPropertyChanged(nameof(PeakTorqueRpmDisplay));
            OnPropertyChanged(nameof(ShiftUpTimeDisplay));
            OnPropertyChanged(nameof(ShiftDownTimeDisplay));
            OnPropertyChanged(nameof(TopSpeedDisplay));
            OnPropertyChanged(nameof(Est0To60Display));
            OnPropertyChanged(nameof(Est0To100Display));
            OnPropertyChanged(nameof(PowerDisplay));
            OnPropertyChanged(nameof(WheelPowerDisplay));
            OnPropertyChanged(nameof(GearingSpeedDisplay));
            OnPropertyChanged(nameof(PowerSpeedDisplay));
        }
        else
        {
            SelectedCar = null;
            SelectedCarMetrics = null;
            HasSelectedCar = false;
            GearRatioDisplay.Clear();
            CarPreviewImage = null;
            HasPreviewImage = false;
        }
    }

    private void LoadPreviewImage(string? imagePath)
    {
        // Dispose previous image
        CarPreviewImage?.Dispose();
        CarPreviewImage = null;
        HasPreviewImage = false;

        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            return;

        try
        {
            using var stream = File.OpenRead(imagePath);
            CarPreviewImage = Bitmap.DecodeToWidth(stream, 400);
            HasPreviewImage = true;
        }
        catch
        {
            // Failed to load image, ignore
            CarPreviewImage = null;
            HasPreviewImage = false;
        }
    }

    // ============================================
    // COMPARE TAB
    // ============================================
    [ObservableProperty]
    private CarData? _compareCar1;

    [ObservableProperty]
    private CarData? _compareCar2;

    [ObservableProperty]
    private BalanceAnalyzer.ComparisonResult? _comparisonData;

    [ObservableProperty]
    private bool _hasComparison;

    [ObservableProperty]
    private IBrush _balanceRatingColor = new SolidColorBrush(Colors.White);

    [ObservableProperty]
    private string _balanceRatingDescription = "";

    [ObservableProperty]
    private bool _hasSuggestions;

    public ObservableCollection<StatItem> Car1Stats { get; } = [];
    public ObservableCollection<StatItem> Car2Stats { get; } = [];
    public ObservableCollection<ComparisonBarItem> ComparisonBars { get; } = [];
    public ObservableCollection<SuggestionItem> Suggestions { get; } = [];

    partial void OnCompareCar1Changed(CarData? value) => UpdateComparison();
    partial void OnCompareCar2Changed(CarData? value) => UpdateComparison();

    private void UpdateComparison()
    {
        Car1Stats.Clear();
        Car2Stats.Clear();
        ComparisonBars.Clear();
        Suggestions.Clear();

        if (CompareCar1 != null && CompareCar2 != null && CompareCar1 != CompareCar2)
        {
            CompareCar1.Metrics ??= PerformanceCalculator.Calculate(CompareCar1);
            CompareCar2.Metrics ??= PerformanceCalculator.Calculate(CompareCar2);

            ComparisonData = BalanceAnalyzer.Compare(CompareCar1, CompareCar2);
            HasComparison = true;

            // Update balance rating color and description
            var rating = ComparisonData.BalanceRating;
            if (rating >= 90)
            {
                BalanceRatingColor = new SolidColorBrush(Color.Parse("#22c55e"));
                BalanceRatingDescription = "Well Balanced";
            }
            else if (rating >= 70)
            {
                BalanceRatingColor = new SolidColorBrush(Color.Parse("#f59e0b"));
                BalanceRatingDescription = "Minor Imbalance";
            }
            else if (rating >= 50)
            {
                BalanceRatingColor = new SolidColorBrush(Color.Parse("#f97316"));
                BalanceRatingDescription = "Moderate Imbalance";
            }
            else
            {
                BalanceRatingColor = new SolidColorBrush(Color.Parse("#ef4444"));
                BalanceRatingDescription = "Significant Imbalance";
            }

            // Populate car stats with unit-aware values
            var m1 = CompareCar1.Metrics;
            var m2 = CompareCar2.Metrics;

            var accelLabel = UseImperialUnits ? "0-60 mph" : "0-100 km/h";
            var accel1 = UseImperialUnits ? m1.Est0To60Mph : m1.Est0To100;
            var accel2 = UseImperialUnits ? m2.Est0To60Mph : m2.Est0To100;
            var ptwUnit = UseImperialUnits ? "HP/kg" : "kW/kg";
            var ptw1 = UseImperialUnits ? m1.PowerToWeight : m1.PowerToWeight * 0.7457f;
            var ptw2 = UseImperialUnits ? m2.PowerToWeight : m2.PowerToWeight * 0.7457f;

            Car1Stats.Add(new StatItem { Label = "Mass", Value = $"{CompareCar1.Basic.TotalMass:F0} kg" });
            Car1Stats.Add(new StatItem { Label = "Peak Power", Value = FormatPower(CompareCar1.Engine.PeakPowerHp) });
            Car1Stats.Add(new StatItem { Label = "Power/Weight", Value = $"{ptw1:F3} {ptwUnit}" });
            Car1Stats.Add(new StatItem { Label = "Top Speed", Value = FormatSpeed(m1.TheoreticalTopSpeed) });
            Car1Stats.Add(new StatItem { Label = accelLabel, Value = $"{accel1:F2}s" });
            Car1Stats.Add(new StatItem { Label = "Drivetrain", Value = CompareCar1.Drivetrain.Type.ToString() });
            Car1Stats.Add(new StatItem { Label = "Perf. Score", Value = $"{m1.PerformanceScore:F1}" });

            Car2Stats.Add(new StatItem { Label = "Mass", Value = $"{CompareCar2.Basic.TotalMass:F0} kg" });
            Car2Stats.Add(new StatItem { Label = "Peak Power", Value = FormatPower(CompareCar2.Engine.PeakPowerHp) });
            Car2Stats.Add(new StatItem { Label = "Power/Weight", Value = $"{ptw2:F3} {ptwUnit}" });
            Car2Stats.Add(new StatItem { Label = "Top Speed", Value = FormatSpeed(m2.TheoreticalTopSpeed) });
            Car2Stats.Add(new StatItem { Label = accelLabel, Value = $"{accel2:F2}s" });
            Car2Stats.Add(new StatItem { Label = "Drivetrain", Value = CompareCar2.Drivetrain.Type.ToString() });
            Car2Stats.Add(new StatItem { Label = "Perf. Score", Value = $"{m2.PerformanceScore:F1}" });

            // Comparison bars with unit-aware values
            var maxScore = Math.Max(m1.PerformanceScore, m2.PerformanceScore);
            ComparisonBars.Add(CreateBar("Performance Score", m1.PerformanceScore, m2.PerformanceScore, maxScore, false));

            var maxPtw = Math.Max(ptw1, ptw2);
            ComparisonBars.Add(CreateBar("Power/Weight", ptw1, ptw2, maxPtw, false, "F3"));

            var topSpeed1 = ConvertSpeed(m1.TheoreticalTopSpeed);
            var topSpeed2 = ConvertSpeed(m2.TheoreticalTopSpeed);
            var maxSpeed = Math.Max(topSpeed1, topSpeed2);
            ComparisonBars.Add(CreateBar("Top Speed", topSpeed1, topSpeed2, maxSpeed, false, "F0", $" {SpeedUnitLabel}"));

            var maxAccel = Math.Max(accel1, accel2);
            ComparisonBars.Add(CreateBar(accelLabel, accel1, accel2, maxAccel, true, "F2", "s")); // Lower is better

            var power1 = ConvertPower(CompareCar1.Engine.PeakPowerHp);
            var power2 = ConvertPower(CompareCar2.Engine.PeakPowerHp);
            var maxPower = Math.Max(power1, power2);
            ComparisonBars.Add(CreateBar("Peak Power", power1, power2, maxPower, false, "F0", $" {PowerUnitLabel}"));

            // Suggestions
            foreach (var s in ComparisonData.Suggestions.Where(s => s.Priority >= BalanceAnalyzer.SuggestionPriority.Medium))
            {
                Suggestions.Add(new SuggestionItem
                {
                    TargetCar = s.TargetCar,
                    Rationale = s.Rationale,
                    File = s.File,
                    Section = s.Section,
                    Key = s.Key,
                    CurrentValue = $"{s.CurrentValue:F2}",
                    SuggestedValue = $"{s.SuggestedValue:F2}",
                    PriorityText = s.IsWarning ? "WARNING" : s.Priority.ToString().ToUpper(),
                    PriorityColor = s.Priority switch
                    {
                        BalanceAnalyzer.SuggestionPriority.Critical => new SolidColorBrush(Color.Parse("#ef4444")),
                        BalanceAnalyzer.SuggestionPriority.High => new SolidColorBrush(Color.Parse("#f97316")),
                        _ => new SolidColorBrush(Color.Parse("#f59e0b"))
                    },
                    IsWarning = s.IsWarning
                });
            }

            HasSuggestions = Suggestions.Count > 0;
        }
        else
        {
            ComparisonData = null;
            HasComparison = false;
            HasSuggestions = false;
        }
    }

    private static ComparisonBarItem CreateBar(string label, float val1, float val2, float max, bool lowerIsBetter, string format = "F1", string suffix = "")
    {
        var pct1 = max > 0 ? (val1 / max) * 100 : 0;
        var pct2 = max > 0 ? (val2 / max) * 100 : 0;

        var color1 = lowerIsBetter
            ? (val1 <= val2 ? "#22c55e" : "#ef4444")
            : (val1 >= val2 ? "#22c55e" : "#ef4444");

        var color2 = lowerIsBetter
            ? (val2 <= val1 ? "#22c55e" : "#ef4444")
            : (val2 >= val1 ? "#22c55e" : "#ef4444");

        return new ComparisonBarItem
        {
            Label = label,
            LeftValue = val1.ToString(format) + suffix,
            RightValue = val2.ToString(format) + suffix,
            LeftPercent = pct1,
            RightPercent = pct2,
            LeftColor = new SolidColorBrush(Color.Parse(color1)),
            RightColor = new SolidColorBrush(Color.Parse(color2))
        };
    }

    // ============================================
    // BALANCE EDITOR TAB
    // ============================================
    [ObservableProperty]
    private CarData? _editorSelectedCar;

    [ObservableProperty]
    private bool _hasEditorCar;

    [ObservableProperty]
    private string _speedAnalysisText = "";

    [ObservableProperty]
    private string _clutchAnalysisText = "";

    [ObservableProperty]
    private bool _hasClutchIssue;

    [ObservableProperty]
    private bool _hasSpeedIssue;

    public ObservableCollection<IssueItem> EditorIssues { get; } = [];
    public ObservableCollection<SuggestionItem> EditorSuggestions { get; } = [];

    partial void OnEditorSelectedCarChanged(CarData? value)
    {
        EditorIssues.Clear();
        EditorSuggestions.Clear();
        SpeedAnalysisText = "";
        ClutchAnalysisText = "";
        HasClutchIssue = false;
        HasSpeedIssue = false;

        if (value != null)
        {
            value.Metrics ??= PerformanceCalculator.Calculate(value);
            HasEditorCar = true;

            // Get speed analysis
            if (value.Metrics.SpeedAnalysis != null)
            {
                SpeedAnalysisText = value.Metrics.SpeedAnalysis.Analysis;
                HasSpeedIssue = value.Metrics.SpeedAnalysis.IsUnrealistic;
            }

            // Get clutch analysis
            if (value.Metrics.ClutchAnalysis != null)
            {
                ClutchAnalysisText = value.Metrics.ClutchAnalysis.Analysis;
                HasClutchIssue = !value.Metrics.ClutchAnalysis.IsAdequate;
            }

            // Get all issues
            var issues = BalanceAnalyzer.AnalyzeCarIssues(value);
            foreach (var issue in issues.OrderByDescending(i => i.Severity))
            {
                EditorIssues.Add(new IssueItem
                {
                    Category = issue.Category.ToString(),
                    Severity = issue.Severity.ToString().ToUpper(),
                    Title = issue.Title,
                    Description = issue.Description,
                    Suggestion = issue.Suggestion,
                    SeverityColor = issue.Severity switch
                    {
                        BalanceAnalyzer.IssueSeverity.Critical => new SolidColorBrush(Color.Parse("#ef4444")),
                        BalanceAnalyzer.IssueSeverity.High => new SolidColorBrush(Color.Parse("#f97316")),
                        BalanceAnalyzer.IssueSeverity.Medium => new SolidColorBrush(Color.Parse("#f59e0b")),
                        _ => new SolidColorBrush(Color.Parse("#3b82f6"))
                    }
                });
            }

            // Generate suggestions if car is unrealistic
            if (BalanceAnalyzer.IsUnrealistic(value))
            {
                var dummyComparison = new BalanceAnalyzer.ComparisonResult
                {
                    Car1 = value,
                    Car2 = value
                };

                // Use reflection to call private method or duplicate logic
                var suggestions = GenerateEditorSuggestions(value);
                foreach (var s in suggestions)
                {
                    EditorSuggestions.Add(new SuggestionItem
                    {
                        TargetCar = s.TargetCar,
                        Rationale = s.Rationale,
                        File = s.File,
                        Section = s.Section,
                        Key = s.Key,
                        CurrentValue = $"{s.CurrentValue:F2}",
                        SuggestedValue = $"{s.SuggestedValue:F2}",
                        PriorityText = s.IsWarning ? "WARNING" : s.Priority.ToString().ToUpper(),
                        PriorityColor = s.Priority switch
                        {
                            BalanceAnalyzer.SuggestionPriority.Critical => new SolidColorBrush(Color.Parse("#ef4444")),
                            BalanceAnalyzer.SuggestionPriority.High => new SolidColorBrush(Color.Parse("#f97316")),
                            _ => new SolidColorBrush(Color.Parse("#f59e0b"))
                        },
                        IsWarning = s.IsWarning
                    });
                }
            }
        }
        else
        {
            HasEditorCar = false;
        }
    }

    private static List<BalanceAnalyzer.BalanceSuggestion> GenerateEditorSuggestions(CarData car)
    {
        // Create a fake comparison to get suggestions for a single car
        var result = BalanceAnalyzer.Compare(car, car);
        return result.Suggestions;
    }

    // ============================================
    // FLEET BALANCE TAB
    // ============================================
    [ObservableProperty]
    private CarData? _referenceCar;

    [ObservableProperty]
    private float _fleetAverageBalance;

    [ObservableProperty]
    private int _fleetTotalIssues;

    public ObservableCollection<FleetCarEntry> FleetEntries { get; } = [];

    partial void OnReferenceCarChanged(CarData? value) => UpdateFleetBalance();

    private void UpdateFleetBalance()
    {
        FleetEntries.Clear();
        FleetAverageBalance = 0;
        FleetTotalIssues = 0;

        if (LoadedCars.Count < 2)
            return;

        var reference = ReferenceCar ?? BalanceAnalyzer.FindReferenceCar(LoadedCars);
        if (reference == null)
            return;

        // Add reference first
        FleetEntries.Add(new FleetCarEntry
        {
            Car = reference,
            IsReference = true,
            BalanceRating = 100,
            ScoreDiff = 0,
            PowerToWeightDiff = 0,
            TopSpeedDiff = 0,
            AccelDiff = 0,
            IssueCount = 0,
            UseImperialUnits = UseImperialUnits
        });

        // Compare all other cars
        var results = BalanceAnalyzer.AnalyzeFleet(LoadedCars, reference);

        foreach (var result in results)
        {
            var issueCount = result.Suggestions.Count(s => s.Priority >= BalanceAnalyzer.SuggestionPriority.Medium);

            FleetEntries.Add(new FleetCarEntry
            {
                Car = result.Car1,
                IsReference = false,
                BalanceRating = result.BalanceRating,
                ScoreDiff = result.PerformanceScoreDifference,
                PowerToWeightDiff = result.PowerToWeightDifference,
                TopSpeedDiff = result.TopSpeedDifference,
                AccelDiff = result.AccelTimeDifference,
                IssueCount = issueCount,
                Comparison = result,
                UseImperialUnits = UseImperialUnits
            });
        }

        // Summary
        FleetAverageBalance = results.Count > 0 ? results.Average(r => r.BalanceRating) : 100;
        FleetTotalIssues = results.Sum(r => r.Suggestions.Count(s => s.Priority >= BalanceAnalyzer.SuggestionPriority.Medium));
    }

    // ============================================
    // COMMANDS
    // ============================================

    public async Task SelectFolderAsync(IStorageProvider storageProvider)
    {
        var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Cars Folder",
            AllowMultiple = false
        });

        if (result.Count > 0)
        {
            var folder = result[0];
            CarsFolder = folder.Path.LocalPath;
            await LoadCarsAsync();
        }
    }

    [RelayCommand]
    private async Task LoadCarsAsync()
    {
        if (string.IsNullOrEmpty(CarsFolder) || !Directory.Exists(CarsFolder))
        {
            StatusMessage = "Invalid folder path";
            return;
        }

        LoadedCars.Clear();
        FleetEntries.Clear();
        SelectedAnalyzeCar = null;
        CompareCar1 = null;
        CompareCar2 = null;
        ReferenceCar = null;

        StatusMessage = "Loading cars...";

        await Task.Run(() =>
        {
            foreach (var dir in Directory.GetDirectories(CarsFolder))
            {
                var car = CarDataLoader.Load(dir);
                if (car != null)
                {
                    car.Metrics = PerformanceCalculator.Calculate(car);
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => LoadedCars.Add(car));
                }
            }
        });

        HasCarsLoaded = LoadedCars.Count > 0;
        StatusMessage = HasCarsLoaded
            ? $"Loaded {LoadedCars.Count} cars from {Path.GetFileName(CarsFolder)}"
            : "No valid car data found in folder";

        // Update fleet balance first
        UpdateFleetBalance();

        // Small delay to ensure UI bindings are ready before auto-selecting
        await Task.Delay(50);

        // Auto-select first car for analyze
        if (LoadedCars.Count > 0)
        {
            SelectedAnalyzeCar = LoadedCars[0];
        }

        // Auto-select for compare if we have 2+ cars
        if (LoadedCars.Count >= 2)
        {
            CompareCar1 = LoadedCars[0];
            CompareCar2 = LoadedCars[1];
        }
    }
}

// ============================================
// HELPER CLASSES
// ============================================

public class GearRatioItem
{
    public required string Gear { get; init; }
    public float Ratio { get; init; }
}

public class StatItem
{
    public required string Label { get; init; }
    public required string Value { get; init; }
}

public class ComparisonBarItem
{
    public required string Label { get; init; }
    public required string LeftValue { get; init; }
    public required string RightValue { get; init; }
    public float LeftPercent { get; init; }
    public float RightPercent { get; init; }
    public required IBrush LeftColor { get; init; }
    public required IBrush RightColor { get; init; }
}

public class SuggestionItem
{
    public required string TargetCar { get; init; }
    public required string Rationale { get; init; }
    public required string File { get; init; }
    public required string Section { get; init; }
    public required string Key { get; init; }
    public required string CurrentValue { get; init; }
    public required string SuggestedValue { get; init; }
    public required string PriorityText { get; init; }
    public required IBrush PriorityColor { get; init; }
    public bool IsWarning { get; init; }
    public bool ShowDetails => !IsWarning && !string.IsNullOrEmpty(File);
}

public class IssueItem
{
    public required string Category { get; init; }
    public required string Severity { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Suggestion { get; init; }
    public required IBrush SeverityColor { get; init; }
}

public partial class FleetCarEntry : ObservableObject
{
    public required CarData Car { get; init; }
    public bool IsReference { get; init; }
    public float BalanceRating { get; init; }
    public float ScoreDiff { get; init; }
    public float PowerToWeightDiff { get; init; }
    public float TopSpeedDiff { get; init; }
    public float AccelDiff { get; init; }
    public int IssueCount { get; init; }
    public BalanceAnalyzer.ComparisonResult? Comparison { get; init; }
    public bool UseImperialUnits { get; init; } = true;

    public string CarName => Car.Name;
    public string BalanceText => IsReference ? "REF" : $"{BalanceRating:F0}";
    public string ScoreDiffText => IsReference ? "-" : $"{ScoreDiff:+0.0;-0.0}";
    public string PtwDiffText => IsReference ? $"{Car.Metrics!.PowerToWeight:F3}" : $"{PowerToWeightDiff:+0.000;-0.000}";

    public string TopSpeedText
    {
        get
        {
            var speedConvert = UseImperialUnits ? 0.621371f : 1f;
            if (IsReference)
                return $"{Car.Metrics!.TheoreticalTopSpeed * speedConvert:F0}";
            return $"{TopSpeedDiff * speedConvert:+0;-0}";
        }
    }

    public string AccelText
    {
        get
        {
            var accel = UseImperialUnits ? Car.Metrics!.Est0To60Mph : Car.Metrics!.Est0To100;
            if (IsReference)
                return $"{accel:F2}s";
            return $"{AccelDiff:+0.00;-0.00}s";
        }
    }

    public string IssuesText => IsReference ? "-" : IssueCount.ToString();
}
