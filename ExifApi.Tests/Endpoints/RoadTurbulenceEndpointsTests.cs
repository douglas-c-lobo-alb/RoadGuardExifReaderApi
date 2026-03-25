using System.Net;
using System.Net.Http.Json;
using ExifApi.Data.Entities;
using ExifApi.Dtos;

namespace ExifApi.Tests.Endpoints;

/// <summary>
/// Tests for /api/turbulences/ endpoints.
/// All tests use turbulence records without hexagon associations so that
/// H3Net (Windows-only native DLL) is never invoked during serialisation.
/// </summary>
public class RoadTurbulenceEndpointsTests : IDisposable
{
    private readonly ExifApiFactory _factory;
    private readonly HttpClient _client;

    public RoadTurbulenceEndpointsTests()
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
    // GET /api/turbulences/
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_EmptyDb_Returns200AndEmptyArray()
    {
        var response = await _client.GetAsync("api/turbulences/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RoadTurbulenceDto>>(ExifApiFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public async Task GetAll_WithRecords_Returns200AndList()
    {
        using var ctx = _factory.CreateDbContext();
        var hex = ctx.Hexagons.Add(new ExifApi.Data.Entities.Hexagon { H3Index = "8f39100e1a500e2" }).Entity;
        await ctx.SaveChangesAsync();
        ctx.RoadTurbulences.AddRange(
            new RoadTurbulence { Index = 3, Kind = RoadTurbulenceType.Pothole, HexagonId = hex.Id, CreatedDate = DateTime.UtcNow },
            new RoadTurbulence { Index = 5, Kind = RoadTurbulenceType.Speedbump, HexagonId = hex.Id, CreatedDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var response = await _client.GetAsync("api/turbulences/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RoadTurbulenceDto>>(ExifApiFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
    }

    // -------------------------------------------------------------------------
    // GET /api/turbulences/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingRecord_Returns200WithDto()
    {
        using var ctx = _factory.CreateDbContext();
        var hex = ctx.Hexagons.Add(new ExifApi.Data.Entities.Hexagon { H3Index = "8f39100e1a500e2" }).Entity;
        await ctx.SaveChangesAsync();
        var record = new RoadTurbulence
        {
            Index = 7,
            Kind = RoadTurbulenceType.Depression,
            HexagonId = hex.Id,
            CreatedDate = DateTime.UtcNow
        };
        ctx.RoadTurbulences.Add(record);
        await ctx.SaveChangesAsync();
        int id = record.Id;

        var response = await _client.GetAsync($"api/turbulences/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<RoadTurbulenceDto>(ExifApiFactory.JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(id, dto.Id);
        Assert.Equal(7, dto.Index);
        Assert.Equal(RoadTurbulenceType.Depression, dto.Kind);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var response = await _client.GetAsync("api/turbulences/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // GET /api/turbulences/h3/{h3Index}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByH3Index_NoMatch_Returns200EmptyArray()
    {
        var response = await _client.GetAsync("api/turbulences/h3/8f39100e1a500e2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RoadTurbulenceDto>>(ExifApiFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    // -------------------------------------------------------------------------
    // POST /api/turbulences/
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_ValidRecords_Returns201AndList()
    {
        using var ctx = _factory.CreateDbContext();
        var hex = ctx.Hexagons.Add(new ExifApi.Data.Entities.Hexagon { H3Index = "8f39100e1a500e2" }).Entity;
        await ctx.SaveChangesAsync();

        var dtos = new List<RoadTurbulenceCreateDto>
        {
            new() { Index = 2, Kind = RoadTurbulenceType.Speedbump, HexagonId = hex.Id },
            new() { Index = 4, Kind = RoadTurbulenceType.Pothole, HexagonId = hex.Id }
        };

        var response = await _client.PostAsJsonAsync("api/turbulences/", dtos);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RoadTurbulenceDto>>(ExifApiFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
    }

    [Fact]
    public async Task Create_EmptyList_Returns400()
    {
        var dtos = new List<RoadTurbulenceCreateDto>();

        var response = await _client.PostAsJsonAsync("api/turbulences/", dtos);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // PUT /api/turbulences/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingRecord_Returns200WithUpdatedDto()
    {
        using var ctx = _factory.CreateDbContext();
        var hex = ctx.Hexagons.Add(new ExifApi.Data.Entities.Hexagon { H3Index = "8f39100e1a500e2" }).Entity;
        await ctx.SaveChangesAsync();
        var record = new RoadTurbulence
        {
            Index = 1,
            Kind = RoadTurbulenceType.Pothole,
            HexagonId = hex.Id,
            CreatedDate = DateTime.UtcNow
        };
        ctx.RoadTurbulences.Add(record);
        await ctx.SaveChangesAsync();
        int id = record.Id;

        var update = new RoadTurbulenceCreateDto { Index = 8, Kind = RoadTurbulenceType.Speedbump };
        var response = await _client.PutAsJsonAsync($"api/turbulences/{id}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<RoadTurbulenceDto>(ExifApiFactory.JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(8, dto.Index);
        Assert.Equal(RoadTurbulenceType.Speedbump, dto.Kind);
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        var update = new RoadTurbulenceCreateDto { Index = 5, Kind = RoadTurbulenceType.Pothole };

        var response = await _client.PutAsJsonAsync("api/turbulences/99999", update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/turbulences/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingRecord_Returns204()
    {
        using var ctx = _factory.CreateDbContext();
        var hex = ctx.Hexagons.Add(new ExifApi.Data.Entities.Hexagon { H3Index = "8f39100e1a500e2" }).Entity;
        await ctx.SaveChangesAsync();
        var record = new RoadTurbulence
        {
            Index = 3,
            Kind = RoadTurbulenceType.Pothole,
            HexagonId = hex.Id,
            CreatedDate = DateTime.UtcNow
        };
        ctx.RoadTurbulences.Add(record);
        await ctx.SaveChangesAsync();
        int id = record.Id;

        var response = await _client.DeleteAsync($"api/turbulences/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verify = _factory.CreateDbContext();
        Assert.Null(await verify.RoadTurbulences.FindAsync(id));
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync("api/turbulences/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
