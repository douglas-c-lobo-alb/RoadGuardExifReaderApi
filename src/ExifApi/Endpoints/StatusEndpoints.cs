namespace ExifApi.Endpoints;

public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this RouteGroupBuilder api, IConfiguration configuration)
    {
        RouteGroupBuilder group = api.MapGroup("/status")
            .WithName("Status")
            .WithTags("Status");
        group.MapGet("/", () => Results.Ok(new
        {
            app = "RoadGuard ExifApi",
            version = $"v{configuration.GetValue<string>("Release:Version")}",
            runtime = $".NET {Environment.Version}",
            utc = DateTime.UtcNow
        }))
        .WithName("GetStatus")
        .WithSummary("Returns basic API health and version info")
        .Produces(StatusCodes.Status200OK);
    }
}
