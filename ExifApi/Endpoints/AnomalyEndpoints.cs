using ExifApi.Dtos;
using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class AnomalyEndpoints
{
    public static void MapAnomalyEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/anomalies")
            .WithName("Anomalies")
            .WithOpenApi();

        group.MapGet("/", GetAll)
            .WithName("GetAllAnomalies")
            .WithDescription("Retrieves all road visual anomaly records");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetAnomalyById");

        group.MapPost("/", Create)
            .WithName("CreateAnomaly")
            .WithDescription("Creates a new road visual anomaly associated with an image");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateAnomaly");

        group.MapPost("/{id:int}/upvote", Upvote)
            .WithName("UpvoteAnomaly");

        group.MapPost("/{id:int}/downvote", Downvote)
            .WithName("DownvoteAnomaly");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteAnomaly");
    }

    private static async Task<IResult> Create(CreateRoadVisualAnomalyDto dto, RoadVisualAnomalyService svc)
    {
        var result = await svc.CreateAsync(dto);
        return result is null
            ? Results.NotFound($"Image {dto.ImageId} not found")
            : Results.Created($"/api/anomalies/{result.Id}", result);
    }

    private static async Task<IResult> GetAll(RoadVisualAnomalyService svc)
        => Results.Ok(await svc.GetAllAsync());

    private static async Task<IResult> GetById(int id, RoadVisualAnomalyService svc)
    {
        var result = await svc.GetByIdAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Update(int id, UpdateRoadVisualAnomalyDto dto, RoadVisualAnomalyService svc)
    {
        var result = await svc.UpdateAsync(id, dto);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Upvote(int id, RoadVisualAnomalyService svc)
    {
        var result = await svc.UpvoteAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Downvote(int id, RoadVisualAnomalyService svc)
    {
        var result = await svc.DownvoteAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Delete(int id, RoadVisualAnomalyService svc)
    {
        var deleted = await svc.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
