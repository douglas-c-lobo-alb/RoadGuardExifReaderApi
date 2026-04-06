
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Redis.OM.Modeling;

namespace ExifApi.Data.Entities;

[Document]
public class RoadVisualAnomaly
{
    [Key] [RedisIdField]
    public int Id { get; set; }
    [Indexed]
    public int HexagonId { get; set; }
    public Hexagon? Hexagon { get; set; }
    [Indexed]
    public int? ImageId { get; set; }
    public Image? Image { get; set; }
    [Indexed]
    public AnomalyType Kind { get; set; }
    [Indexed]
    public decimal Confidence { get; set; }
    public JsonDocument? Metadata { get; set; }
    [Indexed]
    public int BoxX1 { get; set; }
    [Indexed]
    public int BoxY1 { get; set; }
    [Indexed]
    public int BoxX2 { get; set; }
    [Indexed]
    public int BoxY2 { get; set; }
    [Indexed]
    public DateTime? ResolvedAt { get; set; }
    [Indexed]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    [Indexed]
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
}
