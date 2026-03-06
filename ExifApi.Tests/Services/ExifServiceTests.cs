using ExifApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ExifApi.Tests.Services;

public class ExifServiceTests
{
    // -------------------------------------------------------------------------
    // GetImageMetadataByFileName
    // -------------------------------------------------------------------------

    [Fact]
    public void GetImageMetadataByFileName_FileNotFound_ReturnsNull()
    {
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
        var service = new ExifService(NullLogger<ExifService>.Instance, mockEnv.Object);

        var result = service.GetImageMetadataByFileName("nonexistent_xyz_12345.jpg");

        Assert.Null(result);
    }

    [Fact]
    public void GetImageMetadataByFileName_WebRootPathEmpty_ReturnsNull()
    {
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(string.Empty);
        var service = new ExifService(NullLogger<ExifService>.Instance, mockEnv.Object);

        var result = service.GetImageMetadataByFileName("any.jpg");

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // GetAllImageMetadata
    // -------------------------------------------------------------------------

    [Fact]
    public void GetAllImageMetadata_WebRootPathEmpty_ReturnsEmpty()
    {
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(string.Empty);
        var service = new ExifService(NullLogger<ExifService>.Instance, mockEnv.Object);

        var result = service.GetAllImageMetadata();

        Assert.Empty(result);
    }

    [Fact]
    public void GetAllImageMetadata_ImagesFolderDoesNotExist_ReturnsEmpty()
    {
        var nonExistentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(nonExistentRoot);
        var service = new ExifService(NullLogger<ExifService>.Instance, mockEnv.Object);

        var result = service.GetAllImageMetadata();

        Assert.Empty(result);
    }
}
