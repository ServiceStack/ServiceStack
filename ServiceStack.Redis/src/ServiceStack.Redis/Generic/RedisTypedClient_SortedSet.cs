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

using System.Collections.Generic;
using ServiceStack.Model;
using ServiceStack.Redis.Support;
using ServiceStack.Text;

namespace ServiceStack.Redis.Generic
{
    public partial class RedisTypedClient<T>
    {
        public IHasNamed<IRedisSortedSet<T>> SortedSets { get; set; }

        internal partial class RedisClientSortedSets
            : IHasNamed<IRedisSortedSet<T>>
        {
            private readonly RedisTypedClient<T> client;

            public RedisClientSortedSets(RedisTypedClient<T> client)
            {
                this.client = client;
            }

            public IRedisSortedSet<T> this[string setId]
            {
                get
                {
                    return new RedisClientSortedSet<T>(client, setId);
                }
                set
                {
                    var col = this[setId];
                    col.Clear();
                    col.CopyTo(value.ToArray(), 0);
                }
            }
        }

        public static T DeserializeFromString(string serializedObj)
        {
            return JsonSerializer.DeserializeFromString<T>(serializedObj);
        }

        private static IDictionary<T, double> CreateGenericMap(IDictionary<string, double> map)
        {
            var genericMap = new OrderedDictionary<T, double>();
            foreach (var entry in map)
            {
                genericMap[DeserializeFromString(entry.Key)] = entry.Value;
            }
            return genericMap;
        }

        public void AddItemToSortedSet(IRedisSortedSet<T> toSet, T value)
        {
            client.AddItemToSortedSet(toSet.Id, value.SerializeToString());
        }

        public void AddItemToSortedSet(IRedisSortedSet<T> toSet, T value, double score)
        {
            client.AddItemToSortedSet(toSet.Id, value.SerializeToString(), score);
        }

        public bool RemoveItemFromSortedSet(IRedisSortedSet<T> fromSet, T value)
        {
            return client.RemoveItemFromSortedSet(fromSet.Id, value.SerializeToString());
        }

        public T PopItemWithLowestScoreFromSortedSet(IRedisSortedSet<T> fromSet)
        {
            return DeserializeFromString(
                client.PopItemWithLowestScoreFromSortedSet(fromSet.Id));
        }

        public T PopItemWithHighestScoreFromSortedSet(IRedisSortedSet<T> fromSet)
        {
            return DeserializeFromString(
                client.PopItemWithHighestScoreFromSortedSet(fromSet.Id));
        }

        public bool SortedSetContainsItem(IRedisSortedSet<T> set, T value)
        {
            return client.SortedSetContainsItem(set.Id, value.SerializeToString());
        }

        public double IncrementItemInSortedSet(IRedisSortedSet<T> set, T value, double incrementBy)
        {
            return client.IncrementItemInSortedSet(set.Id, value.SerializeToString(), incrementBy);
        }

        public long GetItemIndexInSortedSet(IRedisSortedSet<T> set, T value)
        {
            return client.GetItemIndexInSortedSet(set.Id, value.SerializeToString());
        }

        public long GetItemIndexInSortedSetDesc(IRedisSortedSet<T> set, T value)
        {
            return client.GetItemIndexInSortedSetDesc(set.Id, value.SerializeToString());
        }

        public List<T> GetAllItemsFromSortedSet(IRedisSortedSet<T> set)
        {
            var list = client.GetAllItemsFromSortedSet(set.Id);
            return list.ConvertEachTo<T>();
        }

        public List<T> GetAllItemsFromSortedSetDesc(IRedisSortedSet<T> set)
        {
            var list = client.GetAllItemsFromSortedSetDesc(set.Id);
            return list.ConvertEachTo<T>();
        }

        public List<T> GetRangeFromSortedSet(IRedisSortedSet<T> set, int fromRank, int toRank)
        {
            var list = client.GetRangeFromSortedSet(set.Id, fromRank, toRank);
            return list.ConvertEachTo<T>();
        }

