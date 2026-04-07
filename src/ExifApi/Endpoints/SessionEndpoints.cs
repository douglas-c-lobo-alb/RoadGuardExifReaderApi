using ExifApi.Dtos;
using ExifApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExifApi.Endpoints;

public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/sessions")
            .WithName("Sessions")
            .WithTags("Sessions");

        group.MapGet("/", GetAll)
            .WithName("GetAllSessions")
            .WithSummary("List all sessions")
            .Produces<List<SessionDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", GetById)
            .WithName("GetSessionById")
            .WithSummary("Get a session by ID")
            .Produces<SessionDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateSession")
            .WithSummary("Create a session")
            .Produces<SessionDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/{id:int}/finish", Finish)
            .WithName("FinishSession")
            .WithSummary("Marks a session as finished by setting FinishedAt to now")
            .Produces<SessionDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteSession")
            .WithSummary("Delete a session by ID")
            .Produces<ProblemDetails>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
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
