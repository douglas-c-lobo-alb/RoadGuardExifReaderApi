using System.Text.Json.Serialization;

namespace ExifApi.Data.Entities;

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnomalyType
{
    None = 0,
    Pothole = 1 << 0,
    Crack = 1 << 1,
    MissingRoadSign = 1 << 2,
    WaterLeakage = 1 << 3,
    AnimalCorpse = 1 << 4,
    All = Pothole
        | Crack
        | MissingRoadSign
        | WaterLeakage
        | AnimalCorpse

}
