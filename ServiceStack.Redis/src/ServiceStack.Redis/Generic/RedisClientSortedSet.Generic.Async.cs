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

using ServiceStack.Redis.Internal;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Generic
{
    internal partial class RedisClientSortedSet<T>
        : IRedisSortedSetAsync<T>
    {
        IRedisTypedClientAsync<T> AsyncClient => client;

        IRedisSortedSetAsync<T> AsAsync() => this;

        ValueTask IRedisSortedSetAsync<T>.AddAsync(T item, double score, CancellationToken token)
            => AsyncClient.AddItemToSortedSetAsync(this, item, score, token);

        ValueTask<int> IRedisSortedSetAsync<T>.CountAsync(CancellationToken token)
            => AsyncClient.GetSortedSetCountAsync(this, token).AsInt32();

        ValueTask<List<T>> IRedisSortedSetAsync<T>.GetAllAsync(CancellationToken token)
            => AsyncClient.GetAllItemsFromSortedSetAsync(this, token);

        ValueTask<List<T>> IRedisSortedSetAsync<T>.GetAllDescendingAsync(CancellationToken token)
            => AsyncClient.GetAllItemsFromSortedSetDescAsync(this, token);

        async IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken token)
        {
            var count = await AsAsync().CountAsync(token).ConfigureAwait(false);
            if (count <= PageLimit)
            {
                var all = await AsyncClient.GetAllItemsFromSortedSetAsync(this, token).ConfigureAwait(false);
                foreach (var item in all)
                {
                    yield return item;
                }
            }
            else
            {
                // from GetPagingEnumerator();
                var skip = 0;
                List<T> pageResults;
                do
                {
                    pageResults = await AsyncClient.GetRangeFromSortedSetAsync(this, skip, skip + PageLimit - 1, token).ConfigureAwait(false);
                    foreach (var result in pageResults)
                    {
                        yield return result;
                    }
                    skip += PageLimit;
                } while (pageResults.Count == PageLimit);
            }
        }

        ValueTask<double> IRedisSortedSetAsync<T>.GetItemScoreAsync(T item, CancellationToken token)
            => AsyncClient.GetItemScoreInSortedSetAsync(this, item, token);

        ValueTask<List<T>> IRedisSortedSetAsync<T>.GetRangeAsync(int fromRank, int toRank, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetAsync(this, fromRank, toRank, token);

        ValueTask<List<T>> IRedisSortedSetAsync<T>.GetRangeByHighestScoreAsync(double fromScore, double toScore, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByHighestScoreAsync(this, fromScore, toScore, token);

        ValueTask<List<T>> IRedisSortedSetAsync<T>.GetRangeByHighestScoreAsync(double fromScore, double toScore, int? skip, int? take, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByHighestScoreAsync(this, fromScore, toScore, skip, take, token);

        ValueTask<List<T>> IRedisSortedSetAsync<T>.GetRangeByLowestScoreAsync(double fromScore, double toScore, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByLowestScoreAsync(this, fromScore, toScore, token);

        ValueTask<List<T>> IRedisSortedSetAsync<T>.GetRangeByLowestScoreAsync(double fromScore, double toScore, int? skip, int? take, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByLowestScoreAsync(this, fromScore, toScore, skip, take, token);

        ValueTask<double> IRedisSortedSetAsync<T>.IncrementItemAsync(T item, double incrementBy, CancellationToken token)
            => AsyncClient.IncrementItemInSortedSetAsync(this, item, incrementBy, token);

        ValueTask<int> IRedisSortedSetAsync<T>.IndexOfAsync(T item, CancellationToken token)
            => AsyncClient.GetItemIndexInSortedSetAsync(this, item, token).AsInt32();

        ValueTask<long> IRedisSortedSetAsync<T>.IndexOfDescendingAsync(T item, CancellationToken token)
            => AsyncClient.GetItemIndexInSortedSetDescAsync(this, item, token);

        ValueTask<T> IRedisSortedSetAsync<T>.PopItemWithHighestScoreAsync(CancellationToken token)
            => AsyncClient.PopItemWithHighestScoreFromSortedSetAsync(this, token);

        ValueTask<T> IRedisSortedSetAsync<T>.PopItemWithLowestScoreAsync(CancellationToken token)
            => AsyncClient.PopItemWithLowestScoreFromSortedSetAsync(this, token);

        ValueTask<long> IRedisSortedSetAsync<T>.PopulateWithIntersectOfAsync(IRedisSortedSetAsync<T>[] setIds, CancellationToken token)
            => AsyncClient.StoreIntersectFromSortedSetsAsync(this, setIds, token);

        ValueTask<long> IRedisSortedSetAsync<T>.PopulateWithIntersectOfAsync(IRedisSortedSetAsync<T>[] setIds, string[] args, CancellationToken token)
            => AsyncClient.StoreIntersectFromSortedSetsAsync(this, setIds, args, token);

        ValueTask<long> IRedisSortedSetAsync<T>.PopulateWithUnionOfAsync(IRedisSortedSetAsync<T>[] setIds, CancellationToken token)
            => AsyncClient.StoreUnionFromSortedSetsAsync(this, setIds, token);

        ValueTask<long> IRedisSortedSetAsync<T>.PopulateWithUnionOfAsync(IRedisSortedSetAsync<T>[] setIds, string[] args, CancellationToken token)
            => AsyncClient.StoreUnionFromSortedSetsAsync(this, setIds, args, token);

        ValueTask<long> IRedisSortedSetAsync<T>.RemoveRangeAsync(int minRank, int maxRank, CancellationToken token)
            => AsyncClient.RemoveRangeFromSortedSetAsync(this, minRank, maxRank, token);

        ValueTask<long> IRedisSortedSetAsync<T>.RemoveRangeByScoreAsync(double fromScore, double toScore, CancellationToken token)
            => AsyncClient.RemoveRangeFromSortedSetByScoreAsync(this, fromScore, toScore, token);

        ValueTask IRedisSortedSetAsync<T>.ClearAsync(CancellationToken token)
            => AsyncClient.RemoveEntryAsync(setId, token).Await();

        ValueTask<bool> IRedisSortedSetAsync<T>.ContainsAsync(T value, CancellationToken token)
            => AsyncClient.SortedSetContainsItemAsync(this, value, token);

        ValueTask IRedisSortedSetAsync<T>.AddAsync(T value, CancellationToken token)
            => AsyncClient.AddItemToSortedSetAsync(this, value, token);

        ValueTask<bool> IRedisSortedSetAsync<T>.RemoveAsync(T value, CancellationToken token)
            => AsyncClient.RemoveItemFromSortedSetAsync(this, value, token).AwaitAsTrue(); // see Remove for why "true"

        ValueTask<long> IRedisSortedSetAsync<T>.PopulateWithIntersectOfAsync(params IRedisSortedSetAsync<T>[] setIds)
            => AsAsync().PopulateWithIntersectOfAsync(setIds, token: default);

        ValueTask<long> IRedisSortedSetAsync<T>.PopulateWithUnionOfAsync(params IRedisSortedSetAsync<T>[] setIds)
            => AsAsync().PopulateWithUnionOfAsync(setIds, token: default);
    }
}