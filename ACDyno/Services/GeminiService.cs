using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ACDyno.Models;

namespace ACDyno.Services;

/// <summary>
/// Service for Gemini AI integration for balance suggestions and car analysis
/// Tuned for Assetto Corsa Shuto Expressway server with traffic
/// </summary>
public class GeminiService
{
    private readonly HttpClient _httpClient;
    private string? _apiKey;
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
    
    public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);
    
    // PI Class reference information for AI context
    private const string PIClassReference = """
=== PERFORMANCE INDEX (PI) CLASS SYSTEM ===
This Assetto Corsa server uses a Forza-inspired PI system to classify cars:

| Class | PI Range | Description | Real-World Examples |
|-------|----------|-------------|---------------------|
| D     | 100-400  | Economy/base cars, daily drivers | Stock Civics, Corollas, base compacts |
| C     | 401-500  | Hot hatches, entry sports cars | Golf GTI, Civic Type R, BRZ/86 |
| B     | 501-600  | Sports cars, muscle cars | Mustang GT, 370Z, Cayman |
| A     | 601-700  | High-performance sports | M3/M4, C63 AMG, GT350 |
| S1    | 701-800  | Supercars, GT cars | 911 GT3, R8, McLaren 570S |
| S2    | 801-900  | Hypercars, GT3 race cars | 720S, GT3 RS, Huracan Performante |
| S3    | 901-998  | Le Mans prototypes, extreme builds | LMP cars, heavily modified builds |
| X     | 999      | Unrealistic/broken configurations | Physics-breaking mods |

PI is calculated from: Top Speed (20%), Acceleration (25%), Handling (30%), Braking (15%), Launch (10%)
Cars in the same class should be competitive with each other on the Shuto Expressway map.
""";

    // Racing context for all prompts - clarifies this is for AC simulation
    private const string StreetRacingContext = """
=== CONTEXT: ASSETTO CORSA MULTIPLAYER SERVER ===
This analysis is for balancing modded cars on a custom Assetto Corsa multiplayer server.
The server runs the Shuto Expressway map (Tokyo C1/Wangan) with AI traffic enabled.

IMPORTANT: This is for a VIDEO GAME SIMULATION (Assetto Corsa), not real life.
All references to "street racing" refer to the in-game multiplayer experience.
We are tuning virtual car physics data (INI files) for balanced online gameplay.

SHUTO EXPRESSWAY MAP CHARACTERISTICS:
- High-speed highway sections (Wangan line) with speeds 200+ km/h
- Tight technical sections (C1 inner loop) with 60-120 km/h corners  
- AI traffic cars that players must navigate around
- Tunnel sections with limited visibility
- Elevation changes and banked corners
- Mixed surface grip zones (painted lines, expansion joints)

WHAT MATTERS FOR THIS GAME MODE:
1. **Stability at Speed**: High-speed stability in AC physics - twitchy cars are frustrating around AI traffic
2. **Throttle Response**: Smooth, predictable power delivery for weaving through traffic
3. **Braking Consistency**: Reliable braking while changing lanes around AI cars
4. **Recovery**: Forgiving handling when clipping curbs or catching slides
5. **Rolling Acceleration**: Mid-range power matters more than 0-60 launches
6. **Top Speed**: Wangan straights reward high top speed, but it's not everything

LESS IMPORTANT FOR THIS GAME MODE:
- Launch/0-60 times (rolling starts, not drag races)
- Extreme downforce (highway speeds, not race track speeds)
- Lap time optimization (point-to-point with traffic, not hot laps)
- Tire wear/fuel consumption (typically short sessions)

BALANCE GOALS FOR THE SERVER:
Modded cars should feel fun and competitive in the sim. A well-balanced AC car should:
- Be stable at 250+ km/h without constant corrections
- Handle quick lane changes without snapping loose
- Have progressive grip limits (the player feels it sliding before spinning)
- Be enjoyable to drive, not a constant fight with the physics
""";

    public GeminiService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
        LoadApiKey();
    }
    
    /// <summary>
    /// Load API key from environment or config file
    /// </summary>
    private void LoadApiKey()
    {
        // Try environment variable first
        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        
        // Try config file in app directory
        if (string.IsNullOrEmpty(_apiKey))
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gemini_config.txt");
            if (File.Exists(configPath))
            {
                _apiKey = File.ReadAllText(configPath).Trim();
            }
        }
        
        // Try user's home directory
        if (string.IsNullOrEmpty(_apiKey))
        {
            var userConfigPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".acdyno",
                "gemini_api_key.txt"
            );
            if (File.Exists(userConfigPath))
            {
                _apiKey = File.ReadAllText(userConfigPath).Trim();
            }
        }
    }
    
    /// <summary>
    /// Set the API key manually
    /// </summary>
    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
        
        // Save to user config
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".acdyno"
        );
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "gemini_api_key.txt"), apiKey);
    }
    
    /// <summary>
    /// Get AI-powered balance suggestions for a car
    /// </summary>
    public async Task<GeminiBalanceResult> GetBalanceSuggestions(CarData targetCar, CarData? referenceCar = null)
    {
        if (!IsConfigured)
        {
            return new GeminiBalanceResult
            {
                Success = false,
                Error = "Gemini API key not configured. Set GEMINI_API_KEY environment variable or add key in Settings."
            };
        }
        
        var prompt = BuildBalancePrompt(targetCar, referenceCar);
        
        try
        {
            var response = await CallGeminiApi(prompt);
            return ParseBalanceResponse(response);
        }
        catch (Exception ex)
        {
            return new GeminiBalanceResult
            {
                Success = false,
                Error = $"API call failed: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// Get AI analysis of a car's characteristics with full raw data
    /// </summary>
    public async Task<string> AnalyzeCar(CarData car)
    {
        if (!IsConfigured)
        {
            return "Gemini API key not configured.";
        }
        
        // Get comprehensive raw data from metrics
        var rawData = car.Metrics.GetRawDataForAI();
        var piInfo = GetPIContextString(car.Metrics);
        
        var prompt = $"""
You are an expert Assetto Corsa modder and car tuner. Analyze this modded car's physics data for use on a Shuto Expressway multiplayer server.

{StreetRacingContext}

{PIClassReference}

=== VEHICLE DATA ===
VEHICLE: {car.BasicInfo.ScreenName}
BRAND: {car.BasicInfo.Brand}

{piInfo}

{rawData}

Based on this Assetto Corsa car data, provide a concise analysis (under 300 words) covering:

1. **Driving Character in AC**: What kind of car is this in the sim? 
   - Wangan missile (high-speed straight-line focused)? 
   - C1 runner (technical cornering focused)?
   - All-rounder?
   - How would it feel navigating AI traffic at high speed?

2. **High-Speed Behavior**: 
   - Will the AC physics model be stable on long straights?
   - Look at aero balance, weight distribution, and power curves
   - Any snap-oversteer or lift-off oversteer concerns in the sim?

3. **Physics Balance Assessment**:
   - Grip balance (understeer vs oversteer tendency in AC)
   - Brake bias vs weight distribution 
   - Power vs traction (can it put power down cleanly?)
   - Is it "driveable" - fast but not frustrating to control?

4. **PI Class Fit**: 
   - Is the {car.Metrics.PerformanceClass}-class (PI {car.Metrics.PerformanceIndex}) rating appropriate?
   - Would it be competitive with other {car.Metrics.PerformanceClass}-class cars on this server?
   - Any concerns about it being over/under-tuned for its class?

5. **Red Flags**: Any INI values that seem unrealistic or would make it unfun to drive online?

Be direct and reference specific numbers from the data. Focus on what matters for MULTIPLAYER GAMEPLAY on this specific server type.
""";

        try
        {
            return await CallGeminiApi(prompt);
        }
        catch (Exception ex)
        {
            return $"Analysis failed: {ex.Message}";
        }
    }
    
    private string GetPIContextString(PerformanceMetrics metrics)
    {
        var classDesc = metrics.PerformanceClass switch
        {
            PerformanceClass.D => "Economy/daily driver class - should feel like a stock road car",
            PerformanceClass.C => "Hot hatch/entry sports class - fun but approachable",
            PerformanceClass.B => "Sports car/muscle class - quick but manageable",
            PerformanceClass.A => "High-performance class - fast and demanding",
            PerformanceClass.S1 => "Supercar class - very fast, requires skill",
            PerformanceClass.S2 => "Hypercar/GT3 class - extremely fast, expert level",
            PerformanceClass.S3 => "Prototype/extreme class - maximum performance",
            PerformanceClass.X => "UNREALISTIC - physics values outside normal bounds",
            _ => "Unknown class"
        };

        return $"""
=== PERFORMANCE INDEX ===
PI Rating: {metrics.PerformanceIndex}
Class: {metrics.PerformanceClass} ({classDesc})

Component Scores (0-100):
- Speed Score: {metrics.SpeedScore:F0}/100 (top speed capability)
- Acceleration Score: {metrics.AccelerationScore:F0}/100 (0-60 and rolling acceleration)  
- Handling Score: {metrics.HandlingScore:F0}/100 (lateral grip and cornering)
- Braking Score: {metrics.BrakingScore:F0}/100 (stopping power)
- Launch Score: {metrics.LaunchScore:F0}/100 (traction off the line)
""";
    }
    
    private string BuildBalancePrompt(CarData target, CarData? reference)
    {
        var targetData = target.Metrics.GetRawDataForAI();
        var targetPiInfo = GetPIContextString(target.Metrics);
        
        var prompt = $"""
You are an expert Assetto Corsa car tuner. Suggest INI file modifications to balance this modded car for a Shuto Expressway multiplayer server.

{StreetRacingContext}

{PIClassReference}

=== TARGET CAR: {target.BasicInfo.ScreenName} ===

{targetPiInfo}

{targetData}
""";

        if (reference != null)
        {
            var refData = reference.Metrics.GetRawDataForAI();
            var refPiInfo = GetPIContextString(reference.Metrics);
            
            prompt += $"""

=== REFERENCE CAR (target performance level): {reference.BasicInfo.ScreenName} ===

{refPiInfo}

{refData}

The goal is to modify the TARGET car's INI files so it's competitive with the REFERENCE car on this AC server.
Both cars should feel balanced when racing together with AI traffic.
""";
        }
        else
        {
            prompt += $"""

The goal is to balance this car for competitive online play in the {target.Metrics.PerformanceClass}-class (PI {target.Metrics.PerformanceIndex}).
It should be competitive with other {target.Metrics.PerformanceClass}-class cars on the Shuto Expressway server.
""";
        }

        prompt += """

Provide balance suggestions in this EXACT JSON format:
{
  "analysis": "Brief analysis of the car's current AC physics - is it stable? Predictable? Fun to drive?",
  "suggestions": [
    {
      "file": "engine.ini",
      "section": "HEADER",
      "key": "POWER_MULTIPLIER",
      "currentValue": "1.0",
      "suggestedValue": "0.95",
      "reason": "Reduce power by 5% - currently overpowered for its grip level in AC physics"
    }
  ],
  "summary": "Overall summary - will these INI changes improve the car for this server?"
}

RULES FOR ASSETTO CORSA BALANCE:
- Prioritize STABILITY and PREDICTABILITY in AC's physics engine
- High-speed stability matters - don't create a car that's undriveable at 250 km/h
- Smooth power delivery > peak power numbers
- Progressive grip limits that give the player feedback before spinning
- Proper brake bias for the weight distribution
- Only modify: engine.ini, drivetrain.ini, car.ini (weight), tyres.ini (grip), aero.ini, brakes.ini
- Be conservative - small INI changes have big effects in AC
- Keep the car's character - a muscle car mod should still feel like a muscle car
- If values are broken/unrealistic for AC, say so and suggest corrections
- Reference specific data values and explain how they affect the driving experience
""";

        return prompt;
    }
    
    private async Task<string> CallGeminiApi(string prompt)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 4096
            }
        };
        
        var url = $"{BaseUrl}?key={_apiKey}";
        var response = await _httpClient.PostAsJsonAsync(url, requestBody);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"API returned {response.StatusCode}: {error}");
        }
        
        var result = await response.Content.ReadFromJsonAsync<GeminiApiResponse>();
        return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";
    }
    
    private GeminiBalanceResult ParseBalanceResponse(string response)
    {
        try
        {
            // Find JSON in the response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<GeminiBalanceJson>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (parsed != null)
                {
                    return new GeminiBalanceResult
                    {
                        Success = true,
                        Analysis = parsed.Analysis ?? "",
                        Summary = parsed.Summary ?? "",
                        Suggestions = parsed.Suggestions?.Select(s => new GeminiSuggestion
                        {
                            File = s.File ?? "",
                            Section = s.Section ?? "",
                            Key = s.Key ?? "",
                            CurrentValue = s.CurrentValue ?? "",
                            SuggestedValue = s.SuggestedValue ?? "",
                            Reason = s.Reason ?? ""
                        }).ToList() ?? new()
                    };
                }
            }
            
            // Fallback - return raw response as analysis
            return new GeminiBalanceResult
            {
                Success = true,
                Analysis = response,
                Summary = "Could not parse structured suggestions - showing raw analysis",
                Suggestions = new()
            };
        }
        catch
        {
            return new GeminiBalanceResult
            {
                Success = true,
                Analysis = response,
                Summary = "Response received but could not parse JSON",
                Suggestions = new()
            };
        }
    }
}

#region API Response Models
public class GeminiApiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }
}

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }
}

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart>? Parts { get; set; }
}

public class GeminiPart
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class GeminiBalanceJson
{
    public string? Analysis { get; set; }
    public string? Summary { get; set; }
    public List<GeminiSuggestionJson>? Suggestions { get; set; }
}

public class GeminiSuggestionJson
{
    public string? File { get; set; }
    public string? Section { get; set; }
    public string? Key { get; set; }
    public string? CurrentValue { get; set; }
    public string? SuggestedValue { get; set; }
    public string? Reason { get; set; }
}
#endregion

#region Result Models
public class GeminiBalanceResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string Analysis { get; set; } = "";
    public string Summary { get; set; } = "";
    public List<GeminiSuggestion> Suggestions { get; set; } = new();
}

public class GeminiSuggestion
{
    public string File { get; set; } = "";
    public string Section { get; set; } = "";
    public string Key { get; set; } = "";
    public string CurrentValue { get; set; } = "";
    public string SuggestedValue { get; set; } = "";
    public string Reason { get; set; } = "";
}
#endregion
