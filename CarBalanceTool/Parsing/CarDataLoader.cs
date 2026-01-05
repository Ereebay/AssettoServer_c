using CarBalanceTool.Models;

namespace CarBalanceTool.Parsing;

/// <summary>
/// Loads complete car data from a car's data folder
/// </summary>
public class CarDataLoader
{
    /// <summary>
    /// Load car data from a folder containing the data folder (or the data folder itself)
    /// </summary>
    public static CarData? Load(string path)
    {
        // Determine actual data folder path
        var dataPath = path;
        if (Directory.Exists(Path.Combine(path, "data")))
            dataPath = Path.Combine(path, "data");

        if (!Directory.Exists(dataPath))
            return null;

        var carIni = IniParser.TryParse(Path.Combine(dataPath, "car.ini"));
        var engineIni = IniParser.TryParse(Path.Combine(dataPath, "engine.ini"));
        var drivetrainIni = IniParser.TryParse(Path.Combine(dataPath, "drivetrain.ini"));
        var aeroIni = IniParser.TryParse(Path.Combine(dataPath, "aero.ini"));
        var brakesIni = IniParser.TryParse(Path.Combine(dataPath, "brakes.ini"));
        var tyresIni = IniParser.TryParse(Path.Combine(dataPath, "tyres.ini"));

        if (carIni == null || engineIni == null || drivetrainIni == null)
            return null;

        // Load power curve
        var powerCurvePath = Path.Combine(dataPath, "power.lut");
        var powerLut = LutParser.TryParse(powerCurvePath);

        var folderName = new DirectoryInfo(path).Name;

        // Find preview image from skins folder
        var previewImagePath = FindPreviewImage(path);

        return new CarData
        {
            Name = carIni.GetValue("INFO", "SCREEN_NAME") ?? carIni.GetValue("INFO", "NAME") ?? folderName,
            FolderPath = path,
            Basic = LoadBasicData(carIni),
            Engine = LoadEngineData(engineIni, powerLut),
            Drivetrain = LoadDrivetrainData(drivetrainIni),
            Aero = LoadAeroData(aeroIni),
            Brakes = LoadBrakeData(brakesIni),
            Tyres = LoadTyreData(tyresIni),
            PreviewImagePath = previewImagePath
        };
    }

    /// <summary>
    /// Find a preview image from the car's skins folder
    /// </summary>
    private static string? FindPreviewImage(string carPath)
    {
        var skinsPath = Path.Combine(carPath, "skins");
        if (!Directory.Exists(skinsPath))
            return null;

        // Search through skin folders for preview images
        foreach (var skinDir in Directory.GetDirectories(skinsPath))
        {
            // Common preview image names
            var previewNames = new[] { "preview.jpg", "preview.png", "Preview.jpg", "Preview.png" };

            foreach (var previewName in previewNames)
            {
                var previewPath = Path.Combine(skinDir, previewName);
                if (File.Exists(previewPath))
                    return previewPath;
            }

            // Also check for any jpg/png if no standard preview found
            var jpgFiles = Directory.GetFiles(skinDir, "*.jpg");
            if (jpgFiles.Length > 0)
                return jpgFiles[0];

            var pngFiles = Directory.GetFiles(skinDir, "*.png");
            if (pngFiles.Length > 0)
                return pngFiles[0];
        }

        return null;
    }

    private static CarBasicData LoadBasicData(IniParser ini)
    {
        return new CarBasicData
        {
            ScreenName = ini.GetValue("INFO", "SCREEN_NAME") ?? ini.GetValue("INFO", "NAME") ?? "Unknown",
            TotalMass = ini.GetFloat("BASIC", "TOTALMASS"),
            SteerLock = ini.GetFloat("CONTROLS", "STEER_LOCK"),
            SteerRatio = ini.GetFloat("CONTROLS", "STEER_RATIO"),
            FuelConsumption = ini.GetFloat("FUEL", "CONSUMPTION"),
            MaxFuel = ini.GetFloat("FUEL", "MAX_FUEL", 50)
        };
    }

    private static EngineData LoadEngineData(IniParser ini, LutParser? powerLut)
    {
        var engine = new EngineData
        {
            Limiter = ini.GetInt("ENGINE_DATA", "LIMITER"),
            Minimum = ini.GetInt("ENGINE_DATA", "MINIMUM"),
            Inertia = ini.GetFloat("ENGINE_DATA", "INERTIA"),
            PowerCurve = powerLut?.ToPowerCurve() ?? []
        };

        // Load turbo data
        foreach (var section in ini.GetSectionsMatching("TURBO_"))
        {
            engine.Turbos.Add(new TurboData
            {
                MaxBoost = ini.GetFloat(section, "MAX_BOOST"),
                Wastegate = ini.GetFloat(section, "WASTEGATE"),
                ReferenceRpm = ini.GetInt(section, "REFERENCE_RPM"),
                Gamma = ini.GetFloat(section, "GAMMA"),
                LagUp = ini.GetFloat(section, "LAG_UP"),
                LagDown = ini.GetFloat(section, "LAG_DN")
            });
        }

        return engine;
    }

