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

namespace ServiceStack.Redis
{
    internal partial class RedisClientSortedSet
        : IRedisSortedSetAsync
    {
        private IRedisClientAsync AsyncClient => client;

        ValueTask IRedisSortedSetAsync.AddAsync(string value, CancellationToken token)
            => AsyncClient.AddItemToSortedSetAsync(setId, value, token).Await();

        private IRedisSortedSetAsync AsAsync() => this;

        ValueTask IRedisSortedSetAsync.ClearAsync(CancellationToken token)
            => new ValueTask(AsyncClient.RemoveAsync(setId, token));

        ValueTask<bool> IRedisSortedSetAsync.ContainsAsync(string value, CancellationToken token)
            => AsyncClient.SortedSetContainsItemAsync(setId, value, token);

        ValueTask<int> IRedisSortedSetAsync.CountAsync(CancellationToken token)
            => AsyncClient.GetSortedSetCountAsync(setId, token).AsInt32();

        ValueTask<List<string>> IRedisSortedSetAsync.GetAllAsync(CancellationToken token)
            => AsyncClient.GetAllItemsFromSortedSetAsync(setId, token);

        async IAsyncEnumerator<string> IAsyncEnumerable<string>.GetAsyncEnumerator(CancellationToken token)
        {
            // uses ZSCAN
            await foreach (var pair in AsyncClient.ScanAllSortedSetItemsAsync(setId, token: token).ConfigureAwait(false))
            {
                yield return pair.Key;
            }
        }

        ValueTask<long> IRedisSortedSetAsync.GetItemIndexAsync(string value, CancellationToken token)
            => AsyncClient.GetItemIndexInSortedSetAsync(setId, value, token);

        ValueTask<double> IRedisSortedSetAsync.GetItemScoreAsync(string value, CancellationToken token)
            => AsyncClient.GetItemScoreInSortedSetAsync(setId, value, token);

        ValueTask<List<string>> IRedisSortedSetAsync.GetRangeAsync(int startingRank, int endingRank, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetAsync(setId, startingRank, endingRank, token);

        ValueTask<List<string>> IRedisSortedSetAsync.GetRangeByScoreAsync(string fromStringScore, string toStringScore, CancellationToken token)
            => AsAsync().GetRangeByScoreAsync(fromStringScore, toStringScore, null, null, token);

        ValueTask<List<string>> IRedisSortedSetAsync.GetRangeByScoreAsync(string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByLowestScoreAsync(setId, fromStringScore, toStringScore, skip, take, token);

        ValueTask<List<string>> IRedisSortedSetAsync.GetRangeByScoreAsync(double fromScore, double toScore, CancellationToken token)
            => AsAsync().GetRangeByScoreAsync(fromScore, toScore, null, null, token);

        ValueTask<List<string>> IRedisSortedSetAsync.GetRangeByScoreAsync(double fromScore, double toScore, int? skip, int? take, CancellationToken token)
            => AsyncClient.GetRangeFromSortedSetByLowestScoreAsync(setId, fromScore, toScore, skip, take, token);

        ValueTask IRedisSortedSetAsync.IncrementItemScoreAsync(string value, double incrementByScore, CancellationToken token)
            => AsyncClient.IncrementItemInSortedSetAsync(setId, value, incrementByScore, token).Await();

        ValueTask<string> IRedisSortedSetAsync.PopItemWithHighestScoreAsync(CancellationToken token)
            => AsyncClient.PopItemWithHighestScoreFromSortedSetAsync(setId, token);

        ValueTask<string> IRedisSortedSetAsync.PopItemWithLowestScoreAsync(CancellationToken token)
            => AsyncClient.PopItemWithLowestScoreFromSortedSetAsync(setId, token);

        ValueTask<bool> IRedisSortedSetAsync.RemoveAsync(string value, CancellationToken token)
            => AsyncClient.RemoveItemFromSortedSetAsync(setId, value, token).AwaitAsTrue(); // see Remove() for why "true"

        ValueTask IRedisSortedSetAsync.RemoveRangeAsync(int fromRank, int toRank, CancellationToken token)
            => AsyncClient.RemoveRangeFromSortedSetAsync(setId, fromRank, toRank, token).Await();

        ValueTask IRedisSortedSetAsync.RemoveRangeByScoreAsync(double fromScore, double toScore, CancellationToken token)
            => AsyncClient.RemoveRangeFromSortedSetByScoreAsync(setId, fromScore, toScore, token).Await();

        ValueTask IRedisSortedSetAsync.StoreFromIntersectAsync(IRedisSortedSetAsync[] ofSets, CancellationToken token)
            => AsyncClient.StoreIntersectFromSortedSetsAsync(setId, ofSets.GetIds(), token).Await();

        ValueTask IRedisSortedSetAsync.StoreFromIntersectAsync(params IRedisSortedSetAsync[] ofSets)
            => AsAsync().StoreFromIntersectAsync(ofSets, token: default);

        ValueTask IRedisSortedSetAsync.StoreFromUnionAsync(IRedisSortedSetAsync[] ofSets, CancellationToken token)
            => AsyncClient.StoreUnionFromSortedSetsAsync(setId, ofSets.GetIds(), token).Await();

        ValueTask IRedisSortedSetAsync.StoreFromUnionAsync(params IRedisSortedSetAsync[] ofSets)
            => AsAsync().StoreFromUnionAsync(ofSets, token: default);
    }
}