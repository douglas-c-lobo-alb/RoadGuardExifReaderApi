using ExifApi.Dtos;
using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class RoadTurbulenceEndpoints
{
    public static void MapRoadTurbulenceEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/turbulences")
            .WithName("Turbulence");

        group.MapGet("/", GetAll)
            .WithName("GetAllTurbulence")
            .WithDescription("[API usage only intent] Retrieves all road turbulence records");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetTurbulenceById");

        group.MapGet("/h3/{h3Index}", GetByH3Index)
            .WithName("GetTurbulenceByH3Index")
            .WithDescription("[API usage only intent] Retrieves road turbulence records for a given H3 index");

        group.MapPost("/", Create)
            .WithName("CreateTurbulence")
            .WithDescription("[API usage only intent] Creates one or more road turbulence records atomically");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateTurbulence");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteTurbulence");
    }

    private static async Task<IResult> GetAll(RoadTurbulenceService svc)
        => Results.Ok(await svc.GetAllAsync());

    private static async Task<IResult> GetById(int id, RoadTurbulenceService svc)
    {
        var result = await svc.GetByIdAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetByH3Index(string h3Index, RoadTurbulenceService svc)
        => Results.Ok(await svc.GetByH3IndexAsync(h3Index));

    private static async Task<IResult> Create(List<RoadTurbulenceCreateDto> dtos, RoadTurbulenceService svc)
    {
        if (dtos is null || dtos.Count == 0)
            return Results.BadRequest("At least one turbulence record is required.");

        var created = await svc.CreateManyAsync(dtos);
        return Results.Created("/api/turbulences/", created);
    }

    private static async Task<IResult> Update(int id, RoadTurbulenceCreateDto dto, RoadTurbulenceService svc)
    {
        var result = await svc.UpdateAsync(id, dto);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Delete(int id, RoadTurbulenceService svc)
    {
        var deleted = await svc.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}

