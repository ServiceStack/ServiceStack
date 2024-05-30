// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDb;

public class Seq
{
    public string Id { get; set; }
    public long Counter { get; set; }
}

public class DynamoDbSequenceSource(IPocoDynamo db) : ISequenceSource, ISequenceSourceAsync
{
    private readonly DynamoMetadataType table = DynamoMetadata.RegisterTable<Seq>();

    public void InitSchema()
    {
        db.CreateTableIfMissing(table);
    }

    public long Increment(string tableName, long amount = 1)
    {
        var newCounter = db.IncrementById<Seq>(tableName, x => x.Counter, amount);
        return newCounter;
    }

    public void Reset(string tableName, long startingAt = 0)
    {
        db.PutItem(new Seq { Id = tableName, Counter = startingAt });
    }

    public async Task<long> IncrementAsync(string tableName, long amount = 1, CancellationToken token = default)
    {
        return await db.IncrementByIdAsync<Seq>(tableName, x => x.Counter, amount, token).ConfigAwait();
    }

    public async Task ResetAsync(string tableName, long startingAt = 0, CancellationToken token = default)
    {
        await db.PutItemAsync(new Seq { Id = tableName, Counter = startingAt }, token: token).ConfigAwait();
    }
}

public static class SequenceGeneratorExtensions
{
    public static long Increment(this ISequenceSource seq, DynamoMetadataType meta, int amount = 1)
    {
        return seq.Increment(meta.Name, amount);
    }

    public static async Task<long> IncrementAsync(this ISequenceSourceAsync seq, DynamoMetadataType meta, int amount = 1,
        CancellationToken token=default)
    {
        return await seq.IncrementAsync(meta.Name, amount, token).ConfigAwait();
    }

    public static long Increment<T>(this ISequenceSource seq, int amount = 1)
    {
        var tableName = DynamoMetadata.GetType<T>().Name;
        return seq.Increment(tableName, amount);
    }

    public static async Task<long> IncrementAsync<T>(this ISequenceSourceAsync seq, int amount = 1, CancellationToken token=default)
    {
        var tableName = DynamoMetadata.GetType<T>().Name;
        return await seq.IncrementAsync(tableName, amount, token).ConfigAwait();
    }

    public static void Reset<T>(this ISequenceSource seq, int startingAt = 0)
    {
        var tableName = DynamoMetadata.GetType<T>().Name;
        seq.Reset(tableName, startingAt);
    }

    public static async Task ResetAsync<T>(this ISequenceSourceAsync seq, int startingAt = 0, CancellationToken token=default)
    {
        var tableName = DynamoMetadata.GetType<T>().Name;
        await seq.ResetAsync(tableName, startingAt, token).ConfigAwait();
    }

    public static long Current(this ISequenceSource seq, DynamoMetadataType meta)
    {
        return seq.Increment(meta.Name, 0);
    }

    public static async Task<long> CurrentAsync(this ISequenceSourceAsync seq, DynamoMetadataType meta, CancellationToken token=default)
    {
        return await seq.IncrementAsync(meta.Name, 0, token).ConfigAwait();
    }

    public static long Current<T>(this ISequenceSource seq)
    {
        var tableName = DynamoMetadata.GetType<T>().Name;
        return seq.Increment(tableName, 0);
    }

    public static async Task<long> CurrentAsync<T>(this ISequenceSourceAsync seq, CancellationToken token=default)
    {
        var tableName = DynamoMetadata.GetType<T>().Name;
        return await seq.IncrementAsync(tableName, 0, token);
    }

    public static long[] GetNextSequences<T>(this ISequenceSource seq, int noOfSequences)
    {
        return GetNextSequences(seq, DynamoMetadata.GetType<T>(), noOfSequences);
    }

    public static async Task<long[]> GetNextSequencesAsync<T>(this ISequenceSourceAsync seq, int noOfSequences, CancellationToken token=default)
    {
        return await GetNextSequencesAsync(seq, DynamoMetadata.GetType<T>(), noOfSequences).ConfigAwait();
    }

    public static long[] GetNextSequences(this ISequenceSource seq, DynamoMetadataType meta, int noOfSequences)
    {
        var newCounter = seq.Increment(meta, noOfSequences);
        var firstId = newCounter - noOfSequences + 1;
        var ids = new long[noOfSequences];
        for (var i = 0; i < noOfSequences; i++)
        {
            ids[i] = firstId + i;
        }
        return ids;
    }

    public static async Task<long[]> GetNextSequencesAsync(this ISequenceSourceAsync seq, DynamoMetadataType meta, int noOfSequences)
    {
        var newCounter = await seq.IncrementAsync(meta, noOfSequences).ConfigAwait();
        var firstId = newCounter - noOfSequences + 1;
        var ids = new long[noOfSequences];
        for (var i = 0; i < noOfSequences; i++)
        {
            ids[i] = firstId + i;
        }
        return ids;
    }
}