using ExifApi.Dtos;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace ExifApi.Services;

public class ExifService
{
    private readonly ILogger<ExifService> _logger;
    private readonly IWebHostEnvironment _env;
    public ExifService(ILogger<ExifService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }
    public IEnumerable<ImageInfoDto> GetAllImageMetadata()
    {
        var imageInfoList = new List<ImageInfoDto>();

        if (string.IsNullOrEmpty(_env.WebRootPath))
        {
            _logger.LogError("WebRootPath is not set yet.");
            return Enumerable.Empty<ImageInfoDto>();
        }

        var imagesPath = Path.Combine(_env.WebRootPath, "images");

        if (!System.IO.Directory.Exists(imagesPath))
        {
            _logger.LogWarning("Images folder not found at {Path}", imagesPath);
            return Enumerable.Empty<ImageInfoDto>();
        }

        var imagesFiles = System.IO.Directory.EnumerateFiles(imagesPath, "*.*").Where(IsJpeg);

        foreach (var file in imagesFiles)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(file);
                var fileName = Path.GetFileName(file);
                var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                var cameraMake = ifd0?.GetDescription(ExifDirectoryBase.TagMake);
                var cameraModel = ifd0?.GetDescription(ExifDirectoryBase.TagModel);
                var gps = directories.OfType<GpsDirectory>().FirstOrDefault();
                var geo = gps is not null && gps.TryGetGeoLocation(out var loc) ? loc : (GeoLocation?)null;
                var altitude = gps?.TryGetRational(GpsDirectory.TagAltitude, out var altRational) == true
                    ? (gps?.GetInt32(GpsDirectory.TagAltitudeRef) == 1 ? -altRational.ToDouble() : altRational.ToDouble())
                    : (double?)null;
                var dateTaken = subIfd?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal)
                    ?? ifd0?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal)
                    ?? ifd0?.GetDescription(ExifDirectoryBase.TagDateTime);

                var dto = new ImageInfoDto
                {
                    FileName = fileName,
                    FilePath = "/images/" + fileName,
                    CameraMake = cameraMake,
                    CameraModel = cameraModel,
                    DateTaken = dateTaken,
                    Latitude = geo?.Latitude,
                    Longitude = geo?.Longitude,
                    Altitude = altitude
                };
                imageInfoList.Add(dto);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error reading {File}", file);
            }
        }
        return imageInfoList.OrderBy(i => i.DateTaken);
    }
    private bool IsJpeg(string file)
    {
        var ext = Path.GetExtension(file);
        return ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }
}
