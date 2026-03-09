using ExifApi.Dtos;
using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class HexagonEndpoints
{
    public static void MapHexagonEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/hexagons")
            .WithName("Hexagons")
            .WithOpenApi();

        group.MapGet("/", GetAll)
            .WithName("GetAllHexagons")
            .WithDescription("Returns all stored hexagons");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetHexagonById");

        group.MapPost("/", Create)
            .WithName("CreateHexagon")
            .WithDescription("Creates a hexagon linked to an image, derived from coordinates or a direct H3 index");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateHexagon");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteHexagon");
    }

    private static async Task<IResult> GetAll(H3Service h3Service)
        => Results.Ok(await h3Service.GetAllHexagonsAsync());

    private static async Task<IResult> GetById(int id, H3Service h3Service)
    {
        var result = await h3Service.GetHexagonByIdAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Create(CreateHexagonDto dto, H3Service h3Service)
    {
        var result = await h3Service.CreateHexagonAsync(dto);
        return result is null
            ? Results.BadRequest("Failed to create hexagon — verify the ImageId is valid, the image doesn't already have a hexagon, and the H3 input is correct")
            : Results.Created($"/api/hexagons/{result.Id}", result);
    }

    private static async Task<IResult> Update(int id, UpdateHexagonDto dto, H3Service h3Service)
    {
        var result = await h3Service.UpdateHexagonAsync(id, dto);
        if (result is null)
        {
            var exists = await h3Service.GetHexagonByIdAsync(id);
            return exists is null
                ? Results.NotFound()
                : Results.BadRequest("Failed to update hexagon — verify the H3 input is correct");
        }
        return Results.Ok(result);
    }

    private static async Task<IResult> Delete(int id, H3Service h3Service)
    {
        var deleted = await h3Service.DeleteHexagonAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
