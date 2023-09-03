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

namespace ServiceStack.Redis;

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
    ValueTask<Dictionary<string, string>> InfoAsync(CancellationToken token = default);
    long Db { get; }
    ValueTask SelectAsync(long db, CancellationToken token = default);

    ValueTask<long> DbSizeAsync(CancellationToken token = default);
    ValueTask<DateTime> LastSaveAsync(CancellationToken token = default);
    ValueTask SaveAsync(CancellationToken token = default);
    ValueTask BgSaveAsync(CancellationToken token = default);
    ValueTask ShutdownAsync(bool noSave = false, CancellationToken token = default);
    ValueTask BgRewriteAofAsync(CancellationToken token = default);
    ValueTask QuitAsync(CancellationToken token = default);
    ValueTask FlushDbAsync(CancellationToken token = default);
    ValueTask FlushAllAsync(CancellationToken token = default);
    ValueTask<bool> PingAsync(CancellationToken token = default);
    ValueTask<string> EchoAsync(string text, CancellationToken token = default);
    ValueTask SlaveOfAsync(string hostname, int port, CancellationToken token = default);
    ValueTask SlaveOfNoOneAsync(CancellationToken token = default);
    ValueTask<byte[][]> ConfigGetAsync(string pattern, CancellationToken token = default);
    ValueTask ConfigSetAsync(string item, byte[] value, CancellationToken token = default);
    ValueTask ConfigResetStatAsync(CancellationToken token = default);
    ValueTask ConfigRewriteAsync(CancellationToken token = default);
    ValueTask<byte[][]> TimeAsync(CancellationToken token = default);
    ValueTask DebugSegfaultAsync(CancellationToken token = default);
    ValueTask<byte[]> DumpAsync(string key, CancellationToken token = default);
    ValueTask<byte[]> RestoreAsync(string key, long expireMs, byte[] dumpValue, CancellationToken token = default);
    ValueTask MigrateAsync(string host, int port, string key, int destinationDb, long timeoutMs, CancellationToken token = default);
    ValueTask<bool> MoveAsync(string key, int db, CancellationToken token = default);
    ValueTask<long> ObjectIdleTimeAsync(string key, CancellationToken token = default);
    ValueTask<RedisText> RoleAsync(CancellationToken token = default);

    ValueTask<RedisData> RawCommandAsync(object[] cmdWithArgs, CancellationToken token = default);
    ValueTask<RedisData> RawCommandAsync(params object[] cmdWithArgs); // convenience API
    ValueTask<RedisData> RawCommandAsync(byte[][] cmdWithBinaryArgs, CancellationToken token = default);
    ValueTask<RedisData> RawCommandAsync(params byte[][] cmdWithBinaryArgs); // convenience API

    ValueTask<string> ClientGetNameAsync(CancellationToken token = default);
    ValueTask ClientSetNameAsync(string client, CancellationToken token = default);
    ValueTask ClientKillAsync(string host, CancellationToken token = default);
    ValueTask<long> ClientKillAsync(string addr = null, string id = null, string type = null, string skipMe = null, CancellationToken token = default);
    ValueTask<byte[]> ClientListAsync(CancellationToken token = default);
    ValueTask ClientPauseAsync(int timeOutMs, CancellationToken token = default);

    //Common key-value Redis operations
    ValueTask<byte[][]> KeysAsync(string pattern, CancellationToken token = default);
    ValueTask<string> TypeAsync(string key, CancellationToken token = default);
    ValueTask<long> ExistsAsync(string key, CancellationToken token = default);
    ValueTask<long> StrLenAsync(string key, CancellationToken token = default);
    ValueTask<bool> SetAsync(string key, byte[] value, bool exists, long expirySeconds = 0, long expiryMilliseconds = 0, CancellationToken token = default);
    ValueTask SetAsync(string key, byte[] value, long expirySeconds = 0, long expiryMilliseconds = 0, CancellationToken token = default);
    ValueTask SetExAsync(string key, int expireInSeconds, byte[] value, CancellationToken token = default);
    ValueTask<bool> PersistAsync(string key, CancellationToken token = default);
    ValueTask PSetExAsync(string key, long expireInMs, byte[] value, CancellationToken token = default);
    ValueTask<long> SetNXAsync(string key, byte[] value, CancellationToken token = default);
    ValueTask MSetAsync(byte[][] keys, byte[][] values, CancellationToken token = default);
    ValueTask MSetAsync(string[] keys, byte[][] values, CancellationToken token = default);
    ValueTask<bool> MSetNxAsync(byte[][] keys, byte[][] values, CancellationToken token = default);
    ValueTask<bool> MSetNxAsync(string[] keys, byte[][] values, CancellationToken token = default);
    ValueTask<byte[]> GetAsync(string key, CancellationToken token = default);
    ValueTask<byte[]> GetSetAsync(string key, byte[] value, CancellationToken token = default);
    ValueTask<byte[][]> MGetAsync(byte[][] keysAndArgs, CancellationToken token = default);
    ValueTask<byte[][]> MGetAsync(params byte[][] keysAndArgs); // convenience API
    ValueTask<byte[][]> MGetAsync(string[] keys, CancellationToken token = default);
    ValueTask<byte[][]> MGetAsync(params string[] keys); // convenience API
    ValueTask<long> DelAsync(string key, CancellationToken token = default);
    ValueTask<long> DelAsync(string[] keys, CancellationToken token = default);
    ValueTask<long> DelAsync(params string[] keys); // convenience API
    ValueTask<long> IncrAsync(string key, CancellationToken token = default);
    ValueTask<long> IncrByAsync(string key, long incrBy, CancellationToken token = default);
    ValueTask<double> IncrByFloatAsync(string key, double incrBy, CancellationToken token = default);
    ValueTask<long> DecrAsync(string key, CancellationToken token = default);
    ValueTask<long> DecrByAsync(string key, long decrBy, CancellationToken token = default);
    ValueTask<long> AppendAsync(string key, byte[] value, CancellationToken token = default);
    ValueTask<byte[]> GetRangeAsync(string key, int fromIndex, int toIndex, CancellationToken token = default);
    ValueTask<long> SetRangeAsync(string key, int offset, byte[] value, CancellationToken token = default);
    ValueTask<long> GetBitAsync(string key, int offset, CancellationToken token = default);
    ValueTask<long> SetBitAsync(string key, int offset, int value, CancellationToken token = default);
    ValueTask<long> BitCountAsync(string key, CancellationToken token = default);
    ValueTask<string> RandomKeyAsync(CancellationToken token = default);
    ValueTask RenameAsync(string oldKeyName, string newKeyName, CancellationToken token = default);
    ValueTask<bool> RenameNxAsync(string oldKeyName, string newKeyName, CancellationToken token = default);
    ValueTask<bool> ExpireAsync(string key, int seconds, CancellationToken token = default);
    ValueTask<bool> PExpireAsync(string key, long ttlMs, CancellationToken token = default);
    ValueTask<bool> ExpireAtAsync(string key, long unixTime, CancellationToken token = default);
    ValueTask<bool> PExpireAtAsync(string key, long unixTimeMs, CancellationToken token = default);
    ValueTask<long> TtlAsync(string key, CancellationToken token = default);
    ValueTask<long> PTtlAsync(string key, CancellationToken token = default);

    //Scan APIs
    ValueTask<ScanResult> ScanAsync(ulong cursor, int count = 10, string match = null, CancellationToken token = default);
    ValueTask<ScanResult> SScanAsync(string setId, ulong cursor, int count = 10, string match = null, CancellationToken token = default);
    ValueTask<ScanResult> ZScanAsync(string setId, ulong cursor, int count = 10, string match = null, CancellationToken token = default);
    ValueTask<ScanResult> HScanAsync(string hashId, ulong cursor, int count = 10, string match = null, CancellationToken token = default);

    //Hyperlog
    ValueTask<bool> PfAddAsync(string key, byte[][] elements, CancellationToken token = default);
    ValueTask<bool> PfAddAsync(string key, params byte[][] elements); // convenience API
    ValueTask<long> PfCountAsync(string key, CancellationToken token = default);
    ValueTask PfMergeAsync(string toKeyId, string[] fromKeys, CancellationToken token = default);
    ValueTask PfMergeAsync(string toKeyId, params string[] fromKeys);

    //Redis Sort operation Async(works on lists, sets or hashes)
    ValueTask<byte[][]> SortAsync(string listOrSetId, SortOptions sortOptions, CancellationToken token = default);

    //Redis List operations
    ValueTask<byte[][]> LRangeAsync(string listId, int startingFrom, int endingAt, CancellationToken token = default);
    ValueTask<long> RPushAsync(string listId, byte[] value, CancellationToken token = default);
    ValueTask<long> RPushXAsync(string listId, byte[] value, CancellationToken token = default);
    ValueTask<long> LPushAsync(string listId, byte[] value, CancellationToken token = default);
    ValueTask<long> LPushXAsync(string listId, byte[] value, CancellationToken token = default);
    ValueTask LTrimAsync(string listId, int keepStartingFrom, int keepEndingAt, CancellationToken token = default);
    ValueTask<long> LRemAsync(string listId, int removeNoOfMatches, byte[] value, CancellationToken token = default);
    ValueTask<long> LLenAsync(string listId, CancellationToken token = default);
    ValueTask<byte[]> LIndexAsync(string listId, int listIndex, CancellationToken token = default);
    ValueTask LInsertAsync(string listId, bool insertBefore, byte[] pivot, byte[] value, CancellationToken token = default);
    ValueTask LSetAsync(string listId, int listIndex, byte[] value, CancellationToken token = default);
    ValueTask<byte[]> LPopAsync(string listId, CancellationToken token = default);
    ValueTask<byte[]> RPopAsync(string listId, CancellationToken token = default);
    ValueTask<byte[][]> BLPopAsync(string listId, int timeOutSecs, CancellationToken token = default);
    ValueTask<byte[][]> BLPopAsync(string[] listIds, int timeOutSecs, CancellationToken token = default);
    ValueTask<byte[]> BLPopValueAsync(string listId, int timeOutSecs, CancellationToken token = default);
    ValueTask<byte[][]> BLPopValueAsync(string[] listIds, int timeOutSecs, CancellationToken token = default);
    ValueTask<byte[][]> BRPopAsync(string listId, int timeOutSecs, CancellationToken token = default);
    ValueTask<byte[][]> BRPopAsync(string[] listIds, int timeOutSecs, CancellationToken token = default);
    ValueTask<byte[]> RPopLPushAsync(string fromListId, string toListId, CancellationToken token = default);
    ValueTask<byte[]> BRPopValueAsync(string listId, int timeOutSecs, CancellationToken token = default);
    ValueTask<byte[][]> BRPopValueAsync(string[] listIds, int timeOutSecs, CancellationToken token = default);
    ValueTask<byte[]> BRPopLPushAsync(string fromListId, string toListId, int timeOutSecs, CancellationToken token = default);

    //Redis Set operations
    ValueTask<byte[][]> SMembersAsync(string setId, CancellationToken token = default);
    ValueTask<long> SAddAsync(string setId, byte[] value, CancellationToken token = default);
    ValueTask<long> SAddAsync(string setId, byte[][] value, CancellationToken token = default);
    ValueTask<long> SRemAsync(string setId, byte[] value, CancellationToken token = default);
    ValueTask<byte[]> SPopAsync(string setId, CancellationToken token = default);
    ValueTask<byte[][]> SPopAsync(string setId, int count, CancellationToken token = default);
    ValueTask SMoveAsync(string fromSetId, string toSetId, byte[] value, CancellationToken token = default);
    ValueTask<long> SCardAsync(string setId, CancellationToken token = default);
    ValueTask<long> SIsMemberAsync(string setId, byte[] value, CancellationToken token = default);
    ValueTask<byte[][]> SInterAsync(string[] setIds, CancellationToken token = default);
    ValueTask<byte[][]> SInterAsync(params string[] setIds); // convenience API
    ValueTask SInterStoreAsync(string intoSetId, string[] setIds, CancellationToken token = default);
    ValueTask SInterStoreAsync(string intoSetId, params string[] setIds); // convenience API
    ValueTask<byte[][]> SUnionAsync(string[] setIds, CancellationToken token = default);
    ValueTask<byte[][]> SUnionAsync(params string[] setIds); // convenience API
    ValueTask SUnionStoreAsync(string intoSetId, string[] setIds, CancellationToken token = default);
    ValueTask SUnionStoreAsync(string intoSetId, params string[] setIds); // convenience API
    ValueTask<byte[][]> SDiffAsync(string fromSetId, string[] withSetIds, CancellationToken token = default);
    ValueTask<byte[][]> SDiffAsync(string fromSetId, params string[] withSetIds); // convenience API
    ValueTask SDiffStoreAsync(string intoSetId, string fromSetId, string[] withSetIds, CancellationToken token = default);
    ValueTask SDiffStoreAsync(string intoSetId, string fromSetId, params string[] withSetIds); // convenience API
    ValueTask<byte[]> SRandMemberAsync(string setId, CancellationToken token = default);


    ////Redis Sorted Set operations
    ValueTask<long> ZAddAsync(string setId, double score, byte[] value, CancellationToken token = default);
    ValueTask<long> ZAddAsync(string setId, long score, byte[] value, CancellationToken token = default);
    ValueTask<long> ZRemAsync(string setId, byte[] value, CancellationToken token = default);
    ValueTask<long> ZRemAsync(string setId, byte[][] values, CancellationToken token = default);
    ValueTask<double> ZIncrByAsync(string setId, double incrBy, byte[] value, CancellationToken token = default);
    ValueTask<double> ZIncrByAsync(string setId, long incrBy, byte[] value, CancellationToken token = default);
    ValueTask<long> ZRankAsync(string setId, byte[] value, CancellationToken token = default);
    ValueTask<long> ZRevRankAsync(string setId, byte[] value, CancellationToken token = default);
    ValueTask<byte[][]> ZRangeAsync(string setId, int min, int max, CancellationToken token = default);
    ValueTask<byte[][]> ZRangeWithScoresAsync(string setId, int min, int max, CancellationToken token = default);
    ValueTask<byte[][]> ZRevRangeAsync(string setId, int min, int max, CancellationToken token = default);
    ValueTask<byte[][]> ZRevRangeWithScoresAsync(string setId, int min, int max, CancellationToken token = default);
    ValueTask<byte[][]> ZRangeByScoreAsync(string setId, double min, double max, int? skip, int? take, CancellationToken token = default);
    ValueTask<byte[][]> ZRangeByScoreAsync(string setId, long min, long max, int? skip, int? take, CancellationToken token = default);
    ValueTask<byte[][]> ZRangeByScoreWithScoresAsync(string setId, double min, double max, int? skip, int? take, CancellationToken token = default);
    ValueTask<byte[][]> ZRangeByScoreWithScoresAsync(string setId, long min, long max, int? skip, int? take, CancellationToken token = default);
    ValueTask<byte[][]> ZRevRangeByScoreAsync(string setId, double min, double max, int? skip, int? take, CancellationToken token = default);
    ValueTask<byte[][]> ZRevRangeByScoreAsync(string setId, long min, long max, int? skip, int? take, CancellationToken token = default);
    ValueTask<byte[][]> ZRevRangeByScoreWithScoresAsync(string setId, double min, double max, int? skip, int? take, CancellationToken token = default);
    ValueTask<byte[][]> ZRevRangeByScoreWithScoresAsync(string setId, long min, long max, int? skip, int? take, CancellationToken token = default);
    ValueTask<long> ZRemRangeByRankAsync(string setId, int min, int max, CancellationToken token = default);
    ValueTask<long> ZRemRangeByScoreAsync(string setId, double fromScore, double toScore, CancellationToken token = default);
    ValueTask<long> ZRemRangeByScoreAsync(string setId, long fromScore, long toScore, CancellationToken token = default);
    ValueTask<long> ZCardAsync(string setId, CancellationToken token = default);
    ValueTask<long> ZCountAsync(string setId, double min, double max, CancellationToken token = default);
    ValueTask<double>  ZScoreAsync(string setId, byte[] value, CancellationToken token = default);
    ValueTask<long> ZUnionStoreAsync(string intoSetId, string[] setIds, CancellationToken token = default);
    ValueTask<long> ZUnionStoreAsync(string intoSetId, params string[] setIds); // convenience API
    ValueTask<long> ZInterStoreAsync(string intoSetId, string[] setIds, CancellationToken token = default);
    ValueTask<long> ZInterStoreAsync(string intoSetId, params string[] setIds); // convenience API
    ValueTask<byte[][]> ZRangeByLexAsync(string setId, string min, string max, int? skip = null, int? take = null, CancellationToken token = default);
    ValueTask<long> ZLexCountAsync(string setId, string min, string max, CancellationToken token = default);
    ValueTask<long> ZRemRangeByLexAsync(string setId, string min, string max, CancellationToken token = default);

    ////Redis Hash operations
    ValueTask<long> HSetAsync(string hashId, byte[] key, byte[] value, CancellationToken token = default);
    ValueTask HMSetAsync(string hashId, byte[][] keys, byte[][] values, CancellationToken token = default);
    ValueTask<long> HSetNXAsync(string hashId, byte[] key, byte[] value, CancellationToken token = default);
    ValueTask<long> HIncrbyAsync(string hashId, byte[] key, int incrementBy, CancellationToken token = default);
    ValueTask<double> HIncrbyFloatAsync(string hashId, byte[] key, double incrementBy, CancellationToken token = default);
    ValueTask<byte[]> HGetAsync(string hashId, byte[] key, CancellationToken token = default);
    ValueTask<byte[][]> HMGetAsync(string hashId, byte[][] keysAndArgs, CancellationToken token = default);
    ValueTask<byte[][]> HMGetAsync(string hashId, params byte[][] keysAndArgs); // convenience API
    ValueTask<long> HDelAsync(string hashId, byte[] key, CancellationToken token = default);
    ValueTask<long> HExistsAsync(string hashId, byte[] key, CancellationToken token = default);
    ValueTask<long> HLenAsync(string hashId, CancellationToken token = default);
    ValueTask<byte[][]> HKeysAsync(string hashId, CancellationToken token = default);
    ValueTask<byte[][]> HValsAsync(string hashId, CancellationToken token = default);
    ValueTask<byte[][]> HGetAllAsync(string hashId, CancellationToken token = default);

    //Redis GEO operations
    ValueTask<long> GeoAddAsync(string key, double longitude, double latitude, string member, CancellationToken token = default);
    ValueTask<long> GeoAddAsync(string key, RedisGeo[] geoPoints, CancellationToken token = default);
    ValueTask<long> GeoAddAsync(string key, params RedisGeo[] geoPoints); // convenience API
    ValueTask<double> GeoDistAsync(string key, string fromMember, string toMember, string unit = null, CancellationToken token = default);
    ValueTask<string[]> GeoHashAsync(string key, string[] members, CancellationToken token = default);
    ValueTask<string[]> GeoHashAsync(string key, params string[] members); // convenience API
    ValueTask<List<RedisGeo>> GeoPosAsync(string key, string[] members, CancellationToken token = default);
    ValueTask<List<RedisGeo>> GeoPosAsync(string key, params string[] members); // convenience API
    ValueTask<List<RedisGeoResult>> GeoRadiusAsync(string key, double longitude, double latitude, double radius, string unit,
        bool withCoords = false, bool withDist = false, bool withHash = false, int? count = null, bool? asc = null, CancellationToken token = default);
    ValueTask<List<RedisGeoResult>> GeoRadiusByMemberAsync(string key, string member, double radius, string unit,
        bool withCoords = false, bool withDist = false, bool withHash = false, int? count = null, bool? asc = null, CancellationToken token = default);

    //Redis Pub/Sub operations
    ValueTask WatchAsync(string[] keys, CancellationToken token = default);
    ValueTask WatchAsync(params string[] keys); // convenience API
    ValueTask UnWatchAsync(CancellationToken token = default);
    ValueTask<long> PublishAsync(string toChannel, byte[] message, CancellationToken token = default);
    ValueTask<byte[][]> SubscribeAsync(string[] toChannels, CancellationToken token = default);
    ValueTask<byte[][]> SubscribeAsync(params string[] toChannels); // convenience API
    ValueTask<byte[][]> UnSubscribeAsync(string[] toChannels, CancellationToken token = default);
    ValueTask<byte[][]> UnSubscribeAsync(params string[] toChannels); // convenience API
    ValueTask<byte[][]> PSubscribeAsync(string[] toChannelsMatchingPatterns, CancellationToken token = default);
    ValueTask<byte[][]> PSubscribeAsync(params string[] toChannelsMatchingPatterns); // convenience API
    ValueTask<byte[][]> PUnSubscribeAsync(string[] toChannelsMatchingPatterns, CancellationToken token = default);
    ValueTask<byte[][]> PUnSubscribeAsync(params string[] toChannelsMatchingPatterns); // convenience API
    ValueTask<byte[][]> ReceiveMessagesAsync(CancellationToken token = default);
    ValueTask<IRedisSubscriptionAsync> CreateSubscriptionAsync(CancellationToken token = default);

    //Redis LUA support
    ValueTask<RedisData> EvalCommandAsync(string luaBody, int numberKeysInArgs, byte[][] keys, CancellationToken token = default);
    ValueTask<RedisData> EvalCommandAsync(string luaBody, int numberKeysInArgs, params byte[][] keys); // convenience API
    ValueTask<RedisData> EvalShaCommandAsync(string sha1, int numberKeysInArgs, byte[][] keys, CancellationToken token = default);
    ValueTask<RedisData> EvalShaCommandAsync(string sha1, int numberKeysInArgs, params byte[][] keys); // convenience API

    ValueTask<byte[][]> EvalAsync(string luaBody, int numberOfKeys, byte[][] keysAndArgs, CancellationToken token = default);
    ValueTask<byte[][]> EvalAsync(string luaBody, int numberOfKeys, params byte[][] keysAndArgs); // convenience API
    ValueTask<byte[][]> EvalShaAsync(string sha1, int numberOfKeys, byte[][] keysAndArgs, CancellationToken token = default);
    ValueTask<byte[][]> EvalShaAsync(string sha1, int numberOfKeys, params byte[][] keysAndArgs); // convenience API

    ValueTask<long> EvalIntAsync(string luaBody, int numberOfKeys, byte[][] keysAndArgs, CancellationToken token = default);
    ValueTask<long> EvalIntAsync(string luaBody, int numberOfKeys, params byte[][] keysAndArgs); // convenience API
    ValueTask<long> EvalShaIntAsync(string sha1, int numberOfKeys, byte[][] keysAndArgs, CancellationToken token = default);
    ValueTask<long> EvalShaIntAsync(string sha1, int numberOfKeys, params byte[][] keysAndArgs); // convenience API
    ValueTask<string> EvalStrAsync(string luaBody, int numberOfKeys, byte[][] keysAndArgs, CancellationToken token = default);
    ValueTask<string> EvalStrAsync(string luaBody, int numberOfKeys, params byte[][] keysAndArgs); // convenience API
    ValueTask<string> EvalShaStrAsync(string sha1, int numberOfKeys, byte[][] keysAndArgs, CancellationToken token = default);
    ValueTask<string> EvalShaStrAsync(string sha1, int numberOfKeys, params byte[][] keysAndArgs); // convenience API

    ValueTask<string> CalculateSha1Async(string luaBody, CancellationToken token = default);
    ValueTask<byte[][]> ScriptExistsAsync(byte[][] sha1Refs, CancellationToken token = default);
    ValueTask<byte[][]> ScriptExistsAsync(params byte[][] sha1Refs); // convenience API
    ValueTask ScriptFlushAsync(CancellationToken token = default);
    ValueTask ScriptKillAsync(CancellationToken token = default);
    ValueTask<byte[]> ScriptLoadAsync(string body, CancellationToken token = default);

    ValueTask SlowlogResetAsync(CancellationToken token = default);
    ValueTask<object[]> SlowlogGetAsync(int? top = null, CancellationToken token = default);
}