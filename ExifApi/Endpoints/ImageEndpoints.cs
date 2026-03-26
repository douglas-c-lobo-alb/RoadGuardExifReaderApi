using ExifApi.Dtos;
using ExifApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExifApi.Endpoints;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/images")
            .WithName("Images");

        group.MapGet("/", GetAll)
            .WithName("GetAllImages")
            .WithDescription("[Backoffice usage only intended] Returns all registered images with their hexagon");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetImageById");

        group.MapPost("/", Upload)
            .WithName("UploadImage")
            .WithDescription("[Backoffice usage only intended] Uploads an image, extracts EXIF metadata and registers it in the database")
            .DisableAntiforgery();

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateImage")
            .WithDescription("[Backoffice usage only intended] Updates image metadata");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteImage")
            .WithDescription("[Backoffice usage only intended] Deletes an image");

        group.MapGet("/{id:int}/anomalies", GetAnomalies)
            .WithName("GetAnomaliesByImageId")
            .WithDescription("Returns all anomalies associated with the given image");
    }

    private static async Task<IResult> GetAll(ImageService imageService)
        => Results.Ok(await imageService.GetAllAsync());

    private static async Task<IResult> GetById(int id, ImageService imageService)
    {
        var result = await imageService.GetByIdAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Upload(IFormFile file, [FromForm] int? agentId, ImageService imageService)
    {
        var result = await imageService.RegisterImageAsync(file, agentId);
        if (result is null)
            return agentId.HasValue
                ? Results.BadRequest($"Agent with id={agentId} not found")
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
