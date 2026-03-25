using System.Text.Json.Serialization;
using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class VoteCreateDto
{
    public int? HexagonId { get; set; }

    [JsonPropertyName("lat")]
    public decimal? Latitude { get; set; }

    [JsonPropertyName("lon")]
    public decimal? Longitude { get; set; }

    public int? AgentId { get; set; }
    public int? ImageId { get; set; }
    public AnomalyType Kind { get; set; }
    public decimal? Confidence { get; set; }
    public int BoxX1 { get; set; }
    public int BoxY1 { get; set; }
    public int BoxX2 { get; set; }
    public int BoxY2 { get; set; }
}
