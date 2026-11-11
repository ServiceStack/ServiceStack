using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests;

/// <summary>
/// From v10.5 queued reads return stored empty values as empty (matching non-queued calls), int callbacks still
/// read an empty value as 0, and RedisConfig.PipelineEmptyValuesAsNull restores the previous null results.
/// </summary>
[TestFixture, NonParallelizable]
public class RedisPipelineEmptyValueTests
{
    const string Key = "pipeline-empty:value";
    const string HashId = "pipeline-empty:hash";
    RedisClient redis;

    [SetUp]
    public void SetUp()
    {
        redis = new RedisClient(TestConfig.SingleHost);
        redis.SetValue(Key, "");
        redis.SetEntryInHash(HashId, "field", "");
    }

    [TearDown]
    public void TearDown()
    {
        RedisConfig.PipelineEmptyValuesAsNull = false;
        redis.RemoveAll(new[] { Key, HashId });
        redis.Dispose();
    }

    (string value, string hashValue, int? number) ReadQueued(bool transaction)
    {
        string value = "unset", hashValue = "unset";
        var number = -1;
        if (transaction)
        {
            using var trans = redis.CreateTransaction();
            trans.QueueCommand(r => r.GetValue(Key), v => value = v);
            trans.QueueCommand(r => r.GetValueFromHash(HashId, "field"), v => hashValue = v);
            trans.QueueCommand(r => r.Get<int>(Key), v => number = v);
            trans.Commit();
        }
        else
        {
            using var pipe = redis.CreatePipeline();
            pipe.QueueCommand(r => r.GetValue(Key), v => value = v);
            pipe.QueueCommand(r => r.GetValueFromHash(HashId, "field"), v => hashValue = v);
            pipe.QueueCommand(r => r.Get<int>(Key), v => number = v);
            pipe.Flush();
        }
        return (value, hashValue, number);
    }

    // The async queue API has no int-returning byte command, so int callbacks are covered by the sync path.
    async Task<(string value, string hashValue, int? number)> ReadQueuedAsync(bool transaction)
    {
        string value = "unset", hashValue = "unset";
        var client = (IRedisClientAsync)redis;
        if (transaction)
        {
            await using var trans = await client.CreateTransactionAsync();
            trans.QueueCommand(r => r.GetValueAsync(Key), v => value = v);
            trans.QueueCommand(r => r.GetValueFromHashAsync(HashId, "field"), v => hashValue = v);
            await trans.CommitAsync();
        }
        else
        {
            await using var pipe = client.CreatePipeline();
            pipe.QueueCommand(r => r.GetValueAsync(Key), v => value = v);
            pipe.QueueCommand(r => r.GetValueFromHashAsync(HashId, "field"), v => hashValue = v);
            await pipe.FlushAsync();
        }
        return (value, hashValue, null);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task Queued_reads_return_empty_values_by_default(bool async, bool transaction)
    {
        Assert.That(redis.GetValue(Key), Is.EqualTo(""));
        var (value, hashValue, number) = async ? await ReadQueuedAsync(transaction) : ReadQueued(transaction);
        Assert.That(value, Is.EqualTo(""));
        Assert.That(hashValue, Is.EqualTo(""));
        if (number != null)
            Assert.That(number, Is.EqualTo(0), "An empty value read by an int callback is 0, not a FormatException");
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task PipelineEmptyValuesAsNull_restores_null_results(bool async, bool transaction)
    {
        RedisConfig.PipelineEmptyValuesAsNull = true;
        Assert.That(redis.GetValue(Key), Is.EqualTo(""), "Non-queued reads are unaffected by the flag");
        var (value, hashValue, number) = async ? await ReadQueuedAsync(transaction) : ReadQueued(transaction);
        Assert.That(value, Is.Null);
        Assert.That(hashValue, Is.Null);
        if (number != null) Assert.That(number, Is.EqualTo(0));
    }

    [Test]
    public void RedisConfig_Reset_restores_the_default()
    {
        RedisConfig.PipelineEmptyValuesAsNull = true;
        RedisConfig.Reset();
        Assert.That(RedisConfig.PipelineEmptyValuesAsNull, Is.False);
    }
}
