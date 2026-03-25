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
    public DbSet<Vote> Votes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            .HasOne(r => r.Hexagon)
            .WithMany(h => h.Anomalies)
            .HasForeignKey(r => r.HexagonId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RoadVisualAnomaly>()
            .HasOne(r => r.Image)
            .WithMany(i => i.Anomalies)
            .HasForeignKey(r => r.ImageId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RoadTurbulence>()
            .HasOne(r => r.Hexagon)
            .WithMany(t => t.Turbulences)
            .HasForeignKey(t => t.HexagonId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RoadTurbulence>()
            .HasOne(t => t.Agent)
            .WithMany()
            .HasForeignKey(t => t.AgentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Vote>()
            .HasOne(v => v.Hexagon)
            .WithMany()
            .HasForeignKey(v => v.HexagonId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vote>()
            .HasOne(v => v.Agent)
            .WithMany()
            .HasForeignKey(v => v.AgentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Vote>()
            .HasOne(v => v.Image)
            .WithMany()
            .HasForeignKey(v => v.ImageId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Hexagon>()
            .HasIndex(h => h.H3Index)
            .IsUnique();

        modelBuilder.Entity<Image>()
            .Property(i => i.Metadata)
            .HasConversion(
                v => v == null ? null : v.RootElement.GetRawText(),
                v => v == null ? null : JsonDocument.Parse(v, default))
            .HasColumnType("TEXT");

        modelBuilder.Entity<RoadVisualAnomaly>()
            .Property(r => r.Metadata)
            .HasConversion(
                v => v == null ? null : v.RootElement.GetRawText(),
                v => v == null ? null : JsonDocument.Parse(v, default))
            .HasColumnType("TEXT");

        modelBuilder.Entity<RoadTurbulence>()
            .Property(t => t.Metadata)
            .HasConversion(
                v => v == null ? null : v.RootElement.GetRawText(),
                v => v == null ? null : JsonDocument.Parse(v, default))
            .HasColumnType("TEXT");

        modelBuilder.Entity<Agent>()
            .Property(a => a.Metadata)
            .HasConversion(
                v => v == null ? null : v.RootElement.GetRawText(),
                v => v == null ? null : JsonDocument.Parse(v, default))
            .HasColumnType("TEXT");

        modelBuilder.Entity<Vote>()
            .Property(v => v.Metadata)
            .HasConversion(
                v => v == null ? null : v.RootElement.GetRawText(),
                v => v == null ? null : JsonDocument.Parse(v, default))
            .HasColumnType("TEXT");
    }
}
