using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class SeedEndpoints
{
    public static void MapSeedEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/seed", SeedDatabase)
            .WithName("SeedDatabase")
            .WithDescription("Seeds the database from wwwroot/images EXIF data. Toggle each layer independently.");
        api.MapPost("/cleardatabase", ClearDatabase)
            .WithName("ClearDatabase")
            .WithDescription("Clears the database and images folder, resets IDs to 1");
    }

    private static async Task<IResult> SeedDatabase(
        SeedService seedService,
        bool withAgent = true,
        bool withSession = true,
        bool withImages = true,
        bool withAnomalies = true,
        bool withTurbulences = true)
    {
        try
        {
            var options = new SeedOptions(withAgent, withSession, withImages, withAnomalies, withTurbulences);
            var result = await seedService.RunAsync(options);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, title: "Seed failed", statusCode: 500);
        }
    }

    private static async Task<IResult> ClearDatabase(SeedService seedService)
    {
        try
        {
            await seedService.ClearDatabaseAsync();
            return Results.Ok();
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, title: "Clear failed", statusCode: 500);
        }
    }
}
