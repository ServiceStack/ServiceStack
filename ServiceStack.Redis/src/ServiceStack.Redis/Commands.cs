using System;
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
        public readonly static byte[] PExpire = "PEXPIRE".ToUtf8Bytes();
        public readonly static byte[] PExpireAt = "PEXPIREAT".ToUtf8Bytes();
        public readonly static byte[] DbSize = "DBSIZE".ToUtf8Bytes();
        public readonly static byte[] Expire = "EXPIRE".ToUtf8Bytes();
        public readonly static byte[] ExpireAt = "EXPIREAT".ToUtf8Bytes();
        public readonly static byte[] Ttl = "TTL".ToUtf8Bytes();
        public readonly static byte[] PTtl = "PTTL".ToUtf8Bytes();
        public readonly static byte[] Select = "SELECT".ToUtf8Bytes();
        public readonly static byte[] FlushDb = "FLUSHDB".ToUtf8Bytes();
        public readonly static byte[] FlushAll = "FLUSHALL".ToUtf8Bytes();
        public readonly static byte[] Ping = "PING".ToUtf8Bytes();
        public readonly static byte[] Echo = "ECHO".ToUtf8Bytes();

        public readonly static byte[] Save = "SAVE".ToUtf8Bytes();
        public readonly static byte[] BgSave = "BGSAVE".ToUtf8Bytes();
        public readonly static byte[] LastSave = "LASTSAVE".ToUtf8Bytes();
        public readonly static byte[] Shutdown = "SHUTDOWN".ToUtf8Bytes();
        public readonly static byte[] NoSave = "NOSAVE".ToUtf8Bytes();
        public readonly static byte[] BgRewriteAof = "BGREWRITEAOF".ToUtf8Bytes();

        public readonly static byte[] Info = "INFO".ToUtf8Bytes();
        public readonly static byte[] SlaveOf = "SLAVEOF".ToUtf8Bytes();
        public readonly static byte[] No = "NO".ToUtf8Bytes();
        public readonly static byte[] One = "ONE".ToUtf8Bytes();
        public readonly static byte[] ResetStat = "RESETSTAT".ToUtf8Bytes();
        public readonly static byte[] Rewrite = "REWRITE".ToUtf8Bytes();
        public readonly static byte[] Time = "TIME".ToUtf8Bytes();
        public readonly static byte[] Segfault = "SEGFAULT".ToUtf8Bytes();
        public readonly static byte[] Sleep = "SLEEP".ToUtf8Bytes();
        public readonly static byte[] Dump = "DUMP".ToUtf8Bytes();
        public readonly static byte[] Restore = "RESTORE".ToUtf8Bytes();
        public readonly static byte[] Migrate = "MIGRATE".ToUtf8Bytes();
        public readonly static byte[] Move = "MOVE".ToUtf8Bytes();
        public readonly static byte[] Object = "OBJECT".ToUtf8Bytes();
        public readonly static byte[] IdleTime = "IDLETIME".ToUtf8Bytes();
        public readonly static byte[] Monitor = "MONITOR".ToUtf8Bytes();		//missing
        public readonly static byte[] Debug = "DEBUG".ToUtf8Bytes();			//missing
        public readonly static byte[] Config = "CONFIG".ToUtf8Bytes();			//missing
        public readonly static byte[] Client = "CLIENT".ToUtf8Bytes();
        public readonly static byte[] List = "LIST".ToUtf8Bytes();
        public readonly static byte[] Kill = "KILL".ToUtf8Bytes();
        public readonly static byte[] Addr = "ADDR".ToUtf8Bytes();
        public readonly static byte[] Id = "ID".ToUtf8Bytes();
        public readonly static byte[] SkipMe = "SKIPME".ToUtf8Bytes();
        public readonly static byte[] SetName = "SETNAME".ToUtf8Bytes();
        public readonly static byte[] GetName = "GETNAME".ToUtf8Bytes();
        public readonly static byte[] Pause = "PAUSE".ToUtf8Bytes();
        public readonly static byte[] Role = "ROLE".ToUtf8Bytes();
        //public readonly static byte[] Get = "GET".ToUtf8Bytes();
        //public readonly static byte[] Set = "SET".ToUtf8Bytes();

        public readonly static byte[] StrLen = "STRLEN".ToUtf8Bytes();
        public readonly static byte[] Set = "SET".ToUtf8Bytes();
        public readonly static byte[] Get = "GET".ToUtf8Bytes();
        public readonly static byte[] GetSet = "GETSET".ToUtf8Bytes();
        public readonly static byte[] MGet = "MGET".ToUtf8Bytes();
        public readonly static byte[] SetNx = "SETNX".ToUtf8Bytes();
        public readonly static byte[] SetEx = "SETEX".ToUtf8Bytes();
        public readonly static byte[] Persist = "PERSIST".ToUtf8Bytes();
        public readonly static byte[] PSetEx = "PSETEX".ToUtf8Bytes();
        public readonly static byte[] MSet = "MSET".ToUtf8Bytes();
        public readonly static byte[] MSetNx = "MSETNX".ToUtf8Bytes();
        public readonly static byte[] Incr = "INCR".ToUtf8Bytes();
        public readonly static byte[] IncrBy = "INCRBY".ToUtf8Bytes();
        public readonly static byte[] IncrByFloat = "INCRBYFLOAT".ToUtf8Bytes();
        public readonly static byte[] Decr = "DECR".ToUtf8Bytes();
        public readonly static byte[] DecrBy = "DECRBY".ToUtf8Bytes();
        public readonly static byte[] Append = "APPEND".ToUtf8Bytes();
        public readonly static byte[] GetRange = "GETRANGE".ToUtf8Bytes();
        public readonly static byte[] SetRange = "SETRANGE".ToUtf8Bytes();
        public readonly static byte[] GetBit = "GETBIT".ToUtf8Bytes();
        public readonly static byte[] SetBit = "SETBIT".ToUtf8Bytes();
        public readonly static byte[] BitCount = "BITCOUNT".ToUtf8Bytes();

        public readonly static byte[] Scan = "SCAN".ToUtf8Bytes();
        public readonly static byte[] SScan = "SSCAN".ToUtf8Bytes();
        public readonly static byte[] HScan = "HSCAN".ToUtf8Bytes();
        public readonly static byte[] ZScan = "ZSCAN".ToUtf8Bytes();
        public readonly static byte[] Match = "MATCH".ToUtf8Bytes();
        public readonly static byte[] Count = "COUNT".ToUtf8Bytes();

        public readonly static byte[] PfAdd = "PFADD".ToUtf8Bytes();
        public readonly static byte[] PfCount = "PFCOUNT".ToUtf8Bytes();
        public readonly static byte[] PfMerge = "PFMERGE".ToUtf8Bytes();

        public readonly static byte[] RPush = "RPUSH".ToUtf8Bytes();
        public readonly static byte[] LPush = "LPUSH".ToUtf8Bytes();
        public readonly static byte[] RPushX = "RPUSHX".ToUtf8Bytes();
        public readonly static byte[] LPushX = "LPUSHX".ToUtf8Bytes();
        public readonly static byte[] LLen = "LLEN".ToUtf8Bytes();
        public readonly static byte[] LRange = "LRANGE".ToUtf8Bytes();
        public readonly static byte[] LTrim = "LTRIM".ToUtf8Bytes();
        public readonly static byte[] LIndex = "LINDEX".ToUtf8Bytes();
        public readonly static byte[] LInsert = "LINSERT".ToUtf8Bytes();
        public readonly static byte[] Before = "BEFORE".ToUtf8Bytes();
        public readonly static byte[] After = "AFTER".ToUtf8Bytes();
        public readonly static byte[] LSet = "LSET".ToUtf8Bytes();
        public readonly static byte[] LRem = "LREM".ToUtf8Bytes();
        public readonly static byte[] LPop = "LPOP".ToUtf8Bytes();
        public readonly static byte[] RPop = "RPOP".ToUtf8Bytes();
        public readonly static byte[] BLPop = "BLPOP".ToUtf8Bytes();
        public readonly static byte[] BRPop = "BRPOP".ToUtf8Bytes();
        public readonly static byte[] RPopLPush = "RPOPLPUSH".ToUtf8Bytes();
        public readonly static byte[] BRPopLPush = "BRPOPLPUSH".ToUtf8Bytes();

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
        public readonly static byte[] ZCount = "ZCOUNT".ToUtf8Bytes();
        public readonly static byte[] ZRemRangeByRank = "ZREMRANGEBYRANK".ToUtf8Bytes();
        public readonly static byte[] ZRemRangeByScore = "ZREMRANGEBYSCORE".ToUtf8Bytes();
        public readonly static byte[] ZUnionStore = "ZUNIONSTORE".ToUtf8Bytes();
        public readonly static byte[] ZInterStore = "ZINTERSTORE".ToUtf8Bytes();
        public static readonly byte[] ZRangeByLex = "ZRANGEBYLEX".ToUtf8Bytes();
        public static readonly byte[] ZLexCount = "ZLEXCOUNT".ToUtf8Bytes();
        public static readonly byte[] ZRemRangeByLex = "ZREMRANGEBYLEX".ToUtf8Bytes();

        public readonly static byte[] HSet = "HSET".ToUtf8Bytes();
        public readonly static byte[] HSetNx = "HSETNX".ToUtf8Bytes();
        public readonly static byte[] HGet = "HGET".ToUtf8Bytes();
        public readonly static byte[] HMSet = "HMSET".ToUtf8Bytes();
        public readonly static byte[] HMGet = "HMGET".ToUtf8Bytes();
        public readonly static byte[] HIncrBy = "HINCRBY".ToUtf8Bytes();
        public readonly static byte[] HIncrByFloat = "HINCRBYFLOAT".ToUtf8Bytes();
        public readonly static byte[] HExists = "HEXISTS".ToUtf8Bytes();
        public readonly static byte[] HDel = "HDEL".ToUtf8Bytes();
        public readonly static byte[] HLen = "HLEN".ToUtf8Bytes();
        public readonly static byte[] HKeys = "HKEYS".ToUtf8Bytes();
        public readonly static byte[] HVals = "HVALS".ToUtf8Bytes();
        public readonly static byte[] HGetAll = "HGETALL".ToUtf8Bytes();

        public readonly static byte[] Sort = "SORT".ToUtf8Bytes();

        public readonly static byte[] Watch = "WATCH".ToUtf8Bytes();
        public readonly static byte[] UnWatch = "UNWATCH".ToUtf8Bytes();
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

        public readonly static byte[] Eval = "EVAL".ToUtf8Bytes();
        public readonly static byte[] EvalSha = "EVALSHA".ToUtf8Bytes();
        public readonly static byte[] Script = "SCRIPT".ToUtf8Bytes();
        public readonly static byte[] Load = "LOAD".ToUtf8Bytes();
        //public readonly static byte[] Exists = "EXISTS".ToUtf8Bytes();
        public readonly static byte[] Flush = "FLUSH".ToUtf8Bytes();
        public readonly static byte[] Slowlog = "SLOWLOG".ToUtf8Bytes();

        public readonly static byte[] Ex = "EX".ToUtf8Bytes();
        public readonly static byte[] Px = "PX".ToUtf8Bytes();
        public readonly static byte[] Nx = "NX".ToUtf8Bytes();
        public readonly static byte[] Xx = "XX".ToUtf8Bytes();

        // Sentinel commands
        public readonly static byte[] Sentinel = "SENTINEL".ToUtf8Bytes();
        public readonly static byte[] Masters = "masters".ToUtf8Bytes();
        public readonly static byte[] Sentinels = "sentinels".ToUtf8Bytes();
        public readonly static byte[] Master = "master".ToUtf8Bytes();
        public readonly static byte[] Slaves = "slaves".ToUtf8Bytes();
        public readonly static byte[] Failover = "failover".ToUtf8Bytes();
        public readonly static byte[] GetMasterAddrByName = "get-master-addr-by-name".ToUtf8Bytes();

        //Geo commands
        public readonly static byte[] GeoAdd = "GEOADD".ToUtf8Bytes();
        public readonly static byte[] GeoDist = "GEODIST".ToUtf8Bytes();
        public readonly static byte[] GeoHash = "GEOHASH".ToUtf8Bytes();
        public readonly static byte[] GeoPos = "GEOPOS".ToUtf8Bytes();
        public readonly static byte[] GeoRadius = "GEORADIUS".ToUtf8Bytes();
        public readonly static byte[] GeoRadiusByMember = "GEORADIUSBYMEMBER".ToUtf8Bytes();

        public readonly static byte[] WithCoord = "WITHCOORD".ToUtf8Bytes();
        public readonly static byte[] WithDist = "WITHDIST".ToUtf8Bytes();
        public readonly static byte[] WithHash = "WITHHASH".ToUtf8Bytes();

        public readonly static byte[] Meters = RedisGeoUnit.Meters.ToUtf8Bytes();
        public readonly static byte[] Kilometers = RedisGeoUnit.Kilometers.ToUtf8Bytes();
        public readonly static byte[] Miles = RedisGeoUnit.Miles.ToUtf8Bytes();
        public readonly static byte[] Feet = RedisGeoUnit.Feet.ToUtf8Bytes();

        public static byte[] GetUnit(string unit)
        {
            if (unit == null)
                throw new ArgumentNullException("unit");

            switch (unit)
            {
                case RedisGeoUnit.Meters:
                    return Meters;
                case RedisGeoUnit.Kilometers:
                    return Kilometers;
                case RedisGeoUnit.Miles:
                    return Miles;
                case RedisGeoUnit.Feet:
                    return Feet;
                default:
                    throw new NotSupportedException("Unit '{0}' is not a valid unit".Fmt(unit));
            }
        }
    }
}