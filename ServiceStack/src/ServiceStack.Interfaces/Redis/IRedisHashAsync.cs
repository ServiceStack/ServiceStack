using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Model;

namespace ServiceStack.Redis;

public interface IRedisHashAsync
    : IAsyncEnumerable<KeyValuePair<string, string>>, IHasStringId
{
    ValueTask<bool> AddIfNotExistsAsync(KeyValuePair<string, string> item, CancellationToken token = default);
    ValueTask AddRangeAsync(IEnumerable<KeyValuePair<string, string>> items, CancellationToken token = default);
    ValueTask<long> IncrementValueAsync(string key, int incrementBy, CancellationToken token = default);

    // shim the basic ICollection etc APIs
    ValueTask<int> CountAsync(CancellationToken token = default);
    ValueTask AddAsync(KeyValuePair<string, string> item, CancellationToken token = default);
    ValueTask AddAsync(string key, string value, CancellationToken token = default);
    ValueTask ClearAsync(CancellationToken token = default);
    ValueTask<bool> ContainsKeyAsync(string key, CancellationToken token = default);
    ValueTask<bool> RemoveAsync(string key, CancellationToken token = default);
}