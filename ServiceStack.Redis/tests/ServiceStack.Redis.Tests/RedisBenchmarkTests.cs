using System;
using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Ignore("Benchmark")]
    public class RedisBenchmarkTests
        : RedisClientTestsBase
    {
        const string Value = "Value";

        [Test]
        public void Measure_pipeline_speedup()
        {
            string key = "key";
            int total = 500;
            var temp = new byte[1];
            for (int i = 0; i < total; ++i)
            {
                Redis.Del(key + i.ToString());
            }
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < total; ++i)
            {
                ((RedisNativeClient)Redis).Set(key + i.ToString(), temp);
            }
            sw.Stop();
            Debug.WriteLine(String.Format("Time for {0} Set(key,value) operations: {1} ms", total, sw.ElapsedMilliseconds));

            for (int i = 0; i < total; ++i)
            {
                Redis.Del(key + i.ToString());
            }
            sw = Stopwatch.StartNew();
            using (var pipeline = Redis.CreatePipeline())
            {
                for (int i = 0; i < total; ++i)
                    pipeline.QueueCommand(r => ((RedisNativeClient)Redis).Set(key + i.ToString(), temp));
                pipeline.Flush();

            }
            sw.Stop();
            Debug.WriteLine(String.Format("Time for pipelining {0} Set(key,value) operations: {1} ms", total, sw.ElapsedMilliseconds));
        }

        private string[] stringsFromBytes(byte[][] input)
        {
            if (input == null || input.Length == 0)
            {
                return new string[1];

            }
            var rc = new string[input.Length];
            for (int i = 0; i < input.Length; ++i)
            {
                rc[i] = input[i].FromUtf8Bytes();
            }
            return rc;
        }

        [Test]
        public void Compare_sort_nosort_to_smembers_mget()
        {
            string setKey = "setKey";
            int total = 25;
            int count = 20;
            var temp = new byte[1];
            byte fixedValue = 124;
            temp[0] = fixedValue;

            //initialize set and individual keys
            Redis.Del(setKey);
            for (var i = 0; i < total; ++i)
            {
                string key = setKey + i;
                Redis.SAdd(setKey, key.ToUtf8Bytes());
                Redis.Set(key, temp);
            }

            var sw = Stopwatch.StartNew();

            byte[][] results = null;
            for (int i = 0; i < count; ++i)
            {
                var keys = Redis.SMembers(setKey);
                results = Redis.MGet(keys);

            }

            sw.Stop();

            //make sure that results are valid
            foreach (var result in results)
            {
                Assert.AreEqual(result[0], fixedValue);
            }

            Debug.WriteLine(String.Format("Time to call {0} SMembers and MGet operations: {1} ms", count, sw.ElapsedMilliseconds));
            var opt = new SortOptions() { SortPattern = "nosort", GetPattern = "*" };

            sw = Stopwatch.StartNew();
            for (int i = 0; i < count; ++i)
                results = Redis.Sort(setKey, opt);
            sw.Stop();

            //make sure that results are valid
            foreach (var result in results)
            {
                Assert.AreEqual(result[0], fixedValue);
            }

            Debug.WriteLine(String.Format("Time to call {0} sort operations: {1} ms", count, sw.ElapsedMilliseconds));
        }
    }

    [TestFixture, Ignore("Benchmark")]
    public class RawBytesSetBenchmark
    {
        public void Run(string name, int nBlockSizeBytes, Action<int, byte[]> fn)
        {
            Stopwatch sw;
            long ms1, ms2, interval;
            int nBytesHandled = 0;
            int nMaxIterations = 5;
            byte[] pBuffer = new byte[nBlockSizeBytes];

            // Create Redis Wrapper
            var redis = new RedisNativeClient();

            // Clear DB
            redis.FlushAll();

            sw = Stopwatch.StartNew();
            ms1 = sw.ElapsedMilliseconds;
            for (int i = 0; i < nMaxIterations; i++)
            {
                fn(i, pBuffer);
                nBytesHandled += nBlockSizeBytes;
            }

            ms2 = sw.ElapsedMilliseconds;
            interval = ms2 - ms1;

            // Calculate rate
            double dMBPerSEc = nBytesHandled / 1024.0 / 1024.0 / (interval / 1000.0);
            Console.WriteLine(name + ": Rate {0:N4}, Total: {1}ms", dMBPerSEc, ms2);
        }

        [Test]
        public void Benchmark_SET_raw_bytes_8MB_ServiceStack()
        {
            var redis = new RedisNativeClient();

            Run("ServiceStack.Redis 8MB", 8000000,
                (i, bytes) => redis.Set("eitan" + i.ToString(), bytes));
        }

        [Test]
        public void Benchmark_SET_raw_bytes_1MB_ServiceStack()
        {
            var redis = new RedisNativeClient();

            Run("ServiceStack.Redis 1MB", 1000000,
                (i, bytes) => redis.Set("eitan" + i.ToString(), bytes));
        }

        [Test]
        public void Benchmark_SET_raw_bytes_100k_ServiceStack()
        {
            var redis = new RedisNativeClient();

            Run("ServiceStack.Redis 100K", 100000,
                (i, bytes) => redis.Set("eitan" + i.ToString(), bytes));
        }

        [Test]
        public void Benchmark_SET_raw_bytes_10k_ServiceStack()
        {
            var redis = new RedisNativeClient();

            Run("ServiceStack.Redis 10K", 10000,
                (i, bytes) => redis.Set("eitan" + i.ToString(), bytes));
        }

        [Test]
        public void Benchmark_SET_raw_bytes_1k_ServiceStack()
        {
            var redis = new RedisNativeClient();

            Run("ServiceStack.Redis 1K", 1000,
                (i, bytes) => redis.Set("eitan" + i.ToString(), bytes));
        }
    }

}