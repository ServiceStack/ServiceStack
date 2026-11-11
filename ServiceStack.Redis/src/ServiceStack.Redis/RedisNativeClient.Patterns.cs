using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Redis;

public partial class RedisNativeClient : IRedisPatternCommands, IRedisPatternCommandsAsync
{
    private void AssertStandalonePattern(string pattern, int batchSize, int scanCount)
    {
        if (pattern == null) throw new ArgumentNullException(nameof(pattern));
        if (batchSize < 1) throw new ArgumentOutOfRangeException(nameof(batchSize));
        if (scanCount < 1) throw new ArgumentOutOfRangeException(nameof(scanCount));
        if (Pipeline != null || PipelineAsync != null || Transaction != null)
            throw new NotSupportedException("Pattern cleanup cannot run inside a pipeline or transaction.");
    }

    public long UnlinkByPattern(string pattern, int batchSize = 512, int scanCount = 1000,
        CancellationToken token = default)
    {
        AssertStandalonePattern(pattern, batchSize, scanCount);
        var batch = new List<byte[]>();
        ulong cursor = 0;
        long removed = 0;
        do
        {
            token.ThrowIfCancellationRequested();
            var page = Scan(cursor, scanCount, pattern);
            cursor = page.Cursor;
            foreach (var key in page.Results)
            {
                token.ThrowIfCancellationRequested();
                batch.Add(key);
                if (batch.Count < batchSize) continue;
                removed += DeleteBatch();
            }
        } while (cursor != 0);

        if (batch.Count != 0) removed += DeleteBatch();
        return removed;

        long DeleteBatch()
        {
            token.ThrowIfCancellationRequested();
            var result = SendExpectLong(MergeCommandWithArgs(Commands.Unlink, batch.ToArray()));
            batch.Clear();
            return result;
        }
    }

    public async ValueTask<long> UnlinkByPatternAsync(string pattern, int batchSize = 512, int scanCount = 1000,
        CancellationToken token = default)
    {
        AssertStandalonePattern(pattern, batchSize, scanCount);
        var batch = new List<byte[]>();
        ulong cursor = 0;
        long removed = 0;
        do
        {
            token.ThrowIfCancellationRequested();
            var page = await ((IRedisNativeClientAsync)this).ScanAsync(cursor, scanCount, pattern, token)
                .ConfigureAwait(false);
            cursor = page.Cursor;
            foreach (var key in page.Results)
            {
                token.ThrowIfCancellationRequested();
                batch.Add(key);
                if (batch.Count < batchSize) continue;
                removed += await DeleteBatch().ConfigureAwait(false);
            }
        } while (cursor != 0);

        if (batch.Count != 0) removed += await DeleteBatch().ConfigureAwait(false);
        return removed;

        async ValueTask<long> DeleteBatch()
        {
            token.ThrowIfCancellationRequested();
            var result = await SendExpectLongAsync(token, MergeCommandWithArgs(Commands.Unlink, batch.ToArray()))
                .ConfigureAwait(false);
            batch.Clear();
            return result;
        }
    }
}

public static partial class RedisClientExtensions
{
    public static long UnlinkByPattern(this IRedisClient client, string pattern, int batchSize = 512,
        int scanCount = 1000, CancellationToken token = default)
        => (client as IRedisPatternCommands ??
            throw new NotSupportedException("Client does not support IRedisPatternCommands."))
            .UnlinkByPattern(pattern, batchSize, scanCount, token);

    public static ValueTask<long> UnlinkByPatternAsync(this IRedisClientAsync client, string pattern,
        int batchSize = 512, int scanCount = 1000, CancellationToken token = default)
        => (client as IRedisPatternCommandsAsync ??
            throw new NotSupportedException("Client does not support IRedisPatternCommandsAsync."))
            .UnlinkByPatternAsync(pattern, batchSize, scanCount, token);
}