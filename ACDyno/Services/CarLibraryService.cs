namespace ACDyno.Services;

using ACDyno.Models;

/// <summary>
/// Shared state service for managing the car library and current selection
/// </summary>
public class CarLibraryService
{
    private readonly CarLoader _carLoader;
    
    public CarLibraryService(CarLoader carLoader)
    {
        _carLoader = carLoader;
    }
    
    /// <summary>
    /// The root folder containing car folders
    /// </summary>
    public string? CarsRootPath { get; private set; }
    
    /// <summary>
    /// List of discovered car folder names
    /// </summary>
    public List<CarEntry> AvailableCars { get; private set; } = new();
    
    /// <summary>
    /// Currently loaded/analyzed car (from Analyze tab)
    /// </summary>
    public CarData? CurrentCar { get; private set; }
    
    /// <summary>
    /// Cache of loaded cars to avoid re-parsing
    /// </summary>
    private Dictionary<string, CarData> _carCache = new();
    
    /// <summary>
    /// Event fired when available cars list changes
    /// </summary>
    public event Action? OnCarsListChanged;
    
    /// <summary>
    /// Event fired when current car selection changes
    /// </summary>
    public event Action? OnCurrentCarChanged;
    
    /// <summary>
    /// Scan a folder for car subfolders
    /// </summary>
    public async Task<(bool Success, string Message)> LoadCarsFolder(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            return (false, "Please enter a folder path");
        
        if (!Directory.Exists(rootPath))
            return (false, $"Folder not found: {rootPath}");
        
        CarsRootPath = rootPath;
        var tempList = new List<CarEntry>();
        var errorCount = 0;
        
        try
        {
            await Task.Run(() =>
            {
                string[] directories;
                try
                {
                    directories = Directory.GetDirectories(rootPath);
                }
                catch (UnauthorizedAccessException)
                {
                    throw new Exception($"Access denied to folder: {rootPath}");
                }
                
                foreach (var dir in directories)
                {
                    try
                    {
                        var dataPath = Path.Combine(dir, "data");
                        var carIniPath = Path.Combine(dataPath, "car.ini");
                        
                        // Only include folders that have a data/car.ini file
                        if (Directory.Exists(dataPath) && File.Exists(carIniPath))
                        {
                            var folderName = Path.GetFileName(dir);
                            var displayName = GetCarDisplayName(carIniPath, folderName);
                            
                            tempList.Add(new CarEntry
                            {
                                FolderName = folderName ?? "",
                                FolderPath = dir,
                                DisplayName = displayName
                            });
                        }
                    }
                    catch
                    {
                        errorCount++;
                    }
                }
            });
            
            // Update the list on the main thread context
            AvailableCars = tempList.OrderBy(c => c.DisplayName).ToList();
            _carCache.Clear();
            
            OnCarsListChanged?.Invoke();
            
            var message = $"Found {AvailableCars.Count} cars";
            if (errorCount > 0)
                message += $" ({errorCount} folders skipped due to errors)";
            
            return (true, message);
        }
        catch (Exception ex)
        {
            return (false, $"Error scanning folder: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get display name from car.ini or fall back to folder name
    /// </summary>
    private string GetCarDisplayName(string carIniPath, string fallback)
    {
        try
        {
            var lines = File.ReadAllLines(carIniPath);
            foreach (var line in lines)
            {
                if (line.StartsWith("SCREEN_NAME=", StringComparison.OrdinalIgnoreCase))
                {
                    var name = line.Substring("SCREEN_NAME=".Length).Trim();
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
        }
        catch { }
        
        return fallback;
    }
    
    /// <summary>
    /// Load a car by folder name (uses cache if available)
    /// </summary>
    public async Task<CarData?> LoadCar(string folderName)
    {
        if (_carCache.TryGetValue(folderName, out var cached))
            return cached;
        
        var entry = AvailableCars.FirstOrDefault(c => c.FolderName == folderName);
        if (entry == null) return null;
        
        var car = await Task.Run(() => _carLoader.LoadCar(entry.FolderPath));
        
        if (car != null)
        {
            _carCache[folderName] = car;
        }
        
        return car;
    }
    
    /// <summary>
    /// Load a car by path directly (for paths not in the library)
    /// </summary>
    public async Task<CarData?> LoadCarByPath(string path)
    {
        var folderName = Path.GetFileName(path);
        
        if (_carCache.TryGetValue(folderName, out var cached))
            return cached;
        
        var car = await Task.Run(() => _carLoader.LoadCar(path));
        
        if (car != null)
        {
            _carCache[folderName] = car;
        }
        
        return car;
    }
    
    /// <summary>
    /// Set the current analyzed car (propagates to other tabs)
    /// </summary>
    public void SetCurrentCar(CarData? car)
    {
        CurrentCar = car;
        OnCurrentCarChanged?.Invoke();
    }
    
    /// <summary>
    /// Clear the cache (useful if car files were modified)
    /// </summary>
    public void ClearCache()
    {
        _carCache.Clear();
    }
    
    /// <summary>
    /// Filter available cars by search text
    /// </summary>
    public List<CarEntry> FilterCars(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return AvailableCars;
        
        var search = searchText.ToLowerInvariant();
        return AvailableCars
            .Where(c => c.DisplayName.ToLowerInvariant().Contains(search) ||
                       c.FolderName.ToLowerInvariant().Contains(search))
            .ToList();
    }
}

/// <summary>
/// Represents a car entry in the library
/// </summary>
public class CarEntry
{
    public string FolderName { get; set; } = "";
    public string FolderPath { get; set; } = "";
    public string DisplayName { get; set; } = "";
    
    public override string ToString() => DisplayName;
}
