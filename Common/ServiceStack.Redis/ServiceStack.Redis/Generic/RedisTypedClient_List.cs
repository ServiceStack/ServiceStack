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

namespace ServiceStack.Redis.Generic
{
	internal partial class RedisTypedClient<T>
	{
		const int FirstElement = 0;
		const int LastElement = -1;

		public IHasNamed<IRedisList<T>> Lists { get; set; }

		internal class RedisClientLists
			: IHasNamed<IRedisList<T>>
		{
			private readonly RedisTypedClient<T> client;

			public RedisClientLists(RedisTypedClient<T> client)
			{
				this.client = client;
			}

			public IRedisList<T> this[string listId]
			{
				get
				{
					return new RedisClientList<T>(client, listId);
				}
				set
				{
					var list = this[listId];
					list.Clear();
					list.CopyTo(value.ToArray(), 0);
				}
			}
		}

		private List<T> CreateList(byte[][] multiDataList)
		{
			var results = new List<T>();
			foreach (var multiData in multiDataList)
			{
				results.Add(FromBytes(multiData));
			}
			return results;
		}

		public List<T> GetAllFromList(IRedisList<T> fromList)
		{
			var multiDataList = client.LRange(fromList.Id, FirstElement, LastElement);
			return CreateList(multiDataList);
		}

		public List<T> GetRangeFromList(IRedisList<T> fromList, int startingFrom, int endingAt)
		{
			var multiDataList = client.LRange(fromList.Id, startingFrom, endingAt);
			return CreateList(multiDataList);
		}

		public List<T> SortList(IRedisList<T> fromList, int startingFrom, int endingAt)
		{
			var multiDataList = client.Sort(fromList.Id, startingFrom, endingAt, true, false);
			return CreateList(multiDataList);
		}

		public void AddToList(IRedisList<T> fromList, T value)
		{
			client.RPush(fromList.Id, ToBytes(value));
		}

		public void PrependToList(IRedisList<T> fromList, T value)
		{
			client.LPush(fromList.Id, ToBytes(value));
		}

		public void RemoveAllFromList(IRedisList<T> fromList)
		{
			client.LTrim(fromList.Id, LastElement, FirstElement);
		}

		public void TrimList(IRedisList<T> fromList, int keepStartingFrom, int keepEndingAt)
		{
			client.LTrim(fromList.Id, keepStartingFrom, keepEndingAt);
		}

		public int RemoveValueFromList(IRedisList<T> fromList, T value)
		{
			const int removeAll = 0;
			return client.LRem(fromList.Id, removeAll, ToBytes(value));
		}

		public int RemoveValueFromList(IRedisList<T> fromList, T value, int noOfMatches)
		{
			return client.LRem(fromList.Id, noOfMatches, ToBytes(value));
		}

		public int GetListCount(IRedisList<T> fromList)
		{
			return client.LLen(fromList.Id);
		}

		public T GetItemFromList(IRedisList<T> fromList, int listIndex)
		{
			return FromBytes(client.LIndex(fromList.Id, listIndex));
		}

		public void SetItemInList(IRedisList<T> toList, int listIndex, T value)
		{
			client.LSet(toList.Id, listIndex, ToBytes(value));
		}

		public T DequeueFromList(IRedisList<T> fromList)
		{
			return FromBytes(client.LPop(fromList.Id));
		}

		public T PopFromList(IRedisList<T> fromList)
		{
			return FromBytes(client.RPop(fromList.Id));
		}

		public T PopAndPushBetweenLists(IRedisList<T> fromList, IRedisList<T> toList)
		{
			return FromBytes(client.RPopLPush(fromList.Id, toList.Id));
		}
	}
}