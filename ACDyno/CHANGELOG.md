# ACDyno - Assetto Corsa Car Analyzer Changelog

## Project Overview
A .NET Blazor Hybrid application for analyzing and balancing Assetto Corsa car mods with Forza-style performance classification.

## [0.5.1] - 2026-01-11

### Bug Fixes - Aero Drag Coefficient

**Fixed:**
- Drag coefficient now properly parsed from aero LUT files (e.g., `aero_body_AOA_CD.lut`)
- Previously used CD_GAIN (a multiplier, typically 1.0) instead of actual Cd from LUT
- This was causing severely underestimated top speeds (e.g., 194 km/h instead of 337 km/h)

**Technical Details:**
- Added `BaseCd` and `BaseCl` properties to `WingData` model
- Added `EffectiveCd` property that combines BaseCd * CDGain
- CarLoader now parses aero LUT files to extract base coefficients at 0 degrees AOA
- PerformanceMetrics uses actual drag coefficient for top speed calculations

### Files Changed
- `Models/AeroData.cs` - Added BaseCd, BaseCl, EffectiveCd, EffectiveCl properties
- `Services/CarLoader.cs` - Added GetBaseCoefficientFromLut() method
- `Models/PerformanceMetrics.cs` - Updated drag coefficient extraction

---

## [0.5.0] - 2026-01-10

### Major Overhaul - Physics-Based Performance Metrics

**App Updates:**
- Updated displayed version to v0.5.0
- Added in-app Changelog page with version history
- Updated tagline to "Physics-Based Performance Analysis Tool"

Complete redesign of the performance calculation system based on extensive research into:
- Forza Motorsport PI system
- Real-world motorsport engineering formulas
- Professional dynamometer software methods
- Racing simulation balance-of-performance systems

#### New Performance Calculation Engine

**Top Speed Calculation (Physics-Based)**
- Drag-limited: v = ∛(2P / (ρ × Cd × A))
- Gear-limited: Based on redline, gear ratios, and tire circumference
- Actual top speed = min(drag-limited, gear-limited)
- Reports which factor is limiting

**Acceleration Estimation**
- Energy-based theoretical minimum with correction factors
- Traction-limited check for high-power vehicles
- Drivetrain-specific launch penalties (AWD best, FWD worst)
- Hale formula for quarter-mile: ET = 5.825 × (W/P)^(1/3)

**Braking Distance**
- Physics-based: d = v² / (2 × μ × g)
- Brake balance efficiency calculation
- Aero downforce contribution at speed
- Weight transfer compensation

**Lateral G Calculation**
- Base grip from tire DY0 coefficients
- Weight distribution penalty (50/50 optimal)
- Tire width bonus
- Aero downforce contribution at reference speed

#### New Component Scores (0-100 scale)
- **Speed**: 80 mph = 0, 280 mph = 100
- **Acceleration**: 12s 0-60 = 0, 2s = 100
- **Handling**: 0.6G = 0, 2.5G = 100
- **Braking**: 400ft 60-0 = 0, 85ft = 100
- **Launch**: Composite of drivetrain + grip + torque + weight

#### Performance Index Algorithm
- Weighted composite: Handling 30%, Accel 25%, Speed 20%, Braking 15%, Launch 10%
- Power-to-weight bonus
- Diminishing returns curve at high performance
- Range: 100-998 (999 = unrealistic)

#### Performance Classes
| Class | PI Range | Examples |
|-------|----------|----------|
| D | 100-400 | Economy cars |
| C | 401-500 | Hot hatches |
| B | 501-600 | Sports cars |
| A | 601-700 | Supercars |
| S1 | 701-800 | GT4/Supercars |
| S2 | 801-900 | GT3/Hypercars |
| S3 | 901-998 | LMP/Extreme |
| X | 999 | Unrealistic |

#### Unrealistic Vehicle Detection
- Power-to-weight > 1500 HP/ton
- Mass < 500 kg
- Grip coefficient > 2.0 or < 0.5
- Drag coefficient < 0.18
- Lift coefficient > 5.0
- 0-60 time < 1.5s

#### Unit System
- All internal calculations use SI units
- Display in imperial (mph, ft, lbs, HP, lb-ft)
- Added UnitConversion.cs with comprehensive constants

#### Gemini AI Integration Improvements
- Now sends complete raw vehicle data to AI
- GetRawDataForAI() provides comprehensive dump
- Better analysis prompts with specific data references
- Updated to Gemini 2.0 Flash API

### Files Changed
- New: `Models/UnitConversion.cs`
- New: `Components/Pages/Changelog.razor`
- Rewritten: `Models/PerformanceMetrics.cs` (complete overhaul)
- Updated: `Services/GeminiService.cs` (comprehensive data sending)
- Updated: `Components/Pages/Analyze.razor` (imperial units, new metrics)
- Updated: `Components/Layout/NavMenu.razor` (version update, changelog link)
- Updated: `ACDyno.csproj` (wwwroot copy directive)

