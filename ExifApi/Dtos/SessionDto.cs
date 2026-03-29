namespace ExifApi.Dtos;

public class SessionDto
{
    public int Id { get; set; }
    public int AgentId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
