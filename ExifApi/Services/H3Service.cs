using System;
using ExifApi.Data;
using ExifApi.Dtos;

namespace ExifApi.Services;

public class H3Service
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<H3Service> _logger;
    private readonly IWebHostEnvironment _env;
    public H3Service(ApplicationDbContext context, ILogger<H3Service> logger, IWebHostEnvironment env)
    {
        _context = context;
        _logger = logger;
        _env = env;
    }
    public void ConvertLatLngToH3Cell(IEnumerable<ImageInfoDto> imageInfoDtos)
    {
        _logger.LogWarning("ConvertLatLngToH3Cell called but not yet implemented");
    }
}
