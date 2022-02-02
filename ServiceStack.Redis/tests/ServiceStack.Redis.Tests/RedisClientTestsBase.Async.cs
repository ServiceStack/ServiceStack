using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    public static class AsyncExtensions
    {
        public static async ValueTask ForEachAsync<T>(this List<T> source, Func<T, ValueTask> action)
        {
            foreach (var item in source)
                await action(item).ConfigureAwait(false);
        }
        public static async ValueTask ForEachAsync<TKey, TValue>(this Dictionary<TKey, TValue> source, Func<TKey, TValue, ValueTask> action)
        {
            foreach (var item in source)
                await action(item.Key, item.Value).ConfigureAwait(false);
        }
        public static async ValueTask TimesAsync(this int times, Func<int, ValueTask> action)
        {
            for (int i = 0; i < times; i++)
            {
                await action(i).ConfigureAwait(false);
            }
        }
        public static async ValueTask<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
        {
            var list = new List<T>();
            await foreach (var item in source.ConfigureAwait(false))
                list.Add(item);
            return list;
        }

        public static async ValueTask<int> CountAsync<T>(this IAsyncEnumerable<T> source)
        {
            int count = 0;
            await foreach (var item in source.ConfigureAwait(false))
                count++;
            return count;
        }

        public static IRedisClientAsync ForAsyncOnly(this RedisClient client)
        {
#if DEBUG
            if (client is object) client.DebugAllowSync = false;
#endif
            return client;
        }

        public static async IAsyncEnumerable<T> TakeAsync<T>(this IAsyncEnumerable<T> source, int count)
        {
            await foreach (var item in source.ConfigureAwait(false))
            {
                if (count > 0)
                {
                    count--;
                    yield return item;
                }
            }
        }

        public static async ValueTask<Dictionary<TKey, TValue>> ToDictionaryAsync<T, TKey, TValue>(this IAsyncEnumerable<T> source, Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        {
            var result = new Dictionary<TKey, TValue>();
            await foreach (var item in source.ConfigureAwait(false))
            {
                result.Add(keySelector(item), valueSelector(item));
            }
            return result;
        }
    }
    public class RedisClientTestsBaseAsyncTests // testing the base class features
        : RedisClientTestsBaseAsync
    {
        [Test]
        public void DetectUnexpectedSync()
        {
    #if DEBUG
            Assert.False(RedisRaw.DebugAllowSync, nameof(RedisRaw.DebugAllowSync));
            var ex = Assert.Throws<InvalidOperationException>(() => RedisRaw.Ping());
            Assert.AreEqual("Unexpected synchronous operation detected from 'SendReceive'", ex.Message);
    #endif
        }
    }

    [Category("Async")]
    public abstract class RedisClientTestsBaseAsync : RedisClientTestsBase
    {
        protected IRedisClientAsync RedisAsync => base.Redis;
        protected IRedisNativeClientAsync NativeAsync => base.Redis;

        [Obsolete("This should use RedisAsync or RedisRaw", true)]
        protected new RedisClient Redis => base.Redis;

        protected RedisClient RedisRaw
        {
            get => base.Redis;
            set => base.Redis = value;
        }

        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();
            _ = RedisRaw.ForAsyncOnly();
        }
        public override void OnAfterEachTest()
        {
#if DEBUG
            if(RedisRaw is object) RedisRaw.DebugAllowSync = true;
#endif
            base.OnAfterEachTest();
        }

        protected static async ValueTask<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source, CancellationToken token = default)
        {
            var list = new List<T>();
            await foreach (var value in source.ConfigureAwait(false).WithCancellation(token))
            {
                list.Add(value);
            }
            return list;
        }
    }
}