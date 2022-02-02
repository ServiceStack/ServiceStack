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
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis
{
    internal partial class RedisClientList
        : IRedisListAsync
    {
        private IRedisClientAsync AsyncClient => client;
        private IRedisListAsync AsAsync() => this;

        ValueTask IRedisListAsync.AppendAsync(string value, CancellationToken token)
            => AsyncClient.AddItemToListAsync(listId, value, token);

        ValueTask<string> IRedisListAsync.BlockingDequeueAsync(TimeSpan? timeOut, CancellationToken token)
            => AsyncClient.BlockingDequeueItemFromListAsync(listId, timeOut, token);

        ValueTask<string> IRedisListAsync.BlockingPopAsync(TimeSpan? timeOut, CancellationToken token)
            => AsyncClient.BlockingPopItemFromListAsync(listId, timeOut, token);

        ValueTask<string> IRedisListAsync.BlockingRemoveStartAsync(TimeSpan? timeOut, CancellationToken token)
            => AsyncClient.BlockingRemoveStartFromListAsync(listId, timeOut, token);

        ValueTask<int> IRedisListAsync.CountAsync(CancellationToken token)
            => AsyncClient.GetListCountAsync(listId, token).AsInt32();

        ValueTask<string> IRedisListAsync.DequeueAsync(CancellationToken token)
            => AsyncClient.DequeueItemFromListAsync(listId, token);

        ValueTask IRedisListAsync.EnqueueAsync(string value, CancellationToken token)
            => AsyncClient.EnqueueItemOnListAsync(listId, value, token);

        ValueTask<List<string>> IRedisListAsync.GetAllAsync(CancellationToken token)
            => AsyncClient.GetAllItemsFromListAsync(listId, token);


        async IAsyncEnumerator<string> IAsyncEnumerable<string>.GetAsyncEnumerator(CancellationToken token)
        {
            var count = await AsAsync().CountAsync(token).ConfigureAwait(false);
            if (count <= PageLimit)
            {
                var all = await AsyncClient.GetAllItemsFromListAsync(listId, token).ConfigureAwait(false);
                foreach (var item in all)
                {
                    yield return item;
                }
            }
            else
            {
                // from GetPagingEnumerator()
                var skip = 0;
                List<string> pageResults;
                do
                {
                    pageResults = await AsyncClient.GetRangeFromListAsync(listId, skip, skip + PageLimit - 1, token).ConfigureAwait(false);
                    foreach (var result in pageResults)
                    {
                        yield return result;
                    }
                    skip += PageLimit;
                } while (pageResults.Count == PageLimit);
            }
        }

        ValueTask<List<string>> IRedisListAsync.GetRangeAsync(int startingFrom, int endingAt, CancellationToken token)
            => AsyncClient.GetRangeFromListAsync(listId, startingFrom, endingAt, token);

        ValueTask<List<string>> IRedisListAsync.GetRangeFromSortedListAsync(int startingFrom, int endingAt, CancellationToken token)
            => AsyncClient.GetRangeFromSortedListAsync(listId, startingFrom, endingAt, token);

        ValueTask<string> IRedisListAsync.PopAndPushAsync(IRedisListAsync toList, CancellationToken token)
            => AsyncClient.PopAndPushItemBetweenListsAsync(listId, toList.Id, token);

        ValueTask<string> IRedisListAsync.PopAsync(CancellationToken token)
            => AsyncClient.PopItemFromListAsync(listId, token);

        ValueTask IRedisListAsync.PrependAsync(string value, CancellationToken token)
            => AsyncClient.PrependItemToListAsync(listId, value, token);

        ValueTask IRedisListAsync.PushAsync(string value, CancellationToken token)
            => AsyncClient.PushItemToListAsync(listId, value, token);

        ValueTask IRedisListAsync.RemoveAllAsync(CancellationToken token)
            => AsyncClient.RemoveAllFromListAsync(listId, token);

        ValueTask<string> IRedisListAsync.RemoveEndAsync(CancellationToken token)
            => AsyncClient.RemoveEndFromListAsync(listId, token);

        ValueTask<string> IRedisListAsync.RemoveStartAsync(CancellationToken token)
            => AsyncClient.RemoveStartFromListAsync(listId, token);

        ValueTask<long> IRedisListAsync.RemoveValueAsync(string value, CancellationToken token)
            => AsyncClient.RemoveItemFromListAsync(listId, value, token);

        ValueTask<long> IRedisListAsync.RemoveValueAsync(string value, int noOfMatches, CancellationToken token)
            => AsyncClient.RemoveItemFromListAsync(listId, value, noOfMatches, token);

        ValueTask IRedisListAsync.TrimAsync(int keepStartingFrom, int keepEndingAt, CancellationToken token)
            => AsyncClient.TrimListAsync(listId, keepStartingFrom, keepEndingAt, token);

        async ValueTask<bool> IRedisListAsync.RemoveAsync(string value, CancellationToken token)
            => (await AsyncClient.RemoveItemFromListAsync(listId, value, token).ConfigureAwait(false)) > 0;

        ValueTask IRedisListAsync.AddAsync(string value, CancellationToken token)
            => AsyncClient.AddItemToListAsync(listId, value, token);

        async ValueTask IRedisListAsync.RemoveAtAsync(int index, CancellationToken token)
        {
            //TODO: replace with native implementation when one exists
            var markForDelete = Guid.NewGuid().ToString();
            await AsyncClient.SetItemInListAsync(listId, index, markForDelete, token).ConfigureAwait(false);
            await AsyncClient.RemoveItemFromListAsync(listId, markForDelete, token).ConfigureAwait(false);
        }

        async ValueTask<bool> IRedisListAsync.ContainsAsync(string value, CancellationToken token)
        {
            //TODO: replace with native implementation when exists
            await foreach (var existingItem in this.ConfigureAwait(false).WithCancellation(token))
            {
                if (existingItem == value) return true;
            }
            return false;
        }

        ValueTask IRedisListAsync.ClearAsync(CancellationToken token)
            => AsyncClient.RemoveAllFromListAsync(listId, token);

        async ValueTask<int> IRedisListAsync.IndexOfAsync(string value, CancellationToken token)
        {
            //TODO: replace with native implementation when exists
            var i = 0;
            await foreach (var existingItem in this.ConfigureAwait(false).WithCancellation(token))
            {
                if (existingItem == value) return i;
                i++;
            }
            return -1;
        }

        ValueTask<string> IRedisListAsync.ElementAtAsync(int index, CancellationToken token)
            => AsyncClient.GetItemFromListAsync(listId, index, token);

        ValueTask IRedisListAsync.SetValueAsync(int index, string value, CancellationToken token)
            => AsyncClient.SetItemInListAsync(listId, index, value, token);
    }
}