using System.Text.Json;
using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class ImageViewDto
{
    public int Id { get; set; }
    public string? FilePath { get; set; }
    public DateTime? DateTaken { get; set; }
    public JsonDocument? Metadata { get; set; }
}
