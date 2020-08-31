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
        ValueTask<int> CountAsync(CancellationToken cancellationToken = default);
        ValueTask<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
        ValueTask<List<T>> GetRangeAsync(int startingFrom, int endingAt, CancellationToken cancellationToken = default);
        ValueTask<List<T>> GetRangeFromSortedListAsync(int startingFrom, int endingAt, CancellationToken cancellationToken = default);
        ValueTask RemoveAllAsync(CancellationToken cancellationToken = default);
        ValueTask TrimAsync(int keepStartingFrom, int keepEndingAt, CancellationToken cancellationToken = default);
        ValueTask<long> RemoveValueAsync(T value, CancellationToken cancellationToken = default);
        ValueTask<long> RemoveValueAsync(T value, int noOfMatches, CancellationToken cancellationToken = default);

        ValueTask AddRangeAsync(IEnumerable<T> values, CancellationToken cancellationToken = default);
        ValueTask AppendAsync(T value, CancellationToken cancellationToken = default);
        ValueTask PrependAsync(T value, CancellationToken cancellationToken = default);
        ValueTask<T> RemoveStartAsync(CancellationToken cancellationToken = default);
        ValueTask<T> BlockingRemoveStartAsync(TimeSpan? timeOut, CancellationToken cancellationToken = default);
        ValueTask<T> RemoveEndAsync(CancellationToken cancellationToken = default);

        ValueTask EnqueueAsync(T value, CancellationToken cancellationToken = default);
        ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default);
        ValueTask<T> BlockingDequeueAsync(TimeSpan? timeOut, CancellationToken cancellationToken = default);

        ValueTask PushAsync(T value, CancellationToken cancellationToken = default);
        ValueTask<T> PopAsync(CancellationToken cancellationToken = default);
        ValueTask<T> BlockingPopAsync(TimeSpan? timeOut, CancellationToken cancellationToken = default);
        ValueTask<T> PopAndPushAsync(IRedisListAsync<T> toList, CancellationToken cancellationToken = default);


        ValueTask<bool> RemoveAsync(T item, CancellationToken cancellationToken = default);
        ValueTask AddAsync(T item, CancellationToken cancellationToken = default);
        ValueTask RemoveAtAsync(int index, CancellationToken cancellationToken = default);
        ValueTask<bool> ContainsAsync(T item, CancellationToken cancellationToken = default);
        ValueTask ClearAsync(CancellationToken cancellationToken = default);
        ValueTask<int> IndexOfAsync(T item, CancellationToken cancellationToken = default);

        ValueTask<T> ElementAtAsync(int index, CancellationToken cancellationToken = default);
        ValueTask SetValueAsync(int index, T value, CancellationToken cancellationToken = default);
    }
}