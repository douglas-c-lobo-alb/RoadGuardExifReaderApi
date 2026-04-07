using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using H3Standard;
using Microsoft.EntityFrameworkCore;
using Redis.OM.Modeling;

namespace ExifApi.Data.Entities;

[Document]
public class Hexagon
{
    [Key] [RedisIdField]
    public int Id { get; set; }
    [Required] [Searchable]
    public string H3Index { get; set; } = string.Empty;
    // Resolution is encoded inside the H3 index -- derived, not stored
    [NotMapped] [Indexed(Sortable = true)]
    public int Resolution => string.IsNullOrEmpty(H3Index)
        ? -1
        : H3Net.GetResolution(H3Net.StringToH3(H3Index));
    [Indexed]
    public ICollection<RoadTurbulence> Turbulences { get; set; } = [];
    [Indexed]
    public ICollection<RoadVisualAnomaly> Anomalies { get; set; } = [];
    [Indexed]
    public ICollection<Vote> Votes { get; set; } = [];
    [Indexed]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
