using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Endpoints;
using ExifApi.Infrastructure;
using ExifApi.Infrastructure.Caching;
using ExifApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Redis.OM;
using StackExchange.Redis;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<RoadVisualAnomalyService>();
builder.Services.AddScoped<H3Service>();
builder.Services.AddScoped<ImageService>();
builder.Services.AddScoped<AgentService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<RoadTurbulenceService>();
builder.Services.AddScoped<SeedService>();
builder.Services.AddScoped<VoteService>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableDetailedErrors();
});

var redisConfig = builder.Configuration.GetSection("Redis").Get<RedisConfig>();
var multiplexer = ConnectionMultiplexer.Connect(redisConfig!.MultiplexerConfiguration);
builder.Services.AddSingleton(new RedisConnectionProvider(redisConfig!.Configuration));
builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(multiplexer);
});
builder.Services.AddSingleton<IViewportCacheInvalidator, ViewportCacheInvalidator>();
builder.Services.AddHostedService<RedisIndexCreationService>();

var app = builder.Build();

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
    context.Response.GetTypedHeaders().CacheControl = CacheControlMiddleware.NoCacheHeader;
    return next.Invoke();
});

// Run migrations at startup; EnsureCreated for the test environment (no migrations)
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

api.MapAgentEndpoints();
api.MapSessionEndpoints();
api.MapStatusEndpoints(app.Configuration);
api.MapMetadataEndpoints();
api.MapH3Endpoints();
api.MapHexagonEndpoints();
api.MapImageEndpoints();
api.MapRoadTurbulenceEndpoints();
api.MapAnomalyEndpoints();
api.MapSeedEndpoints();
api.MapIntrospectiveEndpoints();
api.MapVoteEndpoints();

app.Run();

public partial class Program { }

internal static class CacheControlMiddleware
{
    public static readonly CacheControlHeaderValue NoCacheHeader = new() { NoCache = true, NoStore = true };
}
