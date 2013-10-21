//
// https://github.com/ServiceStack/ServiceStack.Redis/
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack.
//
// Licensed under the same terms of Redis and ServiceStack: new BSD license.
//

using System;
using System.Collections.Generic;

namespace ServiceStack.Redis
{
	public interface IRedisNativeClient
		: IDisposable
	{
		//Redis utility operations
		Dictionary<string, string> Info { get; }
		long Db { get; set; }
		long DbSize { get; }
		DateTime LastSave { get; }
		void Save();
		void BgSave();
		void Shutdown();
		void BgRewriteAof();
		void Quit();
		void FlushDb();
		void FlushAll();
		bool Ping();
		string Echo(string text);
		void SlaveOf(string hostname, int port);
		void SlaveOfNoOne();
		byte[][] ConfigGet(string pattern);
		void ConfigSet(string item, byte[] value);
		void ConfigResetStat();
		byte[][] Time();
		void DebugSegfault();
		byte[] Dump(string key);
		byte[] Restore(string key, long expireMs, byte[] dumpValue);
		void Migrate(string host, int port, int destinationDb, long timeoutMs);
		bool Move(string key, int db);
		long ObjectIdleTime(string key);

		//Common key-value Redis operations
		byte[][] Keys(string pattern);
		long Exists(string key);
		long StrLen(string key);
		void Set(string key, byte[] value);
		void SetEx(string key, int expireInSeconds, byte[] value);
		bool Persist(string key);
		void PSetEx(string key, long expireInMs, byte[] value);
		long SetNX(string key, byte[] value);
		void MSet(byte[][] keys, byte[][] values);
		void MSet(string[] keys, byte[][] values);
		bool MSetNx(byte[][] keys, byte[][] values);
		bool MSetNx(string[] keys, byte[][] values);
		byte[] Get(string key);
		byte[] GetSet(string key, byte[] value);
		byte[][] MGet(params byte[][] keysAndArgs);
		byte[][] MGet(params string[] keys);
		long Del(string key);
		long Del(params string[] keys);
		long Incr(string key);
		long IncrBy(string key, int incrBy);
		double IncrByFloat(string key, double incrBy);
		long Decr(string key);
		long DecrBy(string key, int decrBy);
		long Append(string key, byte[] value);
		[Obsolete("Was renamed to GetRange in 2.4")]
		byte[] Substr(string key, int fromIndex, int toIndex);
		byte[] GetRange(string key, int fromIndex, int toIndex);
		long SetRange(string key, int offset, byte[] value);
		long GetBit(string key, int offset);
		long SetBit(string key, int offset, int value);
		
		string RandomKey();
		void Rename(string oldKeyname, string newKeyname);
		bool RenameNx(string oldKeyname, string newKeyname);
		bool Expire(string key, int seconds);
		bool PExpire(string key, long ttlMs);
		bool ExpireAt(string key, long unixTime);
		bool PExpireAt(string key, long unixTimeMs);
		long Ttl(string key);
		long PTtl(string key);

		//Redis Sort operation (works on lists, sets or hashes)
		byte[][] Sort(string listOrSetId, SortOptions sortOptions);

		//Redis List operations
		byte[][] LRange(string listId, int startingFrom, int endingAt);
		long RPush(string listId, byte[] value);
		long RPushX(string listId, byte[] value);
		long LPush(string listId, byte[] value);
		long LPushX(string listId, byte[] value);
		void LTrim(string listId, int keepStartingFrom, int keepEndingAt);
		long LRem(string listId, int removeNoOfMatches, byte[] value);
		long LLen(string listId);
		byte[] LIndex(string listId, int listIndex);
        void LInsert(string listId, bool insertBefore, byte[] pivot, byte[] value);
		void LSet(string listId, int listIndex, byte[] value);
		byte[] LPop(string listId);
		byte[] RPop(string listId);
		byte[][] BLPop(string listId, int timeOutSecs);
	    byte[][] BLPop(string[] listIds, int timeOutSecs);
		byte[] BLPopValue(string listId, int timeOutSecs);
	    byte[][] BLPopValue(string[] listIds, int timeOutSecs);
		byte[][] BRPop(string listId, int timeOutSecs);
	    byte[][] BRPop(string[] listIds, int timeOutSecs);
		byte[] RPopLPush(string fromListId, string toListId);
		byte[] BRPopValue(string listId, int timeOutSecs);
        byte[][] BRPopValue(string[] listIds, int timeOutSecs);
        byte[] BRPopLPush(string fromListId, string toListId, int timeOutSecs);

		//Redis Set operations
		byte[][] SMembers(string setId);
        long SAdd(string setId, byte[] value);
        long SAdd(string setId, byte[][] values);
		long SRem(string setId, byte[] value);
		byte[] SPop(string setId);
		void SMove(string fromSetId, string toSetId, byte[] value);
		long SCard(string setId);
		long SIsMember(string setId, byte[] value);
		byte[][] SInter(params string[] setIds);
		void SInterStore(string intoSetId, params string[] setIds);
		byte[][] SUnion(params string[] setIds);
		void SUnionStore(string intoSetId, params string[] setIds);
		byte[][] SDiff(string fromSetId, params string[] withSetIds);
		void SDiffStore(string intoSetId, string fromSetId, params string[] withSetIds);
		byte[] SRandMember(string setId);


		//Redis Sorted Set operations
		long ZAdd(string setId, double score, byte[] value);
		long ZAdd(string setId, long score, byte[] value);
		long ZRem(string setId, byte[] value);
		double ZIncrBy(string setId, double incrBy, byte[] value);
		double ZIncrBy(string setId, long incrBy, byte[] value);
		long ZRank(string setId, byte[] value);
		long ZRevRank(string setId, byte[] value);
		byte[][] ZRange(string setId, int min, int max);
		byte[][] ZRangeWithScores(string setId, int min, int max);
		byte[][] ZRevRange(string setId, int min, int max);
		byte[][] ZRevRangeWithScores(string setId, int min, int max);
		byte[][] ZRangeByScore(string setId, double min, double max, int? skip, int? take);
		byte[][] ZRangeByScore(string setId, long min, long max, int? skip, int? take);
		byte[][] ZRangeByScoreWithScores(string setId, double min, double max, int? skip, int? take);
		byte[][] ZRangeByScoreWithScores(string setId, long min, long max, int? skip, int? take);
		byte[][] ZRevRangeByScore(string setId, double min, double max, int? skip, int? take);
		byte[][] ZRevRangeByScore(string setId, long min, long max, int? skip, int? take);
		byte[][] ZRevRangeByScoreWithScores(string setId, double min, double max, int? skip, int? take);
		byte[][] ZRevRangeByScoreWithScores(string setId, long min, long max, int? skip, int? take);
		long ZRemRangeByRank(string setId, int min, int max);
		long ZRemRangeByScore(string setId, double fromScore, double toScore);
		long ZRemRangeByScore(string setId, long fromScore, long toScore);
		long ZCard(string setId);
		double ZScore(string setId, byte[] value);
		long ZUnionStore(string intoSetId, params string[] setIds);
		long ZInterStore(string intoSetId, params string[] setIds);

		//Redis Hash operations
		long HSet(string hashId, byte[] key, byte[] value);
		void HMSet(string hashId, byte[][] keys, byte[][] values);
		long HSetNX(string hashId, byte[] key, byte[] value);
		long HIncrby(string hashId, byte[] key, int incrementBy);
		double HIncrbyFloat(string hashId, byte[] key, double incrementBy);
		byte[] HGet(string hashId, byte[] key);
		byte[][] HMGet(string hashId, params byte[][] keysAndArgs);
		long HDel(string hashId, byte[] key);
		long HExists(string hashId, byte[] key);
		long HLen(string hashId);
		byte[][] HKeys(string hashId);
		byte[][] HVals(string hashId);
		byte[][] HGetAll(string hashId);

		//Redis Pub/Sub operations
		void Watch(params string[] keys);
		void UnWatch();
		long Publish(string toChannel, byte[] message);
		byte[][] Subscribe(params string[] toChannels);
		byte[][] UnSubscribe(params string[] toChannels);
		byte[][] PSubscribe(params string[] toChannelsMatchingPatterns);
		byte[][] PUnSubscribe(params string[] toChannelsMatchingPatterns);
		byte[][] ReceiveMessages();

        //Redis LUA support
		long EvalInt(string luaBody, int numberOfKeys, params byte[][] keysAndArgs);
		long EvalShaInt(string sha1, int numberOfKeys, params byte[][] keysAndArgs);
        string EvalStr(string luaBody, int numberOfKeys, params byte[][] keysAndArgs);
        string EvalShaStr(string sha1, int numberOfKeys, params byte[][] keysAndArgs);
        byte[][] Eval(string luaBody, int numberOfKeys, params byte[][] keysAndArgs);
        byte[][] EvalSha(string sha1, int numberOfKeys, params byte[][] keysAndArgs);

        string CalculateSha1(string luaBody);
        byte[][] ScriptExists(params byte[][] sha1Refs);
		void ScriptFlush();
		void ScriptKill();
		byte[] ScriptLoad(string body);
	}
}