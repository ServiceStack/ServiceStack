using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests;

[TestFixture, NonParallelizable]
public class RedisPatternCleanupTests
{
    RedisClient redis;
    string prefix;

    [SetUp]
    public void Setup()
    {
        redis = new RedisClient(TestConfig.SingleHost);
        prefix = "unlink-pattern:" + Guid.NewGuid().ToString("N") + ":";
    }

    [TearDown]
    public void Cleanup()
    {
        redis.UnlinkByPattern(prefix + "*");
        redis.Dispose();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Cleanup_uses_bounded_UNLINK_batches_preserves_binary_keys_and_unrelated_data(bool async)
    {
        for (var i = 0; i < 2500; i++) redis.SetValue(prefix + "match:" + i, "value");
        var binary = (prefix + "match:").ToUtf8Bytes().Concat(new byte[] { 0, 255, 128 }).ToArray();
        redis.RawCommand("SET", binary, "binary");
        redis.SetValue(prefix + "keep", "value");
        var commands = new List<byte[][]>();
        long count;
        using (Diagnostics.Redis.Subscribe(new Observer(e => commands.Add(e.Command))))
            count = async
                ? await redis.AsAsync().UnlinkByPatternAsync(prefix + "match:*", 37, 5000)
                : ((IRedisClient)redis).UnlinkByPattern(prefix + "match:*", 37, 5000);
        Assert.That(count, Is.EqualTo(2501));
        Assert.That(commands.Any(x => x[0].FromUtf8Bytes() is "KEYS" or "DEL"), Is.False);
        Assert.That(commands.Where(x => x[0].FromUtf8Bytes() == "UNLINK").All(x => x.Length <= 38), Is.True);
        Assert.That(commands.Count(x => x[0].FromUtf8Bytes() == "UNLINK"), Is.GreaterThan(1));
        Assert.That(redis.RawCommand("EXISTS", binary).Data.FromUtf8Bytes(), Is.EqualTo("0"));
        Assert.That(redis.GetValue(prefix + "keep"), Is.EqualTo("value"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Empty_scan_pages_continue_until_cursor_zero_and_support_cancellation(bool async)
    {
        for (var i = 0; i < 100; i++) redis.SetValue(prefix + "other:" + i, "value");
        var scans = 0;
        using (Diagnostics.Redis.Subscribe(new Observer(e =>
               {
                   if (e.Command[0].FromUtf8Bytes() == "SCAN") scans++;
               })))
            Assert.That(async
                ? await redis.UnlinkByPatternAsync(prefix + "missing:*", 5, 1)
                : redis.UnlinkByPattern(prefix + "missing:*", 5, 1), Is.Zero);
        Assert.That(scans, Is.GreaterThan(1));
        using var cts = new CancellationTokenSource();
        scans = 0;
        using (Diagnostics.Redis.Subscribe(new Observer(e =>
               {
                   if (e.Command[0].FromUtf8Bytes() == "SCAN")
                   {
                       scans++;
                       cts.Cancel();
                   }
               })))
        {
            try
            {
                if (async) await redis.UnlinkByPatternAsync(prefix + "missing:*", 5, 1, cts.Token);
                else redis.UnlinkByPattern(prefix + "missing:*", 5, 1, cts.Token);
                Assert.Fail("Expected cancellation");
            }
            catch (OperationCanceledException)
            {
            }
        }

        Assert.That(scans, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Cancellation_after_a_batch_leaves_partial_cleanup(bool async)
    {
        for (var i = 0; i < 100; i++) redis.SetValue(prefix + i, "value");
        using var cts = new CancellationTokenSource();
        var unlinks = 0;
        using (Diagnostics.Redis.Subscribe(new Observer(e =>
               {
                   if (e.Command[0].FromUtf8Bytes() == "UNLINK")
                   {
                       unlinks++;
                       cts.Cancel();
                   }
               })))
        {
            try
            {
                if (async) await redis.UnlinkByPatternAsync(prefix + "*", 5, 1000, cts.Token);
                else redis.UnlinkByPattern(prefix + "*", 5, 1000, cts.Token);
                Assert.Fail("Expected cancellation");
            }
            catch (OperationCanceledException)
            {
            }
        }

        Assert.That(unlinks, Is.EqualTo(1));
        Assert.That(redis.ScanAllKeys(prefix + "*").Count(), Is.EqualTo(95));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ACL_denial_does_not_fall_back_to_DEL(bool async)
    {
        var user = "cleanup" + Guid.NewGuid().ToString("N");
        redis.SetValue(prefix + "keep", "value");
        redis.Custom("ACL", "SETUSER", user, "on", ">test-password", "~" + prefix + "*", "+@all", "-unlink");
        try
        {
            var endpoint = TestConfig.SingleHost.ToRedisEndpoint();
            endpoint.Username = user;
            endpoint.Password = "test-password";
            using var restricted = new RedisClient(endpoint);
            try
            {
                if (async) await restricted.UnlinkByPatternAsync(prefix + "*");
                else restricted.UnlinkByPattern(prefix + "*");
                Assert.Fail("Expected ACL error");
            }
            catch (RedisResponseException e)
            {
                Assert.That(e.Message, Does.Contain("NOPERM"));
            }

            Assert.That(redis.GetValue(prefix + "keep"), Is.EqualTo("value"));
        }
        finally
        {
            redis.Custom("ACL", "DELUSER", user);
        }
    }

    [Test]
    public void Validation_precancellation_and_batch_context_reject_before_mutation()
    {
        redis.SetValue(prefix + "keep", "value");
        Assert.Throws<ArgumentNullException>(() => redis.UnlinkByPattern(null));
        Assert.Throws<ArgumentOutOfRangeException>(() => redis.UnlinkByPattern(prefix + "*", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => redis.UnlinkByPattern(prefix + "*", 1, 0));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => redis.UnlinkByPattern(prefix + "*", token: cts.Token));
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await redis.UnlinkByPatternAsync(prefix + "*", token: cts.Token));
        using (var pipeline = redis.CreatePipeline())
            Assert.Throws<NotSupportedException>(() => redis.UnlinkByPattern(prefix + "*"));
        Assert.That(redis.GetValue(prefix + "keep"), Is.EqualTo("value"));
    }

    sealed class Observer(Action<RedisDiagnosticEvent> onAfter) : IObserver<KeyValuePair<string, object>>
    {
        public void OnNext(KeyValuePair<string, object> item)
        {
            if (item.Key == Diagnostics.Events.Redis.WriteCommandAfter &&
                item.Value is RedisDiagnosticEvent e) onAfter(e);
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}