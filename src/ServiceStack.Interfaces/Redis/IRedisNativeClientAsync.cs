//
// https://github.com/ServiceStack/ServiceStack.Redis/
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2017 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis
{
    public interface IRedisNativeClientAsync : IAsyncDisposable
    {
        /*
         non-obvious changes:
        - Db is get only; addition of SelectAsync
        - LastSave is now a method
        - shutdown now takes nosave arg
        - expose the optional args on Set
        - add SlowlogGet and SlowlogReset
        - add ZCount
         */

        //Redis utility operations
        ValueTask<Dictionary<string, string>> InfoAsync(CancellationToken cancellationToken = default);
        long Db { get; }
        ValueTask SelectAsync(long db, CancellationToken cancellationToken = default);

        ValueTask<long> DbSizeAsync(CancellationToken cancellationToken = default);
        ValueTask<DateTime> LastSaveAsync(CancellationToken cancellationToken = default);
        ValueTask SaveAsync(CancellationToken cancellationToken = default);
        ValueTask BgSaveAsync(CancellationToken cancellationToken = default);
        ValueTask ShutdownAsync(bool noSave = false, CancellationToken cancellationToken = default);
        ValueTask BgRewriteAofAsync(CancellationToken cancellationToken = default);
        ValueTask QuitAsync(CancellationToken cancellationToken = default);
        ValueTask FlushDbAsync(CancellationToken cancellationToken = default);
        ValueTask FlushAllAsync(CancellationToken cancellationToken = default);
        ValueTask<bool> PingAsync(CancellationToken cancellationToken = default);
        ValueTask<string> EchoAsync(string text, CancellationToken cancellationToken = default);
        ValueTask SlaveOfAsync(string hostname, int port, CancellationToken cancellationToken = default);
        ValueTask SlaveOfNoOneAsync(CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ConfigGetAsync(string pattern, CancellationToken cancellationToken = default);
        ValueTask ConfigSetAsync(string item, byte[] value, CancellationToken cancellationToken = default);
        ValueTask ConfigResetStatAsync(CancellationToken cancellationToken = default);
        ValueTask ConfigRewriteAsync(CancellationToken cancellationToken = default);
        ValueTask<byte[][]> TimeAsync(CancellationToken cancellationToken = default);
        ValueTask DebugSegfaultAsync(CancellationToken cancellationToken = default);
        ValueTask<byte[]> DumpAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<byte[]> RestoreAsync(string key, long expireMs, byte[] dumpValue, CancellationToken cancellationToken = default);
        ValueTask MigrateAsync(string host, int port, string key, int destinationDb, long timeoutMs, CancellationToken cancellationToken = default);
        ValueTask<bool> MoveAsync(string key, int db, CancellationToken cancellationToken = default);
        ValueTask<long> ObjectIdleTimeAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<RedisText> RoleAsync(CancellationToken cancellationToken = default);

        ValueTask<RedisData> RawCommandAsync(object[] cmdWithArgs, CancellationToken cancellationToken = default);
        ValueTask<RedisData> RawCommandAsync(params object[] cmdWithArgs); // convenience API
        ValueTask<RedisData> RawCommandAsync(byte[][] cmdWithBinaryArgs, CancellationToken cancellationToken = default);
        ValueTask<RedisData> RawCommandAsync(params byte[][] cmdWithBinaryArgs); // convenience API

        ValueTask<string> ClientGetNameAsync(CancellationToken cancellationToken = default);
        ValueTask ClientSetNameAsync(string client, CancellationToken cancellationToken = default);
        ValueTask ClientKillAsync(string host, CancellationToken cancellationToken = default);
        ValueTask<long> ClientKillAsync(string addr = null, string id = null, string type = null, string skipMe = null, CancellationToken cancellationToken = default);
        ValueTask<byte[]> ClientListAsync(CancellationToken cancellationToken = default);
        ValueTask ClientPauseAsync(int timeOutMs, CancellationToken cancellationToken = default);

        //Common key-value Redis operations
        ValueTask<byte[][]> KeysAsync(string pattern, CancellationToken cancellationToken = default);
        ValueTask<string> TypeAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<long> ExistsAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<long> StrLenAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<bool> SetAsync(string key, byte[] value, bool exists, long expirySeconds = 0, long expiryMilliseconds = 0, CancellationToken cancellationToken = default);
        ValueTask SetAsync(string key, byte[] value, long expirySeconds = 0, long expiryMilliseconds = 0, CancellationToken cancellationToken = default);
        ValueTask SetExAsync(string key, int expireInSeconds, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<bool> PersistAsync(string key, CancellationToken cancellationToken = default);
        ValueTask PSetExAsync(string key, long expireInMs, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> SetNXAsync(string key, byte[] value, CancellationToken cancellationToken = default);
        ValueTask MSetAsync(byte[][] keys, byte[][] values, CancellationToken cancellationToken = default);
        ValueTask MSetAsync(string[] keys, byte[][] values, CancellationToken cancellationToken = default);
        ValueTask<bool> MSetNxAsync(byte[][] keys, byte[][] values, CancellationToken cancellationToken = default);
        ValueTask<bool> MSetNxAsync(string[] keys, byte[][] values, CancellationToken cancellationToken = default);
        ValueTask<byte[]> GetAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<byte[]> GetSetAsync(string key, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> MGetAsync(byte[][] keysAndArgs, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> MGetAsync(params byte[][] keysAndArgs); // convenience API
        ValueTask<byte[][]> MGetAsync(string[] keys, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> MGetAsync(params string[] keys); // convenience API
        ValueTask<long> DelAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<long> DelAsync(string[] keys, CancellationToken cancellationToken = default);
        ValueTask<long> DelAsync(params string[] keys); // convenience API
        ValueTask<long> IncrAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<long> IncrByAsync(string key, long incrBy, CancellationToken cancellationToken = default);
        ValueTask<double> IncrByFloatAsync(string key, double incrBy, CancellationToken cancellationToken = default);
        ValueTask<long> DecrAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<long> DecrByAsync(string key, long decrBy, CancellationToken cancellationToken = default);
        ValueTask<long> AppendAsync(string key, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<byte[]> GetRangeAsync(string key, int fromIndex, int toIndex, CancellationToken cancellationToken = default);
        ValueTask<long> SetRangeAsync(string key, int offset, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> GetBitAsync(string key, int offset, CancellationToken cancellationToken = default);
        ValueTask<long> SetBitAsync(string key, int offset, int value, CancellationToken cancellationToken = default);
        ValueTask<long> BitCountAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<string> RandomKeyAsync(CancellationToken cancellationToken = default);
        ValueTask RenameAsync(string oldKeyname, string newKeyname, CancellationToken cancellationToken = default);
        ValueTask<bool> RenameNxAsync(string oldKeyname, string newKeyname, CancellationToken cancellationToken = default);
        ValueTask<bool> ExpireAsync(string key, int seconds, CancellationToken cancellationToken = default);
        ValueTask<bool> PExpireAsync(string key, long ttlMs, CancellationToken cancellationToken = default);
        ValueTask<bool> ExpireAtAsync(string key, long unixTime, CancellationToken cancellationToken = default);
        ValueTask<bool> PExpireAtAsync(string key, long unixTimeMs, CancellationToken cancellationToken = default);
        ValueTask<long> TtlAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<long> PTtlAsync(string key, CancellationToken cancellationToken = default);

        //Scan APIs
        ValueTask<ScanResult> ScanAsync(ulong cursor, int count = 10, string match = null, CancellationToken cancellationToken = default);
        ValueTask<ScanResult> SScanAsync(string setId, ulong cursor, int count = 10, string match = null, CancellationToken cancellationToken = default);
        ValueTask<ScanResult> ZScanAsync(string setId, ulong cursor, int count = 10, string match = null, CancellationToken cancellationToken = default);
        ValueTask<ScanResult> HScanAsync(string hashId, ulong cursor, int count = 10, string match = null, CancellationToken cancellationToken = default);

        //Hyperlog
        ValueTask<bool> PfAddAsync(string key, byte[][] elements, CancellationToken cancellationToken = default);
        ValueTask<bool> PfAddAsync(string key, params byte[][] elements); // convenience API
        ValueTask<long> PfCountAsync(string key, CancellationToken cancellationToken = default);
        ValueTask PfMergeAsync(string toKeyId, string[] fromKeys, CancellationToken cancellationToken = default);
        ValueTask PfMergeAsync(string toKeyId, params string[] fromKeys);

        //Redis Sort operation Async(works on lists, sets or hashes)
        ValueTask<byte[][]> SortAsync(string listOrSetId, SortOptions sortOptions, CancellationToken cancellationToken = default);

        //Redis List operations
        ValueTask<byte[][]> LRangeAsync(string listId, int startingFrom, int endingAt, CancellationToken cancellationToken = default);
        ValueTask<long> RPushAsync(string listId, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> RPushXAsync(string listId, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> LPushAsync(string listId, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> LPushXAsync(string listId, byte[] value, CancellationToken cancellationToken = default);
        ValueTask LTrimAsync(string listId, int keepStartingFrom, int keepEndingAt, CancellationToken cancellationToken = default);
        ValueTask<long> LRemAsync(string listId, int removeNoOfMatches, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> LLenAsync(string listId, CancellationToken cancellationToken = default);
        ValueTask<byte[]> LIndexAsync(string listId, int listIndex, CancellationToken cancellationToken = default);
        ValueTask LInsertAsync(string listId, bool insertBefore, byte[] pivot, byte[] value, CancellationToken cancellationToken = default);
        ValueTask LSetAsync(string listId, int listIndex, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<byte[]> LPopAsync(string listId, CancellationToken cancellationToken = default);
        ValueTask<byte[]> RPopAsync(string listId, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> BLPopAsync(string listId, int timeOutSecs, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> BLPopAsync(string[] listIds, int timeOutSecs, CancellationToken cancellationToken = default);
        ValueTask<byte[]> BLPopValueAsync(string listId, int timeOutSecs, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> BLPopValueAsync(string[] listIds, int timeOutSecs, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> BRPopAsync(string listId, int timeOutSecs, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> BRPopAsync(string[] listIds, int timeOutSecs, CancellationToken cancellationToken = default);
        ValueTask<byte[]> RPopLPushAsync(string fromListId, string toListId, CancellationToken cancellationToken = default);
        ValueTask<byte[]> BRPopValueAsync(string listId, int timeOutSecs, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> BRPopValueAsync(string[] listIds, int timeOutSecs, CancellationToken cancellationToken = default);
        ValueTask<byte[]> BRPopLPushAsync(string fromListId, string toListId, int timeOutSecs, CancellationToken cancellationToken = default);

        //Redis Set operations
        ValueTask<byte[][]> SMembersAsync(string setId, CancellationToken cancellationToken = default);
        ValueTask<long> SAddAsync(string setId, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> SAddAsync(string setId, byte[][] value, CancellationToken cancellationToken = default);
        ValueTask<long> SRemAsync(string setId, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<byte[]> SPopAsync(string setId, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> SPopAsync(string setId, int count, CancellationToken cancellationToken = default);
        ValueTask SMoveAsync(string fromSetId, string toSetId, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> SCardAsync(string setId, CancellationToken cancellationToken = default);
        ValueTask<long> SIsMemberAsync(string setId, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> SInterAsync(string[] setIds, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> SInterAsync(params string[] setIds); // convenience API
        ValueTask SInterStoreAsync(string intoSetId, string[] setIds, CancellationToken cancellationToken = default);
        ValueTask SInterStoreAsync(string intoSetId, params string[] setIds); // convenience API
        ValueTask<byte[][]> SUnionAsync(string[] setIds, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> SUnionAsync(params string[] setIds); // convenience API
        ValueTask SUnionStoreAsync(string intoSetId, string[] setIds, CancellationToken cancellationToken = default);
        ValueTask SUnionStoreAsync(string intoSetId, params string[] setIds); // convenience API
        ValueTask<byte[][]> SDiffAsync(string fromSetId, string[] withSetIds, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> SDiffAsync(string fromSetId, params string[] withSetIds); // convenience API
        ValueTask SDiffStoreAsync(string intoSetId, string fromSetId, string[] withSetIds, CancellationToken cancellationToken = default);
        ValueTask SDiffStoreAsync(string intoSetId, string fromSetId, params string[] withSetIds); // convenience API
        ValueTask<byte[]> SRandMemberAsync(string setId, CancellationToken cancellationToken = default);


        ////Redis Sorted Set operations
        ValueTask<long> ZAddAsync(string setId, double score, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> ZAddAsync(string setId, long score, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> ZRemAsync(string setId, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> ZRemAsync(string setId, byte[][] values, CancellationToken cancellationToken = default);
        ValueTask<double> ZIncrByAsync(string setId, double incrBy, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<double> ZIncrByAsync(string setId, long incrBy, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> ZRankAsync(string setId, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> ZRevRankAsync(string setId, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ZRangeAsync(string setId, int min, int max, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ZRangeWithScoresAsync(string setId, int min, int max, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ZRevRangeAsync(string setId, int min, int max, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ZRevRangeWithScoresAsync(string setId, int min, int max, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ZRangeByScoreAsync(string setId, double min, double max, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ZRangeByScoreAsync(string setId, long min, long max, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ZRangeByScoreWithScoresAsync(string setId, double min, double max, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ZRangeByScoreWithScoresAsync(string setId, long min, long max, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ZRevRangeByScoreAsync(string setId, double min, double max, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ZRevRangeByScoreAsync(string setId, long min, long max, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ZRevRangeByScoreWithScoresAsync(string setId, double min, double max, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ZRevRangeByScoreWithScoresAsync(string setId, long min, long max, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<long> ZRemRangeByRankAsync(string setId, int min, int max, CancellationToken cancellationToken = default);
        ValueTask<long> ZRemRangeByScoreAsync(string setId, double fromScore, double toScore, CancellationToken cancellationToken = default);
        ValueTask<long> ZRemRangeByScoreAsync(string setId, long fromScore, long toScore, CancellationToken cancellationToken = default);
        ValueTask<long> ZCardAsync(string setId, CancellationToken cancellationToken = default);
        ValueTask<long> ZCountAsync(string setId, double min, double max, CancellationToken cancellationToken = default);
        ValueTask<double>  ZScoreAsync(string setId, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> ZUnionStoreAsync(string intoSetId, string[] setIds, CancellationToken cancellationToken = default);
        ValueTask<long> ZUnionStoreAsync(string intoSetId, params string[] setIds); // convenience API
        ValueTask<long> ZInterStoreAsync(string intoSetId, string[] setIds, CancellationToken cancellationToken = default);
        ValueTask<long> ZInterStoreAsync(string intoSetId, params string[] setIds); // convenience API
        ValueTask<byte[][]> ZRangeByLexAsync(string setId, string min, string max, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
        ValueTask<long> ZLexCountAsync(string setId, string min, string max, CancellationToken cancellationToken = default);
        ValueTask<long> ZRemRangeByLexAsync(string setId, string min, string max, CancellationToken cancellationToken = default);

        ////Redis Hash operations
        ValueTask<long> HSetAsync(string hashId, byte[] key, byte[] value, CancellationToken cancellationToken = default);
        ValueTask HMSetAsync(string hashId, byte[][] keys, byte[][] values, CancellationToken cancellationToken = default);
        ValueTask<long> HSetNXAsync(string hashId, byte[] key, byte[] value, CancellationToken cancellationToken = default);
        ValueTask<long> HIncrbyAsync(string hashId, byte[] key, int incrementBy, CancellationToken cancellationToken = default);
        ValueTask<double> HIncrbyFloatAsync(string hashId, byte[] key, double incrementBy, CancellationToken cancellationToken = default);
        ValueTask<byte[]> HGetAsync(string hashId, byte[] key, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> HMGetAsync(string hashId, byte[][] keysAndArgs, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> HMGetAsync(string hashId, params byte[][] keysAndArgs); // convenience API
        ValueTask<long> HDelAsync(string hashId, byte[] key, CancellationToken cancellationToken = default);
        ValueTask<long> HExistsAsync(string hashId, byte[] key, CancellationToken cancellationToken = default);
        ValueTask<long> HLenAsync(string hashId, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> HKeysAsync(string hashId, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> HValsAsync(string hashId, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> HGetAllAsync(string hashId, CancellationToken cancellationToken = default);

        //Redis GEO operations
        ValueTask<long> GeoAddAsync(string key, double longitude, double latitude, string member, CancellationToken cancellationToken = default);
        ValueTask<long> GeoAddAsync(string key, RedisGeo[] geoPoints, CancellationToken cancellationToken = default);
        ValueTask<long> GeoAddAsync(string key, params RedisGeo[] geoPoints); // convenience API
        ValueTask<double> GeoDistAsync(string key, string fromMember, string toMember, string unit = null, CancellationToken cancellationToken = default);
        ValueTask<string[]> GeoHashAsync(string key, string[] members, CancellationToken cancellationToken = default);
        ValueTask<string[]> GeoHashAsync(string key, params string[] members); // convenience API
        ValueTask<List<RedisGeo>> GeoPosAsync(string key, string[] members, CancellationToken cancellationToken = default);
        ValueTask<List<RedisGeo>> GeoPosAsync(string key, params string[] members); // convenience API
        ValueTask<List<RedisGeoResult>> GeoRadiusAsync(string key, double longitude, double latitude, double radius, string unit,
            bool withCoords = false, bool withDist = false, bool withHash = false, int? count = null, bool? asc = null, CancellationToken cancellationToken = default);
        ValueTask<List<RedisGeoResult>> GeoRadiusByMemberAsync(string key, string member, double radius, string unit,
            bool withCoords = false, bool withDist = false, bool withHash = false, int? count = null, bool? asc = null, CancellationToken cancellationToken = default);

        //Redis Pub/Sub operations
        ValueTask WatchAsync(string[] keys, CancellationToken cancellationToken = default);
        ValueTask WatchAsync(params string[] keys); // convenience API
        ValueTask UnWatchAsync(CancellationToken cancellationToken = default);
        ValueTask<long> PublishAsync(string toChannel, byte[] message, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> SubscribeAsync(string[] toChannels, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> SubscribeAsync(params string[] toChannels); // convenience API
        ValueTask<byte[][]> UnSubscribeAsync(string[] toChannels, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> UnSubscribeAsync(params string[] toChannels); // convenience API
        ValueTask<byte[][]> PSubscribeAsync(string[] toChannelsMatchingPatterns, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> PSubscribeAsync(params string[] toChannelsMatchingPatterns); // convenience API
        ValueTask<byte[][]> PUnSubscribeAsync(string[] toChannelsMatchingPatterns, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> PUnSubscribeAsync(params string[] toChannelsMatchingPatterns); // convenience API
        ValueTask<byte[][]> ReceiveMessagesAsync(CancellationToken cancellationToken = default);
        ValueTask<IRedisSubscriptionAsync> CreateSubscriptionAsync(CancellationToken cancellationToken = default);

        //Redis LUA support
        ValueTask<RedisData> EvalCommandAsync(string luaBody, int numberKeysInArgs, byte[][] keys, CancellationToken cancellationToken = default);
        ValueTask<RedisData> EvalCommandAsync(string luaBody, int numberKeysInArgs, params byte[][] keys); // convenience API
        ValueTask<RedisData> EvalShaCommandAsync(string sha1, int numberKeysInArgs, byte[][] keys, CancellationToken cancellationToken = default);
        ValueTask<RedisData> EvalShaCommandAsync(string sha1, int numberKeysInArgs, params byte[][] keys); // convenience API

        ValueTask<byte[][]> EvalAsync(string luaBody, int numberOfKeys, byte[][] keysAndArgs, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> EvalAsync(string luaBody, int numberOfKeys, params byte[][] keysAndArgs); // convenience API
        ValueTask<byte[][]> EvalShaAsync(string sha1, int numberOfKeys, byte[][] keysAndArgs, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> EvalShaAsync(string sha1, int numberOfKeys, params byte[][] keysAndArgs); // convenience API

        ValueTask<long> EvalIntAsync(string luaBody, int numberOfKeys, byte[][] keysAndArgs, CancellationToken cancellationToken = default);
        ValueTask<long> EvalIntAsync(string luaBody, int numberOfKeys, params byte[][] keysAndArgs); // convenience API
        ValueTask<long> EvalShaIntAsync(string sha1, int numberOfKeys, byte[][] keysAndArgs, CancellationToken cancellationToken = default);
        ValueTask<long> EvalShaIntAsync(string sha1, int numberOfKeys, params byte[][] keysAndArgs); // convenience API
        ValueTask<string> EvalStrAsync(string luaBody, int numberOfKeys, byte[][] keysAndArgs, CancellationToken cancellationToken = default);
        ValueTask<string> EvalStrAsync(string luaBody, int numberOfKeys, params byte[][] keysAndArgs); // convenience API
        ValueTask<string> EvalShaStrAsync(string sha1, int numberOfKeys, byte[][] keysAndArgs, CancellationToken cancellationToken = default);
        ValueTask<string> EvalShaStrAsync(string sha1, int numberOfKeys, params byte[][] keysAndArgs); // convenience API

        ValueTask<string> CalculateSha1Async(string luaBody, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ScriptExistsAsync(byte[][] sha1Refs, CancellationToken cancellationToken = default);
        ValueTask<byte[][]> ScriptExistsAsync(params byte[][] sha1Refs); // convenience API
        ValueTask ScriptFlushAsync(CancellationToken cancellationToken = default);
        ValueTask ScriptKillAsync(CancellationToken cancellationToken = default);
        ValueTask<byte[]> ScriptLoadAsync(string body, CancellationToken cancellationToken = default);

        ValueTask SlowlogResetAsync(CancellationToken cancellationToken = default);
        ValueTask<object[]> SlowlogGetAsync(int? top = null, CancellationToken cancellationToken = default);
    }
}