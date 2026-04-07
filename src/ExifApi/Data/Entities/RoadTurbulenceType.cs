using System.Text.Json.Serialization;

namespace ExifApi.Data.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RoadTurbulenceType
{
    None,
    Pothole,
    Speedbump,
    LongitudinalCrack,
    TransverseCrack,
    Depression,
    AbruptSwerving,
    WaterLeakage,
}
