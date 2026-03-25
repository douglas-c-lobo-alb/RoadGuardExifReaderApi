using System;
using ExifApi.Services;

namespace ExifApi.Endpoints;

public static class MetadataEndpoints
{
    public static void MapMetadataEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/metadata")
            .WithName("Metadata");
        group.MapGet("/", GetMetadataAll)
            .WithName("GetAllMetadata")
            .WithDescription("Retrieves all images metadata");
        group.MapGet("/{fileName}", GetMetadataById)
            .WithName("GetMetadataById");
    }
    private static IResult GetMetadataAll(ExifService exifService)
    {
        return Results.Ok(exifService.GetAllImageMetadata());
    }
    private static IResult GetMetadataById(string fileName, ExifService exifService)
    {
        var result = exifService.GetImageMetadataByFileName(fileName);
        if (result is null) return Results.NotFound();
        return Results.Ok(result);
    }
}
