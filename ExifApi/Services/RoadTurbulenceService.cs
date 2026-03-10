using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Services;

public class RoadTurbulenceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoadTurbulenceService> _logger;

    public RoadTurbulenceService(ApplicationDbContext context, ILogger<RoadTurbulenceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RoadTurbulenceDto>> GetAllAsync()
    {
        var records = await _context.RoadTurbulences
            .Include(r => r.Hexagon)
            .OrderByDescending(r => r.DateCreated)
            .ToListAsync();
        return records.Select(ToDto).ToList();
    }

    public async Task<RoadTurbulenceDto?> GetByIdAsync(int id)
    {
        var record = await _context.RoadTurbulences
            .Include(r => r.Hexagon)
            .FirstOrDefaultAsync(r => r.Id == id);
        return record is null ? null : ToDto(record);
    }

    public async Task<List<RoadTurbulenceDto>> GetByH3IndexAsync(string h3Index)
    {
        var records = await _context.RoadTurbulences
            .Include(r => r.Hexagon)
            .Where(r => r.Hexagon != null && r.Hexagon.H3Index == h3Index)
            .OrderByDescending(r => r.DateCreated)
            .ToListAsync();
        return records.Select(ToDto).ToList();
    }

    /// <summary>
    /// Inserts one or more turbulence records atomically (single transaction).
    /// </summary>
    public async Task<List<RoadTurbulenceDto>> CreateAsync(IEnumerable<CreateRoadTurbulenceDto> dtos)
    {
        var entities = dtos.Select(dto => new RoadTurbulence
        {
            Index = dto.Index,
            RoadTurbulenceType = dto.RoadTurbulenceType,
            HexagonId = dto.HexagonId,
            DateCreated = DateTime.UtcNow
        }).ToList();

        _context.RoadTurbulences.AddRange(entities);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created {Count} road turbulence record(s)", entities.Count);

        // Reload with Hexagon navigation
        var ids = entities.Select(e => e.Id).ToHashSet();
        var reloaded = await _context.RoadTurbulences
            .Include(r => r.Hexagon)
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();

        return reloaded.Select(ToDto).ToList();
    }

    public async Task<RoadTurbulenceDto?> UpdateAsync(int id, CreateRoadTurbulenceDto dto)
    {
        var record = await _context.RoadTurbulences
            .Include(r => r.Hexagon)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (record is null) return null;

        record.Index = dto.Index;
        record.RoadTurbulenceType = dto.RoadTurbulenceType;
        record.HexagonId = dto.HexagonId;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated road turbulence id={Id}", id);

        // Reload to pick up any navigation changes
        await _context.Entry(record).Reference(r => r.Hexagon).LoadAsync();
        return ToDto(record);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var record = await _context.RoadTurbulences.FindAsync(id);
        if (record is null) return false;

        _context.RoadTurbulences.Remove(record);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted road turbulence id={Id}", id);
        return true;
    }

    private static RoadTurbulenceDto ToDto(RoadTurbulence r) => new()
    {
        Id = r.Id,
        Index = r.Index,
        RoadTurbulenceType = r.RoadTurbulenceType,
        HexagonId = r.HexagonId,
        DateCreated = r.DateCreated,
        Hexagon = r.Hexagon is null ? null : new HexagonDto
        {
            Id = r.Hexagon.Id,
            H3Index = r.Hexagon.H3Index,
            Resolution = r.Hexagon.Resolution
        }
    };
}
