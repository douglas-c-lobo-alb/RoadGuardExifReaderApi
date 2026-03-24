using System.Text.Json;
using ExifApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Agent> Agents { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<Hexagon> Hexagons { get; set; }
    public DbSet<RoadVisualAnomaly> RoadVisualAnomalies { get; set; }
    public DbSet<RoadTurbulence> RoadTurbulences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agent>()
            .Property(a => a.Metadata)
            .HasConversion(
                v => v == null ? null : v.RootElement.GetRawText(),
                v => v == null ? null : JsonDocument.Parse(v, default))
            .HasColumnType("TEXT");

        modelBuilder.Entity<Image>()
            .HasOne(i => i.Agent)
            .WithMany(a => a.Images)
            .HasForeignKey(i => i.AgentId)
            .OnDelete(DeleteBehavior.SetNull);

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

        modelBuilder.Entity<Image>()
            .Property(i => i.Metadata)
            .HasConversion(
                v => v == null ? null : v.RootElement.GetRawText(),
                v => v == null ? null : JsonDocument.Parse(v, default))
            .HasColumnType("TEXT");

        modelBuilder.Entity<RoadVisualAnomaly>()
            .Property(r => r.Notes)
            .HasConversion(
                v => v == null ? null : v.RootElement.GetRawText(),
                v => v == null ? null : JsonDocument.Parse(v, default))
            .HasColumnType("TEXT");

        modelBuilder.Entity<RoadTurbulence>()
            .HasOne(t => t.Image)
            .WithMany(i => i.Turbulences)
            .HasForeignKey(t => t.ImageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
