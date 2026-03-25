using System.Net;
using System.Net.Http.Json;
using ExifApi.Data.Entities;
using ExifApi.Dtos;

namespace ExifApi.Tests.Endpoints;

public class VoteEndpointsTests : IDisposable
{
    private readonly ExifApiFactory _factory;
    private readonly HttpClient _client;

    public VoteEndpointsTests()
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
    // POST /api/votes/anomaly
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_ValidVoteWithHexagonId_Returns201()
    {
        using var ctx = _factory.CreateDbContext();
        var hex = ctx.Hexagons.Add(new Hexagon { H3Index = "8f39100e1a500e2" }).Entity;
        await ctx.SaveChangesAsync();

        var dto = new VoteCreateDto
        {
            HexagonId = hex.Id,
            Kind = AnomalyType.Pothole,
            Confidence = 0.9m,
            BoxX1 = 10, BoxY1 = 20, BoxX2 = 100, BoxY2 = 200
        };

        var response = await _client.PostAsJsonAsync("api/votes/anomaly", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<VoteDto>(ExifApiFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(hex.Id, body.HexagonId);
        Assert.Equal(AnomalyType.Pothole, body.Kind);
    }

    [Fact]
    public async Task Create_WithLatLon_ComputesHexagon_Returns201()
    {
        var dto = new VoteCreateDto
        {
            Latitude = 37.0997m,
            Longitude = -8.6827m,
            Kind = AnomalyType.Crack,
            Confidence = 0.8m,
            BoxX1 = 5, BoxY1 = 5, BoxX2 = 50, BoxY2 = 50
        };

        var response = await _client.PostAsJsonAsync("api/votes/anomaly", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<VoteDto>(ExifApiFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.True(body.HexagonId > 0);
        Assert.Equal(AnomalyType.Crack, body.Kind);
    }

    [Fact]
    public async Task Create_NoHexagonOrLatLon_Returns400()
    {
        var dto = new VoteCreateDto
        {
            Kind = AnomalyType.Pothole,
            BoxX1 = 0, BoxY1 = 0, BoxX2 = 10, BoxY2 = 10
        };

        var response = await _client.PostAsJsonAsync("api/votes/anomaly", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // GET /api/votes/anomaly
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_Returns200WithList()
    {
        using var ctx = _factory.CreateDbContext();
        var hex = ctx.Hexagons.Add(new Hexagon { H3Index = "8f39100e1a500e2" }).Entity;
        await ctx.SaveChangesAsync();
        ctx.Votes.AddRange(
            new Vote { HexagonId = hex.Id, Kind = AnomalyType.Pothole, CreatedDate = DateTime.UtcNow, LastModifiedDate = DateTime.UtcNow },
            new Vote { HexagonId = hex.Id, Kind = AnomalyType.Crack, CreatedDate = DateTime.UtcNow, LastModifiedDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var response = await _client.GetAsync("api/votes/anomaly");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<VoteDto>>(ExifApiFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
    }

    // -------------------------------------------------------------------------
    // GET /api/votes/anomaly/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_Existing_Returns200()
    {
        using var ctx = _factory.CreateDbContext();
        var hex = ctx.Hexagons.Add(new Hexagon { H3Index = "8f39100e1a500e2" }).Entity;
        await ctx.SaveChangesAsync();
        var vote = ctx.Votes.Add(new Vote
        {
            HexagonId = hex.Id,
            Kind = AnomalyType.Pothole,
            Confidence = 0.75m,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        }).Entity;
        await ctx.SaveChangesAsync();

        var response = await _client.GetAsync($"api/votes/anomaly/{vote.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<VoteDto>(ExifApiFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(vote.Id, body.Id);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var response = await _client.GetAsync("api/votes/anomaly/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/votes/anomaly/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingVote_Returns204()
    {
        using var ctx = _factory.CreateDbContext();
        var hex = ctx.Hexagons.Add(new Hexagon { H3Index = "8f39100e1a500e2" }).Entity;
        await ctx.SaveChangesAsync();
        var vote = ctx.Votes.Add(new Vote
        {
            HexagonId = hex.Id,
            Kind = AnomalyType.Pothole,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        }).Entity;
        await ctx.SaveChangesAsync();

        var response = await _client.DeleteAsync($"api/votes/anomaly/{vote.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verify = _factory.CreateDbContext();
        Assert.Null(await verify.Votes.FindAsync(vote.Id));
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync("api/votes/anomaly/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // POST /api/votes/compute
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Compute_WithEnoughVotes_CreatesAnomaly_DeletesVotes()
    {
        // Pothole threshold is 3 (from appsettings)
        using var ctx = _factory.CreateDbContext();
        var hex = ctx.Hexagons.Add(new Hexagon { H3Index = "8f39100e1a500e2" }).Entity;
        await ctx.SaveChangesAsync();
        ctx.Votes.AddRange(
            new Vote { HexagonId = hex.Id, Kind = AnomalyType.Pothole, Confidence = 0.9m, BoxX1 = 10, BoxY1 = 10, BoxX2 = 100, BoxY2 = 100, CreatedDate = DateTime.UtcNow, LastModifiedDate = DateTime.UtcNow },
            new Vote { HexagonId = hex.Id, Kind = AnomalyType.Pothole, Confidence = 0.8m, BoxX1 = 15, BoxY1 = 15, BoxX2 = 105, BoxY2 = 105, CreatedDate = DateTime.UtcNow, LastModifiedDate = DateTime.UtcNow },
            new Vote { HexagonId = hex.Id, Kind = AnomalyType.Pothole, Confidence = 0.7m, BoxX1 = 12, BoxY1 = 12, BoxX2 = 102, BoxY2 = 102, CreatedDate = DateTime.UtcNow, LastModifiedDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var response = await _client.PostAsync("api/votes/compute", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ComputeResultDto>(ExifApiFactory.JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(1, result.AnomaliesCreated);
        Assert.Equal(0, result.AnomaliesReopened);
        Assert.Equal(3, result.VotesDeleted);

        using var verify = _factory.CreateDbContext();
        Assert.Empty(verify.Votes);
        Assert.Single(verify.RoadVisualAnomalies);
    }

    [Fact]
    public async Task Compute_NotEnoughVotes_NoAnomalyCreated_DeletesVotes()
    {
        // Pothole threshold is 3, only 2 votes
        using var ctx = _factory.CreateDbContext();
        var hex = ctx.Hexagons.Add(new Hexagon { H3Index = "8f39100e1a500e2" }).Entity;
        await ctx.SaveChangesAsync();
        ctx.Votes.AddRange(
            new Vote { HexagonId = hex.Id, Kind = AnomalyType.Pothole, Confidence = 0.9m, BoxX1 = 10, BoxY1 = 10, BoxX2 = 100, BoxY2 = 100, CreatedDate = DateTime.UtcNow, LastModifiedDate = DateTime.UtcNow },
            new Vote { HexagonId = hex.Id, Kind = AnomalyType.Pothole, Confidence = 0.8m, BoxX1 = 15, BoxY1 = 15, BoxX2 = 105, BoxY2 = 105, CreatedDate = DateTime.UtcNow, LastModifiedDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var response = await _client.PostAsync("api/votes/compute", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ComputeResultDto>(ExifApiFactory.JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(0, result.AnomaliesCreated);
        Assert.Equal(2, result.VotesDeleted);

        using var verify = _factory.CreateDbContext();
        Assert.Empty(verify.Votes);
        Assert.Empty(verify.RoadVisualAnomalies);
    }
}
