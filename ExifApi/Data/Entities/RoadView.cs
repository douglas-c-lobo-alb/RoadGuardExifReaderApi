
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Data.Entities;

public class RoadView
{
    [Key]
    public int Id { get; set; }
    public int? ImageId { get; set; }
    public Image? Image { get; set; }
    public int? HexagonId { get; set;}
    public Hexagon? Hexagon { get; set; }
    public decimal Heading { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
}


