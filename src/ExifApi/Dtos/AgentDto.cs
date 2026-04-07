using System.Text.Json;

namespace ExifApi.Dtos;

public class AgentDto
{
    public required string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public JsonDocument? Metadata { get; set; }
    public DateTime CreatedDate { get; set; }
}
