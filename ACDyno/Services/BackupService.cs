namespace ACDyno.Services;

/// <summary>
/// Manages automatic backups of car configuration files before editing
/// </summary>
public class BackupService
{
    private const string BackupFolderName = ".acdyno_backups";
    private const int MaxBackupsPerFile = 10;
    
    /// <summary>
    /// Create a backup of a file before modification
    /// </summary>
    public BackupResult CreateBackup(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new BackupResult
            {
                Success = false,
                Message = $"File not found: {filePath}"
            };
        }
        
        try
        {
            var directory = Path.GetDirectoryName(filePath)!;
            var fileName = Path.GetFileName(filePath);
            var backupFolder = Path.Combine(directory, BackupFolderName);
            
            // Create backup folder if it doesn't exist
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }
            
            // Generate timestamp-based backup name
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var backupFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}{Path.GetExtension(fileName)}";
            var backupPath = Path.Combine(backupFolder, backupFileName);
            
            // Copy the file
            File.Copy(filePath, backupPath, overwrite: true);
            
            // Cleanup old backups
            CleanupOldBackups(backupFolder, fileName);
            
            return new BackupResult
            {
                Success = true,
                BackupPath = backupPath,
                Message = $"Backup created: {backupFileName}"
            };
        }
        catch (Exception ex)
        {
            return new BackupResult
            {
                Success = false,
                Message = $"Backup failed: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// Create backups for all INI/LUT files in a car's data folder
    /// </summary>
    public List<BackupResult> BackupCarData(string carFolderPath)
    {
        var results = new List<BackupResult>();
        var dataFolder = Path.Combine(carFolderPath, "data");
        
        if (!Directory.Exists(dataFolder))
        {
            results.Add(new BackupResult
            {
                Success = false,
                Message = $"Data folder not found: {dataFolder}"
            });
            return results;
        }
        
        // Backup all INI and LUT files
        var files = Directory.GetFiles(dataFolder, "*.ini")
            .Concat(Directory.GetFiles(dataFolder, "*.lut"));
        
        foreach (var file in files)
        {
            results.Add(CreateBackup(file));
        }
        
        return results;
    }
    
    /// <summary>
    /// Get list of available backups for a file
    /// </summary>
    public List<BackupInfo> GetBackups(string filePath)
    {
        var backups = new List<BackupInfo>();
        
        if (!File.Exists(filePath))
            return backups;
        
        var directory = Path.GetDirectoryName(filePath)!;
        var fileName = Path.GetFileName(filePath);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var backupFolder = Path.Combine(directory, BackupFolderName);
        
        if (!Directory.Exists(backupFolder))
            return backups;
        
        // Find all backups matching this file's pattern
        var pattern = $"{fileNameWithoutExt}_*{extension}";
        var backupFiles = Directory.GetFiles(backupFolder, pattern);
        
        foreach (var backupFile in backupFiles)
        {
            var info = new FileInfo(backupFile);
            backups.Add(new BackupInfo
            {
                FilePath = backupFile,
                FileName = info.Name,
                CreatedAt = info.CreationTime,
                SizeBytes = info.Length
            });
        }
        
        return backups.OrderByDescending(b => b.CreatedAt).ToList();
    }
    
    /// <summary>
    /// Restore a file from a backup
    /// </summary>
    public BackupResult RestoreBackup(string backupPath, string targetPath)
    {
        if (!File.Exists(backupPath))
        {
            return new BackupResult
            {
                Success = false,
                Message = $"Backup not found: {backupPath}"
            };
        }
        
        try
        {
            // Create a backup of current file first
            if (File.Exists(targetPath))
            {
                CreateBackup(targetPath);
            }
            
            // Restore the backup
            File.Copy(backupPath, targetPath, overwrite: true);
            
            return new BackupResult
            {
                Success = true,
                BackupPath = targetPath,
                Message = $"Restored from: {Path.GetFileName(backupPath)}"
            };
        }
        catch (Exception ex)
        {
            return new BackupResult
            {
                Success = false,
                Message = $"Restore failed: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// Delete old backups keeping only the most recent ones
    /// </summary>
    private void CleanupOldBackups(string backupFolder, string originalFileName)
    {
        try
        {
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
            var extension = Path.GetExtension(originalFileName);
            var pattern = $"{fileNameWithoutExt}_*{extension}";
            
            var backups = Directory.GetFiles(backupFolder, pattern)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();
            
            // Delete backups beyond the limit
            foreach (var oldBackup in backups.Skip(MaxBackupsPerFile))
            {
                oldBackup.Delete();
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
    
    /// <summary>
    /// Delete all backups for a car
    /// </summary>
    public void ClearBackups(string carFolderPath)
    {
        var dataFolder = Path.Combine(carFolderPath, "data");
        var backupFolder = Path.Combine(dataFolder, BackupFolderName);
        
        if (Directory.Exists(backupFolder))
        {
            Directory.Delete(backupFolder, recursive: true);
        }
    }
}

/// <summary>
/// Result of a backup operation
/// </summary>
public class BackupResult
{
    public bool Success { get; set; }
    public string? BackupPath { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Information about a backup file
/// </summary>
public class BackupInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    
    public string FormattedSize => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024.0):F1} MB"
    };
    
    public string FormattedDate => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
}
