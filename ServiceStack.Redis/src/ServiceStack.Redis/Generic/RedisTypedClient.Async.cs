//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using ServiceStack.Data;
using ServiceStack.Model;
using ServiceStack.Redis.Internal;
using ServiceStack.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Generic
{
    partial class RedisTypedClient<T>
        : IRedisTypedClientAsync<T>
    {
        public IRedisTypedClientAsync<T> AsAsync() => this;

        private IRedisClientAsync AsyncClient => client;
        private IRedisNativeClientAsync AsyncNative => client;

        IRedisSetAsync IRedisTypedClientAsync<T>.TypeIdsSet => TypeIdsSetRaw;

        IRedisClientAsync IRedisTypedClientAsync<T>.RedisClient => client;

        internal ValueTask ExpectQueuedAsync(CancellationToken token)
            => client.ExpectQueuedAsync(token);

        internal ValueTask ExpectOkAsync(CancellationToken token)
            => client.ExpectOkAsync(token);

        internal ValueTask<int> ReadMultiDataResultCountAsync(CancellationToken token)
            => client.ReadMultiDataResultCountAsync(token);

        ValueTask<T> IRedisTypedClientAsync<T>.GetValueAsync(string key, CancellationToken token)
            => DeserializeValueAsync(AsyncNative.GetAsync(key, token));

        async ValueTask IRedisTypedClientAsync<T>.SetValueAsync(string key, T entity, CancellationToken token)
        {
            AssertNotNull(key);
            await AsyncClient.SetAsync(key, SerializeValue(entity), token).ConfigureAwait(false);
            await client.RegisterTypeIdAsync(entity, token).ConfigureAwait(false);
        }

        Task<T> IEntityStoreAsync<T>.GetByIdAsync(object id, CancellationToken token)
        {
            var key = client.UrnKey<T>(id);
            return AsAsync().GetValueAsync(key, token).AsTask();
        }

        internal ValueTask FlushSendBufferAsync(CancellationToken token)
            => client.FlushSendBufferAsync(token);

        internal ValueTask AddTypeIdsRegisteredDuringPipelineAsync(CancellationToken token)
            => client.AddTypeIdsRegisteredDuringPipelineAsync(token);

        async Task<IList<T>> IEntityStoreAsync<T>.GetByIdsAsync(IEnumerable ids, CancellationToken token)
        {
            if (ids != null)
            {
                var urnKeys = ids.Map(x => client.UrnKey<T>(x));
                if (urnKeys.Count != 0)
                    return await AsAsync().GetValuesAsync(urnKeys, token).ConfigureAwait(false);
            }

            return new List<T>();
        }

        async Task<IList<T>> IEntityStoreAsync<T>.GetAllAsync(CancellationToken token)
        {
            var allKeys = await AsyncClient.GetAllItemsFromSetAsync(this.TypeIdsSetKey, token).ConfigureAwait(false);
            return await AsAsync().GetByIdsAsync(allKeys.ToArray(), token).ConfigureAwait(false);
        }

        async Task<T> IEntityStoreAsync<T>.StoreAsync(T entity, CancellationToken token)
        {
            var urnKey = client.UrnKey(entity);
            await AsAsync().SetValueAsync(urnKey, entity, token).ConfigureAwait(false);
            return entity;
        }

        async Task IEntityStoreAsync<T>.StoreAllAsync(IEnumerable<T> entities, CancellationToken token)
        {
            if (PrepareStoreAll(entities, out var keys, out var values, out var entitiesList))
            {
                await AsyncNative.MSetAsync(keys, values, token).ConfigureAwait(false);
                await client.RegisterTypeIdsAsync(entitiesList, token).ConfigureAwait(false);
            }
        }

        async Task IEntityStoreAsync<T>.DeleteAsync(T entity, CancellationToken token)
        {
            var urnKey = client.UrnKey(entity);
            await AsyncClient.RemoveEntryAsync(new[] { urnKey }, token).ConfigureAwait(false);
            await client.RemoveTypeIdsByValueAsync(entity, token).ConfigureAwait(false);
        }

        async Task IEntityStoreAsync<T>.DeleteByIdAsync(object id, CancellationToken token)
        {
            var urnKey = client.UrnKey<T>(id);

            await AsyncClient.RemoveEntryAsync(new[] { urnKey }, token).ConfigureAwait(false);
            await client.RemoveTypeIdsByIdAsync<T>(id.ToString(), token).ConfigureAwait(false);
        }

        async Task IEntityStoreAsync<T>.DeleteByIdsAsync(IEnumerable ids, CancellationToken token)
        {
            if (ids == null) return;

            var idStrings = ids.Cast<object>().Select(x => x.ToString()).ToArray();
            var urnKeys = idStrings.Select(t => client.UrnKey<T>(t)).ToArray();
            if (urnKeys.Length > 0)
            {
                await AsyncClient.RemoveEntryAsync(urnKeys, token).ConfigureAwait(false);
                await client.RemoveTypeIdsByIdsAsync<T>(idStrings, token).ConfigureAwait(false);
            }
        }

        async Task IEntityStoreAsync<T>.DeleteAllAsync(CancellationToken token)
        {
            await DeleteAllAsync(0,RedisConfig.CommandKeysBatchSize, token).ConfigureAwait(false);
        }

        private async Task DeleteAllAsync(ulong cursor, int batchSize, CancellationToken token)
        {
            do
            {
                var scanResult = await AsyncNative.SScanAsync(this.TypeIdsSetKey, cursor, batchSize, token: token).ConfigureAwait(false);
                cursor = scanResult.Cursor;
                var urnKeys = scanResult.Results.Select(x => client.UrnKey<T>(Encoding.UTF8.GetString(x))).ToArray();
                if (urnKeys.Length > 0)
                {
                    await AsyncClient.RemoveEntryAsync(urnKeys, token).ConfigureAwait(false);
                }
            } while (cursor != 0);
            await AsyncClient.RemoveEntryAsync(new[] { this.TypeIdsSetKey }, token).ConfigureAwait(false);
        }

        async ValueTask<List<T>> IRedisTypedClientAsync<T>.GetValuesAsync(List<string> keys, CancellationToken token)
        {
            if (keys.IsNullOrEmpty()) return new List<T>();

            var resultBytesArray = await AsyncNative.MGetAsync(keys.ToArray(), token).ConfigureAwait(false);
            return ProcessGetValues(resultBytesArray);
        }

        ValueTask<IRedisTypedTransactionAsync<T>> IRedisTypedClientAsync<T>.CreateTransactionAsync(CancellationToken token)
        {
            IRedisTypedTransactionAsync<T> obj = new RedisTypedTransaction<T>(this, true);
            return obj.AsValueTaskResult();
        }

        IRedisTypedPipelineAsync<T> IRedisTypedClientAsync<T>.CreatePipeline()
            => new RedisTypedPipeline<T>(this);


        ValueTask<IAsyncDisposable> IRedisTypedClientAsync<T>.AcquireLockAsync(TimeSpan? timeOut, CancellationToken token)
            => AsyncClient.AcquireLockAsync(this.TypeLockKey, timeOut, token);

        long IRedisTypedClientAsync<T>.Db => AsyncClient.Db;

        IHasNamed<IRedisListAsync<T>> IRedisTypedClientAsync<T>.Lists => Lists as IHasNamed<IRedisListAsync<T>> ?? throw new NotSupportedException("The provided Lists does not support IRedisListAsync");
        IHasNamed<IRedisSetAsync<T>> IRedisTypedClientAsync<T>.Sets => Sets as IHasNamed<IRedisSetAsync<T>> ?? throw new NotSupportedException("The provided Sets does not support IRedisSetAsync");
        IHasNamed<IRedisSortedSetAsync<T>> IRedisTypedClientAsync<T>.SortedSets => SortedSets as IHasNamed<IRedisSortedSetAsync<T>> ?? throw new NotSupportedException("The provided SortedSets does not support IRedisSortedSetAsync");

        IRedisHashAsync<TKey, T> IRedisTypedClientAsync<T>.GetHash<TKey>(string hashId) => GetHash<TKey>(hashId) as IRedisHashAsync<TKey, T> ?? throw new NotSupportedException("The provided Hash does not support IRedisHashAsync");

        ValueTask IRedisTypedClientAsync<T>.SelectAsync(long db, CancellationToken token)
            => AsyncClient.SelectAsync(db, token);

        ValueTask<List<string>> IRedisTypedClientAsync<T>.GetAllKeysAsync(CancellationToken token)
            => AsyncClient.GetAllKeysAsync(token);

        ValueTask IRedisTypedClientAsync<T>.SetSequenceAsync(int value, CancellationToken token)
            => AsyncNative.GetSetAsync(SequenceKey, Encoding.UTF8.GetBytes(value.ToString()), token).Await();

        ValueTask<long> IRedisTypedClientAsync<T>.GetNextSequenceAsync(CancellationToken token)
            => AsAsync().IncrementValueAsync(SequenceKey, token);

        ValueTask<long> IRedisTypedClientAsync<T>.GetNextSequenceAsync(int incrBy, CancellationToken token)
            => AsAsync().IncrementValueByAsync(SequenceKey, incrBy, token);

        ValueTask<RedisKeyType> IRedisTypedClientAsync<T>.GetEntryTypeAsync(string key, CancellationToken token)
            => AsyncClient.GetEntryTypeAsync(key, token);

        ValueTask<string> IRedisTypedClientAsync<T>.GetRandomKeyAsync(CancellationToken token)
            => AsyncClient.GetRandomKeyAsync(token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AssertNotNull(object obj, string name = "key")
        {
            if (obj is null) Throw(name);
            static void Throw(string name) => throw new ArgumentNullException(name);
        }

        async ValueTask IRedisTypedClientAsync<T>.SetValueAsync(string key, T entity, TimeSpan expireIn, CancellationToken token)
        {
            AssertNotNull(key);
            await AsyncClient.SetAsync(key, SerializeValue(entity), expireIn, token).ConfigureAwait(false);
            await client.RegisterTypeIdAsync(entity, token).ConfigureAwait(false);
        }

        async ValueTask<bool> IRedisTypedClientAsync<T>.SetValueIfNotExistsAsync(string key, T entity, CancellationToken token)
        {
            var success = await AsyncNative.SetNXAsync(key, SerializeValue(entity), token).IsSuccessAsync().ConfigureAwait(false);
            if (success) await client.RegisterTypeIdAsync(entity, token).ConfigureAwait(false);
            return success;
        }

        async ValueTask<bool> IRedisTypedClientAsync<T>.SetValueIfExistsAsync(string key, T entity, CancellationToken token)
        {
            var success = await AsyncNative.SetAsync(key, SerializeValue(entity), exists: true, token: token).ConfigureAwait(false);
            if (success) await client.RegisterTypeIdAsync(entity, token).ConfigureAwait(false);
            return success;
        }

        async ValueTask<T> IRedisTypedClientAsync<T>.StoreAsync(T entity, TimeSpan expireIn, CancellationToken token)
        {
            var urnKey = client.UrnKey(entity);
            await AsAsync().SetValueAsync(urnKey, entity, expireIn, token).ConfigureAwait(false);
            return entity;
        }

        ValueTask<T> IRedisTypedClientAsync<T>.GetAndSetValueAsync(string key, T value, CancellationToken token)
            => DeserializeValueAsync(AsyncNative.GetSetAsync(key, SerializeValue(value), token));

        ValueTask<bool> IRedisTypedClientAsync<T>.ContainsKeyAsync(string key, CancellationToken token)
            => AsyncNative.ExistsAsync(key, token).IsSuccessAsync();

        ValueTask<bool> IRedisTypedClientAsync<T>.RemoveEntryAsync(string key, CancellationToken token)
            => AsyncNative.DelAsync(key, token).IsSuccessAsync();

        ValueTask<bool> IRedisTypedClientAsync<T>.RemoveEntryAsync(string[] keys, CancellationToken token)
            => AsyncNative.DelAsync(keys, token).IsSuccessAsync();

        async ValueTask<bool> IRedisTypedClientAsync<T>.RemoveEntryAsync(IHasStringId[] entities, CancellationToken token)
        {
            var ids = entities.Select(x => x.Id).ToArray();
            var success = await AsyncNative.DelAsync(ids, token).IsSuccessAsync().ConfigureAwait(false);
            if (success) await client.RemoveTypeIdsByValuesAsync(ids, token).ConfigureAwait(false);
            return success;
        }

        ValueTask<long> IRedisTypedClientAsync<T>.IncrementValueAsync(string key, CancellationToken token)
            => AsyncNative.IncrAsync(key, token);

        ValueTask<long> IRedisTypedClientAsync<T>.IncrementValueByAsync(string key, int count, CancellationToken token)
            => AsyncNative.IncrByAsync(key, count, token);

        ValueTask<long> IRedisTypedClientAsync<T>.DecrementValueAsync(string key, CancellationToken token)
            => AsyncNative.DecrAsync(key, token);

        ValueTask<long> IRedisTypedClientAsync<T>.DecrementValueByAsync(string key, int count, CancellationToken token)
            => AsyncNative.DecrByAsync(key, count, token);

        ValueTask<bool> IRedisTypedClientAsync<T>.ExpireInAsync(object id, TimeSpan expiresIn, CancellationToken token)
        {
            var key = client.UrnKey<T>(id);
            return AsyncClient.ExpireEntryInAsync(key, expiresIn, token);
        }

        ValueTask<bool> IRedisTypedClientAsync<T>.ExpireAtAsync(object id, DateTime expireAt, CancellationToken token)
        {
            var key = client.UrnKey<T>(id);
            return AsyncClient.ExpireEntryAtAsync(key, expireAt, token);
        }

        ValueTask<bool> IRedisTypedClientAsync<T>.ExpireEntryInAsync(string key, TimeSpan expireIn, CancellationToken token)
            => AsyncClient.ExpireEntryInAsync(key, expireIn, token);

        ValueTask<bool> IRedisTypedClientAsync<T>.ExpireEntryAtAsync(string key, DateTime expireAt, CancellationToken token)
            => AsyncClient.ExpireEntryAtAsync(key, expireAt, token);

        async ValueTask<TimeSpan> IRedisTypedClientAsync<T>.GetTimeToLiveAsync(string key, CancellationToken token)
            => TimeSpan.FromSeconds(await AsyncNative.TtlAsync(key, token).ConfigureAwait(false));

        ValueTask IRedisTypedClientAsync<T>.ForegroundSaveAsync(CancellationToken token)
            => AsyncClient.ForegroundSaveAsync(token);

        ValueTask IRedisTypedClientAsync<T>.BackgroundSaveAsync(CancellationToken token)
            => AsyncClient.BackgroundSaveAsync(token);

        ValueTask IRedisTypedClientAsync<T>.FlushDbAsync(CancellationToken token)
            => AsyncClient.FlushDbAsync(token);

        ValueTask IRedisTypedClientAsync<T>.FlushAllAsync(CancellationToken token)
            => new ValueTask(AsyncClient.FlushAllAsync(token));

        async ValueTask<T[]> IRedisTypedClientAsync<T>.SearchKeysAsync(string pattern, CancellationToken token)
        {
            var strKeys = await AsyncClient.SearchKeysAsync(pattern, token).ConfigureAwait(false);
            return SearchKeysParse(strKeys);
        }

        private ValueTask<List<T>> CreateList(ValueTask<byte[][]> pending)
        {
            return pending.IsCompletedSuccessfully ? CreateList(pending.Result).AsValueTaskResult() : Awaited(this, pending);
            static async ValueTask<List<T>> Awaited(RedisTypedClient<T> obj, ValueTask<byte[][]> pending)
                => obj.CreateList(await pending.ConfigureAwait(false));
        }
        private ValueTask<T> DeserializeValueAsync(ValueTask<byte[]> pending)
        {
            return pending.IsCompletedSuccessfully ? DeserializeValue(pending.Result).AsValueTaskResult() : Awaited(this, pending);
            static async ValueTask<T> Awaited(RedisTypedClient<T> obj, ValueTask<byte[]> pending)
                => obj.DeserializeValue(await pending.ConfigureAwait(false));
        }

        private static ValueTask<T> DeserializeFromStringAsync(ValueTask<string> pending)
        {
            return pending.IsCompletedSuccessfully ? DeserializeFromString(pending.Result).AsValueTaskResult() : Awaited(pending);
            static async ValueTask<T> Awaited(ValueTask<string> pending)
                => DeserializeFromString(await pending.ConfigureAwait(false));
        }

        private static ValueTask<IDictionary<T, double>> CreateGenericMapAsync(ValueTask<IDictionary<string, double>> pending)
        {
            return pending.IsCompletedSuccessfully ? CreateGenericMap(pending.Result).AsValueTaskResult() : Awaited(pending);
            static async ValueTask<IDictionary<T, double>> Awaited(ValueTask<IDictionary<string, double>> pending)
                => CreateGenericMap(await pending.ConfigureAwait(false));
        }

        private static ValueTask<Dictionary<TKey, TValue>> ConvertEachToAsync<TKey, TValue>(ValueTask<Dictionary<string, string>> pending)
        {
            return pending.IsCompletedSuccessfully ? ConvertEachTo<TKey, TValue>(pending.Result).AsValueTaskResult() : Awaited(pending);
            static async ValueTask<Dictionary<TKey, TValue>> Awaited(ValueTask<Dictionary<string, string>> pending)
                => ConvertEachTo<TKey, TValue>(await pending.ConfigureAwait(false));
        }

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetSortedEntryValuesAsync(IRedisSetAsync<T> fromSet, int startingFrom, int endingAt, CancellationToken token)
        {
            var sortOptions = new SortOptions { Skip = startingFrom, Take = endingAt, };
            var multiDataList = AsyncNative.SortAsync(fromSet.Id, sortOptions, token);
            return CreateList(multiDataList);
        }

        ValueTask IRedisTypedClientAsync<T>.StoreAsHashAsync(T entity, CancellationToken token)
            => AsyncClient.StoreAsHashAsync(entity, token);

        ValueTask<T> IRedisTypedClientAsync<T>.GetFromHashAsync(object id, CancellationToken token)
            => AsyncClient.GetFromHashAsync<T>(id, token);

        async ValueTask<HashSet<T>> IRedisTypedClientAsync<T>.GetAllItemsFromSetAsync(IRedisSetAsync<T> fromSet, CancellationToken token)
        {
            var multiDataList = await AsyncNative.SMembersAsync(fromSet.Id, token).ConfigureAwait(false);
            return CreateHashSet(multiDataList);
        }

        ValueTask IRedisTypedClientAsync<T>.AddItemToSetAsync(IRedisSetAsync<T> toSet, T item, CancellationToken token)
            => AsyncNative.SAddAsync(toSet.Id, SerializeValue(item), token).Await();

        ValueTask IRedisTypedClientAsync<T>.RemoveItemFromSetAsync(IRedisSetAsync<T> fromSet, T item, CancellationToken token)
            => AsyncNative.SRemAsync(fromSet.Id, SerializeValue(item), token).Await();

        ValueTask<T> IRedisTypedClientAsync<T>.PopItemFromSetAsync(IRedisSetAsync<T> fromSet, CancellationToken token)
            => DeserializeValueAsync(AsyncNative.SPopAsync(fromSet.Id, token));

        ValueTask IRedisTypedClientAsync<T>.MoveBetweenSetsAsync(IRedisSetAsync<T> fromSet, IRedisSetAsync<T> toSet, T item, CancellationToken token)
            => AsyncNative.SMoveAsync(fromSet.Id, toSet.Id, SerializeValue(item), token);

        ValueTask<long> IRedisTypedClientAsync<T>.GetSetCountAsync(IRedisSetAsync<T> set, CancellationToken token)
            => AsyncNative.SCardAsync(set.Id, token);

        ValueTask<bool> IRedisTypedClientAsync<T>.SetContainsItemAsync(IRedisSetAsync<T> set, T item, CancellationToken token)
            => AsyncNative.SIsMemberAsync(set.Id, SerializeValue(item), token).IsSuccessAsync();

        async ValueTask<HashSet<T>> IRedisTypedClientAsync<T>.GetIntersectFromSetsAsync(IRedisSetAsync<T>[] sets, CancellationToken token)
        {
            var multiDataList = await AsyncNative.SInterAsync(sets.Map(x => x.Id).ToArray(), token).ConfigureAwait(false);
            return CreateHashSet(multiDataList);
        }

        ValueTask IRedisTypedClientAsync<T>.StoreIntersectFromSetsAsync(IRedisSetAsync<T> intoSet, IRedisSetAsync<T>[] sets, CancellationToken token)
            => AsyncNative.SInterStoreAsync(intoSet.Id, sets.Map(x => x.Id).ToArray(), token);

        async ValueTask<HashSet<T>> IRedisTypedClientAsync<T>.GetUnionFromSetsAsync(IRedisSetAsync<T>[] sets, CancellationToken token)
        {
            var multiDataList = await AsyncNative.SUnionAsync(sets.Map(x => x.Id).ToArray(), token).ConfigureAwait(false);
            return CreateHashSet(multiDataList);
        }

        ValueTask IRedisTypedClientAsync<T>.StoreUnionFromSetsAsync(IRedisSetAsync<T> intoSet, IRedisSetAsync<T>[] sets, CancellationToken token)
            => AsyncNative.SUnionStoreAsync(intoSet.Id, sets.Map(x => x.Id).ToArray(), token);

        async ValueTask<HashSet<T>> IRedisTypedClientAsync<T>.GetDifferencesFromSetAsync(IRedisSetAsync<T> fromSet, IRedisSetAsync<T>[] withSets, CancellationToken token)
        {
            var multiDataList = await AsyncNative.SDiffAsync(fromSet.Id, withSets.Map(x => x.Id).ToArray(), token).ConfigureAwait(false);
            return CreateHashSet(multiDataList);
        }

        ValueTask IRedisTypedClientAsync<T>.StoreDifferencesFromSetAsync(IRedisSetAsync<T> intoSet, IRedisSetAsync<T> fromSet, IRedisSetAsync<T>[] withSets, CancellationToken token)
            => AsyncNative.SDiffStoreAsync(intoSet.Id, fromSet.Id, withSets.Map(x => x.Id).ToArray(), token);

        ValueTask<T> IRedisTypedClientAsync<T>.GetRandomItemFromSetAsync(IRedisSetAsync<T> fromSet, CancellationToken token)
            => DeserializeValueAsync(AsyncNative.SRandMemberAsync(fromSet.Id, token));

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetAllItemsFromListAsync(IRedisListAsync<T> fromList, CancellationToken token)
        {
            var multiDataList = AsyncNative.LRangeAsync(fromList.Id, FirstElement, LastElement, token);
            return CreateList(multiDataList);
        }

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetRangeFromListAsync(IRedisListAsync<T> fromList, int startingFrom, int endingAt, CancellationToken token)
        {
            var multiDataList = AsyncNative.LRangeAsync(fromList.Id, startingFrom, endingAt, token);
            return CreateList(multiDataList);
        }

        ValueTask<List<T>> IRedisTypedClientAsync<T>.SortListAsync(IRedisListAsync<T> fromList, int startingFrom, int endingAt, CancellationToken token)
        {
            var sortOptions = new SortOptions { Skip = startingFrom, Take = endingAt, };
            var multiDataList = AsyncNative.SortAsync(fromList.Id, sortOptions, token);
            return CreateList(multiDataList);
        }

        ValueTask IRedisTypedClientAsync<T>.AddItemToListAsync(IRedisListAsync<T> fromList, T value, CancellationToken token)
            => AsyncNative.RPushAsync(fromList.Id, SerializeValue(value), token).Await();

        ValueTask IRedisTypedClientAsync<T>.PrependItemToListAsync(IRedisListAsync<T> fromList, T value, CancellationToken token)
            => AsyncNative.LPushAsync(fromList.Id, SerializeValue(value), token).Await();

        ValueTask<T> IRedisTypedClientAsync<T>.RemoveStartFromListAsync(IRedisListAsync<T> fromList, CancellationToken token)
            => DeserializeValueAsync(AsyncNative.LPopAsync(fromList.Id, token));

        async ValueTask<T> IRedisTypedClientAsync<T>.BlockingRemoveStartFromListAsync(IRedisListAsync<T> fromList, TimeSpan? timeOut, CancellationToken token)
        {
            var unblockingKeyAndValue = await AsyncNative.BLPopAsync(fromList.Id, (int)timeOut.GetValueOrDefault().TotalSeconds, token).ConfigureAwait(false);
            return unblockingKeyAndValue.Length == 0
                ? default
                : DeserializeValue(unblockingKeyAndValue[1]);
        }

        ValueTask<T> IRedisTypedClientAsync<T>.RemoveEndFromListAsync(IRedisListAsync<T> fromList, CancellationToken token)
            => DeserializeValueAsync(AsyncNative.RPopAsync(fromList.Id, token));

        ValueTask IRedisTypedClientAsync<T>.RemoveAllFromListAsync(IRedisListAsync<T> fromList, CancellationToken token)
            => AsyncNative.LTrimAsync(fromList.Id, int.MaxValue, FirstElement, token);

        ValueTask IRedisTypedClientAsync<T>.TrimListAsync(IRedisListAsync<T> fromList, int keepStartingFrom, int keepEndingAt, CancellationToken token)
            => AsyncNative.LTrimAsync(fromList.Id, keepStartingFrom, keepEndingAt, token);

        ValueTask<long> IRedisTypedClientAsync<T>.RemoveItemFromListAsync(IRedisListAsync<T> fromList, T value, CancellationToken token)
        {
            const int removeAll = 0;
            return AsyncNative.LRemAsync(fromList.Id, removeAll, SerializeValue(value), token);
        }

        ValueTask<long> IRedisTypedClientAsync<T>.RemoveItemFromListAsync(IRedisListAsync<T> fromList, T value, int noOfMatches, CancellationToken token)
            => AsyncNative.LRemAsync(fromList.Id, noOfMatches, SerializeValue(value), token);

        ValueTask<long> IRedisTypedClientAsync<T>.GetListCountAsync(IRedisListAsync<T> fromList, CancellationToken token)
            => AsyncNative.LLenAsync(fromList.Id, token);

        ValueTask<T> IRedisTypedClientAsync<T>.GetItemFromListAsync(IRedisListAsync<T> fromList, int listIndex, CancellationToken token)
            => DeserializeValueAsync(AsyncNative.LIndexAsync(fromList.Id, listIndex, token));

        ValueTask IRedisTypedClientAsync<T>.SetItemInListAsync(IRedisListAsync<T> toList, int listIndex, T value, CancellationToken token)
            => AsyncNative.LSetAsync(toList.Id, listIndex, SerializeValue(value), token);

        ValueTask IRedisTypedClientAsync<T>.InsertBeforeItemInListAsync(IRedisListAsync<T> toList, T pivot, T value, CancellationToken token)
            => AsyncNative.LInsertAsync(toList.Id, insertBefore: true, pivot: SerializeValue(pivot), value: SerializeValue(value), token: token);

        ValueTask IRedisTypedClientAsync<T>.InsertAfterItemInListAsync(IRedisListAsync<T> toList, T pivot, T value, CancellationToken token)
            => AsyncNative.LInsertAsync(toList.Id, insertBefore: false, pivot: SerializeValue(pivot), value: SerializeValue(value), token: token);

        ValueTask IRedisTypedClientAsync<T>.EnqueueItemOnListAsync(IRedisListAsync<T> fromList, T item, CancellationToken token)
            => AsyncNative.LPushAsync(fromList.Id, SerializeValue(item), token).Await();

        ValueTask<T> IRedisTypedClientAsync<T>.DequeueItemFromListAsync(IRedisListAsync<T> fromList, CancellationToken token)
            => DeserializeValueAsync(AsyncNative.RPopAsync(fromList.Id, token));

        async ValueTask<T> IRedisTypedClientAsync<T>.BlockingDequeueItemFromListAsync(IRedisListAsync<T> fromList, TimeSpan? timeOut, CancellationToken token)
        {
            var unblockingKeyAndValue = await AsyncNative.BRPopAsync(fromList.Id, (int)timeOut.GetValueOrDefault().TotalSeconds, token).ConfigureAwait(false);
            return unblockingKeyAndValue.Length == 0
                ? default
                : DeserializeValue(unblockingKeyAndValue[1]);
        }

        ValueTask IRedisTypedClientAsync<T>.PushItemToListAsync(IRedisListAsync<T> fromList, T item, CancellationToken token)
            => AsyncNative.RPushAsync(fromList.Id, SerializeValue(item), token).Await();

        ValueTask<T> IRedisTypedClientAsync<T>.PopItemFromListAsync(IRedisListAsync<T> fromList, CancellationToken token)
            => DeserializeValueAsync(AsyncNative.RPopAsync(fromList.Id, token));

        async ValueTask<T> IRedisTypedClientAsync<T>.BlockingPopItemFromListAsync(IRedisListAsync<T> fromList, TimeSpan? timeOut, CancellationToken token)
        {
            var unblockingKeyAndValue = await AsyncNative.BRPopAsync(fromList.Id, (int)timeOut.GetValueOrDefault().TotalSeconds, token).ConfigureAwait(false);
            return unblockingKeyAndValue.Length == 0
                ? default
                : DeserializeValue(unblockingKeyAndValue[1]);
        }

        ValueTask<T> IRedisTypedClientAsync<T>.PopAndPushItemBetweenListsAsync(IRedisListAsync<T> fromList, IRedisListAsync<T> toList, CancellationToken token)
            => DeserializeValueAsync(AsyncNative.RPopLPushAsync(fromList.Id, toList.Id, token));

        ValueTask<T> IRedisTypedClientAsync<T>.BlockingPopAndPushItemBetweenListsAsync(IRedisListAsync<T> fromList, IRedisListAsync<T> toList, TimeSpan? timeOut, CancellationToken token)
            => DeserializeValueAsync(AsyncNative.BRPopLPushAsync(fromList.Id, toList.Id, (int)timeOut.GetValueOrDefault().TotalSeconds, token));

        ValueTask IRedisTypedClientAsync<T>.AddItemToSortedSetAsync(IRedisSortedSetAsync<T> toSet, T value, CancellationToken token)
            => AsyncClient.AddItemToSortedSetAsync(toSet.Id, value.SerializeToString(), token).Await();

        ValueTask IRedisTypedClientAsync<T>.AddItemToSortedSetAsync(IRedisSortedSetAsync<T> toSet, T value, double score, CancellationToken token)
            => AsyncClient.AddItemToSortedSetAsync(toSet.Id, value.SerializeToString(), score, token).Await();

        ValueTask<bool> IRedisTypedClientAsync<T>.RemoveItemFromSortedSetAsync(IRedisSortedSetAsync<T> fromSet, T value, CancellationToken token)
            => AsyncClient.RemoveItemFromSortedSetAsync(fromSet.Id, value.SerializeToString(), token);

        ValueTask<T> IRedisTypedClientAsync<T>.PopItemWithLowestScoreFromSortedSetAsync(IRedisSortedSetAsync<T> fromSet, CancellationToken token)
            => DeserializeFromStringAsync(AsyncClient.PopItemWithLowestScoreFromSortedSetAsync(fromSet.Id, token));

        ValueTask<T> IRedisTypedClientAsync<T>.PopItemWithHighestScoreFromSortedSetAsync(IRedisSortedSetAsync<T> fromSet, CancellationToken token)
            => DeserializeFromStringAsync(AsyncClient.PopItemWithHighestScoreFromSortedSetAsync(fromSet.Id, token));

        ValueTask<bool> IRedisTypedClientAsync<T>.SortedSetContainsItemAsync(IRedisSortedSetAsync<T> set, T value, CancellationToken token)
            => AsyncClient.SortedSetContainsItemAsync(set.Id, value.SerializeToString(), token);

        ValueTask<double> IRedisTypedClientAsync<T>.IncrementItemInSortedSetAsync(IRedisSortedSetAsync<T> set, T value, double incrementBy, CancellationToken token)
            => AsyncClient.IncrementItemInSortedSetAsync(set.Id, value.SerializeToString(), incrementBy, token);

        ValueTask<long> IRedisTypedClientAsync<T>.GetItemIndexInSortedSetAsync(IRedisSortedSetAsync<T> set, T value, CancellationToken token)
            => AsyncClient.GetItemIndexInSortedSetAsync(set.Id, value.SerializeToString(), token);

        ValueTask<long> IRedisTypedClientAsync<T>.GetItemIndexInSortedSetDescAsync(IRedisSortedSetAsync<T> set, T value, CancellationToken token)
            => AsyncClient.GetItemIndexInSortedSetDescAsync(set.Id, value.SerializeToString(), token);

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetAllItemsFromSortedSetAsync(IRedisSortedSetAsync<T> set, CancellationToken token)
            => AsyncClient.GetAllItemsFromSortedSetAsync(set.Id, token).ConvertEachToAsync<T>();

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetAllItemsFromSortedSetDescAsync(IRedisSortedSetAsync<T> set, CancellationToken token)
            => AsyncClient.GetAllItemsFromSortedSetDescAsync(set.Id, token).ConvertEachToAsync<T>();

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetRangeFromSortedSetAsync(IRedisSortedSetAsync<T> set, int fromRank, int toRank, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetAsync(set.Id, fromRank, toRank, token).ConvertEachToAsync<T>();

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetRangeFromSortedSetDescAsync(IRedisSortedSetAsync<T> set, int fromRank, int toRank, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetDescAsync(set.Id, fromRank, toRank, token).ConvertEachToAsync<T>();

        ValueTask<IDictionary<T, double>> IRedisTypedClientAsync<T>.GetAllWithScoresFromSortedSetAsync(IRedisSortedSetAsync<T> set, CancellationToken token)
            => CreateGenericMapAsync(AsyncClient.GetRangeWithScoresFromSortedSetAsync(set.Id, FirstElement, LastElement, token));

        ValueTask<IDictionary<T, double>> IRedisTypedClientAsync<T>.GetRangeWithScoresFromSortedSetAsync(IRedisSortedSetAsync<T> set, int fromRank, int toRank, CancellationToken token)
            => CreateGenericMapAsync(AsyncClient.GetRangeWithScoresFromSortedSetAsync(set.Id, fromRank, toRank, token));

        ValueTask<IDictionary<T, double>> IRedisTypedClientAsync<T>.GetRangeWithScoresFromSortedSetDescAsync(IRedisSortedSetAsync<T> set, int fromRank, int toRank, CancellationToken token)
            => CreateGenericMapAsync(AsyncClient.GetRangeWithScoresFromSortedSetDescAsync(set.Id, fromRank, toRank, token));

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetRangeFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByLowestScoreAsync(set.Id, fromStringScore, toStringScore, token).ConvertEachToAsync<T>();

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetRangeFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByLowestScoreAsync(set.Id, fromStringScore, toStringScore, skip, take, token).ConvertEachToAsync<T>();

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetRangeFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByLowestScoreAsync(set.Id, fromScore, toScore, token).ConvertEachToAsync<T>();

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetRangeFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, int? skip, int? take, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByLowestScoreAsync(set.Id, fromScore, toScore, skip, take, token).ConvertEachToAsync<T>();

        ValueTask<IDictionary<T, double>> IRedisTypedClientAsync<T>.GetRangeWithScoresFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, CancellationToken token)
            => CreateGenericMapAsync(AsyncClient.GetRangeWithScoresFromSortedSetByLowestScoreAsync(set.Id, fromStringScore, toStringScore, token));

        ValueTask<IDictionary<T, double>> IRedisTypedClientAsync<T>.GetRangeWithScoresFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token)
            => CreateGenericMapAsync(AsyncClient.GetRangeWithScoresFromSortedSetByLowestScoreAsync(set.Id, fromStringScore, toStringScore, skip, take, token));

        ValueTask<IDictionary<T, double>> IRedisTypedClientAsync<T>.GetRangeWithScoresFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, CancellationToken token)
            => CreateGenericMapAsync(AsyncClient.GetRangeWithScoresFromSortedSetByLowestScoreAsync(set.Id, fromScore, toScore, token));

        ValueTask<IDictionary<T, double>> IRedisTypedClientAsync<T>.GetRangeWithScoresFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, int? skip, int? take, CancellationToken token)
            => CreateGenericMapAsync(AsyncClient.GetRangeWithScoresFromSortedSetByLowestScoreAsync(set.Id, fromScore, toScore, skip, take, token));
        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetRangeFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByHighestScoreAsync(set.Id, fromStringScore, toStringScore, token).ConvertEachToAsync<T>();

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetRangeFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByHighestScoreAsync(set.Id, fromStringScore, toStringScore, skip, take, token).ConvertEachToAsync<T>();

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetRangeFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByHighestScoreAsync(set.Id, fromScore, toScore, token).ConvertEachToAsync<T>();

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetRangeFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, int? skip, int? take, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByHighestScoreAsync(set.Id, fromScore, toScore, skip, take, token).ConvertEachToAsync<T>();

        ValueTask<IDictionary<T, double>> IRedisTypedClientAsync<T>.GetRangeWithScoresFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, CancellationToken token)
            => CreateGenericMapAsync(AsyncClient.GetRangeWithScoresFromSortedSetByHighestScoreAsync(set.Id, fromStringScore, toStringScore, token));

        ValueTask<IDictionary<T, double>> IRedisTypedClientAsync<T>.GetRangeWithScoresFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token)
            => CreateGenericMapAsync(AsyncClient.GetRangeWithScoresFromSortedSetByHighestScoreAsync(set.Id, fromStringScore, toStringScore, skip, take, token));

        ValueTask<IDictionary<T, double>> IRedisTypedClientAsync<T>.GetRangeWithScoresFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, CancellationToken token)
            => CreateGenericMapAsync(AsyncClient.GetRangeWithScoresFromSortedSetByHighestScoreAsync(set.Id, fromScore, toScore, token));

        ValueTask<IDictionary<T, double>> IRedisTypedClientAsync<T>.GetRangeWithScoresFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, int? skip, int? take, CancellationToken token)
            => CreateGenericMapAsync(AsyncClient.GetRangeWithScoresFromSortedSetByHighestScoreAsync(set.Id, fromScore, toScore, skip, take, token));

        ValueTask<long> IRedisTypedClientAsync<T>.RemoveRangeFromSortedSetAsync(IRedisSortedSetAsync<T> set, int minRank, int maxRank, CancellationToken token)
            => AsyncClient.RemoveRangeFromSortedSetAsync(set.Id, minRank, maxRank, token);

        ValueTask<long> IRedisTypedClientAsync<T>.RemoveRangeFromSortedSetByScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, CancellationToken token)
            => AsyncClient.RemoveRangeFromSortedSetByScoreAsync(set.Id, fromScore, toScore, token);

        ValueTask<long> IRedisTypedClientAsync<T>.GetSortedSetCountAsync(IRedisSortedSetAsync<T> set, CancellationToken token)
            => AsyncClient.GetSortedSetCountAsync(set.Id, token);

        ValueTask<double> IRedisTypedClientAsync<T>.GetItemScoreInSortedSetAsync(IRedisSortedSetAsync<T> set, T value, CancellationToken token)
            => AsyncClient.GetItemScoreInSortedSetAsync(set.Id, value.SerializeToString(), token);

        ValueTask<long> IRedisTypedClientAsync<T>.StoreIntersectFromSortedSetsAsync(IRedisSortedSetAsync<T> intoSetId, IRedisSortedSetAsync<T>[] setIds, CancellationToken token)
            => AsyncClient.StoreIntersectFromSortedSetsAsync(intoSetId.Id, setIds.Map(x => x.Id).ToArray(), token);

        ValueTask<long> IRedisTypedClientAsync<T>.StoreIntersectFromSortedSetsAsync(IRedisSortedSetAsync<T> intoSetId, IRedisSortedSetAsync<T>[] setIds, string[] args, CancellationToken token)
            => AsyncClient.StoreIntersectFromSortedSetsAsync(intoSetId.Id, setIds.Map(x => x.Id).ToArray(), args, token);

        ValueTask<long> IRedisTypedClientAsync<T>.StoreUnionFromSortedSetsAsync(IRedisSortedSetAsync<T> intoSetId, IRedisSortedSetAsync<T>[] setIds, CancellationToken token)
            => AsyncClient.StoreUnionFromSortedSetsAsync(intoSetId.Id, setIds.Map(x => x.Id).ToArray(), token);

        ValueTask<long> IRedisTypedClientAsync<T>.StoreUnionFromSortedSetsAsync(IRedisSortedSetAsync<T> intoSetId, IRedisSortedSetAsync<T>[] setIds, string[] args, CancellationToken token)
            => AsyncClient.StoreUnionFromSortedSetsAsync(intoSetId.Id, setIds.Map(x => x.Id).ToArray(), args, token);

        ValueTask<bool> IRedisTypedClientAsync<T>.HashContainsEntryAsync<TKey>(IRedisHashAsync<TKey, T> hash, TKey key, CancellationToken token)
            => AsyncClient.HashContainsEntryAsync(hash.Id, key.SerializeToString(), token);

        ValueTask<bool> IRedisTypedClientAsync<T>.SetEntryInHashAsync<TKey>(IRedisHashAsync<TKey, T> hash, TKey key, T value, CancellationToken token)
            => AsyncClient.SetEntryInHashAsync(hash.Id, key.SerializeToString(), value.SerializeToString(), token);

        ValueTask<bool> IRedisTypedClientAsync<T>.SetEntryInHashIfNotExistsAsync<TKey>(IRedisHashAsync<TKey, T> hash, TKey key, T value, CancellationToken token)
            => AsyncClient.SetEntryInHashIfNotExistsAsync(hash.Id, key.SerializeToString(), value.SerializeToString(), token);

        ValueTask IRedisTypedClientAsync<T>.SetRangeInHashAsync<TKey>(IRedisHashAsync<TKey, T> hash, IEnumerable<KeyValuePair<TKey, T>> keyValuePairs, CancellationToken token)
        {
            var stringKeyValuePairs = keyValuePairs.ToList().ConvertAll(
                x => new KeyValuePair<string, string>(x.Key.SerializeToString(), x.Value.SerializeToString()));

            return AsyncClient.SetRangeInHashAsync(hash.Id, stringKeyValuePairs, token);
        }

        ValueTask<T> IRedisTypedClientAsync<T>.GetValueFromHashAsync<TKey>(IRedisHashAsync<TKey, T> hash, TKey key, CancellationToken token)
            => DeserializeFromStringAsync(AsyncClient.GetValueFromHashAsync(hash.Id, key.SerializeToString(), token));

        ValueTask<bool> IRedisTypedClientAsync<T>.RemoveEntryFromHashAsync<TKey>(IRedisHashAsync<TKey, T> hash, TKey key, CancellationToken token)
            => AsyncClient.RemoveEntryFromHashAsync(hash.Id, key.SerializeToString(), token);

        ValueTask<long> IRedisTypedClientAsync<T>.GetHashCountAsync<TKey>(IRedisHashAsync<TKey, T> hash, CancellationToken token)
            => AsyncClient.GetHashCountAsync(hash.Id, token);

        ValueTask<List<TKey>> IRedisTypedClientAsync<T>.GetHashKeysAsync<TKey>(IRedisHashAsync<TKey, T> hash, CancellationToken token)
            => AsyncClient.GetHashKeysAsync(hash.Id, token).ConvertEachToAsync<TKey>();

        ValueTask<List<T>> IRedisTypedClientAsync<T>.GetHashValuesAsync<TKey>(IRedisHashAsync<TKey, T> hash, CancellationToken token)
            => AsyncClient.GetHashValuesAsync(hash.Id, token).ConvertEachToAsync<T>();

        ValueTask<Dictionary<TKey, T>> IRedisTypedClientAsync<T>.GetAllEntriesFromHashAsync<TKey>(IRedisHashAsync<TKey, T> hash, CancellationToken token)
            => ConvertEachToAsync<TKey, T>(AsyncClient.GetAllEntriesFromHashAsync(hash.Id, token));

        async ValueTask IRedisTypedClientAsync<T>.StoreRelatedEntitiesAsync<TChild>(object parentId, List<TChild> children, CancellationToken token)
        {
            var childRefKey = GetChildReferenceSetKey<TChild>(parentId);
            var childKeys = children.ConvertAll(x => client.UrnKey(x));

            await using var trans = await AsyncClient.CreateTransactionAsync(token).ConfigureAwait(false);
            //Ugly but need access to a generic constraint-free StoreAll method
            trans.QueueCommand(c => ((RedisClient)c).StoreAllAsyncImpl(children, token));
            trans.QueueCommand(c => c.AddRangeToSetAsync(childRefKey, childKeys, token));

            await trans.CommitAsync(token).ConfigureAwait(false);
        }

        ValueTask IRedisTypedClientAsync<T>.StoreRelatedEntitiesAsync<TChild>(object parentId, TChild[] children, CancellationToken token)
            => AsAsync().StoreRelatedEntitiesAsync(parentId, new List<TChild>(children), token);

        ValueTask IRedisTypedClientAsync<T>.DeleteRelatedEntitiesAsync<TChild>(object parentId, CancellationToken token)
        {
            var childRefKey = GetChildReferenceSetKey<TChild>(parentId);
            return new ValueTask(AsyncClient.RemoveAsync(childRefKey, token));
        }

        ValueTask IRedisTypedClientAsync<T>.DeleteRelatedEntityAsync<TChild>(object parentId, object childId, CancellationToken token)
        {
            var childRefKey = GetChildReferenceSetKey<TChild>(parentId);
            return AsyncClient.RemoveItemFromSetAsync(childRefKey, TypeSerializer.SerializeToString(childId), token);
        }

        async ValueTask<List<TChild>> IRedisTypedClientAsync<T>.GetRelatedEntitiesAsync<TChild>(object parentId, CancellationToken token)
        {
            var childRefKey = GetChildReferenceSetKey<TChild>(parentId);
            var childKeys = (await AsyncClient.GetAllItemsFromSetAsync(childRefKey, token).ConfigureAwait(false)).ToList();

            return await AsyncClient.As<TChild>().GetValuesAsync(childKeys, token).ConfigureAwait(false);
        }

        ValueTask<long> IRedisTypedClientAsync<T>.GetRelatedEntitiesCountAsync<TChild>(object parentId, CancellationToken token)
        {
            var childRefKey = GetChildReferenceSetKey<TChild>(parentId);
            return AsyncClient.GetSetCountAsync(childRefKey, token);
        }

        ValueTask IRedisTypedClientAsync<T>.AddToRecentsListAsync(T value, CancellationToken token)
        {
            var key = client.UrnKey(value);
            var nowScore = DateTime.UtcNow.ToUnixTime();
            return AsyncClient.AddItemToSortedSetAsync(RecentSortedSetKey, key, nowScore, token).Await();
        }

        async ValueTask<List<T>> IRedisTypedClientAsync<T>.GetLatestFromRecentsListAsync(int skip, int take, CancellationToken token)
        {
            var toRank = take - 1;
            var keys = await AsyncClient.GetRangeFromSortedSetDescAsync(RecentSortedSetKey, skip, toRank, token).ConfigureAwait(false);
            var values = await AsAsync().GetValuesAsync(keys, token).ConfigureAwait(false);
            return values;
        }

        async ValueTask<List<T>> IRedisTypedClientAsync<T>.GetEarliestFromRecentsListAsync(int skip, int take, CancellationToken token)
        {
            var toRank = take - 1;
            var keys = await AsyncClient.GetRangeFromSortedSetAsync(RecentSortedSetKey, skip, toRank, token).ConfigureAwait(false);
            var values = await AsAsync().GetValuesAsync(keys, token).ConfigureAwait(false);
            return values;
        }

        ValueTask<bool> IRedisTypedClientAsync<T>.RemoveEntryAsync(params string[] args)
            => AsAsync().RemoveEntryAsync(args, token: default);

        ValueTask<bool> IRedisTypedClientAsync<T>.RemoveEntryAsync(params IHasStringId[] entities)
            => AsAsync().RemoveEntryAsync(entities, token: default);

        ValueTask<HashSet<T>> IRedisTypedClientAsync<T>.GetIntersectFromSetsAsync(params IRedisSetAsync<T>[] sets)
            => AsAsync().GetIntersectFromSetsAsync(sets, token: default);

        ValueTask IRedisTypedClientAsync<T>.StoreIntersectFromSetsAsync(IRedisSetAsync<T> intoSet, params IRedisSetAsync<T>[] sets)
            => AsAsync().StoreIntersectFromSetsAsync(intoSet, sets, token: default);

        ValueTask<HashSet<T>> IRedisTypedClientAsync<T>.GetUnionFromSetsAsync(params IRedisSetAsync<T>[] sets)
            => AsAsync().GetUnionFromSetsAsync(sets, token: default);

        ValueTask IRedisTypedClientAsync<T>.StoreUnionFromSetsAsync(IRedisSetAsync<T> intoSet, params IRedisSetAsync<T>[] sets)
            => AsAsync().StoreUnionFromSetsAsync(intoSet, sets, token: default);

        ValueTask<HashSet<T>> IRedisTypedClientAsync<T>.GetDifferencesFromSetAsync(IRedisSetAsync<T> fromSet, params IRedisSetAsync<T>[] withSets)
            => AsAsync().GetDifferencesFromSetAsync(fromSet, withSets, token: default);

        ValueTask IRedisTypedClientAsync<T>.StoreDifferencesFromSetAsync(IRedisSetAsync<T> intoSet, IRedisSetAsync<T> fromSet, params IRedisSetAsync<T>[] withSets)
            => AsAsync().StoreDifferencesFromSetAsync(intoSet, fromSet, withSets, token: default);

        ValueTask<long> IRedisTypedClientAsync<T>.StoreIntersectFromSortedSetsAsync(IRedisSortedSetAsync<T> intoSetId, params IRedisSortedSetAsync<T>[] setIds)
            => AsAsync().StoreIntersectFromSortedSetsAsync(intoSetId, setIds, token: default);

        ValueTask<long> IRedisTypedClientAsync<T>.StoreUnionFromSortedSetsAsync(IRedisSortedSetAsync<T> intoSetId, params IRedisSortedSetAsync<T>[] setIds)
            => AsAsync().StoreUnionFromSortedSetsAsync(intoSetId, setIds, token: default);

        ValueTask IRedisTypedClientAsync<T>.StoreRelatedEntitiesAsync<TChild>(object parentId, params TChild[] children)
            => AsAsync().StoreRelatedEntitiesAsync<TChild>(parentId, children, token: default);
    }
}