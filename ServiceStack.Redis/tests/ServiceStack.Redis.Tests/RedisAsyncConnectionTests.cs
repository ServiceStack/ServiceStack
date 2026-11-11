using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Caching;

namespace ServiceStack.Redis.Tests;

[TestFixture, NonParallelizable]
public class RedisAsyncConnectionTests
{
    int previousVersion;
    int? previousAssumption;
    [SetUp] public void Before()
    {
        previousVersion = RedisNativeClient.ServerVersionNumber;
        previousAssumption = RedisConfig.AssumeServerVersion;
        RedisNativeClient.ServerVersionNumber = 0;
        RedisConfig.AssumeServerVersion = null;
    }
    [TearDown] public void After()
    {
        RedisNativeClient.ServerVersionNumber = previousVersion;
        RedisConfig.AssumeServerVersion = previousAssumption;
    }

    internal sealed class Server : IAsyncDisposable
    {
        readonly TcpListener listener = new(IPAddress.Loopback, 0);
        readonly List<TcpClient> clients = new();
        readonly List<Task> workers = new();
        readonly Func<int, string[], Task<string>> reply;
        readonly Task accepting;
        public readonly CancellationTokenSource Stop = new();
        public readonly TaskCompletionSource<string[]> Paused = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<bool> Closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly List<string[]> Commands = new();
        public string PauseCommand;
        public string CloseAfterCommand;
        public string PauseAfterReply;
        public bool Raw;
        public int Connections;
        public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
        public string Address => $"localhost:{Port}?ConnectTimeout=2000&RetryTimeout=1";
        public Server(Func<int, string[], Task<string>> reply = null)
        {
            this.reply = reply ?? ((_, args) => Task.FromResult(args[0] == "INFO"
                ? "$21\r\nredis_version:8.0.0\r\n\r\n" : "+OK\r\n"));
            listener.Start();
            accepting = Accept();
        }
        async Task Accept()
        {
            try
            {
                while (!Stop.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    lock (clients) clients.Add(client);
                    workers.Add(Serve(client, Interlocked.Increment(ref Connections)));
                }
            }
            catch (Exception) when (Stop.IsCancellationRequested) { }
        }
        async Task Serve(TcpClient client, int number)
        {
            try
            {
                using (client)
                {
                    var stream = client.GetStream();
                    while (!Stop.IsCancellationRequested)
                    {
                        var args = Raw ? new[] { "TLS" } : await ReadCommand(stream);
                        if (args == null) return;
                        lock (Commands) Commands.Add(args);
                        if (Raw || args[0] == PauseCommand)
                        {
                            Paused.TrySetResult(args);
                            var bytes = new byte[1024];
                            while (await stream.ReadAsync(bytes, 0, bytes.Length, Stop.Token) != 0) { }
                            Closed.TrySetResult(true);
                            return;
                        }
                        var text = await reply(number, args);
                        if (text == null) return;
                        var response = Encoding.UTF8.GetBytes(text);
                        await stream.WriteAsync(response, 0, response.Length, Stop.Token);
                        if (args[0] == CloseAfterCommand) return;
                        if (args[0] == PauseAfterReply)
                        {
                            Paused.TrySetResult(args);
                            await Task.Delay(Timeout.Infinite, Stop.Token);
                            return;
                        }
                    }
                }
            }
            catch (Exception) when (Stop.IsCancellationRequested) { }
            catch (IOException) { Closed.TrySetResult(true); }
            catch (SocketException) { Closed.TrySetResult(true); }
        }
        static async Task<string[]> ReadCommand(Stream stream)
        {
            var line = await Line(stream);
            if (line == null) return null;
            if (line[0] != '*') throw new InvalidDataException(line);
            var args = new string[int.Parse(line.Substring(1))];
            for (var i = 0; i < args.Length; i++)
            {
                line = await Line(stream);
                var data = new byte[int.Parse(line.Substring(1))];
                var offset = 0;
                while (offset < data.Length)
                {
                    var count = await stream.ReadAsync(data, offset, data.Length - offset);
                    if (count == 0) return null;
                    offset += count;
                }
                args[i] = Encoding.UTF8.GetString(data);
                await Line(stream); // CRLF
            }
            return args;
        }
        static async Task<string> Line(Stream stream)
        {
            var bytes = new List<byte>(); var one = new byte[1];
            while (await stream.ReadAsync(one, 0, 1) != 0)
            {
                if (one[0] == '\n') return Encoding.UTF8.GetString(bytes.ToArray()).TrimEnd('\r');
                bytes.Add(one[0]);
            }
            return null;
        }
        public async ValueTask DisposeAsync()
        {
            Stop.Cancel(); listener.Stop();
            lock (clients) foreach (var client in clients) client.Dispose();
            await accepting;
            await Task.WhenAll(workers);
            Stop.Dispose();
        }
    }

