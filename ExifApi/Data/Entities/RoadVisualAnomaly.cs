

using System.ComponentModel.DataAnnotations;
using ExifApi.Data.Entities;

public class RoadVisualAnomaly {
    [Key]
    public int Id { get; set; }
    public int? HexagonId { get; set;}
    public Hexagon? Hexagon { get; set; }
    public AnomalyType AnomalyType { get; set; } = AnomalyType.None;
    public ((int StartingX, int StartingY), (int EndingX, int EndingY)) Rectangle { get; set;}
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}