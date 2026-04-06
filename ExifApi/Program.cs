using ExifApi.Data;
using ExifApi.Data.Entities;
using ExifApi.Endpoints;
using ExifApi.Infrastructure;
using ExifApi.Infrastructure.Caching;
using ExifApi.Services;
using Microsoft.EntityFrameworkCore;
using Redis.OM;
using StackExchange.Redis;
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
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
    options.EnableDetailedErrors();
});

var redisConfig = builder.Configuration.GetSection("Redis").Get<RedisConfig>();
builder.Services.AddSingleton(new RedisConnectionProvider(redisConfig!.Configuration));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConfig!.MultiplexerConfiguration;
});
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConfig!.MultiplexerConfiguration));
builder.Services.AddSingleton<IViewportCacheInvalidator, ViewportCacheInvalidator>();
builder.Services.AddHostedService<RedisIndexCreationService>();

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
