using ServiceStack.Text;

namespace ServiceStack.Redis
{
	public static class Commands
	{
		public readonly static byte[] Quit = "QUIT".ToUtf8Bytes();
		public readonly static byte[] Auth = "AUTH".ToUtf8Bytes();
		public readonly static byte[] Exists = "EXISTS".ToUtf8Bytes();
		public readonly static byte[] Del = "DEL".ToUtf8Bytes();
		public readonly static byte[] Type = "TYPE".ToUtf8Bytes();
		public readonly static byte[] Keys = "KEYS".ToUtf8Bytes();
		public readonly static byte[] RandomKey = "RANDOMKEY".ToUtf8Bytes();
		public readonly static byte[] Rename = "RENAME".ToUtf8Bytes();
		public readonly static byte[] RenameNx = "RENAMENX".ToUtf8Bytes();
		public readonly static byte[] DbSize = "DBSIZE".ToUtf8Bytes();
		public readonly static byte[] Expire = "EXPIRE".ToUtf8Bytes();
		public readonly static byte[] ExpireAt = "EXPIREAT".ToUtf8Bytes();
		public readonly static byte[] Ttl = "TTL".ToUtf8Bytes();
		public readonly static byte[] Select = "SELECT".ToUtf8Bytes();
		public readonly static byte[] Move = "MOVE".ToUtf8Bytes();
		public readonly static byte[] FlushDb = "FLUSHDB".ToUtf8Bytes();
		public readonly static byte[] FlushAll = "FLUSHALL".ToUtf8Bytes();
		public readonly static byte[] Ping = "PING".ToUtf8Bytes();				//missing
		public readonly static byte[] Echo = "ECHO".ToUtf8Bytes();				//missing

		public readonly static byte[] Save = "SAVE".ToUtf8Bytes();
		public readonly static byte[] BgSave = "BGSAVE".ToUtf8Bytes();
		public readonly static byte[] LastSave = "LASTSAVE".ToUtf8Bytes();
		public readonly static byte[] Shutdown = "SHUTDOWN".ToUtf8Bytes();
		public readonly static byte[] BgRewriteAof = "BGREWRITEAOF".ToUtf8Bytes();

		public readonly static byte[] Info = "INFO".ToUtf8Bytes();
		public readonly static byte[] Monitor = "MONITOR".ToUtf8Bytes();		//missing
		public readonly static byte[] SlaveOf = "SLAVEOF".ToUtf8Bytes();		//missing
		public readonly static byte[] Debug = "DEBUG".ToUtf8Bytes();			//missing
		public readonly static byte[] Config = "CONFIG".ToUtf8Bytes();			//missing


		public readonly static byte[] Set = "SET".ToUtf8Bytes();
		public readonly static byte[] Get = "GET".ToUtf8Bytes();
		public readonly static byte[] GetSet = "GETSET".ToUtf8Bytes();
		public readonly static byte[] MGet = "MGET".ToUtf8Bytes();
		public readonly static byte[] SetNx = "SETNX".ToUtf8Bytes();
		public readonly static byte[] SetEx = "SETEX".ToUtf8Bytes();
		public readonly static byte[] MSet = "MSET".ToUtf8Bytes();
		public readonly static byte[] MSetNx = "MSETNX".ToUtf8Bytes();
		public readonly static byte[] Incr = "INCR".ToUtf8Bytes();
		public readonly static byte[] IncrBy = "INCRBY".ToUtf8Bytes();
		public readonly static byte[] Decr = "DECR".ToUtf8Bytes();
		public readonly static byte[] DecrBy = "DECRBY".ToUtf8Bytes();
		public readonly static byte[] Append = "APPEND".ToUtf8Bytes();
		public readonly static byte[] Substr = "SUBSTR".ToUtf8Bytes();

		public readonly static byte[] RPush = "RPUSH".ToUtf8Bytes();
		public readonly static byte[] LPush = "LPUSH".ToUtf8Bytes();
		public readonly static byte[] LLen = "LLEN".ToUtf8Bytes();
		public readonly static byte[] LRange = "LRANGE".ToUtf8Bytes();
		public readonly static byte[] LTrim = "LTRIM".ToUtf8Bytes();
		public readonly static byte[] LIndex = "LINDEX".ToUtf8Bytes();
		public readonly static byte[] LSet = "LSET".ToUtf8Bytes();
		public readonly static byte[] LRem = "LREM".ToUtf8Bytes();
		public readonly static byte[] LPop = "LPOP".ToUtf8Bytes();
		public readonly static byte[] RPop = "RPOP".ToUtf8Bytes();
		public readonly static byte[] BLPop = "BLPOP".ToUtf8Bytes();
		public readonly static byte[] BRPop = "BRPOP".ToUtf8Bytes();
		public readonly static byte[] RPopLPush = "RPOPLPUSH".ToUtf8Bytes();

