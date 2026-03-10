using System;
using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class RoadTurbulenceEndpoints
{
    public static void MapRoadTurbulenceEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/turbulence")
            .WithName("Turbulence")
            .WithOpenApi();
        group.MapGet("/", GetTurbulence)
            .WithName("GetAllTurbulence")
            .WithDescription("Retrieves all turbulence data");
    }
    private static IResult GetTurbulence(RoadTurbulenceService roadTurbulenceService)
    {
        return Results.Ok();
    }
}
