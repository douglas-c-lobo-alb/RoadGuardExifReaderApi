using ExifApi.Data;
using ExifApi.Endpoints;
using ExifApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ExifService>();
builder.Services.AddScoped<H3Service>();
builder.Services.AddScoped<ImageService>();
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

var api = app.MapGroup("/api");

api.MapStatusEndpoints();
api.MapMetadataEndpoints();
api.MapH3Endpoints();
api.MapImageEndpoints();

app.Run();
