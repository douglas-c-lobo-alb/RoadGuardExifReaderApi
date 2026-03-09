using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Data.Entities;

public class RoadTurbulence
{
    [Key]
    public int Id { get; set; }
    public DateTime? DateTaken { get; set; }
    public int Turbulence { get; set; } = 0;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
    public bool HasSpeedBump { get; set; }
    public bool HasPothole { get; set; }
}
