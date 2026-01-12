using ACDyno.Models;

namespace ACDyno.Services;

/// <summary>
/// Service to load car data from an Assetto Corsa car mod folder
/// </summary>
public class CarLoader
{
    /// <summary>
    /// Load a car from its data folder path
    /// </summary>
    public CarData LoadCar(string carFolderPath)
    {
        var car = new CarData
        {
            FolderPath = carFolderPath,
            FolderName = Path.GetFileName(carFolderPath),
            LoadedAt = DateTime.Now
        };
        
        var dataPath = Path.Combine(carFolderPath, "data");
        if (!Directory.Exists(dataPath))
        {
            // Maybe we're already in the data folder
            if (File.Exists(Path.Combine(carFolderPath, "car.ini")))
                dataPath = carFolderPath;
            else
                throw new DirectoryNotFoundException($"Data folder not found: {dataPath}");
        }
        
        // Load all configuration files
        LoadCarIni(car, dataPath);
        LoadEngineIni(car, dataPath);
        LoadPowerLut(car, dataPath);
        LoadDrivetrainIni(car, dataPath);
        LoadTyresIni(car, dataPath);
        LoadSuspensionsIni(car, dataPath);
        LoadBrakesIni(car, dataPath);
        LoadAeroIni(car, dataPath);
        
        // Link engine to power curve
        car.Engine.PowerCurve = car.PowerCurve;
        
        // Calculate metrics
        car.Metrics = PerformanceMetrics.Calculate(car);
        
        return car;
    }
    
    private void LoadCarIni(CarData car, string dataPath)
    {
        var path = Path.Combine(dataPath, "car.ini");
        if (!File.Exists(path)) return;
        
        var ini = IniParser.Load(path);
        
        car.BasicInfo = new CarBasicInfo
        {
            Version = ini.GetInt("HEADER", "VERSION"),
            ScreenName = ini.GetString("INFO", "SCREEN_NAME") ?? car.FolderName,
            Brand = ini.GetString("INFO", "BRAND") ?? "",
            TotalMass = ini.GetDouble("BASIC", "TOTALMASS"),
            Inertia = ini.GetDoubleArray("BASIC", "INERTIA", 3),
            GraphicsOffset = ini.GetDoubleArray("BASIC", "GRAPHICS_OFFSET", 3),
            SteerLock = ini.GetDouble("CONTROLS", "STEER_LOCK", 450),
            SteerRatio = ini.GetDouble("CONTROLS", "STEER_RATIO", 15),
            FFMultiplier = ini.GetDouble("CONTROLS", "FFMULT", 1.0),
            FuelConsumption = ini.GetDouble("FUEL", "CONSUMPTION"),
            Fuel = ini.GetDouble("FUEL", "FUEL"),
            MaxFuel = ini.GetDouble("FUEL", "MAX_FUEL"),
            DriverEyes = ini.GetDoubleArray("GRAPHICS", "DRIVEREYES", 3)
        };
    }
    
    private void LoadEngineIni(CarData car, string dataPath)
    {
        var path = Path.Combine(dataPath, "engine.ini");
        if (!File.Exists(path)) return;
        
        var ini = IniParser.Load(path);
        
        car.Engine = new EngineData
        {
            Version = ini.GetInt("HEADER", "VERSION"),
            PowerCurveFile = ini.GetString("HEADER", "POWER_CURVE") ?? "power.lut",
            CoastCurve = ini.GetString("HEADER", "COAST_CURVE") ?? "",
            AltitudeSensitivity = ini.GetDouble("ENGINE_DATA", "ALTITUDE_SENSITIVITY"),
            Inertia = ini.GetDouble("ENGINE_DATA", "INERTIA"),
            Limiter = ini.GetInt("ENGINE_DATA", "LIMITER", 7000),
            LimiterHz = ini.GetInt("ENGINE_DATA", "LIMITER_HZ", 80),
            MinimumRpm = ini.GetInt("ENGINE_DATA", "MINIMUM", 800),
            DefaultTurboAdjustment = ini.GetDouble("ENGINE_DATA", "DEFAULT_TURBO_ADJUSTMENT", 1.0),
            CoastRefRpm = ini.GetInt("COAST_REF", "RPM"),
            CoastRefTorque = ini.GetDouble("COAST_REF", "TORQUE"),
            CoastNonLinearity = ini.GetDouble("COAST_REF", "NON_LINEARITY")
        };
        
        // Load turbo sections (TURBO_0, TURBO_1, etc.)
        for (int i = 0; i < 10; i++)
        {
            var section = $"TURBO_{i}";
            if (!ini.HasSection(section)) break;
            
            car.Engine.Turbos.Add(new TurboData
            {
                Index = i,
                LagDown = ini.GetDouble(section, "LAG_DN"),
                LagUp = ini.GetDouble(section, "LAG_UP"),
                MaxBoost = ini.GetDouble(section, "MAX_BOOST"),
                Wastegate = ini.GetDouble(section, "WASTEGATE"),
                DisplayMaxBoost = ini.GetDouble(section, "DISPLAY_MAX_BOOST"),
                ReferenceRpm = ini.GetInt(section, "REFERENCE_RPM"),
                Gamma = ini.GetDouble(section, "GAMMA", 1.0),
                CockpitAdjustable = ini.GetBool(section, "COCKPIT_ADJUSTABLE")
            });
        }
    }
    
