using System.Text.Json;
using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class RoadVisualAnomalyCreateDto
{
    public int ImageId { get; set; }
    public AnomalyType AnomalyType { get; set; }
    public decimal Confidence { get; set; }
    public JsonDocument? Notes { get; set; }
    public int BoxX1 { get; set; }
    public int BoxY1 { get; set; }
    public int BoxX2 { get; set; }
    public int BoxY2 { get; set; }
}
