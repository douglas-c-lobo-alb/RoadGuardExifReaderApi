
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ExifApi.Data.Entities;

public class RoadVisualAnomaly
{
    [Key]
    public int Id { get; set; }
    public int? ImageId { get; set; }
    public Image? Image { get; set; }
    public AnomalyType AnomalyType { get; set; }
    public decimal Confidence { get; set; }
    public JsonDocument? Notes { get; set; }
    // this field should be set to true after first (up/down)vote
    // being this true, it's now passive to be later deleted
    public bool AlreadyVoted { get; set; } = false;
    public int UpVote { get; set ;} = 0; // reddit-like
    public int DownVote { get; set ;} = 0;
    public int BoxX1 { get; set; }
    public int BoxY1 { get; set; }
    public int BoxX2 { get; set; }
    public int BoxY2 { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
    // have a deleted field to later filter out Deleted anomalies, before
    // deleting them for real?
    // public bool Deleted { get; set; } = false;
}
