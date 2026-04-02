using System.ComponentModel.DataAnnotations;

namespace ExifApi.Dtos;

public class SessionCreateDto
{
    [Required]
    public required string AgentId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
}
