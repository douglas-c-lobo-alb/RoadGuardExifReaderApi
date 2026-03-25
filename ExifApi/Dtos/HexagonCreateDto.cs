using System.ComponentModel.DataAnnotations;

namespace ExifApi.Dtos;

public class HexagonCreateDto
{
    [Range(1, int.MaxValue)]
    public int ImageId { get; set; }

    // Option A: derive index from coordinates
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? Resolution { get; set; }

    // Option B: provide index directly
    public string? H3Index { get; set; }
}
