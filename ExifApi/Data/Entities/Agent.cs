using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ExifApi.Data.Entities;

public class Agent
{
    [Key] [MaxLength(12)] [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required string Id { get; set; }
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    public JsonDocument? Metadata { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
    public ICollection<Session> Sessions { get; set; } = [];
}
