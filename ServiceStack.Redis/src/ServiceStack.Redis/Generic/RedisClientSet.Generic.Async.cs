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
    internal partial class RedisClientSet<T>
        : IRedisSetAsync<T>
    {
        IRedisTypedClientAsync<T> AsyncClient => client;

        ValueTask IRedisSetAsync<T>.AddAsync(T value, CancellationToken token)
            => AsyncClient.AddItemToSetAsync(this, value, token);

        IRedisSetAsync<T> AsAsync() => this;

        ValueTask IRedisSetAsync<T>.ClearAsync(CancellationToken token)
            => AsyncClient.RemoveEntryAsync(setId, token).Await();

        ValueTask<bool> IRedisSetAsync<T>.ContainsAsync(T item, CancellationToken token)
            => AsyncClient.SetContainsItemAsync(this, item, token);

        ValueTask<int> IRedisSetAsync<T>.CountAsync(CancellationToken token)
            => AsyncClient.GetSetCountAsync(this, token).AsInt32();

        ValueTask<HashSet<T>> IRedisSetAsync<T>.GetAllAsync(CancellationToken token)
            => AsyncClient.GetAllItemsFromSetAsync(this, token);

        async IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken token)
        {
            var count = await AsAsync().CountAsync(token).ConfigureAwait(false);
            if (count <= PageLimit)
            {
                var all = await AsyncClient.GetAllItemsFromSetAsync(this, token).ConfigureAwait(false);
                foreach (var item in all)
                {
                    yield return item;
                }
            }
            else
            {
                // from GetPagingEnumerator
                var skip = 0;
                List<T> pageResults;
                do
                {
                    pageResults = await AsyncClient.GetSortedEntryValuesAsync(this, skip, skip + PageLimit - 1, token).ConfigureAwait(false);
                    foreach (var result in pageResults)
                    {
                        yield return result;
                    }
                    skip += PageLimit;
                } while (pageResults.Count == PageLimit);
            }
        }

        ValueTask IRedisSetAsync<T>.GetDifferencesAsync(IRedisSetAsync<T>[] withSets, CancellationToken token)
            => AsyncClient.StoreUnionFromSetsAsync(this, withSets, token);

        ValueTask IRedisSetAsync<T>.GetDifferencesAsync(params IRedisSetAsync<T>[] withSets)
            => AsAsync().GetDifferencesAsync(withSets, token: default);

        ValueTask<T> IRedisSetAsync<T>.GetRandomItemAsync(CancellationToken token)
            => AsyncClient.GetRandomItemFromSetAsync(this, token);

        ValueTask IRedisSetAsync<T>.MoveToAsync(T item, IRedisSetAsync<T> toSet, CancellationToken token)
            => AsyncClient.MoveBetweenSetsAsync(this, toSet, item, token);

        ValueTask<T> IRedisSetAsync<T>.PopRandomItemAsync(CancellationToken token)
            => AsyncClient.PopItemFromSetAsync(this, token);

        ValueTask IRedisSetAsync<T>.PopulateWithDifferencesOfAsync(IRedisSetAsync<T> fromSet, IRedisSetAsync<T>[] withSets, CancellationToken token)
            => AsyncClient.StoreDifferencesFromSetAsync(this, fromSet, withSets, token);

        ValueTask IRedisSetAsync<T>.PopulateWithDifferencesOfAsync(IRedisSetAsync<T> fromSet, params IRedisSetAsync<T>[] withSets)
            => AsAsync().PopulateWithDifferencesOfAsync(fromSet, withSets, token: default);

        ValueTask IRedisSetAsync<T>.PopulateWithIntersectOfAsync(IRedisSetAsync<T>[] sets, CancellationToken token)
            => AsyncClient.StoreIntersectFromSetsAsync(this, sets, token);

        ValueTask IRedisSetAsync<T>.PopulateWithIntersectOfAsync(params IRedisSetAsync<T>[] sets)
            => AsAsync().PopulateWithIntersectOfAsync(sets, token: default);

        ValueTask IRedisSetAsync<T>.PopulateWithUnionOfAsync(IRedisSetAsync<T>[] sets, CancellationToken token)
            => AsyncClient.StoreUnionFromSetsAsync(this, sets, token);

        ValueTask IRedisSetAsync<T>.PopulateWithUnionOfAsync(params IRedisSetAsync<T>[] sets)
            => AsAsync().PopulateWithUnionOfAsync(sets, token: default);

        ValueTask<bool> IRedisSetAsync<T>.RemoveAsync(T value, CancellationToken token)
            => AsyncClient.RemoveItemFromSetAsync(this, value, token).AwaitAsTrue(); // see Remove for why "true"

        ValueTask<List<T>> IRedisSetAsync<T>.SortAsync(int startingFrom, int endingAt, CancellationToken token)
            => AsyncClient.GetSortedEntryValuesAsync(this, startingFrom, endingAt, token);
    }
}