## [0.4.0] - 2026-01-10

### Major Features

#### Native Folder Picker
- **Browse button** for selecting cars folder using Windows native dialog
- No more manual path pasting required

#### Gemini AI Integration
- **AI-powered car analysis** on the Analyze page
- **AI balance suggestions** in the new Editor
- API key saved to user profile folder (~/.acdyno/)
- Environment variable support (GEMINI_API_KEY)

#### New Tune & Edit Page
- **Visual tuning interface** with live graphs
- **Power/Torque curves** displayed as SVG graph
- **Gear ratio visualization** as bar chart
- **Brake bias slider** with visual indicator
- **Weight distribution slider** with axle visualization
- **Tyre grip balance indicator** (oversteer/understeer)
- **AI Suggestions panel** - get Gemini recommendations for tuning
- Categories: Engine, Drivetrain, Brakes, Tyres, Aero, Weight

#### Improved Car Selection
- Native `<select>` dropdowns replace custom FilterableDropdown
- Text filter input for searching large car lists
- More reliable selection behavior

### UI/UX Improvements
- All pages now have proper `@namespace` declarations
- Removed broken FilterableDropdown component
- Cleaner dropdown styling with native elements
- AI analysis button on Analyze page

### Files Changed
- New: `Services/WindowService.cs` - Native dialog access
- New: `Services/GeminiService.cs` - Gemini AI integration
- Removed: `Components/Shared/FilterableDropdown.razor`
- Updated: All page components with namespaces and native dropdowns

## [0.3.0] - 2026-01-10

### Major Change: Standalone Desktop Application
- **Converted from Blazor Server to Photino.Blazor** - Now runs as a native Windows desktop app
  - No browser required
  - Native window with title "ACDyno - Assetto Corsa Car Analyzer"
  - Default window size 1400x900, centered on screen
- Removed ASP.NET Core dependencies (appsettings, launchSettings, etc.)

### Bug Fixes
- **Fixed folder scanning**: Improved thread safety and error handling
  - Now returns detailed success/error messages
  - Handles access denied errors gracefully
  - Skips problematic folders without crashing
  - Reports how many cars found and any errors

### How to Run
```bash
dotnet restore
dotnet build
dotnet run
```
Or build a release:
```bash
dotnet publish -c Release
```

## [0.2.2] - 2026-01-10

### New Features: Car Library System
- **Cars Folder Scanning**: Scan a parent folder containing multiple car folders
  - Automatically discovers cars by detecting `data/car.ini` structure
  - Extracts display names from car.ini SCREEN_NAME field
  - Caches loaded cars to avoid re-parsing
- **Filterable Dropdown Component**: New searchable dropdown for car selection
  - Type to filter by car name or folder name
  - Keyboard navigation (arrow keys, enter, escape)
  - Shows display name and folder name for each car
- **Cross-Tab State Sharing**: Car analyzed on Analyze tab auto-populates other tabs
  - Compare tab: Pre-fills Car 1 selection
  - Balance tab: Pre-fills Target car selection  
  - Editor tab: Pre-fills car selection
- **CarLibraryService**: New singleton service for shared state management
  - Event-based updates when selection changes
  - Persistent state across page navigation

### UI Improvements
- Quick Actions panel added to Analyze page with links to Compare, Balance, Editor
- Better visual feedback when no car library is loaded
- Improved editor with car dropdown selection

## [0.2.1] - 2026-01-10

### Bug Fixes
- **FIXED:** Renamed `DriveType` enum to `DriveLayout` to avoid namespace conflict with `System.IO.DriveType`
  - Updated in: DrivetrainData.cs, PerformanceMetrics.cs, CarLoader.cs, BalanceSuggester.cs
  - Updated in Razor pages: Analyze.razor, Compare.razor
- **FIXED:** Added `EstimatedTopSpeed` alias property to PerformanceMetrics (maps to EstimatedTopSpeedKmh)
- **FIXED:** Removed unused `skillVariance` variable warning in BalanceSuggester.cs (incorporated into calculation)

## [0.2.0] - 2026-01-10

### UI Components Added
- **Home Dashboard** - Quick action cards, performance class reference, balance philosophy
- **Analyze Page** - Load car, view PI/class, performance scores, power/grip/aero stats, validation issues
- **Compare Page** - Side-by-side comparison with race predictions for various scenarios
- **Balance Page** - Generate balance suggestions with quick fixes and comprehensive changes
- **Editor Page** - INI file editor with auto-backup, syntax hints, file browser

### UI Features
- Dyno-inspired dark theme with orange/teal accent colors
- Performance class badges (D through X) with distinctive colors
- Score bar visualizations for Speed, Handling, Acceleration, Launch, Braking
- Race prediction probability bars for different scenarios
- Validation severity indicators (Info, Warning, Error, Critical)
- Responsive layout with sidebar navigation

