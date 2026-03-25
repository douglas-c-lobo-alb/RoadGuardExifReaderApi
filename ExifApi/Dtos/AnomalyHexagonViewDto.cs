using H3Standard;

namespace ExifApi.Dtos;

public class AnomalyHexagonViewDto
{
    public string H3Index { get; set; } = string.Empty;
    public int Resolution { get; set; }
    public double Lat => string.IsNullOrEmpty(H3Index) ? 0 : H3Net.CellToLatLng(H3Net.StringToH3(H3Index)).LatWGS84;
    public double Lon => string.IsNullOrEmpty(H3Index) ? 0 : H3Net.CellToLatLng(H3Net.StringToH3(H3Index)).LngWGS84;
    public List<RoadVisualAnomalyViewDto> Anomalies { get; set; } = [];
    public List<RoadTurbulenceViewDto> Turbulences { get; set; } = [];
}