    private void LoadPowerLut(CarData car, string dataPath)
    {
        var fileName = car.Engine.PowerCurveFile;
        if (string.IsNullOrEmpty(fileName)) fileName = "power.lut";
        
        var path = Path.Combine(dataPath, fileName);
        if (!File.Exists(path)) return;
        
        var lut = LutParser.Load(path);
        car.PowerCurve = lut.ToPowerCurve();
    }
    
    private void LoadDrivetrainIni(CarData car, string dataPath)
    {
        var path = Path.Combine(dataPath, "drivetrain.ini");
        if (!File.Exists(path)) return;
        
        var ini = IniParser.Load(path);
        
        var driveTypeStr = ini.GetString("TRACTION", "TYPE") ?? "RWD";
        var driveLayout = driveTypeStr.ToUpper() switch
        {
            "FWD" => DriveLayout.FWD,
            "AWD" => DriveLayout.AWD,
            _ => DriveLayout.RWD
        };
        
        car.Drivetrain = new DrivetrainData
        {
            Version = ini.GetInt("HEADER", "VERSION"),
            DriveLayout = driveLayout,
            GearCount = ini.GetInt("GEARS", "COUNT"),
            ReverseRatio = ini.GetDouble("GEARS", "GEAR_R"),
            FinalDrive = ini.GetDouble("GEARS", "FINAL"),
            DiffPower = ini.GetDouble("DIFFERENTIAL", "POWER"),
            DiffCoast = ini.GetDouble("DIFFERENTIAL", "COAST"),
            DiffPreload = ini.GetDouble("DIFFERENTIAL", "PRELOAD"),
            ChangeUpTime = ini.GetInt("GEARBOX", "CHANGE_UP_TIME"),
            ChangeDownTime = ini.GetInt("GEARBOX", "CHANGE_DN_TIME"),
            AutoCutoffTime = ini.GetInt("GEARBOX", "AUTO_CUTOFF_TIME"),
            SupportsShifter = ini.GetBool("GEARBOX", "SUPPORTS_SHIFTER"),
            ValidShiftRpmWindow = ini.GetInt("GEARBOX", "VALID_SHIFT_RPM_WINDOW"),
            ControlsWindowGain = ini.GetDouble("GEARBOX", "CONTROLS_WINDOW_GAIN"),
            GearboxInertia = ini.GetDouble("GEARBOX", "INERTIA", 0.02),
            ClutchMaxTorque = ini.GetDouble("CLUTCH", "MAX_TORQUE"),
            AutoShiftUp = ini.GetInt("AUTO_SHIFTER", "UP"),
            AutoShiftDown = ini.GetInt("AUTO_SHIFTER", "DOWN"),
            SlipThreshold = ini.GetDouble("AUTO_SHIFTER", "SLIP_THRESHOLD")
        };
        
        // Load gear ratios
        for (int i = 1; i <= car.Drivetrain.GearCount; i++)
        {
            car.Drivetrain.GearRatios.Add(ini.GetDouble("GEARS", $"GEAR_{i}"));
        }
        
        // Load AWD configuration if applicable
        if (driveLayout == DriveLayout.AWD && ini.HasSection("AWD"))
        {
            car.Drivetrain.AwdConfig = new AwdConfig
            {
                FrontShare = ini.GetDouble("AWD", "FRONT_SHARE"),
                FrontDiffPower = ini.GetDouble("AWD", "FRONT_DIFF_POWER"),
                FrontDiffCoast = ini.GetDouble("AWD", "FRONT_DIFF_COAST"),
                FrontDiffPreload = ini.GetDouble("AWD", "FRONT_DIFF_PRELOAD"),
                CentreDiffPower = ini.GetDouble("AWD", "CENTRE_DIFF_POWER"),
                CentreDiffCoast = ini.GetDouble("AWD", "CENTRE_DIFF_COAST"),
                CentreDiffPreload = ini.GetDouble("AWD", "CENTRE_DIFF_PRELOAD"),
                RearDiffPower = ini.GetDouble("AWD", "REAR_DIFF_POWER"),
                RearDiffCoast = ini.GetDouble("AWD", "REAR_DIFF_COAST"),
                RearDiffPreload = ini.GetDouble("AWD", "REAR_DIFF_PRELOAD")
            };
        }
    }
    
