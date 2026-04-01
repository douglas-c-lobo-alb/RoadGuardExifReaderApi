using System;
using ExifApi.Data.Entities;

namespace ExifApi.Endpoints;

public static class IntrospectiveEndpoints
{
    public static void MapIntrospectiveEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/introspection")
            .WithName("Introspection")
            .WithTags("Introspection")
            .WithDescription("Used to get code-first data");
        group.MapGet("/anomalies", GetAnomalyTypes)
            .WithName("GetAnomalies")
            .WithSummary("Retrieves the list of available anomaly types")
            .Produces<List<string>>(StatusCodes.Status200OK);
        group.MapGet("/turbulences", GetTurbulenceTypes)
            .WithName("GetTurbulences")
            .WithSummary("Retrieves the list of available turbulence types")
            .Produces<List<string>>(StatusCodes.Status200OK);
    }
    private static IResult GetAnomalyTypes()
    {
        return Results.Ok(Enum.GetNames(typeof(AnomalyType)).ToList());
    }
    private static IResult GetTurbulenceTypes()
    {
        return Results.Ok(Enum.GetNames(typeof(RoadTurbulenceType)).ToList());
    }
}
