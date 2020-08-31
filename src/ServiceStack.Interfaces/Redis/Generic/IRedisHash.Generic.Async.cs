//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2017 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Model;

namespace ServiceStack.Redis.Generic
{
    public interface IRedisHashAsync<TKey, TValue> : IAsyncEnumerable<KeyValuePair<TKey, TValue>>, IHasStringId
    {
        ValueTask<Dictionary<TKey, TValue>> GetAllAsync(CancellationToken cancellationToken = default);

        ValueTask<int> CountAsync(CancellationToken cancellationToken = default);
        ValueTask AddAsync(KeyValuePair<TKey, TValue> item, CancellationToken cancellationToken = default);
        ValueTask AddAsync(TKey key, TValue value, CancellationToken cancellationToken = default);
        ValueTask ClearAsync(CancellationToken cancellationToken = default);
        ValueTask<bool> ContainsKeyAsync(TKey key, CancellationToken cancellationToken = default);
        ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default);
    }

}