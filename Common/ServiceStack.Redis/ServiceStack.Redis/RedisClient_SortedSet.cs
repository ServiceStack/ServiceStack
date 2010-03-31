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

using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Redis.Support;

namespace ServiceStack.Redis
{
	public partial class RedisClient : IRedisClient
	{
		public IHasNamed<IRedisSortedSet> SortedSets { get; set; }

		internal class RedisClientSortedSets
			: IHasNamed<IRedisSortedSet>
		{
			private readonly RedisClient client;

			public RedisClientSortedSets(RedisClient client)
			{
				this.client = client;
			}

			public IRedisSortedSet this[string setId]
			{
				get
				{
					return new RedisClientSortedSet(client, setId);
				}
				set
				{
					var col = this[setId];
					col.Clear();
					col.CopyTo(value.ToArray(), 0);
				}
			}
		}

		private static double GetLexicalScore(string value)
		{
			if (string.IsNullOrEmpty(value))
				return 0;

			var lexicalValue = 0;
			if (value.Length >= 1)
				lexicalValue += value[0] * (int)Math.Pow(256, 3);

			if (value.Length >= 2)
				lexicalValue += value[1] * (int)Math.Pow(256, 2);

			if (value.Length >= 3)
				lexicalValue += value[2] * (int)Math.Pow(256, 1);

			if (value.Length >= 4)
				lexicalValue += value[3];

			return lexicalValue;
		}

		public bool AddToSortedSet(string setId, string value)
		{
			return AddToSortedSet(setId, value, GetLexicalScore(value));
		}

		public bool AddToSortedSet(string setId, string value, double score)
		{
			return base.ZAdd(setId, score, value.ToUtf8Bytes()) == Success;
		}

		public double RemoveFromSortedSet(string setId, string value)
		{
			return base.ZRem(setId, value.ToUtf8Bytes());
		}

		public string PopItemWithLowestScoreFromSortedSet(string setId)
		{
			//TODO: this should be atomic
			var topScoreItemBytes = base.ZRange(setId, FirstElement, 1);
			if (topScoreItemBytes.Length == 0) return null;

			base.ZRem(setId, topScoreItemBytes[0]);
			return topScoreItemBytes[0].FromUtf8Bytes();
		}

		public string PopItemWithHighestScoreFromSortedSet(string setId)
		{
			//TODO: this should be atomic
			var topScoreItemBytes = base.ZRevRange(setId, FirstElement, 1);
			if (topScoreItemBytes.Length == 0) return null;

			base.ZRem(setId, topScoreItemBytes[0]);
			return topScoreItemBytes[0].FromUtf8Bytes();
		}

		public bool SortedSetContainsValue(string setId, string value)
		{
			return base.ZRank(setId, value.ToUtf8Bytes()) != -1;
		}

		public double IncrementItemInSortedSet(string setId, string value, double incrementBy)
		{
			return base.ZIncrBy(setId, incrementBy, value.ToUtf8Bytes());
		}

		public int GetItemIndexInSortedSet(string setId, string value)
		{
			return base.ZRank(setId, value.ToUtf8Bytes());
		}

		public int GetItemIndexInSortedSetDesc(string setId, string value)
		{
			return base.ZRevRank(setId, value.ToUtf8Bytes());
		}

		public List<string> GetAllFromSortedSet(string setId)
		{
			var multiDataList = base.ZRange(setId, FirstElement, LastElement);
			return multiDataList.ToStringList();
		}

		public List<string> GetAllFromSortedSetDesc(string setId)
		{
			var multiDataList = base.ZRevRange(setId, FirstElement, LastElement);
			return multiDataList.ToStringList();
		}

		public List<string> GetRangeFromSortedSet(string setId, int fromRank, int toRank)
		{
			var multiDataList = base.ZRange(setId, fromRank, toRank);
			return multiDataList.ToStringList();
		}

		public List<string> GetRangeFromSortedSetDesc(string setId, int fromRank, int toRank)
		{
			var multiDataList = base.ZRange(setId, fromRank, toRank);
			return multiDataList.ToStringList();
		}

		public IDictionary<string, double> GetRangeWithScoresFromSortedSet(string setId, int fromRank, int toRank)
		{
			var multiDataList = base.ZRangeWithScores(setId, fromRank, toRank);
			return CreateSortedScoreMap(multiDataList);

		}

		public IDictionary<string, double> GetRangeWithScoresFromSortedSetDesc(string setId, int fromRank, int toRank)
		{
			var multiDataList = base.ZRevRangeWithScores(setId, fromRank, toRank);
			return CreateSortedScoreMap(multiDataList);
		}

		private static IDictionary<string, double> CreateSortedScoreMap(byte[][] multiDataList)
		{
			var map = new OrderedDictionary<string, double>();

			for (var i = 0; i < multiDataList.Length; i += 2)
			{
				var key = multiDataList[i].FromUtf8Bytes();
				double value;
				double.TryParse(multiDataList[i + 1].FromUtf8Bytes(), out value);
				map[key] = value;
			}

			return map;
		}


		public List<string> GetRangeFromSortedSetByLowestScore(string setId, string fromStringScore, string toStringScore)
		{
			return GetRangeFromSortedSetByLowestScore(setId, fromStringScore, toStringScore, null, null);
		}

