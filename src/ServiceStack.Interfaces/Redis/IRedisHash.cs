using System.Collections.Generic;
using ServiceStack.Model;

namespace ServiceStack.Redis
{
    public interface IRedisHash
        : IDictionary<string, string>, IHasStringId
    {
        bool AddIfNotExists(KeyValuePair<string, string> item);
        void AddRange(IEnumerable<KeyValuePair<string, string>> items);
        long IncrementValue(string key, int incrementBy);
    }
}