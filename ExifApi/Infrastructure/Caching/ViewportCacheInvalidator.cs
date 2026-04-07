using StackExchange.Redis;

namespace ExifApi.Infrastructure.Caching;

public interface IViewportCacheInvalidator
{
    Task InvalidateAllAsync();
}

public class ViewportCacheInvalidator : IViewportCacheInvalidator
{
    private const string KeyPattern = "viewport:v1:*";
    private readonly IConnectionMultiplexer _mux;
    private readonly ILogger<ViewportCacheInvalidator> _logger;

    public ViewportCacheInvalidator(IConnectionMultiplexer mux, ILogger<ViewportCacheInvalidator> logger)
    {
        _mux = mux;
        _logger = logger;
    }

    public async Task InvalidateAllAsync()
    {
        try
        {
            var server = _mux.GetServer(_mux.GetEndPoints()[0]);
            var db = _mux.GetDatabase();
            var deleted = 0;
            await foreach (var key in server.KeysAsync(pattern: KeyPattern))
            {
                await db.KeyDeleteAsync(key);
                deleted++;
            }
            _logger.LogInformation("Viewport cache invalidated: {Count} keys deleted", deleted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Viewport cache invalidation failed. Redis may be unavailable");
        }
    }
}
