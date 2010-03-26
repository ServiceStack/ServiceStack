using System.Collections.Generic;
using System.Linq;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis
{
	public partial class RedisClient 
		: IRedisClient
	{
		const int FirstElement = 0;
		const int LastElement = -1;

		public IHasNamed<IRedisList> Lists { get; set; }

		internal class RedisClientLists
			: IHasNamed<IRedisList>
		{
			private readonly RedisClient client;

			public RedisClientLists(RedisClient client)
			{
				this.client = client;
			}

			public IRedisList this[string listId]
			{
				get
				{
					return new RedisClientList(client, listId);
				}
				set
				{
					var list = this[listId];
					list.Clear();
					list.CopyTo(value.ToArray(), 0);
				}
			}
		}

		private static List<string> CreateList(byte[][] multiDataList)
		{
			var results = new List<string>();
			foreach (var multiData in multiDataList)
			{
				results.Add(ToString(multiData));
			}
			return results;
		}

		public List<string> GetAllFromList(string listId)
		{
			var multiDataList = LRange(listId, FirstElement, LastElement);
			return CreateList(multiDataList);
		}

		public List<string> GetRangeFromList(string listId, int startingFrom, int endingAt)
		{
			var multiDataList = LRange(listId, startingFrom, endingAt);
			return CreateList(multiDataList);
		}

		public List<string> GetRangeFromSortedList(string listId, int startingFrom, int endingAt)
		{
			var multiDataList = Sort(listId, startingFrom, endingAt, true, false);
			return CreateList(multiDataList);
		}

		public void AddToList(string listId, string value)
		{
			RPush(listId, ToBytes(value));
		}

		public void PrependToList(string listId, string value)
		{
			LPush(listId, ToBytes(value));
		}

		public void RemoveAllFromList(string listId)
		{
			LTrim(listId, LastElement, FirstElement);
		}

		public void TrimList(string listId, int keepStartingFrom, int keepEndingAt)
		{
			LTrim(listId, keepStartingFrom, keepEndingAt);
		}

		public int RemoveValueFromList(string listId, string value)
		{
			return LRem(listId, 0, ToBytes(value));
		}

		public int RemoveValueFromList(string listId, string value, int noOfMatches)
		{
			return LRem(listId, noOfMatches, ToBytes(value));
		}

		public int GetListCount(string listId)
		{
			return LLen(listId);
		}

		public string GetItemFromList(string listId, int listIndex)
		{
			return ToString(LIndex(listId, listIndex));
		}

		public void SetItemInList(string listId, int listIndex, string value)
		{
			LSet(listId, listIndex, ToBytes(value));
		}

		public string DequeueFromList(string listId)
		{
			return ToString(LPop(listId));
		}

		public string PopFromList(string listId)
		{
			return ToString(RPop(listId));
		}

		public string PopAndPushBetweenLists(string fromListId, string toListId)
		{
			return ToString(RPopLPush(fromListId, toListId));
		}
	}
}