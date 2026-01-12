namespace ACDyno.Models;

/// <summary>
/// Unit conversion constants - all internal calculations use SI units
/// </summary>
public static class Units
{
    // Velocity conversions
    public const double MPS_TO_MPH = 2.23694;
    public const double MPS_TO_KMH = 3.6;
    public const double MPH_TO_MPS = 0.44704;
    public const double KMH_TO_MPS = 0.27778;
    
    // Distance conversions
    public const double M_TO_FT = 3.28084;
    public const double FT_TO_M = 0.3048;
    public const double M_TO_MI = 0.000621371;
    
    // Mass conversions
    public const double KG_TO_LB = 2.20462;
    public const double LB_TO_KG = 0.453592;
    public const double KG_TO_TON = 0.001;
    
    // Power conversions
    public const double W_TO_HP = 0.00134102;
    public const double HP_TO_W = 745.7;
    public const double W_TO_KW = 0.001;
    public const double HP_TO_KW = 0.7457;
    
    // Torque conversions
    public const double NM_TO_LBFT = 0.737562;
    public const double LBFT_TO_NM = 1.35582;
    
    // Area conversions
    public const double M2_TO_FT2 = 10.7639;
    
    // Physical constants
    public const double G = 9.81; // m/s² - gravitational acceleration
    public const double AIR_DENSITY_SEA = 1.225; // kg/m³ at sea level
    
    // Common reference speeds
    public const double V_60_MPH = 26.8224; // m/s (60 mph)
    public const double V_100_KMH = 27.7778; // m/s (100 km/h)
    public const double V_100_MPH = 44.704; // m/s (100 mph reference for aero)
}
