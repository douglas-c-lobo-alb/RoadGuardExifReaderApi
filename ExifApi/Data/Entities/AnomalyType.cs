using System.Text.Json.Serialization;

namespace ExifApi.Data.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnomalyType
{
    None = 0,
    Pothole = 1,
    Crack = 2,
    MissingRoadSign = 3,
    WaterLeakage = 4,
    AnimalCorpse = 5,
}
