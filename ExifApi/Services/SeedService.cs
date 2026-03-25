using System.Text.Json;
using ExifApi.Data;
using ExifApi.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExifApi.Services;

public record SeedResult(
    int ImagesCreated,
    int HexagonsCreated,
    int AnomaliesCreated,
    int TurbulencesCreated);

public class SeedService(ApplicationDbContext db, ExifService exifService, H3Service h3Service, IWebHostEnvironment env, IConfiguration configuration)
{
    private const double DefaultLat = 48.8566;
    private const double DefaultLon = 2.3522;

    private static readonly AnomalyType[] AnomalyTypes =
    [
        AnomalyType.Pothole, AnomalyType.Crack, AnomalyType.MissingRoadSign,
        AnomalyType.WaterLeakage, AnomalyType.AnimalCorpse
    ];

    private static readonly string[] NoteSeverities = ["low", "medium", "high"];
    private static readonly string[] NoteDescriptions =
    [
        "Surface cracking observed", "Pothole detected near edge",
        "Water pooling on road surface", "Road markings faded",
        "Longitudinal crack along centre line", "Bump causes vehicle swerve"
    ];

    private static readonly RoadTurbulenceType[] TurbulenceTypes =
    [
        RoadTurbulenceType.Pothole,
        RoadTurbulenceType.Speedbump,
        RoadTurbulenceType.LongitudinalCrack,
        RoadTurbulenceType.TransverseCrack,
        RoadTurbulenceType.Depression,
        RoadTurbulenceType.AbruptSwerving,
        RoadTurbulenceType.WaterLeakage,
    ];

    public async Task<SeedResult> RunAsync(bool withAnomalies = true, bool withTurbulences = true)
    {
        await ClearDatabaseAsync();

        var rng = new Random(42);
        var images = BuildImages(rng);
        var hexagonMap = await CreateHexagonsAsync(images);

        db.Images.AddRange(images);
        await db.SaveChangesAsync();

        List<RoadTurbulence> turbulences = [];
        if (withTurbulences)
        {
            turbulences = BuildTurbulences(images, rng);
            db.RoadTurbulences.AddRange(turbulences);
            await db.SaveChangesAsync();
        }

        List<RoadVisualAnomaly> anomalies = [];
        if (withAnomalies)
        {
            anomalies = BuildAnomalies(images, rng);
            db.RoadVisualAnomalies.AddRange(anomalies);
            await db.SaveChangesAsync();
        }

        return new SeedResult(images.Count, hexagonMap.Count, anomalies.Count, turbulences.Count);
    }

