using System;

namespace ServiceStack.Redis;

public class RedisStreamField
{
    public byte[] Name { get; set; } = Array.Empty<byte>();
    public byte[] Value { get; set; } = Array.Empty<byte>();
}

public class RedisStreamEntry
{
    /// <summary>Lossless server ID, or null for an unidentified nil claim result from older servers.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Null when a pending entry's payload was deleted/trimmed; an empty array is distinct.</summary>
    public RedisStreamField[] Fields { get; set; } = Array.Empty<RedisStreamField>();
}

public class RedisStreamPosition
{
    public string Stream { get; set; } = string.Empty;
    public string Id { get; set; } = "0-0";
}

public class RedisStreamReadResult
{
    public string Stream { get; set; } = string.Empty;
    public RedisStreamEntry[] Entries { get; set; } = Array.Empty<RedisStreamEntry>();
}

public class RedisStreamConsumerPending
{
    public string Consumer { get; set; } = string.Empty;
    public long Count { get; set; }
}

public class RedisStreamPendingSummary
{
    public long Count { get; set; }
    public string LowestId { get; set; }
    public string HighestId { get; set; }
    public RedisStreamConsumerPending[] Consumers { get; set; } = Array.Empty<RedisStreamConsumerPending>();
}

public class RedisStreamPendingEntry
{
    public string Id { get; set; } = string.Empty;
    public string Consumer { get; set; } = string.Empty;
    public long IdleMilliseconds { get; set; }
    public long DeliveryCount { get; set; }
}

public class RedisStreamAutoClaimResult
{
    public string NextStartId { get; set; } = "0-0";
    public RedisStreamEntry[] Entries { get; set; } = Array.Empty<RedisStreamEntry>();
    /// <summary>Redis 7+ reports removed pending IDs here; empty on Redis 6.2.</summary>
    public string[] DeletedIds { get; set; } = Array.Empty<string>();
}
