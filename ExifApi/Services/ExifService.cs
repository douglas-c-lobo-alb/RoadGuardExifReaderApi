using ExifApi.Dtos;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace ExifApi.Services;

public class ExifService
{
    public IEnumerable<ImageInfoDto> GetAllImageMetadata()
    {
        var imageInfoList = new List<ImageInfoDto>();

        var imagesPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Images");

        var imagesFiles = System.IO.Directory.GetFiles(imagesPath, "*.*")
            .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));

        foreach(var file in  imagesFiles)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(file);

                var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                var gps = directories.OfType<GpsDirectory>().FirstOrDefault();
                var geo = gps is not null && gps.TryGetGeoLocation(out var loc) ? loc : (GeoLocation?)null;
                var altitude = gps?.TryGetRational(GpsDirectory.TagAltitude, out var altRational) == true
                    ? (gps?.GetInt32(GpsDirectory.TagAltitudeRef) == 1 ? -altRational.ToDouble() : altRational.ToDouble())
                    : (double?)null;

                var dto = new ImageInfoDto
                {
                    FileName = Path.GetFileName(file),
                    CameraMake = ifd0?.GetDescription(ExifDirectoryBase.TagMake),
                    CameraModel = ifd0?.GetDescription(ExifDirectoryBase.TagModel),
                    DateTaken = subIfd?.GetDescription(ExifDirectoryBase.TagDateTime),
                    Latitude = geo?.Latitude,
                    Longitude = geo?.Longitude,
                    Altitude = altitude
                };
                imageInfoList.Add(dto);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error reading {file}: {e.Message}");
            }
        }

        return imageInfoList;
    }
}
