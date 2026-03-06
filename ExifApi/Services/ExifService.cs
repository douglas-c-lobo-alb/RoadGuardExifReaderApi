using ExifApi.Dtos;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace ExifApi.Services;

// the purpose of this service is to retrieve the images metadata, i.e. lat and long
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

        var imagesFiles = System.IO.Directory.EnumerateFiles(imagesPath, "*.*").Where(IsJpeg).ToList();
        _logger.LogInformation("Found {Count} JPEG files in {Path}", imagesFiles.Count, imagesPath);

        foreach (var file in imagesFiles)
        {
            var dto = GetImageMetadata(file);
            if (dto is not null) imageInfoList.Add(dto);
        }

        _logger.LogInformation("Successfully parsed {Parsed}/{Total} images", imageInfoList.Count, imagesFiles.Count);

        return imageInfoList.OrderBy(i => i.DateTaken);
    }

    public ImageInfoDto? ExtractMetadata(string filePath) => GetImageMetadata(filePath);

    public ImageInfoDto? GetImageMetadata(string file)
    {
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(file);
            var fileName = Path.GetFileName(file);
            _logger.LogDebug("Reading EXIF metadata from {FileName}", fileName);
            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var cameraMake = ifd0?.GetDescription(ExifDirectoryBase.TagMake);
            var cameraModel = ifd0?.GetDescription(ExifDirectoryBase.TagModel);
            var gps = directories.OfType<GpsDirectory>().FirstOrDefault();
            var geo = gps is not null && gps.TryGetGeoLocation(out var loc) ? loc : (GeoLocation?)null;

            if (gps is null)
                _logger.LogWarning("No GPS directory found in {FileName}", fileName);
            else if (!geo.HasValue)
                _logger.LogWarning("GPS directory present but no valid geo location in {FileName}", fileName);

            var altitude = gps?.TryGetRational(GpsDirectory.TagAltitude, out var altRational) == true
                ? (gps?.GetInt32(GpsDirectory.TagAltitudeRef) == 1 ? -altRational.ToDouble() : altRational.ToDouble())
                : (double?)null;
            var dateTaken = subIfd?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal)
                ?? ifd0?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal)
                ?? ifd0?.GetDescription(ExifDirectoryBase.TagDateTime);

            if (dateTaken is null)
                _logger.LogWarning("No date taken found in {FileName}", fileName);

            return new ImageInfoDto
            {
                FileName = fileName,
                FilePath = "/images/" + fileName,
                CameraMake = cameraMake,
                CameraModel = cameraModel,
                DateTaken = dateTaken,
                Latitude = geo.HasValue ? (decimal?)geo.Value.Latitude : null,
                Longitude = geo.HasValue ? (decimal?)geo.Value.Longitude : null,
                Altitude = altitude
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error reading {File}", file);
            return null;
        }
    }

    public ImageInfoDto? GetImageMetadataByFileName(string fileName)
    {
        if (string.IsNullOrEmpty(_env.WebRootPath))
        {
            _logger.LogError("WebRootPath is not set, cannot resolve {FileName}", fileName);
            return null;
        }
        var filePath = Path.Combine(_env.WebRootPath, "images", fileName);
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return null;
        }
        _logger.LogInformation("Fetching metadata for {FileName}", fileName);
        return GetImageMetadata(filePath);
    }

    private bool IsJpeg(string file)
    {
        var ext = Path.GetExtension(file);
        return ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }
}
