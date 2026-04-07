using ExifApi.Data.Entities;
using Redis.OM;

namespace ExifApi.Infrastructure.Caching;

public class RedisIndexCreationService : IHostedService
{
    private readonly RedisConnectionProvider _provider;
    private readonly ILogger<RedisIndexCreationService> _logger;

    public RedisIndexCreationService(RedisConnectionProvider provider, ILogger<RedisIndexCreationService> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _provider.Connection.CreateIndexAsync(typeof(Hexagon));
            await _provider.Connection.CreateIndexAsync(typeof(RoadVisualAnomaly));
            await _provider.Connection.CreateIndexAsync(typeof(RoadTurbulence));
            await _provider.Connection.CreateIndexAsync(typeof(Vote));
            _logger.LogInformation("Redis.OM indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis.OM index creation failed — Redis unavailable. App continues without search indexes.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
