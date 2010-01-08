//
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of reddis and ServiceStack: new BSD license.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Redis
{
	/// <summary>
	/// This class contains all the list operations for the RedisClient.
	/// </summary>
	public partial class RedisClient
	{
		const int FirstElement = 0;
		const int LastElement = -1;

		public RedisClientLists Lists { get; set; }

		internal byte[][] LRange(string listId, int startingFrom, int endingAt)
		{
			if (!SendDataCommand(null, "LRANGE {0} {1} {2}\r\n", listId, startingFrom, endingAt))
				throw new Exception("Unable to connect");

			return ReadMultiData();
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

		internal byte[][] Sort(string listOrSetId, int startingFrom, int endingAt, bool sortAlpha, bool sortDesc)
		{
			var sortAlphaOption = sortAlpha ? " ALPHA" : "";
			var sortDescOption = sortDesc ? " DESC" : "";

			if (!SendDataCommand(null, "SORT {0} LIMIT {1} {2}{3}{4}\r\n", listOrSetId, startingFrom, endingAt,
				sortAlphaOption, sortDescOption))
				throw new Exception("Unable to connect");

			return ReadMultiData();
		}

		public List<string> GetRangeFromSortedList(string listId, int startingFrom, int endingAt)
		{
			var multiDataList = Sort(listId, startingFrom, endingAt, true, false);
			return CreateList(multiDataList);
		}

		internal void RPush(string listId, byte[] value)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");
			if (value == null)
				throw new ArgumentNullException("value");

			if (!SendDataCommand(value, "RPUSH {0} {1}\r\n", listId, value.Length))
				throw new Exception("Unable to connect");
			ExpectSuccess();
		}

		public void AddToList(string listId, string value)
		{
			RPush(listId, Encoding.UTF8.GetBytes(value));
		}

		internal void LPush(string listId, byte[] value)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");
			if (value == null)
				throw new ArgumentNullException("value");

			if (!SendDataCommand(value, "LPUSH {0} {1}\r\n", listId, value.Length))
				throw new Exception("Unable to connect");
			ExpectSuccess();
		}

		public void PrependToList(string listId, string value)
		{
			LPush(listId, Encoding.UTF8.GetBytes(value));
		}

		internal void LTrim(string listId, int keepStartingFrom, int keepEndingAt)
		{
			if (!SendCommand("LTRIM {0} {1} {2}\r\n", listId, keepStartingFrom, keepEndingAt))
				throw new Exception("Unable to connect");

			ExpectSuccess();
		}

		internal int LRem(string listId, int removeNoOfMatches, byte[] value)
		{
			if (!SendDataCommand(value, "LREM {0} {1} {2}\r\n", listId, removeNoOfMatches, value.Length))
				throw new Exception("Unable to connect");

			return ReadInt();
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
			return LRem(listId, 0, Encoding.UTF8.GetBytes(value));
		}

		public int RemoveValueFromList(string listId, string value, int noOfMatches)
		{
			return LRem(listId, noOfMatches, Encoding.UTF8.GetBytes(value));
		}

		internal int LLen(string listId)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");

			return SendExpectInt("LLEN {0}\r\n", listId);
		}

		public int GetListCount(string setId)
		{
			return LLen(setId);
		}

		internal byte[] LIndex(string listId, int listIndex)
		{
			if (!SendCommand("LINDEX {0} {1}\r\n", listId, listIndex))
				throw new Exception("Unable to connect");
			return ReadData();
		}

		public string GetItemFromList(string listId, int listIndex)
		{
			return Encoding.UTF8.GetString(LIndex(listId, listIndex));
		}

		internal void LSet(string listId, int listIndex, byte[] value)
		{
			if (!SendDataCommand(value, "LSET {0} {1} {2}\r\n", listId, listIndex, value.Length))
				throw new Exception("Unable to connect");

			ExpectSuccess();
		}

		public void SetItemInList(string listId, int listIndex, string value)
		{
			LSet(listId, listIndex, Encoding.UTF8.GetBytes(value));
		}

		internal byte[] LPop(string listId)
		{
			if (!SendCommand("LPOP {0}\r\n", listId))
				throw new Exception("Unable to connect");
			return ReadData();
		}

		public string DequeueFromList(string listId)
		{
			return Encoding.UTF8.GetString(LPop(listId));
		}

		internal byte[] RPop(string listId)
		{
			if (!SendCommand("RPOP {0}\r\n", listId))
				throw new Exception("Unable to connect");
			return ReadData();
		}

		public string PopFromList(string listId)
		{
			return Encoding.UTF8.GetString(RPop(listId));
		}

		internal void RPopLPush(string fromListId, string toListId)
		{
			if (fromListId == null)
				throw new ArgumentNullException("fromListId");
			if (toListId == null)
				throw new ArgumentNullException("toListId");

			if (!SendCommand("RPOPLPUSH {0} {1}\r\n", fromListId, toListId))
				throw new Exception("Unable to connect");

			ExpectSuccess();
		}

		public void PopAndPushBetweenLists(string fromListId, string toListId)
		{
			RPopLPush(fromListId, toListId);
		}

	}


}