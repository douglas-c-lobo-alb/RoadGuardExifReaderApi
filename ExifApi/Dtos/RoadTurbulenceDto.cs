using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class RoadTurbulenceDto
{
    public int Id { get; set; }
    public int Index { get; set; }
    public RoadTurbulenceType RoadTurbulenceType { get; set; }
    public int? ImageId { get; set; }
    public DateTime DateCreated { get; set; }
}
