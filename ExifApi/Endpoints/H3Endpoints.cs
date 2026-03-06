using ExifApi.Services;

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
}
