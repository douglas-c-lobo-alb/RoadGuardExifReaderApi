using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Services;

public class RoadVisualAnomalyService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoadVisualAnomalyService> _logger;

    public RoadVisualAnomalyService(ApplicationDbContext context, ILogger<RoadVisualAnomalyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RoadVisualAnomalyDto>> GetAllAsync()
    {
        var records = await _context.RoadVisualAnomalies
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

        if (hexagonId is null && dto.ImageId.HasValue)
        {
            var image = await _context.Images.FirstOrDefaultAsync(i => i.Id == dto.ImageId.Value);
            hexagonId = image?.HexagonId;
        }

        if (hexagonId is null && dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            var h3Raw = H3Standard.H3Net.LatLngToCell((double)dto.Latitude.Value, (double)dto.Longitude.Value, 13);
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
        _logger.LogInformation("Created road visual anomaly id={Id} for image id={ImageId}", entity.Id, dto.ImageId);

        await _context.Entry(entity).Reference(r => r.Image).LoadAsync();
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
        _logger.LogInformation("Updated road visual anomaly id={Id}", id);
        return ToDto(record);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var record = await _context.RoadVisualAnomalies.FindAsync(id);
        if (record is null) return false;

        _context.RoadVisualAnomalies.Remove(record);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted road visual anomaly id={Id}", id);
        return true;
    }

    private static RoadVisualAnomalyDto ToDto(RoadVisualAnomaly r) => new()
    {
        Id = r.Id,
        HexagonId = r.HexagonId,
        Kind = r.Kind,
        Confidence = r.Confidence,
        Metadata = r.Metadata,
        Image = r.Image is null ? null : new ImageDto
        {
            Id = r.Image.Id,
            FileName = r.Image.FileName,
            FilePath = r.Image.FilePath,
            CameraMake = r.Image.CameraMake,
            CameraModel = r.Image.CameraModel,
        },
        BoxX1 = r.BoxX1,
        BoxY1 = r.BoxY1,
        BoxX2 = r.BoxX2,
        BoxY2 = r.BoxY2,
        CreatedDate = r.CreatedDate,
        LastModifiedDate = r.LastModifiedDate,
        ResolvedAt = r.ResolvedAt
    };
}
