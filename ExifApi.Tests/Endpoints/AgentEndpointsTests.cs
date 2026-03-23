using System.Net;
using System.Net.Http.Json;
using ExifApi.Data.Entities;
using ExifApi.Dtos;

namespace ExifApi.Tests.Endpoints;

public class AgentEndpointsTests : IDisposable
{
    private readonly ExifApiFactory _factory;
    private readonly HttpClient _client;

    public AgentEndpointsTests()
    {
        _factory = new ExifApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // -------------------------------------------------------------------------
    // GET /api/agents/
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_EmptyDb_Returns200AndEmptyArray()
    {
        var response = await _client.GetAsync("api/agents/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<AgentDto>>();
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public async Task GetAll_WithAgents_Returns200AndList()
    {
        using var ctx = _factory.CreateDbContext();
        ctx.Agents.AddRange(
            new Agent { Name = "Agent Alpha" },
            new Agent { Name = "Agent Beta" });
        await ctx.SaveChangesAsync();

        var response = await _client.GetAsync("api/agents/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<AgentDto>>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
    }

    // -------------------------------------------------------------------------
    // GET /api/agents/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingAgent_Returns200WithDto()
    {
        using var ctx = _factory.CreateDbContext();
        var agent = new Agent { Name = "RoadGuard-01" };
        ctx.Agents.Add(agent);
        await ctx.SaveChangesAsync();
        int id = agent.Id;

        var response = await _client.GetAsync($"api/agents/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AgentDto>();
        Assert.NotNull(dto);
        Assert.Equal(id, dto.Id);
        Assert.Equal("RoadGuard-01", dto.Name);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var response = await _client.GetAsync("api/agents/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // POST /api/agents/
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_ValidDto_Returns201AndDto()
    {
        var dto = new CreateAgentDto { Name = "NewAgent" };

        var response = await _client.PostAsJsonAsync("api/agents/", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AgentDto>();
        Assert.NotNull(body);
        Assert.True(body.Id > 0);
        Assert.Equal("NewAgent", body.Name);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/agents/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingAgent_Returns204()
    {
        using var ctx = _factory.CreateDbContext();
        var agent = new Agent { Name = "ToDelete" };
        ctx.Agents.Add(agent);
        await ctx.SaveChangesAsync();
        int id = agent.Id;

        var response = await _client.DeleteAsync($"api/agents/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verify = _factory.CreateDbContext();
        Assert.Null(await verify.Agents.FindAsync(id));
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync("api/agents/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