    private void LoadTyresIni(CarData car, string dataPath)
    {
        var path = Path.Combine(dataPath, "tyres.ini");
        if (!File.Exists(path)) return;
        
        var ini = IniParser.Load(path);
        
        car.Tyres = new TyreSetData
        {
            Version = ini.GetInt("HEADER", "VERSION"),
            DefaultCompoundIndex = ini.GetInt("COMPOUND_DEFAULT", "INDEX")
        };
        
        // Load default compound (FRONT/REAR)
        if (ini.HasSection("FRONT") && ini.HasSection("REAR"))
        {
            var compound = new TyreCompound
            {
                Index = 0,
                Name = ini.GetString("FRONT", "NAME") ?? "Default",
                ShortName = ini.GetString("FRONT", "SHORT_NAME") ?? "DEF",
                Front = LoadTyreData(ini, "FRONT"),
                Rear = LoadTyreData(ini, "REAR")
            };
            
            if (ini.HasSection("THERMAL_FRONT"))
                compound.FrontThermal = LoadTyreThermal(ini, "THERMAL_FRONT");
            if (ini.HasSection("THERMAL_REAR"))
                compound.RearThermal = LoadTyreThermal(ini, "THERMAL_REAR");
            
            car.Tyres.Compounds.Add(compound);
        }
        
        // Load additional compounds (FRONT_1, REAR_1, etc.)
        for (int i = 1; i < 10; i++)
        {
            var frontSection = $"FRONT_{i}";
            var rearSection = $"REAR_{i}";
            
            if (!ini.HasSection(frontSection) || !ini.HasSection(rearSection))
                break;
            
            var compound = new TyreCompound
            {
                Index = i,
                Name = ini.GetString(frontSection, "NAME") ?? $"Compound {i}",
                ShortName = ini.GetString(frontSection, "SHORT_NAME") ?? $"C{i}",
                Front = LoadTyreData(ini, frontSection),
                Rear = LoadTyreData(ini, rearSection)
            };
            
            var thermalFront = $"THERMAL_FRONT_{i}";
            var thermalRear = $"THERMAL_REAR_{i}";
            
            if (ini.HasSection(thermalFront))
                compound.FrontThermal = LoadTyreThermal(ini, thermalFront);
            if (ini.HasSection(thermalRear))
                compound.RearThermal = LoadTyreThermal(ini, thermalRear);
            
            car.Tyres.Compounds.Add(compound);
        }
    }
    
