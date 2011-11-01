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

namespace ServiceStack.Redis
{
	public interface IRedisNativeClient
		: IDisposable
	{
		//Redis utility operations
		Dictionary<string, string> Info { get; }
		int Db { get; set; }
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

		//Common key-value Redis operations
		void Set(string key, byte[] value);
		void SetEx(string key, int expireInSeconds, byte[] value);
		int SetNX(string key, byte[] value);
		byte[] Get(string key);
		byte[] GetSet(string key, byte[] value);
		int Del(string key);
		long Incr(string key);
		long IncrBy(string key, int count);
		long Decr(string key);
		long DecrBy(string key, int count);
		int Append(string key, byte[] value);
		byte[] Substr(string key, int fromIndex, int toIndex);

		string RandomKey();
		void Rename(string oldKeyname, string newKeyname);
		int Expire(string key, int seconds);
		int ExpireAt(string key, long unixTime);
		int Ttl(string key);

		//Redis Sort operation (works on lists, sets or hashes)
		byte[][] Sort(string listOrSetId, SortOptions sortOptions);

		//Redis List operations
		byte[][] LRange(string listId, int startingFrom, int endingAt);
		int RPush(string listId, byte[] value);
		int LPush(string listId, byte[] value);
		void LTrim(string listId, int keepStartingFrom, int keepEndingAt);
		int LRem(string listId, int removeNoOfMatches, byte[] value);
		int LLen(string listId);
		byte[] LIndex(string listId, int listIndex);
		void LSet(string listId, int listIndex, byte[] value);
		byte[] LPop(string listId);
		byte[] RPop(string listId);
		byte[][] BLPop(string listId, int timeOutSecs);
		byte[][] BRPop(string listId, int timeOutSecs);
		byte[] RPopLPush(string fromListId, string toListId);


		//Redis Set operations
		byte[][] SMembers(string setId);
		int SAdd(string setId, byte[] value);
		int SRem(string setId, byte[] value);
		byte[] SPop(string setId);
		void SMove(string fromSetId, string toSetId, byte[] value);
		int SCard(string setId);
		int SIsMember(string setId, byte[] value);
		byte[][] SInter(params string[] setIds);
		void SInterStore(string intoSetId, params string[] setIds);
		byte[][] SUnion(params string[] setIds);
		void SUnionStore(string intoSetId, params string[] setIds);
		byte[][] SDiff(string fromSetId, params string[] withSetIds);
		void SDiffStore(string intoSetId, string fromSetId, params string[] withSetIds);
		byte[] SRandMember(string setId);


		//Redis Sorted Set operations
		int ZAdd(string setId, double score, byte[] value);
		int ZAdd(string setId, long score, byte[] value);
		int ZRem(string setId, byte[] value);
		double ZIncrBy(string setId, double incrBy, byte[] value);
		double ZIncrBy(string setId, long incrBy, byte[] value);
		int ZRank(string setId, byte[] value);
		int ZRevRank(string setId, byte[] value);
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
		int ZRemRangeByRank(string setId, int min, int max);
		int ZRemRangeByScore(string setId, double fromScore, double toScore);
		int ZRemRangeByScore(string setId, long fromScore, long toScore);
		int ZCard(string setId);
		double ZScore(string setId, byte[] value);
		int ZUnionStore(string intoSetId, params string[] setIds);
		int ZInterStore(string intoSetId, params string[] setIds);

		//Redis Hash operations
		int HSet(string hashId, byte[] key, byte[] value);
		int HSetNX(string hashId, byte[] key, byte[] value);
		void HMSet(string hashId, byte[][] keys, byte[][] values);
		int HIncrby(string hashId, byte[] key, int incrementBy);
		byte[] HGet(string hashId, byte[] key);
		int HDel(string hashId, byte[] key);
		int HExists(string hashId, byte[] key);
		int HLen(string hashId);
		byte[][] HKeys(string hashId);
		byte[][] HVals(string hashId);
		byte[][] HGetAll(string hashId);

		//Redis Pub/Sub operations
		int Publish(string toChannel, byte[] message);
		byte[][] Subscribe(params string[] toChannels);
		byte[][] UnSubscribe(params string[] toChannels);
		byte[][] PSubscribe(params string[] toChannelsMatchingPatterns);
		byte[][] PUnSubscribe(params string[] toChannelsMatchingPatterns);
		byte[][] ReceiveMessages();
	}
}