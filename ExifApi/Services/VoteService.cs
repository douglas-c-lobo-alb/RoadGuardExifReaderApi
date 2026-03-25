using System;
using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using H3Standard;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Services;

public class VoteService(
    ApplicationDbContext context,
    IConfiguration config,
    ILogger<VoteService> logger)
{
    private readonly int _anomalyResolution =
config.GetValue<int>("H3:AnomalyResolution", 13);

    public async Task<VoteDto?> CreateAsync(VoteCreateDto dto)
    {
        // Resolve HexagonId
        int hexagonId;
        if (dto.HexagonId.HasValue)
        {
            hexagonId = dto.HexagonId.Value;
        }
        else if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            var h3Raw = H3Net.LatLngToCell((double)dto.Latitude.Value, (double)dto.Longitude.Value, _anomalyResolution);
            var h3Index = H3Net.H3ToString(h3Raw);
            var hex = await context.Hexagons.FirstOrDefaultAsync(h => h.H3Index
== h3Index)
                      ?? context.Hexagons.Add(new Hexagon
                      {
                          H3Index = h3Index
                      }).Entity;
            await context.SaveChangesAsync();
            hexagonId = hex.Id;
        }
        else return null;

        var entity = new Vote
        {
            HexagonId = hexagonId,
            AgentId = dto.AgentId,
            ImageId = dto.ImageId,
            Kind = dto.Kind,
            Confidence = dto.Confidence,
            BoxX1 = dto.BoxX1,
            BoxY1 = dto.BoxY1,
            BoxX2 = dto.BoxX2,
            BoxY2 = dto.BoxY2,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        context.Votes.Add(entity);
        await context.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<List<VoteDto>> GetAllAsync() =>
        (await context.Votes.OrderByDescending(v =>
v.CreatedDate).ToListAsync())
        .Select(ToDto).ToList();

    public async Task<VoteDto?> GetByIdAsync(int id)
    {
        var v = await context.Votes.FindAsync(id);
        return v is null ? null : ToDto(v);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var v = await context.Votes.FindAsync(id);
        if (v is null) return false;
        context.Votes.Remove(v);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<ComputeResultDto> ComputeAsync()
    {
        // Read thresholds from config: "Votes:Thresholds:Pothole" etc., fallback "Votes:Thresholds:Default"
             var votes = await context.Votes.ToListAsync();
        var grouped = votes.GroupBy(v => (v.HexagonId, v.Kind));

        int created = 0, reopened = 0;

        foreach (var group in grouped)
        {
            var threshold = GetThreshold(group.Key.Kind, config);
            if (group.Count() < threshold) continue;

            var existing = await context.RoadVisualAnomalies
                .FirstOrDefaultAsync(a => a.HexagonId == group.Key.HexagonId &&
a.Kind == group.Key.Kind);

            if (existing is not null && existing.ResolvedAt is null) continue;
            // active anomaly exists

            if (existing is not null)
            {
                existing.ResolvedAt = null;
                existing.LastModifiedDate = DateTime.UtcNow;
                reopened++;
            }
            else
            {
                var best = group.OrderByDescending(v => v.Confidence).First();
                context.RoadVisualAnomalies.Add(new RoadVisualAnomaly
                {
                    HexagonId = group.Key.HexagonId,
                    ImageId = best.ImageId,
                    Kind = group.Key.Kind,
                    Confidence = (decimal)group.Average(v => (double)(v.Confidence ?? 0)),
                    BoxX1 = best.BoxX1,
                    BoxY1 = best.BoxY1,
                    BoxX2 = best.BoxX2,
                    BoxY2 = best.BoxY2,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                });
                created++;
            }
        }

        context.Votes.RemoveRange(votes);
        await context.SaveChangesAsync();

        logger.LogInformation("Compute: {Created} created, {Reopened} reopened, { Deleted} votes deleted", created, reopened, votes.Count);

        return new ComputeResultDto(created, reopened, votes.Count);
    }

    private static int GetThreshold(AnomalyType kind, IConfiguration config)
    {
        var kindStr = kind.ToString();
        var specific = config.GetValue<int?>($"Votes:Thresholds:{kindStr}");
        return specific ?? config.GetValue<int>("Votes:Thresholds:Default", 4);
    }

    private static VoteDto ToDto(Vote v) => new()
    {
        Id = v.Id,
        HexagonId = v.HexagonId,
        AgentId = v.AgentId,
        ImageId =
v.ImageId,
        Kind = v.Kind,
        Confidence = v.Confidence,
        BoxX1 = v.BoxX1,
        BoxY1 = v.BoxY1,
        BoxX2 = v.BoxX2,
        BoxY2 = v.BoxY2,
        CreatedDate = v.CreatedDate,
        LastModifiedDate = v.LastModifiedDate
    };
}

