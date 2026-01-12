namespace ACDyno.Services;

/// <summary>
/// Parser for Assetto Corsa LUT (lookup table) files
/// Format: RPM|VALUE or X|Y
/// </summary>
public class LutParser
{
    public string FilePath { get; private set; } = string.Empty;
    public List<LutPoint> Points { get; private set; } = new();
    
    /// <summary>
    /// Load and parse a LUT file
    /// </summary>
    public static LutParser Load(string path)
    {
        if (!File.Exists(path))
            return new LutParser { FilePath = path };
        
        var parser = new LutParser { FilePath = path };
        parser.Parse(File.ReadAllLines(path));
        return parser;
    }
    
    /// <summary>
    /// Load from string content
    /// </summary>
    public static LutParser LoadFromString(string content)
    {
        var parser = new LutParser();
        parser.Parse(content.Split('\n'));
        return parser;
    }
    
    private void Parse(string[] lines)
    {
        Points.Clear();
        
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            
            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith(';') || line.StartsWith('#'))
                continue;
            
            // Parse pipe-delimited format: X|Y
            var parts = line.Split('|');
            if (parts.Length >= 2)
            {
                if (TryParseDouble(parts[0], out var x) && TryParseDouble(parts[1], out var y))
                {
                    Points.Add(new LutPoint(x, y));
                }
            }
        }
        
        // Sort by X value
        Points = Points.OrderBy(p => p.X).ToList();
    }
    
    private bool TryParseDouble(string str, out double value)
    {
        str = str.Trim().Replace(',', '.');
        return double.TryParse(str, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }
    
    /// <summary>
    /// Interpolate Y value at given X
    /// </summary>
    public double Interpolate(double x)
    {
        if (Points.Count == 0) return 0;
        if (Points.Count == 1) return Points[0].Y;
        
        // Clamp to range
        if (x <= Points.First().X) return Points.First().Y;
        if (x >= Points.Last().X) return Points.Last().Y;
        
        // Find surrounding points and interpolate
        for (int i = 0; i < Points.Count - 1; i++)
        {
            if (x >= Points[i].X && x <= Points[i + 1].X)
            {
                var t = (x - Points[i].X) / (Points[i + 1].X - Points[i].X);
                return Points[i].Y + t * (Points[i + 1].Y - Points[i].Y);
            }
        }
        
        return Points.Last().Y;
    }
    
    /// <summary>
    /// Get the maximum Y value
    /// </summary>
    public double MaxY => Points.Count > 0 ? Points.Max(p => p.Y) : 0;
    
    /// <summary>
    /// Get X where Y is maximum
    /// </summary>
    public double XAtMaxY => Points.Count > 0 ? Points.MaxBy(p => p.Y)?.X ?? 0 : 0;
    
    /// <summary>
    /// Get the minimum Y value
    /// </summary>
    public double MinY => Points.Count > 0 ? Points.Min(p => p.Y) : 0;
    
    /// <summary>
    /// Get X range
    /// </summary>
    public (double min, double max) XRange => Points.Count > 0 
        ? (Points.First().X, Points.Last().X) 
        : (0, 0);
    
    /// <summary>
    /// Add or update a point
    /// </summary>
    public void SetPoint(double x, double y)
    {
        var existing = Points.FirstOrDefault(p => Math.Abs(p.X - x) < 0.001);
        if (existing != null)
        {
            existing.Y = y;
        }
        else
        {
            Points.Add(new LutPoint(x, y));
            Points = Points.OrderBy(p => p.X).ToList();
        }
    }
    
    /// <summary>
    /// Remove a point
    /// </summary>
    public void RemovePoint(double x)
    {
        Points.RemoveAll(p => Math.Abs(p.X - x) < 0.001);
    }
    
    /// <summary>
    /// Scale all Y values by a factor
    /// </summary>
    public void ScaleY(double factor)
    {
        foreach (var point in Points)
        {
            point.Y *= factor;
        }
    }
    
    /// <summary>
    /// Save to file
    /// </summary>
    public void Save(string? path = null)
    {
        path ??= FilePath;
        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("No file path specified");
        
        var lines = Points.Select(p => $"{p.X}|{p.Y}");
        File.WriteAllLines(path, lines);
    }
    
    /// <summary>
    /// Generate LUT string content
    /// </summary>
    public string ToLutString()
    {
        return string.Join(Environment.NewLine, Points.Select(p => 
            $"{p.X.ToString(System.Globalization.CultureInfo.InvariantCulture)}|" +
            $"{p.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
    }
    
    /// <summary>
    /// Create power curve from this LUT (assuming RPM|Torque format)
    /// </summary>
    public Models.PowerCurve ToPowerCurve()
    {
        var curve = new Models.PowerCurve();
        foreach (var point in Points)
        {
            curve.Points.Add(new Models.PowerPoint((int)point.X, point.Y));
        }
        return curve;
    }
}

/// <summary>
/// Single point in a LUT
/// </summary>
public class LutPoint
{
    public double X { get; set; }
    public double Y { get; set; }
    
    public LutPoint() { }
    
    public LutPoint(double x, double y)
    {
        X = x;
        Y = y;
    }
}