		public List<string> GetRangeFromSortedSetByLowestScore(string setId, string fromStringScore, string toStringScore, int? skip, int? take)
		{
			var fromScore = GetLexicalScore(fromStringScore);
			var toScore = GetLexicalScore(toStringScore);
			return GetRangeFromSortedSetByLowestScore(setId, fromScore, toScore, skip, take);
		}

		public List<string> GetRangeFromSortedSetByLowestScore(string setId, double fromScore, double toScore)
		{
			return GetRangeFromSortedSetByLowestScore(setId, fromScore, toScore, null, null);
		}

		public List<string> GetRangeFromSortedSetByLowestScore(string setId, double fromScore, double toScore, int? skip, int? take)
		{
			var multiDataList = base.ZRangeByScore(setId, fromScore, toScore, skip, take);
			return multiDataList.ToStringList();
		}

		public IDictionary<string, double> GetRangeWithScoresFromSortedSetByLowestScore(string setId, string fromStringScore, string toStringScore)
		{
			return GetRangeWithScoresFromSortedSetByLowestScore(setId, fromStringScore, toStringScore, null, null);
		}

		public IDictionary<string, double> GetRangeWithScoresFromSortedSetByLowestScore(string setId, string fromStringScore, string toStringScore, int? skip, int? take)
		{
			var fromScore = GetLexicalScore(fromStringScore);
			var toScore = GetLexicalScore(toStringScore);
			return GetRangeWithScoresFromSortedSetByLowestScore(setId, fromScore, toScore, skip, take);
		}

		public IDictionary<string, double> GetRangeWithScoresFromSortedSetByLowestScore(string setId, double fromScore, double toScore)
		{
			return GetRangeWithScoresFromSortedSetByLowestScore(setId, fromScore, toScore, null, null);
		}

		public IDictionary<string, double> GetRangeWithScoresFromSortedSetByLowestScore(string setId, double fromScore, double toScore, int? skip, int? take)
		{
			var multiDataList = base.ZRangeByScoreWithScores(setId, fromScore, toScore, skip, take);
			return CreateSortedScoreMap(multiDataList);
		}


		public List<string> GetRangeFromSortedSetByHighestScore(string setId, string fromStringScore, string toStringScore)
		{
			return GetRangeFromSortedSetByHighestScore(setId, fromStringScore, toStringScore, null, null);
		}

		public List<string> GetRangeFromSortedSetByHighestScore(string setId, string fromStringScore, string toStringScore, int? skip, int? take)
		{
			var fromScore = GetLexicalScore(fromStringScore);
			var toScore = GetLexicalScore(toStringScore);
			return GetRangeFromSortedSetByHighestScore(setId, fromScore, toScore, skip, take);
		}

		public List<string> GetRangeFromSortedSetByHighestScore(string setId, double fromScore, double toScore)
		{
			return GetRangeFromSortedSetByHighestScore(setId, fromScore, toScore, null, null);
		}

		public List<string> GetRangeFromSortedSetByHighestScore(string setId, double fromScore, double toScore, int? skip, int? take)
		{
			var multiDataList = base.ZRevRangeByScore(setId, fromScore, toScore, skip, take);
			return multiDataList.ToStringList();
		}

		public IDictionary<string, double> GetRangeWithScoresFromSortedSetByHighestScore(string setId, string fromStringScore, string toStringScore)
		{
			return GetRangeWithScoresFromSortedSetByHighestScore(setId, fromStringScore, toStringScore, null, null);
		}

		public IDictionary<string, double> GetRangeWithScoresFromSortedSetByHighestScore(string setId, string fromStringScore, string toStringScore, int? skip, int? take)
		{
			var fromScore = GetLexicalScore(fromStringScore);
			var toScore = GetLexicalScore(toStringScore);
			return GetRangeWithScoresFromSortedSetByHighestScore(setId, fromScore, toScore, skip, take);
		}

		public IDictionary<string, double> GetRangeWithScoresFromSortedSetByHighestScore(string setId, double fromScore, double toScore)
		{
			return GetRangeWithScoresFromSortedSetByHighestScore(setId, fromScore, toScore, null, null);
		}

		public IDictionary<string, double> GetRangeWithScoresFromSortedSetByHighestScore(string setId, double fromScore, double toScore, int? skip, int? take)
		{
			var multiDataList = base.ZRevRangeByScoreWithScores(setId, fromScore, toScore, skip, take);
			return CreateSortedScoreMap(multiDataList);
		}



		public int RemoveRangeFromSortedSet(string setId, int minRank, int maxRank)
		{
			return base.ZRemRangeByRank(setId, maxRank, maxRank);
		}

		public int RemoveRangeFromSortedSetByScore(string setId, double fromScore, double toScore)
		{
			return base.ZRemRangeByScore(setId, toScore, toScore);
		}

		public int GetSortedSetCount(string setId)
		{
			return base.ZCard(setId);
		}

		public double GetItemScoreInSortedSet(string setId, string value)
		{
			return base.ZScore(setId, value.ToUtf8Bytes());
		}

		public int StoreIntersectFromSortedSets(string intoSetId, params string[] setIds)
		{
			return base.ZInter(intoSetId, setIds);
		}

		public int StoreUnionFromSortedSets(string intoSetId, params string[] setIds)
		{
			return base.ZUnion(intoSetId, setIds);
		}
	}
}