    private TyreData LoadTyreData(IniParser ini, string section)
    {
        return new TyreData
        {
            Name = ini.GetString(section, "NAME") ?? "",
            ShortName = ini.GetString(section, "SHORT_NAME") ?? "",
            Width = ini.GetDouble(section, "WIDTH"),
            Radius = ini.GetDouble(section, "RADIUS"),
            RimRadius = ini.GetDouble(section, "RIM_RADIUS"),
            AngularInertia = ini.GetDouble(section, "ANGULAR_INERTIA"),
            Damp = ini.GetDouble(section, "DAMP"),
            Rate = ini.GetDouble(section, "RATE"),
            DY0 = ini.GetDouble(section, "DY0"),
            DY1 = ini.GetDouble(section, "DY1"),
            DX0 = ini.GetDouble(section, "DX0"),
            DX1 = ini.GetDouble(section, "DX1"),
            DXRef = ini.GetDouble(section, "DX_REF"),
            DYRef = ini.GetDouble(section, "DY_REF"),
            FZ0 = ini.GetDouble(section, "FZ0"),
            LSExpY = ini.GetDouble(section, "LS_EXPY"),
            LSExpX = ini.GetDouble(section, "LS_EXPX"),
            WearCurveFile = ini.GetString(section, "WEAR_CURVE") ?? "",
            SpeedSensitivity = ini.GetDouble(section, "SPEED_SENSITIVITY"),
            RelaxationLength = ini.GetDouble(section, "RELAXATION_LENGTH"),
            RollingResistance0 = ini.GetDouble(section, "ROLLING_RESISTANCE_0"),
            RollingResistance1 = ini.GetDouble(section, "ROLLING_RESISTANCE_1"),
            RollingResistanceSlip = ini.GetDouble(section, "ROLLING_RESISTANCE_SLIP"),
            Flex = ini.GetDouble(section, "FLEX"),
            FlexGain = ini.GetDouble(section, "FLEX_GAIN"),
            CamberGain = ini.GetDouble(section, "CAMBER_GAIN"),
            DCamber0 = ini.GetDouble(section, "DCAMBER_0"),
            DCamber1 = ini.GetDouble(section, "DCAMBER_1"),
            FrictionLimitAngle = ini.GetDouble(section, "FRICTION_LIMIT_ANGLE"),
            XMu = ini.GetDouble(section, "XMU"),
            PressureStatic = ini.GetDouble(section, "PRESSURE_STATIC"),
            PressureIdeal = ini.GetDouble(section, "PRESSURE_IDEAL"),
            PressureSpringGain = ini.GetDouble(section, "PRESSURE_SPRING_GAIN"),
            PressureFlexGain = ini.GetDouble(section, "PRESSURE_FLEX_GAIN"),
            PressureRRGain = ini.GetDouble(section, "PRESSURE_RR_GAIN"),
            PressureDGain = ini.GetDouble(section, "PRESSURE_D_GAIN"),
            FalloffLevel = ini.GetDouble(section, "FALLOFF_LEVEL"),
            FalloffSpeed = ini.GetDouble(section, "FALLOFF_SPEED"),
            CXMult = ini.GetDouble(section, "CX_MULT"),
            RadiusAngularK = ini.GetDouble(section, "RADIUS_ANGULAR_K"),
            BrakeDXMod = ini.GetDouble(section, "BRAKE_DX_MOD"),
            CombinedFactor = ini.GetDouble(section, "COMBINED_FACTOR")
        };
    }
    
    private TyreThermalData LoadTyreThermal(IniParser ini, string section)
    {
        return new TyreThermalData
        {
            SurfaceTransfer = ini.GetDouble(section, "SURFACE_TRANSFER"),
            PatchTransfer = ini.GetDouble(section, "PATCH_TRANSFER"),
            CoreTransfer = ini.GetDouble(section, "CORE_TRANSFER"),
            InternalCoreTransfer = ini.GetDouble(section, "INTERNAL_CORE_TRANSFER"),
            FrictionK = ini.GetDouble(section, "FRICTION_K"),
            RollingK = ini.GetDouble(section, "ROLLING_K"),
            PerformanceCurveFile = ini.GetString(section, "PERFORMANCE_CURVE") ?? "",
            GrainGamma = ini.GetDouble(section, "GRAIN_GAMMA"),
            GrainGain = ini.GetDouble(section, "GRAIN_GAIN"),
            BlisterGamma = ini.GetDouble(section, "BLISTER_GAMMA"),
            BlisterGain = ini.GetDouble(section, "BLISTER_GAIN"),
            CoolFactor = ini.GetDouble(section, "COOL_FACTOR"),
            SurfaceRollingK = ini.GetDouble(section, "SURFACE_ROLLING_K")
        };
    }
    
    private void LoadSuspensionsIni(CarData car, string dataPath)
    {
        var path = Path.Combine(dataPath, "suspensions.ini");
        if (!File.Exists(path)) return;
        
        var ini = IniParser.Load(path);
        
        car.Suspension = new SuspensionData
        {
            Version = ini.GetInt("HEADER", "VERSION"),
            Wheelbase = ini.GetDouble("BASIC", "WHEELBASE"),
            CgLocation = ini.GetDouble("BASIC", "CG_LOCATION") * 100, // Convert to percentage
            FrontArb = ini.GetDouble("ARB", "FRONT"),
            RearArb = ini.GetDouble("ARB", "REAR"),
            Front = LoadSuspensionAxle(ini, "FRONT"),
            Rear = LoadSuspensionAxle(ini, "REAR")
        };
    }
    
