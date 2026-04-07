using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Services;

public class SessionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SessionService> _logger;

    public SessionService(ApplicationDbContext context, ILogger<SessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SessionDto>> GetAllAsync()
    {
        var sessions = await _context.Sessions
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();
        return sessions.Select(ToDto).ToList();
    }

    public async Task<SessionDto?> GetByIdAsync(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        return session is null ? null : ToDto(session);
    }

    public async Task<SessionDto?> CreateAsync(SessionCreateDto dto)
    {
        var agentExists = await _context.Agents.AnyAsync(a => a.Id == dto.AgentId);
        if (!agentExists)
        {
            _logger.LogWarning("Agent id={AgentId} not found", dto.AgentId);
            return null;
        }

        var session = new Session
        {
            AgentId = dto.AgentId,
            StartedAt = dto.StartedAt,
            FinishedAt = dto.FinishedAt
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created session id={Id} for agent id={AgentId}", session.Id, session.AgentId);
        return ToDto(session);
    }

    public async Task<SessionDto?> FinishAsync(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session is null) return null;

        session.FinishedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Finished session id={Id}", id);
        return ToDto(session);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session is null) return false;

        _context.Sessions.Remove(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted session id={Id}", id);
        return true;
    }

    private static SessionDto ToDto(Session s) => new()
    {
        Id = s.Id,
        AgentId = s.AgentId,
        StartedAt = s.StartedAt,
        FinishedAt = s.FinishedAt
    };
}
