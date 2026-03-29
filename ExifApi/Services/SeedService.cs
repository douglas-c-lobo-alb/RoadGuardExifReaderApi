using System.Text.Json;
using ExifApi.Data;
using ExifApi.Data.Entities;
using H3Standard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExifApi.Services;

public record SeedOptions(
    bool WithAgent = true,
    bool WithSession = true,
    bool WithImages = true,
    bool WithAnomalies = true,
    bool WithTurbulences = true);

public record SeedResult(
    int AgentsCreated,
    int SessionsCreated,
    int ImagesCreated,
    int HexagonsCreated,
    int AnomaliesCreated,
    int TurbulencesCreated);

public class SeedService(ApplicationDbContext db, ExifService exifService, H3Service h3Service, IWebHostEnvironment env, IConfiguration configuration)
{
    private readonly int _anomalyResolution = configuration.GetValue<int>("H3:AnomalyResolution", 13);
    private const double DefaultLat = 48.8566;
    private const double DefaultLon = 2.3522;

    private static readonly AnomalyType[] AnomalyTypes =
    [
        AnomalyType.Pothole, AnomalyType.RoadCrack, AnomalyType.MissingRoadSign,
        AnomalyType.WaterLeakage, AnomalyType.RoadObstruction
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

    public async Task<SeedResult> RunAsync(SeedOptions options)
    {
        var rng = new Random(42);

        Agent? agent = null;
        if (options.WithAgent)
        {
            agent = new Agent { Name = "SeedAgent", CreatedDate = DateTime.UtcNow, LastModifiedDate = DateTime.UtcNow };
            db.Agents.Add(agent);
            await db.SaveChangesAsync();
        }

        Session? session = null;
        if (options.WithSession && agent is not null)
        {
            session = new Session { AgentId = agent.Id, StartedAt = DateTime.UtcNow };
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
        }

        List<Image> images = [];
        Dictionary<string, Hexagon> hexagonMap = [];
        int[] anomalyHexIds = [];

        if (options.WithImages)
        {
            images = BuildImages(rng);
            (hexagonMap, anomalyHexIds) = await CreateHexagonsAsync(images);
            foreach (var img in images)
                img.SessionId = session?.Id;
            db.Images.AddRange(images);
            await db.SaveChangesAsync();
        }

        List<RoadTurbulence> turbulences = [];
        if (options.WithTurbulences && images.Count > 0)
        {
            turbulences = BuildTurbulences(images, anomalyHexIds, rng);
            foreach (var t in turbulences)
                t.SessionId = session?.Id;
            db.RoadTurbulences.AddRange(turbulences);
            await db.SaveChangesAsync();
        }

        List<RoadVisualAnomaly> anomalies = [];
        if (options.WithAnomalies && images.Count > 0)
        {
            anomalies = BuildAnomalies(images, anomalyHexIds, rng);
            db.RoadVisualAnomalies.AddRange(anomalies);
            await db.SaveChangesAsync();
        }

        return new SeedResult(
            agent is not null ? 1 : 0,
            session is not null ? 1 : 0,
            images.Count,
            hexagonMap.Count,
            anomalies.Count,
            turbulences.Count);
    }

    public async Task ClearDatabaseAsync()
    {
        // Order respects FK constraints: dependents first
        await db.Votes.ExecuteDeleteAsync();
        await db.RoadVisualAnomalies.ExecuteDeleteAsync();
        await db.RoadTurbulences.ExecuteDeleteAsync();
        await db.Images.ExecuteDeleteAsync();
        await db.Hexagons.ExecuteDeleteAsync();
        await db.Sessions.ExecuteDeleteAsync();
        await db.Agents.ExecuteDeleteAsync();

        // Reset auto-increment counters so next inserts start from ID 1
        await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM sqlite_sequence
            WHERE name IN ('Votes', 'RoadVisualAnomalies', 'Images', 'RoadTurbulences', 'Hexagons', 'Sessions', 'Agents');
            """);

        await ClearImagesFolderAsync();
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

    private async Task<(Dictionary<string, Hexagon> appHexMap, int[] anomalyHexIds)> CreateHexagonsAsync(List<Image> images)
    {
        var appHexMap = new Dictionary<string, Hexagon>();
        var anomalyMap = new Dictionary<string, Hexagon>();

        // Pre-compute app-resolution and anomaly-resolution H3 indices for each image in one pass.
        // Anomaly index is derived via CellToParent — identical to what GetHexagonsByViewportAsync does.
        var appIndices = new string?[images.Count];
        var anomalyIndices = new string?[images.Count];
        for (int i = 0; i < images.Count; i++)
        {
            var rawApp = H3Net.LatLngToCell((double)images[i].Latitude!.Value, (double)images[i].Longitude!.Value, 13);
            if (rawApp == 0) continue;
            var raw13 = H3Net.CellToParent(rawApp, _anomalyResolution);
            if (raw13 == 0) continue;
            appIndices[i] = H3Net.H3ToString(rawApp);
            anomalyIndices[i] = H3Net.H3ToString(raw13);
        }

        // Create unique app-resolution hexagons
        for (int i = 0; i < images.Count; i++)
        {
            var idx = appIndices[i];
            if (idx is null || appHexMap.ContainsKey(idx)) continue;
            var hex = new Hexagon { H3Index = idx, CreatedDate = DateTime.UtcNow };
            appHexMap[idx] = hex;
            db.Hexagons.Add(hex);
        }

        // Create unique anomaly hexagons
        for (int i = 0; i < images.Count; i++)
        {
            var idx = anomalyIndices[i];
            if (idx is null || anomalyMap.ContainsKey(idx)) continue;
            var hex = new Hexagon { H3Index = idx, CreatedDate = DateTime.UtcNow };
            anomalyMap[idx] = hex;
            db.Hexagons.Add(hex);
        }

        // Save so PKs are assigned
        await db.SaveChangesAsync();

        // Link images to their app-resolution hexagon and build anomalyHexIds in one pass
        var anomalyHexIds = new int[images.Count];
        for (int i = 0; i < images.Count; i++)
        {
            if (appIndices[i] is not null && appHexMap.TryGetValue(appIndices[i]!, out var appHex))
                images[i].HexagonId = appHex.Id;

            if (anomalyIndices[i] is not null && anomalyMap.TryGetValue(anomalyIndices[i]!, out var anomalyHex))
                anomalyHexIds[i] = anomalyHex.Id;
        }

        return (appHexMap, anomalyHexIds);
    }

    private static JsonDocument BuildImageNotes(Random rng, int i) =>
        JsonDocument.Parse($$"""
        {
            "severity": "{{NoteSeverities[i % NoteSeverities.Length]}}",
            "description": "{{NoteDescriptions[i % NoteDescriptions.Length]}}",
            "confidence": {{Math.Round(0.60 + rng.NextDouble() * 0.39, 2)}}
        }
        """);

    private static List<RoadVisualAnomaly> BuildAnomalies(List<Image> images, int[] anomalyHexIds, Random rng)
    {
        var anomalies = new List<RoadVisualAnomaly>();

        for (int i = 0; i < images.Count; i++)
        {
            if (rng.Next(30) != 0) continue;
            if (anomalyHexIds[i] == 0) continue;
            int count = rng.Next(1, 4);
            for (int j = 0; j < count; j++)
            {
                int x1 = 50 + (i % 8) * 100;
                int y1 = 50 + (j % 4) * 120;
                anomalies.Add(new RoadVisualAnomaly
                {
                    HexagonId        = anomalyHexIds[i],
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

    private static List<RoadTurbulence> BuildTurbulences(List<Image> images, int[] anomalyHexIds, Random rng)
    {
        var turbulences = new List<RoadTurbulence>();

        for (int i = 0; i < images.Count; i++)
        {
            if (rng.Next(30) != 0) continue;
            if (anomalyHexIds[i] == 0) continue;
            int count = rng.Next(1, 3);
            for (int k = 0; k < count; k++)
            {
                turbulences.Add(new RoadTurbulence
                {
                    HexagonId          = anomalyHexIds[i],
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
