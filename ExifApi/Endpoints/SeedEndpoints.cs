using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class SeedEndpoints
{
    public static void MapSeedEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/seed", SeedDatabase)
            .WithName("SeedDatabase")
            .WithDescription("Clears the database and re-seeds it from wwwroot/images EXIF data with mock anomalies and turbulences")
            .WithOpenApi();
        api.MapPost("/cleardatabase", ClearDatabase)
            .WithName("ClearDatabase")
            .WithDescription("Clears the database")
            .WithOpenApi();
    }

    private static async Task<IResult> SeedDatabase(SeedService seedService)
    {
        try
        {
            var result = await seedService.RunAsync();
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
