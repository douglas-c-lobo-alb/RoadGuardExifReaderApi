using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ExifApi.Data.Entities;

public class Vote
{
    [Key]
    public int Id { get; set; }
    public int HexagonId { get; set; }
    public Hexagon? Hexagon { get; set; }
    public int? SessionId { get; set; }
    public Session? Session { get; set; }
    public int? ImageId { get; set; }
    public Image? Image { get; set; }
    public AnomalyType Kind { get; set; }
    public decimal? Confidence { get; set; }
    public JsonDocument? Metadata { get; set; }
    public int BoxX1 { get; set; }
    public int BoxY1 { get; set; }
    public int BoxX2 { get; set; }
    public int BoxY2 { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
}
