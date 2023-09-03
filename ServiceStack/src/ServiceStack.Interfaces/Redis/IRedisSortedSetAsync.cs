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

namespace ServiceStack.Redis;

public interface IRedisSortedSetAsync
    : IAsyncEnumerable<string>, IHasStringId
{
    ValueTask<int> CountAsync(CancellationToken token = default);
    ValueTask<List<string>> GetAllAsync(CancellationToken token = default);
    ValueTask<List<string>> GetRangeAsync(int startingRank, int endingRank, CancellationToken token = default);
    ValueTask<List<string>> GetRangeByScoreAsync(string fromStringScore, string toStringScore, CancellationToken token = default);
    ValueTask<List<string>> GetRangeByScoreAsync(string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token = default);
    ValueTask<List<string>> GetRangeByScoreAsync(double fromScore, double toScore, CancellationToken token = default);
    ValueTask<List<string>> GetRangeByScoreAsync(double fromScore, double toScore, int? skip, int? take, CancellationToken token = default);
    ValueTask RemoveRangeAsync(int fromRank, int toRank, CancellationToken token = default);
    ValueTask RemoveRangeByScoreAsync(double fromScore, double toScore, CancellationToken token = default);
    ValueTask StoreFromIntersectAsync(IRedisSortedSetAsync[] ofSets, CancellationToken token = default);
    ValueTask StoreFromIntersectAsync(params IRedisSortedSetAsync[] ofSets); // convenience API
    ValueTask StoreFromUnionAsync(IRedisSortedSetAsync[] ofSets, CancellationToken token = default);
    ValueTask StoreFromUnionAsync(params IRedisSortedSetAsync[] ofSets); // convenience API
    ValueTask<long> GetItemIndexAsync(string value, CancellationToken token = default);
    ValueTask<double> GetItemScoreAsync(string value, CancellationToken token = default);
    ValueTask IncrementItemScoreAsync(string value, double incrementByScore, CancellationToken token = default);
    ValueTask<string> PopItemWithHighestScoreAsync(CancellationToken token = default);
    ValueTask<string> PopItemWithLowestScoreAsync(CancellationToken token = default);
    ValueTask ClearAsync(CancellationToken token = default);
    ValueTask<bool> ContainsAsync(string item, CancellationToken token = default);
    ValueTask AddAsync(string item, CancellationToken token = default);
    ValueTask<bool> RemoveAsync(string item, CancellationToken token = default);
}