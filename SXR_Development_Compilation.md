# SXR (Shuto Expressway Revival) Server Development Compilation

## Project Overview

**SXR** is a comprehensive plugin suite for AssettoServer (Assetto Corsa multiplayer server) focused on recreating the Tokyo Xtreme Racer / Shutokou Battle experience on the Shuto Expressway map. The server features realistic traffic, TXR-style SP battles, driver progression systems, and a full admin toolkit.

**GitHub Repository:** `https://github.com/CodexDread/AssettoServer_c`

---

## Plugin Suite

### 1. SXRPlayerStatsPlugin
**Purpose:** Player statistics, driver levels, XP, and prestige system

**Features:**
- Driver levels 1-999
- XP earning from driving, races, battles
- Prestige system (P1-P999) - auto-prestige at level 999
- Stats tracking: distance driven, races, wins, collisions, time on server
- Achievement/milestone system
- Persistent storage (JSON/database)

**Key Fields:**
- `DriverLevel`, `TotalXP`, `PrestigeRank`
- `HighestLevelAchieved`, `TimesReachedMaxLevel`
- `EffectiveLevelForUnlocks` (returns 999 for prestiged players)

---

### 2. SXRAdminToolsPlugin
**Purpose:** Server administration via Extended Chat panel

**Features:**
- Admin levels: SuperAdmin, Admin, Moderator
- Player management: kick, ban, pit teleport, lights control
- Ban system: permanent, temporary, IP bans, offline bans
- Server control: time of day, weather (CSP weather types)
- Whitelist management
- Audit logging of all admin actions
- HTTP API for external tools

**UI Location:** Extended Chat (TAB key) → SXR Admin Panel icon

---

### 3. SXRNameplatesPlugin
**Purpose:** 3D floating nameplates above player cars

**Features:**
- Driver name, level, car class display
- Prestige colors:
  - P0: White
  - P1-P49: Gradient colors (Gold → Aqua)
  - P50+: Animated rainbow gradient (Mythic tier)
- Safety rating indicator
- Club tags
- Leaderboard rank display
- Settings panel in Extended Chat

**Tech:** CSP Lua scripts for 3D rendering

---

### 4. SXRSPBattlePlugin (SP Battle)
**Purpose:** TXR-style Spirit Point battle system

**Features:**
- Challenge by flashing lights 3x
- Accept with hazard lights or `/accept` command
- SP drain based on distance zones (6 configurable zones)
- Collision penalties (opponent & wall)
- Overtake bonuses
- Lead bonus per second
- Driver level bonus to max SP
- Elo-style leaderboard with ratings
- Lua UI with dual SP bars, countdown, distance indicator

**Battle Flow:**
1. Flash lights 3x → Challenge
2. Hazards/accept → Line up side-by-side
3. 3-2-1-GO countdown
4. Stay ahead or lose SP
5. SP depleted = loss

---

### 5. SXRTrafficPlugin (Realistic Traffic)
**Purpose:** Server-side traffic simulation with realistic AI behavior

**Features:**
- IDM (Intelligent Driver Model) for car-following
- MOBIL algorithm for lane-change decisions
- Driver personality system (0-1 aggressiveness scale):
  - 0.0: Passive drivers, 10 under speed limit, rare lane changes
  - 1.0: Aggressive drivers, 30 over limit, frequent lane changes
- Zone-based density (less on C1, more on Wangan)
- Time-of-day density variation
- Spatial cell optimization
- Configurable spawn/despawn distances

**Key Config:**
- `BaseDensityPerKm`, `MaxTotalVehicles`, `VehiclesPerPlayer`
- `TimidDriverRatio`, `NormalDriverRatio`, `AggressiveDriverRatio`
- `LaneChangeParams` for MOBIL algorithm

---

### 6. SXRCarLockPlugin
**Purpose:** Vehicle class restrictions by driver level

