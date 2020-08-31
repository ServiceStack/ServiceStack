using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Model;

namespace ServiceStack.Redis
{
    public interface IRedisHashAsync
        : IAsyncEnumerable<KeyValuePair<string, string>>, IHasStringId
    {
        ValueTask<bool> AddIfNotExistsAsync(KeyValuePair<string, string> item, CancellationToken cancellationToken = default);
        ValueTask AddRangeAsync(IEnumerable<KeyValuePair<string, string>> items, CancellationToken cancellationToken = default);
        ValueTask<long> IncrementValueAsync(string key, int incrementBy, CancellationToken cancellationToken = default);

        // shim the basic ICollection etc APIs
        ValueTask<int> CountAsync(CancellationToken cancellationToken = default);
        ValueTask AddAsync(KeyValuePair<string, string> item, CancellationToken cancellationToken = default);
        ValueTask AddAsync(string key, string value, CancellationToken cancellationToken = default);
        ValueTask ClearAsync(CancellationToken cancellationToken = default);
        ValueTask<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}