using System;
using System.ComponentModel.DataAnnotations;

namespace ExifApi.Data.Entities;

public class RoadTurbulence
{
    [Key]
    public int Id { get; set; }
    [Required]
    [Range(0, 10)]
    public int Index { get; set; } = 0;
    public RoadTurbulenceType RoadTurbulenceType { get; set; } = RoadTurbulenceType.None;
    public int? HexagonId { get; set; }
    public Hexagon? Hexagon { get; set;}
    [Required]
    public DateTime DateCreated { get; set; }
}