    private SuspensionAxle LoadSuspensionAxle(IniParser ini, string section)
    {
        var typeStr = ini.GetString(section, "TYPE") ?? "DWB";
        var type = typeStr.ToUpper() switch
        {
            "STRUT" => SuspensionType.STRUT,
            "AXLE" => SuspensionType.AXLE,
            "ML" => SuspensionType.ML,
            _ => SuspensionType.DWB
        };
        
        return new SuspensionAxle
        {
            Type = type,
            BaseY = ini.GetDouble(section, "BASEY"),
            Track = ini.GetDouble(section, "TRACK"),
            RodLength = ini.GetDouble(section, "ROD_LENGTH"),
            HubMass = ini.GetDouble(section, "HUB_MASS"),
            RimOffset = ini.GetDouble(section, "RIM_OFFSET"),
            ToeOut = ini.GetDouble(section, "TOE_OUT"),
            StaticCamber = ini.GetDouble(section, "STATIC_CAMBER"),
            SpringRate = ini.GetDouble(section, "SPRING_RATE"),
            ProgressiveSpringRate = ini.GetDouble(section, "PROGRESSIVE_SPRING_RATE"),
            BumpStopRate = ini.GetDouble(section, "BUMP_STOP_RATE"),
            BumpstopUp = ini.GetDouble(section, "BUMPSTOP_UP"),
            BumpstopDown = ini.GetDouble(section, "BUMPSTOP_DN"),
            PackerRange = ini.GetDouble(section, "PACKER_RANGE"),
            DampBump = ini.GetDouble(section, "DAMP_BUMP"),
            DampFastBump = ini.GetDouble(section, "DAMP_FAST_BUMP"),
            DampFastBumpThreshold = ini.GetDouble(section, "DAMP_FAST_BUMPTHRESHOLD"),
            DampRebound = ini.GetDouble(section, "DAMP_REBOUND"),
            DampFastRebound = ini.GetDouble(section, "DAMP_FAST_REBOUND"),
            DampFastReboundThreshold = ini.GetDouble(section, "DAMP_FAST_REBOUNDTHRESHOLD"),
            WbCarTopFront = ini.GetDoubleArray(section, "WBCAR_TOP_FRONT", 3),
            WbCarTopRear = ini.GetDoubleArray(section, "WBCAR_TOP_REAR", 3),
            WbCarBottomFront = ini.GetDoubleArray(section, "WBCAR_BOTTOM_FRONT", 3),
            WbCarBottomRear = ini.GetDoubleArray(section, "WBCAR_BOTTOM_REAR", 3),
            WbTyreTop = ini.GetDoubleArray(section, "WBTYRE_TOP", 3),
            WbTyreBottom = ini.GetDoubleArray(section, "WBTYRE_BOTTOM", 3),
            WbCarSteer = ini.GetDoubleArray(section, "WBCAR_STEER", 3),
            WbTyreSteer = ini.GetDoubleArray(section, "WBTYRE_STEER", 3),
            StrutCar = ini.GetDoubleArray(section, "STRUT_CAR", 3),
            StrutTyre = ini.GetDoubleArray(section, "STRUT_TYRE", 3)
        };
    }
    
    private void LoadBrakesIni(CarData car, string dataPath)
    {
        var path = Path.Combine(dataPath, "brakes.ini");
        if (!File.Exists(path)) return;
        
        var ini = IniParser.Load(path);
        
        car.Brakes = new BrakeData
        {
            Version = ini.GetInt("HEADER", "VERSION"),
            MaxTorque = ini.GetDouble("DATA", "MAX_TORQUE"),
            FrontShare = ini.GetDouble("DATA", "FRONT_SHARE"),
            HandbrakeTorque = ini.GetDouble("DATA", "HANDBRAKE_TORQUE"),
            CockpitAdjustable = ini.GetBool("DATA", "COCKPIT_ADJUSTABLE"),
            AdjustStep = ini.GetDouble("DATA", "ADJUST_STEP"),
            DiscLF = ini.GetString("DISCS_GRAPHICS", "DISC_LF") ?? "",
            DiscRF = ini.GetString("DISCS_GRAPHICS", "DISC_RF") ?? "",
            DiscLR = ini.GetString("DISCS_GRAPHICS", "DISC_LR") ?? "",
            DiscRR = ini.GetString("DISCS_GRAPHICS", "DISC_RR") ?? "",
            FrontMaxGlow = ini.GetDouble("DISCS_GRAPHICS", "FRONT_MAX_GLOW"),
            RearMaxGlow = ini.GetDouble("DISCS_GRAPHICS", "REAR_MAX_GLOW"),
            LagHot = ini.GetDouble("DISCS_GRAPHICS", "LAG_HOT"),
            LagCool = ini.GetDouble("DISCS_GRAPHICS", "LAG_COOL")
        };
    }
    
