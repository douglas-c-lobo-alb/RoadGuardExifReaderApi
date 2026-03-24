using System.Text.Json.Serialization;

namespace ExifApi.Data.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnomalyType
{
    Pothole,
    Crack,
    MissingRoadSign,
    WaterLeakage,
    AnimalCorpse,
}
