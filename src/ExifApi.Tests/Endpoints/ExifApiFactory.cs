using System.Text.Json;
using System.Text.Json.Serialization;
using ExifApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ExifApi.Tests.Endpoints;

/// <summary>
/// Custom WebApplicationFactory that replaces the real SQLite database with an
/// in-memory SQLite connection for endpoint integration tests.
/// </summary>
public class ExifApiFactory : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SqliteConnection _connection;
    public readonly string TempRoot;

    public ExifApiFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        TempRoot = Path.Combine(Path.GetTempPath(), $"ExifApiTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(TempRoot);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseWebRoot(TempRoot);
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_connection)
                       .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
        });
    }

    /// <summary>Returns a DbContext backed by the shared in-memory connection.</summary>
    public ApplicationDbContext CreateDbContext()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        var ctx = new ApplicationDbContext(opts);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
            if (Directory.Exists(TempRoot))
                Directory.Delete(TempRoot, recursive: true);
        }
        base.Dispose(disposing);
    }
}
