using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class RoadTurbulenceDto
{
    public int Id { get; set; }
    public int Index { get; set; }
    public RoadTurbulenceType RoadTurbulenceType { get; set; }
    public int? HexagonId { get; set; }
    public HexagonDto? Hexagon { get; set; }
    public DateTime DateCreated { get; set; }
}
