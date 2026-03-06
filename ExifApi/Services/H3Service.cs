using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using H3Standard;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Services;

public class H3Service
{
    private const int AppResolution = 15;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<H3Service> _logger;

    public H3Service(ApplicationDbContext context, ILogger<H3Service> logger)
    {
        _context = context;
        _logger = logger;
    }

    public HexagonDto? LatLngToCell(double lat, double lon, int resolution)
    {
        _logger.LogInformation("H3 conversion: lat={Lat}, lon={Lon}, res={Resolution}", lat, lon, resolution);
        var h3Raw = H3Net.LatLngToCell(lat, lon, resolution);
        if (h3Raw == 0)
        {
            _logger.LogWarning("H3 conversion returned 0 — invalid input?");
            return null;
        }
        return ToDto(h3Raw);
    }

    public HexagonDto? CellToParent(string h3IndexStr, int targetResolution)
    {
        var h3Raw = H3Net.StringToH3(h3IndexStr);
        if (h3Raw == 0) return null;
        var parent = H3Net.CellToParent(h3Raw, targetResolution);
        if (parent == 0) return null;
        return ToDto(parent);
    }

    public List<HexagonDto> CellToChildren(string h3IndexStr, int targetResolution)
    {
        var h3Raw = H3Net.StringToH3(h3IndexStr);
        if (h3Raw == 0) return [];
        return H3Net.CellToChildren(h3Raw, targetResolution)
            .Where(c => c != 0)
            .Select(ToDto)
            .ToList();
    }

    public List<HexagonDto> GridDisk(string h3IndexStr, int k)
    {
        var h3Raw = H3Net.StringToH3(h3IndexStr);
        if (h3Raw == 0) return [];
        return H3Net.GridDisk(h3Raw, k)
            .Where(c => c != 0)
            .Select(ToDto)
            .ToList();
    }

    public async Task GenerateHexagonsAsync()
    {
        var images = await _context.Images
            .Where(i => i.Hexagon == null && i.Latitude != null && i.Longitude != null)
            .ToListAsync();

        _logger.LogInformation("GenerateHexagons: {Count} images without hexagon", images.Count);

        foreach (var image in images)
        {
            var h3Raw = H3Net.LatLngToCell((double)image.Latitude!, (double)image.Longitude!, AppResolution);
            if (h3Raw == 0)
            {
                _logger.LogWarning("H3 conversion failed for image {Id}", image.Id);
                continue;
            }
            _context.Hexagons.Add(new Hexagon
            {
                H3Index = H3Net.H3ToString(h3Raw),
                ImageId = image.Id
            });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("GenerateHexagons: saved hexagons for {Count} images", images.Count);
    }

    private static HexagonDto ToDto(ulong h3Raw)
    {
        var center = H3Net.CellToLatLng(h3Raw);
        return new HexagonDto
        {
            H3Index = H3Net.H3ToString(h3Raw),
            Resolution = H3Net.GetResolution(h3Raw),
            Lat = center.LatWGS84,
            Lon = center.LngWGS84
        };
    }
}

