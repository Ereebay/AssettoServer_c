using CarBalanceTool.Models;

namespace CarBalanceTool.Parsing;

/// <summary>
/// Parser for Assetto Corsa LUT (Lookup Table) files
/// </summary>
public class LutParser
{
    public List<LutPoint> Points { get; } = [];

    public static LutParser Parse(string filePath)
    {
        var parser = new LutParser();
        parser.Load(filePath);
        return parser;
    }

    public static LutParser? TryParse(string filePath)
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

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith(';') || line.StartsWith('#'))
                continue;

            // Format: X|Y or X,Y
            var separator = line.Contains('|') ? '|' : ',';
            var parts = line.Split(separator);

            if (parts.Length >= 2)
            {
                var xStr = parts[0].Trim().Replace(',', '.');
                var yStr = parts[1].Trim().Replace(',', '.');

                if (float.TryParse(xStr, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var x) &&
                    float.TryParse(yStr, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var y))
                {
                    Points.Add(new LutPoint { X = x, Y = y });
                }
            }
        }
    }

    /// <summary>
    /// Interpolate value at given X
    /// </summary>
    public float Interpolate(float x)
    {
        if (Points.Count == 0)
            return 0;

        if (Points.Count == 1)
            return Points[0].Y;

        // Find surrounding points
        for (int i = 0; i < Points.Count - 1; i++)
        {
            if (x >= Points[i].X && x <= Points[i + 1].X)
            {
                var t = (x - Points[i].X) / (Points[i + 1].X - Points[i].X);
                return Points[i].Y + t * (Points[i + 1].Y - Points[i].Y);
            }
        }

        // Extrapolate if outside range
        if (x < Points[0].X)
            return Points[0].Y;

        return Points[^1].Y;
    }

    /// <summary>
    /// Convert to power curve points
    /// </summary>
    public List<PowerPoint> ToPowerCurve()
    {
        return Points.Select(p => new PowerPoint
        {
            Rpm = (int)p.X,
            Torque = p.Y
        }).ToList();
    }

    /// <summary>
    /// Get maximum Y value
    /// </summary>
    public float MaxY => Points.Count > 0 ? Points.Max(p => p.Y) : 0;

    /// <summary>
    /// Get X value at maximum Y
    /// </summary>
    public float XAtMaxY => Points.Count > 0
        ? Points.OrderByDescending(p => p.Y).First().X
        : 0;
}

public struct LutPoint
{
    public float X { get; set; }
    public float Y { get; set; }

    public override string ToString() => $"{X}|{Y}";
}
