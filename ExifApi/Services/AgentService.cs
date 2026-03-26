using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Services;

public class AgentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AgentService> _logger;

    public AgentService(ApplicationDbContext context, ILogger<AgentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AgentDto>> GetAllAsync()
    {
        var agents = await _context.Agents
            .OrderBy(a => a.CreatedDate)
            .ToListAsync();
        return agents.Select(ToDto).ToList();
    }

    public async Task<AgentDto?> GetByIdAsync(int id)
    {
        var agent = await _context.Agents.FindAsync(id);
        return agent is null ? null : ToDto(agent);
    }

    public async Task<AgentDto> CreateAsync(AgentCreateDto dto)
    {
        var agent = new Agent
        {
            Name = dto.Name,
            Metadata = dto.Metadata
        };

        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created agent id={Id}, name={Name}", agent.Id, agent.Name);
        return ToDto(agent);
    }

    public async Task<AgentDto?> UpdateAsync(int id, AgentCreateDto dto)
    {
        var agent = await _context.Agents.FindAsync(id);
        if (agent is null) return null;

        agent.Name = dto.Name;
        agent.Metadata = dto.Metadata;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated agent id={Id}, name={Name}", agent.Id, agent.Name);
        return ToDto(agent);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var agent = await _context.Agents.FindAsync(id);
        if (agent is null) return false;

        _context.Agents.Remove(agent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted agent id={Id}", id);
        return true;
    }

    private static AgentDto ToDto(Agent agent) => new()
    {
        Id = agent.Id,
        Name = agent.Name,
        Metadata = agent.Metadata,
        CreatedDate = agent.CreatedDate
    };
}
