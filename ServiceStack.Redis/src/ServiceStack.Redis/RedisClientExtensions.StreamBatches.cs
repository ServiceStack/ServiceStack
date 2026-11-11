using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis;

public static partial class RedisClientExtensions
{
    /// <summary>
    /// Append binary field sets with generated IDs in bounded pipeline batches; return IDs in input order.
    /// Uses an exclusive ServiceStack RedisClient writer lease. Not atomic; failed/cancelled batches are never replayed.
    /// Earlier batches and some/all entries in a failed batch may already exist. No partial IDs are returned on failure.
    /// </summary>
    public static string[] AddStreamEntries(this IRedisClient client, string stream,
        IEnumerable<RedisStreamField[]> entries, int batchSize = 100, long? maxLength = null, bool approximate = false)
    {
        var native = StreamBatchClient(client, stream, entries, batchSize, maxLength, approximate);
        var ids = new List<string>();
        using var iterator = entries.GetEnumerator();
        var batch = new List<RedisStreamField[]>();
        while (NextStreamBatch(iterator, batch, batchSize))
        {
            try
            {
                // Open before queueing: queue delegates must only buffer commands.
                if (!native.AssertConnectedSocket()) throw new RedisException("Redis stream batch connection is not open.");
                using var pipeline = client.CreatePipeline();
                Exception firstError = null;
                foreach (var fields in batch)
                {
                    pipeline.QueueCommand(r => r.XAdd(stream, fields, maxLength: maxLength, approximate: approximate),
                        id => AddStreamBatchId(ids, id), e => StreamBatchError(e, ref firstError));
                }
                pipeline.Flush();
                if (firstError != null) ExceptionDispatchInfo.Capture(firstError).Throw();
            }
            catch
            {
                native.AbortConnection();
                throw;
            }
        }
        return ids.ToArray();
    }

    /// <summary>Async bounded binary append batches. Cancellation can leave partial/unknown writes; no automatic replay.</summary>
    public static async ValueTask<string[]> AddStreamEntriesAsync(this IRedisClientAsync client, string stream,
        IEnumerable<RedisStreamField[]> entries, int batchSize = 100, long? maxLength = null, bool approximate = false,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var native = StreamBatchClient(client, stream, entries, batchSize, maxLength, approximate);
        var ids = new List<string>();
        using var iterator = entries.GetEnumerator();
        var batch = new List<RedisStreamField[]>();
        while (NextStreamBatch(iterator, batch, batchSize, token))
        {
            try
            {
                await native.ConnectAsync(token).ConfigureAwait(false);
                await using var pipeline = client.CreatePipeline();
                Exception firstError = null;
                foreach (var fields in batch)
                {
                    token.ThrowIfCancellationRequested();
                    pipeline.QueueCommand(r => r.XAddAsync(stream, fields, maxLength: maxLength,
                            approximate: approximate, token: token),
                        id => AddStreamBatchId(ids, id), e => StreamBatchError(e, ref firstError));
                }
                await pipeline.FlushAsync(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                if (firstError != null) ExceptionDispatchInfo.Capture(firstError).Throw();
            }
            catch
            {
                native.AbortConnection();
                token.ThrowIfCancellationRequested();
                throw;
            }
        }
        return ids.ToArray();
    }

    /// <summary>Append JSON entries using the same "json" field convention as AddStreamEntry.</summary>
    public static string[] AddStreamEntries<T>(this IRedisClient client, string stream, IEnumerable<T> entries,
        int batchSize = 100, long? maxLength = null, bool approximate = false)
    {
        if (entries == null) throw new ArgumentNullException(nameof(entries));
        return client.AddStreamEntries(stream, entries.Select(JsonFields), batchSize, maxLength, approximate);
    }

    public static ValueTask<string[]> AddStreamEntriesAsync<T>(this IRedisClientAsync client, string stream,
        IEnumerable<T> entries, int batchSize = 100, long? maxLength = null, bool approximate = false,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (entries == null) throw new ArgumentNullException(nameof(entries));
        return client.AddStreamEntriesAsync(stream, entries.Select(JsonFields), batchSize, maxLength, approximate, token);
    }

    private static RedisClient StreamBatchClient(object client, string stream,
        IEnumerable<RedisStreamField[]> entries, int batchSize, long? maxLength, bool approximate)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (entries == null) throw new ArgumentNullException(nameof(entries));
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
        if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength));
        if (approximate && maxLength == null) throw new ArgumentException("Approximate trimming requires maxLength.");
        var native = client as RedisClient
            ?? throw new NotSupportedException("Stream batch helpers require a ServiceStack RedisClient lease.");
        if (native.IsDisposed) throw new ObjectDisposedException(nameof(RedisClient));
        native.AssertStreamStandalone();
        return native;
    }

    private static bool NextStreamBatch(IEnumerator<RedisStreamField[]> iterator, List<RedisStreamField[]> batch,
        int batchSize, CancellationToken token = default)
    {
        batch.Clear();
        while (batch.Count < batchSize)
        {
            token.ThrowIfCancellationRequested();
            if (!iterator.MoveNext()) break;
            token.ThrowIfCancellationRequested();
            RedisNativeClient.ValidateStreamFields(iterator.Current);
            batch.Add(iterator.Current);
        }
        return batch.Count != 0;
    }

    private static void AddStreamBatchId(List<string> ids, string id)
    {
        if (string.IsNullOrEmpty(id)) throw new FormatException("XADD did not return an entry ID.");
        ids.Add(id);
    }

    private static void StreamBatchError(Exception error, ref Exception firstError)
    {
        // Drain ordinary Redis error replies so subsequent queued responses stay aligned.
        // Transport/cancellation/parser failures must stop immediately and close the connection.
        if (error is not RedisResponseException) ExceptionDispatchInfo.Capture(error).Throw();
        firstError ??= error;
    }
}
