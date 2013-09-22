using System;
using System.Collections.Generic;

namespace ServiceStack.CacheAccess
{
	public interface ICacheManager
	{
        ICacheClient CacheClient { get; }

        string ContentType { get; }
        
        T Get<T>(string cacheKey, Func<T> createCacheFn);

		T Get<T>(string cacheKey, TimeSpan expireIn, Func<T> createCacheFn);

        string GetString<T>(string cacheKey, Func<T> createCacheFn);

        string GetString<T>(string cacheKey, TimeSpan expiresIn, Func<T> createCacheFn);
    
        void Clear(IEnumerable<string> cacheKeys);

        void Clear(params string[] cacheKeys);
    }
}