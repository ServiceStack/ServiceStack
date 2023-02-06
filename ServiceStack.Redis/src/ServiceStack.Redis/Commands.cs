using System;
using ServiceStack.Text;

namespace ServiceStack.Redis;

public static class Commands
{
    public static readonly byte[] Quit = "QUIT".ToUtf8Bytes();
    public static readonly byte[] Auth = "AUTH".ToUtf8Bytes();
    public static readonly byte[] Exists = "EXISTS".ToUtf8Bytes();
    public static readonly byte[] Del = "DEL".ToUtf8Bytes();
    public static readonly byte[] Type = "TYPE".ToUtf8Bytes();
    public static readonly byte[] Keys = "KEYS".ToUtf8Bytes();
    public static readonly byte[] RandomKey = "RANDOMKEY".ToUtf8Bytes();
    public static readonly byte[] Rename = "RENAME".ToUtf8Bytes();
    public static readonly byte[] RenameNx = "RENAMENX".ToUtf8Bytes();
    public static readonly byte[] PExpire = "PEXPIRE".ToUtf8Bytes();
    public static readonly byte[] PExpireAt = "PEXPIREAT".ToUtf8Bytes();
    public static readonly byte[] DbSize = "DBSIZE".ToUtf8Bytes();
    public static readonly byte[] Expire = "EXPIRE".ToUtf8Bytes();
    public static readonly byte[] ExpireAt = "EXPIREAT".ToUtf8Bytes();
    public static readonly byte[] Ttl = "TTL".ToUtf8Bytes();
    public static readonly byte[] PTtl = "PTTL".ToUtf8Bytes();
    public static readonly byte[] Select = "SELECT".ToUtf8Bytes();
    public static readonly byte[] FlushDb = "FLUSHDB".ToUtf8Bytes();
    public static readonly byte[] FlushAll = "FLUSHALL".ToUtf8Bytes();
    public static readonly byte[] Ping = "PING".ToUtf8Bytes();
    public static readonly byte[] Echo = "ECHO".ToUtf8Bytes();

    public static readonly byte[] Save = "SAVE".ToUtf8Bytes();
    public static readonly byte[] BgSave = "BGSAVE".ToUtf8Bytes();
    public static readonly byte[] LastSave = "LASTSAVE".ToUtf8Bytes();
    public static readonly byte[] Shutdown = "SHUTDOWN".ToUtf8Bytes();
    public static readonly byte[] NoSave = "NOSAVE".ToUtf8Bytes();
    public static readonly byte[] BgRewriteAof = "BGREWRITEAOF".ToUtf8Bytes();

    public static readonly byte[] Info = "INFO".ToUtf8Bytes();
    public static readonly byte[] SlaveOf = "SLAVEOF".ToUtf8Bytes();
    public static readonly byte[] No = "NO".ToUtf8Bytes();
    public static readonly byte[] One = "ONE".ToUtf8Bytes();
    public static readonly byte[] ResetStat = "RESETSTAT".ToUtf8Bytes();
    public static readonly byte[] Rewrite = "REWRITE".ToUtf8Bytes();
    public static readonly byte[] Time = "TIME".ToUtf8Bytes();
    public static readonly byte[] Segfault = "SEGFAULT".ToUtf8Bytes();
    public static readonly byte[] Sleep = "SLEEP".ToUtf8Bytes();
    public static readonly byte[] Dump = "DUMP".ToUtf8Bytes();
    public static readonly byte[] Restore = "RESTORE".ToUtf8Bytes();
    public static readonly byte[] Migrate = "MIGRATE".ToUtf8Bytes();
    public static readonly byte[] Move = "MOVE".ToUtf8Bytes();
    public static readonly byte[] Object = "OBJECT".ToUtf8Bytes();
    public static readonly byte[] IdleTime = "IDLETIME".ToUtf8Bytes();
    public static readonly byte[] Monitor = "MONITOR".ToUtf8Bytes();		//missing
    public static readonly byte[] Debug = "DEBUG".ToUtf8Bytes();			//missing
    public static readonly byte[] Config = "CONFIG".ToUtf8Bytes();			//missing
    public static readonly byte[] Client = "CLIENT".ToUtf8Bytes();
    public static readonly byte[] List = "LIST".ToUtf8Bytes();
    public static readonly byte[] Kill = "KILL".ToUtf8Bytes();
    public static readonly byte[] Addr = "ADDR".ToUtf8Bytes();
    public static readonly byte[] Id = "ID".ToUtf8Bytes();
    public static readonly byte[] SkipMe = "SKIPME".ToUtf8Bytes();
    public static readonly byte[] SetName = "SETNAME".ToUtf8Bytes();
    public static readonly byte[] GetName = "GETNAME".ToUtf8Bytes();
    public static readonly byte[] Pause = "PAUSE".ToUtf8Bytes();
    public static readonly byte[] Role = "ROLE".ToUtf8Bytes();
    //public static readonly byte[] Get = "GET".ToUtf8Bytes();
    //public static readonly byte[] Set = "SET".ToUtf8Bytes();

