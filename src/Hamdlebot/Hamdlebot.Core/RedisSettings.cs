namespace Hamdlebot.Core;

public class RedisSettings
{
    public string ConnectionString { get; set; }
    public byte MaxRetries { get; set; }
}