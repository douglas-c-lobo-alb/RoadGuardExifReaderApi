namespace ExifApi.Data.Entities;

[Flags]
public enum RoadTurbulenceType
{
    None = 0,
    Pothole = 1 << 0,
    Speedbump = 1 << 1,
    LongitudinalCrack = 1 << 2,
    TransverseCrack = 1 << 3,
    Depression = 1 << 4,
    AbruptSwerving = 1 << 5,
    WaterLeakage = 1 << 6,
    All = Pothole
        | Speedbump
        | LongitudinalCrack
        | TransverseCrack
        | Depression
        | AbruptSwerving
        | WaterLeakage
}
