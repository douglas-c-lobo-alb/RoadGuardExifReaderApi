using ExifApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Image> Images { get; set; }
    public DbSet<Hexagon> Hexagons { get; set; }
}
