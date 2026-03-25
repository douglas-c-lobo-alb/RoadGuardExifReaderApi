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
    private readonly int _anomalyResolution;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<H3Service> _logger;

    public H3Service(ApplicationDbContext context, ILogger<H3Service> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _appResolution = configuration.GetValue<int>("H3:DefaultResolution", 15);
        _anomalyResolution = configuration.GetValue<int>("H3:AnomalyResolution", 13);
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

    public async Task<List<HexagonViewDto>> GetHexagonsByViewportAsync(
        double latMin,
        double latMax,
        double lonMin,
        double lonMax,
        ViewFilterType viewFilterType = ViewFilterType.Or,
        List<AnomalyType>? anomalies = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        var images = await _context.Images
            .Include(i => i.Hexagon)
            .Where(i => startDate == null || DateOnly.FromDateTime((DateTime)i.DateTaken!) >= startDate)
            .Where(i => endDate == null || DateOnly.FromDateTime((DateTime)i.DateTaken!) <= endDate)
            .Where(i => i.Hexagon != null
                && i.Latitude >= (decimal)latMin
                && i.Latitude <= (decimal)latMax
                && i.Longitude >= (decimal)lonMin
                && i.Longitude <= (decimal)lonMax)
            .ToListAsync();

        var parentIndices = images
            .Where(i => i.Hexagon != null)
            .Select(i => H3Net.H3ToString(
                H3Net.CellToParent(H3Net.StringToH3(i.Hexagon!.H3Index), _anomalyResolution)))
            .Distinct()
            .ToList();

        var anomalyHexagons = await _context.Hexagons
            .Include(h => h.Anomalies)
            .Include(h => h.Turbulences)
            .Where(h => parentIndices.Contains(h.H3Index))
            .ToListAsync();

        var anomalyHexSet = anomalyHexagons.Select(h => h.H3Index).ToHashSet();

        if (anomalies is not null && anomalies.Any())
            anomalyHexagons = viewFilterType switch
            {
                ViewFilterType.Or => anomalyHexagons.Where(h => h.Anomalies.Any(a => anomalies.Contains(a.Kind))).ToList(),
                ViewFilterType.And => anomalyHexagons.Where(h => anomalies.All(type => h.Anomalies.Any(a => a.Kind == type))).ToList(),
                ViewFilterType.Not => anomalyHexagons.Where(h => !h.Anomalies.Any(a => anomalies.Contains(a.Kind))).ToList(),
                _ => anomalyHexagons
            };

        _logger.LogDebug("viewFilterType is {ViewFilterType}", viewFilterType);

        // When filtering by anomaly, only return parent hexes that passed the filter.
        // For Not, also include parent hexes with no anomaly entries in the DB at all.
        IEnumerable<string> outputIndices;
        if (anomalies is not null && anomalies.Any())
        {
            var filtered = anomalyHexagons.Select(h => h.H3Index).ToHashSet();
            if (viewFilterType == ViewFilterType.Not)
            {
                var noAnomalyParents = parentIndices.Where(p => !anomalyHexSet.Contains(p));
                outputIndices = filtered.Union(noAnomalyParents);
            }
            else
            {
                outputIndices = filtered;
            }
        }
        else
        {
            outputIndices = parentIndices;
        }

        var outputParentSet = outputIndices.ToHashSet();
        var anomalyHexByIndex = anomalyHexagons.ToDictionary(h => h.H3Index);

        var imageWithParent = images
            .Where(i => i.Hexagon != null)
            .Select(i => (
                Image: i,
                ParentIdx: H3Net.H3ToString(H3Net.CellToParent(H3Net.StringToH3(i.Hexagon!.H3Index), _anomalyResolution))
            ))
            .Where(x => outputParentSet.Contains(x.ParentIdx))
            .ToList();

        return imageWithParent
            .GroupBy(x => x.Image.Hexagon!.H3Index)
            .Select(group =>
            {
                var parentIdx = group.First().ParentIdx;
                anomalyHexByIndex.TryGetValue(parentIdx, out var hex);
                return new HexagonViewDto
                {
                    H3Index = group.Key,
                    Resolution = _appResolution,
                    Images = group.Select(x => new ImageViewDto
                    {
                        Id = x.Image.Id,
                        FilePath = x.Image.FilePath,
                        DateTaken = x.Image.DateTaken,
                    }).ToList(),
                    Anomalies = hex?.Anomalies.Select(a => new RoadVisualAnomalyViewDto
                    {
                        Id = a.Id, Kind = a.Kind, Confidence = a.Confidence,
                        BoxX1 = a.BoxX1, BoxY1 = a.BoxY1, BoxX2 = a.BoxX2, BoxY2 = a.BoxY2,
                        ResolvedAt = a.ResolvedAt
                    }).ToList() ?? [],
                    Turbulences = hex?.Turbulences.Select(t => new RoadTurbulenceViewDto
                    {
                        Id = t.Id, Index = t.Index, Kind = t.Kind, CreatedDate = t.CreatedDate
                    }).ToList() ?? []
                };
            }).ToList();
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
                AnomalyCount = _context.RoadVisualAnomalies.Count(a => a.HexagonId == h.Id)
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
        if (hexagon is null) return null;
        return ToDtoFromEntity(hexagon);
    }

    public async Task<HexagonDto?> CreateHexagonAsync(HexagonCreateDto dto)
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
            var h3Raw = H3Net.LatLngToCell((double)dto.Latitude.Value, (double)dto.Longitude.Value, dto.Resolution.Value);
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

    public async Task<HexagonDto?> UpdateHexagonAsync(int id, HexagonUpdateDto dto)
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
        else if (!dto.ImageId.HasValue)
        {
            _logger.LogWarning("UpdateHexagon: must provide H3Index, Latitude/Longitude/Resolution, or ImageId");
            return null;
        }

        if (dto.ImageId.HasValue)
        {
            var oldImageTask = _context.Images.FirstOrDefaultAsync(i => i.HexagonId == hexagon.Id);
            var newImageTask = _context.Images.FindAsync(dto.ImageId.Value).AsTask();
            await Task.WhenAll(oldImageTask, newImageTask);

            var newImage = newImageTask.Result;
            if (newImage is null)
            {
                _logger.LogWarning("UpdateHexagon: image {Id} not found", dto.ImageId);
                return null;
            }

            if (newImage.HexagonId != hexagon.Id)
            {
                var oldImage = oldImageTask.Result;
                if (oldImage is not null && oldImage.Id != dto.ImageId.Value)
                    oldImage.HexagonId = null;
                newImage.HexagonId = hexagon.Id;
            }
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