**Features:**
- Car class system:
  - S-Class: Level 50+ (Supercars)
  - A-Class: Level 30+ (Sports Cars)
  - B-Class: Level 15+ (Tuners)
  - C-Class: Level 5+ (Street)
  - D-Class: Level 1+ (Starter)
  - E-Class: Level 1+ (Entry/Kei)
- Join validation on connect
- Grace period before enforcement
- Enforcement modes: Spectate or Kick
- Prestige bypass (P1+ can drive any car)
- Integration with SXRWelcomePlugin for warnings

---

### 7. SXRWelcomePlugin
**Purpose:** Welcome popup with server info and restriction warnings

**Features:**
- Welcome message on player join
- Server rules display
- Car restriction warning if player doesn't meet requirements
- List of available cars player CAN drive
- Driver level and XP display
- Dismissable popup (must acknowledge before playing)
- Integrates with SXRCarLockPlugin

---

### 8. SXRLeaderboardPlugin
**Purpose:** Centralized leaderboards

**Features:**
- Multiple board types:
  - Driver Ranks (by level/prestige)
  - SP Battle Ratings
  - Time Trials (per route)
  - Longest Run tracking
- Weekly/monthly boards
- Pit detection integration
- Discord webhook notifications
- HTTP API endpoints

---

## Technical Stack

- **Framework:** .NET 9.0 / C#
- **Server:** AssettoServer (compujuckel fork)
- **Client UI:** CSP (Custom Shaders Patch) Lua scripts
- **Storage:** JSON files, SQLite (optional)
- **Build:** `dotnet build -c Release`

### Project Structure
```
AssettoServer_c/
├── AssettoServer/              # Core server
├── SXRPlayerStatsPlugin/
├── SXRAdminToolsPlugin/
├── SXRNameplatesPlugin/
├── SXRSPBattlePlugin/
├── SXRTrafficPlugin/
├── SXRCarLockPlugin/
├── SXRWelcomePlugin/
├── SXRLeaderboardPlugin/
├── ACDyno/                     # Car analyzer tool
├── CLAUDE.md                   # AI instructions
├── PLUGINS_TODO.md             # Roadmap
└── CHANGELOG.md
```

### Plugin Structure Pattern
```
SXR[Name]Plugin/
├── SXR[Name]Plugin.cs          # Main plugin class
├── SXR[Name]Configuration.cs   # YAML config model
├── SXR[Name]Module.cs          # Autofac DI registration
├── [Additional services].cs
├── lua/
│   └── sxr[name].lua           # CSP Lua UI script
├── cfg/
│   └── plugin_sxr_[name]_configuration.yml
├── README.md
└── CHANGELOG.md
```

---

## Key Architecture Patterns

### Plugin Registration
```csharp
public class SXRExampleModule : AssettoServerModule<SXRExampleConfiguration>
{
    public SXRExampleModule(SXRExampleConfiguration config) : base(config) { }
    
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SXRExamplePlugin>().AsSelf().As<IAssettoServerAutostart>().SingleInstance();
    }
}
```

### Cross-Plugin Integration
```csharp
// Provider pattern for loose coupling
carLock.SetDriverLevelProvider(steamId => playerStats.GetStats(steamId).DriverLevel);
carLock.SetPrestigeRankProvider(steamId => playerStats.GetStats(steamId).PrestigeRank);
nameplates.SetPrestigeRankProvider(steamId => playerStats.GetStats(steamId).PrestigeRank);
```

### Lua UI Registration (Extended Chat)
```lua
ui.registerOnlineExtra(
    ui.Icons.Settings,           -- Icon
    "SXR Plugin Name",           -- Display name
    function() return true end,  -- Visibility check
    function()                   -- Content draw function
        -- Draw UI here
        return false             -- Return false to keep open
    end,
    vec2(400, 450)               -- Panel size
)
```

---

## Traffic System Details

### Modified Core Files
The traffic system modifies AssettoServer core AI files:
- `AiState.cs` - Main AI vehicle state and behavior
- `LaneChangeParams.cs` - MOBIL algorithm parameters