    static async Task Prompt(Task task)
    {
        Assert.That(await Task.WhenAny(task, Task.Delay(2500)), Is.SameAs(task), "Connection operation did not complete promptly");
    }

    [TestCase("AUTH")][TestCase("SELECT")][TestCase("CLIENT")][TestCase("INFO")][TestCase("TLS")]
    public async Task Cancellation_interrupts_every_handshake_stage_and_closes_socket(string stage)
    {
        await using var server = new Server { PauseCommand = stage, Raw = stage == "TLS" };
        using var redis = new RedisClient(new RedisEndpoint("localhost", server.Port)
        {
            Username = "user", Password = "secret", Db = 2, Client = "async-test", Ssl = stage == "TLS", ConnectTimeout = 5000
        });
        using var cts = new CancellationTokenSource(4000);
        var clock = Stopwatch.StartNew();
        var pending = redis.ConnectAsync(cts.Token).AsTask();
        Assert.That(clock.ElapsedMilliseconds, Is.LessThan(1000), "Setup blocked its caller");
        await Prompt(server.Paused.Task);
        cts.Cancel();
        await Prompt(pending);
        try { await pending; Assert.Fail("Expected cancellation"); }
        catch (OperationCanceledException ex) { Assert.That(ex.CancellationToken, Is.EqualTo(cts.Token)); }
        Assert.That(redis.HasConnected, Is.False);
        await Prompt(server.Closed.Task);
        Assert.That(RedisNativeClient.ServerVersionNumber, Is.EqualTo(0), "Cancelled INFO must not cache an unsupported version");
    }

    [TestCase("AUTH")][TestCase("SELECT")][TestCase("CLIENT")][TestCase("INFO")][TestCase("TLS")]
    public async Task Connect_timeout_bounds_every_handshake_stage(string stage)
    {
        await using var server = new Server { PauseCommand = stage, Raw = stage == "TLS" };
        using var redis = new RedisClient(new RedisEndpoint("localhost", server.Port)
        {
            Password = "secret", Db = 1, Client = "deadline", Ssl = stage == "TLS", ConnectTimeout = 250
        });
        var clock = Stopwatch.StartNew();
        var pending = ((IRedisClientAsync)redis).PingAsync().AsTask();
        await Prompt(pending);
        try { await pending; Assert.Fail("Expected connection timeout"); }
        catch (TimeoutException ex) { Assert.That(ex.Message, Does.Contain("ConnectTimeout")); }
        Assert.That(clock.ElapsedMilliseconds, Is.LessThan(1800));
        Assert.That(redis.HasConnected, Is.False);
        await Prompt(server.Closed.Task);
    }

    [Test]
    public async Task Deadline_is_shared_across_auth_select_and_client_name()
    {
        await using var server = new Server(async (_, _) => { await Task.Delay(180); return "+OK\r\n"; }) { PauseCommand = "CLIENT" };
        using var redis = new RedisClient(new RedisEndpoint("127.0.0.1", server.Port)
        {
            Password = "secret", Db = 1, Client = "deadline", ConnectTimeout = 450
        });
        var clock = Stopwatch.StartNew();
        try { await redis.ConnectAsync(); Assert.Fail("Expected shared deadline"); }
        catch (TimeoutException) { }
        Assert.That(clock.ElapsedMilliseconds, Is.InRange(350, 850));
        Assert.That(server.Commands.Select(x => x[0]), Is.EqualTo(new[] { "AUTH", "SELECT", "CLIENT" }));
    }

    [TestCase(false)][TestCase(true)]
    public async Task Initialization_errors_close_the_socket_and_do_not_retry_auth(bool selection)
    {
        await using var server = new Server((_, _) => Task.FromResult("-ERR denied\r\n"));
        using var redis = new RedisClient(new RedisEndpoint("127.0.0.1", server.Port)
        {
            Password = selection ? null : "secret", Db = selection ? 1 : 0, ConnectTimeout = 1000
        });
        try { await redis.ConnectAsync(); Assert.Fail("Expected error"); }
        catch (RedisException ex) { Assert.That(ex.Message, Does.Contain("denied")); }
        Assert.That(redis.HasConnected, Is.False);
        Assert.That(server.Connections, Is.EqualTo(1));
    }

