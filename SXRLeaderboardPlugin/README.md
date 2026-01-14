# SXR Leaderboard Plugin

Centralized leaderboard system for AssettoServer, providing comprehensive tracking for Player Ranks, Time Trials, and Longest Runs.

## Features

### Player Ranks
Track competitive standings with Elo-based rating:
- Overall rank based on Elo rating
- Win/loss records and percentages
- Prestige-aware display
- Favorite car tracking
- Club tag display
- Average and top speed stats

### Time Trials
Route-based time attack leaderboards:
- Multiple predefined routes (C1 Inner, Wangan, Yokohane, etc.)
- Clean/Dirty run distinction
- Personal best tracking
- World record indicators
- Car class display
- Sector time support (optional)

### Longest Run
Endurance racing leaderboard (races before pitting):
- Race count tracking
- Win rate during run
- Distance and duration tracking
- Primary car used
- Active run display

## In-Game UI

Access via **TAB** → **🏆 SXR Leaderboards**

The UI provides:
- Three-tab interface for each leaderboard type
- Pagination for large leaderboards
- Your personal stats displayed
- Active run widget for endurance tracking
- Auto-refresh every 30 seconds

## Leaderboard Headers

### Player Ranks
| # | Level | Player | Favorite Car | W/Races | Club | Avg Speed |
|---|-------|--------|--------------|---------|------|-----------|

### Time Trials  
| # | Player | Car | Route | Time | Status |
|---|--------|-----|-------|------|--------|

### Longest Run
| # | Player | Races | W/L | Distance | Duration |
|---|--------|-------|-----|----------|----------|

## Installation

1. Copy `SXRLeaderboardPlugin` folder to your plugins directory
2. Configure `plugin_sxr_leaderboard_config.yml`
3. Customize routes in `cfg/routes.json`
4. Restart server

## Configuration

```yaml
# Enable features
Enabled: true
EnableTimeTrials: true
EnableLongestRun: true

# Ranking settings
MinRacesForRanking: 5
StartingElo: 1000

# Longest run
MinRacesForLongestRun: 3
```

## HTTP API

### Player Ranks
```
GET /sxrleaderboards/ranks?page=1&pageSize=20&sortBy=elo
GET /sxrleaderboards/ranks/{steamId}
```

### Time Trials
```
GET /sxrleaderboards/timetrials?route=c1_inner_cw&page=1
GET /sxrleaderboards/routes
POST /sxrleaderboards/timetrials (submit new time)
```

### Longest Run
```
GET /sxrleaderboards/longestruns?sortBy=races
GET /sxrleaderboards/activeruns
POST /sxrleaderboards/endrun/{steamId}
```

### Player Summary
```
GET /sxrleaderboards/summary/{steamId}
```

## Integration with Other Plugins

### From SXRSPBattlePlugin
```csharp
var leaderboard = services.GetRequiredService<SXRLeaderboardPlugin>();

// After SP Battle ends
leaderboard.RecordRaceResult(
    steamId: winner.SteamId,
    playerName: winner.Name,
    isWin: true,
    carModel: winner.CarModel,
    eloChange: 25
);
```

### From Pit Detection
```csharp
// When player enters pit area
leaderboard.EndPlayerRun(steamId, RunEndReason.Pitted);
```

### From Time Trial System
```csharp
var entry = leaderboard.SubmitTimeTrial(new TimeTrialSubmission
{
    SteamId = player.SteamId,
    PlayerName = player.Name,
    RouteId = "c1_inner_cw",
    TimeMs = 125340,
    CarModel = "ks_ferrari_488",
    AvgSpeedKph = 185.5f
});

if (entry != null && entry.IsWorldRecord)
{
    // Announce new world record!
}
```

## Data Storage

Data is stored in JSON files in the `data/` folder:
- `player_ranks.json` - Player ranking data
- `time_trials.json` - Time trial entries by route
- `longest_runs.json` - Longest run records

Auto-saves every 5 minutes and on server shutdown.

## Route Configuration

Edit `cfg/routes.json` to add custom routes:

```json
{
  "RouteId": "custom_route",
  "RouteName": "My Custom Route",
  "Category": "Custom",
  "DistanceKm": 5.0,
  "Description": "A custom time trial route",
  "IsActive": true
}
```

## Dependencies

- AssettoServer (latest)
- SXRPlayerStatsPlugin (optional, for level/prestige)
- SXRSPBattlePlugin (optional, for race results)
- SXRCarLockPlugin (optional, for car classes)
