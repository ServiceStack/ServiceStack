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
using ServiceStack.Text;

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

		public List<string> GetAllItemsFromList(string listId)
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
			var sortOptions = new SortOptions { Skip = startingFrom, Take = endingAt, SortAlpha = true };
			var multiDataList = Sort(listId, sortOptions);
			return multiDataList.ToStringList();
		}

		public void AddItemToList(string listId, string value)
		{
			RPush(listId, value.ToUtf8Bytes());
		}

		public void AddRangeToList(string listId, List<string> values)
		{
			var uListId = listId.ToUtf8Bytes();

			var pipeline = CreatePipelineCommand();
			foreach (var value in values)
			{
				pipeline.WriteCommand(Commands.RPush, uListId, value.ToUtf8Bytes());
			}
			pipeline.Flush();

			//the number of items after 
			var intResults = pipeline.ReadAllAsInts();
		}

		public void PrependItemToList(string listId, string value)
		{
			LPush(listId, value.ToUtf8Bytes());
		}

		public void PrependRangeToList(string listId, List<string> values)
		{
			var uListId = listId.ToUtf8Bytes();

			var pipeline = CreatePipelineCommand();
			//ensure list[0] == value[0] after batch operation
			for (var i = values.Count - 1; i >= 0; i--)
			{
				var value = values[i];
				pipeline.WriteCommand(Commands.LPush, uListId, value.ToUtf8Bytes());
			}
			pipeline.Flush();

			//the number of items after 
			var intResults = pipeline.ReadAllAsInts();
		}

		public void RemoveAllFromList(string listId)
		{
			LTrim(listId, LastElement, FirstElement);
		}

		public string RemoveStartFromList(string listId)
		{
			return base.LPop(listId).FromUtf8Bytes();
		}

		public string BlockingRemoveStartFromList(string listId, TimeSpan? timeOut)
		{
			return base.BLPopValue(listId, (int)timeOut.GetValueOrDefault().TotalSeconds).FromUtf8Bytes();
		}

		public string RemoveEndFromList(string listId)
		{
			return base.RPop(listId).FromUtf8Bytes();
		}

		public void TrimList(string listId, int keepStartingFrom, int keepEndingAt)
		{
			LTrim(listId, keepStartingFrom, keepEndingAt);
		}

		public int RemoveItemFromList(string listId, string value)
		{
			return LRem(listId, 0, value.ToUtf8Bytes());
		}

		public int RemoveItemFromList(string listId, string value, int noOfMatches)
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

		public void EnqueueItemOnList(string listId, string value)
		{
			RPush(listId, value.ToUtf8Bytes());
		}

		public string DequeueItemFromList(string listId)
		{
			return LPop(listId).FromUtf8Bytes();
		}

		public string BlockingDequeueItemFromList(string listId, TimeSpan? timeOut)
		{
			return BLPopValue(listId, (int)timeOut.GetValueOrDefault().TotalSeconds).FromUtf8Bytes();
		}

		public void PushItemToList(string listId, string value)
		{
			RPush(listId, value.ToUtf8Bytes());
		}

		public string PopItemFromList(string listId)
		{
			return RPop(listId).FromUtf8Bytes();
		}

		public string BlockingPopItemFromList(string listId, TimeSpan? timeOut)
		{
			return BRPopValue(listId, (int)timeOut.GetValueOrDefault().TotalSeconds).FromUtf8Bytes();
		}

		public string PopAndPushItemBetweenLists(string fromListId, string toListId)
		{
			return RPopLPush(fromListId, toListId).FromUtf8Bytes();
		}
	}
}