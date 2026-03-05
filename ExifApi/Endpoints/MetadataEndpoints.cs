using System;
using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class MetadataEndpoints
{
    public static void MapMetadataEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/metadata")
            .WithName("Metadata")
            .WithOpenApi();
        group.MapGet("/", GetMetadataAll)
            .WithName("GetAllMetadata")
            .WithDescription("Retrieves all images metadata");
        group.MapGet("/{fileName}", GetMetadataById)
            .WithName("GetMetadataById");
    }
    private static IResult GetMetadataAll(ExifService exifService, string? noSort = "no")
    {
        return Results.Ok(exifService.GetAllImageMetadata(noSort));
    }
    private static IResult GetMetadataById(string fileName, ExifService exifService)
    {
        var result = exifService.GetImageMetadataByFileName(fileName);
        if (result is null) return Results.NotFound();
        return Results.Ok(result);
    }
}
