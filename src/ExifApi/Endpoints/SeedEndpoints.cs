using ExifApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExifApi.Endpoints;

public static class SeedEndpoints
{
    public static void MapSeedEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/seed", SeedDatabase)
            .WithName("SeedDatabase")
            .WithTags("Seed")
            .WithSummary("Seeds the database from wwwroot/images EXIF data. Toggle each layer independently.")
            .Produces<SeedResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        api.MapPost("/cleardatabase", ClearDatabase)
            .WithName("ClearDatabase")
            .WithTags("Seed")
            .WithSummary("Clears the database and images folder, resets IDs to 1")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> SeedDatabase(
        SeedService seedService,
        bool withAgent = true,
        bool withSession = true,
        bool withImages = true,
        bool withTurbulences = true,
        bool withAnomalies = true,
        bool withVotes = true)
    {
        try
        {
            var options = new SeedOptions(withAgent, withSession, withImages, withTurbulences, withAnomalies, withVotes);
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
