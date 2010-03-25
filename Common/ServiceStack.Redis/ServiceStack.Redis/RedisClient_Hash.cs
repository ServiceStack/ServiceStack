using System.Collections.Generic;

namespace ServiceStack.Redis
{
	public partial class RedisClient
	{
		public bool SetItemInHash(string hashId, string key, string value)
		{
			return base.HSet(hashId, key, ToBytes(value)) == Success;
		}

		public string GetItemFromHash(string hashId, string key)
		{
			return ToString(base.HGet(hashId, key));
		}

		public bool DeleteItemInHash(string hashId, string key)
		{
			return base.HDel(hashId, key) == Success;
		}

		public int GetHashCount(string hashId)
		{
			return base.HLen(hashId);
		}

		public List<string> GetHashKeys(string hashId)
		{
			var multiDataList = base.HKeys(hashId);
			return CreateList(multiDataList);
		}

		public List<string> GetHashValues(string hashId)
		{
			var multiDataList = base.HValues(hashId);
			return CreateList(multiDataList);
		}

		public Dictionary<string, string> GetAllFromHash(string hashId)
		{
			var multiDataList = base.HGetAll(hashId);
			var map = new Dictionary<string, string>();

			for (var i = 0; i < multiDataList.Length; i += 2)
			{
				var key = ToString(multiDataList[i]);
				map[key] = ToString(multiDataList[i + 1]);
			}

			return map;
		}
	}
}