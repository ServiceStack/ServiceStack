namespace ServiceStack.Redis;

/// <summary>Choose one hash-field expiry condition. This is not a flags enum.</summary>
public enum RedisHashExpiryCondition
{
    None = 0,
    /// <summary>Set expiry only on fields that currently have no expiry.</summary>
    NX = 1,
    /// <summary>Set expiry only on fields that already have expiry.</summary>
    XX = 2,
    /// <summary>Extend expiry only. A persistent field has an infinite lifetime.</summary>
    GT = 4,
    /// <summary>Shorten expiry only. A persistent field has an infinite lifetime.</summary>
    LT = 8
}
