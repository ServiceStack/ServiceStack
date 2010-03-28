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

		public List<string> GetAllFromList(string listId)
		{
			var multiDataList = LRange(listId, FirstElement, LastElement);
			return multiDataList.ToStringList();
		}

		public List<string> GetRangeFromList(string listId, int startingFrom, int endingAt)
		{
			var multiDataList = LRange(listId, startingFrom, endingAt);
			return multiDataList.ToStringList();
		}

		public List<string> GetRangeFromSortedList(string listId, int startingFrom, int endingAt)
		{
			var multiDataList = Sort(listId, startingFrom, endingAt, true, false);
			return multiDataList.ToStringList();
		}

		public void AddToList(string listId, string value)
		{
			RPush(listId, value.ToUtf8Bytes());
		}

		public void PrependToList(string listId, string value)
		{
			LPush(listId, value.ToUtf8Bytes());
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
			return LRem(listId, 0, value.ToUtf8Bytes());
		}

		public int RemoveValueFromList(string listId, string value, int noOfMatches)
		{
			return LRem(listId, noOfMatches, value.ToUtf8Bytes());
		}

		public int GetListCount(string listId)
		{
			return LLen(listId);
		}

		public string GetItemFromList(string listId, int listIndex)
		{
			return LIndex(listId, listIndex).FromUtf8Bytes();
		}

		public void SetItemInList(string listId, int listIndex, string value)
		{
			LSet(listId, listIndex, value.ToUtf8Bytes());
		}

		public string DequeueFromList(string listId)
		{
			return LPop(listId).FromUtf8Bytes();
		}

		public string PopFromList(string listId)
		{
			return RPop(listId).FromUtf8Bytes();
		}

		public string PopAndPushBetweenLists(string fromListId, string toListId)
		{
			return RPopLPush(fromListId, toListId).FromUtf8Bytes();
		}
	}
}