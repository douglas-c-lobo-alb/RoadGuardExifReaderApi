using System.Text.Json.Serialization;

namespace ExifApi.Data.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RoadTurbulenceType
{
    None = 0,
    Pothole = 1,
    Speedbump = 2,
    LongitudinalCrack = 3,
    TransverseCrack = 4,
    Depression = 5,
    AbruptSwerving = 6,
    WaterLeakage = 7,
}
