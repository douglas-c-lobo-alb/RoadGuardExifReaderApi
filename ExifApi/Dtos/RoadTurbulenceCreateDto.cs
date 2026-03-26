using System.ComponentModel.DataAnnotations;
using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class RoadTurbulenceCreateDto
{
    [Required]
    [Range(0, 10)]
    public int Index { get; set; }
    [Required]
    public RoadTurbulenceType Kind { get; set; }
    public string? H3Index { get; set; }
    public int? HexagonId { get; set; }
    public int? AgentId { get; set; }
}
