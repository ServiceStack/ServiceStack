namespace ServiceStack.Redis;

/// <summary>
/// Select at most one GETEX expiration option. No option preserves the current TTL.
/// All numeric options must be positive; absolute times are Unix timestamps.
/// </summary>
public class RedisGetExOptions
{
    public long? ExpirySeconds { get; set; }
    public long? ExpiryMilliseconds { get; set; }
    public long? ExpiryAtSeconds { get; set; }
    public long? ExpiryAtMilliseconds { get; set; }
    public bool Persist { get; set; }
}
