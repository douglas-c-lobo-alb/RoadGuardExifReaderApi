using ExifApi.Dtos;
using ExifApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExifApi.Endpoints;

public static class HexagonEndpoints
{
    public static void MapHexagonEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/hexagons")
            .WithName("Hexagons")
            .WithTags("Hexagons");

        group.MapGet("/", GetAll)
            .WithName("GetAllHexagons")
            .WithSummary("[Backoffice usage only intended] Returns all stored hexagons")
            .Produces<List<HexagonDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", GetById)
            .WithName("GetHexagonById")
            .WithSummary("Get a hexagon by ID")
            .Produces<HexagonDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateHexagon")
            .WithSummary("[Backoffice usage only intended] Creates a hexagon linked to an image, derived from coordinates or a direct H3 index")
            .Produces<HexagonDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateHexagon")
            .WithSummary("[Backoffice usage only intended] Updates a hexagon")
            .Produces<HexagonDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteHexagon")
            .WithSummary("[Backoffice usage only intended] Deletes a hexagon")
            .Produces<ProblemDetails>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAll(H3Service h3Service)
        => Results.Ok(await h3Service.GetAllHexagonsAsync());

    private static async Task<IResult> GetById(int id, H3Service h3Service)
    {
        var result = await h3Service.GetHexagonByIdAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Create(HexagonCreateDto dto, H3Service h3Service)
    {
        var result = await h3Service.CreateHexagonAsync(dto);
        return result is null
            ? Results.BadRequest("Failed to create hexagon -- verify the ImageId is valid, the image doesn't already have a hexagon, and the H3 input is correct")
            : Results.Created($"/api/hexagons/{result.Id}", result);
    }

    private static async Task<IResult> Update(int id, HexagonUpdateDto dto, H3Service h3Service)
    {
        var result = await h3Service.UpdateHexagonAsync(id, dto);
        if (result is null)
        {
            var exists = await h3Service.GetHexagonByIdAsync(id);
            return exists is null
                ? Results.NotFound()
                : Results.BadRequest("Failed to update hexagon -- verify the H3 input is correct");
        }
        return Results.Ok(result);
    }

    private static async Task<IResult> Delete(int id, H3Service h3Service)
    {
        var deleted = await h3Service.DeleteHexagonAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
