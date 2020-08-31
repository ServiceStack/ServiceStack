//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot Async(demis.bellot@gmail.com)
//
// Copyright 2017 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Model;

namespace ServiceStack.Redis
{
    public interface IRedisSetAsync
        : IAsyncEnumerable<string>, IHasStringId
    {
        ValueTask<int> CountAsync(CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetAsync(int startingFrom, int endingAt, CancellationToken cancellationToken = default);
        ValueTask<HashSet<string>> GetAllAsync(CancellationToken cancellationToken = default);
        ValueTask<string> PopAsync(CancellationToken cancellationToken = default);
        ValueTask MoveAsync(string value, IRedisSetAsync toSet, CancellationToken cancellationToken = default);
        ValueTask<HashSet<string>> IntersectAsync(IRedisSetAsync[] withSets, CancellationToken cancellationToken = default);
        ValueTask<HashSet<string>> IntersectAsync(params IRedisSetAsync[] withSets); // convenience API
        ValueTask StoreIntersectAsync(IRedisSetAsync[] withSets, CancellationToken cancellationToken = default);
        ValueTask StoreIntersectAsync(params IRedisSetAsync[] withSets); // convenience API
        ValueTask<HashSet<string>> UnionAsync(IRedisSetAsync[] withSets, CancellationToken cancellationToken = default);
        ValueTask<HashSet<string>> UnionAsync(params IRedisSetAsync[] withSets); // convenience API
        ValueTask StoreUnionAsync(IRedisSetAsync[] withSets, CancellationToken cancellationToken = default);
        ValueTask StoreUnionAsync(params IRedisSetAsync[] withSets); // convenience API
        ValueTask<HashSet<string>> DiffAsync(IRedisSetAsync[] withSets, CancellationToken cancellationToken = default);
        ValueTask StoreDiffAsync(IRedisSetAsync fromSet, IRedisSetAsync[] withSets, CancellationToken cancellationToken = default);
        ValueTask StoreDiffAsync(IRedisSetAsync fromSet, params IRedisSetAsync[] withSets); // convenience API
        ValueTask<string> GetRandomEntryAsync(CancellationToken cancellationToken = default);


        ValueTask<bool> RemoveAsync(string item, CancellationToken cancellationToken = default);
        ValueTask AddAsync(string item, CancellationToken cancellationToken = default);
        ValueTask<bool> ContainsAsync(string item, CancellationToken cancellationToken = default);
        ValueTask ClearAsync(CancellationToken cancellationToken = default);
    }
}