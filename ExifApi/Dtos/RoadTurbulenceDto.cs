using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class RoadTurbulenceDto
{
    public int Id { get; set; }
    public int Index { get; set; }
    public RoadTurbulenceType Kind { get; set; }
    public int HexagonId { get; set; }
    public int? AgentId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
}
