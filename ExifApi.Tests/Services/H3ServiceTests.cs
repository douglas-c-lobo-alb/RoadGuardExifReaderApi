using System.Text.Json;
using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExifApi.Tests.Services;

/// <summary>
/// Tests for H3Service.
/// Uses a real SQLite in-memory database to support EF Core owned JSON types.
/// </summary>
public class H3ServiceTests : IDisposable
{
    // Known cell computed from lat=37.09973, lon=-8.68272 at res 15
    private const string KnownH3Index = "8f39100e1a500e2"; // res 15
    private const double KnownLat = 37.09973;
    private const double KnownLon = -8.68272;

    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly H3Service _service;

    public H3ServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var config = new ConfigurationBuilder().Build(); // uses default H3 resolution (15)
        _service = new H3Service(_context, NullLogger<H3Service>.Instance, config);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // -------------------------------------------------------------------------
    // LatLngToCell
    // -------------------------------------------------------------------------

    [Fact]
    public void LatLngToCell_KnownCoordinates_ReturnsValidCell()
    {
        var result = _service.LatLngToCell(KnownLat, KnownLon, 15);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.H3Index));
        Assert.Equal(15, result.Resolution);
    }

    [Fact]
    public void LatLngToCell_KnownCoordinates_MatchesExpectedIndex()
    {
        var result = _service.LatLngToCell(KnownLat, KnownLon, 15);

        Assert.NotNull(result);
        Assert.Equal(KnownH3Index, result.H3Index);
    }

    [Fact]
    public void LatLngToCell_InvalidResolution_ReturnsNull()
    {
        // Resolution 16 is out of H3 range — library returns 0
        var result = _service.LatLngToCell(KnownLat, KnownLon, 16);

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // CellToParent
    // -------------------------------------------------------------------------

    [Fact]
    public void CellToParent_ValidCell_ReturnsLowerResolution()
    {
        var result = _service.CellToParent(KnownH3Index, 14);

        Assert.NotNull(result);
        Assert.Equal(14, result.Resolution);
    }

    [Fact]
    public void CellToParent_SameResolution_ReturnsSameCell()
    {
        var result = _service.CellToParent(KnownH3Index, 15);

        Assert.NotNull(result);
        Assert.Equal(KnownH3Index, result.H3Index);
    }

    [Fact]
    public void CellToParent_InvalidIndex_ReturnsNull()
    {
        var result = _service.CellToParent("not-a-valid-h3-index", 14);

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // CellToChildren
    // -------------------------------------------------------------------------

    [Fact]
    public void CellToChildren_Res14To15_Returns7Children()
    {
        var parent = _service.CellToParent(KnownH3Index, 14);
        Assert.NotNull(parent);

        var children = _service.CellToChildren(parent.H3Index, 15);

        Assert.Equal(7, children.Count);
        Assert.All(children, c => Assert.Equal(15, c.Resolution));
    }

    [Fact]
    public void CellToChildren_SameResolution_ReturnsSingleCell()
    {
        var children = _service.CellToChildren(KnownH3Index, 15);

        Assert.Single(children);
        Assert.Equal(KnownH3Index, children[0].H3Index);
    }

    [Fact]
    public void CellToChildren_InvalidIndex_ReturnsEmptyList()
    {
        var result = _service.CellToChildren("not-a-valid-h3-index", 15);

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // GridDisk
    // -------------------------------------------------------------------------

    [Fact]
    public void GridDisk_K0_ReturnsCenterCellOnly()
    {
        var result = _service.GridDisk(KnownH3Index, 0);

        Assert.Single(result);
        Assert.Equal(KnownH3Index, result[0].H3Index);
    }

    [Fact]
    public void GridDisk_K1_Returns7Cells()
    {
        // k=1 disk: centre + 6 neighbours = 7
        var result = _service.GridDisk(KnownH3Index, 1);

        Assert.Equal(7, result.Count);
    }

    [Fact]
    public void GridDisk_K2_Returns19Cells()
    {
        // k=2 disk: 1 + 6 + 12 = 19
        var result = _service.GridDisk(KnownH3Index, 2);

        Assert.Equal(19, result.Count);
    }

    [Fact]
    public void GridDisk_InvalidIndex_ReturnsEmptyList()
    {
        var result = _service.GridDisk("not-a-valid-h3-index", 1);

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // GenerateHexagonsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GenerateHexagonsAsync_ImageWithoutHexagon_CreatesHexagon()
    {
        SeedImage(id: 1, lat: (decimal)KnownLat, lon: (decimal)KnownLon);

        await _service.GenerateHexagonsAsync();

        var image = await _context.Images.FindAsync(1);
        Assert.NotNull(image?.HexagonId);
        var hexagon = await _context.Hexagons.FindAsync(image!.HexagonId);
        Assert.NotNull(hexagon);
        Assert.False(string.IsNullOrEmpty(hexagon!.H3Index));
    }

    [Fact]
    public async Task GenerateHexagonsAsync_ImageAlreadyHasHexagon_Skips()
    {
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index);

        await _service.GenerateHexagonsAsync();

        var count = await _context.Hexagons.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GenerateHexagonsAsync_ImageWithNullCoordinates_SkipsHexagonCreation()
    {
        SeedImage(id: 1, lat: null, lon: null);

        await _service.GenerateHexagonsAsync();

        var image = await _context.Images.FindAsync(1);
        Assert.Null(image?.HexagonId);
        Assert.Empty(_context.Hexagons);
    }

    // -------------------------------------------------------------------------
    // GetHexagonsByViewportAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetHexagonsByViewportAsync_EmptyDb_ReturnsEmptyList()
    {
        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_ImageInBounds_ReturnsHexagonWithImage()
    {
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index);

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66);

        Assert.Single(result);
        Assert.Equal(KnownH3Index, result[0].H3Index);
        Assert.Single(result[0].Images);
        Assert.Equal(1, result[0].Images[0].Id);
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_ImageOutsideBounds_ReturnsEmpty()
    {
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index);

        // Bounds somewhere in the North Sea — no images there
        var result = await _service.GetHexagonsByViewportAsync(55.0, 56.0, 2.0, 4.0);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_CoarserResolution_ReturnsCorrectResolutionOnResults()
    {
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index);

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66, resolution: 7);

        Assert.NotEmpty(result);
        Assert.All(result, h => Assert.Equal(7, h.Resolution));
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_TwoCellsInSameParent_DeduplicatesAtCoarserResolution()
    {
        // Both cells are nearby in Portimão — they share the same parent at res 7
        const string cell1 = "8f39100e1a500e6";
        const string cell2 = "8f39100e1a502f1";

        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: cell1);
        SeedImageWithHexagon(id: 2, lat: 37.0997m, lon: -8.6825m, h3Index: cell2);

        var res15 = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66, resolution: 15);
        var res7 = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66, resolution: 7);

        Assert.Equal(2, res15.Count); // two distinct res-15 cells
        Assert.True(res7.Count < res15.Count); // rolled up and deduplicated

        // Both images must be present somewhere in the res-7 response
        var allImages = res7.SelectMany(h => h.Images).ToList();
        Assert.Contains(allImages, img => img.Id == 1);
        Assert.Contains(allImages, img => img.Id == 2);
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_Resolution15_ImageInfoIsPopulated()
    {
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index,
            dateTaken: new DateTime(2026, 2, 25));

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66);

        var image = Assert.Single(result[0].Images);
        Assert.Equal("/images/test_1.jpg", image.FilePath);
        Assert.Equal(new DateTime(2026, 2, 25), image.DateTaken);
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_AnomalyNotes_ArePopulatedInImageDto()
    {
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index,
            anomalyNotes: "crack on panel 3");

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66);

        var image = Assert.Single(result[0].Images);
        Assert.Equal("crack on panel 3", image.AnomalyNotes);
    }

    // -------------------------------------------------------------------------
    // Date filter
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetHexagonsByViewportAsync_StartDate_ExcludesImagesBeforeDate()
    {
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index,
            dateTaken: new DateTime(2025, 1, 15));

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66,
            startDate: new DateOnly(2025, 7, 1));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_StartDate_IncludesImagesAfterDate()
    {
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index,
            dateTaken: new DateTime(2025, 7, 15));

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66,
            startDate: new DateOnly(2025, 6, 1));

        Assert.Single(result);
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_EndDate_ExcludesImagesAfterDate()
    {
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index,
            dateTaken: new DateTime(2025, 7, 15));

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66,
            endDate: new DateOnly(2025, 3, 1));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_BothDates_IncludesImageInRange()
    {
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index,
            dateTaken: new DateTime(2025, 5, 10));

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66,
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));

        Assert.Single(result);
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_BothDates_ExcludesImageOutsideRange()
    {
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index,
            dateTaken: new DateTime(2024, 12, 31));

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66,
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_NoDates_ReturnsAllImages()
    {
        const string cell2 = "8f39100e1a500e6";
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index,
            dateTaken: new DateTime(2024, 1, 1));
        SeedImageWithHexagon(id: 2, lat: 37.0997m, lon: -8.6825m, h3Index: cell2,
            dateTaken: new DateTime(2025, 6, 1));

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66);

        Assert.Equal(2, result.SelectMany(h => h.Images).Count());
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_StartDate_ExcludesImagesWithNullDateTaken()
    {
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index,
            dateTaken: null);

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66,
            startDate: new DateOnly(2025, 1, 1));

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // Anomaly filter
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetHexagonsByViewportAsync_AnomalyFilter_IncludesImageWithMatchingAnomaly()
    {
        SeedImageWithAnomaly(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index,
            anomalies: [Anomalies.Pothole]);

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66,
            anomalies: [Anomalies.Pothole]);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_AnomalyFilter_ExcludesImageWithNonMatchingAnomaly()
    {
        SeedImageWithAnomaly(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index,
            anomalies: [Anomalies.Crack]);

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66,
            anomalies: [Anomalies.Pothole]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_AnomalyFilter_ExcludesImageWithNoAnomalies()
    {
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index);

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66,
            anomalies: [Anomalies.Pothole]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_AnomalyFilter_MultipleTypes_IncludesAnyMatch()
    {
        const string cell2 = "8f39100e1a500e6";
        SeedImageWithAnomaly(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index,
            anomalies: [Anomalies.Pothole]);
        SeedImageWithAnomaly(id: 2, lat: 37.0997m, lon: -8.6825m, h3Index: cell2,
            anomalies: [Anomalies.Crack]);

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66,
            anomalies: [Anomalies.Pothole, Anomalies.Crack]);

        Assert.Equal(2, result.SelectMany(h => h.Images).Count());
    }

    [Fact]
    public async Task GetHexagonsByViewportAsync_NullAnomalyFilter_ReturnsAllImages()
    {
        const string cell2 = "8f39100e1a500e6";
        SeedImageWithHexagon(id: 1, lat: 37.0997m, lon: -8.6827m, h3Index: KnownH3Index);
        SeedImageWithAnomaly(id: 2, lat: 37.0997m, lon: -8.6825m, h3Index: cell2,
            anomalies: [Anomalies.Pothole]);

        var result = await _service.GetHexagonsByViewportAsync(37.09, 37.14, -8.69, -8.66);

        Assert.Equal(2, result.SelectMany(h => h.Images).Count());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SeedImage(int id, decimal? lat, decimal? lon)
    {
        _context.Images.Add(new Image
        {
            Id = id,
            FileName = $"test_{id}.jpg",
            Latitude = lat,
            Longitude = lon
        });
        _context.SaveChanges();
    }

    private void SeedImageWithHexagon(int id, decimal lat, decimal lon, string h3Index,
        DateTime? dateTaken = null, string? anomalyNotes = null)
    {
        var hexagon = new Hexagon { H3Index = h3Index };
        _context.Hexagons.Add(hexagon);
        _context.SaveChanges();

        _context.Images.Add(new Image
        {
            Id = id,
            FileName = $"test_{id}.jpg",
            Latitude = lat,
            Longitude = lon,
            DateTaken = dateTaken,
            AnomalyNotes = anomalyNotes,
            HexagonId = hexagon.Id
        });
        _context.SaveChanges();
    }

    private void SeedImageWithAnomaly(int id, decimal lat, decimal lon, string h3Index, List<Anomalies> anomalies)
    {
        var random = new Random();
        var hexagon = new Hexagon { H3Index = h3Index };
        _context.Hexagons.Add(hexagon);
        _context.SaveChanges();

        _ = _context.Images.Add(new Image
        {
            Id = id,
            FileName = $"test_{id}.jpg",
            Latitude = lat,
            Longitude = lon,
            DateTaken = DateTime.UtcNow,
            AnomalyNotes = null,
            HexagonId = hexagon.Id
        });
        _context.SaveChanges();

        foreach (var anomaly in anomalies)
            _context.RoadVisualAnomalies.Add(new RoadVisualAnomaly
            {
                ImageId = id,
                AnomalyType = anomaly,
                BoxX1 = 156,
                BoxY1 = 143,
                BoxX2 = 210,
                BoxY2 = 190,
                Confidence = (decimal)(random.NextDouble() + 0.1),
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow,
                ResolvedAt = null,
                Notes = JsonDocument.Parse(@"{}")
            });
        _context.SaveChanges();
    }
}
