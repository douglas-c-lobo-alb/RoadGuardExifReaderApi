using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using ExifApi.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExifApi.Tests.Services;

public class AgentServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly AgentService _service;

    public AgentServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _service = new AgentService(_context, NullLogger<AgentService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // -------------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmptyList()
    {
        var result = await _service.GetAllAsync();

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsCorrectDto()
    {
        _context.Agents.Add(new Agent { Id = 1, Name = "Alpha" });
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Alpha", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // CreateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsDto()
    {
        var dto = new CreateAgentDto { Name = "BetaAgent" };

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("BetaAgent", result.Name);
        Assert.Equal(1, await _context.Agents.CountAsync());
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrueAndRemoves()
    {
        _context.Agents.Add(new Agent { Id = 1, Name = "ToDelete" });
        await _context.SaveChangesAsync();

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
        Assert.Null(await _context.Agents.FindAsync(1));
    }

    [Fact]
    public async Task DeleteAsync_MissingId_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(999);

        Assert.False(result);
    }
}