### Service Registration
- All services registered in Program.cs as singletons
- CarLoader, UnrealisticDetector, BalanceSuggester, BackupService

## [0.1.0] - 2026-01-10

### Initial Development
- **Created by:** Claude (AI Assistant)
- **Framework:** .NET 8.0 Blazor Server (easily portable to MAUI Blazor Hybrid)

### Added
- Project structure based on Blazor Server template
- Core INI/LUT file parsing services
- Car data models for all AC car configuration files:
  - `car.ini` - Basic car info (mass, inertia)
  - `engine.ini` - Engine specs (power, turbo, limiter)
  - `drivetrain.ini` - Transmission and differential
  - `tyres.ini` - Tire grip coefficients
  - `suspensions.ini` - Suspension geometry and rates
  - `brakes.ini` - Brake torque and bias
  - `aero.ini` - Aerodynamic coefficients
  - `power.lut` - Torque curve data
- Performance Index (PI) calculation system inspired by Forza:
  - D Class: 100-299
  - C Class: 300-399
  - B Class: 400-499
  - A Class: 500-599
  - S Class: 600-699
  - S1 Class: 700-799
  - S2 Class: 800-899
  - X Class: 900+
- Unrealistic value detection with configurable thresholds
- Car comparison functionality
- Balance suggestion engine
- Auto-backup system for INI file editing
- Dyno-inspired modern dark UI theme

### Technical Notes
- INI parser handles section headers `[SECTION]` and key-value pairs `KEY=VALUE`
- LUT parser handles pipe-delimited format `RPM|VALUE`
- Performance calculations consider:
  - Power-to-weight ratio
  - Peak torque with turbo boost
  - Tire grip coefficients (DX0, DY0)
  - Aerodynamic drag (CD_GAIN)
  - Drivetrain losses (FWD/RWD/AWD)
  - Brake performance
  - Weight distribution

### File Structure
```
ACDyno/
├── Components/
│   ├── Layout/
│   │   └── MainLayout.razor       # Dyno-inspired layout
│   └── Pages/
│       ├── Home.razor             # Dashboard
│       ├── Analyze.razor          # Single car analysis
│       ├── Compare.razor          # Car comparison
│       ├── Balance.razor          # Balance suggestions
│       └── Editor.razor           # INI file editor
├── Models/
│   ├── CarData.cs                 # Main car data container
│   ├── EngineData.cs              # Engine configuration
│   ├── DrivetrainData.cs          # Transmission/diff
│   ├── TyreData.cs                # Tire properties
│   ├── AeroData.cs                # Aerodynamics
│   ├── SuspensionData.cs          # Suspension setup
│   └── PerformanceMetrics.cs      # Calculated stats
├── Services/
│   ├── IniParser.cs               # INI file parsing
│   ├── LutParser.cs               # LUT file parsing
│   ├── CarLoader.cs               # Load car from directory
│   ├── PerformanceCalculator.cs   # PI and stats calc
│   ├── BalanceSuggester.cs        # Balance recommendations
│   ├── UnrealisticDetector.cs     # Value validation
│   └── BackupService.cs           # Auto-backup system
└── wwwroot/
    └── css/
        └── dyno-theme.css         # Custom dyno styling
```

### Known Limitations
- Project created in Linux environment without MAUI workloads
- Requires local `dotnet restore` to fetch NuGet packages
- For MAUI Blazor Hybrid, copy Components/ and Services/ to MAUI project

### Next Steps for Future Development
1. Add telemetry data import for real-world validation
2. Implement track-specific balance profiles
3. Add export to server entry_list.ini format
4. Consider adding lap time simulation
5. Add batch processing for multiple cars
6. Implement custom class definitions

---

## Development Notes

### Converting to MAUI Blazor Hybrid
1. Create new MAUI Blazor Hybrid project in Visual Studio
2. Copy `Components/`, `Models/`, `Services/` folders
3. Update `MauiProgram.cs` to register services
4. Copy `wwwroot/css/dyno-theme.css`
5. Update `MainLayout.razor` imports if needed

### Performance Index Formula
The PI calculation weighs multiple factors:
- Base PI = (Power/Weight) * 200
- Grip modifier = ((DX0_front + DY0_front + DX0_rear + DY0_rear) / 4 - 1.0) * 100
- Aero modifier = (1 - CD_GAIN * 0.1) * 20
- Drivetrain modifier: RWD = 0, FWD = -10, AWD = +15
- Final PI = Base + Grip + Aero + Drivetrain + Brake bonus

### Unrealistic Value Thresholds
Default thresholds for flagging suspicious values:
- Power > 2000 HP
- Power/Weight > 1.0 HP/kg for street cars
- DY0 > 2.5 (extreme tire grip)
- MAX_BOOST > 3.0
- Mass < 500 kg or > 3000 kg
- SPRING_RATE > 500000 N/m
