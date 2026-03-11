
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ExifApi.Data.Entities;

public class RoadVisualAnomaly
{
    [Key]
    public int Id { get; set; }
    public int ImageId { get; set; }
    public Image? Image { get; set; }
    public Anomalies AnomalyType { get; set; } = Anomalies.None;
    public decimal Confidence { get; set; }
    public JsonDocument? Notes { get; set; }
    public int BoxX1 { get; set; }
    public int BoxY1 { get; set; }
    public int BoxX2 { get; set; }
    public int BoxY2 { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
}