    public static readonly byte[] StrLen = "STRLEN".ToUtf8Bytes();
    public static readonly byte[] Set = "SET".ToUtf8Bytes();
    public static readonly byte[] Get = "GET".ToUtf8Bytes();
    public static readonly byte[] GetSet = "GETSET".ToUtf8Bytes();
    public static readonly byte[] MGet = "MGET".ToUtf8Bytes();
    public static readonly byte[] SetNx = "SETNX".ToUtf8Bytes();
    public static readonly byte[] SetEx = "SETEX".ToUtf8Bytes();
    public static readonly byte[] Persist = "PERSIST".ToUtf8Bytes();
    public static readonly byte[] PSetEx = "PSETEX".ToUtf8Bytes();
    public static readonly byte[] MSet = "MSET".ToUtf8Bytes();
    public static readonly byte[] MSetNx = "MSETNX".ToUtf8Bytes();
    public static readonly byte[] Incr = "INCR".ToUtf8Bytes();
    public static readonly byte[] IncrBy = "INCRBY".ToUtf8Bytes();
    public static readonly byte[] IncrByFloat = "INCRBYFLOAT".ToUtf8Bytes();
    public static readonly byte[] Decr = "DECR".ToUtf8Bytes();
    public static readonly byte[] DecrBy = "DECRBY".ToUtf8Bytes();
    public static readonly byte[] Append = "APPEND".ToUtf8Bytes();
    public static readonly byte[] GetRange = "GETRANGE".ToUtf8Bytes();
    public static readonly byte[] SetRange = "SETRANGE".ToUtf8Bytes();
    public static readonly byte[] GetBit = "GETBIT".ToUtf8Bytes();
    public static readonly byte[] SetBit = "SETBIT".ToUtf8Bytes();
    public static readonly byte[] BitCount = "BITCOUNT".ToUtf8Bytes();

    public static readonly byte[] Scan = "SCAN".ToUtf8Bytes();
    public static readonly byte[] SScan = "SSCAN".ToUtf8Bytes();
    public static readonly byte[] HScan = "HSCAN".ToUtf8Bytes();
    public static readonly byte[] ZScan = "ZSCAN".ToUtf8Bytes();
    public static readonly byte[] Match = "MATCH".ToUtf8Bytes();
    public static readonly byte[] Count = "COUNT".ToUtf8Bytes();

    public static readonly byte[] PfAdd = "PFADD".ToUtf8Bytes();
    public static readonly byte[] PfCount = "PFCOUNT".ToUtf8Bytes();
    public static readonly byte[] PfMerge = "PFMERGE".ToUtf8Bytes();

    public static readonly byte[] RPush = "RPUSH".ToUtf8Bytes();
    public static readonly byte[] LPush = "LPUSH".ToUtf8Bytes();
    public static readonly byte[] RPushX = "RPUSHX".ToUtf8Bytes();
    public static readonly byte[] LPushX = "LPUSHX".ToUtf8Bytes();
    public static readonly byte[] LLen = "LLEN".ToUtf8Bytes();
    public static readonly byte[] LRange = "LRANGE".ToUtf8Bytes();
    public static readonly byte[] LTrim = "LTRIM".ToUtf8Bytes();
    public static readonly byte[] LIndex = "LINDEX".ToUtf8Bytes();
    public static readonly byte[] LInsert = "LINSERT".ToUtf8Bytes();
    public static readonly byte[] Before = "BEFORE".ToUtf8Bytes();
    public static readonly byte[] After = "AFTER".ToUtf8Bytes();
    public static readonly byte[] LSet = "LSET".ToUtf8Bytes();
    public static readonly byte[] LRem = "LREM".ToUtf8Bytes();
    public static readonly byte[] LPop = "LPOP".ToUtf8Bytes();
    public static readonly byte[] RPop = "RPOP".ToUtf8Bytes();
    public static readonly byte[] BLPop = "BLPOP".ToUtf8Bytes();
    public static readonly byte[] BRPop = "BRPOP".ToUtf8Bytes();
    public static readonly byte[] RPopLPush = "RPOPLPUSH".ToUtf8Bytes();
    public static readonly byte[] BRPopLPush = "BRPOPLPUSH".ToUtf8Bytes();