    private void LoadAeroIni(CarData car, string dataPath)
    {
        var path = Path.Combine(dataPath, "aero.ini");
        if (!File.Exists(path)) return;
        
        var ini = IniParser.Load(path);
        
        car.Aero = new AeroData
        {
            Version = ini.GetInt("HEADER", "VERSION")
        };
        
        // Load all wing sections
        for (int i = 0; i < 20; i++)
        {
            var section = $"WING_{i}";
            if (!ini.HasSection(section)) break;
            
            var wing = new WingData
            {
                Index = i,
                Name = ini.GetString(section, "NAME") ?? $"WING_{i}",
                Chord = ini.GetDouble(section, "CHORD"),
                Span = ini.GetDouble(section, "SPAN"),
                Position = ini.GetDoubleArray(section, "POSITION", 3),
                LutAoaCL = ini.GetString(section, "LUT_AOA_CL") ?? "",
                LutGhCL = ini.GetString(section, "LUT_GH_CL") ?? "",
                LutAoaCD = ini.GetString(section, "LUT_AOA_CD") ?? "",
                LutGhCD = ini.GetString(section, "LUT_GH_CD") ?? "",
                CLGain = ini.GetDouble(section, "CL_GAIN"),
                CDGain = ini.GetDouble(section, "CD_GAIN"),
                Angle = ini.GetDouble(section, "ANGLE"),
                YawCLGain = ini.GetDouble(section, "YAW_CL_GAIN"),
                ZoneFrontCL = ini.GetDouble(section, "ZONE_FRONT_CL"),
                ZoneFrontCD = ini.GetDouble(section, "ZONE_FRONT_CD"),
                ZoneRearCL = ini.GetDouble(section, "ZONE_REAR_CL"),
                ZoneRearCD = ini.GetDouble(section, "ZONE_REAR_CD"),
                ZoneLeftCL = ini.GetDouble(section, "ZONE_LEFT_CL"),
                ZoneLeftCD = ini.GetDouble(section, "ZONE_LEFT_CD"),
                ZoneRightCL = ini.GetDouble(section, "ZONE_RIGHT_CL"),
                ZoneRightCD = ini.GetDouble(section, "ZONE_RIGHT_CD")
            };
            
            // Parse LUT files to get base coefficients at 0 degrees AOA
            wing.BaseCd = GetBaseCoefficientFromLut(dataPath, wing.LutAoaCD);
            wing.BaseCl = GetBaseCoefficientFromLut(dataPath, wing.LutAoaCL);
            
            car.Aero.Wings.Add(wing);
        }
    }
    
    /// <summary>
    /// Get the base coefficient from an aero LUT file (value at 0 degrees or first entry)
    /// </summary>
    private double GetBaseCoefficientFromLut(string dataPath, string lutFileName)
    {
        if (string.IsNullOrEmpty(lutFileName) || lutFileName.Equals("empty.lut", StringComparison.OrdinalIgnoreCase))
            return 0;
        
        var lutPath = Path.Combine(dataPath, lutFileName);
        if (!File.Exists(lutPath)) return 0;
        
        try
        {
            var lines = File.ReadAllLines(lutPath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;
                
                // Parse "key | value" or "key = value" format
                var parts = trimmed.Split(new[] { '|', '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    if (double.TryParse(parts[0].Trim(), out double key) &&
                        double.TryParse(parts[1].Trim(), out double value))
                    {
                        // Return value at key=0 (straight ahead), or first entry if no 0 key
                        if (Math.Abs(key) < 0.001)
                            return value;
                    }
                }
            }
            
            // If no 0 key found, parse first valid entry
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;
                
                var parts = trimmed.Split(new[] { '|', '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && double.TryParse(parts[1].Trim(), out double value))
                {
                    return value;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        
        return 0;
    }
}
