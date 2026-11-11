using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Redis;

/// <summary>A typed projection retaining the raw fields. Missing/deleted JSON payloads have HasValue=false.</summary>
public class RedisTypedStreamEntry<T>
{
    public string Stream { get; set; }
    public string Id { get; set; }
    public bool HasValue { get; set; }
    public T Value { get; set; }
    public RedisStreamField[] Fields { get; set; }
}

public static partial class RedisClientExtensions
{
    private static readonly byte[] JsonStreamField = "json".ToUtf8Bytes();

    public static string AddStreamEntry<T>(this IRedisClient client, string stream, T value, long? maxLength = null,
        bool approximate = false)
        => client.XAdd(stream, JsonFields(value), maxLength: maxLength, approximate: approximate);

    public static ValueTask<string> AddStreamEntryAsync<T>(this IRedisClientAsync client, string stream, T value,
        long? maxLength = null, bool approximate = false, CancellationToken token = default)
        => client.XAddAsync(stream, JsonFields(value), maxLength: maxLength, approximate: approximate, token: token);

    public static RedisTypedStreamEntry<T>[] ReadStreamEntries<T>(this IRedisClient client,
        RedisStreamPosition[] streams,
        int? count = null, long? blockMilliseconds = null)
        => ProjectStreamEntries<T>(client.XRead(streams, count, blockMilliseconds));

    public static async ValueTask<RedisTypedStreamEntry<T>[]> ReadStreamEntriesAsync<T>(this IRedisClientAsync client,
        RedisStreamPosition[] streams, int? count = null, long? blockMilliseconds = null,
        CancellationToken token = default)
        => ProjectStreamEntries<T>(await client.XReadAsync(streams, count, blockMilliseconds, token)
            .ConfigureAwait(false));

    public static RedisTypedStreamEntry<T>[] ReadStreamGroupEntries<T>(this IRedisClient client, string group,
        string consumer,
        RedisStreamPosition[] streams, int? count = null, long? blockMilliseconds = null)
        => ProjectStreamEntries<T>(client.XReadGroup(group, consumer, streams, count, blockMilliseconds));

    public static async ValueTask<RedisTypedStreamEntry<T>[]> ReadStreamGroupEntriesAsync<T>(
        this IRedisClientAsync client,
        string group, string consumer, RedisStreamPosition[] streams, int? count = null, long? blockMilliseconds = null,
        CancellationToken token = default)
        => ProjectStreamEntries<T>(await client
            .XReadGroupAsync(group, consumer, streams, count, blockMilliseconds, token: token).ConfigureAwait(false));

    private static RedisStreamField[] JsonFields<T>(T value) => [
        new() { Name = JsonStreamField, Value = (value.ToJson() ?? "null").ToUtf8Bytes() }
    ];

    private static T ParseStreamJson<T>(byte[] bytes)
    {
        var json = bytes.FromUtf8Bytes();
        return string.Equals(json?.Trim(), "null", StringComparison.Ordinal) ? default : json.FromJson<T>();
    }

    private static RedisTypedStreamEntry<T>[] ProjectStreamEntries<T>(RedisStreamReadResult[] results)
        => results.SelectMany(stream => stream.Entries.Select(entry =>
        {
            var json = entry.Fields?.Where(x => x.Name.SequenceEqual(JsonStreamField)).ToArray();
            if (json?.Length > 1) throw new FormatException("Typed stream entries require at most one json field.");
            var hasValue = json?.Length == 1;
            return new RedisTypedStreamEntry<T>
            {
                Stream = stream.Stream, Id = entry.Id, Fields = entry.Fields,
                HasValue = hasValue, Value = hasValue ? ParseStreamJson<T>(json[0].Value) : default
            };
        })).ToArray();
}