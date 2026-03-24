using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class RoadTurbulenceViewDto
{

    public int Id { get; set; }
    public int Index { get; set; }
    public RoadTurbulenceType Kind { get; set; }
    public DateTime CreatedDate { get; set; }
}
