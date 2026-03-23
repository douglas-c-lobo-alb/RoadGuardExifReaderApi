using System.Text.Json;

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
    public decimal? Heading { get; set; }
    public int? Turbulence { get; set; }
    public JsonDocument? AnomalyNotes { get; set; }
    public int AnomalyCount { get; set; }
    public int? AgentId { get; set; }
    public HexagonDto? Hexagon { get; set; }
}
