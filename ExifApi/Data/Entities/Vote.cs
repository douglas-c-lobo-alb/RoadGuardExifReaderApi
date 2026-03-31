using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Redis.OM.Modeling;

namespace ExifApi.Data.Entities;

[Document]
public class Vote
{
    [Key] [RedisIdField]
    public int Id { get; set; }
    [Indexed]
    public int HexagonId { get; set; }
    [Indexed]
    public int? SessionId { get; set; }
    public Session? Session { get; set; }
    [Indexed]
    public int? ImageId { get; set; }
    public Image? Image { get; set; }
    [Indexed]
    public AnomalyType Kind { get; set; }
    [Searchable]
    public decimal? Confidence { get; set; }
    public JsonDocument? Metadata { get; set; }
    [Indexed]
    public int BoxX1 { get; set; }
    [Indexed]
    public int BoxY1 { get; set; }
    [Indexed]
    public int BoxX2 { get; set; }
    [Indexed]
    public int BoxY2 { get; set; }
    [Indexed]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    [Indexed]
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
}
