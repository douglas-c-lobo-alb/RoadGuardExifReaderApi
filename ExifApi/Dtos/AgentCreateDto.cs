using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ExifApi.Dtos;

public class AgentCreateDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public JsonDocument? Metadata { get; set; }
}