    private static DrivetrainData LoadDrivetrainData(IniParser ini)
    {
        var typeStr = ini.GetValue("TRACTION", "TYPE", "RWD").ToUpperInvariant();
        var type = typeStr switch
        {
            "FWD" => DrivetrainType.FWD,
            "AWD" => DrivetrainType.AWD,
            _ => DrivetrainType.RWD
        };

        var gearCount = ini.GetInt("GEARS", "COUNT");
        var gearRatios = new List<float>();

        for (int i = 1; i <= gearCount; i++)
        {
            gearRatios.Add(ini.GetFloat("GEARS", $"GEAR_{i}"));
        }

        var drivetrain = new DrivetrainData
        {
            Type = type,
            GearCount = gearCount,
            FinalDrive = ini.GetFloat("GEARS", "FINAL"),
            GearRatios = gearRatios,
            ReverseGear = ini.GetFloat("GEARS", "GEAR_R"),
            Differential = new DifferentialData
            {
                PowerLock = ini.GetFloat("DIFFERENTIAL", "POWER"),
                CoastLock = ini.GetFloat("DIFFERENTIAL", "COAST"),
                Preload = ini.GetFloat("DIFFERENTIAL", "PRELOAD")
            },
            ShiftUpTime = ini.GetInt("GEARBOX", "CHANGE_UP_TIME"),
            ShiftDownTime = ini.GetInt("GEARBOX", "CHANGE_DN_TIME"),
            ClutchMaxTorque = ini.GetFloat("CLUTCH", "MAX_TORQUE")
        };

        // Load AWD data if present
        if (type == DrivetrainType.AWD && ini.HasSection("AWD"))
        {
            drivetrain.Awd = new AwdData
            {
                FrontShare = ini.GetFloat("AWD", "FRONT_SHARE"),
                CentreDiffPower = ini.GetFloat("AWD", "CENTRE_DIFF_POWER"),
                CentreDiffCoast = ini.GetFloat("AWD", "CENTRE_DIFF_COAST"),
                FrontDiffPower = ini.GetFloat("AWD", "FRONT_DIFF_POWER"),
                FrontDiffCoast = ini.GetFloat("AWD", "FRONT_DIFF_COAST"),
                RearDiffPower = ini.GetFloat("AWD", "REAR_DIFF_POWER"),
                RearDiffCoast = ini.GetFloat("AWD", "REAR_DIFF_COAST")
            };
        }

        return drivetrain;
    }

    private static AeroData LoadAeroData(IniParser? ini)
    {
        var aero = new AeroData();

        if (ini == null)
            return aero;

        foreach (var section in ini.GetSectionsMatching("WING_"))
        {
            aero.Wings.Add(new WingData
            {
                Name = ini.GetValue(section, "NAME") ?? section,
                Chord = ini.GetFloat(section, "CHORD"),
                Span = ini.GetFloat(section, "SPAN"),
                ClGain = ini.GetFloat(section, "CL_GAIN"),
                CdGain = ini.GetFloat(section, "CD_GAIN"),
                Angle = ini.GetFloat(section, "ANGLE")
            });
        }

        return aero;
    }

    private static BrakeData LoadBrakeData(IniParser? ini)
    {
        if (ini == null)
            return new BrakeData();

        return new BrakeData
        {
            MaxTorque = ini.GetFloat("DATA", "MAX_TORQUE"),
            FrontShare = ini.GetFloat("DATA", "FRONT_SHARE"),
            HandbrakeTorque = ini.GetFloat("DATA", "HANDBRAKE_TORQUE")
        };
    }

    private static TyreData LoadTyreData(IniParser? ini)
    {
        if (ini == null)
            return new TyreData();

        return new TyreData
        {
            Front = new TyreCompound
            {
                Name = ini.GetValue("FRONT", "NAME") ?? "Unknown",
                Width = ini.GetFloat("FRONT", "WIDTH"),
                Radius = ini.GetFloat("FRONT", "RADIUS"),
                RimRadius = ini.GetFloat("FRONT", "RIM_RADIUS"),
                DY0 = ini.GetFloat("FRONT", "DY0"),
                DX0 = ini.GetFloat("FRONT", "DX0"),
                AngularInertia = ini.GetFloat("FRONT", "ANGULAR_INERTIA")
            },
            Rear = new TyreCompound
            {
                Name = ini.GetValue("REAR", "NAME") ?? "Unknown",
                Width = ini.GetFloat("REAR", "WIDTH"),
                Radius = ini.GetFloat("REAR", "RADIUS"),
                RimRadius = ini.GetFloat("REAR", "RIM_RADIUS"),
                DY0 = ini.GetFloat("REAR", "DY0"),
                DX0 = ini.GetFloat("REAR", "DX0"),
                AngularInertia = ini.GetFloat("REAR", "ANGULAR_INERTIA")
            }
        };
    }
}
