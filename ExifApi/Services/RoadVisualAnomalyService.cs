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

    public async Task<RoadVisualAnomalyDto?> CreateAsync(CreateRoadVisualAnomalyDto dto)
    {
        var imageExists = await _context.Images.AnyAsync(i => i.Id == dto.ImageId);
        if (!imageExists) return null;

        var entity = new RoadVisualAnomaly
        {
            ImageId = dto.ImageId,
            AnomalyType = dto.AnomalyType,
            Confidence = dto.Confidence,
            Notes = dto.Notes,
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

    public async Task<RoadVisualAnomalyDto?> UpdateAsync(int id, UpdateRoadVisualAnomalyDto dto)
    {
        var record = await _context.RoadVisualAnomalies
            .Include(r => r.Image)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (record is null) return null;

        record.AnomalyType = dto.AnomalyType;
        record.Confidence = dto.Confidence;
        record.Notes = dto.Notes;
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

    public async Task<RoadVisualAnomalyDto?> UpvoteAsync(int id)
    {
        var record = await _context.RoadVisualAnomalies
            .Include(r => r.Image)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (record is null) return null;

        if (!record.AlreadyVoted)
            record.AlreadyVoted = true;

        record.UpVote++;
        record.LastModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Upvoted road visual anomaly id={Id}", id);
        return ToDto(record);
    }

    public async Task<RoadVisualAnomalyDto?> DownvoteAsync(int id)
    {
        var record = await _context.RoadVisualAnomalies
            .Include(r => r.Image)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (record is null) return null;

        if (!record.AlreadyVoted)
            record.AlreadyVoted = true;

        record.DownVote++;
        record.LastModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Downvoted road visual anomaly id={Id}", id);
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
        AnomalyType = r.AnomalyType,
        Confidence = r.Confidence,
        Notes = r.Notes,
        AlreadyVoted = r.AlreadyVoted,
        UpVote = r.UpVote,
        DownVote = r.DownVote,
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
