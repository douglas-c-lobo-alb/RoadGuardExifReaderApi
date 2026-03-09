namespace ExifApi.Dtos;

public class CreateHexagonDto
{
    public int ImageId { get; set; }

    // Option A: derive index from coordinates
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Resolution { get; set; }

    // Option B: provide index directly
    public string? H3Index { get; set; }
}
