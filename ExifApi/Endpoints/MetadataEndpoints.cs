using System;
using ExifApi.Dtos;
using ExifApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExifApi.Endpoints;

public static class MetadataEndpoints
{
    public static void MapMetadataEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/metadata")
            .WithName("Metadata")
            .WithTags("Metadata");
        group.MapGet("/", GetMetadataAll)
            .WithName("GetAllMetadata")
            .WithSummary("Retrieves all images metadata")
            .Produces<IEnumerable<ImageInfoDto>>(StatusCodes.Status200OK);
        group.MapGet("/{fileName}", GetMetadataById)
            .WithName("GetMetadataById")
            .WithSummary("Get image metadata by file name")
            .Produces<ImageInfoDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
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
