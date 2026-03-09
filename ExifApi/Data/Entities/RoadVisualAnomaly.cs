
using System.ComponentModel.DataAnnotations;

namespace ExifApi.Data.Entities;

public class RoadVisualAnomaly
{
    [Key]
    public int Id { get; set; }
    public int ImageId { get; set; }
    public Image? Image { get; set; }
    public AnomalyType AnomalyType { get; set; } = AnomalyType.None;
    public decimal Confidence { get; set; }
    public string? Notes { get; set; }
    public int BoxX1 { get; set; }
    public int BoxY1 { get; set; }
    public int BoxX2 { get; set; }
    public int BoxY2 { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
}