    public async Task ClearDatabaseAsync()
    {
        // Order respects FK constraints: dependents first
        await db.Votes.ExecuteDeleteAsync();
        await db.RoadVisualAnomalies.ExecuteDeleteAsync();
        await db.RoadTurbulences.ExecuteDeleteAsync();
        await db.Images.ExecuteDeleteAsync();
        await db.Hexagons.ExecuteDeleteAsync();

        // Reset auto-increment counters so next inserts start from ID 1
        await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM sqlite_sequence
            WHERE name IN ('Votes', 'RoadVisualAnomalies', 'Images', 'RoadTurbulences', 'Hexagons');
            """);
    }

    public Task<int> ClearImagesFolderAsync()
    {
        var folder = configuration.GetSection("Image:Path").Value ?? "images";
        var imagesPath = Path.Combine(env.WebRootPath, folder);

        if (!Directory.Exists(imagesPath))
            return Task.FromResult(0);

        var files = Directory.GetFiles(imagesPath);
        foreach (var file in files)
            File.Delete(file);

        return Task.FromResult(files.Length);
    }

    private List<Image> BuildImages(Random rng)
    {
        var allMeta = exifService.GetAllImageMetadata().ToList();
        var images = new List<Image>(allMeta.Count);

        for (int i = 0; i < allMeta.Count; i++)
        {
            var meta = allMeta[i];
            bool hasGps = meta.Latitude.HasValue && meta.Longitude.HasValue;
            images.Add(new Image
            {
                FileName         = meta.FileName ?? $"unknown_{i}.jpg",
                Latitude         = hasGps ? meta.Latitude  : (decimal)(DefaultLat + i * 0.0001),
                Longitude        = hasGps ? meta.Longitude : (decimal)(DefaultLon + i * 0.0001),
                Altitude         = meta.Altitude,
                CameraMake       = meta.CameraMake,
                CameraModel      = meta.CameraModel,
                Heading          = meta.Heading ?? Math.Round((decimal)(rng.NextDouble() * 360), 2),
                Metadata         = BuildImageNotes(rng, i),
                DateTaken        = meta.DateTaken ?? DateTime.UtcNow.AddDays(-rng.Next(1, 365)),
                CreatedDate      = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            });
        }

        return images;
    }

    private async Task<Dictionary<string, Hexagon>> CreateHexagonsAsync(List<Image> images)
    {
        var hexagonMap = new Dictionary<string, Hexagon>();

        // Build unique hexagons from image coordinates
        foreach (var image in images)
        {
            var dto = h3Service.LatLngToCell(
                (double)image.Latitude!.Value,
                (double)image.Longitude!.Value,
                resolution: 15);

            if (dto is null || hexagonMap.ContainsKey(dto.H3Index)) continue;

            var hexagon = new Hexagon { H3Index = dto.H3Index, CreatedDate = DateTime.UtcNow };
            hexagonMap[dto.H3Index] = hexagon;
            db.Hexagons.Add(hexagon);
        }

        // Save first so PKs are assigned before we link images
        await db.SaveChangesAsync();

        // Link images via FK only — avoids EF tracking images through navigation properties
        foreach (var image in images)
        {
            var dto = h3Service.LatLngToCell(
                (double)image.Latitude!.Value,
                (double)image.Longitude!.Value,
                resolution: 15);

            if (dto is not null && hexagonMap.TryGetValue(dto.H3Index, out var hexagon))
                image.HexagonId = hexagon.Id;
        }

        return hexagonMap;
    }

    private static JsonDocument BuildImageNotes(Random rng, int i) =>
        JsonDocument.Parse($$"""
        {
            "severity": "{{NoteSeverities[i % NoteSeverities.Length]}}",
            "description": "{{NoteDescriptions[i % NoteDescriptions.Length]}}",
            "confidence": {{Math.Round(0.60 + rng.NextDouble() * 0.39, 2)}}
        }
        """);

    private static List<RoadVisualAnomaly> BuildAnomalies(List<Image> images, Random rng)
    {
        var anomalies = new List<RoadVisualAnomaly>();

        for (int i = 0; i < images.Count; i++)
        {
            if (rng.Next(30) != 0) continue;
            int count = rng.Next(1, 4);
            for (int j = 0; j < count; j++)
            {
                int x1 = 50 + (i % 8) * 100;
                int y1 = 50 + (j % 4) * 120;
                if (images[i].HexagonId is null) continue;
                anomalies.Add(new RoadVisualAnomaly
                {
                    HexagonId        = images[i].HexagonId!.Value,
                    ImageId          = images[i].Id,
                    Kind             = AnomalyTypes[(i * 3 + j) % AnomalyTypes.Length],
                    Confidence       = Math.Round((decimal)(0.60 + rng.NextDouble() * 0.39), 2),
                    BoxX1 = x1, BoxY1 = y1, BoxX2 = x1 + 200, BoxY2 = y1 + 150,
                    CreatedDate      = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                });
            }
        }

        return anomalies;
    }

    private static List<RoadTurbulence> BuildTurbulences(List<Image> images, Random rng)
    {
        var turbulences = new List<RoadTurbulence>();

        for (int i = 0; i < images.Count; i++)
        {
            if (rng.Next(30) != 0) continue;
            int count = rng.Next(1, 3);
            for (int k = 0; k < count; k++)
            {
                if (images[i].HexagonId is null) continue;
                turbulences.Add(new RoadTurbulence
                {
                    HexagonId          = images[i].HexagonId!.Value,
                    Index              = 1 + ((i + k) % 8),
                    Kind               = TurbulenceTypes[(i + k) % TurbulenceTypes.Length],
                    CreatedDate        = DateTime.UtcNow.AddDays(-rng.Next(0, 60)),
                    LastModifiedDate   = DateTime.UtcNow
                });
            }
        }

        return turbulences;
    }
}
