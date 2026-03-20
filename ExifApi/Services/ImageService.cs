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
    private readonly string _imagesFolder;

    public ImageService(ApplicationDbContext context, ExifService exifService, ILogger<ImageService> logger, IWebHostEnvironment env, IConfiguration configuration)
    {
        _context = context;
        _exifService = exifService;
        _logger = logger;
        _env = env;
        _imagesFolder = configuration.GetSection("Image:Path").Value ?? "images";
    }

    public async Task<ImageDto?> RegisterImageAsync(IFormFile file)
    {
        var imagesPath = Path.Combine(_env.WebRootPath, _imagesFolder);
        Directory.CreateDirectory(imagesPath);

        var fileName = Path.GetFileName(file.FileName);

        // Skip if already registered
        var existing = await _context.Images
            .Include(i => i.Hexagon)
            .FirstOrDefaultAsync(i => i.FileName == fileName);
        if (existing is not null)
        {
            _logger.LogInformation("Image {FileName} already registered, skipping", fileName);
            return ToDto(existing);
        }

        var filePath = Path.Combine(imagesPath, fileName);

        await using (var stream = File.Create(filePath))
            await file.CopyToAsync(stream);

        _logger.LogInformation("Saved uploaded file: {FileName}", fileName);

        var metadata = _exifService.ExtractMetadata(filePath);
        if (metadata is null)
            _logger.LogWarning("Could not extract EXIF metadata from {FileName}, registering with null fields", fileName);

        var image = new Image
        {
            FileName = fileName,
            CameraMake = metadata?.CameraMake,
            CameraModel = metadata?.CameraModel,
            DateTaken = metadata?.DateTaken,
            Latitude = metadata?.Latitude,
            Longitude = metadata?.Longitude,
            Altitude = metadata?.Altitude,
            Heading = metadata?.Heading
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
            .Include(i => i.Anomalies)
            .OrderBy(i => i.DateTaken)
            .ToListAsync();
        return images.Select(ToDto).ToList();
    }

    public async Task<ImageDto?> GetByIdAsync(int id)
    {
        var image = await _context.Images
            .Include(i => i.Hexagon)
            .Include(i => i.Anomalies)
            .FirstOrDefaultAsync(i => i.Id == id);
        return image is null ? null : ToDto(image);
    }

    public async Task<ImageDto?> UpdateAsync(int id, UpdateImageDto dto)
    {
        var image = await _context.Images
            .Include(i => i.Hexagon)
            .Include(i => i.Anomalies)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (image is null) return null;

        image.CameraMake = dto.CameraMake;
        image.CameraModel = dto.CameraModel;
        image.DateTaken = dto.DateTaken;
        image.Latitude = dto.Latitude;
        image.Longitude = dto.Longitude;
        image.Altitude = dto.Altitude;
        image.Heading = dto.Heading;
        image.Notes = dto.AnomalyNotes;
        image.LastModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ToDto(image);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var image = await _context.Images.Include(i => i.Hexagon).FirstOrDefaultAsync(i => i.Id == id);
        if (image is null) return false;

        var filePath = Path.Combine(_env.WebRootPath, _imagesFolder, image.FileName);
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
        Heading = image.Heading,
        Turbulence = image.RoadTurbulence?.Index,
        AnomalyNotes = image.Notes,
        AnomalyCount = image.Anomalies.Count,
        Hexagon = image.Hexagon is null ? null : new HexagonDto
        {
            Id = image.Hexagon.Id,
            H3Index = image.Hexagon.H3Index,
            Resolution = image.Hexagon.Resolution
        }
    };
}
