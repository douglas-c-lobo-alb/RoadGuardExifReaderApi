using ExifApi.Dtos;
using ExifApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExifApi.Endpoints;

public static class AgentEndpoints
{
    public static void MapAgentEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/agents")
            .WithName("Agents")
            .WithTags("Agents");

        group.MapGet("/", GetAll)
            .WithName("GetAllAgents")
            .WithSummary("List all agents")
            .Produces<IEnumerable<AgentDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id}", GetById)
            .WithName("GetAgentById")
            .WithSummary("Get an agent by ID")
            .Produces<AgentDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateAgent")
            .WithSummary("Create an agent")
            .Produces<AgentDto>(StatusCodes.Status200OK);

        group.MapPut("/{id}", Update)
            .WithName("UpdateAgent")
            .WithSummary("Update an agent by ID")
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<AgentDto>(StatusCodes.Status200OK);

        group.MapDelete("/{id}", Delete)
            .WithName("DeleteAgent")
            .WithSummary("Delete an agent by ID")
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> GetAll(AgentService agentService)
        => Results.Ok(await agentService.GetAllAsync());

    private static async Task<IResult> GetById(string id, AgentService agentService)
    {
        var result = await agentService.GetByIdAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Create(AgentCreateDto dto, AgentService agentService)
    {
        var result = await agentService.CreateAsync(dto);
        return Results.Created($"/api/agents/{result.Id}", result);
    }

    private static async Task<IResult> Update(string id, AgentCreateDto dto, AgentService agentService)
    {
        var result = await agentService.UpdateAsync(id, dto);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Delete(string id, AgentService agentService)
    {
        var deleted = await agentService.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
