using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Redis.Support;

namespace ServiceStack.Redis
{
	public partial class RedisClient
	{
		public IHasNamed<IRedisClientSortedSet> SortedSets { get; set; }

		internal class RedisClientSortedSets
			: IHasNamed<IRedisClientSortedSet>
		{
			private readonly RedisClient client;

			public RedisClientSortedSets(RedisClient client)
			{
				this.client = client;
			}

			public IRedisClientSortedSet this[string setId]
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
			return base.ZAdd(setId, score, ToBytes(value)) == Success;
		}

		public double RemoveFromSortedSet(string setId, string value)
		{
			return base.ZRem(setId, ToBytes(value));
		}

		public string PopFromSortedSetItemWithLowestScore(string setId)
		{
			//TODO: this should be atomic
			var topScoreItemBytes = base.ZRange(setId, FirstElement, 1);
			if (topScoreItemBytes.Length == 0) return null;

			base.ZRem(setId, topScoreItemBytes[0]);
			return ToString(topScoreItemBytes[0]);
		}

		public string PopFromSortedSetItemWithHighestScore(string setId)
		{
			//TODO: this should be atomic
			var topScoreItemBytes = base.ZRevRange(setId, FirstElement, 1);
			if (topScoreItemBytes.Length == 0) return null;

			base.ZRem(setId, topScoreItemBytes[0]);
			return ToString(topScoreItemBytes[0]);
		}

		public bool SortedSetContainsValue(string setId, string value)
		{
			return base.ZRank(setId, ToBytes(value)) != -1;
		}

		public double IncrementItemInSortedSet(string setId, double incrementBy, string value)
		{
			return base.ZIncrBy(setId, incrementBy, ToBytes(value));
		}

		public int GetItemIndexInSortedSet(string setId, string value)
		{
			return base.ZRank(setId, ToBytes(value));
		}

		public int GetItemRankInSortedSetDesc(string setId, string value)
		{
			return base.ZRevRank(setId, ToBytes(value));
		}

		public List<string> GetAllFromSortedSet(string setId)
		{
			var multiDataList = base.ZRange(setId, FirstElement, LastElement);
			return CreateList(multiDataList);
		}

		public List<string> GetAllFromSortedSetDesc(string setId)
		{
			var multiDataList = base.ZRevRange(setId, FirstElement, LastElement);
			return CreateList(multiDataList);
		}

		public List<string> GetRangeFromSortedSet(string setId, int startingFrom, int endingAt)
		{
			var multiDataList = base.ZRange(setId, startingFrom, endingAt);
			return CreateList(multiDataList);
		}

		public IDictionary<string, double> GetRangeFromSortedSetWithScores(string setId, int startingFrom, int endingAt)
		{
			var multiDataList = base.ZRangeWithScores(setId, startingFrom, endingAt);
			return CreateSortedScoreMap(multiDataList);

		}

		public List<string> GetRangeFromSortedSetDesc(string setId, int startingFrom, int endingAt)
		{
			var multiDataList = base.ZRange(setId, startingFrom, endingAt);
			return CreateList(multiDataList);
		}

		public IDictionary<string, double> GetRangeFromSortedSetWithScoresDesc(string setId, int startingFrom, int endingAt)
		{
			var multiDataList = base.ZRevRangeWithScores(setId, startingFrom, endingAt);
			return CreateSortedScoreMap(multiDataList);
		}

		public List<string> GetRangeFromSortedSetByLowestScore(string setId, double startingFrom, double endingAt, int? skip, int? take)
		{
			var multiDataList = base.ZRangeByScore(setId, startingFrom, endingAt, skip, take);
			return CreateList(multiDataList);
		}

		private static IDictionary<string, double> CreateSortedScoreMap(byte[][] multiDataList)
		{
			var map = new OrderedDictionary<string, double>();

			for (var i = 0; i < multiDataList.Length; i += 2)
			{
				var key = ToString(multiDataList[i]);
				double value;
				double.TryParse(ToString(multiDataList[i + 1]), out value);
				map[key] = value;
			}

			return map;
		}

		public IDictionary<string, double> GetRangeWithScoresFromSortedSetByLowestScore(string setId, double fromScore, double toScore, int? skip, int? take)
		{
			var multiDataList = base.ZRangeByScoreWithScores(setId, fromScore, toScore, skip, take);
			return CreateSortedScoreMap(multiDataList);
		}

		public List<string> GetRangeFromSortedSetByLowestScore(string setId, string fromStringScore, string toStringScore, int? skip, int? take)
		{
			var fromScore = GetLexicalScore(fromStringScore);
			var toScore = GetLexicalScore(toStringScore);
			return GetRangeFromSortedSetByLowestScore(setId, fromScore, toScore, skip, take);
		}

		public IDictionary<string, double> GetRangeWithScoresFromSortedSetByLowestScore(string setId, string fromStringScore, string toStringScore, int? skip, int? take)
		{
			var fromScore = GetLexicalScore(fromStringScore);
			var toScore = GetLexicalScore(toStringScore);
			return GetRangeWithScoresFromSortedSetByLowestScore(setId, fromScore, toScore, skip, take);
		}

		public List<string> GetRangeFromSortedSetByHighestScore(string setId, double fromScore, double toScore, int? skip, int? take)
		{
			var multiDataList = base.ZRevRangeByScore(setId, fromScore, toScore, skip, take);
			return CreateList(multiDataList);
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
			return base.ZScore(setId, ToBytes(value));
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