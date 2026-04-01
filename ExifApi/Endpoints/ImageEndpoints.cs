using ExifApi.Dtos;
using ExifApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExifApi.Endpoints;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/images")
            .WithName("Images")
            .WithTags("Images");

        group.MapGet("/", GetAll)
            .WithName("GetAllImages")
            .WithSummary("[Backoffice usage only intended] Returns all registered images with their hexagon")
            .Produces<List<ImageDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", GetById)
            .WithName("GetImageById")
            .WithSummary("Get an image by ID")
            .Produces<ImageDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", Upload)
            .WithName("UploadImage")
            .WithSummary("[Backoffice usage only intended] Uploads an image, extracts EXIF metadata and registers it in the database")
            .Produces<ImageDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .DisableAntiforgery();

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateImage")
            .WithSummary("[Backoffice usage only intended] Updates image metadata")
            .Produces<ImageDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteImage")
            .WithSummary("[Backoffice usage only intended] Deletes an image")
            .Produces<ProblemDetails>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("/{id:int}/anomalies", GetAnomalies)
            .WithName("GetAnomaliesByImageId")
            .WithSummary("Returns all anomalies associated with the given image")
            .Produces<List<RoadVisualAnomalyDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetAll(ImageService imageService)
        => Results.Ok(await imageService.GetAllAsync());

    private static async Task<IResult> GetById(int id, ImageService imageService)
    {
        var result = await imageService.GetByIdAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Upload(IFormFile file, [FromForm] int? sessionId, ImageService imageService)
    {
        var result = await imageService.RegisterImageAsync(file, sessionId);
        if (result is null)
            return sessionId.HasValue
                ? Results.BadRequest($"Session with id={sessionId} not found")
                : Results.BadRequest("Failed to register image");

        return Results.Created($"/api/images/{result.Id}", result);
    }

    private static async Task<IResult> Update(int id, ImageUpdateDto dto, ImageService imageService)
    {
        var result = await imageService.UpdateAsync(id, dto);
        if (result is null)
        {
            var exists = await imageService.GetByIdAsync(id);
            return exists is null ? Results.NotFound() : Results.BadRequest("Failed to update image");
        }
        return Results.Ok(result);
    }

    private static async Task<IResult> Delete(int id, ImageService imageService)
    {
        var deleted = await imageService.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> GetAnomalies(int id, RoadVisualAnomalyService anomalyService)
        => Results.Ok(await anomalyService.GetAllByImageIdAsync(id));
}
