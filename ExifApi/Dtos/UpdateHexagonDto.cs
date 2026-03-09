namespace ExifApi.Dtos;

public class UpdateHexagonDto
{
    // Option A: recompute index from new coordinates
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Resolution { get; set; }

    // Option B: replace index directly
    public string? H3Index { get; set; }
}
