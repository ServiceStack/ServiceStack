using System;
using System.Collections.Generic;

namespace ServiceStack.Caching
{
    /// <summary>
    /// Extend ICacheClient API with shared, non-core features
    /// </summary>
    public interface ICacheClientExtended : ICacheClient
    {
        TimeSpan? GetTimeToLive(string key);

        IEnumerable<string> GetKeysByPattern(string pattern);
    }
}