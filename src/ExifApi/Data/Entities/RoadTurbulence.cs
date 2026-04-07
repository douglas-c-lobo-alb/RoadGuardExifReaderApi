using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Redis.OM.Modeling;

namespace ExifApi.Data.Entities;

[Document]
public class RoadTurbulence
{
    [Key] [RedisIdField]
    public int Id { get; set; }
    [Indexed]
    public int HexagonId { get; set; }
    [Indexed]
    public Hexagon? Hexagon { get; set; }
    public int? SessionId { get; set; }
    public Session? Session { get; set; }
    [Indexed] [Required] [Range(0, 10)]
    public int Index { get; set; } = 0;
    [Indexed]
    public RoadTurbulenceType Kind { get; set; }
    public JsonDocument? Metadata { get; set; }
    [Indexed] [Required] [Indexed]
    public DateTime CreatedDate { get; set; }
    [Indexed]
    public DateTime LastModifiedDate { get; set; }
}
