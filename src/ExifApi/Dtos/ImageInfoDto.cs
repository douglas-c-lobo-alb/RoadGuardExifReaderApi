namespace ExifApi.Dtos;

public class ImageInfoDto
{
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public DateTime? DateTaken { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public double? Altitude { get; set; }
    public decimal? Heading { get; set; }
}
