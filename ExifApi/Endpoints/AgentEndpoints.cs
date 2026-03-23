using ExifApi.Dtos;
using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class AgentEndpoints
{
    public static void MapAgentEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/agents")
            .WithName("Agents")
            .WithOpenApi();

        group.MapGet("/", GetAll)
            .WithName("GetAllAgents");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetAgentById");

        group.MapPost("/", Create)
            .WithName("CreateAgent");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteAgent");
    }

    private static async Task<IResult> GetAll(AgentService agentService)
        => Results.Ok(await agentService.GetAllAsync());

    private static async Task<IResult> GetById(int id, AgentService agentService)
    {
        var result = await agentService.GetByIdAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Create(CreateAgentDto dto, AgentService agentService)
    {
        var result = await agentService.CreateAsync(dto);
        return Results.Created($"/api/agents/{result.Id}", result);
    }

    private static async Task<IResult> Delete(int id, AgentService agentService)
    {
        var deleted = await agentService.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
