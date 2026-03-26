using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using H3Standard;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Services;

public class RoadTurbulenceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoadTurbulenceService> _logger;
    private readonly H3Service _h3Service;

    public RoadTurbulenceService(ApplicationDbContext context, ILogger<RoadTurbulenceService> logger, H3Service h3Service)
    {
        _context = context;
        _logger = logger;
        _h3Service = h3Service;
    }

    public async Task<List<RoadTurbulenceDto>> GetAllAsync()
    {
        var records = await _context.RoadTurbulences
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
        return records.Select(ToDto).ToList();
    }

    public async Task<RoadTurbulenceDto?> GetByIdAsync(int id)
    {
        RoadTurbulence? record = await _context.RoadTurbulences
            .FirstOrDefaultAsync(r => r.Id == id);
        return record is null ? null : ToDto(record);
    }

    public async Task<List<RoadTurbulenceDto>> GetByH3IndexAsync(string h3Index)
    {
        List<RoadTurbulence>? records = await _context.RoadTurbulences
            .Where(t => t.Hexagon != null && t.Hexagon.H3Index == h3Index)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
        return records.Select(ToDto).ToList();
    }

    /// <summary>
    /// Inserts one or more turbulence records atomically (single transaction).
    /// Each DTO must supply either HexagonId or H3Index.
    /// </summary>
    public async Task<List<RoadTurbulenceDto>> CreateAsync(IEnumerable<RoadTurbulenceCreateDto> dtos)
    {
        var entities = new List<RoadTurbulence>();

        foreach (var dto in dtos)
        {
            int hexagonId;

            if (dto.HexagonId.HasValue)
            {
                hexagonId = dto.HexagonId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(dto.H3Index))
            {
                var hex = await _context.Hexagons.FirstOrDefaultAsync(h => h.H3Index == dto.H3Index)
                          ?? _context.Hexagons.Add(new Hexagon { H3Index = dto.H3Index }).Entity;
                await _context.SaveChangesAsync();
                hexagonId = hex.Id;
            }
            else
            {
                throw new ArgumentException("Each turbulence record must supply HexagonId or H3Index.");
            }

            entities.Add(new RoadTurbulence
            {
                Index = dto.Index,
                Kind = dto.Kind,
                HexagonId = hexagonId,
                AgentId = dto.AgentId,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            });
        }

        _context.RoadTurbulences.AddRange(entities);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created {Count} road turbulence record(s)", entities.Count);

        return entities.Select(ToDto).ToList();
    }

    public async Task<RoadTurbulenceDto?> UpdateAsync(int id, RoadTurbulenceCreateDto dto)
    {
        var record = await _context.RoadTurbulences
            .FirstOrDefaultAsync(r => r.Id == id);
        if (record is null) return null;

        record.Index = dto.Index;
        record.Kind = dto.Kind;
        if (dto.HexagonId.HasValue)
        {
            record.HexagonId = dto.HexagonId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(dto.H3Index))
        {
            var hex = await _context.Hexagons.FirstOrDefaultAsync(h => h.H3Index == dto.H3Index)
                      ?? _context.Hexagons.Add(new Hexagon { H3Index = dto.H3Index }).Entity;
            await _context.SaveChangesAsync();
            record.HexagonId = hex.Id;
        }
        record.AgentId = dto.AgentId;
        record.LastModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated road turbulence id={Id}", id);

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
        Kind = r.Kind,
        HexagonId = r.HexagonId,
        AgentId = r.AgentId,
        CreatedDate = r.CreatedDate,
        LastModifiedDate = r.LastModifiedDate,
    };
}
