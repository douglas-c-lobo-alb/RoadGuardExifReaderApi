using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

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

public class SeedService(
    ApplicationDbContext db,
    ExifService exifService,
    AgentService agentService,
    SessionService sessionService,
    ImageService imageService,
    RoadTurbulenceService turbulenceService,
    RoadVisualAnomalyService anomalyService,
    IWebHostEnvironment env,
    IConfiguration configuration)
{
    private readonly string _imagesFolder = configuration.GetSection("Image:Path").Value ?? "images";

    private static readonly string[] AgentNames = ["Autocarro", "Camião de Lixo", "Viatura de Prefeitura"];

    private static readonly AnomalyType[] AnomalyTypes =
    [
        AnomalyType.Pothole, AnomalyType.RoadCrack, AnomalyType.MissingRoadSign,
        AnomalyType.WaterLeakage, AnomalyType.RoadObstruction
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

        // Step 1 — Agents
        var agents = new List<AgentDto>();
        if (options.WithAgent)
        {
            foreach (var name in AgentNames)
            {
                var agent = await agentService.CreateAsync(new AgentCreateDto { Name = name });
                if (agent is not null) agents.Add(agent);
            }
        }

        // Step 2 — Sessions (1–2 per agent, 2–8h shifts spread over past 30 days)
        var sessions = new List<SessionDto>();
        if (options.WithSession && agents.Count > 0)
        {
            foreach (var agent in agents)
            {
                int count = 1 + rng.Next(2);
                for (int s = 0; s < count; s++)
                {
                    var startedAt = DateTime.UtcNow
                        .AddDays(-rng.Next(1, 30))
                        .AddHours(-rng.Next(0, 16));
                    var finishedAt = startedAt + TimeSpan.FromHours(2 + rng.NextDouble() * 6);
                    var session = await sessionService.CreateAsync(new SessionCreateDto
                    {
                        AgentId = agent.Id,
                        StartedAt = startedAt,
                        FinishedAt = finishedAt
                    });
                    if (session is not null) sessions.Add(session);
                }
            }
        }

        // Step 3 — Images (registered via ImageService, auto-generates hexagons)
        var registeredImages = new List<(ImageDto Image, int? SessionId)>();
        if (options.WithImages)
        {
            var allMeta = exifService.GetAllImageMetadata().ToList();
            int sessionIdx = 0;

            for (int i = 0; i < allMeta.Count; i++)
            {
                var meta = allMeta[i];
                if (meta.FileName is null) continue;

                var filePath = Path.Combine(env.WebRootPath, _imagesFolder, meta.FileName);
                if (!File.Exists(filePath)) continue;

                int? assignedSessionId = sessions.Count > 0
                    ? sessions[sessionIdx % sessions.Count].Id
                    : null;
                sessionIdx++;

                await using var stream = File.OpenRead(filePath);
                var formFile = new FormFile(stream, 0, stream.Length, "file", meta.FileName);
                var dto = await imageService.RegisterImageAsync(formFile, assignedSessionId);
                if (dto is not null)
                    registeredImages.Add((dto, assignedSessionId));
            }
        }

        var hexagonsCreated = registeredImages
            .Select(x => x.Image.Hexagon?.Id)
            .Where(id => id.HasValue)
            .Distinct()
            .Count();

        // Step 4 — Turbulences
        int turbulencesCreated = 0;
        if (options.WithTurbulences && registeredImages.Count > 0)
        {
            var withHexagon = registeredImages
                .Where(x => x.Image.Hexagon is not null)
                .ToList();

            for (int i = 0; i < withHexagon.Count; i++)
            {
                if (rng.Next(30) != 0) continue;
                var (img, sid) = withHexagon[i];
                int count = rng.Next(1, 3);
                var batch = new List<RoadTurbulenceCreateDto>(count);
                for (int k = 0; k < count; k++)
                {
                    batch.Add(new RoadTurbulenceCreateDto
                    {
                        HexagonId = img.Hexagon!.Id,
                        Index = 1 + ((i + k) % 8),
                        Kind = TurbulenceTypes[(i + k) % TurbulenceTypes.Length],
                        SessionId = sid
                    });
                }
                var created = await turbulenceService.CreateManyAsync(batch);
                turbulencesCreated += created.Count;
            }
        }

        // Step 5 — Anomalies
        int anomaliesCreated = 0;
        if (options.WithAnomalies && registeredImages.Count > 0)
        {
            var withHexagon = registeredImages
                .Where(x => x.Image.Hexagon is not null)
                .ToList();

            for (int i = 0; i < withHexagon.Count; i++)
            {
                if (rng.Next(30) != 0) continue;
                var (img, _) = withHexagon[i];
                int count = rng.Next(1, 4);
                for (int j = 0; j < count; j++)
                {
                    int x1 = 50 + (i % 8) * 100;
                    int y1 = 50 + (j % 4) * 120;
                    var result = await anomalyService.CreateAsync(new RoadVisualAnomalyCreateDto
                    {
                        ImageId = img.Id,
                        Kind = AnomalyTypes[(i * 3 + j) % AnomalyTypes.Length],
                        Confidence = Math.Round((decimal)(0.60 + rng.NextDouble() * 0.39), 2),
                        BoxX1 = x1, BoxY1 = y1, BoxX2 = x1 + 200, BoxY2 = y1 + 150
                    });
                    if (result is not null) anomaliesCreated++;
                }
            }
        }

        return new SeedResult(
            agents.Count,
            sessions.Count,
            registeredImages.Count,
            hexagonsCreated,
            anomaliesCreated,
            turbulencesCreated);
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
        var imagesPath = Path.Combine(env.WebRootPath, _imagesFolder);

        if (!Directory.Exists(imagesPath))
            return Task.FromResult(0);

        var files = Directory.GetFiles(imagesPath);
        foreach (var file in files)
            File.Delete(file);

        return Task.FromResult(files.Length);
    }
}
