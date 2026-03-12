using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExifApi.Data.Entities;

public class Image
{
    private static IConfiguration? _configuration;

    public static void SetConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    [NotMapped]
    public string FilePath => $"/{_configuration?.GetSection("Image:Path").Value ?? "images"}/{FileName}";
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
    public string? Notes { get; set; }
    public int? HexagonId { get; set; }
    public Hexagon? Hexagon { get; set; }
    public ICollection<RoadVisualAnomaly> Anomalies { get; set; } = [];
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
}
