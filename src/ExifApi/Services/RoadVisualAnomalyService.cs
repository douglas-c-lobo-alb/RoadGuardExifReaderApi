using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using ExifApi.Infrastructure.Caching;
using H3Standard;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Services;

public class RoadVisualAnomalyService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoadVisualAnomalyService> _logger;
    private readonly int _anomalyResolution;
    private readonly IViewportCacheInvalidator _cacheInvalidator;

    public RoadVisualAnomalyService(ApplicationDbContext context, ILogger<RoadVisualAnomalyService> logger, IConfiguration configuration, IViewportCacheInvalidator cacheInvalidator)
    {
        _context = context;
        _logger = logger;
        _anomalyResolution = configuration.GetValue<int>("H3:AnomalyResolution", 13);
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<List<RoadVisualAnomalyDto>> GetAllAsync()
    {
        var records = await _context.RoadVisualAnomalies
            .Include(r => r.Image)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
        return records.Select(ToDto).ToList();
    }

    public async Task<List<RoadVisualAnomalyDto>> GetAllByImageIdAsync(int imageId)
    {
        var records = await _context.RoadVisualAnomalies
            .Where(r => r.ImageId == imageId)
            .Include(r => r.Image)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
        return records.Select(ToDto).ToList();
    }

    public async Task<RoadVisualAnomalyDto?> GetByIdAsync(int id)
    {
        var record = await _context.RoadVisualAnomalies
            .Include(r => r.Image)
            .FirstOrDefaultAsync(r => r.Id == id);
        return record is null ? null : ToDto(record);
    }

    public async Task<RoadVisualAnomalyDto?> CreateAsync(RoadVisualAnomalyCreateDto dto)
    {
        int? hexagonId = dto.HexagonId;

        if (hexagonId is null && !string.IsNullOrWhiteSpace(dto.H3Index))
        {
            var hex = await _context.Hexagons.FirstOrDefaultAsync(h => h.H3Index == dto.H3Index)
                      ?? _context.Hexagons.Add(new Hexagon { H3Index = dto.H3Index }).Entity;
            await _context.SaveChangesAsync();
            hexagonId = hex.Id;
        }

        if (hexagonId is null && dto.ImageId.HasValue)
        {
            var image = await _context.Images.FirstOrDefaultAsync(i => i.Id == dto.ImageId.Value);
            if (image is not null)
            {
                if (image.HexagonId.HasValue)
                {
                    hexagonId = image.HexagonId;
                }
                else if (image.Latitude.HasValue && image.Longitude.HasValue)
                {
                    var h3Raw = H3Net.LatLngToCell((double)image.Latitude.Value, (double)image.Longitude.Value, _anomalyResolution);
                    if (h3Raw == 0) return null;
                    var h3Index = H3Net.H3ToString(h3Raw);
                    var hex = await _context.Hexagons.FirstOrDefaultAsync(h => h.H3Index == h3Index);
                    if (hex is null)
                    {
                        hex = new Hexagon { H3Index = h3Index };
                        _context.Hexagons.Add(hex);
                        await _context.SaveChangesAsync();
                    }
                    hexagonId = hex.Id;
                }
            }
        }

        if (hexagonId is null && dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            var h3Raw = H3Standard.H3Net.LatLngToCell((double)dto.Latitude.Value, (double)dto.Longitude.Value, _anomalyResolution);
            if (h3Raw == 0) return null;
            var h3Index = H3Standard.H3Net.H3ToString(h3Raw);
            var hex = await _context.Hexagons.FirstOrDefaultAsync(h => h.H3Index == h3Index);
            if (hex is null)
            {
                hex = new Hexagon { H3Index = h3Index };
                _context.Hexagons.Add(hex);
                await _context.SaveChangesAsync();
            }
            hexagonId = hex.Id;
        }

        if (hexagonId is null) return null;

        hexagonId = await PromoteToAnomalyResolutionAsync(hexagonId.Value);

        var entity = new RoadVisualAnomaly
        {
            HexagonId = hexagonId.Value,
            ImageId = dto.ImageId,
            Kind = dto.Kind,
            Confidence = dto.Confidence,
            Metadata = dto.Metadata,
            BoxX1 = dto.BoxX1,
            BoxY1 = dto.BoxY1,
            BoxX2 = dto.BoxX2,
            BoxY2 = dto.BoxY2,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow,
        };

        _context.RoadVisualAnomalies.Add(entity);
        await _context.SaveChangesAsync();
        _ = _cacheInvalidator.InvalidateAllAsync();
        _logger.LogInformation("Created road visual anomaly id={Id} for image id={ImageId}", entity.Id, dto.ImageId);

        await _context.Entry(entity).Reference(r => r.Image).LoadAsync(); // needed for ImageFileName in response
        return ToDto(entity);
    }

    public async Task<RoadVisualAnomalyDto?> UpdateAsync(int id, RoadVisualAnomalyUpdateDto dto)
    {
        var record = await _context.RoadVisualAnomalies
            .Include(r => r.Image)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (record is null) return null;

        record.Kind = dto.Kind;
        record.Confidence = dto.Confidence;
        record.Metadata = dto.Metadata;
        record.BoxX1 = dto.BoxX1;
        record.BoxY1 = dto.BoxY1;
        record.BoxX2 = dto.BoxX2;
        record.BoxY2 = dto.BoxY2;
        record.ResolvedAt = dto.ResolvedAt;
        record.LastModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _ = _cacheInvalidator.InvalidateAllAsync();
        _logger.LogInformation("Updated road visual anomaly id={Id}", id);
        return ToDto(record);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var record = await _context.RoadVisualAnomalies.FindAsync(id);
        if (record is null) return false;

        _context.RoadVisualAnomalies.Remove(record);
        await _context.SaveChangesAsync();
        _ = _cacheInvalidator.InvalidateAllAsync();
        _logger.LogInformation("Deleted road visual anomaly id={Id}", id);
        return true;
    }

    private async Task<int> PromoteToAnomalyResolutionAsync(int hexagonId)
    {
        var hex = await _context.Hexagons.FindAsync(hexagonId);
        if (hex is null) return hexagonId;

        var h3Raw = H3Net.StringToH3(hex.H3Index);
        if (H3Net.GetResolution(h3Raw) <= _anomalyResolution) return hexagonId;

        var parentRaw = H3Net.CellToParent(h3Raw, _anomalyResolution);
        if (parentRaw == 0) return hexagonId;

        var parentIndex = H3Net.H3ToString(parentRaw);
        var parentHex = await _context.Hexagons.FirstOrDefaultAsync(h => h.H3Index == parentIndex);
        if (parentHex is null)
        {
            parentHex = new Hexagon { H3Index = parentIndex };
            _context.Hexagons.Add(parentHex);
            await _context.SaveChangesAsync();
        }
        return parentHex.Id;
    }

    public static RoadVisualAnomalyDto ToDto(RoadVisualAnomaly r) => new()
    {
        Id = r.Id,
        HexagonId = r.HexagonId,
        ImageId = r.ImageId,
        ImageFileName = r.Image?.FileName,
        Kind = r.Kind,
        Confidence = r.Confidence,
        Metadata = r.Metadata,
        BoxX1 = r.BoxX1,
        BoxY1 = r.BoxY1,
        BoxX2 = r.BoxX2,
        BoxY2 = r.BoxY2,
        CreatedDate = r.CreatedDate,
        LastModifiedDate = r.LastModifiedDate,
        ResolvedAt = r.ResolvedAt
    };
}
