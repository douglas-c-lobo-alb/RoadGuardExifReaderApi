using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class SeedEndpoints
{
    public static void MapSeedEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/seed", SeedDatabase)
            .WithName("SeedDatabase")
            .WithDescription("Clears the database and re-seeds it from wwwroot/images EXIF data with mock anomalies and turbulences");
        api.MapPost("/cleardatabase", ClearDatabase)
            .WithName("ClearDatabase")
            .WithDescription("Clears the database");
        api.MapPost("/clearimages", ClearImages)
            .WithName("ClearImages")
            .WithDescription("Deletes all files in the wwwroot/images folder");
    }

    private static async Task<IResult> SeedDatabase(
        SeedService seedService,
        bool withAnomalies = true,
        bool withTurbulences = true)
    {
        try
        {
            var result = await seedService.RunAsync(withAnomalies, withTurbulences);
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

    private static async Task<IResult> ClearImages(SeedService seedService)
    {
        try
        {
            var count = await seedService.ClearImagesFolderAsync();
            return Results.Ok(new { FilesDeleted = count });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, title: "Clear images failed", statusCode: 500);
        }
    }
}
