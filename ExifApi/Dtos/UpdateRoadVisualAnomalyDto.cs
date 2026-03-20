using System.Text.Json;
using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class UpdateRoadVisualAnomalyDto
{
    public AnomalyType AnomalyType { get; set; }
    public decimal Confidence { get; set; }
    public JsonDocument? Notes { get; set; }
    public int BoxX1 { get; set; }
    public int BoxY1 { get; set; }
    public int BoxX2 { get; set; }
    public int BoxY2 { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
