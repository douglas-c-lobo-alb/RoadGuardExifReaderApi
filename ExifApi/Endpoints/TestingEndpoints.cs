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

    private static IResult TestH3(double lat, double lng, int resolution, H3Service h3Service, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("TestingEndpoints");
        logger.LogInformation("H3 conversion requested: lat={Lat}, lng={Lng}, resolution={Resolution}", lat, lng, resolution);
        var h3Index = H3Net.LatLngToCell(lat, lng, resolution);
        if (h3Index == 0)
        {
            logger.LogWarning("H3 conversion returned 0 for lat={Lat}, lng={Lng}, resolution={Resolution} — invalid input?", lat, lng, resolution);
            return Results.BadRequest("H3 conversion failed — check lat, lng and resolution (0-15)");
        }
        var h3String = H3Net.H3ToString(h3Index);
        logger.LogInformation("H3 conversion result: {H3Index}", h3String);
        return Results.Ok(new { lat, lng, resolution, h3Index = h3String });
    }
}