        public List<T> GetRangeFromSortedSetDesc(IRedisSortedSet<T> set, int fromRank, int toRank)
        {
            var list = client.GetRangeFromSortedSetDesc(set.Id, fromRank, toRank);
            return list.ConvertEachTo<T>();
        }

        public IDictionary<T, double> GetAllWithScoresFromSortedSet(IRedisSortedSet<T> set)
        {
            var map = client.GetRangeWithScoresFromSortedSet(set.Id, FirstElement, LastElement);
            return CreateGenericMap(map);
        }

        public IDictionary<T, double> GetRangeWithScoresFromSortedSet(IRedisSortedSet<T> set, int fromRank, int toRank)
        {
            var map = client.GetRangeWithScoresFromSortedSet(set.Id, fromRank, toRank);
            return CreateGenericMap(map);
        }

        public IDictionary<T, double> GetRangeWithScoresFromSortedSetDesc(IRedisSortedSet<T> set, int fromRank, int toRank)
        {
            var map = client.GetRangeWithScoresFromSortedSetDesc(set.Id, fromRank, toRank);
            return CreateGenericMap(map);
        }

        public List<T> GetRangeFromSortedSetByLowestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore)
        {
            var list = client.GetRangeFromSortedSetByLowestScore(set.Id, fromStringScore, toStringScore);
            return list.ConvertEachTo<T>();
        }

