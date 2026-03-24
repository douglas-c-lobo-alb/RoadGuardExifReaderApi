using System;
using ExifApi.Data.Entities;

namespace ExifApi.Dtos;

public class VoteDto
{
    public int Id { get; set; }
    public int? HexagonId { get; set; }
    public int AgentId { get; set; }
    public int? ImageId { get; set; }
    public AnomalyType Kind { get; set; }
    public decimal? Confidence { get; set; }
    public int? BoxX1 { get; set; }
    public int? BoxY1 { get; set; }
    public int? BoxX2 { get; set; }
    public int? BoxY2 { get; set; }
    public DateTime CreatedDate {get; set;}
    public DateTime LastModifiedDate {get; set;}
}
