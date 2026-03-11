using System;
using ExifApi.Data.Entities;

namespace ExifApi.Endpoints;

public static class IntrospectiveEndpoints
{
    public static void MapIntrospectiveEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/introspection")
            .WithName("Introspection")
            .WithDescription("Used to get code-first data")
            .WithOpenApi();
        group.MapGet("/anomalies", GetAnomalyTypes)
        .WithName("GetAnomalies")
        .WithDescription("Retrieves the list of available anomaly types");
    }
    private static IResult GetAnomalyTypes()
    {
        return Results.Ok(Enum.GetNames(typeof(AnomalyType)).ToList());
    }
}
