using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/images")
            .WithName("Images")
            .WithOpenApi();

        group.MapGet("/", GetAll)
            .WithName("GetAllImages")
            .WithDescription("[Backoffice usage only intented] Returns all registered images with their hexagon");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetImageById");

        group.MapPost("/", Upload)
            .WithName("UploadImage")
            .WithDescription("[Backoffice usage only intented] Uploads an image, extracts EXIF metadata and registers it in the database")
            .DisableAntiforgery();

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteImage")
            .WithDescription("[Backoffice usage only intented] Deletes an image");
    }

    private static async Task<IResult> GetAll(ImageService imageService)
        => Results.Ok(await imageService.GetAllAsync());

    private static async Task<IResult> GetById(int id, ImageService imageService)
    {
        var result = await imageService.GetByIdAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Upload(IFormFile file, ImageService imageService)
    {
        var result = await imageService.RegisterImageAsync(file);
        return result is null
            ? Results.BadRequest("Failed to register image")
            : Results.Created($"/api/images/{result.Id}", result);
    }

    private static async Task<IResult> Delete(int id, ImageService imageService)
    {
        var deleted = await imageService.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
