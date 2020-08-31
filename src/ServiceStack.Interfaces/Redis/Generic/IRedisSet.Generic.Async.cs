//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot Async(demis.bellot@gmail.com, CancellationToken cancellationToken = default)
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
    public interface IRedisSetAsync<T> : IAsyncEnumerable<T>, IHasStringId
    {
        ValueTask<int> CountAsync(CancellationToken cancellationToken = default);
        ValueTask<List<T>> SortAsync(int startingFrom, int endingAt, CancellationToken cancellationToken = default);
        ValueTask<HashSet<T>> GetAllAsync(CancellationToken cancellationToken = default);
        ValueTask<T> PopRandomItemAsync(CancellationToken cancellationToken = default);
        ValueTask<T> GetRandomItemAsync(CancellationToken cancellationToken = default);
        ValueTask MoveToAsync(T item, IRedisSetAsync<T> toSet, CancellationToken cancellationToken = default);
        ValueTask PopulateWithIntersectOfAsync(IRedisSetAsync<T>[] sets, CancellationToken cancellationToken = default);
        ValueTask PopulateWithIntersectOfAsync(params IRedisSetAsync<T>[] sets); // convenience API
        ValueTask PopulateWithUnionOfAsync(IRedisSetAsync<T>[] sets, CancellationToken cancellationToken = default);
        ValueTask PopulateWithUnionOfAsync(params IRedisSetAsync<T>[] sets); // convenience API
        ValueTask GetDifferencesAsync(IRedisSetAsync<T>[] withSets, CancellationToken cancellationToken = default);
        ValueTask GetDifferencesAsync(params IRedisSetAsync<T>[] withSets); // convenience API
        ValueTask PopulateWithDifferencesOfAsync(IRedisSetAsync<T> fromSet, IRedisSetAsync<T>[] withSets, CancellationToken cancellationToken = default);
        ValueTask PopulateWithDifferencesOfAsync(IRedisSetAsync<T> fromSet, params IRedisSetAsync<T>[] withSets); // convenience API
        ValueTask ClearAsync(CancellationToken cancellationToken = default);
        ValueTask<bool> ContainsAsync(T item, CancellationToken cancellationToken = default);
        ValueTask<bool> RemoveAsync(T item, CancellationToken cancellationToken = default);
        ValueTask AddAsync(T item, CancellationToken cancellationToken = default);
    }

}