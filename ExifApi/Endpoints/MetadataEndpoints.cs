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
        group.MapGet("/{id}", GetMetadataById)
            .WithName("GetMetadataById");
    }
    private static IResult GetMetadataAll(ExifService exifService)
    {
        return Results.Ok(exifService.GetAllImageMetadata());
    }
    private static IResult GetMetadataById(int id, ExifService exifService)
    {
        // to be implemented
        return Results.Problem();
    }
}
