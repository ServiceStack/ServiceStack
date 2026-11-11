using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests;

[TestFixture, NonParallelizable]
public class RedisHashExpiryProtocolTests
{
    static Task<string> Reply(string[] command, string reply) => Task.FromResult(command[0] switch
    {
        "INFO" => "$21\r\nredis_version:8.0.0\r\n\r\n",
        "HPEXPIRE" => reply,
        _ => "+PONG\r\n"
    });

    [TestCase(false, "*1\r\n:1\r\n")][TestCase(true, "*1\r\n:1\r\n")]
    [TestCase(false, "*2\r\n:1\r\n")][TestCase(true, "*2\r\n:1\r\n")]
    [TestCase(false, "*2\r\n:1\r\n:invalid\r\n")][TestCase(true, "*2\r\n:1\r\n:invalid\r\n")]
    [TestCase(false, "*2\r\n:1\r\n:2")][TestCase(true, "*2\r\n:1\r\n:2")]
    [TestCase(false, "*2\r\n:1\r\n-ERR invalid\r\n")][TestCase(true, "*2\r\n:1\r\n-ERR invalid\r\n")]
    [TestCase(false, "*2\n:1\r\n:1\r\n")][TestCase(true, "*2\n:1\r\n:1\r\n")]
    public async Task Invalid_or_partial_arrays_close_the_socket_without_replaying(bool async, string reply)
    {
        await using var server = new RedisAsyncConnectionTests.Server((_, args) => Reply(args, reply)) { CloseAfterCommand = "HPEXPIRE" };
        using var client = new RedisClient(server.Address) { RetryTimeout = 1000 };
        using var cts = new CancellationTokenSource(5000);
        try
        {
            if (async) await ((IRedisClientAsync)client).HPExpireAsync("hash", 60000, new[] { "a", "b" }, token: cts.Token);
            else ((IRedisClient)client).HPExpire("hash", 60000, new[] { "a", "b" });
            Assert.Fail("Expected invalid reply");
        }
        catch (RedisException) { }
        Assert.That(client.HasConnected, Is.False);
        Assert.That(server.Connections, Is.EqualTo(1));
        Assert.That(server.Commands.Count(x => x[0] == "HPEXPIRE"), Is.EqualTo(1));
        Assert.That(await ((IRedisClientAsync)client).PingAsync(), Is.True);
    }

    [Test]
    public async Task Cancellation_during_a_reply_preserves_token_and_allows_clean_reconnection()
    {
        await using var server = new RedisAsyncConnectionTests.Server((_, args) => Reply(args, "*2\r\n:1\r\n:1\r\n")) { PauseCommand = "HPEXPIRE" };
        using var client = new RedisClient(server.Address);
        using var cts = new CancellationTokenSource(5000);
        var pending = ((IRedisClientAsync)client).HPExpireAsync("hash", 60000, new[] { "a", "b" }, token: cts.Token).AsTask();
        Assert.That(await Task.WhenAny(server.Paused.Task, Task.Delay(3000)), Is.SameAs(server.Paused.Task));
        cts.Cancel();
        Assert.That(await Task.WhenAny(pending, Task.Delay(3000)), Is.SameAs(pending));
        try { await pending; Assert.Fail("Expected cancellation"); }
        catch (OperationCanceledException ex) { Assert.That(ex.CancellationToken, Is.EqualTo(cts.Token)); }
        Assert.That(client.HasConnected, Is.False);
        Assert.That(await Task.WhenAny(server.Closed.Task, Task.Delay(3000)), Is.SameAs(server.Closed.Task));
        server.PauseCommand = null;
        Assert.That(await ((IRedisClientAsync)client).PingAsync(), Is.True);
        Assert.That(server.Commands.Count(x => x[0] == "HPEXPIRE"), Is.EqualTo(1));
    }
}
