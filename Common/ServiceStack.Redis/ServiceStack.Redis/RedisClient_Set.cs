using System.Collections.Generic;
using System.Linq;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis
{
	public partial class RedisClient
	{
		public IHasNamed<IRedisClientSet> Sets { get; set; }

		internal class RedisClientSets
			: IHasNamed<IRedisClientSet>
		{
			private readonly RedisClient client;

			public RedisClientSets(RedisClient client)
			{
				this.client = client;
			}

			public IRedisClientSet this[string setId]
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
				results.Add(ToString(multiData));
			}
			return results;
		}

		public List<string> GetSortedRange(string setId, int startingFrom, int endingAt)
		{
			var multiDataList = Sort(setId, startingFrom, endingAt, true, false);
			return CreateList(multiDataList);
		}

		public HashSet<string> GetAllFromSet(string setId)
		{
			var multiDataList = SMembers(setId);
			return CreateHashSet(multiDataList);
		}

		public void AddToSet(string setId, string value)
		{
			SAdd(setId, ToBytes(value));
		}

		public void RemoveFromSet(string setId, string value)
		{
			SRem(setId, ToBytes(value));
		}

		public string PopFromSet(string setId)
		{
			return ToString(SPop(setId));
		}

		public void MoveBetweenSets(string fromSetId, string toSetId, string value)
		{
			SMove(fromSetId, toSetId, ToBytes(value));
		}

		public int GetSetCount(string setId)
		{
			return SCard(setId);
		}

		public bool SetContainsValue(string setId, string value)
		{
			return SIsMember(setId, ToBytes(value)) == 1;
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
			return ToString(SRandMember(setId));
		}
	}
}