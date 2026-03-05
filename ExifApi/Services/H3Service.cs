using ExifApi.Data;
using ExifApi.Dtos;
using H3Standard;

namespace ExifApi.Services;

public class H3Service
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<H3Service> _logger;
    private readonly IWebHostEnvironment _env;

    public H3Service(ApplicationDbContext context, ILogger<H3Service> logger, IWebHostEnvironment env)
    {
        _context = context;
        _logger = logger;
        _env = env;
    }

    public HexagonDto? LatLngToCell(double lat, double lng, int resolution)
    {
        _logger.LogInformation("H3 conversion requested: lat={Lat}, lng={Lng}, resolution={Resolution}", lat, lng, resolution);
        var h3Index = H3Net.LatLngToCell(lat, lng, resolution);
        if (h3Index == 0)
        {
            _logger.LogWarning("H3 conversion returned 0 for lat={Lat}, lng={Lng}, resolution={Resolution} — invalid input?", lat, lng, resolution);
            return null;
        }
        var h3String = H3Net.H3ToString(h3Index);
        _logger.LogInformation("H3 conversion result: {H3Index}", h3String);
        return new HexagonDto { Lat = lat, Lng = lng, Resolution = resolution, H3Index = h3String };
    }

    public void ConvertLatLngToH3Cell(IEnumerable<ImageInfoDto> imageInfoDtos)
    {
        _logger.LogWarning("ConvertLatLngToH3Cell called but not yet implemented");
    }
}
