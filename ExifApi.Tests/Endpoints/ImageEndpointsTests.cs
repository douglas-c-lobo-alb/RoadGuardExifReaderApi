using System.Net;
using System.Net.Http.Json;
using System.Text;
using ExifApi.Data.Entities;
using ExifApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ExifApi.Tests.Endpoints;

public class ImageEndpointsTests : IDisposable
{
    private readonly ExifApiFactory _factory;
    private readonly HttpClient _client;

    public ImageEndpointsTests()
    {
        _factory = new ExifApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // -------------------------------------------------------------------------
    // GET /api/images/
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_EmptyDb_Returns200AndEmptyArray()
    {
        var response = await _client.GetAsync("api/images/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ImageDto>>();
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public async Task GetAll_WithImages_Returns200AndList()
    {
        using var ctx = _factory.CreateDbContext();
        ctx.Images.AddRange(
            new Image { FileName = "img_a.jpg" },
            new Image { FileName = "img_b.jpg" });
        await ctx.SaveChangesAsync();

        var response = await _client.GetAsync("api/images/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ImageDto>>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
    }

    // -------------------------------------------------------------------------
    // GET /api/images/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingImage_Returns200WithDto()
    {
        var image = new Image { FileName = "photo.jpg", CameraMake = "Sony" };
        using var ctx = _factory.CreateDbContext();
        ctx.Images.Add(image);
        await ctx.SaveChangesAsync();
        int id = image.Id;

        var response = await _client.GetAsync($"api/images/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ImageDto>();
        Assert.NotNull(dto);
        Assert.Equal(id, dto.Id);
        Assert.Equal("photo.jpg", dto.FileName);
        Assert.Equal("Sony", dto.CameraMake);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var response = await _client.GetAsync("api/images/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // POST /api/images/  (upload)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Upload_WithBinaryFile_Returns201AndDto()
    {
        // Send any binary — no EXIF data, but the image is still registered
        var content = new MultipartFormDataContent();
        var fileBytes = Encoding.UTF8.GetBytes("not-a-real-jpeg");
        content.Add(new ByteArrayContent(fileBytes), "file", "test.jpg");

        var response = await _client.PostAsync("api/images/", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ImageDto>();
        Assert.NotNull(dto);
        Assert.Equal("test.jpg", dto.FileName);
    }

    [Fact]
    public async Task Upload_SameFileTwice_Returns201WithSameId()
    {
        var content1 = new MultipartFormDataContent();
        content1.Add(new ByteArrayContent(new byte[] { 0xFF, 0xD8 }), "file", "dup.jpg");
        var first = await _client.PostAsync("api/images/", content1);
        var dto1 = await first.Content.ReadFromJsonAsync<ImageDto>();

        var content2 = new MultipartFormDataContent();
        content2.Add(new ByteArrayContent(new byte[] { 0xFF, 0xD8 }), "file", "dup.jpg");
        var second = await _client.PostAsync("api/images/", content2);
        var dto2 = await second.Content.ReadFromJsonAsync<ImageDto>();

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(dto1!.Id, dto2!.Id);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/images/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingImage_Returns204()
    {
        var image = new Image { FileName = "to_delete.jpg" };
        using var ctx = _factory.CreateDbContext();
        ctx.Images.Add(image);
        await ctx.SaveChangesAsync();
        int id = image.Id;

        var response = await _client.DeleteAsync($"api/images/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verify = _factory.CreateDbContext();
        Assert.Null(await verify.Images.FindAsync(id));
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync("api/images/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // POST /api/images/ — sessionId form field
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Upload_WithValidSessionId_Returns201AndDtoWithSessionId()
    {
        using var ctx = _factory.CreateDbContext();
        var agent = new Agent { Name = "Device-01" };
        ctx.Agents.Add(agent);
        await ctx.SaveChangesAsync();
        var session = new Session { AgentId = agent.Id, StartedAt = DateTime.UtcNow };
        ctx.Sessions.Add(session);
        await ctx.SaveChangesAsync();
        int sessionId = session.Id;

        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("fake-jpeg")), "file", "session_photo.jpg");
        content.Add(new StringContent(sessionId.ToString()), "sessionId");

        var response = await _client.PostAsync("api/images/", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ImageDto>();
        Assert.NotNull(dto);
        Assert.Equal(sessionId, dto.SessionId);
    }

    [Fact]
    public async Task Upload_WithInvalidSessionId_Returns400()
    {
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("fake-jpeg")), "file", "orphan_photo.jpg");
        content.Add(new StringContent("99999"), "sessionId");

        var response = await _client.PostAsync("api/images/", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithoutSessionId_Returns201()
    {
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("fake-jpeg")), "file", "no_session.jpg");

        var response = await _client.PostAsync("api/images/", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ImageDto>();
        Assert.NotNull(dto);
        Assert.Null(dto.SessionId);
    }
}
