using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Services;

public class ImageService
{
    private readonly ApplicationDbContext _context;
    private readonly ExifService _exifService;
    private readonly ILogger<ImageService> _logger;
    private readonly IWebHostEnvironment _env;

    public ImageService(ApplicationDbContext context, ExifService exifService, ILogger<ImageService> logger, IWebHostEnvironment env)
    {
        _context = context;
        _exifService = exifService;
        _logger = logger;
        _env = env;
    }

    public async Task<ImageDto?> RegisterImageAsync(IFormFile file)
    {
        var imagesPath = Path.Combine(_env.WebRootPath, "images");
        Directory.CreateDirectory(imagesPath);

        var fileName = Path.GetFileName(file.FileName);
        var filePath = Path.Combine(imagesPath, fileName);

        await using (var stream = File.Create(filePath))
            await file.CopyToAsync(stream);

        _logger.LogInformation("Saved uploaded file: {FileName}", fileName);

        var metadata = _exifService.ExtractMetadata(filePath);
        if (metadata is null)
        {
            _logger.LogWarning("Could not extract metadata from {FileName}, removing file", fileName);
            File.Delete(filePath);
            return null;
        }

        var image = new Image
        {
            FileName = fileName,
            FilePath = "/images/" + fileName,
            CameraMake = metadata.CameraMake,
            CameraModel = metadata.CameraModel,
            DateTaken = metadata.DateTaken,
            Latitude = metadata.Latitude,
            Longitude = metadata.Longitude,
            Altitude = metadata.Altitude
        };

        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Registered image {FileName} with id={Id}", fileName, image.Id);
        return ToDto(image);
    }

    public async Task<List<ImageDto>> GetAllAsync()
    {
        var images = await _context.Images
            .Include(i => i.Hexagon)
            .OrderBy(i => i.DateTaken)
            .ToListAsync();
        return images.Select(ToDto).ToList();
    }

    public async Task<ImageDto?> GetByIdAsync(int id)
    {
        var image = await _context.Images
            .Include(i => i.Hexagon)
            .FirstOrDefaultAsync(i => i.Id == id);
        return image is null ? null : ToDto(image);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var image = await _context.Images.Include(i => i.Hexagon).FirstOrDefaultAsync(i => i.Id == id);
        if (image is null) return false;

        var filePath = Path.Combine(_env.WebRootPath, "images", image.FileName);
        if (File.Exists(filePath)) File.Delete(filePath);

        _context.Images.Remove(image);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted image id={Id}, file={FileName}", id, image.FileName);
        return true;
    }

    private static ImageDto ToDto(Image image) => new()
    {
        Id = image.Id,
        FileName = image.FileName,
        FilePath = image.FilePath,
        CameraMake = image.CameraMake,
        CameraModel = image.CameraModel,
        DateTaken = image.DateTaken,
        Latitude = image.Latitude,
        Longitude = image.Longitude,
        Altitude = image.Altitude,
        Anomaly = image.Anomaly,
        Hexagon = image.Hexagon is null ? null : new HexagonDto
        {
            H3Index = image.Hexagon.H3Index,
            Resolution = image.Hexagon.Resolution,
        }
    };
}
