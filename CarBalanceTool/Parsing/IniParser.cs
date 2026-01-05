namespace CarBalanceTool.Parsing;

/// <summary>
/// Parser for Assetto Corsa INI files
/// </summary>
public class IniParser
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, Dictionary<string, string>> Sections => _sections;

    public static IniParser Parse(string filePath)
    {
        var parser = new IniParser();
        parser.Load(filePath);
        return parser;
    }

    public static IniParser? TryParse(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            return Parse(filePath);
        }
        catch
        {
            return null;
        }
    }

    private void Load(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        string currentSection = "";

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith(';') || line.StartsWith('#'))
                continue;

            // Section header
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].Trim();
                if (!_sections.ContainsKey(currentSection))
                    _sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            // Key=Value pair
            var equalIndex = line.IndexOf('=');
            if (equalIndex > 0 && !string.IsNullOrEmpty(currentSection))
            {
                var key = line[..equalIndex].Trim();
                var value = line[(equalIndex + 1)..].Trim();

                // Remove inline comments
                var commentIndex = value.IndexOf(';');
                if (commentIndex >= 0)
                    value = value[..commentIndex].Trim();

                commentIndex = value.IndexOf('#');
                if (commentIndex >= 0)
                    value = value[..commentIndex].Trim();

                _sections[currentSection][key] = value;
            }
        }
    }

    public bool HasSection(string section) => _sections.ContainsKey(section);

    public string? GetValue(string section, string key)
    {
        if (_sections.TryGetValue(section, out var sectionData) &&
            sectionData.TryGetValue(key, out var value))
        {
            return value;
        }
        return null;
    }

    public string GetValue(string section, string key, string defaultValue)
    {
        return GetValue(section, key) ?? defaultValue;
    }

    public int GetInt(string section, string key, int defaultValue = 0)
    {
        var value = GetValue(section, key);
        return value != null && int.TryParse(value, out var result) ? result : defaultValue;
    }

    public float GetFloat(string section, string key, float defaultValue = 0)
    {
        var value = GetValue(section, key);
        if (value == null)
            return defaultValue;

        // Handle locale issues
        value = value.Replace(',', '.');
        return float.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
    }

    public bool GetBool(string section, string key, bool defaultValue = false)
    {
        var value = GetValue(section, key);
        if (value == null)
            return defaultValue;

        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all sections matching a pattern (e.g., "TURBO_" for TURBO_0, TURBO_1, etc.)
    /// </summary>
    public IEnumerable<string> GetSectionsMatching(string prefix)
    {
        return _sections.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all key-value pairs in a section
    /// </summary>
    public IReadOnlyDictionary<string, string>? GetSection(string section)
    {
        return _sections.TryGetValue(section, out var data) ? data : null;
    }
}
