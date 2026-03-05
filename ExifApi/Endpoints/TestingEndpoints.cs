namespace ExifApi.Endpoints;

public static class TestingEndpoints
{
    public static void MapTestingEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/testing")
            .WithName("Testing")
            .WithOpenApi();

        group.MapGet("/h3", () => Results.Ok("hello test"))
            .WithName("TestH3")
            .WithDescription("Stub test endpoint");
    }
}