    public static readonly byte[] SAdd = "SADD".ToUtf8Bytes();
    public static readonly byte[] SRem = "SREM".ToUtf8Bytes();
    public static readonly byte[] SPop = "SPOP".ToUtf8Bytes();
    public static readonly byte[] SMove = "SMOVE".ToUtf8Bytes();
    public static readonly byte[] SCard = "SCARD".ToUtf8Bytes();
    public static readonly byte[] SIsMember = "SISMEMBER".ToUtf8Bytes();
    public static readonly byte[] SInter = "SINTER".ToUtf8Bytes();
    public static readonly byte[] SInterStore = "SINTERSTORE".ToUtf8Bytes();
    public static readonly byte[] SUnion = "SUNION".ToUtf8Bytes();
    public static readonly byte[] SUnionStore = "SUNIONSTORE".ToUtf8Bytes();
    public static readonly byte[] SDiff = "SDIFF".ToUtf8Bytes();
    public static readonly byte[] SDiffStore = "SDIFFSTORE".ToUtf8Bytes();
    public static readonly byte[] SMembers = "SMEMBERS".ToUtf8Bytes();
    public static readonly byte[] SRandMember = "SRANDMEMBER".ToUtf8Bytes();

    public static readonly byte[] ZAdd = "ZADD".ToUtf8Bytes();
    public static readonly byte[] ZRem = "ZREM".ToUtf8Bytes();
    public static readonly byte[] ZIncrBy = "ZINCRBY".ToUtf8Bytes();
    public static readonly byte[] ZRank = "ZRANK".ToUtf8Bytes();
    public static readonly byte[] ZRevRank = "ZREVRANK".ToUtf8Bytes();
    public static readonly byte[] ZRange = "ZRANGE".ToUtf8Bytes();
    public static readonly byte[] ZRevRange = "ZREVRANGE".ToUtf8Bytes();
    public static readonly byte[] ZRangeByScore = "ZRANGEBYSCORE".ToUtf8Bytes();
    public static readonly byte[] ZRevRangeByScore = "ZREVRANGEBYSCORE".ToUtf8Bytes();
    public static readonly byte[] ZCard = "ZCARD".ToUtf8Bytes();
    public static readonly byte[] ZScore = "ZSCORE".ToUtf8Bytes();
    public static readonly byte[] ZCount = "ZCOUNT".ToUtf8Bytes();
    public static readonly byte[] ZRemRangeByRank = "ZREMRANGEBYRANK".ToUtf8Bytes();
    public static readonly byte[] ZRemRangeByScore = "ZREMRANGEBYSCORE".ToUtf8Bytes();
    public static readonly byte[] ZUnionStore = "ZUNIONSTORE".ToUtf8Bytes();
    public static readonly byte[] ZInterStore = "ZINTERSTORE".ToUtf8Bytes();
    public static readonly byte[] ZRangeByLex = "ZRANGEBYLEX".ToUtf8Bytes();
    public static readonly byte[] ZLexCount = "ZLEXCOUNT".ToUtf8Bytes();
    public static readonly byte[] ZRemRangeByLex = "ZREMRANGEBYLEX".ToUtf8Bytes();

    public static readonly byte[] HSet = "HSET".ToUtf8Bytes();
    public static readonly byte[] HSetNx = "HSETNX".ToUtf8Bytes();
    public static readonly byte[] HGet = "HGET".ToUtf8Bytes();
    public static readonly byte[] HMSet = "HMSET".ToUtf8Bytes();
    public static readonly byte[] HMGet = "HMGET".ToUtf8Bytes();
    public static readonly byte[] HIncrBy = "HINCRBY".ToUtf8Bytes();
    public static readonly byte[] HIncrByFloat = "HINCRBYFLOAT".ToUtf8Bytes();
    public static readonly byte[] HExists = "HEXISTS".ToUtf8Bytes();
    public static readonly byte[] HDel = "HDEL".ToUtf8Bytes();
    public static readonly byte[] HLen = "HLEN".ToUtf8Bytes();
    public static readonly byte[] HKeys = "HKEYS".ToUtf8Bytes();
    public static readonly byte[] HVals = "HVALS".ToUtf8Bytes();
    public static readonly byte[] HGetAll = "HGETALL".ToUtf8Bytes();

    public static readonly byte[] Sort = "SORT".ToUtf8Bytes();

