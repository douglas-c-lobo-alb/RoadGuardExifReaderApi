using MetadataExtractor;

namespace ExifApi.Dtos;

public class ImageInfoDto
{
    public string? FileName { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public string? DateTaken { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Altitude { get; set; }
}