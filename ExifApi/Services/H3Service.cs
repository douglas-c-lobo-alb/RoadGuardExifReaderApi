using System;
using ExifApi.Data;
using ExifApi.Dtos;

namespace ExifApi.Services;

public class H3Service
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExifService> _logger;
    private readonly IWebHostEnvironment _env;
    public H3Service(ApplicationDbContext context, ILogger<ExifService> logger, IWebHostEnvironment env)
    {
        _context = context;
        _logger = logger;
        _env = env;
    }
    public void ConvertLatLngToH3Cell(IEnumerable<ImageInfoDto> imageInfoDtos)
    {
        // to be implemented
    }
}
