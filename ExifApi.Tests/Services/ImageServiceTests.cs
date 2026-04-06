using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Infrastructure.Caching;
using ExifApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ExifApi.Tests.Services;

public class ImageServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly ImageService _service;
    private readonly string _tempRoot;

    public ImageServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _tempRoot = Path.Combine(Path.GetTempPath(), $"ExifApiTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempRoot);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(_tempRoot);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Image:Path"] = "images" })
            .Build();

        var mockCacheInvalidator = new Mock<IViewportCacheInvalidator>();
        var exifService = new ExifService(NullLogger<ExifService>.Instance, mockEnv.Object);
        var anomalyService = new RoadVisualAnomalyService(_context, NullLogger<RoadVisualAnomalyService>.Instance, config, mockCacheInvalidator.Object);
        var h3Service = new H3Service(_context, NullLogger<H3Service>.Instance, config, new Mock<IDistributedCache>().Object);
        _service = new ImageService(_context, exifService, anomalyService, h3Service, NullLogger<ImageService>.Instance, mockEnv.Object, config, mockCacheInvalidator.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
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

    [Fact]
    public async Task GetAllAsync_ReturnsImages_OrderedByDateTaken()
    {
        SeedImage(id: 1, dateTaken: new DateTime(2025, 6, 1));
        SeedImage(id: 2, dateTaken: new DateTime(2024, 1, 1));
        SeedImage(id: 3, dateTaken: new DateTime(2026, 3, 1));

        var result = await _service.GetAllAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);
        Assert.Equal(3, result[2].Id);
    }

    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsCorrectDto()
    {
        SeedImage(id: 1);

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("/images/test_1.jpg", result.FilePath);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_MissingId_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrueAndRemovesFromDb()
    {
        SeedImage(id: 1);

        var success = await _service.DeleteAsync(1);

        Assert.True(success);
        Assert.Null(await _context.Images.FindAsync(1));
    }

    // -------------------------------------------------------------------------
    // RegisterImageAsync — duplicate skip
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RegisterImageAsync_DuplicateFileName_SkipsRegistrationAndReturnsExisting()
    {
        SeedImage(id: 1, fileName: "photo.jpg");

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("photo.jpg");

        var result = await _service.RegisterImageAsync(mockFile.Object);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(1, await _context.Images.CountAsync());
    }

    // -------------------------------------------------------------------------
    // RegisterImageAsync — sessionId
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RegisterImageAsync_WithValidSessionId_SetsSessionId()
    {
        _context.Agents.Add(new ExifApi.Data.Entities.Agent { Id = 10, Name = "Device-A" });
        await _context.SaveChangesAsync();
        _context.Sessions.Add(new ExifApi.Data.Entities.Session { Id = 1, AgentId = 10, StartedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var fileBytes = System.Text.Encoding.UTF8.GetBytes("fake");
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("img_session.jpg");
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
            .Returns(Task.CompletedTask);

        var result = await _service.RegisterImageAsync(mockFile.Object, sessionId: 1);

        Assert.NotNull(result);
        Assert.Equal(1, result.SessionId);
    }

    [Fact]
    public async Task RegisterImageAsync_WithInvalidSessionId_ReturnsNull()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("img_bad.jpg");

        var result = await _service.RegisterImageAsync(mockFile.Object, sessionId: 9999);

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SeedImage(int id, string? fileName = null, DateTime? dateTaken = null)
    {
        _context.Images.Add(new Image
        {
            Id = id,
            FileName = fileName ?? $"test_{id}.jpg",
            DateTaken = dateTaken
        });
        _context.SaveChanges();
    }
}
