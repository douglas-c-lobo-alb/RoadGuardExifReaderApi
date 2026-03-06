using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class ImageDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public DateTime? DateTaken { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public double? Altitude { get; set; }
    public AnomalyData Anomaly { get; set; } = new();
    public HexagonDto? Hexagon { get; set; }
}
