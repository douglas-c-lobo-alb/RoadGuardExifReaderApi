using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ExifApi.Data.Entities;

public class Agent
{
    [Key]
    public int Id { get; set; }
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    public JsonDocument? Metadata { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public ICollection<Image> Images { get; set; } = [];
}
