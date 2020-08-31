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
    public interface IRedisSortedSetAsync<T> : IAsyncEnumerable<T>, IHasStringId
    {
        ValueTask<int> CountAsync(CancellationToken cancellationToken = default);
        ValueTask AddAsync(T item, double score, CancellationToken cancellationToken = default);
        ValueTask<T> PopItemWithHighestScoreAsync(CancellationToken cancellationToken = default);
        ValueTask<T> PopItemWithLowestScoreAsync(CancellationToken cancellationToken = default);
        ValueTask<double> IncrementItemAsync(T item, double incrementBy, CancellationToken cancellationToken = default);
        ValueTask<int> IndexOfAsync(T item, CancellationToken cancellationToken = default);
        ValueTask<long> IndexOfDescendingAsync(T item, CancellationToken cancellationToken = default);
        ValueTask<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
        ValueTask<List<T>> GetAllDescendingAsync(CancellationToken cancellationToken = default);
        ValueTask<List<T>> GetRangeAsync(int fromRank, int toRank, CancellationToken cancellationToken = default);
        ValueTask<List<T>> GetRangeByLowestScoreAsync(double fromScore, double toScore, CancellationToken cancellationToken = default);
        ValueTask<List<T>> GetRangeByLowestScoreAsync(double fromScore, double toScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<List<T>> GetRangeByHighestScoreAsync(double fromScore, double toScore, CancellationToken cancellationToken = default);
        ValueTask<List<T>> GetRangeByHighestScoreAsync(double fromScore, double toScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<long> RemoveRangeAsync(int minRank, int maxRank, CancellationToken cancellationToken = default);
        ValueTask<long> RemoveRangeByScoreAsync(double fromScore, double toScore, CancellationToken cancellationToken = default);
        ValueTask<double> GetItemScoreAsync(T item, CancellationToken cancellationToken = default);
        ValueTask<long> PopulateWithIntersectOfAsync(IRedisSortedSetAsync<T>[] setIds, CancellationToken cancellationToken = default);
        ValueTask<long> PopulateWithIntersectOfAsync(params IRedisSortedSetAsync<T>[] setIds); // convenience API
        ValueTask<long> PopulateWithIntersectOfAsync(IRedisSortedSetAsync<T>[] setIds, string[] args, CancellationToken cancellationToken = default);
        ValueTask<long> PopulateWithUnionOfAsync(IRedisSortedSetAsync<T>[] setIds, CancellationToken cancellationToken = default);
        ValueTask<long> PopulateWithUnionOfAsync(params IRedisSortedSetAsync<T>[] setIds); // convenience API
        ValueTask<long> PopulateWithUnionOfAsync(IRedisSortedSetAsync<T>[] setIds, string[] args, CancellationToken cancellationToken = default);
        ValueTask ClearAsync(CancellationToken cancellationToken = default);

        ValueTask<bool> ContainsAsync(T item, CancellationToken cancellationToken = default);
        ValueTask AddAsync(T item, CancellationToken cancellationToken = default);
        ValueTask<bool> RemoveAsync(T item, CancellationToken cancellationToken = default);
    }
}