using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ExifApi.Data.Entities;

public class RoadTurbulence
{
    [Key]
    public int Id { get; set; }
    public int HexagonId { get; set; }
    public Hexagon? Hexagon { get; set; }
    public int AgentId { get; set; }
    public Agent? Agent { get; set; }
    [Required]
    [Range(0, 10)]
    public int Index { get; set; } = 0;
    public RoadTurbulenceType Kind { get; set; }
    public JsonDocument? Metadata { get; set; }
    [Required]
    public DateTime CreatedDate { get; set; }
}