    [Test]
    public async Task Info_incompatible_proxy_reconnects_asynchronously_before_sending_application_command()
    {
        await using var server = new Server((_, args) => Task.FromResult(args[0] == "INFO" ? null : "+PONG\r\n"));
        using var redis = new RedisClient(server.Address);
        Assert.That(await ((IRedisClientAsync)redis).PingAsync(), Is.True);
        Assert.That(server.Connections, Is.EqualTo(2));
        Assert.That(server.Commands.Select(x => x[0]), Is.EqualTo(new[] { "INFO", "PING" }));
        Assert.That(RedisNativeClient.ServerVersionNumber, Is.EqualTo(-1));
    }

    [Test]
    public async Task Precancelled_setup_opens_no_socket()
    {
        await using var server = new Server();
        using var redis = new RedisClient(server.Address);
        using var cts = new CancellationTokenSource(); cts.Cancel();
        try { await redis.ConnectAsync(cts.Token); Assert.Fail("Expected cancellation"); }
        catch (OperationCanceledException ex) { Assert.That(ex.CancellationToken, Is.EqualTo(cts.Token)); }
        Assert.That(server.Connections, Is.Zero);
    }

    [TestCase("pooled")][TestCase("bounded")][TestCase("overflow")][TestCase("basic")]
    public async Task Cold_manager_acquisition_verifies_master_asynchronously_and_releases_cancelled_creation(string kind)
    {
        await using var server = new Server((_, args) => Task.FromResult(args[0] == "INFO"
            ? "$21\r\nredis_version:8.0.0\r\n\r\n" : "+OK\r\n")) { PauseCommand = "ROLE" };
        using var manager = Manager(kind, server.Address);
        using var cts = new CancellationTokenSource(4000);
        var pending = ((IRedisClientsManagerAsync)manager).GetClientAsync(cts.Token).AsTask();
        Assert.That(pending.IsCompleted, Is.False);
        await Prompt(server.Paused.Task);
        cts.Cancel(); await Prompt(pending);
        try { await pending; Assert.Fail("Expected cancellation"); }
        catch (OperationCanceledException ex) { Assert.That(ex.CancellationToken, Is.EqualTo(cts.Token)); }
        await Prompt(server.Closed.Task);
        if (manager is RedisManagerPool pool && pool.MaxLiveConnections != null)
        {
            Assert.That(pool.GetStats()["clientsLive"], Is.EqualTo("0"));
            Assert.That(pool.GetStats()["clientsCreating"], Is.EqualTo("0"));
        }
        manager.RedisResolver.ResetMasters(new[] { TestConfig.SingleHost });
        await using var next = await ((IRedisClientsManagerAsync)manager).GetClientAsync();
        Assert.That(await next.PingAsync(), Is.True);
    }

    static IRedisClientsManager Manager(string kind, string address) => kind switch
    {
        "pooled" => new PooledRedisClientManager(new[] { address }, new[] { address }, new RedisClientManagerConfig { MaxWritePoolSize = 1, MaxReadPoolSize = 1 }),
        "bounded" => new RedisManagerPool(address, new RedisPoolConfig { MaxPoolSize = 1, MaxLiveConnections = 1, MaxWaitingClients = 1 }),
        "overflow" => new RedisManagerPool(address, new RedisPoolConfig { MaxPoolSize = 1 }),
        _ => new BasicRedisClientManager(address)
    };

