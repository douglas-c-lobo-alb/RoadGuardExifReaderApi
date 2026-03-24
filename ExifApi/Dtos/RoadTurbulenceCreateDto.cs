using System.ComponentModel.DataAnnotations;
using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class RoadTurbulenceCreateDto
{
    [Required]
    [Range(0, 10)]
    public int Index { get; set; }

    [Required]
    public RoadTurbulenceType RoadTurbulenceType { get; set; }

    public int? ImageId { get; set; }
}
