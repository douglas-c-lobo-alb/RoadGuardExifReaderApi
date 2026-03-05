using ExifApi.Services;
using H3Standard;

namespace ExifApi.Endpoints;

public static class TestingEndpoints
{
    public static void MapTestingEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/testing")
            .WithName("Testing")
            .WithOpenApi();

        group.MapGet("/h3", TestH3)
            .WithName("TestH3")
            .WithDescription("Tests H3 cell conversion from lat/lng");
    }

    private static IResult TestH3(double lat, double lng, int resolution, H3Service h3Service)
    {
        var h3Index = H3Net.LatLngToCell(lat, lng, resolution);
        var h3String = H3Net.H3ToString(h3Index);
        return Results.Ok(new { lat, lng, resolution, h3Index = h3String });
    }
}
