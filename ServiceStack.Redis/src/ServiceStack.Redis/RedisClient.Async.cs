//
// https://github.com/ServiceStack/ServiceStack.Redis/
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.Model;
using ServiceStack.Redis.Generic;
using ServiceStack.Redis.Internal;
using ServiceStack.Redis.Pipeline;
using ServiceStack.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis
{
    partial class RedisClient : IRedisClientAsync, IRemoveByPatternAsync, ICacheClientAsync, IAsyncDisposable
    {
        /// <summary>
        /// Access this instance for async usage
        /// </summary>
        public IRedisClientAsync AsAsync() => this;

        // the typed client implements this for us
        IRedisTypedClientAsync<T> IRedisClientAsync.As<T>() => (IRedisTypedClientAsync<T>)As<T>();

        // convenience since we're not saturating the public API; this makes it easy to call
        // the explicit interface implementations; the JIT should make this a direct call
        private IRedisNativeClientAsync NativeAsync => this;

        IHasNamed<IRedisListAsync> IRedisClientAsync.Lists => Lists as IHasNamed<IRedisListAsync> ?? throw new NotSupportedException($"The provided Lists ({Lists?.GetType().FullName}) does not support IRedisListAsync");
        IHasNamed<IRedisSetAsync> IRedisClientAsync.Sets => Sets as IHasNamed<IRedisSetAsync> ?? throw new NotSupportedException($"The provided Sets ({Sets?.GetType().FullName})does not support IRedisSetAsync");
        IHasNamed<IRedisSortedSetAsync> IRedisClientAsync.SortedSets => SortedSets as IHasNamed<IRedisSortedSetAsync> ?? throw new NotSupportedException($"The provided SortedSets ({SortedSets?.GetType().FullName})does not support IRedisSortedSetAsync");
        IHasNamed<IRedisHashAsync> IRedisClientAsync.Hashes => Hashes as IHasNamed<IRedisHashAsync> ?? throw new NotSupportedException($"The provided Hashes ({Hashes?.GetType().FullName})does not support IRedisHashAsync");

        internal ValueTask RegisterTypeIdAsync<T>(T value, CancellationToken token)
        {
            var typeIdsSetKey = GetTypeIdsSetKey<T>();
            var id = value.GetId().ToString();

            return RegisterTypeIdAsync(typeIdsSetKey, id, token);
        }
        internal ValueTask RegisterTypeIdAsync(string typeIdsSetKey, string id, CancellationToken token)
        {
            if (this.Pipeline != null)
            {
                var registeredTypeIdsWithinPipeline = GetRegisteredTypeIdsWithinPipeline(typeIdsSetKey);
                registeredTypeIdsWithinPipeline.Add(id);
                return default;
            }
            else
            {
                return AsAsync().AddItemToSetAsync(typeIdsSetKey, id, token);
            }
        }

        // Called just after original Pipeline is closed.
        internal async ValueTask AddTypeIdsRegisteredDuringPipelineAsync(CancellationToken token)
        {
            foreach (var entry in registeredTypeIdsWithinPipelineMap)
            {
                await AsAsync().AddRangeToSetAsync(entry.Key, entry.Value.ToList(), token).ConfigureAwait(false);
            }
            registeredTypeIdsWithinPipelineMap = new Dictionary<string, HashSet<string>>();
        }


        ValueTask<DateTime> IRedisClientAsync.GetServerTimeAsync(CancellationToken token)
            => NativeAsync.TimeAsync(token).Await(parts => ParseTimeResult(parts));

        IRedisPipelineAsync IRedisClientAsync.CreatePipeline()
            => new RedisAllPurposePipeline(this);

        ValueTask<IRedisTransactionAsync> IRedisClientAsync.CreateTransactionAsync(CancellationToken token)
        {
            AssertServerVersionNumber(); // pre-fetch call to INFO before transaction if needed
            return new RedisTransaction(this, true).AsValueTaskResult<IRedisTransactionAsync>(); // note that the MULTI here will be held and flushed async
        }

        ValueTask<bool> IRedisClientAsync.RemoveEntryAsync(string[] keys, CancellationToken token)
            => keys.Length == 0 ? default : NativeAsync.DelAsync(keys, token).IsSuccessAsync();

        private async ValueTask ExecAsync(Func<IRedisClientAsync, ValueTask> action)
        {
            using (JsConfig.With(new Text.Config { ExcludeTypeInfo = false }))
            {
                await action(this).ConfigureAwait(false);
            }
        }

        private async ValueTask<T> ExecAsync<T>(Func<IRedisClientAsync, ValueTask<T>> action)
        {
            using (JsConfig.With(new Text.Config { ExcludeTypeInfo = false }))
            {
                var ret = await action(this).ConfigureAwait(false);
                return ret;
            }
        }

        ValueTask IRedisClientAsync.SetValueAsync(string key, string value, CancellationToken token)
        {
            var bytesValue = value?.ToUtf8Bytes();
            return NativeAsync.SetAsync(key, bytesValue, token: token);
        }

        ValueTask<string> IRedisClientAsync.GetValueAsync(string key, CancellationToken token)
            => NativeAsync.GetAsync(key, token).FromUtf8BytesAsync();

        Task<T> ICacheClientAsync.GetAsync<T>(string key, CancellationToken token)
        {
            return ExecAsync(async r => {
                if (typeof(T) == typeof(byte[]))
                {
                    var ret = await ((IRedisNativeClientAsync) r).GetAsync(key, token).ConfigureAwait(false);
                    return (T) (object) ret;
                }
                else
                {
                    var val = await r.GetValueAsync(key, token).ConfigureAwait(false);
                    var ret = JsonSerializer.DeserializeFromString<T>(val);
                    return ret;
                }
            }).AsTask();
        }

        async ValueTask<List<string>> IRedisClientAsync.SearchKeysAsync(string pattern, CancellationToken token)
        {
            var list = new List<string>();
            await foreach (var value in ((IRedisClientAsync)this).ScanAllKeysAsync(pattern, token: token).WithCancellation(token).ConfigureAwait(false))
            {
                list.Add(value);
            }
            return list;
        }

        async IAsyncEnumerable<string> IRedisClientAsync.ScanAllKeysAsync(string pattern, int pageSize, [EnumeratorCancellation] CancellationToken token)
        {
            ScanResult ret = default;
            while (true)
            {
                ret = await (pattern != null // note ConfigureAwait is handled below
                    ? NativeAsync.ScanAsync(ret?.Cursor ?? 0, pageSize, match: pattern, token: token)
                    : NativeAsync.ScanAsync(ret?.Cursor ?? 0, pageSize, token: token)
                    ).ConfigureAwait(false);

                foreach (var key in ret.Results)
                {
                    yield return key.FromUtf8Bytes();
                }

                if (ret.Cursor == 0) break;
            }
        }

        ValueTask<RedisKeyType> IRedisClientAsync.GetEntryTypeAsync(string key, CancellationToken token)
            => NativeAsync.TypeAsync(key, token).Await((val, state) => state.ParseEntryType(val), this);

        ValueTask IRedisClientAsync.AddItemToSetAsync(string setId, string item, CancellationToken token)
            => NativeAsync.SAddAsync(setId, item.ToUtf8Bytes(), token).Await();

        ValueTask IRedisClientAsync.AddItemToListAsync(string listId, string value, CancellationToken token)
            => NativeAsync.RPushAsync(listId, value.ToUtf8Bytes(), token).Await();

        ValueTask<bool> IRedisClientAsync.AddItemToSortedSetAsync(string setId, string value, CancellationToken token)
            => ((IRedisClientAsync)this).AddItemToSortedSetAsync(setId, value, GetLexicalScore(value), token);

        ValueTask<bool> IRedisClientAsync.AddItemToSortedSetAsync(string setId, string value, double score, CancellationToken token)
            => NativeAsync.ZAddAsync(setId, score, value.ToUtf8Bytes(), token).IsSuccessAsync();

        ValueTask<bool> IRedisClientAsync.SetEntryInHashAsync(string hashId, string key, string value, CancellationToken token)
            => NativeAsync.HSetAsync(hashId, key.ToUtf8Bytes(), value.ToUtf8Bytes(), token).IsSuccessAsync();

        ValueTask IRedisClientAsync.SetAllAsync(IDictionary<string, string> map, CancellationToken token)
            => GetSetAllBytes(map, out var keyBytes, out var valBytes) ? NativeAsync.MSetAsync(keyBytes, valBytes, token) : default;

        ValueTask IRedisClientAsync.SetAllAsync(IEnumerable<string> keys, IEnumerable<string> values, CancellationToken token)
            => GetSetAllBytes(keys, values, out var keyBytes, out var valBytes) ? NativeAsync.MSetAsync(keyBytes, valBytes, token) : default;

        Task ICacheClientAsync.SetAllAsync<T>(IDictionary<string, T> values, CancellationToken token)
        {
            if (values.Count != 0)
            {
                return ExecAsync(r =>
                {
                    // need to do this inside Exec for the JSON config bits
                    GetSetAllBytesTyped<T>(values, out var keys, out var valBytes);
                    return ((IRedisNativeClientAsync)r).MSetAsync(keys, valBytes, token);
                }).AsTask();
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        ValueTask IRedisClientAsync.RenameKeyAsync(string fromName, string toName, CancellationToken token)
            => NativeAsync.RenameAsync(fromName, toName, token);

        ValueTask<bool> IRedisClientAsync.ContainsKeyAsync(string key, CancellationToken token)
            => NativeAsync.ExistsAsync(key, token).IsSuccessAsync();


        ValueTask<string> IRedisClientAsync.GetRandomKeyAsync(CancellationToken token)
            => NativeAsync.RandomKeyAsync(token);

        ValueTask IRedisClientAsync.SelectAsync(long db, CancellationToken token)
            => NativeAsync.SelectAsync(db, token);

        ValueTask<bool> IRedisClientAsync.ExpireEntryInAsync(string key, TimeSpan expireIn, CancellationToken token)
            => UseMillisecondExpiration(expireIn)
            ? NativeAsync.PExpireAsync(key, (long)expireIn.TotalMilliseconds, token)
            : NativeAsync.ExpireAsync(key, (int)expireIn.TotalSeconds, token);

        ValueTask<bool> IRedisClientAsync.ExpireEntryAtAsync(string key, DateTime expireAt, CancellationToken token)
            => AssertServerVersionNumber() >= 2600
            ? NativeAsync.PExpireAtAsync(key, ConvertToServerDate(expireAt).ToUnixTimeMs(), token)
            : NativeAsync.ExpireAtAsync(key, ConvertToServerDate(expireAt).ToUnixTime(), token);

        Task<TimeSpan?> ICacheClientAsync.GetTimeToLiveAsync(string key, CancellationToken token)
            => NativeAsync.TtlAsync(key, token).Await(ParseTimeToLiveResult).AsTask();

        ValueTask<bool> IRedisClientAsync.PingAsync(CancellationToken token)
            => NativeAsync.PingAsync(token);

        ValueTask<string> IRedisClientAsync.EchoAsync(string text, CancellationToken token)
            => NativeAsync.EchoAsync(text, token);

        ValueTask IRedisClientAsync.ForegroundSaveAsync(CancellationToken token)
            => NativeAsync.SaveAsync(token);

        ValueTask IRedisClientAsync.BackgroundSaveAsync(CancellationToken token)
            => NativeAsync.BgSaveAsync(token);

        ValueTask IRedisClientAsync.ShutdownAsync(CancellationToken token)
            => NativeAsync.ShutdownAsync(false, token);

        ValueTask IRedisClientAsync.ShutdownNoSaveAsync(CancellationToken token)
            => NativeAsync.ShutdownAsync(true, token);

        ValueTask IRedisClientAsync.BackgroundRewriteAppendOnlyFileAsync(CancellationToken token)
            => NativeAsync.BgRewriteAofAsync(token);

        ValueTask IRedisClientAsync.FlushDbAsync(CancellationToken token)
            => NativeAsync.FlushDbAsync(token);

        ValueTask<List<string>> IRedisClientAsync.GetValuesAsync(List<string> keys, CancellationToken token)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            if (keys.Count == 0) return new List<string>().AsValueTaskResult();

            return NativeAsync.MGetAsync(keys.ToArray(), token).Await(ParseGetValuesResult);
        }

        ValueTask<List<T>> IRedisClientAsync.GetValuesAsync<T>(List<string> keys, CancellationToken token)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            if (keys.Count == 0) return new List<T>().AsValueTaskResult();

            return NativeAsync.MGetAsync(keys.ToArray(), token).Await(ParseGetValuesResult<T>);
        }

        ValueTask<Dictionary<string, string>> IRedisClientAsync.GetValuesMapAsync(List<string> keys, CancellationToken token)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            if (keys.Count == 0) return new Dictionary<string, string>().AsValueTaskResult();

            var keysArray = keys.ToArray();
            return NativeAsync.MGetAsync(keysArray, token).Await((resultBytesArray, state) => ParseGetValuesMapResult(state, resultBytesArray), keysArray);
        }

        ValueTask<Dictionary<string, T>> IRedisClientAsync.GetValuesMapAsync<T>(List<string> keys, CancellationToken token)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            if (keys.Count == 0) return new Dictionary<string, T>().AsValueTaskResult();

            var keysArray = keys.ToArray();
            return NativeAsync.MGetAsync(keysArray, token).Await((resultBytesArray, state) => ParseGetValuesMapResult<T>(state, resultBytesArray), keysArray);
        }

        ValueTask<IAsyncDisposable> IRedisClientAsync.AcquireLockAsync(string key, TimeSpan? timeOut, CancellationToken token)
            => RedisLock.CreateAsync(this, key, timeOut, token).Await<RedisLock, IAsyncDisposable>(value => value);

        ValueTask IRedisClientAsync.SetValueAsync(string key, string value, TimeSpan expireIn, CancellationToken token)
        {
            var bytesValue = value?.ToUtf8Bytes();

            if (AssertServerVersionNumber() >= 2610)
            {
                PickTime(expireIn, out var seconds, out var milliseconds);
                return NativeAsync.SetAsync(key, bytesValue, expirySeconds: seconds,
                    expiryMilliseconds: milliseconds, token: token);
            }
            else
            {
                return NativeAsync.SetExAsync(key, (int)expireIn.TotalSeconds, bytesValue, token);
            }
        }

        static void PickTime(TimeSpan? value, out long expirySeconds, out long expiryMilliseconds)
        {
            expirySeconds = expiryMilliseconds = 0;
            if (value.HasValue)
            {
                var expireIn = value.GetValueOrDefault();
                if (expireIn.Milliseconds > 0)
                {
                    expiryMilliseconds = (long)expireIn.TotalMilliseconds;
                }
                else
                {
                    expirySeconds = (long)expireIn.TotalSeconds;
                }
            }
        }
        ValueTask<bool> IRedisClientAsync.SetValueIfNotExistsAsync(string key, string value, TimeSpan? expireIn, CancellationToken token)
        {
            var bytesValue = value?.ToUtf8Bytes();
            PickTime(expireIn, out var seconds, out var milliseconds);
            return NativeAsync.SetAsync(key, bytesValue, false, seconds, milliseconds, token);
        }

        ValueTask<bool> IRedisClientAsync.SetValueIfExistsAsync(string key, string value, TimeSpan? expireIn, CancellationToken token)
        {
            var bytesValue = value?.ToUtf8Bytes();
            PickTime(expireIn, out var seconds, out var milliseconds);
            return NativeAsync.SetAsync(key, bytesValue, true, seconds, milliseconds, token);
        }

        ValueTask IRedisClientAsync.WatchAsync(string[] keys, CancellationToken token)
            => NativeAsync.WatchAsync(keys, token);

        ValueTask IRedisClientAsync.UnWatchAsync(CancellationToken token)
            => NativeAsync.UnWatchAsync(token);

        ValueTask<long> IRedisClientAsync.AppendToValueAsync(string key, string value, CancellationToken token)
            => NativeAsync.AppendAsync(key, value.ToUtf8Bytes(), token);

        async ValueTask<object> IRedisClientAsync.StoreObjectAsync(object entity, CancellationToken token)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var id = entity.GetObjectId();
            var entityType = entity.GetType();
            var urnKey = UrnKey(entityType, id);
            var valueString = JsonSerializer.SerializeToString(entity);

            await ((IRedisClientAsync)this).SetValueAsync(urnKey, valueString, token).ConfigureAwait(false);

            await RegisterTypeIdAsync(GetTypeIdsSetKey(entityType), id.ToString(), token).ConfigureAwait(false);

            return entity;
        }

        ValueTask<string> IRedisClientAsync.PopItemFromSetAsync(string setId, CancellationToken token)
            => NativeAsync.SPopAsync(setId, token).FromUtf8BytesAsync();

        ValueTask<List<string>> IRedisClientAsync.PopItemsFromSetAsync(string setId, int count, CancellationToken token)
            => NativeAsync.SPopAsync(setId, count, token).ToStringListAsync();

        ValueTask IRedisClientAsync.SlowlogResetAsync(CancellationToken token)
            => NativeAsync.SlowlogResetAsync(token);

        ValueTask<SlowlogItem[]> IRedisClientAsync.GetSlowlogAsync(int? numberOfRecords, CancellationToken token)
            => NativeAsync.SlowlogGetAsync(numberOfRecords, token).Await(ParseSlowlog);


        Task<bool> ICacheClientAsync.SetAsync<T>(string key, T value, CancellationToken token)
            => ExecAsync(r => ((IRedisNativeClientAsync)r).SetAsync(key, ToBytes(value), token: token)).AwaitAsTrueTask();

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            Dispose();
            return default;
        }

        ValueTask<long> IRedisClientAsync.GetSortedSetCountAsync(string setId, CancellationToken token)
            => NativeAsync.ZCardAsync(setId, token);

        ValueTask<long> IRedisClientAsync.GetSortedSetCountAsync(string setId, string fromStringScore, string toStringScore, CancellationToken token)
        {
            var fromScore = GetLexicalScore(fromStringScore);
            var toScore = GetLexicalScore(toStringScore);
            return AsAsync().GetSortedSetCountAsync(setId, fromScore, toScore, token);
        }

        ValueTask<long> IRedisClientAsync.GetSortedSetCountAsync(string setId, double fromScore, double toScore, CancellationToken token)
            => NativeAsync.ZCountAsync(setId, fromScore, toScore, token);

        ValueTask<long> IRedisClientAsync.GetSortedSetCountAsync(string setId, long fromScore, long toScore, CancellationToken token)
            => NativeAsync.ZCountAsync(setId, fromScore, toScore, token);

        ValueTask<double> IRedisClientAsync.GetItemScoreInSortedSetAsync(string setId, string value, CancellationToken token)
            => NativeAsync.ZScoreAsync(setId, value.ToUtf8Bytes(), token);

        ValueTask<RedisText> IRedisClientAsync.CustomAsync(object[] cmdWithArgs, CancellationToken token)
            => RawCommandAsync(token, cmdWithArgs).Await(result => result.ToRedisText());

        ValueTask IRedisClientAsync.SetValuesAsync(IDictionary<string, string> map, CancellationToken token)
            => ((IRedisClientAsync)this).SetAllAsync(map, token);

        Task<bool> ICacheClientAsync.SetAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token)
        {
            AssertNotInTransaction();
            return ExecAsync(async r =>
            {
                await r.SetAsync(key, value, token).ConfigureAwait(false);
                await r.ExpireEntryAtAsync(key, ConvertToServerDate(expiresAt), token).ConfigureAwait(false);
            }).AwaitAsTrueTask();
        }
        Task<bool> ICacheClientAsync.SetAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token)
        {
            if (AssertServerVersionNumber() >= 2600)
            {
                return ExecAsync(r => ((IRedisNativeClientAsync)r)
                    .SetAsync(key, ToBytes(value), 0, expiryMilliseconds: (long)expiresIn.TotalMilliseconds, token)).AwaitAsTrueTask();
            }
            else
            {
                return ExecAsync(r => ((IRedisNativeClientAsync)r)
                    .SetExAsync(key, (int)expiresIn.TotalSeconds, ToBytes(value), token)).AwaitAsTrueTask();
            }
        }

        Task ICacheClientAsync.FlushAllAsync(CancellationToken token)
            => NativeAsync.FlushAllAsync(token).AsTask();

        Task<IDictionary<string, T>> ICacheClientAsync.GetAllAsync<T>(IEnumerable<string> keys, CancellationToken token)
        {
            return ExecAsync(r =>
            {
                var keysArray = keys.ToArray();

                return ((IRedisNativeClientAsync)r).MGetAsync(keysArray, token).Await((keyValues, state) => ProcessGetAllResult<T>(state, keyValues), keysArray);
            }).AsTask();
        }

        Task<bool> ICacheClientAsync.RemoveAsync(string key, CancellationToken token)
            => NativeAsync.DelAsync(key, token).IsSuccessTaskAsync();

        IAsyncEnumerable<string> ICacheClientAsync.GetKeysByPatternAsync(string pattern, CancellationToken token)
            => AsAsync().ScanAllKeysAsync(pattern, token: token);

        Task ICacheClientAsync.RemoveExpiredEntriesAsync(CancellationToken token)
        {
            //Redis automatically removed expired Cache Entries
            return Task.CompletedTask;
        }

        async Task IRemoveByPatternAsync.RemoveByPatternAsync(string pattern, CancellationToken token)
        {
            List<string> buffer = null;
            const int BATCH_SIZE = 1024;
            await foreach (var key in AsAsync().ScanAllKeysAsync(pattern, token: token).WithCancellation(token).ConfigureAwait(false))
            {
                (buffer ??= new List<string>()).Add(key);
                if (buffer.Count == BATCH_SIZE)
                {
                    await NativeAsync.DelAsync(buffer.ToArray(), token).ConfigureAwait(false);
                    buffer.Clear();
                }
            }
            if (buffer is object && buffer.Count != 0)
            {
                await NativeAsync.DelAsync(buffer.ToArray(), token).ConfigureAwait(false);
            }
        }

        Task IRemoveByPatternAsync.RemoveByRegexAsync(string regex, CancellationToken token)
            => AsAsync().RemoveByPatternAsync(RegexToGlob(regex), token);

        Task ICacheClientAsync.RemoveAllAsync(IEnumerable<string> keys, CancellationToken token)
            => ExecAsync(r => r.RemoveEntryAsync(keys.ToArray(), token)).AsTask();

        Task<long> ICacheClientAsync.IncrementAsync(string key, uint amount, CancellationToken token)
            => ExecAsync(r => r.IncrementValueByAsync(key, (int)amount, token)).AsTask();

        Task<long> ICacheClientAsync.DecrementAsync(string key, uint amount, CancellationToken token)
            => ExecAsync(r => r.DecrementValueByAsync(key, (int)amount, token)).AsTask();


        Task<bool> ICacheClientAsync.AddAsync<T>(string key, T value, CancellationToken token)
            => ExecAsync(r => ((IRedisNativeClientAsync)r).SetAsync(key, ToBytes(value), exists: false, token: token)).AsTask();

        Task<bool> ICacheClientAsync.ReplaceAsync<T>(string key, T value, CancellationToken token)
            => ExecAsync(r => ((IRedisNativeClientAsync)r).SetAsync(key, ToBytes(value), exists: true, token: token)).AsTask();

        Task<bool> ICacheClientAsync.AddAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token)
        {
            AssertNotInTransaction();

            return ExecAsync(async r =>
            {
                if (await r.AddAsync(key, value, token).ConfigureAwait(false))
                {
                    await r.ExpireEntryAtAsync(key, ConvertToServerDate(expiresAt), token).ConfigureAwait(false);
                    return true;
                }
                return false;
            }).AsTask();
        }

        Task<bool> ICacheClientAsync.ReplaceAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token)
        {
            AssertNotInTransaction();

            return ExecAsync(async r =>
            {
                if (await r.ReplaceAsync(key, value, token).ConfigureAwait(false))
                {
                    await r.ExpireEntryAtAsync(key, ConvertToServerDate(expiresAt), token).ConfigureAwait(false);
                    return true;
                }
                return false;
            }).AsTask();
        }

        Task<bool> ICacheClientAsync.AddAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token)
            => ExecAsync(r => ((IRedisNativeClientAsync)r).SetAsync(key, ToBytes(value), exists: false, token: token)).AsTask();

        Task<bool> ICacheClientAsync.ReplaceAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token)
            => ExecAsync(r => ((IRedisNativeClientAsync)r).SetAsync(key, ToBytes(value), exists: true, token: token)).AsTask();

        ValueTask<long> IRedisClientAsync.DbSizeAsync(CancellationToken token)
            => NativeAsync.DbSizeAsync(token);

        ValueTask<Dictionary<string, string>> IRedisClientAsync.InfoAsync(CancellationToken token)
            => NativeAsync.InfoAsync(token);

        ValueTask<DateTime> IRedisClientAsync.LastSaveAsync(CancellationToken token)
            => NativeAsync.LastSaveAsync(token);

        async Task<T> IEntityStoreAsync.GetByIdAsync<T>(object id, CancellationToken token)
        {
            var key = UrnKey<T>(id);
            var valueString = await AsAsync().GetValueAsync(key, token).ConfigureAwait(false);
            var value = JsonSerializer.DeserializeFromString<T>(valueString);
            return value;
        }

        async Task<IList<T>> IEntityStoreAsync.GetByIdsAsync<T>(ICollection ids, CancellationToken token)
        {
            if (ids == null || ids.Count == 0)
                return new List<T>();

            var urnKeys = ids.Cast<object>().Map(UrnKey<T>);
            return await AsAsync().GetValuesAsync<T>(urnKeys, token).ConfigureAwait(false);
        }

        async Task<T> IEntityStoreAsync.StoreAsync<T>(T entity, CancellationToken token)
        {
            var urnKey = UrnKey(entity);
            var valueString = JsonSerializer.SerializeToString(entity);

            await AsAsync().SetValueAsync(urnKey, valueString, token).ConfigureAwait(false);
            await RegisterTypeIdAsync(entity, token).ConfigureAwait(false);

            return entity;
        }

        Task IEntityStoreAsync.StoreAllAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken token)
            => StoreAllAsyncImpl(entities, token).AsTask();

        internal async ValueTask StoreAllAsyncImpl<TEntity>(IEnumerable<TEntity> entities, CancellationToken token)
        {
            if (PrepareStoreAll(entities, out var keys, out var values, out var entitiesList))
            {
                await NativeAsync.MSetAsync(keys, values, token).ConfigureAwait(false);
                await RegisterTypeIdsAsync(entitiesList, token).ConfigureAwait(false);
            }
        }

        internal ValueTask RegisterTypeIdsAsync<T>(IEnumerable<T> values, CancellationToken token)
        {
            var typeIdsSetKey = GetTypeIdsSetKey<T>();
            var ids = values.Map(x => x.GetId().ToString());

            if (this.Pipeline != null)
            {
                var registeredTypeIdsWithinPipeline = GetRegisteredTypeIdsWithinPipeline(typeIdsSetKey);
                ids.ForEach(x => registeredTypeIdsWithinPipeline.Add(x));
                return default;
            }
            else
            {
                return AsAsync().AddRangeToSetAsync(typeIdsSetKey, ids, token);
            }
        }

        internal ValueTask RemoveTypeIdsByValueAsync<T>(T value, CancellationToken token) =>
            RemoveTypeIdsByIdAsync<T>(value.GetId().ToString(), token);
        internal async ValueTask RemoveTypeIdsByValuesAsync<T>(T[] values, CancellationToken token)
        {
            var typeIdsSetKey = GetTypeIdsSetKey<T>();
            if (this.Pipeline != null)
            {
                var registeredTypeIdsWithinPipeline = GetRegisteredTypeIdsWithinPipeline(typeIdsSetKey);
                values.Each(x => registeredTypeIdsWithinPipeline.Remove(x.GetId().ToString()));
            }
            else
            {
                foreach (var x in values)
                {
                    await AsAsync().RemoveItemFromSetAsync(typeIdsSetKey, x.GetId().ToString(), token).ConfigureAwait(false);
                }
            }
        }

        internal async ValueTask RemoveTypeIdsByIdAsync<T>(string id, CancellationToken token)
        {
            var typeIdsSetKey = GetTypeIdsSetKey<T>();
            if (this.Pipeline != null)
                GetRegisteredTypeIdsWithinPipeline(typeIdsSetKey).Remove(id);
            else
            {
                await AsAsync().RemoveItemFromSetAsync(typeIdsSetKey, id, token).ConfigureAwait(false);
            }
        }

        internal async ValueTask RemoveTypeIdsByIdsAsync<T>(string[] ids, CancellationToken token)
        {
            var typeIdsSetKey = GetTypeIdsSetKey<T>();
            if (this.Pipeline != null)
            {
                var registeredTypeIdsWithinPipeline = GetRegisteredTypeIdsWithinPipeline(typeIdsSetKey);
                ids.Each(x => registeredTypeIdsWithinPipeline.Remove(x));
            }
            else
            {
                foreach (var x in ids)
                {
                    await AsAsync().RemoveItemFromSetAsync(typeIdsSetKey, x, token).ConfigureAwait(false);
                }
            }
        }

        async Task IEntityStoreAsync.DeleteAsync<T>(T entity, CancellationToken token)
        {
            var urnKey = UrnKey(entity);
            await AsAsync().RemoveAsync(urnKey, token).ConfigureAwait(false);
            await this.RemoveTypeIdsByValueAsync(entity, token).ConfigureAwait(false);
        }

        async Task IEntityStoreAsync.DeleteByIdAsync<T>(object id, CancellationToken token)
        {
            var urnKey = UrnKey<T>(id);
            await AsAsync().RemoveAsync(urnKey, token).ConfigureAwait(false);
            await this.RemoveTypeIdsByIdAsync<T>(id.ToString(), token).ConfigureAwait(false);
        }

        async Task IEntityStoreAsync.DeleteByIdsAsync<T>(ICollection ids, CancellationToken token)
        {
            if (ids == null || ids.Count == 0) return;

            var idStrings = ids.Cast<object>().Select(x => x.ToString()).ToArray();
            var urnKeys = idStrings.Select(UrnKey<T>).ToArray();
            await AsAsync().RemoveEntryAsync(urnKeys, token).ConfigureAwait(false);
            await this.RemoveTypeIdsByIdsAsync<T>(idStrings, token).ConfigureAwait(false);
        }

        async Task IEntityStoreAsync.DeleteAllAsync<T>(CancellationToken token)
        {
            await DeleteAllAsync<T>(0, RedisConfig.CommandKeysBatchSize, token).ConfigureAwait(false);
        }
        
        private async Task DeleteAllAsync<T>(ulong cursor, int batchSize, CancellationToken token)
        {
            var typeIdsSetKey = this.GetTypeIdsSetKey<T>();
            var asyncClient = AsAsync();
            do
            {
                var scanResult = await NativeAsync.SScanAsync(typeIdsSetKey, cursor, batchSize, token: token).ConfigureAwait(false);
                cursor = scanResult.Cursor;
                var urnKeys = scanResult.Results.Select(id => UrnKey<T>(id.FromUtf8Bytes())).ToArray();
                if (urnKeys.Length > 0)
                {
                    await asyncClient.RemoveEntryAsync(urnKeys, token).ConfigureAwait(false);
                }
            } while (cursor != 0);
            await asyncClient.RemoveEntryAsync(new[] { typeIdsSetKey }, token).ConfigureAwait(false);
        }

        ValueTask<List<string>> IRedisClientAsync.SearchSortedSetAsync(string setId, string start, string end, int? skip, int? take, CancellationToken token)
        {
            start = GetSearchStart(start);
            end = GetSearchEnd(end);

            return NativeAsync.ZRangeByLexAsync(setId, start, end, skip, take, token).ToStringListAsync();
        }

        ValueTask<long> IRedisClientAsync.SearchSortedSetCountAsync(string setId, string start, string end, CancellationToken token)
            => NativeAsync.ZLexCountAsync(setId, GetSearchStart(start), GetSearchEnd(end), token);

        ValueTask<long> IRedisClientAsync.RemoveRangeFromSortedSetBySearchAsync(string setId, string start, string end, CancellationToken token)
            => NativeAsync.ZRemRangeByLexAsync(setId, GetSearchStart(start), GetSearchEnd(end), token);

        ValueTask<string> IRedisClientAsync.TypeAsync(string key, CancellationToken token)
            => NativeAsync.TypeAsync(key, token);

        ValueTask<long> IRedisClientAsync.GetStringCountAsync(string key, CancellationToken token)
            => NativeAsync.StrLenAsync(key, token);

        ValueTask<long> IRedisClientAsync.GetSetCountAsync(string setId, CancellationToken token)
            => NativeAsync.SCardAsync(setId, token);

        ValueTask<long> IRedisClientAsync.GetListCountAsync(string listId, CancellationToken token)
            => NativeAsync.LLenAsync(listId, token);

        ValueTask<long> IRedisClientAsync.GetHashCountAsync(string hashId, CancellationToken token)
            => NativeAsync.HLenAsync(hashId, token);

        async ValueTask<T> IRedisClientAsync.ExecCachedLuaAsync<T>(string scriptBody, Func<string, ValueTask<T>> scriptSha1, CancellationToken token)
        {
            if (!CachedLuaSha1Map.TryGetValue(scriptBody, out var sha1))
                CachedLuaSha1Map[scriptBody] = sha1 = await AsAsync().LoadLuaScriptAsync(scriptBody, token).ConfigureAwait(false);

            try
            {
                return await scriptSha1(sha1).ConfigureAwait(false);
            }
            catch (RedisResponseException ex)
            {
                if (!ex.Message.StartsWith("NOSCRIPT"))
                    throw;

                CachedLuaSha1Map[scriptBody] = sha1 = await AsAsync().LoadLuaScriptAsync(scriptBody, token).ConfigureAwait(false);
                return await scriptSha1(sha1).ConfigureAwait(false);
            }
        }

        ValueTask<RedisText> IRedisClientAsync.ExecLuaAsync(string luaBody, string[] keys, string[] args, CancellationToken token)
            => NativeAsync.EvalCommandAsync(luaBody, keys?.Length ?? 0, MergeAndConvertToBytes(keys, args), token).Await(data => data.ToRedisText());

        ValueTask<RedisText> IRedisClientAsync.ExecLuaShaAsync(string sha1, string[] keys, string[] args, CancellationToken token)
            => NativeAsync.EvalShaCommandAsync(sha1, keys?.Length ?? 0, MergeAndConvertToBytes(keys, args), token).Await(data => data.ToRedisText());

        ValueTask<string> IRedisClientAsync.ExecLuaAsStringAsync(string luaBody, string[] keys, string[] args, CancellationToken token)
            => NativeAsync.EvalStrAsync(luaBody, keys?.Length ?? 0, MergeAndConvertToBytes(keys, args), token);

        ValueTask<string> IRedisClientAsync.ExecLuaShaAsStringAsync(string sha1, string[] keys, string[] args, CancellationToken token)
            => NativeAsync.EvalShaStrAsync(sha1, keys?.Length ?? 0, MergeAndConvertToBytes(keys, args), token);

        ValueTask<string> IRedisClientAsync.LoadLuaScriptAsync(string body, CancellationToken token)
            => NativeAsync.ScriptLoadAsync(body, token).FromUtf8BytesAsync();

        ValueTask IRedisClientAsync.WriteAllAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken token)
            => PrepareWriteAll(entities, out var keys, out var values) ? NativeAsync.MSetAsync(keys, values, token) : default;

        async ValueTask<HashSet<string>> IRedisClientAsync.GetAllItemsFromSetAsync(string setId, CancellationToken token)
        {
            var multiDataList = await NativeAsync.SMembersAsync(setId, token).ConfigureAwait(false);
            return CreateHashSet(multiDataList);
        }

        async ValueTask IRedisClientAsync.AddRangeToSetAsync(string setId, List<string> items, CancellationToken token)
        {
            if (await AddRangeToSetNeedsSendAsync(setId, items).ConfigureAwait(false))
            {
                var uSetId = setId.ToUtf8Bytes();
                var pipeline = CreatePipelineCommand();
                foreach (var item in items)
                {
                    pipeline.WriteCommand(Commands.SAdd, uSetId, item.ToUtf8Bytes());
                }
                await pipeline.FlushAsync(token).ConfigureAwait(false);

                //the number of items after
                _ = await pipeline.ReadAllAsIntsAsync(token).ConfigureAwait(false);
            }
        }

        async ValueTask<bool> AddRangeToSetNeedsSendAsync(string setId, List<string> items)
        {
            if (setId.IsNullOrEmpty())
                throw new ArgumentNullException("setId");
            if (items == null)
                throw new ArgumentNullException("items");
            if (items.Count == 0)
                return false;

            if (this.Transaction is object || this.PipelineAsync is object)
            {
                var queueable = this.Transaction as IRedisQueueableOperationAsync
                    ?? this.Pipeline as IRedisQueueableOperationAsync;

                if (queueable == null)
                    throw new NotSupportedException("Cannot AddRangeToSetAsync() when Transaction is: " + this.Transaction.GetType().Name);

                //Complete the first QueuedCommand()
                await AsAsync().AddItemToSetAsync(setId, items[0]).ConfigureAwait(false);

                //Add subsequent queued commands
                for (var i = 1; i < items.Count; i++)
                {
                    var item = items[i];
                    queueable.QueueCommand(c => c.AddItemToSetAsync(setId, item));
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        ValueTask IRedisClientAsync.RemoveItemFromSetAsync(string setId, string item, CancellationToken token)
            => NativeAsync.SRemAsync(setId, item.ToUtf8Bytes(), token).Await();

        ValueTask<long> IRedisClientAsync.IncrementValueByAsync(string key, int count, CancellationToken token)
            => NativeAsync.IncrByAsync(key, count, token);

        ValueTask<long> IRedisClientAsync.IncrementValueByAsync(string key, long count, CancellationToken token)
            => NativeAsync.IncrByAsync(key, count, token);

        ValueTask<double> IRedisClientAsync.IncrementValueByAsync(string key, double count, CancellationToken token)
            => NativeAsync.IncrByFloatAsync(key, count, token);
        ValueTask<long> IRedisClientAsync.IncrementValueAsync(string key, CancellationToken token)
            => NativeAsync.IncrAsync(key, token);

        ValueTask<long> IRedisClientAsync.DecrementValueAsync(string key, CancellationToken token)
            => NativeAsync.DecrAsync(key, token);

        ValueTask<long> IRedisClientAsync.DecrementValueByAsync(string key, int count, CancellationToken token)
            => NativeAsync.DecrByAsync(key, count, token);

        async ValueTask<RedisServerRole> IRedisClientAsync.GetServerRoleAsync(CancellationToken token)
        {
            if (AssertServerVersionNumber() >= 2812)
            {
                var text = await NativeAsync.RoleAsync(token).ConfigureAwait(false);
                var roleName = text.Children[0].Text;
                return ToServerRole(roleName);
            }

            var info = await AsAsync().InfoAsync(token).ConfigureAwait(false);
            info.TryGetValue("role", out var role);
            return ToServerRole(role);
        }

        ValueTask<RedisText> IRedisClientAsync.GetServerRoleInfoAsync(CancellationToken token)
            => NativeAsync.RoleAsync(token);

        async ValueTask<string> IRedisClientAsync.GetConfigAsync(string configItem, CancellationToken token)
        {
            var byteArray = await NativeAsync.ConfigGetAsync(configItem, token).ConfigureAwait(false);
            return GetConfigParse(byteArray);
        }

        ValueTask IRedisClientAsync.SetConfigAsync(string configItem, string value, CancellationToken token)
            => NativeAsync.ConfigSetAsync(configItem, value.ToUtf8Bytes(), token);

        ValueTask IRedisClientAsync.SaveConfigAsync(CancellationToken token)
            => NativeAsync.ConfigRewriteAsync(token);

        ValueTask IRedisClientAsync.ResetInfoStatsAsync(CancellationToken token)
            => NativeAsync.ConfigResetStatAsync(token);

        ValueTask<string> IRedisClientAsync.GetClientAsync(CancellationToken token)
            => NativeAsync.ClientGetNameAsync(token);

        ValueTask IRedisClientAsync.SetClientAsync(string name, CancellationToken token)
            => NativeAsync.ClientSetNameAsync(name, token);

        ValueTask IRedisClientAsync.KillClientAsync(string address, CancellationToken token)
            => NativeAsync.ClientKillAsync(address, token);

        ValueTask<long> IRedisClientAsync.KillClientsAsync(string fromAddress, string withId, RedisClientType? ofType, bool? skipMe, CancellationToken token)
        {
            var typeString = ofType?.ToString().ToLower();
            var skipMeString = skipMe.HasValue ? (skipMe.Value ? "yes" : "no") : null;
            return NativeAsync.ClientKillAsync(addr: fromAddress, id: withId, type: typeString, skipMe: skipMeString, token);
        }

        async ValueTask<List<Dictionary<string, string>>> IRedisClientAsync.GetClientsInfoAsync(CancellationToken token)
            => GetClientsInfoParse(await NativeAsync.ClientListAsync(token).ConfigureAwait(false));

        ValueTask IRedisClientAsync.PauseAllClientsAsync(TimeSpan duration, CancellationToken token)
            => NativeAsync.ClientPauseAsync((int)duration.TotalMilliseconds, token);

        ValueTask<List<string>> IRedisClientAsync.GetAllKeysAsync(CancellationToken token)
            => AsAsync().SearchKeysAsync("*", token);

        ValueTask<string> IRedisClientAsync.GetAndSetValueAsync(string key, string value, CancellationToken token)
            => NativeAsync.GetSetAsync(key, value.ToUtf8Bytes(), token).FromUtf8BytesAsync();

        async ValueTask<T> IRedisClientAsync.GetFromHashAsync<T>(object id, CancellationToken token)
        {
            var key = UrnKey<T>(id);
            return (await AsAsync().GetAllEntriesFromHashAsync(key, token).ConfigureAwait(false)).ToJson().FromJson<T>();
        }

        async ValueTask IRedisClientAsync.StoreAsHashAsync<T>(T entity, CancellationToken token)
        {
            var key = UrnKey(entity);
            var hash = ConvertToHashFn(entity);
            await AsAsync().SetRangeInHashAsync(key, hash, token).ConfigureAwait(false);
            await RegisterTypeIdAsync(entity, token).ConfigureAwait(false);
        }

        ValueTask<List<string>> IRedisClientAsync.GetSortedEntryValuesAsync(string setId, int startingFrom, int endingAt, CancellationToken token)
        {
            var sortOptions = new SortOptions { Skip = startingFrom, Take = endingAt, };
            return NativeAsync.SortAsync(setId, sortOptions, token).ToStringListAsync();
        }

        async IAsyncEnumerable<string> IRedisClientAsync.ScanAllSetItemsAsync(string setId, string pattern, int pageSize, [EnumeratorCancellation] CancellationToken token)
        {
            var ret = new ScanResult();
            while (true)
            {
                ret = await (pattern != null // note ConfigureAwait is handled below
                    ? NativeAsync.SScanAsync(setId, ret.Cursor, pageSize, match: pattern, token: token)
                    : NativeAsync.SScanAsync(setId, ret.Cursor, pageSize, token: token)
                    ).ConfigureAwait(false);

                foreach (var key in ret.Results)
                {
                    yield return key.FromUtf8Bytes();
                }

                if (ret.Cursor == 0) break;
            }
        }

        async IAsyncEnumerable<KeyValuePair<string, double>> IRedisClientAsync.ScanAllSortedSetItemsAsync(string setId, string pattern, int pageSize, [EnumeratorCancellation] CancellationToken token)
        {
            var ret = new ScanResult();
            while (true)
            {
                ret = await (pattern != null // note ConfigureAwait is handled below
                    ? NativeAsync.ZScanAsync(setId, ret.Cursor, pageSize, match: pattern, token: token)
                    : NativeAsync.ZScanAsync(setId, ret.Cursor, pageSize, token: token)
                    ).ConfigureAwait(false);

                foreach (var entry in ret.AsItemsWithScores())
                {
                    yield return entry;
                }

                if (ret.Cursor == 0) break;
            }
        }

        async IAsyncEnumerable<KeyValuePair<string, string>> IRedisClientAsync.ScanAllHashEntriesAsync(string hashId, string pattern, int pageSize, [EnumeratorCancellation] CancellationToken token)
        {
            var ret = new ScanResult();
            while (true)
            {
                ret = await (pattern != null // note ConfigureAwait is handled below
                    ? NativeAsync.HScanAsync(hashId, ret.Cursor, pageSize, match: pattern, token: token)
                    : NativeAsync.HScanAsync(hashId, ret.Cursor, pageSize, token: token)
                    ).ConfigureAwait(false);

                foreach (var entry in ret.AsKeyValues())
                {
                    yield return entry;
                }

                if (ret.Cursor == 0) break;
            }
        }

        ValueTask<bool> IRedisClientAsync.AddToHyperLogAsync(string key, string[] elements, CancellationToken token)
            => NativeAsync.PfAddAsync(key, elements.Map(x => x.ToUtf8Bytes()).ToArray(), token);

        ValueTask<long> IRedisClientAsync.CountHyperLogAsync(string key, CancellationToken token)
            => NativeAsync.PfCountAsync(key, token);

        ValueTask IRedisClientAsync.MergeHyperLogsAsync(string toKey, string[] fromKeys, CancellationToken token)
            => NativeAsync.PfMergeAsync(toKey, fromKeys, token);

        ValueTask<long> IRedisClientAsync.AddGeoMemberAsync(string key, double longitude, double latitude, string member, CancellationToken token)
            => NativeAsync.GeoAddAsync(key, longitude, latitude, member, token);

        ValueTask<long> IRedisClientAsync.AddGeoMembersAsync(string key, RedisGeo[] geoPoints, CancellationToken token)
            => NativeAsync.GeoAddAsync(key, geoPoints, token);

        ValueTask<double> IRedisClientAsync.CalculateDistanceBetweenGeoMembersAsync(string key, string fromMember, string toMember, string unit, CancellationToken token)
            => NativeAsync.GeoDistAsync(key, fromMember, toMember, unit, token);

        ValueTask<string[]> IRedisClientAsync.GetGeohashesAsync(string key, string[] members, CancellationToken token)
            => NativeAsync.GeoHashAsync(key, members, token);

        ValueTask<List<RedisGeo>> IRedisClientAsync.GetGeoCoordinatesAsync(string key, string[] members, CancellationToken token)
            => NativeAsync.GeoPosAsync(key, members, token);

        async ValueTask<string[]> IRedisClientAsync.FindGeoMembersInRadiusAsync(string key, double longitude, double latitude, double radius, string unit, CancellationToken token)
        {
            var results = await NativeAsync.GeoRadiusAsync(key, longitude, latitude, radius, unit, token: token).ConfigureAwait(false);
            return ParseFindGeoMembersResult(results);
        }

        ValueTask<List<RedisGeoResult>> IRedisClientAsync.FindGeoResultsInRadiusAsync(string key, double longitude, double latitude, double radius, string unit, int? count, bool? sortByNearest, CancellationToken token)
            => NativeAsync.GeoRadiusAsync(key, longitude, latitude, radius, unit, withCoords: true, withDist: true, withHash: true, count: count, asc: sortByNearest, token: token);

        async ValueTask<string[]> IRedisClientAsync.FindGeoMembersInRadiusAsync(string key, string member, double radius, string unit, CancellationToken token)
        {
            var results = await NativeAsync.GeoRadiusByMemberAsync(key, member, radius, unit, token: token).ConfigureAwait(false);
            return ParseFindGeoMembersResult(results);
        }

        ValueTask<List<RedisGeoResult>> IRedisClientAsync.FindGeoResultsInRadiusAsync(string key, string member, double radius, string unit, int? count, bool? sortByNearest, CancellationToken token)
            => NativeAsync.GeoRadiusByMemberAsync(key, member, radius, unit, withCoords: true, withDist: true, withHash: true, count: count, asc: sortByNearest, token: token);

        ValueTask<IRedisSubscriptionAsync> IRedisClientAsync.CreateSubscriptionAsync(CancellationToken token)
            => new RedisSubscription(this).AsValueTaskResult<IRedisSubscriptionAsync>();

        ValueTask<long> IRedisClientAsync.PublishMessageAsync(string toChannel, string message, CancellationToken token)
            => NativeAsync.PublishAsync(toChannel, message.ToUtf8Bytes(), token);

        ValueTask IRedisClientAsync.MoveBetweenSetsAsync(string fromSetId, string toSetId, string item, CancellationToken token)
            => NativeAsync.SMoveAsync(fromSetId, toSetId, item.ToUtf8Bytes(), token);

        ValueTask<bool> IRedisClientAsync.SetContainsItemAsync(string setId, string item, CancellationToken token)
            => NativeAsync.SIsMemberAsync(setId, item.ToUtf8Bytes(), token).IsSuccessAsync();

        async ValueTask<HashSet<string>> IRedisClientAsync.GetIntersectFromSetsAsync(string[] setIds, CancellationToken token)
        {
            if (setIds.Length == 0)
                return new HashSet<string>();

            var multiDataList = await NativeAsync.SInterAsync(setIds, token).ConfigureAwait(false);
            return CreateHashSet(multiDataList);
        }

        ValueTask IRedisClientAsync.StoreIntersectFromSetsAsync(string intoSetId, string[] setIds, CancellationToken token)
        {
            if (setIds.Length == 0) return default;

            return NativeAsync.SInterStoreAsync(intoSetId, setIds, token);
        }

        async ValueTask<HashSet<string>> IRedisClientAsync.GetUnionFromSetsAsync(string[] setIds, CancellationToken token)
        {
            if (setIds.Length == 0)
                return new HashSet<string>();

            var multiDataList = await NativeAsync.SUnionAsync(setIds, token).ConfigureAwait(false);
            return CreateHashSet(multiDataList);
        }

        ValueTask IRedisClientAsync.StoreUnionFromSetsAsync(string intoSetId, string[] setIds, CancellationToken token)
        {
            if (setIds.Length == 0) return default;

            return NativeAsync.SUnionStoreAsync(intoSetId, setIds, token);
        }

        async ValueTask<HashSet<string>> IRedisClientAsync.GetDifferencesFromSetAsync(string fromSetId, string[] withSetIds, CancellationToken token)
        {
            if (withSetIds.Length == 0)
                return new HashSet<string>();

            var multiDataList = await NativeAsync.SDiffAsync(fromSetId, withSetIds, token).ConfigureAwait(false);
            return CreateHashSet(multiDataList);
        }

        ValueTask IRedisClientAsync.StoreDifferencesFromSetAsync(string intoSetId, string fromSetId, string[] withSetIds, CancellationToken token)
        {
            if (withSetIds.Length == 0) return default;

            return NativeAsync.SDiffStoreAsync(intoSetId, fromSetId, withSetIds, token);
        }

        ValueTask<string> IRedisClientAsync.GetRandomItemFromSetAsync(string setId, CancellationToken token)
            => NativeAsync.SRandMemberAsync(setId, token).FromUtf8BytesAsync();

        ValueTask<List<string>> IRedisClientAsync.GetAllItemsFromListAsync(string listId, CancellationToken token)
            => NativeAsync.LRangeAsync(listId, FirstElement, LastElement, token).ToStringListAsync();

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromListAsync(string listId, int startingFrom, int endingAt, CancellationToken token)
            => NativeAsync.LRangeAsync(listId, startingFrom, endingAt, token).ToStringListAsync();

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedListAsync(string listId, int startingFrom, int endingAt, CancellationToken token)
        {
            var sortOptions = new SortOptions { Skip = startingFrom, Take = endingAt, SortAlpha = true };
            return AsAsync().GetSortedItemsFromListAsync(listId, sortOptions, token);
        }

        ValueTask<List<string>> IRedisClientAsync.GetSortedItemsFromListAsync(string listId, SortOptions sortOptions, CancellationToken token)
            => NativeAsync.SortAsync(listId, sortOptions, token).ToStringListAsync();

        async ValueTask IRedisClientAsync.AddRangeToListAsync(string listId, List<string> values, CancellationToken token)
        {
            var pipeline = AddRangeToListPrepareNonFlushed(listId, values);
            await pipeline.FlushAsync(token).ConfigureAwait(false);

            //the number of items after
            _ = await pipeline.ReadAllAsIntsAsync(token).ConfigureAwait(false);
        }

        ValueTask IRedisClientAsync.PrependItemToListAsync(string listId, string value, CancellationToken token)
            => NativeAsync.LPushAsync(listId, value.ToUtf8Bytes(), token).Await();

        async ValueTask IRedisClientAsync.PrependRangeToListAsync(string listId, List<string> values, CancellationToken token)
        {
            var pipeline = PrependRangeToListPrepareNonFlushed(listId, values);
            await pipeline.FlushAsync(token).ConfigureAwait(false);

            //the number of items after
            _ = await pipeline.ReadAllAsIntsAsync(token).ConfigureAwait(false);
        }

        ValueTask IRedisClientAsync.RemoveAllFromListAsync(string listId, CancellationToken token)
            => NativeAsync.LTrimAsync(listId, LastElement, FirstElement, token);

        ValueTask<string> IRedisClientAsync.RemoveStartFromListAsync(string listId, CancellationToken token)
            => NativeAsync.LPopAsync(listId, token).FromUtf8BytesAsync();

        ValueTask<string> IRedisClientAsync.BlockingRemoveStartFromListAsync(string listId, TimeSpan? timeOut, CancellationToken token)
            => NativeAsync.BLPopValueAsync(listId, (int)timeOut.GetValueOrDefault().TotalSeconds, token).FromUtf8BytesAsync();

        async ValueTask<ItemRef> IRedisClientAsync.BlockingRemoveStartFromListsAsync(string[] listIds, TimeSpan? timeOut, CancellationToken token)
        {
            var value = await NativeAsync.BLPopValueAsync(listIds, (int)timeOut.GetValueOrDefault().TotalSeconds, token).ConfigureAwait(false);
            if (value == null)
                return null;
            return new ItemRef { Id = value[0].FromUtf8Bytes(), Item = value[1].FromUtf8Bytes() };
        }

        ValueTask<string> IRedisClientAsync.RemoveEndFromListAsync(string listId, CancellationToken token)
            => NativeAsync.RPopAsync(listId, token).FromUtf8BytesAsync();

        ValueTask IRedisClientAsync.TrimListAsync(string listId, int keepStartingFrom, int keepEndingAt, CancellationToken token)
            => NativeAsync.LTrimAsync(listId, keepStartingFrom, keepEndingAt, token);

        ValueTask<long> IRedisClientAsync.RemoveItemFromListAsync(string listId, string value, CancellationToken token)
            => NativeAsync.LRemAsync(listId, 0, value.ToUtf8Bytes(), token);

        ValueTask<long> IRedisClientAsync.RemoveItemFromListAsync(string listId, string value, int noOfMatches, CancellationToken token)
            => NativeAsync.LRemAsync(listId, 0, value.ToUtf8Bytes(), token);

        ValueTask<string> IRedisClientAsync.GetItemFromListAsync(string listId, int listIndex, CancellationToken token)
            => NativeAsync.LIndexAsync(listId, listIndex, token).FromUtf8BytesAsync();

        ValueTask IRedisClientAsync.SetItemInListAsync(string listId, int listIndex, string value, CancellationToken token)
            => NativeAsync.LSetAsync(listId, listIndex, value.ToUtf8Bytes(), token);

        ValueTask IRedisClientAsync.EnqueueItemOnListAsync(string listId, string value, CancellationToken token)
            => NativeAsync.LPushAsync(listId, value.ToUtf8Bytes(), token).Await();

        ValueTask<string> IRedisClientAsync.DequeueItemFromListAsync(string listId, CancellationToken token)
            => NativeAsync.RPopAsync(listId, token).FromUtf8BytesAsync();

        ValueTask<string> IRedisClientAsync.BlockingDequeueItemFromListAsync(string listId, TimeSpan? timeOut, CancellationToken token)
            => NativeAsync.BRPopValueAsync(listId, (int)timeOut.GetValueOrDefault().TotalSeconds, token).FromUtf8BytesAsync();

        async ValueTask<ItemRef> IRedisClientAsync.BlockingDequeueItemFromListsAsync(string[] listIds, TimeSpan? timeOut, CancellationToken token)
        {
            var value = await NativeAsync.BRPopValueAsync(listIds, (int)timeOut.GetValueOrDefault().TotalSeconds, token).ConfigureAwait(false);
            if (value == null)
                return null;
            return new ItemRef { Id = value[0].FromUtf8Bytes(), Item = value[1].FromUtf8Bytes() };
        }

        ValueTask IRedisClientAsync.PushItemToListAsync(string listId, string value, CancellationToken token)
            => NativeAsync.RPushAsync(listId, value.ToUtf8Bytes(), token).Await();

        ValueTask<string> IRedisClientAsync.PopItemFromListAsync(string listId, CancellationToken token)
            => NativeAsync.RPopAsync(listId, token).FromUtf8BytesAsync();

        ValueTask<string> IRedisClientAsync.BlockingPopItemFromListAsync(string listId, TimeSpan? timeOut, CancellationToken token)
            => NativeAsync.BRPopValueAsync(listId, (int)timeOut.GetValueOrDefault().TotalSeconds, token).FromUtf8BytesAsync();

        async ValueTask<ItemRef> IRedisClientAsync.BlockingPopItemFromListsAsync(string[] listIds, TimeSpan? timeOut, CancellationToken token)
        {
            var value = await NativeAsync.BRPopValueAsync(listIds, (int)timeOut.GetValueOrDefault().TotalSeconds, token).ConfigureAwait(false);
            if (value == null)
                return null;
            return new ItemRef { Id = value[0].FromUtf8Bytes(), Item = value[1].FromUtf8Bytes() };
        }

        ValueTask<string> IRedisClientAsync.PopAndPushItemBetweenListsAsync(string fromListId, string toListId, CancellationToken token)
            => NativeAsync.RPopLPushAsync(fromListId, toListId, token).FromUtf8BytesAsync();

        ValueTask<string> IRedisClientAsync.BlockingPopAndPushItemBetweenListsAsync(string fromListId, string toListId, TimeSpan? timeOut, CancellationToken token)
            => NativeAsync.BRPopLPushAsync(fromListId, toListId, (int)timeOut.GetValueOrDefault().TotalSeconds, token).FromUtf8BytesAsync();

        async ValueTask<bool> IRedisClientAsync.AddRangeToSortedSetAsync(string setId, List<string> values, double score, CancellationToken token)
        {
            var pipeline = AddRangeToSortedSetPrepareNonFlushed(setId, values, score.ToFastUtf8Bytes());
            await pipeline.FlushAsync(token).ConfigureAwait(false);

            return await pipeline.ReadAllAsIntsHaveSuccessAsync(token).ConfigureAwait(false);
        }

        async ValueTask<bool> IRedisClientAsync.AddRangeToSortedSetAsync(string setId, List<string> values, long score, CancellationToken token)
        {
            var pipeline = AddRangeToSortedSetPrepareNonFlushed(setId, values, score.ToUtf8Bytes());
            await pipeline.FlushAsync(token).ConfigureAwait(false);

            return await pipeline.ReadAllAsIntsHaveSuccessAsync(token).ConfigureAwait(false);
        }

        ValueTask<bool> IRedisClientAsync.RemoveItemFromSortedSetAsync(string setId, string value, CancellationToken token)
            => NativeAsync.ZRemAsync(setId, value.ToUtf8Bytes(), token).IsSuccessAsync();

        ValueTask<long> IRedisClientAsync.RemoveItemsFromSortedSetAsync(string setId, List<string> values, CancellationToken token)
            => NativeAsync.ZRemAsync(setId, values.Map(x => x.ToUtf8Bytes()).ToArray(), token);

        async ValueTask<string> IRedisClientAsync.PopItemWithLowestScoreFromSortedSetAsync(string setId, CancellationToken token)
        {
            //TODO: this should be atomic
            var topScoreItemBytes = await NativeAsync.ZRangeAsync(setId, FirstElement, 1, token).ConfigureAwait(false);
            if (topScoreItemBytes.Length == 0) return null;

            await NativeAsync.ZRemAsync(setId, topScoreItemBytes[0], token).ConfigureAwait(false);
            return topScoreItemBytes[0].FromUtf8Bytes();
        }

       async ValueTask<string> IRedisClientAsync.PopItemWithHighestScoreFromSortedSetAsync(string setId, CancellationToken token)
        {
            //TODO: this should be atomic
            var topScoreItemBytes = await NativeAsync.ZRevRangeAsync(setId, FirstElement, 1, token).ConfigureAwait(false);
            if (topScoreItemBytes.Length == 0) return null;

            await NativeAsync.ZRemAsync(setId, topScoreItemBytes[0], token).ConfigureAwait(false);
            return topScoreItemBytes[0].FromUtf8Bytes();
        }

        ValueTask<bool> IRedisClientAsync.SortedSetContainsItemAsync(string setId, string value, CancellationToken token)
            => NativeAsync.ZRankAsync(setId, value.ToUtf8Bytes(), token).Await(val => val != -1);

        ValueTask<double> IRedisClientAsync.IncrementItemInSortedSetAsync(string setId, string value, double incrementBy, CancellationToken token)
            => NativeAsync.ZIncrByAsync(setId, incrementBy, value.ToUtf8Bytes(), token);

        ValueTask<double> IRedisClientAsync.IncrementItemInSortedSetAsync(string setId, string value, long incrementBy, CancellationToken token)
            => NativeAsync.ZIncrByAsync(setId, incrementBy, value.ToUtf8Bytes(), token);

        ValueTask<long> IRedisClientAsync.GetItemIndexInSortedSetAsync(string setId, string value, CancellationToken token)
            => NativeAsync.ZRankAsync(setId, value.ToUtf8Bytes(), token);

        ValueTask<long> IRedisClientAsync.GetItemIndexInSortedSetDescAsync(string setId, string value, CancellationToken token)
            => NativeAsync.ZRevRankAsync(setId, value.ToUtf8Bytes(), token);

        ValueTask<List<string>> IRedisClientAsync.GetAllItemsFromSortedSetAsync(string setId, CancellationToken token)
            => NativeAsync.ZRangeAsync(setId, FirstElement, LastElement, token).ToStringListAsync();

        ValueTask<List<string>> IRedisClientAsync.GetAllItemsFromSortedSetDescAsync(string setId, CancellationToken token)
            => NativeAsync.ZRevRangeAsync(setId, FirstElement, LastElement, token).ToStringListAsync();

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetAsync(string setId, int fromRank, int toRank, CancellationToken token)
            => NativeAsync.ZRangeAsync(setId, fromRank, toRank, token).ToStringListAsync();

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetDescAsync(string setId, int fromRank, int toRank, CancellationToken token)
            => NativeAsync.ZRevRangeAsync(setId, fromRank, toRank, token).ToStringListAsync();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ValueTask<IDictionary<string, double>> CreateSortedScoreMapAsync(ValueTask<byte[][]> pending)
        {
            return pending.IsCompletedSuccessfully ? CreateSortedScoreMap(pending.Result).AsValueTaskResult() : Awaited(pending);
            static async ValueTask<IDictionary<string, double>> Awaited(ValueTask<byte[][]> pending)
                => CreateSortedScoreMap(await pending.ConfigureAwait(false));
        }

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetAllWithScoresFromSortedSetAsync(string setId, CancellationToken token)
            => CreateSortedScoreMapAsync(NativeAsync.ZRangeWithScoresAsync(setId, FirstElement, LastElement, token));

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetAsync(string setId, int fromRank, int toRank, CancellationToken token)
            => CreateSortedScoreMapAsync(NativeAsync.ZRangeWithScoresAsync(setId, fromRank, toRank, token));

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetDescAsync(string setId, int fromRank, int toRank, CancellationToken token)
            => CreateSortedScoreMapAsync(NativeAsync.ZRevRangeWithScoresAsync(setId, fromRank, toRank, token));

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetByLowestScoreAsync(string setId, string fromStringScore, string toStringScore, CancellationToken token)
            => AsAsync().GetRangeFromSortedSetByLowestScoreAsync(setId, fromStringScore, toStringScore, null, null, token);

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetByLowestScoreAsync(string setId, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token)
        {
            var fromScore = GetLexicalScore(fromStringScore);
            var toScore = GetLexicalScore(toStringScore);
            return AsAsync().GetRangeFromSortedSetByLowestScoreAsync(setId, fromScore, toScore, skip, take, token);
        }

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetByLowestScoreAsync(string setId, double fromScore, double toScore, CancellationToken token)
            => AsAsync().GetRangeFromSortedSetByLowestScoreAsync(setId, fromScore, toScore, null, null, token);

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetByLowestScoreAsync(string setId, long fromScore, long toScore, CancellationToken token)
            => AsAsync().GetRangeFromSortedSetByLowestScoreAsync(setId, fromScore, toScore, null, null, token);

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetByLowestScoreAsync(string setId, double fromScore, double toScore, int? skip, int? take, CancellationToken token)
            => NativeAsync.ZRangeByScoreAsync(setId, fromScore, toScore, skip, take, token).ToStringListAsync();

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetByLowestScoreAsync(string setId, long fromScore, long toScore, int? skip, int? take, CancellationToken token)
            => NativeAsync.ZRangeByScoreAsync(setId, fromScore, toScore, skip, take, token).ToStringListAsync();

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetByLowestScoreAsync(string setId, string fromStringScore, string toStringScore, CancellationToken token)
            => AsAsync().GetRangeWithScoresFromSortedSetByLowestScoreAsync(setId, fromStringScore, toStringScore, null, null, token);

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetByLowestScoreAsync(string setId, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token)
        {
            var fromScore = GetLexicalScore(fromStringScore);
            var toScore = GetLexicalScore(toStringScore);
            return AsAsync().GetRangeWithScoresFromSortedSetByLowestScoreAsync(setId, fromScore, toScore, skip, take, token);
        }

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetByLowestScoreAsync(string setId, double fromScore, double toScore, CancellationToken token)
            => AsAsync().GetRangeWithScoresFromSortedSetByLowestScoreAsync(setId, fromScore, toScore, null, null, token);

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetByLowestScoreAsync(string setId, long fromScore, long toScore, CancellationToken token)
            => AsAsync().GetRangeWithScoresFromSortedSetByLowestScoreAsync(setId, fromScore, toScore, null, null, token);

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetByLowestScoreAsync(string setId, double fromScore, double toScore, int? skip, int? take, CancellationToken token)
            => CreateSortedScoreMapAsync(NativeAsync.ZRangeByScoreWithScoresAsync(setId, fromScore, toScore, skip, take, token));

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetByLowestScoreAsync(string setId, long fromScore, long toScore, int? skip, int? take, CancellationToken token)
            => CreateSortedScoreMapAsync(NativeAsync.ZRangeByScoreWithScoresAsync(setId, fromScore, toScore, skip, take, token));

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetByHighestScoreAsync(string setId, string fromStringScore, string toStringScore, CancellationToken token)
            => AsAsync().GetRangeFromSortedSetByHighestScoreAsync(setId, fromStringScore, toStringScore, null, null, token);

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetByHighestScoreAsync(string setId, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token)
        {
            var fromScore = GetLexicalScore(fromStringScore);
            var toScore = GetLexicalScore(toStringScore);
            return AsAsync().GetRangeFromSortedSetByHighestScoreAsync(setId, fromScore, toScore, skip, take, token);
        }

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetByHighestScoreAsync(string setId, double fromScore, double toScore, CancellationToken token)
            => AsAsync().GetRangeFromSortedSetByHighestScoreAsync(setId, fromScore, toScore, null, null, token);

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetByHighestScoreAsync(string setId, long fromScore, long toScore, CancellationToken token)
            => AsAsync().GetRangeFromSortedSetByHighestScoreAsync(setId, fromScore, toScore, null, null, token);

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetByHighestScoreAsync(string setId, double fromScore, double toScore, int? skip, int? take, CancellationToken token)
            => NativeAsync.ZRevRangeByScoreAsync(setId, fromScore, toScore, skip, take, token).ToStringListAsync();

        ValueTask<List<string>> IRedisClientAsync.GetRangeFromSortedSetByHighestScoreAsync(string setId, long fromScore, long toScore, int? skip, int? take, CancellationToken token)
            => NativeAsync.ZRevRangeByScoreAsync(setId, fromScore, toScore, skip, take, token).ToStringListAsync();

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetByHighestScoreAsync(string setId, string fromStringScore, string toStringScore, CancellationToken token)
            => AsAsync().GetRangeWithScoresFromSortedSetByHighestScoreAsync(setId, fromStringScore, toStringScore, null, null, token);

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetByHighestScoreAsync(string setId, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token)
        {
            var fromScore = GetLexicalScore(fromStringScore);
            var toScore = GetLexicalScore(toStringScore);
            return AsAsync().GetRangeWithScoresFromSortedSetByHighestScoreAsync(setId, fromScore, toScore, skip, take, token);
        }

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetByHighestScoreAsync(string setId, double fromScore, double toScore, CancellationToken token)
            => AsAsync().GetRangeWithScoresFromSortedSetByHighestScoreAsync(setId, fromScore, toScore, null, null, token);

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetByHighestScoreAsync(string setId, long fromScore, long toScore, CancellationToken token)
            => AsAsync().GetRangeWithScoresFromSortedSetByHighestScoreAsync(setId, fromScore, toScore, null, null, token);

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetByHighestScoreAsync(string setId, double fromScore, double toScore, int? skip, int? take, CancellationToken token)
            => CreateSortedScoreMapAsync(NativeAsync.ZRevRangeByScoreWithScoresAsync(setId, fromScore, toScore, skip, take, token));

        ValueTask<IDictionary<string, double>> IRedisClientAsync.GetRangeWithScoresFromSortedSetByHighestScoreAsync(string setId, long fromScore, long toScore, int? skip, int? take, CancellationToken token)
            => CreateSortedScoreMapAsync(NativeAsync.ZRevRangeByScoreWithScoresAsync(setId, fromScore, toScore, skip, take, token));

        ValueTask<long> IRedisClientAsync.RemoveRangeFromSortedSetAsync(string setId, int minRank, int maxRank, CancellationToken token)
            => NativeAsync.ZRemRangeByRankAsync(setId, minRank, maxRank, token);

        ValueTask<long> IRedisClientAsync.RemoveRangeFromSortedSetByScoreAsync(string setId, double fromScore, double toScore, CancellationToken token)
            => NativeAsync.ZRemRangeByScoreAsync(setId, fromScore, toScore, token);

        ValueTask<long> IRedisClientAsync.RemoveRangeFromSortedSetByScoreAsync(string setId, long fromScore, long toScore, CancellationToken token)
            => NativeAsync.ZRemRangeByScoreAsync(setId, fromScore, toScore, token);

        ValueTask<long> IRedisClientAsync.StoreIntersectFromSortedSetsAsync(string intoSetId, string[] setIds, CancellationToken token)
            => NativeAsync.ZInterStoreAsync(intoSetId, setIds, token);

        ValueTask<long> IRedisClientAsync.StoreIntersectFromSortedSetsAsync(string intoSetId, string[] setIds, string[] args, CancellationToken token)
            => base.ZInterStoreAsync(intoSetId, setIds, args, token);

        ValueTask<long> IRedisClientAsync.StoreUnionFromSortedSetsAsync(string intoSetId, string[] setIds, CancellationToken token)
            => NativeAsync.ZUnionStoreAsync(intoSetId, setIds, token);

        ValueTask<long> IRedisClientAsync.StoreUnionFromSortedSetsAsync(string intoSetId, string[] setIds, string[] args, CancellationToken token)
            => base.ZUnionStoreAsync(intoSetId, setIds, args, token);

        ValueTask<bool> IRedisClientAsync.HashContainsEntryAsync(string hashId, string key, CancellationToken token)
            => NativeAsync.HExistsAsync(hashId, key.ToUtf8Bytes(), token).IsSuccessAsync();

        ValueTask<bool> IRedisClientAsync.SetEntryInHashIfNotExistsAsync(string hashId, string key, string value, CancellationToken token)
            => NativeAsync.HSetNXAsync(hashId, key.ToUtf8Bytes(), value.ToUtf8Bytes(), token).IsSuccessAsync();

        ValueTask IRedisClientAsync.SetRangeInHashAsync(string hashId, IEnumerable<KeyValuePair<string, string>> keyValuePairs, CancellationToken token)
            => SetRangeInHashPrepare(keyValuePairs, out var keys, out var values) ? NativeAsync.HMSetAsync(hashId, keys, values, token) : default;

        ValueTask<long> IRedisClientAsync.IncrementValueInHashAsync(string hashId, string key, int incrementBy, CancellationToken token)
            => NativeAsync.HIncrbyAsync(hashId, key.ToUtf8Bytes(), incrementBy, token);

        ValueTask<double> IRedisClientAsync.IncrementValueInHashAsync(string hashId, string key, double incrementBy, CancellationToken token)
            => NativeAsync.HIncrbyFloatAsync(hashId, key.ToUtf8Bytes(), incrementBy, token);

        ValueTask<string> IRedisClientAsync.GetValueFromHashAsync(string hashId, string key, CancellationToken token)
            => NativeAsync.HGetAsync(hashId, key.ToUtf8Bytes(), token).FromUtf8BytesAsync();

        ValueTask<List<string>> IRedisClientAsync.GetValuesFromHashAsync(string hashId, string[] keys, CancellationToken token)
        {
            if (keys.Length == 0) return new List<string>().AsValueTaskResult();
            var keyBytes = ConvertToBytes(keys);
            return NativeAsync.HMGetAsync(hashId, keyBytes, token).ToStringListAsync();
        }

        ValueTask<bool> IRedisClientAsync.RemoveEntryFromHashAsync(string hashId, string key, CancellationToken token)
            => NativeAsync.HDelAsync(hashId, key.ToUtf8Bytes(), token).IsSuccessAsync();

        ValueTask<List<string>> IRedisClientAsync.GetHashKeysAsync(string hashId, CancellationToken token)
            => NativeAsync.HKeysAsync(hashId, token).ToStringListAsync();

        ValueTask<List<string>> IRedisClientAsync.GetHashValuesAsync(string hashId, CancellationToken token)
            => NativeAsync.HValsAsync(hashId, token).ToStringListAsync();

        ValueTask<Dictionary<string, string>> IRedisClientAsync.GetAllEntriesFromHashAsync(string hashId, CancellationToken token)
            => NativeAsync.HGetAllAsync(hashId, token).Await(ret => ret.ToStringDictionary());

        ValueTask<RedisText> IRedisClientAsync.ExecLuaAsync(string body, string[] args, CancellationToken token)
            => NativeAsync.EvalCommandAsync(body, 0, args.ToMultiByteArray(), token).Await(ret => ret.ToRedisText());

        ValueTask<RedisText> IRedisClientAsync.ExecLuaShaAsync(string sha1, string[] args, CancellationToken token)
            => NativeAsync.EvalShaCommandAsync(sha1, 0, args.ToMultiByteArray(), token).Await(ret => ret.ToRedisText());

        ValueTask<string> IRedisClientAsync.ExecLuaAsStringAsync(string body, string[] args, CancellationToken token)
            => NativeAsync.EvalStrAsync(body, 0, args.ToMultiByteArray(), token);

        ValueTask<string> IRedisClientAsync.ExecLuaShaAsStringAsync(string sha1, string[] args, CancellationToken token)
            => NativeAsync.EvalShaStrAsync(sha1, 0, args.ToMultiByteArray(), token);

        ValueTask<long> IRedisClientAsync.ExecLuaAsIntAsync(string body, string[] args, CancellationToken token)
            => NativeAsync.EvalIntAsync(body, 0, args.ToMultiByteArray(), token);

        ValueTask<long> IRedisClientAsync.ExecLuaAsIntAsync(string body, string[] keys, string[] args, CancellationToken token)
            => NativeAsync.EvalIntAsync(body, keys.Length, MergeAndConvertToBytes(keys, args), token);

        ValueTask<long> IRedisClientAsync.ExecLuaShaAsIntAsync(string sha1, string[] args, CancellationToken token)
            => NativeAsync.EvalShaIntAsync(sha1, 0, args.ToMultiByteArray(), token);

        ValueTask<long> IRedisClientAsync.ExecLuaShaAsIntAsync(string sha1, string[] keys, string[] args, CancellationToken token)
            => NativeAsync.EvalShaIntAsync(sha1, keys.Length, MergeAndConvertToBytes(keys, args), token);

        ValueTask<List<string>> IRedisClientAsync.ExecLuaAsListAsync(string body, string[] args, CancellationToken token)
            => NativeAsync.EvalAsync(body, 0, args.ToMultiByteArray(), token).ToStringListAsync();

        ValueTask<List<string>> IRedisClientAsync.ExecLuaAsListAsync(string body, string[] keys, string[] args, CancellationToken token)
            => NativeAsync.EvalAsync(body, keys.Length, MergeAndConvertToBytes(keys, args), token).ToStringListAsync();

        ValueTask<List<string>> IRedisClientAsync.ExecLuaShaAsListAsync(string sha1, string[] args, CancellationToken token)
            => NativeAsync.EvalShaAsync(sha1, 0, args.ToMultiByteArray(), token).ToStringListAsync();

        ValueTask<List<string>> IRedisClientAsync.ExecLuaShaAsListAsync(string sha1, string[] keys, string[] args, CancellationToken token)
            => NativeAsync.EvalShaAsync(sha1, keys.Length, MergeAndConvertToBytes(keys, args), token).ToStringListAsync();

        ValueTask<string> IRedisClientAsync.CalculateSha1Async(string luaBody, CancellationToken token)
            => CalculateSha1(luaBody).AsValueTaskResult();

        async ValueTask<bool> IRedisClientAsync.HasLuaScriptAsync(string sha1Ref, CancellationToken token)
        {
            var map = await AsAsync().WhichLuaScriptsExistsAsync(new[] { sha1Ref }, token).ConfigureAwait(false);
            return map[sha1Ref];
        }

        async ValueTask<Dictionary<string, bool>> IRedisClientAsync.WhichLuaScriptsExistsAsync(string[] sha1Refs, CancellationToken token)
        {
            var intFlags = await NativeAsync.ScriptExistsAsync(sha1Refs.ToMultiByteArray()).ConfigureAwait(false);
            return WhichLuaScriptsExistsParseResult(sha1Refs, intFlags);
        }

        ValueTask IRedisClientAsync.RemoveAllLuaScriptsAsync(CancellationToken token)
            => NativeAsync.ScriptFlushAsync(token);

        ValueTask IRedisClientAsync.KillRunningLuaScriptAsync(CancellationToken token)
            => NativeAsync.ScriptKillAsync(token);

        ValueTask<RedisText> IRedisClientAsync.CustomAsync(params object[] cmdWithArgs)
            => AsAsync().CustomAsync(cmdWithArgs, token: default);

        ValueTask<bool> IRedisClientAsync.RemoveEntryAsync(params string[] args)
            => AsAsync().RemoveEntryAsync(args, token: default);

        ValueTask<bool> IRedisClientAsync.AddToHyperLogAsync(string key, params string[] elements)
            => AsAsync().AddToHyperLogAsync(key, elements, token: default);

        ValueTask IRedisClientAsync.MergeHyperLogsAsync(string toKey, params string[] fromKeys)
            => AsAsync().MergeHyperLogsAsync(toKey, fromKeys, token: default);

        ValueTask<long> IRedisClientAsync.AddGeoMembersAsync(string key, params RedisGeo[] geoPoints)
            => AsAsync().AddGeoMembersAsync(key, geoPoints, token: default);

        ValueTask<string[]> IRedisClientAsync.GetGeohashesAsync(string key, params string[] members)
            => AsAsync().GetGeohashesAsync(key, members, token: default);

        ValueTask<List<RedisGeo>> IRedisClientAsync.GetGeoCoordinatesAsync(string key, params string[] members)
            => AsAsync().GetGeoCoordinatesAsync(key, members, token: default);

        ValueTask IRedisClientAsync.WatchAsync(params string[] keys)
            => AsAsync().WatchAsync(keys, token: default);

        ValueTask<HashSet<string>> IRedisClientAsync.GetIntersectFromSetsAsync(params string[] setIds)
            => AsAsync().GetIntersectFromSetsAsync(setIds, token: default);

        ValueTask IRedisClientAsync.StoreIntersectFromSetsAsync(string intoSetId, params string[] setIds)
            => AsAsync().StoreIntersectFromSetsAsync(intoSetId, setIds, token: default);

        ValueTask<HashSet<string>> IRedisClientAsync.GetUnionFromSetsAsync(params string[] setIds)
            => AsAsync().GetUnionFromSetsAsync(setIds, token: default);

        ValueTask IRedisClientAsync.StoreUnionFromSetsAsync(string intoSetId, params string[] setIds)
            => AsAsync().StoreUnionFromSetsAsync(intoSetId, setIds, token: default);

        ValueTask<HashSet<string>> IRedisClientAsync.GetDifferencesFromSetAsync(string fromSetId, params string[] withSetIds)
            => AsAsync().GetDifferencesFromSetAsync(fromSetId, withSetIds, token: default);

        ValueTask IRedisClientAsync.StoreDifferencesFromSetAsync(string intoSetId, string fromSetId, params string[] withSetIds)
            => AsAsync().StoreDifferencesFromSetAsync(intoSetId, fromSetId, withSetIds, token: default);

        ValueTask<long> IRedisClientAsync.StoreIntersectFromSortedSetsAsync(string intoSetId, params string[] setIds)
            => AsAsync().StoreIntersectFromSortedSetsAsync(intoSetId, setIds, token: default);

        ValueTask<long> IRedisClientAsync.StoreUnionFromSortedSetsAsync(string intoSetId, params string[] setIds)
            => AsAsync().StoreUnionFromSortedSetsAsync(intoSetId, setIds, token: default);

        ValueTask<List<string>> IRedisClientAsync.GetValuesFromHashAsync(string hashId, params string[] keys)
            => AsAsync().GetValuesFromHashAsync(hashId, keys, token: default);

        ValueTask<RedisText> IRedisClientAsync.ExecLuaAsync(string body, params string[] args)
            => AsAsync().ExecLuaAsync(body, args, token: default);

        ValueTask<RedisText> IRedisClientAsync.ExecLuaShaAsync(string sha1, params string[] args)
            => AsAsync().ExecLuaShaAsync(sha1, args, token: default);

        ValueTask<string> IRedisClientAsync.ExecLuaAsStringAsync(string luaBody, params string[] args)
            => AsAsync().ExecLuaAsStringAsync(luaBody, args, token: default);

        ValueTask<string> IRedisClientAsync.ExecLuaShaAsStringAsync(string sha1, params string[] args)
            => AsAsync().ExecLuaShaAsStringAsync(sha1, args, token: default);

        ValueTask<long> IRedisClientAsync.ExecLuaAsIntAsync(string luaBody, params string[] args)
            => AsAsync().ExecLuaAsIntAsync(luaBody, args, token: default);

        ValueTask<long> IRedisClientAsync.ExecLuaShaAsIntAsync(string sha1, params string[] args)
            => AsAsync().ExecLuaShaAsIntAsync(sha1, args, token: default);

        ValueTask<List<string>> IRedisClientAsync.ExecLuaAsListAsync(string luaBody, params string[] args)
            => AsAsync().ExecLuaAsListAsync(luaBody, args, token: default);

        ValueTask<List<string>> IRedisClientAsync.ExecLuaShaAsListAsync(string sha1, params string[] args)
            => AsAsync().ExecLuaShaAsListAsync(sha1, args, token: default);

        ValueTask<Dictionary<string, bool>> IRedisClientAsync.WhichLuaScriptsExistsAsync(params string[] sha1Refs)
            => AsAsync().WhichLuaScriptsExistsAsync(sha1Refs, token: default);
    }
}
