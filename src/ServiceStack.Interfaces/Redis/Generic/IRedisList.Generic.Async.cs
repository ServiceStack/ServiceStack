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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Model;

namespace ServiceStack.Redis.Generic
{
    /// <summary>
    /// Wrap the common redis list operations under a IList[string] interface.
    /// </summary>

    public interface IRedisListAsync<T>
        : IAsyncEnumerable<T>, IHasStringId
    {
        ValueTask<int> CountAsync(CancellationToken token = default);
        ValueTask<List<T>> GetAllAsync(CancellationToken token = default);
        ValueTask<List<T>> GetRangeAsync(int startingFrom, int endingAt, CancellationToken token = default);
        ValueTask<List<T>> GetRangeFromSortedListAsync(int startingFrom, int endingAt, CancellationToken token = default);
        ValueTask RemoveAllAsync(CancellationToken token = default);
        ValueTask TrimAsync(int keepStartingFrom, int keepEndingAt, CancellationToken token = default);
        ValueTask<long> RemoveValueAsync(T value, CancellationToken token = default);
        ValueTask<long> RemoveValueAsync(T value, int noOfMatches, CancellationToken token = default);

        ValueTask AddRangeAsync(IEnumerable<T> values, CancellationToken token = default);
        ValueTask AppendAsync(T value, CancellationToken token = default);
        ValueTask PrependAsync(T value, CancellationToken token = default);
        ValueTask<T> RemoveStartAsync(CancellationToken token = default);
        ValueTask<T> BlockingRemoveStartAsync(TimeSpan? timeOut, CancellationToken token = default);
        ValueTask<T> RemoveEndAsync(CancellationToken token = default);

        ValueTask EnqueueAsync(T value, CancellationToken token = default);
        ValueTask<T> DequeueAsync(CancellationToken token = default);
        ValueTask<T> BlockingDequeueAsync(TimeSpan? timeOut, CancellationToken token = default);

        ValueTask PushAsync(T value, CancellationToken token = default);
        ValueTask<T> PopAsync(CancellationToken token = default);
        ValueTask<T> BlockingPopAsync(TimeSpan? timeOut, CancellationToken token = default);
        ValueTask<T> PopAndPushAsync(IRedisListAsync<T> toList, CancellationToken token = default);


        ValueTask<bool> RemoveAsync(T item, CancellationToken token = default);
        ValueTask AddAsync(T item, CancellationToken token = default);
        ValueTask RemoveAtAsync(int index, CancellationToken token = default);
        ValueTask<bool> ContainsAsync(T item, CancellationToken token = default);
        ValueTask ClearAsync(CancellationToken token = default);
        ValueTask<int> IndexOfAsync(T item, CancellationToken token = default);

        ValueTask<T> ElementAtAsync(int index, CancellationToken token = default);
        ValueTask SetValueAsync(int index, T value, CancellationToken token = default);
    }
}