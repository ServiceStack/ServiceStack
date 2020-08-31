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
    public interface IRedisSortedSetAsync
        : IAsyncEnumerable<string>, IHasStringId
    {
        ValueTask<int> CountAsync(CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetAllAsync(CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeAsync(int startingRank, int endingRank, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeByScoreAsync(string fromStringScore, string toStringScore, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeByScoreAsync(string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeByScoreAsync(double fromScore, double toScore, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeByScoreAsync(double fromScore, double toScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask RemoveRangeAsync(int fromRank, int toRank, CancellationToken cancellationToken = default);
        ValueTask RemoveRangeByScoreAsync(double fromScore, double toScore, CancellationToken cancellationToken = default);
        ValueTask StoreFromIntersectAsync(IRedisSortedSetAsync[] ofSets, CancellationToken cancellationToken = default);
        ValueTask StoreFromIntersectAsync(params IRedisSortedSetAsync[] ofSets); // convenience API
        ValueTask StoreFromUnionAsync(IRedisSortedSetAsync[] ofSets, CancellationToken cancellationToken = default);
        ValueTask StoreFromUnionAsync(params IRedisSortedSetAsync[] ofSets); // convenience API
        ValueTask<long> GetItemIndexAsync(string value, CancellationToken cancellationToken = default);
        ValueTask<double> GetItemScoreAsync(string value, CancellationToken cancellationToken = default);
        ValueTask IncrementItemScoreAsync(string value, double incrementByScore, CancellationToken cancellationToken = default);
        ValueTask<string> PopItemWithHighestScoreAsync(CancellationToken cancellationToken = default);
        ValueTask<string> PopItemWithLowestScoreAsync(CancellationToken cancellationToken = default);
        ValueTask ClearAsync(CancellationToken cancellationToken = default);
        ValueTask<bool> ContainsAsync(string item, CancellationToken cancellationToken = default);
        ValueTask AddAsync(string item, CancellationToken cancellationToken = default);
        ValueTask<bool> RemoveAsync(string item, CancellationToken cancellationToken = default);
    }
}