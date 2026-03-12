using ExifApi.Data;
using ExifApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Services;

public record SeedResult(
    int ImagesCreated,
    int HexagonsCreated,
    int AnomaliesCreated,
    int TurbulencesCreated);

public class SeedService(ApplicationDbContext db, ExifService exifService, H3Service h3Service)
{
    private const double DefaultLat = 48.8566;
    private const double DefaultLon = 2.3522;

    private static readonly AnomalyType[] AnomalyTypes =
    [
        AnomalyType.Pothole, AnomalyType.Crack, AnomalyType.MissingRoadSign,
        AnomalyType.WaterLeakage, AnomalyType.AnimalCorpse, AnomalyType.None
    ];

    private static readonly RoadTurbulenceType[] TurbulenceTypes =
    [
        RoadTurbulenceType.None,
        RoadTurbulenceType.Pothole,
        RoadTurbulenceType.Speedbump,
        RoadTurbulenceType.LongitudinalCrack,
        RoadTurbulenceType.TransverseCrack,
        RoadTurbulenceType.Depression,
        RoadTurbulenceType.AbruptSwerving,
        RoadTurbulenceType.WaterLeakage,
        RoadTurbulenceType.Pothole | RoadTurbulenceType.Depression,
        RoadTurbulenceType.LongitudinalCrack | RoadTurbulenceType.WaterLeakage,
        RoadTurbulenceType.TransverseCrack | RoadTurbulenceType.WaterLeakage,
        RoadTurbulenceType.Depression | RoadTurbulenceType.WaterLeakage | RoadTurbulenceType.TransverseCrack,
        RoadTurbulenceType.Pothole | RoadTurbulenceType.WaterLeakage,
    ];

    public async Task<SeedResult> RunAsync()
    {
        await ClearDatabaseAsync();

        var rng = new Random(42);
        var images = BuildImages(rng);
        var hexagonMap = await CreateHexagonsAsync(images);

        db.Images.AddRange(images);
        await db.SaveChangesAsync(); // assigns all PKs

        var anomalies = BuildAnomalies(images, rng);
        db.RoadVisualAnomalies.AddRange(anomalies);

        var turbulences = BuildTurbulences(hexagonMap, rng);
        db.RoadTurbulences.AddRange(turbulences);

        await db.SaveChangesAsync();

        return new SeedResult(images.Count, hexagonMap.Count, anomalies.Count, turbulences.Count);
    }

    private async Task ClearDatabaseAsync()
    {
        // Order respects FK constraints: dependents first
        await db.RoadVisualAnomalies.ExecuteDeleteAsync();
        await db.RoadTurbulences.ExecuteDeleteAsync();
        await db.Images.ExecuteDeleteAsync();
        await db.Hexagons.ExecuteDeleteAsync();
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

    private static List<RoadVisualAnomaly> BuildAnomalies(List<Image> images, Random rng)
    {
        var anomalies = new List<RoadVisualAnomaly>();

        for (int i = 0; i < images.Count; i++)
        {
            int count = rng.Next(1, 4);
            for (int j = 0; j < count; j++)
            {
                int x1 = 50 + (i % 8) * 100;
                int y1 = 50 + (j % 4) * 120;
                anomalies.Add(new RoadVisualAnomaly
                {
                    ImageId          = images[i].Id,
                    AnomalyType      = AnomalyTypes[(i * 3 + j) % AnomalyTypes.Length],
                    Confidence       = Math.Round((decimal)(0.60 + rng.NextDouble() * 0.39), 2),
                    BoxX1 = x1, BoxY1 = y1, BoxX2 = x1 + 200, BoxY2 = y1 + 150,
                    CreatedDate      = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                });
            }
        }

        return anomalies;
    }

    private static List<RoadTurbulence> BuildTurbulences(Dictionary<string, Hexagon> hexagonMap, Random rng)
    {
        var turbulences = new List<RoadTurbulence>();
        int hexIdx = 0;

        foreach (var hexagon in hexagonMap.Values)
        {
            int count = rng.Next(1, 3);
            for (int k = 0; k < count; k++)
            {
                turbulences.Add(new RoadTurbulence
                {
                    HexagonId          = hexagon.Id,
                    Index              = 1 + ((hexIdx + k) % 8),
                    RoadTurbulenceType = TurbulenceTypes[(hexIdx + k) % TurbulenceTypes.Length],
                    DateCreated        = DateTime.UtcNow.AddDays(-rng.Next(0, 60))
                });
            }
            hexIdx++;
        }

        return turbulences;
    }
}
