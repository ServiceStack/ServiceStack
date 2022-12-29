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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis
{
    internal partial class RedisClientSet
        : IRedisSetAsync
    {
        private IRedisSetAsync AsAsync() => this;
        private IRedisClientAsync AsyncClient => client;

        ValueTask IRedisSetAsync.AddAsync(string item, CancellationToken token)
            => AsyncClient.AddItemToSetAsync(setId, item, token);

        ValueTask IRedisSetAsync.ClearAsync(CancellationToken token)
            => new ValueTask(AsyncClient.RemoveAsync(setId, token));

        ValueTask<bool> IRedisSetAsync.ContainsAsync(string item, CancellationToken token)
            => AsyncClient.SetContainsItemAsync(setId, item, token);

        ValueTask<int> IRedisSetAsync.CountAsync(CancellationToken token)
            => AsyncClient.GetSetCountAsync(setId, token).AsInt32();

        ValueTask<HashSet<string>> IRedisSetAsync.DiffAsync(IRedisSetAsync[] withSets, CancellationToken token)
        {
            var withSetIds = withSets.ToList().ConvertAll(x => x.Id).ToArray();
            return AsyncClient.GetDifferencesFromSetAsync(setId, withSetIds, token);
        }

        ValueTask<HashSet<string>> IRedisSetAsync.GetAllAsync(CancellationToken token)
            => AsyncClient.GetAllItemsFromSetAsync(setId, token);

        IAsyncEnumerator<string> IAsyncEnumerable<string>.GetAsyncEnumerator(CancellationToken token)
            => AsyncClient.ScanAllSetItemsAsync(setId, token: token).GetAsyncEnumerator(token); // uses SSCAN

        ValueTask<string> IRedisSetAsync.GetRandomEntryAsync(CancellationToken token)
            => AsyncClient.GetRandomItemFromSetAsync(setId, token);

        ValueTask<List<string>> IRedisSetAsync.GetRangeFromSortedSetAsync(int startingFrom, int endingAt, CancellationToken token)
            => AsyncClient.GetSortedEntryValuesAsync(setId, startingFrom, endingAt, token);

        ValueTask<HashSet<string>> IRedisSetAsync.IntersectAsync(IRedisSetAsync[] withSets, CancellationToken token)
        {
            var allSetIds = MergeSetIds(withSets);
            return AsyncClient.GetIntersectFromSetsAsync(allSetIds.ToArray(), token);
        }

        ValueTask<HashSet<string>> IRedisSetAsync.IntersectAsync(params IRedisSetAsync[] withSets)
            => AsAsync().IntersectAsync(withSets, token: default);

        private List<string> MergeSetIds(IRedisSetAsync[] withSets)
        {
            var allSetIds = new List<string> { setId };
            allSetIds.AddRange(withSets.ToList().ConvertAll(x => x.Id));
            return allSetIds;
        }

        ValueTask IRedisSetAsync.MoveAsync(string value, IRedisSetAsync toSet, CancellationToken token)
            => AsyncClient.MoveBetweenSetsAsync(setId, toSet.Id, value, token);

        ValueTask<string> IRedisSetAsync.PopAsync(CancellationToken token)
            => AsyncClient.PopItemFromSetAsync(setId, token);

        ValueTask<bool> IRedisSetAsync.RemoveAsync(string item, CancellationToken token)
            => AsyncClient.RemoveItemFromSetAsync(setId, item, token).AwaitAsTrue(); // see Remove for why true

        ValueTask IRedisSetAsync.StoreDiffAsync(IRedisSetAsync fromSet, IRedisSetAsync[] withSets, CancellationToken token)
        {
            var withSetIds = withSets.ToList().ConvertAll(x => x.Id).ToArray();
            return AsyncClient.StoreDifferencesFromSetAsync(setId, fromSet.Id, withSetIds, token);
        }

        ValueTask IRedisSetAsync.StoreDiffAsync(IRedisSetAsync fromSet, params IRedisSetAsync[] withSets)
            => AsAsync().StoreDiffAsync(fromSet, withSets, token: default);

        ValueTask IRedisSetAsync.StoreIntersectAsync(IRedisSetAsync[] withSets, CancellationToken token)
        {
            var withSetIds = withSets.ToList().ConvertAll(x => x.Id).ToArray();
            return AsyncClient.StoreIntersectFromSetsAsync(setId, withSetIds, token);
        }

        ValueTask IRedisSetAsync.StoreIntersectAsync(params IRedisSetAsync[] withSets)
            => AsAsync().StoreIntersectAsync(withSets, token: default);

        ValueTask IRedisSetAsync.StoreUnionAsync(IRedisSetAsync[] withSets, CancellationToken token)
        {
            var withSetIds = withSets.ToList().ConvertAll(x => x.Id).ToArray();
            return AsyncClient.StoreUnionFromSetsAsync(setId, withSetIds, token);
        }

        ValueTask IRedisSetAsync.StoreUnionAsync(params IRedisSetAsync[] withSets)
            => AsAsync().StoreUnionAsync(withSets, token: default);

        ValueTask<HashSet<string>> IRedisSetAsync.UnionAsync(IRedisSetAsync[] withSets, CancellationToken token)
        {
            var allSetIds = MergeSetIds(withSets);
            return AsyncClient.GetUnionFromSetsAsync(allSetIds.ToArray(), token);
        }

        ValueTask<HashSet<string>> IRedisSetAsync.UnionAsync(params IRedisSetAsync[] withSets)
            => AsAsync().UnionAsync(withSets, token: default);
    }
}