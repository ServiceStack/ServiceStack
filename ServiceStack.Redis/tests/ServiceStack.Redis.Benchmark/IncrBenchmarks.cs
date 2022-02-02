using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Pipelines.Sockets.Unofficial;
using Respite;
using StackExchange.Redis;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Benchmark
{
    [SimpleJob(RuntimeMoniker.Net472)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [Orderer(SummaryOrderPolicy.Method, MethodOrderPolicy.Alphabetical)]
    [CategoriesColumn]
    public class IncrBenchmarks
    {
        ConnectionMultiplexer _seredis;
        IServer _seredis_server;
        IDatabase _seredis_db;
        RedisClient _ssredis;
        IRedisClientAsync _ssAsync;
        RespConnection _respite;

        static IncrBenchmarks()
        {
            RedisClient.NewFactoryFn = () => new RedisClient("127.0.0.1", 6379);
        }

        [GlobalSetup]
        public Task Setup() => Setup(false);
        internal async Task Setup(bool minimal)
        {
            _ssredis = RedisClient.New();
            _ssAsync = _ssredis;
            
            if (!minimal)
            {
                _seredis = await ConnectionMultiplexer.ConnectAsync("127.0.0.1:6379");
                _seredis_server = _seredis.GetServer(_seredis.GetEndPoints().Single());
                _seredis_db = _seredis.GetDatabase();

                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SocketConnection.SetRecommendedClientOptions(socket);
                socket.Connect("127.0.0.1", 6379);

                _respite = RespConnection.Create(socket);
            }
        }

        [GlobalCleanup]
        public async Task Teardown()
        {
            _seredis?.Dispose();
            _ssredis?.Dispose();
            if (_respite != null) await _respite.DisposeAsync();

            _seredis_server = null;
            _seredis_db = null;
            _seredis = null;
            _ssredis = null;
            _respite = null;
            _ssAsync = null;
        }

        const string Key = "my_key";
#if DEBUG
        const int PER_TEST = 10;
#else
        const int PER_TEST = 1000;
#endif

        [BenchmarkCategory("IncrAsync")]
        [Benchmark(Description = "SERedis", OperationsPerInvoke = PER_TEST)]
        public async Task<long> SERedisIncrAsync()
        {
            long last = default;
            await _seredis_db.KeyDeleteAsync(Key);
            for (int i = 0; i < PER_TEST; i++)
            {
                last = await _seredis_db.StringIncrementAsync(Key);
            }
            return last;
        }

        [BenchmarkCategory("IncrSync")]
        [Benchmark(Description = "SERedis", OperationsPerInvoke = PER_TEST)]
        public long SERedisIncrSync()
        {
            long last = default;
            _seredis_db.KeyDelete(Key);
            for (int i = 0; i < PER_TEST; i++)
            {
                last = _seredis_db.StringIncrement(Key);
            }
            return last;
        }

        [BenchmarkCategory("PipelineIncrAsync")]
        [Benchmark(Description = "SERedis", OperationsPerInvoke = PER_TEST)]
        public async Task<long> SERedisPipelineIncrAsync()
        {
            var last = Task.FromResult(0L);
            await _seredis_db.KeyDeleteAsync(Key);
            var batch = _seredis_db.CreateBatch();
            for (int i = 0; i < PER_TEST; i++)
            {
                last = batch.StringIncrementAsync(Key);
            }
            batch.Execute();
            return await last;
        }

        [BenchmarkCategory("TransactionIncrAsync")]
        [Benchmark(Description = "SERedis", OperationsPerInvoke = PER_TEST)]
        public async Task<long> SERedisTransactionIncrAsync()
        {
            var last = Task.FromResult(0L);
            await _seredis_db.KeyDeleteAsync(Key);
            var batch = _seredis_db.CreateTransaction();
            for (int i = 0; i < PER_TEST; i++)
            {
                last = batch.StringIncrementAsync(Key);
            }
            await batch.ExecuteAsync();
            return await last;
        }

        [BenchmarkCategory("TransactionIncrSync")]
        [Benchmark(Description = "SERedis", OperationsPerInvoke = PER_TEST)]
        public async Task<long> SERedisTransactionIncrSync()
        {
            var last = Task.FromResult(0L);
            _seredis_db.KeyDelete(Key);
            var batch = _seredis_db.CreateTransaction();
            for (int i = 0; i < PER_TEST; i++)
            {
                last = batch.StringIncrementAsync(Key);
            }
            batch.Execute();
            return await last;
        }

        [BenchmarkCategory("IncrAsync")]
        [Benchmark(Description = "SSRedis", OperationsPerInvoke = PER_TEST)]
        public async Task<long> SSRedisIncrAsync()
        {
            long last = default;
            _ssredis.Del(Key); // todo: asyncify
            for (int i = 0; i < PER_TEST; i++)
            {
                last = await _ssAsync.IncrementValueAsync(Key);
            }
            return last;
        }


        [BenchmarkCategory("IncrSync")]
        [Benchmark(Description = "SSRedis", OperationsPerInvoke = PER_TEST)]
        public long SSRedisIncrSync()
        {
            long last = default;
            _ssredis.Del(Key);
            for (int i = 0; i < PER_TEST; i++)
            {
                last = _ssredis.IncrementValue(Key);
            }
            return last;
        }

        [BenchmarkCategory("PipelineIncrSync")]
        [Benchmark(Description = "SSRedis", OperationsPerInvoke = PER_TEST)]
        public long SSRedisPipelineIncrSync()
        {
            long last = default;
            _ssredis.Del(Key);
            using var trans = _ssredis.CreatePipeline();
            for (int i = 0; i < PER_TEST; i++)
            {
                trans.QueueCommand(r => r.IncrementValue(Key), l => last = l);
            }
            trans.Flush();
            return last;
        }

        [BenchmarkCategory("TransactionIncrSync")]
        [Benchmark(Description = "SSRedis", OperationsPerInvoke = PER_TEST)]
        public long SSRedisTransactionIncrSync()
        {
            long last = default;
            _ssredis.Del(Key);
            using var trans = _ssredis.CreateTransaction();
            for (int i = 0; i < PER_TEST; i++)
            {
                trans.QueueCommand(r => r.IncrementValue(Key), l => last = l);
            }
            trans.Commit();
            return last;
        }

        [BenchmarkCategory("PipelineIncrAsync")]
        [Benchmark(Description = "SSRedis", OperationsPerInvoke = PER_TEST)]
        public async Task<long> SSRedisPipelineIncrAsync()
        {
            long last = default;
            _ssredis.Del(Key); // todo: asyncify
            await using var trans = _ssAsync.CreatePipeline();
            for (int i = 0; i < PER_TEST; i++)
            {
                trans.QueueCommand(r => r.IncrementValueAsync(Key), l => last = l);
            }
            await trans.FlushAsync();
            return last;
        }

        [BenchmarkCategory("TransactionIncrAsync")]
        [Benchmark(Description = "SSRedis", OperationsPerInvoke = PER_TEST)]
        public async Task<long> SSRedisTransactionIncrAsync()
        {
            long last = default;
            _ssredis.Del(Key); // todo: asyncify
            await using var trans = await _ssAsync.CreateTransactionAsync();
            for (int i = 0; i < PER_TEST; i++)
            {
                trans.QueueCommand(r => r.IncrementValueAsync(Key), l => last = l);
            }
            await trans.CommitAsync();
            return last;
        }


        //static readonly RespValue s_Time = RespValue.CreateAggregate(
        //    RespType.Array, RespValue.Create(RespType.BlobString, "time"));

        //static DateTime ParseTime(in RespValue value)
        //{
        //    var parts = value.SubItems;
        //    if (parts.TryGetSingleSpan(out var span))
        //        return Parse(span[0], span[1]);
        //    return Slow(parts);
        //    static DateTime Slow(in ReadOnlyBlock<RespValue> parts)
        //    {
        //        var iter = parts.GetEnumerator();
        //        if (!iter.MoveNext()) Throw();
        //        var seconds = iter.Current;
        //        if (!iter.MoveNext()) Throw();
        //        var microseconds = iter.Current;
        //        return Parse(seconds, microseconds);
        //        static void Throw() => throw new InvalidOperationException();
        //    }

        //    static DateTime Parse(in RespValue seconds, in RespValue microseconds)
        //        => Epoch.AddSeconds(seconds.ToInt64()).AddMilliseconds(microseconds.ToInt64() / 1000.0);
        //}
        //static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        //[BenchmarkCategory("IncrSync")]
        //[Benchmark(Description = "Respite", OperationsPerInvoke = PER_TEST)]
        //public void RespiteTimeSync()
        //{
        //    for (int i = 0; i < PER_TEST; i++)
        //    {
        //        _respite.Call(s_Time, val => ParseTime(val));
        //    }
        //}

        //[BenchmarkCategory("IncrAsync")]
        //[Benchmark(Description = "Respite", OperationsPerInvoke = PER_TEST)]
        //public async Task RespiteTimeAsync()
        //{
        //    for (int i = 0; i < PER_TEST; i++)
        //    {
        //        await _respite.CallAsync(s_Time, val => ParseTime(val));
        //    }
        //}
    }
}
