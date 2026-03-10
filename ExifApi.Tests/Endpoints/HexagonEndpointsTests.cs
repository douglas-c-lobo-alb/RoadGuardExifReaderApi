using System.Net;
using System.Net.Http.Json;
using ExifApi.Data.Entities;
using ExifApi.Dtos;

namespace ExifApi.Tests.Endpoints;

/// <summary>
/// Tests for GET /api/hexagons/ that do not require the H3 native DLL
/// (empty-DB / 404 paths).  Tests that seed hexagon rows or call Create/Update
/// invoke H3Net and require Windows with h3.dll present.
/// </summary>
public class HexagonEndpointsTests : IDisposable
{
    // Known valid H3 index at resolution 15 (lat=37.09973, lon=-8.68272)
    private const string ValidH3Index = "8f39100e1a500e2";

    private readonly ExifApiFactory _factory;
    private readonly HttpClient _client;

    public HexagonEndpointsTests()
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
    // GET /api/hexagons/
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_EmptyDb_Returns200AndEmptyArray()
    {
        var response = await _client.GetAsync("api/hexagons/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    // -------------------------------------------------------------------------
    // GET /api/hexagons/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var response = await _client.GetAsync("api/hexagons/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // POST /api/hexagons/  — requires H3 DLL (Windows only)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_NonExistentImage_Returns400()
    {
        // ImageId 99999 does not exist — service returns null → 400
        var dto = new CreateHexagonDto { ImageId = 99999, H3Index = ValidH3Index };

        var response = await _client.PostAsJsonAsync("api/hexagons/", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ImageAlreadyHasHexagon_Returns400()
    {
        using var ctx = _factory.CreateDbContext();
        var hex = new Hexagon { H3Index = ValidH3Index };
        var img = new Image { FileName = "already_hex.jpg", Hexagon = hex };
        ctx.Images.Add(img);
        await ctx.SaveChangesAsync();

        var dto = new CreateHexagonDto { ImageId = img.Id, H3Index = ValidH3Index };
        var response = await _client.PostAsJsonAsync("api/hexagons/", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidData_Returns201()
    {
        using var ctx = _factory.CreateDbContext();
        var img = new Image { FileName = "new_hex.jpg" };
        ctx.Images.Add(img);
        await ctx.SaveChangesAsync();

        var dto = new CreateHexagonDto { ImageId = img.Id, H3Index = ValidH3Index };
        var response = await _client.PostAsJsonAsync("api/hexagons/", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HexagonDto>();
        Assert.NotNull(body);
        Assert.Equal(ValidH3Index, body.H3Index);
        Assert.Equal(15, body.Resolution);
    }

    // -------------------------------------------------------------------------
    // PUT /api/hexagons/{id}  — requires H3 DLL (Windows only)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        var dto = new UpdateHexagonDto { H3Index = ValidH3Index };

        var response = await _client.PutAsJsonAsync("api/hexagons/99999", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidData_Returns200()
    {
        using var ctx = _factory.CreateDbContext();
        var hex = new Hexagon { H3Index = ValidH3Index };
        ctx.Hexagons.Add(hex);
        await ctx.SaveChangesAsync();

        // Update with the same valid index — just verifies round-trip
        var dto = new UpdateHexagonDto { H3Index = ValidH3Index };
        var response = await _client.PutAsJsonAsync($"api/hexagons/{hex.Id}", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HexagonDto>();
        Assert.NotNull(body);
        Assert.Equal(ValidH3Index, body.H3Index);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/hexagons/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync("api/hexagons/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingHexagon_Returns204()
    {
        using var ctx = _factory.CreateDbContext();
        var hex = new Hexagon { H3Index = ValidH3Index };
        ctx.Hexagons.Add(hex);
        await ctx.SaveChangesAsync();
        int id = hex.Id;

        var response = await _client.DeleteAsync($"api/hexagons/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verify = _factory.CreateDbContext();
        Assert.Null(await verify.Hexagons.FindAsync(id));
    }
}
