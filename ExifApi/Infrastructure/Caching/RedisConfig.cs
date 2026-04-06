using System;

namespace ExifApi.Infrastructure.Caching;

public class RedisConfig
{
    public string Configuration { get; set; } = string.Empty;
    public string MultiplexerConfiguration { get; set; } = string.Empty;
    public int TtlMinutes { get; set; } = 5;
}
