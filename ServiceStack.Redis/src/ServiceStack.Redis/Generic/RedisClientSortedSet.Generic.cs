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
    internal partial class RedisClientSortedSet<T>
        : IRedisSortedSet<T>
    {
        private readonly RedisTypedClient<T> client;
        private readonly string setId;
        private const int PageLimit = 1000;

        public RedisClientSortedSet(RedisTypedClient<T> client, string setId)
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
                    ? client.GetAllItemsFromSortedSet(this).GetEnumerator()
                    : GetPagingEnumerator();
        }

        public IEnumerator<T> GetPagingEnumerator()
        {
            var skip = 0;
            List<T> pageResults;
            do
            {
                pageResults = client.GetRangeFromSortedSet(this, skip, skip + PageLimit - 1);
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
            client.AddItemToSortedSet(this, item);
        }

        public void Add(T item, double score)
        {
            client.AddItemToSortedSet(this, item, score);
        }

        public void Clear()
        {
            client.RemoveEntry(setId);
        }

        public bool Contains(T item)
        {
            return client.SortedSetContainsItem(this, item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            var allItemsInSet = client.GetAllItemsFromSortedSet(this);
            allItemsInSet.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            client.RemoveItemFromSortedSet(this, item);
            return true;
        }

        public int Count
        {
            get
            {
                var setCount = (int)client.GetSortedSetCount(this);
                return setCount;
            }
        }

        public bool IsReadOnly { get { return false; } }

        public T PopItemWithHighestScore()
        {
            return client.PopItemWithHighestScoreFromSortedSet(this);
        }

        public T PopItemWithLowestScore()
        {
            return client.PopItemWithLowestScoreFromSortedSet(this);
        }

        public double IncrementItem(T item, double incrementBy)
        {
            return client.IncrementItemInSortedSet(this, item, incrementBy);
        }

        public int IndexOf(T item)
        {
            return (int)client.GetItemIndexInSortedSet(this, item);
        }

        public long IndexOfDescending(T item)
        {
            return client.GetItemIndexInSortedSetDesc(this, item);
        }

        public List<T> GetAll()
        {
            return client.GetAllItemsFromSortedSet(this);
        }

        public List<T> GetAllDescending()
        {
            return client.GetAllItemsFromSortedSetDesc(this);
        }

        public List<T> GetRange(int fromRank, int toRank)
        {
            return client.GetRangeFromSortedSet(this, fromRank, toRank);
        }

        public List<T> GetRangeByLowestScore(double fromScore, double toScore)
        {
            return client.GetRangeFromSortedSetByLowestScore(this, fromScore, toScore);
        }

        public List<T> GetRangeByLowestScore(double fromScore, double toScore, int? skip, int? take)
        {
            return client.GetRangeFromSortedSetByLowestScore(this, fromScore, toScore, skip, take);
        }

        public List<T> GetRangeByHighestScore(double fromScore, double toScore)
        {
            return client.GetRangeFromSortedSetByHighestScore(this, fromScore, toScore);
        }

        public List<T> GetRangeByHighestScore(double fromScore, double toScore, int? skip, int? take)
        {
            return client.GetRangeFromSortedSetByHighestScore(this, fromScore, toScore, skip, take);
        }

        public long RemoveRange(int minRank, int maxRank)
        {
            return client.RemoveRangeFromSortedSet(this, minRank, maxRank);
        }

        public long RemoveRangeByScore(double fromScore, double toScore)
        {
            return client.RemoveRangeFromSortedSetByScore(this, fromScore, toScore);
        }

        public double GetItemScore(T item)
        {
            return client.GetItemScoreInSortedSet(this, item);
        }

        public long PopulateWithIntersectOf(params IRedisSortedSet<T>[] setIds)
        {
            return client.StoreIntersectFromSortedSets(this, setIds);
        }

        public long PopulateWithIntersectOf(IRedisSortedSet<T>[] setIds, string[] args)
        {
            return client.StoreIntersectFromSortedSets(this, setIds, args);
        }

        public long PopulateWithUnionOf(params IRedisSortedSet<T>[] setIds)
        {
            return client.StoreUnionFromSortedSets(this, setIds);
        }

        public long PopulateWithUnionOf(IRedisSortedSet<T>[] setIds, string[] args)
        {
            return client.StoreUnionFromSortedSets(this, setIds, args);
        }
    }
}