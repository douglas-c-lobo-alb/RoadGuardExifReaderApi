using ExifApi.Dtos;
using ExifApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExifApi.Endpoints;

public static class VoteEndpoints
{
    public static void MapVoteEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/votes")
            .WithName("Votes")
            .WithTags("Votes");

        group.MapGet("/anomaly", GetAll)
            .WithName("GetAllVotes")
            .WithSummary("List all votes")
            .Produces<List<VoteDto>>(StatusCodes.Status200OK);

        group.MapGet("/anomaly/{id:int}", GetById)
            .WithName("GetVoteById")
            .WithSummary("Get a vote by ID")
            .Produces<VoteDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/anomaly", Create)
            .WithName("CreateVote")
            .WithSummary("Create a vote")
            .Produces<VoteDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/anomaly/{id:int}", Delete)
            .WithName("DeleteVote")
            .WithSummary("Delete a vote by ID")
            .Produces<ProblemDetails>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/compute", Compute)
            .WithName("ComputeVotes")
            .WithSummary("Promotes qualifying vote groups to anomalies, then deletes all votes")
            .Produces<ComputeResultDto>(StatusCodes.Status200OK);

        group.MapPost("/compute-dry-run", ComputeDryRun)
            .WithName("ComputeDryRun")
            .WithSummary("Dry run the compute endpoint to visualize what WILL be done")
            .Produces<ComputeResultDto>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetAll(VoteService svc)
        => Results.Ok(await svc.GetAllAsync());

    private static async Task<IResult> GetById(int id, VoteService svc)
    {
        var result = await svc.GetByIdAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Create(VoteCreateDto dto, VoteService svc)
    {
        var result = await svc.CreateAsync(dto);
        return result is null
            ? Results.BadRequest("Must provide HexagonId, Latitude+Longitude, or a valid ImageId with an assigned hexagon")
            : Results.Created($"/api/votes/anomaly/{result.Id}", result);
    }

    private static async Task<IResult> Delete(int id, VoteService svc)
    {
        var deleted = await svc.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> Compute(VoteService svc)
        => Results.Ok(await svc.ComputeAsync());

    private static async Task<IResult> ComputeDryRun(VoteService svc)
        => Results.Ok(await svc.ComputableAsync());
}
