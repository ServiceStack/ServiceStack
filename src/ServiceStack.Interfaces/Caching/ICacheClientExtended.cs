using System;

namespace ServiceStack.Caching
{
    /// <summary>
    /// Extend ICacheClient API with shared, non-core features
    /// </summary>
    public interface ICacheClientExtended : ICacheClient
    {
        TimeSpan? GetTimeToLive(string key);
    }
}