using H3Standard;

namespace ExifApi.Dtos;

public class HexagonDto
{
    public int Id { get; set; }
    public string H3Index { get; set; } = string.Empty;
    public int Resolution { get; set; }
    public double Lat => string.IsNullOrEmpty(H3Index) ? 0 : H3Net.CellToLatLng(H3Net.StringToH3(H3Index)).LatWGS84;
    public double Lon => string.IsNullOrEmpty(H3Index) ? 0 : H3Net.CellToLatLng(H3Net.StringToH3(H3Index)).LngWGS84;
}
