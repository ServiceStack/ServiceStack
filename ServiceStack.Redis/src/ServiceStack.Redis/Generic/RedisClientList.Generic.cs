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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Redis.Generic
{
    internal partial class RedisClientList<T>
        : IRedisList<T>
    {
        private readonly RedisTypedClient<T> client;
        private readonly string listId;
        private const int PageLimit = 1000;

        public RedisClientList(RedisTypedClient<T> client, string listId)
        {
            this.listId = listId;
            this.client = client;
        }

        public string Id
        {
            get { return listId; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.Count <= PageLimit
                    ? client.GetAllItemsFromList(this).GetEnumerator()
                    : GetPagingEnumerator();
        }

        public IEnumerator<T> GetPagingEnumerator()
        {
            var skip = 0;
            List<T> pageResults;
            do
            {
                pageResults = client.GetRangeFromList(this, skip, PageLimit);
                foreach (var result in pageResults)
                {
                    yield return result;
                }
                skip += PageLimit;
            } while (pageResults.Count == PageLimit);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            client.AddItemToList(this, item);
        }

        public void Clear()
        {
            client.RemoveAllFromList(this);
        }

        public bool Contains(T item)
        {
            //TODO: replace with native implementation when exists
            foreach (var existingItem in this)
            {
                if (Equals(existingItem, item)) return true;
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            var allItemsInList = client.GetAllItemsFromList(this);
            allItemsInList.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            var index = this.IndexOf(item);
            if (index != -1)
            {
                this.RemoveAt(index);
                return true;
            }
            return false;
        }

        public int Count
        {
            get
            {
                return (int)client.GetListCount(this);
            }
        }

        public bool IsReadOnly { get { return false; } }

        public int IndexOf(T item)
        {
            //TODO: replace with native implementation when exists
            var i = 0;
            foreach (var existingItem in this)
            {
                if (Equals(existingItem, item)) return i;
                i++;
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            client.InsertAfterItemInList(this, this[index], item);
        }

        public void RemoveAt(int index)
        {
            //TODO: replace with native implementation when one exists
            var markForDelete = Guid.NewGuid().ToString();
            client.NativeClient.LSet(listId, index, Encoding.UTF8.GetBytes(markForDelete));

            const int removeAll = 0;
            client.NativeClient.LRem(listId, removeAll, Encoding.UTF8.GetBytes(markForDelete));
        }

        public T this[int index]
        {
            get { return client.GetItemFromList(this, index); }
            set { client.SetItemInList(this, index, value); }
        }

        public List<T> GetAll()
        {
            return client.GetAllItemsFromList(this);
        }

        public List<T> GetRange(int startingFrom, int endingAt)
        {
            return client.GetRangeFromList(this, startingFrom, endingAt);
        }

        public List<T> GetRangeFromSortedList(int startingFrom, int endingAt)
        {
            return client.SortList(this, startingFrom, endingAt);
        }

        public void RemoveAll()
        {
            client.RemoveAllFromList(this);
        }

        public void Trim(int keepStartingFrom, int keepEndingAt)
        {
            client.TrimList(this, keepStartingFrom, keepEndingAt);
        }

        public long RemoveValue(T value)
        {
            return client.RemoveItemFromList(this, value);
        }

        public long RemoveValue(T value, int noOfMatches)
        {
            return client.RemoveItemFromList(this, value, noOfMatches);
        }

        public void AddRange(IEnumerable<T> values)
        {
            client.AddRangeToList(this, values);
        }

        public void Append(T value)
        {
            Add(value);
        }

        public void Prepend(T value)
        {
            client.PrependItemToList(this, value);
        }

        public T RemoveStart()
        {
            return client.RemoveStartFromList(this);
        }

        public T BlockingRemoveStart(TimeSpan? timeOut)
        {
            return client.BlockingRemoveStartFromList(this, timeOut);
        }

        public T RemoveEnd()
        {
            return client.RemoveEndFromList(this);
        }

        public void Enqueue(T value)
        {
            client.EnqueueItemOnList(this, value);
        }

        public T Dequeue()
        {
            return client.DequeueItemFromList(this);
        }

        public T BlockingDequeue(TimeSpan? timeOut)
        {
            return client.BlockingDequeueItemFromList(this, timeOut);
        }

        public void Push(T value)
        {
            client.PushItemToList(this, value);
        }

        public T Pop()
        {
            return client.PopItemFromList(this);
        }

        public T BlockingPop(TimeSpan? timeOut)
        {
            return client.BlockingPopItemFromList(this, timeOut);
        }

        public T PopAndPush(IRedisList<T> toList)
        {
            return client.PopAndPushItemBetweenLists(this, toList);
        }

        public T BlockingPopAndPush(IRedisList<T> toList, TimeSpan? timeOut)
        {
            return client.BlockingPopAndPushItemBetweenLists(this, toList, timeOut);
        }
    }
}