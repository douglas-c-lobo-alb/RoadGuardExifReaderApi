using ExifApi.Data.Entities;
using ExifApi.Dtos;
using ExifApi.Services;
using H3Standard;
using Microsoft.AspNetCore.Mvc;

namespace ExifApi.Endpoints;

public static class H3Endpoints
{
    public static void MapH3Endpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/h3")
            .WithName("H3")
            .WithOpenApi();

        group.MapGet("/cell", GetCell)
            .WithName("GetH3Cell")
            .WithDescription("Converts lat/lon to H3 cell index");

        group.MapGet("/parent", GetParent)
            .WithName("GetH3Parent")
            .WithDescription("Returns the parent cell at a coarser resolution");

        group.MapGet("/children", GetChildren)
            .WithName("GetH3Children")
            .WithDescription("Returns all children cells at a finer resolution");

        group.MapGet("/disk", GetDisk)
            .WithName("GetH3Disk")
            .WithDescription("Returns all cells within k rings of the given cell");

        group.MapPost("/generate", GenerateHexagons)
            .WithName("GenerateHexagons")
            .WithDescription("Generates H3 cells at res 15 for all images missing one");

        group.MapGet("/view", GetViewport)
            .WithName("GetH3Viewport")
            .WithDescription("Returns hexagons linked to images within a lat/lon viewport, aggregated at the requested resolution");

        // ------------------------------------------------------------
        // weird, alien endpoint
        // ------------------------------------------------------------

        group.MapGet("/next-image", GetNextImage)
            .WithName("GetNextImage")
            .WithDescription("Returns next image from a given image id, n: n -> n+1");

        group.MapGet("/prev-image", GetPrevImage)
            .WithName("GetPrevImage")
            .WithDescription("Returns previous image from a given image id, n: n -> n-1");

        group.MapGet("/metadata", GetHexagonImagesMetadata)
            .WithName("GetHexagonImagesMetadata")
            .WithDescription("Returns, if any, all metadata associated to all images within given hexagon");
    }

    private static IResult GetCell(double lat, double lon, int resolution, H3Service h3Service)
    {
        var result = h3Service.LatLngToCell(lat, lon, resolution);
        return result is null
            ? Results.BadRequest("H3 conversion failed — check lat, lon and resolution (0-15)")
            : Results.Ok(result);
    }

    private static IResult GetParent(string index, int resolution, H3Service h3Service)
    {
        var result = h3Service.CellToParent(index, resolution);
        return result is null
            ? Results.BadRequest("Could not get parent — check index and resolution")
            : Results.Ok(result);
    }

    private static IResult GetChildren(string index, int resolution, H3Service h3Service)
        => Results.Ok(h3Service.CellToChildren(index, resolution));

    private static IResult GetDisk(string index, int k, H3Service h3Service)
        => Results.Ok(h3Service.GridDisk(index, k));

    private static async Task<IResult> GenerateHexagons(H3Service h3Service)
    {
        await h3Service.GenerateHexagonsAsync();
        return Results.Ok("Hexagons generated");
    }

    private static async Task<IResult> GetViewport(
        string latMin,
        string latMax,
        string lonMin,
        string lonMax,
        H3Service h3Service,
        [FromQuery] AnomalyType[]? anomalies = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        int resolution = 15)
    {
        // ASP.NET Core joins duplicate query params with commas -- take the first value only
        static bool TryParseFirst(string raw, out double value) =>
            double.TryParse(raw.Split(',')[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value);

        if (!TryParseFirst(latMin, out var latMinD) || !TryParseFirst(latMax, out var latMaxD) ||
            !TryParseFirst(lonMin, out var lonMinD) || !TryParseFirst(lonMax, out var lonMaxD))
            return Results.BadRequest("Invalid coordinate value -- expected decimal numbers (e.g. 37.09)");

        if (latMinD >= latMaxD)
            return Results.BadRequest("latMin must be less than latMax");
        if (lonMinD >= lonMaxD)
            return Results.BadRequest("lonMin must be less than lonMax");
        if (resolution < 0 || resolution > 15)
            return Results.BadRequest("resolution must be between 0 and 15");
        if (startDate > endDate)
            return Results.BadRequest($"startDate ({startDate}) must be before endDate ({endDate})");

        var result = await h3Service.GetHexagonsByViewportAsync(
            latMinD,
            latMaxD,
            lonMinD,
            lonMaxD,
            anomalies?.ToList(),
            startDate,
            endDate,
            resolution);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetNextImage(int id, ImageService imageService)
    {
        var result = await imageService.GetByIdAsync(id + 1);
        return result is null
            ? Results.NotFound($"No image found with id {id + 1}")
            : Results.Ok(result);
    }
    
    private static async Task<IResult> GetPrevImage(int id, ImageService imageService)
    {
        var result = await imageService.GetByIdAsync(id - 1);
        return result is null
            ? Results.NotFound($"No image found with id {id - 1}")
            : Results.Ok(result);
    }

    private static async Task<IResult> GetHexagonImagesMetadata(
        string h3Index,
        H3Service h3Service
        )
    {
        var result = await h3Service.GetHexagonImagesMetadata(h3Index);
        return result.Count == 0
            ? Results.NotFound($"No data")
            : Results.Ok(result);
    }
}
