using System.Text.Json;

namespace ExifApi.Dtos;

public class ImageUpdateDto
{
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public DateTime? DateTaken { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public double? Altitude { get; set; }
    public decimal? Heading { get; set; }
    public JsonDocument? Metadata { get; set; }
}