    [TestCase(false)][TestCase(true)]
    public async Task Pooled_database_reset_is_async_and_cancelled_lease_can_be_replaced(bool reader)
    {
        await using var server = new Server { PauseCommand = "SELECT" };
        using var manager = new PooledRedisClientManager(new[] { server.Address }, new[] { server.Address },
            new RedisClientManagerConfig { MaxWritePoolSize = 1, MaxReadPoolSize = 1 }, 2, null, null);
        var resolver = new BasicRedisResolver(new[] { server.Address.ToRedisEndpoint() }, new[] { server.Address.ToRedisEndpoint() });
        manager.RedisResolver = resolver;
        // First lease uses SELECT during its lazy handshake, then a reused open lease needs an async SELECT reset.
        var asyncManager = (IRedisClientsManagerAsync)manager;
        await using (var first = await (reader ? asyncManager.GetReadOnlyClientAsync() : asyncManager.GetClientAsync()))
        {
            ((RedisClient)first).Db = 0; // still lazy
            await ((RedisClient)first).ConnectAsync();
        }
        using var cts = new CancellationTokenSource(4000);
        var pending = (reader ? asyncManager.GetReadOnlyClientAsync(cts.Token) : asyncManager.GetClientAsync(cts.Token)).AsTask();
        await Prompt(server.Paused.Task); cts.Cancel(); await Prompt(pending);
        try { await pending; Assert.Fail("Expected cancelled reset"); } catch (OperationCanceledException) { }
        manager.FailoverTo(TestConfig.SingleHost);
        await using var next = await (reader ? asyncManager.GetReadOnlyClientAsync() : asyncManager.GetClientAsync());
        Assert.That(await next.PingAsync(), Is.True);
        Assert.That(((RedisClient)next).Db, Is.EqualTo(2));
    }

    [TestCase("pooled", "INFO")][TestCase("basic", "INFO")]
    [TestCase("pooled", "ROLE")][TestCase("basic", "ROLE")]
    public async Task Manager_timeout_applies_before_role_verification(string kind, string stage)
    {
        await using var server = new Server { PauseCommand = stage };
        using var manager = Manager(kind, server.Address);
        if (manager is PooledRedisClientManager pooled) pooled.ConnectTimeout = 200;
        if (manager is BasicRedisClientManager basic) basic.ConnectTimeout = 200;
        var clock = Stopwatch.StartNew();
        var pending = ((IRedisClientsManagerAsync)manager).GetClientAsync().AsTask();
        await Prompt(pending);
        try { await pending; Assert.Fail("Expected connection timeout"); } catch (TimeoutException) { }
        Assert.That(clock.ElapsedMilliseconds, Is.LessThan(1200));
    }

    [Test]
    public async Task Async_hook_is_awaited_once_per_connection_and_supports_commands()
    {
        using var redis = new RedisClient(TestConfig.SingleHost);
#if DEBUG
        redis.DebugAllowSync = false;
#endif
        var syncCalls = 0; var asyncCalls = 0;
        redis.ConnectionFilter = _ => syncCalls++;
        redis.ConnectionFilterAsync = async (client, token) =>
        {
            await Task.Yield();
            Assert.That(await client.PingAsync(token), Is.True);
            asyncCalls++;
        };
        await redis.ConnectAsync(); await redis.ConnectAsync();
        Assert.That(syncCalls, Is.EqualTo(1)); Assert.That(asyncCalls, Is.EqualTo(1));
    }

    [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
    public async Task Cold_version_dependent_APIs_do_not_use_synchronous_IO(int operation)
    {
        using var redis = new RedisClient(TestConfig.SingleHost);
#if DEBUG
        redis.DebugAllowSync = false;
#endif
        var client = (IRedisClientAsync)redis;
        var key = "async-connect:" + Guid.NewGuid().ToString("N");
        try
        {
            switch (operation)
            {
                case 0: await client.SetValueAsync(key, "value", TimeSpan.FromSeconds(10)); break;
                case 1: await ((ICacheClientAsync)redis).SetAsync(key, "value", TimeSpan.FromSeconds(10)); break;
                case 2: await client.ExpireEntryInAsync(key, TimeSpan.FromMilliseconds(1500)); break;
                case 3: await client.ExpireEntryAtAsync(key, DateTime.UtcNow.AddMinutes(1)); break;
                case 4: Assert.That(await client.GetServerRoleAsync(), Is.EqualTo(RedisServerRole.Master)); break;
                case 5: await using (var transaction = await client.CreateTransactionAsync()) { await transaction.RollbackAsync(); } break;
            }
            Assert.That(redis.HasConnected, Is.True);
        }
        finally { await client.RemoveAsync(key); }
    }
    [Test]
    public async Task Disposed_client_cannot_open_a_new_connection()
    {
        using var redis = new RedisClient(TestConfig.SingleHost);
        redis.Dispose();
        try { await redis.ConnectAsync(); Assert.Fail("Expected disposed client"); } catch (ObjectDisposedException) { }
        Assert.That(redis.HasConnected, Is.False);
    }

    [Test]
    public async Task Refused_connection_closes_the_attempted_socket()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        using var redis = new RedisClient(new RedisEndpoint("localhost", port) { ConnectTimeout = 1000 });
        try { await redis.ConnectAsync(); Assert.Fail("Expected refused connection"); } catch (SocketException) { }
        Assert.That(redis.HasConnected, Is.False);
    }

