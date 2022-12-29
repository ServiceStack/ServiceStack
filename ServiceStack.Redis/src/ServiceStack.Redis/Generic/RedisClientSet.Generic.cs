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

namespace ServiceStack.Redis.Generic
{
    /// <summary>
    /// Wrap the common redis set operations under a ICollection[string] interface.
    /// </summary>
    internal partial class RedisClientSet<T>
        : IRedisSet<T>
    {
        private readonly RedisTypedClient<T> client;
        private readonly string setId;
        private const int PageLimit = 1000;

        public RedisClientSet(RedisTypedClient<T> client, string setId)
        {
            this.client = client;
            this.setId = setId;
        }

        public string Id
        {
            get { return this.setId; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.Count <= PageLimit
                    ? client.GetAllItemsFromSet(this).GetEnumerator()
                    : GetPagingEnumerator();
        }

        public IEnumerator<T> GetPagingEnumerator()
        {
            var skip = 0;
            List<T> pageResults;
            do
            {
                pageResults = client.GetSortedEntryValues(this, skip, skip + PageLimit - 1);
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
            client.AddItemToSet(this, item);
        }

        public void Clear()
        {
            client.RemoveEntry(setId);
        }

        public bool Contains(T item)
        {
            return client.SetContainsItem(this, item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            var allItemsInSet = client.GetAllItemsFromSet(this);
            allItemsInSet.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            client.RemoveItemFromSet(this, item);
            return true;
        }

        public int Count
        {
            get
            {
                var setCount = (int)client.GetSetCount(this);
                return setCount;
            }
        }

        public bool IsReadOnly { get { return false; } }

        public List<T> Sort(int startingFrom, int endingAt)
        {
            return client.GetSortedEntryValues(this, startingFrom, endingAt);
        }

        public HashSet<T> GetAll()
        {
            return client.GetAllItemsFromSet(this);
        }

        public T PopRandomItem()
        {
            return client.PopItemFromSet(this);
        }

        public T GetRandomItem()
        {
            return client.GetRandomItemFromSet(this);
        }

        public void MoveTo(T item, IRedisSet<T> toSet)
        {
            client.MoveBetweenSets(this, toSet, item);
        }

        public void PopulateWithIntersectOf(params IRedisSet<T>[] sets)
        {
            client.StoreIntersectFromSets(this, sets);
        }

        public void PopulateWithUnionOf(params IRedisSet<T>[] sets)
        {
            client.StoreUnionFromSets(this, sets);
        }

        public void GetDifferences(params IRedisSet<T>[] withSets)
        {
            client.StoreUnionFromSets(this, withSets);
        }

        public void PopulateWithDifferencesOf(IRedisSet<T> fromSet, params IRedisSet<T>[] withSets)
        {
            client.StoreDifferencesFromSet(this, fromSet, withSets);
        }
    }
}