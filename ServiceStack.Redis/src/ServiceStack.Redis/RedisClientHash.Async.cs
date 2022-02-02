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
    internal partial class RedisClientHash
        : IRedisHashAsync
    {
        private IRedisClientAsync AsyncClient => client;

        ValueTask IRedisHashAsync.AddAsync(KeyValuePair<string, string> item, CancellationToken token)
            => AsyncClient.SetEntryInHashAsync(hashId, item.Key, item.Value, token).Await();

        ValueTask IRedisHashAsync.AddAsync(string key, string value, CancellationToken token)
            => AsyncClient.SetEntryInHashAsync(hashId, key, value, token).Await();

        ValueTask<bool> IRedisHashAsync.AddIfNotExistsAsync(KeyValuePair<string, string> item, CancellationToken token)
            => AsyncClient.SetEntryInHashIfNotExistsAsync(hashId, item.Key, item.Value, token);

        ValueTask IRedisHashAsync.AddRangeAsync(IEnumerable<KeyValuePair<string, string>> items, CancellationToken token)
            => AsyncClient.SetRangeInHashAsync(hashId, items, token);

        ValueTask IRedisHashAsync.ClearAsync(CancellationToken token)
            => new ValueTask(AsyncClient.RemoveAsync(hashId, token));

        ValueTask<bool> IRedisHashAsync.ContainsKeyAsync(string key, CancellationToken token)
            => AsyncClient.HashContainsEntryAsync(hashId, key, token);

        ValueTask<int> IRedisHashAsync.CountAsync(CancellationToken token)
            => AsyncClient.GetHashCountAsync(hashId, token).AsInt32();

        IAsyncEnumerator<KeyValuePair<string, string>> IAsyncEnumerable<KeyValuePair<string, string>>.GetAsyncEnumerator(CancellationToken token)
            => AsyncClient.ScanAllHashEntriesAsync(hashId).GetAsyncEnumerator(token); // note: we're using HSCAN here, not HGETALL

        ValueTask<long> IRedisHashAsync.IncrementValueAsync(string key, int incrementBy, CancellationToken token)
            => AsyncClient.IncrementValueInHashAsync(hashId, key, incrementBy, token);

        ValueTask<bool> IRedisHashAsync.RemoveAsync(string key, CancellationToken token)
            => AsyncClient.RemoveEntryFromHashAsync(hashId, key, token);
    }
}