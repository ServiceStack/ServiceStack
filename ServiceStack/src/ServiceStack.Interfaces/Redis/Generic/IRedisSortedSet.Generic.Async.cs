//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot Async(demis.bellot@gmail.com, CancellationToken token = default)
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
        ValueTask<int> CountAsync(CancellationToken token = default);
        ValueTask AddAsync(T item, double score, CancellationToken token = default);
        ValueTask<T> PopItemWithHighestScoreAsync(CancellationToken token = default);
        ValueTask<T> PopItemWithLowestScoreAsync(CancellationToken token = default);
        ValueTask<double> IncrementItemAsync(T item, double incrementBy, CancellationToken token = default);
        ValueTask<int> IndexOfAsync(T item, CancellationToken token = default);
        ValueTask<long> IndexOfDescendingAsync(T item, CancellationToken token = default);
        ValueTask<List<T>> GetAllAsync(CancellationToken token = default);
        ValueTask<List<T>> GetAllDescendingAsync(CancellationToken token = default);
        ValueTask<List<T>> GetRangeAsync(int fromRank, int toRank, CancellationToken token = default);
        ValueTask<List<T>> GetRangeByLowestScoreAsync(double fromScore, double toScore, CancellationToken token = default);
        ValueTask<List<T>> GetRangeByLowestScoreAsync(double fromScore, double toScore, int? skip, int? take, CancellationToken token = default);
        ValueTask<List<T>> GetRangeByHighestScoreAsync(double fromScore, double toScore, CancellationToken token = default);
        ValueTask<List<T>> GetRangeByHighestScoreAsync(double fromScore, double toScore, int? skip, int? take, CancellationToken token = default);
        ValueTask<long> RemoveRangeAsync(int minRank, int maxRank, CancellationToken token = default);
        ValueTask<long> RemoveRangeByScoreAsync(double fromScore, double toScore, CancellationToken token = default);
        ValueTask<double> GetItemScoreAsync(T item, CancellationToken token = default);
        ValueTask<long> PopulateWithIntersectOfAsync(IRedisSortedSetAsync<T>[] setIds, CancellationToken token = default);
        ValueTask<long> PopulateWithIntersectOfAsync(params IRedisSortedSetAsync<T>[] setIds); // convenience API
        ValueTask<long> PopulateWithIntersectOfAsync(IRedisSortedSetAsync<T>[] setIds, string[] args, CancellationToken token = default);
        ValueTask<long> PopulateWithUnionOfAsync(IRedisSortedSetAsync<T>[] setIds, CancellationToken token = default);
        ValueTask<long> PopulateWithUnionOfAsync(params IRedisSortedSetAsync<T>[] setIds); // convenience API
        ValueTask<long> PopulateWithUnionOfAsync(IRedisSortedSetAsync<T>[] setIds, string[] args, CancellationToken token = default);
        ValueTask ClearAsync(CancellationToken token = default);

        ValueTask<bool> ContainsAsync(T item, CancellationToken token = default);
        ValueTask AddAsync(T item, CancellationToken token = default);
        ValueTask<bool> RemoveAsync(T item, CancellationToken token = default);
    }
}