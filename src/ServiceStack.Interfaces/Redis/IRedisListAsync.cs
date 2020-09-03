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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Model;

namespace ServiceStack.Redis
{
    public interface IRedisListAsync
        : IAsyncEnumerable<string>, IHasStringId
    {
        ValueTask<int> CountAsync(CancellationToken token = default);
        ValueTask<List<string>> GetAllAsync(CancellationToken token = default);
        ValueTask<List<string>> GetRangeAsync(int startingFrom, int endingAt, CancellationToken token = default);
        ValueTask<List<string>> GetRangeFromSortedListAsync(int startingFrom, int endingAt, CancellationToken token = default);
        ValueTask RemoveAllAsync(CancellationToken token = default);
        ValueTask TrimAsync(int keepStartingFrom, int keepEndingAt, CancellationToken token = default);
        ValueTask<long> RemoveValueAsync(string value, CancellationToken token = default);
        ValueTask<long> RemoveValueAsync(string value, int noOfMatches, CancellationToken token = default);

        ValueTask PrependAsync(string value, CancellationToken token = default);
        ValueTask AppendAsync(string value, CancellationToken token = default);
        ValueTask<string> RemoveStartAsync(CancellationToken token = default);
        ValueTask<string> BlockingRemoveStartAsync(TimeSpan? timeOut, CancellationToken token = default);
        ValueTask<string> RemoveEndAsync(CancellationToken token = default);

        ValueTask EnqueueAsync(string value, CancellationToken token = default);
        ValueTask<string> DequeueAsync(CancellationToken token = default);
        ValueTask<string> BlockingDequeueAsync(TimeSpan? timeOut, CancellationToken token = default);

        ValueTask PushAsync(string value, CancellationToken token = default);
        ValueTask<string> PopAsync(CancellationToken token = default);
        ValueTask<string> BlockingPopAsync(TimeSpan? timeOut, CancellationToken token = default);
        ValueTask<string> PopAndPushAsync(IRedisListAsync toList, CancellationToken token = default);
        
        ValueTask<bool> RemoveAsync(string item, CancellationToken token = default);
        ValueTask AddAsync(string item, CancellationToken token = default);
        ValueTask RemoveAtAsync(int index, CancellationToken token = default);
        ValueTask<bool> ContainsAsync(string item, CancellationToken token = default);
        ValueTask ClearAsync(CancellationToken token = default);
        ValueTask<int> IndexOfAsync(string item, CancellationToken token = default);

        ValueTask<string> ElementAtAsync(int index, CancellationToken token = default);
        ValueTask SetValueAsync(int index, string value, CancellationToken token = default);
    }
}