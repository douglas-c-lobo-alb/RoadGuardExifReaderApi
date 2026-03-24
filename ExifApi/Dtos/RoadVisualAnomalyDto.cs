using System.Text.Json;
using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class RoadVisualAnomalyDto
{
    public int Id { get; set; }
    public int HexagonId { get; set; }
    public ImageDto? Image { get; set; }
    public AnomalyType Kind { get; set; }
    public decimal Confidence { get; set; }
    public JsonDocument? Metadata { get; set; }
    public int BoxX1 { get; set; }
    public int BoxY1 { get; set; }
    public int BoxX2 { get; set; }
    public int BoxY2 { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
}
