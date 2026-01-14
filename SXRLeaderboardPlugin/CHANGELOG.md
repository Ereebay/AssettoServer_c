# SXR Leaderboard Plugin - Changelog

## [1.0.2] - 2026-01-14

### Route Editor Admin Tool

#### Pop-Out UI (F9 to toggle)
- **Route List View**: Browse all existing routes with status indicators
- **Create New Routes**: Add new time trial routes with all settings
- **Edit Routes**: Modify existing route properties
- **Delete Routes**: Remove routes with confirmation dialog

#### Position Capture
- **Capture Start Zone**: Press button to capture current car position as start
- **Capture Finish Zone**: Press button to capture current car position as finish
- **Same as Start**: Quick button to make finish = start (for loops)
- **Add Checkpoints**: Capture checkpoint positions along the route

#### Route Properties
- Route ID (unique identifier)
- Route Name (display name)
- Category (for grouping)
- Distance (km)
- Description
- Active/Inactive toggle
- Start Zone (X, Y, Z, Radius)
- Finish Zone (X, Y, Z, Radius)
- Checkpoints list

#### API Endpoints
- POST /sxrleaderboards/routes - Create route
- PUT /sxrleaderboards/routes/{routeId} - Update route
- DELETE /sxrleaderboards/routes/{routeId} - Delete route
- GET /sxrleaderboards/routes/{routeId} - Get single route

---

## [1.0.1] - 2026-01-14

### Time Trials Improvements

#### Route Filter
- **Route Dropdown**: Filter time trials by specific route
- **Distance Display**: Shows route distance in dropdown

#### Clean Time Priority
- **2-Minute Threshold**: Clean times within 2 minutes of a dirty time now rank higher
- **Rewards Clean Driving**: Encourages collision-free runs even if slightly slower

---

## [1.0.0] - 2026-01-14

### Initial Release - Centralized Leaderboard System

#### Core Features

##### Player Ranks Leaderboard
- **Elo Rating System**: Competitive ranking based on race results
- **Win/Loss Tracking**: Total races, wins, losses, win rate
- **Prestige Display**: Shows prestige rank with color coding
- **Favorite Car**: Tracks most-used vehicle
- **Club Tag Support**: Display club affiliation
- **Average Speed**: Running average speed tracking
- **Top Speed**: Personal best speed recorded

##### Time Trials Leaderboard  
- **Route-Based Times**: Separate leaderboards per route
- **Clean/Dirty Status**: Marks runs with collisions
- **Personal Best Tracking**: Only records improvements
- **World Record Indicator**: Marks fastest overall time
- **Car Class Display**: Shows vehicle class used
- **Sector Times**: Optional split time tracking

##### Longest Run Leaderboard
- **Race Count**: Number of SP Battles completed before pitting
- **Distance Tracking**: Total km driven in run
- **Duration Tracking**: Time spent in run
- **Win Rate in Run**: Performance during endurance stint
- **Primary Car**: Most-used car during run
- **Active Run Display**: Shows current run progress

#### Data Management
- **JSON Storage**: Persistent data storage
- **Auto-Save**: Saves every 5 minutes
- **Graceful Shutdown**: Saves on server stop

#### Lua UI
- **Tab-Based Interface**: Easy switching between leaderboard types
- **Pagination**: Navigate large leaderboards
- **Auto-Refresh**: Updates every 30 seconds
- **Personal Stats**: Shows your own ranking
- **Active Run Widget**: Live display of current run

#### HTTP API
- GET /sxrleaderboards/ranks - Player ranks
- GET /sxrleaderboards/timetrials - Time trials
- GET /sxrleaderboards/longestruns - Longest runs
- GET /sxrleaderboards/routes - Available routes
- GET /sxrleaderboards/summary/{steamId} - Player summary
- GET /sxrleaderboards/activeruns - All active runs
- POST /sxrleaderboards/endrun/{steamId} - End a run (pit detection)

#### Integration Points
- Driver Level provider
- Prestige Rank provider
- Club Tag provider
- Car Display Name provider
- Car Class provider

---

## Planned Features
- Weekly/Monthly leaderboards
- Route checkpoint validation
- Replay recording for top times
- Discord webhook notifications
- Seasonal rankings
- Achievement badges on leaderboard