    [Test]
    public async Task Dns_failure_leaves_no_connection()
    {
        using var redis = new RedisClient(new RedisEndpoint(Guid.NewGuid().ToString("N") + ".invalid", 6379) { ConnectTimeout = 1000 });
        try { await redis.ConnectAsync(); Assert.Fail("Expected lookup failure"); }
        catch (SocketException) { } catch (TimeoutException) { }
        Assert.That(redis.HasConnected, Is.False);
    }

    [TestCase("::1")][TestCase("localhost")]
    public async Task Async_IPv6_and_hostname_connections_reconnect_through_pool_failover(string host)
    {
        var port = Environment.GetEnvironmentVariable("REDIS_IPV6_PORT");
        if (port == null) Assert.Ignore("Set REDIS_IPV6_PORT for the isolated dual-stack fixture");
        var endpoint = new RedisEndpoint(host, int.Parse(port)) { ConnectTimeout = 1500 };
        using var manager = new RedisManagerPool(endpoint.ToString());
        await using (var client = await ((IRedisClientsManagerAsync)manager).GetClientAsync())
            Assert.That(await client.PingAsync(), Is.True);
        manager.FailoverTo(endpoint.ToString());
        await using var next = await ((IRedisClientsManagerAsync)manager).GetClientAsync();
        Assert.That(await next.PingAsync(), Is.True);
    }

    [Test]
    public async Task Async_TLS_validates_certificate_and_preserves_ACL_database_and_client_name()
    {
        var port = Environment.GetEnvironmentVariable("REDIS_TLS_PORT");
        var certPath = Environment.GetEnvironmentVariable("REDIS_TLS_CERT");
        var plainPort = Environment.GetEnvironmentVariable("REDIS_IPV6_PORT");
        if (port == null || certPath == null) Assert.Ignore("Set REDIS_TLS_PORT and REDIS_TLS_CERT for the isolated TLS fixture");
#if NET10_0_OR_GREATER
        using var certificate = X509CertificateLoader.LoadCertificateFromFile(certPath);
#else
        using var certificate = new X509Certificate2(certPath);
#endif
        var previous = RedisConfig.CertificateValidationCallback;
        var user = "async:" + Guid.NewGuid().ToString("N");
        const string password = "test:@/+?%";
        using var admin = new RedisClient(new RedisEndpoint("::1", int.Parse(plainPort)));
        admin.Custom("ACL", "SETUSER", user, "on", ">" + password, "~*", "+@all");
        try
        {
            RedisConfig.CertificateValidationCallback = (_, cert, _, errors) =>
                (errors & (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable)) == 0
                && cert.GetCertHashString() == certificate.GetCertHashString();
            var uri = $"rediss://{Uri.EscapeDataString(user)}:{Uri.EscapeDataString(password)}@[::1]:{port}/2?Client=async-tls&ConnectTimeout=1500&RetryTimeout=1";
            using var manager = new RedisManagerPool(uri);
            await using (var client = await ((IRedisClientsManagerAsync)manager).GetClientAsync())
            {
                Assert.That(await client.PingAsync(), Is.True);
                var native = (RedisClient)client;
                Assert.That(native.Username, Is.EqualTo(user)); Assert.That(native.Db, Is.EqualTo(2));
                var info = await client.GetClientsInfoAsync();
                Assert.That(info.Any(x => x.TryGetValue("name", out var name) && name == "async-tls" && x["db"] == "2" && x["user"] == user), Is.True);
                using var clone = native.CloneClient();
                Assert.That(await ((IRedisClientAsync)clone).PingAsync(), Is.True);
            }
            using var mismatch = new RedisClient($"rediss://127.0.0.1:{port}?ConnectTimeout=1500&RetryTimeout=1");
            Assert.CatchAsync(async () => await mismatch.ConnectAsync());
            Assert.That(mismatch.HasConnected, Is.False);
            RedisConfig.CertificateValidationCallback = null;
            using var untrusted = new RedisClient($"rediss://[::1]:{port}?ConnectTimeout=1500&RetryTimeout=1");
            Assert.CatchAsync(async () => await untrusted.ConnectAsync());
            Assert.That(untrusted.HasConnected, Is.False);
        }
        finally { RedisConfig.CertificateValidationCallback = previous; admin.Custom("ACL", "DELUSER", user); }
    }

}
