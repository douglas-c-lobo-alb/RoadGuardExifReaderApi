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
    [MaxLength(255)]
    public string? CameraMake { get; set; }
    [MaxLength(255)]
    public string? CameraModel { get; set; }
    [MaxLength(255)]
    public string? DateTaken { get; set; }
    public string Anomaly { get; set; } = string.Empty;
    public string Metadata { get; set; } = "{}";
    public string? H3Cell { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Hexagon? Hexagon { get; set; }
}
