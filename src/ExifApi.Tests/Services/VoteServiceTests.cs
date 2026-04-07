using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using ExifApi.Services;
using H3Standard;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExifApi.Tests.Services;

public class VoteServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly VoteService _service;

    public VoteServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["H3:AnomalyResolution"] = "13",
                ["Votes:Thresholds:Default"] = "4",
                ["Votes:Thresholds:Pothole"] = "3",
                ["Votes:Thresholds:WaterLeakage"] = "6",
            })
            .Build();

        _service = new VoteService(_context, config, NullLogger<VoteService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<Hexagon> SeedHexagon(string h3Index = "8f39100e1a500e2")
    {
        var hex = _context.Hexagons.Add(new Hexagon { H3Index = h3Index }).Entity;
        await _context.SaveChangesAsync();
        return hex;
    }

    private async Task<Image> SeedImage(int? hexagonId = null)
    {
        var image = _context.Images.Add(new Image
        {
            FileName = "test.jpg",
            HexagonId = hexagonId,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        }).Entity;
        await _context.SaveChangesAsync();
        return image;
    }

    private Vote MakeVote(int hexagonId, AnomalyType kind = AnomalyType.Pothole, decimal confidence = 0.9m) =>
        new()
        {
            HexagonId = hexagonId,
            Kind = kind,
            Confidence = confidence,
            BoxX1 = 10, BoxY1 = 10, BoxX2 = 100, BoxY2 = 100,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

    // -------------------------------------------------------------------------
    // CreateAsync — location resolution
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithHexagonId_ReturnsDto()
    {
        var hex = await SeedHexagon();

        var result = await _service.CreateAsync(new VoteCreateDto
        {
            HexagonId = hex.Id,
            Kind = AnomalyType.Pothole,
            Confidence = 0.9m,
            BoxX1 = 10, BoxY1 = 10, BoxX2 = 100, BoxY2 = 100
        });

        Assert.NotNull(result);
        Assert.Equal(hex.Id, result.HexagonId);
        Assert.Equal(AnomalyType.Pothole, result.Kind);
        Assert.Equal(1, await _context.Votes.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WithHexagonId_NoExplicitImage_AutoAssignsMostRecentImage()
    {
        var hex = await SeedHexagon();
        var older = await SeedImage(hex.Id);
        // small delay to ensure distinct CreatedDate ordering
        await Task.Delay(10);
        var newer = await SeedImage(hex.Id);

        var result = await _service.CreateAsync(new VoteCreateDto
        {
            HexagonId = hex.Id,
            Kind = AnomalyType.Pothole,
        });

        Assert.NotNull(result);
        Assert.Equal(newer.Id, result.ImageId);
    }

    [Fact]
    public async Task CreateAsync_WithHexagonId_NoImagesInHexagon_ImageIdIsNull()
    {
        var hex = await SeedHexagon();

        var result = await _service.CreateAsync(new VoteCreateDto
        {
            HexagonId = hex.Id,
            Kind = AnomalyType.Pothole,
        });

        Assert.NotNull(result);
        Assert.Null(result.ImageId);
    }

    [Fact]
    public async Task CreateAsync_WithLatLon_CreatesHexagonAndReturnsDto()
    {
        var result = await _service.CreateAsync(new VoteCreateDto
        {
            Latitude = 37.0997m,
            Longitude = -8.6827m,
            Kind = AnomalyType.RoadCrack,
            Confidence = 0.8m,
            BoxX1 = 5, BoxY1 = 5, BoxX2 = 50, BoxY2 = 50
        });

        Assert.NotNull(result);
        Assert.True(result.HexagonId > 0);
        Assert.Equal(AnomalyType.RoadCrack, result.Kind);
        Assert.Equal(1, await _context.Hexagons.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WithLatLon_ExistingHexagon_AutoAssignsMostRecentImage()
    {
        const decimal lat = 37.0997m, lon = -8.6827m;
        var h3Index = H3Net.H3ToString(H3Net.LatLngToCell((double)lat, (double)lon, 13));
        var hex = _context.Hexagons.Add(new Hexagon { H3Index = h3Index }).Entity;
        await _context.SaveChangesAsync();

        var older = await SeedImage(hex.Id);
        await Task.Delay(10);
        var newer = await SeedImage(hex.Id);

        var result = await _service.CreateAsync(new VoteCreateDto
        {
            Latitude = lat,
            Longitude = lon,
            Kind = AnomalyType.Pothole,
        });

        Assert.NotNull(result);
        Assert.Equal(hex.Id, result.HexagonId);
        Assert.Equal(newer.Id, result.ImageId);
    }

    [Fact]
    public async Task CreateAsync_WithLatLon_NewHexagon_NoImages_ImageIdIsNull()
    {
        var result = await _service.CreateAsync(new VoteCreateDto
        {
            Latitude = 37.0997m,
            Longitude = -8.6827m,
            Kind = AnomalyType.Pothole,
        });

        Assert.NotNull(result);
        Assert.Null(result.ImageId);
    }

    [Fact]
    public async Task CreateAsync_WithLatLon_ExistingHexagon_NoImages_ImageIdIsNull()
    {
        const decimal lat = 37.0997m, lon = -8.6827m;
        var h3Index = H3Net.H3ToString(H3Net.LatLngToCell((double)lat, (double)lon, 13));
        _context.Hexagons.Add(new Hexagon { H3Index = h3Index });
        await _context.SaveChangesAsync();

        var result = await _service.CreateAsync(new VoteCreateDto
        {
            Latitude = lat,
            Longitude = lon,
            Kind = AnomalyType.Pothole,
        });

        Assert.NotNull(result);
        Assert.Null(result.ImageId);
    }

    [Fact]
    public async Task CreateAsync_WithImageId_ResolvesHexagonFromImage_ReturnsDto()
    {
        var hex = await SeedHexagon();
        var image = await SeedImage(hexagonId: hex.Id);

        var result = await _service.CreateAsync(new VoteCreateDto
        {
            ImageId = image.Id,
            Kind = AnomalyType.Pothole,
            Confidence = 0.75m,
            BoxX1 = 0, BoxY1 = 0, BoxX2 = 50, BoxY2 = 50
        });

        Assert.NotNull(result);
        Assert.Equal(hex.Id, result.HexagonId);
        Assert.Equal(image.Id, result.ImageId);
    }

    [Fact]
    public async Task CreateAsync_WithImageId_ImageHasNoHexagon_ReturnsNull()
    {
        var image = await SeedImage(hexagonId: null);

        var result = await _service.CreateAsync(new VoteCreateDto
        {
            ImageId = image.Id,
            Kind = AnomalyType.Pothole
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithImageId_ImageNotFound_ReturnsNull()
    {
        var result = await _service.CreateAsync(new VoteCreateDto
        {
            ImageId = 99999,
            Kind = AnomalyType.Pothole
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_NoLocation_ReturnsNull()
    {
        var result = await _service.CreateAsync(new VoteCreateDto
        {
            Kind = AnomalyType.Pothole
        });

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // ComputeAsync — threshold logic
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ComputeAsync_ThresholdMet_CreatesAnomaly()
    {
        var hex = await SeedHexagon();
        // Pothole threshold = 3
        _context.Votes.AddRange(
            MakeVote(hex.Id, AnomalyType.Pothole, 0.9m),
            MakeVote(hex.Id, AnomalyType.Pothole, 0.8m),
            MakeVote(hex.Id, AnomalyType.Pothole, 0.7m));
        await _context.SaveChangesAsync();

        var result = await _service.ComputeAsync();

        Assert.Equal(1, result.AnomaliesCreated);
        Assert.Equal(0, result.AnomaliesReopened);
        Assert.Equal(3, result.VotesDeleted);
        Assert.Empty(_context.Votes);
        Assert.Single(_context.RoadVisualAnomalies);
    }

    [Fact]
    public async Task ComputeAsync_ThresholdNotMet_NoAnomalyCreated()
    {
        var hex = await SeedHexagon();
        // Pothole threshold = 3, only 2 votes
        _context.Votes.AddRange(
            MakeVote(hex.Id, AnomalyType.Pothole, 0.9m),
            MakeVote(hex.Id, AnomalyType.Pothole, 0.8m));
        await _context.SaveChangesAsync();

        var result = await _service.ComputeAsync();

        Assert.Equal(0, result.AnomaliesCreated);
        Assert.Equal(2, result.VotesDeleted);
        Assert.Empty(_context.RoadVisualAnomalies);
    }

    [Fact]
    public async Task ComputeAsync_UsesMaxConfidenceVoteBoundingBox()
    {
        var hex = await SeedHexagon();
        _context.Votes.AddRange(
            new Vote { HexagonId = hex.Id, Kind = AnomalyType.Pothole, Confidence = 0.9m, BoxX1 = 99, BoxY1 = 99, BoxX2 = 199, BoxY2 = 199, CreatedDate = DateTime.UtcNow, LastModifiedDate = DateTime.UtcNow },
            new Vote { HexagonId = hex.Id, Kind = AnomalyType.Pothole, Confidence = 0.5m, BoxX1 = 1, BoxY1 = 1, BoxX2 = 10, BoxY2 = 10, CreatedDate = DateTime.UtcNow, LastModifiedDate = DateTime.UtcNow },
            new Vote { HexagonId = hex.Id, Kind = AnomalyType.Pothole, Confidence = 0.6m, BoxX1 = 2, BoxY1 = 2, BoxX2 = 20, BoxY2 = 20, CreatedDate = DateTime.UtcNow, LastModifiedDate = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        await _service.ComputeAsync();

        var anomaly = await _context.RoadVisualAnomalies.SingleAsync();
        Assert.Equal(0.9m, anomaly.Confidence);
        Assert.Equal(99, anomaly.BoxX1); // bounding box from highest-confidence vote
        Assert.Equal(99, anomaly.BoxY1);
    }

    [Fact]
    public async Task ComputeAsync_ReopensResolvedAnomaly()
    {
        var hex = await SeedHexagon();
        _context.RoadVisualAnomalies.Add(new RoadVisualAnomaly
        {
            HexagonId = hex.Id,
            Kind = AnomalyType.Pothole,
            Confidence = 0.5m,
            ResolvedAt = DateTime.UtcNow.AddDays(-1),
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        });
        _context.Votes.AddRange(
            MakeVote(hex.Id, AnomalyType.Pothole),
            MakeVote(hex.Id, AnomalyType.Pothole),
            MakeVote(hex.Id, AnomalyType.Pothole));
        await _context.SaveChangesAsync();

        var result = await _service.ComputeAsync();

        Assert.Equal(0, result.AnomaliesCreated);
        Assert.Equal(1, result.AnomaliesReopened);
        var anomaly = await _context.RoadVisualAnomalies.SingleAsync();
        Assert.Null(anomaly.ResolvedAt);
    }

    [Fact]
    public async Task ComputeAsync_UpdatesActiveAnomaly()
    {
        var hex = await SeedHexagon();
        _context.RoadVisualAnomalies.Add(new RoadVisualAnomaly
        {
            HexagonId = hex.Id,
            Kind = AnomalyType.Pothole,
            Confidence = 0.3m,
            ImageId = null,
            ResolvedAt = null, // still active
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        });
        var image = await SeedImage(hex.Id);
        _context.Votes.AddRange(
            new Vote { HexagonId = hex.Id, Kind = AnomalyType.Pothole, Confidence = 0.9m, ImageId = image.Id, BoxX1 = 99, BoxY1 = 99, BoxX2 = 199, BoxY2 = 199, CreatedDate = DateTime.UtcNow, LastModifiedDate = DateTime.UtcNow },
            MakeVote(hex.Id, AnomalyType.Pothole, 0.8m),
            MakeVote(hex.Id, AnomalyType.Pothole, 0.7m));
        await _context.SaveChangesAsync();

        var result = await _service.ComputeAsync();

        Assert.Equal(0, result.AnomaliesCreated);
        Assert.Equal(0, result.AnomaliesReopened);
        Assert.Equal(1, result.AnomaliesUpdated);
        Assert.Equal(3, result.VotesDeleted);

        var anomaly = await _context.RoadVisualAnomalies.SingleAsync();
        Assert.Null(anomaly.ResolvedAt);       // still active, not reopened
        Assert.Equal(0.9m, anomaly.Confidence); // updated to max
        Assert.Equal(image.Id, anomaly.ImageId); // image auto-assigned from best vote
        Assert.Equal(99, anomaly.BoxX1);         // bbox from best vote
    }

    [Fact]
    public async Task ComputeAsync_MultipleKinds_CreatesAnomalyPerKind()
    {
        var hex = await SeedHexagon();
        // Pothole threshold=3, RoadCrack threshold=4 (default)
        _context.Votes.AddRange(
            MakeVote(hex.Id, AnomalyType.Pothole),
            MakeVote(hex.Id, AnomalyType.Pothole),
            MakeVote(hex.Id, AnomalyType.Pothole),
            MakeVote(hex.Id, AnomalyType.RoadCrack),
            MakeVote(hex.Id, AnomalyType.RoadCrack),
            MakeVote(hex.Id, AnomalyType.RoadCrack),
            MakeVote(hex.Id, AnomalyType.RoadCrack));
        await _context.SaveChangesAsync();

        var result = await _service.ComputeAsync();

        Assert.Equal(2, result.AnomaliesCreated);
        Assert.Equal(7, result.VotesDeleted);
        Assert.Equal(2, await _context.RoadVisualAnomalies.CountAsync());
    }

    [Fact]
    public async Task ComputeAsync_NoVotes_ReturnsZeroes()
    {
        var result = await _service.ComputeAsync();

        Assert.Equal(0, result.AnomaliesCreated);
        Assert.Equal(0, result.AnomaliesReopened);
        Assert.Equal(0, result.VotesDeleted);
    }

    [Fact]
    public async Task ComputeAsync_DeletesAllVotesRegardlessOfThreshold()
    {
        var hex = await SeedHexagon();
        // 1 vote — below any threshold
        _context.Votes.Add(MakeVote(hex.Id, AnomalyType.Pothole));
        await _context.SaveChangesAsync();

        await _service.ComputeAsync();

        Assert.Empty(_context.Votes);
    }
}
