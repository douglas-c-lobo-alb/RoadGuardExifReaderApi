namespace ExifApi.Data.Entities;

public class Session
{
    public int Id { get; set; }
    public required string AgentId { get; set; }
    public Agent Agent { get; set; } = null!;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public ICollection<Image> Images { get; set; } = [];
    public ICollection<RoadTurbulence> RoadTurbulences { get; set; } = [];
    public ICollection<Vote> Votes { get; set; } = [];
}
