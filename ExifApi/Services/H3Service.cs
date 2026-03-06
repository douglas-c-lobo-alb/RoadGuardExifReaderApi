using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using H3Standard;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Services;

public class H3Service
{
    private readonly int _appResolution;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<H3Service> _logger;

    public H3Service(ApplicationDbContext context, ILogger<H3Service> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _appResolution = configuration.GetValue<int>("H3:DefaultResolution", 15);
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
            var h3Raw = H3Net.LatLngToCell((double)image.Latitude!, (double)image.Longitude!, _appResolution);
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

    public async Task<List<ViewHexagonDto>> GetHexagonsByViewportAsync(
        double latMin, double latMax, double lonMin, double lonMax, int resolution = 15)
    {
        // Load all hexagons within the viewport bounds, including their images
        var hexagons = await _context.Hexagons
            .Include(h => h.Image)
            .Where(h => h.Image != null
                && h.Image.Latitude >= (decimal)latMin
                && h.Image.Latitude <= (decimal)latMax
                && h.Image.Longitude >= (decimal)lonMin
                && h.Image.Longitude <= (decimal)lonMax)
            .ToListAsync();

        if (resolution == 15)
        {
            return hexagons.Select(h => new ViewHexagonDto
            {
                H3Index = h.H3Index,
                Resolution = resolution,
                Images = h.Image is { } img
                    ? [new ViewImageDto { Id = img.Id, FilePath = img.FilePath, DateTaken = img.DateTaken, AnomalyNotes = img.Anomaly.Notes }]
                    : []
            }).ToList();
        }

        // Roll up to the requested resolution, then group and deduplicate
        return hexagons
            .Select(h =>
            {
                var raw = H3Net.StringToH3(h.H3Index);
                var parent = H3Net.CellToParent(raw, resolution);
                return new
                {
                    ParentIndex = H3Net.H3ToString(parent),
                    Image = h.Image
                };
            })
            .GroupBy(x => x.ParentIndex)
            .Select(g => new ViewHexagonDto
            {
                H3Index = g.Key,
                Resolution = resolution,
                Images = g
                    .Where(x => x.Image != null)
                    .Select(x => new ViewImageDto
                    {
                        Id = x.Image!.Id,
                        FilePath = x.Image.FilePath,
                        DateTaken = x.Image.DateTaken,
                        AnomalyNotes = x.Image.Anomaly.Notes
                    }).ToList()
            })
            .ToList();
    }

    private static HexagonDto ToDto(ulong h3Raw) => new()
    {
        H3Index = H3Net.H3ToString(h3Raw),
        Resolution = H3Net.GetResolution(h3Raw)
    };
}

