using ExifApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Image> Images { get; set; }
    public DbSet<Hexagon> Hexagons { get; set; }
    public DbSet<RoadVisualAnomaly> RoadVisualAnomalies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Image>()
            .HasOne(i => i.Hexagon)
            .WithMany()
            .HasForeignKey(i => i.HexagonId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RoadVisualAnomaly>()
            .HasOne(r => r.Image)
            .WithMany(i => i.Anomalies)
            .HasForeignKey(r => r.ImageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
