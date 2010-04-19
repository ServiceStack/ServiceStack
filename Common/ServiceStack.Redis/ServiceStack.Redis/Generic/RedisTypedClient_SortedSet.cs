//
// http://code.google.com/p/servicestack/wiki/ServiceStackRedis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of Redis and ServiceStack: new BSD license.
//

using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common.Utils;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Redis.Support;
using ServiceStack.Text;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Redis.Generic
{
	internal partial class RedisTypedClient<T>
	{
		public IHasNamed<IRedisSortedSet<T>> SortedSets { get; set; }

		internal class RedisClientSortedSets
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

		private static IDictionary<T, double> CreateGenericMap(IDictionary<string, double> map)
		{
			var genericMap = new OrderedDictionary<T, double>();
			foreach (var entry in map)
			{
				genericMap[entry.Key.DeserializeFromString<T>()] = entry.Value;
			}
			return genericMap;
		}

		public void AddToSortedSet(IRedisSortedSet<T> toSet, T value)
		{
			client.AddToSortedSet(toSet.Id, value.SerializeToString());
		}

		public void AddToSortedSet(IRedisSortedSet<T> toSet, T value, double score)
		{
			client.AddToSortedSet(toSet.Id, value.SerializeToString(), score);
		}

		public void RemoveFromSortedSet(IRedisSortedSet<T> fromSet, T value)
		{
			client.RemoveFromSortedSet(fromSet.Id, value.SerializeToString());
		}

		public T PopItemWithLowestScoreFromSortedSet(IRedisSortedSet<T> fromSet)
		{
			return client.PopItemWithLowestScoreFromSortedSet(fromSet.Id)
				.DeserializeFromString<T>();
		}

		public T PopItemWithHighestScoreFromSortedSet(IRedisSortedSet<T> fromSet)
		{
			return client.PopItemWithHighestScoreFromSortedSet(fromSet.Id)
				.DeserializeFromString<T>();
		}

		public bool SortedSetContainsValue(IRedisSortedSet<T> set, T value)
		{
			return client.SortedSetContainsValue(set.Id, value.SerializeToString());
		}

		public double IncrementItemInSortedSet(IRedisSortedSet<T> set, T value, double incrementBy)
		{
			return client.IncrementItemInSortedSet(set.Id, value.SerializeToString(), incrementBy);
		}

		public int GetItemIndexInSortedSet(IRedisSortedSet<T> set, T value)
		{
			return client.GetItemIndexInSortedSet(set.Id, value.SerializeToString());
		}

		public int GetItemIndexInSortedSetDesc(IRedisSortedSet<T> set, T value)
		{
			return client.GetItemIndexInSortedSetDesc(set.Id, value.SerializeToString());
		}

		public List<T> GetAllFromSortedSet(IRedisSortedSet<T> set)
		{
			var list = client.GetAllFromSortedSet(set.Id);
			return list.ConvertEachTo<T>();
		}

		public List<T> GetAllFromSortedSetDesc(IRedisSortedSet<T> set)
		{
			var list = client.GetAllFromSortedSetDesc(set.Id);
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
			var list = client.GetRangeFromSortedSetByHighestScore(set.Id, fromScore, toScore, take, skip);
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

		public int RemoveRangeFromSortedSet(IRedisSortedSet<T> set, int minRank, int maxRank)
		{
			return client.RemoveRangeFromSortedSet(set.Id, maxRank, maxRank);
		}

		public int RemoveRangeFromSortedSetByScore(IRedisSortedSet<T> set, double fromScore, double toScore)
		{
			return client.RemoveRangeFromSortedSetByScore(set.Id, fromScore, toScore);
		}
        
		public int GetSortedSetCount(IRedisSortedSet<T> set)
		{
			return client.GetSortedSetCount(set.Id);
		}

		public double GetItemScoreInSortedSet(IRedisSortedSet<T> set, T value)
		{
			return client.GetItemScoreInSortedSet(set.Id, value.SerializeToString());
		}

		public int StoreIntersectFromSortedSets(IRedisSortedSet<T> intoSetId, params IRedisSortedSet<T>[] setIds)
		{
			return client.StoreIntersectFromSortedSets(intoSetId.Id, setIds.ConvertAll(x => x.Id).ToArray());
		}

		public int StoreUnionFromSortedSets(IRedisSortedSet<T> intoSetId, params IRedisSortedSet<T>[] setIds)
		{
			return client.StoreUnionFromSortedSets(intoSetId.Id, setIds.ConvertAll(x => x.Id).ToArray());
		}
	}
}