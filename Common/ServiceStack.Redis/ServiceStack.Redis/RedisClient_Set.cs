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
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
	public partial class RedisClient
		: IRedisClient
	{
		public IHasNamed<IRedisSet> Sets { get; set; }

		internal class RedisClientSets
			: IHasNamed<IRedisSet>
		{
			private readonly RedisClient client;

			public RedisClientSets(RedisClient client)
			{
				this.client = client;
			}

			public IRedisSet this[string setId]
			{
				get
				{
					return new RedisClientSet(client, setId);
				}
				set
				{
					var col = this[setId];
					col.Clear();
					col.CopyTo(value.ToArray(), 0);
				}
			}
		}

		private static HashSet<string> CreateHashSet(byte[][] multiDataList)
		{
			var results = new HashSet<string>();
			foreach (var multiData in multiDataList)
			{
				results.Add(multiData.FromUtf8Bytes());
			}
			return results;
		}

		public List<string> GetSortedRange(string setId, int startingFrom, int endingAt)
		{
			var sortOptions = new SortOptions { Skip = startingFrom, Take = endingAt, };
			var multiDataList = Sort(setId, sortOptions);
			return multiDataList.ToStringList();
		}

		public HashSet<string> GetAllFromSet(string setId)
		{
			var multiDataList = SMembers(setId);
			return CreateHashSet(multiDataList);
		}

		public void AddToSet(string setId, string value)
		{
			SAdd(setId, value.ToUtf8Bytes());
		}

		public void RemoveFromSet(string setId, string value)
		{
			SRem(setId, value.ToUtf8Bytes());
		}

		public string PopFromSet(string setId)
		{
			return SPop(setId).FromUtf8Bytes();
		}

		public void MoveBetweenSets(string fromSetId, string toSetId, string value)
		{
			SMove(fromSetId, toSetId, value.ToUtf8Bytes());
		}

		public int GetSetCount(string setId)
		{
			return SCard(setId);
		}

		public bool SetContainsValue(string setId, string value)
		{
			return SIsMember(setId, value.ToUtf8Bytes()) == 1;
		}

		public HashSet<string> GetIntersectFromSets(params string[] setIds)
		{
			if (setIds.Length == 0)
				return new HashSet<string>();

			var multiDataList = SInter(setIds);
			return CreateHashSet(multiDataList);
		}

		public void StoreIntersectFromSets(string intoSetId, params string[] setIds)
		{
			if (setIds.Length == 0) return;

			SInterStore(intoSetId, setIds);
		}

		public HashSet<string> GetUnionFromSets(params string[] setIds)
		{
			if (setIds.Length == 0)
				return new HashSet<string>();

			var multiDataList = SUnion(setIds);
			return CreateHashSet(multiDataList);
		}

		public void StoreUnionFromSets(string intoSetId, params string[] setIds)
		{
			if (setIds.Length == 0) return;

			SUnionStore(intoSetId, setIds);
		}

		public HashSet<string> GetDifferencesFromSet(string fromSetId, params string[] withSetIds)
		{
			if (withSetIds.Length == 0)
				return new HashSet<string>();

			var multiDataList = SDiff(fromSetId, withSetIds);
			return CreateHashSet(multiDataList);
		}

		public void StoreDifferencesFromSet(string intoSetId, string fromSetId, params string[] withSetIds)
		{
			if (withSetIds.Length == 0) return;

			SDiffStore(intoSetId, fromSetId, withSetIds);
		}

		public string GetRandomEntryFromSet(string setId)
		{
			return SRandMember(setId).FromUtf8Bytes();
		}
	}
}