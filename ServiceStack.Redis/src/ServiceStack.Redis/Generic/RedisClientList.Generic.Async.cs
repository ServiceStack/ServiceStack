//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using ServiceStack.Redis.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Generic
{
    internal partial class RedisClientList<T>
        : IRedisListAsync<T>
    {
        IRedisTypedClientAsync<T> AsyncClient => client;
        IRedisListAsync<T> AsAsync() => this;

        async ValueTask IRedisListAsync<T>.AddRangeAsync(IEnumerable<T> values, CancellationToken token)
        {
            //TODO: replace it with a pipeline implementation ala AddRangeToSet
            foreach (var value in values)
            {
                await AsyncClient.AddItemToListAsync(this, value, token).ConfigureAwait(false);
            }
        }

        ValueTask IRedisListAsync<T>.AppendAsync(T value, CancellationToken token)
            => AsyncClient.AddItemToListAsync(this, value, token);

        ValueTask<T> IRedisListAsync<T>.BlockingDequeueAsync(TimeSpan? timeOut, CancellationToken token)
            => AsyncClient.BlockingDequeueItemFromListAsync(this, timeOut, token);

        ValueTask<T> IRedisListAsync<T>.BlockingPopAsync(TimeSpan? timeOut, CancellationToken token)
            => AsyncClient.BlockingPopItemFromListAsync(this, timeOut, token);

        ValueTask<T> IRedisListAsync<T>.BlockingRemoveStartAsync(TimeSpan? timeOut, CancellationToken token)
            => AsyncClient.BlockingRemoveStartFromListAsync(this, timeOut, token);

        ValueTask<int> IRedisListAsync<T>.CountAsync(CancellationToken token)
            => AsyncClient.GetListCountAsync(this, token).AsInt32();

        ValueTask<T> IRedisListAsync<T>.DequeueAsync(CancellationToken token)
            => AsyncClient.DequeueItemFromListAsync(this, token);

        ValueTask IRedisListAsync<T>.EnqueueAsync(T value, CancellationToken token)
            => AsyncClient.EnqueueItemOnListAsync(this, value, token);

        ValueTask<List<T>> IRedisListAsync<T>.GetAllAsync(CancellationToken token)
            => AsyncClient.GetAllItemsFromListAsync(this, token);

        async IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken token)
        {
            var count = await AsAsync().CountAsync(token).ConfigureAwait(false);
            if (count <= PageLimit)
            {
                var all = await AsyncClient.GetAllItemsFromListAsync(this, token).ConfigureAwait(false);
                foreach (var item in all)
                {
                    yield return item;
                }
            }
            else
            {
                // from GetPagingEnumerator()
                var skip = 0;
                List<T> pageResults;
                do
                {
                    pageResults = await AsyncClient.GetRangeFromListAsync(this, skip, PageLimit, token).ConfigureAwait(false);
                    foreach (var result in pageResults)
                    {
                        yield return result;
                    }
                    skip += PageLimit;
                } while (pageResults.Count == PageLimit);
            }
        }

        ValueTask<List<T>> IRedisListAsync<T>.GetRangeAsync(int startingFrom, int endingAt, CancellationToken token)
            => AsyncClient.GetRangeFromListAsync(this, startingFrom, endingAt, token);

        ValueTask<List<T>> IRedisListAsync<T>.GetRangeFromSortedListAsync(int startingFrom, int endingAt, CancellationToken token)
            => AsyncClient.SortListAsync(this, startingFrom, endingAt, token);

        ValueTask<T> IRedisListAsync<T>.PopAndPushAsync(IRedisListAsync<T> toList, CancellationToken token)
            => AsyncClient.PopAndPushItemBetweenListsAsync(this, toList, token);

        ValueTask<T> IRedisListAsync<T>.PopAsync(CancellationToken token)
            => AsyncClient.PopItemFromListAsync(this, token);

        ValueTask IRedisListAsync<T>.PrependAsync(T value, CancellationToken token)
            => AsyncClient.PrependItemToListAsync(this, value, token);

        ValueTask IRedisListAsync<T>.PushAsync(T value, CancellationToken token)
            => AsyncClient.PushItemToListAsync(this, value, token);

        ValueTask IRedisListAsync<T>.RemoveAllAsync(CancellationToken token)
            => AsyncClient.RemoveAllFromListAsync(this, token);

        ValueTask<T> IRedisListAsync<T>.RemoveEndAsync(CancellationToken token)
            => AsyncClient.RemoveEndFromListAsync(this, token);

        ValueTask<T> IRedisListAsync<T>.RemoveStartAsync(CancellationToken token)
            => AsyncClient.RemoveStartFromListAsync(this, token);

        ValueTask<long> IRedisListAsync<T>.RemoveValueAsync(T value, CancellationToken token)
            => AsyncClient.RemoveItemFromListAsync(this, value, token);

        ValueTask<long> IRedisListAsync<T>.RemoveValueAsync(T value, int noOfMatches, CancellationToken token)
            => AsyncClient.RemoveItemFromListAsync(this, value, noOfMatches, token);

        ValueTask IRedisListAsync<T>.TrimAsync(int keepStartingFrom, int keepEndingAt, CancellationToken token)
            => AsyncClient.TrimListAsync(this, keepStartingFrom, keepEndingAt, token);

        async ValueTask<bool> IRedisListAsync<T>.RemoveAsync(T value, CancellationToken token)
        {
            var index = await AsAsync().IndexOfAsync(value, token).ConfigureAwait(false);
            if (index != -1)
            {
                await AsAsync().RemoveAtAsync(index, token).ConfigureAwait(false);
                return true;
            }
            return false;
        }

        ValueTask IRedisListAsync<T>.AddAsync(T value, CancellationToken token)
            => AsyncClient.AddItemToListAsync(this, value, token);

        async ValueTask IRedisListAsync<T>.RemoveAtAsync(int index, CancellationToken token)
        {
            //TODO: replace with native implementation when one exists

            var nativeClient = client.NativeClient as IRedisNativeClientAsync ?? throw new NotSupportedException(
                $"The native client ('{client.NativeClient.GetType().Name}') does not implement {nameof(IRedisNativeClientAsync)}");

            var markForDelete = Guid.NewGuid().ToString();
            await nativeClient.LSetAsync(listId, index, Encoding.UTF8.GetBytes(markForDelete), token).ConfigureAwait(false);

            const int removeAll = 0;
            await nativeClient.LRemAsync(listId, removeAll, Encoding.UTF8.GetBytes(markForDelete), token).ConfigureAwait(false);
        }

        async ValueTask<bool> IRedisListAsync<T>.ContainsAsync(T value, CancellationToken token)
        {
            //TODO: replace with native implementation when exists
            await foreach (var existingItem in this.ConfigureAwait(false).WithCancellation(token))
            {
                if (Equals(existingItem, value)) return true;
            }
            return false;
        }

        ValueTask IRedisListAsync<T>.ClearAsync(CancellationToken token)
            => AsyncClient.RemoveAllFromListAsync(this, token);

        async ValueTask<int> IRedisListAsync<T>.IndexOfAsync(T value, CancellationToken token)
        {
            //TODO: replace with native implementation when exists
            var i = 0;
            await foreach (var existingItem in this.ConfigureAwait(false).WithCancellation(token))
            {
                if (Equals(existingItem, value)) return i;
                i++;
            }
            return -1;
        }

        ValueTask<T> IRedisListAsync<T>.ElementAtAsync(int index, CancellationToken token)
            => AsyncClient.GetItemFromListAsync(this, index, token);

        ValueTask IRedisListAsync<T>.SetValueAsync(int index, T value, CancellationToken token)
            => AsyncClient.SetItemInListAsync(this, index, value, token);
    }
}