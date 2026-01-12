using System.Text.RegularExpressions;

namespace ACDyno.Services;

/// <summary>
/// Parser for Assetto Corsa INI configuration files
/// </summary>
public class IniParser
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections = new();
    private readonly List<string> _rawLines = new();
    
    public string FilePath { get; private set; } = string.Empty;
    
    /// <summary>
    /// Load and parse an INI file
    /// </summary>
    public static IniParser Load(string path)
    {
        var parser = new IniParser { FilePath = path };
        parser.Parse(File.ReadAllLines(path));
        return parser;
    }
    
    /// <summary>
    /// Load from string content
    /// </summary>
    public static IniParser LoadFromString(string content)
    {
        var parser = new IniParser();
        parser.Parse(content.Split('\n'));
        return parser;
    }
    
    private void Parse(string[] lines)
    {
        string currentSection = string.Empty;
        
        foreach (var rawLine in lines)
        {
            _rawLines.Add(rawLine);
            var line = rawLine.Trim();
            
            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith(';') || line.StartsWith('#'))
                continue;
            
            // Remove BOM if present
            if (line.StartsWith('\uFEFF'))
                line = line[1..];
            
            // Section header
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].Trim();
                if (!_sections.ContainsKey(currentSection))
                    _sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }
            
            // Key-value pair
            var eqIndex = line.IndexOf('=');
            if (eqIndex > 0 && !string.IsNullOrEmpty(currentSection))
            {
                var key = line[..eqIndex].Trim();
                var value = line[(eqIndex + 1)..].Trim();
                
                // Remove inline comments
                var commentIndex = value.IndexOf(';');
                if (commentIndex > 0)
                    value = value[..commentIndex].Trim();
                
                commentIndex = value.IndexOf("//");
                if (commentIndex > 0)
                    value = value[..commentIndex].Trim();
                
                _sections[currentSection][key] = value;
            }
        }
    }
    
    /// <summary>
    /// Get all section names
    /// </summary>
    public IEnumerable<string> GetSections() => _sections.Keys;
    
    /// <summary>
    /// Check if section exists
    /// </summary>
    public bool HasSection(string section) => 
        _sections.ContainsKey(section);
    
    /// <summary>
    /// Get all keys in a section
    /// </summary>
    public IEnumerable<string> GetKeys(string section) =>
        _sections.TryGetValue(section, out var dict) ? dict.Keys : Enumerable.Empty<string>();
    
    /// <summary>
    /// Get raw string value
    /// </summary>
    public string? GetString(string section, string key, string? defaultValue = null)
    {
        if (_sections.TryGetValue(section, out var dict) && 
            dict.TryGetValue(key, out var value))
            return value;
        return defaultValue;
    }
    
    /// <summary>
    /// Get integer value
    /// </summary>
    public int GetInt(string section, string key, int defaultValue = 0)
    {
        var str = GetString(section, key);
        return int.TryParse(str, out var value) ? value : defaultValue;
    }
    
    /// <summary>
    /// Get double value
    /// </summary>
    public double GetDouble(string section, string key, double defaultValue = 0)
    {
        var str = GetString(section, key);
        if (string.IsNullOrEmpty(str)) return defaultValue;
        
        // Handle both . and , as decimal separator
        str = str.Replace(',', '.');
        return double.TryParse(str, System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, out var value) 
            ? value : defaultValue;
    }
    
    /// <summary>
    /// Get boolean value
    /// </summary>
    public bool GetBool(string section, string key, bool defaultValue = false)
    {
        var str = GetString(section, key);
        if (string.IsNullOrEmpty(str)) return defaultValue;
        
        return str == "1" || str.Equals("true", StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Get array of doubles (comma or space separated)
    /// </summary>
    public double[] GetDoubleArray(string section, string key, int expectedLength = 3)
    {
        var str = GetString(section, key);
        if (string.IsNullOrEmpty(str)) return new double[expectedLength];
        
        var parts = str.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new double[Math.Max(parts.Length, expectedLength)];
        
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Replace(',', '.').Trim();
            if (double.TryParse(part, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var value))
                result[i] = value;
        }
        
        return result;
    }
    
    /// <summary>
    /// Set a value in the INI structure
    /// </summary>
    public void SetValue(string section, string key, string value)
    {
        if (!_sections.ContainsKey(section))
            _sections[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        _sections[section][key] = value;
    }
    
    /// <summary>
    /// Set a double value
    /// </summary>
    public void SetDouble(string section, string key, double value, int decimals = 2)
    {
        SetValue(section, key, value.ToString($"F{decimals}", 
            System.Globalization.CultureInfo.InvariantCulture));
    }
    
    /// <summary>
    /// Set an integer value
    /// </summary>
    public void SetInt(string section, string key, int value)
    {
        SetValue(section, key, value.ToString());
    }
    
    /// <summary>
    /// Save the INI file back to disk
    /// </summary>
    public void Save(string? path = null)
    {
        path ??= FilePath;
        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("No file path specified");
        
        using var writer = new StreamWriter(path);
        string currentSection = string.Empty;
        bool firstSection = true;
        
        foreach (var section in _sections)
        {
            if (!firstSection)
                writer.WriteLine();
            firstSection = false;
            
            writer.WriteLine($"[{section.Key}]");
            
            foreach (var kvp in section.Value)
            {
                writer.WriteLine($"{kvp.Key}={kvp.Value}");
            }
        }
    }
    
    /// <summary>
    /// Generate string content (for preview)
    /// </summary>
    public string ToIniString()
    {
        using var writer = new StringWriter();
        bool firstSection = true;
        
        foreach (var section in _sections)
        {
            if (!firstSection)
                writer.WriteLine();
            firstSection = false;
            
            writer.WriteLine($"[{section.Key}]");
            
            foreach (var kvp in section.Value)
            {
                writer.WriteLine($"{kvp.Key}={kvp.Value}");
            }
        }
        
        return writer.ToString();
    }
    
    /// <summary>
    /// Find all sections matching a pattern (e.g., "TURBO_*" or "FRONT_*")
    /// </summary>
    public IEnumerable<string> FindSections(string pattern)
    {
        var regex = new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$", 
            RegexOptions.IgnoreCase);
        return _sections.Keys.Where(s => regex.IsMatch(s));
    }
    
    /// <summary>
    /// Get entire section as dictionary
    /// </summary>
    public Dictionary<string, string>? GetSection(string section)
    {
        return _sections.TryGetValue(section, out var dict) 
            ? new Dictionary<string, string>(dict) 
            : null;
    }
}
