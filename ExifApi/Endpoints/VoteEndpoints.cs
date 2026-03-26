using ExifApi.Dtos;
using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class VoteEndpoints
{
    public static void MapVoteEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/votes")
            .WithName("Votes");

        group.MapGet("/anomaly", GetAll)
            .WithName("GetAllVotes");

        group.MapGet("/anomaly/{id:int}", GetById)
            .WithName("GetVoteById");

        group.MapPost("/anomaly", Create)
            .WithName("CreateVote");

        group.MapDelete("/anomaly/{id:int}", Delete)
            .WithName("DeleteVote");

        group.MapPost("/compute", Compute)
            .WithName("ComputeVotes")
            .WithDescription("Promotes qualifying vote groups to anomalies, then deletes all votes");
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
}
