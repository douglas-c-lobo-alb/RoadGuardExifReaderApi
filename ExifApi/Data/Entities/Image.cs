using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Data.Entities;

public class Image
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;
    [Precision(10, 6)]
    public decimal? Latitude { get; set; }
    [Precision(10, 6)]
    public decimal? Longitude { get; set; }
    public double? Altitude { get; set; }
    [Precision(5, 2)]
    public decimal? Heading { get; set; }
    [MaxLength(255)]
    public string? CameraMake { get; set; }
    [MaxLength(255)]
    public string? CameraModel { get; set; }
    public DateTime? DateTaken { get; set; }
    public int? Turbulence { get; set; }
    public string? AnomalyNotes { get; set; }
    public int? HexagonId { get; set; }
    public Hexagon? Hexagon { get; set; }
    public ICollection<RoadVisualAnomaly> Anomalies { get; set; } = [];
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
}
