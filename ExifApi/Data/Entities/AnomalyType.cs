using System.Text.Json.Serialization;

namespace ExifApi.Data.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnomalyType
{
    None,
    Pothole,
    RoadCrack,
    MissingRoadSign,
    WaterLeakage,
    RoadObstruction,
    MissingCrosswalk,
    DeterioratedMarkings
}
