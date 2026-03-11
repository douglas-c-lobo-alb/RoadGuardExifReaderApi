using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Endpoints;
using ExifApi.Infrastructure;
using ExifApi.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<EnumSchemaFilter>();
});
builder.Services.AddScoped<ExifService>();
builder.Services.AddScoped<H3Service>();
builder.Services.AddScoped<ImageService>();
builder.Services.AddScoped<RoadTurbulenceService>();
builder.Services.AddScoped<SeedService>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
    options.EnableDetailedErrors();
});

var app = builder.Build();

var startupLogger = app.Logger;
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
    startupLogger.LogError("DefaultConnection string is missing from configuration");
else
    startupLogger.LogInformation("Database connection string resolved: {ConnectionString}", connectionString);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/index.html", permanent: false));

app.Use((context, next) =>
{
    context.Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
    {
        NoCache = true,
        NoStore = true
    };
    return next.Invoke();
});

// Apply any pending EF Core migrations at startup (https://stackoverflow.com/a/70057243)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (app.Environment.IsEnvironment("Testing"))
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();
}

var api = app.MapGroup("/api");

Image.SetConfiguration(app.Configuration);

api.MapStatusEndpoints();
api.MapMetadataEndpoints();
api.MapH3Endpoints();
api.MapHexagonEndpoints();
api.MapImageEndpoints();
api.MapRoadTurbulenceEndpoints();
api.MapSeedEndpoints();
api.MapIntrospectiveEndpoints();

app.Run();

public partial class Program { }
