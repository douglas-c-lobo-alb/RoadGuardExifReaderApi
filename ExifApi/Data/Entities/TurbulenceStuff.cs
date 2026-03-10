namespace ExifApi.Data.Entities;

[Flags]
public enum TurbulenceType
{
    None = 0,
    Pothole = 1 << 0,
    Speedbump = 1 << 1,
    Crack = 1 << 2,
    WaterLeakage = 1 << 3,
    All = Pothole | Speedbump | WaterLeakage
}
