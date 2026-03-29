using ExifApi.Dtos;
using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/sessions")
            .WithName("Sessions");

        group.MapGet("/", GetAll)
            .WithName("GetAllSessions");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetSessionById");

        group.MapPost("/", Create)
            .WithName("CreateSession");

        group.MapPost("/{id:int}/finish", Finish)
            .WithName("FinishSession")
            .WithDescription("Marks a session as finished by setting FinishedAt to now");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteSession");
    }

    private static async Task<IResult> GetAll(SessionService sessionService)
        => Results.Ok(await sessionService.GetAllAsync());

    private static async Task<IResult> GetById(int id, SessionService sessionService)
    {
        var result = await sessionService.GetByIdAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Create(SessionCreateDto dto, SessionService sessionService)
    {
        var result = await sessionService.CreateAsync(dto);
        return result is null
            ? Results.BadRequest($"Agent with id={dto.AgentId} not found")
            : Results.Created($"/api/sessions/{result.Id}", result);
    }

    private static async Task<IResult> Finish(int id, SessionService sessionService)
    {
        var result = await sessionService.FinishAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Delete(int id, SessionService sessionService)
    {
        var deleted = await sessionService.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