    public static readonly byte[] Watch = "WATCH".ToUtf8Bytes();
    public static readonly byte[] UnWatch = "UNWATCH".ToUtf8Bytes();
    public static readonly byte[] Multi = "MULTI".ToUtf8Bytes();
    public static readonly byte[] Exec = "EXEC".ToUtf8Bytes();
    public static readonly byte[] Discard = "DISCARD".ToUtf8Bytes();

    public static readonly byte[] Subscribe = "SUBSCRIBE".ToUtf8Bytes();
    public static readonly byte[] UnSubscribe = "UNSUBSCRIBE".ToUtf8Bytes();
    public static readonly byte[] PSubscribe = "PSUBSCRIBE".ToUtf8Bytes();
    public static readonly byte[] PUnSubscribe = "PUNSUBSCRIBE".ToUtf8Bytes();
    public static readonly byte[] Publish = "PUBLISH".ToUtf8Bytes();


    public static readonly byte[] WithScores = "WITHSCORES".ToUtf8Bytes();
    public static readonly byte[] Limit = "LIMIT".ToUtf8Bytes();
    public static readonly byte[] By = "BY".ToUtf8Bytes();
    public static readonly byte[] Asc = "ASC".ToUtf8Bytes();
    public static readonly byte[] Desc = "DESC".ToUtf8Bytes();
    public static readonly byte[] Alpha = "ALPHA".ToUtf8Bytes();
    public static readonly byte[] Store = "STORE".ToUtf8Bytes();

    public static readonly byte[] Eval = "EVAL".ToUtf8Bytes();
    public static readonly byte[] EvalSha = "EVALSHA".ToUtf8Bytes();
    public static readonly byte[] Script = "SCRIPT".ToUtf8Bytes();
    public static readonly byte[] Load = "LOAD".ToUtf8Bytes();
    //public static readonly byte[] Exists = "EXISTS".ToUtf8Bytes();
    public static readonly byte[] Flush = "FLUSH".ToUtf8Bytes();
    public static readonly byte[] Slowlog = "SLOWLOG".ToUtf8Bytes();

    public static readonly byte[] Ex = "EX".ToUtf8Bytes();
    public static readonly byte[] Px = "PX".ToUtf8Bytes();
    public static readonly byte[] Nx = "NX".ToUtf8Bytes();
    public static readonly byte[] Xx = "XX".ToUtf8Bytes();

    // Sentinel commands
    public static readonly byte[] Sentinel = "SENTINEL".ToUtf8Bytes();
    public static readonly byte[] Masters = "masters".ToUtf8Bytes();
    public static readonly byte[] Sentinels = "sentinels".ToUtf8Bytes();
    public static readonly byte[] Master = "master".ToUtf8Bytes();
    public static readonly byte[] Slaves = "slaves".ToUtf8Bytes();
    public static readonly byte[] Failover = "failover".ToUtf8Bytes();
    public static readonly byte[] GetMasterAddrByName = "get-master-addr-by-name".ToUtf8Bytes();

    //Geo commands
    public static readonly byte[] GeoAdd = "GEOADD".ToUtf8Bytes();
    public static readonly byte[] GeoDist = "GEODIST".ToUtf8Bytes();
    public static readonly byte[] GeoHash = "GEOHASH".ToUtf8Bytes();
    public static readonly byte[] GeoPos = "GEOPOS".ToUtf8Bytes();
    public static readonly byte[] GeoRadius = "GEORADIUS".ToUtf8Bytes();
    public static readonly byte[] GeoRadiusByMember = "GEORADIUSBYMEMBER".ToUtf8Bytes();

    public static readonly byte[] WithCoord = "WITHCOORD".ToUtf8Bytes();
    public static readonly byte[] WithDist = "WITHDIST".ToUtf8Bytes();
    public static readonly byte[] WithHash = "WITHHASH".ToUtf8Bytes();

    public static readonly byte[] Meters = RedisGeoUnit.Meters.ToUtf8Bytes();
    public static readonly byte[] Kilometers = RedisGeoUnit.Kilometers.ToUtf8Bytes();
    public static readonly byte[] Miles = RedisGeoUnit.Miles.ToUtf8Bytes();
    public static readonly byte[] Feet = RedisGeoUnit.Feet.ToUtf8Bytes();

    public static byte[] GetUnit(string unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        return unit switch
        {
            RedisGeoUnit.Meters => Meters,
            RedisGeoUnit.Kilometers => Kilometers,
            RedisGeoUnit.Miles => Miles,
            RedisGeoUnit.Feet => Feet,
            _ => throw new NotSupportedException($"Unit '{unit}' is not a valid unit")
        };
    }
}