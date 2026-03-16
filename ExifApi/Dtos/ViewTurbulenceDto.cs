using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class ViewTurbulenceDto
{

    public int Id { get; set; }
    public int Index { get; set; }
    public RoadTurbulenceType RoadTurbulenceType { get; set; }
    public DateTime DateCreated { get; set; }
}
