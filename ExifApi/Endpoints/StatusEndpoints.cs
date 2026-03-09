namespace ExifApi.Endpoints;

public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/status")
            .WithName("Status")
            .WithOpenApi();
        group.MapGet("/", () => Results.Ok(new
        {
            status = "ok",
            app = "RoadGuard ExifApi",
            version = "v0.4.0",
            runtime = $".NET {Environment.Version}",
            utc = DateTime.UtcNow
        }))
        .WithName("GetStatus")
        .WithDescription("Returns basic API health and version info");
    }
}
