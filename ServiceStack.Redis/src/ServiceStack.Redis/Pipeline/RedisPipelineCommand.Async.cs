using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Pipeline
{
    partial class RedisPipelineCommand
    {
        internal async ValueTask<List<long>> ReadAllAsIntsAsync(CancellationToken token)
        {
            var results = new List<long>();
            while (cmdCount-- > 0)
            {
                results.Add(await client.ReadLongAsync(token).ConfigureAwait(false));
            }

            return results;
        }
        internal async ValueTask<bool> ReadAllAsIntsHaveSuccessAsync(CancellationToken token)
        {
            var allResults = await ReadAllAsIntsAsync(token).ConfigureAwait(false);
            return allResults.All(x => x == RedisNativeClient.Success);
        }

        internal ValueTask FlushAsync(CancellationToken token)
        {
            Flush();
            return default;
        }
    }
}