using System.Text.Json.Serialization;
using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using H3Standard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualBasic;
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
        if (!TryParseH3(h3IndexStr, out var h3Raw)) return null;
        var parent = H3Net.CellToParent(h3Raw, targetResolution);
        if (parent == 0) return null;
        return ToDto(parent);
    }

    public List<HexagonDto> CellToChildren(string h3IndexStr, int targetResolution)
    {
        if (!TryParseH3(h3IndexStr, out var h3Raw)) return [];
        return H3Net.CellToChildren(h3Raw, targetResolution)
            .Where(c => c != 0)
            .Select(ToDto)
            .ToList();
    }

    public List<HexagonDto> GridDisk(string h3IndexStr, int k)
    {
        if (!TryParseH3(h3IndexStr, out var h3Raw)) return [];
        return H3Net.GridDisk(h3Raw, k)
            .Where(c => c != 0)
            .Select(ToDto)
            .ToList();
    }

    public async Task GenerateHexagonsAsync()
    {
        var images = await _context.Images
            .Where(i => i.HexagonId == null && i.Latitude != null && i.Longitude != null)
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

            var h3Index = H3Net.H3ToString(h3Raw);
            var hexagon = await _context.Hexagons.FirstOrDefaultAsync(h => h.H3Index == h3Index);
            if (hexagon is null)
            {
                hexagon = new Hexagon { H3Index = h3Index };
                _context.Hexagons.Add(hexagon);
            }

            image.Hexagon = hexagon;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("GenerateHexagons: saved hexagons for {Count} images", images.Count);
    }

    public async Task<List<ViewHexagonDto>> GetHexagonsByViewportAsync(
        double latMin,
        double latMax,
        double lonMin,
        double lonMax,
        ViewFilterType viewFilterType = ViewFilterType.Or,
        List<AnomalyType>? anomalies = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        int resolution = 15)
    {
        // TODO: later try to implement this as a syntatic sugarred LINQ query
        var images = await _context.Images
            .Include(i => i.Hexagon)
            .Include(i => i.Anomalies)
            .Include(i => i.RoadTurbulence)
            .Where(i => startDate == null || DateOnly.FromDateTime((DateTime)i.DateTaken!) >= startDate)
            .Where(i => endDate == null || DateOnly.FromDateTime((DateTime)i.DateTaken!) <= endDate)
            .Where(i => i.Hexagon != null
                && i.Latitude >= (decimal)latMin
                && i.Latitude <= (decimal)latMax
                && i.Longitude >= (decimal)lonMin
                && i.Longitude <= (decimal)lonMax)
            .ToListAsync();

        if (anomalies is not null && anomalies.Any())
            images = viewFilterType switch
            {
                ViewFilterType.Or => images.Where(i => i.Anomalies.Any(a => anomalies.Contains(a.AnomalyType))).ToList(),
                ViewFilterType.And => images.Where(i => anomalies.All(type => i.Anomalies.Any(a => a.AnomalyType == type))).ToList(),
                ViewFilterType.Not => images.Where(i => !i.Anomalies.Any(a => anomalies.Contains(a.AnomalyType))).ToList(),
                _ => images
            };

        _logger.LogDebug("viewFilterType is {ViewFilterType}", viewFilterType);

        var hexagonIds = images
            .Where(i => i.HexagonId != null)
            .Select(i => i.HexagonId!.Value)
            .Distinct()
            .ToList();

        var turbulences = await _context.RoadTurbulences
            .Include(t => t.Hexagon)
            .Where(t => t.HexagonId != null && hexagonIds.Contains(t.HexagonId.Value))
            .ToListAsync();

        var turbulencesByHexId = turbulences
            .GroupBy(t => t.HexagonId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (resolution == 15)
        {
            return images
                .GroupBy(i => i.Hexagon!.H3Index)
                .Select(g =>
                {
                    var hexId = g.First().HexagonId!.Value;
                    var hexTurbulences = turbulencesByHexId.GetValueOrDefault(hexId, []);
                    return new ViewHexagonDto
                    {
                        H3Index = g.Key,
                        Resolution = resolution,
                        Images = g.Select(i => new ViewImageDto
                        {
                            Id = i.Id,
                            FilePath = i.FilePath,
                            DateTaken = i.DateTaken,
                            AnomalyNotes = i.Notes,
                            Turbulence = i.RoadTurbulence?.Index
                        }).ToList(),
                        RoadTurbulences = hexTurbulences.Select(t => new ViewTurbulenceDto
                        {
                            Id = t.Id,
                            Index = t.Index,
                            RoadTurbulenceType = t.RoadTurbulenceType,
                            DateCreated = t.DateCreated
                        }).ToList()
                    };
                }).ToList();
        }

        var turbulencesByParent = turbulences
            .Where(t => t.Hexagon != null)
            .GroupBy(t => H3Net.H3ToString(
                H3Net.CellToParent(H3Net.StringToH3(t.Hexagon!.H3Index), resolution)))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Roll up to the requested resolution, then group and deduplicate
        return images
            .Select(i =>
            {
                var raw = H3Net.StringToH3(i.Hexagon!.H3Index);
                var parent = H3Net.CellToParent(raw, resolution);
                return new { ParentIndex = H3Net.H3ToString(parent), Image = i };
            })
            .GroupBy(x => x.ParentIndex)
            .Select(g =>
            {
                var hexTurbulences = turbulencesByParent.GetValueOrDefault(g.Key, []);
                return new ViewHexagonDto
                {
                    H3Index = g.Key,
                    Resolution = resolution,
                    Images = g.Select(x => new ViewImageDto
                    {
                        Id = x.Image.Id,
                        FilePath = x.Image.FilePath,
                        DateTaken = x.Image.DateTaken,
                        AnomalyNotes = x.Image.Notes,
                        Turbulence = x.Image.RoadTurbulence?.Index
                    }).ToList(),
                    RoadTurbulences = hexTurbulences.Select(t => new ViewTurbulenceDto
                    {
                        Id = t.Id,
                        Index = t.Index,
                        RoadTurbulenceType = t.RoadTurbulenceType,
                        DateCreated = t.DateCreated
                    }).ToList()
                };
            })
            .ToList();
    }

    public async Task<List<ImageInfoDto>> GetHexagonImagesMetadata(string h3Index)
    {
        List<string>? hexagons = CellToChildren(h3Index, _appResolution)
            .Select(h => h.H3Index)
            .ToList();

        _logger.LogDebug("Hexagons: {Hexagons}", hexagons);

        IQueryable<Image>? images = _context.Images
            .Include(i => i.Hexagon)
            .Where(i => i.Hexagon != null
            && hexagons.Contains(i.Hexagon.H3Index));

        _logger.LogDebug("Images: {Images}", images);


        return await images.Select(i =>
            new ImageInfoDto
            {
                FileName = i.FileName,
                FilePath = i.FilePath,
                CameraMake = i.CameraMake,
                CameraModel = i.CameraModel,
                DateTaken = i.DateTaken,
                Altitude = i.Altitude,
                Latitude = i.Latitude,
                Longitude = i.Longitude
            }
        ).ToListAsync();
    }

    private static bool TryParseH3(string index, out ulong h3Raw)
    {
        try
        {
            h3Raw = H3Net.StringToH3(index);
            return h3Raw != 0;
        }
        catch (FormatException)
        {
            h3Raw = 0;
            return false;
        }
    }

    // CRUD

    public async Task<List<HexagonDto>> GetAllHexagonsAsync()
    {
        var hexagons = await _context.Hexagons
            .Select(h => new
            {
                Hexagon = h,
                ImageCount = _context.Images.Count(i => i.HexagonId == h.Id),
                AnomalyCount = _context.Images
                    .Where(i => i.HexagonId == h.Id)
                    .SelectMany(i => i.Anomalies)
                    .Count()
            })
            .ToListAsync();

        return hexagons.Select(x =>
        {
            var dto = ToDtoFromEntity(x.Hexagon);
            dto.ImageCount = x.ImageCount;
            dto.AnomalyCount = x.AnomalyCount;
            return dto;
        }).ToList();
    }

    public async Task<HexagonDto?> GetHexagonByIdAsync(int id)
    {
        var hexagon = await _context.Hexagons.FindAsync(id);
        return hexagon is null ? null : ToDtoFromEntity(hexagon);
    }

    public async Task<HexagonDto?> CreateHexagonAsync(CreateHexagonDto dto)
    {
        var image = await _context.Images
            .Include(i => i.Hexagon)
            .FirstOrDefaultAsync(i => i.Id == dto.ImageId);

        if (image is null)
        {
            _logger.LogWarning("CreateHexagon: image {Id} not found", dto.ImageId);
            return null;
        }

        if (image.Hexagon is not null)
        {
            _logger.LogWarning("CreateHexagon: image {Id} already has a hexagon", dto.ImageId);
            return null;
        }

        string h3IndexStr;

        if (!string.IsNullOrWhiteSpace(dto.H3Index))
        {
            if (!TryParseH3(dto.H3Index, out _))
            {
                _logger.LogWarning("CreateHexagon: invalid H3Index '{Index}'", dto.H3Index);
                return null;
            }
            h3IndexStr = dto.H3Index;
        }
        else if (dto.Latitude.HasValue && dto.Longitude.HasValue && dto.Resolution.HasValue)
        {
            var h3Raw = H3Net.LatLngToCell(dto.Latitude.Value, dto.Longitude.Value, dto.Resolution.Value);
            if (h3Raw == 0)
            {
                _logger.LogWarning("CreateHexagon: H3 conversion failed for lat={Lat}, lon={Lon}", dto.Latitude, dto.Longitude);
                return null;
            }
            h3IndexStr = H3Net.H3ToString(h3Raw);
        }
        else
        {
            _logger.LogWarning("CreateHexagon: must provide H3Index or Latitude/Longitude/Resolution");
            return null;
        }

        // Reuse existing hexagon with same H3Index (spatial deduplication)
        var hexagon = await _context.Hexagons.FirstOrDefaultAsync(h => h.H3Index == h3IndexStr);
        if (hexagon is null)
        {
            hexagon = new Hexagon { H3Index = h3IndexStr };
            _context.Hexagons.Add(hexagon);
            await _context.SaveChangesAsync();
        }

        image.HexagonId = hexagon.Id;
        await _context.SaveChangesAsync();
        return ToDtoFromEntity(hexagon);
    }

    public async Task<HexagonDto?> UpdateHexagonAsync(int id, UpdateHexagonDto dto)
    {
        var hexagon = await _context.Hexagons.FindAsync(id);
        if (hexagon is null)
        {
            _logger.LogWarning("UpdateHexagon: hexagon {Id} not found", id);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(dto.H3Index))
        {
            if (!TryParseH3(dto.H3Index, out _))
            {
                _logger.LogWarning("UpdateHexagon: invalid H3Index '{Index}'", dto.H3Index);
                return null;
            }
            hexagon.H3Index = dto.H3Index;
        }
        else if (dto.Latitude.HasValue && dto.Longitude.HasValue && dto.Resolution.HasValue)
        {
            var h3Raw = H3Net.LatLngToCell(dto.Latitude.Value, dto.Longitude.Value, dto.Resolution.Value);
            if (h3Raw == 0)
            {
                _logger.LogWarning("UpdateHexagon: H3 conversion failed for lat={Lat}, lon={Lon}", dto.Latitude, dto.Longitude);
                return null;
            }
            hexagon.H3Index = H3Net.H3ToString(h3Raw);
        }
        else
        {
            _logger.LogWarning("UpdateHexagon: must provide H3Index or Latitude/Longitude/Resolution");
            return null;
        }

        await _context.SaveChangesAsync();
        return ToDtoFromEntity(hexagon);
    }

    public async Task<bool> DeleteHexagonAsync(int id)
    {
        var hexagon = await _context.Hexagons.FindAsync(id);
        if (hexagon is null) return false;
        _context.Hexagons.Remove(hexagon);
        await _context.SaveChangesAsync();
        return true;
    }

    private static HexagonDto ToDtoFromEntity(Hexagon h)
    {
        var h3Raw = H3Net.StringToH3(h.H3Index);
        return new HexagonDto
        {
            Id = h.Id,
            H3Index = h.H3Index,
            Resolution = H3Net.GetResolution(h3Raw)
        };
    }

    private static HexagonDto ToDto(ulong h3Raw) => new()
    {
        H3Index = H3Net.H3ToString(h3Raw),
        Resolution = H3Net.GetResolution(h3Raw)
    };

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ViewFilterType
    {
        Or,
        And,
        Not
    }
}