        public List<T> GetRangeFromSortedSetByLowestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore, int? skip, int? take)
        {
            var list = client.GetRangeFromSortedSetByLowestScore(set.Id, fromStringScore, toStringScore, skip, take);
            return list.ConvertEachTo<T>();
        }

        public List<T> GetRangeFromSortedSetByLowestScore(IRedisSortedSet<T> set, double fromScore, double toScore)
        {
            var list = client.GetRangeFromSortedSetByLowestScore(set.Id, fromScore, toScore);
            return list.ConvertEachTo<T>();
        }

        public List<T> GetRangeFromSortedSetByLowestScore(IRedisSortedSet<T> set, double fromScore, double toScore, int? skip, int? take)
        {
            var list = client.GetRangeFromSortedSetByLowestScore(set.Id, fromScore, toScore, skip, take);
            return list.ConvertEachTo<T>();
        }

        public IDictionary<T, double> GetRangeWithScoresFromSortedSetByLowestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore)
        {
            var map = client.GetRangeWithScoresFromSortedSetByLowestScore(set.Id, fromStringScore, toStringScore);
            return CreateGenericMap(map);
        }

        public IDictionary<T, double> GetRangeWithScoresFromSortedSetByLowestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore, int? skip, int? take)
        {
            var map = client.GetRangeWithScoresFromSortedSetByLowestScore(set.Id, fromStringScore, toStringScore, skip, take);
            return CreateGenericMap(map);
        }

        public IDictionary<T, double> GetRangeWithScoresFromSortedSetByLowestScore(IRedisSortedSet<T> set, double fromScore, double toScore)
        {
            var map = client.GetRangeWithScoresFromSortedSetByLowestScore(set.Id, fromScore, toScore);
            return CreateGenericMap(map);
        }

        public IDictionary<T, double> GetRangeWithScoresFromSortedSetByLowestScore(IRedisSortedSet<T> set, double fromScore, double toScore, int? skip, int? take)
        {
            var map = client.GetRangeWithScoresFromSortedSetByLowestScore(set.Id, fromScore, toScore, skip, take);
            return CreateGenericMap(map);
        }

        public List<T> GetRangeFromSortedSetByHighestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore)
        {
            var list = client.GetRangeFromSortedSetByHighestScore(set.Id, fromStringScore, toStringScore);
            return list.ConvertEachTo<T>();
        }

        public List<T> GetRangeFromSortedSetByHighestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore, int? skip, int? take)
        {
            var list = client.GetRangeFromSortedSetByHighestScore(set.Id, fromStringScore, toStringScore, skip, take);
            return list.ConvertEachTo<T>();
        }

        public List<T> GetRangeFromSortedSetByHighestScore(IRedisSortedSet<T> set, double fromScore, double toScore)
        {
            var list = client.GetRangeFromSortedSetByHighestScore(set.Id, fromScore, toScore);
            return list.ConvertEachTo<T>();
        }

        public List<T> GetRangeFromSortedSetByHighestScore(IRedisSortedSet<T> set, double fromScore, double toScore, int? skip, int? take)
        {
            var list = client.GetRangeFromSortedSetByHighestScore(set.Id, fromScore, toScore, skip, take);
            return list.ConvertEachTo<T>();
        }

        public IDictionary<T, double> GetRangeWithScoresFromSortedSetByHighestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore)
        {
            var map = client.GetRangeWithScoresFromSortedSetByHighestScore(set.Id, fromStringScore, toStringScore);
            return CreateGenericMap(map);
        }

        public IDictionary<T, double> GetRangeWithScoresFromSortedSetByHighestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore, int? skip, int? take)
        {
            var map = client.GetRangeWithScoresFromSortedSetByHighestScore(set.Id, fromStringScore, toStringScore, skip, take);
            return CreateGenericMap(map);
        }

        public IDictionary<T, double> GetRangeWithScoresFromSortedSetByHighestScore(IRedisSortedSet<T> set, double fromScore, double toScore)
        {
            var map = client.GetRangeWithScoresFromSortedSetByHighestScore(set.Id, fromScore, toScore);
            return CreateGenericMap(map);
        }

        public IDictionary<T, double> GetRangeWithScoresFromSortedSetByHighestScore(IRedisSortedSet<T> set, double fromScore, double toScore, int? skip, int? take)
        {
            var map = client.GetRangeWithScoresFromSortedSetByHighestScore(set.Id, fromScore, toScore, skip, take);
            return CreateGenericMap(map);
        }

        public long RemoveRangeFromSortedSet(IRedisSortedSet<T> set, int minRank, int maxRank)
        {
            return client.RemoveRangeFromSortedSet(set.Id, minRank, maxRank);
        }

        public long RemoveRangeFromSortedSetByScore(IRedisSortedSet<T> set, double fromScore, double toScore)
        {
            return client.RemoveRangeFromSortedSetByScore(set.Id, fromScore, toScore);
        }

        public long GetSortedSetCount(IRedisSortedSet<T> set)
        {
            return client.GetSortedSetCount(set.Id);
        }

        public double GetItemScoreInSortedSet(IRedisSortedSet<T> set, T value)
        {
            return client.GetItemScoreInSortedSet(set.Id, value.SerializeToString());
        }

        public long StoreIntersectFromSortedSets(IRedisSortedSet<T> intoSetId, params IRedisSortedSet<T>[] setIds)
        {
            return client.StoreIntersectFromSortedSets(intoSetId.Id, setIds.Map(x => x.Id).ToArray());
        }

        public long StoreIntersectFromSortedSets(IRedisSortedSet<T> intoSetId, IRedisSortedSet<T>[] setIds, string[] args)
        {
            return client.StoreIntersectFromSortedSets(intoSetId.Id, setIds.Map(x => x.Id).ToArray(), args);
        }

        public long StoreUnionFromSortedSets(IRedisSortedSet<T> intoSetId, params IRedisSortedSet<T>[] setIds)
        {
            return client.StoreUnionFromSortedSets(intoSetId.Id, setIds.Map(x => x.Id).ToArray());
        }

        public long StoreUnionFromSortedSets(IRedisSortedSet<T> intoSetId, IRedisSortedSet<T>[] setIds, string[] args)
        {
            return client.StoreUnionFromSortedSets(intoSetId.Id, setIds.Map(x => x.Id).ToArray(), args);
        }
    }
}
