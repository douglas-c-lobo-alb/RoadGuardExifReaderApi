namespace ExifApi.Dtos;

public class HexagonDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public int Resolution { get; set; }
    public string H3Index { get; set; } = string.Empty;
}
