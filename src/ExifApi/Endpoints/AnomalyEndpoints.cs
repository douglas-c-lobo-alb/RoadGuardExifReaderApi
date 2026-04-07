using ExifApi.Dtos;
using ExifApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExifApi.Endpoints;

public static class AnomalyEndpoints
{
    public static void MapAnomalyEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/anomalies")
            .WithName("Anomalies")
            .WithTags("Anomalies");

        group.MapGet("/", GetAll)
            .WithName("GetAllAnomalies")
            .WithSummary("Get all anomalies")
            .Produces<RoadVisualAnomalyDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:int}", GetById)
            .WithName("GetAnomalyById")
            .WithSummary("Get an anomay by ID")
            .Produces<RoadVisualAnomalyDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateAnomaly")
            .WithSummary("Creates an anomaly")
            .Produces<RoadVisualAnomalyDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateAnomaly")
            .WithSummary("Update an anomly by ID")
            .Produces<RoadVisualAnomalyDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteAnomaly")
            .WithSummary("Delete an anomaly by ID")
            .Produces<ProblemDetails>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Create(RoadVisualAnomalyCreateDto dto, RoadVisualAnomalyService svc)
    {
        var result = await svc.CreateAsync(dto);
        return result is null
            ? Results.BadRequest("Must provide HexagonId, ImageId, or Latitude+Longitude")
            : Results.Created($"/api/anomalies/{result.Id}", result);
    }

    private static async Task<IResult> GetAll(RoadVisualAnomalyService svc)
        => Results.Ok(await svc.GetAllAsync());

    private static async Task<IResult> GetById(int id, RoadVisualAnomalyService svc)
    {
        var result = await svc.GetByIdAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Update(int id, RoadVisualAnomalyUpdateDto dto, RoadVisualAnomalyService svc)
    {
        var result = await svc.UpdateAsync(id, dto);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Delete(int id, RoadVisualAnomalyService svc)
    {
        var deleted = await svc.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
