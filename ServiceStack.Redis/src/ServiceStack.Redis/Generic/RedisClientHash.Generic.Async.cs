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
    internal partial class RedisClientHash<TKey, T>
        : IRedisHashAsync<TKey, T>
    {
        IRedisTypedClientAsync<T> AsyncClient => client;

        ValueTask IRedisHashAsync<TKey, T>.AddAsync(KeyValuePair<TKey, T> item, CancellationToken token)
            => AsyncClient.SetEntryInHashAsync(this, item.Key, item.Value, token).Await();

        ValueTask IRedisHashAsync<TKey, T>.AddAsync(TKey key, T value, CancellationToken token)
            => AsyncClient.SetEntryInHashAsync(this, key, value, token).Await();

        ValueTask IRedisHashAsync<TKey, T>.ClearAsync(CancellationToken token)
            => AsyncClient.RemoveEntryAsync(new[] { this }, token).Await();

        ValueTask<bool> IRedisHashAsync<TKey, T>.ContainsKeyAsync(TKey key, CancellationToken token)
            => AsyncClient.HashContainsEntryAsync(this, key, token);

        ValueTask<int> IRedisHashAsync<TKey, T>.CountAsync(CancellationToken token)
            => AsyncClient.GetHashCountAsync(this, token).AsInt32();

        ValueTask<Dictionary<TKey, T>> IRedisHashAsync<TKey, T>.GetAllAsync(CancellationToken token)
            => AsyncClient.GetAllEntriesFromHashAsync(this, token);

        async IAsyncEnumerator<KeyValuePair<TKey, T>> IAsyncEnumerable<KeyValuePair<TKey, T>>.GetAsyncEnumerator(CancellationToken token)
        {
            var all = await AsyncClient.GetAllEntriesFromHashAsync(this, token).ConfigureAwait(false);
            foreach (var pair in all)
            {
                yield return pair;
            }
        }

        ValueTask<bool> IRedisHashAsync<TKey, T>.RemoveAsync(TKey key, CancellationToken token)
            => AsyncClient.RemoveEntryFromHashAsync(this, key, token);
    }
}