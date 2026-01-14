# Traffic Overhaul Plugin - Changelog

## [2.2.0] - 2026-01-14

### SMOOTH LANE CHANGE TRANSITIONS
Fixed the instant teleport issue - AI cars now smoothly transition between lanes!

#### How It Works:
1. When a lane change starts, an `ActiveLaneChange` record is created
2. Each tick, the plugin calculates progress (0.0 → 1.0) over the duration
3. Uses **quintic polynomial** for smooth S-curve lateral offset
4. Applies offset perpendicular to car's heading
5. When complete, finalizes the spline point switch

#### Key Changes:
- Added `ActiveLaneChange` class to track in-progress lane changes
- Added `UpdateLaneChangePosition()` - applies smooth lateral offset each tick
- Added `StartSmoothLaneChange()` - initiates transition with calculated duration
- Added `FinalizeLaneChange()` - switches spline point only when complete
- Removed old instant `ExecuteLaneChange()` / `TrySmoothLaneChange()`

#### Duration Formula (from LaneChangeTrajectory.cs):
- ~3.5 seconds at 100 km/h
- ~5-6 seconds at 300 km/h
- Clamped between 2.5 and 7.0 seconds

---

## [2.1.0] - 2026-01-14

### PROPER ASSETTOSERVER INTEGRATION
This version now properly integrates with AssettoServer's AI system!

#### Key Integration Points:
- **AiSpline Integration**: Uses AssettoServer's actual spline data for world position lookup
- **AiState Direct Control**: Modifies AiState.Acceleration directly for IDM behavior  
- **SlowestAiStates**: Uses built-in leader detection for efficient car-following
- **Lane Changes**: Smooth lane changes via reflection, updates spline tracking properly

#### How It Works:
1. Plugin runs alongside the built-in AI system (doesn't replace it)
2. On each tick, collects all initialized AiState objects from EntryCarManager
3. For each AI car:
   - Finds leader using `_spline.SlowestAiStates[pointId]`
   - Calculates IDM acceleration based on gap/speed/approaching rate
   - Overrides `aiState.Acceleration` if IDM suggests more braking
   - Evaluates MOBIL lane change criteria if blocked by slower traffic
   - Executes lane change by updating `CurrentSplinePointId` via reflection

### Changed
- SXRRealisticTrafficPlugin now requires `AiSpline` dependency injection
- SXRTrafficManager now tracks lane change cooldowns per AiState (not internal TrafficVehicle)
- Removed internal parallel traffic simulation (was disconnected from actual AI)

### Fixed  
- Plugin now actually affects AI car behavior!
- Lane changes update SlowestAiStates tracking properly
- Reflection-based lane changes preserve AI state (speed, color, etc.)

---

## [1.0.1] - 2026-01-14

### Bug Fixes
- Fixed `CarStatus.SplinePosition` property access - now calculates from world position
- Added `EstimateSplinePosition()` method to traffic manager for world-to-spline conversion
- Fixed `FileStream.Read` CA2022 warning - now uses `ReadExactly` for proper byte reading
- Added `using Autofac;` for ContainerBuilder
- Made `TrafficZone` class partial in both ShutoZones.cs and ShutoZonesV2.cs
- Added `AssettoServer.Shared` project reference for CriticalBackgroundService
- Added `<PlatformTarget>x64</PlatformTarget>` to match AssettoServer architecture
- Updated to .NET 9.0

---

## [0.2.0] - 2026-01-11

### Spline Analysis & Zone Calibration
- Analyzed actual fast_lane.ai pack for shutoko_revival_project_094_ptb1
- Parsed 139 spline files (26 main routes + 78 junctions + 35 exits)
- Extracted world coordinate bounds for zone configuration
- Created ShutoZonesV2.cs with accurate zone mappings based on real spline data

### Map Analysis Results
- **Total map size**: 18.2km x 26.6km (X: -11049 to 7190, Z: -10027 to 16610)
- **C1/C2 Inner Loop**: fast_lane1-6 (125,231 points total)
- **Wangan/Bayshore**: fast_lane15-16 (44,730 points, ~6.5km documented length)
- **Outer Sections**: fast_lane10-14 (78,112 points)
- **Extended Routes**: fast_lane17-26 (31,458 points)
- **Junctions**: fast_lanelj1-78 (78 junction splines for merges/splits)
- **Exit Ramps**: fast_lanea1-17, fast_laneb1-11, fast_lanee1-14

### Zone Configuration
- Spline-based zone identification (by filename)
- World coordinate fallback for position-based queries
- Pattern matching for junction/exit spline groups

---

## [0.1.0] - 2026-01-11

### Initial Implementation
- Created RealisticTrafficPlugin as a new plugin for AssettoServer
- Implemented Intelligent Driver Model (IDM) for realistic car-following behavior
- Implemented MOBIL algorithm for intelligent lane-change decisions
- Added zone-based traffic density system for Shuto Expressway sections
- Added driver personality variance system (timid, normal, aggressive)
- Added smooth quintic polynomial lane change trajectories
- Added spatial grid partitioning for efficient O(1) neighbor lookups
- Created configuration system for all traffic parameters

### Architecture Decisions
- Plugin-based approach: Does NOT modify core AssettoServer code
- Server-authoritative: All AI logic runs server-side, clients only receive position updates
- Uses existing spline system: Enhances behavior, doesn't replace pathfinding

### Research Sources Used
- Treiber et al. IDM paper (2000) for car-following model
- Kesting et al. MOBIL paper (2007) for lane-change logic
- Shuto Expressway traffic data from Metropolitan Expressway Company
- FiveM OneSync patterns for networking optimization

### Known Limitations
- Requires testing with actual AssettoServer integration
- Zone definitions need calibration against actual Shuto spline coordinates
- May need adjustment for different spline configurations

### TODO
- [ ] Integration testing with AssettoServer
- [ ] Performance profiling with 100+ vehicles
- [ ] Fine-tune zone boundaries for Shuto map variants
- [ ] Add time-of-day traffic density modulation
- [ ] Consider junction-aware spawning logic