		public readonly static byte[] SAdd = "SADD".ToUtf8Bytes();
		public readonly static byte[] SRem = "SREM".ToUtf8Bytes();
		public readonly static byte[] SPop = "SPOP".ToUtf8Bytes();
		public readonly static byte[] SMove = "SMOVE".ToUtf8Bytes();
		public readonly static byte[] SCard = "SCARD".ToUtf8Bytes();
		public readonly static byte[] SIsMember = "SISMEMBER".ToUtf8Bytes();
		public readonly static byte[] SInter = "SINTER".ToUtf8Bytes();
		public readonly static byte[] SInterStore = "SINTERSTORE".ToUtf8Bytes();
		public readonly static byte[] SUnion = "SUNION".ToUtf8Bytes();
		public readonly static byte[] SUnionStore = "SUNIONSTORE".ToUtf8Bytes();
		public readonly static byte[] SDiff = "SDIFF".ToUtf8Bytes();
		public readonly static byte[] SDiffStore = "SDIFFSTORE".ToUtf8Bytes();
		public readonly static byte[] SMembers = "SMEMBERS".ToUtf8Bytes();
		public readonly static byte[] SRandMember = "SRANDMEMBER".ToUtf8Bytes();

		public readonly static byte[] ZAdd = "ZADD".ToUtf8Bytes();
		public readonly static byte[] ZRem = "ZREM".ToUtf8Bytes();
		public readonly static byte[] ZIncrBy = "ZINCRBY".ToUtf8Bytes();
		public readonly static byte[] ZRank = "ZRANK".ToUtf8Bytes();
		public readonly static byte[] ZRevRank = "ZREVRANK".ToUtf8Bytes();
		public readonly static byte[] ZRange = "ZRANGE".ToUtf8Bytes();
		public readonly static byte[] ZRevRange = "ZREVRANGE".ToUtf8Bytes();
		public readonly static byte[] ZRangeByScore = "ZRANGEBYSCORE".ToUtf8Bytes();
		public readonly static byte[] ZRevRangeByScore = "ZREVRANGEBYSCORE".ToUtf8Bytes();
		public readonly static byte[] ZCard = "ZCARD".ToUtf8Bytes();
		public readonly static byte[] ZScore = "ZSCORE".ToUtf8Bytes();
		public readonly static byte[] ZRemRangeByRank = "ZREMRANGEBYRANK".ToUtf8Bytes();
		public readonly static byte[] ZRemRangeByScore = "ZREMRANGEBYSCORE".ToUtf8Bytes();
		public readonly static byte[] ZUnion = "ZUNION".ToUtf8Bytes();
		public readonly static byte[] ZInter = "ZINTER".ToUtf8Bytes();

		public readonly static byte[] HSet = "HSET".ToUtf8Bytes();
		public readonly static byte[] HSetNx = "HSETNX".ToUtf8Bytes();
		public readonly static byte[] HGet = "HGET".ToUtf8Bytes();
		public readonly static byte[] HMSet = "HMSET".ToUtf8Bytes();
		public readonly static byte[] HMGet = "HMGET".ToUtf8Bytes();
		public readonly static byte[] HIncrBy = "HINCRBY".ToUtf8Bytes();
		public readonly static byte[] HExists = "HEXISTS".ToUtf8Bytes();
		public readonly static byte[] HDel = "HDEL".ToUtf8Bytes();
		public readonly static byte[] HLen = "HLEN".ToUtf8Bytes();
		public readonly static byte[] HKeys = "HKEYS".ToUtf8Bytes();
		public readonly static byte[] HVals = "HVALS".ToUtf8Bytes();
		public readonly static byte[] HGetAll = "HGETALL".ToUtf8Bytes();

		public readonly static byte[] Sort = "SORT".ToUtf8Bytes();

		public readonly static byte[] Multi = "MULTI".ToUtf8Bytes();
		public readonly static byte[] Exec = "EXEC".ToUtf8Bytes();
		public readonly static byte[] Discard = "DISCARD".ToUtf8Bytes();

		public readonly static byte[] Subscribe = "SUBSCRIBE".ToUtf8Bytes();
		public readonly static byte[] UnSubscribe = "UNSUBSCRIBE".ToUtf8Bytes();
		public readonly static byte[] PSubscribe = "PSUBSCRIBE".ToUtf8Bytes();
		public readonly static byte[] PUnSubscribe = "PUNSUBSCRIBE".ToUtf8Bytes();
		public readonly static byte[] Publish = "PUBLISH".ToUtf8Bytes();


		public readonly static byte[] WithScores = "WITHSCORES".ToUtf8Bytes();
		public readonly static byte[] Limit = "LIMIT".ToUtf8Bytes();
		public readonly static byte[] By = "BY".ToUtf8Bytes();
		public readonly static byte[] Asc = "ASC".ToUtf8Bytes();
		public readonly static byte[] Desc = "DESC".ToUtf8Bytes();
		public readonly static byte[] Alpha = "ALPHA".ToUtf8Bytes();
		public readonly static byte[] Store = "STORE".ToUtf8Bytes();
	}
}