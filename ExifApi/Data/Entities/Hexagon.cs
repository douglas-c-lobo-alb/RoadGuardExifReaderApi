using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using H3Standard;

namespace ExifApi.Data.Entities;

public class Hexagon
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string H3Index { get; set; } = string.Empty;
    // Resolution is encoded inside the H3 index -- derived, not stored
    [NotMapped]
    public int Resolution => string.IsNullOrEmpty(H3Index)
        ? -1
        : H3Net.GetResolution(H3Net.StringToH3(H3Index));
    public ICollection<RoadTurbulence> Turbulences { get; set; } = [];
    public ICollection<RoadVisualAnomaly> Anomalies { get; set; } = [];
    public ICollection<Vote> Votes { get; set; } = [];
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
