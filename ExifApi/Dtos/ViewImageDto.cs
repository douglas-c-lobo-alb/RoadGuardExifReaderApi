using System.Text.Json;
using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class ViewImageDto
{
    public int Id { get; set; }
    public string? FilePath { get; set; }
    public DateTime? DateTaken { get; set; }
    public JsonDocument? AnomalyNotes { get; set; }
    public int? Turbulence { get; set; }
    public RoadTurbulenceDto? roadTurbulenceDto { get; set; }
    public ICollection<RoadVisualAnomaly>? Anomalies { get; set; }
}
