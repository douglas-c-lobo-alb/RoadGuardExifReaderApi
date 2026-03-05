using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class H3Endpoints
{
    public static void MapH3Endpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/h3")
            .WithName("H3")
            .WithOpenApi();

        group.MapGet("/cell", GetCell)
            .WithName("GetH3Cell")
            .WithDescription("Converts lat/lng coordinates to an H3 cell index");
    }

    private static IResult GetCell(double lat, double lng, int resolution, H3Service h3Service)
    {
        var result = h3Service.LatLngToCell(lat, lng, resolution);
        if (result is null)
            return Results.BadRequest("H3 conversion failed - check lat, lng and resolution (0-15)");

        return Results.Ok(result);
    }
}
