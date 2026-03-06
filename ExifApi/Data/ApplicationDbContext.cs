using ExifApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Image> Images { get; set; }
    public DbSet<Hexagon> Hexagons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Image>()
            .OwnsOne(i => i.Anomaly, b => b.ToJson("Anomaly"));
    }
}