### Key Algorithms
1. **IDM (Intelligent Driver Model)** - Car following with safe gap maintenance
2. **MOBIL** - Lane change decisions based on advantage and safety
3. **Aggressiveness (0-1)** - Affects speed variance, lane change frequency, gap acceptance

### Aggressiveness Behaviors
| Value | Speed Offset | Lane Changes | Gap Acceptance |
|-------|--------------|--------------|----------------|
| 0.0   | -10 km/h     | Rare         | Conservative   |
| 0.5   | ±0 km/h      | Normal       | Standard       |
| 1.0   | +30 km/h     | Frequent     | Aggressive     |

---

## ACDyno - Car Analyzer Tool

**Purpose:** Analyze and balance Assetto Corsa car mods for the server

**Features:**
- Performance Index (PI) calculation (Forza-style)
- Car class assignment (D through X)
- INI file parsing and editing
- Gemini AI integration for balance suggestions
- Comparison between cars
- Backup system for edits

**Tech:** Photino.Blazor (cross-platform desktop app)

**PI Calculation:**
- Speed: 20%
- Acceleration: 25%
- Handling: 30%
- Braking: 15%
- Launch: 10%

---

## Current Development State

### Completed
- [x] Player stats and leveling system
- [x] Prestige system (P1-P999)
- [x] Admin tools panel
- [x] 3D nameplates with prestige colors
- [x] SP Battle system (TXR-style)
- [x] Car lock by driver level
- [x] Welcome popup with restrictions
- [x] Basic traffic improvements
- [x] Leaderboard framework

### In Progress
- [ ] Traffic aggressiveness/personality variance
- [ ] Traffic density tuning
- [ ] Leaderboard pit detection
- [ ] Discord webhook integration

### Planned (from PLUGINS_TODO.md)
- [ ] Time Trial routes with checkpoints
- [ ] Discord bot integration
- [ ] Crew/team system
- [ ] Economy system (in-game currency)
- [ ] Car customization persistence

---

## Server Configuration

### extra_cfg.yml
```yaml
EnablePlugins:
  - SXRPlayerStatsPlugin
  - SXRAdminToolsPlugin
  - SXRNameplatesPlugin
  - SXRSPBattlePlugin
  - SXRCarLockPlugin
  - SXRWelcomePlugin
  - SXRLeaderboardPlugin

AiParams:
  MaxAiTargetCount: 500
  AiPerPlayerTargetCount: 40
  TrafficDensity: 1.0
  MaxSpeedKph: 90
  TwoWayTraffic: false
  WrongWayTraffic: true
  HourlyTrafficDensity: [0.15, 0.10, 0.08, ...]
```

### Server Time (Permanent Midnight)
```ini
[SERVER]
SUN_ANGLE=-76           ; Midnight
TIME_OF_DAY_MULT=0      ; Time frozen
```

---

## Build & Deploy

### Build Plugin
```powershell
cd C:\Users\NVLL\source\repos\CodexDread\AssettoServer_c
dotnet build SXR[Name]Plugin -c Release
```

### Deploy Plugin
```powershell
Copy-Item ".\SXR[Name]Plugin\bin\Release\net9.0\SXR[Name]Plugin.dll" `
  "S:\Games\steamapps\common\assettocorsa\server\custom_server\plugins\" -Force
```

### Server Location
`S:\Games\steamapps\common\assettocorsa\server\custom_server\`

---

## Key Files to Reference

1. `CLAUDE.md` - Project instructions for AI assistance
2. `PLUGINS_TODO.md` - Development roadmap
3. `CHANGELOG.md` - Version history
4. `AssettoServer/Ai/AiState.cs` - Traffic AI behavior
5. `AssettoServer/Ai/LaneChangeParams.cs` - Lane change config

---

## Testing Notes

- Debug mode: Set `Debug: true` in `extra_cfg.yml` AiParams
- AI debug overlay shows numbers on traffic cars
- Logs location: `logs/` folder in server directory
- Plugin configs auto-generate on first run

